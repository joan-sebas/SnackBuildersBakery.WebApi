# ADR 0002: In-memory Scheduler Coordinator with Single Mutation Lock

## Context

Kitchen capacity is configurable through scheduler settings: oven count, trays per oven, turnover duration, tier weights, aging factor, and bake times. The current local defaults are two ovens and three trays per oven, but the domain policy must not depend on those concrete numbers.

The scheduling policy must remain deterministic and testable as a pure function. Runtime coordination still needs a clear owner for live state so concurrent payments, queue updates, and background reconciliation cannot mutate the same kitchen projection in conflicting ways.

## Decision

### Pure Policy in Domain

`KitchenScheduler.Reconcile(state, now)` is a pure function. It receives immutable input state and an explicit timestamp, then returns a new immutable state. It does not read the system clock, access persistence, log, publish metrics, or mutate shared objects.

`SelectionRank` is the single source of the queue ordering rule: score first, then base tier, then FIFO. `KitchenQueue.SelectNext`, reconciliation, and ready-time estimation rely on that same ranking model so queue behavior cannot diverge across code paths.

### Live State Owned by Application Coordinator

`SchedulerCoordinator`, exposed through `ISchedulerCoordinator`, owns the mutable runtime reference to `SchedulerState`. It serializes mutations with one `SemaphoreSlim(1, 1)` and uses injected `TimeProvider` as the only runtime source of `now`.

Every mutation follows the same flow:

1. Acquire the coordinator lock.
2. Read `now` from `TimeProvider`.
3. Apply the requested state change.
4. Run `KitchenScheduler.Reconcile`.
5. Publish the committed state snapshot.
6. Release the lock in `finally`.

Reads use the latest committed snapshot and avoid holding the mutation lock. This keeps monitoring and estimate reads cheap while preserving mutation safety.

### State Derived From Order Items

No separate scheduler-state or slot table is stored as the durable source of truth. On startup, the runtime scheduler projection is reconstructed from queued and baking `OrderItem` rows and their absolute timestamps.

This avoids maintaining two durable sources of truth: order items remain the persisted business record, and scheduler state remains a runtime projection.

### Estimated Ready Time Computed on Read

`EstimateReadyTimes` computes a fresh projection from the current scheduler snapshot. Estimates are not cached or persisted because every mutation can change queue ordering, slot availability, and aging score.

### VIP Recalculation Emerges From the Ranking Rule

VIP arrival does not require a dedicated mutation path. A VIP item enters the same queue model as every other item, and the ranking rule places it appropriately on the next reconciliation/estimate. Lower-priority estimates shift naturally because estimates are computed from the current ordering.

## Consequences

- Domain scheduling behavior is easy to test without mocks because all time and state are explicit inputs.
- Runtime mutation safety is concentrated in one application service instead of spread across endpoints, repositories, or background services.
- The in-memory coordinator is suitable for a single API process. Multiple active API instances would require partitioning by kitchen, a DB-backed coordinator, distributed locking, or an external queue.
- Reconstructing scheduler state from order items gives restart resilience without persisting a second slot model.
- Computed estimates avoid cache invalidation, but callers pay a small computation cost on each estimate read.

## Alternatives Considered

- DB-backed coordinator from day one: rejected because it would add locking and persistence complexity before the assignment needs horizontal runtime coordination.
- Persisted slot table as the primary source of truth: rejected because it duplicates information already represented by order-item timestamps and creates divergence risk.
- Stored estimate cache on VIP arrival: rejected because estimates can change after any queue or slot mutation and would need broad invalidation logic.
- Dedicated VIP enqueue path: rejected because it would duplicate the selection rule and make priority behavior harder to audit.

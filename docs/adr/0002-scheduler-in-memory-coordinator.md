# ADR 0002: In-memory scheduler coordinator with single mutation lock

Narrows ADR 0001 §"Scheduler state is owned in-memory" with the specific design
adopted during M2.

## Context

The kitchen has fixed physical capacity: 2 ovens × 3 trays = 6 baking slots. A single
coordinator owns all slot state at runtime. The scheduling policy — priority aging,
queue selection, slot lifecycle — must be deterministically testable as a pure function
with no clock or side-effect dependencies.

The implementation must make concurrency control explicit and auditable in-process, which
requires a clearly owned lock boundary rather than implicit optimistic retries or
infrastructure-level serialization.

## Decision

### Pure policy in Domain

`KitchenScheduler.Reconcile(state, now)` is a pure function: immutable input, immutable
output, no clock reads, no mutable state. Given the same `(state, now)` pair it always
returns the same result. This makes the scheduling behavior unit-testable without mocks.

`SelectionRank` is the single source of the queue selection rule (score → tier → FIFO).
Both `KitchenQueue.SelectNext` and the `Reconcile` assignment phase reference it, so the
two sites cannot diverge.

### Live state owned by Application coordinator

`SchedulerCoordinator` (behind `ISchedulerCoordinator`) is the only place that holds a
mutable reference to `SchedulerState`. It owns:

- One `SemaphoreSlim(1, 1)` that serializes every mutation.
- An injected `TimeProvider` that is the sole source of `now` for mutations.
- A `SchedulerState` field published via `Volatile.Write` so lock-free reads always
  see a fully committed snapshot.

Every mutation (`EnqueueAsync`, `ReconcileAsync`) follows: await lock → read `now` →
apply change → `KitchenScheduler.Reconcile` → publish new state → release lock in
`finally`. Reads (`GetSnapshot`, `EstimateReadyTimes`) access `Volatile.Read(_state)`
with no lock.

### State derived from order-items, not persisted separately

No `SchedulerState` snapshot is stored in the database. On startup the coordinator
reconstructs state from queued and in-progress `OrderItem` rows using their absolute
timestamps (`StartedBakingAt`, `BakingEndsAt`). This keeps the database as the durable
source of truth and avoids a second, potentially divergent slot table.

### Estimated ready time computed on read

`EstimateReadyTimes` simulates slot free-times against the current queue ordering and
returns a fresh projection each call. Nothing is cached or stored, so estimates are
always consistent with committed state and require no invalidation logic.

### VIP recalculation emergent, no dedicated path

When a VIP item is enqueued, `SelectionRank` places it at the front of the queue on the
next `Reconcile`. Lower-priority queued estimates shift automatically on the next
`EstimateReadyTimes` read. No separate VIP-arrival mutation is needed; the single-source
selection rule subsumes it.

## Consequences

- The Domain policy and the concurrency boundary are independently testable: pure-function
  tests need no coordinator; coordinator tests need no time-advance complexity beyond
  `FakeTimeProvider`.
- The ISchedulerCoordinator port isolates the in-memory design from all consumers. Replacing
  the in-memory coordinator with a DB-backed or partitioned one is a single adapter swap that
  touches neither `KitchenScheduler` nor any use case.
- Scaling path: partition by kitchen (one coordinator per physical kitchen), restore state
  from `OrderItem` rows on failover. The pure policy and the port contract carry forward
  unchanged.
- Single-process ownership is the trade-off today: two application instances cannot share
  live slot state without coordination. Accepted for the current single-kitchen scope.

## Alternatives Considered

- **DB-backed coordinator from day one**: rejected. It would add persistence complexity
  before the domain model is stable, obscure the in-process concurrency behavior that the
  design is meant to demonstrate, and provide no real headroom given the 6-slot physical
  ceiling of a single kitchen.
- **Stored estimate cache on VIP arrival**: rejected. Storing a pre-computed estimate
  introduces a second source of truth that must be invalidated on every mutation. The
  computed-on-read model is strictly simpler and always consistent.
- **Dedicated VIP-enqueue mutation path**: rejected. A separate code path for VIP would
  duplicate the selection rule and create a divergence risk. The single-source
  `SelectionRank` already handles any priority level uniformly.

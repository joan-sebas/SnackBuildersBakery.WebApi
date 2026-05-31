# VIP scheduling sequence

A VIP item arrives via `SchedulerCoordinator.EnqueueAsync`. The coordinator serializes the
mutation under a single `SemaphoreSlim(1,1)`, delegates the pure scheduling decision to
`KitchenScheduler.Reconcile`, and publishes the new state lock-free via `Volatile.Write`.
The subsequent estimate read requires no lock.

## Mutation path (under the single lock)

```mermaid
sequenceDiagram
    participant C as Client
    participant Coord as SchedulerCoordinator
    participant TP as TimeProvider
    participant Sched as KitchenScheduler

    C->>+Coord: EnqueueAsync(vip)
    Coord->>Coord: SemaphoreSlim.WaitAsync()
    Note over Coord: lock acquired — all other mutations wait here
    Coord->>TP: GetUtcNow()
    TP-->>Coord: now
    Coord->>Coord: Append(state, vip, now)
    Note over Coord: adds VIP as Queued with EnqueuedAt = now

    Coord->>+Sched: Reconcile(state, now)
    Note over Sched: SelectionRank re-scores the full queue<br/>VIP (tierWeight=30 + aging) outranks<br/>Delivery (20) and WalkIn (10) at equal wait
    alt free slot available
        Sched->>Sched: assign VIP to slot
        Note over Sched: VIP to Baking, slot to Baking<br/>lower-priority items remain Queued
    else no free slot
        Note over Sched: VIP stays Queued, ranked first<br/>ahead of lower-priority queued items
    end
    Note over Sched: Baking items not touched<br/>BakeFinished only when BakingEndsAt <= now<br/>Baking to Queued transition is forbidden
    Sched-->>-Coord: new SchedulerState

    Coord->>Coord: Volatile.Write(_state, newState)
    Coord->>Coord: SemaphoreSlim.Release()
    Note over Coord: lock released
    Coord-->>-C: new SchedulerState
```

## Read path (lock-free)

After the mutation completes, callers read estimates without acquiring the lock.

```mermaid
sequenceDiagram
    participant C as Client
    participant Coord as SchedulerCoordinator
    participant TP as TimeProvider
    participant Sched as KitchenScheduler

    C->>+Coord: EstimateReadyTimes()
    Coord->>TP: GetUtcNow()
    TP-->>Coord: now
    Coord->>Coord: Volatile.Read(_state)
    Note over Coord: no lock acquired
    Coord->>+Sched: EstimateReadyTimes(state, now)
    Note over Sched: walks queue in SelectionRank order<br/>VIP ranked first, assigned earliest free slot<br/>lower-priority items shifted by bake+turnover each<br/>baking items report their BakingEndsAt
    Sched-->>-Coord: estimates map
    Coord-->>-C: estimates
```

## Lock boundary summary

| Operation | Lock | State change |
|-----------|------|--------------|
| `EnqueueAsync` | acquired | appends item, runs Reconcile, publishes new state |
| `ReconcileAsync` | acquired | runs Reconcile on current state, publishes new state |
| `GetSnapshot` | none | returns `Volatile.Read(_state)` |
| `EstimateReadyTimes` | none | reads snapshot, calls pure Domain projection |

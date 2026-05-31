# concurrency-tester

## Description

Define repeatable test patterns for scheduler correctness under randomized and concurrent execution.

## When To Run

- Run when scheduler selection, reconciliation, or locking behavior changes.
- Run before merging scheduler-related features.

## Invariants To Assert

1. Active baking count never exceeds slot capacity (default maximum: 6 slots).
2. An item already in `Baking` is never preempted back to `Queued`.
3. No starvation: waiting items eventually progress under aging policy.

## Procedure

1. Build property-based tests with FsCheck over randomized sequences of:
   - New paid order items.
   - VIP arrivals.
   - Clock advances.
2. Use `FakeTimeProvider` to advance time deterministically.
3. After each generated step, run scheduler reconciliation and assert invariants.
4. Add N-parallel-operations tests that issue concurrent enqueue/reconcile calls.
5. Assert post-run invariants and state consistency after all tasks complete.

## Output Format

```text
Test plan: concurrency + property-based
Invariants:
- capacity <= 6
- no baking preemption
- no starvation
Coverage focus: <paths and operations>
Result: PASS|FAIL with file:line findings
```

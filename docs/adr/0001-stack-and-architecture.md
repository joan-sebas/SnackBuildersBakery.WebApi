# ADR 0001: Stack and Architecture Baseline

## Context

The project requires a high-performance bakery backend with strict scheduling behavior, deterministic testability, and clear architectural boundaries. The implementation must prioritize correctness under concurrency and preserve maintainability during iterative milestones.

The selected runtime and language baseline is .NET 10 with C# 14. The system currently runs as a single deployable and uses minimal APIs at the edge.

## Decision

Use Clean Architecture with four layers and inward dependency direction:

- `Domain`: business rules and scheduling policy.
- `Application`: use-case orchestration and ports.
- `Infrastructure`: EF Core, persistence, external adapters, time and payment implementations.
- `Api`: minimal API endpoints and transport concerns.

Use a modular monolith as a single deployable process.

The scheduler state is owned in-memory within one process and protected by process-level locks. This keeps a single owner of oven-slot truth and avoids distributed locking and coordination overhead at this stage.

Patterns used at a high level:

- Strategy for aging and payment behavior variants.
- Domain service for scheduler policy orchestration at the domain boundary.
- Port and adapter boundaries for infrastructure substitutions.
- Repository for persistence access behind domain-oriented contracts.
- Factory for aggregate creation flows that require consistent setup.
- Domain events for internal decoupled reactions.

## Consequences

- Architectural boundaries remain explicit and testable.
- Scheduler concurrency behavior stays deterministic in-process.
- The deployment model remains simple during early milestones.
- Future scale path remains open by partitioning ownership by kitchen and introducing additional processes without rewriting domain policy.
- The team must enforce boundary discipline to avoid layer leakage.

## Alternatives Considered

- Distributed microservices from the start: rejected due to unnecessary distributed coordination complexity for a single-kitchen capacity model.
- Layer collapse into a single project: rejected due to reduced test isolation and weaker boundary control.

# ADR 0001: Stack and Architecture Baseline

## Context

The project requires a bakery backend with deterministic scheduling behavior, explicit concurrency boundaries, and a delivery path that can be verified from a fresh clone. The implementation must prioritize correctness, testability, and maintainability across iterative milestones.

The selected runtime and language baseline is .NET 10 with C# 14. The system runs as a single deployable API process and uses minimal APIs at the HTTP edge.

## Decision

Use Clean Architecture with inward dependency direction:

- `Domain`: business entities, value objects, domain errors, and pure scheduling policy.
- `Application`: use cases, ports, domain events, and runtime scheduler coordination contracts.
- `Infrastructure`: EF Core persistence, repositories, payment simulation, idempotency persistence, scheduler configuration, and scheduler reconstruction.
- `Api`: HTTP endpoints, authentication/authorization, Problem Details, logging, metrics, health checks, OpenAPI/Scalar, Docker-facing startup, and hosted runtime workers.

Use a modular monolith as a single deployable process. This keeps the assignment scope concrete while preserving clear boundaries that can be split later if scale requires it.

The scheduler is modeled as domain policy plus an application-level coordinator. Domain owns the scheduling rules; Application owns the mutable runtime state boundary; Infrastructure rebuilds the state from durable order-item rows; Api starts the runtime worker that advances time-driven transitions.

High-level design patterns are documented separately in [ADR 0004](0004-design-patterns-and-boundary-choices.md). Runtime delivery and observability decisions are documented in [ADR 0003](0003-runtime-delivery-and-observability.md).

## Consequences

- Business rules remain independent from HTTP, EF Core, logging, metrics, and Docker.
- The domain scheduler can be tested as pure policy, while runtime concerns are tested at the application/API boundary.
- The deployment model stays simple for local development and assessment.
- The architecture keeps a future scale path open by replacing adapters or partitioning scheduler ownership without rewriting domain policy.
- Boundary discipline must be enforced during changes; the composition root is responsible for wiring cross-cutting behavior.

## Alternatives Considered

- Distributed microservices from the start: rejected because the single-kitchen assignment would pay distributed coordination costs before there is evidence of a scaling need.
- Layer collapse into a single project: rejected because it weakens test isolation, makes side effects harder to control, and obscures which code owns business rules.
- Persisting every scheduler slot as the primary model: rejected for the baseline because order-item timestamps are enough durable state, while live slot assignment is a runtime projection documented in ADR 0002.

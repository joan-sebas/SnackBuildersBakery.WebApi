# ADR 0004: Design Patterns and Boundary Choices

## Context

The assignment asks for insight into design thinking, not only working endpoints. The implementation uses several patterns to keep business rules testable, side effects isolated, and runtime concerns replaceable.

This ADR records the main patterns, where they appear, why they were used, and the tradeoffs accepted.

## Decision

### Clean Architecture / Ports and Adapters

**Where:** `Domain`, `Application`, `Infrastructure`, and `Api` projects.

**Why:** Business rules should not depend on HTTP, EF Core, Docker, Serilog, Scalar, or metrics libraries. Application defines ports; Infrastructure and Api provide adapters and composition.

**Alternative considered:** A single project with direct EF Core and endpoint calls from business logic.

**Tradeoff:** More projects and interfaces require stricter discipline, but tests and substitutions stay simpler.

### Repository Pattern

**Where:** `IOrderRepository`, `IMenuRepository`, `OrderRepository`, and `MenuRepository`.

**Why:** Use cases need domain-oriented persistence operations without depending on EF Core query details.

**Alternative considered:** Inject `AppDbContext` directly into use cases.

**Tradeoff:** Repositories add a small abstraction layer, but keep Application free from persistence technology.

### Factory Pattern

**Where:** `OrderFactory`.

**Why:** Order creation must apply quantity validation, price snapshots, ticket creation, and aggregate setup consistently.

**Alternative considered:** Let endpoints or use cases construct `Order`, `OrderItem`, and `Ticket` directly.

**Tradeoff:** Creation logic is centralized, so changes to aggregate construction go through one path.

### Strategy / Policy Pattern

**Where:** `IAgingPolicy`, `LinearAgingPolicy`, `KitchenScheduler`, `SelectionRank`, and payment simulation behavior.

**Why:** Scheduling rank and payment behavior are business policies that may vary independently from orchestration.

**Alternative considered:** Hardcode priority calculations and payment outcomes directly in use cases.

**Tradeoff:** More named policy types, but fewer hidden business constants and easier focused tests.

### Coordinator Pattern

**Where:** `SchedulerCoordinator` behind `ISchedulerCoordinator`.

**Why:** Runtime scheduler state needs one mutation boundary. The coordinator serializes mutations while delegating policy decisions to the pure domain scheduler.

**Alternative considered:** Let endpoints, payment use cases, and background workers mutate scheduler state directly.

**Tradeoff:** The coordinator becomes a critical runtime seam, so it needs concurrency tests and clear ownership.

### Decorator Pattern

**Where:** `MeteredSchedulerCoordinator` and `IdempotentPaymentGateway`.

**Why:** Metrics and idempotency are cross-cutting behaviors. Decorators add them without changing domain policy or application use-case contracts.

**Alternative considered:** Add metrics/idempotency logic directly into every use case or domain service.

**Tradeoff:** Dependency injection registration order matters and must be owned explicitly by the composition root.

### Options Pattern

**Where:** scheduler settings, payment gateway settings, API keys/roles, and kitchen worker interval.

**Why:** Tunable values belong in configuration, not hidden in application flow or endpoints.

**Alternative considered:** Hardcode bake times, failure rates, capacity, and worker cadence in code.

**Tradeoff:** Invalid configuration becomes a startup/runtime concern, so options validation is the next hardening step for production.

### Domain Events

**Where:** `PaymentSucceeded`, `IDomainEventDispatcher`, and domain event handlers.

**Why:** Important domain facts can be published after persistence without tightly coupling the aggregate to every reaction.

**Alternative considered:** Call every reaction directly from the payment use case.

**Tradeoff:** In-process delivery is best-effort. A transactional outbox would be needed for stronger production guarantees.

### Unit of Work

**Where:** EF Core `AppDbContext` and repository save operations.

**Why:** Related persistence changes should commit through one database context boundary.

**Alternative considered:** Independent ad-hoc database writes per repository method.

**Tradeoff:** EF Core owns the transaction boundary, which is appropriate here but would need explicit transaction orchestration for multi-resource workflows.

### Idempotency Key Pattern

**Where:** endpoint idempotency store and payment gateway idempotency persistence.

**Why:** Unsafe operations can be retried by clients or networks. Persisted idempotency records allow duplicate calls to replay the original response instead of repeating side effects.

**Alternative considered:** Rely on clients not to retry or use only in-memory duplicate detection.

**Tradeoff:** Requires a durable idempotency table and careful handling when endpoint-level and gateway-level idempotency share the same key.

### Hosted Worker Pattern

**Where:** `KitchenReconciliationWorker`.

**Why:** Some state transitions are time-driven rather than request-driven. A hosted worker keeps the runtime projection current.

**Alternative considered:** Reconcile only when requests arrive.

**Tradeoff:** Adds a background actor that must be deterministic in tests and safe under cancellation.

### RFC 7807 Problem Details

**Where:** API error handling and authentication challenge/forbid responses.

**Why:** Error responses should be consistent, machine-readable, and safe from leaking implementation details.

**Alternative considered:** Return ad-hoc strings or anonymous error shapes per endpoint.

**Tradeoff:** Requires centralized mapping from domain/application errors to HTTP semantics.

## Consequences

- The design demonstrates explicit boundaries and replaceable adapters instead of only endpoint implementation.
- Cross-cutting concerns are pushed toward the composition root.
- The codebase has more seams than a small script-style API, but each seam carries a testability or maintainability reason.
- Future production hardening should focus on options validation, stronger event delivery, documented OpenAPI security, and a distributed scheduler ownership model if horizontal scaling becomes required.

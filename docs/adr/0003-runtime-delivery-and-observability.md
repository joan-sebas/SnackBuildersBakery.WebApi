# ADR 0003: Runtime Delivery and Observability

## Context

The API must be demonstrable from a fresh clone, run consistently in Docker, expose operational health, and advance kitchen state even when no customer request arrives. The design must add runtime concerns without coupling Domain or Application to ASP.NET Core, EF Core startup behavior, logging providers, or metrics infrastructure.

The scheduler has time-driven transitions: baking can complete and turnover can expire without an inbound HTTP request. A request-only reconciliation model would leave kitchen monitoring stale until the next command happens to touch the scheduler.

## Decision

### Hosted Reconciliation Worker

Use `KitchenReconciliationWorker` as an ASP.NET Core hosted service. It calls `ISchedulerCoordinator.ReconcileAsync` on a configurable interval from `KitchenWorker:ReconcileInterval`.

The worker uses injected `TimeProvider` and `PeriodicTimer`. Production uses `TimeProvider.System`; tests use `FakeTimeProvider` to advance time deterministically.

The worker is not registered in the `Testing` environment. Integration tests drive scheduler time explicitly and avoid a second background actor mutating state during assertions.

### Startup Persistence Flow

Outside the `Testing` environment, startup applies EF Core migrations, seeds the baseline menu, and reconstructs the in-memory scheduler projection from durable order-item rows before the API starts serving normal requests.

This supports a fresh Docker Compose run with no manual database setup.

### Health and Readiness Split

Expose two health endpoints:

- `/health`: liveness; verifies the process can respond.
- `/ready`: readiness; verifies the database dependency is reachable.

This separates container/process supervision from dependency readiness.

### Observability at the Composition Root

Use Serilog as the `ILogger` provider with compact JSON console output. Business code depends only on `ILogger` abstractions where logging is needed.

Use `System.Diagnostics.Metrics` through one API meter, `SnackBuilders.Api`. Metrics are recorded from endpoint seams and a scheduler coordinator decorator rather than from Domain/Application internals.

### Docker Compose Delivery

Docker Compose runs Postgres and the API together. The API container waits for Postgres health, applies migrations on startup, seeds menu data, and exposes health/readiness endpoints for verification.

## Why These Options

- Hosted worker over request-only reconciliation: completed bakes and turnover windows should progress without waiting for another customer action.
- `TimeProvider` over direct clock calls: tests can advance time without sleeping, and repository rules prohibit direct production clock reads.
- Startup migrations over manual local setup: the assignment emphasizes fresh-clone delivery and reproducible demos. A mature production deployment would usually move migrations into CI/CD release orchestration.
- Metrics decorator over metrics inside Domain/Application: cross-cutting telemetry is added without changing business contracts or polluting pure domain policy.
- Docker Compose over a hand-written setup guide: the database, API, ports, health checks, and environment variables become executable documentation.

## Consequences

- The running API now reflects time-driven kitchen state without additional requests.
- Local and Docker demos are easier to reproduce.
- Domain and Application remain independent from runtime observability and hosting details.
- The worker cadence is a tuning knob; very small intervals increase reconciliation frequency and log/CPU pressure.
- Startup migrations are acceptable for the assignment and local demo, but production-grade deployments should run migrations as an explicit release step.
- Horizontal scale still needs a different scheduler ownership model, such as one coordinator per kitchen partition, DB-backed coordination, distributed locks, or an external queue.

## Alternatives Considered

- Reconcile only on payment/order requests: rejected because monitoring could show stale baking/turnover state while time has already passed.
- Add an admin endpoint to jump time: rejected for the production API surface because it creates an unrealistic operational control. Tests use `FakeTimeProvider`; demos can shorten configured bake times.
- Put metrics inside domain services: rejected because it couples business policy to infrastructure concerns and makes pure scheduler tests less direct.
- Require manual migrations before Docker startup: rejected because it weakens the fresh-clone experience and makes delivery harder to verify.

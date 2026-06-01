# Snacks Builder Web API

Backend API for bakery order processing, pay-first checkout, and kitchen scheduling.

The solution follows Clean Architecture:

- `src/Domain`: business entities, value objects, domain errors, and pure scheduler policy.
- `src/Application`: use cases, ports, domain-event dispatcher, and scheduler coordinator.
- `src/Infrastructure`: EF Core persistence, repositories, payment simulation, idempotency storage, and scheduler reconstruction.
- `src/Api`: minimal API endpoints, auth, Problem Details, logging, metrics, health checks, and API reference.
- `tests`: unit and integration tests by layer.

## Key Design Decisions

The design favors explicit boundaries and deterministic behavior over a thinner endpoint-only implementation.

- **Clean Architecture:** Domain owns business rules and has no dependency on ASP.NET Core, EF Core, logging, metrics, Docker, or Scalar. Application exposes use cases and ports; Infrastructure implements persistence/payment adapters; Api composes runtime concerns.
- **Scheduler ownership:** Scheduling rules are pure domain policy, while live kitchen state is owned by `ISchedulerCoordinator` behind a single in-process mutation boundary. Durable state remains in order-item rows, and startup reconstruction rebuilds the scheduler projection.
- **Time-driven reconciliation:** Baking completion and turnover can happen without an inbound request, so `KitchenReconciliationWorker` advances scheduler state on configurable ticks. It uses `TimeProvider` so tests can advance time deterministically instead of sleeping.
- **Configuration over hardcoding:** Bake times, tier weights, aging factor, oven count, trays per oven, turnover, payment failure rates, latency, API keys, and worker cadence are configuration-backed. The current local kitchen default is 2 ovens x 3 trays.
- **Idempotency for unsafe operations:** Order and payment writes support `Idempotency-Key` so retries can replay stored responses rather than duplicating side effects. Payment gateway idempotency also protects gateway calls.
- **Observability at the edge:** Serilog, health checks, readiness checks, and `System.Diagnostics.Metrics` are wired in Api/composition code so Domain and Application stay focused on business behavior.
- **Docker-first delivery:** Docker Compose runs Postgres and the API together. The API applies migrations, seeds baseline menu data, and reconstructs scheduler state on startup outside the Testing environment for fresh-clone verification.
- **Documented tradeoffs:** ADRs record why these options were chosen over alternatives such as request-only reconciliation, direct EF Core use in use cases, persisted slot tables, endpoint-local metrics, and early distributed coordination.

## Prerequisites

- Docker with Docker Compose.
- `curl` for the quickstart commands.
- `jq` for the shell snippets that extract IDs from JSON responses.

## Quickstart

Create a local environment file from the placeholders:

```bash
cp .env.example .env
```

Start the API and Postgres:

```bash
docker compose up --build
```

The API listens on `http://localhost:8080`. On startup it applies EF Core migrations,
seeds the baseline menu, and reconstructs scheduler state from persisted order items.

Check liveness and readiness:

```bash
curl http://localhost:8080/health
curl http://localhost:8080/ready
```

List menu items and place an order:

```bash
MENU_ITEM_ID=$(curl -s http://localhost:8080/v1/menu | jq -r '.[0].id')

ORDER=$(
  curl -s -X POST http://localhost:8080/v1/orders \
    -H "Content-Type: application/json" \
    -H "Idempotency-Key: 11111111-1111-1111-1111-111111111111" \
    -d "{
      \"priorityLevel\": \"WalkIn\",
      \"lines\": [
        { \"menuItemId\": \"${MENU_ITEM_ID}\", \"quantity\": 1 }
      ]
    }"
)

echo "$ORDER" | jq .
ORDER_ID=$(echo "$ORDER" | jq -r '.orderId')
```

Pay for the order:

```bash
curl -s -X POST "http://localhost:8080/v1/orders/${ORDER_ID}/payment" \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: 22222222-2222-2222-2222-222222222222" \
  -d '{
    "method": "Cash",
    "amount": 100.00,
    "currency": "USD"
  }' | jq .
```

Track the order:

```bash
curl -s "http://localhost:8080/v1/orders/${ORDER_ID}" | jq .
```

View kitchen monitoring as a manager:

```bash
curl -s http://localhost:8080/v1/kitchen \
  -H "X-Api-Key: manager-dev-key" | jq .
```

Stop the stack:

```bash
docker compose down
```

Remove the Postgres volume when a clean database is needed:

```bash
docker compose down -v
```

## Demo Script

Use this flow to demonstrate the full delivery without relying on hidden setup:

1. Start the stack with `docker compose up --build`.
2. Verify runtime health with `GET /health` and dependency readiness with `GET /ready`.
3. Open Scalar at `http://localhost:8080/scalar/v1` to inspect the API surface.
4. List menu items, place an order with an `Idempotency-Key`, pay it, and track the order.
5. Open `GET /v1/kitchen` with `X-Api-Key: manager-dev-key` to show slot and queue state.
6. Keep the kitchen endpoint open or call it repeatedly; the hosted reconciliation worker advances baking and turnover state on configured ticks.
7. For a faster live demo, shorten `Scheduler__BakeTimes__*` and `KitchenWorker__ReconcileInterval` through environment configuration instead of adding a time-jump endpoint.

## API Reference

In the Development environment, the OpenAPI document and interactive API reference are
available at:

- `http://localhost:8080/openapi/v1.json`
- `http://localhost:8080/scalar/v1`

## Authentication

Manager-only endpoints use the `X-Api-Key` header. Local placeholder keys are defined in
`.env.example` and should be replaced outside source control for real deployments.

Public ordering and payment routes are intentionally simple for the assignment scope.
Unsafe operations use `Idempotency-Key` to prevent duplicate order/payment effects.

## Observability

Logging goes through `ILogger` with Serilog as the provider and compact JSON console
output.

Health endpoints:

- `/health`: liveness; no dependency checks.
- `/ready`: readiness; checks database connectivity.

Metrics use a single `System.Diagnostics.Metrics` meter named `SnackBuilders.Api`.

Metric names:

- `snackbuilders.orders.placed`
- `snackbuilders.payments.processed` with tag `outcome=success|failure`
- `snackbuilders.items.baked`
- `snackbuilders.queue.depth`
- `snackbuilders.slots.occupied`

## Known Limitations

- Scheduler state is owned by one API process. Horizontal scaling would require partitioning by kitchen, DB-backed coordination, distributed locking, or an external queue.
- Startup migrations are used for fresh-clone and Docker demo convenience. A production deployment should normally run migrations through an explicit release pipeline.
- API keys provide assignment-level manager/public role separation, not full customer identity, account ownership, or JWT/OAuth authorization.
- Scalar currently exposes the API shape, but the OpenAPI document does not yet declare the `X-Api-Key` security scheme visually for protected endpoints.
- The runtime API intentionally has no endpoint to jump time forward. Tests use `FakeTimeProvider`; demos should shorten configured bake times and worker cadence.

## Local Development

Restore, build, and test:

```bash
dotnet restore SnackBuilders.slnx
dotnet build SnackBuilders.slnx
dotnet test SnackBuilders.slnx --no-build
```

Apply migrations manually against a local database:

```bash
dotnet tool install --global dotnet-ef --version 10.0.8
dotnet ef database update --project src/Infrastructure/Infrastructure.csproj --startup-project src/Infrastructure/Infrastructure.csproj
```

## Documentation

- [Architecture ADR](docs/adr/0001-stack-and-architecture.md)
- [Scheduler coordinator ADR](docs/adr/0002-scheduler-in-memory-coordinator.md)
- [Runtime delivery and observability ADR](docs/adr/0003-runtime-delivery-and-observability.md)
- [Design patterns and boundary choices ADR](docs/adr/0004-design-patterns-and-boundary-choices.md)
- [Assumptions](docs/assumptions.md)
- [Layered architecture diagram](docs/diagrams/architecture.md)
- [Domain state diagram](docs/diagrams/states.md)
- [Scheduler VIP sequence](docs/diagrams/vip-sequence.md)
- [Entity relationship diagram](docs/diagrams/erd.md)
- [AI usage](AI_USAGE.md)
- [Release notes](docs/RELEASE_NOTES.md)

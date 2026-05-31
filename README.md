# Snacks Builder Web API

Backend API for bakery order processing, pay-first checkout, and kitchen scheduling.

The solution follows Clean Architecture:

- `src/Domain`: business entities, value objects, domain errors, and pure scheduler policy.
- `src/Application`: use cases, ports, domain-event dispatcher, and scheduler coordinator.
- `src/Infrastructure`: EF Core persistence, repositories, payment simulation, idempotency storage, and scheduler reconstruction.
- `src/Api`: minimal API endpoints, auth, Problem Details, logging, metrics, health checks, and API reference.
- `tests`: unit and integration tests by layer.

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
- [Assumptions](docs/assumptions.md)
- [Layered architecture diagram](docs/diagrams/architecture.md)
- [Domain state diagram](docs/diagrams/states.md)
- [Scheduler VIP sequence](docs/diagrams/vip-sequence.md)
- [Entity relationship diagram](docs/diagrams/erd.md)
- [AI usage](AI_USAGE.md)
- [Release notes](docs/RELEASE_NOTES.md)

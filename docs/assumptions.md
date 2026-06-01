# Assumptions

## Domain Scope Assumptions

1. No `Customer` entity is part of the current domain scope.
2. Order priority is an order attribute, not a person attribute.
3. Priority values are limited to configured tier levels and validated on input.
4. Menu browsing is public in the assignment scope; manager mutations require an API key.

## Order and Payment Assumptions

1. Order placement and payment are separate operations.
2. Items enter scheduling only after payment is completed.
3. The initial ready-time returned at order placement is a projection subject to payment timing.
4. Unsafe order and payment operations can be retried by clients, so idempotency keys are supported.

## Product and Scheduling Assumptions

1. Bake time is derived from snack type, not manually edited per item.
2. Kitchen capacity is configurable through oven count and trays per oven. The current local default is 2 ovens x 3 trays each.
3. Scheduling unit is `OrderItem`, not `Order`.
4. Scheduler state is a runtime projection reconstructed from durable order-item rows on startup.
5. Runtime baking and turnover transitions advance through periodic reconciliation ticks; there is no production endpoint for jumping time forward.

## Delivery and Operations Assumptions

1. Docker Compose is the supported local delivery path for the API and Postgres together.
2. Startup migrations and seed data are acceptable for the assignment and local demo path.
3. Production-grade deployments would normally run migrations through an explicit release pipeline.
4. API keys provide assignment-level role separation; they are not a full customer identity model.
5. Horizontal scaling would require a different scheduler ownership strategy, such as partitioning by kitchen, DB-backed coordination, distributed locking, or an external queue.

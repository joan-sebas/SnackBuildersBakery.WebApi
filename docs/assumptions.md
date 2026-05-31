# Assumptions

## Domain Scope Assumptions

1. No `Customer` entity is part of the current domain scope.
2. Order priority is an order attribute, not a person attribute.
3. Priority values are limited to configured tier levels and validated on input.

## Order and Payment Assumptions

1. Order placement and payment are separate operations.
2. Items enter scheduling only after payment is completed.
3. The initial ready-time returned at order placement is a projection subject to payment timing.

## Product and Scheduling Assumptions

1. Bake time is derived from snack type, not manually edited per item.
2. Kitchen capacity baseline is 2 ovens x 3 trays each (6 concurrent slots).
3. Scheduling unit is `OrderItem`, not `Order`.

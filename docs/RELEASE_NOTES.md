# Release Notes

## v1.0.0

Status: pending final tag after the delivery branch is merged, CI is green on `main`, and
the fresh-clone run is repeated from the README.

### Included Scope

- M0 Bootstrap: solution structure, repository rules, CI baseline, and platform setup.
- M1 Domain: enums, value objects, entities, lifecycle guards, and state diagrams.
- M2 Scheduler: queue aging, reconcile policy, turnover handling, estimates, and concurrency behavior.
- M3 Application: ports, domain events, use cases, payment flow, and queries.
- M4 Infrastructure: EF Core mappings, migrations, repositories, seed data, payment simulation, and idempotency persistence.
- M5 API: minimal API endpoints, RFC 7807 Problem Details, role authorization, idempotency headers, and API reference.
- M6 Delivery: structured logging, metrics, health/readiness, Docker Compose, CI, README, and AI usage record.

### Reference Material

- Architecture: `docs/diagrams/architecture.md`
- Domain states: `docs/diagrams/states.md`
- Scheduler VIP sequence: `docs/diagrams/vip-sequence.md`
- Database ERD: `docs/diagrams/erd.md`
- Scheduler ADR: `docs/adr/0002-scheduler-in-memory-coordinator.md`

# endpoint-scaffolder

## Description

Scaffold minimal API endpoints under `/v1` with consistent error handling, authorization, idempotency, and boundary mapping.

## When To Run

- Run when introducing new API resources or commands.
- Run when refactoring endpoint composition and filters.

## Procedure

1. Create or update `/v1` route groups by bounded resource.
2. Keep handler signatures async and cancellation-aware.
3. Wire RFC 7807 problem responses for validation, domain, and unexpected errors.
4. Apply role-based authorization at group or endpoint level.
5. Handle `Idempotency-Key` for command endpoints that require replay protection.
6. Keep DTO-to-domain mapping at the API boundary; do not leak transport models inward.
7. Delegate use-case orchestration to application services, not endpoint glue logic.

## Output Format

```text
Endpoint scaffold review:
- Route group under /v1: PASS|FAIL
- RFC 7807 problems: PASS|FAIL
- Role auth applied: PASS|FAIL
- Idempotency-Key handling: PASS|FAIL
- Async handler signatures: PASS|FAIL
- DTO <-> domain boundary respected: PASS|FAIL
Findings:
- path/to/endpoint.cs:line <issue and fix direction>
```

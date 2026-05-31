# solid-clean-reviewer

## Description

Review a proposed diff against SOLID and Clean Architecture constraints, with emphasis on dependency direction, domain boundaries, and configuration discipline.

## When To Run

- Run before every commit during the per-commit diff gate.
- Run again before push against the full branch diff in the pre-push gate.

## Checklist

1. SOLID principles are respected in changed code.
2. Dependencies point inward: `Api -> Application -> Domain` and `Infrastructure -> Application -> Domain`.
3. Domain remains framework-agnostic and free of infrastructure concerns.
4. No hardcoded business tuning values in flow code or endpoints.
5. Business rules stay in domain code; tunable values come from configuration behind an abstraction.
6. Repositories expose domain entities and domain-oriented contracts, not ORM rows or persistence DTOs.
7. Time is read through injected `TimeProvider`; no `DateTime.Now` or `DateTimeOffset.UtcNow`.
8. New abstractions are justified by complexity reduction, not speculative design.

## Output Format

- `PASS` if no blocking findings.
- `FAIL` if any blocking finding exists.
- Findings must include: `severity`, `file:line`, violated rule, and a concrete fix direction.

Template:

```text
Result: PASS|FAIL
Findings:
- [severity] path/to/file.cs:42 Rule: <rule> - <why it fails>. Fix: <what to change>.
```

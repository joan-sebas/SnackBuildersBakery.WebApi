# ef-migration

## Description

Apply deterministic EF Core migration workflow with review checkpoints that preserve domain and configuration rules.

## When To Run

- Run when persistence model or mapping configuration changes.
- Run before committing migration artifacts.

## Procedure

1. Confirm design-time DbContext factory is available and deterministic.
2. Generate migration with clear timestamped or intent-based naming.
3. Review generated migration code and model snapshot.
4. Generate and inspect SQL script before applying migration.
5. Validate:
   - no `DateTime.Now` or `DateTimeOffset.UtcNow` usage,
   - no hardcoded business tuning values in migration or seed data,
   - seed data remains placeholder or domain-approved constants only.
6. Apply migration locally and run tests.

## Output Format

```text
Migration review:
- Name: <migration name>
- Design-time factory: PASS|FAIL
- SQL reviewed: PASS|FAIL
- Time-source rule: PASS|FAIL
- Hardcode rule: PASS|FAIL
Findings:
- path/to/file.cs:line <issue and fix direction>
```

# AI-Assisted Development

This project used AI development tools as implementation and review accelerators. The
developer retained ownership of scope, architecture, acceptance criteria, review gates,
and final approval.

## Tools Used

- Claude and Claude Code-style workflows for planning, implementation pairing, and review.
- Codex for local repository edits, terminal-driven validation, and documentation passes.
- Editor completion for routine syntax and repetitive boilerplate.

## How AI Assisted

- Helped translate milestone prompts into file plans and incremental implementation steps.
- Drafted and refined Clean Architecture scaffolding, API wiring, and test structure.
- Suggested edge cases for domain invariants, scheduler behavior, idempotency, and error handling.
- Generated initial Mermaid diagrams, ADR drafts, and README structure.
- Supported review passes for SOLID boundaries, dependency direction, and hidden hardcoding.
- Assisted with Docker, CI, observability, and delivery documentation.

## Human-Controlled Decisions

- Stack selection and architecture boundaries.
- Domain rules, scheduling policy, and pay-first flow.
- Issue scope, task ordering, and acceptance criteria.
- Whether diffs were accepted, revised, split, or rejected.
- Merge readiness, release readiness, and final delivery approval.

## Verification Performed

- `dotnet build SnackBuilders.slnx`.
- `dotnet test SnackBuilders.slnx --no-build`.
- API integration tests with Testcontainers Postgres.
- EF Core migration checks against Postgres.
- Manual review of dependency direction and source-control hygiene.
- Documentation review for English-only wording, no confidential text, and no real secrets.

## Guardrails

- AI output was not treated as authoritative.
- Repository rules remained the source of truth for code style, language, and delivery gates.
- Business tuning values were kept in configuration.
- Production code avoids direct wall-clock access and reads time through `TimeProvider`.
- Secrets and deployment-specific credentials are represented only as placeholders.

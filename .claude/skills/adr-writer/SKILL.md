# adr-writer

## Description

Create or update Architecture Decision Records that capture stable technical decisions and their tradeoffs.

## When To Run

- Run when a task introduces or changes a structural decision.
- Run when decision brief items in section 17 require formal ADR entries.
- Run before merge when architectural behavior changed but no ADR was updated.

## Procedure

1. Identify the decision scope and impacted layers.
2. Collect inputs from decision brief sections, related issue prompt, and implemented diff.
3. Write ADR content using this template:
   - Context
   - Decision
   - Consequences
   - Alternatives Considered
4. Keep statements factual, concise, and impersonal.
5. Link related ADRs when a decision supersedes or narrows an earlier one.

## Output Format

```text
ADR: <id and title>
Path: docs/adr/<file>.md
Summary:
- Context: ...
- Decision: ...
- Consequences: ...
- Alternatives Considered: ...
```

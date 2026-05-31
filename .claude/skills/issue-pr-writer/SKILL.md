# issue-pr-writer

## Description

Generate issue and PR text that is concise, structured, and aligned with repository workflow rules.

## When To Run

- Run when creating a new issue from a milestone prompt.
- Run when opening or updating a PR description.
- Run when acceptance criteria or references need clarification.

## Procedure

1. For issues, produce this skeleton:
   - Brief
   - Acceptance Criteria
   - Out of Scope
   - Definition of Done
2. Keep issue language in English and impersonal.
3. For PRs, produce:
   - One-sentence design decision summary.
   - `Closes #N` line in PR body.
4. Keep commit/PR separation clear:
   - `Closes #N` belongs in PR body, not commit message.

## Output Format

```text
Issue draft
- Brief: ...
- Acceptance Criteria: ...
- Out of Scope: ...
- Definition of Done: ...

PR draft
- Decision summary: ...
- Closes #N
```

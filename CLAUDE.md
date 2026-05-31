# CLAUDE.md

Repository operating rules for autonomous agents.

## Language

- Use English only in code, comments, docs, commit messages, issues, and PRs.
- Do not include confidential statement text or company names in repository content.

## Comments and Docs

- Comment only non-obvious rationale, invariants, or tradeoffs.
- Do not paraphrase code behavior in comments.
- If a comment explains what code does, improve naming instead.
- Use XML docs only on meaningful public contracts.

## Writing Tone

- Keep wording impersonal.
- Avoid first-person wording such as "we", "I", or "let's".

## No Hardcode

- Keep business rules in domain code.
- Keep tunable values in configuration behind an abstraction.
- Do not hardcode business constants in application flow or endpoints.

## Time Source

- Do not use `DateTime.Now` or `DateTimeOffset.UtcNow` directly.
- Read time through injected `TimeProvider`.

## Commit Size

- Keep each commit to approximately 5 files or fewer.
- Split larger changes into atomic commits.

## Commit Format

- Use Conventional Commits: `<type>(<scope>): <imperative subject>`.
- Match commit scope with the issue `area:` label.
- Use commit body for the decision rationale ("why"), not a diff summary.

## Mandatory Pre-Commit Review Skills

- Run `solid-clean-reviewer` before showing the commit diff.
- Run `human-style-reviewer` before showing the commit diff.
- Address findings before requesting human diff approval.

## Gate Flow

Follow the process defined in [prompts/EXECUTION_FLOW.md](/D:/usuario/Documents/Proyecto/Snacks%20Builder%20Web%20Api/prompts/EXECUTION_FLOW.md):

1. File plan -> human approval.
2. Per-commit diff -> human approval.
3. Pre-push quality gate on full branch diff.
4. Push -> human action.
5. CI -> green.
6. Merge using rebase-and-merge.

Each commit should build and test green before merge.

# commit-formatter

## Description

Produce commit messages that follow repository conventions and capture rationale rather than diff narration.

## When To Run

- Run for every commit message before `git commit`.
- Re-run if staged content changes after review feedback.

## Checklist

1. Subject follows Conventional Commits: `<type>(<scope>): <imperative subject>`.
2. `type` is one of: `feat`, `fix`, `refactor`, `test`, `docs`, `chore`, `ci`, `perf`.
3. `scope` matches the issue `area:` label.
4. Subject is imperative, concise, lowercase style, and has no trailing period.
5. Body explains the decision rationale ("why"), not a file-by-file summary.
6. Body stays impersonal and in English.
7. Mention that `Closes #N` belongs in the PR body, not in commit message.

## Output Format

Return:

```text
Subject: <type>(<scope>): <imperative subject>
Body:
<why and decision rationale>

PR note: add "Closes #N" in the PR description.
```

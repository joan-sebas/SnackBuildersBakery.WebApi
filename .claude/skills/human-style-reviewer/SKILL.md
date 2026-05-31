# human-style-reviewer

## Description

Review changed files for writing quality and maintainability signals that keep code and docs concise, natural, and readable.

## When To Run

- Run before every commit during the per-commit diff gate.
- Run again before push against the full branch diff in the pre-push gate.

## Checklist

1. All added or edited repository content is in English.
2. Tone is impersonal in docs, comments, commits, and generated text.
3. Comments explain non-obvious rationale only, never restate code behavior.
4. Obvious or redundant comments are removed.
5. Naming is natural and specific; avoid verbose, generic, or AI-sounding names.
6. No dead code, commented-out blocks, or unused scaffolding in changed files.
7. No over-engineering: avoid unnecessary layers, wrappers, or abstractions.
8. Formatting and structure are concise and skimmable.

## Output Format

- `PASS` if no blocking findings.
- `FAIL` if any blocking finding exists.
- Findings must include: `severity`, `file:line`, violated rule, and a concrete rewrite direction.

Template:

```text
Result: PASS|FAIL
Findings:
- [severity] path/to/file.md:10 Rule: <rule> - <why it fails>. Fix: <rewrite direction>.
```

# test-namer

## Description

Standardize xUnit test names and structure so behavior intent is explicit and consistent across layers.

## When To Run

- Run when adding or renaming tests.
- Run during test review before commit.

## Naming Convention

- Preferred format: `Method_Scenario_ExpectedResult`.
- Allowed alternative for complex workflows: given/when/then phrasing.
- Keep names behavior-focused and deterministic.

## Procedure

1. Ensure each test validates one behavior.
2. Structure test body using Arrange / Act / Assert sections.
3. Avoid implementation-detail wording in names.
4. Use explicit domain terms from production code.
5. Keep names concise without losing scenario clarity.

## Output Format

```text
Test naming review:
- Pattern used: Method_Scenario_ExpectedResult | Given_When_Then
- One behavior per test: PASS|FAIL
- AAA layout present: PASS|FAIL
Findings:
- path/to/test.cs:line <rename or structure fix>
```

# context-primer

## Description

Load the minimum required planning context for a task and restate execution boundaries before any implementation starts.

## When To Run

- Run first in every task session.
- Run again if the task prompt changes or scope is redirected.

## Inputs To Load

1. Named sections from `_planning/DECISION_BRIEF.md`.
2. Parent issue file (`_issue.md`) for the current task folder.
3. Milestone file (`_milestone.md`) for the current milestone folder.
4. Named prior logs under `logs/` referenced by the task prompt.
5. Any explicitly named workflow doc (for example `prompts/EXECUTION_FLOW.md`).

## Procedure

1. Read only the referenced sections/files, not whole planning trees.
2. Extract and list:
   - Goal
   - Constraints
   - Out-of-scope items
   - Acceptance criteria
3. Restate those items before writing or editing code.
4. Confirm proposed file plan aligns with loaded context.
5. Proceed only after alignment is explicit.

## Output Format

```text
Context loaded:
- Decision brief sections: ...
- Parent issue: ...
- Milestone: ...
- Prior logs: ...
- Workflow references: ...

Execution restatement:
- Goal: ...
- Constraints: ...
- Out of scope: ...
- Acceptance criteria: ...
- Planned files: ...
```

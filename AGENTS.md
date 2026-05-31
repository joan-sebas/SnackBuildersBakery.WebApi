# AGENTS.md

`CLAUDE.md` is the single source of truth for repository operating rules. All agents should follow [CLAUDE.md] When a task says to run a named skill and the current agent does not support skills, manually load the same context that `context-primer` would load (named brief sections, parent `_issue.md`, and relevant prior `logs/`), then apply the same reviewer checks as a manual diff gate before commit.

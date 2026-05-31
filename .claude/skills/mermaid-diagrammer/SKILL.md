# mermaid-diagrammer

## Description

Author and maintain Mermaid diagrams embedded in Markdown documents under `docs/`, aligned with implemented architecture.

## When To Run

- Run when architecture or lifecycle behavior changes.
- Run when a task explicitly requests a diagram update.
- Run in the same commit as the code or docs change the diagram represents.

## Target Diagrams

1. States diagram: `OrderItem` and oven slot lifecycle.
2. VIP sequence diagram: queue recalculation and estimate updates.
3. Layered architecture diagram: dependency direction toward domain.
4. ERD diagram: persistent entities and relationships.

## Procedure

1. Locate the target docs file in `docs/`.
2. Update embedded Mermaid blocks, not external image files.
3. Keep style consistent across diagrams:
   - Clear node labels.
   - Stable direction (`LR` or `TD`) by diagram type.
   - Minimal visual noise.
4. Validate that diagram text matches implemented behavior and naming.
5. Ensure the diagram commit includes the related architecture change.

## Output Format

```text
Diagram update: <name>
Doc path: docs/<file>.md
Change summary: <what changed and why>
Consistency check: PASS|FAIL
```

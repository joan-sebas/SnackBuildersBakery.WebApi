# GitHub Platform Setup

This document captures the repository platform bootstrap for M0-I02-t01.

## Remote

- Repository visibility: `public`
- Remote name: `origin`
- Remote URL: `https://github.com/joan-sebas/SnackBuildersBakery.WebApi`

## Labels (14)

### Type labels

- `type: feature`
- `type: bug`
- `type: refactor`
- `type: chore`
- `type: docs`
- `type: ci`
- `type: test`

### Area labels

- `area: domain`
- `area: application`
- `area: infrastructure`
- `area: api`
- `area: scheduler`
- `area: docs`
- `area: repo`

## Milestones (M0-M6)

- `M0 Bootstrap`
- `M1 Domain`
- `M2 Scheduler`
- `M3 Application`
- `M4 Infrastructure`
- `M5 API`
- `M6 Delivery`

## Commands Used

### Create public repository

```bash
gh repo create SnackBuildersBakery.WebApi --public --description "Backend API for bakery order processing and kitchen scheduling"
```

### Configure remote and publish genesis

```bash
git remote add origin https://github.com/joan-sebas/SnackBuildersBakery.WebApi
git push -u origin main
```

If `origin` already exists, skip `git remote add` and run:

```bash
git push -u origin main
```

### Create labels

```bash
gh label create "type: feature" --color 1D76DB --description "New user-facing behavior"
gh label create "type: bug" --color D73A4A --description "Defect fix"
gh label create "type: refactor" --color 5319E7 --description "Internal restructuring"
gh label create "type: chore" --color 6B7280 --description "Repository maintenance task"
gh label create "type: docs" --color 0E8A16 --description "Documentation update"
gh label create "type: ci" --color 0052CC --description "CI pipeline work"
gh label create "type: test" --color FBCA04 --description "Test coverage work"

gh label create "area: domain" --color C5DEF5 --description "Domain layer"
gh label create "area: application" --color BFD4F2 --description "Application layer"
gh label create "area: infrastructure" --color A2C4F5 --description "Infrastructure layer"
gh label create "area: api" --color 76A7FA --description "API layer"
gh label create "area: scheduler" --color 3B82F6 --description "Scheduler core"
gh label create "area: docs" --color 34D399 --description "Documentation area"
gh label create "area: repo" --color 9CA3AF --description "Repository meta and tooling"
```

### Create milestones

```bash
gh api repos/joan-sebas/SnackBuildersBakery.WebApi/milestones -f title="M0 Bootstrap"
gh api repos/joan-sebas/SnackBuildersBakery.WebApi/milestones -f title="M1 Domain"
gh api repos/joan-sebas/SnackBuildersBakery.WebApi/milestones -f title="M2 Scheduler"
gh api repos/joan-sebas/SnackBuildersBakery.WebApi/milestones -f title="M3 Application"
gh api repos/joan-sebas/SnackBuildersBakery.WebApi/milestones -f title="M4 Infrastructure"
gh api repos/joan-sebas/SnackBuildersBakery.WebApi/milestones -f title="M5 API"
gh api repos/joan-sebas/SnackBuildersBakery.WebApi/milestones -f title="M6 Delivery"
```

## Verification Commands

```bash
gh label list --limit 100
gh api repos/joan-sebas/SnackBuildersBakery.WebApi/milestones
git ls-remote --heads origin main
```

## Local Environment Note

The current local session does not have `gh` installed. The repository document was prepared locally and the platform commands are ready to run in a shell with GitHub CLI authentication.

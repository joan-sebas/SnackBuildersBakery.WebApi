# Main Branch Protection Ruleset

This document records the active protection policy for `main`.

## Required Rules

- Pull request required before merge.
- Required status check: `build-test`.
- Required status checks must be up to date before merge (`strict: true`).
- At least 1 approving review required.
- Admins are also enforced by branch protection.
- Force pushes are disabled.
- Branch deletion is disabled.

## Merge Method Policy

- Rebase merge: enabled.
- Squash merge: disabled.
- Merge commit: disabled.

This keeps `main` linear and aligned with the workflow requirement to preserve atomic commits through rebase-and-merge.

## Commands Used

```bash
gh api -X PATCH repos/joan-sebas/SnackBuildersBakery.WebApi \
  -f allow_rebase_merge=true \
  -f allow_squash_merge=false \
  -f allow_merge_commit=false
```

```bash
gh api -X PUT repos/joan-sebas/SnackBuildersBakery.WebApi/branches/main/protection \
  --input branch-protection.json
```

Payload:

```json
{
  "required_status_checks": {
    "strict": true,
    "contexts": ["build-test"]
  },
  "enforce_admins": true,
  "required_pull_request_reviews": {
    "required_approving_review_count": 1
  },
  "restrictions": null
}
```

## Verification

```bash
gh api repos/joan-sebas/SnackBuildersBakery.WebApi/branches/main/protection
gh api repos/joan-sebas/SnackBuildersBakery.WebApi --jq '{allow_rebase_merge,allow_squash_merge,allow_merge_commit}'
```

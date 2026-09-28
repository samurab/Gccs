# Release, reconciliation, and failure procedure

Read current `.github/workflows` and release documentation before executing a story. The workflows observed on 2026-09-28 have staging deployment on `main` push or manual dispatch and production promotion by manual protected release tag and approved manifest. This observation is not permanent authority; verify current YAML and live GitHub settings each time. The legacy production workflow is for rollback only.

For each eligible story, inspect existing work; create or reuse its isolated branch from a verified baseline; implement and test the narrow change; review diff and sensitive-data exposure; commit/push and create/update its PR; verify checks and required reviews; verify staging at the candidate revision; merge through governance; verify the merged artifact relationship; promote through the established production workflow only when the story has a meaningful deployable change and release gates pass; then perform safe smoke checks. If main push already initiated staging, inspect that run before dispatching another. Governance or documentation-only work should record deployment `N/A` when no application artifact needs promotion; any publishing workflow still needs verification.

At every boundary record implementation, evidence, approvals, and deployment independently. Inspect GitHub PR and workflow run state before retrying a failed or interrupted action; inspect Azure deployment metadata without secrets or customer content. Never recreate a branch, PR, resource, or deployment just because the local record is stale. A merge that changes the tested candidate requires the applicable revalidation before production promotion.

## Dry-run scenarios

Rehearse these as read-only decisions and record the outcome in the ledger:

1. Failed check: block merge/promotion; capture failing run and tested revision; repair within story scope and rerun the failed gate.
2. GitHub or Azure credential unavailable: keep local work, mark external steps unverified, record minimum required access, and stop before dependent mutations.
3. Dependency, owner approval, reviewer, protected evidence store, or observation period unmet: record exact blocker and consult authoritative stage gate before starting a later story.
4. Interrupted run after dispatch: query live PR/workflow/deployment state first; do not replay a succeeded or still-running action.
5. Named task thread tool unavailable: continue in this coordinating thread with exact-title record and ready-to-use handoff; never claim a separate UI thread exists.

No dry-run scenario authorizes bypassing branch protection, environment approval, or governance. Record a scenario's observed tool availability and limitations rather than presenting the rehearsal as a live failure or completed operational control.

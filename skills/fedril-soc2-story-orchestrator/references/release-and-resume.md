# Release, reconciliation, and failure procedure

Read current `.github/workflows` and release documentation before executing a story. The workflows observed on 2026-09-28 have staging deployment on `main` push or manual dispatch and production promotion by manual protected release tag and approved manifest. This observation is not permanent authority; verify current YAML and live GitHub settings each time. The legacy production workflow is for rollback only.

Inspect existing work and effective authorization first. For deployable changes,
reuse the isolated branch/PR and tested candidate; implement, verify and review the
bounded change. Commit/push/PR work is performed only within existing authority;
inspect workflow triggers and data exposure before pushing. Follow the actual
approved workflow order for merge, staging and production rather than assuming
staging occurs before merge. Preserve branch protection, exposure holds and
environment approvals. Promote only when a deployable change needs it and the
exact candidate, evidence, authority and gates are verified. If a push already
initiated a run, inspect it before any dispatch. Documentation-only work needs
no application deployment; record N/A with rationale while checking any actual
publishing trigger. Do not add release work to every technical unit.

At every boundary record implementation, evidence, approvals, and deployment independently. Inspect GitHub PR and workflow run state before retrying a failed or interrupted action; inspect Azure deployment metadata without secrets or customer content. Never recreate a branch, PR, resource, or deployment just because the local record is stale. A merge that changes the tested candidate requires the applicable revalidation before production promotion.

## Dry-run scenarios

Rehearse these as read-only decisions and record the outcome in the ledger:

1. Failed check: block merge/promotion; capture failing run and tested revision; repair within story scope and rerun the failed gate.
2. GitHub or Azure credential unavailable: keep local work, mark external steps unverified, record minimum required access, and stop before dependent mutations.
3. Dependency, owner approval, reviewer, protected evidence store, or observation period unmet: stop the affected operation, record its exact blocker and consult section 39 for other dependency-satisfied authorized work. Never bypass final acceptance or a live-risk hold.
4. Interrupted run after dispatch: query live PR/workflow/deployment state first; do not replay a succeeded or still-running action.
5. Named task thread tool unavailable: continue in this coordinating thread with exact-title record and ready-to-use handoff; never claim a separate UI thread exists.

No dry-run scenario authorizes bypassing branch protection, environment approval, or governance. Record a scenario's observed tool availability and limitations rather than presenting the rehearsal as a live failure or completed operational control.

## Bounded transport diagnosis

After repeated stream failures, preserve the last verified checkpoint and a
sanitized failure time, client/version, affected session/run and observed error
class. Examine only relevant supported client, connection, proxy or service
diagnostics. Do not export auth caches, raw environment variables, secret-bearing
logs or token payloads, and do not invent a root cause from a disconnect message.
Do not change proxy/security settings or add undocumented configuration knobs.

For an idempotent read with a transient transport, 429 or 5xx failure, allow at
most two retries after the initial failure with short bounded backoff, respecting
provider retry guidance. If its delay exceeds the unit's available budget,
checkpoint it instead. Do not retry unchanged permission, input or interactive
authentication errors. Verify live access with the needed endpoint: a cached
account summary or installed CLI is not proof of a usable token or resource access.
For mutations, reconcile exact target/run state before any possible replay; this
read-retry policy never authorizes mutation retries. Continue independent eligible
work when a required read is unavailable. Use existing blocker fields, not another
register. Diagnose a real failure rather than rerunning setup on every resume.

## Compact recovery and tooling

Maintain the latest current checkpoint in the existing ledger/story record above
historical entries. Root alone writes shared records; do not rebuild them from
chat summaries or make a second task register. Record task, source hash, candidate
SHA and relevant dirty-file fingerprints, environment, verified evidence/results,
pending run/session IDs, exact authorization, held operations and next action.
Restricted identifiers/logs belong only in approved protected custody.

After interruption, a missing terminal response does not prove failure. Query the
exact external run/PR/deployment and inspect relevant local changes/processes.
If succeeded, record its result; if running, observe with bounded backoff; if
failed, diagnose and correct within authority. An unresolved outcome stays held.
Avoid repeated unchanged polling and repeated full inspections. Stop only
dependent actions and continue independent work. Never weaken a failing check.

The installed CLI was checked on 2026-10-19: `codex-cli 0.137.0`; its `exec --help`
exposes `--json`, `--output-schema`, `--output-last-message` and session resume.
Verify installed syntax before use; current documentation can describe newer
clients. Resume an explicit session ID, not `--last` when multiple sessions exist.
Keep logs/evidence protected; JSON events can contain sensitive command output.
Retain normal permissions and rules. CLI alone is not a durable supervisor and
may also disconnect. No unattended retry runner is installed by this skill.

GitHub connector is installed and `gh` is available; `az` is available with a
successful sanitized account-state check at this cutoff. These observations do
not prove access to every repository setting or Azure resource. Check scoped
effective read permissions when needed; do not print access tokens, raw personnel
exports or account/resource identifiers. Prefer these existing tools before a
new plugin. Add a service only for a demonstrated capability gap and preserve
existing protected-evidence and purchase/communication approval rules.

Additional behavioral rehearsals:

6. Review deferred but another task's prerequisites met: keep first task's review
   open; continue eligible work without saying the first story is accepted.
7. Candidate or dirty files changed after a pass: invalidate affected results and
   run required verification against the new inputs; preserve historical passes.
8. Agents touch a shared register: return findings to root; no concurrent writers.
9. Technical answer available in provider metadata: inspect it safely before
   asking the founder; inaccessible state remains unverified.
10. No eligible work remains: save the exact decision/access/evidence dependency
    with recommendation. Never invent review, elapsed-period evidence or approval.

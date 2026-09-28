# FeDril SOC 2 story execution ledger

Updated: 2026-09-28. This is a sanitized coordination checkpoint, not control evidence, management approval, an examination, or an assurance claim. `Not started` below means no accepted completion evidence has been reconciled; local draft records can still exist.

## Source and repository checkpoint

- Authoritative source: `docs/development-story-prompts.md`, section 39 and companion prompt. SHA-256 of the **original checkout's current file** on 2026-09-28: `72129c1acd3825ae71d85c6879cf2b57761bcb9802bc8a140980cb9926559248`. Its committed HEAD version has SHA-256 `9f61e499cfcdfb2676a11418b164135c000c4cf00cd9566c437abfc366def06c`. The current file has uncommitted edits to story prompts and companion instructions. Re-read and reconcile those edits before any story acceptance or release; this ledger does not silently adopt them into the isolated branch.
- Original checkout: branch `codex/local-restored-readiness`, commit `885ecc2a6e10a8479ddc0c23ba31763bd73995cf`, 31 dirty paths observed; preserve all unrelated work. Isolated release branch: `codex/soc2-orchestrator-clean`, based on `origin/main` and containing only the scoped skill/baseline commit. No Story 39.1 branch, release, or deployment was created by this setup.
- Prior local, untracked `docs/soc2` records for Stories 39.1–39.4, scope, controls, readiness, decisions, and evidence were observed. They are provisional local sources, not published or accepted evidence. Review and sanitize them before any version-control adoption. The tracked `docs/soc2/system-boundary-index.md` is the existing repository index.
- GitHub CLI access: authenticated and repository administration metadata visible. Azure CLI: an enabled AzureCloud session is visible. Neither fact proves authorization to change an environment, the existence of a protected evidence workspace, or a successful deployment.
- Release configuration observed: `.github/workflows/staging.yml` triggers on `main` push or manual dispatch; `.github/workflows/production-release.yml` is manual and requires a protected final tag and approved release manifest; `.github/workflows/production.yml` is a legacy rollback workflow. GitHub environment metadata confirmed `staging` and `Production`; the latest listed staging workflow completed successfully on 2026-09-27 and the latest listed production-release workflow completed successfully on 2026-09-26. This verifies configured targets and recent workflow outcomes, not the health or artifact of a future candidate. Recheck live settings, run state, approvals, and artifact identity before action.

## Current checkpoint

Current story: **Story 39.1: Define SOC 2 Scope And Readiness Decision**. Stage: reconcile existing local draft and establish the candidate scope, sponsor, criteria decision, versioned draft matrix, explicit unknowns, and qualified review path. Status: **In progress** for authorized setup; final acceptance is blocked by unverified management and qualified-review decisions. Story 39.4 public-exposure triage is immediate under the document's exception, but its existing local draft must be reconciled before any containment action. Do not advance to Story 39.18 until Stories 39.4–39.17 are accepted or approved not applicable and 39.1–39.3 remain current. Story 39.19 is optional; Story 39.20 requires a 39.19 disposition, not necessarily a Type I report.

Exact next action: inspect the current 39.1 draft, matrix, boundary index, product behavior, and authorized management decisions; create the exact-title 39.1 task thread if the available thread tool supports it; then update that canonical record with a criterion-by-criterion evidence and approval disposition. Prioritize urgent 39.4 triage when the document requires it, while keeping only one story active at a time. Do not call the draft `Complete` or create an examination claim.

| Story 39.1 checkpoint field | Verified value at this ledger update |
| --- | --- |
| Source and dependency | Current working-copy document hash above; no prior story prerequisite; 39.4 has a separate immediate-triage exception |
| Story branch / revision / PR | None verified; `codex/soc2-orchestrator` is the skill setup branch, not the Story 39.1 implementation branch |
| Existing evidence | Local provisional `docs/soc2/story-39.1-scope-readiness-record.md`, `story-39.1-common-criteria-matrix.csv`, and tracked `docs/soc2/system-boundary-index.md`; local draft contents require review and qualified acceptance |
| Story checks | No current Story 39.1 acceptance test or qualified review verified; skill validation and title-parity checks are setup checks only |
| Staging / production | No Story 39.1 candidate, run, artifact, or deployment verified; documentation/governance changes require release-workflow applicability review |
| Governance / blocker | Final scope approval, accountable sponsor and qualified reviewer unverified; do not infer these from authenticated GitHub/Azure sessions |
| Exact next action | Reconcile the existing 39.1 draft and matrix against current source, code, tests, and management inputs; record each acceptance criterion and the minimum outstanding decision in its canonical record |

## Ordered story register

| Order | Exact story title | Dependency / gate checkpoint | Current status | Canonical record or expected handoff |
| --- | --- | --- | --- | --- |
| 1 | Story 39.1: Define SOC 2 Scope And Readiness Decision | Candidate scope and management/qualified review decision | In progress | Local provisional `docs/soc2/story-39.1-scope-readiness-record.md` |
| 2 | Story 39.2: Remediate Gaps And Collect Operating Evidence | Provisional setup may use 39.1 candidate scope; operating evidence remains separate | Not started | Local provisional `docs/soc2/story-39.2-program-setup-record.md` |
| 3 | Story 39.3: Govern Independent Examination And Report Distribution | Needs 39.1/39.2 setup; no engagement/report implied | Not started | Local provisional `docs/soc2/story-39.3-examination-governance-setup.md` |
| 4 | Story 39.4: Investigate Repository Exposure And Govern Containment | Immediate triage exception; authorized containment only | Not started | Local provisional `docs/soc2/story-39.4-exposure-investigation.md` |
| 5 | Story 39.5: Establish Governance Risk Policies And Management Oversight | Before integrated readiness gate | Not started | Per-story record pending |
| 6 | Story 39.6: Implement Workforce Access Device Security And Training | Before readiness; recurring evidence | Not started | Per-story record pending |
| 7 | Story 39.7: Verify Application Security And Tenant Data Boundaries | Before readiness; security boundary tests | Not started | Per-story record pending |
| 8 | Story 39.8: Enforce Secure SDLC Change Approval And Separation Of Duties | Before readiness; per-change evidence | Not started | Per-story record pending |
| 9 | Story 39.9: Harden Production Configuration Networks And Drift Detection | Before readiness; recurring drift evidence | Not started | Per-story record pending |
| 10 | Story 39.10: Govern Secrets Encryption And Key Lifecycle | Before readiness; recurring lifecycle evidence | Not started | Per-story record pending |
| 11 | Story 39.11: Operate Vulnerability Patch And Security Testing Controls | Before readiness; recurring evidence | Not started | Per-story record pending |
| 12 | Story 39.12: Validate Audit Integrity Logging Monitoring And Alert Response | Before readiness; continuous evidence | Not started | Per-story record pending |
| 13 | Story 39.13: Implement Incident Response And No-CUI Spill Exercises | Before readiness; recurring exercise evidence | Not started | Per-story record pending |
| 14 | Story 39.14: Prove Backup Restoration Business Continuity And Recovery | Before readiness; restoration and recurring evidence | Not started | Per-story record pending |
| 15 | Story 39.15: Govern Vendors Subprocessors And Inherited Controls | Before readiness; provider evidence | Not started | Per-story record pending |
| 16 | Story 39.16: Enforce Data Classification Retention Disposal And Customer Responsibilities | Before readiness; No-CUI and lifecycle evidence | Not started | Per-story record pending |
| 17 | Story 39.17: Decide Additional Trust Categories And Implement Conditional Controls | Scope decision before readiness; conditional implementation | Not started | Per-story record pending |
| 18 | Story 39.18: Validate Integrated Readiness And Type I Decision Gate | 39.4–39.17 accepted/approved N/A; 39.1–39.3 current | Blocked—dependency | Per-story record pending |
| 19 | Story 39.19: Coordinate Optional Type I Examination And Report Acceptance | Optional; external examination and acceptance cannot be fabricated | Blocked—dependency | Per-story record pending |
| 20 | Story 39.20: Establish Type II Period Populations And Evidence Entry Gate | 39.18 gate and 39.19 disposition | Blocked—dependency | Per-story record pending |
| 21 | Story 39.21: Operate Type II Controls And Review Failures Throughout The Period | Observation period and operating evidence | Blocked—dependency | Per-story record pending |
| 22 | Story 39.22: Coordinate Type II Examination And Resolve Report Findings | External examination/finding disposition | Blocked—dependency | Per-story record pending |
| 23 | Story 39.23: Maintain Report Distribution Continuous Readiness And Renewal | Accepted report, authorized disclosure, recurring controls | Blocked—dependency | Per-story record pending |

## State of implementation, release, and evidence

For all 23 stories, branch, story revision, pull request, test run, staging run, production run, approval, and accepted operating-evidence reference are **none verified in this orchestration session**. Existing local draft records may describe earlier work; inspect them and live systems before updating a cell. No application deployment is justified solely by this governance/skill setup. For each active story, maintain the detailed fields in the per-story record template and link its canonical record here.

## Dry-run decision checks

Read-only scenario walkthrough on 2026-09-28. Inputs below are simulated unless expressly called an observation; no live run was failed, replayed, deployed, or changed by this walkthrough.

| Input exercised | Skill decision checked | Result and limit |
| --- | --- | --- |
| A hypothetical required check fails on a candidate revision | Block merge and promotion; record run/revision; investigate and rerun the failed gate | Pass as procedural decision; no live failing candidate tested |
| Hypothetical GitHub or Azure credential unavailable | Preserve local state; mark external verification unknown; specify minimum access; stop dependent mutation | Pass as procedural decision; actual CLI sessions were available for metadata lookup |
| Story 39.18 requested with 39.4–39.17 not accepted | Stop at dependency gate; retain 39.18 `Blocked—dependency` | Pass against the authoritative gate; no approval inferred |
| Session stops after a hypothetical deployment dispatch before ledger update | Query run and environment state before retry; do not dispatch an already succeeded or active run | Pass as procedural decision; no deployment was dispatched |
| Named task-thread creation hypothetically unavailable | Keep exact-title per-story handoff in coordinator and state UI limitation | Pass as fallback design; a thread-creation tool is actually available in this environment, subject to its successful use |

The dry run validates instructions and gate handling only. It does not validate an external provider fault, a deployed artifact, a completed story, or a configured background runner.

## Decisions, blockers, and recurring controls

- Scope approval, accountable management sponsor, applicable Trust Services Criteria decision, qualified reviewer, and protected evidence workspace approval remain unverified in this session. Use the local provisional decision records; seek only the unresolved decision after all authorized evidence gathering.
- The local `docs/soc2/soc2-readiness-reference.md` summarizes an earlier control inventory, prioritized gaps, and decision list but is untracked; source dates and access limitations in that record must be refreshed before publication.
- The local `docs/soc2/soc2-evidence-inventory.csv` is an untracked preliminary inventory. Restricted evidence is not imported here. Verify protection, ownership, retention, and mapping before considering it an operating evidence register.
- Recurring-control calendar starter is in the authoritative story document. Each frequency, owner, evidence requirement, next due date, and observed execution belongs in the applicable story record. Until an owner and start date are approved, next due dates are unassigned; configuration alone is not operating evidence.

Resume with `$fedril-soc2-story-orchestrator`: `status` first, reconcile the saved checkpoint with live GitHub/Azure and the current source hash, then `resume` the exact next action. No background runner is configured by this ledger.

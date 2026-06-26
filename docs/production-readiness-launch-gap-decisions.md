# Production Readiness Launch Gap Decisions

Review status: Complete.

Review date: 2026-06-26.

Review owner: Product owner.

Source gap artifact: `docs/production-readiness-completed-story-dod-review.md`.

Decision rule: every failed, partial, skipped, or untested Definition of Done item must be classified as launch blocker, accepted risk, deferred follow-up, or not applicable. No gap may remain without owner, severity, mitigation, contingency, approver, target date, and current status.

No deferred item in this log expands the No-CUI posture, weakens tenant isolation, bypasses RBAC, removes audit logging, reduces support readiness, or permits unsupported customer claims.

| Gap ID | Gap summary | Classification | Owner | Severity | Mitigation | Contingency | Approver | Target date | Current status | No-CUI and claims impact |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| DOD-GAP-001 | Staging and production UI evidence for validation failure, permission denial, empty state, error state, and basic accessibility is not attached yet. | Deferred follow-up | QA owner | Medium | Capture UI state and accessibility evidence during PR-3.2, PR-3.4, and PR-7.2 smoke execution using synthetic or non-sensitive data. | If evidence is missing at launch approval, block production launch until evidence is attached or the affected UI scope is removed from launch. | Product owner and engineering lead | Before PR-6.1 launch approvals | Open | Does not expand No-CUI posture; evidence collection must preserve No-CUI claims and synthetic-only staging data. |
| DOD-GAP-002 | Some completion evidence is historical and spread across test suites rather than attached to one launch evidence package. | Deferred follow-up | Engineering lead | Medium | Gather test output, commit references, migration evidence, staging smoke evidence, rollback evidence, and readiness artifact links into the launch evidence package. | If evidence is incomplete at launch candidate tagging, block PR-6.2 launch candidate tag until missing links are attached or scope is removed. | Product owner and engineering lead | Before PR-6.2 launch candidate tag | Open | Does not expand data posture; evidence package must preserve No-CUI launch scope and claim controls. |
| DOD-GAP-003 | Malware scanning launch path remains undecided. | Launch blocker | Security owner | High | Decide and document enabled production scanner or formal launch exception with compensating controls in PR-4.3 and the known-risk acceptance log. | If no scanner or approved exception exists, keep production launch blocked and disable production file upload paths if necessary. | Security owner and product owner | Before PR-4.3 completion | Open | Blocks launch until upload risk is resolved; does not authorize real CUI or prohibited upload handling. |

## Accepted Risks

No accepted risks are recorded for PR-2.3. All identified gaps are either deferred follow-ups with launch evidence dependencies or launch blockers.

## Launch Blockers

- `DOD-GAP-003`: Malware scanning launch path remains undecided until PR-4.3 records an enabled scanner or formal exception with compensating controls.

## Deferred Follow-Ups

- `DOD-GAP-001`: UI validation, denial, empty, error, and accessibility evidence must be attached during staging and production smoke stories.
- `DOD-GAP-002`: Historical completion evidence must be gathered into the launch evidence package before launch candidate tagging.

## Required Follow-Up

- PR-4.3 must update this log or the future known-risk acceptance log with the malware scanning decision.
- PR-6.1 must verify no launch blocker remains open before approvals are treated as complete.
- PR-6.2 must verify launch evidence links include the deferred UI and completion evidence records.

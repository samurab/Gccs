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
| DOD-GAP-003 | Malware scanning launch path remains undecided. | Accepted risk | Security owner | High | PR-4.3 enabled the scanner control path and documented formal exception `PR43-MALWARE-001` with compensating controls. | If external scanner evidence is not attached before exception expiration, disable production file upload paths or block production launch. | Security owner and product owner | Before exception expiration | Closed on 2026-07-02 by approved exception and enabled fail-closed scanner path | Does not authorize real CUI or prohibited upload handling. |
| DOD-GAP-004 | Staging restore rehearsal has not been executed. | Launch blocker | Engineering lead | High | Execute the PR-4.1 restore runbook in `docs/production-readiness-launch-closure-evidence.md`, attach restore output, reviewer, and teardown evidence. | If restore evidence is missing at PR-6.1, block launch candidate approvals and do not tag PR-6.2. | Engineering lead and security owner | Before PR-6.1 launch approvals | Restore execution pending | Does not expand No-CUI posture; restored data must remain synthetic-only. |
| DOD-GAP-005 | Five high-risk compliance content records remain `needs_review`. | Launch blocker | Compliance content owner | High | Approve the high-risk records or withhold them from customer-facing production views; record reviewer, date, scope, and limitations. | If approval or withholding is incomplete, block production launch or remove affected content from launch scope. | Compliance content owner and legal or contracting advisor | Before PR-6.1 launch approvals | Expert approval or withholding pending | Blocks unsupported compliance content claims; does not authorize legal advice or government endorsement. |
| DOD-GAP-006 | Required final launch approvals are not recorded. | Launch blocker | Product owner | Critical | Collect PR-6.1 approval records for product, engineering, security, compliance content, support, and legal or contracting advisor with evidence links and exceptions. | If any approval is missing, do not tag the launch candidate. | Product owner | Before PR-6.2 launch candidate tag | Approvals pending | Prevents informal production launch without accountable signoff. |
| DOD-GAP-007 | PR-3.3 authenticated staging tenant isolation and RBAC checks are not executed for the full role matrix. | Launch blocker | Security owner | High | Provide staging-only tokens or smoke identities for Admin, Compliance Manager, Contributor, Auditor, and Advisor role contexts; run direct API cross-tenant and role-denial checks; attach sanitized outputs in `docs/production-readiness-staging-security-evidence.md`. | If full authenticated staging authorization evidence is missing, do not proceed to PR-3.4 or later launch evidence stories. | Security owner and engineering lead | Before PR-3.4 execution | Closed on 2026-07-02; role-matrix staging evidence passed for Owner, Admin, Compliance Manager, Contributor, Auditor, and Advisor. | Does not expand No-CUI posture; staging checks used synthetic-only tenants and data. |
| DOD-GAP-008 | Unintended orphan staging tenant `PR-3.3 blocked admin tenant` was created during an invalid Admin-cycle probe. | Launch blocker | Engineering lead | Medium | Use database or Azure/admin access to locate and archive the orphan tenant; attach cleanup evidence. | If cleanup cannot be confirmed before PR-3.3 completion, keep PR-3.3 blocked and do not proceed to PR-3.4. | Engineering lead and security owner | Before PR-3.3 completion | Closed on 2026-07-02; tenant archived, audit entry inserted, and temporary firewall rule removed. | Does not expand No-CUI posture; tenant was staging-only, archived with zero memberships, and cleanup evidence is attached. |
| DOD-GAP-009 | PR-3.4 authenticated staging upload guardrail and report-control checks could not execute without a signed-in staging session or approved smoke credential. | Launch blocker | QA owner | High | Provide a staging-only smoke identity, signed-in browser session, or approved API token; run synthetic-only upload acknowledgement, blocked upload, report generation, report RBAC, source metadata, and prohibited-claim checks; attach sanitized outputs in `docs/production-readiness-staging-upload-report-evidence.md`. | If PR-3.4 authenticated staging evidence is missing, do not proceed to PR-4.1 or later launch evidence stories unless accountable approvers remove PR-3.4 from launch scope. | Security owner and engineering lead | Before PR-4.1 execution | Closed on 2026-07-02; signed-in in-app browser session was provided, synthetic-only authenticated smoke checks passed, and sanitized outputs are attached. | Does not expand No-CUI posture; evidence used synthetic-only files and did not store real CUI or customer data. |

## Accepted Risks

`PR43-MALWARE-001` is accepted for the No-CUI MVP launch candidate only. It does not expand the data posture and expires before production customer launch, or 30 days after exception approval, whichever comes first.

## Known-Risk Acceptance Log

| Risk ID | Story | Risk | Classification | Owner | Compensating controls | Expiration | Required approvers | Current status | Launch disposition |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| PR43-MALWARE-001 | PR-4.3 | External production scanner endpoint evidence is not attached yet, although scanner control path is enabled and fails closed. | Accepted risk for No-CUI MVP launch candidate | Security owner | No-CUI posture, prohibited upload rejection, per-file No-CUI attestation, server-side validation, scanner-before-storage enforcement, clean-only persistence, detected-malware rejection, scanner-unavailable fail closed, audit logging, support escalation, and ability to disable production file upload paths. | Before production customer launch, or 30 days after exception approval, whichever comes first | Product owner and security owner | Approved on 2026-07-02 | PR-4.3 launch blocker closed; attach external scanner evidence before exception expiration. |

## Launch Blockers

- `DOD-GAP-004`: Staging restore rehearsal remains unexecuted until PR-4.1 restore evidence is attached.
- `DOD-GAP-005`: High-risk expert content remains pending until approved or withheld from customer-facing production.
- `DOD-GAP-006`: Required launch approvals remain pending until PR-6.1 records accountable signoff.

## Closed Gaps

- `DOD-GAP-008`: PR-3.3 staging cleanup was completed on 2026-07-02. The unintended orphan tenant `PR-3.3 blocked admin tenant` was located in staging PostgreSQL, verified with exactly one matching row and zero memberships, archived with an audit entry, and the temporary PostgreSQL firewall rule used for cleanup was removed.
- `DOD-GAP-007`: PR-3.3 staging authorization evidence was completed on 2026-07-02. Audited role-cycle checks exercised Owner, Admin, Compliance Manager, Contributor, Auditor, and Advisor contexts against direct API cross-tenant reads, update/delete denial, role-restricted denial, standard problem responses, and no-mutation fixture snapshots.
- `DOD-GAP-009`: PR-3.4 staging upload/report evidence was completed on 2026-07-02. Authenticated synthetic-only checks verified No-CUI acknowledgement, upload guardrails, upload audit events, report RBAC, tenant scope, obligation source metadata, and prohibited-claim controls.
- `DOD-GAP-003`: PR-4.3 malware scanning launch path was resolved on 2026-07-02 by enabled scanner control path plus approved exception `PR43-MALWARE-001`.

## Deferred Follow-Ups

- `DOD-GAP-001`: UI validation, denial, empty, error, and accessibility evidence must be attached during staging and production smoke stories.
- `DOD-GAP-002`: Historical completion evidence must be gathered into the launch evidence package before launch candidate tagging.

## Required Follow-Up

- PR-3.4 closed `DOD-GAP-009`; the production readiness sequence can continue to PR-4.1.
- PR-4.3 updated this log with approved exception `PR43-MALWARE-001`; external scanner evidence remains due before exception expiration.
- PR-4.1 must attach restore rehearsal evidence before launch approval.
- PR-5.1 must attach expert content approval or withholding evidence before launch approval.
- PR-6.1 must verify no launch blocker remains open before approvals are treated as complete.
- PR-6.2 must verify launch evidence links include the deferred UI and completion evidence records.

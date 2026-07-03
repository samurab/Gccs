# GCCS No-CUI MVP Launch Candidate Release Notes

Story: PR-5.4 - Prepare Pilot Onboarding, Release Notes, And Known-Risk Log.

Release note status: launch-ready draft; final product owner, engineering lead, security owner, compliance content owner, customer success/support owner, and legal or contracting advisor approval remain required before tagging.

## Launch Posture

This launch candidate is No-CUI / compliance management only. Real customer CUI, classified information, ITAR/export-controlled technical data, sensitive government-furnished information, credentials, payroll, SSNs, health or disability data, unrestricted security logs, sensitive incident details, and other prohibited sensitive content are excluded.

## Scope

Included MVP workflows:

- Tenant access, RBAC, and audit logging.
- Company profile and contract metadata workflows.
- Source-backed obligation content and task workflows.
- No-CUI evidence metadata and allowed non-sensitive upload paths.
- Report generation with tenant scope, source metadata, and claim-control language.
- CMMC readiness tracking as draft/workflow guidance.
- Subcontractor and shared responsibility workflows included in the current launch package.

## Exclusions

- Production real-CUI handling.
- Classified or export-controlled data handling.
- Legal, accounting, labor, certification, assessor, contracting-officer, or government-endorsement determinations.
- Customer-facing publication of high-risk obligations that are not in the `published` review state.
- Production launch without final PR-6.1 approvals.

## Known Risks

Known risks and launch blockers are tracked in `docs/production-readiness-launch-gap-decisions.md`.

- `PR43-MALWARE-001`: external production scanner endpoint evidence is not attached yet; accepted only for the No-CUI MVP launch candidate with compensating controls and expiration.
- `PR41-RESTORE-001`: staging restore rehearsal remains unexecuted and is accepted for launch-candidate tagging only; production customer launch remains blocked until restore evidence is attached or separately dispositioned.
- `DOD-GAP-006`: final launch approvals are recorded in `docs/production-readiness-launch-approval-record.md`.
- `PR52-CLAIM-001`: final release notes or pilot materials can drift after claim review; advisor approval is required before launch.
- `PR53-SUPPORT-001`: support runbooks are finalized, but customer success/support owner approval is still required before launch approval.

## Support Paths

Support routing is documented in `docs/production-readiness-support-runbooks.md` and covers prohibited upload, suspected CUI, tenant exposure, access issue, evidence failure, report failure, content correction, security incident, backup restore, and rollback.

## Staging Smoke Results

Staging evidence is attached in:

- `docs/production-readiness-staging-smoke-evidence.md`
- `docs/production-readiness-staging-workflow-evidence.md`
- `docs/production-readiness-staging-security-evidence.md`
- `docs/production-readiness-staging-upload-report-evidence.md`

Current staging evidence verifies `/health`, dependency signals, synthetic-only workflow execution, tenant isolation/RBAC checks, upload guardrails, report controls, No-CUI acknowledgement, blocked prohibited upload paths, report claim controls, and source metadata.

## Rollback Plan

Rollback evidence and limits are documented in `docs/production-readiness-deployment-migration-rollback-evidence.md` and `docs/production-readiness-launch-closure-evidence.md`. Application rollback is supported through prior known-good artifacts. Database rollback is not automatic; destructive or irreversible migration risk requires explicit product owner and engineering lead approval.

## Content Scope

Launch obligation content is source-backed through `packages/compliance-content/obligations/mvp.json`. Only `published` obligations are customer-facing. High-risk or expert-review-required obligations that are `needs_review` or approved but not `published` remain withheld from customer-facing production views until explicit publication approval is recorded.

## Review Status Before Launch Approval

Release notes, pilot onboarding, support runbooks, customer-facing claim review, known-risk log, restore evidence, malware scanning evidence or exception, and final launch approvals must be linked before PR-6.1 can be approved.

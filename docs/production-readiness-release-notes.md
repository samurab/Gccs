# FeDril No-CUI MVP Launch Candidate Release Notes

Story: PR-5.4 - Prepare Pilot Onboarding, Release Notes, And Known-Risk Log.

Release note status: launch-ready draft for broader customer launch; approved for the 2026-08-03 solo-controlled No-CUI pilot candidate under the combined-role approval posture. Independent production separation-of-duties approval remains required before broader customer launch.

Launch candidate tag: `launch-candidate-2026-08-03-2`.

Approved launch candidate manifest: `docs/release/approved-launch-candidate.json`.

Launch candidate tag record: `docs/production-readiness-launch-candidate-tag.md`.

## Launch Posture

This launch candidate is No-CUI / compliance management only. Real customer CUI, classified information, ITAR/export-controlled technical data, sensitive government-furnished information, credentials, payroll, SSNs, health or disability data, unrestricted security logs, sensitive incident details, and other prohibited sensitive content are excluded.

## Scope

Included MVP workflows:

- External presentation-boundary branding uses FeDril while internal namespaces, service identifiers, schema, API headers, storage keys, telemetry, and deployment identifiers remain unchanged and excluded from external display.
- Repeated non-notifying obligation-owner assignments are idempotent; changed assignments retain the existing audit and notification behavior.
- Concurrent creation of a tenant user's default notification preferences resolves to the tenant-scoped persisted record instead of surfacing the expected PostgreSQL uniqueness race.
- Marketing demonstration seed and capture presentation behavior remain development/build-time gated and do not enable a production demo seed endpoint or change the No-CUI posture.
- Public FeDril marketing pages include a narrated 60-second homepage overview, a dedicated flagship walkthrough, captions, an AI-narration disclosure, and a mobile-compatible media source. All demonstrated organization, user, evidence, requirement, task, date, and activity data is fictional.
- Editable Playwright, Remotion, narration, caption, render, and validation sources are retained under `marketing/demo-video`; the externally published materials remain limited to verified FeDril presentation branding and defensible readiness-workflow language.
- Public visitors can request a scheduled demo with a preferred date, time, and IANA time zone. Accepted requests are persisted with transactional internal-notification and requester-acknowledgement delivery records.
- Authorized platform operators can review demo requests, see delivery state, queue approved response templates, and access tenant-onboarding administration. `/platform` permissions fail closed when server-provided permission data is absent or malformed.
- Demo-request email uses environment-isolated Azure Communication Services resources and App Service managed identity. Non-development startup fails closed when required provider, endpoint, sender, or recipient settings are invalid.
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
- Broader customer launch without independent production separation-of-duties approval.
- Real customer documents or file contents in the published demonstration; evidence shown in the video is fictional metadata only.

## Known Risks

Known risks and launch blockers are tracked in `docs/production-readiness-launch-gap-decisions.md`.

- `PR43-MALWARE-001`: production scanner evidence is attached for the private ClamAV-compatible path; the single-instance topology is accepted only for the controlled No-CUI pilot and requires hardening before broader launch.
- `PR41-RESTORE-001`: staging restore rehearsal passed on 2026-07-05 for the tested point-in-time restore path; do not claim geo-disaster recovery or production customer-data restore from this evidence.
- `DOD-GAP-006`: final launch approvals are recorded in `docs/production-readiness-launch-approval-record.md`.
- `PR52-CLAIM-001`: final release notes or pilot materials can drift after claim review; the combined-role pilot reapproval is recorded for this candidate, and independent advisor approval remains required before broader customer launch.
- `PR53-SUPPORT-001`: support runbooks and combined-role pilot approval are recorded; independent customer success/support approval remains required before broader customer launch.

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

## Candidate Approval Status

Release notes, pilot onboarding, support runbooks, customer-facing claim review, known-risk log, restore evidence, malware scanning evidence, and the combined-role candidate approval are linked for this solo-controlled No-CUI pilot. Independent production separation-of-duties and professional approvals remain required before broader customer launch.

Candidate-specific demo publication evidence is recorded in `marketing/demo-video/QA-CHECKLIST.md`. The 86.41 MB flagship video is accepted for this controlled release but should move to durable media/object storage before frequent revisions.

Candidate-specific demo-request approval and staging delivery evidence are recorded in `docs/production-readiness-launch-approval-record.md`. External email acceptance does not guarantee inbox placement because recipient mail systems can quarantine or filter messages after provider acceptance.

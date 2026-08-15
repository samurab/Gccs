# MVP Production Readiness Checklist

Story 17.4 defines the launch gate for the No-CUI / compliance management only MVP. This checklist must be reviewed before any production launch. It is a release-control artifact, not a claim that production is approved.

PR-6.1 launch approval record: `docs/production-readiness-launch-approval-record.md`.

PR-6.2 launch candidate tag record: `docs/production-readiness-launch-candidate-tag.md`.

Approval posture addendum: `docs/production-readiness-approval-posture-addendum.md`.

## Launch Gate

Launch gate status: blocked until all required items are complete and approved.

Required approvals before production launch:

- Product owner approval.
- Engineering lead approval.
- Security owner approval.
- Compliance content owner approval.
- Customer success/support owner approval.
- Legal or contracting advisor approval for customer-facing compliance claims.

Required approval status:

| Required approver | Current status | Launch blocker while pending |
| --- | --- | --- |
| Product owner | Approved on 2026-07-03 by accountable solo-controlled pilot approver for product-owner scope | No for solo-controlled pilot testing; yes for broader production launch |
| Engineering lead | Approved on 2026-07-03 by accountable solo-controlled pilot approver for engineering scope | No for solo-controlled pilot testing; yes for broader production launch |
| Security owner | Approved on 2026-07-03 by accountable solo-controlled pilot approver for security scope | No for solo-controlled pilot testing; yes for broader production launch |
| Compliance content owner | Approved on 2026-07-03 by accountable solo-controlled pilot approver for compliance-content scope | No for solo-controlled pilot testing; yes for broader production launch |
| Customer success/support owner | Approved on 2026-07-03 by accountable solo-controlled pilot approver for support scope | No for solo-controlled pilot testing; yes for broader production launch |
| Legal or contracting advisor | Approved on 2026-07-03 by accountable solo-controlled pilot approver for legal/contracting scope | No for solo-controlled pilot testing; yes for broader production launch |

| Area | Required item | Evidence | Owner | Approver | Current status |
| --- | --- | --- | --- | --- | --- |
| No-CUI posture | Customer-facing data handling notice is visible in onboarding, upload flows, docs, and support scripts. Real CUI upload remains prohibited until a future approved `CuiReady` posture is implemented. | `README.md`, `docs/product-strategy.md`, `docs/mvp-execution-plan.md` | Product owner | Legal or contracting advisor | Ready for approval |
| Terms and claims | Product copy avoids legal advice, certification, CMMC approval, assessment success, and government endorsement claims. | `docs/software-delivery-plan.md`, `docs/compliance-content-governance.md`, `docs/production-readiness-customer-claims-review.md`, `output/production-readiness/customer-claims-review.json` | Product owner | Legal or contracting advisor | Claim review recorded; solo-controlled pilot legal/contracting approval scope recorded in PR-6.1 approval record |
| Support path | Support path exists for prohibited uploads, suspected CUI, tenant exposure, access issues, evidence upload failures, report failures, compliance content corrections, security incidents, backup restore, and rollback. | `docs/mvp-execution-plan.md`, `docs/software-delivery-plan.md`, `docs/production-readiness-support-runbooks.md` | Customer success/support owner | Product owner | Runbooks finalized; solo-controlled pilot support approval scope recorded in PR-6.1 approval record |
| Prohibited uploads | Prohibited upload guidance covers CUI, classified data, export-controlled technical data, SSNs, payroll, protected health or disability data, credentials, unrestricted security logs, and sensitive incident details. | `docs/mvp-execution-plan.md` | Security owner | Legal or contracting advisor | Ready for approval |
| Staging MVP workflow | Authenticated staging run proves tenant creation or verification, user invite, role assignment, company profile, contract creation, allowed upload, blocked prohibited upload, clause tagging, obligation generation, task creation, evidence upload, report generation, and audit log export using synthetic-only data. | `docs/production-readiness-staging-workflow-evidence.md` | QA owner | Product owner | Ready for approval |
| Staging tenant isolation and RBAC | Authenticated staging direct API checks prove cross-tenant access denial, server-side role enforcement, consistent permission errors, and denied-action evidence for owner, admin, compliance manager, contributor, auditor, and advisor contexts. | `docs/production-readiness-staging-security-evidence.md`, `output/playwright/production-readiness/pr-3.3/role-matrix-owner.json`, `output/playwright/production-readiness/pr-3.3/role-matrix-admin.json`, `output/playwright/production-readiness/pr-3.3/role-matrix-compliance-manager.json`, `output/playwright/production-readiness/pr-3.3/role-matrix-contributor.json`, `output/playwright/production-readiness/pr-3.3/role-matrix-auditor.json`, `output/playwright/production-readiness/pr-3.3/role-matrix-advisor.json`, `output/playwright/production-readiness/pr-3.3/role-matrix-no-mutation-summary.json`, `tests/Gccs.Api.Tests/SecurityIsolationVerificationTests.cs`, `tests/Gccs.Api.Tests/RoleBasedPermissionTests.cs` | Security owner | Engineering lead | Ready for approval |
| Staging upload guardrails and report controls | Authenticated staging checks prove No-CUI upload acknowledgement, blocked risky uploads, allowed and blocked upload audit events, report RBAC, tenant scope, source metadata, and prohibited-claim controls using synthetic-only data. | `docs/production-readiness-staging-upload-report-evidence.md`, `output/playwright/production-readiness/pr-3.4/staging-health.json`, `output/playwright/production-readiness/pr-3.4/authenticated-upload-report-smoke.json`, `output/playwright/production-readiness/pr-3.4/authentication-blocker.json`, `tests/Gccs.Api.Tests/NoCuiAcknowledgementTests.cs`, `tests/Gccs.Api.Tests/EvidenceFileUploadTests.cs`, `tests/Gccs.Api.Tests/TenantModeWorkflowEnforcementTests.cs`, `tests/Gccs.Api.Tests/ComplianceStatusReportTests.cs`, `tests/Gccs.Api.Tests/CmmcReadinessReportTests.cs`, `tests/Gccs.Api.Tests/EvidencePackageReportTests.cs` | QA owner | Security owner | Ready for approval |
| Backups and restore | Backup policy, restore runbook, and staging restore evidence are available. | `docs/software-delivery-plan.md`, `docs/staging-environment.md`, `docs/production-readiness-backup-restore-evidence.md`, `docs/production-readiness-launch-closure-evidence.md`, `output/production-readiness/backup-restore/staging-postgres-backup-config.json`, `output/production-readiness/backup-restore/restore-rehearsal-summary.json` | Engineering lead | Security owner | Restore rehearsal passed on 2026-07-05; `PR41-RESTORE-001` closed for the tested staging point-in-time restore path |
| Logs and alerts | API, web, migration, upload/storage, queue, job failure, health, and error alerts are routed to the launch support owner. | `docs/staging-environment.md`, `docs/production-readiness-production-smoke-evidence.md`, `output/production-readiness/alerts/production-alert-route-summary.json`, `output/production-readiness/alerts/production-alert-email-receipt.json` | Engineering lead | Security owner | App Service logs and API `Http5xx` alert resource are active; approved action-group receiver and Azure Monitor delivery receipt are attached |
| Rollback plan | Production rollback plan is documented and simulated in staging before launch. | This checklist, `docs/staging-environment.md`, `docs/production-readiness-deployment-migration-rollback-evidence.md`, `output/production-readiness/deployment-migration-rollback/gccs-staging-migrations.sql` | Engineering lead | Product owner | Simulated for staging with migration rollback limitation documented |
| Malware scanning | MVP limitation is documented when scanner is placeholder-only; production launch requires an enabled malware scanning path or explicit launch exception. | `README.md`, `docs/software-delivery-plan.md`, `docs/mvp-execution-plan.md`, `docs/production-readiness-malware-scanning-decision.md`, `docs/production-readiness-production-smoke-evidence.md`, `docs/production-readiness-launch-closure-evidence.md`, `tests/Gccs.Api.Tests/EvidenceFileUploadTests.cs` | Security owner | Product owner | Production private ClamAV scanner `gccs-clamav-production` is configured; PR-7.2 clean byte-level upload returned `201`, `malwareScanStatus=clean`, and `isUsable=true` |
| Expert-reviewed content | Customer-facing launch content has source URL, last reviewed date, confidence, review owner, and review state. | `packages/compliance-content/obligations/mvp.json`, `docs/production-readiness-launch-closure-evidence.md`, `output/production-readiness/expert-content/staging-content-review-summary.json`, `output/production-readiness/expert-content/high-risk-obligation-review.json` | Compliance content owner | Legal or contracting advisor | High-risk review decisions recorded; seven high-risk or expert-review records remain withheld from customer-facing production views until publication approval |
| Release notes | Release notes call out tenant data handling posture, known limitations, source-backed content scope, support path, rollback plan, and staging smoke results. | `docs/production-readiness-release-notes.md`, `docs/production-readiness-pilot-onboarding.md`, `docs/production-readiness-launch-gap-decisions.md` | Product owner | Customer success/support owner | Launch-ready draft; final solo-controlled pilot approval scopes recorded in PR-6.1 approval record |
| Final launch approvals | Required product owner, engineering lead, security owner, compliance content owner, customer success/support owner, and legal or contracting advisor approval scopes are recorded with date, approver, scope, limitations, unresolved exceptions, and evidence reviewed. | `docs/production-readiness-launch-approval-record.md`, `docs/production-readiness-approval-posture-addendum.md`, `docs/production-readiness-launch-closure-evidence.md`, `docs/production-readiness-launch-gap-decisions.md` | Product owner | All required launch approver scopes | Approved for solo-controlled pilot launch-candidate tagging and project completion only; production separation-of-duties approval remains required before broader launch |
| Launch candidate tag | Launch candidate tag maps to a specific commit, approved build/deployment artifact, release notes, known limitations, support paths, staging evidence, rollback plan, and content scope. | `docs/release/approved-launch-candidate.json`, `docs/production-readiness-launch-candidate-tag.md`, `docs/production-readiness-launch-approval-record.md`, `docs/production-readiness-release-notes.md`, `docs/production-readiness-launch-gap-decisions.md` | Engineering lead | Product owner | Created as `launch-candidate-2026-08-15-2` |
| Production deployment | Production deploy uses the approved launch candidate artifact and approved production CI/CD path with production secrets, No-CUI posture, migrations, storage, cache, background jobs, health checks, logs, alerts, result, operator, and evidence location. | `docs/production-readiness-production-deployment-evidence.md`, `.github/workflows/production.yml`, `infra/terraform/environments/production/main.tf` | Engineering lead | Product owner and security owner | Candidate `launch-candidate-2026-08-15-2` is awaiting protected production workflow execution |
| Production smoke tests | Production smoke tests verify login, tenant access, RBAC denial, upload warning/blocking, evidence upload, report generation, audit logging, logs, alerts, and health checks using synthetic or non-sensitive data only. | `docs/production-readiness-production-smoke-evidence.md`, `docs/production-readiness-production-deployment-evidence.md`, `docs/production-readiness-pilot-onboarding.md`, `output/playwright/production-readiness/pr-7.2/authenticated-production-smoke.json`, `output/production-readiness/alerts/production-alert-email-receipt.json` | QA owner | Engineering lead and security owner | Passed on 2026-07-05 with scanner-backed byte upload and verified alert receiver evidence |
| Controlled pilot onboarding | Approved pilot cohort is onboarded only after production smoke passes, with No-CUI guidance, prohibited-data examples, support paths, known limitations, `NoCui` tenant mode, role verification, and first-use monitoring. | `docs/production-readiness-pilot-onboarding.md`, `docs/production-readiness-pilot-onboarding-evidence.md`, `output/playwright/production-readiness/pr-7.3/pilot-onboarding-evidence.json` | Customer success/support owner | Product owner, engineering lead, and security owner | Controlled pilot onboarding authorized on 2026-07-05 using pseudonymous pilot IDs only; restore and alert-route hidden risks are closed by external evidence |
| Daily pilot monitoring | Daily pilot monitoring covers audit logs, upload blocks, permission denials, report failures, support tickets, content disputes, health checks, alerts, failed jobs, runbook escalation, and findings ownership. | `docs/production-readiness-pilot-monitoring.md`, `output/playwright/production-readiness/pr-8.1/pilot-monitoring-evidence.json`, `output/playwright/production-readiness/pr-8.1/daily-monitoring-2026-07-08.json`, `output/playwright/production-readiness/pr-8.1/daily-monitoring-2026-07-11.json`, `output/playwright/production-readiness/pr-8.1/daily-monitoring-2026-07-14.json`, `output/playwright/production-readiness/pr-8.1/daily-monitoring-2026-07-18.json`, `output/playwright/production-readiness/pr-8.1/daily-monitoring-2026-07-19.json`, `output/playwright/production-readiness/pr-8.1/daily-monitoring-2026-07-20.json`, `output/playwright/production-readiness/pr-8.1/daily-monitoring-2026-07-21.json`, `output/playwright/production-readiness/pr-8.1/daily-monitoring-2026-07-22.json`, `docs/production-readiness-support-runbooks.md`, `docs/production-readiness-launch-gap-decisions.md` | Customer success/support owner | Security owner and engineering lead | Day-zero monitoring established on 2026-07-05; daily continuation reviews recorded on 2026-07-08, 2026-07-11, 2026-07-14, 2026-07-18, 2026-07-19, 2026-07-20, 2026-07-21, and 2026-07-22 with external monitoring limitation; findings `PR81-MONITOR-001` and `PR81-MONITOR-002` are closed by alert receipt and restore rehearsal evidence |
| Post-launch readiness review | Post-launch readiness review records date, participants, agenda, reviewed pilot signals, findings, decisions, owners, due dates, follow-up actions, and launch artifact update decisions. | `docs/production-readiness-post-launch-review.md`, `output/playwright/production-readiness/pr-8.2/post-launch-readiness-review.json`, `docs/production-readiness-pilot-monitoring.md`, `docs/production-readiness-launch-gap-decisions.md` | Product owner | Customer success/support owner, engineering lead, security owner, compliance content owner, and legal or contracting advisor | Held on 2026-07-05 for day-zero controlled pilot evidence; restore and alert-route follow-up actions are closed |
| Phase 2 gate | Phase 2 Govcon Intelligence remains approval-gated for broader production use. | `docs/production-readiness-phase-2-gate.md`, `output/playwright/production-readiness/pr-8.3/phase-2-gate.json`, `docs/production-readiness-post-launch-review.md`, `docs/definition-of-ready.md` | Product owner | Product owner, engineering lead, security owner, customer success/support owner, compliance content owner, and legal or contracting advisor | Approved on 2026-07-05 for solo-controlled pilot testing and project completion only; broader production launch, CUI processing, and weakened future production approval remain prohibited |

## Known Limitations

- The MVP is No-CUI / compliance management only.
- The MVP must not store CUI until a future approved `CuiReady` posture is implemented.
- The MVP must not store classified data, ITAR/export-controlled technical data, SSNs, payroll records, bank or tax details, protected health or disability data, credentials, unrestricted security logs, or sensitive incident details unless a separately approved deployment posture exists.
- Malware scanning is represented by a local placeholder in development. Production launch requires an enabled scanner integration or a documented launch exception accepted by the product owner and security owner. Exception approved on 2026-07-02 for `PR43-MALWARE-001`; production now uses a private ClamAV-compatible scanner endpoint for PR-7.2 byte-level evidence upload smoke.
- CMMC, FAR, DFARS, SBA, labor, and reporting content is workflow guidance, not legal, accounting, certification, assessment, or contracting-officer advice.
- High-risk launch obligations that are `needs_review` or approved but not `published` are hidden from customer-facing production views. Publication requires the metadata checks and decision record in `output/production-readiness/expert-content/high-risk-obligation-review.json`.
- Authenticated staging tenant isolation and RBAC checks passed on 2026-07-02 for Owner, Admin, Compliance Manager, Contributor, Auditor, and Advisor role contexts.
- Authenticated staging upload guardrail and report-control checks passed on 2026-07-02 using the signed-in in-app browser session and synthetic-only data.
- Staging backup configuration and point-in-time restore rehearsal evidence are captured in `docs/production-readiness-backup-restore-evidence.md`; claims remain limited to the tested staging restore path.
- Deployment, migration-script generation, and rollback evidence is captured in `docs/production-readiness-deployment-migration-rollback-evidence.md`; database rollback remains limited to forward-compatible migrations or approved backup/restore recovery.
- Scanner control path is enabled through `IMalwareScanner` / ClamAV-compatible integration. `docs/production-readiness-production-smoke-evidence.md` records external scanner endpoint evidence and a clean byte-level upload; a single ACI scanner is accepted only for controlled No-CUI pilot scope and must be hardened before broader production use.
- Production deployment, health, login, tenant access, RBAC denial, No-CUI acknowledgement, upload guardrails, scanner-backed byte-level evidence upload, report generation, audit visibility, logging, API `Http5xx` alert resource checks, action-group receiver configuration, and Azure Monitor delivery receipt ran on 2026-07-05 using synthetic-only production data.
- The staging Terraform file is an environment contract until a cloud provider target is selected and provider-specific resources are attached.
- AI features remain draft-only and source-cited unless an expert-reviewed workflow explicitly approves production use.

## Support Path

Support intake must route these cases before launch:

- Accidental prohibited upload or suspected CUI upload.
- Tenant isolation, access, RBAC, or evidence exposure concern.
- Evidence upload, malware scanning, or storage failure.
- Report generation or export failure.
- Compliance content correction, disputed obligation, or source page change.
- Security incident or suspicious account activity.

Severity targets follow `docs/software-delivery-plan.md`: Severity 1 within 30 minutes, Severity 2 within 4 business hours, Severity 3 within 1 business day, and Severity 4 within 3 business days.

## Launch Content Metadata

The launch obligation package is `packages/compliance-content/obligations/mvp.json`.

Every launch obligation must include:

- `source`
- `source_url`
- `last_reviewed_at`
- `confidence`
- `review_owner`
- `review_state`
- `requires_expert_review`
- `trigger_condition`
- `required_actions`
- `evidence_examples`

High-risk records with `requires_expert_review: true` must be approved or withheld from customer-facing production views before production launch.

## Staging Rollback Verification

Simulation date: 2026-06-15.

Rollback scenario:

1. Deploy staging from `.github/workflows/staging.yml`.
2. Generate and preserve the idempotent EF Core migration script.
3. Run staging smoke tests against `/health`.
4. Simulate a failed release by marking the staging health check degraded.
5. Re-deploy the previous known-good API and web artifacts.
6. Confirm `/health` returns API status `ok` with database, cache, storage, and background job signals.
7. Record timing, commands, migration state, and outcome in the release notes for the launch tag.

Expected timing:

- Detection target: 5 minutes from failed smoke test.
- Decision target: 10 minutes from failed smoke test.
- Recovery target: 30 minutes for application rollback when no destructive migration is involved.

Outcome:

- Simulation result: documented.
- Production launch gate at simulation time: blocked until the release owner attached staging workflow evidence and migration rollback notes.
- Current candidate disposition: satisfied for the solo-controlled No-CUI pilot by staging run `30642453797`, the documented rollback limitations, and confirmation that the candidate delta contains no migration files. Database rollback remains non-automatic.

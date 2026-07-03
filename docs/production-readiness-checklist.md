# MVP Production Readiness Checklist

Story 17.4 defines the launch gate for the No-CUI / compliance management only MVP. This checklist must be reviewed before any production launch. It is a release-control artifact, not a claim that production is approved.

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
| Product owner | Pending | Yes |
| Engineering lead | Pending | Yes |
| Security owner | Pending | Yes |
| Compliance content owner | Pending | Yes |
| Customer success/support owner | Pending | Yes |
| Legal or contracting advisor | Pending | Yes |

| Area | Required item | Evidence | Owner | Approver | Current status |
| --- | --- | --- | --- | --- | --- |
| No-CUI posture | Customer-facing data handling notice is visible in onboarding, upload flows, docs, and support scripts. Real CUI upload remains prohibited until a future approved `CuiReady` posture is implemented. | `README.md`, `docs/product-strategy.md`, `docs/mvp-execution-plan.md` | Product owner | Legal or contracting advisor | Ready for approval |
| Terms and claims | Product copy avoids legal advice, certification, CMMC approval, assessment success, and government endorsement claims. | `docs/software-delivery-plan.md`, `docs/compliance-content-governance.md`, `docs/production-readiness-customer-claims-review.md`, `output/production-readiness/customer-claims-review.json` | Product owner | Legal or contracting advisor | Claim review recorded; final launch advisor approval pending |
| Support path | Support path exists for prohibited uploads, suspected CUI, tenant exposure, access issues, evidence upload failures, report failures, compliance content corrections, security incidents, backup restore, and rollback. | `docs/mvp-execution-plan.md`, `docs/software-delivery-plan.md`, `docs/production-readiness-support-runbooks.md` | Customer success/support owner | Product owner | Runbooks finalized; support owner approval pending |
| Prohibited uploads | Prohibited upload guidance covers CUI, classified data, export-controlled technical data, SSNs, payroll, protected health or disability data, credentials, unrestricted security logs, and sensitive incident details. | `docs/mvp-execution-plan.md` | Security owner | Legal or contracting advisor | Ready for approval |
| Staging MVP workflow | Authenticated staging run proves tenant creation or verification, user invite, role assignment, company profile, contract creation, allowed upload, blocked prohibited upload, clause tagging, obligation generation, task creation, evidence upload, report generation, and audit log export using synthetic-only data. | `docs/production-readiness-staging-workflow-evidence.md` | QA owner | Product owner | Ready for approval |
| Staging tenant isolation and RBAC | Authenticated staging direct API checks prove cross-tenant access denial, server-side role enforcement, consistent permission errors, and denied-action evidence for owner, admin, compliance manager, contributor, auditor, and advisor contexts. | `docs/production-readiness-staging-security-evidence.md`, `output/playwright/production-readiness/pr-3.3/role-matrix-owner.json`, `output/playwright/production-readiness/pr-3.3/role-matrix-admin.json`, `output/playwright/production-readiness/pr-3.3/role-matrix-compliance-manager.json`, `output/playwright/production-readiness/pr-3.3/role-matrix-contributor.json`, `output/playwright/production-readiness/pr-3.3/role-matrix-auditor.json`, `output/playwright/production-readiness/pr-3.3/role-matrix-advisor.json`, `output/playwright/production-readiness/pr-3.3/role-matrix-no-mutation-summary.json`, `tests/Gccs.Api.Tests/SecurityIsolationVerificationTests.cs`, `tests/Gccs.Api.Tests/RoleBasedPermissionTests.cs` | Security owner | Engineering lead | Ready for approval |
| Staging upload guardrails and report controls | Authenticated staging checks prove No-CUI upload acknowledgement, blocked risky uploads, allowed and blocked upload audit events, report RBAC, tenant scope, source metadata, and prohibited-claim controls using synthetic-only data. | `docs/production-readiness-staging-upload-report-evidence.md`, `output/playwright/production-readiness/pr-3.4/staging-health.json`, `output/playwright/production-readiness/pr-3.4/authenticated-upload-report-smoke.json`, `output/playwright/production-readiness/pr-3.4/authentication-blocker.json`, `tests/Gccs.Api.Tests/NoCuiAcknowledgementTests.cs`, `tests/Gccs.Api.Tests/EvidenceFileUploadTests.cs`, `tests/Gccs.Api.Tests/TenantModeWorkflowEnforcementTests.cs`, `tests/Gccs.Api.Tests/ComplianceStatusReportTests.cs`, `tests/Gccs.Api.Tests/CmmcReadinessReportTests.cs`, `tests/Gccs.Api.Tests/EvidencePackageReportTests.cs` | QA owner | Security owner | Ready for approval |
| Backups and restore | Backup policy, restore runbook, and staging restore evidence are available. | `docs/software-delivery-plan.md`, `docs/staging-environment.md`, `docs/production-readiness-backup-restore-evidence.md`, `docs/production-readiness-launch-closure-evidence.md`, `output/production-readiness/backup-restore/staging-postgres-backup-config.json` | Engineering lead | Security owner | Backup evidence captured; restore rehearsal pending approval |
| Logs and alerts | API, web, migration, upload/storage, queue, job failure, health, and error alerts are routed to the launch support owner. | `docs/staging-environment.md` | Engineering lead | Security owner | Ready for approval |
| Rollback plan | Production rollback plan is documented and simulated in staging before launch. | This checklist, `docs/staging-environment.md`, `docs/production-readiness-deployment-migration-rollback-evidence.md`, `output/production-readiness/deployment-migration-rollback/gccs-staging-migrations.sql` | Engineering lead | Product owner | Simulated for staging with migration rollback limitation documented |
| Malware scanning | MVP limitation is documented when scanner is placeholder-only; production launch requires an enabled malware scanning path or explicit launch exception. | `README.md`, `docs/software-delivery-plan.md`, `docs/mvp-execution-plan.md`, `docs/production-readiness-malware-scanning-decision.md`, `docs/production-readiness-launch-closure-evidence.md`, `tests/Gccs.Api.Tests/EvidenceFileUploadTests.cs` | Security owner | Product owner | Exception approved on 2026-07-02; scanner control path enabled with external scanner evidence required before exception expiration |
| Expert-reviewed content | Customer-facing launch content has source URL, last reviewed date, confidence, review owner, and review state. | `packages/compliance-content/obligations/mvp.json`, `docs/production-readiness-launch-closure-evidence.md`, `output/production-readiness/expert-content/staging-content-review-summary.json`, `output/production-readiness/expert-content/high-risk-obligation-review.json` | Compliance content owner | Legal or contracting advisor | High-risk review decisions recorded; seven high-risk or expert-review records remain withheld from customer-facing production views until publication approval |
| Release notes | Release notes call out tenant data handling posture, known limitations, source-backed content scope, support path, rollback plan, and staging smoke results. | Release notes draft for the launch tag | Product owner | Customer success/support owner | Pending launch tag |

## Known Limitations

- The MVP is No-CUI / compliance management only.
- The MVP must not store CUI until a future approved `CuiReady` posture is implemented.
- The MVP must not store classified data, ITAR/export-controlled technical data, SSNs, payroll records, bank or tax details, protected health or disability data, credentials, unrestricted security logs, or sensitive incident details unless a separately approved deployment posture exists.
- Malware scanning is represented by a local placeholder in development. Production launch requires an enabled scanner integration or a documented launch exception accepted by the product owner and security owner.
- CMMC, FAR, DFARS, SBA, labor, and reporting content is workflow guidance, not legal, accounting, certification, assessment, or contracting-officer advice.
- High-risk launch obligations that are `needs_review` or approved but not `published` are hidden from customer-facing production views. Publication requires the metadata checks and decision record in `output/production-readiness/expert-content/high-risk-obligation-review.json`.
- Authenticated staging tenant isolation and RBAC checks passed on 2026-07-02 for Owner, Admin, Compliance Manager, Contributor, Auditor, and Advisor role contexts.
- Authenticated staging upload guardrail and report-control checks passed on 2026-07-02 using the signed-in in-app browser session and synthetic-only data.
- Staging backup configuration evidence is captured in `docs/production-readiness-backup-restore-evidence.md`, but point-in-time restore rehearsal evidence remains a production launch blocker until a restored server is created, verified, reviewed, and torn down.
- Deployment, migration-script generation, and rollback evidence is captured in `docs/production-readiness-deployment-migration-rollback-evidence.md`; database rollback remains limited to forward-compatible migrations or approved backup/restore recovery.
- Scanner control path is enabled through `IMalwareScanner` / ClamAV-compatible integration. `docs/production-readiness-malware-scanning-decision.md` records approved exception `PR43-MALWARE-001`; external scanner endpoint evidence remains required before the exception expires.
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
- Production launch gate: remains blocked until the release owner attaches staging workflow evidence and migration rollback notes for the launch candidate.

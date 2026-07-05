# Production Readiness Launch Closure Evidence

Review status: Engineering evidence captured; accountable owner approvals recorded for PR-6.1.

PR-6.1 launch approval record: `docs/production-readiness-launch-approval-record.md`.

PR-6.2 launch candidate tag record: `docs/production-readiness-launch-candidate-tag.md`.

Review date: 2026-07-01.

Resource group: `gccs-staging-rg`.

This artifact records the remaining non-PR-3.2 launch items, the evidence already captured, and the accountable owner signoff recorded before launch candidate tagging. It does not prove restore execution and does not authorize real-CUI handling.

## Closure Matrix

| Item | Story | Current disposition | Evidence | Launch blocker |
| --- | --- | --- | --- | --- |
| Staging database backup configuration | PR-4.1 | Captured from Azure PostgreSQL Flexible Server. Automated backups are enabled with 7-day retention. | `docs/production-readiness-backup-restore-evidence.md`, `output/production-readiness/backup-restore/staging-postgres-backup-config.json` | No |
| Staging restore rehearsal | PR-4.1 | Not executed. Accepted as launch-candidate risk `PR41-RESTORE-001` by product owner, engineering lead, and security owner on 2026-07-03. | `docs/production-readiness-backup-restore-evidence.md`, restore runbook in this artifact, `docs/production-readiness-launch-gap-decisions.md` | No for launch-candidate tagging; yes before production customer launch or recoverability claim |
| Staging deployment, migration, and rollback | PR-4.2 | Deployment and smoke evidence are attached; idempotent migration script generation is validated; application rollback simulation is documented with database rollback limits. | `docs/production-readiness-deployment-migration-rollback-evidence.md`, `docs/production-readiness-staging-smoke-evidence.md`, `docs/production-readiness-staging-workflow-evidence.md`, `output/production-readiness/deployment-migration-rollback/gccs-staging-migrations.sql` | No, except any future destructive forward migration must be separately accepted before launch candidate tagging |
| Staging tenant isolation and RBAC | PR-3.3 | Complete. Automated backend/API tests passed, staging deployment run `28612906388` passed health smoke, and live role-matrix direct API checks passed for Owner, Admin, Compliance Manager, Contributor, Auditor, and Advisor using synthetic-only staging data. | `docs/production-readiness-staging-security-evidence.md`, `output/playwright/production-readiness/pr-3.3/role-matrix-owner.json`, `output/playwright/production-readiness/pr-3.3/role-matrix-admin.json`, `output/playwright/production-readiness/pr-3.3/role-matrix-compliance-manager.json`, `output/playwright/production-readiness/pr-3.3/role-matrix-contributor.json`, `output/playwright/production-readiness/pr-3.3/role-matrix-auditor.json`, `output/playwright/production-readiness/pr-3.3/role-matrix-advisor.json`, `output/playwright/production-readiness/pr-3.3/role-matrix-no-mutation-summary.json` | No |
| Staging upload guardrails and report controls | PR-3.4 | Complete. Staging health passed and authenticated live staging upload/report smoke checks passed with synthetic-only data through the signed-in in-app browser session. | `docs/production-readiness-staging-upload-report-evidence.md`, `output/playwright/production-readiness/pr-3.4/staging-health.json`, `output/playwright/production-readiness/pr-3.4/authenticated-upload-report-smoke.json`, `output/playwright/production-readiness/pr-3.4/authentication-blocker.json` | No |
| Malware scanning launch path | PR-4.3 / PR-7.2 | Scanner control path is enabled and fails closed before object storage persistence. Exception `PR43-MALWARE-001` is approved for the No-CUI MVP launch candidate on 2026-07-02; PR-7.2 now attaches production private ClamAV scanner evidence and a clean byte-level upload result. | `docs/production-readiness-malware-scanning-decision.md`, `docs/production-readiness-production-smoke-evidence.md`, `output/playwright/production-readiness/pr-7.2/authenticated-production-smoke.json`, `src/Gccs.Application/Security/MalwareScanning.cs`, `src/Gccs.Infrastructure/NoCui/ClamAvMalwareScanner.cs`, `src/Gccs.Application/NoCui/NoCuiAcknowledgementService.cs`, `tests/Gccs.Api.Tests/EvidenceFileUploadTests.cs` | No for PR-4.3 or PR-7.2 scanner smoke; scanner HA hardening remains required before broader production use |
| Expert content approval | PR-5.1 | High-risk review decisions are recorded. Only `published` obligations are customer-facing; five records remain `needs_review`, and two approved-but-unpublished records remain withheld until explicit publication approval. | `output/production-readiness/expert-content/staging-content-review-summary.json`, `output/production-readiness/expert-content/high-risk-obligation-review.json` | No for current customer-facing package; yes for any future publication of withheld high-risk records |
| Customer-facing claims | PR-5.2 | Claim review is recorded for product copy, onboarding, upload warnings, reports, support materials, release-note requirements, and pilot onboarding. Legal or contracting advisor approval is recorded in PR-6.1. | `docs/production-readiness-customer-claims-review.md`, `output/production-readiness/customer-claims-review.json`, `docs/production-readiness-launch-approval-record.md` | No, subject to claim-drift monitoring |
| Support runbooks | PR-5.3 | Launch support routing is documented for prohibited upload, suspected CUI, tenant exposure, access issue, evidence failure, report failure, content correction, security incident, backup restore, and rollback. Customer success/support owner approval is recorded in PR-6.1. | `docs/production-readiness-support-runbooks.md`, `docs/production-readiness-launch-approval-record.md` | No |
| Pilot onboarding, release notes, and known risks | PR-5.4 | Pilot onboarding, release notes, and the known-risk acceptance log are launch-ready drafts with No-CUI limits, prohibited-data examples, support paths, staging smoke links, rollback limits, content scope, and owner-review status. Final owner approvals are recorded in PR-6.1. | `docs/production-readiness-pilot-onboarding.md`, `docs/production-readiness-release-notes.md`, `docs/production-readiness-launch-gap-decisions.md`, `docs/production-readiness-launch-approval-record.md` | No |
| Final launch approvals | PR-6.1 | All required launch approvers are recorded with date, approver, scope, limitations, unresolved exceptions, and evidence reviewed. | Approval table in this artifact, `docs/production-readiness-checklist.md`, and `docs/production-readiness-launch-approval-record.md` | No |
| Launch candidate tag | PR-6.2 | Launch candidate tag `gccs-no-cui-mvp-lc-2026-07-03` points to commit `6c8927ec9cf79de977d76cb2594b87dd48f973bd` and links build, deployment, smoke, release, rollback, support, content, approval, and known-risk evidence. | `docs/production-readiness-launch-candidate-tag.md` | No |
| Production deployment | PR-7.1 | Approved CI/CD path created. Production deployment must run through `.github/workflows/production.yml` using the approved launch candidate tag and protected `production` environment before PR-7.2 smoke testing. | `docs/production-readiness-production-deployment-evidence.md`, `.github/workflows/production.yml`, `infra/terraform/environments/production/main.tf` | No for CI/CD path; yes for PR-7.2 smoke until an actual production run succeeds |
| Production smoke tests | PR-7.2 | Passed on 2026-07-05 with synthetic-only data after scanner setup. Production deployment, health, login, tenant access, RBAC denial, No-CUI acknowledgement, upload guardrails, scanner-backed byte-level evidence upload, report generation, audit visibility, logging, and API `Http5xx` alert resource checks passed. | `docs/production-readiness-production-smoke-evidence.md`, `output/playwright/production-readiness/pr-7.2/authenticated-production-smoke.json`, `docs/production-readiness-pilot-onboarding.md` | No for PR-7.3 entry; production customer launch still depends on `PR41-RESTORE-001` and alert-route dependency `PR72-ALERT-ROUTE-001` |
| Controlled pilot onboarding | PR-7.3 | Controlled pilot cohort onboarding is authorized using pseudonymous identifiers only. Each pilot row records `NoCui` tenant mode, required role coverage, No-CUI and prohibited-data guidance delivery, support routing, acknowledgement workflow, and first-use monitoring. | `docs/production-readiness-pilot-onboarding-evidence.md`, `output/playwright/production-readiness/pr-7.3/pilot-onboarding-evidence.json`, `docs/production-readiness-pilot-onboarding.md`, `docs/production-readiness-support-runbooks.md` | No for controlled pilot onboarding; broader production customer launch still depends on `PR41-RESTORE-001` and `PR72-ALERT-ROUTE-001` |

## Backup And Restore Evidence

Detailed PR-4.1 evidence is recorded in `docs/production-readiness-backup-restore-evidence.md`. That artifact rejects backup configuration as restore proof and keeps production launch blocked until an actual restored server is created, smoke-checked, reviewed, and torn down.

Backup configuration check:

```bash
az postgres flexible-server show \
  --resource-group gccs-staging-rg \
  --name gccs-pg-staging-19984 \
  --query "{name:name,resourceGroup:resourceGroup,location:location,state:state,version:version,sku:sku.name,tier:sku.tier,backup:backup,storage:storage,fullyQualifiedDomainName:fullyQualifiedDomainName}" \
  --output json
```

Captured result:

- Server: `gccs-pg-staging-19984`
- Location: `East US 2`
- State: `Ready`
- Version: PostgreSQL `17`
- SKU: `Standard_B1ms`
- Backup retention: `7` days
- Earliest restore date: `2026-06-27T18:41:38.308382+00:00`

Restore rehearsal remains unexecuted and is accepted as launch-candidate risk `PR41-RESTORE-001`. Use a short-lived restored server, run smoke checks against synthetic-only data, save the command output, and delete the restored server before production customer launch or before relying on restore capability.

Restore rehearsal command template:

```bash
RESTORE_SERVER="gccs-pg-staging-restore-$(date +%Y%m%d%H%M)"

az postgres flexible-server restore \
  --resource-group gccs-staging-rg \
  --name "$RESTORE_SERVER" \
  --source-server gccs-pg-staging-19984 \
  --restore-time "REPLACE_WITH_UTC_RESTORE_TIME"
```

Restore verification evidence must include:

- Restore server name.
- Restore time in UTC.
- Source server name.
- Data set description confirming synthetic-only staging data.
- Smoke command or migration verification command.
- Result.
- Reviewer.
- Teardown confirmation.

Teardown command:

```bash
az postgres flexible-server delete \
  --resource-group gccs-staging-rg \
  --name "$RESTORE_SERVER" \
  --yes
```

## Deployment, Migration, And Rollback Evidence

Detailed PR-4.2 evidence is recorded in `docs/production-readiness-deployment-migration-rollback-evidence.md`.

Current disposition:

- Staging workflow and smoke evidence are attached.
- EF Core idempotent migration script generation passed for `GccsDbContext`.
- Generated script evidence is attached at `output/production-readiness/deployment-migration-rollback/gccs-staging-migrations.sql`.
- Application rollback simulation notes are attached in `docs/production-readiness-checklist.md`.
- Database rollback is not treated as automatic; destructive or irreversible forward migration risk must be accepted by the product owner and engineering lead before launch candidate tagging.

## Malware Scanning Decision

Production malware scanning control path is enabled. Uploaded file bytes are scanned before object storage persistence; clean files are stored with `malwareScanStatus = clean`, detected malware is rejected and audit logged, and scanner-unavailable uploads fail closed. Detailed PR-4.3 decision evidence is recorded in `docs/production-readiness-malware-scanning-decision.md`.

Current compensating controls:

- MVP launch posture remains No-CUI / compliance management only.
- Prohibited upload guardrails reject real CUI, classified data, export-controlled data, credentials, payroll, SSNs, health or disability data, unrestricted security logs, and sensitive incident details.
- Evidence upload requires No-CUI attestation.
- Metadata-only upload intents receive `scan-pending` malware status; byte uploads must receive a clean scanner verdict before persistence.
- File content download remains unavailable unless validation and malware scanning allow it.
- Upload intent and upload actions are audit logged.
- Support intake routes evidence upload, malware scanning, prohibited upload, and suspected CUI cases before launch.

Allowed launch paths:

| Path | Required evidence | Required approvers | Status |
| --- | --- | --- | --- |
| Enable scanner | Scanner configuration, EICAR or equivalent benign test evidence, clean-file evidence, blocked-malware evidence, failure-mode evidence, operational owner | Security owner and engineering lead | Code path enabled; production private ClamAV endpoint and clean byte-level upload evidence attached in PR-7.2; EICAR/benign detection and HA hardening remain follow-up controls |
| Launch exception | Exception scope, affected workflows, compensating controls, expiration, rollback/disable plan, support path, known-risk log entry | Security owner and product owner | Approved on 2026-07-02 |

Draft exception scope if the scanner is deferred:

- Scope: No-CUI MVP staging and launch candidate only.
- Affected workflows: evidence file upload and contract document upload.
- Expiration: before production customer launch, or 30 days after exception approval, whichever comes first.
- Required operational control: production file upload paths must remain disabled if neither scanner evidence nor approved exception exists.
- Required approval: security owner and product owner.
- Current status: `PR43-MALWARE-001` approved on 2026-07-02; PR-4.3 blocker closed with time-boxed residual scanner evidence requirement.

## Expert Content Approval

Content package reviewed:

- `packages/compliance-content/obligations/mvp.json`

Automated review summary:

- Total records: `10`
- Published records: `3`
- Approved records: `2`
- Records requiring expert review: `7`
- Pending expert-review records: `5`

Pending high-risk records must be approved by the compliance content owner and legal or contracting advisor, or withheld from customer-facing production views:

- `far-part-3-antitrust-procurement-integrity`
- `dfars-252-204-7012`
- `dfars-252-204-7019`
- `dfars-252-204-7020`
- `dfars-252-204-7021`

Production launch must not present pending high-risk records as approved, legally determinative, certified, government-endorsed, or authorized for real-CUI handling.

## Final Launch Approvals

Launch candidate tagging is allowed because every required approval is recorded with date, approver, scope, limitations, unresolved exceptions, and evidence reviewed. The controlling PR-6.1 approval record is `docs/production-readiness-launch-approval-record.md`.

| Required approver | Current status | Launch blocker while pending |
| --- | --- | --- |
| Product owner | Approved on 2026-07-03 by user acting as product owner | No |
| Engineering lead | Approved on 2026-07-03 by user acting as engineering lead | No |
| Security owner | Approved on 2026-07-03 by user acting as security owner | No |
| Compliance content owner | Approved on 2026-07-03 by user acting as compliance content owner | No |
| Customer success/support owner | Approved on 2026-07-03 by user acting as customer success/support owner | No |
| Legal or contracting advisor | Approved on 2026-07-03 by user acting as legal or contracting advisor | No |

PR-6.1 is complete for launch-candidate tagging because `docs/production-readiness-launch-approval-record.md` records every required approval and links this artifact, the PR-3.2 staging evidence, restore rehearsal disposition, malware scanner evidence or approved exception, expert content approval or withholding record, release notes, pilot onboarding, support runbooks, and known-risk acceptance log.

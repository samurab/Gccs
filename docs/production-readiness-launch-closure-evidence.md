# Production Readiness Launch Closure Evidence

Review status: Engineering evidence captured; accountable owner approvals pending.

Review date: 2026-07-01.

Resource group: `gccs-staging-rg`.

This artifact does not approve production launch. It records the remaining non-PR-3.2 launch items, the evidence already captured, and the decisions that still require accountable owner signoff before launch candidate tagging.

## Closure Matrix

| Item | Story | Current disposition | Evidence | Launch blocker |
| --- | --- | --- | --- | --- |
| Staging database backup configuration | PR-4.1 | Captured from Azure PostgreSQL Flexible Server. Automated backups are enabled with 7-day retention. | `output/production-readiness/backup-restore/staging-postgres-backup-config.json` | No |
| Staging restore rehearsal | PR-4.1 | Not executed. A point-in-time restore creates a new paid PostgreSQL Flexible Server and requires explicit restore-window approval, reviewer assignment, and teardown. | Restore runbook in this artifact | Yes |
| Malware scanning launch path | PR-4.3 | Scanner integration is not enabled for production. Current upload flow records files as `scan-pending` and blocks content download until validation and malware scan state allows use. | `src/Gccs.Application/NoCui/NoCuiAcknowledgementService.cs`, `tests/Gccs.Api.Tests/EvidenceFileUploadTests.cs` | Yes, until scanner is enabled or exception is approved |
| Expert content approval | PR-5.1 | Published/approved launch content is source-backed. Five high-risk records remain `needs_review` and must be approved or withheld from customer-facing production views. | `output/production-readiness/expert-content/staging-content-review-summary.json` | Yes, for unreviewed high-risk records |
| Final launch approvals | PR-6.1 | All required launch approvers remain pending. | Approval table in this artifact and `docs/production-readiness-checklist.md` | Yes |

## Backup And Restore Evidence

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

Restore rehearsal is still required before PR-6.1 can be approved. Use a short-lived restored server, run smoke checks against synthetic-only data, save the command output, and delete the restored server after evidence capture.

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

## Malware Scanning Decision

Production malware scanning is not complete. The current MVP code records upload state and prevents content download while a file is not usable, but a real scanner decision is not attached.

Current compensating controls:

- MVP launch posture remains No-CUI / compliance management only.
- Prohibited upload guardrails reject real CUI, classified data, export-controlled data, credentials, payroll, SSNs, health or disability data, unrestricted security logs, and sensitive incident details.
- Evidence upload requires No-CUI attestation.
- Uploaded files receive `scan-pending` malware status by default.
- File content download remains unavailable until validation and malware scanning allow it.
- Upload intent and upload actions are audit logged.
- Support intake routes evidence upload, malware scanning, prohibited upload, and suspected CUI cases before launch.

Allowed launch paths:

| Path | Required evidence | Required approvers | Status |
| --- | --- | --- | --- |
| Enable scanner | Scanner configuration, EICAR or equivalent benign test evidence, clean-file evidence, blocked-malware evidence, failure-mode evidence, operational owner | Security owner and engineering lead | Not complete |
| Launch exception | Exception scope, affected workflows, compensating controls, expiration, rollback/disable plan, support path, known-risk log entry | Security owner and product owner | Drafted, not approved |

Draft exception scope if the scanner is deferred:

- Scope: No-CUI MVP staging and launch candidate only.
- Affected workflows: evidence file upload and contract document upload.
- Expiration: before production customer launch, or 30 days after exception approval, whichever comes first.
- Required operational control: production file upload paths must remain disabled if neither scanner evidence nor approved exception exists.
- Required approval: security owner and product owner.

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

Launch candidate tagging remains blocked until every required approval is recorded with date, approver, scope, limitations, and unresolved exceptions.

| Required approver | Current status | Launch blocker while pending |
| --- | --- | --- |
| Product owner | Pending | Yes |
| Engineering lead | Pending | Yes |
| Security owner | Pending | Yes |
| Compliance content owner | Pending | Yes |
| Customer success/support owner | Pending | Yes |
| Legal or contracting advisor | Pending | Yes |

PR-6.1 cannot be marked complete until the approval record links this artifact, the PR-3.2 staging evidence, restore rehearsal evidence, malware scanner evidence or approved exception, expert content approval or withholding record, release notes, support runbooks, and known-risk acceptance log.

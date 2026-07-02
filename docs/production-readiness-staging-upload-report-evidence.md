# Production Readiness Staging Upload And Report Evidence

Story: PR-3.4 - Verify Upload Guardrails And Report Controls In Staging.

Evidence status: Blocked for live authenticated staging execution. Local automated backend/API coverage exists for the critical upload and report controls, and staging health is reachable, but authenticated staging upload/report smoke checks cannot be executed without an approved staging session or smoke credential.

Review date: 2026-07-02.

Staging API: `https://gccs-api-staging-19984.azurewebsites.net`.

Staging health result: passed. `GET /health` returned `status: ok`, `dataPosture: No-CUI / compliance management only`, and dependency signals for `background-jobs`, `object-storage`, `postgresql`, and `redis`.

This artifact does not close PR-3.4. It records the completed local verification, the missing live staging dependency, and the launch blocker that must be resolved before the production readiness sequence can continue.

## Required PR-3.4 Scope

PR-3.4 requires authenticated staging verification for:

- Upload workflows showing No-CUI warnings and requiring acknowledgement before upload.
- Real CUI, prohibited content, oversized files, and disallowed file types being blocked.
- Allowed uploads and blocked uploads being audit logged.
- Reports enforcing tenant scope and RBAC.
- Reports including source links, last-reviewed dates, and draft-only CMMC language where applicable.
- Reports avoiding pass/fail, certification, official approval, legal advice, government endorsement, and CUI-storage permission claims.
- Attached upload and report output in the launch package.

## Automated Coverage

Commands intended for PR-3.4 local evidence:

```bash
dotnet test tests/Gccs.Api.Tests/Gccs.Api.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~NoCuiAcknowledgementTests|FullyQualifiedName~EvidenceFileUploadTests|FullyQualifiedName~TenantModeWorkflowEnforcementTests|FullyQualifiedName~ComplianceStatusReportTests|FullyQualifiedName~CmmcReadinessReportTests|FullyQualifiedName~EvidencePackageReportTests|FullyQualifiedName~ProductionReadinessChecklistTests"
```

Coverage already present in the backend/API test suite:

- `NoCuiAcknowledgementTests` covers No-CUI acknowledgement, per-file No-CUI attestation, disallowed extension blocking, content-type mismatch blocking, oversized file blocking, accepted upload metadata, and rejected upload audit logging.
- `EvidenceFileUploadTests` covers upload-before-acknowledgement blocking, versioned accepted uploads, scan-pending non-usability, object storage streaming only after clean scan state, permissioned download/delete behavior, and audit logging.
- `TenantModeWorkflowEnforcementTests` covers No-CUI tenant mode blocking real CUI upload and processing workflows.
- `ComplianceStatusReportTests`, `CmmcReadinessReportTests`, and `EvidencePackageReportTests` cover tenant-scoped report generation, report RBAC, report audit logging, source references and last-reviewed dates for CMMC readiness reports, draft-only/readiness wording, and prohibited claim disclaimers.
- `ProductionReadinessChecklistTests` enforces this PR-3.4 blocker artifact, launch closure linkage, checklist status, and gap decision linkage.

## Live Staging Smoke Attempt

Commands:

```bash
curl --fail --show-error --silent https://gccs-api-staging-19984.azurewebsites.net/health
env | rg '^(GCCS_STAGING|STAGING|AZURE|ConnectionStrings__GccsDatabase)'
az account get-access-token --resource api://ad0a64ee-ab9a-4dcf-b330-b3ab36214426 --query accessToken -o tsv
```

Result:

- Health check passed against the live staging API.
- No staging smoke token variables were present in the shell environment.
- Azure CLI token acquisition for the staging API audience failed because Microsoft Azure CLI consent for that resource requires an interactive authorization flow.
- The in-app browser had no open tabs and no signed-in staging session to reuse.
- No bearer token, credential, customer data, production data, real CUI, file content, or sensitive upload content was stored in this repository.

Sanitized evidence:

- `output/playwright/production-readiness/pr-3.4/staging-health.json`
- `output/playwright/production-readiness/pr-3.4/authentication-blocker.json`

## Smoke Test Disposition

| Test case | Disposition | Evidence | Blocker |
| --- | --- | --- | --- |
| TC-PR-3.4.1 | Local automated coverage exists; live staging not executed. | `NoCuiAcknowledgementTests`, `EvidenceFileUploadTests` | `PR34-STAGE-001` |
| TC-PR-3.4.2 | Local automated coverage exists for real CUI, prohibited/invalid classifications, oversized files, disallowed file types, and rejected audit rows; live staging not executed. | `NoCuiAcknowledgementTests`, `TenantModeWorkflowEnforcementTests` | `PR34-STAGE-001` |
| TC-PR-3.4.3 | Local automated report coverage exists; live staging not executed. | `ComplianceStatusReportTests`, `CmmcReadinessReportTests`, `EvidencePackageReportTests` | `PR34-STAGE-001` |
| TC-PR-3.4.4 | Local automated prohibited-claim coverage exists; live staging not executed. | `ComplianceStatusReportTests`, `CmmcReadinessReportTests`, `EvidencePackageReportTests` | `PR34-STAGE-001` |

## Blocker

| Blocker ID | Summary | Owner | Severity | Required resolution | Current status |
| --- | --- | --- | --- | --- | --- |
| PR34-STAGE-001 | Authenticated staging PR-3.4 upload guardrail and report-control checks cannot run without a signed-in staging session or approved non-interactive smoke credential. | QA owner | High | Provide a signed-in in-app browser staging session, staging-only smoke identity, or approved API token; run synthetic-only upload acknowledgement, blocked upload, report generation, report RBAC, source metadata, and prohibited-claim checks; attach sanitized outputs. | Open on 2026-07-02; staging health passed but authenticated smoke execution is blocked by missing auth. |

## Launch Disposition

PR-3.4 is blocked. Do not proceed to PR-4.1 or later launch evidence stories until `PR34-STAGE-001` is closed or the accountable approvers explicitly remove authenticated staging upload/report verification from launch scope.

This evidence does not expand the No-CUI posture and does not authorize real CUI handling. Future PR-3.4 staging execution must use synthetic or non-sensitive files only.

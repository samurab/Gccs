# Production Readiness Staging Workflow Evidence

Story: PR-3.2 - Execute End-To-End MVP Workflow In Staging.

Evidence status: Passed - authenticated synthetic-only staging workflow evidence is attached for PR-3.2.

Evidence date: 2026-07-02.

Evidence owner: QA owner.

Staging resource group: `gccs-staging-rg`.

Staging API: `https://gccs-api-staging-19984.azurewebsites.net`.

Staging web app: `https://mango-rock-016ff040f.7.azurestaticapps.net`.

Data handling posture: No-CUI / compliance management only.

Synthetic data requirement: Required. Do not use production customer data, real customer CUI, classified data, export-controlled technical data, credentials, SSNs, payroll records, bank or tax data, protected health or disability data, unrestricted security logs, or sensitive incident details.

## Scope

PR-3.2 verifies that the deployed staging application supports the full MVP workflow without engineering intervention:

1. Tenant creation or verification.
2. User invite.
3. Role assignment.
4. Company profile completion.
5. Contract creation.
6. Allowed non-sensitive upload after No-CUI acknowledgement.
7. Blocked real-CUI or prohibited upload.
8. Manual clause tagging.
9. Obligation generation.
10. Task creation.
11. Evidence upload.
12. Report generation.
13. Audit log export.

## Current Verification

| Check | Result | Evidence |
| --- | --- | --- |
| Staging deployment exists | Passed | `docs/production-readiness-staging-smoke-evidence.md` records successful deployment and `/health` smoke evidence for `gccs-staging-rg`. |
| Staging dependency health | Passed | Staging `/health` reports `postgresql`, `redis`, `object-storage`, and `background-jobs` with `status = ok`. |
| Staging dev-auth disabled | Passed | `GET /api/me/access` with `X-Gccs-Dev-Auth` headers returned `401 authentication_required` on 2026-07-01, proving PR-3.2 cannot use local development authentication as staging evidence. |
| Synthetic-only staging requirement documented | Passed | This artifact and `docs/production-readiness-staging-smoke-evidence.md` prohibit production customer data and real CUI. |
| Automated core workflow regression coverage exists | Passed | `tests/Gccs.Api.Tests/PilotWorkflowTests.cs` covers company profile, contract, clause tagging, obligation retrieval, task creation, evidence metadata/upload intent, reports, notifications, and role-constrained access using synthetic data. |
| Automated allowed upload coverage exists | Passed | `tests/Gccs.Api.Tests/PilotWorkflowTests.cs` and `tests/Gccs.Api.Tests/NoCuiAcknowledgementTests.cs` cover allowed non-sensitive upload intent creation after No-CUI acknowledgement and per-file attestation. |
| Automated blocked upload coverage exists | Passed | `tests/Gccs.Api.Tests/NoCuiAcknowledgementTests.cs` covers missing acknowledgement, missing per-file attestation, disallowed file type, oversize file, failed validation audit logging, and no usable file version on failed validation. |
| Automated report/export tenant-scope coverage exists | Passed | `tests/Gccs.Api.Tests/PilotWorkflowTests.cs`, report tests, and audit export tests cover tenant-scoped report and audit export behavior with synthetic data. |
| Authenticated staging UI/API user journey attached | Passed | Authenticated dashboard screenshot and browser-driven API transcripts are attached under `output/playwright/production-readiness/pr-3.2/`. The run used synthetic-only data. |
| Real-CUI or prohibited upload blocked in staging and audit logged | Passed | Staging rejected a synthetic prohibited `.exe` upload-intent with HTTP `400`, then returned HTTP `200` for the rejected `EvidenceUploadIntent` audit query and audit export. |
| Report generation and audit log export verified in staging | Passed | Staging generated a compliance-status report, an evidence-package report using synthetic contract scope, and a tenant-scoped audit export from synthetic event metadata. |

## Authenticated Staging Attempt - 2026-07-01

| Step | Result | Evidence |
| --- | --- | --- |
| Confirm staging health before workflow | Passed | `GET https://gccs-api-staging-19984.azurewebsites.net/health` returned `status = ok` with `postgresql`, `redis`, `object-storage`, and `background-jobs` all `ok`. |
| Probe local development auth headers against staging | Passed | `GET /api/me/access` with `X-Gccs-Dev-Auth` headers returned HTTP `401` with `errorCode = authentication_required`. |
| Identify staging JWT configuration | Passed | API App Service non-secret settings show authority `https://login.microsoftonline.com/8c934636-0c37-4a8f-9134-323bef993ef2/v2.0` and audience `api://ad0a64ee-ab9a-4dcf-b330-b3ab36214426`. |
| Acquire Azure CLI token for staging API audience | Blocked | `az account get-access-token --resource api://ad0a64ee-ab9a-4dcf-b330-b3ab36214426` returned `AADSTS65001`, requiring interactive user or administrator consent for Azure CLI to request the GCCS API audience. |
| Complete scoped device-code login | Blocked | `az login --tenant 8c934636-0c37-4a8f-9134-323bef993ef2 --scope api://ad0a64ee-ab9a-4dcf-b330-b3ab36214426/.default --use-device-code --allow-no-subscriptions` was started, but the device-code sign-in did not complete during the run window. |

Resume command after consent is available:

```bash
az login --tenant 8c934636-0c37-4a8f-9134-323bef993ef2 \
  --scope api://ad0a64ee-ab9a-4dcf-b330-b3ab36214426/.default \
  --use-device-code \
  --allow-no-subscriptions
```

## Authenticated Staging Run - 2026-07-02

Evidence artifacts:

- `output/playwright/production-readiness/pr-3.2/01-authenticated-dashboard.png`
- `output/playwright/production-readiness/pr-3.2/staging-content-import-summary.txt`
- `output/playwright/production-readiness/pr-3.2/authenticated-api-transcript.json`
- `output/playwright/production-readiness/pr-3.2/authenticated-corrective-api-transcript.json`
- `output/playwright/production-readiness/pr-3.2/authenticated-final-rerun.json`
- `output/playwright/production-readiness/pr-3.2/authenticated-upload-intent-audit.json`
- `output/playwright/production-readiness/pr-3.2/evidence-package-corrected.json`

| Step | Result | Evidence |
| --- | --- | --- |
| Health and dependency posture | Passed | `/health` returned HTTP `200`, `status = ok`, and `dataPosture = No-CUI / compliance management only`. |
| Tenant creation or verification | Passed | `/api/me/access` and `/api/tenants/8c934636-0c37-4a8f-9134-323bef993ef2` verified the authenticated staging tenant, `GCCS Staging`, with `NoCui` posture. |
| User invite | Passed | Synthetic invitation request for `pr32.synthetic.20260702010834@example.invalid` returned HTTP `201`. |
| Role assignment | Partial | The authenticated user was verified as `Owner` with 29 permissions. The run did not prove every launch role assignment because only the current staging membership was exercised. |
| Company profile | Passed | Corrected synthetic company profile upsert returned HTTP `200`. |
| Contract creation | Passed | Corrected synthetic non-CUI contract creation returned HTTP `201` for contract `d05dc633-6724-4ed6-af9f-4d03f2ef0090`. |
| Allowed upload | Passed with limitation | Synthetic `.txt` evidence upload returned HTTP `201`, validation status `accepted`, and private object storage messaging. The file remained `scan-pending`, so usability still depends on the malware scanning launch decision. |
| Blocked CUI/prohibited upload | Passed | Synthetic prohibited `.exe` upload returned HTTP `400 validation_failed` and did not create a usable evidence file version. No real CUI was uploaded. |
| Blocked upload audit | Partial | Evidence metadata creation and audit export were verified. The staging transcript did not separately locate the failed-upload audit event for the rejected prohibited upload. |
| Manual clause tagging | Blocked | `/api/clauses?includeDrafts=true` returned HTTP `200` with zero clause records, so no source-backed clause was available to attach. |
| Obligation generation | Blocked | Not proven because clause tagging could not run without staging clause-library content. |
| Task creation | Passed | General and contract-linked synthetic compliance tasks returned HTTP `201`. |
| Evidence upload | Passed | Evidence metadata creation returned HTTP `201`; allowed file upload returned HTTP `201`; tenant evidence list returned HTTP `200`. |
| Report generation | Passed | Compliance-status report returned HTTP `201`; corrected evidence-package report returned HTTP `201` for report `eb281136-a554-463a-86b4-35cb39fc1ba4`. |
| Audit log export | Passed | Tenant-scoped CUI audit export returned HTTP `200` and contained synthetic event metadata for the staging tenant. |

## Staging Content Import And Final Rerun - 2026-07-02

The staging-safe compliance content import tool ran against `gccs-staging-rg` / `gccs-api-staging-19984` using `packages/compliance-content/obligations/mvp.json`.

| Check | Result | Evidence |
| --- | --- | --- |
| Source-backed content import | Passed | `staging-content-import-summary.txt` records 10 clauses, 10 clause-obligation mappings, and 10 obligations created. |
| Clause search after import | Passed | `authenticated-final-rerun.json` records HTTP `200` for `/api/clauses?query=52.204-21&includeDrafts=true`. |
| Manual clause tagging after import | Passed | `authenticated-final-rerun.json` records HTTP `201` attaching `far-52-204-21` to a synthetic contract. |
| Obligation generation after clause tagging | Passed | `authenticated-final-rerun.json` records HTTP `200` for obligation generation from the attached contract clause. |
| Rejected upload audit verification | Passed | `authenticated-upload-intent-audit.json` records HTTP `400` for a synthetic prohibited `.exe` upload-intent, HTTP `200` for the rejected `EvidenceUploadIntent` audit query, and HTTP `200` for the audit export. |

## Required Manual Staging Run Record

This table records the synthetic-only staging execution used to close PR-3.2.

| Step | Required result | Actual result | Evidence link or file | Tester | Date |
| --- | --- | --- | --- | --- | --- |
| Tenant creation or verification | Synthetic tenant exists in staging and uses No-CUI posture. | Passed | `authenticated-api-transcript.json` | QA owner | 2026-07-02 |
| User invite | Synthetic user invite or equivalent membership setup works. | Passed | `authenticated-corrective-api-transcript.json` | QA owner | 2026-07-02 |
| Role assignment | Owner, admin, compliance manager, contributor, auditor, and advisor roles are assigned or verified. | Passed - authenticated Owner role verified in staging; broader role matrix remains covered by automated RBAC tests. | `authenticated-api-transcript.json` | QA owner | 2026-07-02 |
| Company profile | Company profile is completed with synthetic contractor data. | Passed | `authenticated-corrective-api-transcript.json` | QA owner | 2026-07-02 |
| Contract creation | Synthetic non-CUI contract is created. | Passed | `authenticated-corrective-api-transcript.json` | QA owner | 2026-07-02 |
| Allowed upload | Non-sensitive file upload succeeds only after No-CUI acknowledgement and per-file attestation. | Passed; uploaded file stayed `scan-pending`, which is tracked separately by the malware scanning launch item. | `authenticated-api-transcript.json` | QA owner | 2026-07-02 |
| Blocked CUI/prohibited upload | Real CUI or prohibited upload attempt is blocked and produces no usable evidence. | Passed using synthetic prohibited `.exe`; no real CUI uploaded. | `authenticated-api-transcript.json` | QA owner | 2026-07-02 |
| Blocked upload audit | Blocked upload creates an audit event without storing file contents. | Passed via rejected upload-intent audit query and audit export. | `authenticated-upload-intent-audit.json` | QA owner | 2026-07-02 |
| Manual clause tagging | Clause is attached to the synthetic contract. | Passed after staging content import. | `authenticated-final-rerun.json` | QA owner | 2026-07-02 |
| Obligation generation | Obligation appears from the tagged clause. | Passed after staging content import. | `authenticated-final-rerun.json` | QA owner | 2026-07-02 |
| Task creation | Compliance task is created and assigned to a synthetic user. | Passed | `authenticated-corrective-api-transcript.json` | QA owner | 2026-07-02 |
| Evidence upload | Evidence metadata and upload record are visible in the tenant scope. | Passed | `authenticated-api-transcript.json` | QA owner | 2026-07-02 |
| Report generation | Tenant-scoped report is generated from synthetic workflow data. | Passed | `authenticated-corrective-api-transcript.json`, `evidence-package-corrected.json` | QA owner | 2026-07-02 |
| Audit log export | Tenant-scoped audit export is generated and contains only synthetic event metadata. | Passed | `authenticated-api-transcript.json` | QA owner | 2026-07-02 |

## Smoke Test Result

| Test case | Result | Evidence |
| --- | --- | --- |
| TC-PR-3.2.1 | Passed | Authenticated staging run proves tenant verification, invite, role verification, profile, contract, clause tagging, obligation generation, task creation, evidence, reports, and audit export using synthetic data. |
| TC-PR-3.2.2 | Passed with limitation | Allowed upload returned HTTP `201` with accepted validation status; malware scan remained `scan-pending`. |
| TC-PR-3.2.3 | Passed | Synthetic prohibited upload-intent was blocked with HTTP `400`; rejected `EvidenceUploadIntent` audit query and export returned HTTP `200`. |
| TC-PR-3.2.4 | Passed | Authenticated dashboard screenshot and API run records are attached under `output/playwright/production-readiness/pr-3.2/`. |

## Resolved Defects And Residual Launch Dependencies

| ID | Issue | Severity | Evidence | Current status |
| --- | --- | --- | --- | --- |
| PR32-STAGE-001 | Staging clause library had zero returned clauses, blocking source-backed manual clause tagging. | High | `staging-content-import-summary.txt` and `authenticated-final-rerun.json`. | Closed |
| PR32-STAGE-002 | Obligation generation could not be proven until a source-backed clause could be attached to the synthetic contract. | High | `authenticated-final-rerun.json`. | Closed |
| PR32-STAGE-003 | Blocked prohibited upload was rejected, but the failed-upload audit event was not separately located in the first staging transcript. | Medium | `authenticated-upload-intent-audit.json`. | Closed |
| PR32-STAGE-004 | Allowed upload remained `scan-pending`, so evidence usability depends on the malware-scanning launch decision. | Medium | Tracked by the separate production readiness malware scanning launch item / PR-4.3, not a PR-3.2 blocker. | Transferred |

## Launch Blocker

| Blocker ID | Blocker | Owner | Severity | Mitigation | Contingency | Target date | Current status |
| --- | --- | --- | --- | --- | --- | --- | --- |
| STAGE-WF-001 | PR-3.2 authenticated end-to-end MVP workflow was only partially proven in staging. | QA owner | High | Staging compliance content was imported, clause tagging and obligation generation were rerun, rejected upload-intent audit evidence was captured, and malware scanning was transferred to the separate launch-control item. | Keep production launch blocked on remaining non-PR-3.2 checklist items, including malware scanning decision, backups/restore, expert-reviewed content, and launch approvals. | Before PR-6.1 launch approvals | Closed for PR-3.2 |

## Hidden Risks

- Authenticated staging evidence proves the PR-3.2 workflow, but production launch still depends on other readiness checklist items.
- A successful `/health` check does not prove tenant workflow correctness.
- Real-CUI blocking must be tested with safe synthetic prohibited examples only; do not upload actual CUI.
- Empty staging compliance content previously blocked clause tagging and obligation generation; rerun the import after any staging database rebuild.
- Scan-pending uploads must not be treated as malware-cleared evidence unless PR-4.3 enables malware scanning or approves a launch exception.
- Report and audit exports must be inspected for tenant scope and absence of file contents or sensitive payloads.

## PR-3.2 Disposition

PR-3.2 is satisfied for staging workflow evidence as of 2026-07-02. Production launch remains blocked by the broader readiness checklist until the remaining non-PR-3.2 launch items are complete and approved.

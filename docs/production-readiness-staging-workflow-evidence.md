# Production Readiness Staging Workflow Evidence

Story: PR-3.2 - Execute End-To-End MVP Workflow In Staging.

Evidence status: Blocked - authenticated end-to-end staging workflow execution is not yet attached to the launch package.

Evidence date: 2026-07-01.

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
| Synthetic-only staging requirement documented | Passed | This artifact and `docs/production-readiness-staging-smoke-evidence.md` prohibit production customer data and real CUI. |
| Automated core workflow regression coverage exists | Passed | `tests/Gccs.Api.Tests/PilotWorkflowTests.cs` covers company profile, contract, clause tagging, obligation retrieval, task creation, evidence metadata/upload intent, reports, notifications, and role-constrained access using synthetic data. |
| Automated allowed upload coverage exists | Passed | `tests/Gccs.Api.Tests/PilotWorkflowTests.cs` and `tests/Gccs.Api.Tests/NoCuiAcknowledgementTests.cs` cover allowed non-sensitive upload intent creation after No-CUI acknowledgement and per-file attestation. |
| Automated blocked upload coverage exists | Passed | `tests/Gccs.Api.Tests/NoCuiAcknowledgementTests.cs` covers missing acknowledgement, missing per-file attestation, disallowed file type, oversize file, failed validation audit logging, and no usable file version on failed validation. |
| Automated report/export tenant-scope coverage exists | Passed | `tests/Gccs.Api.Tests/PilotWorkflowTests.cs`, report tests, and audit export tests cover tenant-scoped report and audit export behavior with synthetic data. |
| Authenticated staging UI/API user journey attached | Blocked | No complete PR-3.2 run record, screenshots, authenticated API transcript, or QA sign-off is attached yet. |
| Real-CUI or prohibited upload blocked in staging and audit logged | Blocked | Automated tests cover the behavior, but a staging execution record is not yet attached. |
| Report generation and audit log export verified in staging | Blocked | Automated tests cover the behavior, but a staging execution record is not yet attached. |

## Required Manual Staging Run Record

Complete this table with synthetic-only data before PR-3.2 can be closed.

| Step | Required result | Actual result | Evidence link or file | Tester | Date |
| --- | --- | --- | --- | --- | --- |
| Tenant creation or verification | Synthetic tenant exists in staging and uses No-CUI posture. | Pending | Pending | QA owner | Pending |
| User invite | Synthetic user invite or equivalent membership setup works. | Pending | Pending | QA owner | Pending |
| Role assignment | Owner, admin, compliance manager, contributor, auditor, and advisor roles are assigned or verified. | Pending | Pending | QA owner | Pending |
| Company profile | Company profile is completed with synthetic contractor data. | Pending | Pending | QA owner | Pending |
| Contract creation | Synthetic non-CUI contract is created. | Pending | Pending | QA owner | Pending |
| Allowed upload | Non-sensitive file upload succeeds only after No-CUI acknowledgement and per-file attestation. | Pending | Pending | QA owner | Pending |
| Blocked CUI/prohibited upload | Real CUI or prohibited upload attempt is blocked and produces no usable evidence. | Pending | Pending | QA owner | Pending |
| Blocked upload audit | Blocked upload creates an audit event without storing file contents. | Pending | Pending | QA owner | Pending |
| Manual clause tagging | Clause is attached to the synthetic contract. | Pending | Pending | QA owner | Pending |
| Obligation generation | Obligation appears from the tagged clause. | Pending | Pending | QA owner | Pending |
| Task creation | Compliance task is created and assigned to a synthetic user. | Pending | Pending | QA owner | Pending |
| Evidence upload | Evidence metadata and upload record are visible in the tenant scope. | Pending | Pending | QA owner | Pending |
| Report generation | Tenant-scoped report is generated from synthetic workflow data. | Pending | Pending | QA owner | Pending |
| Audit log export | Tenant-scoped audit export is generated and contains only synthetic event metadata. | Pending | Pending | QA owner | Pending |

## Smoke Test Result

| Test case | Result | Evidence |
| --- | --- | --- |
| TC-PR-3.2.1 | Blocked | Full authenticated staging workflow run record is not attached. |
| TC-PR-3.2.2 | Blocked | Allowed upload is automated but not yet proven through staging UI/API evidence. |
| TC-PR-3.2.3 | Blocked | Blocked upload and audit event are automated but not yet proven through staging UI/API evidence. |
| TC-PR-3.2.4 | Blocked | Screenshots, logs, test output, or run records for the authenticated staging workflow are not attached. |

## Launch Blocker

| Blocker ID | Blocker | Owner | Severity | Mitigation | Contingency | Target date | Current status |
| --- | --- | --- | --- | --- | --- | --- | --- |
| STAGE-WF-001 | PR-3.2 authenticated end-to-end MVP workflow evidence is not attached for staging. | QA owner | High | Execute the full PR-3.2 staging workflow with synthetic-only data, attach screenshots or authenticated API transcript, and record pass/fail for each required step. | Keep production launch blocked and do not close PR-3.2 until evidence proves allowed upload, blocked upload audit logging, report generation, and audit export in staging. | Before PR-6.1 launch approvals | Open |

## Hidden Risks

- Automated tests reduce regression risk but do not replace authenticated staging evidence.
- A successful `/health` check does not prove tenant workflow correctness.
- Real-CUI blocking must be tested with safe synthetic prohibited examples only; do not upload actual CUI.
- Report and audit exports must be inspected for tenant scope and absence of file contents or sensitive payloads.

## PR-3.2 Disposition

PR-3.2 is not satisfied for launch approval as of 2026-07-01. The launch package now has an explicit evidence artifact and blocker, but the story remains blocked until the authenticated staging workflow run is completed and attached.

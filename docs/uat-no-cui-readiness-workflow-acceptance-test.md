# GCCS UAT: No-CUI Readiness Workflow

Status basis: Implemented UI tabs and API behavior were checked against `apps/web/src/App.tsx`, `apps/api/Program.cs`, and focused API tests for contracts, clauses, obligations, evidence metadata, reports, audit logs, and RBAC.

Do not use real customer CUI, classified information, export-controlled technical data, credentials, payroll records, private keys, or production customer evidence in this UAT.

## Acceptance Categories

| Category | UAT Coverage | Current-State Label |
| --- | --- | --- |
| One configured No-CUI readiness workflow that shows contract metadata | UAT-01 through UAT-04 | Implemented |
| Attached or reviewed clauses | UAT-05 through UAT-06 | Implemented |
| Generated obligations | UAT-07 | Implemented |
| Owner/status tracking | UAT-08 through UAT-09 | Implemented |
| Allowed evidence metadata | UAT-10 through UAT-12 | Implemented |
| A current report artifact | UAT-13 | Implemented |
| Audit history | UAT-14 through UAT-15 | Implemented |

## Roles

| Role | UAT Actor | Use In This Test | Permission Expectation |
| --- | --- | --- | --- |
| Owner | Morgan Lane, `morgan.lane+uat@example.com` | Tenant setup, No-CUI mode, audit review | Can manage tenant settings and view audit log |
| Admin | Jordan Miles, `jordan.miles+uat@example.com` | User setup and operational fallback | Can manage users and workflow records, but not tenant ownership |
| Compliance Manager | Priya Shah, `priya.shah+uat@example.com` | Contract, clause, obligation, evidence, reports | Can manage core compliance workflow |
| Contributor | Devin Brooks, `devin.brooks+uat@example.com` | Evidence metadata support | Can manage evidence and tasks, cannot approve evidence or generate reports |
| Auditor | Elena Carter, `elena.carter+uat@example.com` | Read-only acceptance review | Can view records and reports, cannot modify workflow data or audit logs |
| Advisor | Avery Quinn, `avery.quinn+advisor@example.com` | External compliance advisor review | Can help manage compliance workflow and view audit log inside tenant boundary |

Note: In local development, the React app uses development authentication headers. Treat the names above as test personas unless separate login accounts are configured.

## Test Data

| Data Type | Field | Value |
| --- | --- | --- |
| Tenant | Active tenant | `GCCS Development Tenant` |
| Tenant mode | Data handling mode | `NoCui` |
| Contract | Contract number | `DEMO-NC-26-0007` |
| Contract | Title | `Non-CUI Help Desk Support BPA Call` |
| Contract | Agency or prime | `Fictional Prime Systems Inc. for DHS` |
| Contract | Role | `Subcontractor` |
| Contract | Contract type | `Fixed price` |
| Contract | Status | `Active` |
| Contract | Awarded | `2026-06-15` |
| Contract | Start | `2026-07-01` |
| Contract | End | `2027-06-30` |
| Contract | FCI/CUI posture | `FCI only` |
| Contract | Place of performance | `Virginia, remote support` |
| Contract | Description | `Synthetic No-CUI contract for UAT. FCI-only support workflow, no customer CUI, no classified data, no export-controlled technical data.` |
| Document | File name | `demo-nc-contract.txt` |
| Document | Content | `Synthetic No-CUI contract fixture for UAT. Includes FAR 52.204-21, FAR 52.204-25, and FAR 52.204-27 references. No customer CUI, classified information, export-controlled technical data, credentials, payroll, or secrets.` |
| Clause | Search terms | `52.204-21`, `52.204-25`, `52.204-27` |
| Evidence | Title | `MFA configuration summary - synthetic` |
| Evidence | Type | `System configuration` |
| Evidence | Owner | `Security` |
| Evidence | Status | `Approved` |
| Evidence | Effective | `2026-06-01` |
| Evidence | Expires | `2027-01-31` |
| Evidence | Tags | `FAR 52.204-21, FCI, MFA, UAT` |
| Evidence | Controls | `AC.L1-3.1.1` |
| Evidence | Classification | `FCI` |
| Evidence | Classification reason | `User confirmed synthetic FCI-only evidence for No-CUI UAT.` |
| Report | Package title | `Prime review evidence package - No-CUI UAT` |

## Pre-Publication Checklist

Before using this UAT as a sales, demo, or customer-facing asset, confirm:

| Check | Required Evidence |
| --- | --- |
| Does the UI expose this flow? | `Dashboard`, `Settings`, `Contracts`, `Obligations`, `Evidence`, `Reports` tabs are visible for the role under test |
| Does the API enforce this rule? | Endpoint returns expected success, `403`, `404`, or validation error |
| Is there a test proving it? | Relevant API test exists for the behavior |
| Does wording avoid overclaims? | No claim of certification, official compliance status, legal advice, government approval, guaranteed security, or audit-ready status |
| Does it preserve No-CUI posture? | Test data is synthetic and `NoCui` mode rejects CUI handling |

## UAT-01: Confirm No-CUI Mode

Category: One configured No-CUI readiness workflow that shows contract metadata.

Role: Owner.

Tab: `Settings`.

Steps:

1. Open the GCCS app in local development.
2. Click the `Settings` tab.
3. Find `Data handling mode`.
4. Confirm the displayed mode is `NoCui`.
5. If the form is editable, enter `NoCui` for `Mode`.
6. Enter `UAT reset to No-CUI compliance management mode.` for `Reason for mode change`.
7. Leave `Approval checklist ID` blank.
8. Click `Update mode`.
9. Confirm `Tenant data handling mode history` shows a row with `New` = `NoCui`.

Expected result: The tenant is configured for No-CUI compliance management only.

Reason: This establishes the safety boundary for every later step. If the tenant is not in `NoCui`, evidence, reporting, and contract-document expectations may not match the MVP posture.

## UAT-02: Verify Role Access Surface

Category: One configured No-CUI readiness workflow that shows contract metadata.

Role: Owner or Admin.

Tab: `Settings`.

Steps:

1. Stay on `Settings`.
2. Review the visible role guidance or current access details if shown.
3. Confirm the test actor can access `Contracts`, `Obligations`, `Evidence`, and `Reports`.
4. Confirm `Auditor` can view workflow records and reports but cannot create or update them.
5. Confirm `Contributor` can help with evidence and task work but cannot generate reports or approve evidence.

Expected result: UAT actors are assigned to roles that match the work they perform.

Reason: A UAT result is not meaningful if the actor has excessive privileges. This step separates operator behavior from reviewer behavior.

## UAT-03: Create The No-CUI Contract Record

Category: One configured No-CUI readiness workflow that shows contract metadata.

Role: Compliance Manager.

Tab: `Contracts`.

Steps:

1. Click the `Contracts` tab.
2. Click `New contract` if an existing contract is selected.
3. In `Create contract record`, enter the contract data from the `Test Data` section.
4. Confirm `FCI/CUI posture` is `FCI only`.
5. Click `Create contract`.
6. Select `DEMO-NC-26-0007` in `Contract records`.

Expected result: The selected contract displays its number, title, agency or prime, relationship, type, status, dates, posture, place of performance, and description.

Reason: Contract metadata is the anchor for clause attachment, obligation generation, evidence scope, reporting scope, and audit history.

## UAT-04: Upload Contract Document Metadata

Category: One configured No-CUI readiness workflow that shows contract metadata.

Role: Compliance Manager.

Tab: `Contracts`.

Steps:

1. Stay on `Contracts`.
2. Select `DEMO-NC-26-0007`.
3. In `Documents`, set `Document type` to `Contract`.
4. Set `Contract document classification` to `FCI`.
5. Choose a local text file named `demo-nc-contract.txt` containing the allowed synthetic text from `Test Data`.
6. Click `Upload metadata`.
7. Confirm the document appears in the document list with `FCI` classification.

Expected result: The app accepts synthetic FCI-only contract document metadata.

Reason: This proves the workflow can record contract-document context without accepting prohibited CUI content.

## UAT-05: Search Source-Backed Clauses

Category: Attached or reviewed clauses.

Role: Compliance Manager or Advisor.

Tab: `Obligations`.

Steps:

1. Click the `Obligations` tab.
2. Find `Clause library search`.
3. Search `52.204-21`.
4. Record the published clause ID for the matching FAR clause.
5. Repeat for `52.204-25`.
6. Repeat for `52.204-27`.
7. Confirm each result includes clause number, title, source URL, confidence or review state when displayed.

Expected result: Published clause records can be located before attachment.

Reason: Clause-driven obligations should come from reviewed source-backed content, not free-form user text.

## UAT-06: Attach Clauses To Contract

Category: Attached or reviewed clauses.

Role: Compliance Manager.

Tab: `Contracts`.

Steps:

1. Click the `Contracts` tab.
2. Select `DEMO-NC-26-0007`.
3. In `Attached clauses`, paste the published clause ID for `52.204-21`.
4. Enter `Manual UAT tagging from synthetic contract text.` for `Attachment reason`.
5. Enter `demo-nc-contract.txt` for `Source document reference`.
6. Click `Attach clause`.
7. Repeat steps 3 through 6 for `52.204-25` and `52.204-27`.
8. Confirm attached rows show clause number, title, source URL, and review metadata when available.

Expected result: Three clauses are attached to the contract.

Reason: Attachment creates the bridge from contract metadata to source-backed obligation generation.

## UAT-07: Generate And Review Contract Obligations

Category: Generated obligations.

Role: Compliance Manager.

Tab: `Contracts`, then `Obligations`.

Steps:

1. Stay on `Contracts`.
2. In the attached clause row for `52.204-21`, use the available generate action if visible.
3. If generation occurs automatically after attachment, proceed to the next step.
4. Click the `Obligations` tab.
5. In `Obligation work queue`, filter by contract `DEMO-NC-26-0007`.
6. Apply any useful filters: `Risk` = `High`, `Owner` = `Security` or `IT/security`, `Module` = `Cybersecurity`, `Source` = `52.204-21`.
7. Open the matching obligation with `View details`.
8. Confirm detail sections include `Why it applies`, `Required action`, `Owner`, `Source`, `Confidence`, `Last reviewed`, `Evidence examples`, and `Flow-down`.

Expected result: At least one source-backed obligation appears for the contract, preserving clause and source metadata.

Reason: The acceptance point is not merely that a clause is attached. The system should turn reviewed clause mappings into actionable work while preserving provenance.

## UAT-08: Update Obligation Status

Category: Owner/status tracking.

Role: Compliance Manager.

Tab: `Obligations`.

Steps:

1. Stay in the opened obligation detail.
2. In `Update status`, choose `In progress`.
3. Click `Save status`.
4. Confirm the status shown in detail or the work queue changes to `In progress`.

Expected result: The obligation status updates for the selected tenant-scoped contract obligation.

Reason: Status tracking is the operational control that turns static compliance content into an active readiness workflow.

## UAT-09: Assign Obligation Owner

Category: Owner/status tracking.

Role: Compliance Manager.

Tab: `Obligations`.

Steps:

1. Stay in the opened obligation detail.
2. In `Assign by`, choose `Role`.
3. In `Role`, choose `Compliance manager` or `Security`.
4. Check `Notify owner` if available.
5. Click `Assign owner`.
6. Return to the work queue and confirm the owner/role assignment appears.

Expected result: The owner assignment updates and remains visible with the obligation.

Reason: Owner assignment prevents generated obligations from becoming unowned backlog. It also supports reminders, calendar review, and management reporting.

## UAT-10: Acknowledge No-CUI Evidence Rules

Category: Allowed evidence metadata.

Role: Contributor or Compliance Manager.

Tab: `Evidence`.

Steps:

1. Click the `Evidence` tab.
2. Find `No-CUI acknowledgement`.
3. Read the notice and confirm it prohibits real customer CUI in No-CUI mode.
4. Click `I acknowledge the No-CUI upload limitation`.
5. Confirm the status changes to `Acknowledged`.

Expected result: Evidence controls become available only after acknowledgement.

Reason: The acknowledgement is an operational safety gate. It educates users before they create or upload evidence in a No-CUI tenant.

## UAT-11: Create Allowed Evidence Metadata

Category: Allowed evidence metadata.

Role: Contributor or Compliance Manager.

Tab: `Evidence`.

Steps:

1. Stay on `Evidence`.
2. Find `Evidence metadata`.
3. Click `New evidence`.
4. Enter the evidence data from the `Test Data` section.
5. For `Obligations`, paste the generated FAR 52.204-21 obligation ID if visible; otherwise leave blank and rely on the `Controls` link.
6. For `Controls`, enter `AC.L1-3.1.1`.
7. Click `Create metadata`.
8. Confirm the record appears in `Evidence list`.

Expected result: The evidence record appears with title, type, owner, status, dates, tags, classification, and control or obligation links.

Reason: Evidence metadata lets the system track proof without requiring storage of sensitive file content.

## UAT-12: Negative Evidence Classification Check

Category: Allowed evidence metadata.

Role: Compliance Manager.

Tab: `Evidence`.

Steps:

1. Stay on `Evidence`.
2. Select the synthetic evidence item or create a new test evidence record.
3. Attempt to classify it as `CUI` while the tenant remains in `NoCui` mode.
4. Enter `Negative UAT check: NoCui mode should not accept CUI evidence.` as the classification reason.
5. Submit the classification change.

Expected result: The workflow is rejected or blocked by the No-CUI policy.

Reason: A positive-only UAT misses the main safety guarantee. This negative test proves that No-CUI mode does not silently accept CUI-labeled evidence.

## UAT-13: Generate Current Report Artifact

Category: A current report artifact.

Role: Compliance Manager.

Tab: `Reports`.

Steps:

1. Click the `Reports` tab.
2. In `Compliance status`, click `Generate status`.
3. Confirm a report appears under `Generated this session`.
4. In `Evidence package builder`, enter `Prime review evidence package - No-CUI UAT` for `Package title`.
5. Select the generated obligation if available.
6. Select contract `DEMO-NC-26-0007`.
7. Select control `AC.L1-3.1.1` if available.
8. Leave `Include draft/rejected evidence when authorized` unchecked.
9. Click `Generate package`.
10. Confirm the package appears under `Generated this session` or `Approved evidence packages`.
11. Confirm the report wording says it is workflow guidance and does not claim legal advice, certification decision, assessor determination, contracting-officer determination, or government endorsement.

Expected result: A current report artifact is generated from tenant-scoped obligations, contract data, and approved evidence metadata.

Reason: Reports are the handoff artifact for internal reviews and prime-review preparation. The acceptance test also verifies that the report avoids certification and legal overclaims.

## UAT-14: Verify Audit History For Created Records

Category: Audit history.

Role: Owner, Admin, or Advisor.

Tab: `Settings`.

Steps:

1. Click the `Settings` tab.
2. Find `Audit log`.
3. Set `Action` to `Created`.
4. Set `Entity` to `Contract`.
5. Click `Filter`.
6. Confirm a created event exists for `DEMO-NC-26-0007` or the created contract ID.
7. Change `Entity` to `ContractClause`.
8. Click `Filter`.
9. Confirm created events exist for attached clauses.
10. Change `Entity` to `EvidenceItem`.
11. Click `Filter`.
12. Confirm a created event exists for `MFA configuration summary - synthetic`.
13. Change `Entity` to `Report`.
14. Click `Filter`.
15. Confirm a created event exists for the generated report.

Expected result: The audit log shows tenant-scoped history for the contract, clause, evidence, and report actions.

Reason: Audit history is the accountability trail. It should show who did what, when, and to which tenant-scoped entity.

## UAT-15: Verify Audit Access By Role

Category: Audit history.

Role: Auditor, Contributor, Owner.

Tab: `Settings`.

Steps:

1. As Owner, confirm the `Settings` tab and `Audit log` are visible.
2. As Contributor, attempt to access `Settings` or audit log review.
3. Confirm Contributor cannot view audit logs.
4. As Auditor, attempt to access audit logs.
5. Confirm Auditor cannot access audit logs unless the implementation grants `ViewAuditLog`.
6. As Advisor, confirm audit visibility is available if assigned the `Advisor` role.

Expected result: Audit log access follows the role permission matrix.

Reason: Audit logs can expose sensitive operational metadata. Access should be restricted to roles with explicit audit permission.

## Acceptance Exit Criteria

The UAT passes only if all of these conditions are true:

1. Tenant mode is `NoCui`.
2. Contract `DEMO-NC-26-0007` exists and displays the expected metadata.
3. Published clauses are found and attached with source references.
4. At least one obligation is generated or visible from the attached clause mapping.
5. Obligation status and owner assignment can be updated by an authorized role.
6. Allowed synthetic FCI evidence metadata can be created.
7. CUI classification or upload is blocked in No-CUI mode.
8. A current report or evidence package artifact is generated.
9. Report language avoids certification, legal, compliance, government-approval, and audit-readiness overclaims.
10. Audit history shows the tested create/update/report events.
11. Unauthorized roles cannot mutate restricted workflow records.

## Hidden Risks, Edge Cases, And Dependencies

| Item | Risk |
| --- | --- |
| Local development auth | Role switching may require API header changes or manual test setup; the default UI does not present a production sign-in flow. |
| Clause IDs | Clause search returns implementation-specific IDs; testers should copy the actual published IDs from the current environment. |
| Obligation generation timing | Some obligations may generate automatically on clause attachment; others may require an explicit generate action. |
| Evidence linking | Evidence can be accepted without an obligation link if the obligation ID is not visible; this reduces report package completeness. |
| Report scope | Evidence packages include only evidence matching selected scope and status rules. Draft/rejected evidence requires authorization. |
| Audit filters | Audit records may be easier to find by `Entity` than by exact actor when local development uses generated user IDs. |
| No-CUI enforcement | Do not use real CUI to test blocking. Use synthetic classification labels and allowed synthetic text only. |
| Customer-facing claims | This UAT proves workflow behavior only. It does not prove CMMC certification, legal compliance, government approval, or production CUI readiness. |

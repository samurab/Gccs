# GCCS No-CUI and CUI-Ready UAT Guide

This guide is written for a new GCCS user running User Acceptance Testing with synthetic data. Do not use real customer CUI, classified information, export-controlled technical data, credentials, payroll records, private keys, or production customer evidence.

## Main Tabs

Use the left navigation tabs in this order for the full UAT flow:

1. `Dashboard`
2. `Settings`
3. `Profile`
4. `Contracts`
5. `Obligations`
6. `Calendar`
7. `Evidence`
8. `CMMC`
9. `Subcontractors`
10. `Reports`
11. `Settings` again for audit review

## Test Users

The current local React app does not have a visible sign-in screen. In local development, the frontend automatically sends development authentication headers to the API. The API treats the local user as the configured development user, and the development bootstrapper creates the default active tenant when the API starts.

For UAT, use the names below as role/persona labels when entering assignments, invitations, evidence owners, and review notes. They are not separate login accounts unless a future identity flow or manual test setup creates them.

| User | Email | Suggested Role | Use For |
| --- | --- | --- | --- |
| Morgan Lane | morgan.lane+uat@example.com | Admin | Tenant setup, mode switching, acknowledgements |
| Priya Shah | priya.shah+uat@example.com | Compliance Manager | Profile, obligations, reports |
| Devin Brooks | devin.brooks+uat@example.com | Contributor | Evidence and CMMC readiness |
| Elena Carter | elena.carter+uat@example.com | Compliance Manager | Contracts and flow-downs |
| Avery Quinn | avery.quinn+platform@example.com | Admin / platform security tester | CUI-ready approval |

## Tenant Modes

| Mode | What It Means | UAT Rule |
| --- | --- | --- |
| `NoCui` | Compliance management only. Real CUI is blocked. | Use for normal MVP testing. |
| `CuiReady` | CUI workflows are allowed only after approval gates. | Use only after completing the CUI-ready checklist. |
| `DemoSandbox` | Demo/training mode for approved synthetic CUI examples. | Use only with approved synthetic demo seed data. |

The existing approved synthetic dataset is [dataset.json](/Users/devups/Development/CodexProjects/Gccs/packages/demo-content/synthetic-cui/dataset.json), version `2026.06.phase1a`.

## Local Development Access

There is currently no frontend form for `Sign in` or `Create tenant`.

Use this local setup instead:

1. Start local services and apply migrations.
2. Start the API in `Development`.
3. Restart the API after pulling this UAT guide update so the development tenant bootstrapper can create the default tenant if it is missing.
4. Start the web app.
5. Open the app and go directly to `Settings`.

The frontend sends `X-Gccs-Dev-Auth: true` automatically in development. By default, the API uses:

| Development Auth Setting | Default Value |
| --- | --- |
| Tenant ID | `11111111-1111-1111-1111-111111111111` |
| Active tenant name | `GCCS Development Tenant` |
| User ID | `22222222-2222-2222-2222-222222222222` |
| Email | `developer@gccs.local` |
| Role | `Owner` |

If the `Settings` tab is visible but `Data handling mode` says tenant context has not loaded, restart the API and refresh the web app. If it still appears, check that the database is running, migrations are applied, and the API can connect to PostgreSQL.

## Quick Rule For Switching Gates

Go to `Settings` tab -> `Data handling mode` form.

To switch to No-CUI:

| Field | Value |
| --- | --- |
| Mode | `NoCui` |
| Reason for mode change | `UAT reset to No-CUI compliance management mode.` |
| Approval checklist ID | Leave blank |

Click `Update mode`.

To switch to CUI-ready, you must first complete `Settings` tab -> `Approval checklist`, then copy the displayed `Approved checklist ID`.

| Field | Value |
| --- | --- |
| Mode | `CuiReady` |
| Reason for mode change | `UAT CUI-ready gate validation after approved checklist.` |
| Approval checklist ID | Paste the approved checklist ID |

Click `Update mode`.

Expected gate behavior: `CuiReady` fails if `Approval checklist ID` is blank, invalid, not approved, from another tenant, or older than one year.

## UAT-01: Start In Settings And Confirm No-CUI Mode

Goal: Confirm the tenant starts in safe No-CUI mode.

1. In local development, open the app as the automatically authenticated development user.
2. Click the `Settings` tab.
3. Find the `Data handling mode` section.
4. Confirm the status badge shows `NoCui`.
5. In the `Data handling mode` form, enter:

| Field | Value |
| --- | --- |
| Mode | `NoCui` |
| Reason for mode change | `Initial UAT confirmation of No-CUI mode.` |
| Approval checklist ID | Leave blank |

6. Click `Update mode`.
7. In the `Tenant data handling mode history` table, confirm a row appears with `New` = `NoCui`.

Expected result: The tenant is in `NoCui`, and the mode history is visible.

## UAT-02: Invite Synthetic Users

Goal: Confirm user onboarding works.

1. Stay on the `Settings` tab.
2. Find the `User invitations` section.
3. In the invitation form, create these invitations one at a time:

| Email Field | Role Field | Button |
| --- | --- | --- |
| priya.shah+uat@example.com | `Compliance Manager` | `Invite` |
| devin.brooks+uat@example.com | `Contributor` | `Invite` |
| elena.carter+uat@example.com | `Compliance Manager` | `Invite` |

Expected result: Each invitation appears in the invitation list with role, status, and expiration date.

## UAT-03: Complete The Company Profile

Goal: Create the company compliance profile.

1. Click the `Profile` tab.
2. In the `Create company profile` form, enter:

| Field Name | Value |
| --- | --- |
| Legal entity | `Aegis Systems Workshop LLC` |
| DBA | `Aegis Workshop` |
| UEI | `DEMOUEI12345` |
| CAGE | `9ZZZ9` |
| SAM expires | `2026-10-31` |
| Role | `Subcontractor` |
| Agency customers | `DoD, DHS` |
| Products and services | `Help desk support, compliance documentation support, and secure workflow consulting using synthetic UAT data only.` |
| Employees | `Small` |
| Revenue | `Small` |
| Location | `Headquarters` |
| Street | `100 Example Parkway` |
| City | `Arlington` |
| State | `VA` |
| Postal code | `22201` |
| Country | `US` |
| IT summary | `Microsoft 365 Business Premium, MFA, endpoint protection, MSP support. No CUI stored in this No-CUI tenant.` |
| FCI/CUI posture | `FCI only` |
| Key systems | `Microsoft 365, SharePoint, Intune, GCCS` |
| Uses external service provider | Checked |
| External service provider | `DemoSecure MSP LLC` |

3. In `NAICS codes`, enter two rows:

| Primary | Code | Title | Size basis | Status |
| --- | --- | --- | --- | --- |
| Selected | `541512` | `Computer Systems Design Services` | `Public SBA synthetic test basis` | `Small` |
| Not selected | `541519` | `Other Computer Related Services` | `Public SBA synthetic test basis` | `Small` |

4. In `Certifications`, enter two rows:

| Type | Certification status | Issuer | Effective | Expires | Reference |
| --- | --- | --- | --- | --- | --- |
| `WOSB` | `Active` | `SBA` | `2026-01-01` | `2027-01-01` | `WOSB-UAT-001` |
| `SDB` | `Active` | `SBA` | `2026-01-01` | `2027-01-01` | `SDB-UAT-001` |

5. Click `Complete profile`.

Expected result: The profile status changes from `Draft` toward `Complete`, and no CUI warning appears because posture is `FCI only`.

## UAT-04: Acknowledge No-CUI Upload Limits

Goal: Confirm uploads are blocked until the user acknowledges No-CUI rules.

1. Click the `Evidence` tab.
2. Find the `No-CUI acknowledgement` panel.
3. Confirm the notice says No-CUI mode prohibits real customer CUI.
4. Click `I acknowledge the No-CUI upload limitation`.
5. Confirm `Status` changes to `Acknowledged`.

Expected result: Evidence upload controls become available after acknowledgement.

## UAT-05: Create Evidence Metadata

Goal: Create reusable synthetic evidence records.

1. Stay on the `Evidence` tab.
2. Find the `Evidence metadata` form.
3. Click `New evidence`.
4. Enter the first evidence record:

| Field Name | Value |
| --- | --- |
| Title | `MFA configuration summary - synthetic` |
| Type | `System configuration` |
| Owner | `Security` |
| Status | `Approved` |
| Effective | `2026-06-01` |
| Expires | `2027-01-31` |
| Tags | `FAR 52.204-21, FCI, MFA, UAT` |
| Obligations | Leave blank unless an obligation ID is available |
| Controls | `AC.L1-3.1.1` |
| Classification | `FCI` |
| Classification reason | `User confirmed synthetic FCI-only evidence for No-CUI UAT.` |
| Description | `Synthetic summary proving MFA is configured for test users. Contains no screenshots, credentials, logs, or real system data.` |

5. Click `Create metadata`.
6. Repeat for:

| Title | Type | Owner | Status | Effective | Expires | Tags | Classification | Classification reason |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `Access control policy - synthetic` | `Policy` | `Security` | `In review` | `2026-06-01` | `2027-03-31` | `policy, access control, FAR 52.204-21` | `FCI` | `Synthetic No-CUI policy evidence.` |
| `Vendor telecom attestation - synthetic` | `Vendor attestation` | `Contracts` | `Requested` | `2026-06-01` | `2026-12-31` | `FAR 52.204-25, supplier` | `FCI` | `Synthetic supplier attestation for No-CUI UAT.` |

Expected result: Evidence records appear in the `Evidence list` with classification badges and expiration dates.

## UAT-06: Upload A Synthetic Evidence File

Goal: Confirm upload intent works only after acknowledgement.

1. In the `Evidence` tab, find `Upload area`.
2. In `Upload area`, find the `Evidence file` upload control.
3. Choose a small synthetic file, for example a TXT file named `uat-mfa-summary.txt`.
4. Set `Upload classification` = `Unclassified`.
5. Set `Upload classification reason` = `User confirmed this synthetic UAT upload contains no CUI or prohibited data.`
6. Confirm the file contains only this synthetic text:

```text
Synthetic UAT evidence. MFA is enabled for test users only. No real customer data, credentials, CUI, classified information, or export-controlled information.
```

7. Click `Upload evidence`.

Expected result: The app creates an upload intent or shows the accepted-upload placeholder. If upload is blocked, the error should clearly explain the gate.

## UAT-07: Create A No-CUI Contract

Goal: Create a contract record with FCI-only posture.

1. Click the `Contracts` tab.
2. Click `New contract` if an existing contract is selected.
3. In the `Create contract record` form, enter:

| Field Name | Value |
| --- | --- |
| Contract number | `DEMO-NC-26-0007` |
| Title | `Non-CUI Help Desk Support BPA Call` |
| Agency or prime | `Fictional Prime Systems Inc. for DHS` |
| Role | `Subcontractor` |
| Contract type | `Fixed price` |
| Status | `Active` |
| Awarded | `2026-06-15` |
| Start | `2026-07-01` |
| End | `2027-06-30` |
| FCI/CUI posture | `FCI only` |
| Place of performance | `Virginia, remote support` |
| Description | `Synthetic No-CUI contract for UAT. FCI-only support workflow, no customer CUI, no classified data, no export-controlled technical data.` |

4. Click `Create contract`.

Expected result: `DEMO-NC-26-0007` appears in the `Contract records` list.

## UAT-08: Add Contract Deliverables

Goal: Confirm contract dates appear as managed work.

1. Stay on the `Contracts` tab.
2. Select contract `DEMO-NC-26-0007`.
3. In the `Deliverables` form, enter:

| Field Name | Value |
| --- | --- |
| Name | `Monthly synthetic compliance status update` |
| Owner | `Contracts` |
| Due date | `2026-07-31` |
| Deliverable status | `Not started` |
| Deliverable description | `Monthly internal status summary using synthetic UAT records only.` |

4. Click `Add deliverable`.

Expected result: The deliverable appears in the deliverable list and should be eligible for calendar visibility.

## UAT-09: Upload Contract Document Metadata

Goal: Validate contract document classification behavior in No-CUI mode.

1. Stay on the `Contracts` tab.
2. In the `Documents` section, choose:

| Field / Control | Value |
| --- | --- |
| Document type dropdown | `Contract` |
| Contract document classification dropdown | `FCI` |
| Clause candidate review status dropdown | `All review states` |
| Contract document file | `demo-nc-contract.txt` synthetic file |

3. The file content may be:

```text
Synthetic No-CUI contract fixture for UAT.
Includes FAR 52.204-21, FAR 52.204-25, and FAR 52.204-27 references.
No customer CUI, classified information, export-controlled technical data, credentials, payroll, or secrets.
```

4. Click `Upload metadata`.

Expected result: The document appears in the contract document list with an `FCI` classification badge.

## UAT-10: Prove No-CUI Blocks CUI

Goal: Confirm the gate blocks real CUI workflows in `NoCui` mode.

1. Stay on `Contracts`.
2. In the same `Documents` section, set `Contract document classification` to `CUI`.
3. Select the same synthetic test file.
4. Click `Upload metadata`.

Expected result: Upload is rejected because `NoCui` tenants cannot create, upload, process, report on, export, or delete real CUI records.

Then confirm audit logging:

1. Click `Settings`.
2. Find `Audit log`.
3. In the filter form, enter:

| Field | Value |
| --- | --- |
| Action | `Rejected` |
| Entity | `TenantDataHandlingModePolicy` |

4. Click `Filter`.

Expected result: A rejected audit event appears for the blocked CUI attempt.

## UAT-11: Attach Clauses

Goal: Link source-backed clauses to the contract.

1. Click the `Obligations` tab.
2. Find `Clause library search`.
3. Search and record the published clause IDs for:

| Clause search | Category |
| --- | --- |
| `52.204-21` | `FAR` |
| `52.204-25` | `FAR` |
| `52.204-27` | `FAR` |

4. Return to the `Contracts` tab.
5. Select `DEMO-NC-26-0007`.
6. In `Attached clauses`, fill the form once for each published clause:

| Field Name | Example Value |
| --- | --- |
| Published clause ID | Paste the selected published clause ID |
| Attachment reason | `Manual UAT tagging from synthetic contract text.` |
| Source document reference | `demo-nc-contract.txt` |

7. Click `Attach clause` after each row.

Expected result: Attached clauses appear with clause number, title, source URL, and reviewed date.

## UAT-12: Review Obligation Work Queue

Goal: Confirm obligations can be filtered and reviewed.

1. Click the `Obligations` tab.
2. In `Obligation work queue`, use these filters:

| Filter Field | Value |
| --- | --- |
| Contract | `DEMO-NC-26-0007` |
| Risk | `High` |
| Owner | `Security` |
| Status | `All status` |
| Module | `Cybersecurity` |
| Due date | `Next 30 days` |
| Source | `52.204-21` |

3. Click `Apply filters`.
4. Open a matching obligation by clicking `View details`.
5. In `Obligation detail`, verify these sections are present:
   - `Why it applies`
   - `Required action`
   - `Owner`
   - `Source`
   - `Confidence`
   - `Last reviewed`
   - `Evidence examples`
   - `Flow-down`
6. In `Update status`, choose `In progress`.
7. Click `Save status`.
8. In `Assign by`, choose `Role`.
9. In `Role`, choose `Compliance manager`.
10. Check `Notify owner`.
11. Click `Assign owner`.

Expected result: The obligation status and owner assignment update, and the source-backed details remain visible.

## UAT-13: Review Calendar

Goal: Confirm tasks and due dates roll into the calendar.

1. Click the `Calendar` tab.
2. Filter by the contract or owner if available.
3. Confirm the calendar includes applicable items such as:
   - Contract deliverable due `2026-07-31`
   - SAM expiration `2026-10-31`
   - Evidence expiration dates
   - Obligation due dates

Expected result: Calendar data is tenant scoped and reflects profile, contract, evidence, and obligation dates.

## UAT-14: Create CMMC Readiness Workspace

Goal: Confirm CMMC readiness setup for No-CUI / FCI-only work.

1. Click the `CMMC` tab.
2. In `CMMC and NIST workspace`, enter:

| Field Name | Value |
| --- | --- |
| Assessment name | `No-CUI Level 1 readiness workspace` |
| Target level | `Level 1` |
| Framework | `FAR basic safeguarding` |
| Status | `In progress` |
| Started | `2026-06-15` |
| Affirmation due | `2027-06-15` |
| Owner | `Security` |
| Contract link | `DEMO-NC-26-0007` |

3. Click `Create assessment`.
4. In `POA&M remediation`, enter:

| Field Name | Value |
| --- | --- |
| Control | Select the first available control, if present |
| Risk | `High` |
| Status | `Open` |
| Owner | `Security` |
| Due date | `2026-07-15` |
| Gap | `Synthetic UAT gap: document annual access review evidence.` |
| Remediation plan | `Upload synthetic access review summary and link it to the control.` |

5. Click `Create POA&M`.

Expected result: The assessment appears in `Readiness assessments`; the POA&M appears in `POA&M remediation`.

## UAT-15: Create Subcontractor And Flow-Down

Goal: Confirm supplier tracking works.

1. Click the `Subcontractors` tab.
2. In the subcontractor form, enter:

| Field Name | Value |
| --- | --- |
| Name | `Northstar Demo Components LLC` |
| Contact name | `Rowan Ellis` |
| Contact email | `rowan.ellis+uat@example.com` |
| Small business | `Small, SDB` |
| CMMC status | `Level 1 self-assessment draft` |
| Required CMMC level from contract | `Level 1` |
| Insurance expires | `2026-12-15` |
| NDA status | `Signed` |
| Workshare % | `18` |
| Contract link | `DEMO-NC-26-0007` |
| CUI access allowed | Unchecked for No-CUI UAT |
| Export-control exposure | Unchecked |
| Role | `Component documentation support using synthetic No-CUI records only.` |

3. Click `Create subcontractor`.
4. Select `Northstar Demo Components LLC` from the `Subcontractor list`.
5. In `Assign flow-down`, enter:

| Field Name | Value |
| --- | --- |
| Contract | `DEMO-NC-26-0007` |
| Obligation | Select the FAR 52.204-21 obligation if present; otherwise `Manual entry` |
| Clause number | `FAR 52.204-21` |
| Status | `Sent` |
| Title | `Basic safeguarding flow-down acknowledgement` |
| Signed evidence | `No evidence linked`, or select the synthetic policy evidence |

6. Click `Save flow-down`.
7. In `Request evidence`, enter:

| Field Name | Value |
| --- | --- |
| Requested item | `Signed FAR 52.204-21 flow-down acknowledgement` |
| Evidence type | `SignedFlowDown` |
| Due date | `2026-08-05` |
| Recipient | `Rowan Ellis` |
| Email | `rowan.ellis+uat@example.com` |
| Related flow-down | Select the saved flow-down if available |
| Status | `Sent` |

8. Click `Create request`.

Expected result: The flow-down appears in `Flow-down register`, and the evidence request appears in `Evidence requests`.

## UAT-16: Generate Reports

Goal: Confirm reports are tenant-scoped and source-backed.

1. Click the `Reports` tab.
2. In `Compliance status`, click `Generate status`.
3. In `CMMC readiness`, choose the assessment `No-CUI Level 1 readiness workspace`.
4. Click `Generate readiness`.
5. In `Subcontractor compliance`, set:

| Field | Value |
| --- | --- |
| Contract filter | `DEMO-NC-26-0007` |

6. Click `Generate supplier report`.
7. In `Evidence package builder`, enter:

| Field Name | Value |
| --- | --- |
| Package title | `Prime review evidence package - No-CUI UAT` |
| Obligation | Select a FAR 52.204-21 obligation if available |
| Contract | `DEMO-NC-26-0007` |
| Control ID | `AC.L1-3.1.1` |
| Subcontractor | `Northstar Demo Components LLC` |
| Include draft/rejected evidence when authorized | Leave unchecked |

8. Click `Generate package`.

Expected result: Reports appear under `Generated this session`. They must not claim legal approval, certification, official pass/fail status, or government endorsement.

## UAT-17: Prepare CUI-Ready Approval

Goal: Complete the formal gate before switching to `CuiReady`.

1. Click the `Settings` tab.
2. Find `Shared responsibility matrix`.
3. Click `Acknowledge`.
4. In the summary cards under `Shared responsibility matrix`, confirm `Matrix acknowledgement status` shows `Current`. You may also see the message `Matrix acknowledgement status is Current.`
5. Find `Approval checklist`.
6. Click `New checklist`.
7. For every checklist row, click `Mark complete`.
8. Confirm each row shows:
   - `Status`: `Complete`
   - `Owner`: `Security`, or the owner already shown for that checklist item
   - `Review date`: today's date
9. In `Review reason`, enter:

```text
Approved for CUI-ready UAT using synthetic data and approved gate controls.
```

10. Click `Submit`.
11. Click `Approve`.
12. Copy the displayed `Approved checklist ID`.

Expected result: The checklist state becomes `Approved`, and an approved checklist ID is shown.

## UAT-18: Switch To CUI-Ready

Goal: Enable the CUI-ready gate using the approved checklist.

1. Stay on the `Settings` tab.
2. Find `Data handling mode`.
3. Enter:

| Field Name | Value |
| --- | --- |
| Mode | `CuiReady` |
| Reason for mode change | `UAT CUI-ready gate validation after approved checklist.` |
| Approval checklist ID | Paste the approved checklist ID from UAT-17 |

4. Click `Update mode`.
5. Confirm the status badge changes to `CuiReady`.
6. Confirm the history table shows `Previous` = `NoCui` and `New` = `CuiReady`.

Expected result: The tenant enters CUI-ready mode only after checklist approval.

## UAT-19: Create CUI-Ready Contract

Goal: Confirm CUI-ready mode allows CUI posture where No-CUI did not.

1. Click the `Contracts` tab.
2. Click `New contract`.
3. Enter:

| Field Name | Value |
| --- | --- |
| Contract number | `DEMO-CUI-26-0001` |
| Title | `Synthetic CUI Handling Training Support Order` |
| Agency or prime | `Fictional Defense Prime LLC for DoD` |
| Role | `Subcontractor` |
| Contract type | `Fixed price` |
| Status | `Active` |
| Awarded | `2026-07-15` |
| Start | `2026-08-01` |
| End | `2027-07-31` |
| FCI/CUI posture | `CUI` |
| Place of performance | `Maryland secure workspace` |
| Description | `Synthetic CUI-ready UAT contract. Uses synthetic CUI-like workflow records only; no real CUI, classified data, or export-controlled technical data.` |

4. Click `Create contract`.

Expected result: The CUI posture is selectable and the contract is created.

## UAT-20: CUI-Ready Workflow And Negative Checks

Goal: Confirm CUI-ready still requires classification and approval checks.

Positive path:

1. Click `Evidence`.
2. In `Evidence metadata`, create:

| Field Name | Value |
| --- | --- |
| Title | `Synthetic system boundary narrative` |
| Type | `Risk assessment` |
| Owner | `Security` (type it if the suggestion list is not open) |
| Status | `In review` |
| Effective | `2026-08-01` |
| Expires | `2027-02-28` |
| Tags | `CUI-ready, synthetic, boundary` |
| Obligations | Leave blank, or select an applicable obligation if one is available |
| Controls | `AC.L2-3.1.3` |
| Classification | `CUI` |
| Classification reason | `CUI-ready UAT classification confirmed; synthetic-safe record contains no real customer CUI.` |
| Description | `Synthetic boundary narrative for UAT only.` |

3. Click `Create metadata`.

Negative checks:

1. Create another evidence metadata record with `Classification` = `Unknown`.
2. Confirm it appears in `Classification review queue`.
3. Make sure the unknown evidence record has:

| Field Name | Value |
| --- | --- |
| Status | `Approved` |
| Controls | `AC.L2-3.1.3` |
| Classification | `Unknown` |

4. Click the `Reports` tab.
5. In `Evidence package builder`, enter:

| Field Name | Value |
| --- | --- |
| Package title | `Unknown classification package test` |
| Obligation | `No obligation scope` |
| Contract | `No contract scope` |
| Control ID | `AC.L2-3.1.3` |
| Subcontractor | `No subcontractor scope` |
| Include draft/rejected evidence when authorized | Leave unchecked |

6. Click `Generate package`.
7. Expected: package generation is blocked until the evidence classification is reviewed. If the package generates successfully but does not include the unknown evidence, confirm the evidence is `Approved` and linked to `AC.L2-3.1.3`, then retry.
8. Return to the `Evidence` tab.
9. Select the unknown evidence record from `Evidence list`.
10. Change `Classification` to `Prohibited`.
11. Enter `Classification reason` = `Synthetic prohibited-content gate test.`
12. Click `Review classification`.
13. Expected: item remains blocked and should route to escalation/review behavior.

## UAT-21: DemoSandbox Synthetic CUI Check

Goal: Confirm synthetic CUI seed data is isolated to demo tenants.

1. Click the `Settings` tab.
2. Find `Data handling mode` at the top of the page.
3. Confirm `Active tenant`, `Tenant ID`, and `Current mode` are visible.
4. Confirm `Current mode` = `DemoSandbox`. If it does not, set `Mode` = `DemoSandbox`, enter `Reason for mode change` = `UAT validation of approved synthetic demo dataset.`, leave `Approval checklist ID` blank, and click `Update mode`.
5. The current UI does not provide a tenant switcher. Do not look for a sign-in, tenant picker, or tenant creation form during this UAT.
6. Find `Demo sandbox seed`.
7. Confirm the panel shows:
   - `Required mode`: `DemoSandbox`
   - `Dataset version`: `2026.06.phase1a`
   - `Classification`: `SyntheticCui`
8. Click `Seed synthetic data`.
9. Confirm the success message says the synthetic demo dataset was seeded and references dataset version `2026.06.phase1a`.
10. Confirm seeded records display:
   - `Synthetic demo data`
   - `SyntheticCui`
11. Click the `Evidence` tab.
12. Find `Upload area`.
13. Select a harmless synthetic file in `Evidence file`.
14. Set `Upload classification` = `CUI`.
15. Set `Upload classification reason` = `DemoSandbox negative test: user attempted customer CUI upload classification.`
16. Click `Upload evidence`.

Expected result: Approved synthetic seed content is usable in `DemoSandbox`; real CUI upload is blocked.

## UAT-22: Final Audit And Isolation Review

Goal: Confirm traceability and tenant isolation.

1. Click `Settings`.
2. Scroll to the `Audit log` section near the bottom of the page.
3. Use the visible filter fields `Action`, `Entity`, `From`, and `To`.
4. For this UAT, leave `From` and `To` blank unless you intentionally want to narrow the date range.
5. `Expected Summary` is not a form field. It is the text to confirm in the `Summary` column of the results table after filtering.
6. In `Audit log`, run these filters one at a time:

| Test ID | Action filter | Entity filter | From | To | Expected row values | Prerequisite if no row appears |
| --- | --- | --- | --- | --- | --- | --- |
| `UAT-22-A` | `Created` | `SharedResponsibilityMatrixAcknowledgement` | Leave blank | Leave blank | `Action` = `Created`; `Entity` = `SharedResponsibilityMatrixAcknowledgement`; `Summary` contains `Shared responsibility matrix acknowledged.` | Go to `Shared responsibility matrix`, click `Acknowledge`, then rerun the filter. |
| `UAT-22-B` | `Created` | `CuiReadyApprovalChecklist` | Leave blank | Leave blank | `Action` = `Created`; `Entity` = `CuiReadyApprovalChecklist`; `Summary` contains `CUI-ready approval checklist was created.` | Go to `Approval checklist`, click `New checklist`, then rerun the filter. |
| `UAT-22-C` | `Approved` | `CuiReadyApprovalChecklist` | Leave blank | Leave blank | `Action` = `Approved`; `Entity` = `CuiReadyApprovalChecklist`; `Summary` contains `CUI-ready approval checklist was approved.` | Complete every checklist row, enter `Review reason`, click `Submit`, click `Approve`, then rerun the filter. |
| `UAT-22-D` | `Updated` | `Tenant` | Leave blank | Leave blank | `Action` = `Updated`; `Entity` = `Tenant`; `Summary` contains `data handling mode changed to DemoSandbox`, `NoCui`, or `CuiReady`. | In `Data handling mode`, update `Mode` with a reason, then rerun the filter. |
| `UAT-22-E` | `Rejected` | `TenantDataHandlingModePolicy` | Leave blank | Leave blank | `Action` = `Rejected`; `Entity` = `TenantDataHandlingModePolicy`; `Summary` contains `Tenant data handling mode blocked a restricted workflow.` | In `DemoSandbox`, go to `Evidence` -> `Upload area`, set `Upload classification` = `CUI`, and click `Upload evidence`; then rerun the filter. |
| `UAT-22-F` | `Uploaded` | `EvidenceFileVersion` | Leave blank | Leave blank | `Action` = `Uploaded`; `Entity` = `EvidenceFileVersion`; `Summary` contains `Evidence file upload metadata was accepted and versioned.` | In an allowed mode/classification path, upload a harmless synthetic file from `Evidence` -> `Upload area`, then rerun the filter. |
| `UAT-22-G` | `Created` | `SyntheticDemoSeed` | Leave blank | Leave blank | `Action` = `Created`; `Entity` = `SyntheticDemoSeed`; `Summary` contains `Synthetic demo dataset seed completed.` | In `Settings` -> `Demo sandbox seed`, click `Seed synthetic data`, then rerun the filter. |
| `UAT-22-H` | `Created` | `Report` | Leave blank | Leave blank | `Action` = `Created`; `Entity` = `Report`; `Summary` contains `report was generated` or `Evidence package was generated.` | Go to `Reports`, generate a compliance, CMMC, supplier, or evidence package report, then rerun the filter. |

7. Click `Filter` after each filter combination.
8. Confirm audit rows show `Date`, `Actor`, `Action`, `Entity`, and `Summary`.
9. Confirm the `Actor` value is either the local development user ID or `System`.
10. Confirm the `Summary` text matches the action you performed in the same tenant.
11. If you have access to a second tenant, repeat report/dashboard checks there.
12. Confirm no records from `GCCS Development Tenant` appear in the second tenant.

Expected result: Audit history is traceable, and tenant data does not leak across tenants.

## UAT Exit Criteria

UAT passes when:

- A new user can follow the tabs and forms without engineering help.
- `NoCui` blocks all real CUI workflows.
- `CuiReady` cannot be enabled without an approved checklist and valid `Approval checklist ID`.
- `CuiReady` still enforces classification and workflow approval checks.
- `DemoSandbox` permits only approved synthetic demo CUI content.
- Reports are tenant-scoped and avoid legal, certification, official pass/fail, or government endorsement claims.
- Audit logs capture acknowledgements, checklist lifecycle, mode changes, report generation, and rejected gate attempts.

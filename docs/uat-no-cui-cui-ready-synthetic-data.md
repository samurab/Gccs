# GCCS No-CUI and CUI-Ready UAT Guide

This guide is written for a new GCCS user running User Acceptance Testing with synthetic data. Do not use real customer CUI, classified information, export-controlled technical data, credentials, payroll records, private keys, or production customer evidence.

Status basis: The UI and API surfaces were reviewed on 2026-09-18. New workflows are labeled `Implemented`, `Partially implemented`, `Blocked by environment`, or `Do not claim` according to the available UI, authorization rules, and test evidence. This guide does not claim certification, legal approval, government approval, or authorization to handle real CUI.

## How To Execute Every Case

Each case uses the same beginner-safe order. Do not infer a missing control or replace a required UI step with an API call.

1. Read `Current-state label`, `Actor and role`, `Tenant context`, and `Preconditions` before editing anything.
2. Select the active tenant shown by the application and record its visible name and ID. Copy IDs returned by the current tenant; never invent or reuse IDs from another tenant.
3. Open the exact `Tab or page` and locate the named form, control, field, filter, or button in `Form and control/value anchors`.
4. Enter only the literal synthetic values shown in the case. Do not use real CUI, classified information, export-controlled technical data, credentials, secrets, or customer files.
5. Complete the numbered steps in order. Capture the UI result and any returned record ID before starting a retry, negative, stale, cross-tenant, or provider-failure step.
6. Compare the result with `Expected result` and record `Pass`, `Fail`, `Blocked by environment`, `Not applicable`, or `Not run`. `Implemented` is not a test result.
7. If a named tab, form, field, filter, button, result, or API dependency is absent, stop and record the exact visible state. Mark `Failed` for a discoverability defect, or `Blocked by environment` only when the case identifies the missing dependency.

The `Form and control/value anchors` line includes both control labels and literal synthetic values. It is a locator aid; the numbered steps are authoritative.

## Table Of Contents

Use these links to jump to a test area or individual case.

- [How To Execute Every Case](#how-to-execute-every-case)
- [Main Tabs](#main-tabs)
- [Test Users](#test-users)
- [Tenant Modes](#tenant-modes)
- [Local Development Access](#local-development-access)
- [Quick Rule For Switching Gates](#quick-rule-for-switching-gates)
- [UAT-01: Start In Settings And Confirm No-CUI Mode](#uat-01-start-in-settings-and-confirm-no-cui-mode)
- [UAT-02: Invite Synthetic Users](#uat-02-invite-synthetic-users)
- [UAT-03: Complete The Company Profile](#uat-03-complete-the-company-profile)
- [UAT-04: Acknowledge No-CUI Upload Limits](#uat-04-acknowledge-no-cui-upload-limits)
- [UAT-05: Create Evidence Metadata](#uat-05-create-evidence-metadata)
- [UAT-06: Upload A Synthetic Evidence File](#uat-06-upload-a-synthetic-evidence-file)
- [UAT-07: Create A No-CUI Contract](#uat-07-create-a-no-cui-contract)
- [UAT-08: Add Contract Deliverables](#uat-08-add-contract-deliverables)
- [UAT-09: Upload Contract Document Metadata](#uat-09-upload-contract-document-metadata)
- [UAT-10: Prove No-CUI Blocks CUI](#uat-10-prove-no-cui-blocks-cui)
- [UAT-11: Attach Clauses](#uat-11-attach-clauses)
- [UAT-12: Review Obligation Work Queue](#uat-12-review-obligation-work-queue)
- [UAT-13: Review Calendar](#uat-13-review-calendar)
- [UAT-14: Create CMMC Readiness Workspace](#uat-14-create-cmmc-readiness-workspace)
- [UAT-15: Create Subcontractor And Flow-Down](#uat-15-create-subcontractor-and-flow-down)
- [UAT-16: Generate Reports](#uat-16-generate-reports)
- [UAT-17: Prepare CUI-Ready Approval](#uat-17-prepare-cui-ready-approval)
- [UAT-18: Switch To CUI-Ready](#uat-18-switch-to-cui-ready)
- [UAT-19: Create CUI-Ready Contract](#uat-19-create-cui-ready-contract)
- [UAT-20: CUI-Ready Workflow And Negative Checks](#uat-20-cui-ready-workflow-and-negative-checks)
- [UAT-21: DemoSandbox Synthetic CUI Check](#uat-21-demosandbox-synthetic-cui-check)
- [UAT-22: Final Audit And Isolation Review](#uat-22-final-audit-and-isolation-review)
- [UAT-23: Security Review](#uat-23-security-review)
- [UAT-24: Technical Control Verification](#uat-24-technical-control-verification)
- [UAT-25: Incident Readiness](#uat-25-incident-readiness)
- [UAT-26: Invite An External Reviewer](#uat-26-invite-an-external-reviewer)
- [UAT-27: Invitation Access And Revocation](#uat-27-invitation-access-and-revocation)
- [UAT-28: System Security Plan Sections](#uat-28-system-security-plan-sections)
- [UAT-29: SSP Narrative Review](#uat-29-ssp-narrative-review)
- [UAT-30: SSP Review Packages](#uat-30-ssp-review-packages)
- [UAT Exit Criteria](#uat-exit-criteria)

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

Newer workflows are located inside existing tabs:

- `Settings` -> `Security and incident readiness`
- `Settings` -> `External portal invitations`
- `CMMC` -> `System Security Plan sections`
- `CMMC` -> `SSP narrative builder`
- `CMMC` -> `SSP review packages`

## Test Users

The current local React app does not have a visible sign-in screen. In local development, the frontend automatically sends development authentication headers to the API. The API treats the local user as the configured development user, and the development bootstrapper creates the default active tenant when the API starts.

For UAT, use the names below as role/persona labels when entering assignments, invitations, evidence owners, and review notes. They are not separate login accounts unless a future identity flow or manual test setup creates them.

| User | Email | Suggested Role | Use For |
| --- | --- | --- | --- |
| Morgan Lane | morgan.lane+uat@example.com | Admin | Tenant setup, mode switching, acknowledgements |
| Priya Shah | priya.shah+uat@example.com | Compliance Manager | Profile, obligations, reports |
| Devin Brooks | devin.brooks+uat@example.com | Contributor | Evidence and CMMC readiness |
| Elena Carter | elena.carter+uat@example.com | Compliance Manager | Contracts and flow-downs |
| Avery Quinn | avery.quinn+platform@example.com | Admin / platform security tester | future `CuiReady` approval |

## Tenant Modes

| Mode | What It Means | UAT Rule |
| --- | --- | --- |
| `NoCui` | Compliance management only. Real CUI is blocked. | Use for normal MVP testing. |
| `CuiReady` | CUI handling workflows are allowed only after approval gates. | Use only after completing the CUI-ready checklist. |
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


Execution map:
Current-state label: Implemented for the synthetic workflow documented here when the named UI/API surface is available; provider-dependent or API-only steps must be recorded as `Blocked by environment`. This test does not authorize real CUI, classified data, export-controlled data, or customer evidence.

Actor and role: Use the role named in each step. Use an Owner or authorized Compliance Manager for write/approval actions; use Auditor only for read-only checks and negative authorization checks.

Tenant context: Use the active synthetic tenant displayed by the application. Record the visible tenant name and tenant ID before starting. Copy returned record IDs; never invent IDs or use an ID from another tenant.

Tab or page: `Settings` -> `Data handling mode`

Form and control/value anchors: `Settings`; `Data handling mode`; `NoCui`; `Initial UAT confirmation of No-CUI mode.`; `Update mode`; `Tenant data handling mode history`; `New`

Synthetic data: Use only the exact synthetic values in this case and the shared synthetic-data tables. Do not enter real CUI, classified information, export-controlled technical data, credentials, secrets, or customer files.

Preconditions: Confirm the active tenant, role, mode, and any prerequisite record IDs before changing a record. If a prerequisite record is unavailable, stop and record `Blocked by environment` rather than substituting another tenant or invented ID.

Missing-control rule: If a named tab, form, field, filter, button, result, or API dependency is absent, stop at that step and record the exact visible state. Mark `Failed` for a discoverability defect, or `Blocked by environment` only when the case explicitly identifies the missing dependency.

Execution order: Complete the numbered steps in order. Capture the result before executing a retry, negative, stale, cross-tenant, or provider-failure step.

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


Evidence to capture: Capture the active tenant name and ID, role, exact tab/form/control labels, entered synthetic values, returned record IDs, visible result, sanitized API status and trace ID when an authorized API step is named, and any denial or no-side-effect proof.

Execution record: Status: Pass / Fail / Blocked by environment / Not applicable / Not run | Defect: ______ | Tester: ______ | Date/time: ______ | Tenant: ______ | Evidence location: ______ | Notes: ______

Expected result: The tenant is in `NoCui`, and the mode history is visible.

## UAT-02: Invite Synthetic Users

Goal: Confirm user onboarding works.


Execution map:
Current-state label: Implemented for the synthetic workflow documented here when the named UI/API surface is available; provider-dependent or API-only steps must be recorded as `Blocked by environment`. This test does not authorize real CUI, classified data, export-controlled data, or customer evidence.

Actor and role: Use the role named in each step. Use an Owner or authorized Compliance Manager for write/approval actions; use Auditor only for read-only checks and negative authorization checks.

Tenant context: Use the active synthetic tenant displayed by the application. Record the visible tenant name and tenant ID before starting. Copy returned record IDs; never invent IDs or use an ID from another tenant.

Tab or page: `Settings` -> `User invitations`

Form and control/value anchors: `Settings`; `User invitations`; `Compliance Manager`; `Invite`; `Contributor`

Synthetic data: Use only the exact synthetic values in this case and the shared synthetic-data tables. Do not enter real CUI, classified information, export-controlled technical data, credentials, secrets, or customer files.

Preconditions: Confirm the active tenant, role, mode, and any prerequisite record IDs before changing a record. If a prerequisite record is unavailable, stop and record `Blocked by environment` rather than substituting another tenant or invented ID.

Missing-control rule: If a named tab, form, field, filter, button, result, or API dependency is absent, stop at that step and record the exact visible state. Mark `Failed` for a discoverability defect, or `Blocked by environment` only when the case explicitly identifies the missing dependency.

Execution order: Complete the numbered steps in order. Capture the result before executing a retry, negative, stale, cross-tenant, or provider-failure step.

1. Stay on the `Settings` tab.
2. Find the `User invitations` section.
3. In the invitation form, create these invitations one at a time:

| Email Field | Role Field | Button |
| --- | --- | --- |
| priya.shah+uat@example.com | `Compliance Manager` | `Invite` |
| devin.brooks+uat@example.com | `Contributor` | `Invite` |
| elena.carter+uat@example.com | `Compliance Manager` | `Invite` |


Evidence to capture: Capture the active tenant name and ID, role, exact tab/form/control labels, entered synthetic values, returned record IDs, visible result, sanitized API status and trace ID when an authorized API step is named, and any denial or no-side-effect proof.

Execution record: Status: Pass / Fail / Blocked by environment / Not applicable / Not run | Defect: ______ | Tester: ______ | Date/time: ______ | Tenant: ______ | Evidence location: ______ | Notes: ______

Expected result: Each invitation appears in the invitation list with role, status, and expiration date.

## UAT-03: Complete The Company Profile

Goal: Create the company compliance profile.


Execution map:
Current-state label: Implemented for the synthetic workflow documented here when the named UI/API surface is available; provider-dependent or API-only steps must be recorded as `Blocked by environment`. This test does not authorize real CUI, classified data, export-controlled data, or customer evidence.

Actor and role: Use the role named in each step. Use an Owner or authorized Compliance Manager for write/approval actions; use Auditor only for read-only checks and negative authorization checks.

Tenant context: Use the active synthetic tenant displayed by the application. Record the visible tenant name and tenant ID before starting. Copy returned record IDs; never invent IDs or use an ID from another tenant.

Tab or page: `Profile` -> `Company profile`

Form and control/value anchors: `Profile`; `Create company profile`; `Aegis Systems Workshop LLC`; `Aegis Workshop`; `DEMOUEI12345`; `9ZZZ9`; `2026-10-31`; `Subcontractor`; `DoD, DHS`; `Small`; `Headquarters`; `100 Example Parkway`; `Arlington`; `VA`; `22201`; `US`; `FCI only`; `Microsoft 365, SharePoint, Intune, GCCS`

Synthetic data: Use only the exact synthetic values in this case and the shared synthetic-data tables. Do not enter real CUI, classified information, export-controlled technical data, credentials, secrets, or customer files.

Preconditions: Confirm the active tenant, role, mode, and any prerequisite record IDs before changing a record. If a prerequisite record is unavailable, stop and record `Blocked by environment` rather than substituting another tenant or invented ID.

Missing-control rule: If a named tab, form, field, filter, button, result, or API dependency is absent, stop at that step and record the exact visible state. Mark `Failed` for a discoverability defect, or `Blocked by environment` only when the case explicitly identifies the missing dependency.

Execution order: Complete the numbered steps in order. Capture the result before executing a retry, negative, stale, cross-tenant, or provider-failure step.

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


Evidence to capture: Capture the active tenant name and ID, role, exact tab/form/control labels, entered synthetic values, returned record IDs, visible result, sanitized API status and trace ID when an authorized API step is named, and any denial or no-side-effect proof.

Execution record: Status: Pass / Fail / Blocked by environment / Not applicable / Not run | Defect: ______ | Tester: ______ | Date/time: ______ | Tenant: ______ | Evidence location: ______ | Notes: ______

Expected result: The profile status changes from `Draft` toward `Complete`, and no CUI warning appears because posture is `FCI only`.

## UAT-04: Acknowledge No-CUI Upload Limits

Goal: Confirm uploads are blocked until the user acknowledges No-CUI rules.


Execution map:
Current-state label: Implemented for the synthetic workflow documented here when the named UI/API surface is available; provider-dependent or API-only steps must be recorded as `Blocked by environment`. This test does not authorize real CUI, classified data, export-controlled data, or customer evidence.

Actor and role: Use the role named in each step. Use an Owner or authorized Compliance Manager for write/approval actions; use Auditor only for read-only checks and negative authorization checks.

Tenant context: Use the active synthetic tenant displayed by the application. Record the visible tenant name and tenant ID before starting. Copy returned record IDs; never invent IDs or use an ID from another tenant.

Tab or page: `Settings` -> `Shared responsibility matrix`

Form and control/value anchors: `Evidence`; `No-CUI acknowledgement`; `I acknowledge the No-CUI upload limitation`; `Status`; `Acknowledged`

Synthetic data: Use only the exact synthetic values in this case and the shared synthetic-data tables. Do not enter real CUI, classified information, export-controlled technical data, credentials, secrets, or customer files.

Preconditions: Confirm the active tenant, role, mode, and any prerequisite record IDs before changing a record. If a prerequisite record is unavailable, stop and record `Blocked by environment` rather than substituting another tenant or invented ID.

Missing-control rule: If a named tab, form, field, filter, button, result, or API dependency is absent, stop at that step and record the exact visible state. Mark `Failed` for a discoverability defect, or `Blocked by environment` only when the case explicitly identifies the missing dependency.

Execution order: Complete the numbered steps in order. Capture the result before executing a retry, negative, stale, cross-tenant, or provider-failure step.

1. Click the `Evidence` tab.
2. Find the `No-CUI acknowledgement` panel.
3. Confirm the notice says No-CUI mode prohibits real customer CUI.
4. Click `I acknowledge the No-CUI upload limitation`.
5. Confirm `Status` changes to `Acknowledged`.


Evidence to capture: Capture the active tenant name and ID, role, exact tab/form/control labels, entered synthetic values, returned record IDs, visible result, sanitized API status and trace ID when an authorized API step is named, and any denial or no-side-effect proof.

Execution record: Status: Pass / Fail / Blocked by environment / Not applicable / Not run | Defect: ______ | Tester: ______ | Date/time: ______ | Tenant: ______ | Evidence location: ______ | Notes: ______

Expected result: Evidence upload controls become available after acknowledgement.

## UAT-05: Create Evidence Metadata

Goal: Create reusable synthetic evidence records.


Execution map:
Current-state label: Implemented for the synthetic workflow documented here when the named UI/API surface is available; provider-dependent or API-only steps must be recorded as `Blocked by environment`. This test does not authorize real CUI, classified data, export-controlled data, or customer evidence.

Actor and role: Use the role named in each step. Use an Owner or authorized Compliance Manager for write/approval actions; use Auditor only for read-only checks and negative authorization checks.

Tenant context: Use the active synthetic tenant displayed by the application. Record the visible tenant name and tenant ID before starting. Copy returned record IDs; never invent IDs or use an ID from another tenant.

Tab or page: `Evidence` -> `Evidence metadata`

Form and control/value anchors: `Evidence`; `Evidence metadata`; `New evidence`; `MFA configuration summary - synthetic`; `System configuration`; `Security`; `Approved`; `2026-06-01`; `2027-01-31`; `FAR 52.204-21, FCI, MFA, UAT`; `AC.L1-3.1.1`; `FCI`; `User confirmed synthetic FCI-only evidence for No-CUI UAT.`; `Create metadata`; `Access control policy - synthetic`; `Policy`; `In review`; `2027-03-31`

Synthetic data: Use only the exact synthetic values in this case and the shared synthetic-data tables. Do not enter real CUI, classified information, export-controlled technical data, credentials, secrets, or customer files.

Preconditions: Confirm the active tenant, role, mode, and any prerequisite record IDs before changing a record. If a prerequisite record is unavailable, stop and record `Blocked by environment` rather than substituting another tenant or invented ID.

Missing-control rule: If a named tab, form, field, filter, button, result, or API dependency is absent, stop at that step and record the exact visible state. Mark `Failed` for a discoverability defect, or `Blocked by environment` only when the case explicitly identifies the missing dependency.

Execution order: Complete the numbered steps in order. Capture the result before executing a retry, negative, stale, cross-tenant, or provider-failure step.

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


Evidence to capture: Capture the active tenant name and ID, role, exact tab/form/control labels, entered synthetic values, returned record IDs, visible result, sanitized API status and trace ID when an authorized API step is named, and any denial or no-side-effect proof.

Execution record: Status: Pass / Fail / Blocked by environment / Not applicable / Not run | Defect: ______ | Tester: ______ | Date/time: ______ | Tenant: ______ | Evidence location: ______ | Notes: ______

Expected result: Evidence records appear in the `Evidence list` with classification badges and expiration dates.

## UAT-06: Upload A Synthetic Evidence File

Goal: Confirm upload intent works only after acknowledgement.


Execution map:
Current-state label: Implemented for the synthetic workflow documented here when the named UI/API surface is available; provider-dependent or API-only steps must be recorded as `Blocked by environment`. This test does not authorize real CUI, classified data, export-controlled data, or customer evidence.

Actor and role: Use the role named in each step. Use an Owner or authorized Compliance Manager for write/approval actions; use Auditor only for read-only checks and negative authorization checks.

Tenant context: Use the active synthetic tenant displayed by the application. Record the visible tenant name and tenant ID before starting. Copy returned record IDs; never invent IDs or use an ID from another tenant.

Tab or page: `Evidence` -> `Upload area`

Form and control/value anchors: `Evidence`; `Upload area`; `Evidence file`; `uat-mfa-summary.txt`; `Upload classification`; `Unclassified`; `Upload classification reason`; `7. Click`

Synthetic data: Use only the exact synthetic values in this case and the shared synthetic-data tables. Do not enter real CUI, classified information, export-controlled technical data, credentials, secrets, or customer files.

Preconditions: Confirm the active tenant, role, mode, and any prerequisite record IDs before changing a record. If a prerequisite record is unavailable, stop and record `Blocked by environment` rather than substituting another tenant or invented ID.

Missing-control rule: If a named tab, form, field, filter, button, result, or API dependency is absent, stop at that step and record the exact visible state. Mark `Failed` for a discoverability defect, or `Blocked by environment` only when the case explicitly identifies the missing dependency.

Execution order: Complete the numbered steps in order. Capture the result before executing a retry, negative, stale, cross-tenant, or provider-failure step.

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


Evidence to capture: Capture the active tenant name and ID, role, exact tab/form/control labels, entered synthetic values, returned record IDs, visible result, sanitized API status and trace ID when an authorized API step is named, and any denial or no-side-effect proof.

Execution record: Status: Pass / Fail / Blocked by environment / Not applicable / Not run | Defect: ______ | Tester: ______ | Date/time: ______ | Tenant: ______ | Evidence location: ______ | Notes: ______

Expected result: The app creates an upload intent or shows the accepted-upload placeholder. If upload is blocked, the error should clearly explain the gate.

## UAT-07: Create A No-CUI Contract

Goal: Create a contract record with FCI-only posture.


Execution map:
Current-state label: Implemented for the synthetic workflow documented here when the named UI/API surface is available; provider-dependent or API-only steps must be recorded as `Blocked by environment`. This test does not authorize real CUI, classified data, export-controlled data, or customer evidence.

Actor and role: Use the role named in each step. Use an Owner or authorized Compliance Manager for write/approval actions; use Auditor only for read-only checks and negative authorization checks.

Tenant context: Use the active synthetic tenant displayed by the application. Record the visible tenant name and tenant ID before starting. Copy returned record IDs; never invent IDs or use an ID from another tenant.

Tab or page: `Contracts` -> `New contract`

Form and control/value anchors: `Contracts`; `New contract`; `Create contract record`; `DEMO-NC-26-0007`; `Non-CUI Help Desk Support BPA Call`; `Fictional Prime Systems Inc. for DHS`; `Subcontractor`; `Fixed price`; `Active`; `2026-06-15`; `2026-07-01`; `2027-06-30`; `FCI only`; `Virginia, remote support`; `Create contract`; `Contract records`

Synthetic data: Use only the exact synthetic values in this case and the shared synthetic-data tables. Do not enter real CUI, classified information, export-controlled technical data, credentials, secrets, or customer files.

Preconditions: Confirm the active tenant, role, mode, and any prerequisite record IDs before changing a record. If a prerequisite record is unavailable, stop and record `Blocked by environment` rather than substituting another tenant or invented ID.

Missing-control rule: If a named tab, form, field, filter, button, result, or API dependency is absent, stop at that step and record the exact visible state. Mark `Failed` for a discoverability defect, or `Blocked by environment` only when the case explicitly identifies the missing dependency.

Execution order: Complete the numbered steps in order. Capture the result before executing a retry, negative, stale, cross-tenant, or provider-failure step.

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


Evidence to capture: Capture the active tenant name and ID, role, exact tab/form/control labels, entered synthetic values, returned record IDs, visible result, sanitized API status and trace ID when an authorized API step is named, and any denial or no-side-effect proof.

Execution record: Status: Pass / Fail / Blocked by environment / Not applicable / Not run | Defect: ______ | Tester: ______ | Date/time: ______ | Tenant: ______ | Evidence location: ______ | Notes: ______

Expected result: `DEMO-NC-26-0007` appears in the `Contract records` list.

## UAT-08: Add Contract Deliverables

Goal: Confirm contract dates appear as managed work.


Execution map:
Current-state label: Implemented for the synthetic workflow documented here when the named UI/API surface is available; provider-dependent or API-only steps must be recorded as `Blocked by environment`. This test does not authorize real CUI, classified data, export-controlled data, or customer evidence.

Actor and role: Use the role named in each step. Use an Owner or authorized Compliance Manager for write/approval actions; use Auditor only for read-only checks and negative authorization checks.

Tenant context: Use the active synthetic tenant displayed by the application. Record the visible tenant name and tenant ID before starting. Copy returned record IDs; never invent IDs or use an ID from another tenant.

Tab or page: `Contracts` -> `Deliverables`

Form and control/value anchors: `Contracts`; `DEMO-NC-26-0007`; `Deliverables`; `Monthly synthetic compliance status update`; `2026-07-31`; `Not started`; `Monthly internal status summary using synthetic UAT records only.`; `Add deliverable`

Synthetic data: Use only the exact synthetic values in this case and the shared synthetic-data tables. Do not enter real CUI, classified information, export-controlled technical data, credentials, secrets, or customer files.

Preconditions: Confirm the active tenant, role, mode, and any prerequisite record IDs before changing a record. If a prerequisite record is unavailable, stop and record `Blocked by environment` rather than substituting another tenant or invented ID.

Missing-control rule: If a named tab, form, field, filter, button, result, or API dependency is absent, stop at that step and record the exact visible state. Mark `Failed` for a discoverability defect, or `Blocked by environment` only when the case explicitly identifies the missing dependency.

Execution order: Complete the numbered steps in order. Capture the result before executing a retry, negative, stale, cross-tenant, or provider-failure step.

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


Evidence to capture: Capture the active tenant name and ID, role, exact tab/form/control labels, entered synthetic values, returned record IDs, visible result, sanitized API status and trace ID when an authorized API step is named, and any denial or no-side-effect proof.

Execution record: Status: Pass / Fail / Blocked by environment / Not applicable / Not run | Defect: ______ | Tester: ______ | Date/time: ______ | Tenant: ______ | Evidence location: ______ | Notes: ______

Expected result: The deliverable appears in the deliverable list and should be eligible for calendar visibility.

## UAT-09: Upload Contract Document Metadata

Goal: Validate contract document classification behavior in No-CUI mode.


Execution map:
Current-state label: Implemented for the synthetic workflow documented here when the named UI/API surface is available; provider-dependent or API-only steps must be recorded as `Blocked by environment`. This test does not authorize real CUI, classified data, export-controlled data, or customer evidence.

Actor and role: Use the role named in each step. Use an Owner or authorized Compliance Manager for write/approval actions; use Auditor only for read-only checks and negative authorization checks.

Tenant context: Use the active synthetic tenant displayed by the application. Record the visible tenant name and tenant ID before starting. Copy returned record IDs; never invent IDs or use an ID from another tenant.

Tab or page: `Contracts` -> `Upload document metadata`

Form and control/value anchors: `Contracts`; `Documents`; `Contract`; `FCI`; `All review states`; `demo-nc-contract.txt`; `4. Click`

Synthetic data: Use only the exact synthetic values in this case and the shared synthetic-data tables. Do not enter real CUI, classified information, export-controlled technical data, credentials, secrets, or customer files.

Preconditions: Confirm the active tenant, role, mode, and any prerequisite record IDs before changing a record. If a prerequisite record is unavailable, stop and record `Blocked by environment` rather than substituting another tenant or invented ID.

Missing-control rule: If a named tab, form, field, filter, button, result, or API dependency is absent, stop at that step and record the exact visible state. Mark `Failed` for a discoverability defect, or `Blocked by environment` only when the case explicitly identifies the missing dependency.

Execution order: Complete the numbered steps in order. Capture the result before executing a retry, negative, stale, cross-tenant, or provider-failure step.

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


Evidence to capture: Capture the active tenant name and ID, role, exact tab/form/control labels, entered synthetic values, returned record IDs, visible result, sanitized API status and trace ID when an authorized API step is named, and any denial or no-side-effect proof.

Execution record: Status: Pass / Fail / Blocked by environment / Not applicable / Not run | Defect: ______ | Tester: ______ | Date/time: ______ | Tenant: ______ | Evidence location: ______ | Notes: ______

Expected result: The document appears in the contract document list with an `FCI` classification badge.

## UAT-10: Prove No-CUI Blocks CUI

Goal: Confirm the gate blocks real CUI handling workflows in `NoCui` mode.


Execution map:
Current-state label: Implemented for the synthetic workflow documented here when the named UI/API surface is available; provider-dependent or API-only steps must be recorded as `Blocked by environment`. This test does not authorize real CUI, classified data, export-controlled data, or customer evidence.

Actor and role: Use the role named in each step. Use an Owner or authorized Compliance Manager for write/approval actions; use Auditor only for read-only checks and negative authorization checks.

Tenant context: Use the active synthetic tenant displayed by the application. Record the visible tenant name and tenant ID before starting. Copy returned record IDs; never invent IDs or use an ID from another tenant.

Tab or page: `Evidence` -> `Classification review` and upload/classification controls

Form and control/value anchors: `NoCui`; `Contracts`; `Documents`; `Contract document classification`; `CUI`; `Upload metadata`; `Settings`; `Audit log`; `Rejected`; `TenantDataHandlingModePolicy`; `Filter`

Synthetic data: Use only the exact synthetic values in this case and the shared synthetic-data tables. Do not enter real CUI, classified information, export-controlled technical data, credentials, secrets, or customer files.

Preconditions: Confirm the active tenant, role, mode, and any prerequisite record IDs before changing a record. If a prerequisite record is unavailable, stop and record `Blocked by environment` rather than substituting another tenant or invented ID.

Missing-control rule: If a named tab, form, field, filter, button, result, or API dependency is absent, stop at that step and record the exact visible state. Mark `Failed` for a discoverability defect, or `Blocked by environment` only when the case explicitly identifies the missing dependency.

Execution order: Complete the numbered steps in order. Capture the result before executing a retry, negative, stale, cross-tenant, or provider-failure step.

1. Stay on `Contracts`.
2. In the same `Documents` section, set `Contract document classification` to `CUI`.
3. Select the same synthetic test file.
4. Click `Upload metadata`.


Evidence to capture: Capture the active tenant name and ID, role, exact tab/form/control labels, entered synthetic values, returned record IDs, visible result, sanitized API status and trace ID when an authorized API step is named, and any denial or no-side-effect proof.

Execution record: Status: Pass / Fail / Blocked by environment / Not applicable / Not run | Defect: ______ | Tester: ______ | Date/time: ______ | Tenant: ______ | Evidence location: ______ | Notes: ______

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


Execution map:
Current-state label: Implemented for the synthetic workflow documented here when the named UI/API surface is available; provider-dependent or API-only steps must be recorded as `Blocked by environment`. This test does not authorize real CUI, classified data, export-controlled data, or customer evidence.

Actor and role: Use the role named in each step. Use an Owner or authorized Compliance Manager for write/approval actions; use Auditor only for read-only checks and negative authorization checks.

Tenant context: Use the active synthetic tenant displayed by the application. Record the visible tenant name and tenant ID before starting. Copy returned record IDs; never invent IDs or use an ID from another tenant.

Tab or page: `Contracts` -> `Clause library`

Form and control/value anchors: `Obligations`; `Clause library search`; `52.204-21`; `FAR`; `52.204-25`; `52.204-27`; `Contracts`; `DEMO-NC-26-0007`; `Attached clauses`; `Manual UAT tagging from synthetic contract text.`; `demo-nc-contract.txt`; `Attach clause`

Synthetic data: Use only the exact synthetic values in this case and the shared synthetic-data tables. Do not enter real CUI, classified information, export-controlled technical data, credentials, secrets, or customer files.

Preconditions: Confirm the active tenant, role, mode, and any prerequisite record IDs before changing a record. If a prerequisite record is unavailable, stop and record `Blocked by environment` rather than substituting another tenant or invented ID.

Missing-control rule: If a named tab, form, field, filter, button, result, or API dependency is absent, stop at that step and record the exact visible state. Mark `Failed` for a discoverability defect, or `Blocked by environment` only when the case explicitly identifies the missing dependency.

Execution order: Complete the numbered steps in order. Capture the result before executing a retry, negative, stale, cross-tenant, or provider-failure step.

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


Evidence to capture: Capture the active tenant name and ID, role, exact tab/form/control labels, entered synthetic values, returned record IDs, visible result, sanitized API status and trace ID when an authorized API step is named, and any denial or no-side-effect proof.

Execution record: Status: Pass / Fail / Blocked by environment / Not applicable / Not run | Defect: ______ | Tester: ______ | Date/time: ______ | Tenant: ______ | Evidence location: ______ | Notes: ______

Expected result: Attached clauses appear with clause number, title, source URL, and reviewed date.

## UAT-12: Review Obligation Work Queue

Goal: Confirm obligations can be filtered and reviewed.


Execution map:
Current-state label: Implemented for the synthetic workflow documented here when the named UI/API surface is available; provider-dependent or API-only steps must be recorded as `Blocked by environment`. This test does not authorize real CUI, classified data, export-controlled data, or customer evidence.

Actor and role: Use the role named in each step. Use an Owner or authorized Compliance Manager for write/approval actions; use Auditor only for read-only checks and negative authorization checks.

Tenant context: Use the active synthetic tenant displayed by the application. Record the visible tenant name and tenant ID before starting. Copy returned record IDs; never invent IDs or use an ID from another tenant.

Tab or page: `Obligations` -> obligation work queue

Form and control/value anchors: `Obligations`; `Obligation work queue`; `DEMO-NC-26-0007`; `High`; `Security`; `All status`; `Cybersecurity`; `Next 30 days`; `52.204-21`; `Apply filters`; `View details`; `Obligation detail`; `Why it applies`; `Required action`; `Owner`; `Source`; `Confidence`; `Last reviewed`

Synthetic data: Use only the exact synthetic values in this case and the shared synthetic-data tables. Do not enter real CUI, classified information, export-controlled technical data, credentials, secrets, or customer files.

Preconditions: Confirm the active tenant, role, mode, and any prerequisite record IDs before changing a record. If a prerequisite record is unavailable, stop and record `Blocked by environment` rather than substituting another tenant or invented ID.

Missing-control rule: If a named tab, form, field, filter, button, result, or API dependency is absent, stop at that step and record the exact visible state. Mark `Failed` for a discoverability defect, or `Blocked by environment` only when the case explicitly identifies the missing dependency.

Execution order: Complete the numbered steps in order. Capture the result before executing a retry, negative, stale, cross-tenant, or provider-failure step.

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


Evidence to capture: Capture the active tenant name and ID, role, exact tab/form/control labels, entered synthetic values, returned record IDs, visible result, sanitized API status and trace ID when an authorized API step is named, and any denial or no-side-effect proof.

Execution record: Status: Pass / Fail / Blocked by environment / Not applicable / Not run | Defect: ______ | Tester: ______ | Date/time: ______ | Tenant: ______ | Evidence location: ______ | Notes: ______

Expected result: The obligation status and owner assignment update, and the source-backed details remain visible.

## UAT-13: Review Calendar

Goal: Confirm tasks and due dates roll into the calendar.


Execution map:
Current-state label: Implemented for the synthetic workflow documented here when the named UI/API surface is available; provider-dependent or API-only steps must be recorded as `Blocked by environment`. This test does not authorize real CUI, classified data, export-controlled data, or customer evidence.

Actor and role: Use the role named in each step. Use an Owner or authorized Compliance Manager for write/approval actions; use Auditor only for read-only checks and negative authorization checks.

Tenant context: Use the active synthetic tenant displayed by the application. Record the visible tenant name and tenant ID before starting. Copy returned record IDs; never invent IDs or use an ID from another tenant.

Tab or page: `Calendar` -> filters

Form and control/value anchors: `Calendar`; `2026-07-31`; `2026-10-31`

Synthetic data: Use only the exact synthetic values in this case and the shared synthetic-data tables. Do not enter real CUI, classified information, export-controlled technical data, credentials, secrets, or customer files.

Preconditions: Confirm the active tenant, role, mode, and any prerequisite record IDs before changing a record. If a prerequisite record is unavailable, stop and record `Blocked by environment` rather than substituting another tenant or invented ID.

Missing-control rule: If a named tab, form, field, filter, button, result, or API dependency is absent, stop at that step and record the exact visible state. Mark `Failed` for a discoverability defect, or `Blocked by environment` only when the case explicitly identifies the missing dependency.

Execution order: Complete the numbered steps in order. Capture the result before executing a retry, negative, stale, cross-tenant, or provider-failure step.

1. Click the `Calendar` tab.
2. Filter by the contract or owner if available.
3. Confirm the calendar includes applicable items such as:
   - Contract deliverable due `2026-07-31`
   - SAM expiration `2026-10-31`
   - Evidence expiration dates
   - Obligation due dates


Evidence to capture: Capture the active tenant name and ID, role, exact tab/form/control labels, entered synthetic values, returned record IDs, visible result, sanitized API status and trace ID when an authorized API step is named, and any denial or no-side-effect proof.

Execution record: Status: Pass / Fail / Blocked by environment / Not applicable / Not run | Defect: ______ | Tester: ______ | Date/time: ______ | Tenant: ______ | Evidence location: ______ | Notes: ______

Expected result: Calendar data is tenant scoped and reflects profile, contract, evidence, and obligation dates.

## UAT-14: Create CMMC Readiness Workspace

Goal: Confirm CMMC readiness setup for No-CUI / FCI-only work.


Execution map:
Current-state label: Implemented for the synthetic workflow documented here when the named UI/API surface is available; provider-dependent or API-only steps must be recorded as `Blocked by environment`. This test does not authorize real CUI, classified data, export-controlled data, or customer evidence.

Actor and role: Use the role named in each step. Use an Owner or authorized Compliance Manager for write/approval actions; use Auditor only for read-only checks and negative authorization checks.

Tenant context: Use the active synthetic tenant displayed by the application. Record the visible tenant name and tenant ID before starting. Copy returned record IDs; never invent IDs or use an ID from another tenant.

Tab or page: `CMMC` -> `Readiness assessments`

Form and control/value anchors: `CMMC`; `CMMC and NIST workspace`; `No-CUI Level 1 readiness workspace`; `Level 1`; `FAR basic safeguarding`; `In progress`; `2026-06-15`; `2027-06-15`; `Security`; `DEMO-NC-26-0007`; `Create assessment`; `POA&M remediation`; `High`; `Open`; `2026-07-15`; `Synthetic UAT gap: document annual access review evidence.`; `Upload synthetic access review summary and link it to the control.`; `Create POA&M`

Synthetic data: Use only the exact synthetic values in this case and the shared synthetic-data tables. Do not enter real CUI, classified information, export-controlled technical data, credentials, secrets, or customer files.

Preconditions: Confirm the active tenant, role, mode, and any prerequisite record IDs before changing a record. If a prerequisite record is unavailable, stop and record `Blocked by environment` rather than substituting another tenant or invented ID.

Missing-control rule: If a named tab, form, field, filter, button, result, or API dependency is absent, stop at that step and record the exact visible state. Mark `Failed` for a discoverability defect, or `Blocked by environment` only when the case explicitly identifies the missing dependency.

Execution order: Complete the numbered steps in order. Capture the result before executing a retry, negative, stale, cross-tenant, or provider-failure step.

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


Evidence to capture: Capture the active tenant name and ID, role, exact tab/form/control labels, entered synthetic values, returned record IDs, visible result, sanitized API status and trace ID when an authorized API step is named, and any denial or no-side-effect proof.

Execution record: Status: Pass / Fail / Blocked by environment / Not applicable / Not run | Defect: ______ | Tester: ______ | Date/time: ______ | Tenant: ______ | Evidence location: ______ | Notes: ______

Expected result: The assessment appears in `Readiness assessments`; the POA&M appears in `POA&M remediation`.

## UAT-15: Create Subcontractor And Flow-Down

Goal: Confirm supplier tracking works.


Execution map:
Current-state label: Implemented for the synthetic workflow documented here when the named UI/API surface is available; provider-dependent or API-only steps must be recorded as `Blocked by environment`. This test does not authorize real CUI, classified data, export-controlled data, or customer evidence.

Actor and role: Use the role named in each step. Use an Owner or authorized Compliance Manager for write/approval actions; use Auditor only for read-only checks and negative authorization checks.

Tenant context: Use the active synthetic tenant displayed by the application. Record the visible tenant name and tenant ID before starting. Copy returned record IDs; never invent IDs or use an ID from another tenant.

Tab or page: `Subcontractors` -> subcontractor form

Form and control/value anchors: `Subcontractors`; `Northstar Demo Components LLC`; `Rowan Ellis`; `rowan.ellis+uat@example.com`; `Small, SDB`; `Level 1 self-assessment draft`; `Level 1`; `2026-12-15`; `Signed`; `18`; `DEMO-NC-26-0007`; `Component documentation support using synthetic No-CUI records only.`; `Create subcontractor`; `Subcontractor list`; `Assign flow-down`; `Manual entry`; `FAR 52.204-21`; `Sent`

Synthetic data: Use only the exact synthetic values in this case and the shared synthetic-data tables. Do not enter real CUI, classified information, export-controlled technical data, credentials, secrets, or customer files.

Preconditions: Confirm the active tenant, role, mode, and any prerequisite record IDs before changing a record. If a prerequisite record is unavailable, stop and record `Blocked by environment` rather than substituting another tenant or invented ID.

Missing-control rule: If a named tab, form, field, filter, button, result, or API dependency is absent, stop at that step and record the exact visible state. Mark `Failed` for a discoverability defect, or `Blocked by environment` only when the case explicitly identifies the missing dependency.

Execution order: Complete the numbered steps in order. Capture the result before executing a retry, negative, stale, cross-tenant, or provider-failure step.

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


Evidence to capture: Capture the active tenant name and ID, role, exact tab/form/control labels, entered synthetic values, returned record IDs, visible result, sanitized API status and trace ID when an authorized API step is named, and any denial or no-side-effect proof.

Execution record: Status: Pass / Fail / Blocked by environment / Not applicable / Not run | Defect: ______ | Tester: ______ | Date/time: ______ | Tenant: ______ | Evidence location: ______ | Notes: ______

Expected result: The flow-down appears in `Flow-down register`, and the evidence request appears in `Evidence requests`.

## UAT-16: Generate Reports

Goal: Confirm reports are tenant-scoped and source-backed.


Execution map:
Current-state label: Implemented for the synthetic workflow documented here when the named UI/API surface is available; provider-dependent or API-only steps must be recorded as `Blocked by environment`. This test does not authorize real CUI, classified data, export-controlled data, or customer evidence.

Actor and role: Use the role named in each step. Use an Owner or authorized Compliance Manager for write/approval actions; use Auditor only for read-only checks and negative authorization checks.

Tenant context: Use the active synthetic tenant displayed by the application. Record the visible tenant name and tenant ID before starting. Copy returned record IDs; never invent IDs or use an ID from another tenant.

Tab or page: `Reports` -> report generation

Form and control/value anchors: `Reports`; `Compliance status`; `Generate status`; `CMMC readiness`; `No-CUI Level 1 readiness workspace`; `Generate readiness`; `Subcontractor compliance`; `DEMO-NC-26-0007`; `Generate supplier report`; `Evidence package builder`; `Prime review evidence package - No-CUI UAT`; `AC.L1-3.1.1`; `Northstar Demo Components LLC`; `Generate package`; `Generated this session`

Synthetic data: Use only the exact synthetic values in this case and the shared synthetic-data tables. Do not enter real CUI, classified information, export-controlled technical data, credentials, secrets, or customer files.

Preconditions: Confirm the active tenant, role, mode, and any prerequisite record IDs before changing a record. If a prerequisite record is unavailable, stop and record `Blocked by environment` rather than substituting another tenant or invented ID.

Missing-control rule: If a named tab, form, field, filter, button, result, or API dependency is absent, stop at that step and record the exact visible state. Mark `Failed` for a discoverability defect, or `Blocked by environment` only when the case explicitly identifies the missing dependency.

Execution order: Complete the numbered steps in order. Capture the result before executing a retry, negative, stale, cross-tenant, or provider-failure step.

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


Evidence to capture: Capture the active tenant name and ID, role, exact tab/form/control labels, entered synthetic values, returned record IDs, visible result, sanitized API status and trace ID when an authorized API step is named, and any denial or no-side-effect proof.

Execution record: Status: Pass / Fail / Blocked by environment / Not applicable / Not run | Defect: ______ | Tester: ______ | Date/time: ______ | Tenant: ______ | Evidence location: ______ | Notes: ______

Expected result: Reports appear under `Generated this session`. They must not claim legal approval, certification, official pass/fail status, or government endorsement.

## UAT-17: Prepare CUI-Ready Approval

Goal: Complete the formal gate before switching to `CuiReady`.


Execution map:
Current-state label: Implemented for the synthetic workflow documented here when the named UI/API surface is available; provider-dependent or API-only steps must be recorded as `Blocked by environment`. This test does not authorize real CUI, classified data, export-controlled data, or customer evidence.

Actor and role: Use the role named in each step. Use an Owner or authorized Compliance Manager for write/approval actions; use Auditor only for read-only checks and negative authorization checks.

Tenant context: Use the active synthetic tenant displayed by the application. Record the visible tenant name and tenant ID before starting. Copy returned record IDs; never invent IDs or use an ID from another tenant.

Tab or page: `Settings` -> `CUI-ready approval`

Form and control/value anchors: `CuiReady`; `Settings`; `Shared responsibility matrix`; `Acknowledge`; `Matrix acknowledgement status`; `Current`; `Matrix acknowledgement status is Current.`; `Approval checklist`; `New checklist`; `Mark complete`; `Status`; `Complete`; `Owner`; `Security`; `Review date`; `Review reason`; `10. Click`; `.
11. Click`

Synthetic data: Use only the exact synthetic values in this case and the shared synthetic-data tables. Do not enter real CUI, classified information, export-controlled technical data, credentials, secrets, or customer files.

Preconditions: Confirm the active tenant, role, mode, and any prerequisite record IDs before changing a record. If a prerequisite record is unavailable, stop and record `Blocked by environment` rather than substituting another tenant or invented ID.

Missing-control rule: If a named tab, form, field, filter, button, result, or API dependency is absent, stop at that step and record the exact visible state. Mark `Failed` for a discoverability defect, or `Blocked by environment` only when the case explicitly identifies the missing dependency.

Execution order: Complete the numbered steps in order. Capture the result before executing a retry, negative, stale, cross-tenant, or provider-failure step.

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


Evidence to capture: Capture the active tenant name and ID, role, exact tab/form/control labels, entered synthetic values, returned record IDs, visible result, sanitized API status and trace ID when an authorized API step is named, and any denial or no-side-effect proof.

Execution record: Status: Pass / Fail / Blocked by environment / Not applicable / Not run | Defect: ______ | Tester: ______ | Date/time: ______ | Tenant: ______ | Evidence location: ______ | Notes: ______

Expected result: The checklist state becomes `Approved`, and an approved checklist ID is shown.

## UAT-18: Switch To CUI-Ready

Goal: Enable the CUI-ready gate using the approved checklist.


Execution map:
Current-state label: Implemented for the synthetic workflow documented here when the named UI/API surface is available; provider-dependent or API-only steps must be recorded as `Blocked by environment`. This test does not authorize real CUI, classified data, export-controlled data, or customer evidence.

Actor and role: Use the role named in each step. Use an Owner or authorized Compliance Manager for write/approval actions; use Auditor only for read-only checks and negative authorization checks.

Tenant context: Use the active synthetic tenant displayed by the application. Record the visible tenant name and tenant ID before starting. Copy returned record IDs; never invent IDs or use an ID from another tenant.

Tab or page: `Settings` -> `Data handling mode`

Form and control/value anchors: `Settings`; `Data handling mode`; `CuiReady`; `UAT CUI-ready gate validation after approved checklist.`; `Update mode`; `Previous`; `NoCui`; `New`

Synthetic data: Use only the exact synthetic values in this case and the shared synthetic-data tables. Do not enter real CUI, classified information, export-controlled technical data, credentials, secrets, or customer files.

Preconditions: Confirm the active tenant, role, mode, and any prerequisite record IDs before changing a record. If a prerequisite record is unavailable, stop and record `Blocked by environment` rather than substituting another tenant or invented ID.

Missing-control rule: If a named tab, form, field, filter, button, result, or API dependency is absent, stop at that step and record the exact visible state. Mark `Failed` for a discoverability defect, or `Blocked by environment` only when the case explicitly identifies the missing dependency.

Execution order: Complete the numbered steps in order. Capture the result before executing a retry, negative, stale, cross-tenant, or provider-failure step.

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


Evidence to capture: Capture the active tenant name and ID, role, exact tab/form/control labels, entered synthetic values, returned record IDs, visible result, sanitized API status and trace ID when an authorized API step is named, and any denial or no-side-effect proof.

Execution record: Status: Pass / Fail / Blocked by environment / Not applicable / Not run | Defect: ______ | Tester: ______ | Date/time: ______ | Tenant: ______ | Evidence location: ______ | Notes: ______

Expected result: The tenant enters CUI-ready mode only after checklist approval.

## UAT-19: Create CUI-Ready Contract

Goal: Confirm CUI-ready mode allows CUI posture where No-CUI did not.


Execution map:
Current-state label: Implemented for the synthetic workflow documented here when the named UI/API surface is available; provider-dependent or API-only steps must be recorded as `Blocked by environment`. This test does not authorize real CUI, classified data, export-controlled data, or customer evidence.

Actor and role: Use the role named in each step. Use an Owner or authorized Compliance Manager for write/approval actions; use Auditor only for read-only checks and negative authorization checks.

Tenant context: Use the active synthetic tenant displayed by the application. Record the visible tenant name and tenant ID before starting. Copy returned record IDs; never invent IDs or use an ID from another tenant.

Tab or page: `Contracts` -> `New contract`

Form and control/value anchors: `Contracts`; `New contract`; `DEMO-CUI-26-0001`; `Synthetic CUI Handling Training Support Order`; `Fictional Defense Prime LLC for DoD`; `Subcontractor`; `Fixed price`; `Active`; `2026-07-15`; `2026-08-01`; `2027-07-31`; `CUI`; `Maryland secure workspace`; `Create contract`

Synthetic data: Use only the exact synthetic values in this case and the shared synthetic-data tables. Do not enter real CUI, classified information, export-controlled technical data, credentials, secrets, or customer files.

Preconditions: Confirm the active tenant, role, mode, and any prerequisite record IDs before changing a record. If a prerequisite record is unavailable, stop and record `Blocked by environment` rather than substituting another tenant or invented ID.

Missing-control rule: If a named tab, form, field, filter, button, result, or API dependency is absent, stop at that step and record the exact visible state. Mark `Failed` for a discoverability defect, or `Blocked by environment` only when the case explicitly identifies the missing dependency.

Execution order: Complete the numbered steps in order. Capture the result before executing a retry, negative, stale, cross-tenant, or provider-failure step.

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


Evidence to capture: Capture the active tenant name and ID, role, exact tab/form/control labels, entered synthetic values, returned record IDs, visible result, sanitized API status and trace ID when an authorized API step is named, and any denial or no-side-effect proof.

Execution record: Status: Pass / Fail / Blocked by environment / Not applicable / Not run | Defect: ______ | Tester: ______ | Date/time: ______ | Tenant: ______ | Evidence location: ______ | Notes: ______

Expected result: The CUI posture is selectable and the contract is created.

## UAT-20: CUI-Ready Workflow And Negative Checks

Goal: Confirm CUI-ready still requires classification and approval checks.


Execution map:
Current-state label: Implemented for the synthetic workflow documented here when the named UI/API surface is available; provider-dependent or API-only steps must be recorded as `Blocked by environment`. This test does not authorize real CUI, classified data, export-controlled data, or customer evidence.

Actor and role: Use the role named in each step. Use an Owner or authorized Compliance Manager for write/approval actions; use Auditor only for read-only checks and negative authorization checks.

Tenant context: Use the active synthetic tenant displayed by the application. Record the visible tenant name and tenant ID before starting. Copy returned record IDs; never invent IDs or use an ID from another tenant.

Tab or page: `Evidence` and `Contracts` -> classification and contract controls

Form and control/value anchors: `Evidence`; `Evidence metadata`; `Synthetic system boundary narrative`; `Risk assessment`; `Security`; `In review`; `2026-08-01`; `2027-02-28`; `CUI-ready, synthetic, boundary`; `AC.L2-3.1.3`; `CUI`; `Synthetic boundary narrative for UAT only.`; `Create metadata`; `Classification`; `Unknown`; `Classification review queue`; `Approved`; `Reports`

Synthetic data: Use only the exact synthetic values in this case and the shared synthetic-data tables. Do not enter real CUI, classified information, export-controlled technical data, credentials, secrets, or customer files.

Preconditions: Confirm the active tenant, role, mode, and any prerequisite record IDs before changing a record. If a prerequisite record is unavailable, stop and record `Blocked by environment` rather than substituting another tenant or invented ID.

Missing-control rule: If a named tab, form, field, filter, button, result, or API dependency is absent, stop at that step and record the exact visible state. Mark `Failed` for a discoverability defect, or `Blocked by environment` only when the case explicitly identifies the missing dependency.

Execution order: Complete the numbered steps in order. Capture the result before executing a retry, negative, stale, cross-tenant, or provider-failure step.

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

Expected result: In `CuiReady`, synthetic evidence may be created only with an explicit classification and reason; `Unknown` or `Prohibited` evidence cannot be included as eligible report-package content, and classification review/escalation remains visible. This workflow does not authorize real CUI handling.


Evidence to capture: Capture the active tenant name and ID, role, exact tab/form/control labels, entered synthetic values, returned record IDs, visible result, sanitized API status and trace ID when an authorized API step is named, and any denial or no-side-effect proof.

Execution record: Status: Pass / Fail / Blocked by environment / Not applicable / Not run | Defect: ______ | Tester: ______ | Date/time: ______ | Tenant: ______ | Evidence location: ______ | Notes: ______

## UAT-21: DemoSandbox Synthetic CUI Check

Goal: Confirm synthetic CUI seed data is isolated to demo tenants.


Execution map:
Current-state label: Implemented for the synthetic workflow documented here when the named UI/API surface is available; provider-dependent or API-only steps must be recorded as `Blocked by environment`. This test does not authorize real CUI, classified data, export-controlled data, or customer evidence.

Actor and role: Use the role named in each step. Use an Owner or authorized Compliance Manager for write/approval actions; use Auditor only for read-only checks and negative authorization checks.

Tenant context: Use the active synthetic tenant displayed by the application. Record the visible tenant name and tenant ID before starting. Copy returned record IDs; never invent IDs or use an ID from another tenant.

Tab or page: `Settings` -> `DemoSandbox` seed controls

Form and control/value anchors: `Settings`; `Data handling mode`; `Active tenant`; `Tenant ID`; `Current mode`; `DemoSandbox`; `Mode`; `Reason for mode change`; `UAT validation of approved synthetic demo dataset.`; `Approval checklist ID`; `Update mode`; `Workspace`; `Demo sandbox seed`; `Required mode`; `Dataset version`; `2026.06.phase1a`; `Classification`; `SyntheticCui`

Synthetic data: Use only the exact synthetic values in this case and the shared synthetic-data tables. Do not enter real CUI, classified information, export-controlled technical data, credentials, secrets, or customer files.

Preconditions: Confirm the active tenant, role, mode, and any prerequisite record IDs before changing a record. If a prerequisite record is unavailable, stop and record `Blocked by environment` rather than substituting another tenant or invented ID.

Missing-control rule: If a named tab, form, field, filter, button, result, or API dependency is absent, stop at that step and record the exact visible state. Mark `Failed` for a discoverability defect, or `Blocked by environment` only when the case explicitly identifies the missing dependency.

Execution order: Complete the numbered steps in order. Capture the result before executing a retry, negative, stale, cross-tenant, or provider-failure step.

1. Click the `Settings` tab.
2. Find `Data handling mode` at the top of the page.
3. Confirm `Active tenant`, `Tenant ID`, and `Current mode` are visible.
4. Confirm `Current mode` = `DemoSandbox`. If it does not, set `Mode` = `DemoSandbox`, enter `Reason for mode change` = `UAT validation of approved synthetic demo dataset.`, leave `Approval checklist ID` blank, and click `Update mode`.
5. Use the `Workspace` selector in the left sidebar if the authenticated user belongs to more than one tenant. Confirm the selected tenant is the intended `DemoSandbox` tenant before continuing.
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


Evidence to capture: Capture the active tenant name and ID, role, exact tab/form/control labels, entered synthetic values, returned record IDs, visible result, sanitized API status and trace ID when an authorized API step is named, and any denial or no-side-effect proof.

Execution record: Status: Pass / Fail / Blocked by environment / Not applicable / Not run | Defect: ______ | Tester: ______ | Date/time: ______ | Tenant: ______ | Evidence location: ______ | Notes: ______

Expected result: Approved synthetic seed content is usable in `DemoSandbox`; real CUI upload is blocked.

## UAT-22: Final Audit And Isolation Review

Goal: Confirm traceability and tenant isolation.


Execution map:
Current-state label: Implemented for the synthetic workflow documented here when the named UI/API surface is available; provider-dependent or API-only steps must be recorded as `Blocked by environment`. This test does not authorize real CUI, classified data, export-controlled data, or customer evidence.

Actor and role: Use the role named in each step. Use an Owner or authorized Compliance Manager for write/approval actions; use Auditor only for read-only checks and negative authorization checks.

Tenant context: Use the active synthetic tenant displayed by the application. Record the visible tenant name and tenant ID before starting. Copy returned record IDs; never invent IDs or use an ID from another tenant.

Tab or page: `Settings` -> `Audit log`

Form and control/value anchors: `Settings`; `Audit log`; `Action`; `Entity`; `From`; `To`; `Expected Summary`; `Summary`; `UAT-22-A`; `Created`; `SharedResponsibilityMatrixAcknowledgement`; `Shared responsibility matrix acknowledged.`; `Shared responsibility matrix`; `Acknowledge`; `UAT-22-B`; `CuiReadyApprovalChecklist`; `CUI-ready approval checklist was created.`; `Approval checklist`

Synthetic data: Use only the exact synthetic values in this case and the shared synthetic-data tables. Do not enter real CUI, classified information, export-controlled technical data, credentials, secrets, or customer files.

Preconditions: Confirm the active tenant, role, mode, and any prerequisite record IDs before changing a record. If a prerequisite record is unavailable, stop and record `Blocked by environment` rather than substituting another tenant or invented ID.

Missing-control rule: If a named tab, form, field, filter, button, result, or API dependency is absent, stop at that step and record the exact visible state. Mark `Failed` for a discoverability defect, or `Blocked by environment` only when the case explicitly identifies the missing dependency.

Execution order: Complete the numbered steps in order. Capture the result before executing a retry, negative, stale, cross-tenant, or provider-failure step.

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


Evidence to capture: Capture the active tenant name and ID, role, exact tab/form/control labels, entered synthetic values, returned record IDs, visible result, sanitized API status and trace ID when an authorized API step is named, and any denial or no-side-effect proof.

Execution record: Status: Pass / Fail / Blocked by environment / Not applicable / Not run | Defect: ______ | Tester: ______ | Date/time: ______ | Tenant: ______ | Evidence location: ______ | Notes: ______

Expected result: Audit history is traceable, and tenant data does not leak across tenants.

## UAT-23: Security Review


Execution map:
Current-state label: `Implemented` as a tenant-scoped, versioned readiness record. This is not a production security assessment or certification determination.

Goal: Complete and approve the security review using synthetic evidence.

Current-state label: Implemented for the synthetic workflow documented here when the named UI/API surface is available; provider-dependent or API-only steps must be recorded as `Blocked by environment`. This test does not authorize real CUI, classified data, export-controlled data, or customer evidence.

Actor and role: Use the role named in each step. Use an Owner or authorized Compliance Manager for write/approval actions; use Auditor only for read-only checks and negative authorization checks.

Tenant context: Use the active synthetic tenant displayed by the application. Record the visible tenant name and tenant ID before starting. Copy returned record IDs; never invent IDs or use an ID from another tenant.

Tab or page: `Settings` -> `Security and incident readiness` -> `Security review`

Form and control/value anchors: `Implemented`; `Settings`; `Security and incident readiness`; `Security review`; `tenant isolation`; `evidence storage`; `encryption`; `malware scanning`; `retention`; `backup`; `restore`; `admin access`; `support access`; `antitrust procurement integrity`; `logging`; `monitoring`; `incident response`; `Passed`

Synthetic data: Use only the exact synthetic values in this case and the shared synthetic-data tables. Do not enter real CUI, classified information, export-controlled technical data, credentials, secrets, or customer files.

Preconditions: Confirm the active tenant, role, mode, and any prerequisite record IDs before changing a record. If a prerequisite record is unavailable, stop and record `Blocked by environment` rather than substituting another tenant or invented ID.

Missing-control rule: If a named tab, form, field, filter, button, result, or API dependency is absent, stop at that step and record the exact visible state. Mark `Failed` for a discoverability defect, or `Blocked by environment` only when the case explicitly identifies the missing dependency.

Execution order: Complete the numbered steps in order. Capture the result before executing a retry, negative, stale, cross-tenant, or provider-failure step.

1. Open `Settings` and scroll to `Security and incident readiness`.
2. In the `Security review` card, confirm the review state, readiness count, blocking-finding count, and review-area grid are visible.
3. Confirm these rows are present: `tenant isolation`, `evidence storage`, `encryption`, `malware scanning`, `retention`, `backup`, `restore`, `admin access`, `support access`, `antitrust procurement integrity`, `logging`, `monitoring`, and `incident response`.
4. For every row, select `Passed`, enter `MFA configuration summary - synthetic` in `Evidence reference`, and enter `Synthetic control verification for UAT; no production security details.` in `Rationale`.
5. Click `Add finding`. Set `Area` = `tenant-isolation`, `Summary` = `Synthetic access review follow-up`, `Severity` = `Medium`, `Status` = `Closed`, `Remediation owner` = `Security`, `Due date` = `2026-10-01`, and `Closure notes` = `Synthetic closure evidence reviewed; no production security details.`
6. Click `Add accepted risk`. Set `Related finding` = `General review risk`, `Scope` = `Synthetic UAT-only residual risk`, `Review date` = `2026-09-18`, `Expiration date` = `2026-12-31`, and `Mitigation` = `Monitor the synthetic workflow and repeat the review before expiration.`
7. Click `Save new review version`, reload, and confirm the row statuses, finding, accepted-risk fields, version, and readiness history persist.
8. Enter `Synthetic reviewer approval for the current UAT security review.` in `Approval notes` and click `Approve security review` as the authorized approver.
9. Confirm the approved state, approver, date, notes, version, and audit/history event.
10. Repeat with missing evidence, an open `Critical` finding, a stale version, `Auditor`, and a Tenant B identifier. Confirm validation/authorization failure and no unauthorized state change.


Evidence to capture: Capture the active tenant name and ID, role, exact tab/form/control labels, entered synthetic values, returned record IDs, visible result, sanitized API status and trace ID when an authorized API step is named, and any denial or no-side-effect proof.

Execution record: Status: Pass / Fail / Blocked by environment / Not applicable / Not run | Defect: ______ | Tester: ______ | Date/time: ______ | Tenant: ______ | Evidence location: ______ | Notes: ______

Expected result: The security review is evidence-linked, versioned, tenant-scoped, and auditable. It does not authorize real CUI or claim certification.

## UAT-24: Technical Control Verification


Execution map:
Current-state label: `Implemented` as evidence-linked readiness records. It does not prove deployed infrastructure configuration or an independent assessment.

Goal: Save and approve six synthetic technical control verification records.

Current-state label: Implemented for the synthetic workflow documented here when the named UI/API surface is available; provider-dependent or API-only steps must be recorded as `Blocked by environment`. This test does not authorize real CUI, classified data, export-controlled data, or customer evidence.

Actor and role: Use the role named in each step. Use an Owner or authorized Compliance Manager for write/approval actions; use Auditor only for read-only checks and negative authorization checks.

Tenant context: Use the active synthetic tenant displayed by the application. Record the visible tenant name and tenant ID before starting. Copy returned record IDs; never invent IDs or use an ID from another tenant.

Tab or page: `Settings` -> `Security and incident readiness` -> `Technical control verification`

Form and control/value anchors: `Implemented`; `Settings`; `Security and incident readiness`; `Technical control verification`; `staging-synthetic`; `Environment`; `2026-09-18`; `Execution date`; `External immutable artifact`; `tenant isolation`; `evidence storage`; `malware scanner`; `backup restore`; `administrator access`; `support access`; `HTTPS artifact URI`; `aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa`; `SHA-256 digest`

Synthetic data: Use only the exact synthetic values in this case and the shared synthetic-data tables. Do not enter real CUI, classified information, export-controlled technical data, credentials, secrets, or customer files.

Preconditions: Confirm the active tenant, role, mode, and any prerequisite record IDs before changing a record. If a prerequisite record is unavailable, stop and record `Blocked by environment` rather than substituting another tenant or invented ID.

Missing-control rule: If a named tab, form, field, filter, button, result, or API dependency is absent, stop at that step and record the exact visible state. Mark `Failed` for a discoverability defect, or `Blocked by environment` only when the case explicitly identifies the missing dependency.

Execution order: Complete the numbered steps in order. Capture the result before executing a retry, negative, stale, cross-tenant, or provider-failure step.

1. In `Settings` -> `Security and incident readiness`, locate `Technical control verification`.
2. Enter `staging-synthetic` in `Environment` and `2026-09-18` in `Execution date`.
3. For each control source editor, select `External immutable artifact`.
4. For `tenant isolation`, `evidence storage`, `malware scanner`, `backup restore`, `administrator access`, and `support access`, enter `https://example.invalid/uat/technical-controls.txt` in `HTTPS artifact URI` and `aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa` in `SHA-256 digest`.
5. Click `Save executed controls`.
6. Confirm the card shows six executed records with the environment, execution date, `Passed` result, reviewer, URI, and digest.
7. Reload and confirm all six records persist.
8. Enter `Synthetic approval of executed technical controls.` in `Approval notes` and click `Approve technical readiness` as the authorized approver.
9. Clear one URI, use a non-HTTPS URI, or use a non-hex digest. Confirm save is disabled or rejected without a partial record.
10. Repeat approval as `Auditor`, with a stale version, and with a Tenant B identifier. Confirm denial/conflict and no state change.


Evidence to capture: Capture the active tenant name and ID, role, exact tab/form/control labels, entered synthetic values, returned record IDs, visible result, sanitized API status and trace ID when an authorized API step is named, and any denial or no-side-effect proof.

Execution record: Status: Pass / Fail / Blocked by environment / Not applicable / Not run | Defect: ______ | Tester: ______ | Date/time: ______ | Tenant: ______ | Evidence location: ______ | Notes: ______

Expected result: Technical verification records retain control, environment, date, reviewer, result, evidence source, and approval history. The result remains internal readiness evidence.

## UAT-25: Incident Readiness


Execution map:
Current-state label: `Implemented` as a tenant-scoped readiness record. It is not an incident-response service or emergency-access authorization.

Goal: Configure synthetic incident contacts, tabletop evidence, and follow-up work.

Current-state label: Implemented for the synthetic workflow documented here when the named UI/API surface is available; provider-dependent or API-only steps must be recorded as `Blocked by environment`. This test does not authorize real CUI, classified data, export-controlled data, or customer evidence.

Actor and role: Use the role named in each step. Use an Owner or authorized Compliance Manager for write/approval actions; use Auditor only for read-only checks and negative authorization checks.

Tenant context: Use the active synthetic tenant displayed by the application. Record the visible tenant name and tenant ID before starting. Copy returned record IDs; never invent IDs or use an ID from another tenant.

Tab or page: `Settings` -> `Security and incident readiness` -> `Incident readiness`

Form and control/value anchors: `Implemented`; `Settings`; `Security and incident readiness`; `Incident readiness`; `Security`; `Escalation owner`; `security-uat@example.invalid`; `support-uat@example.invalid`; `legal-compliance-uat@example.invalid`; `engineering-uat@example.invalid`; `customer-success-uat@example.invalid`; `Annual`; `Review basis`; `2027-09-18`; `Next review date`; `Reviewed trigger criteria`; `Executed tabletop evidence`; `External immutable artifact`

Synthetic data: Use only the exact synthetic values in this case and the shared synthetic-data tables. Do not enter real CUI, classified information, export-controlled technical data, credentials, secrets, or customer files.

Preconditions: Confirm the active tenant, role, mode, and any prerequisite record IDs before changing a record. If a prerequisite record is unavailable, stop and record `Blocked by environment` rather than substituting another tenant or invented ID.

Missing-control rule: If a named tab, form, field, filter, button, result, or API dependency is absent, stop at that step and record the exact visible state. Mark `Failed` for a discoverability defect, or `Blocked by environment` only when the case explicitly identifies the missing dependency.

Execution order: Complete the numbered steps in order. Capture the result before executing a retry, negative, stale, cross-tenant, or provider-failure step.

1. In `Settings` -> `Security and incident readiness`, locate `Incident readiness`.
2. Enter `Security` in `Escalation owner`.
3. Enter these values in the contact fields: `security-uat@example.invalid`, `support-uat@example.invalid`, `legal-compliance-uat@example.invalid`, `engineering-uat@example.invalid`, and `customer-success-uat@example.invalid`.
4. Select `Annual` in `Review basis` and enter `2027-09-18` in `Next review date`.
5. Enter `Suspected sensitive-data exposure, malware detection, cross-tenant exposure, or failed deletion/export request.` in `Reviewed trigger criteria`.
6. In `Executed tabletop evidence`, select `External immutable artifact`, enter `https://example.invalid/uat/incident-tabletop.txt`, and enter SHA-256 `bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb`.
7. Click `Add follow-up`. Enter `Synthetic tabletop follow-up`, choose `Medium`, leave `Open`, set `Owner` to `Security`, and set the due date to `2026-10-18`.
8. Click `Save playbooks and exercise`, reload, and confirm contacts, trigger criteria, tabletop evidence, review date, and follow-up persist.
9. Enter `Synthetic incident-readiness approval for UAT.` in `Approval notes` and click `Approve incident readiness` as the authorized approver.
10. Close the follow-up with `Synthetic follow-up completed; no customer data involved.` and save the new version.
11. Clear a required contact or trigger criterion, then repeat as `Auditor`, with a stale version, and with a Tenant B identifier. Confirm rejection/denial and no unauthorized state change.


Evidence to capture: Capture the active tenant name and ID, role, exact tab/form/control labels, entered synthetic values, returned record IDs, visible result, sanitized API status and trace ID when an authorized API step is named, and any denial or no-side-effect proof.

Execution record: Status: Pass / Fail / Blocked by environment / Not applicable / Not run | Defect: ______ | Tester: ______ | Date/time: ______ | Tenant: ______ | Evidence location: ______ | Notes: ______

Expected result: Incident contacts, playbooks, tabletop evidence, follow-ups, approval, and history remain synthetic, tenant-scoped, and reviewable without storing real incident-sensitive data.

## UAT-26: Invite An External Reviewer


Execution map:
Current-state label: `Implemented` for tenant-admin invitation management. It does not authorize CUI sharing.

Goal: Create a least-privilege external reviewer invitation scoped to one approved package and contract.

Current-state label: Implemented for the synthetic workflow documented here when the named UI/API surface is available; provider-dependent or API-only steps must be recorded as `Blocked by environment`. This test does not authorize real CUI, classified data, export-controlled data, or customer evidence.

Actor and role: Use the role named in each step. Use an Owner or authorized Compliance Manager for write/approval actions; use Auditor only for read-only checks and negative authorization checks.

Tenant context: Use the active synthetic tenant displayed by the application. Record the visible tenant name and tenant ID before starting. Copy returned record IDs; never invent IDs or use an ID from another tenant.

Tab or page: `Settings` -> `External portal invitations` -> `Invite an external reviewer`

Form and control/value anchors: `Implemented`; `Settings`; `External portal invitations`; `Invite an external reviewer`; `reviewer+uat@example.invalid`; `Portal reviewer email`; `Auditor reviewer`; `Portal role`; `Prime reviewer`; `Advisor reviewer`; `Package recipient`; `Approved package IDs`; `DEMO-NC-26-0007`; `Contract scope IDs`; `2026-12-31`; `Expiration date`; `Allow approved package downloads`; `Require strong authentication`

Synthetic data: Use only the exact synthetic values in this case and the shared synthetic-data tables. Do not enter real CUI, classified information, export-controlled technical data, credentials, secrets, or customer files.

Preconditions: Confirm the active tenant, role, mode, and any prerequisite record IDs before changing a record. If a prerequisite record is unavailable, stop and record `Blocked by environment` rather than substituting another tenant or invented ID.

Missing-control rule: If a named tab, form, field, filter, button, result, or API dependency is absent, stop at that step and record the exact visible state. Mark `Failed` for a discoverability defect, or `Blocked by environment` only when the case explicitly identifies the missing dependency.

Execution order: Complete the numbered steps in order. Capture the result before executing a retry, negative, stale, cross-tenant, or provider-failure step.

1. Complete UAT-30 or record an existing approved synthetic SSP package ID returned by the current tenant.
2. Open `Settings` -> `External portal invitations`.
3. Locate the form `Invite an external reviewer`.
4. Enter `reviewer+uat@example.invalid` in `Portal reviewer email`.
5. Select `Auditor reviewer` in `Portal role`. Confirm the available controlled choices include `Prime reviewer`, `Auditor reviewer`, `Advisor reviewer`, and `Package recipient`.
6. Enter the returned SSP package ID in `Approved package IDs`.
7. Enter the contract ID for `DEMO-NC-26-0007` in `Contract scope IDs`.
8. Enter `2026-12-31` in `Expiration date`.
9. Leave `Allow approved package downloads` unchecked and keep `Require strong authentication` checked.
10. Click `Create portal invitation` once.
11. Confirm the invitation appears under `Invitation access` with the email, role, status, expiration, package count, contract-scope count, downloads `blocked`, and strong authentication `required`.
12. Try a missing package, malformed email, past expiration, Tenant B package ID, and `Auditor` administration. Confirm validation/authorization failure and no invitation creation.


Evidence to capture: Capture the active tenant name and ID, role, exact tab/form/control labels, entered synthetic values, returned record IDs, visible result, sanitized API status and trace ID when an authorized API step is named, and any denial or no-side-effect proof.

Execution record: Status: Pass / Fail / Blocked by environment / Not applicable / Not run | Defect: ______ | Tester: ______ | Date/time: ______ | Tenant: ______ | Evidence location: ______ | Notes: ______

Expected result: The invitation has explicit package and contract scope, strong authentication, no download permission, and no inherited tenant membership privileges.

## UAT-27: Invitation Access And Revocation


Execution map:
Current-state label: UI administration is `Implemented`; external resource validation is `Partially implemented` in the current build because a complete external reviewer sign-in screen is not exposed. Use an authorized portal API harness for access validation or mark those steps `Blocked by environment`.

Goal: Verify identity, scope, authentication, expiration, history, and revocation enforcement.

Current-state label: Implemented for the synthetic workflow documented here when the named UI/API surface is available; provider-dependent or API-only steps must be recorded as `Blocked by environment`. This test does not authorize real CUI, classified data, export-controlled data, or customer evidence.

Actor and role: Use the role named in each step. Use an Owner or authorized Compliance Manager for write/approval actions; use Auditor only for read-only checks and negative authorization checks.

Tenant context: Use the active synthetic tenant displayed by the application. Record the visible tenant name and tenant ID before starting. Copy returned record IDs; never invent IDs or use an ID from another tenant.

Tab or page: `Settings` -> `External portal invitations` -> `Invitation access`

Form and control/value anchors: `Implemented`; `Partially implemented`; `Blocked by environment`; `Settings`; `External portal invitations`; `Invitation access`; `reviewer+uat@example.invalid`; `View access history`; `download=true`; `Extend`; `2027-01-31`; `New expiration date`; `Confirm extend`; `Revoke invitation`; `Synthetic UAT revocation after access-scope test.`; `Confirm revoke`; `Revoked`; `Auditor`

Synthetic data: Use only the exact synthetic values in this case and the shared synthetic-data tables. Do not enter real CUI, classified information, export-controlled technical data, credentials, secrets, or customer files.

Preconditions: Confirm the active tenant, role, mode, and any prerequisite record IDs before changing a record. If a prerequisite record is unavailable, stop and record `Blocked by environment` rather than substituting another tenant or invented ID.

Missing-control rule: If a named tab, form, field, filter, button, result, or API dependency is absent, stop at that step and record the exact visible state. Mark `Failed` for a discoverability defect, or `Blocked by environment` only when the case explicitly identifies the missing dependency.

Execution order: Complete the numbered steps in order. Capture the result before executing a retry, negative, stale, cross-tenant, or provider-failure step.

1. In `Settings` -> `External portal invitations` -> `Invitation access`, locate `reviewer+uat@example.invalid` and record the invitation ID.
2. Click `View access history` and confirm the initial state contains no unrelated tenant records.
3. In an authorized synthetic portal context, request `GET /api/external-portal/invitations/{invitationId}/access?packageId={packageId}&contractId={contractId}&download=false`.
4. Confirm the request is allowed only when the authenticated email matches the invitation and strong authentication is present.
5. Repeat with weak authentication, an unrelated package ID, a Tenant B contract ID, and `download=true` while downloads are disabled. Confirm denial without disclosure.
6. Click `View access history` again and confirm allowed/denied results, result codes, timestamps, and invitation linkage.
7. Click `Extend`, enter `2027-01-31` in `New expiration date`, and click `Confirm extend`.
8. Confirm only the expiration changed. Then click `Revoke invitation`, enter `Synthetic UAT revocation after access-scope test.`, and click `Confirm revoke`.
9. Confirm the row shows `Revoked`, the reason is visible, and a post-revocation access request is denied.
10. Attempt Tenant B and `Auditor` access/management. Confirm safe denial and no cross-tenant history disclosure.


Evidence to capture: Capture the active tenant name and ID, role, exact tab/form/control labels, entered synthetic values, returned record IDs, visible result, sanitized API status and trace ID when an authorized API step is named, and any denial or no-side-effect proof.

Execution record: Status: Pass / Fail / Blocked by environment / Not applicable / Not run | Defect: ______ | Tester: ______ | Date/time: ______ | Tenant: ______ | Evidence location: ______ | Notes: ______

Expected result: External access is limited to the invited identity, approved package, permitted contract scope, authentication requirements, expiration, and download setting. Revocation takes effect at the server boundary.

## UAT-28: System Security Plan Sections


Execution map:
Current-state label: `Implemented` as an internal SSP readiness workflow. Approval does not certify compliance or authorize CUI handling.

Goal: Create, review, approve, and supersede one SSP section.

Current-state label: Implemented for the synthetic workflow documented here when the named UI/API surface is available; provider-dependent or API-only steps must be recorded as `Blocked by environment`. This test does not authorize real CUI, classified data, export-controlled data, or customer evidence.

Actor and role: Use the role named in each step. Use an Owner or authorized Compliance Manager for write/approval actions; use Auditor only for read-only checks and negative authorization checks.

Tenant context: Use the active synthetic tenant displayed by the application. Record the visible tenant name and tenant ID before starting. Copy returned record IDs; never invent IDs or use an ID from another tenant.

Tab or page: `CMMC` -> `System Security Plan sections`

Form and control/value anchors: `Implemented`; `CMMC`; `System Security Plan sections`; `SSP section editor`; `SystemDescription`; `Section type`; `Synthetic SSP system description`; `Title`; `Security`; `Owner`; `Synthetic company profile and contract`; `Source name`; `Source URL`; `2026-09-18`; `Source reviewed`; `CompanyProfile`; `Linked record type`; `Linked record ID`

Synthetic data: Use only the exact synthetic values in this case and the shared synthetic-data tables. Do not enter real CUI, classified information, export-controlled technical data, credentials, secrets, or customer files.

Preconditions: Confirm the active tenant, role, mode, and any prerequisite record IDs before changing a record. If a prerequisite record is unavailable, stop and record `Blocked by environment` rather than substituting another tenant or invented ID.

Missing-control rule: If a named tab, form, field, filter, button, result, or API dependency is absent, stop at that step and record the exact visible state. Mark `Failed` for a discoverability defect, or `Blocked by environment` only when the case explicitly identifies the missing dependency.

Execution order: Complete the numbered steps in order. Capture the result before executing a retry, negative, stale, cross-tenant, or provider-failure step.

1. Click the `CMMC` tab and locate `System Security Plan sections`.
2. In `SSP section editor`, select `SystemDescription` in `Section type`.
3. Enter `Synthetic SSP system description` in `Title`, `Security` in `Owner`, `Synthetic company profile and contract` in `Source name`, `https://example.invalid/uat/ssp-source` in `Source URL`, and `2026-09-18` in `Source reviewed`.
4. Select `CompanyProfile` in `Linked record type`, enter the returned company-profile ID in `Linked record ID`, and enter `Synthetic source link for No-CUI UAT` in `Link rationale`.
5. Click `Create section` and confirm the card shows `SystemDescription`, `Draft`, owner, version, governed-link count, and source-reference count.
6. Select the card, click `Submit for review`, enter `Security` in `Reviewer`, enter `2026-09-18` in `Review date`, and click `Approve section`.
7. Confirm the section is `Approved`, the editor is read-only, and lifecycle history shows actor/date/status.
8. Attempt to edit the approved section, use a Tenant B linked record, omit the source URL, use an invalid URL, use a stale version, and mutate as `Auditor`. Confirm rejection/denial.


Evidence to capture: Capture the active tenant name and ID, role, exact tab/form/control labels, entered synthetic values, returned record IDs, visible result, sanitized API status and trace ID when an authorized API step is named, and any denial or no-side-effect proof.

Execution record: Status: Pass / Fail / Blocked by environment / Not applicable / Not run | Defect: ______ | Tester: ______ | Date/time: ______ | Tenant: ______ | Evidence location: ______ | Notes: ______

Expected result: SSP section type, title, owner, sources, linked record, lifecycle, reviewer, and history remain tenant-scoped and traceable.

## UAT-29: SSP Narrative Review


Execution map:
Current-state label: `Implemented` as deterministic/source-backed draft workflow. Generated or edited text remains draft-only until approval succeeds.

Goal: Generate, edit, compare, and approve a synthetic SSP narrative.

Current-state label: Implemented for the synthetic workflow documented here when the named UI/API surface is available; provider-dependent or API-only steps must be recorded as `Blocked by environment`. This test does not authorize real CUI, classified data, export-controlled data, or customer evidence.

Actor and role: Use the role named in each step. Use an Owner or authorized Compliance Manager for write/approval actions; use Auditor only for read-only checks and negative authorization checks.

Tenant context: Use the active synthetic tenant displayed by the application. Record the visible tenant name and tenant ID before starting. Copy returned record IDs; never invent IDs or use an ID from another tenant.

Tab or page: `CMMC` -> `SSP narrative builder`

Form and control/value anchors: `Implemented`; `Synthetic SSP system description`; `SSP narrative builder`; `Evidence`; `Source type`; `Approved source record ID`; `Generate draft`; `Draft narrative`; `Draft-human review required`; `Narrative text`; `Synthetic reviewer note; source links checked.`; `Reviewer notes`; `Content classification`; `Unclassified`; `Save draft`; `Compare with current approved`; `Current approved narrative`; `Proposed narrative`

Synthetic data: Use only the exact synthetic values in this case and the shared synthetic-data tables. Do not enter real CUI, classified information, export-controlled technical data, credentials, secrets, or customer files.

Preconditions: Confirm the active tenant, role, mode, and any prerequisite record IDs before changing a record. If a prerequisite record is unavailable, stop and record `Blocked by environment` rather than substituting another tenant or invented ID.

Missing-control rule: If a named tab, form, field, filter, button, result, or API dependency is absent, stop at that step and record the exact visible state. Mark `Failed` for a discoverability defect, or `Blocked by environment` only when the case explicitly identifies the missing dependency.

Execution order: Complete the numbered steps in order. Capture the result before executing a retry, negative, stale, cross-tenant, or provider-failure step.

1. Select `Synthetic SSP system description` from the SSP section list.
2. In `SSP narrative builder`, select `Evidence` in `Source type` and enter the approved synthetic evidence ID in `Approved source record ID`.
3. Click `Generate draft` and confirm `Draft narrative`, `Draft-human review required`, source-link count, and classification are shown.
4. Replace `Narrative text` with `Synthetic narrative describing the No-CUI system boundary and FCI-only workflow. No customer CUI, classified data, or export-controlled technical data.`
5. Enter `Synthetic reviewer note; source links checked.` in `Reviewer notes`, keep `Content classification` = `Unclassified`, and click `Save draft`.
6. Click `Compare with current approved` and confirm `Current approved narrative` and `Proposed narrative` are distinct. Record the explicit empty state if no approved narrative exists.
7. Enter `2026-09-18` in `Review date` and click `Approve narrative` as the authorized reviewer.
8. Generate a later draft and approve it. Confirm the previous approved narrative is superseded rather than overwritten.
9. Repeat with a duplicate source, Tenant B source, expired/prohibited/unknown/CUI source, missing review date, and `Auditor` approval. Confirm no invalid approved narrative is created.


Evidence to capture: Capture the active tenant name and ID, role, exact tab/form/control labels, entered synthetic values, returned record IDs, visible result, sanitized API status and trace ID when an authorized API step is named, and any denial or no-side-effect proof.

Execution record: Status: Pass / Fail / Blocked by environment / Not applicable / Not run | Defect: ______ | Tester: ______ | Date/time: ______ | Tenant: ______ | Evidence location: ______ | Notes: ______

Expected result: Narrative generation is bounded to approved source IDs, edits remain drafts, comparison is available, and approval preserves reviewer/date/version history.

## UAT-30: SSP Review Packages


Execution map:
Current-state label: `Implemented` for immutable internal-review snapshots. Packages are not certification submissions, assessor determinations, government approvals, or authorization to handle real CUI.

Goal: Generate an SSP review package from approved same-tenant sources and verify eligibility filtering.

Current-state label: Implemented for the synthetic workflow documented here when the named UI/API surface is available; provider-dependent or API-only steps must be recorded as `Blocked by environment`. This test does not authorize real CUI, classified data, export-controlled data, or customer evidence.

Actor and role: Use the role named in each step. Use an Owner or authorized Compliance Manager for write/approval actions; use Auditor only for read-only checks and negative authorization checks.

Tenant context: Use the active synthetic tenant displayed by the application. Record the visible tenant name and tenant ID before starting. Copy returned record IDs; never invent IDs or use an ID from another tenant.

Tab or page: `CMMC` -> `SSP review packages`

Form and control/value anchors: `Implemented`; `CMMC`; `SSP review packages`; `SSP-UAT-2026.09.18`; `Package version`; `Security`; `Package reviewer`; `System boundary`; `Approved evidence IDs`; `POA&M item IDs`; `Generate internal review package`; `SSP package history`; `Human-readable report`; `Machine-readable metadata`; `NoCui`; `CuiReady`; `Approval checklist ID`; `DemoSandbox`

Synthetic data: Use only the exact synthetic values in this case and the shared synthetic-data tables. Do not enter real CUI, classified information, export-controlled technical data, credentials, secrets, or customer files.

Preconditions: Confirm the active tenant, role, mode, and any prerequisite record IDs before changing a record. If a prerequisite record is unavailable, stop and record `Blocked by environment` rather than substituting another tenant or invented ID.

Missing-control rule: If a named tab, form, field, filter, button, result, or API dependency is absent, stop at that step and record the exact visible state. Mark `Failed` for a discoverability defect, or `Blocked by environment` only when the case explicitly identifies the missing dependency.

Execution order: Complete the numbered steps in order. Capture the result before executing a retry, negative, stale, cross-tenant, or provider-failure step.

1. Open the `CMMC` tab and locate `SSP review packages`.
2. Confirm the panel states that packages are immutable internal-review snapshots and external sharing requires separate approval.
3. Enter `SSP-UAT-2026.09.18` in `Package version`, `Security` in `Package reviewer`, and `Synthetic FeDril No-CUI workspace, evidence, contract, and readiness records only.` in `System boundary`.
4. Enter the approved evidence ID from UAT-05/UAT-06 in `Approved evidence IDs` and the POA&M ID from UAT-14 in `POA&M item IDs`.
5. Click `Generate internal review package` once.
6. Confirm the success message says the internal SSP review package was generated and audit logged.
7. In `SSP package history`, confirm the row shows version, status, generated date, and section count. Select it and expand `Human-readable report` and `Machine-readable metadata`.
8. Confirm the package contains tenant, system boundary, section status, reviewer metadata, source references, approved evidence references, POA&M references, disclaimer, and history.
9. Reload and confirm the package persists once. Do not share it during this No-CUI synthetic UAT.
10. Repeat with missing reviewer/boundary, unavailable/expired/prohibited/unknown/CUI evidence, Tenant B IDs, and an unauthorized role. Confirm the request is rejected without a partial package.


Evidence to capture: Capture the active tenant name and ID, role, exact tab/form/control labels, entered synthetic values, returned record IDs, visible result, sanitized API status and trace ID when an authorized API step is named, and any denial or no-side-effect proof.

Execution record: Status: Pass / Fail / Blocked by environment / Not applicable / Not run | Defect: ______ | Tester: ______ | Date/time: ______ | Tenant: ______ | Evidence location: ______ | Notes: ______

Expected result: An eligible same-tenant SSP package is created as an immutable internal snapshot. Ineligible or cross-tenant inputs do not create a package.

## UAT Exit Criteria

UAT passes when:

- A new user can follow the tabs and forms without engineering help.
- `NoCui` blocks all real CUI handling workflows.
- `CuiReady` cannot be enabled without an approved checklist and valid `Approval checklist ID`.
- `CuiReady` still enforces classification and workflow approval checks.
- `DemoSandbox` permits only approved synthetic demo CUI content.
- Reports are tenant-scoped and avoid legal, certification, official pass/fail, or government endorsement claims.
- Audit logs capture acknowledgements, checklist lifecycle, mode changes, report generation, and rejected gate attempts.

## Document Change History

| Version | Date | Change |
| --- | --- | --- |
| 1.0 | 2026-06 historical | Original No-CUI and CUI-ready synthetic-data walkthrough. |
| 1.1 | 2026-09-18 | Added a uniform execution card to all 30 cases with actor, tenant, exact tab/form/control-value anchors, synthetic-data boundaries, missing-control handling, evidence capture, and execution records. Added beginner-safe execution rules and current-state caveats. |

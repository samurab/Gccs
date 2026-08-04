# Sample Obligation And Evidence Workflow

Document type: sales demo workflow and pilot setup guide.

Data status: synthetic, redacted, or non-sensitive data only.

Required boundary: FeDril tracks readiness workflow, obligation ownership, and evidence metadata. It does not determine legal compliance, certify CMMC status, or authorize real CUI handling in the MVP.

Publication status: controlled internal / demo-prep draft. Do not send externally until the pre-publication checklist in this document is complete.

## Current-State Verification

| Claim | Status | Evidence |
| --- | --- | --- |
| Contract metadata can be entered in the `Contracts` tab. | Implemented | UI labels verified in `apps/web/src/App.tsx`; API contract routes require `ManageContracts`. |
| Contract document upload requires current No-CUI acknowledgement. | Implemented | `ContractDocumentFileService.UploadAsync` checks acknowledgement before upload; `ContractRecordTests.TC_8_2_1` proves a `428` response before acknowledgement. |
| Evidence upload requires current No-CUI acknowledgement and user attestation. | Implemented | `EvidenceFileUploadTests.TC_12_2_1` and `NoCuiAcknowledgementTests.TC_4_1_2` prove server-side blocking before acknowledgement. |
| Evidence metadata can be created and linked to obligations or controls. | Implemented | `Evidence` tab exposes `Evidence metadata`, `Obligations (optional)`, and `Controls`; evidence API routes require evidence permissions. |
| Reports are workflow guidance only. | Implemented in UI | `Reports` tab includes the no-legal-advice/no-certification disclaimer. |
| Paid pilot delivery, conversion credit, and customer testimonial workflow. | Planned | Business process only; not an implemented product capability. |
| Real CUI upload or processing. | Do not claim | MVP posture prohibits real CUI handling. |

## Workflow Goal

Show a buyer how one contract requirement becomes a managed readiness workflow:

1. Contract metadata is entered.
2. Clauses are attached or reviewed.
3. Source-backed obligations are generated and reviewed.
4. Owners and due dates are assigned.
5. Allowed evidence metadata is linked.
6. Readiness status is reported.
7. Audit history records compliance-relevant changes.

## Sample Scenario

| Field | Value |
| --- | --- |
| Customer | Acme Federal Services LLC |
| Contract | Synthetic DoD Subcontract 2026-001 |
| Buyer persona | Operations lead and security lead |
| Data posture | No-CUI / compliance management only |
| Primary workflow | CMMC-related readiness tracking and evidence organization |
| Demo objective | Replace spreadsheet tracking with owned obligations and reportable evidence metadata |

## Step 1: Confirm Data Handling Boundary

Before evidence upload or contract document upload is enabled, the user must acknowledge the No-CUI posture. The acknowledgement is also displayed during contract intake so the buyer sees the data boundary before discussing evidence collection.

### Where to find it in the app

| Tab | UI section or action | Notes |
| --- | --- | --- |
| Contracts | No-CUI acknowledgement panel | Displayed during contract intake as `Required before contract or evidence work`. |
| Evidence | No-CUI acknowledgement panel | Displayed before the upload workflow as `Required before evidence upload`. |
| Contracts | Contract documents upload workflow | Upload-related actions require the current No-CUI acknowledgement. |
| Evidence | Upload area | Upload is disabled until acknowledgement and No-CUI attestation requirements are satisfied. |

### Required user acknowledgement

- I will not upload, paste, import, or attach real CUI.
- I will not upload classified information, ITAR/export-controlled data, credentials, payroll records, SSNs, health data, or sensitive incident details.
- I will use synthetic, redacted, or non-sensitive data during the pilot.
- I understand FeDril reports are workflow guidance, not legal advice or certification decisions.

### Sales demo note

Show this step before discussing evidence upload or contract document upload. This prevents the prospect from assuming FeDril is a CUI repository. Do not state that all contract metadata entry is blocked before acknowledgement; the current MVP blocks upload-related workflows, while contract intake still displays the acknowledgement boundary.

## Step 2: Add Contract Metadata

The user enters non-sensitive contract metadata.

### Where to find it in the app

| Tab | UI section or action | Notes |
| --- | --- | --- |
| Contracts | Create contract record | Use `New contract` when a selected contract is already open. |
| Contracts | Contract form | Enter the fields listed below, then select `Create contract` or `Update contract`. |

| UI field | Sample value |
| --- | --- |
| Contract number | SYN-DOD-SUB-2026-001 |
| Title | Synthetic DoD Subcontract 2026-001 |
| Agency or prime | Redacted Prime Contractor |
| Role | Subcontractor |
| Contract type | Fixed price |
| Status | Active |
| Awarded | 2026-07-15 |
| Start | 2026-08-01 |
| End | 2026-12-31 |
| FCI/CUI posture | FCI only |
| Place of performance | Remote / Redacted location |
| Description | Synthetic pilot contract used for readiness workflow demonstration. |

### Fields not currently shown in contract metadata

| Semantic field | Current app treatment | Recommendation |
| --- | --- | --- |
| Review owner | Not a Contract form field. Obligation ownership is assigned later from the obligation detail panel. | Do not claim this as contract metadata in the demo. Add a contract-level owner field only if customer discovery shows buyers need one accountable contract lead before obligations exist. |
| Security owner | Not a Contract form field. Security ownership is represented through obligation owner assignment and evidence workflow ownership. | Do not claim this as contract metadata in the demo. Add a contract-level security owner only if it is needed for routing, reminders, or reporting. |

## Step 3: Attach Or Review Clauses

This workflow is implemented in the `Contracts` tab under the `Attached clauses` section. The user attaches a published clause to the selected contract instead of typing an unsupported free-form clause tag.

The clause search helper is also visible in the `Obligations` tab as `Clause library search`, with the eyebrow label `Manual clause tagging`. Use it to find published clauses by clause number, title, or category before attaching them to a contract.

### Contracts tab fields

| UI field or action | Purpose |
| --- | --- |
| Attached clauses | Shows the number of clauses attached to the selected contract. |
| Published clause ID | Published clause identifier selected from the clause library. |
| Attachment reason | Why the clause is relevant to this contract. |
| Source document reference | Optional reference to the source contract document or section. |
| Attach clause | Saves the clause attachment to the selected contract. |
| Remove | Removes an attached clause when a removal reason is provided. |

### Obligations tab clause search fields

| UI field or action | Purpose |
| --- | --- |
| Clause library search | Search surface for published clauses. |
| Clause search | Search by clause number or title. |
| Category | Filter by published category such as FAR, DFARS, CMMC, Labor, Telecom, ByteDance, or Custom. |
| Search clauses | Runs the clause library search. |
| Select clause | Selects a mappable clause from the search results. |

| Clause family | Sample tag | Review status |
| --- | --- | --- |
| FAR safeguarding | Basic safeguarding workflow required | Needs review |
| DFARS cyber | Cyber incident handling workflow required | Needs review |
| CMMC readiness | Readiness tracking relevant | Needs review |
| Flow-down | Subcontractor review may be required | Needs review |

## Step 4: Generate And Review Obligation Records

After mapped clauses are attached to a contract, FeDril generates source-backed obligation records for review. Each obligation should include source family, trigger, required action, owner, evidence examples, risk level, confidence label, and review state.

### Where to find it in the app

| Tab | UI section or action | Notes |
| --- | --- | --- |
| Obligations | Obligation work queue | Shows tenant-scoped obligations after company profile, contract intake, and mapped clause attachment are in place. |
| Obligations | View details | Opens `Obligation detail` for the selected obligation. |
| Obligations | Obligation detail | Shows `Why it applies`, `Required action`, `Owner`, `Assignment`, `Source`, `Confidence`, `Last reviewed`, `Evidence examples`, `Flow-down`, `Expert review`, `Linked tasks`, and `Linked evidence`. |
| Dashboard | Priority obligations | Shows a high-level readiness view and selected priority obligations. |

| Obligation | Trigger | Required action | Owner | Due date | Review state |
| --- | --- | --- | --- | --- | --- |
| Maintain basic safeguarding workflow | Contract includes safeguarding requirement | Document responsible owner and evidence metadata | Security Lead | 2026-08-01 | Draft sample |
| Track cyber incident escalation path | Cyber incident workflow is contract-relevant | Record internal contact and escalation metadata | Security Lead | 2026-08-07 | Draft sample |
| Assign CMMC readiness owners | DoD supplier readiness workflow | Assign control-family owners and status | Operations Lead | 2026-08-15 | Draft sample |
| Review subcontractor flow-down applicability | Work may be subcontracted | Determine whether subcontractor tracking is required | Contracts Lead | 2026-08-20 | Draft sample |

## Step 5: Link Evidence Metadata

Evidence metadata should describe what exists, who owns it, where it is stored, and whether it is allowed under the No-CUI posture. Do not store prohibited content in FeDril.

### Where to find it in the app

| Tab | UI section or action | Notes |
| --- | --- | --- |
| Evidence | Evidence metadata | Create or update reusable evidence records. |
| Evidence | Evidence list | Select an existing evidence record or use `New evidence`. |
| Evidence | Obligations (optional) | Link evidence metadata to an obligation ID. |
| Evidence | Controls | Link evidence metadata to CMMC or NIST control IDs where applicable. |
| Evidence | Review classification | Reclassify selected evidence metadata after review. |

| Evidence metadata | Linked obligation | Allowed in MVP? | Notes |
| --- | --- | --- | --- |
| Synthetic policy excerpt | Maintain basic safeguarding workflow | Yes | Synthetic demonstration data. |
| Redacted training register | Maintain basic safeguarding workflow | Yes | Confirm no SSNs, payroll, health, or sensitive employee data. |
| Incident response contact placeholder | Track cyber incident escalation path | Yes | Placeholder only; no sensitive incident details. |
| External secure repository pointer | Assign CMMC readiness owners | Yes | Metadata pointer only; do not include CUI. |
| Real marked CUI document | Any obligation | No | Must be blocked or kept outside FeDril MVP. |

## Step 6: Update Workflow Status

Use the app's workflow-oriented obligation statuses. Avoid pass/fail, certified, compliant, or assessment-success labels.

### Where to find it in the app

| Tab | UI section or action | Notes |
| --- | --- | --- |
| Obligations | Obligation work queue | Select `View details` on an obligation. |
| Obligations | Update status | Choose a workflow status and select `Save status`. |
| Obligations | Assign by | Assign the obligation by `Tenant member` or `Role`, then save the assignment. |

| Status | Meaning |
| --- | --- |
| Open | Obligation exists and is waiting for owner action or review. |
| In progress | Owner assigned and work is underway. |
| Blocked | Work cannot proceed until an issue, missing input, or dependency is resolved. |
| Waiting for review | Work is ready for accountable-owner or reviewer review. |
| Done | Workflow item is complete for internal tracking purposes, not certified compliance. |
| Canceled | Workflow item is no longer active for this contract context. |

## Step 7: Generate Reports Or Evidence Package

Use the Reports tab to generate one of the current report artifacts:

### Where to find it in the app

| Tab | UI section or action | Notes |
| --- | --- | --- |
| Reports | Compliance status | Select `Generate status`. |
| Reports | CMMC readiness | Select an `Assessment`, then select `Generate readiness`. |
| Reports | Subcontractor compliance | Optionally choose a `Contract filter`, then select `Generate supplier report`. |
| Reports | Evidence package builder | Configure `Package title`, `Obligation`, `Contract`, `Control ID`, `Subcontractor`, and draft/rejected evidence inclusion before generating the package. |
| Reports | Generated this session | Shows generated report artifacts during the current session. |
| Reports | Approved evidence packages | Shows approved evidence package artifacts. |

- Compliance Status report.
- CMMC Readiness report.
- Subcontractor Compliance report.
- Evidence Package.

The selected report or package should show, where applicable:

- Contract workflow summary.
- Obligation matrix.
- Evidence metadata status.
- Owners and due dates.
- Open gaps.
- Audit history summary.
- Required No-CUI and no-legal-advice disclaimer.

## Step 8: Record Audit History

Audit history should capture compliance-relevant changes:

### Where to find it in the app

| Tab | UI section or action | Notes |
| --- | --- | --- |
| Settings | Audit log | Visible to users with audit-log permission. |
| Settings | Audit trail filters | Filter by `Actor ID`, `Action`, `Entity`, `From`, and `To`, then select `Filter`. |
| Settings | Tenant audit events | Review `Date`, `Actor`, `Action`, `Entity`, and `Summary`. |

- Data-handling acknowledgement.
- Contract record creation.
- Clause tag creation or update.
- Obligation creation or update.
- Evidence metadata attachment.
- Report generation or evidence package export.
- Status changes.

## Hidden Risks And Edge Cases

- A buyer may attempt to upload real CUI during a demo or pilot; the workflow must block or redirect the user.
- A readiness status can be misread as a certification decision; labels must remain workflow-oriented.
- Clause handling creates reviewer dependency; do not imply extraction candidates or generated obligations are final compliance determinations without review.
- Evidence metadata can still leak sensitive information if users paste prohibited details into descriptions.
- Advisor or counsel review is required before using this workflow as external customer-facing guidance.

## Pre-Publication Checklist

| Check | Pass condition |
| --- | --- |
| UI flow verified | The demo still matches `Contracts`, `Obligations`, `Evidence`, `Reports`, and `Settings` tab labels. |
| API enforcement verified | Any word such as `required`, `blocked`, or `disabled` maps to upload-related API enforcement or UI state. |
| Test evidence checked | No-CUI upload gating tests and contract document upload tests still pass or have been reviewed. |
| No-CUI posture preserved | Demo data is synthetic, redacted, or non-sensitive only. |
| Claims reviewed | No legal advice, CMMC certification, compliance guarantee, government endorsement, or secure CUI storage claim appears. |
| Counsel/product/security review | Required before sending this workflow as customer-facing guidance. |

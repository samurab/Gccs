# Security And Data Handling

Document type: customer-facing web page draft.

Review requirement: legal, contracting, and security owner review required before publication.

Publication status: draft page copy. Do not publish until security owner, product owner, and counsel approve the exact wording.

## Current-State Verification

| Claim | Status | Evidence |
| --- | --- | --- |
| FeDril is currently No-CUI / compliance management only. | Implemented posture | No-CUI notice, upload guardrails, and marketing asset boundary all use this posture. |
| Tenant-scoped workspaces and role-based access are part of the product model. | Implemented | API routes require permissions such as `ManageContracts`, `ManageEvidence`, `ManageReports`, and `ViewAuditLog`; tenant context is server-side. |
| No-CUI acknowledgement appears in contract and evidence workflows. | Implemented | `NoCuiAcknowledgementPanel` is rendered in current UI flows. |
| Upload-related workflows require acknowledgement and attestation where supported. | Implemented for contract document and evidence upload paths | Contract/evidence upload services and tests prove server-side blocking before acknowledgement. |
| Evidence metadata and report artifacts are supported. | Implemented | `Evidence` and `Reports` tabs expose these workflows. |
| Public security warranties, formal compliance status, or CUI-ready operation. | Do not claim | Not verified and outside MVP posture. |

## Page Hero

### Headline

Security and data handling for No-CUI compliance management.

### Supporting copy

FeDril helps small government contractors manage compliance workflows, obligations, tasks, evidence metadata, reports, and audit history under a No-CUI MVP posture. The product is designed for compliance management using synthetic, redacted, or non-sensitive data unless a future separately approved data posture is implemented.

## Current MVP Data Posture

FeDril is currently No-CUI / compliance management only.

Customers may use FeDril to manage:

- Company compliance profile metadata.
- Contract metadata.
- Source-backed obligations.
- Task ownership and due dates.
- Evidence metadata and allowed non-sensitive evidence.
- CMMC readiness workflow records.
- Subcontractor tracking records.
- Current report artifacts and audit history.

Customers must not upload, paste, import, attach, or process:

- Real CUI.
- Classified information.
- ITAR or export-controlled technical data.
- Sensitive government-furnished information.
- Credentials, passwords, secrets, private keys, or unrestricted security logs.
- Payroll records, SSNs, bank data, tax data, health data, disability data, or sensitive incident details.
- Production customer data unless separately approved as non-sensitive and in scope.

## What FeDril Does

- Tracks compliance work in tenant-scoped workspaces.
- Supports role-based access to protected tenant workflows.
- Maintains audit history for compliance-relevant activity.
- Displays No-CUI acknowledgement in contract and evidence workflows.
- Enforces acknowledgement and attestation guardrails on upload-related workflows where supported by the current application.
- Keeps obligation content source-backed and reviewable.
- Helps users organize evidence metadata and readiness status.

## What FeDril Does Not Do In The MVP

- Store or process real CUI.
- Store classified information or export-controlled technical data.
- Provide legal advice, accounting advice, labor determinations, or contracting-officer determinations.
- Certify CMMC compliance or guarantee assessment success.
- Replace a C3PAO, attorney, CPA, labor expert, contracting officer, or compliance advisor.
- Claim government approval, endorsement, or authorization.

## Customer Responsibilities

Customers are responsible for:

- Confirming that data entered into FeDril is allowed under the No-CUI posture.
- Keeping prohibited data out of notes, filenames, descriptions, uploads, tickets, screenshots, and support messages.
- Reviewing generated reports before external distribution.
- Involving qualified advisors where legal, contracting, cybersecurity, labor, SBA, or accounting judgment is required.
- Not treating readiness workflow status as certification, compliance determination, or official assessment status.

## Evidence Handling

FeDril can be used to track evidence metadata and allowed non-sensitive evidence for readiness workflows. Upload-related workflows require the current No-CUI acknowledgement and applicable attestation before accepted evidence file metadata or files are processed.

Acceptable examples:

- Synthetic policy excerpts.
- Redacted training completion metadata.
- Non-sensitive checklist records.
- Evidence location references that do not expose prohibited information.
- Owner, due date, status, and review metadata.

Prohibited examples:

- Real CUI-marked documents.
- Classified material.
- ITAR/export-controlled technical data.
- Passwords, secrets, private keys, or unrestricted security logs.
- Payroll, SSN, tax, banking, health, disability, or sensitive incident data.

## Incident And Support Handling

If prohibited data is suspected:

1. Stop adding data to the affected workflow.
2. Do not paste prohibited information into support tickets, email, screenshots, or chat messages.
3. Contact FeDril support with a non-sensitive description of the issue.
4. Follow the documented support escalation process.

## Future CUI-Ready Operation

Future CUI-ready operation would require a separately approved architecture, customer terms, shared responsibility matrix, support process, operational controls, and launch approval. The current MVP does not authorize real CUI handling.

## Current Report Artifacts

The current Reports tab supports Compliance Status, CMMC Readiness, Subcontractor Compliance, and Evidence Package artifacts. These artifacts are workflow aids and must not be described as legal conclusions, certification outcomes, assessor determinations, or government decisions.

## Required Footer Disclaimer

FeDril is a compliance management and readiness workflow tool. It does not provide legal advice, CMMC certification, assessment determinations, contracting-officer determinations, accounting advice, labor determinations, or government endorsement. The current MVP is No-CUI / compliance management only.

## Pre-Publication Checklist

| Check | Pass condition |
| --- | --- |
| Security owner review | Confirms data-handling, upload, support, and incident language is accurate. |
| Counsel review | Confirms disclaimers, customer responsibility, prohibited-data language, and liability-related language are acceptable. |
| Product verification | Confirms page claims match current UI/API behavior. |
| Enforcement language | `Requires`, `blocks`, `enforces`, and `guardrails` are used only for verified upload-related controls. |
| No-CUI posture | Page does not imply real CUI storage, secure CUI handling, or CUI-ready operation. |
| Public launch approval | Page is approved for publication before being placed on the landing page or shared externally. |

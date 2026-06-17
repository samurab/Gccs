# MVP Execution Plan

This artifact turns the product strategy into a launch plan for a CUI-ready MVP with gated CUI acceptance.

## Launch Gates

Tenant isolation, RBAC, audit logging, and CUI/data-handling implications are release-blocking controls. Use `docs/security-control-implications.md` when reviewing feature acceptance, security tests, support paths, imports, exports, reports, and launch readiness.

### Alpha

Alpha can begin when internal users can complete the core workflow end to end:

- Create tenant and users.
- Complete company profile.
- Create contract.
- Manually tag clauses.
- Generate obligations.
- Create tasks.
- Upload allowed evidence and demonstrate CUI-aware workflows with synthetic or redacted CUI examples.
- Generate a basic report.

### Beta

Beta can begin with 3-5 design partners when:

- Security review for tenant isolation and upload controls is complete.
- Initial obligation library is SME reviewed.
- Data classification warnings, CUI gating, and prohibited-data controls are visible in upload flows.
- Support playbook and escalation paths are documented.
- Backup restore has been verified in staging.

### MVP Launch

MVP launch can begin with 8-12 pilot customers when:

- Acceptance criteria for MVP modules pass.
- Release signoff is complete.
- Customer onboarding materials are ready.
- Production monitoring, backups, audit logs, and support workflow are live.
- Customer-facing product claims have been reviewed.

## MVP Data Policy

Allowed in default MVP and demo tenants:

- Company profile metadata.
- Contract metadata.
- Clause numbers and non-sensitive clause notes.
- Non-CUI policies, screenshots, checklists, attestations, training records, vendor documents, and audit notes.
- Evidence metadata and non-sensitive evidence files.
- Synthetic or redacted CUI demo artifacts.

Allowed only in approved CUI-ready tenants:

- Real customer CUI.
- CUI marking guides and contract packages containing CUI.
- Evidence files containing CUI.

Prohibited unless a separately approved deployment exists:

- Classified data.
- ITAR/export-controlled technical data.
- SSNs and government ID numbers.
- Payroll records and bank/tax details.
- Protected medical, disability, or accommodation data.
- Passwords, secrets, private keys, and unrestricted security logs.
- Sensitive incident details that would expand customer or platform regulatory obligations.

Required controls:

- Data handling acknowledgement during onboarding and before upload.
- Upload warning and tenant data handling status near the file picker.
- Metadata checkbox for suspected CUI or prohibited sensitive content.
- Block real CUI upload unless the tenant is approved as CUI-ready.
- Block upload when the user marks content as prohibited.
- Audit log for acknowledgement, upload, block, delete, and export events.
- Support escalation path for accidental CUI or prohibited uploads.

## Advisor Access Model

Consultants, MSPs, attorneys, and CPAs may support multiple clients only through explicit tenant membership.

Required boundaries:

- Advisor identity is global, but access is tenant-scoped.
- Advisor dashboards must group clients without mixing underlying tenant data.
- Advisors cannot copy evidence, reports, or contract data between tenants without explicit export/import authorization.
- Every advisor action is audit logged with advisor user ID and tenant ID.
- Client admins can revoke advisor access.
- Advisor permissions should default to least privilege.

## Import And Export Requirements

MVP import targets:

- Contracts.
- Clauses.
- Subcontractors.
- Tasks.
- Evidence metadata.

MVP export targets:

- Contract obligation matrix.
- Compliance status report.
- CMMC readiness report.
- Evidence package metadata and files.
- Subcontractor status report.
- Audit log export.

CSV imports must validate tenant ownership, required fields, data classification acknowledgements, and duplicate handling before records are created.

Exports must preserve source links, last-reviewed dates, evidence status, and audit metadata where applicable so customers can leave the platform with usable records.

## Module Acceptance Criteria

| Module | Done means |
| --- | --- |
| Tenant and RBAC | Tenant data is isolated; roles are enforced server-side; sensitive actions are audit logged. |
| Company profile | User can capture UEI, CAGE, SAM, NAICS, certifications, role, locations, and FCI/CUI posture; renewals create tasks. |
| Contract intake | User can create a contract, attach allowed files, acknowledge data handling rules, classify CUI posture, and manually tag clauses. |
| Obligation dashboard | User can see source-backed obligations by contract, risk, owner, due date, status, and source. |
| Compliance calendar | Tasks from obligations, renewals, deliverables, training, and evidence expirations appear with reminders and owners. |
| Evidence vault | User can upload allowed evidence, tag it, link it to obligations/controls/contracts/vendors, track status and expiration, and export packages. |
| CMMC readiness | User can track Level 1 and Level 2 readiness, map evidence, assign tasks, and see draft-only guidance with source references. |
| Subcontractor tracker | User can track profile, flow-downs, CMMC status, CUI access flag, insurance, NDAs, workshare, and evidence requests. |
| Reports | User can generate obligation matrix, compliance status, CMMC readiness, evidence package, subcontractor status, and audit log reports. |
| Obligation library | Content cannot publish without source URL, last reviewed date, trigger logic, confidence, review owner, and review state. |

## Support Escalation

| Escalation path | Examples | Owner |
| --- | --- | --- |
| Legal/compliance | Clause interpretation, certification language, disputed obligation, marketing claim review. | Compliance SME and legal/contracting advisor. |
| CMMC/security | CMMC readiness claims, CUI questions, ESP implications, incident response. | Security owner and CMMC SME. |
| Technical | Bugs, integrations, uploads, reports, performance, access issues. | Engineering lead. |
| Billing/account | Subscription, pilot terms, renewals, plan changes. | Customer success. |
| Data incident | Tenant isolation risk, accidental prohibited upload, evidence exposure. | Security owner with engineering and legal. |

## Content Test Set

Maintain a representative test corpus before automated extraction or AI features are enabled:

- Federal solicitations.
- Prime contracts.
- Subcontracts.
- Purchase orders.
- Flow-down attachments.
- Wage determinations.
- DD Form 254 metadata examples.
- CUI marking guide metadata examples.

The test set should measure clause extraction precision, clause extraction recall, missed flow-downs, missed reporting deadlines, false positives, data handling classification errors, and reviewer rejection reasons.

## Release Readiness Checklist

- CUI-ready gated positioning reviewed across product, docs, support, and sales copy.
- Tenant isolation and RBAC tests pass.
- Upload restrictions and audit logs verified.
- Obligation content has required source and review metadata.
- Customer-facing claims reviewed.
- Backup and restore verified.
- Support runbooks created.
- Pilot onboarding materials complete.
- Known risks accepted by accountable owners.

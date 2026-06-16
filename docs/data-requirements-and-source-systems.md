# Data Requirements And Source Systems

This document identifies the MVP data fields GCCS needs, the source system or input source for each field, and the current implementation status. It is a product and engineering contract, not legal advice.

The MVP remains **No-CUI / compliance management only**. Source documents, evidence, and notes must not contain CUI, classified data, export-controlled technical data, payroll records, SSNs, secrets, or other prohibited sensitive content unless a separately approved CUI-ready deployment exists.

## Source System Register

| Source system | Data supplied | MVP use | Integration posture |
| --- | --- | --- | --- |
| User-entered tenant data | Company profile, roles, locations, IT posture, task ownership, evidence metadata, subcontractor records | Primary MVP data entry path | Implemented through app/API workflows where modules exist |
| Customer contract package | Contract metadata, clauses, deliverables, reporting deadlines, flow-down attachments, wage determinations, CUI markings if present | Contract intake and obligation generation | Manual entry and non-CUI document metadata in MVP; automated extraction deferred |
| Source-backed obligation library | Clause numbers, trigger logic, required actions, evidence examples, source URLs, confidence, review state | Obligation dashboard, reports, clause search | Implemented as governed seed package and persistence-backed content |
| SAM.gov / GSA Entity API | UEI, entity registration status, SAM expiration, CAGE, legal entity metadata | Company profile verification and future profile assist | Identified source; direct API integration deferred |
| SBA size standards and certification references | NAICS size standards, small-business qualification support, socioeconomic certification references | Company profile and SBA-oriented readiness | Identified source; MVP stores user-entered status and references |
| Acquisition.gov FAR/DFARS | FAR and DFARS clause source text and URLs | Clause library and obligations | Used as source URLs in obligation content |
| eCFR | 32 CFR Part 170 and other regulatory references | CMMC obligation source and governance review | Used as source URLs in obligation content |
| DoD CMMC resources | CMMC program guidance and documentation references | CMMC readiness tracker and content review | Identified source; content requires SME review |
| NIST CSRC | NIST SP 800-171 references | CMMC/NIST readiness context | Identified source; content requires SME review |
| NARA CUI Registry | CUI category references | Data posture, CUI warnings, future CUI mapping | Identified source; MVP should not store CUI content |
| Tenant audit log | Actor, action, entity, timestamp, request metadata, summary | Traceability, reports, support review | Implemented append-only through application APIs |
| Internal system clocks and schedulers | Reminder dates, overdue state, renewal lead times | Calendar, notifications, renewal task generation | Partly implemented for task/calendar workflows |

## Required Data By MVP Workflow

| Workflow | Required fields for MVP completion | Source/input | Current implementation status |
| --- | --- | --- | --- |
| Tenant | Display name, status | Platform/admin entry | Implemented |
| User membership | User ID, email, display name, role, membership status, tenant ID | Tenant admin invitation or membership workflow | Implemented |
| User invitation | Email, role, expiration, token, status | Tenant admin entry and app-generated token | Implemented |
| RBAC role | Role name and permission set | GCCS role catalog | Implemented |
| No-CUI acknowledgement | Tenant ID, user ID, notice version, acknowledged timestamp | User acknowledgement and app policy | Implemented |
| Company profile | Legal entity name, UEI, CAGE code, SAM expiration, at least one NAICS code, contractor role, products/services, employee range, revenue range, at least one location, IT environment summary, FCI/CUI posture | User entry; future assist from SAM.gov/GSA and SBA sources | Implemented validation for profile completion |
| NAICS size status | NAICS code, title, primary flag, size standard, qualifies-as-small status | User entry; future assist from SBA size standards | Implemented in profile workflow |
| Certification | Type, status, issuer/reference when available, effective date, expiration date | User entry; future assist from SBA/certification records where available | Implemented in profile workflow |
| Contract record | Contract number, title, agency or prime, contractor relationship, contract type, status, place of performance, period of performance, data handling posture | User entry from solicitation, contract, subcontract, or purchase order | Implemented |
| Contract document metadata | Document type, file name, content type, size, prohibited-content flag, No-CUI acknowledgement version | User upload metadata and upload guardrails | Implemented as metadata/guardrail workflow |
| Contract clause attachment | Published clause library ID, attachment reason, source document reference when available | User manual tagging from contract package and governed clause library | Implemented |
| Contract deliverable | Name, owner function, due date/status when known | User entry from contract/SOW | Implemented |
| Obligation library item | Source, title, trigger condition, required actions, evidence examples, risk, owner function, source URL, last reviewed date, confidence, review state, flow-down flag | Governed compliance content package and SME review | Implemented for seeded content and publication metadata |
| Contract obligation | Contract clause ID, obligation ID, status, owner user or role when assigned, due date when applicable | Generated from clause mapping and user assignment | Implemented |
| Compliance task | Title, owner, linked entity type, linked entity ID for linked tasks, status, due date when applicable | Obligations, renewals, deliverables, evidence expiration, manual entry | Implemented |
| Calendar event | Source module, title, owner, status, due date/date range, risk when available | Derived from tasks, renewals, deliverables, evidence, and CMMC records | Implemented |
| Evidence metadata | Title, evidence type, owner, approval status, tags, expiration date when applicable, linked obligation/control/contract/vendor/subcontractor IDs when applicable | User entry and approved non-CUI evidence records | Implemented |
| Evidence file metadata | File name, content type, file size, validation status, malware scan placeholder status, storage URI when enabled | Upload workflow and object storage adapter | Implemented as No-CUI guarded metadata; production storage maturity still pending |
| Evidence approval | Decision, reviewer, reviewed timestamp, rejection/request-changes reason when applicable | Authorized reviewer action | Implemented |
| CMMC assessment | Name, framework, Level 1 or Level 2, owner, status, assessment dates, linked company/contract scope when available | User entry; source context from CMMC/NIST/32 CFR references | Implemented |
| CMMC control readiness | Assessment ID, control ID, status, notes, linked evidence/tasks/assets/POA&M when available | CMMC baseline content and user readiness tracking | Implemented |
| POA&M item | Control ID, gap/weakness, remediation plan, owner, due date/status, risk | User entry from CMMC readiness review | Implemented |
| Annual affirmation | Assessment ID, due/submitted dates, status, evidence links when available | User entry and CMMC readiness workflow | Implemented |
| Subcontractor profile | Legal name, role, point of contact when available, status, CUI/export-control access flags, workshare percentage, contract links | User entry from supplier/subcontract records | Implemented |
| Flow-down clause | Clause number, title, status, due/sent/acknowledged/signed dates when applicable | User entry from subcontract/flow-down package and clause library | Implemented |
| Subcontractor evidence request | Requested evidence item, at least one evidence type, due date/status, received evidence reference when satisfied | User entry and subcontractor workflow | Implemented |
| Reports | Report type, generated by, generated timestamp, source scope, included contracts/obligations/evidence/subcontractors as applicable | GCCS workflow data and audit trail | Implemented for MVP reports |
| Audit log | Tenant ID, actor, action, entity type/id, timestamp, summary, request metadata, structured metadata | Application-generated event writer | Implemented |

## Field Source Rules

- User-entered fields must be tenant-scoped, validated, and audit logged when they affect compliance posture, access, evidence, reports, or customer-facing status.
- Source-backed compliance content must include source name, source URL, last reviewed date, confidence, review state, owner/reviewer metadata, and expert-review flag before publication.
- Derived fields such as completion percentage, overdue state, renewal reminders, report status, and CMMC progress must be reproducible from stored source records.
- Imported or extracted fields must preserve provenance: source document, imported file/API, extraction timestamp, confidence, and human review state.
- Customer-facing reports must expose source links and last-reviewed dates for obligation content and must distinguish tenant-entered facts from GCCS-governed content.
- Any field that could indicate CUI, export-controlled data, classified data, payroll/PII, or security secrets must trigger No-CUI warnings or blocking controls in the MVP.

## Deferred Source Integrations

| Integration | Why deferred | Required before enabling |
| --- | --- | --- |
| SAM.gov/GSA Entity API lookup | MVP can start with user-entered profile data | API credentials/config, rate-limit handling, provenance metadata, stale-data warnings |
| SBA size helper | Size determinations can carry business/legal risk | Source-update process, calculation assumptions, SME/legal review, customer-facing disclaimer |
| Automated contract/clause extraction | Manual tagging is safer for MVP validation | Extraction evaluation set, precision/recall targets, confidence labels, human review workflow |
| Wage determination lookup | Labor compliance has high complexity and risk | DOL/source integration design, labor SME review, data retention policy |
| SPRS/CMMC external status import | Assessment/readiness claims require careful controls | Customer authorization, source limitations, CMMC SME review, audit trail |
| CUI category mapping from NARA registry | MVP is No-CUI and must not invite CUI upload | CUI-ready architecture decision, intake controls, support process, shared responsibility matrix |

## Release Checklist

- Every MVP module has a documented required-field set in this file or a linked API schema.
- Every external source has a named system, owner, update/review cadence, and limitation note before customer-facing use.
- Every customer-visible obligation includes source URL, last reviewed date, confidence, review state, and owner/reviewer metadata.
- Every report identifies whether data came from tenant entry, governed content, derived workflow state, or future external integration.
- No-CUI controls are present anywhere users upload, import, paste, or describe source documents/evidence.

# Production Readiness Frozen Launch Scope

Scope status: Frozen.

Freeze date: 2026-06-26.

Scope owner: Product owner.

Technical approver: Engineering lead.

Launch posture: No-CUI / compliance management only with synthetic CUI-ready demonstration workflows. Real customer CUI remains prohibited unless a future `CuiReady` posture is separately approved.

## Frozen MVP Launch Scope

The production launch candidate is limited to launch-critical MVP modules that support pilot readiness, No-CUI operations, tenant isolation, RBAC, auditability, source-backed compliance workflow guidance, and supportable evidence collection.

| Module | Launch-critical scope | Explicitly excluded from launch scope |
| --- | --- | --- |
| Tenant and RBAC | Tenant creation, tenant-scoped access, role permissions, direct API denial behavior, and audit-relevant access events. | Enterprise SSO/SAML, SCIM, cross-client advisor analytics beyond scoped tenant access, and private cloud identity integrations. |
| Company profile | Legal entity metadata, UEI, CAGE, SAM expiration, NAICS, certifications, agency/customer role, locations, and FCI/CUI posture metadata. | Automated SAM.gov enrichment beyond approved lookup behavior, advanced size-standard decisioning, and legal determinations. |
| Contract intake | Contract metadata, allowed document metadata upload, No-CUI acknowledgement, manual clause tagging, classification controls, and blocked real CUI upload. | Production automated clause extraction as an unreviewed dependency, real CUI contract uploads, classified packages, and uncontrolled OCR/AI processing. |
| Obligation dashboard | Source-backed obligation display, owner, status, risk, evidence needs, review metadata, and task linkage. | New unreviewed obligation categories, unsupported legal interpretations, and customer-facing high-risk content without approval. |
| Compliance calendar | Tasks from obligations, renewals, deliverables, evidence expirations, reminders, and status tracking. | Advanced workflow automation that changes due dates, legal obligations, or customer commitments without review. |
| Evidence vault | Metadata, allowed uploads, tagging, expiration, status, obligation/control/contract/vendor links, approval states, and evidence package generation. | Real CUI evidence storage, unmanaged file scanning exceptions, unrestricted bulk import/export, and sensitive personal data storage. |
| CMMC readiness | Level 1 and Level 2 readiness tracking with draft-only, source-backed guidance and evidence mapping. | SSP builder, SPRS score calculator, official pass/fail language, assessment success claims, and CMMC certification determinations. |
| Subcontractor tracker | Subcontractor profile, flow-down status, CMMC status metadata, CUI access flag, insurance/NDA/workshare metadata, and evidence requests. | Automated prime portal workflows, uncontrolled cross-tenant sharing, and legal determinations on subcontractor eligibility. |
| Reports and exports | Obligation matrix, compliance status, CMMC readiness, evidence package, subcontractor status, and audit log reports with tenant scope, RBAC, and claim controls. | Reports that certify compliance, make legal determinations, imply government endorsement, or export real CUI from No-CUI tenants. |
| Source-backed obligation library | Initial MVP obligation records with source URL, last reviewed date, confidence, review owner, review state, and expert-review flags. | Publishing high-risk records that remain `needs_review` or lack required source/review metadata. |
| Support and launch operations | Support paths, known limitations, launch blockers, rollback notes, backup/restore evidence, malware scanning decision, monitoring, and approval package. | Public launch, broad self-service onboarding, or production rollout without required approvals. |

## Deferred Phase 2+ Scope

Phase 2 or later work is deferred unless the product owner and engineering lead record evidence that it removes a production blocker.

| Deferred scope | Phase | Launch disposition | Approval needed to add to launch |
| --- | --- | --- | --- |
| Automated clause extraction beyond controlled staging/demo verification | Phase 2 | Deferred | Product owner and engineering lead approval with tenant-mode, RBAC, audit, and extraction test evidence. |
| AI assistant, RAG, or generated compliance advice | Phase 3 | Deferred | Product owner, engineering lead, security owner, compliance content owner, and legal/contracting advisor approval. |
| SSP builder and SPRS score calculator | Phase 3 | Deferred | Product owner and engineering lead approval plus qualified advisor validation. |
| eSRS support and advanced labor compliance | Phase 3 | Deferred | Product owner and engineering lead approval plus content/legal review. |
| Prime contractor portal and auditor portal expansion | Phase 3 | Deferred | Product owner and engineering lead approval plus tenant isolation and RBAC evidence. |
| Enterprise SSO/SAML, SCIM, GovCloud, FedRAMP readiness, private cloud, customer-managed keys, and higher-assurance CUI enclave | Phase 4 | Deferred | Separate regulated deployment approval and security/legal review. |
| Production `CuiReady` real-CUI acceptance | Future gated posture | Excluded from launch | Separate `CuiReady` approval gate with architecture, customer terms, shared responsibility matrix, support model, operating controls, and required signoff. |

## Known Limitations For Launch Notes

- MVP production launch is No-CUI / compliance management only.
- Real customer CUI, classified data, ITAR/export-controlled technical data, SSNs, payroll, bank/tax data, protected health or disability data, credentials, unrestricted security logs, and sensitive incident details remain prohibited unless a separately approved deployment posture exists.
- Synthetic or redacted CUI examples are permitted only for approved demo workflows and must not be presented as authorization to process customer CUI.
- Malware scanning requires either an enabled production scanner or a formally approved launch exception with compensating controls.
- Compliance content is workflow guidance, not legal advice, certification, assessment success, or government endorsement.
- Phase 2+ automation, AI, extraction, portal, GovCloud, and CUI-ready behavior is deferred unless added through the scope-change approval gate.

## Scope-Change Approval Gate

Any launch scope addition requires:

| Required item | Required evidence |
| --- | --- |
| Product owner approval | Written launch-scope decision explaining customer value and why the item cannot wait. |
| Engineering lead approval | Written impact assessment for architecture, tests, migrations, deployment, rollback, and supportability. |
| Security and compliance review when data handling changes | Tenant-mode, RBAC, audit logging, tenant isolation, upload/import/export/report/search/AI/extraction impact review. |
| Test mapping update | Updated `TC-*` mapping and passing targeted tests or documented staging/manual evidence. |
| Known-risk update | Launch blocker, accepted risk, deferred follow-up, or release note update with owner and target date. |

New scope is rejected by default until the gate evidence is complete.

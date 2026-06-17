# MVP Roadmap

## Phase 0 - Research and Validation

- Persona map.
- Regulatory obligation map.
- Competitive matrix.
- Clickable prototypes.
- 20-30 customer and expert interviews.
- Pricing hypothesis.
- MVP requirements.
- Advisor review.

## Phase 1 - MVP

- Tenant, user, and RBAC foundation.
- React + Vite authenticated application shell backed by the ASP.NET Core API.
- CUI-ready data posture foundation with tenant-level CUI gating, onboarding acknowledgement, upload guardrails, and demo/sandbox support for synthetic CUI workflows.
- Company profile.
- Contract upload and manual clause tagging.
- Obligation dashboard.
- Compliance calendar.
- Evidence vault.
- Basic CMMC Level 1 and Level 2 readiness tracking without SSP generation or SPRS scoring.
- Subcontractor flow-down tracker.
- Reports.
- Notifications.
- Audit log.
- Source-backed obligation library with FAR, DFARS, CMMC, SBA, and initial high-frequency sources.

## Phase 1A - CUI Readiness Gate

This is a readiness track inside Phase 1, not a separate product phase. It must be completed before any production tenant can upload real customer CUI.

- Tenant data handling modes: `DemoSandbox`, `NoCui`, and `CuiReady`.
- Data classification controls for uploads, notes, reports, extraction jobs, and evidence.
- Synthetic CUI demo dataset and seeded CUI workflow examples.
- CUI-ready tenant approval checklist.
- Shared responsibility matrix baseline.
- Customer-facing data handling notice for CUI-ready and non-CUI tenants.
- Support escalation path for accidental CUI upload, suspected CUI, and prohibited data.
- Audit events for data handling mode changes, CUI classification, upload blocks, approvals, downloads, exports, and deletions.
- Security review covering tenant isolation, evidence storage, encryption, malware scanning, retention, backup, restore, admin access, and incident response.

## Phase 2 - Govcon Intelligence

Detailed delivery backlog: `docs/development-phase-use-cases.md`, sections 18-28.

- Automated clause extraction.
- Human review workflow for extracted clauses and AI-suggested obligations.
- Clause library.
- Applicability engine.
- SAM.gov entity lookup.
- SBA size helper.
- Subcontractor tracker.
- Policy templates.
- Evidence request workflows.
- CMMC Level 2 readiness.
- Content test set for extraction precision and recall.
- Extraction and AI workflows respect tenant data handling mode and must not process real CUI unless the tenant is approved for CUI-ready operation.

## Phase 3 - Advanced Compliance

- SSP builder.
- SPRS score calculator.
- eSRS support.
- Labor compliance module, if pilot/customer demand justifies it.
- AI assistant with citations, logging, and human-review guardrails.
- Prime contractor and auditor portals.

## Phase 4 - Enterprise / Regulated Deployment

- SSO/SAML and SCIM.
- GovCloud or government cloud deployment path.
- FedRAMP readiness package if selling directly to federal agencies.
- Higher-assurance CUI enclave, customer-managed keys, and GovCloud deployment path, if approved.

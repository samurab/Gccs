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
- No-CUI data posture foundation with tenant-level mode enforcement, onboarding acknowledgement, upload guardrails, and demo/sandbox support for synthetic CUI-ready demonstration workflows.
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
- Future `CuiReady` tenant approval checklist.
- Shared responsibility matrix baseline.
- Customer-facing data handling notice for future `CuiReady` and No-CUI tenants.
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
- Extraction and AI workflows respect tenant data handling mode and must not process real CUI unless the tenant is approved for future `CuiReady` operation.

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
- **Planned - SOC 2 assurance program:** Define the FeDril production-system scope, assign control owners, operate and collect evidence for applicable controls, and complete an independent SOC 2 examination. An issued report may serve as an enterprise procurement or availability gate when required by the target customer segment.
    1. Complete an auditor-supported scope and readiness assessment.
    2. Remediate control gaps and collect operating evidence.
    3. Complete a SOC 2 Type I examination if commercially justified.
    4. Complete a SOC 2 Type II examination after the required operating period.
    5. Establish controlled report distribution and an ongoing examination-renewal process.

SOC 2 planning or an issued report does not authorize CUI storage or establish CMMC certification, FedRAMP authorization, government approval, or government endorsement. Customer-facing material must not describe FeDril as "SOC 2 certified," "SOC 2 compliant," or "audit ready." Claims must identify the actual report type, covered system, and examination period and must be reviewed before publication.

## FedRAMP Decision And Readiness Track

Current state: **Foundation partially implemented; certification program not activated**. FeDril is not represented by this roadmap as FedRAMP Certified, FedRAMP authorized, FedRAMP Moderate equivalent, or approved to process real customer CUI. Existing government-cloud configuration, control-mapping, trust-artifact, SSP, and readiness-package features are internal readiness workflow capabilities only; they are not evidence that the deployed FeDril cloud service offering has completed an independent assessment or government authorization.

### Activation Gates

Do not start a full FedRAMP certification program solely as an MVP feature. Activate the funded readiness and certification track when at least one of these conditions is validated:

- A federal agency intends to procure or sponsor FeDril for an in-scope federal use case.
- A federal customer requires FeDril to create, collect, process, store, or maintain federal information inside a FedRAMP boundary.
- FeDril will become an in-boundary dependency of another FedRAMP cloud service offering.
- The approved product strategy changes to permit FeDril to store, process, or transmit CUI for DoD contractors, requiring the applicable FedRAMP Moderate authorization or DoD equivalency path before that use begins.

Until an activation gate is met, preserve the No-CUI MVP boundary and implement only reusable security and operational foundations that reduce future retrofit cost.

### Planned Backlog

| ID | Timing | Status | Backlog item | Completion evidence |
| --- | --- | --- | --- | --- |
| FR-0 | Phase 1 through Phase 3 | Planned | Maintain a documented FedRAMP decision record covering target customers, federal use case, expected information types, likely impact level or certification class, budget, owner, and activation-gate status. Review it at each phase gate. | Approved decision record with dated reviews and an explicit proceed/defer decision. |
| FR-1 | Phase 1 | Partially implemented | The legacy client Boolean is retained temporarily for compatibility but ignored, and readiness-package language always fails closed. A restricted governance record backed by official evidence, reviewer identity, scope, effective/expiry dates, history, and audit events remains planned before any positive claim can exist. | Current true-Boolean API test proves tenant administrators cannot produce positive authorization wording. Future completion requires governance allow/deny, expiry, concurrency, and rollback evidence. |
| FR-2 | Phase 1 through Phase 2 | Partially implemented | Control mappings, gaps, evidence links, readiness packages, included-record snapshots, and lifecycle history now have durable tenant-scoped EF persistence, optimistic concurrency, and transactional audit writes. Trust and SSP artifacts remain to be migrated. | Additive migration; restart and tenant-isolation tests; PostgreSQL concurrency and audit-rollback tests. Complete when trust and SSP repositories have equivalent evidence. |
| FR-3 | Phase 1 through Phase 3 | Planned | Preserve reusable security foundations: tenant isolation, server-side RBAC, MFA-capable identity, least-privilege administration, encryption, secrets management, immutable audit history, vulnerability and dependency scanning, SBOM inventory, hardened configuration, backup/restore rehearsal, incident response, change control, and evidence retention. | Control-owner matrix and operating evidence from the commercial No-CUI environment; no claim that this evidence establishes FedRAMP status. |
| FR-4 | Phase 2 through Phase 3 | Planned | Define the prospective cloud service offering and minimum assessment boundary, including application components, data flows, information resources, administrators, subprocessors, inherited cloud controls, external connections, customer responsibilities, and prohibited data. | Reviewed boundary diagram, resource inventory, data-flow inventory, supplier register, and shared-responsibility matrix. |
| FR-5 | Phase 3, after an activation gate | Planned | Select the applicable current FedRAMP certification profile and path with qualified FedRAMP counsel or an advisor and a FedRAMP-recognized independent assessment service. Do not assume that GovCloud hosting determines the certification class or establishes authorization. | Approved target profile, path, scope, assessor engagement, program plan, cost estimate, and schedule. |
| FR-6 | Phase 3 through Phase 4 | Partially implemented foundation | Commercial production now has real Terraform declarations, import blocks, CI validation, and a gated drift workflow. Remote-state adoption, imports, a reviewed zero-drift baseline, hardening, policy-as-code, and a separate regulated deployment remain planned. Current resource settings are discovery/adoption inputs, not approved controls. | Current static `fmt`/`validate` evidence. Completion requires secure state, successful imports, zero-drift and recurring drift evidence, reproducible regulated deployment, configuration tests, inherited-control evidence, and independent review. |
| FR-7 | At least 6-12 months before the intended application, adjusted to current rules | Planned | Run a formal gap assessment and begin persistent control verification and security-metric collection early enough to meet the selected certification profile's historical-evidence requirements. Track remediation owners, target dates, exceptions, accepted risks, and significant changes. | Current control matrix, remediation register, persistent verification history, vulnerability-response evidence, incident/contingency exercises, and assessor-reviewed readiness result. |
| FR-8 | Phase 4 | Planned | Produce the required human-readable and machine-readable certification package, secure-configuration guidance, policies, plans, assessment materials, and customer-responsibility documentation from governed evidence. | Package completeness validation against the then-current official FedRAMP rules and independent-assessor feedback. |
| FR-9 | Phase 4 | Planned | Complete the applicable Marketplace, independent-assessment, Program, and/or agency authorization process. Customer-facing status language must match the official status and approved scope exactly. | Official Marketplace status, applicable certification or agency authorization evidence, approved claims register, and scoped customer communication. |
| FR-10 | Phase 4 and ongoing | Planned | Operate continuous monitoring, package maintenance, vulnerability response, incident reporting, access review, evidence refresh, significant-change review, supplier monitoring, and recurring independent assessment at the cadence required by the selected current profile. | Recurring evidence schedule, accountable owners, completed monitoring cycles, submitted reports, tracked findings, and maintained official status. |

Execution and assessment artifacts for the foundation and evidence work:

- `docs/fedramp-foundation-execution-prompt.md`
- `docs/fedramp/README.md`
- `docs/fedramp/control-evidence-register.md`
- `docs/fedramp/evidence-collection-plan.md`
- `docs/fedramp/remediation-backlog.md`

### Rationale

- FedRAMP evaluates a specific deployed cloud service offering and its complete operational boundary. Application features that track controls or generate readiness packages do not establish certification or authorization.
- Full certification before validated federal demand would consume substantial assessment, infrastructure, policy, personnel, and continuous-monitoring effort while the current target market and production posture remain No-CUI contractor workflows.
- Deferring all security preparation until Phase 4 would create an expensive retrofit. Identity, tenant isolation, auditability, infrastructure reproducibility, evidence durability, vulnerability management, incident response, and recovery should mature earlier because they benefit the MVP and are difficult to add after architecture and operations stabilize.
- A government-cloud region or FedRAMP-authorized infrastructure provider supplies only inherited controls. FeDril remains responsible for its application, configuration, personnel, operating procedures, suppliers, customer responsibilities, evidence, and assessment boundary.
- Certification profiles and submission rules change. The target profile, required artifacts, historical-evidence period, and assessment path must be revalidated against the official rules when an activation gate is reached and again before application.
- If FeDril will process real CUI for DoD contractors, the No-CUI posture cannot be relaxed merely because readiness work has started. The applicable FedRAMP Moderate authorization or DoD equivalency requirements and contractual incident obligations must be satisfied and independently evidenced before that production use is permitted.

### Claim And Release Guardrail

Before publishing any FedRAMP-related product, sales, security, procurement, or readiness statement, verify all of the following:

- The statement identifies whether the capability is `Implemented`, `Partially implemented`, `Planned`, or `Do not claim`.
- The claimed behavior exists in the deployed environment, is server-enforced where enforcement language is used, and has current test or operating evidence.
- The statement distinguishes internal readiness tracking from the official status of the FeDril cloud service offering.
- Any certification, authorization, equivalency, Marketplace, or government-approval wording matches current official evidence and the exact assessed boundary.
- The statement preserves the No-CUI product posture unless a separately approved and assessed deployment has formally replaced it.

# Software Delivery Plan

This plan turns GCCS into a practical, shippable No-CUI government contractor compliance SaaS for small U.S. businesses. It is product and engineering guidance, not legal advice. Production obligation content must be reviewed by qualified government contracts, cybersecurity, labor, CMMC, SBA, or finance experts as applicable.

## 1. Delivery Objectives

### Product Goal

Help small government contractors know what applies, prove what they did, and stay ready for audits, renewals, bids, certifications, and prime contractor reviews.

### MVP Promise

The first release should provide a reliable compliance operating workspace for:

- Company compliance profile.
- Contract and clause intake.
- Source-backed obligation dashboard.
- Compliance calendar.
- Evidence vault.
- CMMC Level 1 and Level 2 readiness tracking.
- Subcontractor flow-down tracking.
- Basic reporting.

### MVP Posture

The MVP is **No-CUI / compliance management only**. Users must be warned and technically prevented from intentionally uploading CUI until a CUI-ready architecture, shared responsibility matrix, customer terms, operating controls, and assessment posture are complete.

Allowed No-CUI MVP data includes company profile metadata, contract metadata, clause references, non-sensitive policies, screenshots, checklists, attestations, training records, vendor documents, and evidence metadata. Prohibited data includes CUI, classified data, ITAR/export-controlled technical data, SSNs, payroll records, protected medical or disability data, secrets, private keys, and unrestricted security logs unless a separately approved deployment posture exists.

## 2. Team And Cadence

### Suggested Team

- Product owner: owns roadmap, acceptance decisions, pricing assumptions, and customer discovery.
- Delivery lead / scrum master: owns sprint planning, dependency tracking, ceremonies, and release coordination.
- Technical lead: owns architecture, engineering standards, implementation decisions, and code review quality.
- Backend engineer: ASP.NET Core APIs, domain model, persistence, auth, integrations, jobs.
- Frontend engineer: React workspace, UX workflows, accessibility, dashboard and form ergonomics.
- QA engineer: test plans, regression coverage, release verification, exploratory testing.
- DevOps / security engineer: CI/CD, environments, observability, secrets, backups, hardening.
- Compliance content owner: obligation library, source references, review workflow, expert coordination.
- Subject matter reviewers: govcon attorney, CMMC RP/assessor, labor compliance specialist, CPA as needed.

### Delivery Cadence

- Sprint length: 2 weeks.
- Release train: demo every sprint, production release every 1-2 sprints once the MVP foundation is stable.
- Backlog refinement: weekly.
- Sprint planning: first day of sprint.
- Daily standup: 15 minutes.
- Sprint review: working software demo against acceptance criteria.
- Retrospective: actionable process improvements only.
- Content review board: biweekly during MVP, monthly after launch.

## 3. Requirements

### Functional Requirements

#### Company Profile

- Capture legal entity name, UEI, CAGE code, SAM expiration, NAICS codes, SBA size status by NAICS, socioeconomic certifications, agency customers, prime/sub role, products/services, employee and revenue ranges, locations, IT summary, and FCI/CUI handling posture.
- Track profile completeness and missing data.
- Support source-backed reminders for expirations and renewals.

#### Contract Intake

- Support manual contract creation for solicitations, contracts, subcontracts, purchase orders, statements of work, wage determinations, flow-down attachments, DD Form 254 metadata, and CUI marking guide metadata.
- Support file upload in No-CUI mode with clear prohibited content messaging.
- Capture agency or prime, contract number, period of performance, contract type, place of performance, clauses, deliverables, reporting deadlines, data handling requirements, labor requirements, and flow-down obligations.
- MVP starts with manual clause tagging; automated extraction comes later.

#### Obligation Engine

- Map clauses and rules to obligations using a curated obligation library.
- For each obligation, show plain-English summary, source link, trigger logic, owner role, required action, evidence needed, flow-down requirement, risk level, due date, confidence, last reviewed date, and expert review flag.
- Distinguish product workflow guidance from legal determinations.
- Require source references for all published obligations.

#### Compliance Calendar

- Track SAM renewal, certification renewals, CMMC affirmation, insurance certificates, training, policy reviews, subcontractor expiration dates, contract deliverables, and option period notices.
- Defer SPRS score calculation, eSRS integration, and wage determination update automation to later phases unless pilot customers make them launch blockers.
- Allow tasks to be linked to obligations, contracts, controls, subcontractors, or evidence.
- Support due dates, reminders, assignment, status, priority, and audit trail.

#### Evidence Vault

- Store evidence items with tags instead of rigid folders.
- Link evidence to obligations, controls, contracts, vendors, subcontractors, employees, and reports.
- Track version, expiration date, owner, approval status, and audit history.
- Support read-only evidence package generation.
- MVP must include No-CUI warning, malware scanning path, allowed file types, and upload size limits.

#### CMMC / NIST Workspace

- Track CMMC Level 1 self-assessment.
- Track Level 2 readiness using NIST SP 800-171 Rev. 2 mapping for the CMMC program baseline while acknowledging customer questions about Rev. 3.
- Map controls to evidence, tasks, POA&M items, responsible parties, asset groups, system boundary notes, and external service provider responsibilities.
- Support annual affirmation tracking.

#### Subcontractor Management

- Track subcontractor profile, role, small business status, required flow-down clauses, CMMC status, insurance, NDAs, CUI access flag, export-control status, workshare percentage, and evidence requests.
- Link subcontractors to contracts and obligations.
- Generate a subcontractor compliance status report.

#### Reporting

- Generate compliance status report.
- Generate contract obligation matrix.
- Generate CMMC readiness report.
- Generate prime contractor evidence package.
- Generate SAM/SBA profile report.
- Generate subcontractor compliance report.
- Include source links and last-reviewed dates where compliance content appears.

### Non-Functional Requirements

#### Security

- MFA-ready authentication.
- Tenant isolation at every API and persistence boundary.
- RBAC for owner, admin, compliance manager, contributor, auditor, and advisor roles.
- TLS everywhere.
- Encryption at rest for database and object storage.
- Secrets in a secrets manager.
- Immutable audit logs for sensitive actions.
- Least-privilege service accounts.
- Malware scanning for uploads.
- Explicit No-CUI content controls.

#### Reliability

- Health checks for API, database, cache, storage, queue, and worker.
- Daily backups in production.
- Point-in-time recovery for production database.
- Restore test before GA and quarterly after GA.
- Graceful failure for background jobs.
- Idempotent notification and report jobs.

#### Performance

- Dashboard API responses under 500 ms for typical tenant data.
- Evidence metadata search under 1 second for typical MVP tenants.
- File upload progress and resilient retry behavior.
- Initial page load optimized for authenticated dashboard use.

#### Usability

- Workflow-first UI, not a marketing page.
- Clear ownership, due dates, and next actions.
- No dense legal walls without plain-English summaries.
- Source links and expert-review flags visible in context.
- Accessible forms and dashboards, including keyboard navigation and semantic labels.

#### Compliance Content Governance

- Every obligation record must have source name, source URL, last reviewed date, trigger conditions, required actions, evidence examples, risk level, confidence, and review state.
- Clauses must preserve text version, effective date when known, source URL, source hash when a source snapshot exists, review owner, review state, and superseded/replaced status.
- Human review states must cover draft, needs review, approved, rejected, customer disputed, published, and retired content.
- High-risk content requires expert review before production publication.
- CMMC, FAR, DFARS, SBA, and labor content should be monitored monthly.
- Customer-facing material changes should produce release notes.

## 4. Architecture

### Current Stack

- Frontend: React + Vite authenticated SaaS app in `apps/web`.
- Backend: ASP.NET Core API in `apps/api`.
- Domain: framework-independent compliance model in `src/Gccs.Domain`.
- Application layer: use cases, ports, and DTOs in `src/Gccs.Application`.
- Infrastructure: persistence, storage, queue, search, external APIs in `src/Gccs.Infrastructure`.
- Compliance content: source-backed obligation seed data in `packages/compliance-content`.
- Local services: PostgreSQL, Redis, MinIO, and ClamAV placeholders via Docker Compose.

### Target MVP Architecture

```text
React + Vite Web App
        |
ASP.NET Core API
        |
Application Services
        |
Domain Model
        |
Infrastructure Adapters
        |
PostgreSQL + Object Storage + Redis + Queue + Search
```

### Architectural Principles

- Tenant ID must be present and enforced in all tenant-owned data access.
- Domain entities should not depend on web, database, or cloud SDKs.
- Compliance content should be versioned and source-backed.
- Background work should be queued for upload scanning, document processing, notification delivery, report generation, and future extraction.
- AI features must be deferred until source-backed workflows and audit logging are mature.
- CUI storage must remain disabled until a separate CUI-ready architecture is designed and assessed.

### Key Data Domains

- Identity and access: tenant, user, role, invitation, membership.
- Company profile: profile, NAICS, certification, location, agency customer.
- Contracts: contract, document metadata, clause, deliverable, deadline.
- Obligations: source reference, obligation, applicability dimension, task, owner.
- Evidence: evidence item, version, tag, link, approval, expiration.
- CMMC: assessment, control, POA&M item, asset, system boundary, affirmation.
- Subcontractors: organization, flow-down clause, evidence request, status.
- Reporting: report run, export artifact, report snapshot.
- Operations: audit log, notification, job, content publication.

## 5. Development Plan

### Phase 0: Discovery And Delivery Setup, 4-6 Weeks

Deliverables:

- Persona map and customer interview notes.
- Regulatory obligation map for MVP clauses and CMMC baseline.
- Competitive matrix.
- Clickable prototypes for core workflows.
- Delivery backlog with estimates.
- Architecture decision records for No-CUI posture, tenancy, file storage, and content governance.
- Definition of Ready (`docs/definition-of-ready.md`) and Definition of Done.
- Initial CI pipeline with build, lint, and tests.

Exit criteria:

- MVP scope approved.
- Top 20-30 obligations identified and source-backed.
- Prototype validated with target users.
- Security and No-CUI posture approved by product and technical lead.

### Phase 1: MVP Foundation, 10-14 Weeks

Deliverables:

- Tenant, user, invitation, and RBAC foundation.
- Company profile workflow.
- Contract intake and manual clause tagging.
- Obligation dashboard backed by curated content.
- Task calendar.
- Evidence vault with metadata and No-CUI controls.
- Basic CMMC Level 1 and Level 2 readiness tracker.
- Subcontractor tracker.
- Basic reports.
- Audit log.
- Notifications.

Exit criteria:

- One pilot tenant can onboard, enter company and contract data, map clauses to obligations, upload non-CUI evidence, track tasks, and generate reports.
- All MVP workflows have acceptance tests and release verification.
- Production-like staging environment is operational.

### Phase 2: Govcon Intelligence, 8-12 Weeks

Deliverables:

- Automated clause extraction with human review.
- Expanded clause library and applicability engine.
- SAM.gov entity lookup.
- SBA size helper.
- Evidence request workflows.
- Policy templates.
- CMMC Level 2 readiness depth.
- Advisor/consultant multi-client view.

Exit criteria:

- Clause extraction produces reviewable drafts with source traceability.
- Users can approve or reject extracted obligations.
- Content changes are versioned and auditable.

### Phase 3: Advanced Compliance, 12-20 Weeks

Deliverables:

- SSP builder.
- POA&M manager.
- SPRS score calculator.
- CUI data-flow mapping.
- Labor compliance module.
- eSRS support.
- Prime contractor portal.
- Auditor read-only portal.
- AI assistant with citations and human-review guardrails.
- Integration APIs.

Exit criteria:

- Advanced workflows can support higher-tier pilots without storing CUI unless a CUI-ready tier is formally launched.

### Phase 4: Enterprise And Regulated Deployment

Deliverables:

- SSO/SAML.
- SCIM.
- GovCloud option.
- SOC 2 readiness package.
- Advanced encryption and customer-managed key option.
- FedRAMP readiness package if selling directly to federal agencies.
- Data residency controls.

## 6. Testing Strategy

### Test Layers

- Unit tests: domain rules, applicability logic, due-date calculations, risk scoring, validation.
- Application tests: use case behavior, tenant scoping, permissions, obligation mapping, task creation.
- API integration tests: endpoints, auth, authorization, validation, error handling, pagination.
- Frontend tests: forms, dashboard states, role-specific UI, accessibility checks.
- End-to-end tests: onboarding, profile completion, contract intake, obligation mapping, evidence upload, report generation.
- Security tests: RBAC bypass attempts, tenant isolation, upload validation, dependency scanning, secrets scanning.
- Content tests: obligation records require source URL, last reviewed date, trigger logic, risk, confidence, and review status.
- Backup/restore tests: staging restore and production restore drill.

### Required MVP Test Scenarios

- A user cannot access another tenant's contracts, obligations, tasks, or evidence.
- A contributor cannot perform admin-only user management.
- A read-only auditor can view approved evidence packages but cannot modify data.
- A No-CUI upload warning appears before file upload.
- Disallowed file types and oversized files are rejected.
- Uploaded files create audit log entries.
- A contract clause creates the expected obligation tasks.
- A due obligation appears on the dashboard and calendar.
- A completed task remains visible in the audit trail.
- A report includes source-backed obligation details.
- Compliance content without source metadata cannot be published.

### Definition Of Done

Every story is done only when:

- Acceptance criteria pass.
- Unit or integration tests are added where behavior changed.
- Tenant isolation and RBAC implications are reviewed.
- Audit logging is added for sensitive actions.
- Error and empty states are handled.
- Accessibility basics are checked for user-facing UI.
- Documentation or release notes are updated when behavior changes.
- Product owner accepts the story in sprint review.

## 7. Deployment Plan

### Environments

- Dev: local Docker Compose plus developer machines.
- Staging: production-like cloud environment with test data only.
- Production: hardened environment with backups, monitoring, WAF, secrets manager, and restricted access.

### CI/CD Pipeline

Pipeline stages:

1. Restore dependencies.
2. Run format/lint checks.
3. Build frontend and backend.
4. Run unit tests.
5. Run integration tests.
6. Run dependency and vulnerability scans.
7. Build containers.
8. Run database migration validation.
9. Deploy to staging.
10. Run smoke tests.
11. Manual approval for production.
12. Deploy production with rolling or blue/green strategy.
13. Run production smoke tests.

### Release Controls

- Database migrations must be backward compatible whenever possible.
- Feature flags should wrap incomplete workflows.
- Production releases require a rollback plan.
- Content library releases should be versioned separately from code when feasible.
- Material compliance content changes require release notes.

### Production Readiness Checklist

- TLS configured.
- MFA enabled for privileged users.
- Backups enabled and restore tested.
- Logs centralized.
- Alerts configured.
- Audit log immutable or append-only.
- Secrets stored outside source control.
- Object storage encryption enabled.
- Malware scanning path enabled for uploads.
- WAF configured.
- Error tracking enabled.
- Privacy, terms, No-CUI notice, and support paths published.
- Customer-facing claims reviewed so the product does not imply legal advice, certification, CMMC approval, assessment success, or government endorsement.

## 8. Production Support

### Support Tiers

- Severity 1: system unavailable, data exposure, tenant isolation risk, evidence upload outage, auth outage.
- Severity 2: major workflow broken, report generation failed for many customers, notification failure.
- Severity 3: single-customer workflow issue with workaround, minor data display issue.
- Severity 4: question, enhancement, documentation issue.

### Initial Response Targets

- Severity 1: 30 minutes.
- Severity 2: 4 business hours.
- Severity 3: 1 business day.
- Severity 4: 3 business days.

### Operational Runbooks

Create runbooks for:

- Production deploy and rollback.
- Database migration failure.
- Tenant access incident.
- Evidence upload failure.
- Malware scanner outage.
- Report generation backlog.
- Notification delivery failure.
- Backup restore.
- Compliance content correction.
- Security incident response.

### Monitoring And Alerts

Monitor:

- API availability and latency.
- Error rates.
- Database health.
- Queue depth and job failure rates.
- Upload failures.
- Malware scan failures.
- Authentication failures.
- Permission denied spikes.
- Storage utilization.
- Backup success.
- Report generation failures.

### Customer Support Workflow

1. Intake ticket with tenant, user, workflow, timestamps, and screenshots if available.
2. Triage severity and customer impact.
3. Check audit logs and application logs.
4. Reproduce in staging when possible.
5. Patch, test, and release through normal pipeline unless severity requires emergency path.
6. Communicate status and resolution.
7. Add regression test or runbook update for recurring issues.

## 9. Agile Epics, User Stories, And Acceptance Criteria

See [Development Phase Use Cases, Stories, Tasks, And Acceptance Criteria](development-phase-use-cases.md) for the sequential Phase 1 MVP backlog that expands each development process into use cases, stories, tasks, and acceptance criteria.

### Epic 1: Tenant, Identity, And RBAC Foundation

Goal: Ensure every customer has isolated access, role-based permissions, and auditability from the start.

#### Story 1.1: Tenant Creation

As a platform admin, I want to create a tenant so that a customer organization can use GCCS in an isolated workspace.

Acceptance criteria:

- Tenant has name, status, created date, and unique identifier.
- Tenant data is not visible to other tenants.
- Tenant creation is audit logged.

#### Story 1.2: User Invitations

As a tenant admin, I want to invite users so that my team can collaborate.

Acceptance criteria:

- Admin can invite a user by email and role.
- Invitation has pending, accepted, expired, and revoked states.
- Non-admin users cannot invite users.
- Invitation actions are audit logged.

#### Story 1.3: Role-Based Permissions

As a tenant admin, I want role-based permissions so that users only access appropriate workflows.

Acceptance criteria:

- Roles include owner, admin, compliance manager, contributor, auditor, and advisor.
- Restricted actions are denied server-side.
- UI hides actions unavailable to the user.
- Permission denial returns a clear error.

### Epic 2: Company Compliance Profile

Goal: Capture the business facts needed to determine likely obligations and renewal needs.

#### Story 2.1: Create Company Profile

As a compliance manager, I want to create a company profile so that compliance tasks can be based on my business context.

Acceptance criteria:

- Profile captures legal entity name, UEI, CAGE code, SAM expiration, NAICS codes, certifications, role, locations, agency customers, and FCI/CUI posture.
- Required fields are validated.
- Profile completion percentage is shown.
- Updates are audit logged.

#### Story 2.2: Certification Tracking

As a compliance manager, I want to track socioeconomic certifications so that renewals do not get missed.

Acceptance criteria:

- User can add certification type, issuing body, status, effective date, expiration date, and evidence link.
- Expiring certifications create calendar tasks.
- Expired certifications are flagged on the dashboard.

#### Story 2.3: No-CUI Posture Disclosure

As a product owner, I want users to acknowledge No-CUI limitations so that upload expectations are clear.

Acceptance criteria:

- User sees a No-CUI notice during onboarding and before file upload.
- Acknowledgement is recorded.
- The notice explains that the MVP is for compliance management only and is not ready to store CUI.

### Epic 3: Contract And Clause Intake

Goal: Capture contract facts and clauses that drive obligation tracking.

#### Story 3.1: Create Contract Record

As a contracts admin, I want to create a contract record so that obligations can be tracked by contract.

Acceptance criteria:

- User can enter contract number, agency or prime, contract type, role, period of performance, place of performance, and description.
- Contract can be saved as draft or active.
- Contract appears in the tenant contract list.

#### Story 3.2: Upload Contract Documents

As a contracts admin, I want to upload non-CUI contract documents so that evidence and source materials are attached to the contract.

Acceptance criteria:

- Upload requires No-CUI acknowledgement.
- File metadata is saved with contract link.
- Disallowed file types and oversized files are rejected.
- Upload creates audit log entry.

#### Story 3.3: Manual Clause Tagging

As a contracts admin, I want to tag contract clauses manually so that obligations can be generated before automated extraction exists.

Acceptance criteria:

- User can search curated clause library.
- User can add clause to contract with source reference.
- Added clause generates mapped obligations where available.
- User can remove a clause with reason captured.

### Epic 4: Source-Backed Obligation Library

Goal: Maintain a governed obligation library that is traceable to sources.

#### Story 4.1: Obligation Record Schema

As a compliance content owner, I want a structured obligation schema so that content is consistent and reviewable.

Acceptance criteria:

- Obligation includes source, source URL, last reviewed date, trigger logic, required actions, evidence examples, owner, risk, confidence, flow-down requirement, and review state.
- Content without source URL cannot be published.
- Content without last reviewed date cannot be published.

#### Story 4.2: Obligation Dashboard

As a compliance manager, I want a dashboard of obligations so that I know what requires action.

Acceptance criteria:

- Dashboard shows obligations by status, risk, due date, owner, contract, and source.
- User can filter by contract, risk, owner, and module.
- Each obligation displays summary, required action, evidence needed, and source link.

#### Story 4.3: Expert Review Flag

As a compliance content owner, I want to flag obligations requiring expert review so that risky interpretations are not published casually.

Acceptance criteria:

- Obligation can be marked requires expert review.
- Flagged content cannot move to published without reviewer identity and date.
- UI shows review status to internal content owners.

### Epic 5: Task And Compliance Calendar

Goal: Convert obligations, renewals, and reviews into trackable work.

#### Story 5.1: Create Obligation Tasks

As a compliance manager, I want obligations to create tasks so that required actions are assigned and tracked.

Acceptance criteria:

- Task can be linked to obligation, contract, control, evidence, or subcontractor.
- Task includes owner, due date, status, priority, and notes.
- Task status changes are audit logged.

#### Story 5.2: Calendar View

As a compliance manager, I want a calendar view so that upcoming compliance work is visible.

Acceptance criteria:

- Calendar shows tasks, renewals, reports, and contract deadlines.
- User can filter by owner, status, risk, and module.
- Overdue tasks are visually distinct.

#### Story 5.3: Notifications

As a user, I want reminders before due dates so that I can act before obligations are overdue.

Acceptance criteria:

- User receives reminder based on configurable lead time.
- Notification includes task, due date, related obligation, and source context when available.
- Notification delivery result is logged.

### Epic 6: Evidence Vault

Goal: Make evidence easy to organize, reuse, review, and export.

#### Story 6.1: Evidence Metadata

As a compliance manager, I want to create evidence records so that proof is linked to obligations.

Acceptance criteria:

- Evidence has title, type, owner, status, expiration date, tags, and linked records.
- Evidence can satisfy multiple obligations or controls.
- Evidence status includes draft, submitted, approved, rejected, and expired.

#### Story 6.2: Evidence Upload

As a contributor, I want to upload non-CUI evidence so that compliance proof is stored with the obligation.

Acceptance criteria:

- Upload requires No-CUI acknowledgement.
- File is associated with evidence metadata.
- File is queued for malware scanning.
- File is unavailable for approval until scan succeeds.
- Upload and scan outcome are audit logged.

#### Story 6.3: Evidence Package Export

As a compliance manager, I want to export an evidence package so that I can respond to a prime or auditor request.

Acceptance criteria:

- User can select obligations, controls, or contract scope.
- Export includes approved evidence metadata and files.
- Export includes source-backed obligation matrix.
- Export action is audit logged.

### Epic 7: CMMC / NIST Readiness

Goal: Provide CMMC readiness tracking without pretending to be a final assessment determination.

#### Story 7.1: CMMC Level 1 Checklist

As a DoD subcontractor, I want a Level 1 checklist so that I can track basic safeguarding readiness.

Acceptance criteria:

- Checklist includes practices mapped to source-backed requirements.
- User can set status, owner, due date, evidence, and notes.
- Readiness summary shows complete, in progress, not started, and blocked counts.

#### Story 7.2: Level 2 Readiness Workspace

As an IT/security owner, I want a Level 2 readiness workspace so that NIST SP 800-171 Rev. 2 requirements can be tracked.

Acceptance criteria:

- Control list supports status, evidence, POA&M item, owner, and implementation notes.
- UI clearly labels content as readiness tracking, not certification.
- Control export includes source references and last reviewed dates.

#### Story 7.3: POA&M Tracker

As a security owner, I want a POA&M tracker so that remediation work is visible and assignable.

Acceptance criteria:

- POA&M item includes weakness, related control, risk, owner, planned completion date, status, and evidence.
- Overdue POA&M items appear on dashboard.
- Closing a POA&M item requires evidence or closure rationale.

### Epic 8: Subcontractor Flow-Down Management

Goal: Track subcontractor obligations and evidence requests tied to contract flow-downs.

#### Story 8.1: Subcontractor Profile

As a contracts admin, I want to create subcontractor profiles so that flow-down obligations can be managed.

Acceptance criteria:

- Profile captures legal name, role, contact, small business status, CMMC status, insurance status, NDA status, CUI access flag, and export-control flag.
- Subcontractor can be linked to contracts.
- Profile changes are audit logged.

#### Story 8.2: Flow-Down Clause Tracking

As a contracts admin, I want to assign flow-down clauses to subcontractors so that required terms are tracked.

Acceptance criteria:

- Flow-down clause can be linked from contract clause or obligation.
- Status includes required, sent, signed, waived, and not applicable.
- Signed flow-down evidence can be attached.

#### Story 8.3: Subcontractor Evidence Requests

As a compliance manager, I want to request evidence from subcontractors so that prime obligations are supported.

Acceptance criteria:

- Request includes due date, evidence type, related obligation, and instructions.
- Request status is tracked.
- Overdue requests appear on dashboard and calendar.

### Epic 9: Reporting And Audit Trail

Goal: Produce practical outputs for management, primes, auditors, and internal reviews.

#### Story 9.1: Compliance Status Report

As an owner, I want a status report so that I can see compliance health across the company.

Acceptance criteria:

- Report summarizes open obligations, overdue tasks, expiring evidence, CMMC readiness, and subcontractor risks.
- Report can be exported.
- Report includes generated date and data scope.

#### Story 9.2: Contract Obligation Matrix

As a contracts admin, I want an obligation matrix so that a contract's compliance requirements are clear.

Acceptance criteria:

- Matrix includes clause, obligation, owner, action, evidence needed, due date, flow-down, risk, source URL, and status.
- User can filter by contract and export.

#### Story 9.3: Immutable Audit Log

As a security/compliance owner, I want an audit log so that sensitive actions are traceable.

Acceptance criteria:

- Audit log captures user, tenant, action, entity, timestamp, before/after summary where appropriate, and IP/user agent when available.
- Audit entries cannot be edited through the app.
- Admin can filter audit log by date, user, entity, and action.

### Epic 10: Deployment, Operations, And Support

Goal: Operate the SaaS safely with repeatable releases and production support.

#### Story 10.1: CI Pipeline

As a technical lead, I want CI to run on every change so that defects are caught early.

Acceptance criteria:

- Backend build and tests run.
- Frontend build and lint run.
- Dependency scan runs.
- Pipeline fails on test or build failure.

#### Story 10.2: Staging Deployment

As a delivery lead, I want a staging environment so that releases can be verified before production.

Acceptance criteria:

- Staging deploys from main or release branch.
- Staging has separate test database and object storage.
- Smoke tests run after deployment.

#### Story 10.3: Production Monitoring

As an operations owner, I want monitoring and alerts so that production issues are detected quickly.

Acceptance criteria:

- API health, error rate, latency, queue depth, job failures, and upload failures are monitored.
- Severity thresholds are documented.
- Alerts route to support owner.

## 10. Prioritized Backlog

| Priority | Epic | Item | Type | Target Phase |
| --- | --- | --- | --- | --- |
| P0 | Tenant/RBAC | Tenant model and tenant-scoped API pipeline | Story | Phase 1 |
| P0 | Tenant/RBAC | User membership and role enforcement | Story | Phase 1 |
| P0 | Tenant/RBAC | Server-side permission checks | Story | Phase 1 |
| P0 | Operations | CI build, lint, test pipeline | Story | Phase 1 |
| P0 | Company Profile | Company profile create/edit | Story | Phase 1 |
| P0 | Company Profile | No-CUI acknowledgement workflow | Story | Phase 1 |
| P0 | Contracts | Contract record create/edit/list | Story | Phase 1 |
| P0 | Contracts | Manual clause tagging | Story | Phase 1 |
| P0 | Obligations | Source-backed obligation schema | Story | Phase 1 |
| P0 | Obligations | Obligation dashboard | Story | Phase 1 |
| P0 | Tasks | Task model and task list | Story | Phase 1 |
| P0 | Tasks | Calendar view | Story | Phase 1 |
| P0 | Evidence | Evidence metadata and upload | Story | Phase 1 |
| P0 | Evidence | Malware scan job integration path | Story | Phase 1 |
| P0 | Audit | Audit log for sensitive actions | Story | Phase 1 |
| P0 | Reports | Contract obligation matrix export | Story | Phase 1 |
| P1 | CMMC | Level 1 checklist | Story | Phase 1 |
| P1 | CMMC | Level 2 readiness workspace | Story | Phase 1 |
| P1 | CMMC | POA&M tracker | Story | Phase 1 |
| P1 | Subcontractors | Subcontractor profile | Story | Phase 1 |
| P1 | Subcontractors | Flow-down clause tracking | Story | Phase 1 |
| P1 | Notifications | Due-date reminders | Story | Phase 1 |
| P1 | Reports | Compliance status report | Story | Phase 1 |
| P1 | Reports | CMMC readiness report | Story | Phase 1 |
| P1 | Operations | Staging environment | Story | Phase 1 |
| P1 | Operations | Production monitoring baseline | Story | Phase 1 |
| P2 | Content | Expert review workflow | Story | Phase 2 |
| P2 | Contracts | Automated clause extraction draft workflow | Story | Phase 2 |
| P2 | Integrations | SAM.gov entity lookup | Story | Phase 2 |
| P2 | SBA | SBA size helper | Story | Phase 2 |
| P2 | Evidence | Evidence request workflow | Story | Phase 2 |
| P2 | Advisors | Multi-client advisor dashboard | Story | Phase 2 |
| P2 | Templates | Policy templates | Story | Phase 2 |
| P3 | CMMC | SSP builder | Story | Phase 3 |
| P3 | CMMC | SPRS score calculator | Story | Phase 3 |
| P3 | CMMC | CUI data-flow mapping | Story | Phase 3 |
| P3 | Labor | Wage determination and SCA/DBA checklist | Story | Phase 3 |
| P3 | Reporting | Prime contractor portal | Story | Phase 3 |
| P3 | Reporting | Auditor read-only portal | Story | Phase 3 |
| P3 | AI | Cited AI assistant with review workflow | Story | Phase 3 |
| P4 | Enterprise | SSO/SAML | Story | Phase 4 |
| P4 | Enterprise | SCIM provisioning | Story | Phase 4 |
| P4 | Enterprise | GovCloud deployment option | Story | Phase 4 |
| P4 | Enterprise | SOC 2 readiness controls | Story | Phase 4 |

## 11. Suggested Sprint Plan For Phase 1

### Sprint 1: Delivery Foundation

- Confirm MVP requirements and acceptance criteria.
- Add CI for backend and frontend.
- Establish tenant model.
- Establish role model and permission policy.
- Create first audit log model.

### Sprint 2: Company Profile

- Build profile API and UI.
- Add certification tracking.
- Add No-CUI acknowledgement.
- Add profile completeness indicators.

### Sprint 3: Contracts And Clause Intake

- Build contract create/edit/list.
- Add document metadata and upload flow.
- Add manual clause tagging.
- Connect clauses to obligation generation.

### Sprint 4: Obligation Dashboard

- Finalize obligation schema.
- Build obligation list, filters, detail view.
- Add source metadata display.
- Add content validation checks.

### Sprint 5: Tasks And Calendar

- Build task model.
- Link tasks to obligations, contracts, controls, and evidence.
- Build calendar and overdue views.
- Add basic reminder jobs.

### Sprint 6: Evidence Vault

- Build evidence metadata UI.
- Build upload flow.
- Add scan-pending status.
- Link evidence to obligations and controls.
- Add approved/rejected workflow.

### Sprint 7: CMMC And Subcontractors

- Build Level 1 checklist.
- Build Level 2 readiness workspace.
- Build POA&M tracker.
- Build subcontractor profile and flow-down status.

### Sprint 8: Reports, Hardening, And Pilot

- Build compliance status report.
- Build contract obligation matrix.
- Build CMMC readiness report.
- Complete release readiness checks.
- Run pilot onboarding and regression testing.

## 12. MVP Release Acceptance

The MVP is releasable when:

- A pilot customer can complete onboarding without engineering support.
- A company profile can drive renewal and risk visibility.
- A contract can be entered, tagged with clauses, and mapped to obligations.
- Obligations can create tasks and appear in dashboard/calendar views.
- Non-CUI evidence can be uploaded, scanned, linked, reviewed, and exported.
- CMMC readiness can be tracked at Level 1 and basic Level 2 readiness depth.
- Subcontractor flow-downs can be tracked.
- Reports are useful enough for owner, prime, and internal review conversations.
- Tenant isolation, RBAC, audit logging, backup, and monitoring are verified.
- Compliance content has source URLs, last-reviewed dates, and review status.
- No-CUI posture is visible, acknowledged, and enforced in the upload workflow.

# Development Phase Use Cases, Stories, Tasks, And Acceptance Criteria

This backlog expands the Phase 1 MVP and Phase 2 Govcon Intelligence development phases into a sequential delivery plan. It assumes the MVP posture is CUI-ready by design with gated CUI acceptance and that production compliance content is reviewed by qualified subject matter experts before publication. Demo workflows may use synthetic or redacted CUI; real customer CUI requires approved CUI-ready tenant status.

## Delivery Sequence

| Sequence | Process | Primary Outcome |
| --- | --- | --- |
| 1 | Delivery foundation | Team can build, test, review, and deploy consistently. |
| 2 | Tenant, identity, and RBAC | Each customer works inside an isolated tenant with role-based access. |
| 3 | Authenticated application shell | Users can navigate the SaaS workspace and call protected APIs. |
| 4 | CUI-ready gated controls | Users are warned, guided through data classification, and blocked from uploading real CUI unless the tenant is approved as CUI-ready. |
| 5 | Audit logging | Sensitive actions are traceable from the beginning. |
| 6 | Compliance content foundation | Source-backed clauses and obligations can be loaded, reviewed, and published. |
| 7 | Company compliance profile | A contractor can enter the business facts that drive compliance workflows. |
| 8 | Contract intake | A contractor can create contract records, classify data handling posture, and attach allowed source materials. |
| 9 | Manual clause tagging | A user can map contract clauses to curated obligations. |
| 10 | Obligation dashboard | A user can see what applies, who owns it, and what evidence is needed. |
| 11 | Task and compliance calendar | Obligations, renewals, and deadlines become assigned work. |
| 12 | Evidence vault | Users can store and link allowed evidence to obligations and controls, with CUI upload governed by tenant data handling mode. |
| 13 | CMMC readiness tracker | DoD suppliers can track Level 1 and Level 2 readiness work. |
| 14 | Subcontractor flow-down tracker | Prime and subcontract users can track supplier obligations and evidence. |
| 15 | Reports | Users can generate status, obligation, CMMC, evidence, and subcontractor reports. |
| 16 | Notifications | Users receive reminders for deadlines, renewals, and assigned work. |
| 17 | MVP hardening and release readiness | The pilot release is tested, secure, observable, and deployable. |
| 18 | Automated clause extraction | Uploaded allowed contract text can be parsed into clause candidates, with real CUI processing limited to approved CUI-ready tenants. |
| 19 | Human review workflow | Extracted clauses and AI-suggested obligations require review before use. |
| 20 | Clause library expansion | Curated clauses become searchable, versioned, and source-backed. |
| 21 | Applicability engine | Company, contract, clause, data, and subcontractor facts drive obligation applicability. |
| 22 | SAM.gov entity lookup | Users can enrich company and subcontractor records with official SAM entity data. |
| 23 | SBA size helper | Users can evaluate small-business size context by NAICS with source traceability. |
| 24 | Subcontractor tracker expansion | Subcontractor management moves beyond flow-downs into full supplier compliance tracking. |
| 25 | Policy templates | Users can generate draft policies from approved templates and obligation context. |
| 26 | Evidence request workflows | Users can request, collect, review, and track evidence from internal users and subcontractors. |
| 27 | CMMC Level 2 readiness expansion | Users can manage Level 2 readiness with richer assessment, evidence, and responsibility tracking. |
| 28 | Extraction content test set | Extraction precision and recall can be measured against representative contract documents. |

## Acceptance Criteria Testability Standard

Every story acceptance criterion must be testable before the story can be treated as done. A criterion is testable only when it identifies:

- The actor or system under test.
- The action, state, or input being exercised.
- The observable result, persisted record, API response, UI state, audit event, report/export output, or blocked behavior.
- The relevant invariant when applicable: tenant isolation, server-side RBAC, audit logging, CUI/data-handling guardrails, source traceability, or standard error handling.

Acceptance criteria should avoid subjective phrases such as "easy," "appropriate," "robust," or "clear" unless paired with an observable check. The executable `TC-*` cases in `docs/development-story-test-cases.md` are the regression contract for these acceptance criteria, and story implementation is incomplete until focused automated coverage exists for each applicable `TC-*` case.

## 1. Delivery Foundation

### Use Case

As the delivery team, we need a repeatable engineering foundation so that MVP features can be built, reviewed, tested, and released without manual guesswork.

### User Stories

#### Story 1.1: Repository And Project Structure
## Done ##
As a technical lead, I want the application structure to separate web, API, application, domain, infrastructure, docs, and compliance content so that development stays maintainable.

Tasks:

- Confirm solution structure for `apps/api`, `apps/web`, `src/Gccs.Domain`, `src/Gccs.Application`, `src/Gccs.Infrastructure`, `packages/compliance-content`, `docs`, and `infra`.
- Document ownership boundaries for each project.
- Add or update README instructions for local setup.
- Verify the solution builds from a clean checkout.

Acceptance criteria:

- A new developer can identify where frontend, backend, domain, persistence, infrastructure, and compliance content live.
- The solution builds locally with documented commands.
- No compliance workflow logic is embedded only in the UI.
- Documentation points to the CUI-ready gated MVP posture.

#### Story 1.2: Local Development Services
## Done ##
As a developer, I want local database, cache, object storage, and malware-scanning placeholders so that feature work can run against realistic services.

Tasks:

- Configure local PostgreSQL, Redis, object storage, and ClamAV placeholder services.
- Add environment variable examples.
- Add health checks for local service startup.
- Document local reset and migration steps.

Acceptance criteria:

- Local services start with one documented command.
- API can connect to required local dependencies.
- Missing environment variables produce clear startup errors.
- Local configuration does not contain production secrets.

#### Story 1.3: Continuous Integration Baseline
## Done ##
As a delivery lead, I want automated validation so that broken builds and obvious regressions are caught before review.

Tasks:

- Add CI steps for dependency restore, backend build, frontend build, linting, unit tests, and integration tests.
- Add dependency and secret scanning where available.
- Add migration validation for database changes.
- Publish build artifacts or test summaries.

Acceptance criteria:

- Pull requests run automated validation.
- A failing build, lint, or test step blocks merge.
- CI logs identify the failing project and step.
- Security scan failures are visible to reviewers.

## 2. Tenant, Identity, And RBAC

### Use Case

As a platform customer, I need my company data isolated from every other customer, and I need users to have permissions that match their role.

### User Stories

#### Story 2.1: Tenant Creation
## Done ##
As a platform admin, I want to create a tenant so that a customer organization can use GCCS in an isolated workspace.

Tasks:

- Create tenant persistence model and API contract.
- Add tenant status values such as active, suspended, and archived.
- Add tenant ID to tenant-owned entities.
- Enforce tenant filtering in repositories and application services.
- Add seed or admin workflow for initial tenant creation.

Acceptance criteria:

- Tenant has unique ID, display name, status, created date, and updated date.
- Tenant-owned records include tenant ID.
- A user from one tenant cannot retrieve another tenant's data through API calls.
- Tenant creation and status changes are audit logged.

#### Story 2.2: User Memberships
## Done ##
As a tenant admin, I want to add team members to my tenant so that multiple people can work in the same compliance workspace.

Tasks:

- Model user, tenant membership, and membership status.
- Create API endpoints for listing tenant users and assigning memberships.
- Add validation for duplicate memberships.
- Add UI for viewing members.

Acceptance criteria:

- A user can belong to one or more tenants when explicitly assigned.
- Tenant member list only shows users in the current tenant.
- Duplicate membership creation is rejected.
- Membership changes are audit logged.

#### Story 2.3: User Invitations
## Done ##
As a tenant admin, I want to invite users by email and role so that onboarding is controlled.

Tasks:

- Model invitation token, role, expiration, and status.
- Add invitation create, accept, expire, and revoke workflows.
- Add email dispatch placeholder or local notification mechanism.
- Add invitation UI states.

Acceptance criteria:

- Admin can invite a user by email and role.
- Invitations have pending, accepted, expired, and revoked states.
- Expired or revoked invitations cannot be accepted.
- Non-admin users cannot invite users.
- Invitation actions are audit logged.

#### Story 2.4: Role-Based Permissions
## Done ##
As a tenant admin, I want role-based permissions so that users only access workflows appropriate for their responsibilities.

Tasks:

- Define owner, admin, compliance manager, contributor, auditor, and advisor roles.
- Map each role to permissions for profile, contracts, obligations, tasks, evidence, reports, subcontractors, and admin actions.
- Enforce authorization server-side.
- Hide restricted actions in the UI.
- Add permission tests.

Acceptance criteria:

- Restricted actions are denied server-side even if called directly.
- UI only shows actions the current role can perform.
- Permission failures return a clear error.
- Auditor users can view approved evidence packages but cannot modify tenant data.
- RBAC decisions are covered by tests.

## 3. Authenticated Application Shell

### Use Case

As an authenticated user, I need a consistent workspace where I can move between company profile, contracts, obligations, calendar, evidence, CMMC, subcontractors, and reports.

### User Stories

#### Story 3.1: Protected API Access

As a developer, I want authenticated API calls to include tenant and user context so that all workflows are scoped correctly.

Tasks:

- Add authentication middleware or development auth shim.
- Resolve current tenant and current user for each request.
- Add standard API error responses.
- Add request correlation IDs.

Acceptance criteria:

- Protected endpoints reject unauthenticated requests.
- API handlers can access current tenant and user context.
- Missing tenant context returns a clear error.
- API errors use a consistent response shape.

#### Story 3.2: SaaS Navigation Shell

As a user, I want clear navigation so that I can access each MVP workflow without hunting through the interface.

Tasks:

- Build authenticated layout with primary navigation.
- Add route placeholders for dashboard, profile, contracts, obligations, calendar, evidence, CMMC, subcontractors, reports, and settings.
- Add loading, empty, and error states.
- Add role-aware navigation visibility.

Acceptance criteria:

- Authenticated users land in the workspace, not a marketing page.
- Navigation is keyboard accessible.
- Restricted navigation items are hidden for roles without access.
- Empty and error states are visible and understandable.

## 4. CUI-Ready Gated Controls

### Use Case

As the product owner, I need the MVP to clearly prohibit CUI uploads so that customers do not mistake the product for a CUI-ready enclave.

### User Stories

#### Story 4.1: Data Handling Acknowledgement

As a user, I want to understand the upload limitation before using the product so that I know what content is prohibited.

Tasks:

- Add data handling notice content to onboarding and upload workflows.
- Record acknowledgement with user, tenant, timestamp, and notice version.
- Add a way to retrieve current acknowledgement status.
- Add UI prompt when acknowledgement is missing.

Acceptance criteria:

- User sees a data handling notice before first upload.
- User must acknowledge the notice before upload is enabled.
- Acknowledgement is audit logged.
- Notice copy states that demo tenants use synthetic/redacted CUI workflows and real CUI upload requires approved CUI-ready tenant status.

#### Story 4.2: Upload Guardrails

As a security lead, I want upload controls so that prohibited or risky files are blocked early.

Tasks:

- Configure allowed file types and upload size limits.
- Add server-side validation for all uploads.
- Add malware scanning job placeholder.
- Add user-facing rejected upload messages.
- Add tests for file type and file size validation.

Acceptance criteria:

- Disallowed file types are rejected server-side.
- Oversized files are rejected server-side.
- Upload metadata records scan status.
- Failed scans or validation failures do not create usable evidence.
- Upload failures are audit logged.

## 5. Audit Logging

### Use Case

As a tenant admin and compliance reviewer, I need sensitive actions captured in an audit trail so that the organization can prove who changed what and when.

### User Stories

#### Story 5.1: Append-Only Audit Events

As a technical lead, I want append-only audit events so that important compliance actions cannot be silently overwritten.

Tasks:

- Model audit event fields for tenant, actor, action, entity type, entity ID, timestamp, source IP or request metadata, and before/after summary where appropriate.
- Add audit event writer in application services.
- Add database constraints that prevent destructive updates when feasible.
- Add tests for audit event creation.

Acceptance criteria:

- Sensitive actions create audit events.
- Audit events include tenant ID and actor ID.
- Audit events are not editable through normal application APIs.
- Audit failures are surfaced for critical actions.

#### Story 5.2: Audit Log Viewer

As a tenant admin, I want to view audit events so that I can investigate compliance and access activity.

Tasks:

- Add audit log query endpoint with pagination and filters.
- Add UI table with date, actor, action, entity, and summary.
- Add filters by actor, action, date range, and entity type.
- Restrict access to admin, owner, and advisor roles as configured.

Acceptance criteria:

- Admins can view audit events for their tenant only.
- Non-authorized users cannot access audit logs.
- Audit list supports pagination.
- Filters return correct tenant-scoped results.

## 6. Compliance Content Foundation

### Use Case

As a compliance content owner, I need a governed source-backed obligation library so that user workflows cite real clauses and do not present unsupported compliance claims.

### User Stories

#### Story 6.1: Obligation Schema

As a compliance content owner, I want every obligation to follow a structured schema so that content is consistent and reviewable.

Tasks:

- Define clause, source reference, obligation, evidence example, applicability dimension, and review metadata models.
- Include fields for source, source URL, last reviewed date, trigger logic, required actions, owner, risk, confidence, flow-down requirement, and expert review flag.
- Add validation for required source metadata.
- Add tests for invalid content.

Acceptance criteria:

- Obligation records cannot be published without source URL.
- Obligation records cannot be published without last reviewed date.
- Obligation records identify risk, owner, confidence, whether expert review is required, and review state.
- Evidence examples can be linked to obligations.

#### Story 6.2: Content Import

As a developer, I want to load curated obligation content so that the app has useful MVP data.

Tasks:

- Create seed/import process for `packages/compliance-content`.
- Validate JSON schema before import.
- Add import idempotency.
- Preserve source metadata, review metadata, and the expert-review-required flag.
- Add import logs and failure reporting.

Acceptance criteria:

- Valid content imports successfully.
- Invalid content fails with actionable errors.
- Re-running import does not create duplicate records.
- Imported obligations retain source metadata, review metadata, and whether expert review is required.

#### Story 6.3: Content Review State

As a compliance content owner, I want review states so that draft content is not accidentally shown as published guidance.

Tasks:

- Add draft, in_review, approved, published, and retired states.
- Restrict customer-facing views to published content.
- Add reviewer identity and review date fields.
- Add workflow for retiring content.

Acceptance criteria:

- Draft content is hidden from customer-facing obligation views.
- Expert-review-required content cannot be published without reviewer and date.
- Retired content is no longer used for new mappings.
- Content state changes are audit logged.

## 7. Company Compliance Profile

### Use Case

As a small government contractor, I need to capture my company facts so the system can drive renewals, obligations, and profile-based readiness checks.

### User Stories

#### Story 7.1: Create Company Profile

As a compliance manager, I want to create a company profile so that GCCS understands my business context.

Tasks:

- Build API and UI form for legal entity name, UEI, CAGE code, SAM expiration, NAICS codes, SBA size status, certifications, agency customers, role, products/services, employee range, revenue range, locations, IT summary, and FCI/CUI posture.
- Add field validation and save draft behavior.
- Add profile detail page.
- Add audit events for create and update.

Acceptance criteria:

- Required fields are validated before profile completion.
- Profile can be saved as draft when non-critical fields are missing.
- Profile shows completion percentage.
- Profile changes are audit logged.

#### Story 7.2: NAICS And Size Status

As a compliance manager, I want to track NAICS codes and size status so that bid readiness can be reviewed by opportunity.

Tasks:

- Add NAICS code list to company profile.
- Allow users to mark primary NAICS.
- Capture size status and basis for each NAICS.
- Add profile warnings for missing size status.

Acceptance criteria:

- User can add multiple NAICS codes.
- One NAICS can be marked primary.
- Size status is stored per NAICS.
- Missing size status appears in profile gaps.

#### Story 7.3: Certification Tracking

As a compliance manager, I want to track socioeconomic certifications so that renewals do not get missed.

Tasks:

- Add certification model and UI.
- Capture type, issuing body, status, effective date, expiration date, and evidence link.
- Generate renewal task for certifications with expiration dates.
- Show expired and expiring certifications on dashboard.

Acceptance criteria:

- User can add 8(a), WOSB, EDWOSB, HUBZone, SDVOSB, SDB, and custom certifications.
- Expiring certifications create calendar tasks.
- Expired certifications are flagged.
- Certification changes are audit logged.

## 8. Contract Intake

### Use Case

As a contracts admin, I need to enter contracts and source documents so that obligations, deliverables, deadlines, and evidence can be tracked by contract.

### User Stories

#### Story 8.1: Create Contract Record

As a contracts admin, I want to create a contract record so that compliance work can be organized by award, solicitation, subcontract, or purchase order.

Tasks:

- Build contract API and UI.
- Capture contract number, agency or prime, contract type, role, status, period of performance, place of performance, description, and data handling posture.
- Support draft and active status.
- Add contract list and detail pages.

Acceptance criteria:

- User can create draft and active contract records.
- Contract list is tenant scoped.
- Contract detail shows key dates and role.
- Contract create and update actions are audit logged.

#### Story 8.2: Contract Document Metadata And Upload

As a contracts admin, I want to upload allowed contract documents and record document metadata so that source materials are available for review.

Tasks:

- Add document metadata model for solicitation, contract, subcontract, purchase order, SOW, flow-down attachment, wage determination, DD Form 254 metadata, and CUI marking guide metadata.
- Add upload workflow with data handling acknowledgement and tenant CUI gating.
- Store file metadata and object storage reference.
- Add scan status and validation status.

Acceptance criteria:

- Upload is disabled until data handling acknowledgement is complete.
- File metadata is linked to the contract.
- Disallowed files are rejected.
- Upload and delete actions are audit logged.

#### Story 8.3: Contract Dates And Deliverables

As a contracts admin, I want to capture deliverables and deadlines so that contract performance obligations appear in the calendar.

Tasks:

- Add deliverable and deadline models.
- Add UI to create deliverables with owner, due date, status, and description.
- Link deliverables to calendar tasks.
- Add overdue handling.

Acceptance criteria:

- Deliverables appear on contract detail.
- Deliverable due dates appear on calendar.
- Overdue deliverables are flagged.
- Status changes are audit logged.

## 9. Manual Clause Tagging

### Use Case

As a contracts admin, I need to manually tag clauses from a curated library so that obligations can be generated before automated extraction is available.

### User Stories

#### Story 9.1: Clause Library Search

As a contracts admin, I want to search the curated clause library so that I can quickly add applicable clauses to a contract.

Tasks:

- Add clause search endpoint.
- Add filters by FAR, DFARS, CMMC, labor, telecom, ByteDance, and custom categories.
- Add UI search and selection pattern.
- Show source URL and last reviewed date in search results.

Acceptance criteria:

- User can search by clause number, title, and category.
- Only published clauses are available for customer mapping.
- Search results show source and last reviewed date.
- Search is tenant safe and does not expose draft content.

#### Story 9.2: Attach Clause To Contract

As a contracts admin, I want to attach a clause to a contract so that its obligations can be tracked.

Tasks:

- Add contract-clause relationship.
- Add attachment reason and source document reference.
- Add duplicate prevention.
- Add remove clause workflow with reason.

Acceptance criteria:

- User can attach a published clause to a contract.
- Duplicate clause attachments are prevented.
- Removing a clause requires a reason.
- Add and remove actions are audit logged.

#### Story 9.3: Generate Obligations From Clause

As a compliance manager, I want mapped obligations to appear when a clause is added so that compliance work starts immediately.

Tasks:

- Map clause records to obligation templates.
- Generate contract-specific obligation instances.
- Generate default tasks where required.
- Preserve source references and review metadata.

Acceptance criteria:

- Adding a clause creates mapped obligations when templates exist.
- Generated obligations link back to contract and clause.
- Generated obligations include source URL, owner, required action, evidence examples, risk, confidence, and review metadata.
- Generation is idempotent.

## 10. Obligation Dashboard

### Use Case

As a compliance manager, I need a clear dashboard showing what applies, what is due, who owns it, and what evidence proves completion.

### User Stories

#### Story 10.1: Obligation List And Filters

As a compliance manager, I want to view and filter obligations so that I can focus on the most important work.

Tasks:

- Add obligation list endpoint with filters.
- Build dashboard table or work queue.
- Support filters for contract, risk, owner, status, due date, module, and source.
- Add empty state when no obligations exist.

Acceptance criteria:

- Dashboard shows tenant-scoped obligations only.
- User can filter by contract, risk, owner, status, and module.
- Overdue and high-risk obligations are easy to identify.
- Empty state guides user to company profile or contract intake.

#### Story 10.2: Obligation Detail

As a compliance manager, I want obligation details so that I understand why it applies and what action is expected.

Tasks:

- Build obligation detail endpoint and page.
- Show plain-English summary, trigger, required action, owner, evidence examples, flow-down requirement, source link, confidence, last reviewed date, and expert review flag.
- Show linked tasks and evidence.
- Add status update workflow.

Acceptance criteria:

- Obligation detail includes source-backed content.
- Source link is visible.
- User can see linked tasks and evidence.
- Status changes are audit logged.

#### Story 10.3: Ownership Assignment

As a compliance manager, I want to assign obligation owners so that accountability is clear.

Tasks:

- Add owner assignment to obligation instance.
- Add role and user owner options.
- Add assignment UI.
- Notify assigned user when notifications are enabled.

Acceptance criteria:

- Obligations can be assigned to a user or role.
- Assignment changes appear on the dashboard.
- Unauthorized users cannot assign owners.
- Assignment changes are audit logged.

## 11. Task And Compliance Calendar

### Use Case

As a user responsible for compliance work, I need assigned tasks and a calendar so that deadlines, renewals, and reviews are not missed.

### User Stories

#### Story 11.1: Task Management

As a compliance manager, I want to create and assign tasks so that obligations turn into trackable work.

Tasks:

- Build task model and API.
- Link tasks to obligations, contracts, controls, evidence, subcontractors, or certifications.
- Add owner, due date, status, priority, reminder date, and notes.
- Add task create, update, complete, and reopen workflows.

Acceptance criteria:

- Tasks can be linked to relevant compliance entities.
- Task status includes open, in_progress, blocked, completed, and canceled.
- Task updates are tenant scoped.
- Task status changes are audit logged.

#### Story 11.2: Calendar View

As a compliance manager, I want a calendar view so that upcoming work is visible by date.

Tasks:

- Add calendar endpoint that aggregates tasks, renewals, deliverables, and reviews.
- Build month, list, or agenda view.
- Add filters by owner, status, risk, contract, and module.
- Add overdue and upcoming sections.

Acceptance criteria:

- Calendar shows tasks, renewals, reports, contract deadlines, and policy reviews.
- User can filter calendar items.
- Overdue items are visually distinct.
- Calendar data is tenant scoped.

#### Story 11.3: Renewal Generation

As a compliance manager, I want renewal tasks generated from profile and evidence dates so that recurring compliance dates are not missed.

Tasks:

- Generate tasks for SAM expiration, certification expiration, evidence expiration, insurance expiration, policy review, and CMMC affirmation.
- Add configurable lead times.
- Avoid duplicate generated tasks.
- Add tests for due-date calculations.

Acceptance criteria:

- Renewal tasks are generated from dated records.
- Duplicate renewal tasks are not created for the same entity and due date.
- Lead times can be configured or defaulted.
- Generated tasks link back to the source record.

## 12. Evidence Vault

### Use Case

As a contractor, I need a secure evidence vault governed by tenant data handling mode so that I can prove what I did for contracts, clauses, CMMC controls, subcontractors, and audits.

### User Stories

#### Story 12.1: Evidence Metadata

As a compliance manager, I want to create evidence records with tags and links so that proof can be reused across obligations.

Tasks:

- Add evidence item model with title, type, owner, approval status, expiration date, tags, description, and source links.
- Add relationships to obligations, controls, contracts, vendors, subcontractors, employees, and reports.
- Build evidence list and detail pages.
- Add metadata validation.

Acceptance criteria:

- Evidence can be linked to multiple obligations or controls.
- Evidence supports folderless tags.
- Evidence expiration dates can generate tasks.
- Evidence metadata changes are audit logged.

#### Story 12.2: Evidence File Upload

As a contributor, I want to upload approved evidence files so that compliance proof is attached to the right work.

Tasks:

- Add file upload to evidence records.
- Enforce allowed file types, size limits, data handling acknowledgement, tenant CUI gating, and malware scan status.
- Store file version metadata.
- Add download permissions.

Acceptance criteria:

- Upload requires current data handling acknowledgement.
- Files are not marked usable until validation and scan state allow it.
- New file uploads create versions instead of overwriting history.
- Upload, download, and delete actions are audit logged.

#### Story 12.3: Evidence Approval

As a compliance manager, I want to approve evidence so that reports and auditor views only include reviewed material.

Tasks:

- Add evidence states such as draft, submitted, approved, rejected, expired, and archived.
- Add approval and rejection workflow with comments.
- Restrict approval to authorized roles.
- Show approval state in obligation and report views.

Acceptance criteria:

- Only authorized users can approve evidence.
- Rejection requires a reason.
- Approved evidence can be included in reports.
- Approval decisions are audit logged.

## 13. CMMC Readiness Tracker

### Use Case

As a DoD supplier, I need to track CMMC Level 1 and Level 2 readiness so that I can prepare for self-assessments, evidence reviews, and annual affirmations.

### User Stories

#### Story 13.1: CMMC Level Selection

As a compliance manager, I want to select a CMMC target level so that the workspace shows the right readiness scope.

Tasks:

- Add CMMC assessment model with target level, status, assessment date, affirmation due date, and responsible owner.
- Add Level 1 and Level 2 options.
- Add workspace summary.
- Link assessment to company profile and contracts.

Acceptance criteria:

- User can create a CMMC readiness assessment.
- Assessment stores target level and status.
- Assessment summary shows completion progress.
- Changes are audit logged.

#### Story 13.2: Control Readiness

As an IT/security owner, I want to track control status and evidence so that gaps are visible.

Tasks:

- Load Level 1 controls and Level 2 readiness mappings.
- Add control status values such as not_started, implemented, partially_implemented, not_applicable, and needs_review.
- Link controls to evidence, tasks, assets, and POA&M items.
- Add control detail page.

Acceptance criteria:

- Controls can be marked with readiness status.
- Controls can link to evidence and tasks.
- Control status contributes to assessment progress.
- Source baseline is shown for each control.

#### Story 13.3: POA&M Items

As a security owner, I want to create POA&M items so that control gaps become assigned remediation work.

Tasks:

- Add POA&M model with control, gap, remediation plan, owner, due date, risk, and status.
- Link POA&M items to tasks.
- Show open POA&M count on CMMC summary.
- Add overdue handling.

Acceptance criteria:

- POA&M item links to a control.
- POA&M item has owner, due date, status, and risk.
- Open and overdue POA&M items appear in CMMC summary and calendar.
- POA&M changes are audit logged.

#### Story 13.4: Annual Affirmation Tracker

As a company owner, I want to track CMMC affirmation dates so that annual requirements are not missed.

Tasks:

- Add affirmation due date and last affirmation date.
- Generate affirmation renewal task.
- Add evidence link for affirmation record.
- Add dashboard warning for upcoming affirmation.

Acceptance criteria:

- Affirmation due date appears on calendar.
- Upcoming affirmation creates a reminder task.
- User can link evidence to affirmation.
- Affirmation updates are audit logged.

## 14. Subcontractor Flow-Down Tracker

### Use Case

As a prime or subcontractor, I need to track subcontractor profiles, required flow-down clauses, CMMC posture, insurance, NDAs, and evidence requests.

### User Stories

#### Story 14.1: Subcontractor Profile

As a contracts admin, I want to create subcontractor profiles so that supplier compliance can be tracked.

Tasks:

- Add subcontractor model and API.
- Capture legal name, point of contact, role, small business status, CMMC status, insurance expiration, NDA status, CUI access flag, export-control flag, and workshare percentage.
- Add subcontractor list and detail pages.
- Link subcontractor to contracts.

Acceptance criteria:

- User can create and update subcontractor profiles.
- Subcontractors can be linked to contracts.
- CUI access and export-control flags are visible.
- Changes are audit logged.

#### Story 14.2: Flow-Down Clause Tracking

As a contracts admin, I want to assign required flow-down clauses so that subcontractor obligations are visible.

Tasks:

- Add subcontractor flow-down relationship.
- Allow clauses to be assigned from contract obligations.
- Track status such as required, sent, acknowledged, signed, waived, or not_applicable.
- Link signed flow-down evidence.

Acceptance criteria:

- Flow-down clauses can be assigned to subcontractors.
- Flow-down status is visible by subcontractor and contract.
- Signed evidence can be linked.
- Status changes are audit logged.

#### Story 14.3: Subcontractor Evidence Requests

As a compliance manager, I want to request evidence from subcontractors so that supplier compliance gaps can be closed.

Tasks:

- Add evidence request model with requested item, due date, status, recipient, and linked obligation.
- Add internal request workflow for MVP.
- Add overdue tracking.
- Link received evidence to subcontractor.

Acceptance criteria:

- User can create an evidence request for a subcontractor.
- Request appears on calendar.
- Received evidence can satisfy the request.
- Overdue requests are flagged.

## 15. Reports

### Use Case

As a compliance manager or company owner, I need exportable reports so that I can brief leadership, answer prime contractor requests, and prepare for reviews.

### User Stories

#### Story 15.1: Compliance Status Report

As a company owner, I want a compliance status report so that I can see overall risk and readiness.

Tasks:

- Define report snapshot model.
- Include obligation status, overdue tasks, evidence status, CMMC progress, subcontractor gaps, and high-risk items.
- Add generate report action.
- Add export to PDF or HTML for MVP.

Acceptance criteria:

- Report includes current status summary.
- Report is tenant scoped.
- Report includes generation timestamp.
- Report generation is audit logged.

#### Story 15.2: Contract Obligation Matrix

As a contracts admin, I want a contract obligation matrix so that I can review clauses, obligations, owners, evidence, and due dates by contract.

Tasks:

- Build contract-level report query.
- Include clause, source, obligation, owner, status, risk, due date, evidence, and flow-down requirement.
- Add filter by contract.
- Add export.

Acceptance criteria:

- User can generate matrix for one contract.
- Matrix includes source links and last reviewed dates.
- Matrix includes flow-down indicators.
- Export matches on-screen data.

#### Story 15.3: CMMC Readiness Report

As an IT/security owner, I want a CMMC readiness report so that leadership and advisors can see control progress and gaps.

Tasks:

- Include assessment target level, control statuses, evidence links, POA&M items, open gaps, and affirmation dates.
- Add export.
- Restrict report access by role.
- Add report snapshot history.

Acceptance criteria:

- Report shows CMMC progress by control family or category.
- Open POA&M items are included.
- Evidence links only include records the user can access.
- Report access is RBAC protected.

#### Story 15.4: Evidence Package

As a compliance manager, I want to generate an evidence package so that I can respond to a prime contractor or auditor request.

Tasks:

- Allow user to select obligations, contract, CMMC controls, or subcontractor scope.
- Include approved evidence only by default.
- Include metadata manifest.
- Add read-only package view.

Acceptance criteria:

- Evidence package includes selected scope and approved evidence.
- Draft or rejected evidence is excluded unless explicitly allowed by authorized user.
- Package includes manifest with title, evidence type, linked obligation/control, approval state, and timestamp.
- Package generation is audit logged.

#### Story 15.5: Subcontractor Compliance Report

As a contracts admin, I want a subcontractor compliance report so that I can monitor supplier readiness.

Tasks:

- Include subcontractor profile status, flow-down status, CMMC status, insurance expiration, NDA status, evidence requests, and overdue items.
- Add contract filter.
- Add export.
- Add risk summary.

Acceptance criteria:

- Report can be filtered by contract.
- Report flags missing or overdue subcontractor evidence.
- Report includes flow-down status.
- Export is tenant scoped.

## 16. Notifications

### Use Case

As a user, I need reminders before deadlines and alerts when work is assigned so that compliance tasks do not go stale.

### User Stories

#### Story 16.1: Notification Preferences

As a user, I want notification preferences so that reminders are useful and not noisy.

Tasks:

- Add notification preference model.
- Support preferences for assignments, due soon, overdue, evidence requests, certification renewals, and CMMC affirmation.
- Add UI settings.
- Add defaults by role.

Acceptance criteria:

- Users can update notification preferences.
- Defaults exist for new users.
- Preferences are tenant scoped when needed.
- Preference changes are audit logged.

#### Story 16.2: Due-Date Reminders

As a compliance manager, I want reminders before due dates so that I can act before obligations are overdue.

Tasks:

- Add reminder job.
- Query upcoming and overdue tasks.
- Send in-app notification and email placeholder.
- Ensure idempotency.

Acceptance criteria:

- Reminder job identifies upcoming tasks based on configured lead time.
- Same reminder is not sent repeatedly for the same event.
- Overdue reminders are sent separately.
- Reminder delivery failures are logged.

#### Story 16.3: Assignment Notifications

As a user, I want to be notified when work is assigned to me so that I know what requires my attention.

Tasks:

- Emit notification when task, obligation, POA&M item, or evidence request is assigned.
- Add notification center UI.
- Mark notifications as read.
- Add link from notification to source record.

Acceptance criteria:

- Assigned users receive notification.
- Notification links to the relevant record.
- User can mark notification as read.
- Unauthorized users cannot open linked records.

## 17. MVP Hardening And Release Readiness

### Use Case

As the delivery team, we need to verify the MVP end to end so that a pilot tenant can onboard, use the core workflows, and trust the system boundaries.

### User Stories

#### Story 17.1: End-To-End Pilot Workflow

As a product owner, I want a complete pilot workflow tested so that we know the MVP supports the core promise.

Tasks:

- Create test tenant and users for owner, admin, compliance manager, contributor, auditor, and advisor.
- Run onboarding, company profile, contract intake, clause tagging, obligation dashboard, task calendar, evidence upload, CMMC tracker, subcontractor tracker, reports, and notifications.
- Capture gaps and fix release blockers.
- Add regression tests for the happy path.

Acceptance criteria:

- One pilot tenant can complete all MVP workflows with non-CUI data.
- Role-specific users can only perform permitted actions.
- Reports reflect the data created during the workflow.
- Critical workflow defects are resolved before release.

#### Story 17.2: Security And Tenant Isolation Verification

As a security lead, I want tenant isolation and RBAC tested so that customer data boundaries are enforced.

Tasks:

- Add automated tests for cross-tenant access attempts.
- Add tests for direct API calls that bypass hidden UI controls.
- Review all tenant-owned queries for tenant filters.
- Verify audit logging for sensitive workflows.

Acceptance criteria:

- Cross-tenant API access is denied.
- Restricted role actions are denied server-side.
- Tenant-owned records are filtered by tenant in repositories and services.
- Security test results are documented.

#### Story 17.3: Staging Environment

As a delivery lead, I want a production-like staging environment so that releases can be verified before production.

Tasks:

- Provision staging API, web app, database, object storage, cache, queue, and secrets.
- Run migrations automatically.
- Configure logs, health checks, and basic alerts.
- Add staging smoke tests.

Acceptance criteria:

- Staging can deploy from CI/CD.
- Staging has no production customer data.
- Health checks cover API, database, cache, storage, and jobs.
- Smoke tests pass after deployment.

#### Story 17.4: Production Readiness Checklist

As a product owner, I want a release checklist so that launch risks are reviewed deliberately.

Tasks:

- Confirm data handling notice, terms, support path, and prohibited upload guidance.
- Confirm backups, restore test, logs, alerts, and rollback plan.
- Confirm malware scanning path or explicit MVP limitation.
- Confirm expert-reviewed compliance content for launch obligations.
- Confirm release notes and known limitations.

Acceptance criteria:

- Checklist is complete before production launch.
- Known limitations are documented.
- Launch content has source URLs and review metadata.
- Rollback plan is documented and tested in staging.

## 18. Automated Clause Extraction

### Use Case

As a contracts user, I need GCCS to identify likely clauses in uploaded allowed contract materials so that manual clause tagging is faster and less error-prone.

### User Stories

#### Story 18.1: Extraction Job Intake

As a compliance manager, I want to start clause extraction from a contract document so that the system can analyze the document asynchronously.

Tasks:

- Add extraction job model with status, source document, tenant, requested by, started date, completed date, and failure reason.
- Add API endpoint to start extraction from an existing contract document.
- Add queue worker or background service stub for processing extraction jobs.
- Add UI action to start extraction from the contract document detail view.
- Add audit events for extraction job creation, completion, and failure.

Acceptance criteria:

- User with contract edit permission can start extraction for a document in the current tenant.
- User without contract edit permission receives a server-side authorization error.
- Extraction job stores tenant ID, source document ID, requester ID, status, and timestamps.
- Starting extraction for another tenant's document is denied.
- Extraction job creation is audit logged.

#### Story 18.2: Text Extraction And Clause Candidate Detection

As a compliance manager, I want the system to detect clause candidates from contract text so that I can review likely matches before applying them.

Tasks:

- Extract text from supported non-CUI document formats already allowed by the MVP upload policy.
- Detect clause references such as FAR, DFARS, agency supplement, and local clause identifiers.
- Store clause candidates with raw text, normalized clause number, title when detected, page or location metadata, confidence score, and match method.
- Link candidates to known clause library records when an exact or high-confidence match exists.
- Add error handling for unsupported, encrypted, image-only, empty, or unreadable documents.

Acceptance criteria:

- Supported text documents produce clause candidates when recognizable clause references are present.
- Each candidate includes source document, normalized citation, raw extracted text, confidence, and location metadata when available.
- Exact matches link to the corresponding clause library record.
- Unsupported or unreadable documents produce a failed job with a user-visible reason.
- Extracted text and candidates remain tenant-scoped.

#### Story 18.3: Extraction Results Review Screen

As a compliance manager, I want to see extraction results beside the source contract so that I can decide which clauses to accept.

Tasks:

- Add extraction result list with filters for matched, unmatched, confidence, and review status.
- Add candidate detail view with extracted text, source location, proposed library match, and confidence.
- Add actions to accept, reject, edit citation, or link to a clause library record.
- Add empty, processing, failed, and completed states.
- Add result count and extraction status to the contract detail view.

Acceptance criteria:

- User can view extraction results for documents in the current tenant.
- Results show citation, confidence, match status, review status, and source location when available.
- Accepted candidates create reviewed contract clause links only after user action.
- Rejected candidates remain visible in extraction history and do not create contract clause links.
- Candidate edits and review decisions are audit logged.

## 19. Human Review Workflow

### Use Case

As a compliance team, we need extracted clauses and AI-suggested obligations reviewed before customers rely on them so that GCCS does not silently publish unverified compliance guidance.

### User Stories

#### Story 19.1: Review States For Extracted Clauses

As a compliance manager, I want extracted clauses to move through explicit review states so that unreviewed results cannot be treated as authoritative.

Tasks:

- Add review states for pending review, accepted, rejected, needs clarification, and superseded.
- Add reviewer, reviewed date, decision note, and decision reason fields.
- Enforce allowed state transitions in application services.
- Add review status filters to extraction result screens.
- Add audit events for state transitions.

Acceptance criteria:

- New extraction candidates default to pending review.
- Only users with clause review permission can accept or reject candidates.
- Accepted candidates record reviewer, reviewed date, and decision note when provided.
- Rejected and superseded candidates do not generate obligations.
- Review state transitions are audit logged.

#### Story 19.2: AI-Suggested Obligation Review

As a compliance SME, I want AI-suggested obligations to require review before publication so that draft content is not shown as approved compliance guidance.

Tasks:

- Model suggested obligations separately from approved obligations.
- Store generated summary, proposed owner, required actions, evidence suggestions, source citations, confidence, prompt version, model identifier, and retrieved source references.
- Add review workflow for approve, revise, reject, and send to expert review.
- Add UI labels that mark suggestions as draft until approved.
- Prevent draft suggestions from appearing in customer reports as approved obligations.

Acceptance criteria:

- AI-suggested obligations are stored with source references, confidence, and draft status.
- Draft suggestions are not included in approved obligation dashboards or reports.
- Reviewer can approve, revise, reject, or escalate a suggestion.
- Approved suggestions record reviewer, approval date, and source citations.
- Rejected suggestions remain in review history and are audit logged.

#### Story 19.3: Expert Escalation Queue

As a compliance content owner, I want uncertain clause and obligation decisions escalated to experts so that high-risk interpretations receive qualified review.

Tasks:

- Add expert review queue for clause candidates and suggested obligations.
- Add priority, topic, assigned expert, due date, and resolution fields.
- Add escalation reasons such as high-risk clause, low confidence, conflicting sources, customer dispute, and legal interpretation.
- Add queue filters and assignment workflow.
- Add notifications for assigned expert review items.

Acceptance criteria:

- Reviewer can escalate a candidate or suggested obligation with a required reason.
- Escalated items appear in an expert review queue.
- Assigned expert receives a notification.
- Resolution records decision, reviewer, date, and notes.
- Escalated items cannot be published as approved until resolved.

## 20. Clause Library Expansion

### Use Case

As a compliance content owner, I need a source-backed clause library that supports extraction matching, obligation mapping, and customer-facing traceability.

### User Stories

#### Story 20.1: Versioned Clause Records

As a compliance content owner, I want clauses to be versioned so that changes to source text or interpretation are traceable.

Tasks:

- Add clause version fields for citation, title, source URL, effective date, last reviewed date, review owner, status, and supersedes relationship.
- Add statuses for draft, under review, approved, deprecated, and superseded.
- Add import/update workflow for curated clause records.
- Add API and UI for clause detail with version history.
- Add audit events for clause creation, update, approval, and deprecation.

Acceptance criteria:

- Clause records include citation, title, source URL, status, last reviewed date, and review owner.
- Approved versions can be used for extraction matching and obligation mapping.
- Deprecated or superseded versions are visible in history but not selected by default for new mappings.
- Clause version changes preserve prior version history.
- Clause changes are audit logged.

#### Story 20.2: Clause Search And Discovery

As a contracts user, I want to search the clause library by citation, title, source, and obligation area so that I can quickly find the correct clause.

Tasks:

- Add searchable fields for citation, normalized citation, title, source family, keywords, obligation area, and risk level.
- Add filters for FAR, DFARS, agency supplement, CMMC, labor, cybersecurity, reporting, and flow-down relevance.
- Add clause search API with tenant-safe access to approved library content.
- Improve UI search results with source, last reviewed date, confidence, and status.
- Add empty and no-results states.

Acceptance criteria:

- Search by exact citation returns the matching approved clause when present.
- Search by title or keyword returns relevant approved clauses.
- Filters narrow results by source family, obligation area, and flow-down relevance.
- Results show source URL, status, and last reviewed date.
- Draft or under-review clauses are hidden from standard users unless they have content review permission.

#### Story 20.3: Clause-To-Obligation Mapping

As a compliance content owner, I want clauses mapped to approved obligation templates so that accepted clauses can generate consistent obligations.

Tasks:

- Add mapping model between clause versions and obligation templates.
- Store trigger condition, required action, owner role, evidence examples, reporting deadlines, flow-down requirement, risk level, confidence, and expert review flag.
- Add mapping approval workflow.
- Add validation that approved mappings include source URL, trigger condition, confidence, and last reviewed date.
- Add UI for viewing and editing mappings.

Acceptance criteria:

- Approved clause mapping can generate an obligation for a contract.
- Mapping requires trigger condition, required action, source URL, confidence, and review metadata before approval.
- Draft mappings cannot generate customer-visible approved obligations.
- Mapping changes preserve history.
- Mapping approval and changes are audit logged.

## 21. Applicability Engine

### Use Case

As a compliance manager, I need GCCS to determine which obligations likely apply based on company, contract, clause, data, and subcontractor facts so that the obligation dashboard reflects contract-specific context.

### User Stories

#### Story 21.1: Applicability Facts Model

As a developer, I want a structured facts model so that applicability decisions can be computed consistently.

Tasks:

- Define facts for tenant company profile, NAICS, certifications, agency, contract type, role, place of performance, data type, labor category, clause, subcontractor role, and CUI/FCI indicators.
- Add persistence or read model for facts used by the applicability engine.
- Add fact provenance, source record, last updated date, and unknown values.
- Add validation for required facts by workflow.
- Document fact definitions and expected sources.

Acceptance criteria:

- Applicability facts can be derived from existing company, contract, clause, and subcontractor records.
- Unknown facts are represented explicitly instead of inferred as false.
- Each fact records source record and last updated date when available.
- Fact model is tenant-scoped.
- Fact definitions are documented.

#### Story 21.2: Rule Evaluation

As a compliance manager, I want rules evaluated against facts so that obligations are marked applicable, not applicable, or needs review.

Tasks:

- Add applicability rule format with conditions, source, confidence, effective date, and review metadata.
- Implement rule evaluator for deterministic rules.
- Return result states of applicable, not applicable, needs review, and insufficient information.
- Store evaluation explanation and facts used.
- Add tests for common FAR, DFARS, CMMC, SAM/SBA, and flow-down rule patterns.

Acceptance criteria:

- Rule evaluator returns a result state, explanation, source rule ID, and facts used.
- Missing required facts produce insufficient information or needs review rather than a silent positive result.
- Rule evaluation is repeatable for the same inputs.
- Evaluation results are tenant-scoped.
- Rule evaluator behavior is covered by automated tests.

#### Story 21.3: Obligation Applicability Updates

As a compliance manager, I want obligation applicability to update when relevant facts change so that dashboards stay current.

Tasks:

- Trigger reevaluation when company profile, contract facts, clause mappings, data type, subcontractor, or rule versions change.
- Store current and prior applicability results.
- Add UI indicators for applicable, not applicable, needs review, and insufficient information.
- Add explanation panel showing why an obligation applies or needs review.
- Add audit events for material applicability changes.

Acceptance criteria:

- Updating a relevant fact reevaluates affected obligations.
- Dashboard displays the current applicability state.
- Explanation shows source rule, facts used, and missing facts when applicable.
- Prior result history is retained.
- Material changes from applicable to not applicable or needs review are audit logged.

## 22. SAM.gov Entity Lookup

### Use Case

As a company admin or subcontractor manager, I need to look up official SAM.gov entity data so that company and subcontractor records can be enriched with authoritative registration context.

### User Stories

#### Story 22.1: SAM.gov API Configuration

As a developer, I want SAM.gov API access configured securely so that entity lookup can run without exposing secrets.

Tasks:

- Add configuration for SAM.gov base URL, API key, timeout, retry policy, and rate limit behavior.
- Store API keys in the existing secrets management approach.
- Add service interface and infrastructure adapter for SAM entity lookup.
- Add health check or diagnostic endpoint that does not expose secrets.
- Add standard error handling for unavailable, rate-limited, and invalid-response cases.

Acceptance criteria:

- SAM.gov API key is not stored in source control.
- Lookup service uses configured timeout and retry behavior.
- API failures return a standard, user-safe error.
- Logs do not contain API keys or sensitive response payloads.
- Adapter can be replaced or mocked in tests.

#### Story 22.2: Company Entity Lookup

As a tenant admin, I want to search SAM.gov by UEI or legal business name so that I can verify company registration details.

Tasks:

- Add company lookup form for UEI and legal business name.
- Display matched entity details such as legal name, UEI, CAGE, registration status, expiration date, physical address, and available NAICS codes.
- Allow authorized user to apply selected fields to company profile.
- Store source, retrieved date, and applied by user.
- Add conflict handling when SAM data differs from existing profile values.

Acceptance criteria:

- Authorized user can search by UEI or legal business name.
- Search results show source and retrieved date.
- User can apply selected fields to the company profile.
- Existing profile values are not overwritten without explicit user confirmation.
- Applied SAM data changes are audit logged.

#### Story 22.3: Subcontractor Entity Lookup

As a subcontractor manager, I want to enrich subcontractor profiles with SAM.gov data so that supplier compliance tracking starts from official entity records.

Tasks:

- Add SAM lookup action to subcontractor profile.
- Display entity status, UEI, CAGE, expiration date, NAICS codes, and exclusion or status indicators when available from the configured API response.
- Allow authorized user to apply selected fields to subcontractor record.
- Store source metadata and retrieved date.
- Add warning when lookup returns no match or multiple possible matches.

Acceptance criteria:

- Authorized user can search SAM.gov for a subcontractor by UEI or name.
- Applied fields update only the current tenant's subcontractor record.
- No-match and multiple-match results are shown without changing existing data.
- Source and retrieved date are stored with applied data.
- Subcontractor SAM updates are audit logged.

## 23. SBA Size Helper

### Use Case

As a small business user, I need guidance on SBA size context by NAICS so that I can track where my company may qualify as small and where expert review may be needed.

### User Stories

#### Story 23.1: Size Standard Reference Data

As a compliance content owner, I want SBA size standard reference data loaded with source metadata so that size helper calculations are traceable.

Tasks:

- Define size standard data fields for NAICS code, industry title, size metric, threshold, source URL, effective date, last reviewed date, and status.
- Add import workflow for approved reference data.
- Add content review states for draft, under review, approved, deprecated, and superseded.
- Add validation for required source metadata.
- Add tests for import validation.

Acceptance criteria:

- Approved size standard records include NAICS, metric, threshold, source URL, effective date, last reviewed date, and status.
- Draft records are not used in customer-facing helper results.
- Import rejects records missing source metadata.
- Deprecated records remain visible to content reviewers.
- Import and approval actions are audit logged.

#### Story 23.2: Company Size Evaluation Helper

As a tenant admin, I want to compare my company profile values against size standards so that I can identify likely small-business status by NAICS.

Tasks:

- Add UI for selecting company NAICS codes and entering annual receipts or employee count ranges.
- Evaluate selected NAICS codes against approved size standard reference data.
- Return result labels such as likely small, likely other than small, insufficient information, and expert review recommended.
- Store evaluation inputs, result, source records, and run date.
- Add disclaimer that the helper is not a final legal determination.

Acceptance criteria:

- Evaluation uses approved size standard records only.
- Missing revenue or employee inputs produce insufficient information.
- Results show NAICS, metric, threshold, entered value or range, source URL, and run date.
- User can save evaluation results to the company profile.
- Saved evaluations are audit logged.

#### Story 23.3: Opportunity NAICS Size Check

As a proposal manager, I want to check an opportunity or contract NAICS code against company data so that I can flag size-status questions early.

Tasks:

- Add size check action on contract or opportunity records with NAICS code.
- Compare opportunity NAICS to company size inputs and approved size standards.
- Show result and missing inputs.
- Add task creation when expert review is recommended.
- Store evaluation history on the contract.

Acceptance criteria:

- User can run size check for a contract NAICS code.
- Result shows likely status, source standard, and missing information when applicable.
- Expert-review recommended result can create a task assigned to an owner.
- Evaluation history remains available from the contract record.
- Size check actions are audit logged.

## 24. Subcontractor Tracker Expansion

### Use Case

As a prime contractor or subcontractor manager, I need a fuller subcontractor compliance tracker so that flow-downs, data access, insurance, certifications, CMMC status, and evidence requests are managed together.

### User Stories

#### Story 24.1: Expanded Subcontractor Compliance Profile

As a subcontractor manager, I want richer subcontractor profile fields so that supplier compliance risk can be assessed consistently.

Tasks:

- Add fields for UEI, CAGE, NAICS, small-business status, socioeconomic certifications, insurance expiration, NDA status, CUI access, export-control status, CMMC level/status, workshare percentage, and responsible owner.
- Add validation for expiration dates and controlled vocabularies.
- Add profile completeness indicator.
- Add filters for status, CUI access, certification, insurance expiration, and CMMC readiness.
- Add audit events for sensitive profile changes.

Acceptance criteria:

- Authorized user can create and update expanded subcontractor fields.
- Profile completeness reflects required fields configured for the tenant.
- Filters return only subcontractors in the current tenant.
- Expiring insurance or certification dates can be surfaced in list filters.
- Sensitive field changes are audit logged.

#### Story 24.2: Subcontractor Risk Status

As a compliance manager, I want subcontractor risk status calculated from key compliance signals so that I can prioritize follow-up.

Tasks:

- Define risk inputs for missing flow-downs, expired insurance, missing NDA, CUI access without CMMC status, overdue evidence, SAM status, and unresolved expert review.
- Implement risk status labels such as low, medium, high, and needs review.
- Show risk drivers on subcontractor detail and list views.
- Recalculate risk when underlying signals change.
- Add tests for risk status rules.

Acceptance criteria:

- Risk status is calculated from documented inputs.
- Risk drivers are visible to authorized users.
- Updating evidence, insurance, NDA, CMMC status, or SAM data updates risk status.
- Missing or unknown data can produce needs review.
- Risk calculation is covered by automated tests.

#### Story 24.3: Contract-Specific Subcontractor Obligations

As a subcontractor manager, I want to connect subcontractors to contract-specific obligations so that supplier requirements are tracked by contract.

Tasks:

- Add relationship between subcontractor, contract, flow-down clause, obligation, and evidence request.
- Show subcontractor obligations on contract detail and subcontractor detail.
- Add owner, due date, status, and evidence requirement to each supplier obligation.
- Allow bulk creation from accepted flow-down clauses.
- Add audit events for creation and status changes.

Acceptance criteria:

- User can link a subcontractor to a contract and applicable flow-down obligations.
- Supplier obligations show owner, due date, status, and required evidence.
- Bulk creation uses accepted flow-down clauses only.
- Supplier obligations are tenant-scoped.
- Creation and status changes are audit logged.

## 25. Policy Templates

### Use Case

As a compliance manager, I need approved policy templates that can be tailored from company and obligation context so that draft policies are faster to prepare without becoming unreviewed legal advice.

### User Stories

#### Story 25.1: Approved Template Library

As a compliance content owner, I want policy templates managed with review metadata so that only approved templates are available to customers.

Tasks:

- Add template model with title, category, body, placeholders, source references, version, status, owner, last reviewed date, and expert review flag.
- Add statuses for draft, under review, approved, deprecated, and superseded.
- Add template preview and version history.
- Add validation that approved templates include source references and review metadata.
- Add audit events for template lifecycle changes.

Acceptance criteria:

- Approved templates include title, category, version, source references, owner, and last reviewed date.
- Draft templates are hidden from standard users.
- Deprecated templates remain visible to content reviewers.
- Template approval requires source and review metadata.
- Template lifecycle changes are audit logged.

#### Story 25.2: Generate Draft Policy From Template

As a compliance manager, I want to generate a draft policy from an approved template so that I can tailor it for my company.

Tasks:

- Add template selection workflow.
- Populate placeholders from company profile, contract, obligation, and CMMC context where available.
- Mark generated policy as draft.
- Store source template version and generation date.
- Add edit and save workflow in the evidence vault or policy area.

Acceptance criteria:

- User can generate a draft policy from an approved template.
- Placeholder values are populated from tenant data when available.
- Missing placeholder values are flagged for user completion.
- Generated policy stores source template version and generation date.
- Generated policy is marked draft until approved by the tenant.

#### Story 25.3: Policy Approval And Evidence Linking

As a compliance manager, I want approved policies linked to obligations and controls so that they can be reused as evidence.

Tasks:

- Add tenant-level policy approval status and approver metadata.
- Allow approved policies to be linked to obligations, CMMC controls, and evidence packages.
- Add expiration or review date tracking.
- Add audit trail for approval, rejection, and revision.
- Add report inclusion for approved policies.

Acceptance criteria:

- Authorized user can approve, reject, or revise a draft policy.
- Approved policy records approver, approval date, source template, and review date.
- Approved policy can be linked to obligations and controls as evidence.
- Revisions preserve prior approved versions.
- Policy approval actions are audit logged.

## 26. Evidence Request Workflows

### Use Case

As a compliance manager, I need to request evidence from internal users and subcontractors so that obligation, CMMC, and supplier evidence can be collected and reviewed on time.

### User Stories

#### Story 26.1: Evidence Request Creation

As a compliance manager, I want to create evidence requests tied to obligations, controls, contracts, or subcontractors so that each request has context and a due date.

Tasks:

- Add evidence request model with requester, assignee, related record, due date, status, instructions, required evidence type, and priority.
- Add create workflow from obligation, control, contract, subcontractor, and evidence vault views.
- Add validation for assignee, due date, and related record permissions.
- Add notification on assignment.
- Add audit event for request creation.

Acceptance criteria:

- Authorized user can create an evidence request tied to a supported record type.
- Request stores requester, assignee, due date, status, instructions, and related record.
- Assignee receives notification.
- User cannot assign a request to a user or subcontractor outside the tenant context.
- Request creation is audit logged.

#### Story 26.2: Evidence Submission And Review

As an assignee, I want to submit evidence to a request so that the requester can review whether it satisfies the requirement.

Tasks:

- Add submission workflow for existing evidence items and new uploads allowed by CUI/data-handling guardrails.
- Add statuses for open, submitted, accepted, returned, overdue, and canceled.
- Add reviewer comments and returned-for-revision reason.
- Link accepted submissions to the related obligation, control, or subcontractor requirement.
- Add notifications for submission, return, acceptance, and overdue states.

Acceptance criteria:

- Assignee can submit evidence to an open request.
- Upload submissions enforce CUI/data-handling guardrails and tenant scope.
- Reviewer can accept or return submitted evidence with comments.
- Accepted evidence is linked to the related requirement.
- Status changes and review decisions are audit logged.

#### Story 26.3: Evidence Request Dashboard

As a compliance manager, I want a dashboard of evidence requests so that I can track overdue, submitted, accepted, and blocked requests.

Tasks:

- Add list and filters for status, due date, assignee, related record type, priority, and subcontractor.
- Add overdue calculation.
- Add bulk reminder action for selected open or overdue requests.
- Add export or report section for evidence request status.
- Add role-aware views for requester, assignee, auditor, and advisor.

Acceptance criteria:

- Dashboard shows only evidence requests in the current tenant.
- Filters return requests by status, due date, assignee, related type, and priority.
- Overdue requests are calculated from due date and current status.
- Bulk reminders create notifications without changing request status.
- Auditors can view approved or accepted evidence request records but cannot modify them.

## 27. CMMC Level 2 Readiness Expansion

### Use Case

As a DoD supplier, I need richer CMMC Level 2 readiness tracking so that control implementation, evidence, responsibility, and readiness gaps can be managed before assessment.

### User Stories

#### Story 27.1: Level 2 Control Assessment Detail

As a security owner, I want detailed Level 2 control assessment fields so that readiness is tracked beyond simple status.

Tasks:

- Add fields for assessment objective status, implementation status, evidence status, inherited responsibility, external service provider responsibility, notes, assessment date, and assessor.
- Add control detail UI for Level 2 controls.
- Add validation for allowed status values.
- Add history for status changes.
- Add audit events for control assessment updates.

Acceptance criteria:

- Authorized user can update Level 2 control assessment detail.
- Control detail stores implementation, evidence, inherited, ESP responsibility, notes, assessment date, and assessor.
- Status history is retained.
- Control updates are tenant-scoped.
- Control assessment updates are audit logged.

#### Story 27.2: Responsibility Matrix

As a security owner, I want a responsibility matrix for internal teams and external service providers so that CMMC control ownership is explicit.

Tasks:

- Add responsibility assignments for organization, MSP/ESP, cloud provider, subcontractor, and shared responsibility.
- Link responsibility to CMMC controls and evidence requests.
- Add matrix view grouped by control family and responsible party.
- Add export for review with advisors or MSPs.
- Add validation that controls with external responsibility include provider name or notes.

Acceptance criteria:

- User can assign responsible party for each Level 2 control.
- Matrix shows control, responsibility type, owner, provider, evidence status, and notes.
- Controls marked external or shared require provider or responsibility notes.
- Responsibility changes are audit logged.
- Matrix export reflects current tenant data.

#### Story 27.3: Readiness Gap Prioritization

As a compliance manager, I want Level 2 gaps prioritized so that limited resources focus on the most important readiness work.

Tasks:

- Define gap inputs for control status, evidence status, due date, risk level, CUI relevance, inherited responsibility, and assessment objective coverage.
- Calculate gap priority such as critical, high, medium, low, and needs review.
- Show prioritized gaps on CMMC dashboard.
- Allow creating POA&M or task items from gaps.
- Add tests for priority rules.

Acceptance criteria:

- Gap priority is calculated from documented inputs.
- Dashboard lists gaps by priority with reason codes.
- User can create a POA&M item or task from a gap.
- Priority recalculates when control or evidence status changes.
- Priority rules are covered by automated tests.

#### Story 27.4: Level 2 Readiness Report

As a compliance manager, I want a Level 2 readiness report with draft-only language and source context so that leadership and advisors can review progress.

Tasks:

- Add report sections for control status, evidence status, gaps, POA&M items, responsibility matrix, and source references.
- Mark report output as readiness tracking and not certification or assessment determination.
- Include generated date, tenant, source control version, and reviewer metadata.
- Add export to existing report format.
- Add permission checks for report generation and viewing.

Acceptance criteria:

- Authorized user can generate a Level 2 readiness report.
- Report includes control status, evidence status, gaps, POA&M items, responsibility matrix, source references, and generated date.
- Report contains no pass/fail certification language.
- Report uses tenant-scoped data only.
- Report generation is audit logged.

## 28. Extraction Content Test Set

### Use Case

As the delivery and compliance team, we need a representative content test set so that automated clause extraction can be measured for precision, recall, and regressions before customers rely on it.

### User Stories

#### Story 28.1: Curated Test Document Set

As a QA owner, I want representative synthetic, redacted, or otherwise allowed contract documents and expected clause labels so that extraction accuracy can be evaluated consistently.

Tasks:

- Create approved test corpus using public, synthetic, or customer-approved non-CUI documents.
- Label expected clause citations, source locations, titles, and flow-down indicators.
- Store test metadata including document type, agency/source family, contract type, and known limitations.
- Add review workflow for label quality.
- Document data handling rules for the test set.

Acceptance criteria:

- Test corpus contains only public, synthetic, or explicitly approved non-CUI documents.
- Each labeled document includes expected clause citations and source locations when available.
- Test metadata identifies document type, source family, and limitations.
- Label set is reviewed before use as a benchmark.
- Test set data handling rules are documented.

#### Story 28.2: Precision And Recall Evaluation

As a QA owner, I want automated extraction evaluation so that the team can measure whether clause detection is improving or regressing.

Tasks:

- Build evaluation runner that compares extracted candidates against expected labels.
- Calculate precision, recall, false positives, false negatives, and unmatched expected clauses.
- Generate machine-readable and human-readable results.
- Add thresholds for blocking release or flagging review.
- Add CI or scheduled execution for the extraction test set.

Acceptance criteria:

- Evaluation runner produces precision, recall, false positive, and false negative metrics.
- Results identify missed and extra clause detections by document.
- Threshold failures are visible in CI or scheduled test output.
- Metrics are stored or published for trend review.
- Evaluation can run without customer data.

#### Story 28.3: Extraction Regression Review

As a compliance content owner, I want failed extraction cases reviewed so that matcher, library, and label improvements are tracked deliberately.

Tasks:

- Add workflow for reviewing missed clauses and false positives.
- Classify failures as parser issue, matcher issue, library gap, label issue, source document quality, or expected limitation.
- Create follow-up tasks from reviewed failures.
- Track status and resolution notes.
- Add summary report for release readiness.

Acceptance criteria:

- Each reviewed failure has a classification, owner, status, and resolution note.
- Follow-up tasks can be created from failures.
- Resolved failures are linked to matcher, library, parser, or label updates when applicable.
- Release summary shows open extraction risks and metric trends.
- Regression review records are audit logged or otherwise traceable.

## Definition Of Ready

A development story is ready when:

- The user role and business goal are clear.
- Required data fields and source systems are identified.
- Tenant isolation, RBAC, audit logging, and CUI/data-handling implications are understood.
- Acceptance criteria are testable.
- Dependencies are known.
- Compliance content stories identify whether expert review is required.

## Definition Of Done

A development story is done when:

- Acceptance criteria pass.
- Backend and frontend behavior are implemented where applicable.
- Tenant isolation and RBAC are enforced server-side.
- Sensitive actions are audit logged.
- Unit, integration, or end-to-end tests are added according to risk.
- Empty, loading, validation, and error states are handled.? ********* this will be handled later. So: not a blocker. Keep it as a follow-up hardening item.
- Accessibility basics are checked for user-facing UI. ? ********* this will be handled later. So: not a blocker. Keep it as a follow-up hardening item.
- Documentation, release notes, or content metadata are updated when behavior changes. ? ********* this will be handled later. So: not a blocker. Keep it as a follow-up hardening item.
- Product owner or delegated reviewer accepts the story.? ********* this will be handled later. So: not a blocker. Keep it as a follow-up hardening item.

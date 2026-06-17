# Development Phase Use Cases, Stories, Tasks, And Acceptance Criteria

This backlog expands the Phase 1 MVP development phase into a sequential delivery plan. It assumes the MVP posture is No-CUI / compliance management only and that production compliance content is reviewed by qualified subject matter experts before publication.

## Delivery Sequence

| Sequence | Process | Primary Outcome |
| --- | --- | --- |
| 1 | Delivery foundation | Team can build, test, review, and deploy consistently. |
| 2 | Tenant, identity, and RBAC | Each customer works inside an isolated tenant with role-based access. |
| 3 | Authenticated application shell | Users can navigate the SaaS workspace and call protected APIs. |
| 4 | No-CUI controls | Users are warned and technically guided away from uploading CUI. |
| 5 | Audit logging | Sensitive actions are traceable from the beginning. |
| 6 | Compliance content foundation | Source-backed clauses and obligations can be loaded, reviewed, and published. |
| 7 | Company compliance profile | A contractor can enter the business facts that drive compliance workflows. |
| 8 | Contract intake | A contractor can create contract records and attach non-CUI source materials. |
| 9 | Manual clause tagging | A user can map contract clauses to curated obligations. |
| 10 | Obligation dashboard | A user can see what applies, who owns it, and what evidence is needed. |
| 11 | Task and compliance calendar | Obligations, renewals, and deadlines become assigned work. |
| 12 | Evidence vault | Users can store and link non-CUI evidence to obligations and controls. |
| 13 | CMMC readiness tracker | DoD suppliers can track Level 1 and Level 2 readiness work. |
| 14 | Subcontractor flow-down tracker | Prime and subcontract users can track supplier obligations and evidence. |
| 15 | Reports | Users can generate status, obligation, CMMC, evidence, and subcontractor reports. |
| 16 | Notifications | Users receive reminders for deadlines, renewals, and assigned work. |
| 17 | MVP hardening and release readiness | The pilot release is tested, secure, observable, and deployable. |

## Acceptance Criteria Testability Standard

Every story acceptance criterion must be testable before the story can be treated as done. A criterion is testable only when it identifies:

- The actor or system under test.
- The action, state, or input being exercised.
- The observable result, persisted record, API response, UI state, audit event, report/export output, or blocked behavior.
- The relevant invariant when applicable: tenant isolation, server-side RBAC, audit logging, No-CUI guardrails, source traceability, or standard error handling.

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
- Documentation points to the No-CUI MVP posture.

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

## 4. No-CUI Controls

### Use Case

As the product owner, I need the MVP to clearly prohibit CUI uploads so that customers do not mistake the product for a CUI-ready enclave.

### User Stories

#### Story 4.1: No-CUI Acknowledgement

As a user, I want to understand the upload limitation before using the product so that I know what content is prohibited.

Tasks:

- Add No-CUI notice content to onboarding and upload workflows.
- Record acknowledgement with user, tenant, timestamp, and notice version.
- Add a way to retrieve current acknowledgement status.
- Add UI prompt when acknowledgement is missing.

Acceptance criteria:

- User sees a No-CUI notice before first upload.
- User must acknowledge the notice before upload is enabled.
- Acknowledgement is audit logged.
- Notice copy states that the MVP is compliance management only and is not ready to store CUI.

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

As a contracts admin, I want to upload non-CUI contract documents and record document metadata so that source materials are available for review.

Tasks:

- Add document metadata model for solicitation, contract, subcontract, purchase order, SOW, flow-down attachment, wage determination, DD Form 254 metadata, and CUI marking guide metadata.
- Add upload workflow with No-CUI acknowledgement.
- Store file metadata and object storage reference.
- Add scan status and validation status.

Acceptance criteria:

- Upload is disabled until No-CUI acknowledgement is complete.
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

As a contractor, I need a secure non-CUI evidence vault so that I can prove what I did for contracts, clauses, CMMC controls, subcontractors, and audits.

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

As a contributor, I want to upload approved non-CUI evidence files so that compliance proof is attached to the right work.

Tasks:

- Add file upload to evidence records.
- Enforce allowed file types, size limits, No-CUI acknowledgement, and malware scan status.
- Store file version metadata.
- Add download permissions.

Acceptance criteria:

- Upload requires No-CUI acknowledgement.
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

- Confirm No-CUI notice, terms, support path, and prohibited upload guidance.
- Confirm backups, restore test, logs, alerts, and rollback plan.
- Confirm malware scanning path or explicit MVP limitation.
- Confirm expert-reviewed compliance content for launch obligations.
- Confirm release notes and known limitations.

Acceptance criteria:

- Checklist is complete before production launch.
- Known limitations are documented.
- Launch content has source URLs and review metadata.
- Rollback plan is documented and tested in staging.

## Definition Of Ready

A development story is ready when:

- The user role and business goal are clear.
- Required data fields and source systems are identified.
- Tenant isolation, RBAC, audit logging, and No-CUI implications are understood.
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

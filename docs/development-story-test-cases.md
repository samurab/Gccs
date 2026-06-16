# Development Story Test Cases

These test cases cover every sprint user story in [development-story-prompts.md](development-story-prompts.md). They are intended to be used with backend xUnit tests, frontend Vitest/React Testing Library tests, API/integration tests, and selected end-to-end smoke tests.

Common expectations for all functional stories:

- Tenant-owned reads and writes are scoped to the current tenant.
- Restricted actions are denied server-side, even when UI controls are hidden.
- Compliance-relevant create, update, delete, status, upload, approval, report, and notification actions are audit logged.
- No-CUI controls are preserved for all upload workflows.
- User-facing errors are clear and use the standard API/UI error pattern.

## 1. Delivery Foundation

### Story 1.1: Repository And Project Structure

- **TC-1.1.1 - Documented structure is complete:** Verify README/docs identify `apps/api`, `apps/web`, `src/Gccs.Domain`, `src/Gccs.Application`, `src/Gccs.Infrastructure`, `packages/compliance-content`, `docs`, and `infra`, with ownership boundaries for each.
- **TC-1.1.2 - Clean checkout build commands work:** From a clean checkout, run documented restore/build/test commands and verify backend and frontend projects build successfully.
- **TC-1.1.3 - Compliance logic is not UI-only:** Inspect implemented workflows and tests to confirm compliance decisions live in domain/application/API layers, with UI acting as a client.
- **TC-1.1.4 - No-CUI posture is visible:** Verify developer docs and setup guidance explicitly describe the MVP as No-CUI / compliance management only.

### Story 1.2: Local Development Services

- **TC-1.2.1 - One-command local services startup:** Run the documented local services command and verify PostgreSQL, Redis, object storage, and malware-scanning placeholder health checks pass.
- **TC-1.2.2 - API dependency connectivity:** Start the API with local environment values and verify health endpoints report database, cache, storage, and scanner connectivity.
- **TC-1.2.3 - Missing config fails clearly:** Remove each required environment variable one at a time and verify startup fails with an actionable missing-config message.
- **TC-1.2.4 - No committed production secrets:** Scan local config examples and committed files for production credentials, tokens, or real customer data.

### Story 1.3: Continuous Integration Baseline

- **TC-1.3.1 - Pull request validation runs:** Open or simulate a PR and verify CI runs restore, backend build, frontend build, lint, tests, migration validation, and scans.
- **TC-1.3.2 - Failing steps block merge:** Introduce a controlled failing lint/test/build condition and verify branch protection marks the PR unmergeable.
- **TC-1.3.3 - CI logs are actionable:** Verify logs identify the failing project, command, and step without requiring maintainers to inspect unrelated job output.
- **TC-1.3.4 - Security scan failures are visible:** Trigger or mock a dependency/secret scan finding and verify reviewers can see the failure in PR checks.

## 2. Tenant, Identity, And RBAC

### Story 2.1: Tenant Creation

- **TC-2.1.1 - Create tenant with required metadata:** Create a tenant and verify unique ID, display name, status, created date, and updated date are persisted.
- **TC-2.1.2 - Tenant-owned records include tenant ID:** Create tenant-owned sample records and verify each stores the correct tenant ID.
- **TC-2.1.3 - Cross-tenant API access is denied:** As a user in tenant A, request tenant B data by ID and verify a 404/403-style response with no data leakage.
- **TC-2.1.4 - Tenant lifecycle audit events:** Create and change tenant status, then verify audit events include tenant, actor, action, timestamps, and before/after status.

### Story 2.2: User Memberships

- **TC-2.2.1 - Explicit multi-tenant membership:** Assign one user to two tenants and verify each membership is visible only when that tenant is active.
- **TC-2.2.2 - Tenant member list is scoped:** Seed users in two tenants and verify each tenant's member list excludes the other tenant's users.
- **TC-2.2.3 - Duplicate membership rejected:** Attempt to assign the same user to the same tenant twice and verify validation prevents duplication.
- **TC-2.2.4 - Membership audit events:** Add, update, or deactivate a membership and verify the action is audit logged.

### Story 2.3: User Invitations

- **TC-2.3.1 - Admin creates invitation:** As a tenant admin, invite a user by email and role; verify token, expiration, pending status, and local notification/email placeholder.
- **TC-2.3.2 - Non-admin cannot invite:** As contributor/auditor, call the invitation endpoint directly and verify permission denial.
- **TC-2.3.3 - Invalid invitation acceptance blocked:** Try accepting expired and revoked invitations and verify no membership is created.
- **TC-2.3.4 - Invitation state audit trail:** Create, accept, expire, and revoke invitations; verify each action is audit logged.

### Story 2.4: Role-Based Permissions

- **TC-2.4.1 - Server-side permission matrix:** For each role, call representative profile, contract, obligation, task, evidence, report, subcontractor, and admin endpoints and verify allowed/denied results match the permission matrix.
- **TC-2.4.2 - UI hides restricted actions:** Render workspace pages for each role and verify unavailable actions are not shown.
- **TC-2.4.3 - Clear permission failure:** Directly call a restricted action and verify the response includes a consistent authorization error message.
- **TC-2.4.4 - Auditor read-only access:** Verify auditor can view approved evidence packages but cannot create, update, approve, delete, or assign tenant data.

## 3. Authenticated Application Shell

### Story 3.1: Protected API Access

- **TC-3.1.1 - Unauthenticated requests rejected:** Call protected endpoints without auth context and verify authentication failure.
- **TC-3.1.2 - Current user and tenant resolved:** Call a protected endpoint with valid dev auth/auth token and verify handlers receive the expected user ID and tenant ID.
- **TC-3.1.3 - Missing tenant context error:** Call with user context but no active tenant and verify a clear, standard error response.
- **TC-3.1.4 - Correlation IDs included:** Verify successful and failed API responses/logs include a request correlation ID.

### Story 3.2: SaaS Navigation Shell

- **TC-3.2.1 - Authenticated landing route:** Sign in and verify the first screen is the workspace/dashboard, not a marketing page.
- **TC-3.2.2 - Keyboard-accessible navigation:** Use keyboard-only navigation to reach each primary route and verify focus states and activation work.
- **TC-3.2.3 - Role-aware navigation:** Render navigation for restricted roles and verify hidden items cannot be accessed through visible links.
- **TC-3.2.4 - Loading, empty, and error states:** Mock loading, empty, and failed route data and verify understandable states are displayed.

## 4. No-CUI Controls

### Story 4.1: No-CUI Acknowledgement

- **TC-4.1.1 - Notice shown before first upload:** With no acknowledgement, open an upload workflow and verify the No-CUI notice is displayed.
- **TC-4.1.2 - Upload disabled until acknowledgement:** Attempt upload before acknowledgement and verify UI and API both block it.
- **TC-4.1.3 - Acknowledgement persisted:** Acknowledge the notice and verify user, tenant, timestamp, and notice version are stored.
- **TC-4.1.4 - Acknowledgement audit and copy:** Verify audit event creation and confirm notice copy says the MVP is compliance management only and not ready to store CUI.

### Story 4.2: Upload Guardrails

- **TC-4.2.1 - Disallowed file type rejected:** Upload an unsupported file extension/content type and verify server-side rejection with no usable evidence record.
- **TC-4.2.2 - Oversized file rejected:** Upload a file over the configured size limit and verify server-side rejection.
- **TC-4.2.3 - Scan status recorded:** Upload a valid file and verify metadata records validation and malware scan placeholder status.
- **TC-4.2.4 - Failed upload audit:** Force validation or scan failure and verify the rejected upload is audit logged and not usable.

## 5. Audit Logging

### Story 5.1: Append-Only Audit Events

- **TC-5.1.1 - Sensitive action creates event:** Perform representative sensitive actions and verify audit events are written with tenant, actor, action, entity, timestamp, and summary.
- **TC-5.1.2 - Audit events are append-only:** Attempt to update or delete audit events through normal APIs and verify no mutation path exists.
- **TC-5.1.3 - Critical audit failure surfaces:** Simulate audit writer failure during a critical action and verify the action fails or surfaces a clear critical error.
- **TC-5.1.4 - Request metadata captured:** Verify source IP/correlation ID/request metadata is stored when available.

### Story 5.2: Audit Log Viewer

- **TC-5.2.1 - Admin sees own tenant events:** As admin/owner/advisor, view audit logs and verify only current tenant events appear.
- **TC-5.2.2 - Unauthorized access blocked:** As contributor/auditor, call the audit log endpoint and verify access is denied.
- **TC-5.2.3 - Pagination works:** Seed more events than one page and verify page size, next/previous behavior, and stable ordering.
- **TC-5.2.4 - Filters are correct and tenant scoped:** Filter by actor, action, date range, and entity type; verify only matching current-tenant events return.

## 6. Compliance Content Foundation

### Story 6.1: Obligation Schema

- **TC-6.1.1 - Source URL required for publish:** Attempt to publish an obligation without source URL and verify validation fails.
- **TC-6.1.2 - Last reviewed date required:** Attempt to publish without last reviewed date and verify validation fails.
- **TC-6.1.3 - Required metadata enforced:** Verify published obligations require risk, owner, confidence, trigger logic, required actions, flow-down flag, expert-review-required flag, and review state.
- **TC-6.1.4 - Evidence examples link:** Link evidence examples to an obligation and verify they are returned with the obligation.

### Story 6.2: Content Import

- **TC-6.2.1 - Valid content imports:** Import a valid compliance content package and verify clauses/obligations are created with source metadata, review metadata, and expert-review-required flags.
- **TC-6.2.2 - Invalid content fails clearly:** Import schema-invalid JSON and verify actionable errors identify file/path/field.
- **TC-6.2.3 - Import is idempotent:** Run the same import twice and verify duplicate clauses/obligations are not created.
- **TC-6.2.4 - Import logs captured:** Verify successful and failed imports produce logs/failure reports useful for maintainers.

### Story 6.3: Content Review State

- **TC-6.3.1 - Draft hidden from customer views:** Seed draft and published content; verify only published content appears in customer-facing search/mapping.
- **TC-6.3.2 - Expert review required for publish:** Attempt to publish expert-review-required content without reviewer/date and verify validation fails.
- **TC-6.3.3 - Retired content excluded from new mappings:** Retire content and verify it cannot be selected for new clause mappings.
- **TC-6.3.4 - State changes audit logged:** Move content through draft, in_review, approved, published, and retired states and verify audit events.

## 7. Company Compliance Profile

### Story 7.1: Create Company Profile

- **TC-7.1.1 - Required field validation:** Attempt to complete a profile without required fields and verify validation messages.
- **TC-7.1.2 - Draft save allows missing non-critical fields:** Save a partial draft and verify it persists without being marked complete.
- **TC-7.1.3 - Completion percentage updates:** Add/remove profile data and verify completion percentage recalculates.
- **TC-7.1.4 - Profile changes audited and scoped:** Create/update profile as tenant A and verify audit events plus no visibility from tenant B.

### Story 7.2: NAICS And Size Status

- **TC-7.2.1 - Multiple NAICS codes:** Add multiple valid NAICS codes and verify all are stored on the profile.
- **TC-7.2.2 - Single primary NAICS:** Mark one NAICS as primary, then switch primary and verify only one primary remains.
- **TC-7.2.3 - Size status per NAICS:** Store different size statuses and bases per NAICS and verify detail display.
- **TC-7.2.4 - Missing size status gap:** Add a NAICS without size status and verify profile gaps warn the user.

### Story 7.3: Certification Tracking

- **TC-7.3.1 - Supported and custom certification types:** Add 8(a), WOSB, EDWOSB, HUBZone, SDVOSB, SDB, and custom certifications.
- **TC-7.3.2 - Expiring certification renewal task:** Add a certification with an upcoming expiration and verify a calendar renewal task is generated.
- **TC-7.3.3 - Expired certification flagged:** Add an expired certification and verify dashboard/profile flags it.
- **TC-7.3.4 - Certification audit events:** Create, update, and delete/archive certifications and verify audit events.

## 8. Contract Intake

### Story 8.1: Create Contract Record

- **TC-8.1.1 - Create draft and active contracts:** Create records in draft and active status with required contract fields and verify persistence.
- **TC-8.1.2 - Tenant-scoped contract list:** Seed contracts in two tenants and verify each list only returns current-tenant contracts.
- **TC-8.1.3 - Contract detail key fields:** Open detail page and verify key dates, role, contract type, agency/prime, and data handling posture.
- **TC-8.1.4 - Contract create/update audit:** Verify create and update actions write audit events.

### Story 8.2: Contract Document Metadata And Upload

- **TC-8.2.1 - Upload requires No-CUI acknowledgement:** Attempt upload without acknowledgement and verify disabled UI plus API rejection.
- **TC-8.2.2 - Metadata linked to contract:** Upload valid non-CUI document metadata and verify document type, storage reference, scan status, and contract link.
- **TC-8.2.3 - Disallowed file rejected:** Upload an unsupported file and verify rejection with no usable document.
- **TC-8.2.4 - Upload/delete audit events:** Upload and delete a contract document and verify both actions are audit logged.

### Story 8.3: Contract Dates And Deliverables

- **TC-8.3.1 - Deliverables on contract detail:** Create deliverables with owner, due date, status, and description; verify they appear on contract detail.
- **TC-8.3.2 - Due dates appear on calendar:** Verify deliverable due dates create or appear as calendar items.
- **TC-8.3.3 - Overdue deliverables flagged:** Seed a past-due incomplete deliverable and verify overdue styling/status.
- **TC-8.3.4 - Status changes audit logged:** Change deliverable status and verify audit event creation.

## 9. Manual Clause Tagging

### Story 9.1: Clause Library Search

- **TC-9.1.1 - Search by number, title, category:** Search using clause number, title text, and category filters and verify expected results.
- **TC-9.1.2 - Only published clauses mappable:** Seed draft and published clauses and verify draft clauses do not appear in customer mapping.
- **TC-9.1.3 - Source metadata visible:** Verify each result shows source URL and last reviewed date.
- **TC-9.1.4 - Tenant/content safety:** Verify search does not expose draft, retired, or other tenant custom content.

### Story 9.2: Attach Clause To Contract

- **TC-9.2.1 - Attach published clause:** Attach a published clause to a contract with reason and source document reference.
- **TC-9.2.2 - Duplicate attachment rejected:** Attach the same clause twice to the same contract and verify duplicate prevention.
- **TC-9.2.3 - Removal requires reason:** Attempt removal without reason and verify validation fails; remove with reason succeeds.
- **TC-9.2.4 - Add/remove audit and tenant scope:** Verify add/remove events are audit logged and cross-tenant contract/clause IDs are denied.

### Story 9.3: Generate Obligations From Clause

- **TC-9.3.1 - Clause creates obligations:** Attach a clause with mapped templates and verify contract-specific obligations are generated.
- **TC-9.3.2 - Generated links and metadata:** Verify generated obligations link to contract/clause and include source URL, owner, action, evidence examples, risk, confidence, and review metadata.
- **TC-9.3.3 - Default tasks generated:** For templates requiring default tasks, verify tasks are created and linked.
- **TC-9.3.4 - Generation idempotent:** Re-run generation or reprocess the same attachment and verify duplicates are not created.

## 10. Obligation Dashboard

### Story 10.1: Obligation List And Filters

- **TC-10.1.1 - Tenant-scoped dashboard:** Seed obligations in multiple tenants and verify only current-tenant obligations appear.
- **TC-10.1.2 - Filters return correct data:** Filter by contract, risk, owner, status, module, due date, and source; verify results.
- **TC-10.1.3 - High-risk/overdue visible:** Verify high-risk and overdue obligations are visually distinct and accessible.
- **TC-10.1.4 - Empty state guides setup:** With no obligations, verify empty state points to company profile or contract intake.

### Story 10.2: Obligation Detail

- **TC-10.2.1 - Source-backed details shown:** Open obligation detail and verify summary, trigger, action, owner, evidence examples, flow-down, source link, confidence, last reviewed, and expert review flag.
- **TC-10.2.2 - Linked tasks/evidence visible:** Link tasks and evidence and verify they display on detail.
- **TC-10.2.3 - Status update workflow:** Change obligation status and verify persistence and dashboard update.
- **TC-10.2.4 - Status audit and tenant scope:** Verify status changes are audit logged and cross-tenant detail access is denied.

### Story 10.3: Ownership Assignment

- **TC-10.3.1 - Assign user owner:** Assign an obligation to a tenant member and verify dashboard/detail reflects the user.
- **TC-10.3.2 - Assign role owner:** Assign an obligation to a role and verify dashboard/detail reflects the role.
- **TC-10.3.3 - Unauthorized assignment denied:** As an unauthorized role, call assignment endpoint and verify denial.
- **TC-10.3.4 - Assignment audit/notification:** Verify assignment changes are audit logged and notification is emitted when enabled.

## 11. Task And Compliance Calendar

### Story 11.1: Task Management

- **TC-11.1.1 - Create linked tasks:** Create tasks linked to obligations, contracts, controls, evidence, subcontractors, and certifications.
- **TC-11.1.2 - Valid status transitions:** Move tasks through open, in_progress, blocked, completed, canceled, and reopened states according to rules.
- **TC-11.1.3 - Tenant-scoped updates:** Attempt to update another tenant's task and verify denial/no leakage.
- **TC-11.1.4 - Status changes audited:** Verify task status changes create audit events.

### Story 11.2: Calendar View

- **TC-11.2.1 - Aggregated calendar items:** Verify calendar shows tasks, renewals, reports, contract deadlines, deliverables, and policy reviews.
- **TC-11.2.2 - Calendar filters:** Filter by owner, status, risk, contract, and module and verify matching items.
- **TC-11.2.3 - Overdue visual treatment:** Verify overdue items are visually distinct and accessible to screen readers.
- **TC-11.2.4 - Tenant-scoped calendar:** Verify calendar excludes other tenant items.

### Story 11.3: Renewal Generation

- **TC-11.3.1 - Renewal tasks from dated records:** Generate tasks for SAM, certifications, evidence, insurance, policy review, and CMMC affirmation dates.
- **TC-11.3.2 - Duplicate prevention:** Run renewal generation twice and verify the same source/due date does not create duplicates.
- **TC-11.3.3 - Lead time calculation:** Verify default and configured lead times produce correct reminder/due dates.
- **TC-11.3.4 - Source record links:** Verify generated tasks link back to the originating profile/evidence/certification/etc.

## 12. Evidence Vault

### Story 12.1: Evidence Metadata

- **TC-12.1.1 - Create evidence metadata:** Create an evidence record with title, type, owner, approval status, expiration date, tags, description, and source links.
- **TC-12.1.2 - Link to multiple records:** Link evidence to multiple obligations/controls and verify reuse from all linked views.
- **TC-12.1.3 - Folderless tag filtering:** Add tags and verify list/detail filtering by tags works without folder dependency.
- **TC-12.1.4 - Expiration task and audit:** Add expiration date and verify task generation plus metadata change audit events.

### Story 12.2: Evidence File Upload

- **TC-12.2.1 - Upload requires acknowledgement:** Attempt evidence upload before No-CUI acknowledgement and verify it is blocked.
- **TC-12.2.2 - Usability gated by validation/scan:** Upload file and verify it is not usable until validation and scan status allow it.
- **TC-12.2.3 - Version history preserved:** Upload a replacement file and verify a new version is created without overwriting previous metadata.
- **TC-12.2.4 - Download/delete permissions and audit:** Verify allowed users can download/delete per RBAC and all actions are audit logged.

### Story 12.3: Evidence Approval

- **TC-12.3.1 - Authorized approval only:** Verify only configured roles can approve evidence.
- **TC-12.3.2 - Rejection requires reason:** Reject evidence without comment/reason and verify validation fails.
- **TC-12.3.3 - Approved evidence in reports:** Approve evidence and verify it becomes eligible for report/evidence package inclusion.
- **TC-12.3.4 - Approval audit:** Verify approve/reject/archive/expire decisions are audit logged.

## 13. CMMC Readiness Tracker

### Story 13.1: CMMC Level Selection

- **TC-13.1.1 - Create readiness assessment:** Create assessment for Level 1 and Level 2 and verify target level, status, dates, and owner.
- **TC-13.1.2 - Link to profile/contracts:** Link assessment to company profile and contracts and verify detail display.
- **TC-13.1.3 - Completion progress summary:** Add control statuses and verify assessment progress recalculates.
- **TC-13.1.4 - Assessment changes audited:** Verify create/update/status changes create audit events.

### Story 13.2: Control Readiness

- **TC-13.2.1 - Controls loaded by level:** Verify Level 1 controls and Level 2 mappings load for the selected assessment scope.
- **TC-13.2.2 - Control status update:** Set control status to not_started, implemented, partially_implemented, not_applicable, and needs_review.
- **TC-13.2.3 - Link evidence/tasks/assets/POA&M:** Link related records to a control and verify they display on detail.
- **TC-13.2.4 - Source baseline visible and progress updated:** Verify baseline source appears and status contributes to assessment progress.

### Story 13.3: POA&M Items

- **TC-13.3.1 - Create POA&M linked to control:** Create item with control, gap, remediation plan, owner, due date, risk, and status.
- **TC-13.3.2 - POA&M links to task/calendar:** Verify linked task is created or associated and appears on calendar.
- **TC-13.3.3 - Open/overdue summary:** Seed open and overdue items and verify CMMC summary counts and flags.
- **TC-13.3.4 - POA&M audit events:** Verify create/update/status changes are audit logged.

### Story 13.4: Annual Affirmation Tracker

- **TC-13.4.1 - Affirmation due date on calendar:** Set affirmation due date and verify it appears on calendar.
- **TC-13.4.2 - Reminder task generated:** Verify upcoming affirmation creates a reminder task.
- **TC-13.4.3 - Evidence link:** Link evidence to affirmation record and verify it displays.
- **TC-13.4.4 - Affirmation updates audited:** Verify last/due date and evidence changes are audit logged.

## 14. Subcontractor Flow-Down Tracker

### Story 14.1: Subcontractor Profile

- **TC-14.1.1 - Create/update profile:** Create subcontractor with legal name, POC, role, statuses, flags, dates, and workshare percentage.
- **TC-14.1.2 - Link to contracts:** Link subcontractor to one or more contracts and verify list/detail display.
- **TC-14.1.3 - Sensitive flags visible:** Verify CUI access and export-control flags are prominent but do not imply CUI storage.
- **TC-14.1.4 - Tenant scope and audit:** Verify cross-tenant access is denied and changes are audit logged.

### Story 14.2: Flow-Down Clause Tracking

- **TC-14.2.1 - Assign flow-down clauses:** Assign required clauses from contract obligations to subcontractor.
- **TC-14.2.2 - Status visibility:** Update statuses required, sent, acknowledged, signed, waived, and not_applicable; verify display by subcontractor and contract.
- **TC-14.2.3 - Link signed evidence:** Link approved signed evidence and verify it appears on the flow-down record.
- **TC-14.2.4 - Status audit:** Verify flow-down assignment/status changes are audit logged.

### Story 14.3: Subcontractor Evidence Requests

- **TC-14.3.1 - Create evidence request:** Create request with requested item, due date, status, recipient, and linked obligation.
- **TC-14.3.2 - Request appears on calendar:** Verify request due date appears on calendar.
- **TC-14.3.3 - Received evidence satisfies request:** Link received evidence and verify request status/completion updates.
- **TC-14.3.4 - Overdue request flagged:** Seed overdue request and verify list/calendar/dashboard warning.

## 15. Reports

### Story 15.1: Compliance Status Report

- **TC-15.1.1 - Generate current status report:** Generate report and verify obligation status, overdue tasks, evidence status, CMMC progress, subcontractor gaps, and high-risk items.
- **TC-15.1.2 - Tenant-scoped report:** Verify report excludes other tenant data.
- **TC-15.1.3 - Timestamp and snapshot:** Verify generation timestamp and snapshot metadata are stored.
- **TC-15.1.4 - Report audit event:** Verify generation is audit logged.

### Story 15.2: Contract Obligation Matrix

- **TC-15.2.1 - Generate matrix for one contract:** Select a contract and verify clause, source, obligation, owner, status, risk, due date, evidence, and flow-down columns.
- **TC-15.2.2 - Source metadata included:** Verify source links and last reviewed dates are present.
- **TC-15.2.3 - Flow-down indicators included:** Verify obligations requiring flow-down are clearly identified.
- **TC-15.2.4 - Export matches screen:** Export the matrix and compare exported rows/fields to on-screen data.

### Story 15.3: CMMC Readiness Report

- **TC-15.3.1 - Progress by control family/category:** Generate report and verify control status rollups by family/category.
- **TC-15.3.2 - POA&M items included:** Verify open POA&M items, gaps, evidence links, and affirmation dates appear.
- **TC-15.3.3 - Evidence links permission filtered:** As restricted user, verify inaccessible evidence links are omitted or blocked.
- **TC-15.3.4 - RBAC and snapshot history:** Verify report access is role-protected and generated snapshots are retained.

### Story 15.4: Evidence Package

- **TC-15.4.1 - Package selected scope:** Generate package by selected obligations, contract, CMMC controls, or subcontractor scope.
- **TC-15.4.2 - Approved evidence included by default:** Verify draft/rejected evidence is excluded unless explicitly allowed by authorized user.
- **TC-15.4.3 - Manifest complete:** Verify manifest includes title, evidence type, linked obligation/control, approval state, and timestamp.
- **TC-15.4.4 - Read-only package and audit:** Verify package view is read-only and generation is audit logged.

### Story 15.5: Subcontractor Compliance Report

- **TC-15.5.1 - Contract-filtered report:** Generate report filtered by contract and verify subcontractor data matches scope.
- **TC-15.5.2 - Missing/overdue evidence flagged:** Verify missing and overdue subcontractor evidence requests are highlighted.
- **TC-15.5.3 - Flow-down status included:** Verify flow-down statuses appear by subcontractor/contract.
- **TC-15.5.4 - Tenant-scoped export:** Export report and verify no other tenant data is included.

## 16. Notifications

### Story 16.1: Notification Preferences

- **TC-16.1.1 - Defaults for new users:** Create users by role and verify default preferences are assigned.
- **TC-16.1.2 - User updates preferences:** Update preferences for assignments, due soon, overdue, evidence requests, renewals, and CMMC affirmation.
- **TC-16.1.3 - Tenant-scoped preferences:** For multi-tenant user, verify tenant-specific preferences do not leak across tenants when applicable.
- **TC-16.1.4 - Preference audit:** Verify preference changes are audit logged.

### Story 16.2: Due-Date Reminders

- **TC-16.2.1 - Upcoming reminders identified:** Run reminder job and verify tasks within configured lead time are selected.
- **TC-16.2.2 - Idempotent delivery:** Run the job twice and verify the same reminder is not sent repeatedly for the same event.
- **TC-16.2.3 - Overdue reminders separate:** Verify overdue reminders are categorized/sent separately from upcoming reminders.
- **TC-16.2.4 - Delivery failure logged:** Simulate notification/email placeholder failure and verify failure is logged without crashing unrelated deliveries.

### Story 16.3: Assignment Notifications

- **TC-16.3.1 - Assignment emits notification:** Assign task, obligation, POA&M item, or evidence request and verify assigned user receives notification.
- **TC-16.3.2 - Notification links to source:** Open notification and verify it navigates to the linked record.
- **TC-16.3.3 - Mark as read:** Mark notification as read and verify state persists.
- **TC-16.3.4 - Linked record authorization:** As an unauthorized user, open notification link and verify access is denied.

## 17. MVP Hardening And Release Readiness

### Story 17.1: End-To-End Pilot Workflow

- **TC-17.1.1 - Complete pilot workflow:** With non-CUI data, onboard tenant/users, create profile, contract, clauses, obligations, tasks, evidence, CMMC records, subcontractors, reports, and notifications.
- **TC-17.1.2 - Role-specific happy paths:** Execute the same workflow with owner, admin, compliance manager, contributor, auditor, and advisor users and verify each can only perform permitted actions.
- **TC-17.1.3 - Reports reflect workflow data:** Generate reports and verify they reflect data created during the end-to-end workflow.
- **TC-17.1.4 - Regression suite captures happy path:** Verify automated regression coverage exists for the pilot workflow's critical path.

### Story 17.2: Security And Tenant Isolation Verification

- **TC-17.2.1 - Cross-tenant access denied globally:** Attempt cross-tenant access for all tenant-owned modules and verify denial.
- **TC-17.2.2 - Direct API RBAC bypass attempts denied:** Call restricted endpoints directly for each role and verify server-side denial.
- **TC-17.2.3 - Tenant filters reviewed/tested:** Verify repository/service tests cover tenant filters for tenant-owned queries.
- **TC-17.2.4 - Security test results documented:** Confirm tenant isolation, RBAC, and audit logging verification results are documented.

### Story 17.3: Staging Environment

- **TC-17.3.1 - CI/CD deploys staging:** Trigger staging deployment and verify API, web, database, storage, cache, queue, secrets, and jobs provision/deploy.
- **TC-17.3.2 - No production data:** Verify staging contains no production customer data or production secrets.
- **TC-17.3.3 - Health checks cover dependencies:** Verify health checks cover API, database, cache, storage, and jobs.
- **TC-17.3.4 - Smoke tests pass post-deploy:** Run staging smoke tests after deployment and verify success/failure is visible in CI/CD.

### Story 17.4: Production Readiness Checklist

- **TC-17.4.1 - Checklist completed before launch:** Verify release cannot proceed until readiness checklist items are complete/approved.
- **TC-17.4.2 - Known limitations documented:** Confirm No-CUI limits, malware scanning limitation/path, support path, and prohibited upload guidance are documented.
- **TC-17.4.3 - Launch content reviewed:** Verify launch obligations have source URLs, last reviewed dates, confidence, and review metadata.
- **TC-17.4.4 - Rollback tested in staging:** Execute or simulate rollback in staging and verify steps, timing, and outcome are documented.

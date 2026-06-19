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

## 4. No-CUI Upload Guardrails

### Story 4.1: Data Handling Acknowledgement

- **TC-4.1.1 - Notice shown before first upload:** With no acknowledgement, open an upload workflow and verify the data handling notice is displayed.
- **TC-4.1.2 - Upload disabled until acknowledgement:** Attempt upload before acknowledgement and verify UI and API both block it.
- **TC-4.1.3 - Acknowledgement persisted:** Acknowledge the notice and verify user, tenant, timestamp, and notice version are stored.
- **TC-4.1.4 - Acknowledgement audit and copy:** Verify audit event creation and confirm notice copy says the MVP is No-CUI / compliance management only and real CUI upload is prohibited until a future approved CUI-ready posture is implemented.

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

- **TC-8.2.1 - Upload requires data handling acknowledgement:** Attempt upload without acknowledgement and verify disabled UI plus API rejection.
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

- **TC-12.2.1 - Upload requires acknowledgement:** Attempt evidence upload before data handling acknowledgement and verify it is blocked.
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
- **TC-17.4.2 - Known limitations documented:** Confirm CUI/data-handling limits, malware scanning limitation/path, support path, and prohibited upload guidance are documented.
- **TC-17.4.3 - Launch content reviewed:** Verify launch obligations have source URLs, last reviewed dates, confidence, and review metadata.
- **TC-17.4.4 - Rollback tested in staging:** Execute or simulate rollback in staging and verify steps, timing, and outcome are documented.

## Phase 2 - Govcon Intelligence

## 18. Automated Clause Extraction

### Story 18.1: Extraction Job Intake

- **TC-18.1.1 - User with contract edit permission can start:** Verify user with contract edit permission can start extraction for a document in the current tenant.
- **TC-18.1.2 - User without contract edit permission receives a:** Verify user without contract edit permission receives a server-side authorization error.
- **TC-18.1.3 - Extraction job stores tenant ID, source document:** Verify extraction job stores tenant ID, source document ID, requester ID, status, and timestamps.
- **TC-18.1.4 - Starting extraction for another tenant's document is:** Verify starting extraction for another tenant's document is denied.
- **TC-18.1.5 - Extraction job creation is audit logged:** Verify extraction job creation is audit logged.

### Story 18.2: Text Extraction And Clause Candidate Detection

- **TC-18.2.1 - Supported text documents produce clause candidates when:** Verify supported text documents produce clause candidates when recognizable clause references are present.
- **TC-18.2.2 - Each candidate includes source document, normalized citation,:** Verify each candidate includes source document, normalized citation, raw extracted text, confidence, and location metadata when available.
- **TC-18.2.3 - Exact matches link to the corresponding clause:** Verify exact matches link to the corresponding clause library record.
- **TC-18.2.4 - Unsupported or unreadable documents produce a failed:** Verify unsupported or unreadable documents produce a failed job with a user-visible reason.
- **TC-18.2.5 - Extracted text and candidates remain tenant-scoped:** Verify extracted text and candidates remain tenant-scoped.

### Story 18.3: Extraction Results Review Screen

- **TC-18.3.1 - View extraction results for documents in the:** Verify user can view extraction results for documents in the current tenant.
- **TC-18.3.2 - Results show citation, confidence, match status, review:** Verify results show citation, confidence, match status, review status, and source location when available.
- **TC-18.3.3 - Accepted candidates create reviewed contract clause links:** Verify accepted candidates create reviewed contract clause links only after user action.
- **TC-18.3.4 - Rejected candidates remain visible in extraction history:** Verify rejected candidates remain visible in extraction history and do not create contract clause links.
- **TC-18.3.5 - Candidate edits and review decisions are audit:** Verify candidate edits and review decisions are audit logged.

## 19. Human Review Workflow

### Story 19.1: Review States For Extracted Clauses

- **TC-19.1.1 - New extraction candidates default to pending review:** Verify new extraction candidates default to pending review.
- **TC-19.1.2 - Only users with clause review permission can:** Verify only users with clause review permission can accept or reject candidates.
- **TC-19.1.3 - Accepted candidates record reviewer, reviewed date, and:** Verify accepted candidates record reviewer, reviewed date, and decision note when provided.
- **TC-19.1.4 - Rejected and superseded candidates do not generate:** Verify rejected and superseded candidates do not generate obligations.
- **TC-19.1.5 - Review state transitions are audit logged:** Verify review state transitions are audit logged.

### Story 19.2: AI-Suggested Obligation Review

- **TC-19.2.1 - AI-suggested obligations are stored with source references,:** Verify AI-suggested obligations are stored with source references, confidence, and draft status.
- **TC-19.2.2 - Draft suggestions are not included in approved:** Verify draft suggestions are not included in approved obligation dashboards or reports.
- **TC-19.2.3 - Approve, revise, reject, or escalate a suggestion:** Verify reviewer can approve, revise, reject, or escalate a suggestion.
- **TC-19.2.4 - Approved suggestions record reviewer, approval date, and:** Verify approved suggestions record reviewer, approval date, and source citations.
- **TC-19.2.5 - Rejected suggestions remain in review history and:** Verify rejected suggestions remain in review history and are audit logged.

### Story 19.3: Expert Escalation Queue

- **TC-19.3.1 - Escalate a candidate or suggested obligation with:** Verify reviewer can escalate a candidate or suggested obligation with a required reason.
- **TC-19.3.2 - Escalated items appear in an expert review:** Verify escalated items appear in an expert review queue.
- **TC-19.3.3 - Assigned expert receives a notification:** Verify assigned expert receives a notification.
- **TC-19.3.4 - Resolution records decision, reviewer, date, and notes:** Verify resolution records decision, reviewer, date, and notes.
- **TC-19.3.5 - Escalated items cannot be published as approved:** Verify escalated items cannot be published as approved until resolved.

## 20. Clause Library Expansion

### Story 20.1: Versioned Clause Records

- **TC-20.1.1 - Clause records include citation, title, source URL,:** Verify clause records include citation, title, source URL, status, last reviewed date, and review owner.
- **TC-20.1.2 - Approved versions can be used for extraction:** Verify approved versions can be used for extraction matching and obligation mapping.
- **TC-20.1.3 - Deprecated or superseded versions are visible in:** Verify deprecated or superseded versions are visible in history but not selected by default for new mappings.
- **TC-20.1.4 - Clause version changes preserve prior version history:** Verify clause version changes preserve prior version history.
- **TC-20.1.5 - Clause changes are audit logged:** Verify clause changes are audit logged.

### Story 20.2: Clause Search And Discovery

- **TC-20.2.1 - Search by exact citation returns the matching:** Verify search by exact citation returns the matching approved clause when present.
- **TC-20.2.2 - Search by title or keyword returns relevant:** Verify search by title or keyword returns relevant approved clauses.
- **TC-20.2.3 - Filters narrow results by source family, obligation:** Verify filters narrow results by source family, obligation area, and flow-down relevance.
- **TC-20.2.4 - Results show source URL, status, and last:** Verify results show source URL, status, and last reviewed date.
- **TC-20.2.5 - Draft or under-review clauses are hidden from:** Verify draft or under-review clauses are hidden from standard users unless they have content review permission.

### Story 20.3: Clause-To-Obligation Mapping

- **TC-20.3.1 - Approved clause mapping can generate an obligation:** Verify approved clause mapping can generate an obligation for a contract.
- **TC-20.3.2 - Mapping requires trigger condition, required action, source:** Verify mapping requires trigger condition, required action, source URL, confidence, and review metadata before approval.
- **TC-20.3.3 - Draft mappings cannot generate customer-visible approved obligations:** Verify draft mappings cannot generate customer-visible approved obligations.
- **TC-20.3.4 - Mapping changes preserve history:** Verify mapping changes preserve history.
- **TC-20.3.5 - Mapping approval and changes are audit logged:** Verify mapping approval and changes are audit logged.

## 21. Applicability Engine

### Story 21.1: Applicability Facts Model

- **TC-21.1.1 - Applicability facts can be derived from existing:** Verify applicability facts can be derived from existing company, contract, clause, and subcontractor records.
- **TC-21.1.2 - Unknown facts are represented explicitly instead of:** Verify unknown facts are represented explicitly instead of inferred as false.
- **TC-21.1.3 - Each fact records source record and last:** Verify each fact records source record and last updated date when available.
- **TC-21.1.4 - Fact model is tenant-scoped:** Verify fact model is tenant-scoped.
- **TC-21.1.5 - Fact definitions are documented:** Verify fact definitions are documented.

### Story 21.2: Rule Evaluation

- **TC-21.2.1 - Rule evaluator returns a result state, explanation,:** Verify rule evaluator returns a result state, explanation, source rule ID, and facts used.
- **TC-21.2.2 - Missing required facts produce insufficient information or:** Verify missing required facts produce insufficient information or needs review rather than a silent positive result.
- **TC-21.2.3 - Rule evaluation is repeatable for the same:** Verify rule evaluation is repeatable for the same inputs.
- **TC-21.2.4 - Evaluation results are tenant-scoped:** Verify evaluation results are tenant-scoped.
- **TC-21.2.5 - Rule evaluator behavior is covered by automated:** Verify rule evaluator behavior is covered by automated tests.

### Story 21.3: Obligation Applicability Updates

- **TC-21.3.1 - Updating a relevant fact reevaluates affected obligations:** Verify updating a relevant fact reevaluates affected obligations.
- **TC-21.3.2 - Dashboard displays the current applicability state:** Verify dashboard displays the current applicability state.
- **TC-21.3.3 - Explanation shows source rule, facts used, and:** Verify explanation shows source rule, facts used, and missing facts when applicable.
- **TC-21.3.4 - Prior result history is retained:** Verify prior result history is retained.
- **TC-21.3.5 - Material changes from applicable to not applicable:** Verify material changes from applicable to not applicable or needs review are audit logged.

## 22. SAM.gov Entity Lookup

### Story 22.1: SAM.gov API Configuration

- **TC-22.1.1 - SAM.gov API key is not stored in:** Verify SAM.gov API key is not stored in source control.
- **TC-22.1.2 - Lookup service uses configured timeout and retry:** Verify lookup service uses configured timeout and retry behavior.
- **TC-22.1.3 - API failures return a standard, user-safe error:** Verify API failures return a standard, user-safe error.
- **TC-22.1.4 - Logs do not contain API keys or:** Verify logs do not contain API keys or sensitive response payloads.
- **TC-22.1.5 - Adapter can be replaced or mocked in:** Verify adapter can be replaced or mocked in tests.

### Story 22.2: Company Entity Lookup

- **TC-22.2.1 - Search by UEI or legal business name:** Verify authorized user can search by UEI or legal business name.
- **TC-22.2.2 - Search results show source and retrieved date:** Verify search results show source and retrieved date.
- **TC-22.2.3 - Apply selected fields to the company profile:** Verify user can apply selected fields to the company profile.
- **TC-22.2.4 - Existing profile values are not overwritten without:** Verify existing profile values are not overwritten without explicit user confirmation.
- **TC-22.2.5 - Applied SAM data changes are audit logged:** Verify applied SAM data changes are audit logged.

### Story 22.3: Subcontractor Entity Lookup

- **TC-22.3.1 - Search SAM.gov for a subcontractor by UEI:** Verify authorized user can search SAM.gov for a subcontractor by UEI or name.
- **TC-22.3.2 - Applied fields update only the current tenant's:** Verify applied fields update only the current tenant's subcontractor record.
- **TC-22.3.3 - No-match and multiple-match results are shown without:** Verify no-match and multiple-match results are shown without changing existing data.
- **TC-22.3.4 - Source and retrieved date are stored with:** Verify source and retrieved date are stored with applied data.
- **TC-22.3.5 - Subcontractor SAM updates are audit logged:** Verify subcontractor SAM updates are audit logged.

## 23. SBA Size Helper

### Story 23.1: Size Standard Reference Data

- **TC-23.1.1 - Approved size standard records include NAICS, metric,:** Verify approved size standard records include NAICS, metric, threshold, source URL, effective date, last reviewed date, and status.
- **TC-23.1.2 - Draft records are not used in customer-facing:** Verify draft records are not used in customer-facing helper results.
- **TC-23.1.3 - Import rejects records missing source metadata:** Verify import rejects records missing source metadata.
- **TC-23.1.4 - Deprecated records remain visible to content reviewers:** Verify deprecated records remain visible to content reviewers.
- **TC-23.1.5 - Import and approval actions are audit logged:** Verify import and approval actions are audit logged.

### Story 23.2: Company Size Evaluation Helper

- **TC-23.2.1 - Evaluation uses approved size standard records only:** Verify evaluation uses approved size standard records only.
- **TC-23.2.2 - Missing revenue or employee inputs produce insufficient:** Verify missing revenue or employee inputs produce insufficient information.
- **TC-23.2.3 - Results show NAICS, metric, threshold, entered value:** Verify results show NAICS, metric, threshold, entered value or range, source URL, and run date.
- **TC-23.2.4 - Save evaluation results to the company profile:** Verify user can save evaluation results to the company profile.
- **TC-23.2.5 - Saved evaluations are audit logged:** Verify saved evaluations are audit logged.

### Story 23.3: Opportunity NAICS Size Check

- **TC-23.3.1 - Run size check for a contract NAICS:** Verify user can run size check for a contract NAICS code.
- **TC-23.3.2 - Result shows likely status, source standard, and:** Verify result shows likely status, source standard, and missing information when applicable.
- **TC-23.3.3 - Expert-review recommended result can create a task:** Verify expert-review recommended result can create a task assigned to an owner.
- **TC-23.3.4 - Evaluation history remains available from the contract:** Verify evaluation history remains available from the contract record.
- **TC-23.3.5 - Size check actions are audit logged:** Verify size check actions are audit logged.

## 24. Subcontractor Tracker Expansion

### Story 24.1: Expanded Subcontractor Compliance Profile

- **TC-24.1.1 - Create and update expanded subcontractor fields:** Verify authorized user can create and update expanded subcontractor fields.
- **TC-24.1.2 - Profile completeness reflects required fields configured for:** Verify profile completeness reflects required fields configured for the tenant.
- **TC-24.1.3 - Filters return only subcontractors in the current:** Verify filters return only subcontractors in the current tenant.
- **TC-24.1.4 - Expiring insurance or certification dates can be:** Verify expiring insurance or certification dates can be surfaced in list filters.
- **TC-24.1.5 - Sensitive field changes are audit logged:** Verify sensitive field changes are audit logged.

### Story 24.2: Subcontractor Risk Status

- **TC-24.2.1 - Risk status is calculated from documented inputs:** Verify risk status is calculated from documented inputs.
- **TC-24.2.2 - Risk drivers are visible to authorized users:** Verify risk drivers are visible to authorized users.
- **TC-24.2.3 - Updating evidence, insurance, NDA, CMMC status, or:** Verify updating evidence, insurance, NDA, CMMC status, or SAM data updates risk status.
- **TC-24.2.4 - Missing or unknown data can produce needs:** Verify missing or unknown data can produce needs review.
- **TC-24.2.5 - Risk calculation is covered by automated tests:** Verify risk calculation is covered by automated tests.

### Story 24.3: Contract-Specific Subcontractor Obligations

- **TC-24.3.1 - Link a subcontractor to a contract and:** Verify user can link a subcontractor to a contract and applicable flow-down obligations.
- **TC-24.3.2 - Supplier obligations show owner, due date, status,:** Verify supplier obligations show owner, due date, status, and required evidence.
- **TC-24.3.3 - Bulk creation uses accepted flow-down clauses only:** Verify bulk creation uses accepted flow-down clauses only.
- **TC-24.3.4 - Supplier obligations are tenant-scoped:** Verify supplier obligations are tenant-scoped.
- **TC-24.3.5 - Creation and status changes are audit logged:** Verify creation and status changes are audit logged.

## 25. Policy Templates

### Story 25.1: Approved Template Library

- **TC-25.1.1 - Approved templates include title, category, version, source:** Verify approved templates include title, category, version, source references, owner, and last reviewed date.
- **TC-25.1.2 - Draft templates are hidden from standard users:** Verify draft templates are hidden from standard users.
- **TC-25.1.3 - Deprecated templates remain visible to content reviewers:** Verify deprecated templates remain visible to content reviewers.
- **TC-25.1.4 - Template approval requires source and review metadata:** Verify template approval requires source and review metadata.
- **TC-25.1.5 - Template lifecycle changes are audit logged:** Verify template lifecycle changes are audit logged.

### Story 25.2: Generate Draft Policy From Template

- **TC-25.2.1 - Generate a draft policy from an approved:** Verify user can generate a draft policy from an approved template.
- **TC-25.2.2 - Placeholder values are populated from tenant data:** Verify placeholder values are populated from tenant data when available.
- **TC-25.2.3 - Missing placeholder values are flagged for user:** Verify missing placeholder values are flagged for user completion.
- **TC-25.2.4 - Generated policy stores source template version and:** Verify generated policy stores source template version and generation date.
- **TC-25.2.5 - Generated policy is marked draft until approved:** Verify generated policy is marked draft until approved by the tenant.

### Story 25.3: Policy Approval And Evidence Linking

- **TC-25.3.1 - Approve, reject, or revise a draft policy:** Verify authorized user can approve, reject, or revise a draft policy.
- **TC-25.3.2 - Approved policy records approver, approval date, source:** Verify approved policy records approver, approval date, source template, and review date.
- **TC-25.3.3 - Approved policy can be linked to obligations:** Verify approved policy can be linked to obligations and controls as evidence.
- **TC-25.3.4 - Revisions preserve prior approved versions:** Verify revisions preserve prior approved versions.
- **TC-25.3.5 - Policy approval actions are audit logged:** Verify policy approval actions are audit logged.

## 26. Evidence Request Workflows

### Story 26.1: Evidence Request Creation

- **TC-26.1.1 - Create an evidence request tied to a:** Verify authorized user can create an evidence request tied to a supported record type.
- **TC-26.1.2 - Request stores requester, assignee, due date, status,:** Verify request stores requester, assignee, due date, status, instructions, and related record.
- **TC-26.1.3 - Assignee receives notification:** Verify assignee receives notification.
- **TC-26.1.4 - User cannot assign a request to a:** Verify user cannot assign a request to a user or subcontractor outside the tenant context.
- **TC-26.1.5 - Request creation is audit logged:** Verify request creation is audit logged.

### Story 26.2: Evidence Submission And Review

- **TC-26.2.1 - Submit evidence to an open request:** Verify assignee can submit evidence to an open request.
- **TC-26.2.2 - Upload submissions enforce CUI/data-handling guardrails and tenant:** Verify upload submissions enforce CUI/data-handling guardrails and tenant scope.
- **TC-26.2.3 - Accept or return submitted evidence with comments:** Verify reviewer can accept or return submitted evidence with comments.
- **TC-26.2.4 - Accepted evidence is linked to the related:** Verify accepted evidence is linked to the related requirement.
- **TC-26.2.5 - Status changes and review decisions are audit:** Verify status changes and review decisions are audit logged.

### Story 26.3: Evidence Request Dashboard

- **TC-26.3.1 - Dashboard shows only evidence requests in the:** Verify dashboard shows only evidence requests in the current tenant.
- **TC-26.3.2 - Filters return requests by status, due date,:** Verify filters return requests by status, due date, assignee, related type, and priority.
- **TC-26.3.3 - Overdue requests are calculated from due date:** Verify overdue requests are calculated from due date and current status.
- **TC-26.3.4 - Bulk reminders create notifications without changing request:** Verify bulk reminders create notifications without changing request status.
- **TC-26.3.5 - Auditors can view approved or accepted evidence:** Verify auditors can view approved or accepted evidence request records but cannot modify them.

## 27. CMMC Level 2 Readiness Expansion

### Story 27.1: Level 2 Control Assessment Detail

- **TC-27.1.1 - Update Level 2 control assessment detail:** Verify authorized user can update Level 2 control assessment detail.
- **TC-27.1.2 - Control detail stores implementation, evidence, inherited, ESP:** Verify control detail stores implementation, evidence, inherited, ESP responsibility, notes, assessment date, and assessor.
- **TC-27.1.3 - Status history is retained:** Verify status history is retained.
- **TC-27.1.4 - Control updates are tenant-scoped:** Verify control updates are tenant-scoped.
- **TC-27.1.5 - Control assessment updates are audit logged:** Verify control assessment updates are audit logged.

### Story 27.2: Responsibility Matrix

- **TC-27.2.1 - Assign responsible party for each Level 2:** Verify user can assign responsible party for each Level 2 control.
- **TC-27.2.2 - Matrix shows control, responsibility type, owner, provider,:** Verify matrix shows control, responsibility type, owner, provider, evidence status, and notes.
- **TC-27.2.3 - Controls marked external or shared require provider:** Verify controls marked external or shared require provider or responsibility notes.
- **TC-27.2.4 - Responsibility changes are audit logged:** Verify responsibility changes are audit logged.
- **TC-27.2.5 - Matrix export reflects current tenant data:** Verify matrix export reflects current tenant data.

### Story 27.3: Readiness Gap Prioritization

- **TC-27.3.1 - Gap priority is calculated from documented inputs:** Verify gap priority is calculated from documented inputs.
- **TC-27.3.2 - Dashboard lists gaps by priority with reason:** Verify dashboard lists gaps by priority with reason codes.
- **TC-27.3.3 - Create a POA&M item or task from:** Verify user can create a POA&M item or task from a gap.
- **TC-27.3.4 - Priority recalculates when control or evidence status:** Verify priority recalculates when control or evidence status changes.
- **TC-27.3.5 - Priority rules are covered by automated tests:** Verify priority rules are covered by automated tests.

### Story 27.4: Level 2 Readiness Report

- **TC-27.4.1 - Generate a Level 2 readiness report:** Verify authorized user can generate a Level 2 readiness report.
- **TC-27.4.2 - Report includes control status, evidence status, gaps,:** Verify report includes control status, evidence status, gaps, POA&M items, responsibility matrix, source references, and generated date.
- **TC-27.4.3 - Report contains no pass/fail certification language:** Verify report contains no pass/fail certification language.
- **TC-27.4.4 - Report uses tenant-scoped data only:** Verify report uses tenant-scoped data only.
- **TC-27.4.5 - Report generation is audit logged:** Verify report generation is audit logged.

## 28. Extraction Content Test Set

### Story 28.1: Curated Test Document Set

- **TC-28.1.1 - Test corpus contains only public, synthetic, or:** Verify test corpus contains only public, synthetic, or explicitly approved non-CUI documents.
- **TC-28.1.2 - Each labeled document includes expected clause citations:** Verify each labeled document includes expected clause citations and source locations when available.
- **TC-28.1.3 - Test metadata identifies document type, source family,:** Verify test metadata identifies document type, source family, and limitations.
- **TC-28.1.4 - Label set is reviewed before use as:** Verify label set is reviewed before use as a benchmark.
- **TC-28.1.5 - Test set data handling rules are documented:** Verify test set data handling rules are documented.

### Story 28.2: Precision And Recall Evaluation

- **TC-28.2.1 - Evaluation metrics precision, recall, false positive, and:** Verify evaluation runner produces precision, recall, false positive, and false negative metrics.
- **TC-28.2.2 - Results identify missed and extra clause detections:** Verify results identify missed and extra clause detections by document.
- **TC-28.2.3 - Threshold failures are visible in CI or:** Verify threshold failures are visible in CI or scheduled test output.
- **TC-28.2.4 - Metrics are stored or published for trend:** Verify metrics are stored or published for trend review.
- **TC-28.2.5 - Evaluation can run without customer data:** Verify evaluation can run without customer data.

### Story 28.3: Extraction Regression Review

- **TC-28.3.1 - Each reviewed failure has a classification, owner,:** Verify each reviewed failure has a classification, owner, status, and resolution note.
- **TC-28.3.2 - Follow-up tasks can be created from failures:** Verify follow-up tasks can be created from failures.
- **TC-28.3.3 - Resolved failures are linked to matcher, library,:** Verify resolved failures are linked to matcher, library, parser, or label updates when applicable.
- **TC-28.3.4 - Release summary shows open extraction risks and:** Verify release summary shows open extraction risks and metric trends.
- **TC-28.3.5 - Regression review records are audit logged or:** Verify regression review records are audit logged or otherwise traceable.

## 30. SPRS Score Calculator

### Story 30.1: Scoring Rule Baseline

- **TC-30.1.1 - Published scoring metadata complete:** Verify published scoring rules include source URL, version, owner, reviewer, review date, and effective date.
- **TC-30.1.2 - Publish validation enforced:** Attempt to publish scoring rules with missing source or review metadata and verify validation fails.
- **TC-30.1.3 - Retired rules blocked:** Retire a scoring rule set and verify it cannot be selected for a new calculation.
- **TC-30.1.4 - Rule version attached to calculations:** Calculate a score and verify the calculation stores the scoring rule version used.
- **TC-30.1.5 - Scoring rule lifecycle traceable:** Move scoring rules through draft, approved, published, superseded, and retired states and verify audit events or source-control traceability.

### Story 30.2: Score Calculation Workspace

- **TC-30.2.1 - Draft score calculation succeeds:** As an authorized user, calculate a draft SPRS score from Level 2 control assessment data and verify score, deductions, reasons, rule version, generated date, and gaps are returned.
- **TC-30.2.2 - Score is tenant scoped:** Seed assessment data in two tenants and verify each score uses only current-tenant data.
- **TC-30.2.3 - Recalculation follows control changes:** Change a relevant control assessment status and verify recalculation updates score and deductions.
- **TC-30.2.4 - Manual notes do not override calculated values:** Add reviewer notes and verify calculated score and deduction values remain rule-derived.
- **TC-30.2.5 - Score calculation audited:** Run score calculation and verify audit event includes tenant, actor, rule version, generated date, and result.

### Story 30.3: SPRS Readiness Report

- **TC-30.3.1 - Generate readiness report:** As an authorized user, generate an SPRS readiness report and verify score, deductions, unresolved controls, POA&M references, evidence status, scoring rule version, and generated date are included.
- **TC-30.3.2 - Not-submitted language present:** Verify the report states that GCCS has not submitted the score to SPRS.
- **TC-30.3.3 - Report uses tenant data only:** Seed neighboring tenant score data and verify the report excludes it.
- **TC-30.3.4 - Report permissions enforced:** Attempt to generate or view the report as unauthorized roles and verify access is denied.
- **TC-30.3.5 - Report generation audited:** Generate the report and verify report generation is audit logged.

## 31. eSRS Support

### Story 31.1: eSRS Applicability And Reporting Calendar

- **TC-31.1.1 - eSRS applicability recorded:** Mark a contract as eSRS-applicable with report type, period, due date, and source and verify persistence.
- **TC-31.1.2 - Calendar item created:** Activate an eSRS obligation and verify the reporting deadline appears on the compliance calendar.
- **TC-31.1.3 - Source or rationale required:** Attempt to activate an eSRS obligation without source clause or documented rationale and verify validation fails.
- **TC-31.1.4 - Overdue eSRS task calculated:** Seed an incomplete past-due eSRS report task and verify overdue status.
- **TC-31.1.5 - Applicability changes audited:** Create and update eSRS applicability and verify audit events are written.

### Story 31.2: Subcontracting Report Data Collection

- **TC-31.2.1 - Report data row created:** Create subcontracting report data linked to contract and subcontractor records and verify required fields persist.
- **TC-31.2.2 - Report data validation enforced:** Attempt negative amounts, missing socioeconomic category, duplicate rows, and period mismatches and verify validation errors.
- **TC-31.2.3 - Evidence link stored:** Attach supporting evidence to a report data row and verify the link is returned in detail and package preparation.
- **TC-31.2.4 - Review required for package inclusion:** Attempt to include unreviewed data rows in a final package and verify they are blocked unless explicitly accepted.
- **TC-31.2.5 - Data row changes audited:** Create, update, accept, and reject report data rows and verify audit events.

### Story 31.3: eSRS Report Package

- **TC-31.3.1 - Generate eSRS preparation package:** Generate a package for contract, period, and report type and verify subcontractor/spend summaries, exceptions, evidence references, and generated date.
- **TC-31.3.2 - Not-submitted language present:** Verify package states that GCCS has not submitted the report to eSRS.
- **TC-31.3.3 - Package approval metadata complete:** Approve a package and verify reviewer, approval date, package version, and review notes are stored.
- **TC-31.3.4 - Package permissions enforced:** Attempt package generation, approval, and viewing with unauthorized roles and verify denial.
- **TC-31.3.5 - Package generation and approval audited:** Verify package generation, approval, supersede, and archive actions are audit logged.

## 32. Labor Compliance Module

### Story 32.1: Labor Applicability And Wage Determinations

- **TC-32.1.1 - Labor applicability recorded:** Record labor applicability with source clause, place of performance, contract period, and wage determination reference and verify persistence.
- **TC-32.1.2 - Wage determination upload guardrails:** Upload a wage determination document and verify tenant data-handling guardrails, classification, scan status, and contract link.
- **TC-32.1.3 - Missing source blocks activation:** Attempt to activate labor obligations without source clause or documented rationale and verify validation fails.
- **TC-32.1.4 - Labor review task generated:** Activate labor applicability and verify linked review tasks are created or updated.
- **TC-32.1.5 - Labor applicability audited:** Create, update, activate, and deactivate labor applicability and verify audit events.

### Story 32.2: Labor Category And Employee Classification

- **TC-32.2.1 - Labor category and assignment created:** Create labor categories and employee assignments for a contract and verify required fields persist.
- **TC-32.2.2 - Assignment validation enforced:** Attempt inactive category assignment, missing source reference, and conflicting effective dates and verify validation errors.
- **TC-32.2.3 - Sensitive employee fields restricted:** As roles with and without HR permissions, view assignment details and verify sensitive fields are shown or hidden appropriately.
- **TC-32.2.4 - Classification history preserved:** Change an employee classification and verify prior category, new category, actor, timestamp, and reason are retained.
- **TC-32.2.5 - Assignment changes audited:** Create, update, deactivate, and reclassify labor assignments and verify audit events.

### Story 32.3: Labor Evidence And Compliance Report

- **TC-32.3.1 - Dashboard shows labor status:** Seed labor obligations, assignments, evidence, gaps, and overdue items and verify dashboard filters show current-tenant results.
- **TC-32.3.2 - Generate labor report:** Generate a labor compliance report and verify source clauses, wage determinations, categories, assignments, gaps, evidence references, and generated date.
- **TC-32.3.3 - Employee-sensitive report sections restricted:** Generate or view labor report sections as roles with and without HR permissions and verify restricted content handling.
- **TC-32.3.4 - No legal determination language:** Inspect labor report and verify it presents workflow status without final legal determination language.
- **TC-32.3.5 - Labor report audited:** Generate and export the labor report and verify audit events are written.

## 33. AI Assistant With Citations, Logging, And Human Review

### Story 33.1: Retrieval And Source Citation Pipeline

- **TC-33.1.1 - Retrieval respects tenant and RBAC:** Ask a question with neighboring tenant sources seeded and verify retrieved sources are limited to authorized current-tenant and approved library content.
- **TC-33.1.2 - Citations required:** Ask a substantive compliance question and verify every substantive answer statement includes citation metadata.
- **TC-33.1.3 - Unsupported answer blocked:** Ask a question with no approved supporting source and verify the assistant refuses or routes to review rather than inventing an answer.
- **TC-33.1.4 - Unsafe sources excluded:** Seed prohibited, unknown, unapproved, and cross-tenant content and verify retrieval excludes all unsafe sources.
- **TC-33.1.5 - Retrieval decisions logged:** Verify retrieval source IDs, policy decisions, tenant, actor, and workflow context are logged.

### Story 33.2: AI Output Logging And Review

- **TC-33.2.1 - AI interaction log complete:** Trigger an AI interaction and verify prompt metadata, model config, retrieved sources, output, actor, tenant, timestamp, and workflow context are stored.
- **TC-33.2.2 - Draft state required for deliverables:** Use AI output in a report, policy, SSP, POA&M, or customer deliverable and verify it remains draft until human approved.
- **TC-33.2.3 - Review decisions stored:** Approve, reject, supersede, and archive AI output and verify reviewer, note, reason, timestamp, and state are retained.
- **TC-33.2.4 - AI logs respect retention and access:** Verify AI logs are tenant-scoped, RBAC-protected, and follow retention and data-handling mode rules.
- **TC-33.2.5 - AI review audited:** Verify AI review decisions and state changes are audit logged.

### Story 33.3: Guarded Assistant User Experience

- **TC-33.3.1 - Answer displays guardrails:** Ask an allowed assistant question and verify citations, draft label, confidence or support status, and review requirement are shown.
- **TC-33.3.2 - Prohibited prompts blocked:** Ask for legal determination, certification claim, unsupported CUI processing, classified content handling, and cross-tenant data and verify each is blocked or redirected.
- **TC-33.3.3 - Draft actions created from answer:** Create a draft task, evidence request, note, or review item from a supported answer and verify it links back to the AI answer.
- **TC-33.3.4 - Feedback stored:** Submit helpful, incorrect, missing source, and needs expert review feedback and verify answer, user, tenant, timestamp, and reason are stored.
- **TC-33.3.5 - Assistant actions audited:** Verify assistant-created actions and blocked requests create audit events.

## 34. Prime Contractor And Auditor Portals

### Story 34.1: External Portal Access Model

- **TC-34.1.1 - Portal invitation created:** As tenant admin, invite an external reviewer with role, scope, expiration, package access, and download permission and verify persistence.
- **TC-34.1.2 - Expired and revoked invitations blocked:** Attempt to use expired and revoked invitations and verify portal access is denied.
- **TC-34.1.3 - Portal scope enforced:** Seed multiple packages and contracts and verify portal user can access only assigned scope.
- **TC-34.1.4 - Portal users are read only:** Attempt workspace modification through portal UI and direct API calls and verify denial.
- **TC-34.1.5 - Portal access audited:** Verify invitation, access, resend, extension, and revocation actions are audit logged.

### Story 34.2: Approved Package Portal Review

- **TC-34.2.1 - Only approved shared packages visible:** Share approved and draft packages and verify portal reviewer sees only approved packages explicitly assigned.
- **TC-34.2.2 - Unsafe records hidden:** Seed drafts, internal notes, prohibited data, unknown classification records, and unrelated tenant records and verify they are hidden from portal review.
- **TC-34.2.3 - Reviewer comments do not modify source records:** Add portal comments and questions and verify source tenant package and evidence records remain unchanged.
- **TC-34.2.4 - Downloads include metadata:** Download a package with watermarking enabled and verify package metadata and watermark are included.
- **TC-34.2.5 - Portal review audited:** View, comment, question, and download package records and verify audit events.

### Story 34.3: Portal Package Lifecycle And Revocation

- **TC-34.3.1 - Shared package lifecycle states work:** Move shared packages through active, superseded, expired, revoked, and archived states and verify allowed transitions.
- **TC-34.3.2 - Revocation cuts off access:** Revoke an active package and verify portal users lose access immediately.
- **TC-34.3.3 - Superseded package links replacement:** Supersede a package and verify the old package links to the replacement version.
- **TC-34.3.4 - Portal activity report complete:** Generate activity report and verify access, comments, downloads, expiration, supersede, and revocation history are included.
- **TC-34.3.5 - Lifecycle actions audited:** Expire, revoke, supersede, reissue, and archive shared packages and verify audit events.

## Phase 1A - CUI Readiness Gate

These test cases cover the Phase 1A readiness-gate stories appended to `/Users/devups/Development/CodexProjects/Gccs/docs/development-phase-use-cases.md`. They verify the controls required before any production tenant can upload real customer CUI.

## 1A.1 Tenant Data Handling Modes

### Story 1A.1.1: Tenant Data Handling Mode Model

- **TC-1A.1.1.1 - Single active mode per tenant:** Create or update tenants through supported workflows and verify each tenant has exactly one active mode: `DemoSandbox`, `NoCui`, or `CuiReady`.
- **TC-1A.1.1.2 - Default mode assignment:** Create a new pilot tenant without an explicit mode and verify it defaults to `NoCui`; create an explicitly demo tenant and verify it starts as `DemoSandbox`.
- **TC-1A.1.1.3 - CuiReady requires approval checklist:** Attempt to set a tenant to `CuiReady` without an approved checklist and verify the mode change is rejected.
- **TC-1A.1.1.4 - Mode history persisted:** Change tenant mode and verify actor, timestamp, reason, previous mode, new mode, effective date, and approval reference are stored.
- **TC-1A.1.1.5 - Mode available to dependent workflows:** Call upload, evidence, report, note, and extraction workflow services and verify each receives the tenant's current data handling mode.

### Story 1A.1.2: Mode-Based Workflow Enforcement

- **TC-1A.1.2.1 - DemoSandbox real CUI upload blocked:** As a `DemoSandbox` tenant, attempt to upload a customer file marked `CUI` and verify it is rejected while seeded synthetic examples remain usable.
- **TC-1A.1.2.2 - NoCui real CUI processing blocked:** As a `NoCui` tenant, attempt to create, classify, process, report on, or export real CUI and verify each action is blocked.
- **TC-1A.1.2.3 - CuiReady workflows require classification and approval:** As a `CuiReady` tenant, attempt CUI workflows with missing classification or missing approval checks and verify rejection; repeat with valid checks and verify success.
- **TC-1A.1.2.4 - Direct API bypass denied:** Call restricted APIs directly for upload, evidence, notes, reports, and extraction and verify server-side mode checks match UI behavior.
- **TC-1A.1.2.5 - Mode enforcement audit event:** Trigger a mode-restricted failure and verify the clear error response plus audit event with tenant, actor, workflow, mode, and result.

## 1A.2 Data Classification Controls

### Story 1A.2.1: Classification Metadata Schema

- **TC-1A.2.1.1 - Classification required for active content:** Attempt to store uploads, notes, reports, extraction jobs, evidence items, and document records without classification metadata and verify validation fails.
- **TC-1A.2.1.2 - CUI rejected outside CuiReady:** Create content classified as `CUI` in `DemoSandbox` and `NoCui` tenants and verify rejection.
- **TC-1A.2.1.3 - SyntheticCui restricted to demo/test workflows:** Classify normal customer content as `SyntheticCui` and verify rejection; classify approved demo seed content and verify acceptance.
- **TC-1A.2.1.4 - Unknown blocks downstream processing:** Save an item classified as `Unknown` and verify report generation, extraction, export, and evidence approval are blocked until review.
- **TC-1A.2.1.5 - Classification change history preserved:** Reclassify content and verify previous value, new value, actor, timestamp, source, confidence, reviewer, review date, and reason are retained.

### Story 1A.2.2: Classification UX And Review

- **TC-1A.2.2.1 - User must confirm classification:** In upload, note, evidence, report, and extraction flows, attempt submission without selecting or confirming classification and verify UI and API block the action.
- **TC-1A.2.2.2 - Unknown review queue behavior:** Create `Unknown` items and verify they appear in the review queue and cannot be used in reports or extraction jobs.
- **TC-1A.2.2.3 - Prohibited content escalation:** Classify an item as `Prohibited` and verify it is blocked from use and routed to the escalation workflow.
- **TC-1A.2.2.4 - Authorized reviewer reclassification:** As an authorized reviewer, update an item's classification with a reason and verify the new classification controls downstream behavior.
- **TC-1A.2.2.5 - Classification badges visible:** Verify list, detail, and report generation screens display the current classification badge for classified items.

## 1A.3 Synthetic CUI Demo Dataset

### Story 1A.3.1: Synthetic Dataset Definition

- **TC-1A.3.1.1 - Dataset contains no real sensitive data:** Review seeded synthetic company, contract, evidence, CMMC, subcontractor, and report examples and verify no real customer CUI, classified data, export-controlled technical data, or customer proprietary data is present.
- **TC-1A.3.1.2 - Synthetic records tagged:** Import or inspect the dataset and verify every seeded record has `SyntheticCui` classification and dataset version.
- **TC-1A.3.1.3 - Synthetic labels shown in demo UI:** Open demo contract, obligation, evidence, CMMC, subcontractor, and report views and verify synthetic labels are visible.
- **TC-1A.3.1.4 - Dataset metadata complete:** Verify dataset metadata includes purpose, limitations, owner, source basis, review date, and approved reviewer.
- **TC-1A.3.1.5 - Review required before seed import:** Attempt to run demo seed import with unapproved dataset content and verify the import is blocked.

### Story 1A.3.2: Demo Tenant Seeding

- **TC-1A.3.2.1 - Seed runs only for DemoSandbox tenants:** Run seed workflow for `DemoSandbox`, `NoCui`, and `CuiReady` tenants and verify only the `DemoSandbox` tenant is seeded.
- **TC-1A.3.2.2 - Seed process is idempotent:** Run demo seed twice for the same tenant and verify duplicate records are not created.
- **TC-1A.3.2.3 - End-to-end demo data present:** After seeding, verify demo examples appear across contract intake, clause tagging, obligations, evidence, CMMC, subcontractor, report, and escalation workflows.
- **TC-1A.3.2.4 - Customer tenants cannot receive demo seed data:** Attempt seed import through normal admin workflows for customer `NoCui` and `CuiReady` tenants and verify rejection.
- **TC-1A.3.2.5 - Seed and reset audited:** Run seed and reset actions and verify audit events include tenant, actor, dataset version, action, timestamp, and result.

## 1A.4 CUI-Ready Tenant Approval Checklist

### Story 1A.4.1: Approval Checklist Model

- **TC-1A.4.1.1 - Required checklist items block approval:** Attempt to approve a checklist with incomplete required items and verify approval fails.
- **TC-1A.4.1.2 - Completed item metadata required:** Complete checklist items and verify owner, reviewer, review date, and supporting note or evidence link are required and stored.
- **TC-1A.4.1.3 - Rejected checklist records reason:** Reject a checklist and verify rejection reason, reviewer, timestamp, and tenant link are retained.
- **TC-1A.4.1.4 - Approved checklist required for CuiReady mode:** Approve a checklist and verify its ID is required and referenced when changing tenant mode to `CuiReady`.
- **TC-1A.4.1.5 - Checklist changes audited:** Create, update, approve, reject, and supersede checklist records and verify audit events for each action.

### Story 1A.4.2: Approval Gate Enforcement

- **TC-1A.4.2.1 - Final approval permission enforced:** Attempt final checklist approval as unauthorized and authorized roles and verify only authorized platform roles succeed.
- **TC-1A.4.2.2 - Invalid checklist states block CuiReady:** Try `CuiReady` mode change with incomplete, rejected, expired, and superseded checklists and verify all are blocked.
- **TC-1A.4.2.3 - Final approval metadata persisted:** Approve a checklist and verify approving user, timestamp, checklist version, and approval notes are stored.
- **TC-1A.4.2.4 - CuiReady mode references approved checklist:** Change a tenant to `CuiReady` and verify the mode history points to the approved checklist record.
- **TC-1A.4.2.5 - Failed approval attempt audited:** Trigger failed approval and failed `CuiReady` enablement attempts and verify clear errors plus audit events.

## 1A.5 Shared Responsibility Matrix Baseline

### Story 1A.5.1: Baseline Responsibility Matrix

- **TC-1A.5.1.1 - Matrix categories complete:** Verify the baseline matrix includes tenant administration, user access, MFA, upload classification, evidence storage, encryption, malware scanning, retention, backup, export, deletion, incident reporting, support, and customer content decisions.
- **TC-1A.5.1.2 - Matrix row metadata complete:** Verify each row has responsibility value, notes, source reference when applicable, effective date, review owner, and version.
- **TC-1A.5.1.3 - Publish validation enforced:** Attempt to publish a matrix with missing owner or review metadata and verify publication fails.
- **TC-1A.5.1.4 - Published matrix visible in workflows:** Publish the matrix and verify it is visible from tenant settings and the CUI approval checklist.
- **TC-1A.5.1.5 - Matrix lifecycle traceable:** Publish and retire a matrix and verify audit events or source-control traceability capture the lifecycle.

### Story 1A.5.2: Tenant Matrix Acknowledgement

- **TC-1A.5.2.1 - Tenant admin acknowledges current matrix:** As a tenant admin, view and acknowledge the current matrix and verify acknowledgement status is persisted.
- **TC-1A.5.2.2 - Missing acknowledgement blocks approval:** Attempt CUI-ready approval without current matrix acknowledgement and verify the checklist or approval gate blocks it.
- **TC-1A.5.2.3 - Acknowledgement history complete:** Verify history records matrix version, user, tenant, timestamp, and status.
- **TC-1A.5.2.4 - New matrix version invalidates prior acknowledgement:** Publish a new matrix version and verify prior acknowledgement is marked outdated for future approvals.
- **TC-1A.5.2.5 - Matrix acknowledgement audited:** Acknowledge the matrix and verify an audit event records tenant, actor, matrix version, timestamp, and result.

## 1A.6 Customer-Facing Data Handling Notices

### Story 1A.6.1: Versioned Notice Content

- **TC-1A.6.1.1 - Published notice exists for each mode:** Verify published notice content is available for `DemoSandbox`, `NoCui`, and `CuiReady` tenant modes.
- **TC-1A.6.1.2 - Notice publish metadata required:** Attempt to publish notice content without owner, reviewer, review date, or effective date and verify validation fails.
- **TC-1A.6.1.3 - NoCui notice prohibits real CUI:** Retrieve the `NoCui` notice and verify it states that real customer CUI upload is prohibited.
- **TC-1A.6.1.4 - CuiReady notice limits CUI handling:** Retrieve the `CuiReady` notice and verify it states CUI handling is limited to approved workflows and customer responsibilities.
- **TC-1A.6.1.5 - Notice retrieval matches mode and context:** Request notice content for multiple tenant modes and workflow contexts and verify the correct published version is returned.

### Story 1A.6.2: Notice Placement And Acknowledgement

- **TC-1A.6.2.1 - Missing acknowledgement blocks CUI-relevant actions:** Without acknowledgement, attempt upload, classified note save, report generation from classified content, and extraction job creation and verify each is blocked.
- **TC-1A.6.2.2 - Acknowledgement metadata persisted:** Acknowledge a notice and verify user, tenant, mode, workflow, notice version, and timestamp are stored.
- **TC-1A.6.2.3 - Updated notice requires renewal:** Publish a new notice version and verify previously acknowledged users are re-prompted before CUI-relevant actions.
- **TC-1A.6.2.4 - Notice copy matches tenant mode:** Open onboarding, upload, note, report, extraction, and support flows for each mode and verify displayed notice text matches the current mode.
- **TC-1A.6.2.5 - Notice acknowledgement audited:** Acknowledge and renew acknowledgement and verify audit events are created.

## 1A.7 CUI Support Escalation Path

### Story 1A.7.1: Escalation Intake And Classification

- **TC-1A.7.1.1 - Authorized escalation creation:** Create escalation records from upload rejection, evidence detail, note detail, report detail, extraction job detail, and support page and verify they persist with required metadata.
- **TC-1A.7.1.2 - Escalations are tenant scoped:** Seed escalations in two tenants and verify users only see escalations for their active tenant.
- **TC-1A.7.1.3 - Prohibited data blocks affected content:** Create a prohibited data escalation and verify the affected content is blocked from use.
- **TC-1A.7.1.4 - Support fields can be assigned:** As a support agent, assign owner, severity, and status and verify updates persist.
- **TC-1A.7.1.5 - Escalation access restricted and audited:** Verify unauthorized users cannot access restricted escalation views and create/update actions are audit logged.

### Story 1A.7.2: Escalation Workflow And Resolution

- **TC-1A.7.2.1 - Status changes require note:** Change escalation status and verify actor, timestamp, and note are required and persisted.
- **TC-1A.7.2.2 - Containment blocks affected content:** Keep escalation in submitted, triage, and contained states and verify affected downloads, exports, extraction jobs, report use, and evidence approval are blocked.
- **TC-1A.7.2.3 - Resolution records complete:** Resolve an escalation and verify resolution type, resolver, timestamp, and summary are stored.
- **TC-1A.7.2.4 - Reopen preserves history:** Reopen a resolved escalation and verify prior resolution history remains visible.
- **TC-1A.7.2.5 - Escalation workflow audited:** Move an escalation through multiple statuses and verify each workflow event is audit logged.

## 1A.8 CUI Audit Event Coverage

### Story 1A.8.1: Required CUI Audit Events

- **TC-1A.8.1.1 - Required event types emitted:** Perform mode changes, classification create/update, upload block, checklist approval/rejection, matrix acknowledgement, notice acknowledgement, download, export, deletion, escalation create/update, and extraction start/stop and verify matching audit events.
- **TC-1A.8.1.2 - Blocked actions audited:** Trigger blocked upload, blocked extraction, blocked report, failed mode change, and failed CUI approval and verify failure-path audit events.
- **TC-1A.8.1.3 - Audit fields complete:** Inspect Phase 1A audit events and verify tenant ID, actor ID, event type, entity reference, classification, mode, timestamp, request metadata, and result when applicable.
- **TC-1A.8.1.4 - Sensitive content excluded from summaries:** Audit CUI-relevant actions with document text and verify event summaries do not expose sensitive document content.
- **TC-1A.8.1.5 - Successful and blocked paths covered:** Verify automated tests exist for both successful and blocked audit paths for each required Phase 1A event family.

### Story 1A.8.2: CUI Audit Filters And Export

- **TC-1A.8.2.1 - CUI audit filters return correct data:** Filter audit events by event type, classification, tenant mode, actor, entity type, date range, and result and verify matching tenant-scoped results.
- **TC-1A.8.2.2 - Unauthorized audit access denied:** As a non-authorized user, attempt to view or export CUI audit events and verify access is denied.
- **TC-1A.8.2.3 - Export tenant scope enforced:** Export CUI audit events from a tenant with neighboring tenant data seeded and verify the export contains only current-tenant events.
- **TC-1A.8.2.4 - Export metadata included:** Verify audit export includes generated by, generated at, tenant, and filter criteria metadata.
- **TC-1A.8.2.5 - Export action audited:** Generate a CUI audit export and verify the export action itself creates an audit event.

## 1A.9 Security Readiness Review

### Story 1A.9.1: Security Review Checklist

- **TC-1A.9.1.1 - Review areas complete:** Verify the security review checklist includes tenant isolation, evidence storage, encryption, malware scanning, retention, backup, restore, admin access, support access, logging, monitoring, and incident response.
- **TC-1A.9.1.2 - Checklist item evidence required:** Complete checklist items and verify status, reviewer, review date, and evidence link or rationale are required.
- **TC-1A.9.1.3 - High or critical findings block approval:** Create high and critical open findings and verify CUI-ready tenant approval is blocked.
- **TC-1A.9.1.4 - Accepted risk metadata complete:** Record an accepted risk and verify approver, date, scope, expiration or review date, and mitigation note are stored.
- **TC-1A.9.1.5 - Security review changes audited:** Create, update, close, and accept risk items and verify audit events are written.

### Story 1A.9.2: Technical Control Verification

- **TC-1A.9.2.1 - CUI tenant isolation tests pass:** Seed CUI-classified records and files in two tenants and verify one tenant cannot access another tenant's classified records or files.
- **TC-1A.9.2.2 - Evidence storage control metadata present:** Upload or inspect evidence files and verify encryption state, scan state, retention state, deletion state, and storage access-control metadata are recorded.
- **TC-1A.9.2.3 - Backup and restore verification documented:** Verify backup and restore evidence includes date, environment, reviewer, and result for classified tenant content metadata.
- **TC-1A.9.2.4 - Admin/support access permission checked:** Attempt admin/support access to CUI-relevant records with allowed and disallowed roles and verify permission checks plus audit events.
- **TC-1A.9.2.5 - Readiness summary complete:** Generate or review the security readiness summary and verify it identifies passed checks, open findings, accepted risks, and release recommendation.

### Story 1A.9.3: Incident Response Readiness

- **TC-1A.9.3.1 - Required playbooks exist:** Verify playbooks exist for accidental CUI upload, suspected CUI in a non-CUI tenant, prohibited data upload, cross-tenant exposure suspicion, malware detection, and failed deletion/export request.
- **TC-1A.9.3.2 - Playbook content complete:** Inspect each playbook and verify trigger, containment steps, notification path, evidence to collect, owner, and closure criteria are present.
- **TC-1A.9.3.3 - Tabletop evidence captured:** Record a readiness tabletop and verify date, participants, findings, and follow-up actions are stored.
- **TC-1A.9.3.4 - Critical response gaps block approval:** Create an open critical incident response gap and verify `CuiReady` approval is blocked.
- **TC-1A.9.3.5 - Incident readiness approval traceable:** Approve incident response readiness and verify the approval is audit logged or source-control traceable.

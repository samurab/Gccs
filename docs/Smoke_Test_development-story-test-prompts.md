# Development Story Test Prompts

Use these prompts to drive implementation or manual verification of each test case in [development-story-test-cases.md](development-story-test-cases.md).


Recommended prefix for manual/smoke-test prompts:

```text
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.
```

## 1. Delivery Foundation

### Done ##
## Story 1.1: Repository And Project Structure.
Please perform Smoke test on  Story 1.1: Repository And Project Structure. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-1.1.1:** Verify the docs/README describe `apps/api`, `apps/web`, `src/Gccs.Domain`, `src/Gccs.Application`, `src/Gccs.Infrastructure`, `packages/compliance-content`, `docs`, and `infra`, including clear ownership boundaries for each.
- **TC-1.1.2:** From a clean checkout, follow the documented restore, build, and test commands, then verify backend and frontend projects build successfully.
- **TC-1.1.3:** Inspect implemented workflows and tests to confirm compliance decisions are enforced in domain/application/API layers and are not UI-only.
- **TC-1.1.4:** Verify developer docs and setup guidance explicitly position the MVP as CUI-ready by design with gated CUI acceptance.
#-----------------------------------

### Done ##
## Story 1.2: Local Development Services
please perform smoke test on Story 1.2: Local Development Services. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-1.2.1:** Run the documented one-command local services startup and verify PostgreSQL, Redis, object storage, and malware-scanning placeholder health checks pass.
- **TC-1.2.2:** Start the API with local environment values and verify health endpoints report connectivity for database, cache, storage, and scanner dependencies.
- **TC-1.2.3:** Remove each required environment variable one at a time and verify startup fails with an actionable missing-config message.
- **TC-1.2.4:** Scan committed config examples and repository files for production credentials, tokens, or real customer data, then report any findings.
#-----------------------------------
### Story 1.3: Continuous Integration Baseline
### Done ##
Please perform Smoke test on  Story 1.3: Continuous Integration Baseline. Please provide the results of the tests.

Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-1.3.1:** Open or simulate a pull request and verify CI runs restore, backend build, frontend build, lint, tests, migration validation, and security scans.
- **TC-1.3.2:** Introduce a controlled failing lint, test, or build condition and verify branch protection marks the pull request unmergeable.
- **TC-1.3.3:** Trigger a failing CI step and verify logs identify the failing project, command, and step without requiring unrelated job inspection.
- **TC-1.3.4:** Trigger or mock a dependency or secret scan finding and verify the failure is visible in pull request checks.
#-----------------------------------
## 2. Tenant, Identity, And RBAC
### Done ## 
### Story 2.1: Tenant Creation
Please perform Smoke test on  Story 2.1: Tenant Creation Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-2.1.1:** Test tenant creation with required metadata and verify ID, display name, status, created date, and updated date persist.
- **TC-2.1.2:** Create tenant-owned sample records and verify each record stores the correct tenant ID.
- **TC-2.1.3:** As a user in tenant A, request tenant B data by ID and verify a 404/403-style response with no data leakage.
- **TC-2.1.4:** Create a tenant and change its status, then verify audit events include tenant, actor, action, timestamps, and before/after status.
#-----------------------------------
#### Story 2.2: User Memberships
### Done ## 
Please perform Smoke test on  Story 2.2: User Memberships. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-2.2.1:** Assign one user to two tenants and verify each membership is visible only when that tenant is active.
- **TC-2.2.2:** Seed users in two tenants and verify each tenant member list excludes users from the other tenant.
- **TC-2.2.3:** Attempt to assign the same user to the same tenant twice and verify duplicate membership validation prevents it.
- **TC-2.2.4:** Add, update, and deactivate a membership, then verify each action is audit logged.
#-----------------------------------
### Story 2.3: User Invitations
### Done ## 
Please perform Smoke test on  Story 2.3: User Invitations. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-2.3.1:** As a tenant admin, invite a user by email and role, then verify token, expiration, pending status, and local notification/email placeholder.
- **TC-2.3.2:** As contributor and auditor roles, call the invitation endpoint directly and verify permission denial.
- **TC-2.3.3:** Attempt to accept expired and revoked invitations and verify no membership is created.
- **TC-2.3.4:** Create, accept, expire, and revoke invitations, then verify each state change is audit logged.
#-----------------------------------
# *** TEST FAILS
### Done ## PLEASE RERUN LATER *****Main missing coverage: profile, contract, task/calendar, direct evidence CRUD, and subcontractor API endpoints are not implemented yet, so TC-2.4 can’t verify runtime RBAC for those endpoint families.
### Story 2.4: Role-Based Permissions
### Done ## 
Please perform Smoke test on  Story 2.4: Role-Based Permissions. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-2.4.1:** For each role, call representative profile, contract, obligation, task, evidence, report, subcontractor, and admin endpoints and verify results match the permission matrix.
- **TC-2.4.2:** Render workspace pages for each role and verify actions the role cannot perform are hidden.
- **TC-2.4.3:** Directly call a restricted action and verify the API returns the standard authorization error response.
- **TC-2.4.4:** Verify an auditor can view approved evidence packages but cannot create, update, approve, delete, or assign tenant data.
#-----------------------------------
## 3. Authenticated Application Shell
### Done ## 
### Story 3.1: Protected API Access
Please perform Smoke test on  Story 3.1: Protected API Access. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-3.1.1:** Call protected endpoints without auth context and verify authentication failure.
- **TC-3.1.2:** Call a protected endpoint with valid dev auth or auth token and verify handlers receive the expected user ID and tenant ID.
- **TC-3.1.3:** Call a protected endpoint with user context but no active tenant and verify the standard missing-tenant error response.
- **TC-3.1.4:** Verify successful and failed API responses/logs include a request correlation ID.
#-----------------------------------
### Story 3.2: SaaS Navigation Shell
### Done ##
Please perform Smoke test on  Story 3.2: SaaS Navigation Shell. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-3.2.1:** Sign in and verify the first authenticated screen is the workspace/dashboard, not a marketing page.
- **TC-3.2.2:** Use keyboard-only navigation to reach each primary route and verify focus states and activation work.
- **TC-3.2.3:** Render navigation for restricted roles and verify hidden items cannot be reached through visible links.
- **TC-3.2.4:** Mock loading, empty, and failed route data and verify understandable UI states are displayed.
#-----------------------------------
## 4. CUI-Ready Gated Controls

### Story 4.1: Data Handling Acknowledgement
### Done ##
Please perform Smoke test on  Story 4.1: Data Handling Acknowledgement. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-4.1.1:** With no acknowledgement on record, open an upload workflow and verify the data handling notice is displayed before upload.
- **TC-4.1.2:** Attempt upload before acknowledgement and verify both UI and API block the upload.
- **TC-4.1.3:** Acknowledge the data handling notice and verify user, tenant, timestamp, and notice version are persisted.
- **TC-4.1.4:** Verify acknowledgement creates an audit event and notice copy says the MVP supports CUI-ready workflows with gated CUI acceptance and real CUI upload requires approved CUI-ready tenant status.
#-----------------------------------
### Story 4.2: Upload Guardrails
### Done ##
Please perform Smoke test on  Story 4.2: Upload Guardrails. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-4.2.1:** Upload an unsupported extension/content type and verify server-side rejection with no usable evidence record.
- **TC-4.2.2:** Upload a file above the configured size limit and verify server-side rejection.
- **TC-4.2.3:** Upload a valid file and verify metadata records validation status and malware scan placeholder status.
- **TC-4.2.4:** Force validation or scan failure and verify the rejected upload is audit logged and not usable.
#-----------------------------------
## 5. Audit Logging

### Story 5.1: Append-Only Audit Events
### Done ##
Please perform Smoke test on  Story 5.1: Append-Only Audit Events. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-5.1.1:** Perform representative sensitive actions and verify audit events include tenant, actor, action, entity, timestamp, and summary.
- **TC-5.1.2:** Attempt to update or delete audit events through normal APIs and verify no mutation path exists.
- **TC-5.1.3:** Simulate audit writer failure during a critical action and verify the action fails or surfaces a clear critical error.
- **TC-5.1.4:** Verify source IP, correlation ID, and request metadata are stored when available.
#-----------------------------------
### Story 5.2: Audit Log Viewer
### Done ##
Please perform Smoke test on  Story 5.2: Audit Log Viewer. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-5.2.1:** As admin, owner, or advisor, view audit logs and verify only current-tenant events appear.
- **TC-5.2.2:** As contributor and auditor roles, call the audit log endpoint and verify access is denied.
- **TC-5.2.3:** Seed more audit events than one page and verify page size, next/previous behavior, and stable ordering.
- **TC-5.2.4:** Filter audit logs by actor, action, date range, and entity type, then verify only matching current-tenant events return.
#-----------------------------------
## 6. Compliance Content Foundation

### Story 6.1: Obligation Schema
### Done ##
Please perform Smoke test on  Story 6.1: Obligation Schema. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-6.1.1:** Attempt to publish an obligation without a source URL and verify validation fails.
- **TC-6.1.2:** Attempt to publish an obligation without a last reviewed date and verify validation fails.
- **TC-6.1.3:** Verify published obligations require risk, owner, confidence, trigger logic, required actions, flow-down flag, and review state.
- **TC-6.1.4:** Link evidence examples to an obligation and verify they are returned with the obligation.
#-----------------------------------
### Story 6.2: Content Import
### Done ##
Please perform Smoke test on  Story 6.2: Content Import. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-6.2.1:** Import a valid compliance content package and verify clauses/obligations are created with source and review metadata.
- **TC-6.2.2:** Import schema-invalid JSON and verify actionable errors identify file, path, and field.
- **TC-6.2.3:** Run the same content import twice and verify duplicate clauses/obligations are not created.
- **TC-6.2.4:** Verify successful and failed imports produce logs or failure reports useful for maintainers.
#-----------------------------------
### Story 6.3: Content Review State
### Done ##
Please perform Smoke test on  Story 6.3: Content Review State. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-6.3.1:** Seed draft and published content, then verify only published content appears in customer-facing search and mapping.
- **TC-6.3.2:** Attempt to publish expert-review-required content without reviewer/date and verify validation fails.
- **TC-6.3.3:** Retire content and verify it cannot be selected for new clause mappings.
- **TC-6.3.4:** Move content through draft, in_review, approved, published, and retired states and verify audit events.
#-----------------------------------
## 7. Company Compliance Profile

### Story 7.1: Create Company Profile
### Done ##
Please perform Smoke test on  Story 7.1: Create Company Profile. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-7.1.1:** Attempt to complete a company profile without required fields and verify validation messages.
- **TC-7.1.2:** Save a partial profile draft and verify it persists without being marked complete.
- **TC-7.1.3:** Add and remove profile data and verify completion percentage recalculates.
- **TC-7.1.4:** Create and update a profile as tenant A, verify audit events, and verify tenant B cannot see it.
#-----------------------------------
### Story 7.2: NAICS And Size Status
### Done ##
Please perform Smoke test on  Story 7.2: NAICS And Size Status. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-7.2.1:** Add multiple valid NAICS codes and verify all are stored on the profile.
- **TC-7.2.2:** Mark one NAICS as primary, switch the primary code, and verify only one primary remains.
- **TC-7.2.3:** Store different size statuses and bases per NAICS code and verify the detail display.
- **TC-7.2.4:** Add a NAICS code without size status and verify profile gaps warn the user.
#-----------------------------------
### Story 7.3: Certification Tracking
### Done ##
Please perform Smoke test on  Story 7.3: Certification Tracking Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-7.3.1:** Add 8(a), WOSB, EDWOSB, HUBZone, SDVOSB, SDB, and custom certifications and verify they persist/display correctly.
- **TC-7.3.2:** Add a certification with an upcoming expiration and verify a calendar renewal task is generated.
- **TC-7.3.3:** Add an expired certification and verify dashboard/profile flags it.
- **TC-7.3.4:** Create, update, and delete/archive certifications and verify audit events.
#-----------------------------------
## 8. Contract Intake

### Story 8.1: Create Contract Record
### Done ##
Please perform Smoke test on  Story 8.1: Create Contract Record. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-8.1.1:** Create draft and active contract records with required fields and verify persistence.
- **TC-8.1.2:** Seed contracts in two tenants and verify each contract list only returns current-tenant contracts.
- **TC-8.1.3:** Open contract detail and verify key dates, role, contract type, agency/prime, and data handling posture.
- **TC-8.1.4:** Create and update a contract and verify both actions write audit events.
#-----------------------------------
### Story 8.2: Contract Document Metadata And Upload
### Done ##
Please perform Smoke test on  Story 8.2: Contract Document Metadata And Upload. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-8.2.1:** Attempt contract document upload without data handling acknowledgement and verify disabled UI plus API rejection.
- **TC-8.2.2:** Upload valid allowed contract document metadata and verify document type, storage reference, scan status, and contract link.
- **TC-8.2.3:** Upload an unsupported contract document and verify rejection with no usable document.
- **TC-8.2.4:** Upload and delete a contract document and verify both actions are audit logged.
#-----------------------------------
### Story 8.3: Contract Dates And Deliverables
### Done ##
Please perform Smoke test on  Story 8.3: Contract Dates And Deliverables. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-8.3.1:** Create deliverables with owner, due date, status, and description and verify they appear on contract detail.
- **TC-8.3.2:** Verify deliverable due dates create or appear as calendar items.
- **TC-8.3.3:** Seed a past-due incomplete deliverable and verify overdue styling/status.
- **TC-8.3.4:** Change deliverable status and verify audit event creation.
#-----------------------------------
## 9. Manual Clause Tagging

### Story 9.1: Clause Library Search
### Done ##
Please perform Smoke test on  Story 9.1: Clause Library Search. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-9.1.1:** Search the clause library by clause number, title text, and category filters and verify expected results.
- **TC-9.1.2:** Seed draft and published clauses and verify only published clauses are available for customer mapping.
- **TC-9.1.3:** Verify each clause search result shows source URL and last reviewed date.
- **TC-9.1.4:** Verify clause search does not expose draft, retired, or other-tenant custom content.
#-----------------------------------
### Story 9.2: Attach Clause To Contract
### Done ##
Please perform Smoke test on  Story 9.2: Attach Clause To Contract. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-9.2.1:** Attach a published clause to a contract with reason and source document reference.
- **TC-9.2.2:** Attach the same clause twice to the same contract and verify duplicate prevention.
- **TC-9.2.3:** Attempt to remove a clause without reason and verify validation fails, then remove it with a reason and verify success.
- **TC-9.2.4:** Verify clause add/remove events are audit logged and cross-tenant contract/clause IDs are denied.
#-----------------------------------
### Story 9.3: Generate Obligations From Clause
### Done ##
Please perform Smoke test on  Story 9.3: Generate Obligations From Clause. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-9.3.1:** Attach a clause with mapped templates and verify contract-specific obligations are generated.
- **TC-9.3.2:** Verify generated obligations link to contract/clause and include source URL, owner, action, evidence examples, risk, confidence, and review metadata.
- **TC-9.3.3:** For templates requiring default tasks, verify tasks are created and linked.
- **TC-9.3.4:** Re-run generation or reprocess the same attachment and verify duplicate obligations/tasks are not created.
#-----------------------------------
## 10. Obligation Dashboard

### Story 10.1: Obligation List And Filters
### Done ##
Please perform Smoke test on  Story 10.1: Obligation List And Filters. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-10.1.1:** Seed obligations in multiple tenants and verify only current-tenant obligations appear on the dashboard.
- **TC-10.1.2:** Filter obligations by contract, risk, owner, status, module, due date, and source and verify matching results.
- **TC-10.1.3:** Verify high-risk and overdue obligations are visually distinct and accessible.
- **TC-10.1.4:** With no obligations, verify the empty state points users to company profile or contract intake.
#-----------------------------------
### Story 10.2: Obligation Detail
### Done ##
Please perform Smoke test on  Story 10.2: Obligation Detail. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-10.2.1:** Open obligation detail and verify summary, trigger, action, owner, evidence examples, flow-down, source link, confidence, last reviewed, and expert review flag.
- **TC-10.2.2:** Link tasks and evidence to an obligation and verify they display on the detail view.
- **TC-10.2.3:** Change obligation status and verify persistence plus dashboard update.
- **TC-10.2.4:** Verify obligation status changes are audit logged and cross-tenant detail access is denied.
#-----------------------------------
### Story 10.3: Ownership Assignment
### Done ##
Please perform Smoke test on  Story 10.3: Ownership Assignment. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-10.3.1:** Assign an obligation to a tenant member and verify dashboard/detail reflects the user owner.
- **TC-10.3.2:** Assign an obligation to a role and verify dashboard/detail reflects the role owner.
- **TC-10.3.3:** As an unauthorized role, call the obligation assignment endpoint and verify denial.
- **TC-10.3.4:** Verify assignment changes are audit logged and a notification is emitted when enabled.
#-----------------------------------
## 11. Task And Compliance Calendar

### Story 11.1: Task Management
### Done ##
Please perform Smoke test on  Story 11.1: Task Management. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-11.1.1:** Create tasks linked to obligations, contracts, controls, evidence, subcontractors, and certifications.
- **TC-11.1.2:** Move tasks through open, in_progress, blocked, completed, canceled, and reopened states according to transition rules.
- **TC-11.1.3:** Attempt to update another tenant's task and verify denial with no data leakage.
- **TC-11.1.4:** Change task status and verify audit events are created.
#-----------------------------------
### Story 11.2: Calendar View
### Done ##
Please perform Smoke test on  Story 11.2: Calendar View. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-11.2.1:** Verify the calendar shows tasks, renewals, reports, contract deadlines, deliverables, and policy reviews.
- **TC-11.2.2:** Filter calendar items by owner, status, risk, contract, and module and verify matching items.
- **TC-11.2.3:** Verify overdue calendar items are visually distinct and accessible to screen readers.
- **TC-11.2.4:** Verify the calendar excludes items from other tenants.
#-----------------------------------
### Story 11.3: Renewal Generation
### Done ##
Please perform Smoke test on  Story 11.3: Renewal Generation. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-11.3.1:** Generate renewal tasks for SAM, certifications, evidence, insurance, policy review, and CMMC affirmation dates.
- **TC-11.3.2:** Run renewal generation twice and verify the same source/due date does not create duplicates.
- **TC-11.3.3:** Verify default and configured lead times produce correct reminder and due dates.
- **TC-11.3.4:** Verify generated renewal tasks link back to the originating profile, evidence, certification, or related source record.
#-----------------------------------
## 12. Evidence Vault

### Story 12.1: Evidence Metadata
### Done ##
Please perform Smoke test on  Story 12.1: Evidence Metadata. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-12.1.1:** Create evidence metadata with title, type, owner, approval status, expiration date, tags, description, and source links.
- **TC-12.1.2:** Link evidence to multiple obligations/controls and verify it is reusable from all linked views.
- **TC-12.1.3:** Add evidence tags and verify list/detail filtering by tags works without folder dependency.
- **TC-12.1.4:** Add an evidence expiration date and verify task generation plus metadata-change audit events.
#-----------------------------------
### Story 12.2: Evidence File Upload
### Done ##
Please perform Smoke test on  Story 12.2: Evidence File Upload. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-12.2.1:** Attempt evidence upload before data handling acknowledgement and verify it is blocked.
- **TC-12.2.2:** Upload an evidence file and verify it is not usable until validation and scan status allow it.
- **TC-12.2.3:** Upload a replacement evidence file and verify a new version is created without overwriting prior metadata.
- **TC-12.2.4:** Verify allowed users can download/delete evidence per RBAC and all download/delete actions are audit logged.
#-----------------------------------
### Story 12.3: Evidence Approval
### Done ##
Please perform Smoke test on  Story 12.3: Evidence Approval. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-12.3.1:** Verify only configured roles can approve evidence.
- **TC-12.3.2:** Reject evidence without comment/reason and verify validation fails.
- **TC-12.3.3:** Approve evidence and verify it becomes eligible for report and evidence package inclusion.
- **TC-12.3.4:** Verify approve, reject, archive, and expire decisions are audit logged.
#-----------------------------------
## 13. CMMC Readiness Tracker

### Story 13.1: CMMC Level Selection
### Done ##
Please perform Smoke test on  Story 13.1: CMMC Level Selection. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-13.1.1:** Create readiness assessments for CMMC Level 1 and Level 2 and verify target level, status, dates, and owner.
- **TC-13.1.2:** Link a readiness assessment to company profile and contracts and verify detail display.
- **TC-13.1.3:** Add control statuses and verify assessment completion progress recalculates.
- **TC-13.1.4:** Create, update, and change assessment status and verify audit events.
#-----------------------------------
### Story 13.2: Control Readiness
### Done ##
Please perform Smoke test on  Story 13.2: Control Readiness. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-13.2.1:** Verify Level 1 controls and Level 2 mappings load for the selected assessment scope.
- **TC-13.2.2:** Set control status to not_started, implemented, partially_implemented, not_applicable, and needs_review.
- **TC-13.2.3:** Link evidence, tasks, assets, and POA&M items to a control and verify they display on detail.
- **TC-13.2.4:** Verify the control baseline source is visible and control status contributes to assessment progress.
#-----------------------------------
### Story 13.3: POA&M Items
### Done ##
Please perform Smoke test on  Story 13.3: POA&M Items. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-13.3.1:** Create a POA&M item with control, gap, remediation plan, owner, due date, risk, and status.
- **TC-13.3.2:** Verify the POA&M-linked task is created or associated and appears on the calendar.
- **TC-13.3.3:** Seed open and overdue POA&M items and verify CMMC summary counts and flags.
- **TC-13.3.4:** Create, update, and change POA&M status and verify audit events.
#-----------------------------------
### Story 13.4: Annual Affirmation Tracker
### Done ##
Please perform Smoke test on  Story 13.4: Annual Affirmation Tracker. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-13.4.1:** Set an annual affirmation due date and verify it appears on the calendar.
- **TC-13.4.2:** Verify an upcoming annual affirmation creates a reminder task.
- **TC-13.4.3:** Link evidence to an affirmation record and verify it displays.
- **TC-13.4.4:** Update affirmation last/due dates and evidence links and verify audit events.
#-----------------------------------
## 14. Subcontractor Flow-Down Tracker

### Story 14.1: Subcontractor Profile
### Done ##
Please perform Smoke test on  Story 14.1: Subcontractor Profile. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-14.1.1:** Create and update a subcontractor with legal name, POC, role, statuses, flags, dates, and workshare percentage.
- **TC-14.1.2:** Link a subcontractor to one or more contracts and verify list/detail display.
- **TC-14.1.3:** Verify CUI access and export-control flags are prominent but do not imply CUI storage.
- **TC-14.1.4:** Verify cross-tenant subcontractor access is denied and profile changes are audit logged.
#-----------------------------------
### Story 14.2: Flow-Down Clause Tracking
### Done ##
Please perform Smoke test on  Story 14.2: Flow-Down Clause Tracking. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-14.2.1:** Assign required flow-down clauses from contract obligations to a subcontractor.
- **TC-14.2.2:** Update flow-down statuses required, sent, acknowledged, signed, waived, and not_applicable, then verify display by subcontractor and contract.
- **TC-14.2.3:** Link approved signed evidence to a flow-down record and verify it appears there.
- **TC-14.2.4:** Verify flow-down assignment and status changes are audit logged.
#-----------------------------------
### Story 14.3: Subcontractor Evidence Requests
### Done ##
Please perform Smoke test on  Story 14.3: Subcontractor Evidence Requests. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-14.3.1:** Create a subcontractor evidence request with requested item, due date, status, recipient, and linked obligation.
- **TC-14.3.2:** Verify subcontractor evidence request due dates appear on the calendar.
- **TC-14.3.3:** Link received evidence to a request and verify request status/completion updates.
- **TC-14.3.4:** Seed an overdue subcontractor evidence request and verify list, calendar, and dashboard warnings.
#-----------------------------------
## 15. Reports

### Story 15.1: Compliance Status Report
### Done ##
Please perform Smoke test on Story 15.1: Compliance Status Report. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-15.1.1:** Generate a compliance status report and verify obligation status, overdue tasks, evidence status, CMMC progress, subcontractor gaps, and high-risk items.
- **TC-15.1.2:** Verify the compliance status report excludes other-tenant data.
- **TC-15.1.3:** Generate a report and verify generation timestamp and snapshot metadata are stored.
- **TC-15.1.4:** Generate a report and verify the action is audit logged.
#-----------------------------------
### Story 15.2: Contract Obligation Matrix
### Done ##
Please perform Smoke test on  Story 15.2: Contract Obligation Matrix. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-15.2.1:** Generate a contract obligation matrix for one contract and verify clause, source, obligation, owner, status, risk, due date, evidence, and flow-down columns.
- **TC-15.2.2:** Verify contract obligation matrix rows include source links and last reviewed dates.
- **TC-15.2.3:** Verify obligations requiring flow-down are clearly identified in the matrix.
- **TC-15.2.4:** Export the contract obligation matrix and compare exported rows/fields to the on-screen data.
#-----------------------------------
### Story 15.3: CMMC Readiness Report
### Done ##
Please perform Smoke test on  Story 15.3: CMMC Readiness Report. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-15.3.1:** Generate a CMMC readiness report and verify control status rollups by family/category.
- **TC-15.3.2:** Verify open POA&M items, gaps, evidence links, and affirmation dates appear in the CMMC readiness report.
- **TC-15.3.3:** As a restricted user, verify inaccessible evidence links are omitted or blocked in the CMMC readiness report.
- **TC-15.3.4:** Verify CMMC readiness report access is role-protected and generated snapshots are retained.
#-----------------------------------
### Story 15.4: Evidence Package
### Done ##
Please perform Smoke test on  Story 15.4: Evidence Package. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-15.4.1:** Generate an evidence package scoped by selected obligations, contract, CMMC controls, or subcontractor.
- **TC-15.4.2:** Verify approved evidence is included by default and draft/rejected evidence is excluded unless explicitly allowed by an authorized user.
- **TC-15.4.3:** Verify the evidence package manifest includes title, evidence type, linked obligation/control, approval state, and timestamp.
- **TC-15.4.4:** Verify the evidence package view is read-only and package generation is audit logged.
#-----------------------------------
### Story 15.5: Subcontractor Compliance Report
### Done ##
Please perform Smoke test on  Story 15.5: Subcontractor Compliance Report. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-15.5.1:** Generate a subcontractor compliance report filtered by contract and verify subcontractor data matches scope.
- **TC-15.5.2:** Verify missing and overdue subcontractor evidence requests are highlighted.
- **TC-15.5.3:** Verify flow-down statuses appear by subcontractor and contract.
- **TC-15.5.4:** Export the subcontractor compliance report and verify no other-tenant data is included.
#-----------------------------------
## 16. Notifications

### Story 16.1: Notification Preferences
### Done ##

Please perform Smoke test on  Story 16.1: Notification Preferences. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-16.1.1:** Create users by role and verify default notification preferences are assigned.
- **TC-16.1.2:** Update preferences for assignments, due soon, overdue, evidence requests, renewals, and CMMC affirmation.
- **TC-16.1.3:** For a multi-tenant user, verify tenant-specific notification preferences do not leak across tenants when applicable.
- **TC-16.1.4:** Change notification preferences and verify audit events.
#-----------------------------------
### Story 16.2: Due-Date Reminders
### Done ##
Please perform Smoke test on  Story 16.2: Due-Date Reminders. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-16.2.1:** Run the due-date reminder job and verify tasks within configured lead time are selected.
- **TC-16.2.2:** Run the reminder job twice and verify the same reminder is not sent repeatedly for the same event.
- **TC-16.2.3:** Verify overdue reminders are categorized or sent separately from upcoming reminders.
- **TC-16.2.4:** Simulate notification/email placeholder failure and verify failure is logged without crashing unrelated deliveries.
#-----------------------------------
### Story 16.3: Assignment Notifications
### Done ##
Please perform Smoke test on  Story 16.3: Assignment Notifications. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-16.3.1:** Assign a task, obligation, POA&M item, or evidence request and verify the assigned user receives a notification.
- **TC-16.3.2:** Open an assignment notification and verify it navigates to the linked source record.
- **TC-16.3.3:** Mark a notification as read and verify state persists.
- **TC-16.3.4:** As an unauthorized user, open a notification link and verify access is denied.
#-----------------------------------
## 17. MVP Hardening And Release Readiness

### Story 17.1: End-To-End Pilot Workflow
### Done ##
Please perform Smoke test on  Story 17.1: End-To-End Pilot Workflow. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-17.1.1:** With non-CUI data, execute the full pilot workflow: onboard tenant/users, create profile, contract, clauses, obligations, tasks, evidence, CMMC records, subcontractors, reports, and notifications.
- **TC-17.1.2:** Execute the pilot workflow with owner, admin, compliance manager, contributor, auditor, and advisor users and verify each role can only perform permitted actions.
- **TC-17.1.3:** Generate reports after the pilot workflow and verify they reflect the workflow data.
- **TC-17.1.4:** Verify automated regression coverage exists for the pilot workflow critical path.
#-----------------------------------
### Story 17.2: Security And Tenant Isolation Verification
### Done ##
Please perform Smoke test on  Story 17.2: Security And Tenant Isolation Verification. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-17.2.1:** Attempt cross-tenant access for every tenant-owned module and verify denial with no data leakage.
- **TC-17.2.2:** Call restricted endpoints directly for each role and verify server-side RBAC denial.
- **TC-17.2.3:** Verify repository/service tests cover tenant filters for tenant-owned queries.
- **TC-17.2.4:** Confirm tenant isolation, RBAC, and audit logging verification results are documented.
#-----------------------------------
### Story 17.3: Staging Environment
### Done ##
Please perform Smoke test on  Story 17.3: Staging Environment. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-17.3.1:** Trigger staging deployment and verify API, web, database, storage, cache, queue, secrets, and jobs provision/deploy.
- **TC-17.3.2:** Verify staging contains no production customer data or production secrets.
- **TC-17.3.3:** Verify staging health checks cover API, database, cache, storage, and jobs.
- **TC-17.3.4:** Run staging smoke tests after deployment and verify success/failure is visible in CI/CD.
#-----------------------------------
### Story 17.4: Production Readiness Checklist
### Done ##
Please perform Smoke test on  Story 17.4: Production Readiness Checklist. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-17.4.1:** Verify release cannot proceed until production readiness checklist items are complete and approved.
- **TC-17.4.2:** Confirm CUI/data-handling limits, malware scanning limitation/path, support path, and prohibited upload guidance are documented.
- **TC-17.4.3:** Verify launch obligations have source URLs, last reviewed dates, confidence, and review metadata.
- **TC-17.4.4:** Execute or simulate staging rollback and verify steps, timing, and outcome are documented.

## Phase 2 - Govcon Intelligence

## 18. Automated Clause Extraction

### Story 18.1: Extraction Job Intake
### Done ##
Please perform Smoke test on  Story 18.1: Extraction Job Intake. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-18.1.1:** Verify user with contract edit permission can start extraction for a document in the current tenant.
- **TC-18.1.2:** Verify user without contract edit permission receives a server-side authorization error.
- **TC-18.1.3:** Verify extraction job stores tenant ID, source document ID, requester ID, status, and timestamps.
- **TC-18.1.4:** Verify starting extraction for another tenant's document is denied.
- **TC-18.1.5:** Verify extraction job creation is audit logged.
#-----------------------------------

### Story 18.2: Text Extraction And Clause Candidate Detection
### Done ##
Please perform Smoke test on  Story 18.2: Text Extraction And Clause Candidate Detection. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-18.2.1:** Verify supported text documents produce clause candidates when recognizable clause references are present.
- **TC-18.2.2:** Verify each candidate includes source document, normalized citation, raw extracted text, confidence, and location metadata when available.
- **TC-18.2.3:** Verify exact matches link to the corresponding clause library record.
- **TC-18.2.4:** Verify unsupported or unreadable documents produce a failed job with a user-visible reason.
- **TC-18.2.5:** Verify extracted text and candidates remain tenant-scoped.
#-----------------------------------

### Story 18.3: Extraction Results Review Screen
### Done ##
Please perform Smoke test on Story 18.3: Extraction Results Review Screen. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-18.3.1:** Verify user can view extraction results for documents in the current tenant.
- **TC-18.3.2:** Verify results show citation, confidence, match status, review status, and source location when available.
- **TC-18.3.3:** Verify accepted candidates create reviewed contract clause links only after user action.
- **TC-18.3.4:** Verify rejected candidates remain visible in extraction history and do not create contract clause links.
- **TC-18.3.5:** Verify candidate edits and review decisions are audit logged.
#-----------------------------------

## 19. Human Review Workflow

### Story 19.1: Review States For Extracted Clauses
### Done ##
Please perform Smoke test on Story 19.1: Review States For Extracted Clauses. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-19.1.1:** Verify new extraction candidates default to pending review.
- **TC-19.1.2:** Verify only users with clause review permission can accept or reject candidates.
- **TC-19.1.3:** Verify accepted candidates record reviewer, reviewed date, and decision note when provided.
- **TC-19.1.4:** Verify rejected and superseded candidates do not generate obligations.
- **TC-19.1.5:** Verify review state transitions are audit logged.
#-----------------------------------

### Story 19.2: AI-Suggested Obligation Review
### Done ##
Please perform Smoke test on Story 19.2: AI-Suggested Obligation Review. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-19.2.1:** Verify AI-suggested obligations are stored with source references, confidence, and draft status.
- **TC-19.2.2:** Verify draft suggestions are not included in approved obligation dashboards or reports.
- **TC-19.2.3:** Verify reviewer can approve, revise, reject, or escalate a suggestion.
- **TC-19.2.4:** Verify approved suggestions record reviewer, approval date, and source citations.
- **TC-19.2.5:** Verify rejected suggestions remain in review history and are audit logged.
#-----------------------------------

### Story 19.3: Expert Escalation Queue
### Done ##
Please perform Smoke test on Story 19.3: Expert Escalation Queue. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-19.3.1:** Verify reviewer can escalate a candidate or suggested obligation with a required reason.
- **TC-19.3.2:** Verify escalated items appear in an expert review queue.
- **TC-19.3.3:** Verify assigned expert receives a notification.
- **TC-19.3.4:** Verify resolution records decision, reviewer, date, and notes.
- **TC-19.3.5:** Verify escalated items cannot be published as approved until resolved.
#-----------------------------------

## 20. Clause Library Expansion
### Done ##
### Story 20.1: Versioned Clause Records
Please perform Smoke test on Story 20.1: Versioned Clause Records. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-20.1.1:** Verify clause records include citation, title, source URL, status, last reviewed date, and review owner.
- **TC-20.1.2:** Verify approved versions can be used for extraction matching and obligation mapping.
- **TC-20.1.3:** Verify deprecated or superseded versions are visible in history but not selected by default for new mappings.
- **TC-20.1.4:** Verify clause version changes preserve prior version history.
- **TC-20.1.5:** Verify clause changes are audit logged.
#-----------------------------------
### Done ##
### Story 20.2: Clause Search And Discovery
Please perform Smoke test on Story 20.2: Clause Search And Discovery. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-20.2.1:** Verify search by exact citation returns the matching approved clause when present.
- **TC-20.2.2:** Verify search by title or keyword returns relevant approved clauses.
- **TC-20.2.3:** Verify filters narrow results by source family, obligation area, and flow-down relevance.
- **TC-20.2.4:** Verify results show source URL, status, and last reviewed date.
- **TC-20.2.5:** Verify draft or under-review clauses are hidden from standard users unless they have content review permission.
#-----------------------------------
### Done ##
### Story 20.3: Clause-To-Obligation Mapping
Please perform Smoke test on Story 20.3: Clause-To-Obligation Mapping. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-20.3.1:** Verify approved clause mapping can generate an obligation for a contract.
- **TC-20.3.2:** Verify mapping requires trigger condition, required action, source URL, confidence, and review metadata before approval.
- **TC-20.3.3:** Verify draft mappings cannot generate customer-visible approved obligations.
- **TC-20.3.4:** Verify mapping changes preserve history.
- **TC-20.3.5:** Verify mapping approval and changes are audit logged.
#-----------------------------------

## 21. Applicability Engine

### Story 21.1: Applicability Facts Model
### Done ##
Please perform Smoke test on Story 21.1: Applicability Facts Model. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-21.1.1:** Verify applicability facts can be derived from existing company, contract, clause, and subcontractor records.
- **TC-21.1.2:** Verify unknown facts are represented explicitly instead of inferred as false.
- **TC-21.1.3:** Verify each fact records source record and last updated date when available.
- **TC-21.1.4:** Verify fact model is tenant-scoped.
- **TC-21.1.5:** Verify fact definitions are documented.
#-----------------------------------

### Story 21.2: Rule Evaluation
### Done ##
Please perform Smoke test on Story 21.2: Rule Evaluation. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-21.2.1:** Verify rule evaluator returns a result state, explanation, source rule ID, and facts used.
- **TC-21.2.2:** Verify missing required facts produce insufficient information or needs review rather than a silent positive result.
- **TC-21.2.3:** Verify rule evaluation is repeatable for the same inputs.
- **TC-21.2.4:** Verify evaluation results are tenant-scoped.
- **TC-21.2.5:** Verify rule evaluator behavior is covered by automated tests.
#-----------------------------------

### Story 21.3: Obligation Applicability Updates
### Done ##
Please perform Smoke test on Story 21.3: Obligation Applicability Updates. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-21.3.1:** Verify updating a relevant fact reevaluates affected obligations.
- **TC-21.3.2:** Verify dashboard displays the current applicability state.
- **TC-21.3.3:** Verify explanation shows source rule, facts used, and missing facts when applicable.
- **TC-21.3.4:** Verify prior result history is retained.
- **TC-21.3.5:** Verify material changes from applicable to not applicable or needs review are audit logged.
#-----------------------------------

## 22. SAM.gov Entity Lookup

### Story 22.1: SAM.gov API Configuration
Please perform Smoke test on Story 22.1: SAM.gov API Configuration. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-22.1.1:** Verify SAM.gov API key is not stored in source control.
- **TC-22.1.2:** Verify lookup service uses configured timeout and retry behavior.
- **TC-22.1.3:** Verify API failures return a standard, user-safe error.
- **TC-22.1.4:** Verify logs do not contain API keys or sensitive response payloads.
- **TC-22.1.5:** Verify adapter can be replaced or mocked in tests.
#-----------------------------------

### Story 22.2: Company Entity Lookup
Please perform Smoke test on Story 22.2: Company Entity Lookup Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-22.2.1:** Verify authorized user can search by UEI or legal business name.
- **TC-22.2.2:** Verify search results show source and retrieved date.
- **TC-22.2.3:** Verify user can apply selected fields to the company profile.
- **TC-22.2.4:** Verify existing profile values are not overwritten without explicit user confirmation.
- **TC-22.2.5:** Verify applied SAM data changes are audit logged.
#-----------------------------------

### Story 22.3: Subcontractor Entity Lookup
### Done ##
Please perform Smoke test on  Story 22.3: Subcontractor Entity Lookup. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-22.3.1:** Verify authorized user can search SAM.gov for a subcontractor by UEI or name.
- **TC-22.3.2:** Verify applied fields update only the current tenant's subcontractor record.
- **TC-22.3.3:** Verify no-match and multiple-match results are shown without changing existing data.
- **TC-22.3.4:** Verify source and retrieved date are stored with applied data.
- **TC-22.3.5:** Verify subcontractor SAM updates are audit logged.
#-----------------------------------

## 23. SBA Size Helper

### Story 23.1: Size Standard Reference Data
### Done ##
Please perform Smoke test on Story 23.1: Size Standard Reference Data. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-23.1.1:** Verify approved size standard records include NAICS, metric, threshold, source URL, effective date, last reviewed date, and status.
- **TC-23.1.2:** Verify draft records are not used in customer-facing helper results.
- **TC-23.1.3:** Verify import rejects records missing source metadata.
- **TC-23.1.4:** Verify deprecated records remain visible to content reviewers.
- **TC-23.1.5:** Verify import and approval actions are audit logged.
#-----------------------------------

### Story 23.2: Company Size Evaluation Helper
### Done ##
Please perform Smoke test on Story 23.2: Company Size Evaluation Helper. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-23.2.1:** Verify evaluation uses approved size standard records only.
- **TC-23.2.2:** Verify missing revenue or employee inputs produce insufficient information.
- **TC-23.2.3:** Verify results show NAICS, metric, threshold, entered value or range, source URL, and run date.
- **TC-23.2.4:** Verify user can save evaluation results to the company profile.
- **TC-23.2.5:** Verify saved evaluations are audit logged.
#-----------------------------------

### Story 23.3: Opportunity NAICS Size Check
### Done ##
Please perform Smoke test on Story 23.3: Opportunity NAICS Size Check. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-23.3.1:** Verify user can run size check for a contract NAICS code.
- **TC-23.3.2:** Verify result shows likely status, source standard, and missing information when applicable.
- **TC-23.3.3:** Verify expert-review recommended result can create a task assigned to an owner.
- **TC-23.3.4:** Verify evaluation history remains available from the contract record.
- **TC-23.3.5:** Verify size check actions are audit logged.
#-----------------------------------

## 24. Subcontractor Tracker Expansion

### Story 24.1: Expanded Subcontractor Compliance Profile
Please perform Smoke test on Story 24.1: Expanded Subcontractor Compliance Profile. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-24.1.1:** Verify authorized user can create and update expanded subcontractor fields.
- **TC-24.1.2:** Verify profile completeness reflects required fields configured for the tenant.
- **TC-24.1.3:** Verify filters return only subcontractors in the current tenant.
- **TC-24.1.4:** Verify expiring insurance or certification dates can be surfaced in list filters.
- **TC-24.1.5:** Verify sensitive field changes are audit logged.
#-----------------------------------

### Story 24.2: Subcontractor Risk Status
Please perform Smoke test on Story 24.2: Subcontractor Risk Status Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-24.2.1:** Verify risk status is calculated from documented inputs.
- **TC-24.2.2:** Verify risk drivers are visible to authorized users.
- **TC-24.2.3:** Verify updating evidence, insurance, NDA, CMMC status, or SAM data updates risk status.
- **TC-24.2.4:** Verify missing or unknown data can produce needs review.
- **TC-24.2.5:** Verify risk calculation is covered by automated tests.
#-----------------------------------

### Story 24.3: Contract-Specific Subcontractor Obligations
Please perform Smoke test on Story 24.3: Contract-Specific Subcontractor Obligations. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-24.3.1:** Verify user can link a subcontractor to a contract and applicable flow-down obligations.
- **TC-24.3.2:** Verify supplier obligations show owner, due date, status, and required evidence.
- **TC-24.3.3:** Verify bulk creation uses accepted flow-down clauses only.
- **TC-24.3.4:** Verify supplier obligations are tenant-scoped.
- **TC-24.3.5:** Verify creation and status changes are audit logged.
#-----------------------------------

## 25. Policy Templates

### Story 25.1: Approved Template Library
Please perform Smoke test on  Story 25.1: Approved Template Library. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-25.1.1:** Verify approved templates include title, category, version, source references, owner, and last reviewed date.
- **TC-25.1.2:** Verify draft templates are hidden from standard users.
- **TC-25.1.3:** Verify deprecated templates remain visible to content reviewers.
- **TC-25.1.4:** Verify template approval requires source and review metadata.
- **TC-25.1.5:** Verify template lifecycle changes are audit logged.
#-----------------------------------

### Story 25.2: Generate Draft Policy From Template
Please perform Smoke test on Story 25.2: Generate Draft Policy From Template. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-25.2.1:** Verify user can generate a draft policy from an approved template.
- **TC-25.2.2:** Verify placeholder values are populated from tenant data when available.
- **TC-25.2.3:** Verify missing placeholder values are flagged for user completion.
- **TC-25.2.4:** Verify generated policy stores source template version and generation date.
- **TC-25.2.5:** Verify generated policy is marked draft until approved by the tenant.
#-----------------------------------

### Story 25.3: Policy Approval And Evidence Linking
Please perform Smoke test on Story 25.3: Policy Approval And Evidence Linking. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-25.3.1:** Verify authorized user can approve, reject, or revise a draft policy.
- **TC-25.3.2:** Verify approved policy records approver, approval date, source template, and review date.
- **TC-25.3.3:** Verify approved policy can be linked to obligations and controls as evidence.
- **TC-25.3.4:** Verify revisions preserve prior approved versions.
- **TC-25.3.5:** Verify policy approval actions are audit logged.
#-----------------------------------

## 26. Evidence Request Workflows

### Story 26.1: Evidence Request Creation
Please perform Smoke test on Story 26.1: Evidence Request Creation. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-26.1.1:** Verify authorized user can create an evidence request tied to a supported record type.
- **TC-26.1.2:** Verify request stores requester, assignee, due date, status, instructions, and related record.
- **TC-26.1.3:** Verify assignee receives notification.
- **TC-26.1.4:** Verify user cannot assign a request to a user or subcontractor outside the tenant context.
- **TC-26.1.5:** Verify request creation is audit logged.
#-----------------------------------

### Story 26.2: Evidence Submission And Review
Please perform Smoke test on Story 26.2: Evidence Submission And Review. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-26.2.1:** Verify assignee can submit evidence to an open request.
- **TC-26.2.2:** Verify upload submissions enforce CUI/data-handling guardrails and tenant scope.
- **TC-26.2.3:** Verify reviewer can accept or return submitted evidence with comments.
- **TC-26.2.4:** Verify accepted evidence is linked to the related requirement.
- **TC-26.2.5:** Verify status changes and review decisions are audit logged.
#-----------------------------------

### Story 26.3: Evidence Request Dashboard
Please perform Smoke test on  Story 26.3: Evidence Request Dashboard. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-26.3.1:** Verify dashboard shows only evidence requests in the current tenant.
- **TC-26.3.2:** Verify filters return requests by status, due date, assignee, related type, and priority.
- **TC-26.3.3:** Verify overdue requests are calculated from due date and current status.
- **TC-26.3.4:** Verify bulk reminders create notifications without changing request status.
- **TC-26.3.5:** Verify auditors can view approved or accepted evidence request records but cannot modify them.
#-----------------------------------

## 27. CMMC Level 2 Readiness Expansion

### Story 27.1: Level 2 Control Assessment Detail
Please perform Smoke test on Story 27.1: Level 2 Control Assessment Detail. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-27.1.1:** Verify authorized user can update Level 2 control assessment detail.
- **TC-27.1.2:** Verify control detail stores implementation, evidence, inherited, ESP responsibility, notes, assessment date, and assessor.
- **TC-27.1.3:** Verify status history is retained.
- **TC-27.1.4:** Verify control updates are tenant-scoped.
- **TC-27.1.5:** Verify control assessment updates are audit logged.
#-----------------------------------

### Story 27.2: Responsibility Matrix
Please perform Smoke test on  Story 27.2: Responsibility Matrix. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-27.2.1:** Verify user can assign responsible party for each Level 2 control.
- **TC-27.2.2:** Verify matrix shows control, responsibility type, owner, provider, evidence status, and notes.
- **TC-27.2.3:** Verify controls marked external or shared require provider or responsibility notes.
- **TC-27.2.4:** Verify responsibility changes are audit logged.
- **TC-27.2.5:** Verify matrix export reflects current tenant data.
#-----------------------------------

### Story 27.3: Readiness Gap Prioritization
Please perform Smoke test on  Story 27.3: Readiness Gap Prioritization. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-27.3.1:** Verify gap priority is calculated from documented inputs.
- **TC-27.3.2:** Verify dashboard lists gaps by priority with reason codes.
- **TC-27.3.3:** Verify user can create a POA&M item or task from a gap.
- **TC-27.3.4:** Verify priority recalculates when control or evidence status changes.
- **TC-27.3.5:** Verify priority rules are covered by automated tests.
#-----------------------------------

### Story 27.4: Level 2 Readiness Report
Please perform Smoke test on Story 27.4: Level 2 Readiness Report. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-27.4.1:** Verify authorized user can generate a Level 2 readiness report.
- **TC-27.4.2:** Verify report includes control status, evidence status, gaps, POA&M items, responsibility matrix, source references, and generated date.
- **TC-27.4.3:** Verify report contains no pass/fail certification language.
- **TC-27.4.4:** Verify report uses tenant-scoped data only.
- **TC-27.4.5:** Verify report generation is audit logged.
#-----------------------------------

## 28. Extraction Content Test Set

### Story 28.1: Curated Test Document Set
please perform smoke test on Story 28.1: Curated Test Document Set. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-28.1.1:** Verify test corpus contains only public, synthetic, or explicitly approved non-CUI documents.
- **TC-28.1.2:** Verify each labeled document includes expected clause citations and source locations when available.
- **TC-28.1.3:** Verify test metadata identifies document type, source family, and limitations.
- **TC-28.1.4:** Verify label set is reviewed before use as a benchmark.
- **TC-28.1.5:** Verify test set data handling rules are documented.
#-----------------------------------

### Story 28.2: Precision And Recall Evaluation
please perform smoke test on Story 28.2: Precision And Recall Evaluation. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-28.2.1:** Verify evaluation runner produces precision, recall, false positive, and false negative metrics.
- **TC-28.2.2:** Verify results identify missed and extra clause detections by document.
- **TC-28.2.3:** Verify threshold failures are visible in CI or scheduled test output.
- **TC-28.2.4:** Verify metrics are stored or published for trend review.
- **TC-28.2.5:** Verify evaluation can run without customer data.
#-----------------------------------

### Story 28.3: Extraction Regression Review

please perform smoke test on Story 28.3: Extraction Regression Review. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-28.3.1:** Verify each reviewed failure has a classification, owner, status, and resolution note.
- **TC-28.3.2:** Verify follow-up tasks can be created from failures.
- **TC-28.3.3:** Verify resolved failures are linked to matcher, library, parser, or label updates when applicable.
- **TC-28.3.4:** Verify release summary shows open extraction risks and metric trends.
- **TC-28.3.5:** Verify regression review records are audit logged or otherwise traceable.
#-----------------------------------


## Phase 1A - CUI Readiness Gate

## 1A.1 Tenant Data Handling Modes
### Story 1A.1.1: Tenant Data Handling Mode Model
Please perform Smoke test on Story 1A.1.1: Tenant Data Handling Mode Model. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-1A.1.1.1:** Create or update tenants through supported workflows and verify each tenant has exactly one active mode: `DemoSandbox`, `NoCui`, or `CuiReady`.
- **TC-1A.1.1.2:** Create a new pilot tenant without an explicit mode and verify it defaults to `NoCui`; create an explicitly demo tenant and verify it starts as `DemoSandbox`.
- **TC-1A.1.1.3:** Attempt to set a tenant to `CuiReady` without an approved checklist and verify the mode change is rejected.
- **TC-1A.1.1.4:** Change tenant mode and verify actor, timestamp, reason, previous mode, new mode, effective date, and approval reference are stored.
- **TC-1A.1.1.5:** Call upload, evidence, report, note, and extraction workflow services and verify each receives the tenant's current data handling mode.
#-----------------------------------
### Story 1A.1.2: Mode-Based Workflow Enforcement
Please perform Smoke test on Story 1A.1.2: Mode-Based Workflow Enforcement. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-1A.1.2.1:** As a `DemoSandbox` tenant, attempt to upload a customer file marked `CUI` and verify it is rejected while seeded synthetic examples remain usable.
- **TC-1A.1.2.2:** As a `NoCui` tenant, attempt to create, classify, process, report on, or export real CUI and verify each action is blocked.
- **TC-1A.1.2.3:** As a `CuiReady` tenant, attempt CUI workflows with missing classification or missing approval checks and verify rejection; repeat with valid checks and verify success.
- **TC-1A.1.2.4:** Call restricted APIs directly for upload, evidence, notes, reports, and extraction and verify server-side mode checks match UI behavior.
- **TC-1A.1.2.5:** Trigger a mode-restricted failure and verify the clear error response plus audit event with tenant, actor, workflow, mode, and result.
#-----------------------------------

## 1A.2 Data Classification Controls
### Story 1A.2.1: Classification Metadata Schema
Please perform Smoke test on Story 1A.2.1: Classification Metadata Schema. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-1A.2.1.1:** Attempt to store uploads, notes, reports, extraction jobs, evidence items, and document records without classification metadata and verify validation fails.
- **TC-1A.2.1.2:** Create content classified as `CUI` in `DemoSandbox` and `NoCui` tenants and verify rejection.
- **TC-1A.2.1.3:** Classify normal customer content as `SyntheticCui` and verify rejection; classify approved demo seed content and verify acceptance.
- **TC-1A.2.1.4:** Save an item classified as `Unknown` and verify report generation, extraction, export, and evidence approval are blocked until review.
- **TC-1A.2.1.5:** Reclassify content and verify previous value, new value, actor, timestamp, source, confidence, reviewer, review date, and reason are retained.
#-----------------------------------
### Story 1A.2.2: Classification UX And Review
Please perform Smoke test on Story 1A.2.2: Classification UX And Review. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-1A.2.2.1:** In upload, note, evidence, report, and extraction flows, attempt submission without selecting or confirming classification and verify UI and API block the action.
- **TC-1A.2.2.2:** Create `Unknown` items and verify they appear in the review queue and cannot be used in reports or extraction jobs.
- **TC-1A.2.2.3:** Classify an item as `Prohibited` and verify it is blocked from use and routed to the escalation workflow.
- **TC-1A.2.2.4:** As an authorized reviewer, update an item's classification with a reason and verify the new classification controls downstream behavior.
- **TC-1A.2.2.5:** Verify list, detail, and report generation screens display the current classification badge for classified items.
#-----------------------------------

## 1A.3 Synthetic CUI Demo Dataset
### Story 1A.3.1: Synthetic Dataset Definition
Please perform Smoke test on Story 1A.3.1: Synthetic Dataset Definition. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-1A.3.1.1:** Review seeded synthetic company, contract, evidence, CMMC, subcontractor, and report examples and verify no real customer CUI, classified data, export-controlled technical data, or customer proprietary data is present.
- **TC-1A.3.1.2:** Import or inspect the dataset and verify every seeded record has `SyntheticCui` classification and dataset version.
- **TC-1A.3.1.3:** Open demo contract, obligation, evidence, CMMC, subcontractor, and report views and verify synthetic labels are visible.
- **TC-1A.3.1.4:** Verify dataset metadata includes purpose, limitations, owner, source basis, review date, and approved reviewer.
- **TC-1A.3.1.5:** Attempt to run demo seed import with unapproved dataset content and verify the import is blocked.
#-----------------------------------
### Story 1A.3.2: Demo Tenant Seeding
Please perform Smoke test on Story 1A.3.2: Demo Tenant Seeding. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-1A.3.2.1:** Run seed workflow for `DemoSandbox`, `NoCui`, and `CuiReady` tenants and verify only the `DemoSandbox` tenant is seeded.
- **TC-1A.3.2.2:** Run demo seed twice for the same tenant and verify duplicate records are not created.
- **TC-1A.3.2.3:** After seeding, verify demo examples appear across contract intake, clause tagging, obligations, evidence, CMMC, subcontractor, report, and escalation workflows.
- **TC-1A.3.2.4:** Attempt seed import through normal admin workflows for customer `NoCui` and `CuiReady` tenants and verify rejection.
- **TC-1A.3.2.5:** Run seed and reset actions and verify audit events include tenant, actor, dataset version, action, timestamp, and result.
#-----------------------------------

## 1A.4 CUI-Ready Tenant Approval Checklist
### Story 1A.4.1: Approval Checklist Model
Please perform Smoke test on Story 1A.4.1: Approval Checklist Model. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-1A.4.1.1:** Attempt to approve a checklist with incomplete required items and verify approval fails.
- **TC-1A.4.1.2:** Complete checklist items and verify owner, reviewer, review date, and supporting note or evidence link are required and stored.
- **TC-1A.4.1.3:** Reject a checklist and verify rejection reason, reviewer, timestamp, and tenant link are retained.
- **TC-1A.4.1.4:** Approve a checklist and verify its ID is required and referenced when changing tenant mode to `CuiReady`.
- **TC-1A.4.1.5:** Create, update, approve, reject, and supersede checklist records and verify audit events for each action.
#-----------------------------------
### Story 1A.4.2: Approval Gate Enforcement
Please perform Smoke test on Story 1A.4.2: Approval Gate Enforcement. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-1A.4.2.1:** Attempt final checklist approval as unauthorized and authorized roles and verify only authorized platform roles succeed.
- **TC-1A.4.2.2:** Try `CuiReady` mode change with incomplete, rejected, expired, and superseded checklists and verify all are blocked.
- **TC-1A.4.2.3:** Approve a checklist and verify approving user, timestamp, checklist version, and approval notes are stored.
- **TC-1A.4.2.4:** Change a tenant to `CuiReady` and verify the mode history points to the approved checklist record.
- **TC-1A.4.2.5:** Trigger failed approval and failed `CuiReady` enablement attempts and verify clear errors plus audit events.
#-----------------------------------

## 1A.5 Shared Responsibility Matrix Baseline
### Story 1A.5.1: Baseline Responsibility Matrix
Please perform Smoke test on Story 1A.5.1: Baseline Responsibility Matrix. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-1A.5.1.1:** Verify the baseline matrix includes tenant administration, user access, MFA, upload classification, evidence storage, encryption, malware scanning, retention, backup, export, deletion, incident reporting, support, and customer content decisions.
- **TC-1A.5.1.2:** Verify each row has responsibility value, notes, source reference when applicable, effective date, review owner, and version.
- **TC-1A.5.1.3:** Attempt to publish a matrix with missing owner or review metadata and verify publication fails.
- **TC-1A.5.1.4:** Publish the matrix and verify it is visible from tenant settings and the CUI approval checklist.
- **TC-1A.5.1.5:** Publish and retire a matrix and verify audit events or source-control traceability capture the lifecycle.
#-----------------------------------
### Story 1A.5.2: Tenant Matrix Acknowledgement
Please perform Smoke test on Story 1A.5.2: Tenant Matrix Acknowledgement. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-1A.5.2.1:** As a tenant admin, view and acknowledge the current matrix and verify acknowledgement status is persisted.
- **TC-1A.5.2.2:** Attempt CUI-ready approval without current matrix acknowledgement and verify the checklist or approval gate blocks it.
- **TC-1A.5.2.3:** Verify history records matrix version, user, tenant, timestamp, and status.
- **TC-1A.5.2.4:** Publish a new matrix version and verify prior acknowledgement is marked outdated for future approvals.
- **TC-1A.5.2.5:** Acknowledge the matrix and verify an audit event records tenant, actor, matrix version, timestamp, and result.
#-----------------------------------

## 1A.6 Customer-Facing Data Handling Notices
### Story 1A.6.1: Versioned Notice Content
Please perform Smoke test on Story 1A.6.1: Versioned Notice Content. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-1A.6.1.1:** Verify published notice content is available for `DemoSandbox`, `NoCui`, and `CuiReady` tenant modes.
- **TC-1A.6.1.2:** Attempt to publish notice content without owner, reviewer, review date, or effective date and verify validation fails.
- **TC-1A.6.1.3:** Retrieve the `NoCui` notice and verify it states that real customer CUI upload is prohibited.
- **TC-1A.6.1.4:** Retrieve the `CuiReady` notice and verify it states CUI handling is limited to approved workflows and customer responsibilities.
- **TC-1A.6.1.5:** Request notice content for multiple tenant modes and workflow contexts and verify the correct published version is returned.
#-----------------------------------
### Story 1A.6.2: Notice Placement And Acknowledgement
Please perform Smoke test on Story 1A.6.2: Notice Placement And Acknowledgement. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-1A.6.2.1:** Without acknowledgement, attempt upload, classified note save, report generation from classified content, and extraction job creation and verify each is blocked.
- **TC-1A.6.2.2:** Acknowledge a notice and verify user, tenant, mode, workflow, notice version, and timestamp are stored.
- **TC-1A.6.2.3:** Publish a new notice version and verify previously acknowledged users are re-prompted before CUI-relevant actions.
- **TC-1A.6.2.4:** Open onboarding, upload, note, report, extraction, and support flows for each mode and verify displayed notice text matches the current mode.
- **TC-1A.6.2.5:** Acknowledge and renew acknowledgement and verify audit events are created.
#-----------------------------------

## 1A.7 CUI Support Escalation Path
### Story 1A.7.1: Escalation Intake And Classification
Please perform Smoke test on Story 1A.7.1: Escalation Intake And Classification. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-1A.7.1.1:** Create escalation records from upload rejection, evidence detail, note detail, report detail, extraction job detail, and support page and verify they persist with required metadata.
- **TC-1A.7.1.2:** Seed escalations in two tenants and verify users only see escalations for their active tenant.
- **TC-1A.7.1.3:** Create a prohibited data escalation and verify the affected content is blocked from use.
- **TC-1A.7.1.4:** As a support agent, assign owner, severity, and status and verify updates persist.
- **TC-1A.7.1.5:** Verify unauthorized users cannot access restricted escalation views and create/update actions are audit logged.
#-----------------------------------
### Story 1A.7.2: Escalation Workflow And Resolution
Please perform Smoke test on Story 1A.7.2: Escalation Workflow And Resolution. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-1A.7.2.1:** Change escalation status and verify actor, timestamp, and note are required and persisted.
- **TC-1A.7.2.2:** Keep escalation in submitted, triage, and contained states and verify affected downloads, exports, extraction jobs, report use, and evidence approval are blocked.
- **TC-1A.7.2.3:** Resolve an escalation and verify resolution type, resolver, timestamp, and summary are stored.
- **TC-1A.7.2.4:** Reopen a resolved escalation and verify prior resolution history remains visible.
- **TC-1A.7.2.5:** Move an escalation through multiple statuses and verify each workflow event is audit logged.
#-----------------------------------

## 1A.8 CUI Audit Event Coverage
### Story 1A.8.1: Required CUI Audit Events
Please perform Smoke test on Story 1A.8.1: Required CUI Audit Events. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-1A.8.1.1:** Perform mode changes, classification create/update, upload block, checklist approval/rejection, matrix acknowledgement, notice acknowledgement, download, export, deletion, escalation create/update, and extraction start/stop and verify matching audit events.
- **TC-1A.8.1.2:** Trigger blocked upload, blocked extraction, blocked report, failed mode change, and failed CUI approval and verify failure-path audit events.
- **TC-1A.8.1.3:** Inspect Phase 1A audit events and verify tenant ID, actor ID, event type, entity reference, classification, mode, timestamp, request metadata, and result when applicable.
- **TC-1A.8.1.4:** Audit CUI-relevant actions with document text and verify event summaries do not expose sensitive document content.
- **TC-1A.8.1.5:** Verify automated tests exist for both successful and blocked audit paths for each required Phase 1A event family.
#-----------------------------------
### Story 1A.8.2: CUI Audit Filters And Export
Please perform Smoke test on Story 1A.8.2: CUI Audit Filters And Export. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-1A.8.2.1:** Filter audit events by event type, classification, tenant mode, actor, entity type, date range, and result and verify matching tenant-scoped results.
- **TC-1A.8.2.2:** As a non-authorized user, attempt to view or export CUI audit events and verify access is denied.
- **TC-1A.8.2.3:** Export CUI audit events from a tenant with neighboring tenant data seeded and verify the export contains only current-tenant events.
- **TC-1A.8.2.4:** Verify audit export includes generated by, generated at, tenant, and filter criteria metadata.
- **TC-1A.8.2.5:** Generate a CUI audit export and verify the export action itself creates an audit event.
#-----------------------------------

## 1A.9 Security Readiness Review
### Story 1A.9.1: Security Review Checklist
Please perform Smoke test on Story 1A.9.1: Security Review Checklist. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-1A.9.1.1:** Verify the security review checklist includes tenant isolation, evidence storage, encryption, malware scanning, retention, backup, restore, admin access, support access, logging, monitoring, and incident response.
- **TC-1A.9.1.2:** Complete checklist items and verify status, reviewer, review date, and evidence link or rationale are required.
- **TC-1A.9.1.3:** Create high and critical open findings and verify CUI-ready tenant approval is blocked.
- **TC-1A.9.1.4:** Record an accepted risk and verify approver, date, scope, expiration or review date, and mitigation note are stored.
- **TC-1A.9.1.5:** Create, update, close, and accept risk items and verify audit events are written.
#-----------------------------------
### Story 1A.9.2: Technical Control Verification
Please perform Smoke test on Story 1A.9.2: Technical Control Verification. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-1A.9.2.1:** Seed CUI-classified records and files in two tenants and verify one tenant cannot access another tenant's classified records or files.
- **TC-1A.9.2.2:** Upload or inspect evidence files and verify encryption state, scan state, retention state, deletion state, and storage access-control metadata are recorded.
- **TC-1A.9.2.3:** Verify backup and restore evidence includes date, environment, reviewer, and result for classified tenant content metadata.
- **TC-1A.9.2.4:** Attempt admin/support access to CUI-relevant records with allowed and disallowed roles and verify permission checks plus audit events.
- **TC-1A.9.2.5:** Generate or review the security readiness summary and verify it identifies passed checks, open findings, accepted risks, and release recommendation.
#-----------------------------------
### Story 1A.9.3: Incident Response Readiness
Please perform Smoke test on Story 1A.9.3: Incident Response Readiness. Please provide the results of the tests.
Using the local GCCS app, execute the following test case as a verification script. Capture setup data, exact steps, expected result, actual result, and any defects or missing coverage.

- **TC-1A.9.3.1:** Verify playbooks exist for accidental CUI upload, suspected CUI in a non-CUI tenant, prohibited data upload, cross-tenant exposure suspicion, malware detection, and failed deletion/export request.
- **TC-1A.9.3.2:** Inspect each playbook and verify trigger, containment steps, notification path, evidence to collect, owner, and closure criteria are present.
- **TC-1A.9.3.3:** Record a readiness tabletop and verify date, participants, findings, and follow-up actions are stored.
- **TC-1A.9.3.4:** Create an open critical incident response gap and verify `CuiReady` approval is blocked.
- **TC-1A.9.3.5:** Approve incident response readiness and verify the approval is audit logged or source-control traceable.
#-----------------------------------

# Development Story Test Prompts

Use these prompts to drive implementation or manual verification of each test case in [development-story-test-cases.md](development-story-test-cases.md).

Recommended prefix for automated-test prompts:

```text
Using the existing GCCS architecture and test patterns, create or update automated tests for the following test case. Keep tenant isolation, server-side RBAC, audit logging, No-CUI controls, and standard error handling in scope where relevant. Run the narrowest relevant test command and report results.
```


## 1. Delivery Foundation

### Done ## 
## Please perform automated test on Story 1.1: Repository And Project Structure. Please provide the results of the tests.
Using the existing GCCS architecture and test patterns, create or update automated tests for the following test case. Keep tenant isolation, server-side RBAC, audit logging, No-CUI controls, and standard error handling in scope where relevant. Run the narrowest relevant test command and report results.

- **TC-1.1.1:** Verify the docs/README describe `apps/api`, `apps/web`, `src/Gccs.Domain`, `src/Gccs.Application`, `src/Gccs.Infrastructure`, `packages/compliance-content`, `docs`, and `infra`, including clear ownership boundaries for each.
- **TC-1.1.2:** From a clean checkout, follow the documented restore, build, and test commands, then verify backend and frontend projects build successfully.
- **TC-1.1.3:** Inspect implemented workflows and tests to confirm compliance decisions are enforced in domain/application/API layers and are not UI-only.
- **TC-1.1.4:** Verify developer docs and setup guidance explicitly position the MVP as No-CUI / compliance management only.
#-----------------------------------
### Done ## 
### Please perform automated test on Story 1.2: Local Development Services. Please provide the results of the tests.
Using the existing GCCS architecture and test patterns, create or update automated tests for the following test case. Keep tenant isolation, server-side RBAC, audit logging, No-CUI controls, and standard error handling in scope where relevant. Run the narrowest relevant test command and report results.

- **TC-1.2.1:** Run the documented one-command local services startup and verify PostgreSQL, Redis, object storage, and malware-scanning placeholder health checks pass.
- **TC-1.2.2:** Start the API with local environment values and verify health endpoints report connectivity for database, cache, storage, and scanner dependencies.
- **TC-1.2.3:** Remove each required environment variable one at a time and verify startup fails with an actionable missing-config message.
- **TC-1.2.4:** Scan committed config examples and repository files for production credentials, tokens, or real customer data, then report any findings.
#-----------------------------------
### Done ## 
### Please perform automated test on Story 1.3: Continuous Integration Baseline. Please provide the results of the tests.
Using the existing GCCS architecture and test patterns, create or update automated tests for the following test case. Keep tenant isolation, server-side RBAC, audit logging, No-CUI controls, and standard error handling in scope where relevant. Run the narrowest relevant test command and report results.

- **TC-1.3.1:** Open or simulate a pull request and verify CI runs restore, backend build, frontend build, lint, tests, migration validation, and security scans.
- **TC-1.3.2:** Introduce a controlled failing lint, test, or build condition and verify branch protection marks the pull request unmergeable.
- **TC-1.3.3:** Trigger a failing CI step and verify logs identify the failing project, command, and step without requiring unrelated job inspection.
- **TC-1.3.4:** Trigger or mock a dependency or secret scan finding and verify the failure is visible in pull request checks.
#-----------------------------------

## 2. Tenant, Identity, And RBAC
### Done ## 
### Please perform automated test on Story 2.1: Tenant Creation. Please provide the results of the tests.
Using the existing GCCS architecture and test patterns, create or update automated tests for the following test case. Keep tenant isolation, server-side RBAC, audit logging, No-CUI controls, and standard error handling in scope where relevant. Run the narrowest relevant test command and report results.

- **TC-2.1.1:** Test tenant creation with required metadata and verify ID, display name, status, created date, and updated date persist.
- **TC-2.1.2:** Create tenant-owned sample records and verify each record stores the correct tenant ID.
- **TC-2.1.3:** As a user in tenant A, request tenant B data by ID and verify a 404/403-style response with no data leakage.
- **TC-2.1.4:** Create a tenant and change its status, then verify audit events include tenant, actor, action, timestamps, and before/after status.
#-----------------------------------
### Done ## 
### Please perform automated test on Story 2.2: User Memberships. Please provide the results of the tests.
Using the existing GCCS architecture and test patterns, create or update automated tests for the following test case. Keep tenant isolation, server-side RBAC, audit logging, No-CUI controls, and standard error handling in scope where relevant. Run the narrowest relevant test command and report results.

- **TC-2.2.1:** Assign one user to two tenants and verify each membership is visible only when that tenant is active.
- **TC-2.2.2:** Seed users in two tenants and verify each tenant member list excludes users from the other tenant.
- **TC-2.2.3:** Attempt to assign the same user to the same tenant twice and verify duplicate membership validation prevents it.
- **TC-2.2.4:** Add, update, and deactivate a membership, then verify each action is audit logged.
#-----------------------------------

### Please perform automated test on Story 2.3: User Invitations. Please provide the results of the tests.
### Done ## 
Using the existing GCCS architecture and test patterns, create or update automated tests for the following test case. Keep tenant isolation, server-side RBAC, audit logging, No-CUI controls, and standard error handling in scope where relevant. Run the narrowest relevant test command and report results.

- **TC-2.3.1:** As a tenant admin, invite a user by email and role, then verify token, expiration, pending status, and local notification/email placeholder.
- **TC-2.3.2:** As contributor and auditor roles, call the invitation endpoint directly and verify permission denial.
- **TC-2.3.3:** Attempt to accept expired and revoked invitations and verify no membership is created.
- **TC-2.3.4:** Create, accept, expire, and revoke invitations, then verify each state change is audit logged.
#-----------------------------------
# ******* TEST FAILS
### Done ## PLEASE RERUN LATER *****Main missing coverage: profile, contract, task/calendar, direct evidence CRUD, and subcontractor API endpoints are not implemented yet, so TC-2.4 can’t verify runtime RBAC for those endpoint families.
###  Please perform automated test on Story 2.4: Role-Based Permissions. Please provide the results of the tests.

Using the existing GCCS architecture and test patterns, create or update automated tests for the following test case. Keep tenant isolation, server-side RBAC, audit logging, No-CUI controls, and standard error handling in scope where relevant. Run the narrowest relevant test command and report results.

- **TC-2.4.1:** For each role, call representative profile, contract, obligation, task, evidence, report, subcontractor, and admin endpoints and verify results match the permission matrix.
- **TC-2.4.2:** Render workspace pages for each role and verify actions the role cannot perform are hidden.
- **TC-2.4.3:** Directly call a restricted action and verify the API returns the standard authorization error response.
- **TC-2.4.4:** Verify an auditor can view approved evidence packages but cannot create, update, approve, delete, or assign tenant data.
#-----------------------------------

## 3. Authenticated Application Shell
### Done ##
###  Please perform automated test on Story 3.1: Protected API Access. Please provide the results of the tests.
Using the existing GCCS architecture and test patterns, create or update automated tests for the following test case. Keep tenant isolation, server-side RBAC, audit logging, No-CUI controls, and standard error handling in scope where relevant. Run the narrowest relevant test command and report results.

- **TC-3.1.1:** Call protected endpoints without auth context and verify authentication failure.
- **TC-3.1.2:** Call a protected endpoint with valid dev auth or auth token and verify handlers receive the expected user ID and tenant ID.
- **TC-3.1.3:** Call a protected endpoint with user context but no active tenant and verify the standard missing-tenant error response.
- **TC-3.1.4:** Verify successful and failed API responses/logs include a request correlation ID.
#-----------------------------------

###  Please perform automated test on Story 3.2: SaaS Navigation Shell. Please provide the results of the tests.
Using the existing GCCS architecture and test patterns, create or update automated tests for the following test case. Keep tenant isolation, server-side RBAC, audit logging, No-CUI controls, and standard error handling in scope where relevant. Run the narrowest relevant test command and report results.

- **TC-3.2.1:** Sign in and verify the first authenticated screen is the workspace/dashboard, not a marketing page.
- **TC-3.2.2:** Use keyboard-only navigation to reach each primary route and verify focus states and activation work.
- **TC-3.2.3:** Render navigation for restricted roles and verify hidden items cannot be reached through visible links.
- **TC-3.2.4:** Mock loading, empty, and failed route data and verify understandable UI states are displayed.
#-----------------------------------

## 4. No-CUI Controls

###  Please perform automated test on Story 4.1: No-CUI Acknowledgement. Please provide the results of the tests.
### Done ##
Using the existing GCCS architecture and test patterns, create or update automated tests for the following test case. Keep tenant isolation, server-side RBAC, audit logging, No-CUI controls, and standard error handling in scope where relevant. Run the narrowest relevant test command and report results.

- **TC-4.1.1:** With no acknowledgement on record, open an upload workflow and verify the No-CUI notice is displayed before upload.
- **TC-4.1.2:** Attempt upload before acknowledgement and verify both UI and API block the upload.
- **TC-4.1.3:** Acknowledge the No-CUI notice and verify user, tenant, timestamp, and notice version are persisted.
- **TC-4.1.4:** Verify acknowledgement creates an audit event and notice copy says the MVP is compliance management only and not ready to store CUI.
#-----------------------------------

###  Please perform automated test on Story 4.2: Upload Guardrails. Please provide the results of the tests.
### Done ##
Using the existing GCCS architecture and test patterns, create or update automated tests for the following test case. Keep tenant isolation, server-side RBAC, audit logging, No-CUI controls, and standard error handling in scope where relevant. Run the narrowest relevant test command and report results.

- **TC-4.2.1:** Upload an unsupported extension/content type and verify server-side rejection with no usable evidence record.
- **TC-4.2.2:** Upload a file above the configured size limit and verify server-side rejection.
- **TC-4.2.3:** Upload a valid file and verify metadata records validation status and malware scan placeholder status.
- **TC-4.2.4:** Force validation or scan failure and verify the rejected upload is audit logged and not usable.
#-----------------------------------
## 5. Audit Logging

###  Please perform an automated test on Story 5.1: Append-Only Audit Events. Please provide the results of the tests.
### Done ##
Using the existing GCCS architecture and test patterns, create or update automated tests for the following test case. Keep tenant isolation, server-side RBAC, audit logging, No-CUI controls, and standard error handling in scope where relevant. Run the narrowest relevant test command and report results.

- **TC-5.1.1:** Perform representative sensitive actions and verify audit events include tenant, actor, action, entity, timestamp, and summary.
- **TC-5.1.2:** Attempt to update or delete audit events through normal APIs and verify no mutation path exists.
- **TC-5.1.3:** Simulate audit writer failure during a critical action and verify the action fails or surfaces a clear critical error.
- **TC-5.1.4:** Verify source IP, correlation ID, and request metadata are stored when available.
#-----------------------------------

###  Please perform automated test on Story 5.2: Audit Log Viewer. Please provide the results of the tests.
### Done ##
Using the existing GCCS architecture and test patterns, create or update automated tests for the following test case. Keep tenant isolation, server-side RBAC, audit logging, No-CUI controls, and standard error handling in scope where relevant. Run the narrowest relevant test command and report results.

- **TC-5.2.1:** As admin, owner, or advisor, view audit logs and verify only current-tenant events appear.
- **TC-5.2.2:** As contributor and auditor roles, call the audit log endpoint and verify access is denied.
- **TC-5.2.3:** Seed more audit events than one page and verify page size, next/previous behavior, and stable ordering.
- **TC-5.2.4:** Filter audit logs by actor, action, date range, and entity type, then verify only matching current-tenant events return.
#-----------------------------------

## 6. Compliance Content Foundation

###  Please perform automated test on Story 6.1: Obligation Schema. Please provide the results of the tests.
### Done ##
Using the existing GCCS architecture and test patterns, create or update automated tests for the following test case. Keep tenant isolation, server-side RBAC, audit logging, No-CUI controls, and standard error handling in scope where relevant. Run the narrowest relevant test command and report results.

- **TC-6.1.1:** Attempt to publish an obligation without a source URL and verify validation fails.
- **TC-6.1.2:** Attempt to publish an obligation without a last reviewed date and verify validation fails.
- **TC-6.1.3:** Verify published obligations require risk, owner, confidence, trigger logic, required actions, flow-down flag, and review state.
- **TC-6.1.4:** Link evidence examples to an obligation and verify they are returned with the obligation.
#-----------------------------------

###  Please perform automated test on Story 6.2: Content Import. Please provide the results of the tests.
### Done ##
Using the existing GCCS architecture and test patterns, create or update automated tests for the following test case. Keep tenant isolation, server-side RBAC, audit logging, No-CUI controls, and standard error handling in scope where relevant. Run the narrowest relevant test command and report results.

- **TC-6.2.1:** Import a valid compliance content package and verify clauses/obligations are created with source and review metadata.
- **TC-6.2.2:** Import schema-invalid JSON and verify actionable errors identify file, path, and field.
- **TC-6.2.3:** Run the same content import twice and verify duplicate clauses/obligations are not created.
- **TC-6.2.4:** Verify successful and failed imports produce logs or failure reports useful for maintainers.
#-----------------------------------

###  Please perform automated test on Story 6.3: Content Review State. Please provide the results of the tests.
### Done ##
Using the existing GCCS architecture and test patterns, create or update automated tests for the following test case. Keep tenant isolation, server-side RBAC, audit logging, No-CUI controls, and standard error handling in scope where relevant. Run the narrowest relevant test command and report results.

- **TC-6.3.1:** Seed draft and published content, then verify only published content appears in customer-facing search and mapping.
- **TC-6.3.2:** Attempt to publish expert-review-required content without reviewer/date and verify validation fails.
- **TC-6.3.3:** Retire content and verify it cannot be selected for new clause mappings.
- **TC-6.3.4:** Move content through draft, in_review, approved, published, and retired states and verify audit events.
#-----------------------------------

## 7. Company Compliance Profile

###  Please perform automated test on Story 7.1: Create Company Profile. Please provide the results of the tests.
### Done ##
Using the existing GCCS architecture and test patterns, create or update automated tests for the following test case. Keep tenant isolation, server-side RBAC, audit logging, No-CUI controls, and standard error handling in scope where relevant. Run the narrowest relevant test command and report results.

- **TC-7.1.1:** Attempt to complete a company profile without required fields and verify validation messages.
- **TC-7.1.2:** Save a partial profile draft and verify it persists without being marked complete.
- **TC-7.1.3:** Add and remove profile data and verify completion percentage recalculates.
- **TC-7.1.4:** Create and update a profile as tenant A, verify audit events, and verify tenant B cannot see it.
#-----------------------------------

###  Please perform automated test on Story 7.2: NAICS And Size Status. Please provide the results of the tests.
### Done ##
Using the existing GCCS architecture and test patterns, create or update automated tests for the following test case. Keep tenant isolation, server-side RBAC, audit logging, No-CUI controls, and standard error handling in scope where relevant. Run the narrowest relevant test command and report results.

- **TC-7.2.1:** Add multiple valid NAICS codes and verify all are stored on the profile.
- **TC-7.2.2:** Mark one NAICS as primary, switch the primary code, and verify only one primary remains.
- **TC-7.2.3:** Store different size statuses and bases per NAICS code and verify the detail display.
- **TC-7.2.4:** Add a NAICS code without size status and verify profile gaps warn the user.
#-----------------------------------

###  Please perform automated test on Story 7.3: Certification Tracking. Please provide the results of the tests.
### Done ##
Using the existing GCCS architecture and test patterns, create or update automated tests for the following test case. Keep tenant isolation, server-side RBAC, audit logging, No-CUI controls, and standard error handling in scope where relevant. Run the narrowest relevant test command and report results.

- **TC-7.3.1:** Add 8(a), WOSB, EDWOSB, HUBZone, SDVOSB, SDB, and custom certifications and verify they persist/display correctly.
- **TC-7.3.2:** Add a certification with an upcoming expiration and verify a calendar renewal task is generated.
- **TC-7.3.3:** Add an expired certification and verify dashboard/profile flags it.
- **TC-7.3.4:** Create, update, and delete/archive certifications and verify audit events.
#-----------------------------------

## 8. Contract Intake

###  Please perform automated test on Story 8.1: Create Contract Record. Please provide the results of the tests.
### Done ##
Using the existing GCCS architecture and test patterns, create or update automated tests for the following test case. Keep tenant isolation, server-side RBAC, audit logging, No-CUI controls, and standard error handling in scope where relevant. Run the narrowest relevant test command and report results.

- **TC-8.1.1:** Create draft and active contract records with required fields and verify persistence.
- **TC-8.1.2:** Seed contracts in two tenants and verify each contract list only returns current-tenant contracts.
- **TC-8.1.3:** Open contract detail and verify key dates, role, contract type, agency/prime, and data handling posture.
- **TC-8.1.4:** Create and update a contract and verify both actions write audit events.
#-----------------------------------

###  Please perform automated test on Story 8.2: Contract Document Metadata And Upload. Please provide the results of the tests.
### Done ##
Using the existing GCCS architecture and test patterns, create or update automated tests for the following test case. Keep tenant isolation, server-side RBAC, audit logging, No-CUI controls, and standard error handling in scope where relevant. Run the narrowest relevant test command and report results.

- **TC-8.2.1:** Attempt contract document upload without No-CUI acknowledgement and verify disabled UI plus API rejection.
- **TC-8.2.2:** Upload valid non-CUI contract document metadata and verify document type, storage reference, scan status, and contract link.
- **TC-8.2.3:** Upload an unsupported contract document and verify rejection with no usable document.
- **TC-8.2.4:** Upload and delete a contract document and verify both actions are audit logged.
#-----------------------------------

###  Please perform automated test on Story 8.3: Contract Dates And Deliverables. Please provide the results of the tests.
### Done ##
Using the existing GCCS architecture and test patterns, create or update automated tests for the following test case. Keep tenant isolation, server-side RBAC, audit logging, No-CUI controls, and standard error handling in scope where relevant. Run the narrowest relevant test command and report results.

- **TC-8.3.1:** Create deliverables with owner, due date, status, and description and verify they appear on contract detail.
- **TC-8.3.2:** Verify deliverable due dates create or appear as calendar items.
- **TC-8.3.3:** Seed a past-due incomplete deliverable and verify overdue styling/status.
- **TC-8.3.4:** Change deliverable status and verify audit event creation.
#-----------------------------------
## 9. Manual Clause Tagging

###  Please perform automated test on Story 9.1: Clause Library Search. Please provide the results of the tests.
### Done ##
Using the existing GCCS architecture and test patterns, create or update automated tests for the following test case. Keep tenant isolation, server-side RBAC, audit logging, No-CUI controls, and standard error handling in scope where relevant. Run the narrowest relevant test command and report results.

- **TC-9.1.1:** Search the clause library by clause number, title text, and category filters and verify expected results.
- **TC-9.1.2:** Seed draft and published clauses and verify only published clauses are available for customer mapping.
- **TC-9.1.3:** Verify each clause search result shows source URL and last reviewed date.
- **TC-9.1.4:** Verify clause search does not expose draft, retired, or other-tenant custom content.
#-----------------------------------

###  Please perform automated test on Story 9.2: Attach Clause To Contract. Please provide the results of the tests.
### Done ##
Using the existing GCCS architecture and test patterns, create or update automated tests for the following test case. Keep tenant isolation, server-side RBAC, audit logging, No-CUI controls, and standard error handling in scope where relevant. Run the narrowest relevant test command and report results.

- **TC-9.2.1:** Attach a published clause to a contract with reason and source document reference.
- **TC-9.2.2:** Attach the same clause twice to the same contract and verify duplicate prevention.
- **TC-9.2.3:** Attempt to remove a clause without reason and verify validation fails, then remove it with a reason and verify success.
- **TC-9.2.4:** Verify clause add/remove events are audit logged and cross-tenant contract/clause IDs are denied.
#-----------------------------------

###  Please perform automated test on Story 9.3: Generate Obligations From Clause. Please provide the results of the tests.
### Done ##
Using the existing GCCS architecture and test patterns, create or update automated tests for the following test case. Keep tenant isolation, server-side RBAC, audit logging, No-CUI controls, and standard error handling in scope where relevant. Run the narrowest relevant test command and report results.

- **TC-9.3.1:** Attach a clause with mapped templates and verify contract-specific obligations are generated.
- **TC-9.3.2:** Verify generated obligations link to contract/clause and include source URL, owner, action, evidence examples, risk, confidence, and review metadata.
- **TC-9.3.3:** For templates requiring default tasks, verify tasks are created and linked.
- **TC-9.3.4:** Re-run generation or reprocess the same attachment and verify duplicate obligations/tasks are not created.
#-----------------------------------

## 10. Obligation Dashboard

###  Please perform automated test on Story 10.1: Obligation List And Filters. Please provide the results of the tests.
Using the existing GCCS architecture and test patterns, create or update automated tests for the following test case. Keep tenant isolation, server-side RBAC, audit logging, No-CUI controls, and standard error handling in scope where relevant. Run the narrowest relevant test command and report results.

- **TC-10.1.1:** Seed obligations in multiple tenants and verify only current-tenant obligations appear on the dashboard.
- **TC-10.1.2:** Filter obligations by contract, risk, owner, status, module, due date, and source and verify matching results.
- **TC-10.1.3:** Verify high-risk and overdue obligations are visually distinct and accessible.
- **TC-10.1.4:** With no obligations, verify the empty state points users to company profile or contract intake.
#-----------------------------------

###  Please perform automated test on Story 10.2: Obligation Detail. Please provide the results of the tests.
Using the existing GCCS architecture and test patterns, create or update automated tests for the following test case. Keep tenant isolation, server-side RBAC, audit logging, No-CUI controls, and standard error handling in scope where relevant. Run the narrowest relevant test command and report results.

- **TC-10.2.1:** Open obligation detail and verify summary, trigger, action, owner, evidence examples, flow-down, source link, confidence, last reviewed, and expert review flag.
- **TC-10.2.2:** Link tasks and evidence to an obligation and verify they display on the detail view.
- **TC-10.2.3:** Change obligation status and verify persistence plus dashboard update.
- **TC-10.2.4:** Verify obligation status changes are audit logged and cross-tenant detail access is denied.
#-----------------------------------

###  Please perform automated test on Story 10.3: Ownership Assignment. Please provide the results of the tests.
Using the existing GCCS architecture and test patterns, create or update automated tests for the following test case. Keep tenant isolation, server-side RBAC, audit logging, No-CUI controls, and standard error handling in scope where relevant. Run the narrowest relevant test command and report results.

- **TC-10.3.1:** Assign an obligation to a tenant member and verify dashboard/detail reflects the user owner.
- **TC-10.3.2:** Assign an obligation to a role and verify dashboard/detail reflects the role owner.
- **TC-10.3.3:** As an unauthorized role, call the obligation assignment endpoint and verify denial.
- **TC-10.3.4:** Verify assignment changes are audit logged and a notification is emitted when enabled.
#-----------------------------------

## 11. Task And Compliance Calendar

###  Please perform automated test on Story 11.1: Task Management. Please provide the results of the tests.
Using the existing GCCS architecture and test patterns, create or update automated tests for the following test case. Keep tenant isolation, server-side RBAC, audit logging, No-CUI controls, and standard error handling in scope where relevant. Run the narrowest relevant test command and report results.

- **TC-11.1.1:** Create tasks linked to obligations, contracts, controls, evidence, subcontractors, and certifications.
- **TC-11.1.2:** Move tasks through open, in_progress, blocked, completed, canceled, and reopened states according to transition rules.
- **TC-11.1.3:** Attempt to update another tenant's task and verify denial with no data leakage.
- **TC-11.1.4:** Change task status and verify audit events are created.
#-----------------------------------

###  Please perform automated test on Story 11.2: Calendar View. Please provide the results of the tests.
Using the existing GCCS architecture and test patterns, create or update automated tests for the following test case. Keep tenant isolation, server-side RBAC, audit logging, No-CUI controls, and standard error handling in scope where relevant. Run the narrowest relevant test command and report results.

- **TC-11.2.1:** Verify the calendar shows tasks, renewals, reports, contract deadlines, deliverables, and policy reviews.
- **TC-11.2.2:** Filter calendar items by owner, status, risk, contract, and module and verify matching items.
- **TC-11.2.3:** Verify overdue calendar items are visually distinct and accessible to screen readers.
- **TC-11.2.4:** Verify the calendar excludes items from other tenants.
#-----------------------------------

###  Please perform automated test on Story 11.3: Renewal Generation. Please provide the results of the tests.
Using the existing GCCS architecture and test patterns, create or update automated tests for the following test case. Keep tenant isolation, server-side RBAC, audit logging, No-CUI controls, and standard error handling in scope where relevant. Run the narrowest relevant test command and report results.

- **TC-11.3.1:** Generate renewal tasks for SAM, certifications, evidence, insurance, policy review, and CMMC affirmation dates.
- **TC-11.3.2:** Run renewal generation twice and verify the same source/due date does not create duplicates.
- **TC-11.3.3:** Verify default and configured lead times produce correct reminder and due dates.
- **TC-11.3.4:** Verify generated renewal tasks link back to the originating profile, evidence, certification, or related source record.
#-----------------------------------

## 12. Evidence Vault

###  Please perform automated test on Story 12.1: Evidence Metadata. Please provide the results of the tests.
Using the existing GCCS architecture and test patterns, create or update automated tests for the following test case. Keep tenant isolation, server-side RBAC, audit logging, No-CUI controls, and standard error handling in scope where relevant. Run the narrowest relevant test command and report results.

- **TC-12.1.1:** Create evidence metadata with title, type, owner, approval status, expiration date, tags, description, and source links.
- **TC-12.1.2:** Link evidence to multiple obligations/controls and verify it is reusable from all linked views.
- **TC-12.1.3:** Add evidence tags and verify list/detail filtering by tags works without folder dependency.
- **TC-12.1.4:** Add an evidence expiration date and verify task generation plus metadata-change audit events.
#-----------------------------------

###  Please perform automated test on Story 12.2: Evidence File Upload. Please provide the results of the tests.
Using the existing GCCS architecture and test patterns, create or update automated tests for the following test case. Keep tenant isolation, server-side RBAC, audit logging, No-CUI controls, and standard error handling in scope where relevant. Run the narrowest relevant test command and report results.

- **TC-12.2.1:** Attempt evidence upload before No-CUI acknowledgement and verify it is blocked.
- **TC-12.2.2:** Upload an evidence file and verify it is not usable until validation and scan status allow it.
- **TC-12.2.3:** Upload a replacement evidence file and verify a new version is created without overwriting prior metadata.
- **TC-12.2.4:** Verify allowed users can download/delete evidence per RBAC and all download/delete actions are audit logged.
#-----------------------------------

###  Please perform automated test on Story 12.3: Evidence Approval. Please provide the results of the tests.
Using the existing GCCS architecture and test patterns, create or update automated tests for the following test case. Keep tenant isolation, server-side RBAC, audit logging, No-CUI controls, and standard error handling in scope where relevant. Run the narrowest relevant test command and report results.

- **TC-12.3.1:** Verify only configured roles can approve evidence.
- **TC-12.3.2:** Reject evidence without comment/reason and verify validation fails.
- **TC-12.3.3:** Approve evidence and verify it becomes eligible for report and evidence package inclusion.
- **TC-12.3.4:** Verify approve, reject, archive, and expire decisions are audit logged.
#-----------------------------------

## 13. CMMC Readiness Tracker

###  Please perform automated test on Story 13.1: CMMC Level Selection. Please provide the results of the tests.
Using the existing GCCS architecture and test patterns, create or update automated tests for the following test case. Keep tenant isolation, server-side RBAC, audit logging, No-CUI controls, and standard error handling in scope where relevant. Run the narrowest relevant test command and report results.

- **TC-13.1.1:** Create readiness assessments for CMMC Level 1 and Level 2 and verify target level, status, dates, and owner.
- **TC-13.1.2:** Link a readiness assessment to company profile and contracts and verify detail display.
- **TC-13.1.3:** Add control statuses and verify assessment completion progress recalculates.
- **TC-13.1.4:** Create, update, and change assessment status and verify audit events.
#-----------------------------------

###  Please perform automated test on Story 13.2: Control Readiness. Please provide the results of the tests.
Using the existing GCCS architecture and test patterns, create or update automated tests for the following test case. Keep tenant isolation, server-side RBAC, audit logging, No-CUI controls, and standard error handling in scope where relevant. Run the narrowest relevant test command and report results.

- **TC-13.2.1:** Verify Level 1 controls and Level 2 mappings load for the selected assessment scope.
- **TC-13.2.2:** Set control status to not_started, implemented, partially_implemented, not_applicable, and needs_review.
- **TC-13.2.3:** Link evidence, tasks, assets, and POA&M items to a control and verify they display on detail.
- **TC-13.2.4:** Verify the control baseline source is visible and control status contributes to assessment progress.
#-----------------------------------

###  Please perform automated test on Story 13.3: POA&M Items. Please provide the results of the tests.
Using the existing GCCS architecture and test patterns, create or update automated tests for the following test case. Keep tenant isolation, server-side RBAC, audit logging, No-CUI controls, and standard error handling in scope where relevant. Run the narrowest relevant test command and report results.

- **TC-13.3.1:** Create a POA&M item with control, gap, remediation plan, owner, due date, risk, and status.
- **TC-13.3.2:** Verify the POA&M-linked task is created or associated and appears on the calendar.
- **TC-13.3.3:** Seed open and overdue POA&M items and verify CMMC summary counts and flags.
- **TC-13.3.4:** Create, update, and change POA&M status and verify audit events.
#-----------------------------------

###  Please perform automated test on Story 13.4: Annual Affirmation Tracker. Please provide the results of the tests.
Using the existing GCCS architecture and test patterns, create or update automated tests for the following test case. Keep tenant isolation, server-side RBAC, audit logging, No-CUI controls, and standard error handling in scope where relevant. Run the narrowest relevant test command and report results.

- **TC-13.4.1:** Set an annual affirmation due date and verify it appears on the calendar.
- **TC-13.4.2:** Verify an upcoming annual affirmation creates a reminder task.
- **TC-13.4.3:** Link evidence to an affirmation record and verify it displays.
- **TC-13.4.4:** Update affirmation last/due dates and evidence links and verify audit events.
#-----------------------------------

## 14. Subcontractor Flow-Down Tracker

###  Please perform automated test on Story 14.1: Subcontractor Profile. Please provide the results of the tests.
Using the existing GCCS architecture and test patterns, create or update automated tests for the following test case. Keep tenant isolation, server-side RBAC, audit logging, No-CUI controls, and standard error handling in scope where relevant. Run the narrowest relevant test command and report results.

- **TC-14.1.1:** Create and update a subcontractor with legal name, POC, role, statuses, flags, dates, and workshare percentage.
- **TC-14.1.2:** Link a subcontractor to one or more contracts and verify list/detail display.
- **TC-14.1.3:** Verify CUI access and export-control flags are prominent but do not imply CUI storage.
- **TC-14.1.4:** Verify cross-tenant subcontractor access is denied and profile changes are audit logged.
#-----------------------------------

###  Please perform automated test on Story 14.2: Flow-Down Clause Tracking. Please provide the results of the tests.
Using the existing GCCS architecture and test patterns, create or update automated tests for the following test case. Keep tenant isolation, server-side RBAC, audit logging, No-CUI controls, and standard error handling in scope where relevant. Run the narrowest relevant test command and report results.

- **TC-14.2.1:** Assign required flow-down clauses from contract obligations to a subcontractor.
- **TC-14.2.2:** Update flow-down statuses required, sent, acknowledged, signed, waived, and not_applicable, then verify display by subcontractor and contract.
- **TC-14.2.3:** Link approved signed evidence to a flow-down record and verify it appears there.
- **TC-14.2.4:** Verify flow-down assignment and status changes are audit logged.
#-----------------------------------

###  Please perform automated test on Story 14.3: Subcontractor Evidence Requests. Please provide the results of the tests.
Using the existing GCCS architecture and test patterns, create or update automated tests for the following test case. Keep tenant isolation, server-side RBAC, audit logging, No-CUI controls, and standard error handling in scope where relevant. Run the narrowest relevant test command and report results.

- **TC-14.3.1:** Create a subcontractor evidence request with requested item, due date, status, recipient, and linked obligation.
- **TC-14.3.2:** Verify subcontractor evidence request due dates appear on the calendar.
- **TC-14.3.3:** Link received evidence to a request and verify request status/completion updates.
- **TC-14.3.4:** Seed an overdue subcontractor evidence request and verify list, calendar, and dashboard warnings.
#-----------------------------------

## 15. Reports

###  Please perform automated test on Story 15.1: Compliance Status Report. Please provide the results of the tests.
Using the existing GCCS architecture and test patterns, create or update automated tests for the following test case. Keep tenant isolation, server-side RBAC, audit logging, No-CUI controls, and standard error handling in scope where relevant. Run the narrowest relevant test command and report results.

- **TC-15.1.1:** Generate a compliance status report and verify obligation status, overdue tasks, evidence status, CMMC progress, subcontractor gaps, and high-risk items.
- **TC-15.1.2:** Verify the compliance status report excludes other-tenant data.
- **TC-15.1.3:** Generate a report and verify generation timestamp and snapshot metadata are stored.
- **TC-15.1.4:** Generate a report and verify the action is audit logged.
#-----------------------------------

###  Please perform automated test on Story 15.2: Contract Obligation Matrix. Please provide the results of the tests.
Using the existing GCCS architecture and test patterns, create or update automated tests for the following test case. Keep tenant isolation, server-side RBAC, audit logging, No-CUI controls, and standard error handling in scope where relevant. Run the narrowest relevant test command and report results.

- **TC-15.2.1:** Generate a contract obligation matrix for one contract and verify clause, source, obligation, owner, status, risk, due date, evidence, and flow-down columns.
- **TC-15.2.2:** Verify contract obligation matrix rows include source links and last reviewed dates.
- **TC-15.2.3:** Verify obligations requiring flow-down are clearly identified in the matrix.
- **TC-15.2.4:** Export the contract obligation matrix and compare exported rows/fields to the on-screen data.
#-----------------------------------

###  Please perform automated test on Story 15.3: CMMC Readiness Report. Please provide the results of the tests.
Using the existing GCCS architecture and test patterns, create or update automated tests for the following test case. Keep tenant isolation, server-side RBAC, audit logging, No-CUI controls, and standard error handling in scope where relevant. Run the narrowest relevant test command and report results.

- **TC-15.3.1:** Generate a CMMC readiness report and verify control status rollups by family/category.
- **TC-15.3.2:** Verify open POA&M items, gaps, evidence links, and affirmation dates appear in the CMMC readiness report.
- **TC-15.3.3:** As a restricted user, verify inaccessible evidence links are omitted or blocked in the CMMC readiness report.
- **TC-15.3.4:** Verify CMMC readiness report access is role-protected and generated snapshots are retained.
#-----------------------------------

###  Please perform automated test on Story 15.4: Evidence Package. Please provide the results of the tests.
Using the existing GCCS architecture and test patterns, create or update automated tests for the following test case. Keep tenant isolation, server-side RBAC, audit logging, No-CUI controls, and standard error handling in scope where relevant. Run the narrowest relevant test command and report results.

- **TC-15.4.1:** Generate an evidence package scoped by selected obligations, contract, CMMC controls, or subcontractor.
- **TC-15.4.2:** Verify approved evidence is included by default and draft/rejected evidence is excluded unless explicitly allowed by an authorized user.
- **TC-15.4.3:** Verify the evidence package manifest includes title, evidence type, linked obligation/control, approval state, and timestamp.
- **TC-15.4.4:** Verify the evidence package view is read-only and package generation is audit logged.
#-----------------------------------

###  Please perform automated test on Story 15.5: Subcontractor Compliance Report. Please provide the results of the tests.
Using the existing GCCS architecture and test patterns, create or update automated tests for the following test case. Keep tenant isolation, server-side RBAC, audit logging, No-CUI controls, and standard error handling in scope where relevant. Run the narrowest relevant test command and report results.

- **TC-15.5.1:** Generate a subcontractor compliance report filtered by contract and verify subcontractor data matches scope.
- **TC-15.5.2:** Verify missing and overdue subcontractor evidence requests are highlighted.
- **TC-15.5.3:** Verify flow-down statuses appear by subcontractor and contract.
- **TC-15.5.4:** Export the subcontractor compliance report and verify no other-tenant data is included.
#-----------------------------------

## 16. Notifications

###  Please perform automated test on Story 16.1: Notification Preferences. Please provide the results of the tests.
Using the existing GCCS architecture and test patterns, create or update automated tests for the following test case. Keep tenant isolation, server-side RBAC, audit logging, No-CUI controls, and standard error handling in scope where relevant. Run the narrowest relevant test command and report results.

- **TC-16.1.1:** Create users by role and verify default notification preferences are assigned.
- **TC-16.1.2:** Update preferences for assignments, due soon, overdue, evidence requests, renewals, and CMMC affirmation.
- **TC-16.1.3:** For a multi-tenant user, verify tenant-specific notification preferences do not leak across tenants when applicable.
- **TC-16.1.4:** Change notification preferences and verify audit events.
#-----------------------------------

###  Please perform automated test on Story 16.2: Due-Date Reminders. Please provide the results of the tests.
Using the existing GCCS architecture and test patterns, create or update automated tests for the following test case. Keep tenant isolation, server-side RBAC, audit logging, No-CUI controls, and standard error handling in scope where relevant. Run the narrowest relevant test command and report results.

- **TC-16.2.1:** Run the due-date reminder job and verify tasks within configured lead time are selected.
- **TC-16.2.2:** Run the reminder job twice and verify the same reminder is not sent repeatedly for the same event.
- **TC-16.2.3:** Verify overdue reminders are categorized or sent separately from upcoming reminders.
- **TC-16.2.4:** Simulate notification/email placeholder failure and verify failure is logged without crashing unrelated deliveries.
#-----------------------------------

###  Please perform automated test on Story 16.3: Assignment Notifications. Please provide the results of the tests.
Using the existing GCCS architecture and test patterns, create or update automated tests for the following test case. Keep tenant isolation, server-side RBAC, audit logging, No-CUI controls, and standard error handling in scope where relevant. Run the narrowest relevant test command and report results.

- **TC-16.3.1:** Assign a task, obligation, POA&M item, or evidence request and verify the assigned user receives a notification.
- **TC-16.3.2:** Open an assignment notification and verify it navigates to the linked source record.
- **TC-16.3.3:** Mark a notification as read and verify state persists.
- **TC-16.3.4:** As an unauthorized user, open a notification link and verify access is denied.
#-----------------------------------

## 17. MVP Hardening And Release Readiness

###  Please perform automated test on Story 17.1: End-To-End Pilot Workflow. Please provide the results of the tests.
Using the existing GCCS architecture and test patterns, create or update automated tests for the following test case. Keep tenant isolation, server-side RBAC, audit logging, No-CUI controls, and standard error handling in scope where relevant. Run the narrowest relevant test command and report results.

- **TC-17.1.1:** With non-CUI data, execute the full pilot workflow: onboard tenant/users, create profile, contract, clauses, obligations, tasks, evidence, CMMC records, subcontractors, reports, and notifications.
- **TC-17.1.2:** Execute the pilot workflow with owner, admin, compliance manager, contributor, auditor, and advisor users and verify each role can only perform permitted actions.
- **TC-17.1.3:** Generate reports after the pilot workflow and verify they reflect the workflow data.
- **TC-17.1.4:** Verify automated regression coverage exists for the pilot workflow critical path.
#-----------------------------------

###  Please perform automated test on Story 17.2: Security And Tenant Isolation Verification. Please provide the results of the tests.
Using the existing GCCS architecture and test patterns, create or update automated tests for the following test case. Keep tenant isolation, server-side RBAC, audit logging, No-CUI controls, and standard error handling in scope where relevant. Run the narrowest relevant test command and report results.

- **TC-17.2.1:** Attempt cross-tenant access for every tenant-owned module and verify denial with no data leakage.
- **TC-17.2.2:** Call restricted endpoints directly for each role and verify server-side RBAC denial.
- **TC-17.2.3:** Verify repository/service tests cover tenant filters for tenant-owned queries.
- **TC-17.2.4:** Confirm tenant isolation, RBAC, and audit logging verification results are documented.
#-----------------------------------

###  Please perform automated test on Story 17.3: Staging Environment. Please provide the results of the tests.
Using the existing GCCS architecture and test patterns, create or update automated tests for the following test case. Keep tenant isolation, server-side RBAC, audit logging, No-CUI controls, and standard error handling in scope where relevant. Run the narrowest relevant test command and report results.

- **TC-17.3.1:** Trigger staging deployment and verify API, web, database, storage, cache, queue, secrets, and jobs provision/deploy.
- **TC-17.3.2:** Verify staging contains no production customer data or production secrets.
- **TC-17.3.3:** Verify staging health checks cover API, database, cache, storage, and jobs.
- **TC-17.3.4:** Run staging smoke tests after deployment and verify success/failure is visible in CI/CD.
#-----------------------------------

###  Please perform automated test on Story 17.4: Production Readiness Checklist. Please provide the results of the tests.
Using the existing GCCS architecture and test patterns, create or update automated tests for the following test case. Keep tenant isolation, server-side RBAC, audit logging, No-CUI controls, and standard error handling in scope where relevant. Run the narrowest relevant test command and report results.

- **TC-17.4.1:** Verify release cannot proceed until production readiness checklist items are complete and approved.
- **TC-17.4.2:** Confirm No-CUI limits, malware scanning limitation/path, support path, and prohibited upload guidance are documented.
- **TC-17.4.3:** Verify launch obligations have source URLs, last reviewed dates, confidence, and review metadata.
- **TC-17.4.4:** Execute or simulate staging rollback and verify steps, timing, and outcome are documented.

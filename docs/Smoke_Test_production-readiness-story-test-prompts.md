# Production Readiness Smoke Test Prompts

Use these prompts to perform smoke verification for each story in [production-readiness-story-test-cases.md](production-readiness-story-test-cases.md). These prompts are optimized for quick launch-readiness checks, not exhaustive regression testing.

Recommended smoke-test instruction:

```text
Please perform a smoke test on the referenced Production Readiness story. Use synthetic or non-sensitive data only. Capture setup data, exact steps, expected result, actual result, evidence location, defects, missing coverage, and blocker/risk disposition. Do not mark the smoke test passed if required evidence, approval, blocker disposition, or No-CUI control evidence is missing.
```

Common smoke-test rules:

- Treat the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.
- Do not use real customer CUI, production secrets, production logs, or customer-identifying data.
- Verify the fastest observable evidence path first: document review, grep check, launch package evidence, approval record, smoke command, health check, API check, or UI check.
- Any failed, skipped, or unavailable check must be reported as blocker, accepted risk, deferred follow-up, or not applicable with owner and mitigation.

## 1. PR-0 - Launch Posture Freeze

### Story PR-0.1: Record And Approve Launch Posture

Please perform a smoke test on Story PR-0.1: Record And Approve Launch Posture. Please provide the results of the tests.

- **TC-PR-0.1.1:** Verify `docs/production-readiness-plan.md` names No-CUI / compliance management only as the only approved MVP production posture.
- **TC-PR-0.1.2:** Verify the decision prohibits real customer CUI until a separately approved future `CuiReady` posture exists.
- **TC-PR-0.1.3:** Verify product, engineering, security, compliance content, support, and legal/contracting approvers are named or explicitly marked pending.
- **TC-PR-0.1.4:** Verify any pending approval appears in launch blockers, readiness checklist, or launch package status.

#-----------------------------------

### Story PR-0.2: Align Launch Documents To No-CUI Posture

Please perform a smoke test on Story PR-0.2: Align Launch Documents To No-CUI Posture. Please provide the results of the tests.

- **TC-PR-0.2.1:** Search referenced launch documents and verify none describe the MVP as production CUI-capable.
- **TC-PR-0.2.2:** Verify `CuiReady` references describe future, excluded, or separately approved capability only.
- **TC-PR-0.2.3:** Search for certification, official approval, legal advice, government endorsement, CMMC pass/fail, and real CUI upload claims; verify none appear in customer-facing launch text.
- **TC-PR-0.2.4:** Verify unresolved posture conflicts have owner, severity, mitigation, and launch disposition.

#-----------------------------------

### Story PR-0.3: Verify Tenant Mode Boundary Design

Please perform a smoke test on Story PR-0.3: Verify Tenant Mode Boundary Design. Please provide the results of the tests.

- **TC-PR-0.3.1:** Verify `DemoSandbox`, `NoCui`, and future `CuiReady` modes have documented allowed and prohibited behavior.
- **TC-PR-0.3.2:** Execute or inspect upload/import/extraction/report paths to verify real CUI blocking does not depend only on UI copy.
- **TC-PR-0.3.3:** Verify demo workflows allow only synthetic or redacted demonstration data and do not imply customer CUI authorization.
- **TC-PR-0.3.4:** Verify tests or launch blockers cover allowed synthetic demo data, blocked real CUI, and future `CuiReady` exclusion.

#-----------------------------------

## 2. PR-1 - Backlog Readiness Gate

### Story PR-1.1: Re-Run Definition Of Ready For Open MVP Stories

Please perform a smoke test on Story PR-1.1: Re-Run Definition Of Ready For Open MVP Stories. Please provide the results of the tests.

- **TC-PR-1.1.1:** Verify every open MVP launch story is listed in a readiness review artifact.
- **TC-PR-1.1.2:** Verify actor, goal, business value, included scope, excluded scope, acceptance criteria, dependencies, data needs, security, RBAC, audit logging, and CUI/data-handling implications are reviewed.
- **TC-PR-1.1.3:** Verify stories missing required readiness fields are not accepted into launch scope.
- **TC-PR-1.1.4:** Verify no story with unresolved No-CUI or tenant-mode ambiguity remains in launch scope.

#-----------------------------------

### Story PR-1.2: Map Open Stories To Test Cases

Please perform a smoke test on Story PR-1.2: Map Open Stories To Test Cases. Please provide the results of the tests.

- **TC-PR-1.2.1:** Verify each open launch story references applicable `TC-*` cases or documents why no test case applies.
- **TC-PR-1.2.2:** Verify gaps in unit, integration, API, frontend, staging, tenant isolation, RBAC, upload, report, or audit coverage become launch tasks or blockers.
- **TC-PR-1.2.3:** Verify stories affecting uploads, reports, evidence, imports, exports, search, AI, extraction, or background jobs include tenant-mode coverage.
- **TC-PR-1.2.4:** Verify any story expanding data posture beyond No-CUI is rejected unless a separate approval gate exists.

#-----------------------------------

### Story PR-1.3: Gate Risky Workflow Changes

Please perform a smoke test on Story PR-1.3: Gate Risky Workflow Changes. Please provide the results of the tests.

- **TC-PR-1.3.1:** Verify all stories changing upload, import, export, search, AI, evidence, report, extraction, or background processing are listed.
- **TC-PR-1.3.2:** Verify each risky story includes tenant-mode, RBAC, audit logging, and tenant isolation coverage.
- **TC-PR-1.3.3:** Verify stories without sufficient controls are deferred, blocked, or narrowed.
- **TC-PR-1.3.4:** Verify no unreviewed data ingress, data egress, or automated processing story remains in launch scope.

#-----------------------------------

## 3. PR-2 - Completion And Scope Freeze

### Story PR-2.1: Freeze MVP Launch Scope

Please perform a smoke test on Story PR-2.1: Freeze MVP Launch Scope. Please provide the results of the tests.

- **TC-PR-2.1.1:** Verify launch-critical MVP modules and stories are listed as the frozen scope.
- **TC-PR-2.1.2:** Verify Phase 2 or later work is excluded unless it has launch-blocking evidence.
- **TC-PR-2.1.3:** Verify scope exclusions and known limitations appear in release notes, launch package, or readiness artifacts.
- **TC-PR-2.1.4:** Verify any launch scope addition requires product owner and engineering lead approval.

#-----------------------------------

### Story PR-2.2: Re-Run Definition Of Done For Completed MVP Stories

Please perform a smoke test on Story PR-2.2: Re-Run Definition Of Done For Completed MVP Stories. Please provide the results of the tests.

- **TC-PR-2.2.1:** Verify each completed launch story has evidence that acceptance criteria passed.
- **TC-PR-2.2.2:** Verify workflows with protected tenant data have tenant isolation and RBAC review evidence.
- **TC-PR-2.2.3:** Verify sensitive actions have audit logging evidence or a documented exception.
- **TC-PR-2.2.4:** Verify applicable UI stories include validation failure, permission denial, empty state, error state, and basic accessibility evidence.

#-----------------------------------

### Story PR-2.3: Convert Completion Gaps Into Launch Decisions

Please perform a smoke test on Story PR-2.3: Convert Completion Gaps Into Launch Decisions. Please provide the results of the tests.

- **TC-PR-2.3.1:** Verify failed, partial, skipped, or untested Definition of Done items are listed.
- **TC-PR-2.3.2:** Verify each gap is classified as blocker, accepted risk, deferred follow-up, or not applicable.
- **TC-PR-2.3.3:** Verify accepted risks include owner, severity, mitigation, contingency, approver, target date, and status.
- **TC-PR-2.3.4:** Verify deferred items do not undermine No-CUI posture, tenant isolation, RBAC, audit logging, support readiness, or customer claims.

#-----------------------------------

## 4. PR-3 - Staging Verification

### Story PR-3.1: Deploy And Smoke Test Staging

Please perform a smoke test on Story PR-3.1: Deploy And Smoke Test Staging. Please provide the results of the tests.

- **TC-PR-3.1.1:** Verify staging deployment evidence references the approved pipeline, artifact, environment, date, and result.
- **TC-PR-3.1.2:** Verify staging has no production customer data, real CUI, production secrets, production uploads, or production logs.
- **TC-PR-3.1.3:** Verify `/health` reports API, database, cache, storage, background job, and expected dependency signals.
- **TC-PR-3.1.4:** Verify staging health output includes `dataPosture = No-CUI / compliance management only`.

#-----------------------------------

### Story PR-3.2: Execute End-To-End MVP Workflow In Staging

Please perform a smoke test on Story PR-3.2: Execute End-To-End MVP Workflow In Staging. Please provide the results of the tests.

- **TC-PR-3.2.1:** Execute tenant creation, invite, role assignment, profile, contract, clause tagging, obligation generation, task creation, evidence upload, report generation, and audit export using synthetic data.
- **TC-PR-3.2.2:** Verify allowed non-sensitive upload succeeds after acknowledgement and produces expected metadata.
- **TC-PR-3.2.3:** Verify blocked upload produces no usable evidence and creates an audit event.
- **TC-PR-3.2.4:** Verify screenshots, logs, test output, or run records are attached to the launch package.

#-----------------------------------

### Story PR-3.3: Verify Tenant Isolation And RBAC In Staging

Please perform a smoke test on Story PR-3.3: Verify Tenant Isolation And RBAC In Staging. Please provide the results of the tests.

- **TC-PR-3.3.1:** Attempt cross-tenant access for contracts, evidence, tasks, reports, exports, and audit logs; verify denial and no data leakage.
- **TC-PR-3.3.2:** Attempt cross-tenant update/delete actions and verify denial and no mutation.
- **TC-PR-3.3.3:** Test owner, admin, compliance manager, contributor, auditor, and advisor direct API calls against allowed and restricted actions.
- **TC-PR-3.3.4:** Verify tenant isolation and RBAC test output is attached to the launch package.

#-----------------------------------

### Story PR-3.4: Verify Upload Guardrails And Report Controls In Staging

Please perform a smoke test on Story PR-3.4: Verify Upload Guardrails And Report Controls In Staging. Please provide the results of the tests.

- **TC-PR-3.4.1:** Verify upload workflows show No-CUI warnings and require acknowledgement before upload.
- **TC-PR-3.4.2:** Verify real CUI, prohibited content, oversized files, and disallowed file types are blocked and audit logged.
- **TC-PR-3.4.3:** Verify reports include tenant scope, RBAC enforcement, source links, last-reviewed dates, and draft-only CMMC language where applicable.
- **TC-PR-3.4.4:** Verify reports do not include pass/fail, certification, official approval, legal advice, government endorsement, or CUI-storage permission claims.

#-----------------------------------

## 5. PR-4 - Launch Evidence Package

### Story PR-4.1: Attach Backup And Restore Evidence

Please perform a smoke test on Story PR-4.1: Attach Backup And Restore Evidence. Please provide the results of the tests.

- **TC-PR-4.1.1:** Verify the launch package includes staging backup evidence for the launch candidate.
- **TC-PR-4.1.2:** Verify restore evidence shows a successful restore from backup, not just backup creation.
- **TC-PR-4.1.3:** Verify restore evidence includes date, environment, data set, command or pipeline reference, result, and reviewer.
- **TC-PR-4.1.4:** If restore evidence is absent, verify production launch remains blocked.

#-----------------------------------

### Story PR-4.2: Attach Deployment, Migration, And Rollback Evidence

Please perform a smoke test on Story PR-4.2: Attach Deployment, Migration, And Rollback Evidence. Please provide the results of the tests.

- **TC-PR-4.2.1:** Verify staging workflow and smoke results are included in the launch package.
- **TC-PR-4.2.2:** Verify migration evidence identifies scripts, environment, result, reviewer, and failure handling.
- **TC-PR-4.2.3:** Verify rollback simulation notes and migration rollback notes are attached.
- **TC-PR-4.2.4:** Verify any irreversible migration has owner, mitigation, contingency, and approver acceptance.

#-----------------------------------

### Story PR-4.3: Decide Malware Scanning Launch Path

Please perform a smoke test on Story PR-4.3: Decide Malware Scanning Launch Path. Please provide the results of the tests.

- **TC-PR-4.3.1:** If malware scanning is enabled, verify scanner configuration and upload scan test evidence are attached.
- **TC-PR-4.3.2:** If scanning is not enabled, verify a launch exception includes compensating controls, affected workflows, owner, expiration date, and approvers.
- **TC-PR-4.3.3:** Verify the malware scanning path appears in the known-risk acceptance log.
- **TC-PR-4.3.4:** Verify upload launch readiness remains blocked when neither scanner evidence nor exception approval exists.

#-----------------------------------

## 6. PR-5 - Content, Claims, And Support Readiness

### Story PR-5.1: Review High-Risk Obligation Content

Please perform a smoke test on Story PR-5.1: Review High-Risk Obligation Content. Please provide the results of the tests.

- **TC-PR-5.1.1:** Verify high-risk obligation records are listed for review.
- **TC-PR-5.1.2:** Verify published obligations include source URL, trigger condition, required actions, evidence examples, confidence, review owner, review state, and last-reviewed date.
- **TC-PR-5.1.3:** Verify high-risk records missing required metadata or expert review are hidden, retired, or blocked from customer-facing production views.
- **TC-PR-5.1.4:** Verify approval, hiding, retirement, or blocker decisions include owner, date, and rationale.

#-----------------------------------

### Story PR-5.2: Review Customer-Facing Claims

Please perform a smoke test on Story PR-5.2: Review Customer-Facing Claims. Please provide the results of the tests.

- **TC-PR-5.2.1:** Search product copy, onboarding, uploads, reports, support materials, and release notes for legal advice, certification, official approval, government endorsement, and CMMC pass/fail claims.
- **TC-PR-5.2.2:** Verify onboarding, upload flows, support materials, and release notes state No-CUI launch limits and prohibited data examples.
- **TC-PR-5.2.3:** Verify CMMC guidance or readiness outputs use draft-only or review-required language unless expert-reviewed.
- **TC-PR-5.2.4:** Verify customer-facing claim review status is recorded before launch candidate approval.

#-----------------------------------

### Story PR-5.3: Finalize Support Runbooks

Please perform a smoke test on Story PR-5.3: Finalize Support Runbooks. Please provide the results of the tests.

- **TC-PR-5.3.1:** Verify runbooks exist for prohibited upload, suspected CUI, tenant exposure, access issue, evidence failure, report failure, content correction, security incident, backup restore, and rollback.
- **TC-PR-5.3.2:** Verify each runbook includes owner, triage steps, escalation path, severity guidance, and evidence to capture.
- **TC-PR-5.3.3:** Verify prohibited upload and suspected CUI runbooks require containment, escalation, and No-CUI posture preservation.
- **TC-PR-5.3.4:** Verify support routing appears in release notes, launch package, or pilot onboarding materials.

#-----------------------------------

### Story PR-5.4: Prepare Pilot Onboarding, Release Notes, And Known-Risk Log

Please perform a smoke test on Story PR-5.4: Prepare Pilot Onboarding, Release Notes, And Known-Risk Log. Please provide the results of the tests.

- **TC-PR-5.4.1:** Verify pilot onboarding states No-CUI limits, prohibited data examples, support paths, known limitations, and synthetic demo explanation.
- **TC-PR-5.4.2:** Verify release notes include posture, scope, exclusions, known risks, support paths, staging smoke results, rollback plan, and content scope.
- **TC-PR-5.4.3:** Verify known risks include owner, mitigation, contingency, target date, current status, and approver.
- **TC-PR-5.4.4:** Verify support, onboarding, release notes, and known-risk artifacts have required review status before launch approval.

#-----------------------------------

## 7. PR-6 - Approval And Launch Candidate

### Story PR-6.1: Collect Required Launch Approvals

Please perform a smoke test on Story PR-6.1: Collect Required Launch Approvals. Please provide the results of the tests.

- **TC-PR-6.1.1:** Verify product owner approval includes date, scope, limitations, unresolved exceptions, and evidence reviewed.
- **TC-PR-6.1.2:** Verify engineering lead and security owner approvals include scope, limitations, unresolved exceptions, and evidence reviewed.
- **TC-PR-6.1.3:** Verify compliance content, customer success/support, and legal/contracting approvals are present with scope and limitations.
- **TC-PR-6.1.4:** Verify launch candidate tagging is blocked when any required approval is missing.

#-----------------------------------

### Story PR-6.2: Tag Launch Candidate With Evidence Links

Please perform a smoke test on Story PR-6.2: Tag Launch Candidate With Evidence Links. Please provide the results of the tests.

- **TC-PR-6.2.1:** Verify evidence package and required approvals are complete before launch candidate tagging.
- **TC-PR-6.2.2:** Verify the tag record includes commit, build artifact, deployment artifact, and evidence package location.
- **TC-PR-6.2.3:** Verify release notes, known limitations, support paths, staging evidence, rollback plan, and content scope are linked.
- **TC-PR-6.2.4:** Attempt or simulate tag readiness with missing evidence and verify tag creation is blocked.

#-----------------------------------

## 8. PR-7 - Production Deployment And Pilot Launch

### Story PR-7.1: Deploy Production Through Approved CI/CD

Please perform a smoke test on Story PR-7.1: Deploy Production Through Approved CI/CD. Please provide the results of the tests.

- **TC-PR-7.1.1:** Verify production deployment references the approved launch candidate artifact.
- **TC-PR-7.1.2:** Verify deployment runs through approved CI/CD, not manual ad hoc commands.
- **TC-PR-7.1.3:** Verify secrets source, migrations, storage, cache, background jobs, health checks, logs, alerts, and No-CUI data posture are checked.
- **TC-PR-7.1.4:** Verify deployment time, artifact, operator, environment, result, and evidence location are recorded.

#-----------------------------------

### Story PR-7.2: Run Production Smoke Tests

Please perform a smoke test on Story PR-7.2: Run Production Smoke Tests. Please provide the results of the tests.

- **TC-PR-7.2.1:** Verify login, tenant access, and RBAC denial tests pass in production.
- **TC-PR-7.2.2:** Verify upload warning/blocking, evidence upload, report generation, and audit logging smoke tests pass using synthetic or non-sensitive data.
- **TC-PR-7.2.3:** Verify logs, alerts, and health checks are active and observed after deployment.
- **TC-PR-7.2.4:** Verify pilot onboarding does not proceed when a critical smoke test fails.

#-----------------------------------

### Story PR-7.3: Onboard Controlled Pilot Customers

Please perform a smoke test on Story PR-7.3: Onboard Controlled Pilot Customers. Please provide the results of the tests.

- **TC-PR-7.3.1:** Verify pilot onboarding begins only after production smoke tests pass.
- **TC-PR-7.3.2:** Verify each pilot customer receives No-CUI data handling guidance, prohibited data examples, support paths, and known limitations.
- **TC-PR-7.3.3:** Verify each pilot tenant has correct tenant mode, user roles, and support routing.
- **TC-PR-7.3.4:** Verify first workflow completion or first-use monitoring is recorded for each pilot tenant using non-sensitive identifiers.

#-----------------------------------

## 9. PR-8 - Post-Launch Control

### Story PR-8.1: Monitor Pilot Signals Daily

Please perform a smoke test on Story PR-8.1: Monitor Pilot Signals Daily. Please provide the results of the tests.

- **TC-PR-8.1.1:** Verify audit logs, upload blocks, permission denials, report failures, support tickets, content disputes, health checks, alerts, and failed jobs are reviewed daily during pilot.
- **TC-PR-8.1.2:** Verify each finding has severity, owner, mitigation, and target date.
- **TC-PR-8.1.3:** Verify security, tenant isolation, data-handling, suspected CUI, and overclaim issues are escalated according to runbooks.
- **TC-PR-8.1.4:** Verify production readiness regressions appear in the risk register, known-risk log, or backlog tracker.

#-----------------------------------

### Story PR-8.2: Hold Post-Launch Readiness Review

Please perform a smoke test on Story PR-8.2: Hold Post-Launch Readiness Review. Please provide the results of the tests.

- **TC-PR-8.2.1:** Verify the post-launch readiness review has date, participants, agenda, findings, and decisions.
- **TC-PR-8.2.2:** Verify incidents, defects, tickets, upload blocks, permission denials, content disputes, report failures, and feedback are reviewed.
- **TC-PR-8.2.3:** Verify production readiness regressions have owner, severity, mitigation, and due date.
- **TC-PR-8.2.4:** Verify release notes, support materials, known-risk log, readiness checklist, or decision log are updated for material findings.

#-----------------------------------

### Story PR-8.3: Gate Phase 2 Until MVP Controls Are Stable

Please perform a smoke test on Story PR-8.3: Gate Phase 2 Until MVP Controls Are Stable. Please provide the results of the tests.

- **TC-PR-8.3.1:** Verify launch findings are converted into backlog items that satisfy Definition of Ready.
- **TC-PR-8.3.2:** Verify Phase 2 remains blocked while tenant isolation, RBAC, uploads, reports, audit logging, support, content governance, claims, or No-CUI posture are unstable.
- **TC-PR-8.3.3:** Verify Phase 2 gate criteria identify required evidence, owner, approvers, and pass/fail status.
- **TC-PR-8.3.4:** Verify Phase 2 gate status is recorded before Govcon Intelligence work proceeds.

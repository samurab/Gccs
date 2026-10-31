# Production Readiness Story Test Cases

These test cases cover every production readiness user story in [production-readiness-phase-use-cases.md](production-readiness-phase-use-cases.md). They are intended for documentation review, launch package verification, grep-based checks, backend/API tests, frontend tests, staging smoke tests, production smoke tests, and manual approval evidence where automation is not appropriate.

Common expectations for all production readiness stories:

- The MVP launch posture remains **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.
- Real customer CUI remains prohibited unless a future `CuiReady` posture is separately approved.
- Readiness claims must be backed by artifacts, test results, approvals, owner assignments, blocker records, or known-risk entries.
- Tenant isolation, RBAC, audit logging, source traceability, review metadata, upload controls, report controls, and customer-claim controls must remain intact.
- Any failed, skipped, missing, or manual-only verification must have a disposition: blocker, accepted risk, deferred follow-up, or not applicable.

## 1. PR-0 - Launch Posture Freeze

### Story PR-0.1: Record And Approve Launch Posture

- **TC-PR-0.1.1 - No-CUI decision is explicit:** Verify `docs/production-readiness-plan.md` names No-CUI / compliance management only as the only approved MVP production posture.
- **TC-PR-0.1.2 - Real CUI exclusion is explicit:** Verify the decision prohibits real customer CUI until a separately approved future `CuiReady` posture exists.
- **TC-PR-0.1.3 - Approver list is complete:** Verify product, engineering, security, compliance content, support, and legal/contracting approvers are named or explicitly marked pending.
- **TC-PR-0.1.4 - Missing approvals block launch:** Verify any pending approval appears in launch blockers, readiness checklist, or launch package status.

### Story PR-0.2: Align Launch Documents To No-CUI Posture

- **TC-PR-0.2.1 - Referenced documents avoid production CUI claims:** Search referenced launch documents and verify none describe the MVP as production CUI-capable.
- **TC-PR-0.2.2 - Future CuiReady is separately gated:** Verify `CuiReady` references describe future, excluded, or separately approved capability only.
- **TC-PR-0.2.3 - Risky claim language is controlled:** Search for certification, official approval, legal advice, government endorsement, CMMC pass/fail, and real CUI upload claims; verify none appear in customer-facing launch text.
- **TC-PR-0.2.4 - Conflicts have disposition:** Verify unresolved posture conflicts have owner, severity, mitigation, and launch disposition.

### Story PR-0.3: Verify Tenant Mode Boundary Design

- **TC-PR-0.3.1 - Tenant modes are documented:** Verify `DemoSandbox`, `NoCui`, and future `CuiReady` modes have documented allowed and prohibited behavior.
- **TC-PR-0.3.2 - NoCui real CUI blocking is server-side:** Execute or inspect upload/import/extraction/report paths to verify real CUI blocking does not depend only on UI copy.
- **TC-PR-0.3.3 - DemoSandbox is synthetic-only:** Verify demo workflows allow only synthetic or redacted demonstration data and do not imply customer CUI authorization.
- **TC-PR-0.3.4 - Tenant mode tests exist:** Verify tests or launch blockers cover allowed synthetic demo data, blocked real CUI, and future `CuiReady` exclusion.

## 2. PR-1 - Backlog Readiness Gate

### Story PR-1.1: Re-Run Definition Of Ready For Open MVP Stories

- **TC-PR-1.1.1 - Open launch stories inventoried:** Verify every open MVP launch story is listed in a readiness review artifact.
- **TC-PR-1.1.2 - Required readiness fields checked:** Verify actor, goal, business value, included scope, excluded scope, acceptance criteria, dependencies, data needs, security, RBAC, audit logging, and CUI/data-handling implications are reviewed.
- **TC-PR-1.1.3 - Incomplete stories rejected or deferred:** Verify stories missing required readiness fields are not accepted into launch scope.
- **TC-PR-1.1.4 - Data-handling ambiguity is blocked:** Verify no story with unresolved No-CUI or tenant-mode ambiguity remains in launch scope.

### Story PR-1.2: Map Open Stories To Test Cases

- **TC-PR-1.2.1 - Every open story has test mapping:** Verify each open launch story references applicable `TC-*` cases or documents why no test case applies.
- **TC-PR-1.2.2 - Missing coverage becomes work:** Verify gaps in unit, integration, API, frontend, staging, tenant isolation, RBAC, upload, report, or audit coverage become launch tasks or blockers.
- **TC-PR-1.2.3 - Risky workflows include tenant-mode tests:** Verify stories affecting uploads, reports, evidence, imports, exports, search, AI, extraction, or background jobs include tenant-mode coverage.
- **TC-PR-1.2.4 - No-CUI expansion is rejected:** Verify any story expanding data posture beyond No-CUI is rejected unless a separate approval gate exists.

### Story PR-1.3: Gate Risky Workflow Changes

- **TC-PR-1.3.1 - Risky workflow stories identified:** Verify all stories changing upload, import, export, search, AI, evidence, report, extraction, or background processing are listed.
- **TC-PR-1.3.2 - Guardrail coverage verified:** Verify each risky story includes tenant-mode, RBAC, audit logging, and tenant isolation coverage.
- **TC-PR-1.3.3 - Unguarded stories are removed from launch scope:** Verify stories without sufficient controls are deferred, blocked, or narrowed.
- **TC-PR-1.3.4 - Data ingress and egress are reviewed:** Verify no unreviewed data ingress, data egress, or automated processing story remains in launch scope.

## 3. PR-2 - Completion And Scope Freeze

### Story PR-2.1: Freeze MVP Launch Scope

- **TC-PR-2.1.1 - Frozen launch scope is documented:** Verify launch-critical MVP modules and stories are listed as the frozen scope.
- **TC-PR-2.1.2 - Phase 2+ work is deferred:** Verify Phase 2 or later work is excluded unless it has launch-blocking evidence.
- **TC-PR-2.1.3 - Known limitations are visible:** Verify scope exclusions and known limitations appear in release notes, launch package, or readiness artifacts.
- **TC-PR-2.1.4 - New scope requires approval:** Verify any launch scope addition requires product owner and engineering lead approval.

### Story PR-2.2: Re-Run Definition Of Done For Completed MVP Stories

- **TC-PR-2.2.1 - Completed stories have acceptance evidence:** Verify each completed launch story has evidence that acceptance criteria passed.
- **TC-PR-2.2.2 - Protected workflows have control review:** Verify workflows with protected tenant data have tenant isolation and RBAC review evidence.
- **TC-PR-2.2.3 - Sensitive actions have audit evidence:** Verify sensitive actions have audit logging evidence or a documented exception.
- **TC-PR-2.2.4 - UI completion checks exist:** Verify applicable UI stories include validation failure, permission denial, empty state, error state, and basic accessibility evidence.

### Story PR-2.3: Convert Completion Gaps Into Launch Decisions

- **TC-PR-2.3.1 - Completion gaps are inventoried:** Verify failed, partial, skipped, or untested Definition of Done items are listed.
- **TC-PR-2.3.2 - Every gap has disposition:** Verify each gap is classified as blocker, accepted risk, deferred follow-up, or not applicable.
- **TC-PR-2.3.3 - Accepted risks are complete:** Verify accepted risks include owner, severity, mitigation, contingency, approver, target date, and status.
- **TC-PR-2.3.4 - Deferred gaps do not contradict launch posture:** Verify deferred items do not undermine No-CUI posture, tenant isolation, RBAC, audit logging, support readiness, or customer claims.

## 4. PR-3 - Staging Verification

### Story PR-3.1: Deploy And Smoke Test Staging

- **TC-PR-3.1.1 - Staging deploy uses approved CI/CD:** Verify staging deployment evidence references the approved pipeline, artifact, environment, date, and result.
- **TC-PR-3.1.2 - Staging contains synthetic-only data:** Verify staging has no production customer data, real CUI, production secrets, production uploads, or production logs.
- **TC-PR-3.1.3 - Health endpoint reports dependencies:** Verify `/health` reports API, database, cache, storage, background job, and expected dependency signals.
- **TC-PR-3.1.4 - Health endpoint reports data posture:** Verify staging health output includes `dataPosture = No-CUI / compliance management only`.

### Story PR-3.2: Execute End-To-End MVP Workflow In Staging

- **TC-PR-3.2.1 - Core staging workflow completes:** Execute tenant creation, invite, role assignment, profile, contract, clause tagging, obligation generation, task creation, evidence upload, report generation, and audit export using synthetic data.
- **TC-PR-3.2.2 - Allowed upload succeeds under No-CUI:** Verify allowed non-sensitive upload succeeds after acknowledgement and produces expected metadata.
- **TC-PR-3.2.3 - Real CUI or prohibited upload is blocked:** Verify blocked upload produces no usable evidence and creates an audit event.
- **TC-PR-3.2.4 - Workflow evidence is attached:** Verify screenshots, logs, test output, or run records are attached to the launch package.

### Story PR-3.3: Verify Tenant Isolation And RBAC In Staging

- **TC-PR-3.3.1 - Cross-tenant reads are denied:** Attempt cross-tenant access for contracts, evidence, tasks, reports, exports, and audit logs; verify denial and no data leakage.
- **TC-PR-3.3.2 - Cross-tenant writes are denied:** Attempt cross-tenant update/delete actions and verify denial and no mutation.
- **TC-PR-3.3.3 - Role matrix is enforced server-side:** Test owner, admin, compliance manager, contributor, auditor, and advisor direct API calls against allowed and restricted actions.
- **TC-PR-3.3.4 - Authorization evidence is attached:** Verify tenant isolation and RBAC test output is attached to the launch package.

### Story PR-3.4: Verify Upload Guardrails And Report Controls In Staging

- **TC-PR-3.4.1 - Upload acknowledgement and warnings work:** Verify upload workflows show No-CUI warnings and require acknowledgement before upload.
- **TC-PR-3.4.2 - Upload validation blocks risky files:** Verify real CUI, prohibited content, oversized files, and disallowed file types are blocked and audit logged.
- **TC-PR-3.4.3 - Reports preserve source-backed limits:** Verify reports include tenant scope, RBAC enforcement, source links, last-reviewed dates, and draft-only CMMC language where applicable.
- **TC-PR-3.4.4 - Reports avoid prohibited claims:** Verify reports do not include pass/fail, certification, official approval, legal advice, government endorsement, or CUI-storage permission claims.

## 5. PR-4 - Launch Evidence Package

### Story PR-4.1: Attach Backup And Restore Evidence

- **TC-PR-4.1.1 - Backup evidence exists:** Verify the launch package includes staging backup evidence for the launch candidate.
- **TC-PR-4.1.2 - Restore evidence proves recovery:** Verify restore evidence shows a successful restore from backup, not just backup creation.
- **TC-PR-4.1.3 - Restore metadata is complete:** Verify restore evidence includes date, environment, data set, command or pipeline reference, result, and reviewer.
- **TC-PR-4.1.4 - Missing restore remains blocker:** If restore evidence is absent, verify production launch remains blocked.

### Story PR-4.2: Attach Deployment, Migration, And Rollback Evidence

- **TC-PR-4.2.1 - Staging evidence is attached:** Verify staging workflow and smoke results are included in the launch package.
- **TC-PR-4.2.2 - Migration evidence is complete:** Verify migration evidence identifies scripts, environment, result, reviewer, and failure handling.
- **TC-PR-4.2.3 - Rollback evidence exists:** Verify rollback simulation notes and migration rollback notes are attached.
- **TC-PR-4.2.4 - Irreversible migration risk is accepted:** Verify any irreversible migration has owner, mitigation, contingency, and approver acceptance.

### Story PR-4.3: Decide Malware Scanning Launch Path

- **TC-PR-4.3.1 - Scanner-enabled path has evidence:** If malware scanning is enabled, verify scanner configuration and upload scan test evidence are attached.
- **TC-PR-4.3.2 - Exception path is complete:** If scanning is not enabled, verify a launch exception includes compensating controls, affected workflows, owner, expiration date, and approvers.
- **TC-PR-4.3.3 - Known-risk log records decision:** Verify the malware scanning path appears in the known-risk acceptance log.
- **TC-PR-4.3.4 - Upload launch readiness blocks on missing decision:** Verify upload launch readiness remains blocked when neither scanner evidence nor exception approval exists.

## 6. PR-5 - Content, Claims, And Support Readiness

### Story PR-5.1: Review High-Risk Obligation Content

- **TC-PR-5.1.1 - High-risk obligations inventoried:** Verify high-risk obligation records are listed for review.
- **TC-PR-5.1.2 - Published obligations have required metadata:** Verify published obligations include source URL, trigger condition, required actions, evidence examples, confidence, review owner, review state, and last-reviewed date.
- **TC-PR-5.1.3 - Incomplete high-risk content is hidden:** Verify high-risk records missing required metadata or expert review are hidden, retired, or blocked from customer-facing production views.
- **TC-PR-5.1.4 - Review decisions are recorded:** Verify approval, hiding, retirement, or blocker decisions include owner, date, and rationale.

### Story PR-5.2: Review Customer-Facing Claims

- **TC-PR-5.2.1 - Legal/certification claims are absent:** Search product copy, onboarding, uploads, reports, support materials, and release notes for legal advice, certification, official approval, government endorsement, and CMMC pass/fail claims.
- **TC-PR-5.2.2 - No-CUI limits are visible:** Verify onboarding, upload flows, support materials, and release notes state No-CUI launch limits and prohibited data examples.
- **TC-PR-5.2.3 - CMMC outputs are draft-only where required:** Verify CMMC guidance or readiness outputs use draft-only or review-required language unless expert-reviewed.
- **TC-PR-5.2.4 - Legal/contracting review is recorded:** Verify customer-facing claim review status is recorded before launch candidate approval.

### Story PR-5.3: Finalize Support Runbooks

- **TC-PR-5.3.1 - Required runbooks exist:** Verify runbooks exist for prohibited upload, suspected CUI, tenant exposure, access issue, evidence failure, report failure, content correction, security incident, backup restore, and rollback.
- **TC-PR-5.3.2 - Runbooks have operational fields:** Verify each runbook includes owner, triage steps, escalation path, severity guidance, and evidence to capture.
- **TC-PR-5.3.3 - CUI runbooks require containment:** Verify prohibited upload and suspected CUI runbooks require containment, escalation, and No-CUI posture preservation.
- **TC-PR-5.3.4 - Support routing is launch-visible:** Verify support routing appears in release notes, launch package, or pilot onboarding materials.

### Story PR-5.4: Prepare Pilot Onboarding, Release Notes, And Known-Risk Log

- **TC-PR-5.4.1 - Pilot onboarding covers No-CUI:** Verify pilot onboarding states No-CUI limits, prohibited data examples, support paths, known limitations, and synthetic demo explanation.
- **TC-PR-5.4.2 - Release notes include launch essentials:** Verify release notes include posture, scope, exclusions, known risks, support paths, staging smoke results, rollback plan, and content scope.
- **TC-PR-5.4.3 - Known-risk entries are complete:** Verify known risks include owner, mitigation, contingency, target date, current status, and approver.
- **TC-PR-5.4.4 - Launch materials are reviewed:** Verify support, onboarding, release notes, and known-risk artifacts have required review status before launch approval.

## 7. PR-6 - Approval And Launch Candidate

### Story PR-6.1: Collect Required Launch Approvals

- **TC-PR-6.1.1 - Product approval recorded:** Verify product owner approval includes date, scope, limitations, unresolved exceptions, and evidence reviewed.
- **TC-PR-6.1.2 - Engineering/security approvals recorded:** Verify engineering lead and security owner approvals include scope, limitations, unresolved exceptions, and evidence reviewed.
- **TC-PR-6.1.3 - Content/support/legal approvals recorded:** Verify compliance content, customer success/support, and legal/contracting approvals are present with scope and limitations.
- **TC-PR-6.1.4 - Missing approval blocks tag:** Verify launch candidate tagging is blocked when any required approval is missing.

### Story PR-6.2: Tag Launch Candidate With Evidence Links

- **TC-PR-6.2.1 - Tag preconditions are complete:** Verify evidence package and required approvals are complete before launch candidate tagging.
- **TC-PR-6.2.2 - Tag maps to immutable release inputs:** Verify the tag record includes commit, build artifact, deployment artifact, and evidence package location.
- **TC-PR-6.2.3 - Tag record links launch artifacts:** Verify release notes, known limitations, support paths, staging evidence, rollback plan, and content scope are linked.
- **TC-PR-6.2.4 - Missing evidence prevents tag:** Attempt or simulate tag readiness with missing evidence and verify tag creation is blocked.

## 8. PR-7 - Production Deployment And Pilot Launch

### Story PR-7.1: Deploy Production Through Approved CI/CD

- **TC-PR-7.1.1 - Production deploy uses approved artifact:** Verify production deployment references the approved launch candidate artifact.
- **TC-PR-7.1.2 - Production deploy uses approved pipeline:** Verify deployment runs through approved CI/CD, not manual ad hoc commands.
- **TC-PR-7.1.3 - Production configuration is verified:** Verify secrets source, migrations, storage, cache, background jobs, health checks, logs, alerts, and No-CUI data posture are checked.
- **TC-PR-7.1.4 - Deployment evidence is recorded:** Verify deployment time, artifact, operator, environment, result, and evidence location are recorded.

### Story PR-7.2: Run Production Smoke Tests

- **TC-PR-7.2.1 - Smoke tests cover core access:** Verify login, tenant access, and RBAC denial tests pass in production.
- **TC-PR-7.2.2 - Smoke tests cover core workflows:** Verify upload warning/blocking, evidence upload, report generation, and audit logging smoke tests pass using synthetic or non-sensitive data.
- **TC-PR-7.2.3 - Smoke tests cover operations:** Verify logs, alerts, and health checks are active and observed after deployment.
- **TC-PR-7.2.4 - Critical smoke failure blocks pilot onboarding:** Verify pilot onboarding does not proceed when a critical smoke test fails.

### Story PR-7.3: Onboard Controlled Pilot Customers

- **TC-PR-7.3.1 - Pilot starts after smoke pass:** Verify pilot onboarding begins only after production smoke tests pass.
- **TC-PR-7.3.2 - Pilot receives No-CUI guidance:** Verify each pilot customer receives No-CUI data handling guidance, prohibited data examples, support paths, and known limitations.
- **TC-PR-7.3.3 - Pilot tenant setup is correct:** Verify each pilot tenant has correct tenant mode, user roles, and support routing.
- **TC-PR-7.3.4 - First workflow is monitored:** Verify first workflow completion or first-use monitoring is recorded for each pilot tenant using non-sensitive identifiers.

## 9. PR-8 - Post-Launch Control

### Story PR-8.1: Monitor Pilot Signals Daily

- **TC-PR-8.1.1 - Daily monitoring covers required signals:** Verify audit logs, upload blocks, permission denials, report failures, support tickets, content disputes, health checks, alerts, and failed jobs are reviewed daily during pilot.
- **TC-PR-8.1.2 - Findings have ownership:** Verify each finding has severity, owner, mitigation, and target date.
- **TC-PR-8.1.3 - High-risk issues escalate:** Verify security, tenant isolation, data-handling, suspected CUI, and overclaim issues are escalated according to runbooks.
- **TC-PR-8.1.4 - Regressions enter tracking:** Verify production readiness regressions appear in the risk register, known-risk log, or backlog tracker.

### Story PR-8.2: Hold Post-Launch Readiness Review

- **TC-PR-8.2.1 - Review is held and recorded:** Verify the post-launch readiness review has date, participants, agenda, findings, and decisions.
- **TC-PR-8.2.2 - Pilot findings are triaged:** Verify incidents, defects, tickets, upload blocks, permission denials, content disputes, report failures, and feedback are reviewed.
- **TC-PR-8.2.3 - Regressions have mitigation:** Verify production readiness regressions have owner, severity, mitigation, and due date.
- **TC-PR-8.2.4 - Launch artifacts are updated:** Verify release notes, support materials, known-risk log, readiness checklist, or decision log are updated for material findings.

### Story PR-8.3: Gate Phase 2 Until MVP Controls Are Stable

- **TC-PR-8.3.1 - Launch findings become ready backlog items:** Verify launch findings are converted into backlog items that satisfy Definition of Ready.
- **TC-PR-8.3.2 - Critical control instability blocks Phase 2:** Verify Phase 2 remains blocked while tenant isolation, RBAC, uploads, reports, audit logging, support, content governance, claims, or No-CUI posture are unstable.
- **TC-PR-8.3.3 - Stability criteria are explicit:** Verify Phase 2 gate criteria identify required evidence, owner, approvers, and pass/fail status.
- **TC-PR-8.3.4 - Gate decision is recorded:** Verify Phase 2 gate status is recorded before Govcon Intelligence work proceeds.

## Hidden Risks And Test Dependencies

- Documentation tests can pass while executable controls fail; server-side tenant mode, RBAC, upload, report, and audit behavior still require code-level or staging verification.
- Manual approval evidence is only valid when it identifies approver, date, scope, limitations, evidence reviewed, and unresolved exceptions.
- Staging and production tests must use synthetic or non-sensitive data only.
- Search-based claim tests depend on current terminology; reviewers must still inspect semantically equivalent overclaims.
- Missing malware scanning, restore evidence, launch approvals, or production smoke evidence remains launch-blocking unless formally excepted by accountable owners.

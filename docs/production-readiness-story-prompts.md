# Production Readiness Story Execution Prompts

These prompts are designed to be copied into a fresh execution thread, one story at a time. Each prompt points back to the source backlog in `docs/production-readiness-phase-use-cases.md` and must be executed under the launch posture in `docs/production-readiness-plan.md`.

## Shared Prompt Requirements

Use these requirements for every production readiness story:

- Treat the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.
- Real customer CUI remains prohibited unless a future `CuiReady` posture is separately approved.
- Preserve tenant isolation, RBAC, audit logging, source traceability, review metadata, data-handling controls, and customer-claim controls.
- Read `docs/production-readiness-plan.md`, `docs/production-readiness-phase-use-cases.md`, `docs/production-readiness-checklist.md`, and the referenced story before editing files.
- Inspect existing files first and avoid rewriting unrelated documents.
- Convert vague readiness statements into concrete artifacts, evidence, owners, dates, test results, approval records, blocker records, or known-risk entries.
- Do not mark a story complete unless its acceptance criteria are satisfied or each unsatisfied item is recorded as a blocker, accepted risk, or deferred follow-up with owner and mitigation.
- Add or update tests when the story changes executable behavior, especially tenant mode enforcement, upload blocking, RBAC, tenant isolation, audit logging, reports, deployment checks, or health checks.
- For documentation-only stories, run formatting, link, or grep-based verification where practical and report what was checked.
- After execution, summarize changed files, verification commands, unresolved risks, and any launch blockers.

## 1. PR-0 - Launch Posture Freeze

### Story PR-0.1: Record And Approve Launch Posture

Prompt:
You are helping execute GCCS Production Readiness Story PR-0.1, "Record And Approve Launch Posture," from `docs/production-readiness-phase-use-cases.md`.

First, read the production readiness plan, production readiness checklist, decision log, and PR-0.1 source story. Analyze whether the launch posture decision is structurally sufficient for a No-CUI MVP launch. Identify at least three ways the current posture could fail at scale, create a security or compliance exposure, or block production launch.

Then make the smallest safe documentation updates needed to satisfy PR-0.1. Confirm the launch decision states No-CUI / compliance management only with synthetic CUI-ready demonstration workflows; explicitly prohibits real customer CUI until a separately approved future `CuiReady` posture; identifies required approvers; records approval or pending status; and lists missing approvals as launch blockers.

Acceptance target:
- The posture decision names No-CUI as the only approved MVP production posture.
- Real customer CUI is explicitly prohibited until future `CuiReady` approval.
- Required approvers are named or marked pending.
- Missing approvals are visible as launch blockers.

Run a grep-based verification for posture, approver, pending, blocker, and `CuiReady` language. Report changed files, verification results, hidden risks, and remaining blockers.

#-----------------------------------------

### Story PR-0.2: Align Launch Documents To No-CUI Posture

Prompt:
You are helping execute GCCS Production Readiness Story PR-0.2, "Align Launch Documents To No-CUI Posture," from `docs/production-readiness-phase-use-cases.md`.

Read the source story and all launch reference documents listed in `docs/production-readiness-plan.md`. Search for language implying production CUI readiness, certification, official approval, legal advice, government endorsement, CMMC assessment success, or permission to upload real CUI.

Reject any contradictory launch language. Update or flag inconsistent language with owner, severity, and disposition. Preserve accurate references to synthetic or redacted demo workflows, but ensure future `CuiReady` capability is described only as excluded or separately gated.

Acceptance target:
- No referenced launch document describes the MVP as production CUI-capable.
- Future `CuiReady` capability is described only as excluded or separately gated.
- Unresolved language conflicts have owner, severity, and disposition.
- Customer-facing documents do not contradict server-side tenant mode behavior.

Run repository searches for risky phrases such as `CUI-ready`, `CuiReady`, `certified`, `certification`, `approved`, `legal advice`, `government endorsement`, and `real CUI`. Report changed files, verification results, hidden risks, and remaining blockers.

#-----------------------------------------

### Story PR-0.3: Verify Tenant Mode Boundary Design

Prompt:
You are helping execute GCCS Production Readiness Story PR-0.3, "Verify Tenant Mode Boundary Design," from `docs/production-readiness-phase-use-cases.md`.

Read the production readiness plan, architecture docs, security-control docs, relevant upload/evidence/report code, and PR-0.3. Analyze whether tenant mode boundaries are enforced by trusted server-side checks or merely implied by UI copy. Identify at least three failure modes, including bypass through direct API calls, background jobs, reports/exports, imports/extraction, or future `CuiReady` leakage.

Implement the smallest safe change needed to document, test, or enforce tenant modes for `DemoSandbox`, `NoCui`, and future `CuiReady`. If executable enforcement is missing, add focused tests or document the missing control as a launch blocker.

Acceptance target:
- Tenant mode definitions are documented with allowed and prohibited behavior.
- `NoCui` tenants block real CUI server-side.
- `DemoSandbox` workflows allow only synthetic or redacted demonstration data.
- Tests cover allowed synthetic demo data, blocked real CUI, and future `CuiReady` exclusion.

Run targeted backend/frontend tests if behavior changes, otherwise run grep-based verification for tenant mode definitions and test references. Report changed files, verification results, hidden risks, and blockers.

## 2. PR-1 - Backlog Readiness Gate

### Story PR-1.1: Re-Run Definition Of Ready For Open MVP Stories

Prompt:
You are helping execute GCCS Production Readiness Story PR-1.1, "Re-Run Definition Of Ready For Open MVP Stories," from `docs/production-readiness-phase-use-cases.md`.

Read the Definition of Ready, production readiness plan, production readiness checklist, development story docs, and PR-1.1. Inventory remaining open MVP launch stories. Analyze whether any story has unresolved scope, missing actor/goal/value, missing included or excluded scope, missing acceptance criteria, missing dependencies, missing security/RBAC/audit/CUI implications, or untestable language.

Update the readiness tracking artifacts with pass/fail status, rejected/deferred stories, follow-up records, and explicit acceptance limitations. Do not silently keep ambiguous work in launch scope.

Acceptance target:
- Every open launch story has a completed readiness review.
- Stories missing required readiness fields are not accepted into launch scope.
- Deferred stories have named follow-up records or explicit acceptance limitations.
- No launch story remains with unresolved data-handling ambiguity.

Run verification that every reviewed story has readiness status and disposition. Report changed files, verification results, hidden risks, and remaining blockers.

#-----------------------------------------

### Story PR-1.2: Map Open Stories To Test Cases

Prompt:
You are helping execute GCCS Production Readiness Story PR-1.2, "Map Open Stories To Test Cases," from `docs/production-readiness-phase-use-cases.md`.

Read `docs/development-story-test-cases.md`, production readiness docs, open MVP story docs, and PR-1.2. Map every open launch story to applicable `TC-*` cases or document why no test case applies. Identify missing unit, integration, API, frontend, staging, tenant isolation, RBAC, upload, report, and audit coverage.

Update the appropriate readiness or test mapping artifact. Any story affecting tenant isolation, RBAC, uploads, reports, evidence, imports, exports, search, AI, or extraction must include tenant-mode coverage or be blocked/deferred.

Acceptance target:
- Every open launch story references applicable `TC-*` cases or a documented exception.
- Missing required coverage is represented as a launch task.
- Risky workflows include tenant-mode test coverage.
- Any story expanding data posture beyond No-CUI is rejected unless separately approved.

Run grep-based verification for `TC-*` mappings and risky workflow coverage. Report changed files, verification results, hidden risks, and remaining blockers.

#-----------------------------------------

### Story PR-1.3: Gate Risky Workflow Changes

Prompt:
You are helping execute GCCS Production Readiness Story PR-1.3, "Gate Risky Workflow Changes," from `docs/production-readiness-phase-use-cases.md`.

Read PR-1.3, production readiness docs, open launch story docs, and code/docs for upload, import, export, search, AI, evidence, report, extraction, and background processing. Identify launch stories that alter data ingress, egress, automated processing, tenant boundaries, RBAC, or audit behavior.

Reject, defer, narrow, or mark blocked any risky workflow story that lacks server-side tenant-mode enforcement, RBAC review, audit logging implications, and tenant isolation coverage. Update tracking artifacts with risk classification and disposition.

Acceptance target:
- Risky workflow stories are explicitly identified.
- Each risky workflow has tenant-mode, RBAC, audit logging, and tenant isolation coverage.
- Stories without coverage are deferred, blocked, or narrowed.
- Launch scope contains no unreviewed data ingress, egress, or automated processing changes.

Run verification searches for risky workflow categories and their dispositions. Report changed files, verification results, hidden risks, and remaining blockers.

## 3. PR-2 - Completion And Scope Freeze

### Story PR-2.1: Freeze MVP Launch Scope

Prompt:
You are helping execute GCCS Production Readiness Story PR-2.1, "Freeze MVP Launch Scope," from `docs/production-readiness-phase-use-cases.md`.

Read the production readiness plan, MVP roadmap, execution plan, readiness checklist, and PR-2.1. Analyze whether the current launch scope admits Phase 2+ work, automation, AI, extraction, or CUI-ready behavior that could destabilize launch.

Update launch scope artifacts so the MVP scope is frozen to launch-critical modules. Defer non-launch Phase 2+ features unless they remove a production blocker. Record scope exclusions, known limitations, and the approval requirement for any new scope.

Acceptance target:
- MVP launch scope is documented and frozen.
- Phase 2+ work is deferred unless explicitly launch-blocking with evidence.
- Scope exclusions are listed in release notes or known limitations.
- New scope requires product owner and engineering lead approval.

Run verification for scope freeze, deferred Phase 2+ work, exclusions, and approval language. Report changed files, verification results, hidden risks, and blockers.

#-----------------------------------------

### Story PR-2.2: Re-Run Definition Of Done For Completed MVP Stories

Prompt:
You are helping execute GCCS Production Readiness Story PR-2.2, "Re-Run Definition Of Done For Completed MVP Stories," from `docs/production-readiness-phase-use-cases.md`.

Read the Definition of Done, production readiness docs, completed MVP story records, test docs, and PR-2.2. Audit completed launch stories for passing acceptance criteria, relevant tests, tenant isolation review, RBAC review, audit logging, error states, empty states, validation failures, permission denials, accessibility coverage, and documentation/release note updates.

Update completion tracking artifacts. Any missing Definition of Done item must become a blocker, accepted risk, deferred follow-up, or not-applicable decision with evidence.

Acceptance target:
- Completed launch stories have evidence that acceptance criteria passed.
- Protected workflows include RBAC and tenant isolation review evidence.
- Sensitive actions include audit logging evidence or documented exception.
- UI workflows include applicable validation, denial, empty, error, and accessibility coverage.

Run verification for Definition of Done evidence, tests, and dispositions. Report changed files, verification results, hidden risks, and blockers.

#-----------------------------------------

### Story PR-2.3: Convert Completion Gaps Into Launch Decisions

Prompt:
You are helping execute GCCS Production Readiness Story PR-2.3, "Convert Completion Gaps Into Launch Decisions," from `docs/production-readiness-phase-use-cases.md`.

Read PR-2.3, readiness checklist, known-risk log or decision log, and completion review artifacts. Identify every failed, partial, skipped, or untested Definition of Done item. Classify each gap as launch blocker, accepted risk, deferred follow-up, or not applicable.

Update the launch blocker, known-risk, decision, or follow-up artifacts with owner, severity, mitigation, contingency, target date, current status, and approver where applicable.

Acceptance target:
- Every unresolved completion gap has a disposition.
- Launch blockers are visible in readiness or launch package artifacts.
- Accepted risks include owner, mitigation, contingency, and approver.
- Deferred items do not contradict No-CUI posture or release claims.

Run verification that no unresolved completion gap lacks disposition. Report changed files, verification results, hidden risks, and blockers.

## 4. PR-3 - Staging Verification

### Story PR-3.1: Deploy And Smoke Test Staging

Prompt:
You are helping execute GCCS Production Readiness Story PR-3.1, "Deploy And Smoke Test Staging," from `docs/production-readiness-phase-use-cases.md`.

Read staging environment docs, production readiness plan, checklist, health-check implementation, CI/CD docs, and PR-3.1. Analyze whether staging deployment evidence proves the real deployment path and whether staging is free of production customer data, real CUI, production secrets, uploads, and logs.

Run or document the approved staging deployment and `/health` smoke test path. Verify health reports API, database, cache, storage, background job signals, and `dataPosture = No-CUI / compliance management only`. Attach or update launch evidence artifacts.

Acceptance target:
- Staging deploys through approved CI/CD.
- Staging contains no production customer data, real CUI, production secrets, production uploads, or production logs.
- `/health` reports expected dependency and data posture signals.
- Deployment and smoke evidence are attached to the launch package.

Run the actual commands available in the environment, or document why they cannot be run and what evidence remains missing. Report changed files, verification results, hidden risks, and blockers.

#-----------------------------------------

### Story PR-3.2: Execute End-To-End MVP Workflow In Staging

Prompt:
You are helping execute GCCS Production Readiness Story PR-3.2, "Execute End-To-End MVP Workflow In Staging," from `docs/production-readiness-phase-use-cases.md`.

Read PR-3.2, staging docs, MVP workflow docs, API/UI test docs, and production readiness docs. Execute or document the staging MVP workflow: tenant creation, user invite, role assignment, company profile, contract creation, allowed upload, blocked CUI upload, manual clause tagging, obligation generation, task creation, evidence upload, report generation, and audit log export.

Capture test output, screenshots, logs, or run records where possible. Record defects with severity, owner, and blocker status.

Acceptance target:
- End-to-end workflow completes using synthetic or non-sensitive data only.
- Allowed upload succeeds under No-CUI controls.
- Real CUI or prohibited upload is blocked and audit logged.
- Report generation and audit log export are tenant-scoped.
- Workflow evidence is attached to the launch package.

Run available E2E, API, frontend, or manual verification commands. Report changed files, verification results, hidden risks, and blockers.

#-----------------------------------------

### Story PR-3.3: Verify Tenant Isolation And RBAC In Staging

Prompt:
You are helping execute GCCS Production Readiness Story PR-3.3, "Verify Tenant Isolation And RBAC In Staging," from `docs/production-readiness-phase-use-cases.md`.

Read PR-3.3, tenant/RBAC docs, security docs, API tests, and staging docs. Run or document cross-tenant read, update, delete, export, report, evidence, contract, task, and audit-log access tests. Run role tests for owner, admin, compliance manager, contributor, auditor, and advisor.

Treat any direct API authorization bypass as a launch blocker. UI-hidden actions are insufficient unless the server denies direct calls.

Acceptance target:
- Cross-tenant access attempts are denied across protected workflows.
- Role-restricted actions are denied server-side.
- Permission failures return consistent error responses.
- Tenant isolation and RBAC evidence is attached to the launch package.

Run targeted backend/API tests if available. Report changed files, verification results, hidden risks, and blockers.

#-----------------------------------------

### Story PR-3.4: Verify Upload Guardrails And Report Controls In Staging

Prompt:
You are helping execute GCCS Production Readiness Story PR-3.4, "Verify Upload Guardrails And Report Controls In Staging," from `docs/production-readiness-phase-use-cases.md`.

Read PR-3.4, upload/evidence docs, report docs, staging docs, and relevant tests/code. Verify upload acknowledgement, warning display, real CUI blocking, prohibited content blocking, oversized file blocking, disallowed file type blocking, allowed upload audit logging, and blocked upload audit logging.

Then verify report tenant scope, RBAC, source links, last-reviewed dates, draft-only CMMC language, and absence of pass/fail, certification, official approval, legal advice, or permission-to-store-CUI claims.

Acceptance target:
- Upload guardrails pass acknowledgement, warning, blocking, validation, and audit logging cases.
- Reports include source links and last-reviewed dates where obligations appear.
- Reports do not include prohibited compliance or CUI-storage claims.
- Upload and report evidence is attached to the launch package.

Run available upload/report tests or document missing evidence as a blocker. Report changed files, verification results, hidden risks, and blockers.

## 5. PR-4 - Launch Evidence Package

### Story PR-4.1: Attach Backup And Restore Evidence

Prompt:
You are helping execute GCCS Production Readiness Story PR-4.1, "Attach Backup And Restore Evidence," from `docs/production-readiness-phase-use-cases.md`.

Read PR-4.1, backup/restore docs, staging docs, production readiness checklist, and launch evidence artifacts. Analyze whether current evidence proves a successful restore or merely asserts that backups exist.

Complete or document staging backup and restore verification. Record restore date, environment, data set, command or pipeline reference, result, reviewer, and evidence location. Missing restore evidence must remain a production launch blocker.

Acceptance target:
- Backup evidence is available for the staging launch candidate.
- Restore evidence proves a successful restore from backup.
- Restore evidence includes date, environment, data set, result, and reviewer.
- Missing restore evidence remains a production launch blocker.

Run available backup/restore verification commands, or document why execution is blocked. Report changed files, verification results, hidden risks, and blockers.

#-----------------------------------------

### Story PR-4.2: Attach Deployment, Migration, And Rollback Evidence

Prompt:
You are helping execute GCCS Production Readiness Story PR-4.2, "Attach Deployment, Migration, And Rollback Evidence," from `docs/production-readiness-phase-use-cases.md`.

Read PR-4.2, CI/CD docs, migration docs, rollback docs, staging evidence, and production readiness checklist. Verify whether deployment, migration, and rollback evidence is complete enough for launch approval.

Attach or update staging workflow evidence, smoke results, migration script validation evidence, rollback simulation notes, and migration rollback notes. If a migration is irreversible, record the risk, owner, mitigation, contingency, and approver.

Acceptance target:
- Staging workflow and smoke evidence are included in the launch package.
- Migration evidence identifies scripts, environment, result, and reviewer.
- Rollback evidence describes tested rollback behavior and limitations.
- Irreversible migration risk has explicit owner and approver acceptance.

Run available migration/rollback validation commands or document blockers. Report changed files, verification results, hidden risks, and blockers.

#-----------------------------------------

### Story PR-4.3: Decide Malware Scanning Launch Path

Prompt:
You are helping execute GCCS Production Readiness Story PR-4.3, "Decide Malware Scanning Launch Path," from `docs/production-readiness-phase-use-cases.md`.

Read PR-4.3, upload/evidence docs, security docs, production readiness checklist, and current malware scanning implementation/configuration. Determine whether production malware scanning is enabled for uploads. If enabled, attach scanner configuration and test evidence. If not enabled, document the launch exception, compensating controls, affected workflows, owner, expiration date, and required approvers.

Do not present malware scanning as complete unless executable evidence exists.

Acceptance target:
- Production malware scanning is enabled with evidence or formally excepted.
- Any exception includes compensating controls and product/security owner approval.
- The known-risk acceptance log includes the malware scanning decision.
- Upload launch readiness remains blocked until this decision is complete.

Run available scanner configuration/tests or document the blocker. Report changed files, verification results, hidden risks, and blockers.

## 6. PR-5 - Content, Claims, And Support Readiness

### Story PR-5.1: Review High-Risk Obligation Content

Prompt:
You are helping execute GCCS Production Readiness Story PR-5.1, "Review High-Risk Obligation Content," from `docs/production-readiness-phase-use-cases.md`.

Read PR-5.1, compliance content governance docs, obligation library content, production readiness checklist, and content review artifacts. Identify high-risk obligation records and verify source URL, trigger condition, required actions, evidence examples, confidence, review owner, review state, and last-reviewed date.

Approve only records that satisfy publication rules. Hide, retire, or mark blocked records that are incomplete, stale, ambiguous, or awaiting expert review.

Acceptance target:
- Every published obligation includes required source and review metadata.
- High-risk records are approved before publication or hidden from customer-facing production views.
- Content missing required metadata is not published.
- Content approval or hiding decisions are recorded.

Run content schema validation or grep-based metadata verification where possible. Report changed files, verification results, hidden risks, and blockers.

#-----------------------------------------

### Story PR-5.2: Review Customer-Facing Claims

Prompt:
You are helping execute GCCS Production Readiness Story PR-5.2, "Review Customer-Facing Claims," from `docs/production-readiness-phase-use-cases.md`.

Read PR-5.2, product copy, onboarding text, upload warnings, report templates, release notes, support materials, pilot onboarding materials, and production readiness plan. Search for claims implying legal advice, certification, CMMC approval, official assessment success, government endorsement, or permission to store real CUI.

Revise overclaimed language. Preserve No-CUI boundaries and draft-only CMMC language. Record legal or contracting review status and any accepted claim risk.

Acceptance target:
- Customer-facing copy avoids legal, certification, CMMC approval, government endorsement, official success, or real CUI storage claims.
- No-CUI launch limits appear in onboarding, upload flows, support materials, and release notes.
- Draft-only CMMC language is preserved where CMMC outputs appear.
- Legal or contracting review status is recorded before launch approval.

Run repository searches for risky claim phrases and report results. Report changed files, verification results, hidden risks, and blockers.

#-----------------------------------------

### Story PR-5.3: Finalize Support Runbooks

Prompt:
You are helping execute GCCS Production Readiness Story PR-5.3, "Finalize Support Runbooks," from `docs/production-readiness-phase-use-cases.md`.

Read PR-5.3, support docs, production readiness plan, security incident docs, backup/restore docs, rollback docs, and known-risk artifacts. Finalize support runbooks for prohibited upload, suspected CUI, tenant exposure, access issue, evidence failure, report failure, content correction, security incident, backup restore, and rollback.

Each runbook must include owner, triage steps, escalation path, severity guidance, evidence to capture, and No-CUI-specific containment instructions where applicable.

Acceptance target:
- Each required support runbook exists and has an owner.
- Runbooks include triage steps, escalation path, severity guidance, and evidence capture.
- Suspected CUI and prohibited upload runbooks require containment and escalation.
- Support routing is included in launch materials.

Run verification that every required runbook topic exists and has an owner/escalation path. Report changed files, verification results, hidden risks, and blockers.

#-----------------------------------------

### Story PR-5.4: Prepare Pilot Onboarding, Release Notes, And Known-Risk Log

Prompt:
You are helping execute GCCS Production Readiness Story PR-5.4, "Prepare Pilot Onboarding, Release Notes, And Known-Risk Log," from `docs/production-readiness-phase-use-cases.md`.

Read PR-5.4, production readiness plan, release note drafts, onboarding materials, known-risk artifacts, support docs, and staging evidence. Prepare pilot onboarding materials with No-CUI limits, prohibited data examples, support paths, known limitations, and synthetic demo explanation.

Prepare release notes with launch posture, scope, exclusions, known risks, support paths, staging smoke results, rollback plan, and content scope. Build or update the known-risk acceptance log with owner, mitigation, contingency, target date, status, and approver.

Acceptance target:
- Pilot onboarding materials state No-CUI limits and prohibited data examples.
- Release notes include posture, scope, exclusions, known risks, support paths, staging smoke results, rollback plan, and content scope.
- Known risks include owner, mitigation, contingency, target date, status, and approver.
- Support, onboarding, release notes, and known-risk artifacts are launch-ready.

Run verification for required sections and No-CUI/prohibited data language. Report changed files, verification results, hidden risks, and blockers.

## 7. PR-6 - Approval And Launch Candidate

### Story PR-6.1: Collect Required Launch Approvals

Prompt:
You are helping execute GCCS Production Readiness Story PR-6.1, "Collect Required Launch Approvals," from `docs/production-readiness-phase-use-cases.md`.

Read PR-6.1, production readiness plan, launch evidence package, known-risk log, release notes, and approval records. Determine whether product owner, engineering lead, security owner, compliance content owner, customer success/support owner, and legal or contracting advisor approvals are present.

Record approval date, approver, scope, limitations, unresolved exceptions, and evidence reviewed. Missing approvals must block launch candidate tagging.

Acceptance target:
- All required launch approvals are recorded before launch candidate tagging.
- Each approval identifies scope, limitations, and unresolved exceptions.
- Missing approval blocks launch candidate tagging.
- Accepted exceptions are present in the known-risk acceptance log.

Run verification for every required approval role and exception linkage. Report changed files, verification results, hidden risks, and blockers.

#-----------------------------------------

### Story PR-6.2: Tag Launch Candidate With Evidence Links

Prompt:
You are helping execute GCCS Production Readiness Story PR-6.2, "Tag Launch Candidate With Evidence Links," from `docs/production-readiness-phase-use-cases.md`.

Read PR-6.2, release process docs, launch evidence package, approval records, release notes, rollback plan, and content scope. Verify that evidence and approvals are complete before creating or documenting a launch candidate tag.

If approvals or evidence are missing, do not tag. Instead, record blockers. If complete, create the launch candidate through the approved release process and record tag, commit, build artifact, deployment artifact, and evidence package location.

Acceptance target:
- Launch candidate tag is not created until evidence and approvals are complete.
- Tag record includes release notes, known limitations, support paths, staging evidence, rollback plan, and content scope.
- Tag maps to a specific commit and build artifact.
- Launch candidate creation is recorded in the decision or release log.

Run the approved git/release commands only when preconditions are satisfied. Report changed files, command results, hidden risks, and blockers.

## 8. PR-7 - Production Deployment And Pilot Launch

### Story PR-7.1: Deploy Production Through Approved CI/CD

Prompt:
You are helping execute GCCS Production Readiness Story PR-7.1, "Deploy Production Through Approved CI/CD," from `docs/production-readiness-phase-use-cases.md`.

Read PR-7.1, release process docs, production environment docs, CI/CD docs, launch candidate record, and production readiness plan. Confirm the approved launch candidate artifact, environment configuration, secrets source, migrations, storage, cache, background jobs, health checks, logs, alerts, and No-CUI data posture.

Deploy production only through the approved CI/CD path when launch prerequisites are satisfied. Record deployment time, artifact, operator, environment, result, and evidence location.

Acceptance target:
- Production deployment uses the approved launch candidate artifact.
- Deployment runs through approved CI/CD.
- Production configuration confirms No-CUI data posture.
- Deployment evidence is recorded.

Run deployment commands only when authorized launch prerequisites are complete. Report command results, changed files, hidden risks, and blockers.

#-----------------------------------------

### Story PR-7.2: Run Production Smoke Tests

Prompt:
You are helping execute GCCS Production Readiness Story PR-7.2, "Run Production Smoke Tests," from `docs/production-readiness-phase-use-cases.md`.

Read PR-7.2, production smoke test docs, deployment records, health-check docs, upload/report/audit docs, and production readiness plan. Run production smoke tests immediately after deployment using synthetic or non-sensitive data only.

Verify login, tenant access, RBAC denial, upload warning and blocking behavior, evidence upload, report generation, audit logging, logs, alerts, and health checks. Critical failures must block pilot onboarding.

Acceptance target:
- Production smoke tests pass for login, tenant access, RBAC denial, upload controls, evidence, reports, audit logging, logs, alerts, and health checks.
- Smoke tests use synthetic or non-sensitive data only.
- Failed critical smoke tests block pilot onboarding.
- Production smoke evidence is attached to launch records.

Run available smoke test commands or document manual verification evidence. Report results, hidden risks, and blockers.

#-----------------------------------------

### Story PR-7.3: Onboard Controlled Pilot Customers

Prompt:
You are helping execute GCCS Production Readiness Story PR-7.3, "Onboard Controlled Pilot Customers," from `docs/production-readiness-phase-use-cases.md`.

Read PR-7.3, pilot onboarding materials, support routing docs, production smoke results, tenant setup docs, and production readiness plan. Confirm production smoke tests passed before onboarding any pilot customer.

Onboard only approved pilot customers. Provide No-CUI data handling guidance, prohibited data examples, support route, known limitations, and acknowledgement workflow. Verify tenant setup, roles, tenant mode, and first workflow completion monitoring.

Acceptance target:
- Pilot customers are onboarded only after production smoke tests pass.
- Each pilot customer receives No-CUI guidance and prohibited data examples.
- Pilot tenants are configured with appropriate roles and tenant mode.
- Support routing and escalation paths are active before pilot use begins.

Do not add real customer data to docs. Record onboarding checklist status using non-sensitive identifiers. Report changed files, verification results, hidden risks, and blockers.

## 9. PR-8 - Post-Launch Control

### Story PR-8.1: Monitor Pilot Signals Daily

Prompt:
You are helping execute GCCS Production Readiness Story PR-8.1, "Monitor Pilot Signals Daily," from `docs/production-readiness-phase-use-cases.md`.

Read PR-8.1, monitoring docs, support runbooks, audit logging docs, upload guardrail docs, known-risk log, and production readiness plan. Establish or update daily pilot monitoring for audit logs, upload blocks, permission denials, report failures, support tickets, content disputes, health checks, alerts, and failed jobs.

Findings must have severity, owner, mitigation, and target date. Security, tenant isolation, data-handling, or overclaim issues must escalate according to runbooks.

Acceptance target:
- Daily monitoring covers required production and support signals.
- Findings have severity, owner, mitigation, and target date.
- Security, tenant isolation, data-handling, or overclaim issues are escalated according to runbooks.
- Production readiness regressions are visible in the risk or backlog tracker.

Run available monitoring queries or document the monitoring checklist. Report changed files, verification results, hidden risks, and blockers.

#-----------------------------------------

### Story PR-8.2: Hold Post-Launch Readiness Review

Prompt:
You are helping execute GCCS Production Readiness Story PR-8.2, "Hold Post-Launch Readiness Review," from `docs/production-readiness-phase-use-cases.md`.

Read PR-8.2, pilot monitoring findings, support tickets, incident records, content disputes, upload block records, permission denial records, release notes, known-risk log, and production readiness plan. Prepare and record the post-launch readiness review.

Review pilot incidents, defects, support tickets, upload blocks, permission denials, content disputes, report failures, and customer feedback. Convert findings into decisions, owners, due dates, and follow-up actions. Update launch artifacts when material findings change posture, support, claims, or risk.

Acceptance target:
- Post-launch readiness review is held and recorded.
- Pilot findings are triaged and assigned.
- Production readiness regressions have owner, severity, mitigation, and due date.
- Updated launch artifacts reflect material findings.

Use non-sensitive summaries only. Report changed files, verification results, hidden risks, and blockers.

#-----------------------------------------

### Story PR-8.3: Gate Phase 2 Until MVP Controls Are Stable

Prompt:
You are helping execute GCCS Production Readiness Story PR-8.3, "Gate Phase 2 Until MVP Controls Are Stable," from `docs/production-readiness-phase-use-cases.md`.

Read PR-8.3, post-launch readiness review, production readiness plan, MVP findings, risk log, backlog docs, and Phase 2 roadmap. Determine whether tenant isolation, RBAC, uploads, reports, audit logging, support, content governance, customer claims, and No-CUI posture are stable enough to unblock Phase 2 Govcon Intelligence.

Convert launch findings into backlog items using Definition of Ready. Define stability criteria, evidence requirements, approvers, and Phase 2 gate status. Keep Phase 2 blocked while critical launch controls are unstable.

Acceptance target:
- Launch findings are converted into Definition-of-Ready backlog items.
- Phase 2 remains blocked while critical launch controls are unstable.
- Stability criteria identify required evidence and approvers.
- Phase 2 gate decision is recorded before Govcon Intelligence proceeds.

Run verification that all launch findings have backlog disposition and gate status. Report changed files, verification results, hidden risks, and blockers.

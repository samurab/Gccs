# Production Readiness Roadmap

This roadmap continues the GCCS project from MVP completion into production readiness. It is a planning and release-control artifact, not proof that production launch is approved.

The launch posture is **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**. Real customer CUI remains prohibited unless a future `CuiReady` posture is separately approved.

Reference documents:

- `docs/product-readiness-note.md`
- `docs/production-readiness-checklist.md`
- `docs/software-delivery-plan.md`
- `docs/mvp-execution-plan.md`
- `docs/mvp-roadmap.md`
- `docs/product-strategy.md`
- `docs/staging-environment.md`
- `docs/definition-of-ready.md`
- `docs/security-control-implications.md`
- `docs/decision-log.md`

## Launch Blockers

Production launch remains blocked until these items are resolved or formally accepted by the accountable approvers:

- Staging restore evidence is attached to the launch package.
- Malware scanning is enabled for production uploads or an explicit launch exception is approved with compensating controls.
- High-risk obligation records are approved or hidden from customer-facing production views.
- Launch release notes and known-risk acceptance log are complete.
- Required product, engineering, security, compliance content, support, and legal/contracting approvals are complete.

## Phase PR-0 - Launch Posture Freeze

- Lock the launch posture as No-CUI / compliance management only with synthetic CUI-ready demonstration workflows.
- Confirm the referenced readiness, delivery, roadmap, strategy, and staging documents use that posture consistently.
- Record the No-CUI MVP launch posture decision in `docs/decision-log.md`.
- Confirm `DemoSandbox`, `NoCui`, and future `CuiReady` tenant modes are documented.
- Confirm tenant mode boundaries are enforced server-side, not only through UI copy.
- Confirm future `CuiReady` capability is excluded from production claims until separately approved.

Exit criteria:

- No referenced launch document describes the MVP as production CUI-capable.
- The decision log contains the No-CUI MVP launch posture decision.
- Tenant mode enforcement tests cover real CUI blocking and synthetic demo boundaries.

## Phase PR-1 - Backlog Readiness Gate

- Re-run Definition of Ready review for every remaining open MVP story.
- Confirm each open story has actor, goal, business value, included scope, excluded scope, and acceptance criteria.
- Confirm every open story maps to matching `TC-*` test cases.
- Confirm every open story identifies data requirements, dependencies, security implications, RBAC implications, audit logging implications, and CUI/data-handling implications.
- Reject or defer any story that changes upload, import, export, search, AI, evidence, report, or extraction behavior without tenant-mode test coverage.

Exit criteria:

- Open launch stories are ready without unresolved scope, data, security, or test ambiguity.
- Deferred stories have named follow-up records or explicit acceptance limitations.
- No launch story expands the data posture beyond No-CUI without a separate approval gate.

## Phase PR-2 - Completion And Scope Freeze

- Freeze MVP scope to launch-critical modules only.
- Defer Phase 2 and later features unless required to unblock production readiness.
- Re-run Definition of Done review for every completed MVP story.
- Confirm completed stories have passing acceptance criteria and relevant unit, integration, API, frontend, or regression tests.
- Confirm tenant isolation was reviewed for every affected workflow.
- Confirm RBAC was reviewed for every protected action.
- Confirm sensitive actions are audit logged.
- Confirm error states, empty states, validation failures, and permission denials are handled.
- Confirm user-facing UI changes have basic accessibility coverage.
- Confirm documentation or release notes were updated for changed behavior.

Exit criteria:

- MVP launch scope is frozen.
- Completed MVP stories satisfy Definition of Done.
- Phase 2+ work is either deferred or explicitly marked as launch-blocking with evidence.

## Phase PR-3 - Staging Verification

- Run full staging deployment from CI/CD.
- Confirm staging uses synthetic-only data.
- Confirm staging contains no production customer data, real CUI, production secrets, production uploads, or production logs.
- Run staging smoke tests against `/health`.
- Confirm health reports API, database, cache, storage, and background job signals.
- Confirm staging health reports `dataPosture = No-CUI / compliance management only`.
- Run the end-to-end MVP workflow in staging: tenant creation, user invite, role assignment, company profile, contract creation, allowed upload, blocked CUI upload, manual clause tagging, obligation generation, task creation, evidence upload, report generation, and audit log export.
- Run tenant isolation tests for cross-tenant read, update, delete, export, report, evidence, contract, task, and audit-log access.
- Run RBAC tests for owner, admin, compliance manager, contributor, auditor, and advisor roles.
- Run upload guardrail tests for acknowledgement, warning display, real CUI blocking, prohibited content blocking, oversized file blocking, disallowed file type blocking, allowed upload audit logging, and blocked upload audit logging.
- Run report/export tests for tenant scope, RBAC, source links, last-reviewed dates, draft-only CMMC language, and no pass/fail or certification claims.

Exit criteria:

- Staging deployment and smoke evidence are attached to the launch package.
- End-to-end MVP workflow completes with synthetic or non-sensitive data only.
- Tenant isolation, RBAC, upload guardrail, and report/export tests pass.

## Phase PR-4 - Launch Evidence Package

- Complete staging backup and restore verification.
- Attach restore evidence to the launch package.
- Attach staging workflow evidence, smoke results, migration script evidence, rollback simulation notes, and migration rollback notes.
- Decide the malware scanning path: enabled production scanner or documented launch exception with compensating controls.
- Record any launch exception in the known-risk acceptance log.

Exit criteria:

- Backup and restore evidence is available.
- Rollback and migration evidence is attached.
- Malware scanning is enabled or formally excepted by the product and security owners.

## Phase PR-5 - Content, Claims, And Support Readiness

- Complete high-risk obligation content review.
- Approve high-risk records or hide them from customer-facing production views.
- Confirm all published obligation records include source URL, trigger condition, required actions, evidence examples, confidence, review owner, review state, and last-reviewed date.
- Review all customer-facing claims.
- Remove language implying legal advice, certification, CMMC approval, government endorsement, official assessment success, or permission to store real CUI.
- Finalize support runbooks for prohibited upload, suspected CUI, tenant exposure, access issue, evidence failure, report failure, content correction, security incident, backup restore, and rollback.
- Finalize pilot onboarding materials with No-CUI limits, prohibited data examples, support paths, known limitations, and synthetic demo explanation.
- Prepare launch release notes with posture, scope, exclusions, known risks, support paths, staging smoke results, rollback plan, and content scope.
- Build the known-risk acceptance log.

Exit criteria:

- Customer-facing obligations are source-backed and review-governed.
- Customer-facing copy does not overstate legal, certification, CMMC, government, or CUI-storage capabilities.
- Support, onboarding, release notes, and known-risk artifacts are launch-ready.

## Phase PR-6 - Approval And Launch Candidate

- Obtain product owner approval.
- Obtain engineering lead approval.
- Obtain security owner approval.
- Obtain compliance content owner approval.
- Obtain customer success/support owner approval.
- Obtain legal or contracting advisor approval for customer-facing compliance claims.
- Tag the launch candidate only after evidence and approvals are complete.

Exit criteria:

- Required launch approvals are recorded.
- Launch candidate tag includes release notes, known limitations, support paths, staging evidence, rollback plan, and content scope.

## Phase PR-7 - Production Deployment And Pilot Launch

- Deploy production through the approved CI/CD path.
- Run production smoke tests immediately after deployment.
- Verify login, tenant access, RBAC denial, upload warning and blocking behavior, evidence upload, report generation, audit logging, logs, alerts, and health checks.
- Start controlled MVP launch with pilot customers only.

Exit criteria:

- Production smoke tests pass.
- Operational monitoring and support routing are active.
- Pilot customers are onboarded under the No-CUI data handling boundary.

## Phase PR-8 - Post-Launch Control

- Monitor audit logs, upload blocks, permission denials, report failures, support tickets, and content disputes daily during pilot.
- Hold a post-launch readiness review.
- Convert launch findings into backlog items using Definition of Ready.
- Block Phase 2 Govcon Intelligence until MVP launch controls are stable.

Exit criteria:

- Pilot findings are triaged and assigned.
- Any production readiness regressions have owners, severity, and mitigation plans.
- Phase 2 remains gated until MVP launch controls are stable.

## Hidden Risks

- Malware scanning remains a launch blocker unless enabled or formally excepted.
- Missing restore evidence remains a production blocker.
- Any report that sounds like certification, official approval, or legal advice can block launch.
- Any workflow that bypasses tenant mode enforcement can create an accidental real-CUI intake path.
- Future `CuiReady` capability must remain excluded from production claims until separately approved.

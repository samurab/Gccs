# AGENTS.md - Gccs Project Instructions

Use these instructions at the beginning of every Codex session in this repository.

## Project Summary

Gccs is a Government Contractor Compliance SaaS for small U.S. government contractors.

- The product is multi-tenant SaaS.
- The MVP posture is No-CUI / compliance management only.
- The app supports compliance management, readiness workflows, evidence tracking, obligation tracking, reporting, and auditability.
- The app must not claim to provide CMMC certification, legal advice, accounting advice, labor determinations, or government endorsement.
- Production compliance content must remain source-backed, reviewable, and governed by qualified subject-matter review.

## Architecture Rules

Preserve the existing Clean Architecture boundaries.

- `apps/api`: ASP.NET Core API endpoints, authentication, tenant context, RBAC policies, request validation, response shaping, and API composition.
- `src/Gccs.Application`: use cases, DTOs, ports/interfaces, workflow orchestration, and application services.
- `src/Gccs.Domain`: framework-independent entities, value objects, enums, and domain rules.
- `src/Gccs.Infrastructure`: EF Core persistence, migrations, repository adapters, local seed adapters, and external integrations.
- `apps/web`: React + Vite UI, route shell, user-visible states, client API calls, accessibility, and presentation logic.
- `packages/compliance-content`: source-backed obligation seed content and compliance content metadata.
- `docs`: product, architecture, API, database, governance, and delivery documentation.

Do not put database queries directly in API endpoints. Keep controllers and minimal API handlers thin. Business workflow logic belongs in application services. Persistence belongs in infrastructure repositories. Domain rules should not depend on ASP.NET, EF Core, React, HTTP, database, or cloud SDKs.

## Security Rules

- Tenant isolation is mandatory on every tenant-scoped read, write, export, report, background job, and search query.
- RBAC is mandatory on tenant-scoped actions.
- Never trust a tenant id, user id, role, permission, or ownership value supplied only by the client body.
- Do not leak cross-tenant entity ids, metadata, names, evidence records, report data, audit logs, or errors.
- Prefer `404` for missing or cross-tenant resources unless the existing API standard for that feature uses `403`.
- Do not log secrets, passwords, tokens, credentials, sensitive file contents, or raw customer documents.
- Use the project standard API error contract. Do not expose stack traces to clients.
- Preserve append-only behavior for audit logs and compliance-relevant history.

## Compliance Rules

- Audit-log compliance-relevant actions, including create/update/delete, approval/rejection, upload acceptance/rejection, status changes, exports, policy acknowledgements, failed authorization attempts where supported, and data-handling posture changes.
- Enforce the No-CUI upload policy. The default MVP must reject or warn against CUI, classified information, export-controlled data, ITAR data, and sensitive government-furnished information.
- Do not store file contents in audit logs.
- Preserve evidence traceability, source traceability, review metadata, audit history, tenant boundaries, and compliance content provenance.
- Keep obligation and regulatory content source-backed with source URL, effective/review dates, confidence, and review state where the model supports it.
- Treat AI output as draft-only unless a reviewed workflow explicitly says otherwise. AI-generated compliance content must cite sources and remain reviewable.

## Coding Rules

- Make small, focused, reviewable changes.
- Do not modify unrelated files.
- Follow existing naming conventions, service patterns, repository patterns, DTO patterns, validation patterns, and error handling patterns.
- Prefer simple, explicit, maintainable code over broad abstractions.
- Prefer explicit validation over implicit assumptions.
- Use existing middleware, authorization policies, services, repositories, DTOs, and helpers before introducing new ones.
- Keep frontend screens complete for normal UX states when touched: loading, empty, success, error, and authorization-denied states where applicable.
- For EF Core changes, add migrations when the project uses migrations and keep generated migrations scoped to the model change.

## Backward Compatibility and Regression Prevention

Existing working behavior is a product contract. A new feature, defect fix, refactor, configuration change, or UI redesign must not silently remove, disable, weaken, or alter an existing working flow.

Before changing code:

1. Identify the existing callers, routes, controls, persisted fields, configuration keys, background workers, tests, and documented/UAT flows affected by the change.
2. Record the current expected behavior and determine which behavior must remain compatible.
3. Run the narrowest relevant existing tests before editing when practical. Treat unexpected baseline failures as existing conditions to investigate, not as permission to change the contract.
4. Add or identify a regression test for any previously working behavior that the change could affect. When fixing a regression, reproduce it with a failing test before implementing the fix where practical.
5. Prefer additive changes. Do not rename or remove API fields, routes, roles, permissions, UI actions, configuration keys, storage keys, seeded identities, or database semantics without explicit authorization and a compatibility/migration plan.

During implementation:

1. Preserve existing public and internal contracts unless the requested change explicitly requires a breaking change.
2. Keep security enforcement, tenant isolation, RBAC, audit history, No-CUI controls, token lifecycle, retry behavior, and user identity semantics at least as strong as before.
3. Do not fix one environment by weakening another. Development-only behavior must be explicitly gated and must not alter staging or production authentication or authorization.
4. When a workflow spans UI, API, persistence, email/background processing, or external providers, trace and verify the complete workflow rather than validating only the changed layer.
5. If a breaking change is unavoidable, stop and document the affected behavior, migration steps, rollback path, and required user approval before implementing it.

Before declaring the change complete:

1. Run the new focused tests and the existing tests for adjacent behavior.
2. Build every touched application or project.
3. Perform a smoke test of the original working flow and the new/changed flow using realistic persisted state.
4. Inspect the final diff for unrelated changes, removed behavior, stale copy, configuration drift, and accidental security or authorization changes.
5. Report exactly which regression suites, builds, and smoke tests were run. If a broader suite was not run, disclose that limitation and the remaining regression risk.
6. Do not mark a change complete while a known backward regression remains. Restore compatibility or explicitly obtain approval for the breaking behavior.

## Testing Rules

Add or update focused tests for the risk introduced by the change.

- Tenant isolation tests for tenant-scoped data.
- RBAC tests for protected actions.
- Audit logging tests for compliance-relevant actions.
- No-CUI/data-handling policy enforcement tests for upload or document flows.
- Validation and error handling tests for invalid input, missing resources, conflicts, and unexpected failures where applicable.
- Cross-tenant tests must prove data is not returned, updated, exported, counted, or linked across tenants.
- Empty-state tests must prove tenant-safe endpoints return valid empty responses instead of throwing.
- Cross-field and lifecycle validation tests must cover valid ordering, reversed ordering, equality boundaries, individually omitted optional values, expired/historical values where allowed, and incompatible status/date combinations.
- Rejected-mutation tests must prove that invalid requests do not partially persist data, create audit events, enqueue tasks or notifications, rotate tokens, or trigger external side effects.
- When adding database constraints to existing tables, inspect current persisted data first. Do not silently rewrite ambiguous customer values; use an explicit audited remediation or a staged constraint such as PostgreSQL `NOT VALID`, then track validation of existing rows.
- Regression tests must include realistic UAT values and previously observed failure cases when those cases are safe to retain as synthetic fixtures.
- A UAT test must execute and assert every verb in the acceptance criterion. Visibility of a card does not prove that its detail action works; click the control, verify the API call and response, and assert the resulting content is visible and keyboard-focused.
- For content rendered outside the current viewport, verify discoverability after interaction with scroll/focus behavior and a real-browser test. A DOM-only assertion is insufficient for below-the-fold panels, dialogs, drawers, menus, and route transitions.
- Build a negative and positive permission matrix for each protected resource action. Treat view, generate, export, archive/restore, edit, and delete as separate permissions unless the product contract explicitly combines them.
- Compliance report artifacts are immutable snapshots. Do not add in-place report editing or hard deletion. Administrative lifecycle changes must use explicit archive/restore transitions, require a reason, preserve tenant isolation, and write append-only audit events.
- For idempotent lifecycle endpoints, prove repeated requests do not duplicate audit events or side effects. Also prove rejected, invalid, and cross-tenant requests leave both the resource and audit history unchanged.

Run the narrowest relevant tests first, then broader build/test commands when practical.

## Deep Verification and Hidden-Bug Prevention

Treat acceptance criteria as executable contracts, not prose-only guidance.

1. Decompose each requirement into actor, action, preconditions, expected result, forbidden result, tenant boundary, persisted state, audit event, and external side effects. Record which UI control, API endpoint, application service, repository rule, and automated test proves each applicable part.
2. For authorization, tenant isolation, classification, exports, reports, uploads, approvals, and auditability, test the complete endpoint inventory. A representative endpoint is not sufficient evidence for a security boundary. Every added or changed endpoint must declare machine-readable authorization metadata and be included in an executable endpoint contract or manifest.
3. Keep permissions server-authoritative. The UI must consume explicit server permissions, fail closed when access data is missing or malformed, and hide restricted actions. Never maintain a second client-side role-to-permission matrix.
4. Test both allowed and denied behavior for every affected role and mutation. Denied tests must prove the response contract and prove no report, audit event, related row, notification, job, token change, or external call was created.
5. Compliance-relevant business writes and their audit events must be atomic in one database transaction, or use a proven transactional-outbox design when an external system is involved. Add real-provider fault-injection tests that force audit or outbox failure and prove rollback.
6. Every report, export, background job, and search path must independently enforce tenant scope, RBAC, data-handling mode, and content classification on the source records it consumes. Do not infer safety from UI filters or from a request-level boolean.
7. Trace cross-layer workflows through UI, HTTP, application service, persistence, audit, background processing, and external providers. Unit or mocked-browser tests do not replace a real-stack test for a critical workflow.
8. Maintain separate mocked UI tests and real-stack Playwright tests. CI must run critical UAT personas against the actual API and PostgreSQL schema, including direct API attempts that bypass hidden or disabled controls.
9. Cover lifecycle and boundary states: zero/one/many records, stale and deleted links, cross-tenant identifiers, unknown/prohibited/synthetic classifications, equality boundaries, expired data, retries, duplicate submission, concurrency, cancellation, and partial infrastructure failure.
10. Coverage percentage, test-name substring matching, snapshots, and generated verification documents are supporting signals only. They are never proof that an acceptance criterion is enforced.
11. Verification evidence must identify the commit SHA, environment, database provider, commands, result counts, and execution time. Treat evidence from an older SHA or a mocked-only environment as stale.
12. Deploy to staging only after focused tests, adjacent regression suites, builds, migrations, dependency scans, and real-stack critical-path tests pass. Deploy to production only from the repository's approved launch-candidate tag and manifest after required approvals and staging evidence.
13. Never claim that all bugs are detected. State the verified scope, untested scope, skipped tests, environmental differences, and remaining risks.

## Codex Workflow

Before editing:

1. Inspect the existing project structure.
2. Read this `AGENTS.md`.
3. Read relevant docs, specs, schema/model files, tests, and implementation files.
4. Summarize the current implementation state and the existing behavior that must not regress.
5. Identify the smallest safe change needed and its compatibility surface.
6. State the files you plan to modify and the existing regression tests you will preserve or extend.
7. Wait for confirmation before editing if the change is large, risky, touches many files, or requires a breaking compatibility decision.

During implementation:

1. Preserve tenant isolation, RBAC, audit logging, No-CUI policy, source traceability, and review metadata.
2. Avoid unrelated rewrites and formatting churn.
3. Work with existing uncommitted user changes; do not revert them unless explicitly asked.
4. Keep API responses aligned with the project standard error format.
5. Preserve verified existing behavior and add regression coverage for any working flow placed at risk.

After implementation:

1. Summarize what changed.
2. List files modified.
3. Explain how tenant isolation, RBAC, audit logging, and No-CUI policy were preserved when relevant.
4. List tests added or updated.
5. Provide commands run and commands the user can run locally.
6. Call out hidden risks, edge cases, dependencies, and follow-up work.
7. State which original workflows were re-tested and whether any backward-compatibility limitations remain.

## Key Project References

Read these when relevant instead of duplicating their contents here:

- `README.md`
- `docs/architecture.md`
- `docs/database-models.md`
- `docs/compliance-content-governance.md`
- `docs/security-control-implications.md`
- `docs/product-strategy.md`
- `docs/mvp-execution-plan.md`
- `docs/mvp-roadmap.md`
- `docs/design-flow-diagrams.md`
- `docs/workflow-diagram.md`
- `docs/development-story-prompts.md`
- `docs/development-story-test-cases.md`
- `docs/regression-test-execution-prompts.md`

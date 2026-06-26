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

## Testing Rules

Add or update focused tests for the risk introduced by the change.

- Tenant isolation tests for tenant-scoped data.
- RBAC tests for protected actions.
- Audit logging tests for compliance-relevant actions.
- No-CUI/data-handling policy enforcement tests for upload or document flows.
- Validation and error handling tests for invalid input, missing resources, conflicts, and unexpected failures where applicable.
- Cross-tenant tests must prove data is not returned, updated, exported, counted, or linked across tenants.
- Empty-state tests must prove tenant-safe endpoints return valid empty responses instead of throwing.

Run the narrowest relevant tests first, then broader build/test commands when practical.

## Codex Workflow

Before editing:

1. Inspect the existing project structure.
2. Read this `AGENTS.md`.
3. Read relevant docs, specs, schema/model files, tests, and implementation files.
4. Summarize the current implementation state.
5. Identify the smallest safe change needed.
6. State the files you plan to modify.
7. Wait for confirmation before editing if the change is large, risky, or touches many files.

During implementation:

1. Preserve tenant isolation, RBAC, audit logging, No-CUI policy, source traceability, and review metadata.
2. Avoid unrelated rewrites and formatting churn.
3. Work with existing uncommitted user changes; do not revert them unless explicitly asked.
4. Keep API responses aligned with the project standard error format.

After implementation:

1. Summarize what changed.
2. List files modified.
3. Explain how tenant isolation, RBAC, audit logging, and No-CUI policy were preserved when relevant.
4. List tests added or updated.
5. Provide commands run and commands the user can run locally.
6. Call out hidden risks, edge cases, dependencies, and follow-up work.

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

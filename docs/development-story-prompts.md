# Development Story Implementation Prompts

These prompts are designed to be copied into a fresh implementation thread, one story at a time. Each prompt points back to the source backlog in [development-phase-use-cases.md](development-phase-use-cases.md) and should be executed under the project guidance in [../AGENTS.md](../AGENTS.md).

Phase 0 Stories `0.1` through `0.8` remain intentionally excluded from this implementation inventory. SOC 2 Stories `39.1` through `39.23` form the consolidated, manually gated program below: original governance parents 39.1–39.3 plus scoped engineering, operations and independent-assurance delivery stories. Do not invoke the automatic software story sequence for this section or treat code/templates as SOC 2 completion.

## Evidence-Based Status Reconciliation

Status reconciliation date: **2026-09-09**.

These labels describe the current implementation of each bounded software story. They do not represent production approval, deployment freshness, external-provider availability, independent assessment, certification, or authorization to process real customer CUI.

- `Implemented`: Current source, persistence, API behavior, authorization controls, applicable UI, and automated tests support the story-scoped acceptance criteria. External services still require valid deployment configuration where the story explicitly permits a replaceable adapter, placeholder, or local implementation.
- `Partially implemented`: Meaningful implementation exists, but a material provider, real-stack, deployment, operational-evidence, or approval requirement remains unverified or incomplete.
- `Planned`: No meaningful implementation evidence has been verified.
- `Do not claim`: The outcome is prohibited, unsupported, expired, or controlled by independent authority rather than application code.

Current verification evidence:

- Backend Release regression with PostgreSQL: **1,819 passed, 0 failed, 0 skipped** using `tests/Gccs.Api.Tests/regression.runsettings`.
- Frontend verification: ESLint passed, **204 tests passed across 23 files**, and the production Vite build passed.
- Current source inspection confirmed the Clean Architecture boundaries, EF Core models and migrations, tenant-scoped APIs, server-side permissions, append-only audit behavior, No-CUI enforcement, and story-specific test inventories.
- Historical staging, restore, monitoring, and launch evidence remains useful but is not treated as proof that the current working tree is deployed or approved for broader production use.

Reconciliation summary:

| Story range | Classification | Evidence-based limitation |
| --- | --- | --- |
| `1.1`-`16.3`, except the rows below | Implemented | Story-scoped product behavior is currently covered by source and passing backend/frontend verification. Production scanner, object-storage, and email-provider availability remain deployment dependencies and must not be inferred from story completion. |
| `17.1` | Partially implemented | Automated pilot coverage exists, but a current real-stack browser/API/PostgreSQL/object-storage/scanner pilot run was not re-established in this reconciliation. |
| `17.2` | Implemented | Current regression includes tenant-isolation, RBAC, direct-API, audit, and PostgreSQL boundary tests. This is not an independent penetration test or broader-production approval. |
| `17.3` | Partially implemented | CI/CD and historical staging evidence exist, but the current working tree was not deployed and smoke-tested in staging during this reconciliation. |
| `17.4` | Partially implemented | Checklist and solo-controlled No-CUI pilot evidence exist; broader-production separation-of-duties approval remains absent. |
| `18.1`-`21.3`, `23.1`-`28.3` | Implemented | Story-scoped Phase 2 code and tests pass under the No-CUI posture; production activation still requires the applicable approval posture. |
| `22.1`-`22.3` | Partially implemented | Configuration, replaceable adapters, workflows, persistence, UI, and tests exist, but the dependency register still classifies live SAM.gov/GSA Entity API access as deferred. |
| `1A.1.1`-`1A.9.1` | Implemented | The bounded readiness controls and workflows are implemented; this does not authorize real CUI or establish an operational CUI environment. |
| `1A.9.2`-`1A.9.3` | Partially implemented | Durable verification/readiness workflows exist, but complete current operating evidence, exercised incident response, production storage/scanner proof, and independent approval remain incomplete. |

`Do not claim`: real-CUI authorization, secure or operational CUI storage, CMMC certification or assessment success, FedRAMP authorization/equivalency, government approval or endorsement, legal/labor/accounting determinations, broader-production approval, or independent security assurance. Only authoritative evidence for the exact deployed boundary can change those classifications.

## Shared Prompt Requirements

Use these requirements for every story:

- Treat the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.
- Preserve tenant isolation, RBAC, audit logging, source traceability, and review metadata.
- Follow the existing project structure: React + Vite web app, ASP.NET Core API, application/domain/infrastructure layers, PostgreSQL persistence, and compliance content package.
- Read the referenced story, tasks, and acceptance criteria before editing.
- Keep changes scoped to the story unless a small supporting change is required.
- Add or update tests according to risk, especially for tenant boundaries, authorization, validation, and audit behavior.
- For each implemented user story, add or update backend xUnit tests for .NET behavior and frontend Vitest tests with React Testing Library for user-visible React behavior when that layer is affected.
- Update docs, API contracts, seed content, or UI states when the behavior changes.
- Verify the relevant build/test commands before handing off, or clearly report anything that could not be run.

## 1. Delivery Foundation

### Story 1.1: Repository And Project Structure
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, read the Agile/Scrum plan, API specification, SQL schema, architecture files, and use-case document in this workspace. Then summarize the current implementation state, identify the next Scrum story that should be built, and propose a small implementation plan before editing files.
 Important product rules:

- This is a multi-tenant SaaS.

- Tenant isolation is mandatory.

- RBAC must be enforced on tenant-scoped actions.

- Compliance-relevant events must be audit logged.

- CUI upload policy must be enforced.

- Features should follow the acceptance criteria in the Agile/Scrum plan.

- Add focused tests for tenant isolation, permissions, audit logging, and policy enforcement where relevant.

- Treat the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.

- Preserve tenant isolation, RBAC, audit logging, source traceability, and review metadata.

- Follow the existing project structure: React + Vite web app, ASP.NET Core API, application/domain/infrastructure layers, PostgreSQL persistence, and compliance content package.

- Read the referenced story, tasks, and acceptance criteria before editing.

- Keep changes scoped to the story unless a small supporting change is required.

- Add or update tests according to risk, especially for tenant boundaries, authorization, validation, and audit behavior.

- Update docs, API contracts, seed content, or UI states when the behavior changes.

- Verify the relevant build/test commands before handing off, or clearly report anything that could not be run.


After implementation, run the appropriate xUnit test for .NET and Vitest, paired with the React Testing Library lint/test/build commands, and report what passed or failed.
> Context:
>
> - Epic: Delivery Foundation
>
> - User story: As a technical lead, I want the application structure to separate web, API, application, domain, infrastructure, docs, and compliance content so that development stays maintainable.
>
> - Acceptance criteria:
>
> - A new developer can identify where frontend, backend, domain, persistence, infrastructure, and compliance content live.
> - The solution builds locally with documented commands.
> - No compliance workflow logic is embedded only in the UI.
> - Documentation points to the No-CUI MVP posture with synthetic CUI-ready demonstration workflows.
>
> Implement Story 1.1, "Repository And Project Structure," from `docs/development-phase-use-cases.md`. Confirm and improve the solution organization across `apps/api`, `apps/web`, `src/Gccs.Domain`, `src/Gccs.Application`, `src/Gccs.Infrastructure`, `packages/compliance-content`, `docs`, and `infra`. Update documentation so a new developer understands ownership boundaries, local setup, build commands, and the No-CUI MVP posture with synthetic CUI-ready demonstration workflows. Verify the solution builds cleanly and ensure compliance workflow logic is not trapped only in the UI.

Instructions:

1. Inspect the existing codebase, schema, API spec, and use-case docs before editing.

2. Reuse existing project patterns and avoid broad refactors.

3. Implement the smallest complete vertical slice that satisfies the acceptance criteria.

4. Enforce tenant scoping, RBAC, validation, and audit logging where applicable.

5. Add or update tests for the behavior.

6. Update API/schema/docs only if the implementation changes the contract.

7. Run the relevant verification commands and summarize results.

#-----------------------------------------
### Story 1.2: Local Development Services
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, read the Agile/Scrum plan, API specification, SQL schema, architecture files, and use-case document in this workspace. Then summarize the current implementation state, identify the next Scrum story that should be built, and propose a small implementation plan before editing files.
 Important product rules:

- This is a multi-tenant SaaS.

- Tenant isolation is mandatory.

- RBAC must be enforced on tenant-scoped actions.

- Compliance-relevant events must be audit logged.

- CUI upload policy must be enforced.

- Features should follow the acceptance criteria in the Agile/Scrum plan.

- Add focused tests for tenant isolation, permissions, audit logging, and policy enforcement where relevant.

- Treat the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.

- Preserve tenant isolation, RBAC, audit logging, source traceability, and review metadata.

- Follow the existing project structure: React + Vite web app, ASP.NET Core API, application/domain/infrastructure layers, PostgreSQL persistence, and compliance content package.

- Read the referenced story, tasks, and acceptance criteria before editing.

- Keep changes scoped to the story unless a small supporting change is required.

- Add or update tests according to risk, especially for tenant boundaries, authorization, validation, and audit behavior.

- Update docs, API contracts, seed content, or UI states when the behavior changes.

- Verify the relevant build/test commands before handing off, or clearly report anything that could not be run.


After implementation, run the appropriate xUnit test for .NET and Vitest, paired with the React Testing Library lint/test/build commands, and report what passed or failed.
> Context:
>
> - Epic: Delivery Foundation
>
> - User story: As a developer, I want local database, cache, object storage, and malware-scanning placeholders so that feature work can run against realistic services.
>
> - Acceptance criteria:
>
> - Local services start with one documented command.
> - API can connect to required local dependencies.
> - Missing environment variables produce clear startup errors.
> - Local configuration does not contain production secrets.
>
> Implement Story 1.2, "Local Development Services," from `docs/development-phase-use-cases.md`. Configure or refine local PostgreSQL, Redis, object storage, and malware-scanning placeholder services; add health checks, environment examples, and local reset/migration documentation. Ensure startup failures for missing configuration are clear and that no production secrets are committed.

Instructions:

1. Inspect the existing codebase, schema, API spec, and use-case docs before editing.

2. Reuse existing project patterns and avoid broad refactors.

3. Implement the smallest complete vertical slice that satisfies the acceptance criteria.

4. Enforce tenant scoping, RBAC, validation, and audit logging where applicable.

5. Add or update tests for the behavior.

6. Update API/schema/docs only if the implementation changes the contract.

7. Run the relevant verification commands and summarize results.

#-----------------------------------------

### Story 1.3: Continuous Integration Baseline
**Status: Implemented**
After this implementation the project will have CI implemented and CD planned/documented but not fully implemented yet.


Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, read the Agile/Scrum plan, API specification, SQL schema, architecture files, and use-case document in this workspace. Then summarize the current implementation state, identify the next Scrum story that should be built, and propose a small implementation plan before editing files.
Important product rules:

- This is a multi-tenant SaaS.

- Tenant isolation is mandatory.

- RBAC must be enforced on tenant-scoped actions.

- Compliance-relevant events must be audit logged.

- CUI upload policy must be enforced.

- Features should follow the acceptance criteria in the Agile/Scrum plan.

- Add focused tests for tenant isolation, permissions, audit logging, and policy enforcement where relevant.

- Treat the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.

- Preserve tenant isolation, RBAC, audit logging, source traceability, and review metadata.

- Follow the existing project structure: React + Vite web app, ASP.NET Core API, application/domain/infrastructure layers, PostgreSQL persistence, and compliance content package.

- Read the referenced story, tasks, and acceptance criteria before editing.

- Keep changes scoped to the story unless a small supporting change is required.

- Add or update tests according to risk, especially for tenant boundaries, authorization, validation, and audit behavior.

- Update docs, API contracts, seed content, or UI states when the behavior changes.

- Verify the relevant build/test commands before handing off, or clearly report anything that could not be run.

After implementation, run the appropriate xUnit test for .NET and Vitest, paired with the React Testing Library lint/test/build commands, and report what passed or failed.
> Context:
>
> - Epic: Delivery Foundation
>
> - User story: As a delivery lead, I want automated validation so that broken builds and obvious regressions are caught before review.
>
> - Acceptance criteria:
>
> - Pull requests run automated validation.
> - A failing build, lint, or test step blocks merge.
> - CI logs identify the failing project and step.
> - Security scan failures are visible to reviewers.
>
> Implement Story 1.3, "Continuous Integration Baseline," from `docs/development-phase-use-cases.md`. Add CI validation for dependency restore, backend build, frontend build, linting, unit tests, integration tests, migration validation, and available dependency or secret scans. Make failure output actionable for reviewers and ensure failing validation blocks merge.
Instructions:

1. Inspect the existing codebase, schema, API spec, and use-case docs before editing.

2. Reuse existing project patterns and avoid broad refactors.

3. Implement the smallest complete vertical slice that satisfies the acceptance criteria.

4. Enforce tenant scoping, RBAC, validation, and audit logging where applicable.

5. Add or update tests for the behavior.

6. Update API/schema/docs only if the implementation changes the contract.

7. Run the relevant verification commands and summarize results.


#-----------------------------------------

## 2. Tenant, Identity, And RBAC

### Story 2.1: Tenant Creation
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, read the Agile/Scrum plan, API specification, SQL schema, architecture files, and use-case document in this workspace. Then summarize the current implementation state, identify the next Scrum story that should be built, and propose a small implementation plan before editing files.
Important product rules:

- This is a multi-tenant SaaS.

- Tenant isolation is mandatory.

- RBAC must be enforced on tenant-scoped actions.

- Compliance-relevant events must be audit logged.

- CUI upload policy must be enforced.

- Features should follow the acceptance criteria in the Agile/Scrum plan.

- Add focused tests for tenant isolation, permissions, audit logging, and policy enforcement where relevant.

- Treat the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.

- Preserve tenant isolation, RBAC, audit logging, source traceability, and review metadata.

- Follow the existing project structure: React + Vite web app, ASP.NET Core API, application/domain/infrastructure layers, PostgreSQL persistence, and compliance content package.

- Read the referenced story, tasks, and acceptance criteria before editing.

- Keep changes scoped to the story unless a small supporting change is required.

- Add or update tests according to risk, especially for tenant boundaries, authorization, validation, and audit behavior.

- Update docs, API contracts, seed content, or UI states when the behavior changes.

- Verify the relevant build/test commands before handing off, or clearly report anything that could not be run.

After implementation, run the appropriate xUnit test for .NET and Vitest, paired with the React Testing Library lint/test/build commands, and report what passed or failed.

> Context:
>
> - Epic: Tenant, Identity, And RBAC
>
> - User story: As a platform admin, I want to create a tenant so that a customer organization can use GCCS in an isolated workspace.
>
> - Acceptance criteria:
>
> - Tenant has unique ID, display name, status, created date, and updated date.
> - Tenant-owned records include tenant ID.
> - A user from one tenant cannot retrieve another tenant's data through API calls.
> - Tenant creation and status changes are audit logged.
>
> Implement Story 2.1, "Tenant Creation," from `docs/development-phase-use-cases.md`. Add the tenant persistence model, API contract, tenant status values, tenant-owned entity scoping, tenant filtering in repositories/services, initial tenant creation support, and audit logging for tenant creation/status changes. Add tests proving one tenant cannot retrieve another tenant's data.

Instructions:

1. Inspect the existing codebase, schema, API spec, and use-case docs before editing.

2. Reuse existing project patterns and avoid broad refactors.

3. Implement the smallest complete vertical slice that satisfies the acceptance criteria.

4. Enforce tenant scoping, RBAC, validation, and audit logging where applicable.

5. Add or update tests for the behavior.

6. Update API/schema/docs only if the implementation changes the contract.

7. Run the relevant verification commands and summarize results.

#-----------------------------------------

### Story 2.2: User Memberships
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, read the Agile/Scrum plan, API specification, SQL schema, architecture files, and use-case document in this workspace. Then summarize the current implementation state, identify the next Scrum story that should be built, and propose a small implementation plan before editing files.
 Important product rules:

- This is a multi-tenant SaaS.

- Tenant isolation is mandatory.

- RBAC must be enforced on tenant-scoped actions.

- Compliance-relevant events must be audit logged.

- CUI upload policy must be enforced.

- Features should follow the acceptance criteria in the Agile/Scrum plan.

- Add focused tests for tenant isolation, permissions, audit logging, and policy enforcement where relevant.

- Treat the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.

- Preserve tenant isolation, RBAC, audit logging, source traceability, and review metadata.

- Follow the existing project structure: React + Vite web app, ASP.NET Core API, application/domain/infrastructure layers, PostgreSQL persistence, and compliance content package.

- Read the referenced story, tasks, and acceptance criteria before editing.

- Keep changes scoped to the story unless a small supporting change is required.

- Add or update tests according to risk, especially for tenant boundaries, authorization, validation, and audit behavior.

- Update docs, API contracts, seed content, or UI states when the behavior changes.

- Verify the relevant build/test commands before handing off, or clearly report anything that could not be run.

After implementation, run the appropriate xUnit test for .NET and Vitest, paired with the React Testing Library lint/test/build commands, and report what passed or failed.

> Context:
>
> - Epic: Tenant, Identity, And RBAC
>
> - User story: As a tenant admin, I want to add team members to my tenant so that multiple people can work in the same compliance workspace.
>
> - Acceptance criteria:
>
> - A user can belong to one or more tenants when explicitly assigned.
> - Tenant member list only shows users in the current tenant.
> - Duplicate membership creation is rejected.
> - Membership changes are audit logged.
>
> Implement Story 2.2, "User Memberships," from `docs/development-phase-use-cases.md`. Model users, tenant memberships, membership status, duplicate prevention, tenant-scoped member listing, and UI for viewing members. Ensure membership changes are audit logged and users only see memberships for the active tenant.

Instructions:

1. Inspect the existing codebase, schema, API spec, and use-case docs before editing.

2. Reuse existing project patterns and avoid broad refactors.

3. Implement the smallest complete vertical slice that satisfies the acceptance criteria.

4. Enforce tenant scoping, RBAC, validation, and audit logging where applicable.

5. Add or update tests for the behavior.

6. Update API/schema/docs only if the implementation changes the contract.

7. Run the relevant verification commands and summarize results.

#-----------------------------------------

### Story 2.3: User Invitations
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, read the Agile/Scrum plan, API specification, SQL schema, architecture files, and use-case document in this workspace. Then summarize the current implementation state, identify the next Scrum story that should be built, and propose a small implementation plan before editing files.
 Important product rules:

- This is a multi-tenant SaaS.

- Tenant isolation is mandatory.

- RBAC must be enforced on tenant-scoped actions.

- Compliance-relevant events must be audit logged.

- CUI upload policy must be enforced.

- Features should follow the acceptance criteria in the Agile/Scrum plan.

- Add focused tests for tenant isolation, permissions, audit logging, and policy enforcement where relevant.

- Treat the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.

- Preserve tenant isolation, RBAC, audit logging, source traceability, and review metadata.

- Follow the existing project structure: React + Vite web app, ASP.NET Core API, application/domain/infrastructure layers, PostgreSQL persistence, and compliance content package.

- Read the referenced story, tasks, and acceptance criteria before editing.

- Keep changes scoped to the story unless a small supporting change is required.

- Add or update tests according to risk, especially for tenant boundaries, authorization, validation, and audit behavior.

- Update docs, API contracts, seed content, or UI states when the behavior changes.

- Verify the relevant build/test commands before handing off, or clearly report anything that could not be run.

After implementation, run the appropriate xUnit test for .NET and Vitest, paired with the React Testing Library lint/test/build commands, and report what passed or failed.

> Context:
>
> - Epic: Tenant, Identity, And RBAC
>
> - User story: As a tenant admin, I want to invite users by email and role so that onboarding is controlled.
>
> - Acceptance criteria:
>
> - Admin can invite a user by email and role.
> - Invitations have pending, accepted, expired, and revoked states.
> - Expired or revoked invitations cannot be accepted.
> - Non-admin users cannot invite users.
> - Invitation actions are audit logged.
>
> Implement Story 2.3, "User Invitations," from `docs/development-phase-use-cases.md`. Add invitation tokens, role assignment, expiration, pending/accepted/expired/revoked states, create/accept/expire/revoke workflows, a local email or notification placeholder, and invitation UI states. Enforce admin-only invitation creation and audit all invitation actions.

Instructions:

1. Inspect the existing codebase, schema, API spec, and use-case docs before editing.

2. Reuse existing project patterns and avoid broad refactors.

3. Implement the smallest complete vertical slice that satisfies the acceptance criteria.

4. Enforce tenant scoping, RBAC, validation, and audit logging where applicable.

5. Add or update tests for the behavior.

6. Update API/schema/docs only if the implementation changes the contract.

7. Run the relevant verification commands and summarize results.

#-----------------------------------------

### Story 2.4: Role-Based Permissions
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, read the Agile/Scrum plan, API specification, SQL schema, architecture files, and use-case document in this workspace. Then summarize the current implementation state, identify the next Scrum story that should be built, and propose a small implementation plan before editing files.
Important product rules:

- This is a multi-tenant SaaS.

- Tenant isolation is mandatory.

- RBAC must be enforced on tenant-scoped actions.

- Compliance-relevant events must be audit logged.

- CUI upload policy must be enforced.

- Features should follow the acceptance criteria in the Agile/Scrum plan.

- Add focused tests for tenant isolation, permissions, audit logging, and policy enforcement where relevant.

- Treat the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.

- Preserve tenant isolation, RBAC, audit logging, source traceability, and review metadata.

- Follow the existing project structure: React + Vite web app, ASP.NET Core API, application/domain/infrastructure layers, PostgreSQL persistence, and compliance content package.

- Read the referenced story, tasks, and acceptance criteria before editing.

- Keep changes scoped to the story unless a small supporting change is required.

- Add or update tests according to risk, especially for tenant boundaries, authorization, validation, and audit behavior.

- Update docs, API contracts, seed content, or UI states when the behavior changes.

- Verify the relevant build/test commands before handing off, or clearly report anything that could not be run.

After implementation, run the appropriate xUnit test for .NET and Vitest, paired with the React Testing Library lint/test/build commands, and report what passed or failed.

> Context:
>
> - Epic: Tenant, Identity, And RBAC
>
> - User story: As a tenant admin, I want role-based permissions so that users only access workflows appropriate for their responsibilities.
>
> - Acceptance criteria:
>
> - Restricted actions are denied server-side even if called directly.
> - UI only shows actions the current role can perform.
> - Permission failures return a clear error.
> - Auditor users can view approved evidence packages but cannot modify tenant data.
> - RBAC decisions are covered by tests.
>
> Implement Story 2.4, "Role-Based Permissions," from `docs/development-phase-use-cases.md`. Define owner, admin, compliance manager, contributor, auditor, and advisor roles; map permissions across profile, contracts, obligations, tasks, evidence, reports, subcontractors, and admin actions; enforce authorization server-side; hide restricted UI actions; and add permission tests, including auditor read-only behavior.

Instructions:

1. Inspect the existing codebase, schema, API spec, and use-case docs before editing.

2. Reuse existing project patterns and avoid broad refactors.

3. Implement the smallest complete vertical slice that satisfies the acceptance criteria.

4. Enforce tenant scoping, RBAC, validation, and audit logging where applicable.

5. Add or update tests for the behavior.

6. Update API/schema/docs only if the implementation changes the contract.

7. Run the relevant verification commands and summarize results.

#-----------------------------------------

## 3. Authenticated Application Shell

### Story 3.1: Protected API Access
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, read the Agile/Scrum plan, API specification, SQL schema, architecture files, and use-case document in this workspace. Then summarize the current implementation state, identify the next Scrum story that should be built, and propose a small implementation plan before editing files.
 Important product rules:

- This is a multi-tenant SaaS.

- Tenant isolation is mandatory.

- RBAC must be enforced on tenant-scoped actions.

- Compliance-relevant events must be audit logged.

- CUI upload policy must be enforced.

- Features should follow the acceptance criteria in the Agile/Scrum plan.

- Add focused tests for tenant isolation, permissions, audit logging, and policy enforcement where relevant.

- Treat the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.

- Preserve tenant isolation, RBAC, audit logging, source traceability, and review metadata.

- Follow the existing project structure: React + Vite web app, ASP.NET Core API, application/domain/infrastructure layers, PostgreSQL persistence, and compliance content package.

- Read the referenced story, tasks, and acceptance criteria before editing.

- Keep changes scoped to the story unless a small supporting change is required.

- Add or update tests according to risk, especially for tenant boundaries, authorization, validation, and audit behavior.

- Update docs, API contracts, seed content, or UI states when the behavior changes.

- Verify the relevant build/test commands before handing off, or clearly report anything that could not be run.

After implementation, run the appropriate xUnit test for .NET and Vitest, paired with the React Testing Library lint/test/build commands, and report what passed or failed.

> Context:
>
> - Epic: Authenticated Application Shell
>
> - User story: As a developer, I want authenticated API calls to include tenant and user context so that all workflows are scoped correctly.
>
> - Acceptance criteria:
>
> - Protected endpoints reject unauthenticated requests.
> - API handlers can access current tenant and user context.
> - Missing tenant context returns a clear error.
> - API errors use a consistent response shape.
>
> Implement Story 3.1, "Protected API Access," from `docs/development-phase-use-cases.md`. Add authentication middleware or a development auth shim, current tenant/user resolution, consistent API error responses, and request correlation IDs. Protected endpoints must reject unauthenticated requests and return clear errors when tenant context is missing.

Instructions:

1. Inspect the existing codebase, schema, API spec, and use-case docs before editing.

2. Reuse existing project patterns and avoid broad refactors.

3. Implement the smallest complete vertical slice that satisfies the acceptance criteria.

4. Enforce tenant scoping, RBAC, validation, and audit logging where applicable.

5. Add or update tests for the behavior.

6. Update API/schema/docs only if the implementation changes the contract.

7. Run the relevant verification commands and summarize results.

#-----------------------------------------

### Story 3.2: SaaS Navigation Shell
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, read the Agile/Scrum plan, API specification, SQL schema, architecture files, and use-case document in this workspace. Then summarize the current implementation state, identify the next Scrum story that should be built, and propose a small implementation plan before editing files.
 Important product rules:

- This is a multi-tenant SaaS.

- Tenant isolation is mandatory.

- RBAC must be enforced on tenant-scoped actions.

- Compliance-relevant events must be audit logged.

- CUI upload policy must be enforced.

- Features should follow the acceptance criteria in the Agile/Scrum plan.

- Add focused tests for tenant isolation, permissions, audit logging, and policy enforcement where relevant.

- Treat the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.

- Preserve tenant isolation, RBAC, audit logging, source traceability, and review metadata.

- Follow the existing project structure: React + Vite web app, ASP.NET Core API, application/domain/infrastructure layers, PostgreSQL persistence, and compliance content package.

- Read the referenced story, tasks, and acceptance criteria before editing.

- Keep changes scoped to the story unless a small supporting change is required.

- Add or update tests according to risk, especially for tenant boundaries, authorization, validation, and audit behavior.

- Update docs, API contracts, seed content, or UI states when the behavior changes.

- Verify the relevant build/test commands before handing off, or clearly report anything that could not be run.

After implementation, run the appropriate xUnit test for .NET and Vitest, paired with the React Testing Library lint/test/build commands, and report what passed or failed.

> Context:
>
> - Epic: Authenticated Application Shell
>
> - User story: As a user, I want clear navigation so that I can access each MVP workflow without hunting through the interface.
>
> - Acceptance criteria:
>
> - Authenticated users land in the workspace, not a marketing page.
> - Navigation is keyboard accessible.
> - Restricted navigation items are hidden for roles without access.
> - Empty and error states are visible and understandable.
>
> Implement Story 3.2, "SaaS Navigation Shell," from `docs/development-phase-use-cases.md`. Build the authenticated workspace layout with route placeholders for dashboard, profile, contracts, obligations, calendar, evidence, CMMC, subcontractors, reports, and settings. Include keyboard-accessible, role-aware navigation plus loading, empty, and error states.

Instructions:

1. Inspect the existing codebase, schema, API spec, and use-case docs before editing.

2. Reuse existing project patterns and avoid broad refactors.

3. Implement the smallest complete vertical slice that satisfies the acceptance criteria.

4. Enforce tenant scoping, RBAC, validation, and audit logging where applicable.

5. Add or update tests for the behavior.

6. Update API/schema/docs only if the implementation changes the contract.

7. Run the relevant verification commands and summarize results.

#-----------------------------------------

## 4. CUI-Ready Gated Controls

### Story 4.1: Data Handling Acknowledgement
**Status: Implemented**

Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, read the Agile/Scrum plan, API specification, SQL schema, architecture files, and use-case document in this workspace. Then summarize the current implementation state, identify the next Scrum story that should be built, and propose a small implementation plan before editing files.
 Important product rules:

- This is a multi-tenant SaaS.

- Tenant isolation is mandatory.

- RBAC must be enforced on tenant-scoped actions.

- Compliance-relevant events must be audit logged.

- CUI upload policy must be enforced.

- Features should follow the acceptance criteria in the Agile/Scrum plan.

- Add focused tests for tenant isolation, permissions, audit logging, and policy enforcement where relevant.

- Treat the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.

- Preserve tenant isolation, RBAC, audit logging, source traceability, and review metadata.

- Follow the existing project structure: React + Vite web app, ASP.NET Core API, application/domain/infrastructure layers, PostgreSQL persistence, and compliance content package.

- Read the referenced story, tasks, and acceptance criteria before editing.

- Keep changes scoped to the story unless a small supporting change is required.

- Add or update tests according to risk, especially for tenant boundaries, authorization, validation, and audit behavior.

- Update docs, API contracts, seed content, or UI states when the behavior changes.

- Verify the relevant build/test commands before handing off, or clearly report anything that could not be run.

After implementation, run the appropriate xUnit test for .NET and Vitest, paired with the React Testing Library lint/test/build commands, and report what passed or failed.

> Context:
>
> - Epic: CUI-Ready Gated Controls
>
> - User story: As a user, I want to understand the upload limitation before using the product so that I know what content is prohibited.
>
> - Acceptance criteria:
>
> - User sees a data handling notice before first upload.
> - User must acknowledge the notice before upload is enabled.
> - Acknowledgement is audit logged.
> - Notice copy states that the MVP supports synthetic CUI-ready demonstration workflows and that real CUI upload requires approved future `CuiReady` tenant status.
>
> Implement Story 4.1, "Data Handling Acknowledgement," from `docs/development-phase-use-cases.md`. Add data handling notice content to onboarding and upload flows, require acknowledgement before upload, store acknowledgement by user/tenant/timestamp/notice version, expose acknowledgement status, and audit the acknowledgement. The copy must clearly state the MVP supports synthetic CUI-ready demonstration workflows and that real CUI upload requires approved future `CuiReady` tenant status.

Instructions:

1. Inspect the existing codebase, schema, API spec, and use-case docs before editing.

2. Reuse existing project patterns and avoid broad refactors.

3. Implement the smallest complete vertical slice that satisfies the acceptance criteria.

4. Enforce tenant scoping, RBAC, validation, and audit logging where applicable.

5. Add or update tests for the behavior.

6. Update API/schema/docs only if the implementation changes the contract.

7. Run the relevant verification commands and summarize results.

#-----------------------------------------

### Story 4.2: Upload Guardrails
**Status: Implemented**

Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, read the Agile/Scrum plan, API specification, SQL schema, architecture files, and use-case document in this workspace. Then summarize the current implementation state, identify the next Scrum story that should be built, and propose a small implementation plan before editing files.
 Important product rules:

- This is a multi-tenant SaaS.

- Tenant isolation is mandatory.

- RBAC must be enforced on tenant-scoped actions.

- Compliance-relevant events must be audit logged.

- CUI upload policy must be enforced.

- Features should follow the acceptance criteria in the Agile/Scrum plan.

- Add focused tests for tenant isolation, permissions, audit logging, and policy enforcement where relevant.

- Treat the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.

- Preserve tenant isolation, RBAC, audit logging, source traceability, and review metadata.

- Follow the existing project structure: React + Vite web app, ASP.NET Core API, application/domain/infrastructure layers, PostgreSQL persistence, and compliance content package.

- Read the referenced story, tasks, and acceptance criteria before editing.

- Keep changes scoped to the story unless a small supporting change is required.

- Add or update tests according to risk, especially for tenant boundaries, authorization, validation, and audit behavior.

- Update docs, API contracts, seed content, or UI states when the behavior changes.

- Verify the relevant build/test commands before handing off, or clearly report anything that could not be run.

After implementation, run the appropriate xUnit test for .NET and Vitest, paired with the React Testing Library lint/test/build commands, and report what passed or failed.

> Context:
>
> - Epic: CUI-Ready Gated Controls
>
> - User story: As a security lead, I want upload controls so that prohibited or risky files are blocked early.
>
> - Acceptance criteria:
>
> - Disallowed file types are rejected server-side.
> - Oversized files are rejected server-side.
> - Upload metadata records scan status.
> - Failed scans or validation failures do not create usable evidence.
> - Upload failures are audit logged.
>
> Implement Story 4.2, "Upload Guardrails," from `docs/development-phase-use-cases.md`. Add allowed file type and size validation server-side, malware scan status placeholder, rejected upload messages, and tests for file type and size handling. Failed validation or scan states must not create usable evidence, and upload failures must be audit logged.

Instructions:

1. Inspect the existing codebase, schema, API spec, and use-case docs before editing.

2. Reuse existing project patterns and avoid broad refactors.

3. Implement the smallest complete vertical slice that satisfies the acceptance criteria.

4. Enforce tenant scoping, RBAC, validation, and audit logging where applicable.

5. Add or update tests for the behavior.

6. Update API/schema/docs only if the implementation changes the contract.

7. Run the relevant verification commands and summarize results.

#-----------------------------------------

## 5. Audit Logging

### Story 5.1: Append-Only Audit Events
**Status: Implemented**

Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, read the Agile/Scrum plan, API specification, SQL schema, architecture files, and use-case document in this workspace. Then summarize the current implementation state, identify the next Scrum story that should be built, and propose a small implementation plan before editing files.
 Important product rules:

- This is a multi-tenant SaaS.

- Tenant isolation is mandatory.

- RBAC must be enforced on tenant-scoped actions.

- Compliance-relevant events must be audit logged.

- CUI upload policy must be enforced.

- Features should follow the acceptance criteria in the Agile/Scrum plan.

- Add focused tests for tenant isolation, permissions, audit logging, and policy enforcement where relevant.

- Treat the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.

- Preserve tenant isolation, RBAC, audit logging, source traceability, and review metadata.

- Follow the existing project structure: React + Vite web app, ASP.NET Core API, application/domain/infrastructure layers, PostgreSQL persistence, and compliance content package.

- Read the referenced story, tasks, and acceptance criteria before editing.

- Keep changes scoped to the story unless a small supporting change is required.

- Add or update tests according to risk, especially for tenant boundaries, authorization, validation, and audit behavior.

- Update docs, API contracts, seed content, or UI states when the behavior changes.

- Verify the relevant build/test commands before handing off, or clearly report anything that could not be run.

After implementation, run the appropriate xUnit test for .NET and Vitest, paired with the React Testing Library lint/test/build commands, and report what passed or failed.

> Context:
>
> - Epic: Audit Logging
>
> - User story: As a technical lead, I want append-only audit events so that important compliance actions cannot be silently overwritten.
>
> - Acceptance criteria:
>
> - Sensitive actions create audit events.
> - Audit events include tenant ID and actor ID.
> - Audit events are not editable through normal application APIs.
> - Audit failures are surfaced for critical actions.
>
> Implement Story 5.1, "Append-Only Audit Events," from `docs/development-phase-use-cases.md`. Model append-only audit events with tenant, actor, action, entity type, entity ID, timestamp, request metadata, and before/after summaries where useful. Add an application-level audit writer, protect events from normal editing, surface critical audit failures, and test audit creation for sensitive actions.

Instructions:

1. Inspect the existing codebase, schema, API spec, and use-case docs before editing.

2. Reuse existing project patterns and avoid broad refactors.

3. Implement the smallest complete vertical slice that satisfies the acceptance criteria.

4. Enforce tenant scoping, RBAC, validation, and audit logging where applicable.

5. Add or update tests for the behavior.

6. Update API/schema/docs only if the implementation changes the contract.

7. Run the relevant verification commands and summarize results.

#-----------------------------------------

### Story 5.2: Audit Log Viewer
**Status: Implemented**

Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, read the Agile/Scrum plan, API specification, SQL schema, architecture files, and use-case document in this workspace. Then summarize the current implementation state, identify the next Scrum story that should be built, and propose a small implementation plan before editing files.
 Important product rules:

- This is a multi-tenant SaaS.

- Tenant isolation is mandatory.

- RBAC must be enforced on tenant-scoped actions.

- Compliance-relevant events must be audit logged.

- CUI upload policy must be enforced.

- Features should follow the acceptance criteria in the Agile/Scrum plan.

- Add focused tests for tenant isolation, permissions, audit logging, and policy enforcement where relevant.

- Treat the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.

- Preserve tenant isolation, RBAC, audit logging, source traceability, and review metadata.

- Follow the existing project structure: React + Vite web app, ASP.NET Core API, application/domain/infrastructure layers, PostgreSQL persistence, and compliance content package.

- Read the referenced story, tasks, and acceptance criteria before editing.

- Keep changes scoped to the story unless a small supporting change is required.

- Add or update tests according to risk, especially for tenant boundaries, authorization, validation, and audit behavior.

- Update docs, API contracts, seed content, or UI states when the behavior changes.

- Verify the relevant build/test commands before handing off, or clearly report anything that could not be run.

After implementation, run the appropriate xUnit test for .NET and Vitest, paired with the React Testing Library lint/test/build commands, and report what passed or failed.
> Context:
>
> - Epic: Audit Logging
>
> - User story: As a tenant admin, I want to view audit events so that I can investigate compliance and access activity.
>
> - Acceptance criteria:
>
> - Admins can view audit events for their tenant only.
> - Non-authorized users cannot access audit logs.
> - Audit list supports pagination.
> - Filters return correct tenant-scoped results.
>
> Implement Story 5.2, "Audit Log Viewer," from `docs/development-phase-use-cases.md`. Add a tenant-scoped audit log query endpoint with pagination and filters, plus a UI table showing date, actor, action, entity, and summary. Restrict access to configured admin/owner/advisor roles and test that unauthorized users cannot access audit logs.

Instructions:

1. Inspect the existing codebase, schema, API spec, and use-case docs before editing.

2. Reuse existing project patterns and avoid broad refactors.

3. Implement the smallest complete vertical slice that satisfies the acceptance criteria.

4. Enforce tenant scoping, RBAC, validation, and audit logging where applicable.

5. Add or update tests for the behavior.

6. Update API/schema/docs only if the implementation changes the contract.

7. Run the relevant verification commands and summarize results.

#-----------------------------------------

## 6. Compliance Content Foundation

### Story 6.1: Obligation Schema
**Status: Implemented**

Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, read the Agile/Scrum plan, API specification, SQL schema, architecture files, and use-case document in this workspace. Then summarize the current implementation state, identify the next Scrum story that should be built, and propose a small implementation plan before editing files.
 Important product rules:

- This is a multi-tenant SaaS.

- Tenant isolation is mandatory.

- RBAC must be enforced on tenant-scoped actions.

- Compliance-relevant events must be audit logged.

- CUI upload policy must be enforced.

- Features should follow the acceptance criteria in the Agile/Scrum plan.

- Add focused tests for tenant isolation, permissions, audit logging, and policy enforcement where relevant.

- Treat the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.

- Preserve tenant isolation, RBAC, audit logging, source traceability, and review metadata.

- Follow the existing project structure: React + Vite web app, ASP.NET Core API, application/domain/infrastructure layers, PostgreSQL persistence, and compliance content package.

- Read the referenced story, tasks, and acceptance criteria before editing.

- Keep changes scoped to the story unless a small supporting change is required.

- Add or update tests according to risk, especially for tenant boundaries, authorization, validation, and audit behavior.

- Update docs, API contracts, seed content, or UI states when the behavior changes.

- Verify the relevant build/test commands before handing off, or clearly report anything that could not be run.

After implementation, run the appropriate xUnit test for .NET and Vitest, paired with the React Testing Library lint/test/build commands, and report what passed or failed.

> Context:
>
> - Epic: Compliance Content Foundation
>
> - User story: As a compliance content owner, I want every obligation to follow a structured schema so that content is consistent and reviewable.
>
> - Acceptance criteria:
>
> - Obligation records cannot be published without source URL.
> - Obligation records cannot be published without last reviewed date.
> - Obligation records identify risk, owner, confidence, and review state.
> - Evidence examples can be linked to obligations.
>
> Implement Story 6.1, "Obligation Schema," from `docs/development-phase-use-cases.md`. Define or refine clause, source reference, obligation, evidence example, applicability dimension, and review metadata models. Enforce required source URL, last reviewed date, trigger logic, required actions, owner, risk, confidence, flow-down requirement, and expert review metadata before publication, with tests for invalid content.

Instructions:

1. Inspect the existing codebase, schema, API spec, and use-case docs before editing.

2. Reuse existing project patterns and avoid broad refactors.

3. Implement the smallest complete vertical slice that satisfies the acceptance criteria.

4. Enforce tenant scoping, RBAC, validation, and audit logging where applicable.

5. Add or update tests for the behavior.

6. Update API/schema/docs only if the implementation changes the contract.

7. Run the relevant verification commands and summarize results.

#-----------------------------------------

### Story 6.2: Content Import
**Status: Implemented**

Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, read the Agile/Scrum plan, API specification, SQL schema, architecture files, and use-case document in this workspace. Then summarize the current implementation state, identify the next Scrum story that should be built, and propose a small implementation plan before editing files.
Important product rules:

- This is a multi-tenant SaaS.

- Tenant isolation is mandatory.

- RBAC must be enforced on tenant-scoped actions.

- Compliance-relevant events must be audit logged.

- CUI upload policy must be enforced.

- Features should follow the acceptance criteria in the Agile/Scrum plan.

- Add focused tests for tenant isolation, permissions, audit logging, and policy enforcement where relevant.

- Treat the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.

- Preserve tenant isolation, RBAC, audit logging, source traceability, and review metadata.

- Follow the existing project structure: React + Vite web app, ASP.NET Core API, application/domain/infrastructure layers, PostgreSQL persistence, and compliance content package.

- Read the referenced story, tasks, and acceptance criteria before editing.

- Keep changes scoped to the story unless a small supporting change is required.

- Add or update tests according to risk, especially for tenant boundaries, authorization, validation, and audit behavior.

- Update docs, API contracts, seed content, or UI states when the behavior changes.

- Verify the relevant build/test commands before handing off, or clearly report anything that could not be run.

After implementation, run the appropriate xUnit test for .NET and Vitest, paired with the React Testing Library lint/test/build commands, and report what passed or failed.

> Context:
>
> - Epic: Compliance Content Foundation
>
> - User story: As a developer, I want to load curated obligation content so that the app has useful MVP data.
>
> - Acceptance criteria:
>
> - Valid content imports successfully.
> - Invalid content fails with actionable errors.
> - Re-running import does not create duplicate records.
> - Imported obligations retain source and review metadata.
>
> Implement Story 6.2, "Content Import," from `docs/development-phase-use-cases.md`. Create or improve the seed/import process for `packages/compliance-content`, validate JSON schema before import, make imports idempotent, and add import logs/failure reporting. Verify valid content imports, invalid content fails clearly, and source/review metadata is preserved.

Instructions:

1. Inspect the existing codebase, schema, API spec, and use-case docs before editing.

2. Reuse existing project patterns and avoid broad refactors.

3. Implement the smallest complete vertical slice that satisfies the acceptance criteria.

4. Enforce tenant scoping, RBAC, validation, and audit logging where applicable.

5. Add or update tests for the behavior.

6. Update API/schema/docs only if the implementation changes the contract.

7. Run the relevant verification commands and summarize results.

#-----------------------------------------

### Story 6.3: Content Review State
**Status: Implemented**

Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, read the Agile/Scrum plan, API specification, SQL schema, architecture files, and use-case document in this workspace. Then summarize the current implementation state, identify the next Scrum story that should be built, and propose a small implementation plan before editing files.
Important product rules:

- This is a multi-tenant SaaS.

- Tenant isolation is mandatory.

- RBAC must be enforced on tenant-scoped actions.

- Compliance-relevant events must be audit logged.

- CUI upload policy must be enforced.

- Features should follow the acceptance criteria in the Agile/Scrum plan.

- Add focused tests for tenant isolation, permissions, audit logging, and policy enforcement where relevant.

- Treat the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.

- Preserve tenant isolation, RBAC, audit logging, source traceability, and review metadata.

- Follow the existing project structure: React + Vite web app, ASP.NET Core API, application/domain/infrastructure layers, PostgreSQL persistence, and compliance content package.

- Read the referenced story, tasks, and acceptance criteria before editing.

- Keep changes scoped to the story unless a small supporting change is required.

- Add or update tests according to risk, especially for tenant boundaries, authorization, validation, and audit behavior.

- Update docs, API contracts, seed content, or UI states when the behavior changes.

- Verify the relevant build/test commands before handing off, or clearly report anything that could not be run.

After implementation, run the appropriate xUnit test for .NET and Vitest, paired with the React Testing Library lint/test/build commands, and report what passed or failed.

> Context:
>
> - Epic: Compliance Content Foundation
>
> - User story: As a compliance content owner, I want review states so that draft content is not accidentally shown as published guidance.
>
> - Acceptance criteria:
>
> - Draft content is hidden from customer-facing obligation views.
> - Expert-review-required content cannot be published without reviewer and date.
> - Retired content is no longer used for new mappings.
> - Content state changes are audit logged.
>
> Implement Story 6.3, "Content Review State," from `docs/development-phase-use-cases.md`. Add draft, in_review, approved, published, and retired states; restrict customer-facing views to published content; require reviewer/date for expert-review-required publication; support retiring content; and audit content state changes.

Instructions:

1. Inspect the existing codebase, schema, API spec, and use-case docs before editing.

2. Reuse existing project patterns and avoid broad refactors.

3. Implement the smallest complete vertical slice that satisfies the acceptance criteria.

4. Enforce tenant scoping, RBAC, validation, and audit logging where applicable.

5. Add or update tests for the behavior.

6. Update API/schema/docs only if the implementation changes the contract.

7. Run the relevant verification commands and summarize results.

#-----------------------------------------

## 7. Company Compliance Profile

### Story 7.1: Create Company Profile
**Status: Implemented**

Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, read the Agile/Scrum plan, API specification, SQL schema, architecture files, and use-case document in this workspace. Then summarize the current implementation state, identify the next Scrum story that should be built, and propose a small implementation plan before editing files.
Important product rules:

- This is a multi-tenant SaaS.

- Tenant isolation is mandatory.

- RBAC must be enforced on tenant-scoped actions.

- Compliance-relevant events must be audit logged.

- CUI upload policy must be enforced.

- Features should follow the acceptance criteria in the Agile/Scrum plan.

- Add focused tests for tenant isolation, permissions, audit logging, and policy enforcement where relevant.

- Treat the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.

- Preserve tenant isolation, RBAC, audit logging, source traceability, and review metadata.

- Follow the existing project structure: React + Vite web app, ASP.NET Core API, application/domain/infrastructure layers, PostgreSQL persistence, and compliance content package.

- Read the referenced story, tasks, and acceptance criteria before editing.

- Keep changes scoped to the story unless a small supporting change is required.

- Add or update tests according to risk, especially for tenant boundaries, authorization, validation, and audit behavior.

- Update docs, API contracts, seed content, or UI states when the behavior changes.

- Verify the relevant build/test commands before handing off, or clearly report anything that could not be run.

After implementation, run the appropriate xUnit test for .NET and Vitest, paired with the React Testing Library lint/test/build commands, and report what passed or failed.

> Context:
>
> - Epic: Company Compliance Profile
>
> - User story: As a compliance manager, I want to create a company profile so that GCCS understands my business context.
>
> - Acceptance criteria:
>
> - Required fields are validated before profile completion.
> - Profile can be saved as draft when non-critical fields are missing.
> - Profile shows completion percentage.
> - Profile changes are audit logged.
>
> Implement Story 7.1, "Create Company Profile," from `docs/development-phase-use-cases.md`. Build the API and UI for company profile creation and update, including legal entity name, UEI, CAGE code, SAM expiration, NAICS, SBA size status, certifications, agency customers, role, products/services, employee and revenue ranges, locations, IT summary, and FCI/CUI posture. Add validation, draft saves, profile detail, completion percentage, and audit events.

Instructions:

1. Inspect the existing codebase, schema, API spec, and use-case docs before editing.

2. Reuse existing project patterns and avoid broad refactors.

3. Implement the smallest complete vertical slice that satisfies the acceptance criteria.

4. Enforce tenant scoping, RBAC, validation, and audit logging where applicable.

5. Add or update tests for the behavior.

6. Update API/schema/docs only if the implementation changes the contract.

7. Run the relevant verification commands and summarize results.

#-----------------------------------------

### Story 7.2: NAICS And Size Status
**Status: Implemented**

Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, read the Agile/Scrum plan, API specification, SQL schema, architecture files, and use-case document in this workspace. Then summarize the current implementation state, identify the next Scrum story that should be built, and propose a small implementation plan before editing files.
Important product rules:

- This is a multi-tenant SaaS.

- Tenant isolation is mandatory.

- RBAC must be enforced on tenant-scoped actions.

- Compliance-relevant events must be audit logged.

- CUI upload policy must be enforced.

- Features should follow the acceptance criteria in the Agile/Scrum plan.

- Add focused tests for tenant isolation, permissions, audit logging, and policy enforcement where relevant.

- Treat the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.

- Preserve tenant isolation, RBAC, audit logging, source traceability, and review metadata.

- Follow the existing project structure: React + Vite web app, ASP.NET Core API, application/domain/infrastructure layers, PostgreSQL persistence, and compliance content package.

- Read the referenced story, tasks, and acceptance criteria before editing.

- Keep changes scoped to the story unless a small supporting change is required.

- Add or update tests according to risk, especially for tenant boundaries, authorization, validation, and audit behavior.

- Update docs, API contracts, seed content, or UI states when the behavior changes.

- Verify the relevant build/test commands before handing off, or clearly report anything that could not be run.

After implementation, run the appropriate xUnit test for .NET and Vitest, paired with the React Testing Library lint/test/build commands, and report what passed or failed.

> Context:
>
> - Epic: Company Compliance Profile
>
> - User story: As a compliance manager, I want to track NAICS codes and size status so that bid readiness can be reviewed by opportunity.
>
> - Acceptance criteria:
>
> - User can add multiple NAICS codes.
> - One NAICS can be marked primary.
> - Size status is stored per NAICS.
> - Missing size status appears in profile gaps.
>
> Implement Story 7.2, "NAICS And Size Status," from `docs/development-phase-use-cases.md`. Add multiple NAICS codes to the company profile, primary NAICS selection, per-NAICS size status and basis, and profile gap warnings for missing size status. Ensure the behavior is tenant scoped and covered by validation tests.

Instructions:

1. Inspect the existing codebase, schema, API spec, and use-case docs before editing.

2. Reuse existing project patterns and avoid broad refactors.

3. Implement the smallest complete vertical slice that satisfies the acceptance criteria.

4. Enforce tenant scoping, RBAC, validation, and audit logging where applicable.

5. Add or update tests for the behavior.

6. Update API/schema/docs only if the implementation changes the contract.

7. Run the relevant verification commands and summarize results.

#-----------------------------------------

### Story 7.3: Certification Tracking
**Status: Implemented**

Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, read the Agile/Scrum plan, API specification, SQL schema, architecture files, and use-case document in this workspace. Then summarize the current implementation state, identify the next Scrum story that should be built, and propose a small implementation plan before editing files.
 Important product rules:

- This is a multi-tenant SaaS.

- Tenant isolation is mandatory.

- RBAC must be enforced on tenant-scoped actions.

- Compliance-relevant events must be audit logged.

- CUI upload policy must be enforced.

- Features should follow the acceptance criteria in the Agile/Scrum plan.

- Add focused tests for tenant isolation, permissions, audit logging, and policy enforcement where relevant.

- Treat the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.

- Preserve tenant isolation, RBAC, audit logging, source traceability, and review metadata.

- Follow the existing project structure: React + Vite web app, ASP.NET Core API, application/domain/infrastructure layers, PostgreSQL persistence, and compliance content package.

- Read the referenced story, tasks, and acceptance criteria before editing.

- Keep changes scoped to the story unless a small supporting change is required.

- Add or update tests according to risk, especially for tenant boundaries, authorization, validation, and audit behavior.

- Update docs, API contracts, seed content, or UI states when the behavior changes.

- Verify the relevant build/test commands before handing off, or clearly report anything that could not be run.

After implementation, run the appropriate xUnit test for .NET and Vitest, paired with the React Testing Library lint/test/build commands, and report what passed or failed.

> Context:
>
> - Epic: Company Compliance Profile
>
> - User story: As a compliance manager, I want to track socioeconomic certifications so that renewals do not get missed.
>
> - Acceptance criteria:
>
> - User can add 8(a), WOSB, EDWOSB, HUBZone, SDVOSB, SDB, and custom certifications.
> - Expiring certifications create calendar tasks.
> - Expired certifications are flagged.
> - Certification changes are audit logged.
>
> Implement Story 7.3, "Certification Tracking," from `docs/development-phase-use-cases.md`. Add certification tracking for 8(a), WOSB, EDWOSB, HUBZone, SDVOSB, SDB, and custom certifications with issuing body, status, effective/expiration dates, and evidence links. Generate renewal tasks, flag expired/expiring certifications, show them on the dashboard, and audit changes.

Instructions:

1. Inspect the existing codebase, schema, API spec, and use-case docs before editing.

2. Reuse existing project patterns and avoid broad refactors.

3. Implement the smallest complete vertical slice that satisfies the acceptance criteria.

4. Enforce tenant scoping, RBAC, validation, and audit logging where applicable.

5. Add or update tests for the behavior.

6. Update API/schema/docs only if the implementation changes the contract.

7. Run the relevant verification commands and summarize results.

#-----------------------------------------

## 8. Contract Intake

### Story 8.1: Create Contract Record
**Status: Implemented**

Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, read the Agile/Scrum plan, API specification, SQL schema, architecture files, and use-case document in this workspace. Then summarize the current implementation state, identify the next Scrum story that should be built, and propose a small implementation plan before editing files.
Important product rules:

- This is a multi-tenant SaaS.

- Tenant isolation is mandatory.

- RBAC must be enforced on tenant-scoped actions.

- Compliance-relevant events must be audit logged.

- CUI upload policy must be enforced.

- Features should follow the acceptance criteria in the Agile/Scrum plan.

- Add focused tests for tenant isolation, permissions, audit logging, and policy enforcement where relevant.

- Treat the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.

- Preserve tenant isolation, RBAC, audit logging, source traceability, and review metadata.

- Follow the existing project structure: React + Vite web app, ASP.NET Core API, application/domain/infrastructure layers, PostgreSQL persistence, and compliance content package.

- Read the referenced story, tasks, and acceptance criteria before editing.

- Keep changes scoped to the story unless a small supporting change is required.

- Add or update tests according to risk, especially for tenant boundaries, authorization, validation, and audit behavior.

- Update docs, API contracts, seed content, or UI states when the behavior changes.

- Verify the relevant build/test commands before handing off, or clearly report anything that could not be run.

After implementation, run the appropriate xUnit test for .NET and Vitest, paired with the React Testing Library lint/test/build commands, and report what passed or failed.

> Context:
>
> - Epic: Contract Intake
>
> - User story: As a contracts admin, I want to create a contract record so that compliance work can be organized by award, solicitation, subcontract, or purchase order.
>
> - Acceptance criteria:
>
> - User can create draft and active contract records.
> - Contract list is tenant scoped.
> - Contract detail shows key dates and role.
> - Contract create and update actions are audit logged.
>
> Implement Story 8.1, "Create Contract Record," from `docs/development-phase-use-cases.md`. Build contract API and UI support for contract number, agency/prime, contract type, role, status, period of performance, place of performance, description, and data handling posture. Support draft/active states, tenant-scoped list/detail pages, key dates, and audit logging for create/update actions.

Instructions:

1. Inspect the existing codebase, schema, API spec, and use-case docs before editing.

2. Reuse existing project patterns and avoid broad refactors.

3. Implement the smallest complete vertical slice that satisfies the acceptance criteria.

4. Enforce tenant scoping, RBAC, validation, and audit logging where applicable.

5. Add or update tests for the behavior.

6. Update API/schema/docs only if the implementation changes the contract.

7. Run the relevant verification commands and summarize results.

#-----------------------------------------

### Story 8.2: Contract Document Metadata And Upload
**Status: Implemented**

Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, read the Agile/Scrum plan, API specification, SQL schema, architecture files, and use-case document in this workspace. Then summarize the current implementation state, identify the next Scrum story that should be built, and propose a small implementation plan before editing files.
Important product rules:

- This is a multi-tenant SaaS.

- Tenant isolation is mandatory.

- RBAC must be enforced on tenant-scoped actions.

- Compliance-relevant events must be audit logged.

- CUI upload policy must be enforced.

- Features should follow the acceptance criteria in the Agile/Scrum plan.

- Add focused tests for tenant isolation, permissions, audit logging, and policy enforcement where relevant.

- Treat the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.

- Preserve tenant isolation, RBAC, audit logging, source traceability, and review metadata.

- Follow the existing project structure: React + Vite web app, ASP.NET Core API, application/domain/infrastructure layers, PostgreSQL persistence, and compliance content package.

- Read the referenced story, tasks, and acceptance criteria before editing.

- Keep changes scoped to the story unless a small supporting change is required.

- Add or update tests according to risk, especially for tenant boundaries, authorization, validation, and audit behavior.

- Update docs, API contracts, seed content, or UI states when the behavior changes.

- Verify the relevant build/test commands before handing off, or clearly report anything that could not be run.

After implementation, run the appropriate xUnit test for .NET and Vitest, paired with the React Testing Library lint/test/build commands, and report what passed or failed.

> Context:
>
> - Epic: Contract Intake
>
> - User story: As a contracts admin, I want to upload allowed contract documents and record document metadata so that source materials are available for review.
>
> - Acceptance criteria:
>
> - Upload is disabled until data handling acknowledgement is complete.
> - File metadata is linked to the contract.
> - Disallowed files are rejected.
> - Upload and delete actions are audit logged.
>
> Implement Story 8.2, "Contract Document Metadata And Upload," from `docs/development-phase-use-cases.md`. Add contract document metadata for solicitation, contract, subcontract, purchase order, SOW, flow-down attachment, wage determination, DD Form 254 metadata, and CUI marking guide metadata. Integrate data handling acknowledgement, file metadata, object storage reference, scan/validation status, rejection handling, and audit logging for upload/delete.

Instructions:

1. Inspect the existing codebase, schema, API spec, and use-case docs before editing.

2. Reuse existing project patterns and avoid broad refactors.

3. Implement the smallest complete vertical slice that satisfies the acceptance criteria.

4. Enforce tenant scoping, RBAC, validation, and audit logging where applicable.

5. Add or update tests for the behavior.

6. Update API/schema/docs only if the implementation changes the contract.

7. Run the relevant verification commands and summarize results.

#-----------------------------------------

### Story 8.3: Contract Dates And Deliverables
**Status: Implemented**

Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, read the Agile/Scrum plan, API specification, SQL schema, architecture files, and use-case document in this workspace. Then summarize the current implementation state, identify the next Scrum story that should be built, and propose a small implementation plan before editing files.
Important product rules:

- This is a multi-tenant SaaS.

- Tenant isolation is mandatory.

- RBAC must be enforced on tenant-scoped actions.

- Compliance-relevant events must be audit logged.

- CUI upload policy must be enforced.

- Features should follow the acceptance criteria in the Agile/Scrum plan.

- Add focused tests for tenant isolation, permissions, audit logging, and policy enforcement where relevant.

- Treat the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.

- Preserve tenant isolation, RBAC, audit logging, source traceability, and review metadata.

- Follow the existing project structure: React + Vite web app, ASP.NET Core API, application/domain/infrastructure layers, PostgreSQL persistence, and compliance content package.

- Read the referenced story, tasks, and acceptance criteria before editing.

- Keep changes scoped to the story unless a small supporting change is required.

- Add or update tests according to risk, especially for tenant boundaries, authorization, validation, and audit behavior.

- Update docs, API contracts, seed content, or UI states when the behavior changes.

- Verify the relevant build/test commands before handing off, or clearly report anything that could not be run.

After implementation, run the appropriate xUnit test for .NET and Vitest, paired with the React Testing Library lint/test/build commands, and report what passed or failed.

> Context:
>
> - Epic: Contract Intake
>
> - User story: As a contracts admin, I want to capture deliverables and deadlines so that contract performance obligations appear in the calendar.
>
> - Acceptance criteria:
>
> - Deliverables appear on contract detail.
> - Deliverable due dates appear on calendar.
> - Overdue deliverables are flagged.
> - Status changes are audit logged.
>
> Implement Story 8.3, "Contract Dates And Deliverables," from `docs/development-phase-use-cases.md`. Add deliverable and deadline models, UI for owner/due date/status/description, calendar task linking, overdue handling, and audit logging. Deliverables must appear on contract detail and in the compliance calendar.

Instructions:

1. Inspect the existing codebase, schema, API spec, and use-case docs before editing.

2. Reuse existing project patterns and avoid broad refactors.

3. Implement the smallest complete vertical slice that satisfies the acceptance criteria.

4. Enforce tenant scoping, RBAC, validation, and audit logging where applicable.

5. Add or update tests for the behavior.

6. Update API/schema/docs only if the implementation changes the contract.

7. Run the relevant verification commands and summarize results.

#-----------------------------------------

## 9. Manual Clause Tagging

### Story 9.1: Clause Library Search
**Status: Implemented**

Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, read the Agile/Scrum plan, API specification, SQL schema, architecture files, and use-case document in this workspace. Then summarize the current implementation state, identify the next Scrum story that should be built, and propose a small implementation plan before editing files.
> Context:
>
> - Epic: Manual Clause Tagging
>
> - User story: As a contracts admin, I want to search the curated clause library so that I can quickly add applicable clauses to a contract.
>
> - Acceptance criteria:
>
> - User can search by clause number, title, and category.
> - Only published clauses are available for customer mapping.
> - Search results show source and last reviewed date.
> - Search is tenant safe and does not expose draft content.
>
> Implement Story 9.1, "Clause Library Search," from `docs/development-phase-use-cases.md`. Add clause search with filters for FAR, DFARS, CMMC, labor, telecom, ByteDance, and custom categories. Build the UI search/selection pattern and show source URL plus last reviewed date. Only published clauses should be mappable, and draft content must never leak into customer search results.

Instructions:

1. Inspect the existing codebase, schema, API spec, and use-case docs before editing.

2. Reuse existing project patterns and avoid broad refactors.

3. Implement the smallest complete vertical slice that satisfies the acceptance criteria.

4. Enforce tenant scoping, RBAC, validation, and audit logging where applicable.

5. Add or update tests for the behavior.

6. Update API/schema/docs only if the implementation changes the contract.

7. Run the relevant verification commands and summarize results.

#-----------------------------------------

### Story 9.2: Attach Clause To Contract
**Status: Implemented**

Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, read the Agile/Scrum plan, API specification, SQL schema, architecture files, and use-case document in this workspace. Then summarize the current implementation state, identify the next Scrum story that should be built, and propose a small implementation plan before editing files.
Important product rules:

- This is a multi-tenant SaaS.

- Tenant isolation is mandatory.

- RBAC must be enforced on tenant-scoped actions.

- Compliance-relevant events must be audit logged.

- CUI upload policy must be enforced.

- Features should follow the acceptance criteria in the Agile/Scrum plan.

- Add focused tests for tenant isolation, permissions, audit logging, and policy enforcement where relevant.

- Treat the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.

- Preserve tenant isolation, RBAC, audit logging, source traceability, and review metadata.

- Follow the existing project structure: React + Vite web app, ASP.NET Core API, application/domain/infrastructure layers, PostgreSQL persistence, and compliance content package.

- Read the referenced story, tasks, and acceptance criteria before editing.

- Keep changes scoped to the story unless a small supporting change is required.

- Add or update tests according to risk, especially for tenant boundaries, authorization, validation, and audit behavior.

- Update docs, API contracts, seed content, or UI states when the behavior changes.

- Verify the relevant build/test commands before handing off, or clearly report anything that could not be run.

After implementation, run the appropriate lint/test/build commands and report what passed or failed.

> Context:
>
> - Epic: Manual Clause Tagging
>
> - User story: As a contracts admin, I want to attach a clause to a contract so that its obligations can be tracked.
>
> - Acceptance criteria:
>
> - User can attach a published clause to a contract.
> - Duplicate clause attachments are prevented.
> - Removing a clause requires a reason.
> - Add and remove actions are audit logged.
>
> Implement Story 9.2, "Attach Clause To Contract," from `docs/development-phase-use-cases.md`. Add the contract-clause relationship, attachment reason, source document reference, duplicate prevention, and remove-with-reason workflow. Ensure add/remove actions are tenant scoped and audit logged.

Instructions:

1. Inspect the existing codebase, schema, API spec, and use-case docs before editing.

2. Reuse existing project patterns and avoid broad refactors.

3. Implement the smallest complete vertical slice that satisfies the acceptance criteria.

4. Enforce tenant scoping, RBAC, validation, and audit logging where applicable.

5. Add or update tests for the behavior.

6. Update API/schema/docs only if the implementation changes the contract.

7. Run the relevant verification commands and summarize results.

#-----------------------------------------

### Story 9.3: Generate Obligations From Clause
**Status: Implemented**

Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, read the Agile/Scrum plan, API specification, SQL schema, architecture files, and use-case document in this workspace. Then summarize the current implementation state, identify the next Scrum story that should be built, and propose a small implementation plan before editing files.
Important product rules:

- This is a multi-tenant SaaS.

- Tenant isolation is mandatory.

- RBAC must be enforced on tenant-scoped actions.

- Compliance-relevant events must be audit logged.

- CUI upload policy must be enforced.

- Features should follow the acceptance criteria in the Agile/Scrum plan.

- Add focused tests for tenant isolation, permissions, audit logging, and policy enforcement where relevant.

- Treat the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.

- Preserve tenant isolation, RBAC, audit logging, source traceability, and review metadata.

- Follow the existing project structure: React + Vite web app, ASP.NET Core API, application/domain/infrastructure layers, PostgreSQL persistence, and compliance content package.

- Read the referenced story, tasks, and acceptance criteria before editing.

- Keep changes scoped to the story unless a small supporting change is required.

- Add or update tests according to risk, especially for tenant boundaries, authorization, validation, and audit behavior.

- Update docs, API contracts, seed content, or UI states when the behavior changes.

- Verify the relevant build/test commands before handing off, or clearly report anything that could not be run.

After implementation, run the appropriate lint/test/build commands and report what passed or failed.

> Context:
>
> - Epic: Manual Clause Tagging
>
> - User story: As a compliance manager, I want mapped obligations to appear when a clause is added so that compliance work starts immediately.
>
> - Acceptance criteria:
>
> - Adding a clause creates mapped obligations when templates exist.
> - Generated obligations link back to contract and clause.
> - Generated obligations include source URL, owner, required action, evidence examples, risk, confidence, and review metadata.
> - Generation is idempotent.
>
> Implement Story 9.3, "Generate Obligations From Clause," from `docs/development-phase-use-cases.md`. Map clauses to obligation templates and generate contract-specific obligation instances, including default tasks where required. Preserve source URL, owner, action, evidence examples, risk, confidence, review metadata, contract, and clause links. Make generation idempotent and tested.

Instructions:

1. Inspect the existing codebase, schema, API spec, and use-case docs before editing.

2. Reuse existing project patterns and avoid broad refactors.

3. Implement the smallest complete vertical slice that satisfies the acceptance criteria.

4. Enforce tenant scoping, RBAC, validation, and audit logging where applicable.

5. Add or update tests for the behavior.

6. Update API/schema/docs only if the implementation changes the contract.

7. Run the relevant verification commands and summarize results.

#-----------------------------------------

## 10. Obligation Dashboard

### Story 10.1: Obligation List And Filters
**Status: Implemented**

Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, read the Agile/Scrum plan, API specification, SQL schema, architecture files, and use-case document in this workspace. Then summarize the current implementation state, identify the next Scrum story that should be built, and propose a small implementation plan before editing files.
Important product rules:

- This is a multi-tenant SaaS.

- Tenant isolation is mandatory.

- RBAC must be enforced on tenant-scoped actions.

- Compliance-relevant events must be audit logged.

- CUI upload policy must be enforced.

- Features should follow the acceptance criteria in the Agile/Scrum plan.

- Add focused tests for tenant isolation, permissions, audit logging, and policy enforcement where relevant.

- Treat the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.

- Preserve tenant isolation, RBAC, audit logging, source traceability, and review metadata.

- Follow the existing project structure: React + Vite web app, ASP.NET Core API, application/domain/infrastructure layers, PostgreSQL persistence, and compliance content package.

- Read the referenced story, tasks, and acceptance criteria before editing.

- Keep changes scoped to the story unless a small supporting change is required.

- Add or update tests according to risk, especially for tenant boundaries, authorization, validation, and audit behavior.

- Update docs, API contracts, seed content, or UI states when the behavior changes.

- Verify the relevant build/test commands before handing off, or clearly report anything that could not be run.

After implementation, run the appropriate lint/test/build commands and report what passed or failed.

> Context:
>
> - Epic: Obligation Dashboard
>
> - User story: As a compliance manager, I want to view and filter obligations so that I can focus on the most important work.
>
> - Acceptance criteria:
>
> - Dashboard shows tenant-scoped obligations only.
> - User can filter by contract, risk, owner, status, and module.
> - Overdue and high-risk obligations are easy to identify.
> - Empty state guides user to company profile or contract intake.
>
> Implement Story 10.1, "Obligation List And Filters," from `docs/development-phase-use-cases.md`. Add a tenant-scoped obligation list endpoint and dashboard/work queue with filters for contract, risk, owner, status, due date, module, and source. Make overdue and high-risk obligations easy to identify and include an empty state that guides users to profile or contract intake.

Instructions:

1. Inspect the existing codebase, schema, API spec, and use-case docs before editing.

2. Reuse existing project patterns and avoid broad refactors.

3. Implement the smallest complete vertical slice that satisfies the acceptance criteria.

4. Enforce tenant scoping, RBAC, validation, and audit logging where applicable.

5. Add or update tests for the behavior.

6. Update API/schema/docs only if the implementation changes the contract.

7. Run the relevant verification commands and summarize results.

#-----------------------------------------

### Story 10.2: Obligation Detail
**Status: Implemented**

Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, read the Agile/Scrum plan, API specification, SQL schema, architecture files, and use-case document in this workspace. Then summarize the current implementation state, identify the next Scrum story that should be built, and propose a small implementation plan before editing files.
Important product rules:

- This is a multi-tenant SaaS.

- Tenant isolation is mandatory.

- RBAC must be enforced on tenant-scoped actions.

- Compliance-relevant events must be audit logged.

- CUI upload policy must be enforced.

- Features should follow the acceptance criteria in the Agile/Scrum plan.

- Add focused tests for tenant isolation, permissions, audit logging, and policy enforcement where relevant.

- Treat the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.

- Preserve tenant isolation, RBAC, audit logging, source traceability, and review metadata.

- Follow the existing project structure: React + Vite web app, ASP.NET Core API, application/domain/infrastructure layers, PostgreSQL persistence, and compliance content package.

- Read the referenced story, tasks, and acceptance criteria before editing.

- Keep changes scoped to the story unless a small supporting change is required.

- Add or update tests according to risk, especially for tenant boundaries, authorization, validation, and audit behavior.

- Update docs, API contracts, seed content, or UI states when the behavior changes.

- Verify the relevant build/test commands before handing off, or clearly report anything that could not be run.

After implementation, run the appropriate lint/test/build commands and report what passed or failed.

> Context:
>
> - Epic: Obligation Dashboard
>
> - User story: As a compliance manager, I want obligation details so that I understand why it applies and what action is expected.
>
> - Acceptance criteria:
>
> - Obligation detail includes source-backed content.
> - Source link is visible.
> - User can see linked tasks and evidence.
> - Status changes are audit logged.
>
> Implement Story 10.2, "Obligation Detail," from `docs/development-phase-use-cases.md`. Build obligation detail API and UI showing plain-English summary, trigger, required action, owner, evidence examples, flow-down requirement, source link, confidence, last reviewed date, expert review flag, linked tasks/evidence, and status update workflow. Audit all status changes.

Instructions:

1. Inspect the existing codebase, schema, API spec, and use-case docs before editing.

2. Reuse existing project patterns and avoid broad refactors.

3. Implement the smallest complete vertical slice that satisfies the acceptance criteria.

4. Enforce tenant scoping, RBAC, validation, and audit logging where applicable.

5. Add or update tests for the behavior.

6. Update API/schema/docs only if the implementation changes the contract.

7. Run the relevant verification commands and summarize results.

#-----------------------------------------

### Story 10.3: Ownership Assignment
**Status: Implemented**

Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, read the Agile/Scrum plan, API specification, SQL schema, architecture files, and use-case document in this workspace. Then summarize the current implementation state, identify the next Scrum story that should be built, and propose a small implementation plan before editing files.
Important product rules:

- This is a multi-tenant SaaS.

- Tenant isolation is mandatory.

- RBAC must be enforced on tenant-scoped actions.

- Compliance-relevant events must be audit logged.

- CUI upload policy must be enforced.

- Features should follow the acceptance criteria in the Agile/Scrum plan.

- Add focused tests for tenant isolation, permissions, audit logging, and policy enforcement where relevant.

- Treat the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.

- Preserve tenant isolation, RBAC, audit logging, source traceability, and review metadata.

- Follow the existing project structure: React + Vite web app, ASP.NET Core API, application/domain/infrastructure layers, PostgreSQL persistence, and compliance content package.

- Read the referenced story, tasks, and acceptance criteria before editing.

- Keep changes scoped to the story unless a small supporting change is required.

- Add or update tests according to risk, especially for tenant boundaries, authorization, validation, and audit behavior.

- Update docs, API contracts, seed content, or UI states when the behavior changes.

- Verify the relevant build/test commands before handing off, or clearly report anything that could not be run.

After implementation, run the appropriate lint/test/build commands and report what passed or failed.
> Context:
>
> - Epic: Obligation Dashboard
>
> - User story: As a compliance manager, I want to assign obligation owners so that accountability is clear.
>
> - Acceptance criteria:
>
> - Obligations can be assigned to a user or role.
> - Assignment changes appear on the dashboard.
> - Unauthorized users cannot assign owners.
> - Assignment changes are audit logged.
>
> Implement Story 10.3, "Ownership Assignment," from `docs/development-phase-use-cases.md`. Add user and role owner assignment for obligation instances, UI assignment controls, dashboard updates, authorization checks, optional notification emission, and audit logging for assignment changes.

Instructions:

1. Inspect the existing codebase, schema, API spec, and use-case docs before editing.

2. Reuse existing project patterns and avoid broad refactors.

3. Implement the smallest complete vertical slice that satisfies the acceptance criteria.

4. Enforce tenant scoping, RBAC, validation, and audit logging where applicable.

5. Add or update tests for the behavior.

6. Update API/schema/docs only if the implementation changes the contract.

7. Run the relevant verification commands and summarize results.

#-----------------------------------------

## 11. Task And Compliance Calendar

### Story 11.1: Task Management
**Status: Implemented**

Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, read the Agile/Scrum plan, API specification, SQL schema, architecture files, and use-case document in this workspace. Then summarize the current implementation state, identify the next Scrum story that should be built, and propose a small implementation plan before editing files.
Important product rules:

- This is a multi-tenant SaaS.

- Tenant isolation is mandatory.

- RBAC must be enforced on tenant-scoped actions.

- Compliance-relevant events must be audit logged.

- CUI upload policy must be enforced.

- Features should follow the acceptance criteria in the Agile/Scrum plan.

- Add focused tests for tenant isolation, permissions, audit logging, and policy enforcement where relevant.

- Treat the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.

- Preserve tenant isolation, RBAC, audit logging, source traceability, and review metadata.

- Follow the existing project structure: React + Vite web app, ASP.NET Core API, application/domain/infrastructure layers, PostgreSQL persistence, and compliance content package.

- Read the referenced story, tasks, and acceptance criteria before editing.

- Keep changes scoped to the story unless a small supporting change is required.

- Add or update tests according to risk, especially for tenant boundaries, authorization, validation, and audit behavior.

- Update docs, API contracts, seed content, or UI states when the behavior changes.

- Verify the relevant build/test commands before handing off, or clearly report anything that could not be run.

After implementation, run the appropriate lint/test/build commands and report what passed or failed.

> Context:
>
> - Epic: Task And Compliance Calendar
>
> - User story: As a compliance manager, I want to create and assign tasks so that obligations turn into trackable work.
>
> - Acceptance criteria:
>
> - Tasks can be linked to relevant compliance entities.
> - Task status includes open, in_progress, blocked, completed, and canceled.
> - Task updates are tenant scoped.
> - Task status changes are audit logged.
>
> Implement Story 11.1, "Task Management," from `docs/development-phase-use-cases.md`. Build task model/API and workflows to create, update, complete, and reopen tasks linked to obligations, contracts, controls, evidence, subcontractors, or certifications. Include owner, due date, status, priority, reminder date, notes, tenant scoping, and audit logging.

Instructions:

1. Inspect the existing codebase, schema, API spec, and use-case docs before editing.

2. Reuse existing project patterns and avoid broad refactors.

3. Implement the smallest complete vertical slice that satisfies the acceptance criteria.

4. Enforce tenant scoping, RBAC, validation, and audit logging where applicable.

5. Add or update tests for the behavior.

6. Update API/schema/docs only if the implementation changes the contract.

7. Run the relevant verification commands and summarize results.

#-----------------------------------------

### Story 11.2: Calendar View
**Status: Implemented**

Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, read the Agile/Scrum plan, API specification, SQL schema, architecture files, and use-case document in this workspace. Then summarize the current implementation state, identify the next Scrum story that should be built, and propose a small implementation plan before editing files.
 Important product rules:

- This is a multi-tenant SaaS.

- Tenant isolation is mandatory.

- RBAC must be enforced on tenant-scoped actions.

- Compliance-relevant events must be audit logged.

- CUI upload policy must be enforced.

- Features should follow the acceptance criteria in the Agile/Scrum plan.

- Add focused tests for tenant isolation, permissions, audit logging, and policy enforcement where relevant.

- Treat the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.

- Preserve tenant isolation, RBAC, audit logging, source traceability, and review metadata.

- Follow the existing project structure: React + Vite web app, ASP.NET Core API, application/domain/infrastructure layers, PostgreSQL persistence, and compliance content package.

- Read the referenced story, tasks, and acceptance criteria before editing.

- Keep changes scoped to the story unless a small supporting change is required.

- Add or update tests according to risk, especially for tenant boundaries, authorization, validation, and audit behavior.

- Update docs, API contracts, seed content, or UI states when the behavior changes.

- Verify the relevant build/test commands before handing off, or clearly report anything that could not be run.

After implementation, run the appropriate lint/test/build commands and report what passed or failed.
> Context:
>
> - Epic: Task And Compliance Calendar
>
> - User story: As a compliance manager, I want a calendar view so that upcoming work is visible by date.
>
> - Acceptance criteria:
>
> - Calendar shows tasks, renewals, reports, contract deadlines, and policy reviews.
> - User can filter calendar items.
> - Overdue items are visually distinct.
> - Calendar data is tenant scoped.
>
> Implement Story 11.2, "Calendar View," from `docs/development-phase-use-cases.md`. Add a calendar endpoint aggregating tasks, renewals, deliverables, and reviews, plus a month/list/agenda UI with filters by owner, status, risk, contract, and module. Overdue items should be visually distinct and all data tenant scoped.

Instructions:

1. Inspect the existing codebase, schema, API spec, and use-case docs before editing.

2. Reuse existing project patterns and avoid broad refactors.

3. Implement the smallest complete vertical slice that satisfies the acceptance criteria.

4. Enforce tenant scoping, RBAC, validation, and audit logging where applicable.

5. Add or update tests for the behavior.

6. Update API/schema/docs only if the implementation changes the contract.

7. Run the relevant verification commands and summarize results.

#-----------------------------------------

### Story 11.3: Renewal Generation
**Status: Implemented**

Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, read the Agile/Scrum plan, API specification, SQL schema, architecture files, and use-case document in this workspace. Then summarize the current implementation state, identify the next Scrum story that should be built, and propose a small implementation plan before editing files.
 Important product rules:

- This is a multi-tenant SaaS.

- Tenant isolation is mandatory.

- RBAC must be enforced on tenant-scoped actions.

- Compliance-relevant events must be audit logged.

- CUI upload policy must be enforced.

- Features should follow the acceptance criteria in the Agile/Scrum plan.

- Add focused tests for tenant isolation, permissions, audit logging, and policy enforcement where relevant.

- Treat the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.

- Preserve tenant isolation, RBAC, audit logging, source traceability, and review metadata.

- Follow the existing project structure: React + Vite web app, ASP.NET Core API, application/domain/infrastructure layers, PostgreSQL persistence, and compliance content package.

- Read the referenced story, tasks, and acceptance criteria before editing.

- Keep changes scoped to the story unless a small supporting change is required.

- Add or update tests according to risk, especially for tenant boundaries, authorization, validation, and audit behavior.

- Update docs, API contracts, seed content, or UI states when the behavior changes.

- Verify the relevant build/test commands before handing off, or clearly report anything that could not be run.

After implementation, run the appropriate lint/test/build commands and report what passed or failed.

> Context:
>
> - Epic: Task And Compliance Calendar
>
> - User story: As a compliance manager, I want renewal tasks generated from profile and evidence dates so that recurring compliance dates are not missed.
>
> - Acceptance criteria:
>
> - Renewal tasks are generated from dated records.
> - Duplicate renewal tasks are not created for the same entity and due date.
> - Lead times can be configured or defaulted.
> - Generated tasks link back to the source record.
>
> Implement Story 11.3, "Renewal Generation," from `docs/development-phase-use-cases.md`. Generate renewal tasks from SAM expiration, certification expiration, evidence expiration, insurance expiration, policy review, and CMMC affirmation dates. Add configurable or default lead times, duplicate prevention, source record links, and due-date calculation tests.

Instructions:

1. Inspect the existing codebase, schema, API spec, and use-case docs before editing.

2. Reuse existing project patterns and avoid broad refactors.

3. Implement the smallest complete vertical slice that satisfies the acceptance criteria.

4. Enforce tenant scoping, RBAC, validation, and audit logging where applicable.

5. Add or update tests for the behavior.

6. Update API/schema/docs only if the implementation changes the contract.

7. Run the relevant verification commands and summarize results.

#-----------------------------------------

## 12. Evidence Vault

### Story 12.1: Evidence Metadata
**Status: Implemented**

Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, read the Agile/Scrum plan, API specification, SQL schema, architecture files, and use-case document in this workspace. Then summarize the current implementation state, identify the next Scrum story that should be built, and propose a small implementation plan before editing files.
 Important product rules:

- This is a multi-tenant SaaS.

- Tenant isolation is mandatory.

- RBAC must be enforced on tenant-scoped actions.

- Compliance-relevant events must be audit logged.

- CUI upload policy must be enforced.

- Features should follow the acceptance criteria in the Agile/Scrum plan.

- Add focused tests for tenant isolation, permissions, audit logging, and policy enforcement where relevant.

- Treat the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.

- Preserve tenant isolation, RBAC, audit logging, source traceability, and review metadata.

- Follow the existing project structure: React + Vite web app, ASP.NET Core API, application/domain/infrastructure layers, PostgreSQL persistence, and compliance content package.

- Read the referenced story, tasks, and acceptance criteria before editing.

- Keep changes scoped to the story unless a small supporting change is required.

- Add or update tests according to risk, especially for tenant boundaries, authorization, validation, and audit behavior.

- Update docs, API contracts, seed content, or UI states when the behavior changes.

- Verify the relevant build/test commands before handing off, or clearly report anything that could not be run.

After implementation, run the appropriate lint/test/build commands and report what passed or failed.

> Context:
>
> - Epic: Evidence Vault
>
> - User story: As a compliance manager, I want to create evidence records with tags and links so that proof can be reused across obligations.
>
> - Acceptance criteria:
>
> - Evidence can be linked to multiple obligations or controls.
> - Evidence supports folderless tags.
> - Evidence expiration dates can generate tasks.
> - Evidence metadata changes are audit logged.
>
> Implement Story 12.1, "Evidence Metadata," from `docs/development-phase-use-cases.md`. Add evidence metadata with title, type, owner, approval status, expiration date, tags, description, and source links. Support relationships to obligations, controls, contracts, vendors, subcontractors, employees, and reports; build list/detail views; validate metadata; generate expiration tasks; and audit metadata changes.

Instructions:

1. Inspect the existing codebase, schema, API spec, and use-case docs before editing.

2. Reuse existing project patterns and avoid broad refactors.

3. Implement the smallest complete vertical slice that satisfies the acceptance criteria.

4. Enforce tenant scoping, RBAC, validation, and audit logging where applicable.

5. Add or update tests for the behavior.

6. Update API/schema/docs only if the implementation changes the contract.

7. Run the relevant verification commands and summarize results.

#-----------------------------------------

### Story 12.2: Evidence File Upload
**Status: Implemented**

Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, read the Agile/Scrum plan, API specification, SQL schema, architecture files, and use-case document in this workspace. Then summarize the current implementation state, identify the next Scrum story that should be built, and propose a small implementation plan before editing files.
 Important product rules:

- This is a multi-tenant SaaS.

- Tenant isolation is mandatory.

- RBAC must be enforced on tenant-scoped actions.

- Compliance-relevant events must be audit logged.

- CUI upload policy must be enforced.

- Features should follow the acceptance criteria in the Agile/Scrum plan.

- Add focused tests for tenant isolation, permissions, audit logging, and policy enforcement where relevant.

- Treat the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.

- Preserve tenant isolation, RBAC, audit logging, source traceability, and review metadata.

- Follow the existing project structure: React + Vite web app, ASP.NET Core API, application/domain/infrastructure layers, PostgreSQL persistence, and compliance content package.

- Read the referenced story, tasks, and acceptance criteria before editing.

- Keep changes scoped to the story unless a small supporting change is required.

- Add or update tests according to risk, especially for tenant boundaries, authorization, validation, and audit behavior.

- Update docs, API contracts, seed content, or UI states when the behavior changes.

- Verify the relevant build/test commands before handing off, or clearly report anything that could not be run.

After implementation, run the appropriate lint/test/build commands and report what passed or failed.

> Context:
>
> - Epic: Evidence Vault
>
> - User story: As a contributor, I want to upload approved allowed evidence files so that compliance proof is attached to the right work.
>
> - Acceptance criteria:
>
> - Upload requires data handling acknowledgement.
> - Files are not marked usable until validation and scan state allow it.
> - New file uploads create versions instead of overwriting history.
> - Upload, download, and delete actions are audit logged.
>
> Implement Story 12.2, "Evidence File Upload," from `docs/development-phase-use-cases.md`. Add file upload to evidence records with data handling acknowledgement, allowed file types, size limits, malware scan status, file version metadata, and download permissions. Files must not be usable until validation and scan state allow it, and upload/download/delete actions must be audit logged.

Instructions:

1. Inspect the existing codebase, schema, API spec, and use-case docs before editing.

2. Reuse existing project patterns and avoid broad refactors.

3. Implement the smallest complete vertical slice that satisfies the acceptance criteria.

4. Enforce tenant scoping, RBAC, validation, and audit logging where applicable.

5. Add or update tests for the behavior.

6. Update API/schema/docs only if the implementation changes the contract.

7. Run the relevant verification commands and summarize results.

#-----------------------------------------

### Story 12.3: Evidence Approval
**Status: Implemented**

Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, read the Agile/Scrum plan, API specification, SQL schema, architecture files, and use-case document in this workspace. Then summarize the current implementation state, identify the next Scrum story that should be built, and propose a small implementation plan before editing files.
 Important product rules:

- This is a multi-tenant SaaS.

- Tenant isolation is mandatory.

- RBAC must be enforced on tenant-scoped actions.

- Compliance-relevant events must be audit logged.

- CUI upload policy must be enforced.

- Features should follow the acceptance criteria in the Agile/Scrum plan.

- Add focused tests for tenant isolation, permissions, audit logging, and policy enforcement where relevant.

- Treat the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.

- Preserve tenant isolation, RBAC, audit logging, source traceability, and review metadata.

- Follow the existing project structure: React + Vite web app, ASP.NET Core API, application/domain/infrastructure layers, PostgreSQL persistence, and compliance content package.

- Read the referenced story, tasks, and acceptance criteria before editing.

- Keep changes scoped to the story unless a small supporting change is required.

- Add or update tests according to risk, especially for tenant boundaries, authorization, validation, and audit behavior.

- Update docs, API contracts, seed content, or UI states when the behavior changes.

- Verify the relevant build/test commands before handing off, or clearly report anything that could not be run.

After implementation, run the appropriate lint/test/build commands and report what passed or failed.

> Context:
>
> - Epic: Evidence Vault
>
> - User story: As a compliance manager, I want to approve evidence so that reports and auditor views only include reviewed material.
>
> - Acceptance criteria:
>
> - Only authorized users can approve evidence.
> - Rejection requires a reason.
> - Approved evidence can be included in reports.
> - Approval decisions are audit logged.
>
> Implement Story 12.3, "Evidence Approval," from `docs/development-phase-use-cases.md`. Add evidence states for draft, submitted, approved, rejected, expired, and archived; implement approval/rejection with comments; restrict approval to authorized roles; show approval state in obligation/report views; require rejection reasons; and audit approval decisions.

Instructions:

1. Inspect the existing codebase, schema, API spec, and use-case docs before editing.

2. Reuse existing project patterns and avoid broad refactors.

3. Implement the smallest complete vertical slice that satisfies the acceptance criteria.

4. Enforce tenant scoping, RBAC, validation, and audit logging where applicable.

5. Add or update tests for the behavior.

6. Update API/schema/docs only if the implementation changes the contract.

7. Run the relevant verification commands and summarize results.

#-----------------------------------------

## 13. CMMC Readiness Tracker

### Story 13.1: CMMC Level Selection
**Status: Implemented**

Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, read the Agile/Scrum plan, API specification, SQL schema, architecture files, and use-case document in this workspace. Then summarize the current implementation state, identify the next Scrum story that should be built, and propose a small implementation plan before editing files.
 Important product rules:

- This is a multi-tenant SaaS.

- Tenant isolation is mandatory.

- RBAC must be enforced on tenant-scoped actions.

- Compliance-relevant events must be audit logged.

- CUI upload policy must be enforced.

- Features should follow the acceptance criteria in the Agile/Scrum plan.

- Add focused tests for tenant isolation, permissions, audit logging, and policy enforcement where relevant.

- Treat the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.

- Preserve tenant isolation, RBAC, audit logging, source traceability, and review metadata.

- Follow the existing project structure: React + Vite web app, ASP.NET Core API, application/domain/infrastructure layers, PostgreSQL persistence, and compliance content package.

- Read the referenced story, tasks, and acceptance criteria before editing.

- Keep changes scoped to the story unless a small supporting change is required.

- Add or update tests according to risk, especially for tenant boundaries, authorization, validation, and audit behavior.

- Update docs, API contracts, seed content, or UI states when the behavior changes.

- Verify the relevant build/test commands before handing off, or clearly report anything that could not be run.

After implementation, run the appropriate lint/test/build commands and report what passed or failed.

> Context:
>
> - Epic: CMMC Readiness Tracker
>
> - User story: As a compliance manager, I want to select a CMMC target level so that the workspace shows the right readiness scope.
>
> - Acceptance criteria:
>
> - User can create a CMMC readiness assessment.
> - Assessment stores target level and status.
> - Assessment summary shows completion progress.
> - Changes are audit logged.
>
> Implement Story 13.1, "CMMC Level Selection," from `docs/development-phase-use-cases.md`. Add CMMC assessment model/API/UI with target level, status, assessment date, affirmation due date, responsible owner, Level 1/Level 2 choices, company/contract links, workspace summary, progress calculation, and audit logging.

Instructions:

1. Inspect the existing codebase, schema, API spec, and use-case docs before editing.

2. Reuse existing project patterns and avoid broad refactors.

3. Implement the smallest complete vertical slice that satisfies the acceptance criteria.

4. Enforce tenant scoping, RBAC, validation, and audit logging where applicable.

5. Add or update tests for the behavior.

6. Update API/schema/docs only if the implementation changes the contract.

7. Run the relevant verification commands and summarize results.

#-----------------------------------------

### Story 13.2: Control Readiness
**Status: Implemented**

Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, read the Agile/Scrum plan, API specification, SQL schema, architecture files, and use-case document in this workspace. Then summarize the current implementation state, identify the next Scrum story that should be built, and propose a small implementation plan before editing files.
 Important product rules:

- This is a multi-tenant SaaS.

- Tenant isolation is mandatory.

- RBAC must be enforced on tenant-scoped actions.

- Compliance-relevant events must be audit logged.

- CUI upload policy must be enforced.

- Features should follow the acceptance criteria in the Agile/Scrum plan.

- Add focused tests for tenant isolation, permissions, audit logging, and policy enforcement where relevant.

- Treat the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.

- Preserve tenant isolation, RBAC, audit logging, source traceability, and review metadata.

- Follow the existing project structure: React + Vite web app, ASP.NET Core API, application/domain/infrastructure layers, PostgreSQL persistence, and compliance content package.

- Read the referenced story, tasks, and acceptance criteria before editing.

- Keep changes scoped to the story unless a small supporting change is required.

- Add or update tests according to risk, especially for tenant boundaries, authorization, validation, and audit behavior.

- Update docs, API contracts, seed content, or UI states when the behavior changes.

- Verify the relevant build/test commands before handing off, or clearly report anything that could not be run.

After implementation, run the appropriate lint/test/build commands and report what passed or failed.

> Context:
>
> - Epic: CMMC Readiness Tracker
>
> - User story: As an IT/security owner, I want to track control status and evidence so that gaps are visible.
>
> - Acceptance criteria:
>
> - Controls can be marked with readiness status.
> - Controls can link to evidence and tasks.
> - Control status contributes to assessment progress.
> - Source baseline is shown for each control.
>
> Implement Story 13.2, "Control Readiness," from `docs/development-phase-use-cases.md`. Load Level 1 controls and Level 2 readiness mappings, add control statuses, link controls to evidence/tasks/assets/POA&M items, build a control detail page, show source baseline, and roll status into assessment progress.

Instructions:

1. Inspect the existing codebase, schema, API spec, and use-case docs before editing.

2. Reuse existing project patterns and avoid broad refactors.

3. Implement the smallest complete vertical slice that satisfies the acceptance criteria.

4. Enforce tenant scoping, RBAC, validation, and audit logging where applicable.

5. Add or update tests for the behavior.

6. Update API/schema/docs only if the implementation changes the contract.

7. Run the relevant verification commands and summarize results.

#-----------------------------------------

### Story 13.3: POA&M Items
**Status: Implemented**

Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, read the Agile/Scrum plan, API specification, SQL schema, architecture files, and use-case document in this workspace. Then summarize the current implementation state, identify the next Scrum story that should be built, and propose a small implementation plan before editing files.
 Important product rules:

- This is a multi-tenant SaaS.

- Tenant isolation is mandatory.

- RBAC must be enforced on tenant-scoped actions.

- Compliance-relevant events must be audit logged.

- CUI upload policy must be enforced.

- Features should follow the acceptance criteria in the Agile/Scrum plan.

- Add focused tests for tenant isolation, permissions, audit logging, and policy enforcement where relevant.

- Treat the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.

- Preserve tenant isolation, RBAC, audit logging, source traceability, and review metadata.

- Follow the existing project structure: React + Vite web app, ASP.NET Core API, application/domain/infrastructure layers, PostgreSQL persistence, and compliance content package.

- Read the referenced story, tasks, and acceptance criteria before editing.

- Keep changes scoped to the story unless a small supporting change is required.

- Add or update tests according to risk, especially for tenant boundaries, authorization, validation, and audit behavior.

- Update docs, API contracts, seed content, or UI states when the behavior changes.

- Verify the relevant build/test commands before handing off, or clearly report anything that could not be run.

After implementation, run the appropriate lint/test/build commands and report what passed or failed.

> Context:
>
> - Epic: CMMC Readiness Tracker
>
> - User story: As a security owner, I want to create POA&M items so that control gaps become assigned remediation work.
>
> - Acceptance criteria:
>
> - POA&M item links to a control.
> - POA&M item has owner, due date, status, and risk.
> - Open and overdue POA&M items appear in CMMC summary and calendar.
> - POA&M changes are audit logged.
>
> Implement Story 13.3, "POA&M Items," from `docs/development-phase-use-cases.md`. Add POA&M items linked to controls with gap, remediation plan, owner, due date, risk, and status. Link POA&M items to tasks, surface open/overdue counts in CMMC summary and calendar, and audit changes.

Instructions:

1. Inspect the existing codebase, schema, API spec, and use-case docs before editing.

2. Reuse existing project patterns and avoid broad refactors.

3. Implement the smallest complete vertical slice that satisfies the acceptance criteria.

4. Enforce tenant scoping, RBAC, validation, and audit logging where applicable.

5. Add or update tests for the behavior.

6. Update API/schema/docs only if the implementation changes the contract.

7. Run the relevant verification commands and summarize results.

#-----------------------------------------

### Story 13.4: Annual Affirmation Tracker
**Status: Implemented**

Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, read the Agile/Scrum plan, API specification, SQL schema, architecture files, and use-case document in this workspace. Then summarize the current implementation state, identify the next Scrum story that should be built, and propose a small implementation plan before editing files.
 Important product rules:

- This is a multi-tenant SaaS.

- Tenant isolation is mandatory.

- RBAC must be enforced on tenant-scoped actions.

- Compliance-relevant events must be audit logged.

- CUI upload policy must be enforced.

- Features should follow the acceptance criteria in the Agile/Scrum plan.

- Add focused tests for tenant isolation, permissions, audit logging, and policy enforcement where relevant.

- Treat the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.

- Preserve tenant isolation, RBAC, audit logging, source traceability, and review metadata.

- Follow the existing project structure: React + Vite web app, ASP.NET Core API, application/domain/infrastructure layers, PostgreSQL persistence, and compliance content package.

- Read the referenced story, tasks, and acceptance criteria before editing.

- Keep changes scoped to the story unless a small supporting change is required.

- Add or update tests according to risk, especially for tenant boundaries, authorization, validation, and audit behavior.

- Update docs, API contracts, seed content, or UI states when the behavior changes.

- Verify the relevant build/test commands before handing off, or clearly report anything that could not be run.

After implementation, run the appropriate lint/test/build commands and report what passed or failed.

> Context:
>
> - Epic: CMMC Readiness Tracker
>
> - User story: As a company owner, I want to track CMMC affirmation dates so that annual requirements are not missed.
>
> - Acceptance criteria:
>
> - Affirmation due date appears on calendar.
> - Upcoming affirmation creates a reminder task.
> - User can link evidence to affirmation.
> - Affirmation updates are audit logged.
>
> Implement Story 13.4, "Annual Affirmation Tracker," from `docs/development-phase-use-cases.md`. Add CMMC affirmation last/due dates, renewal task generation, evidence links, dashboard warnings for upcoming affirmations, calendar visibility, reminders, and audit logging.

Instructions:

1. Inspect the existing codebase, schema, API spec, and use-case docs before editing.

2. Reuse existing project patterns and avoid broad refactors.

3. Implement the smallest complete vertical slice that satisfies the acceptance criteria.

4. Enforce tenant scoping, RBAC, validation, and audit logging where applicable.

5. Add or update tests for the behavior.

6. Update API/schema/docs only if the implementation changes the contract.

7. Run the relevant verification commands and summarize results.

#-----------------------------------------

## 14. Subcontractor Flow-Down Tracker

### Story 14.1: Subcontractor Profile
**Status: Implemented**

Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, read the Agile/Scrum plan, API specification, SQL schema, architecture files, and use-case document in this workspace. Then summarize the current implementation state, identify the next Scrum story that should be built, and propose a small implementation plan before editing files.
 Important product rules:

- This is a multi-tenant SaaS.

- Tenant isolation is mandatory.

- RBAC must be enforced on tenant-scoped actions.

- Compliance-relevant events must be audit logged.

- CUI upload policy must be enforced.

- Features should follow the acceptance criteria in the Agile/Scrum plan.

- Add focused tests for tenant isolation, permissions, audit logging, and policy enforcement where relevant.

- Treat the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.

- Preserve tenant isolation, RBAC, audit logging, source traceability, and review metadata.

- Follow the existing project structure: React + Vite web app, ASP.NET Core API, application/domain/infrastructure layers, PostgreSQL persistence, and compliance content package.

- Read the referenced story, tasks, and acceptance criteria before editing.

- Keep changes scoped to the story unless a small supporting change is required.

- Add or update tests according to risk, especially for tenant boundaries, authorization, validation, and audit behavior.

- Update docs, API contracts, seed content, or UI states when the behavior changes.

- Verify the relevant build/test commands before handing off, or clearly report anything that could not be run.

After implementation, run the appropriate lint/test/build commands and report what passed or failed.

> Context:
>
> - Epic: Subcontractor Flow-Down Tracker
>
> - User story: As a contracts admin, I want to create subcontractor profiles so that supplier compliance can be tracked.
>
> - Acceptance criteria:
>
> - User can create and update subcontractor profiles.
> - Subcontractors can be linked to contracts.
> - CUI access and export-control flags are visible.
> - Changes are audit logged.
>
> Implement Story 14.1, "Subcontractor Profile," from `docs/development-phase-use-cases.md`. Add subcontractor model/API/UI with legal name, point of contact, role, small business status, CMMC status, insurance expiration, NDA status, CUI access flag, export-control flag, workshare percentage, list/detail pages, contract links, and audit logging.

Instructions:

1. Inspect the existing codebase, schema, API spec, and use-case docs before editing.

2. Reuse existing project patterns and avoid broad refactors.

3. Implement the smallest complete vertical slice that satisfies the acceptance criteria.

4. Enforce tenant scoping, RBAC, validation, and audit logging where applicable.

5. Add or update tests for the behavior.

6. Update API/schema/docs only if the implementation changes the contract.

7. Run the relevant verification commands and summarize results.

#-----------------------------------------

### Story 14.2: Flow-Down Clause Tracking
**Status: Implemented**

Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, read the Agile/Scrum plan, API specification, SQL schema, architecture files, and use-case document in this workspace. Then summarize the current implementation state, identify the next Scrum story that should be built, and propose a small implementation plan before editing files.
 Important product rules:

- This is a multi-tenant SaaS.

- Tenant isolation is mandatory.

- RBAC must be enforced on tenant-scoped actions.

- Compliance-relevant events must be audit logged.

- CUI upload policy must be enforced.

- Features should follow the acceptance criteria in the Agile/Scrum plan.

- Add focused tests for tenant isolation, permissions, audit logging, and policy enforcement where relevant.

- Treat the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.

- Preserve tenant isolation, RBAC, audit logging, source traceability, and review metadata.

- Follow the existing project structure: React + Vite web app, ASP.NET Core API, application/domain/infrastructure layers, PostgreSQL persistence, and compliance content package.

- Read the referenced story, tasks, and acceptance criteria before editing.

- Keep changes scoped to the story unless a small supporting change is required.

- Add or update tests according to risk, especially for tenant boundaries, authorization, validation, and audit behavior.

- Update docs, API contracts, seed content, or UI states when the behavior changes.

- Verify the relevant build/test commands before handing off, or clearly report anything that could not be run.

After implementation, run the appropriate lint/test/build commands and report what passed or failed.

> Context:
>
> - Epic: Subcontractor Flow-Down Tracker
>
> - User story: As a contracts admin, I want to assign required flow-down clauses so that subcontractor obligations are visible.
>
> - Acceptance criteria:
>
> - Flow-down clauses can be assigned to subcontractors.
> - Flow-down status is visible by subcontractor and contract.
> - Signed evidence can be linked.
> - Status changes are audit logged.
>
> Implement Story 14.2, "Flow-Down Clause Tracking," from `docs/development-phase-use-cases.md`. Add subcontractor flow-down relationships, assignment from contract obligations, status tracking for required/sent/acknowledged/signed/waived/not_applicable, signed evidence links, by-subcontractor and by-contract visibility, and audit logging.

Instructions:

1. Inspect the existing codebase, schema, API spec, and use-case docs before editing.

2. Reuse existing project patterns and avoid broad refactors.

3. Implement the smallest complete vertical slice that satisfies the acceptance criteria.

4. Enforce tenant scoping, RBAC, validation, and audit logging where applicable.

5. Add or update tests for the behavior.

6. Update API/schema/docs only if the implementation changes the contract.

7. Run the relevant verification commands and summarize results.

#-----------------------------------------

### Story 14.3: Subcontractor Evidence Requests
**Status: Implemented**

Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, read the Agile/Scrum plan, API specification, SQL schema, architecture files, and use-case document in this workspace. Then summarize the current implementation state, identify the next Scrum story that should be built, and propose a small implementation plan before editing files.
> Context:
>
> - Epic: Subcontractor Flow-Down Tracker
>
> - User story: As a compliance manager, I want to request evidence from subcontractors so that supplier compliance gaps can be closed.
>
> - Acceptance criteria:
>
> - User can create an evidence request for a subcontractor.
> - Request appears on calendar.
> - Received evidence can satisfy the request.
> - Overdue requests are flagged.
>
> Implement Story 14.3, "Subcontractor Evidence Requests," from `docs/development-phase-use-cases.md`. Add evidence requests for subcontractors with requested item, due date, status, recipient, linked obligation, internal MVP workflow, overdue tracking, calendar visibility, received evidence links, and tenant-safe access.

Instructions:

1. Inspect the existing codebase, schema, API spec, and use-case docs before editing.

2. Reuse existing project patterns and avoid broad refactors.

3. Implement the smallest complete vertical slice that satisfies the acceptance criteria.

4. Enforce tenant scoping, RBAC, validation, and audit logging where applicable.

5. Add or update tests for the behavior.

6. Update API/schema/docs only if the implementation changes the contract.

7. Run the relevant verification commands and summarize results.

#-----------------------------------------

## 15. Reports

### Story 15.1: Compliance Status Report
**Status: Implemented**

Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, read the Agile/Scrum plan, API specification, SQL schema, architecture files, and use-case document in this workspace. Then summarize the current implementation state, identify the next Scrum story that should be built, and propose a small implementation plan before editing files.
 Important product rules:

- This is a multi-tenant SaaS.

- Tenant isolation is mandatory.

- RBAC must be enforced on tenant-scoped actions.

- Compliance-relevant events must be audit logged.

- CUI upload policy must be enforced.

- Features should follow the acceptance criteria in the Agile/Scrum plan.

- Add focused tests for tenant isolation, permissions, audit logging, and policy enforcement where relevant.

- Treat the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.

- Preserve tenant isolation, RBAC, audit logging, source traceability, and review metadata.

- Follow the existing project structure: React + Vite web app, ASP.NET Core API, application/domain/infrastructure layers, PostgreSQL persistence, and compliance content package.

- Read the referenced story, tasks, and acceptance criteria before editing.

- Keep changes scoped to the story unless a small supporting change is required.

- Add or update tests according to risk, especially for tenant boundaries, authorization, validation, and audit behavior.

- Update docs, API contracts, seed content, or UI states when the behavior changes.

- Verify the relevant build/test commands before handing off, or clearly report anything that could not be run.

After implementation, run the appropriate lint/test/build commands and report what passed or failed.

> Context:
>
> - Epic: Reports
>
> - User story: As a company owner, I want a compliance status report so that I can see overall risk and readiness.
>
> - Acceptance criteria:
>
> - Report includes current status summary.
> - Report is tenant scoped.
> - Report includes generation timestamp.
> - Report generation is audit logged.
>
> Implement Story 15.1, "Compliance Status Report," from `docs/development-phase-use-cases.md`. Define report snapshots and generate a tenant-scoped compliance status report with obligation status, overdue tasks, evidence status, CMMC progress, subcontractor gaps, high-risk items, timestamp, HTML or PDF export for MVP, and audit logging.

Instructions:

1. Inspect the existing codebase, schema, API spec, and use-case docs before editing.

2. Reuse existing project patterns and avoid broad refactors.

3. Implement the smallest complete vertical slice that satisfies the acceptance criteria.

4. Enforce tenant scoping, RBAC, validation, and audit logging where applicable.

5. Add or update tests for the behavior.

6. Update API/schema/docs only if the implementation changes the contract.

7. Run the relevant verification commands and summarize results.

#-----------------------------------------

### Story 15.2: Contract Obligation Matrix
**Status: Implemented**

Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, read the Agile/Scrum plan, API specification, SQL schema, architecture files, and use-case document in this workspace. Then summarize the current implementation state, identify the next Scrum story that should be built, and propose a small implementation plan before editing files.
 Important product rules:

- This is a multi-tenant SaaS.

- Tenant isolation is mandatory.

- RBAC must be enforced on tenant-scoped actions.

- Compliance-relevant events must be audit logged.

- CUI upload policy must be enforced.

- Features should follow the acceptance criteria in the Agile/Scrum plan.

- Add focused tests for tenant isolation, permissions, audit logging, and policy enforcement where relevant.

- Treat the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.

- Preserve tenant isolation, RBAC, audit logging, source traceability, and review metadata.

- Follow the existing project structure: React + Vite web app, ASP.NET Core API, application/domain/infrastructure layers, PostgreSQL persistence, and compliance content package.

- Read the referenced story, tasks, and acceptance criteria before editing.

- Keep changes scoped to the story unless a small supporting change is required.

- Add or update tests according to risk, especially for tenant boundaries, authorization, validation, and audit behavior.

- Update docs, API contracts, seed content, or UI states when the behavior changes.

- Verify the relevant build/test commands before handing off, or clearly report anything that could not be run.

After implementation, run the appropriate lint/test/build commands and report what passed or failed.

> Context:
>
> - Epic: Reports
>
> - User story: As a contracts admin, I want a contract obligation matrix so that I can review clauses, obligations, owners, evidence, and due dates by contract.
>
> - Acceptance criteria:
>
> - User can generate matrix for one contract.
> - Matrix includes source links and last reviewed dates.
> - Matrix includes flow-down indicators.
> - Export matches on-screen data.
>
> Implement Story 15.2, "Contract Obligation Matrix," from `docs/development-phase-use-cases.md`. Build the contract-level obligation matrix query/UI/export with clause, source, obligation, owner, status, risk, due date, evidence, flow-down requirement, source links, and last reviewed dates. Ensure exported data matches on-screen data.

Instructions:

1. Inspect the existing codebase, schema, API spec, and use-case docs before editing.

2. Reuse existing project patterns and avoid broad refactors.

3. Implement the smallest complete vertical slice that satisfies the acceptance criteria.

4. Enforce tenant scoping, RBAC, validation, and audit logging where applicable.

5. Add or update tests for the behavior.

6. Update API/schema/docs only if the implementation changes the contract.

7. Run the relevant verification commands and summarize results.

#-----------------------------------------

### Story 15.3: CMMC Readiness Report
**Status: Implemented**

Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, read the Agile/Scrum plan, API specification, SQL schema, architecture files, and use-case document in this workspace. Then summarize the current implementation state, identify the next Scrum story that should be built, and propose a small implementation plan before editing files.
 Important product rules:

- This is a multi-tenant SaaS.

- Tenant isolation is mandatory.

- RBAC must be enforced on tenant-scoped actions.

- Compliance-relevant events must be audit logged.

- CUI upload policy must be enforced.

- Features should follow the acceptance criteria in the Agile/Scrum plan.

- Add focused tests for tenant isolation, permissions, audit logging, and policy enforcement where relevant.

- Treat the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.

- Preserve tenant isolation, RBAC, audit logging, source traceability, and review metadata.

- Follow the existing project structure: React + Vite web app, ASP.NET Core API, application/domain/infrastructure layers, PostgreSQL persistence, and compliance content package.

- Read the referenced story, tasks, and acceptance criteria before editing.

- Keep changes scoped to the story unless a small supporting change is required.

- Add or update tests according to risk, especially for tenant boundaries, authorization, validation, and audit behavior.

- Update docs, API contracts, seed content, or UI states when the behavior changes.

- Verify the relevant build/test commands before handing off, or clearly report anything that could not be run.

After implementation, run the appropriate lint/test/build commands and report what passed or failed.

> Context:
>
> - Epic: Reports
>
> - User story: As an IT/security owner, I want a CMMC readiness report so that leadership and advisors can see control progress and gaps.
>
> - Acceptance criteria:
>
> - Report shows CMMC progress by control family or category.
> - Open POA&M items are included.
> - Evidence links only include records the user can access.
> - Report access is RBAC protected.
>
> Implement Story 15.3, "CMMC Readiness Report," from `docs/development-phase-use-cases.md`. Generate a role-protected CMMC readiness report with target level, control statuses, evidence links the user can access, POA&M items, open gaps, affirmation dates, progress by family/category, export, and report snapshot history.

Instructions:

1. Inspect the existing codebase, schema, API spec, and use-case docs before editing.

2. Reuse existing project patterns and avoid broad refactors.

3. Implement the smallest complete vertical slice that satisfies the acceptance criteria.

4. Enforce tenant scoping, RBAC, validation, and audit logging where applicable.

5. Add or update tests for the behavior.

6. Update API/schema/docs only if the implementation changes the contract.

7. Run the relevant verification commands and summarize results.

#-----------------------------------------

### Story 15.4: Evidence Package
**Status: Implemented**

Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, read the Agile/Scrum plan, API specification, SQL schema, architecture files, and use-case document in this workspace. Then summarize the current implementation state, identify the next Scrum story that should be built, and propose a small implementation plan before editing files.
 Important product rules:

- This is a multi-tenant SaaS.

- Tenant isolation is mandatory.

- RBAC must be enforced on tenant-scoped actions.

- Compliance-relevant events must be audit logged.

- CUI upload policy must be enforced.

- Features should follow the acceptance criteria in the Agile/Scrum plan.

- Add focused tests for tenant isolation, permissions, audit logging, and policy enforcement where relevant.

- Treat the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.

- Preserve tenant isolation, RBAC, audit logging, source traceability, and review metadata.

- Follow the existing project structure: React + Vite web app, ASP.NET Core API, application/domain/infrastructure layers, PostgreSQL persistence, and compliance content package.

- Read the referenced story, tasks, and acceptance criteria before editing.

- Keep changes scoped to the story unless a small supporting change is required.

- Add or update tests according to risk, especially for tenant boundaries, authorization, validation, and audit behavior.

- Update docs, API contracts, seed content, or UI states when the behavior changes.

- Verify the relevant build/test commands before handing off, or clearly report anything that could not be run.

After implementation, run the appropriate lint/test/build commands and report what passed or failed.

> Context:
>
> - Epic: Reports
>
> - User story: As a compliance manager, I want to generate an evidence package so that I can respond to a prime contractor or auditor request.
>
> - Acceptance criteria:
>
> - Evidence package includes selected scope and approved evidence.
> - Draft or rejected evidence is excluded unless explicitly allowed by authorized user.
> - Package includes manifest with title, evidence type, linked obligation/control, approval state, and timestamp.
> - Package generation is audit logged.
>
> Implement Story 15.4, "Evidence Package," from `docs/development-phase-use-cases.md`. Let authorized users generate evidence packages scoped by obligations, contract, CMMC controls, or subcontractor. Include approved evidence by default, support explicit authorized inclusion of draft/rejected evidence if required, produce a metadata manifest, provide a read-only package view, and audit generation.

Instructions:

1. Inspect the existing codebase, schema, API spec, and use-case docs before editing.

2. Reuse existing project patterns and avoid broad refactors.

3. Implement the smallest complete vertical slice that satisfies the acceptance criteria.

4. Enforce tenant scoping, RBAC, validation, and audit logging where applicable.

5. Add or update tests for the behavior.

6. Update API/schema/docs only if the implementation changes the contract.

7. Run the relevant verification commands and summarize results.

#-----------------------------------------

### Story 15.5: Subcontractor Compliance Report
**Status: Implemented**

Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, read the Agile/Scrum plan, API specification, SQL schema, architecture files, and use-case document in this workspace. Then summarize the current implementation state, identify the next Scrum story that should be built, and propose a small implementation plan before editing files.
 Important product rules:

- This is a multi-tenant SaaS.

- Tenant isolation is mandatory.

- RBAC must be enforced on tenant-scoped actions.

- Compliance-relevant events must be audit logged.

- CUI upload policy must be enforced.

- Features should follow the acceptance criteria in the Agile/Scrum plan.

- Add focused tests for tenant isolation, permissions, audit logging, and policy enforcement where relevant.

- Treat the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.

- Preserve tenant isolation, RBAC, audit logging, source traceability, and review metadata.

- Follow the existing project structure: React + Vite web app, ASP.NET Core API, application/domain/infrastructure layers, PostgreSQL persistence, and compliance content package.

- Read the referenced story, tasks, and acceptance criteria before editing.

- Keep changes scoped to the story unless a small supporting change is required.

- Add or update tests according to risk, especially for tenant boundaries, authorization, validation, and audit behavior.

- Update docs, API contracts, seed content, or UI states when the behavior changes.

- Verify the relevant build/test commands before handing off, or clearly report anything that could not be run.

After implementation, run the appropriate lint/test/build commands and report what passed or failed.

> Context:
>
> - Epic: Reports
>
> - User story: As a contracts admin, I want a subcontractor compliance report so that I can monitor supplier readiness.
>
> - Acceptance criteria:
>
> - Report can be filtered by contract.
> - Report flags missing or overdue subcontractor evidence.
> - Report includes flow-down status.
> - Export is tenant scoped.
>
> Implement Story 15.5, "Subcontractor Compliance Report," from `docs/development-phase-use-cases.md`. Generate tenant-scoped subcontractor reports with profile status, flow-down status, CMMC status, insurance expiration, NDA status, evidence requests, overdue items, contract filter, risk summary, and export.

Instructions:

1. Inspect the existing codebase, schema, API spec, and use-case docs before editing.

2. Reuse existing project patterns and avoid broad refactors.

3. Implement the smallest complete vertical slice that satisfies the acceptance criteria.

4. Enforce tenant scoping, RBAC, validation, and audit logging where applicable.

5. Add or update tests for the behavior.

6. Update API/schema/docs only if the implementation changes the contract.

7. Run the relevant verification commands and summarize results.

#-----------------------------------------

## 16. Notifications

### Story 16.1: Notification Preferences
**Status: Implemented**

Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, read the Agile/Scrum plan, API specification, SQL schema, architecture files, and use-case document in this workspace. Then summarize the current implementation state, identify the next Scrum story that should be built, and propose a small implementation plan before editing files.
 Important product rules:

- This is a multi-tenant SaaS.

- Tenant isolation is mandatory.

- RBAC must be enforced on tenant-scoped actions.

- Compliance-relevant events must be audit logged.

- CUI upload policy must be enforced.

- Features should follow the acceptance criteria in the Agile/Scrum plan.

- Add focused tests for tenant isolation, permissions, audit logging, and policy enforcement where relevant.

- Treat the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.

- Preserve tenant isolation, RBAC, audit logging, source traceability, and review metadata.

- Follow the existing project structure: React + Vite web app, ASP.NET Core API, application/domain/infrastructure layers, PostgreSQL persistence, and compliance content package.

- Read the referenced story, tasks, and acceptance criteria before editing.

- Keep changes scoped to the story unless a small supporting change is required.

- Add or update tests according to risk, especially for tenant boundaries, authorization, validation, and audit behavior.

- Update docs, API contracts, seed content, or UI states when the behavior changes.

- Verify the relevant build/test commands before handing off, or clearly report anything that could not be run.

After implementation, run the appropriate lint/test/build commands and report what passed or failed.

> Context:
>
> - Epic: Notifications
>
> - User story: As a user, I want notification preferences so that reminders are useful and not noisy.
>
> - Acceptance criteria:
>
> - Users can update notification preferences.
> - Defaults exist for new users.
> - Preferences are tenant scoped when needed.
> - Preference changes are audit logged.
>
> Implement Story 16.1, "Notification Preferences," from `docs/development-phase-use-cases.md`. Add notification preferences for assignments, due soon, overdue, evidence requests, certification renewals, and CMMC affirmation. Include defaults by role, UI settings, tenant-aware preference behavior, validation, and audit logging.

Instructions:

1. Inspect the existing codebase, schema, API spec, and use-case docs before editing.

2. Reuse existing project patterns and avoid broad refactors.

3. Implement the smallest complete vertical slice that satisfies the acceptance criteria.

4. Enforce tenant scoping, RBAC, validation, and audit logging where applicable.

5. Add or update tests for the behavior.

6. Update API/schema/docs only if the implementation changes the contract.

7. Run the relevant verification commands and summarize results.

#-----------------------------------------

### Story 16.2: Due-Date Reminders
**Status: Implemented**

Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, read the Agile/Scrum plan, API specification, SQL schema, architecture files, and use-case document in this workspace. Then summarize the current implementation state, identify the next Scrum story that should be built, and propose a small implementation plan before editing files.
 Important product rules:

- This is a multi-tenant SaaS.

- Tenant isolation is mandatory.

- RBAC must be enforced on tenant-scoped actions.

- Compliance-relevant events must be audit logged.

- CUI upload policy must be enforced.

- Features should follow the acceptance criteria in the Agile/Scrum plan.

- Add focused tests for tenant isolation, permissions, audit logging, and policy enforcement where relevant.

- Treat the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.

- Preserve tenant isolation, RBAC, audit logging, source traceability, and review metadata.

- Follow the existing project structure: React + Vite web app, ASP.NET Core API, application/domain/infrastructure layers, PostgreSQL persistence, and compliance content package.

- Read the referenced story, tasks, and acceptance criteria before editing.

- Keep changes scoped to the story unless a small supporting change is required.

- Add or update tests according to risk, especially for tenant boundaries, authorization, validation, and audit behavior.

- Update docs, API contracts, seed content, or UI states when the behavior changes.

- Verify the relevant build/test commands before handing off, or clearly report anything that could not be run.

After implementation, run the appropriate lint/test/build commands and report what passed or failed.

> Context:
>
> - Epic: Notifications
>
> - User story: As a compliance manager, I want reminders before due dates so that I can act before obligations are overdue.
>
> - Acceptance criteria:
>
> - Reminder job identifies upcoming tasks based on configured lead time.
> - Same reminder is not sent repeatedly for the same event.
> - Overdue reminders are sent separately.
> - Reminder delivery failures are logged.
>
> Implement Story 16.2, "Due-Date Reminders," from `docs/development-phase-use-cases.md`. Add an idempotent reminder job that finds upcoming and overdue tasks based on configured lead time, emits in-app notifications and an email placeholder, separates overdue reminders, and logs delivery failures.

Instructions:

1. Inspect the existing codebase, schema, API spec, and use-case docs before editing.

2. Reuse existing project patterns and avoid broad refactors.

3. Implement the smallest complete vertical slice that satisfies the acceptance criteria.

4. Enforce tenant scoping, RBAC, validation, and audit logging where applicable.

5. Add or update tests for the behavior.

6. Update API/schema/docs only if the implementation changes the contract.

7. Run the relevant verification commands and summarize results.

#-----------------------------------------

### Story 16.3: Assignment Notifications
**Status: Implemented**

Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, read the Agile/Scrum plan, API specification, SQL schema, architecture files, and use-case document in this workspace. Then summarize the current implementation state, identify the next Scrum story that should be built, and propose a small implementation plan before editing files.
 Important product rules:

- This is a multi-tenant SaaS.

- Tenant isolation is mandatory.

- RBAC must be enforced on tenant-scoped actions.

- Compliance-relevant events must be audit logged.

- CUI upload policy must be enforced.

- Features should follow the acceptance criteria in the Agile/Scrum plan.

- Add focused tests for tenant isolation, permissions, audit logging, and policy enforcement where relevant.

- Treat the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.

- Preserve tenant isolation, RBAC, audit logging, source traceability, and review metadata.

- Follow the existing project structure: React + Vite web app, ASP.NET Core API, application/domain/infrastructure layers, PostgreSQL persistence, and compliance content package.

- Read the referenced story, tasks, and acceptance criteria before editing.

- Keep changes scoped to the story unless a small supporting change is required.

- Add or update tests according to risk, especially for tenant boundaries, authorization, validation, and audit behavior.

- Update docs, API contracts, seed content, or UI states when the behavior changes.

- Verify the relevant build/test commands before handing off, or clearly report anything that could not be run.

After implementation, run the appropriate lint/test/build commands and report what passed or failed.

> Context:
>
> - Epic: Notifications
>
> - User story: As a user, I want to be notified when work is assigned to me so that I know what requires my attention.
>
> - Acceptance criteria:
>
> - Assigned users receive notification.
> - Notification links to the relevant record.
> - User can mark notification as read.
> - Unauthorized users cannot open linked records.
>
> Implement Story 16.3, "Assignment Notifications," from `docs/development-phase-use-cases.md`. Emit notifications when tasks, obligations, POA&M items, or evidence requests are assigned; add a notification center UI; support marking notifications as read; link notifications to source records; and enforce authorization when opening linked records.

Instructions:

1. Inspect the existing codebase, schema, API spec, and use-case docs before editing.

2. Reuse existing project patterns and avoid broad refactors.

3. Implement the smallest complete vertical slice that satisfies the acceptance criteria.

4. Enforce tenant scoping, RBAC, validation, and audit logging where applicable.

5. Add or update tests for the behavior.

6. Update API/schema/docs only if the implementation changes the contract.

7. Run the relevant verification commands and summarize results.

#-----------------------------------------

## 17. MVP Hardening And Release Readiness

### Story 17.1: End-To-End Pilot Workflow
**Status: Partially implemented**

Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, read the Agile/Scrum plan, API specification, SQL schema, architecture files, and use-case document in this workspace. Then summarize the current implementation state, identify the next Scrum story that should be built, and propose a small implementation plan before editing files.
 Important product rules:

- This is a multi-tenant SaaS.

- Tenant isolation is mandatory.

- RBAC must be enforced on tenant-scoped actions.

- Compliance-relevant events must be audit logged.

- CUI upload policy must be enforced.

- Features should follow the acceptance criteria in the Agile/Scrum plan.

- Add focused tests for tenant isolation, permissions, audit logging, and policy enforcement where relevant.

- Treat the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.

- Preserve tenant isolation, RBAC, audit logging, source traceability, and review metadata.

- Follow the existing project structure: React + Vite web app, ASP.NET Core API, application/domain/infrastructure layers, PostgreSQL persistence, and compliance content package.

- Read the referenced story, tasks, and acceptance criteria before editing.

- Keep changes scoped to the story unless a small supporting change is required.

- Add or update tests according to risk, especially for tenant boundaries, authorization, validation, and audit behavior.

- Update docs, API contracts, seed content, or UI states when the behavior changes.

- Verify the relevant build/test commands before handing off, or clearly report anything that could not be run.

After implementation, run the appropriate lint/test/build commands and report what passed or failed.

> Context:
>
> - Epic: MVP Hardening And Release Readiness
>
> - User story: As a product owner, I want a complete pilot workflow tested so that we know the MVP supports the core promise.
>
> - Acceptance criteria:
>
> - One pilot tenant can complete all MVP workflows with non-CUI data.
> - Role-specific users can only perform permitted actions.
> - Reports reflect the data created during the workflow.
> - Critical workflow defects are resolved before release.
>
> Implement Story 17.1, "End-To-End Pilot Workflow," from `docs/development-phase-use-cases.md`. Create a representative pilot tenant and users for owner, admin, compliance manager, contributor, auditor, and advisor. Exercise onboarding, profile, contract intake, clause tagging, obligations, calendar, evidence upload, CMMC, subcontractors, reports, and notifications with non-CUI data. Fix release blockers and add regression coverage for the happy path.

Instructions:

1. Inspect the existing codebase, schema, API spec, and use-case docs before editing.

2. Reuse existing project patterns and avoid broad refactors.

3. Implement the smallest complete vertical slice that satisfies the acceptance criteria.

4. Enforce tenant scoping, RBAC, validation, and audit logging where applicable.

5. Add or update tests for the behavior.

6. Update API/schema/docs only if the implementation changes the contract.

7. Run the relevant verification commands and summarize results.

#-----------------------------------------

### Story 17.2: Security And Tenant Isolation Verification
**Status: Implemented**

Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, read the Agile/Scrum plan, API specification, SQL schema, architecture files, and use-case document in this workspace. Then summarize the current implementation state, identify the next Scrum story that should be built, and propose a small implementation plan before editing files.
 Important product rules:

- This is a multi-tenant SaaS.

- Tenant isolation is mandatory.

- RBAC must be enforced on tenant-scoped actions.

- Compliance-relevant events must be audit logged.

- CUI upload policy must be enforced.

- Features should follow the acceptance criteria in the Agile/Scrum plan.

- Add focused tests for tenant isolation, permissions, audit logging, and policy enforcement where relevant.

- Treat the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.

- Preserve tenant isolation, RBAC, audit logging, source traceability, and review metadata.

- Follow the existing project structure: React + Vite web app, ASP.NET Core API, application/domain/infrastructure layers, PostgreSQL persistence, and compliance content package.

- Read the referenced story, tasks, and acceptance criteria before editing.

- Keep changes scoped to the story unless a small supporting change is required.

- Add or update tests according to risk, especially for tenant boundaries, authorization, validation, and audit behavior.

- Update docs, API contracts, seed content, or UI states when the behavior changes.

- Verify the relevant build/test commands before handing off, or clearly report anything that could not be run.

After implementation, run the appropriate lint/test/build commands and report what passed or failed.

> Context:
>
> - Epic: MVP Hardening And Release Readiness
>
> - User story: As a security lead, I want tenant isolation and RBAC tested so that customer data boundaries are enforced.
>
> - Acceptance criteria:
>
> - Cross-tenant API access is denied.
> - Restricted role actions are denied server-side.
> - Tenant-owned records are filtered by tenant in repositories and services.
> - Security test results are documented.
>
> Implement Story 17.2, "Security And Tenant Isolation Verification," from `docs/development-phase-use-cases.md`. Add automated security tests for cross-tenant access, server-side RBAC denial, direct API calls that bypass hidden UI controls, tenant-owned query filtering, and audit logging for sensitive workflows. Document security test results.

Instructions:

1. Inspect the existing codebase, schema, API spec, and use-case docs before editing.

2. Reuse existing project patterns and avoid broad refactors.

3. Implement the smallest complete vertical slice that satisfies the acceptance criteria.

4. Enforce tenant scoping, RBAC, validation, and audit logging where applicable.

5. Add or update tests for the behavior.

6. Update API/schema/docs only if the implementation changes the contract.

7. Run the relevant verification commands and summarize results.

#-----------------------------------------

### Story 17.3: Staging Environment
**Status: Partially implemented**

Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, read the Agile/Scrum plan, API specification, SQL schema, architecture files, and use-case document in this workspace. Then summarize the current implementation state, identify the next Scrum story that should be built, and propose a small implementation plan before editing files.
 Important product rules:

- This is a multi-tenant SaaS.

- Tenant isolation is mandatory.

- RBAC must be enforced on tenant-scoped actions.

- Compliance-relevant events must be audit logged.

- CUI upload policy must be enforced.

- Features should follow the acceptance criteria in the Agile/Scrum plan.

- Add focused tests for tenant isolation, permissions, audit logging, and policy enforcement where relevant.

- Treat the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.

- Preserve tenant isolation, RBAC, audit logging, source traceability, and review metadata.

- Follow the existing project structure: React + Vite web app, ASP.NET Core API, application/domain/infrastructure layers, PostgreSQL persistence, and compliance content package.

- Read the referenced story, tasks, and acceptance criteria before editing.

- Keep changes scoped to the story unless a small supporting change is required.

- Add or update tests according to risk, especially for tenant boundaries, authorization, validation, and audit behavior.

- Update docs, API contracts, seed content, or UI states when the behavior changes.

- Verify the relevant build/test commands before handing off, or clearly report anything that could not be run.

After implementation, run the appropriate lint/test/build commands and report what passed or failed.

> Context:
>
> - Epic: MVP Hardening And Release Readiness
>
> - User story: As a delivery lead, I want a production-like staging environment so that releases can be verified before production.
>
> - Acceptance criteria:
>
> - Staging can deploy from CI/CD.
> - Staging has no production customer data.
> - Health checks cover API, database, cache, storage, and jobs.
> - Smoke tests pass after deployment.
>
> Implement Story 17.3, "Staging Environment," from `docs/development-phase-use-cases.md`. Provision or document production-like staging for API, web app, database, object storage, cache, queue, and secrets. Automate migrations, configure logs, health checks, basic alerts, and staging smoke tests. Ensure staging deploys from CI/CD and contains no production customer data.

Instructions:

1. Inspect the existing codebase, schema, API spec, and use-case docs before editing.

2. Reuse existing project patterns and avoid broad refactors.

3. Implement the smallest complete vertical slice that satisfies the acceptance criteria.

4. Enforce tenant scoping, RBAC, validation, and audit logging where applicable.

5. Add or update tests for the behavior.

6. Update API/schema/docs only if the implementation changes the contract.

7. Run the relevant verification commands and summarize results.

#-----------------------------------------

### Story 17.4: Production Readiness Checklist
**Status: Partially implemented**

Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, read the Agile/Scrum plan, API specification, SQL schema, architecture files, and use-case document in this workspace. Then summarize the current implementation state, identify the next Scrum story that should be built, and propose a small implementation plan before editing files.
 Important product rules:

- This is a multi-tenant SaaS.

- Tenant isolation is mandatory.

- RBAC must be enforced on tenant-scoped actions.

- Compliance-relevant events must be audit logged.

- CUI upload policy must be enforced.

- Features should follow the acceptance criteria in the Agile/Scrum plan.

- Add focused tests for tenant isolation, permissions, audit logging, and policy enforcement where relevant.

- Treat the MVP as **No-CUI / compliance management only with synthetic CUI-ready demonstration workflows**.

- Preserve tenant isolation, RBAC, audit logging, source traceability, and review metadata.

- Follow the existing project structure: React + Vite web app, ASP.NET Core API, application/domain/infrastructure layers, PostgreSQL persistence, and compliance content package.

- Read the referenced story, tasks, and acceptance criteria before editing.

- Keep changes scoped to the story unless a small supporting change is required.

- Add or update tests according to risk, especially for tenant boundaries, authorization, validation, and audit behavior.

- Update docs, API contracts, seed content, or UI states when the behavior changes.

- Verify the relevant build/test commands before handing off, or clearly report anything that could not be run.

After implementation, run the appropriate lint/test/build commands and report what passed or failed.

> Context:
>
> - Epic: MVP Hardening And Release Readiness
>
> - User story: As a product owner, I want a release checklist so that launch risks are reviewed deliberately.
>
> - Acceptance criteria:
>
> - Checklist is complete before production launch.
> - Known limitations are documented.
> - Launch content has source URLs and review metadata.
> - Rollback plan is documented and tested in staging.
>
> Implement Story 17.4, "Production Readiness Checklist," from `docs/development-phase-use-cases.md`. Create the MVP production readiness checklist covering data handling notice, terms, support path, prohibited upload guidance, backups, restore test, logs, alerts, rollback plan, malware scanning path or limitation, expert-reviewed content, release notes, known limitations, source URLs, review metadata, and staging rollback verification.

Instructions:

1. Inspect the existing codebase, schema, API spec, and use-case docs before editing.

2. Reuse existing project patterns and avoid broad refactors.

3. Implement the smallest complete vertical slice that satisfies the acceptance criteria.

4. Enforce tenant scoping, RBAC, validation, and audit logging where applicable.

5. Add or update tests for the behavior.

6. Update API/schema/docs only if the implementation changes the contract.

7. Run the relevant verification commands and summarize results.

#-----------------------------------------

### Phase 2 ###

Use the shared prompt requirements above for every Phase 2 story. Each story prompt below is intended to be copied into a fresh implementation thread after the prior story has been completed and verified.

## 18. Automated Clause Extraction

### Story 18.1: Extraction Job Intake
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for automated clause extraction and propose a small implementation plan before editing files.

> Context:
>
> - Epic: Automated Clause Extraction
> - User story: As a compliance manager, I want to start clause extraction from a contract document so that the system can analyze the document asynchronously.
> - Acceptance criteria:
> - User with contract edit permission can start extraction for a document in the current tenant.
> - User without contract edit permission receives a server-side authorization error.
> - Extraction job stores tenant ID, source document ID, requester ID, status, and timestamps.
> - Starting extraction for another tenant's document is denied.
> - Extraction job creation is audit logged.

Implement Story 18.1, "Extraction Job Intake," from `docs/development-phase-use-cases.md`. Add the extraction job model, API endpoint, queue/background worker stub, contract document UI action, and audit events for job creation, completion, and failure. Preserve tenant isolation, RBAC, validation, audit logging, and the No-CUI MVP posture with synthetic CUI-ready demonstration workflows. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------

### Story 18.2: Text Extraction And Clause Candidate Detection
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing extraction job code, upload policy, document storage, clause library, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state and propose a small implementation plan before editing files.

> Context:
>
> - Epic: Automated Clause Extraction
> - User story: As a compliance manager, I want the system to detect clause candidates from contract text so that I can review likely matches before applying them.
> - Acceptance criteria:
> - Supported text documents produce clause candidates when recognizable clause references are present.
> - Each candidate includes source document, normalized citation, raw extracted text, confidence, and location metadata when available.
> - Exact matches link to the corresponding clause library record.
> - Unsupported or unreadable documents produce a failed job with a user-visible reason.
> - Extracted text and candidates remain tenant-scoped.

Implement Story 18.2, "Text Extraction And Clause Candidate Detection," from `docs/development-phase-use-cases.md`. Extract text from MVP-allowed non-CUI formats, detect FAR/DFARS/agency/local clause references, store clause candidates with normalized citation and metadata, link exact or high-confidence library matches, and handle unsupported or unreadable documents with safe failure states. Add tests for parsing, tenant scoping, library matching, and failure handling, then run the relevant verification commands and report results.

#-----------------------------------------

### Story 18.3: Extraction Results Review Screen
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the extraction job/candidate APIs, contract document detail UI, clause tagging workflow, audit behavior, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state and propose a small implementation plan before editing files.

> Context:
>
> - Epic: Automated Clause Extraction
> - User story: As a compliance manager, I want to see extraction results beside the source contract so that I can decide which clauses to accept.
> - Acceptance criteria:
> - User can view extraction results for documents in the current tenant.
> - Results show citation, confidence, match status, review status, and source location when available.
> - Accepted candidates create reviewed contract clause links only after user action.
> - Rejected candidates remain visible in extraction history and do not create contract clause links.
> - Candidate edits and review decisions are audit logged.

Implement Story 18.3, "Extraction Results Review Screen," from `docs/development-phase-use-cases.md`. Add result lists, filters, candidate detail, accept/reject/edit/link actions, empty/processing/failed/completed states, and contract detail status/counts. Ensure accepted candidates create contract clause links only after explicit review. Add backend and React tests for permissions, tenant scoping, review actions, states, and audit behavior, then run verification.

#-----------------------------------------

## 19. Human Review Workflow

### Story 19.1: Review States For Extracted Clauses
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect extraction candidate models, services, APIs, UI review flows, permissions, audit logging, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state and propose a small implementation plan before editing files.

> Context:
>
> - Epic: Human Review Workflow
> - User story: As a compliance manager, I want extracted clauses to move through explicit review states so that unreviewed results cannot be treated as authoritative.
> - Acceptance criteria:
> - New extraction candidates default to pending review.
> - Only users with clause review permission can accept or reject candidates.
> - Accepted candidates record reviewer, reviewed date, and decision note when provided.
> - Rejected and superseded candidates do not generate obligations.
> - Review state transitions are audit logged.

Implement Story 19.1, "Review States For Extracted Clauses," from `docs/development-phase-use-cases.md`. Add explicit review states, reviewer metadata, allowed transition enforcement, review filters, and audit events. Prevent rejected or superseded candidates from generating obligations. Add focused tests for state transitions, authorization, audit logging, and tenant isolation, then run verification.

#-----------------------------------------

### Story 19.2: AI-Suggested Obligation Review
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect obligation models, obligation dashboards/reports, AI or suggestion placeholders, content review patterns, audit logging, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state and propose a small implementation plan before editing files.

> Context:
>
> - Epic: Human Review Workflow
> - User story: As a compliance SME, I want AI-suggested obligations to require review before publication so that draft content is not shown as approved compliance guidance.
> - Acceptance criteria:
> - AI-suggested obligations are stored with source references, confidence, and draft status.
> - Draft suggestions are not included in approved obligation dashboards or reports.
> - Reviewer can approve, revise, reject, or escalate a suggestion.
> - Approved suggestions record reviewer, approval date, and source citations.
> - Rejected suggestions remain in review history and are audit logged.

Implement Story 19.2, "AI-Suggested Obligation Review," from `docs/development-phase-use-cases.md`. Model suggested obligations separately from approved obligations, store generation/source metadata, add approve/revise/reject/escalate workflow, label suggestions as draft, and exclude draft suggestions from approved customer dashboards and reports. Add tests for review states, report exclusion, audit logging, and permissions, then run verification.

#-----------------------------------------

### Story 19.3: Expert Escalation Queue
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect review workflows, notification patterns, permissions, audit logging, queue/list UI patterns, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state and propose a small implementation plan before editing files.

> Context:
>
> - Epic: Human Review Workflow
> - User story: As a compliance content owner, I want uncertain clause and obligation decisions escalated to experts so that high-risk interpretations receive qualified review.
> - Acceptance criteria:
> - Reviewer can escalate a candidate or suggested obligation with a required reason.
> - Escalated items appear in an expert review queue.
> - Assigned expert receives a notification.
> - Resolution records decision, reviewer, date, and notes.
> - Escalated items cannot be published as approved until resolved.

Implement Story 19.3, "Expert Escalation Queue," from `docs/development-phase-use-cases.md`. Add an expert review queue for clause candidates and suggested obligations with priority, topic, assignment, due date, escalation reasons, resolution workflow, filters, and notifications. Block publication until escalation is resolved. Add tests for escalation requirements, queue scoping, notifications, publication blocking, and audit/traceability, then run verification.

#-----------------------------------------

## 20. Clause Library Expansion

### Story 20.1: Versioned Clause Records
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect clause library models, seed/import content, APIs, UI, audit logging, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state and propose a small implementation plan before editing files.

> Context:
>
> - Epic: Clause Library Expansion
> - User story: As a compliance content owner, I want clauses to be versioned so that changes to source text or interpretation are traceable.
> - Acceptance criteria:
> - Clause records include citation, title, source URL, status, last reviewed date, and review owner.
> - Approved versions can be used for extraction matching and obligation mapping.
> - Deprecated or superseded versions are visible in history but not selected by default for new mappings.
> - Clause version changes preserve prior version history.
> - Clause changes are audit logged.

Implement Story 20.1, "Versioned Clause Records," from `docs/development-phase-use-cases.md`. Add version fields, lifecycle statuses, supersedes relationships, curated import/update workflow, clause detail/version history UI and API, and audit events for create/update/approval/deprecation. Add tests for version history, default selection, approved-only matching/mapping, metadata validation, and audit logging, then run verification.

#-----------------------------------------

### Story 20.2: Clause Search And Discovery
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect clause library data, search patterns, permissions, UI list patterns, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state and propose a small implementation plan before editing files.

> Context:
>
> - Epic: Clause Library Expansion
> - User story: As a contracts user, I want to search the clause library by citation, title, source, and obligation area so that I can quickly find the correct clause.
> - Acceptance criteria:
> - Search by exact citation returns the matching approved clause when present.
> - Search by title or keyword returns relevant approved clauses.
> - Filters narrow results by source family, obligation area, and flow-down relevance.
> - Results show source URL, status, and last reviewed date.
> - Draft or under-review clauses are hidden from standard users unless they have content review permission.

Implement Story 20.2, "Clause Search And Discovery," from `docs/development-phase-use-cases.md`. Add searchable fields, source/area/risk/flow-down filters, tenant-safe approved-content search API, and UI results with source, status, confidence, last reviewed date, empty states, and reviewer-only draft visibility. Add tests for exact citation search, keyword search, filters, permissions, and hidden draft content, then run verification.

#-----------------------------------------

### Story 20.3: Clause-To-Obligation Mapping
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect clause library, obligation templates, contract obligation generation, review metadata, audit logging, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state and propose a small implementation plan before editing files.

> Context:
>
> - Epic: Clause Library Expansion
> - User story: As a compliance content owner, I want clauses mapped to approved obligation templates so that accepted clauses can generate consistent obligations.
> - Acceptance criteria:
> - Approved clause mapping can generate an obligation for a contract.
> - Mapping requires trigger condition, required action, source URL, confidence, and review metadata before approval.
> - Draft mappings cannot generate customer-visible approved obligations.
> - Mapping changes preserve history.
> - Mapping approval and changes are audit logged.

Implement Story 20.3, "Clause-To-Obligation Mapping," from `docs/development-phase-use-cases.md`. Add clause-version-to-obligation-template mappings with trigger, actions, owner, evidence examples, deadlines, flow-down, risk, confidence, expert review flag, approval workflow, validation, history, and UI. Add tests for approved-only generation, required metadata validation, draft exclusion, history, tenant scoping, and audit logging, then run verification.

#-----------------------------------------

## 21. Applicability Engine

### Story 21.1: Applicability Facts Model
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect company, contract, clause, subcontractor, obligation, and CMMC domain models; persistence patterns; docs; tests; and `docs/development-phase-use-cases.md`. Then summarize the current implementation state and propose a small implementation plan before editing files.

> Context:
>
> - Epic: Applicability Engine
> - User story: As a developer, I want a structured facts model so that applicability decisions can be computed consistently.
> - Acceptance criteria:
> - Applicability facts can be derived from existing company, contract, clause, and subcontractor records.
> - Unknown facts are represented explicitly instead of inferred as false.
> - Each fact records source record and last updated date when available.
> - Fact model is tenant-scoped.
> - Fact definitions are documented.

Implement Story 21.1, "Applicability Facts Model," from `docs/development-phase-use-cases.md`. Define tenant-scoped facts for company profile, NAICS, certifications, agency, contract type, role, performance location, data type, labor category, clause, subcontractor role, and CUI/FCI indicators. Store provenance, source record, update timestamps, unknown values, and validation. Document fact definitions and sources. Add tests for derivation, unknown handling, provenance, and tenant scoping, then run verification.

#-----------------------------------------

### Story 21.2: Rule Evaluation
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect applicability facts, obligation generation, compliance content metadata, application service patterns, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state and propose a small implementation plan before editing files.

> Context:
>
> - Epic: Applicability Engine
> - User story: As a compliance manager, I want rules evaluated against facts so that obligations are marked applicable, not applicable, or needs review.
> - Acceptance criteria:
> - Rule evaluator returns a result state, explanation, source rule ID, and facts used.
> - Missing required facts produce insufficient information or needs review rather than a silent positive result.
> - Rule evaluation is repeatable for the same inputs.
> - Evaluation results are tenant-scoped.
> - Rule evaluator behavior is covered by automated tests.

Implement Story 21.2, "Rule Evaluation," from `docs/development-phase-use-cases.md`. Add deterministic rule format with conditions, source, confidence, effective date, and review metadata; implement evaluator result states; store explanations and facts used; and cover FAR, DFARS, CMMC, SAM/SBA, and flow-down patterns. Add tests for repeatability, missing facts, state outputs, explanations, tenant scoping, and rule metadata, then run verification.

#-----------------------------------------

### Story 21.3: Obligation Applicability Updates
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect obligation dashboards, company/contract/subcontractor update flows, clause mappings, rule versions, audit logging, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state and propose a small implementation plan before editing files.

> Context:
>
> - Epic: Applicability Engine
> - User story: As a compliance manager, I want obligation applicability to update when relevant facts change so that dashboards stay current.
> - Acceptance criteria:
> - Updating a relevant fact reevaluates affected obligations.
> - Dashboard displays the current applicability state.
> - Explanation shows source rule, facts used, and missing facts when applicable.
> - Prior result history is retained.
> - Material changes from applicable to not applicable or needs review are audit logged.

Implement Story 21.3, "Obligation Applicability Updates," from `docs/development-phase-use-cases.md`. Trigger reevaluation on relevant fact, mapping, data type, subcontractor, or rule version changes; store current and prior applicability results; add dashboard indicators and explanation panel; and audit material changes. Add tests for change triggers, result history, dashboard state, explanations, tenant scoping, and audit logging, then run verification.

#-----------------------------------------

## 22. SAM.gov Entity Lookup

### Story 22.1: SAM.gov API Configuration
**Status: Partially implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect infrastructure configuration, secrets handling, HTTP adapter patterns, health checks, logging, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state and propose a small implementation plan before editing files.

> Context:
>
> - Epic: SAM.gov Entity Lookup
> - User story: As a developer, I want SAM.gov API access configured securely so that entity lookup can run without exposing secrets.
> - Acceptance criteria:
> - SAM.gov API key is not stored in source control.
> - Lookup service uses configured timeout and retry behavior.
> - API failures return a standard, user-safe error.
> - Logs do not contain API keys or sensitive response payloads.
> - Adapter can be replaced or mocked in tests.

Implement Story 22.1, "SAM.gov API Configuration," from `docs/development-phase-use-cases.md`. Add secure configuration for SAM.gov base URL, API key, timeout, retries, and rate limits; create service interface and infrastructure adapter; add safe diagnostics/health behavior; and standardize user-safe errors. Add tests for configuration, mocking, failures, retry/timeout handling, and secret redaction, then run verification.

#-----------------------------------------

### Story 22.2: Company Entity Lookup
**Status: Partially implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect company profile APIs/UI, SAM.gov adapter, audit logging, conflict handling patterns, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state and propose a small implementation plan before editing files.

> Context:
>
> - Epic: SAM.gov Entity Lookup
> - User story: As a tenant admin, I want to search SAM.gov by UEI or legal business name so that I can verify company registration details.
> - Acceptance criteria:
> - Authorized user can search by UEI or legal business name.
> - Search results show source and retrieved date.
> - User can apply selected fields to the company profile.
> - Existing profile values are not overwritten without explicit user confirmation.
> - Applied SAM data changes are audit logged.

Implement Story 22.2, "Company Entity Lookup," from `docs/development-phase-use-cases.md`. Add company lookup form and API, show matched legal name, UEI, CAGE, status, expiration, address, and NAICS data, allow explicit selected-field application with source metadata and conflict confirmation, and audit applied changes. Add tests for authorization, no-overwrite behavior, source metadata, tenant scoping, and audit logging, then run verification.

#-----------------------------------------

### Story 22.3: Subcontractor Entity Lookup
**Status: Partially implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect subcontractor profile APIs/UI, SAM.gov adapter, tenant scoping, audit logging, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state and propose a small implementation plan before editing files.

> Context:
>
> - Epic: SAM.gov Entity Lookup
> - User story: As a subcontractor manager, I want to enrich subcontractor profiles with SAM.gov data so that supplier compliance tracking starts from official entity records.
> - Acceptance criteria:
> - Authorized user can search SAM.gov for a subcontractor by UEI or name.
> - Applied fields update only the current tenant's subcontractor record.
> - No-match and multiple-match results are shown without changing existing data.
> - Source and retrieved date are stored with applied data.
> - Subcontractor SAM updates are audit logged.

Implement Story 22.3, "Subcontractor Entity Lookup," from `docs/development-phase-use-cases.md`. Add subcontractor SAM lookup, display entity status, UEI, CAGE, expiration, NAICS, and exclusion/status indicators when available, support selected-field application, store source metadata, and handle no-match/multiple-match safely. Add tests for tenant scoping, authorization, no-change result states, metadata, and audit logging, then run verification.

#-----------------------------------------

## 23. SBA Size Helper

### Story 23.1: Size Standard Reference Data
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect compliance content import patterns, reference data models, content review metadata, audit logging, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state and propose a small implementation plan before editing files.

> Context:
>
> - Epic: SBA Size Helper
> - User story: As a compliance content owner, I want SBA size standard reference data loaded with source metadata so that size helper calculations are traceable.
> - Acceptance criteria:
> - Approved size standard records include NAICS, metric, threshold, source URL, effective date, last reviewed date, and status.
> - Draft records are not used in customer-facing helper results.
> - Import rejects records missing source metadata.
> - Deprecated records remain visible to content reviewers.
> - Import and approval actions are audit logged.

Implement Story 23.1, "Size Standard Reference Data," from `docs/development-phase-use-cases.md`. Define SBA size standard data fields, import workflow, content review states, required source metadata validation, and audit events. Ensure only approved records feed customer-facing helper results. Add tests for import validation, approval states, deprecated visibility, approved-only usage, and audit logging, then run verification.

#-----------------------------------------

### Story 23.2: Company Size Evaluation Helper
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect company profile, NAICS data, size reference records, UI form patterns, audit logging, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state and propose a small implementation plan before editing files.

> Context:
>
> - Epic: SBA Size Helper
> - User story: As a tenant admin, I want to compare my company profile values against size standards so that I can identify likely small-business status by NAICS.
> - Acceptance criteria:
> - Evaluation uses approved size standard records only.
> - Missing revenue or employee inputs produce insufficient information.
> - Results show NAICS, metric, threshold, entered value or range, source URL, and run date.
> - User can save evaluation results to the company profile.
> - Saved evaluations are audit logged.

Implement Story 23.2, "Company Size Evaluation Helper," from `docs/development-phase-use-cases.md`. Add NAICS selection and annual receipts/employee range inputs, evaluate against approved size standards, return likely small/other than small/insufficient information/expert review recommended, display required source context and disclaimer, and support saving results to the profile. Add tests for approved-only data, missing inputs, result labels, saved metadata, tenant scoping, and audit logging, then run verification.

#-----------------------------------------

### Story 23.3: Opportunity NAICS Size Check
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect contract/opportunity models, company size evaluations, task creation, audit logging, UI detail views, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state and propose a small implementation plan before editing files.

> Context:
>
> - Epic: SBA Size Helper
> - User story: As a proposal manager, I want to check an opportunity or contract NAICS code against company data so that I can flag size-status questions early.
> - Acceptance criteria:
> - User can run size check for a contract NAICS code.
> - Result shows likely status, source standard, and missing information when applicable.
> - Expert-review recommended result can create a task assigned to an owner.
> - Evaluation history remains available from the contract record.
> - Size check actions are audit logged.

Implement Story 23.3, "Opportunity NAICS Size Check," from `docs/development-phase-use-cases.md`. Add a contract/opportunity size check action, compare NAICS against company inputs and approved standards, show source-backed results and missing data, support task creation for expert review, and store evaluation history on the contract. Add tests for results, task creation, history, tenant scoping, permissions, and audit logging, then run verification.

#-----------------------------------------

## 24. Subcontractor Tracker Expansion

### Story 24.1: Expanded Subcontractor Compliance Profile
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect subcontractor domain models, APIs, UI list/detail views, filters, validation, audit logging, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state and propose a small implementation plan before editing files.

> Context:
>
> - Epic: Subcontractor Tracker Expansion
> - User story: As a subcontractor manager, I want richer subcontractor profile fields so that supplier compliance risk can be assessed consistently.
> - Acceptance criteria:
> - Authorized user can create and update expanded subcontractor fields.
> - Profile completeness reflects required fields configured for the tenant.
> - Filters return only subcontractors in the current tenant.
> - Expiring insurance or certification dates can be surfaced in list filters.
> - Sensitive field changes are audit logged.

Implement Story 24.1, "Expanded Subcontractor Compliance Profile," from `docs/development-phase-use-cases.md`. Add UEI, CAGE, NAICS, size/certification, insurance, NDA, CUI access, export-control, CMMC, workshare, and owner fields; add validation, completeness indicator, filters, and sensitive-change audit events. Add tests for create/update, validation, filters, tenant scoping, completeness, and audit logging, then run verification.

#-----------------------------------------

### Story 24.2: Subcontractor Risk Status
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect subcontractor profiles, evidence/flow-down/SAM/CMMC data, risk or status patterns, UI list/detail views, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state and propose a small implementation plan before editing files.

> Context:
>
> - Epic: Subcontractor Tracker Expansion
> - User story: As a compliance manager, I want subcontractor risk status calculated from key compliance signals so that I can prioritize follow-up.
> - Acceptance criteria:
> - Risk status is calculated from documented inputs.
> - Risk drivers are visible to authorized users.
> - Updating evidence, insurance, NDA, CMMC status, or SAM data updates risk status.
> - Missing or unknown data can produce needs review.
> - Risk calculation is covered by automated tests.

Implement Story 24.2, "Subcontractor Risk Status," from `docs/development-phase-use-cases.md`. Define risk inputs for flow-downs, insurance, NDA, CUI/CMMC, overdue evidence, SAM status, and expert review; calculate low/medium/high/needs review; show drivers; recalculate when signals change; and document the rule inputs. Add tests for risk rules, updates, unknowns, visibility, and tenant scoping, then run verification.

#-----------------------------------------

### Story 24.3: Contract-Specific Subcontractor Obligations
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect contracts, subcontractors, flow-down clauses, obligations, evidence requests, APIs/UI, audit logging, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state and propose a small implementation plan before editing files.

> Context:
>
> - Epic: Subcontractor Tracker Expansion
> - User story: As a subcontractor manager, I want to connect subcontractors to contract-specific obligations so that supplier requirements are tracked by contract.
> - Acceptance criteria:
> - User can link a subcontractor to a contract and applicable flow-down obligations.
> - Supplier obligations show owner, due date, status, and required evidence.
> - Bulk creation uses accepted flow-down clauses only.
> - Supplier obligations are tenant-scoped.
> - Creation and status changes are audit logged.

Implement Story 24.3, "Contract-Specific Subcontractor Obligations," from `docs/development-phase-use-cases.md`. Add relationships between subcontractor, contract, flow-down clause, obligation, and evidence request; display supplier obligations on contract and subcontractor detail; support owner/due/status/evidence fields and bulk creation from accepted flow-down clauses only. Add tests for linking, bulk creation, accepted-only behavior, tenant scoping, status changes, and audit logging, then run verification.

#-----------------------------------------

## 25. Policy Templates

### Story 25.1: Approved Template Library
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect compliance content library patterns, policy/evidence areas, review metadata, APIs/UI, audit logging, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state and propose a small implementation plan before editing files.

> Context:
>
> - Epic: Policy Templates
> - User story: As a compliance content owner, I want policy templates managed with review metadata so that only approved templates are available to customers.
> - Acceptance criteria:
> - Approved templates include title, category, version, source references, owner, and last reviewed date.
> - Draft templates are hidden from standard users.
> - Deprecated templates remain visible to content reviewers.
> - Template approval requires source and review metadata.
> - Template lifecycle changes are audit logged.

Implement Story 25.1, "Approved Template Library," from `docs/development-phase-use-cases.md`. Add template model, placeholders, source references, versioning, lifecycle statuses, owner/review metadata, expert review flag, preview/version history, approval validation, and lifecycle audit events. Add tests for approved-only visibility, reviewer visibility, required metadata, version history, permissions, and audit logging, then run verification.

#-----------------------------------------

### Story 25.2: Generate Draft Policy From Template
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect approved template library, company/contract/obligation/CMMC data, evidence vault or policy area, APIs/UI, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state and propose a small implementation plan before editing files.

> Context:
>
> - Epic: Policy Templates
> - User story: As a compliance manager, I want to generate a draft policy from an approved template so that I can tailor it for my company.
> - Acceptance criteria:
> - User can generate a draft policy from an approved template.
> - Placeholder values are populated from tenant data when available.
> - Missing placeholder values are flagged for user completion.
> - Generated policy stores source template version and generation date.
> - Generated policy is marked draft until approved by the tenant.

Implement Story 25.2, "Generate Draft Policy From Template," from `docs/development-phase-use-cases.md`. Add template selection, placeholder population from tenant context, missing value flags, generated draft policy storage with source template version/date, and edit/save workflow. Add tests for approved-template-only generation, placeholder population, missing values, draft status, tenant scoping, and permissions, then run verification.

#-----------------------------------------

### Story 25.3: Policy Approval And Evidence Linking
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect generated policies, evidence linking, obligations, CMMC controls, reports, approval/audit patterns, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state and propose a small implementation plan before editing files.

> Context:
>
> - Epic: Policy Templates
> - User story: As a compliance manager, I want approved policies linked to obligations and controls so that they can be reused as evidence.
> - Acceptance criteria:
> - Authorized user can approve, reject, or revise a draft policy.
> - Approved policy records approver, approval date, source template, and review date.
> - Approved policy can be linked to obligations and controls as evidence.
> - Revisions preserve prior approved versions.
> - Policy approval actions are audit logged.

Implement Story 25.3, "Policy Approval And Evidence Linking," from `docs/development-phase-use-cases.md`. Add tenant policy approval states and metadata, link approved policies to obligations, controls, and evidence packages, track expiration/review dates, preserve revisions, include approved policies in reports, and audit approval/rejection/revision actions. Add tests for approval permissions, linking, revision history, report inclusion, tenant scoping, and audit logging, then run verification.

#-----------------------------------------

## 26. Evidence Request Workflows

### Story 26.1: Evidence Request Creation
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect evidence vault, obligations, controls, contracts, subcontractors, notifications, permissions, audit logging, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state and propose a small implementation plan before editing files.

> Context:
>
> - Epic: Evidence Request Workflows
> - User story: As a compliance manager, I want to create evidence requests tied to obligations, controls, contracts, or subcontractors so that each request has context and a due date.
> - Acceptance criteria:
> - Authorized user can create an evidence request tied to a supported record type.
> - Request stores requester, assignee, due date, status, instructions, and related record.
> - Assignee receives notification.
> - User cannot assign a request to a user or subcontractor outside the tenant context.
> - Request creation is audit logged.

Implement Story 26.1, "Evidence Request Creation," from `docs/development-phase-use-cases.md`. Add evidence request model, create workflows from supported views, validation for assignee/due date/related record permissions, assignment notifications, and audit events. Add tests for supported record types, assignment boundaries, tenant scoping, notification creation, validation, permissions, and audit logging, then run verification.

#-----------------------------------------

### Story 26.2: Evidence Submission And Review
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect evidence requests, evidence upload guardrails, evidence linking, notifications, permissions, audit logging, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state and propose a small implementation plan before editing files.

> Context:
>
> - Epic: Evidence Request Workflows
> - User story: As an assignee, I want to submit evidence to a request so that the requester can review whether it satisfies the requirement.
> - Acceptance criteria:
> - Assignee can submit evidence to an open request.
> - Upload submissions enforce CUI/data-handling guardrails and tenant scope.
> - Reviewer can accept or return submitted evidence with comments.
> - Accepted evidence is linked to the related requirement.
> - Status changes and review decisions are audit logged.

Implement Story 26.2, "Evidence Submission And Review," from `docs/development-phase-use-cases.md`. Add submission workflow for existing evidence and new allowed uploads, statuses for open/submitted/accepted/returned/overdue/canceled, reviewer comments and return reasons, accepted evidence linking, and notifications. Add tests for assignee submission, CUI/data-handling enforcement, reviewer decisions, linking, notifications, tenant scoping, and audit logging, then run verification.

#-----------------------------------------

### Story 26.3: Evidence Request Dashboard
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect evidence request data, dashboards/list patterns, role permissions, notifications, reports/exports, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state and propose a small implementation plan before editing files.

> Context:
>
> - Epic: Evidence Request Workflows
> - User story: As a compliance manager, I want a dashboard of evidence requests so that I can track overdue, submitted, accepted, and blocked requests.
> - Acceptance criteria:
> - Dashboard shows only evidence requests in the current tenant.
> - Filters return requests by status, due date, assignee, related type, and priority.
> - Overdue requests are calculated from due date and current status.
> - Bulk reminders create notifications without changing request status.
> - Auditors can view approved or accepted evidence request records but cannot modify them.

Implement Story 26.3, "Evidence Request Dashboard," from `docs/development-phase-use-cases.md`. Add dashboard list and filters, overdue calculation, bulk reminders, export/report section, and role-aware requester/assignee/auditor/advisor views. Add tests for filters, overdue logic, tenant scoping, bulk reminders, auditor read-only behavior, and notification creation, then run verification.

#-----------------------------------------

## 27. CMMC Level 2 Readiness Expansion

### Story 27.1: Level 2 Control Assessment Detail
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect CMMC control models, assessment UI/API, evidence status, history/audit patterns, validation, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state and propose a small implementation plan before editing files.

> Context:
>
> - Epic: CMMC Level 2 Readiness Expansion
> - User story: As a security owner, I want detailed Level 2 control assessment fields so that readiness is tracked beyond simple status.
> - Acceptance criteria:
> - Authorized user can update Level 2 control assessment detail.
> - Control detail stores implementation, evidence, inherited, ESP responsibility, notes, assessment date, and assessor.
> - Status history is retained.
> - Control updates are tenant-scoped.
> - Control assessment updates are audit logged.

Implement Story 27.1, "Level 2 Control Assessment Detail," from `docs/development-phase-use-cases.md`. Add detailed assessment objective, implementation, evidence, inherited, ESP responsibility, notes, assessment date, and assessor fields; update Level 2 control detail UI; validate statuses; retain history; and audit updates. Add tests for update permissions, validation, history, tenant scoping, and audit logging, then run verification.

#-----------------------------------------

### Story 27.2: Responsibility Matrix
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect CMMC control ownership patterns, evidence requests, exports/reports, UI table patterns, audit logging, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state and propose a small implementation plan before editing files.

> Context:
>
> - Epic: CMMC Level 2 Readiness Expansion
> - User story: As a security owner, I want a responsibility matrix for internal teams and external service providers so that CMMC control ownership is explicit.
> - Acceptance criteria:
> - User can assign responsible party for each Level 2 control.
> - Matrix shows control, responsibility type, owner, provider, evidence status, and notes.
> - Controls marked external or shared require provider or responsibility notes.
> - Responsibility changes are audit logged.
> - Matrix export reflects current tenant data.

Implement Story 27.2, "Responsibility Matrix," from `docs/development-phase-use-cases.md`. Add responsibility assignments for organization, MSP/ESP, cloud provider, subcontractor, and shared responsibility; link to controls/evidence requests; add grouped matrix view and export; validate external/shared provider notes; and audit changes. Add tests for assignment, validation, matrix rendering/API, export, tenant scoping, and audit logging, then run verification.

#-----------------------------------------

### Story 27.3: Readiness Gap Prioritization
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect CMMC control assessment data, evidence status, POA&M/task creation, dashboard patterns, rule tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state and propose a small implementation plan before editing files.

> Context:
>
> - Epic: CMMC Level 2 Readiness Expansion
> - User story: As a compliance manager, I want Level 2 gaps prioritized so that limited resources focus on the most important readiness work.
> - Acceptance criteria:
> - Gap priority is calculated from documented inputs.
> - Dashboard lists gaps by priority with reason codes.
> - User can create a POA&M item or task from a gap.
> - Priority recalculates when control or evidence status changes.
> - Priority rules are covered by automated tests.

Implement Story 27.3, "Readiness Gap Prioritization," from `docs/development-phase-use-cases.md`. Define priority inputs, calculate critical/high/medium/low/needs review, show prioritized gaps with reason codes, support POA&M/task creation, and recalculate on control/evidence changes. Add tests for priority rules, recalculation, dashboard data, POA&M/task creation, tenant scoping, and permissions, then run verification.

#-----------------------------------------

### Story 27.4: Level 2 Readiness Report
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect report generation, CMMC Level 2 data, responsibility matrix, POA&M/gaps, source references, permissions, audit logging, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state and propose a small implementation plan before editing files.

> Context:
>
> - Epic: CMMC Level 2 Readiness Expansion
> - User story: As a compliance manager, I want a Level 2 readiness report with draft-only language and source context so that leadership and advisors can review progress.
> - Acceptance criteria:
> - Authorized user can generate a Level 2 readiness report.
> - Report includes control status, evidence status, gaps, POA&M items, responsibility matrix, source references, and generated date.
> - Report contains no pass/fail certification language.
> - Report uses tenant-scoped data only.
> - Report generation is audit logged.

Implement Story 27.4, "Level 2 Readiness Report," from `docs/development-phase-use-cases.md`. Add report sections for control/evidence status, gaps, POA&M items, responsibility matrix, and source references; include generated date, tenant, control version, and reviewer metadata; use draft-only readiness language; add export; enforce permissions; and audit generation. Add tests for content, forbidden certification language, tenant scoping, permissions, export, and audit logging, then run verification.

#-----------------------------------------

## 28. Extraction Content Test Set

### Story 28.1: Curated Test Document Set
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect extraction tests, sample content directories, data handling docs, label/review patterns, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state and propose a small implementation plan before editing files.

> Context:
>
> - Epic: Extraction Content Test Set
> - User story: As a QA owner, I want representative allowed contract documents and expected clause labels so that extraction accuracy can be evaluated consistently.
> - Acceptance criteria:
> - Test corpus contains only public, synthetic, or explicitly approved non-CUI documents.
> - Each labeled document includes expected clause citations and source locations when available.
> - Test metadata identifies document type, source family, and limitations.
> - Label set is reviewed before use as a benchmark.
> - Test set data handling rules are documented.

Implement Story 28.1, "Curated Test Document Set," from `docs/development-phase-use-cases.md`. Create a public/synthetic/approved non-CUI test corpus structure, labels for expected clause citations/locations/titles/flow-down indicators, metadata for source family and limitations, label review workflow, and documented data handling rules. Add tests or validation for corpus metadata and label requirements where practical, then run verification.

#-----------------------------------------

### Story 28.2: Precision And Recall Evaluation
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect extraction runner/tests, test corpus labels, CI configuration, reporting patterns, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state and propose a small implementation plan before editing files.

> Context:
>
> - Epic: Extraction Content Test Set
> - User story: As a QA owner, I want automated extraction evaluation so that the team can measure whether clause detection is improving or regressing.
> - Acceptance criteria:
> - Evaluation runner produces precision, recall, false positive, and false negative metrics.
> - Results identify missed and extra clause detections by document.
> - Threshold failures are visible in CI or scheduled test output.
> - Metrics are stored or published for trend review.
> - Evaluation can run without customer data.

Implement Story 28.2, "Precision And Recall Evaluation," from `docs/development-phase-use-cases.md`. Build an evaluation runner comparing extracted candidates to expected labels, calculate precision/recall/false positives/false negatives/unmatched expected clauses, output machine-readable and human-readable results, add thresholds, and wire CI or scheduled execution without customer data. Add tests for metric calculation, threshold behavior, and output format, then run verification.

#-----------------------------------------

### Story 28.3: Extraction Regression Review
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect extraction evaluation outputs, task/workflow models, content review queues, reporting/audit patterns, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state and propose a small implementation plan before editing files.

> Context:
>
> - Epic: Extraction Content Test Set
> - User story: As a compliance content owner, I want failed extraction cases reviewed so that matcher, library, and label improvements are tracked deliberately.
> - Acceptance criteria:
> - Each reviewed failure has a classification, owner, status, and resolution note.
> - Follow-up tasks can be created from failures.
> - Resolved failures are linked to matcher, library, parser, or label updates when applicable.
> - Release summary shows open extraction risks and metric trends.
> - Regression review records are audit logged or otherwise traceable.

Implement Story 28.3, "Extraction Regression Review," from `docs/development-phase-use-cases.md`. Add workflow for missed clauses and false positives, classify failures as parser/matcher/library/label/source-quality/expected-limitation, create follow-up tasks, track owner/status/resolution notes, link resolved failures to updates, and produce release readiness summary with metric trends and open risks. Add tests for classifications, task creation, traceability/audit behavior, summary output, and tenant/content boundaries where applicable, then run verification.


## Phase 1A - CUI Readiness Gate
Use the shared prompt requirements above for every Phase 1A story. Phase 1A is a readiness gate inside Phase 1 and must be completed before any production tenant can upload real customer CUI. Each story prompt below is intended to be copied into a fresh implementation thread after the prior story has been completed and verified.

## 1A.1 Tenant Data Handling Modes
### Story 1A.1.1: Tenant Data Handling Mode Model
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 1A CUI readiness gate story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: Tenant Data Handling Modes
> - User story: As a platform admin, I want each tenant to have a data handling mode so that CUI controls can be enforced consistently across the application.
> - Acceptance criteria:
> - Each tenant has exactly one active data handling mode.
> - New pilot tenants default to `NoCui` unless explicitly created as `DemoSandbox`.
> - `CuiReady` cannot be assigned without a completed approval checklist.
> - Mode changes persist actor, timestamp, reason, previous mode, and new mode.
> - Tenant data handling mode is available to upload, evidence, report, note, and extraction workflows.

Implement Story 1A.1.1, "Tenant Data Handling Mode Model," from `docs/development-phase-use-cases.md`. Add the tenant data handling mode model, mode history, validation, tenant administration display, and service access for upload, evidence, report, note, and extraction workflows. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, review metadata, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------
### Story 1A.1.2: Mode-Based Workflow Enforcement
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 1A CUI readiness gate story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: Tenant Data Handling Modes
> - User story: As a compliance manager, I want the application to enforce tenant mode automatically so that users cannot bypass CUI restrictions from the UI or direct API calls.
> - Acceptance criteria:
> - `DemoSandbox` tenants can use seeded synthetic CUI examples but cannot upload real customer files marked as CUI.
> - `NoCui` tenants cannot create or process records classified as real CUI.
> - `CuiReady` tenants can use CUI handling workflows only when required classification and approval checks pass.
> - Direct API calls receive the same mode restrictions as UI actions.
> - Mode enforcement failures return a clear error and create an audit event.

Implement Story 1A.1.2, "Mode-Based Workflow Enforcement," from `docs/development-phase-use-cases.md`. Add centralized server-side mode enforcement across contract intake, evidence, notes, reports, and extraction jobs, with matching UI restrictions, standard errors, and audit events. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, review metadata, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------

## 1A.2 Data Classification Controls
### Story 1A.2.1: Classification Metadata Schema
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 1A CUI readiness gate story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: Data Classification Controls
> - User story: As a developer, I want a shared classification metadata schema so that every CUI-relevant object stores data handling facts consistently.
> - Acceptance criteria:
> - Classification metadata is required before content can be stored as active tenant content.
> - `CUI` classification is rejected for tenants that are not in `CuiReady` mode.
> - `SyntheticCui` classification is allowed only for approved demo or test data workflows.
> - `Unknown` classification blocks downstream processing until reviewed or reclassified.
> - Classification changes preserve previous value, new value, actor, timestamp, and reason.

Implement Story 1A.2.1, "Classification Metadata Schema," from `docs/development-phase-use-cases.md`. Add shared classification metadata for uploads, notes, reports, extraction jobs, evidence items, and documents, including validation, review metadata, and change history. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, review metadata, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------
### Story 1A.2.2: Classification UX And Review
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 1A CUI readiness gate story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: Data Classification Controls
> - User story: As a user, I want to classify content during normal work so that I can make data handling decisions before uploading or processing information.
> - Acceptance criteria:
> - User must select or confirm classification before upload, note save, report generation, or extraction job creation.
> - Items classified as `Unknown` are visible in a review queue and cannot be used in reports or extraction jobs.
> - Items classified as `Prohibited` are blocked from use and routed to escalation.
> - Authorized reviewers can update classification with a reason.
> - Lists and detail views display the current classification for each classified item.

Implement Story 1A.2.2, "Classification UX And Review," from `docs/development-phase-use-cases.md`. Add classification selectors, warnings, badges, review queue behavior, reviewer reclassification, and blocked/prohibited routing across CUI-relevant workflows. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, review metadata, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------

## 1A.3 Synthetic CUI Demo Dataset
### Story 1A.3.1: Synthetic Dataset Definition
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 1A CUI readiness gate story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: Synthetic CUI Demo Dataset
> - User story: As a compliance content owner, I want a reviewed synthetic CUI dataset so that demo content cannot be mistaken for real controlled information.
> - Acceptance criteria:
> - Synthetic dataset contains no real customer CUI, classified data, export-controlled technical data, or customer proprietary information.
> - Every seeded synthetic record is tagged with `SyntheticCui` and dataset version.
> - Demo UI views identify synthetic examples as synthetic.
> - Dataset metadata includes owner, source basis, review date, and approved reviewer.
> - Dataset review status is required before demo seed import runs.

Implement Story 1A.3.1, "Synthetic Dataset Definition," from `docs/development-phase-use-cases.md`. Create the reviewed synthetic CUI dataset definition, metadata, classification tags, visible synthetic labels, and import precheck. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, review metadata, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------
### Story 1A.3.2: Demo Tenant Seeding
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 1A CUI readiness gate story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: Synthetic CUI Demo Dataset
> - User story: As a customer success lead, I want demo tenants to be seeded with synthetic CUI handling workflows so that onboarding and training can show end-to-end behavior safely.
> - Acceptance criteria:
> - Seed process runs only for `DemoSandbox` tenants.
> - Re-running the seed process does not duplicate demo records.
> - Demo tenants show seeded examples across contract, obligation, evidence, CMMC, subcontractor, and report workflows.
> - Customer `NoCui` and `CuiReady` tenants cannot receive demo seed data through normal admin workflows.
> - Seed and reset actions are audit logged.

Implement Story 1A.3.2, "Demo Tenant Seeding," from `docs/development-phase-use-cases.md`. Create the demo tenant seed/reset workflow for synthetic contract, obligation, evidence, CMMC, subcontractor, report, and escalation examples with idempotency and mode restrictions. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, review metadata, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------

## 1A.4 CUI-Ready Tenant Approval Checklist
### Story 1A.4.1: Approval Checklist Model
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 1A CUI readiness gate story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: CUI-Ready Tenant Approval Checklist
> - User story: As a platform admin, I want a Future `CuiReady` approval checklist so that required readiness evidence is captured before enabling CUI handling workflows.
> - Acceptance criteria:
> - Checklist cannot be approved while required items are incomplete.
> - Each completed item records owner, reviewer, review date, and supporting note or evidence link.
> - Rejected checklists include rejection reason and remain linked to the tenant.
> - Approved checklist ID is required for a tenant mode change to `CuiReady`.
> - Checklist changes are audit logged.

Implement Story 1A.4.1, "Approval Checklist Model," from `docs/development-phase-use-cases.md`. Add the Future `CuiReady` approval checklist model, states, item metadata, tenant linkage, API/UI workflows, and audit logging. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, review metadata, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------
### Story 1A.4.2: Approval Gate Enforcement
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 1A CUI readiness gate story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: CUI-Ready Tenant Approval Checklist
> - User story: As an engineering lead, I want `CuiReady` enablement to be blocked unless approval criteria are complete so that configuration mistakes do not authorize CUI handling.
> - Acceptance criteria:
> - Only authorized platform roles can approve a checklist.
> - A tenant cannot move to `CuiReady` from an incomplete, rejected, expired, or superseded checklist.
> - Final approval records approving user, timestamp, checklist version, and approval notes.
> - Mode change to `CuiReady` references the approved checklist record.
> - Failed approval attempts return a clear error and create an audit event.

Implement Story 1A.4.2, "Approval Gate Enforcement," from `docs/development-phase-use-cases.md`. Add server-side approval gate enforcement for `CuiReady` mode, final approval permissions, stale-check detection, UI messaging, and failure audit events. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, review metadata, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------

## 1A.5 Shared Responsibility Matrix Baseline
### Story 1A.5.1: Baseline Responsibility Matrix
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 1A CUI readiness gate story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: Shared Responsibility Matrix Baseline
> - User story: As a security owner, I want a baseline shared responsibility matrix so that internal teams and customers understand who owns each control, process, and support obligation.
> - Acceptance criteria:
> - Matrix includes all Phase 1A categories required for CUI readiness.
> - Each row has responsibility owner, notes, effective date, review owner, and version.
> - Matrix cannot be published without required owner and review metadata.
> - Published matrix is viewable from tenant settings and CUI approval checklist.
> - Matrix publication and retirement are audit logged or source-control traceable.

Implement Story 1A.5.1, "Baseline Responsibility Matrix," from `docs/development-phase-use-cases.md`. Create the baseline shared responsibility matrix content/model, review and publish workflow, tenant settings visibility, checklist linkage, and lifecycle traceability. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, review metadata, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------
### Story 1A.5.2: Tenant Matrix Acknowledgement
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 1A CUI readiness gate story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: Shared Responsibility Matrix Baseline
> - User story: As a tenant admin, I want to acknowledge the shared responsibility matrix so that approved future `CuiReady` operation has a recorded customer acceptance.
> - Acceptance criteria:
> - Tenant admin can view and acknowledge the current published matrix.
> - future `CuiReady` approval is blocked if the tenant has not acknowledged the current matrix version.
> - Acknowledgement history records version, user, timestamp, and tenant.
> - New matrix version marks prior acknowledgement as outdated for future approvals.
> - Matrix acknowledgement is audit logged.

Implement Story 1A.5.2, "Tenant Matrix Acknowledgement," from `docs/development-phase-use-cases.md`. Add tenant admin matrix acknowledgement, version history, current-version approval gate enforcement, change notifications, and audit logging. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, review metadata, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------

## 1A.6 Customer-Facing Data Handling Notices
### Story 1A.6.1: Versioned Notice Content
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 1A CUI readiness gate story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: Customer-Facing Data Handling Notices
> - User story: As a product owner, I want versioned data handling notices so that customer-facing guidance is consistent, reviewable, and traceable.
> - Acceptance criteria:
> - Published notice exists for each tenant data handling mode.
> - Notice content cannot publish without owner, reviewer, review date, and effective date.
> - `NoCui` notice states that real customer CUI upload is prohibited.
> - `CuiReady` notice states that CUI handling is limited to approved tenant workflows and customer responsibilities.
> - Notice retrieval returns the correct published version for tenant mode and workflow context.

Implement Story 1A.6.1, "Versioned Notice Content," from `docs/development-phase-use-cases.md`. Create versioned data handling notices for `DemoSandbox`, `NoCui`, and `CuiReady` modes with review metadata and context-aware retrieval. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, review metadata, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------
### Story 1A.6.2: Notice Placement And Acknowledgement
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 1A CUI readiness gate story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: Customer-Facing Data Handling Notices
> - User story: As a user, I want relevant data handling notices in the workflows where mistakes can occur so that I see restrictions before submitting content.
> - Acceptance criteria:
> - User cannot upload, save classified notes, generate reports from classified content, or start extraction until required notice acknowledgement exists.
> - Acknowledgement records include user, tenant, mode, workflow, notice version, and timestamp.
> - Updated notice versions require renewed acknowledgement.
> - Notice copy shown to the user matches the tenant's current mode.
> - Acknowledgement and renewed acknowledgement are audit logged.

Implement Story 1A.6.2, "Notice Placement And Acknowledgement," from `docs/development-phase-use-cases.md`. Place and enforce data handling notices in onboarding, upload, note, report, extraction, and support flows, including renewed acknowledgement on version changes. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, review metadata, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------

## 1A.7 CUI Support Escalation Path
### Story 1A.7.1: Escalation Intake And Classification
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 1A CUI readiness gate story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: CUI Support Escalation Path
> - User story: As a user or support agent, I want to report suspected CUI or prohibited data so that the issue can be triaged quickly.
> - Acceptance criteria:
> - Authorized users can create escalation records from CUI-relevant workflows.
> - Escalation records are tenant scoped and hidden from unrelated tenants.
> - Prohibited data escalations mark affected content as blocked from use.
> - Support agents can assign owner, severity, and status.
> - Escalation creation and updates are audit logged.

Implement Story 1A.7.1, "Escalation Intake And Classification," from `docs/development-phase-use-cases.md`. Add CUI support escalation intake, categories, affected item references, restricted support/admin views, prohibited-content blocking, and audit logging. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, review metadata, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------
### Story 1A.7.2: Escalation Workflow And Resolution
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 1A CUI readiness gate story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: CUI Support Escalation Path
> - User story: As a support lead, I want escalation status tracking so that accidental CUI and prohibited data cases have documented outcomes.
> - Acceptance criteria:
> - Escalation status changes require actor, timestamp, and note.
> - Affected content remains blocked while escalation status is submitted, triage, or contained.
> - Resolution records include resolution type, resolver, timestamp, and summary.
> - Reopened escalations preserve prior resolution history.
> - Escalation workflow events are audit logged.

Implement Story 1A.7.2, "Escalation Workflow And Resolution," from `docs/development-phase-use-cases.md`. Add escalation status transitions, containment behavior, resolution history, reopen handling, notifications, reporting, and audit events. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, review metadata, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------

## 1A.8 CUI Audit Event Coverage
### Story 1A.8.1: Required CUI Audit Events
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 1A CUI readiness gate story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: CUI Audit Event Coverage
> - User story: As a security owner, I want required CUI audit events defined and emitted so that every high-risk data handling action is traceable.
> - Acceptance criteria:
> - Each required Phase 1A event type is emitted when the corresponding action occurs.
> - Blocked upload, blocked extraction, blocked report, failed mode change, and failed CUI approval attempts are audit logged.
> - Audit events include tenant ID, actor ID, event type, entity reference, timestamp, and result.
> - Audit events do not expose sensitive document content in event summaries.
> - Automated tests cover successful and blocked audit paths.

Implement Story 1A.8.1, "Required CUI Audit Events," from `docs/development-phase-use-cases.md`. Define and emit required Phase 1A CUI audit event types across success and blocked paths without leaking sensitive content in summaries. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, review metadata, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------
### Story 1A.8.2: CUI Audit Filters And Export
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 1A CUI readiness gate story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: CUI Audit Event Coverage
> - User story: As a tenant admin or security reviewer, I want to filter and export CUI-relevant audit events so that readiness reviews and incident investigations are efficient.
> - Acceptance criteria:
> - Authorized users can filter audit events by CUI-relevant event type, classification, mode, actor, entity, date range, and result.
> - Non-authorized users cannot view or export CUI audit events.
> - Export contains only tenant-scoped events.
> - Export includes generated by, generated at, tenant, and filter criteria metadata.
> - Audit export action is itself audit logged.

Implement Story 1A.8.2, "CUI Audit Filters And Export," from `docs/development-phase-use-cases.md`. Add CUI audit filters, saved readiness view or equivalent, tenant-scoped export, export metadata, authorization, and export audit event. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, review metadata, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------

## 1A.9 Security Readiness Review
### Story 1A.9.1: Security Review Checklist
**Status: Implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 1A CUI readiness gate story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: Security Readiness Review
> - User story: As a security owner, I want a Phase 1A security review checklist so that required CUI readiness controls are assessed consistently.
> - Acceptance criteria:
> - Security review checklist includes every required Phase 1A review area.
> - Each checklist item records status, reviewer, review date, and evidence or rationale.
> - High or critical open findings block future `CuiReady` approval.
> - Accepted risks include approver, date, scope, expiration or review date, and mitigation note.
> - Security review changes are audit logged.

Implement Story 1A.9.1, "Security Review Checklist," from `docs/development-phase-use-cases.md`. Add the Phase 1A security review checklist, finding tracking, accepted risk metadata, CUI approval linkage, open finding reporting, and audit events. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, review metadata, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------
### Story 1A.9.2: Technical Control Verification
**Status: Partially implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 1A CUI readiness gate story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: Security Readiness Review
> - User story: As an engineering lead, I want automated or documented verification for CUI readiness controls so that approval is based on evidence rather than assumption.
> - Acceptance criteria:
> - Tenant isolation tests prove one tenant cannot access another tenant's classified records or files.
> - Evidence file storage records encryption state, scan state, retention state, and deletion state.
> - Backup and restore verification includes date, environment, reviewer, and result.
> - Admin/support access to CUI-relevant records is permission checked and audit logged.
> - Security readiness summary identifies passed checks, open findings, accepted risks, and release recommendation.

Implement Story 1A.9.2, "Technical Control Verification," from `docs/development-phase-use-cases.md`. Add or document technical control verification for CUI tenant isolation, storage controls, backup/restore evidence, admin/support access, and readiness summary output. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, review metadata, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------
### Story 1A.9.3: Incident Response Readiness
**Status: Partially implemented**
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 1A CUI readiness gate story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: Security Readiness Review
> - User story: As a security owner, I want incident response readiness checked before CUI handling workflows are enabled so that accidental CUI upload or data handling incidents can be handled immediately.
> - Acceptance criteria:
> - Required incident playbooks exist before `CuiReady` approval.
> - Each playbook identifies trigger, containment steps, notification path, evidence to collect, owner, and closure criteria.
> - Readiness review records tabletop date, participants, findings, and follow-up actions.
> - Open critical incident response gaps block future `CuiReady` approval.
> - Incident readiness approval is audit logged or source-control traceable.

Implement Story 1A.9.3, "Incident Response Readiness," from `docs/development-phase-use-cases.md`. Add incident response readiness playbooks, escalation owner records, tabletop checklist/evidence capture, approval linkage, reminders, and traceability. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, review metadata, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------

## Phase 3 - Advanced Compliance

### Story 29.1: SSP Data Model And Sections
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 3 Advanced Compliance story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: SSP Builder
> - User story: As a security owner, I want structured SSP sections so that system security plan content is consistent, source-backed, and reusable.
> - Acceptance criteria:
> - Authorized user can create and update SSP sections for the current tenant.
> - SSP sections link to source records instead of duplicating unsupported compliance claims.
> - Required sections cannot be marked approved without owner, reviewer, review date, and source references or rationale.
> - SSP section changes preserve status history.
> - SSP section create, update, approval, and archive actions are audit logged.

Implement Story 29.1, "SSP Data Model And Sections," from `docs/development-phase-use-cases.md`. Add structured SSP section types for system description, authorization boundary, environment, interconnections, users, roles, data types, CUI handling posture, control implementation narratives, inherited responsibilities, external service providers, and evidence references; link sections to company profile, system boundary, assets, CMMC controls, responsibility matrix, policies, POA&M items, and evidence; add ownership, review status, reviewer, review date, source references, lifecycle states, tenant-scoped API contracts, validation, history, and audit logging. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, source traceability, review metadata, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------

### Story 29.2: SSP Narrative Builder
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 3 Advanced Compliance story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: SSP Builder
> - User story: As a compliance manager, I want to build SSP narratives from approved data and editable drafts so that the plan reflects actual implementation without becoming unreviewed legal or assessment advice.
> - Acceptance criteria:
> - User can create draft SSP narrative text from approved tenant records.
> - Draft or AI-assisted narrative text is visibly marked as draft until approved.
> - Narrative approval is blocked when required source links are missing or referenced records are outdated.
> - User can compare current approved narrative with proposed changes.
> - Narrative generation, edits, and approvals are audit logged.

Implement Story 29.2, "SSP Narrative Builder," from `docs/development-phase-use-cases.md`. Add SSP narrative draft generation from approved tenant records and approved compliance content, editable narrative text, source links, reviewer notes, draft-only marking for generated or AI-assisted text, comparison between current approved narrative and proposed changes, validation for missing source links, unresolved placeholders, outdated references, unapproved sources, and cross-tenant sources, plus audit logging for generation, edits, and approvals. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, source traceability, review metadata, draft-only language, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------

### Story 29.3: SSP Export And Review Package
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 3 Advanced Compliance story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: SSP Builder
> - User story: As a security owner, I want to export an SSP review package so that leadership, advisors, or assessors can review the current plan with supporting references.
> - Acceptance criteria:
> - Authorized user can export an SSP package for the current tenant.
> - Export includes generated date, package version, tenant, section statuses, reviewer metadata, and source references.
> - Export excludes prohibited, unknown, unapproved, and cross-tenant records.
> - Export contains no certification or assessor determination language.
> - SSP export is audit logged.

Implement Story 29.3, "SSP Export And Review Package," from `docs/development-phase-use-cases.md`. Add SSP export package generation for human-readable report content and machine-readable metadata, generated date, tenant, package version, system boundary, section statuses, approved narrative summaries, evidence references, POA&M references, reviewer metadata, disclaimers, package history, and export audit logging. Exclude prohibited, unknown, unapproved, and cross-tenant evidence. Enforce external-share restrictions and ensure export language does not claim certification, assessment determination, authorization, or government endorsement. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, source traceability, review metadata, draft-only language, report/export permissions, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------

### Story 30.1: Scoring Rule Baseline
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 3 Advanced Compliance story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: SPRS Score Calculator
> - User story: As a compliance content owner, I want the SPRS scoring rule baseline captured with source metadata so that calculations are traceable and reviewable.
> - Acceptance criteria:
> - Published scoring rule set includes source URL, version, owner, reviewer, review date, and effective date.
> - Scoring rules cannot publish without required review metadata.
> - Retired scoring rules cannot be used for new calculations.
> - Calculation services identify which scoring rule version was used.
> - Scoring rule lifecycle changes are audit logged or source-control traceable.

Implement Story 30.1, "Scoring Rule Baseline," from `docs/development-phase-use-cases.md`. Add governed SPRS scoring rule content/model, lifecycle states, publish validation, source and review metadata, retired-rule protections, calculation version identification, and scoring edge-case tests. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, source traceability, review metadata, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------
### Story 30.2: Score Calculation Workspace
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 3 Advanced Compliance story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: SPRS Score Calculator
> - User story: As a security owner, I want to calculate a draft SPRS score from control assessment data so that I can identify score drivers and gaps.
> - Acceptance criteria:
> - Authorized user can calculate a draft SPRS score for the current tenant.
> - Calculation output shows score, deductions, requirement reasons, rule version, generated date, and unresolved gaps.
> - Score recalculates when relevant control assessment status changes.
> - Manual notes are stored separately from calculated values.
> - Score calculations are tenant-scoped and audit logged.

Implement Story 30.2, "Score Calculation Workspace," from `docs/development-phase-use-cases.md`. Add tenant-scoped draft SPRS score calculation from Level 2 control assessment data, deduction reasons, unresolved gap output, recalculation behavior, manual reviewer notes separated from calculated values, calculation history, and audit logging. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, source traceability, review metadata, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------
### Story 30.3: SPRS Readiness Report
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 3 Advanced Compliance story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: SPRS Score Calculator
> - User story: As a compliance manager, I want an SPRS readiness report so that leadership can review score context before deciding whether to submit or update SPRS.
> - Acceptance criteria:
> - Authorized user can generate an SPRS readiness report for the current tenant.
> - Report includes score, deductions, unresolved controls, POA&M references, evidence status, scoring rule version, and generated date.
> - Report states that GCCS has not submitted the score to SPRS.
> - Report uses tenant-scoped data only.
> - Report generation is audit logged.

Implement Story 30.3, "SPRS Readiness Report," from `docs/development-phase-use-cases.md`. Add the SPRS readiness report with score summary, deductions, unresolved controls, POA&M and evidence context, scoring rule version, generated date, draft/not-submitted language, permissions, export/history behavior, and audit logging. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, source traceability, review metadata, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------

## 31. SAM.gov Subcontracting Plan Reporting (SPR) Support
### Story 31.1: SPR Applicability And Reporting Calendar
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 3 Advanced Compliance story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: SAM.gov Subcontracting Plan Reporting (SPR) Support
> - User story: As a contracts manager, I want to identify contracts with SAM.gov SPR obligations so that required reports appear on the compliance calendar.
> - Acceptance criteria:
> - Authorized user can mark a contract as SPR-applicable with report type, period, due date, and source.
> - SPR report obligations appear on the compliance calendar.
> - Missing source clause or rationale blocks activation of an SPR obligation.
> - Overdue SPR tasks are calculated from due date and status.
> - SPR applicability changes are audit logged.

Implement Story 31.1, "SPR Applicability And Reporting Calendar," from `docs/development-phase-use-cases.md`. Add SAM.gov SPR applicability fields, source-backed activation validation, reporting period and due-date tracking, calendar/task integration, default ISR/SSR schedule support where applicable, overdue behavior, reminders, and audit logging. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, source traceability, review metadata, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------
### Story 31.2: Subcontracting Report Data Collection
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 3 Advanced Compliance story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: SAM.gov Subcontracting Plan Reporting (SPR) Support
> - User story: As a contracts manager, I want to collect subcontracting report data so that SAM.gov SPR package preparation uses documented subcontractor and spend information.
> - Acceptance criteria:
> - User can create report data rows linked to subcontractor and contract records.
> - Validation rejects negative amounts, missing required categories, duplicate rows, and period mismatches.
> - Report data rows link to supporting evidence when provided.
> - Data rows cannot be included in a final package until reviewed or explicitly marked as accepted.
> - Data row changes are audit logged.

Implement Story 31.2, "Subcontracting Report Data Collection," from `docs/development-phase-use-cases.md`. Add SAM.gov SPR report data rows linked to contracts, subcontractors, spend/category data, periods, plans, evidence, review states, import template support, reporting role, fiscal year/period, UEI, PIID, conditional subcontract number, documented external eligibility basis, validation for bad or duplicate data, package-inclusion gating, and audit logging. Preserve legacy eSRS routes and internal persistence identifiers only as compatibility aliases. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, source traceability, review metadata, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

Schema evolution and legacy remediation extension: resolve the current reviewed, published, effective SPR schema profile server-side; persist profile ID/version/source/definition hash on canonical rows; expose tenant-scoped field-level blockers and non-persisted UEI/PIID suggestions; never silently promote legacy rows; and audit changed field names and readiness transitions when enrichment is saved.

#-----------------------------------------
### Story 31.3: SAM.gov SPR Report Package
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 3 Advanced Compliance story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: SAM.gov Subcontracting Plan Reporting (SPR) Support
> - User story: As a contracts manager, I want to prepare a SAM.gov SPR report package so that internal reviewers can verify data before manual external submission.
> - Acceptance criteria:
> - Authorized user can generate a SAM.gov SPR preparation package for the current tenant.
> - Package includes contract, period, report type, subcontractor/spend summaries, exceptions, evidence references, and generated date.
> - Package states that FeDril has not submitted the report to SAM.gov.
> - Approved packages include reviewer and approval date.
> - Package generation and approval are audit logged.

Implement Story 31.3, "SAM.gov SPR Report Package," from `docs/development-phase-use-cases.md`. Add SAM.gov SPR preparation package generation, report metadata, subcontractor/spend summaries, exceptions, evidence references, preparation-only/not-submitted language, review workflow, package version/history, permissions, export behavior, and audit logging. Do not claim or implement SAM.gov submission or synchronization without an authorized contractor API. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, source traceability, review metadata, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------

## 32. Labor Compliance Module
### Story 32.1: Labor Applicability And Wage Determinations
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 3 Advanced Compliance story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: Labor Compliance Module
> - User story: As a contracts or HR manager, I want to identify labor clauses and wage determinations for a contract so that labor compliance tasks are generated from source-backed requirements.
> - Acceptance criteria:
> - Authorized user can record labor applicability with source clause, place of performance, and wage determination reference.
> - Wage determination uploads enforce tenant data-handling guardrails.
> - Missing source clause or documented rationale blocks labor obligation activation.
> - Labor applicability creates or updates linked review tasks.
> - Labor applicability changes are audit logged.

Implement Story 32.1, "Labor Applicability And Wage Determinations," from `docs/development-phase-use-cases.md`. Add labor applicability records, SCA/DBA/FAR Part 22 fields, wage determination references/uploads with data-handling guardrails, source-backed activation validation, contract/clause/task/evidence links, review status, task generation, and audit logging. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, source traceability, review metadata, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------
### Story 32.2: Labor Category And Employee Classification
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 3 Advanced Compliance story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: Labor Compliance Module
> - User story: As an HR or compliance manager, I want to map employees to labor categories so that wage, fringe, and classification evidence can be tracked by contract.
> - Acceptance criteria:
> - Authorized user can create labor categories and employee assignments for the current tenant.
> - Assignment validation rejects inactive categories, missing source references, and conflicting effective dates.
> - Sensitive employee fields are permission restricted.
> - Classification history preserves prior category, new category, actor, timestamp, and reason.
> - Labor category and assignment changes are audit logged.

Implement Story 32.2, "Labor Category And Employee Classification," from `docs/development-phase-use-cases.md`. Add labor categories, employee assignment records, wage determination classification/rate/fringe/effective-date data, source references, validation for inactive categories and date conflicts, sensitive employee-data permissions, classification history, and audit logging. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, source traceability, review metadata, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------
### Story 32.3: Labor Evidence And Compliance Report
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 3 Advanced Compliance story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: Labor Compliance Module
> - User story: As a compliance manager, I want a labor evidence package and status report so that contract, HR, and advisor reviewers can see labor compliance status.
> - Acceptance criteria:
> - Dashboard shows labor obligations, assignments, evidence status, gaps, and overdue items for the current tenant.
> - Report includes source clauses, wage determinations, labor categories, assignments, gaps, evidence references, and generated date.
> - Employee-sensitive sections are visible only to authorized roles.
> - Report contains workflow status and not legal determination language.
> - Report generation is audit logged.

Implement Story 32.3, "Labor Evidence And Compliance Report," from `docs/development-phase-use-cases.md`. Add labor evidence links, dashboard filters, gap/overdue status, labor compliance report generation, employee-sensitive report section authorization, workflow-status disclaimers, report history/export, and audit logging. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, source traceability, review metadata, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------

## 33. AI Assistant With Citations, Logging, And Human Review
### Story 33.1: Retrieval And Source Citation Pipeline
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 3 Advanced Compliance story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: AI Assistant With Citations, Logging, And Human Review
> - User story: As a developer, I want AI responses grounded in approved retrieval sources so that every answer can be traced to compliance content or tenant documents.
> - Acceptance criteria:
> - Assistant retrieves only tenant-authorized and approved sources.
> - Responses include citations for every substantive compliance statement.
> - Assistant refuses or asks for review when no approved source supports the answer.
> - Retrieval excludes prohibited, unknown, unapproved, or cross-tenant content.
> - Retrieval source IDs and policy decisions are logged.

Implement Story 33.1, "Retrieval And Source Citation Pipeline," from `docs/development-phase-use-cases.md`. Add approved retrieval-source policy, tenant/RBAC/classification/data-handling enforcement, citation metadata, unsupported-answer refusal behavior, unsafe-source exclusion, and retrieval source/policy logging. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, source traceability, review metadata, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------
### Story 33.2: AI Output Logging And Review
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 3 Advanced Compliance story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: AI Assistant With Citations, Logging, And Human Review
> - User story: As a compliance content owner, I want prompts, retrieved sources, generated output, and user decisions logged so that AI-assisted work can be reviewed and improved.
> - Acceptance criteria:
> - AI interaction logs include prompt metadata, retrieved sources, output, actor, tenant, timestamp, and workflow context.
> - AI output is marked draft until human approved where used in reports, policies, SSPs, POA&Ms, or customer deliverables.
> - Reviewer can approve, reject, or supersede AI output with notes.
> - AI logs respect tenant scope, RBAC, retention, and data-handling mode.
> - AI review decisions are audit logged.

Implement Story 33.2, "AI Output Logging And Review," from `docs/development-phase-use-cases.md`. Add AI interaction logging, draft/review states, deliverable approval gates, reviewer decisions and comments, retention/export controls, prohibited-data handling, tenant/RBAC/data-mode protections, and audit logging. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, source traceability, review metadata, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------
### Story 33.3: Guarded Assistant User Experience
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 3 Advanced Compliance story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: AI Assistant With Citations, Logging, And Human Review
> - User story: As a compliance manager, I want the assistant to provide bounded answers and next actions so that users understand source limits and review requirements.
> - Acceptance criteria:
> - Assistant answers include citations, draft label, confidence or support status, and review requirement.
> - Assistant blocks or redirects unsupported legal, certification, classified, prohibited, or cross-tenant requests.
> - User can create draft tasks, evidence requests, notes, or review items from supported answers.
> - Feedback is stored with answer, user, tenant, timestamp, and reason.
> - Assistant actions and blocked requests are audit logged.

Implement Story 33.3, "Guarded Assistant User Experience," from `docs/development-phase-use-cases.md`. Add assistant UI entry points, citation/draft/confidence/review displays, prohibited prompt handling, safe draft action creation, user feedback capture, escalation routing, and audit logging for actions and blocked requests. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, source traceability, review metadata, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------

## 34. Prime Contractor And Auditor Portals
### Story 34.1: External Portal Access Model
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 3 Advanced Compliance story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: Prime Contractor And Auditor Portals
> - User story: As a tenant admin, I want to invite prime contractor and auditor users into limited portals so that external review access is controlled by role, scope, and expiration.
> - Acceptance criteria:
> - Tenant admin can invite external portal users with role, scope, expiration, and package access.
> - Expired or revoked portal invitations cannot be used.
> - Portal users can access only assigned packages and scoped records.
> - Portal users cannot modify tenant workspace data.
> - Portal invitation, access, and revocation events are audit logged.

Implement Story 34.1, "External Portal Access Model," from `docs/development-phase-use-cases.md`. Add external portal roles, scoped invitations, expiration/revocation/resend/extension behavior, assigned package and contract scopes, strong-authentication hooks where configured, read-only portal enforcement, access history, and audit logging. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, source traceability, review metadata, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------
### Story 34.2: Approved Package Portal Review
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 3 Advanced Compliance story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: Prime Contractor And Auditor Portals
> - User story: As a prime contractor or auditor reviewer, I want to review approved packages, evidence references, and status reports so that I can complete my review without direct access to the tenant workspace.
> - Acceptance criteria:
> - Portal reviewer sees only approved packages explicitly shared with them.
> - Drafts, internal notes, prohibited data, unknown classification records, and unrelated records are hidden.
> - Reviewer can add comments or questions without modifying source tenant records.
> - Downloads include package metadata and watermarking when configured.
> - Portal review and download actions are audit logged.

Implement Story 34.2, "Approved Package Portal Review," from `docs/development-phase-use-cases.md`. Add the external portal package dashboard, approved-package visibility, unsafe/draft/internal/cross-tenant record exclusion, reviewer comments/questions, controlled download metadata/watermarking, and portal activity audit logging. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, source traceability, review metadata, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------
### Story 34.3: Portal Package Lifecycle And Revocation
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 3 Advanced Compliance story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: Prime Contractor And Auditor Portals
> - User story: As a tenant admin, I want to manage shared package lifecycle so that outdated or over-shared packages can be superseded, revoked, or reissued.
> - Acceptance criteria:
> - Tenant admin can expire, revoke, supersede, and reissue shared packages.
> - Revoked packages become inaccessible immediately to portal users.
> - Superseded packages link to the replacement package version.
> - Portal activity report shows access, comments, downloads, expiration, and revocation history.
> - Package lifecycle actions are audit logged.

Implement Story 34.3, "Portal Package Lifecycle And Revocation," from `docs/development-phase-use-cases.md`. Add shared package lifecycle states, expiration reminders and automatic expiration, reissue/supersede linkage, revocation reason and immediate access cutoff, tenant admin portal activity reporting, and audit logging. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, source traceability, review metadata, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------

## Phase 4 - Enterprise / Regulated Deployment

## 35. SSO/SAML And SCIM
### Story 35.1: SAML Identity Provider Configuration
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 4 Enterprise / Regulated Deployment story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: SSO/SAML And SCIM
> - User story: As a tenant admin, I want to configure a SAML identity provider so that my users can sign in through enterprise SSO.
> - Acceptance criteria:
> - Authorized tenant admin can create and test a SAML configuration for the current tenant.
> - SAML configuration cannot be enabled with missing metadata, expired certificate, invalid callback, or failed validation.
> - Test connection results include timestamp, actor, result, and diagnostic summary without exposing secrets.
> - Disabled or archived SAML configurations cannot be used for sign-in.
> - SAML configuration lifecycle actions are audit logged.

Implement Story 35.1, "SAML Identity Provider Configuration," from `docs/development-phase-use-cases.md`. Add tenant SAML configuration fields for entity ID, SSO URL, certificate, signing requirement, name ID format, attribute mappings, status, and metadata URL, validation for required provider metadata, certificate expiration, duplicate entity IDs, and callback URL ownership, test connection workflow with success, warning, and failure results, and related workflow controls. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, source traceability, review metadata, enterprise identity controls, regulated-environment controls, key-management safety, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------
### Story 35.2: SSO Sign-In Enforcement And Account Linking
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 4 Enterprise / Regulated Deployment story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: SSO/SAML And SCIM
> - User story: As a tenant admin, I want to enforce SSO sign-in for selected users or the whole tenant so that authentication policy is consistent.
> - Acceptance criteria:
> - Tenant admin can set SSO enforcement mode with required confirmation and permission checks.
> - Existing members can link to SAML identities and sign in when required attributes match.
> - Unmapped, inactive, cross-tenant, or missing-attribute SSO attempts are denied.
> - Break-glass access requires approval metadata, expiration, and audit trail.
> - SSO sign-in successes, failures, enforcement changes, and break-glass use are audit logged.

Implement Story 35.2, "SSO Sign-In Enforcement And Account Linking," from `docs/development-phase-use-cases.md`. Add tenant SSO enforcement modes for optional, required_for_members, required_for_all_except_break_glass, and disabled, account linking from SAML subject and mapped email to existing tenant membership, break-glass admin account controls with expiration, reason, approval, and audit trail, and related workflow controls. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, source traceability, review metadata, enterprise identity controls, regulated-environment controls, key-management safety, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------
### Story 35.3: SCIM User And Group Provisioning
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 4 Enterprise / Regulated Deployment story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: SSO/SAML And SCIM
> - User story: As an enterprise tenant admin, I want SCIM provisioning so that users and groups are created, updated, deactivated, and mapped to roles automatically.
> - Acceptance criteria:
> - Authorized tenant admin can enable SCIM provisioning and rotate or revoke SCIM tokens.
> - SCIM create, update, deactivate, reactivate, group assign, and group remove actions affect only the current tenant.
> - Deactivated SCIM users lose application access while their audit history remains intact.
> - Invalid group mappings, duplicate identities, and cross-tenant provisioning attempts are rejected.
> - SCIM provisioning events and token lifecycle actions are audit logged.

Implement Story 35.3, "SCIM User And Group Provisioning," from `docs/development-phase-use-cases.md`. Add tenant SCIM endpoint, bearer token lifecycle, status, last sync time, and provisioning settings, SCIM create, update, deactivate, reactivate, group assign, and group remove workflows with tenant scoping, SCIM groups to GCCS roles with validation and conflict handling, and related workflow controls. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, source traceability, review metadata, enterprise identity controls, regulated-environment controls, key-management safety, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------

## 36. GovCloud Or Government Cloud Deployment Path
### Story 36.1: Government Cloud Environment Configuration
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 4 Enterprise / Regulated Deployment story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: GovCloud Or Government Cloud Deployment Path
> - User story: As an engineering lead, I want government cloud environment configuration captured separately from commercial environments so that regulated deployments use approved infrastructure settings.
> - Acceptance criteria:
> - Government cloud environment records include region, boundary, network, storage, database, key, logging, and backup settings.
> - Environment approval is blocked when required government cloud controls or review metadata are missing.
> - Only approved environments can be selected for regulated tenant deployment.
> - Environment records preserve status history and reviewer metadata.
> - Environment configuration lifecycle changes are audit logged or source-control traceable.

Implement Story 36.1, "Government Cloud Environment Configuration," from `docs/development-phase-use-cases.md`. Add environment records for commercial, staging, GovCloud, and government cloud variants with region, boundary, network, storage, key vault, database, logging, and backup settings, configuration validation for required government cloud controls, region allowlist, encryption settings, private networking, audit logging, and backup policy, environment readiness status for draft, under_review, approved, blocked, deployed, and retired, and related workflow controls. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, source traceability, review metadata, enterprise identity controls, regulated-environment controls, key-management safety, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------
### Story 36.2: Regulated Tenant Provisioning Workflow
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 4 Enterprise / Regulated Deployment story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: GovCloud Or Government Cloud Deployment Path
> - User story: As an operations lead, I want a regulated tenant provisioning workflow so that GovCloud customers are created with the right controls before use.
> - Acceptance criteria:
> - Authorized operations user can create a regulated tenant provisioning request with required environment and control metadata.
> - Provisioning cannot start until required approvals and checklist items are complete.
> - Regulated tenant provisioning creates tenant records only in the approved target environment.
> - Failed provisioning records status, reason, rollback decision, and owner.
> - Provisioning lifecycle changes are audit logged.

Implement Story 36.2, "Regulated Tenant Provisioning Workflow," from `docs/development-phase-use-cases.md`. Add provisioning request fields for tenant, customer type, deployment environment, data handling mode, CUI approval status, key policy, support model, and migration source, approval gates for security, engineering, customer success, legal/compliance, and product where applicable, provisioning checklist for tenant isolation, storage, encryption, logging, monitoring, backup, restore, access policy, and support access, and related workflow controls. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, source traceability, review metadata, enterprise identity controls, regulated-environment controls, key-management safety, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------
### Story 36.3: Government Cloud Release And Operations Readiness
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 4 Enterprise / Regulated Deployment story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: GovCloud Or Government Cloud Deployment Path
> - User story: As an operations lead, I want release and operations readiness checks for government cloud deployments so that regulated environments are not promoted without evidence.
> - Acceptance criteria:
> - Government cloud releases require completed readiness checklist and approver metadata before promotion.
> - Open critical security, migration, backup, restore, or incident response gaps block release approval.
> - Release readiness record links to required operations evidence.
> - Release history identifies environment, version, window, owner, approver, result, and rollback status.
> - Government cloud release approval and deployment actions are audit logged or source-control traceable.

Implement Story 36.3, "Government Cloud Release And Operations Readiness," from `docs/development-phase-use-cases.md`. Add release readiness checklist for migrations, smoke tests, security scans, dependency review, backup, restore, monitoring, incident response, support coverage, and rollback plan, environment-specific release approval records for GovCloud and government cloud deployments, operations evidence links for runbooks, alert routing, access review, vulnerability scan, backup restore, and incident drill, and related workflow controls. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, source traceability, review metadata, enterprise identity controls, regulated-environment controls, key-management safety, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------

## 37. FedRAMP Readiness Package
### Story 37.1: FedRAMP Control Mapping Baseline
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 4 Enterprise / Regulated Deployment story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: FedRAMP Readiness Package
> - User story: As a security owner, I want a FedRAMP readiness control mapping baseline so that product controls, inherited services, and evidence gaps are tracked consistently.
> - Acceptance criteria:
> - FedRAMP readiness controls include control ID, family, baseline, owner, implementation status, evidence or gap rationale, and source reference.
> - Control mappings can link to existing GCCS security and operations evidence.
> - Approval is blocked when owner, reviewer, review date, source, or evidence/gap rationale is missing.
> - Open gaps are reportable by family, severity, owner, and target date.
> - Control mapping lifecycle changes are audit logged or source-control traceable.

Implement Story 37.1, "FedRAMP Control Mapping Baseline," from `docs/development-phase-use-cases.md`. Add fedRAMP readiness control records with control ID, family, baseline, implementation status, implementation summary, inherited provider, responsible owner, evidence links, gaps, and source references, mapping from existing GCCS security controls, audit logs, evidence storage, identity, encryption, incident response, and vulnerability management records, review states for draft, in_review, approved, gap_identified, accepted_risk, superseded, and archived, and related workflow controls. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, source traceability, review metadata, enterprise identity controls, regulated-environment controls, key-management safety, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------
### Story 37.2: Trust Artifact Library
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 4 Enterprise / Regulated Deployment story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: FedRAMP Readiness Package
> - User story: As a customer success lead, I want a governed trust artifact library so that procurement and security review materials are accurate, current, and approved before sharing.
> - Acceptance criteria:
> - Trust artifacts include owner, version, status, audience, effective date, review date, expiration date, and approver metadata.
> - Artifact publication is blocked when required review or approval metadata is missing.
> - Expired, superseded, or draft artifacts cannot be shared externally.
> - Sharing restrictions are enforced by audience, tenant, environment, and NDA requirement.
> - Artifact lifecycle and sharing actions are audit logged.

Implement Story 37.2, "Trust Artifact Library," from `docs/development-phase-use-cases.md`. Add artifact records for security overview, architecture diagram, shared responsibility matrix, subprocessors list, data retention policy, incident response summary, AI usage policy, access control summary, and support SLA, artifact metadata for owner, version, status, audience, effective date, review date, expiration date, approver, and source file, publication states for draft, in_review, approved, published, expired, superseded, and archived, and related workflow controls. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, source traceability, review metadata, enterprise identity controls, regulated-environment controls, key-management safety, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------
### Story 37.3: FedRAMP Readiness Export Package
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 4 Enterprise / Regulated Deployment story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: FedRAMP Readiness Package
> - User story: As a product owner, I want to export a FedRAMP readiness package so that leadership, advisors, and prospective federal customers can review current readiness without overstating authorization status.
> - Acceptance criteria:
> - Authorized user can generate a FedRAMP readiness package from approved and current artifacts.
> - Package includes generated date, version, scope, environment, reviewer metadata, gaps, accepted risks, and readiness summary.
> - Package states readiness status without claiming FedRAMP authorization unless approved by governance.
> - Draft, expired, superseded, restricted, prohibited, and cross-tenant records are excluded.
> - Package generation, approval, sharing, and revocation are audit logged.

Implement Story 37.3, "FedRAMP Readiness Export Package," from `docs/development-phase-use-cases.md`. Add export package generation for selected control mappings, trust artifacts, operations evidence, gap register, accepted risks, and readiness summary, generated date, package version, environment, scope, reviewer metadata, disclaimers, and authorization-status language, draft, expired, superseded, restricted, prohibited, or cross-tenant artifacts, and related workflow controls. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, source traceability, review metadata, enterprise identity controls, regulated-environment controls, key-management safety, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------

## 38. Higher-Assurance CUI Enclave And Customer-Managed Keys
### Story 38.1: CUI Enclave Boundary Model
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 4 Enterprise / Regulated Deployment story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: Higher-Assurance CUI Enclave And Customer-Managed Keys
> - User story: As a security owner, I want a CUI enclave boundary model so that approved tenants, storage, compute, network paths, and workflows are isolated and reviewable.
> - Acceptance criteria:
> - CUI enclave record includes tenant, environment, boundary, storage, compute, network, logging, backup, workflows, and support model metadata.
> - Enclave activation is blocked unless tenant is `CuiReady` with required approvals and acknowledgements.
> - Only approved enclave workflows can process content classified as real CUI.
> - Suspended, retired, or revoked enclaves block new CUI processing.
> - Enclave lifecycle actions are audit logged.

Implement Story 38.1, "CUI Enclave Boundary Model," from `docs/development-phase-use-cases.md`. Add enclave records with tenant, environment, boundary description, data handling mode, approved workflows, storage location, compute boundary, network restrictions, logging destination, backup policy, and support access model, enclave approval to future `CuiReady` tenant approval, security review checklist, incident readiness, and shared responsibility matrix acknowledgement, status workflow for draft, under_review, approved, active, suspended, retired, and revoked, and related workflow controls. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, source traceability, review metadata, enterprise identity controls, regulated-environment controls, key-management safety, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------
### Story 38.2: Customer-Managed Key Policy And Rotation
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 4 Enterprise / Regulated Deployment story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: Higher-Assurance CUI Enclave And Customer-Managed Keys
> - User story: As a CUI-ready customer admin, I want customer-managed key policies so that encryption control, rotation, suspension, and revocation are governed.
> - Acceptance criteria:
> - Authorized tenant admin can register and validate a customer-managed key policy for an approved environment.
> - Key policy activation is blocked when key availability, permissions, region, or encryption compatibility validation fails.
> - Rotation, suspension, revocation, and revalidation preserve status history and reviewer metadata.
> - Workflows using unavailable, revoked, or suspended keys are blocked with clear operational status.
> - Key lifecycle and validation events are audit logged.

Implement Story 38.2, "Customer-Managed Key Policy And Rotation," from `docs/development-phase-use-cases.md`. Add customer-managed key policy records with key provider, key ID, environment, tenant, status, rotation cadence, last rotation date, next rotation date, owner, approver, and emergency contact, key validation workflow for availability, permissions, region match, encryption compatibility, and backup implications, rotation, suspension, revocation, and revalidation workflows, and related workflow controls. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, source traceability, review metadata, enterprise identity controls, regulated-environment controls, key-management safety, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------
### Story 38.3: Enclave Access, Export, And Support Controls
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 4 Enterprise / Regulated Deployment story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: Higher-Assurance CUI Enclave And Customer-Managed Keys
> - User story: As a security owner, I want restricted access, export, and support controls for CUI enclave data so that customer content is protected during operations and support.
> - Acceptance criteria:
> - Enclave records and files are accessible only to roles with enclave-specific permissions.
> - Just-in-time support access requires reason, scope, approver, duration, and automatic expiration.
> - Enclave exports enforce package type, recipient, watermarking, encryption, and approval policy.
> - Emergency access requires elevated approval, incident linkage, time limit, and post-access review.
> - Enclave access, export, support, and emergency actions are audit logged.

Implement Story 38.3, "Enclave Access, Export, And Support Controls," from `docs/development-phase-use-cases.md`. Add enclave-specific RBAC permissions for view, upload, download, export, approve, support access, and emergency access, just-in-time support access request workflow with reason, scope, approver, duration, session log, and expiration, export policy controls for allowed package types, recipient restrictions, watermarking, encryption, and approval requirements, and related workflow controls. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, source traceability, review metadata, enterprise identity controls, regulated-environment controls, key-management safety, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------

## 39. SOC 2 Assurance Program

Revision: 2026-09-19. This is the consolidated execution backlog for the SOC 2 program, not evidence that the work has been completed. It expands the original governed Stories 39.1–39.3 in [development-phase-use-cases.md](development-phase-use-cases.md). Stories 39.4–39.23 are new delivery stories defined here; do not assume they already exist in that source backlog or its test inventory. Preserve the original three IDs and titles. Do not register this section with the automatic software story sequence: some tasks require management, legal, cloud-administrator, or independent CPA action.

The [2026-09-19 assessment](soc2-readiness-assessment.md) is the starting evidence snapshot, not a current production-state guarantee. Its 36 checklist items and 15 gap IDs are mapped below. Its proposed story additions are incorporated here. Preserve that historical assessment; record subsequent evidence, decisions, and remediation separately.

### Execution Order And Stage Gates

Run the setup portions of 39.1, 39.2, and 39.3 first, then follow 39.4–39.23 in numeric order. **Exception: begin 39.4 exposure triage immediately if exposure is still plausible; do not wait for governance paperwork.** Stories 39.1–39.3 are parent workflows: their setup gates permit child work; their final closure requires child evidence. Do not create a circular dependency by demanding their final closure before starting the children.

| Order | Story | Work and gate | When |
| --- | --- | --- | --- |
| 1 | 39.1 | Scope, system description, baseline inventory; approve candidate scope | Now |
| 2 | 39.2 | Gap/task register, protected evidence store and calendar setup | Now; maintained throughout |
| 3 | 39.3 | Examination/claims governance setup; no engagement or report implied | Now; maintained throughout |
| 4 | 39.4 | Potential public-data exposure investigation and authorized containment | Immediate triage |
| 5 | 39.5 | Governance, risk assessment, policies and oversight | Before readiness gate |
| 6 | 39.6 | Workforce lifecycle, MFA, endpoints, privileged access and training | Before readiness gate; recurring |
| 7 | 39.7 | Verify application access, tenant isolation, sessions and No-CUI boundaries | Before readiness gate |
| 8 | 39.8 | Secure SDLC, change approvals and separation of duties | Before readiness gate; per change |
| 9 | 39.9 | Production network baseline and drift detection | Before readiness gate; recurring |
| 10 | 39.10 | Secrets, encryption and key lifecycle | Before readiness gate; recurring |
| 11 | 39.11 | Vulnerability, patch and security-testing program | Before readiness gate; recurring |
| 12 | 39.12 | Audit integrity, telemetry and alert response | Before readiness gate; continuous |
| 13 | 39.13 | Incident response, spill handling and exercises | Before readiness gate; recurring |
| 14 | 39.14 | Backup, restoration, continuity and recovery | Before readiness gate; recurring |
| 15 | 39.15 | Vendors, subprocessors and inherited controls | Before readiness gate; recurring |
| 16 | 39.16 | Data classification, retention and secure disposal | Before readiness gate; recurring |
| 17 | 39.17 | Additional-category decision and conditional controls | Before scope freeze; or defer with reasons |
| 18 | 39.18 | Integrated design/implementation and Type I readiness gate | Before examination decision |
| 19 | 39.19 | Optional Type I examination and actual-report handling | Only after approved business decision |
| 20 | 39.20 | Type II period, populations and evidence readiness gate | Before proposed review period |
| 21 | 39.21 | Repeated control operation and management review | Throughout agreed Type II period |
| 22 | 39.22 | Type II examination, exceptions and actual-report handling | After sufficient period evidence |
| 23 | 39.23 | Controlled disclosure, continuous readiness and renewal | After issuance and ongoing |

Independent control work in 39.5–39.17 may run in parallel after the scope and evidence setup gates, provided each story's dependencies are satisfied. Capture genuine evidence from the first operation; do not wait for a formal period to begin collecting it. Material failures require reassessment, not silent continuation.

Type I concerns the system description and suitability of control design at an as-of date; readiness also requires controls to be placed in operation. Type II additionally addresses operating effectiveness over the specified period. An issued Type I report is not an automatic prerequisite for Type II; record the selected route with the service auditor. A Type I report does not establish sustained operation. Do not invent a mandatory three-, six-, or twelve-month period or treat an annual cadence, scan product, or penetration-test frequency as a universal SOC 2 mandate. FeDril must approve risk-based controls and confirm scope, evidence sufficiency, and period with its CPA.

### Shared Execution Contract — Required For Every Story

Copy this contract together with the selected story, or have the executing agent read this entire section before acting.

1. Read `AGENTS.md`, the selected story, its prerequisites, the assessment, and relevant code/configuration/evidence. Record date/time zone, exact commit, environment, evidence cutoff, reviewer role, available sources, and limitations. Verify previous findings; absence from Git is not proof that a human or cloud control is absent.
2. Assign actual named accountable owner, operator/evidence custodian, reviewer and approver. Role names below are proposals, not appointments. Missing people, access, funding, policies or approvals are explicit blockers with an owner and next action; never fabricate approval.
3. Maintain stable IDs `SOC2-39.n-T01` onward for the numbered task checkboxes, and link each to its parent, criterion/control ID, risk, assessment gap, priority, dependency, due date, required evidence and acceptance test. Use P0 blocker, P1 foundation, P2 material remediation, P3 operating consistency, P4 enhancement, adjusted for verified risk. Do not mistake a later sequence number for low security urgency.
4. Track task status separately as Planned / In Progress / Blocked / Ready For Review / Accepted / Deferred / Not Applicable. These story labels do not certify existing controls. For each control separately record Design (adequate/gap/unverified), Implementation (verified/partial/not implemented/unverified), and Operating Evidence (sufficient for stated period/partial/unavailable/unverified). Every conclusion needs evidence and a limitation; N/A or deferral needs rationale, approver, revisit date, and criteria impact.
5. Create or reuse a protected SOC 2 workspace and index. Public Git may contain sanitized templates, task IDs and non-sensitive summaries only. Do not commit reports, personnel records, access exports, detailed vulnerability findings, customer documents, credentials, sensitive system descriptions or raw evidence. Record evidence ID, control, source, environment, date/period, full population or sampling basis, collector, reviewer, result, freshness, integrity/version reference, retention, access restriction and location. Verify completeness and accuracy of system-produced evidence; redact without destroying necessary traceability. Never backdate evidence.
6. Technical stories permit only explicitly requested, bounded code/IaC/test changes when that story is executed. First prove a gap; reuse working controls rather than rebuilding them or adding a SOC 2 UI. Apply high-risk verification from `AGENTS.md` to security boundaries. Document baseline, affected contracts, implementation, tests, rollout and rollback. Production deployments, account/permission changes, destructive deletion/history rewrites, secret rotation, external scans, purchases and communications require separate authorization and change approval; stop at that boundary with a runbook.
7. Governance stories create drafts, registers, templates and supported decision records; an agent cannot sign management assertions, perform an independent CPA examination, appoint itself approver, execute a recurring human review merely by documenting it, or claim a report was issued.
8. Preserve No-CUI, server-side RBAC, tenant isolation, audit atomicity, safe error handling and evidence traceability. SOC 2 does not authorize CUI processing or substitute for CMMC, FedRAMP, legal advice or government approval. Never use “SOC 2 certified”; prefer exact reviewed report wording. No checklist guarantees a favorable opinion or eliminates residual risk.
9. Close a gap only after an authorized reviewer validates the specific design, implementation or operating deficiency and retest evidence. Record failures, interim safeguards, risk acceptance, reviewer conflicts, deadline, escalation and remaining risk. Acceptance does not erase historical failures or bind the auditor's opinion.
10. Every execution must end with tasks completed/not completed, changed files/configurations, evidence references, exact verification commands/results, skipped checks, human actions/approvals still required, residual risks, updated register entries and the next unblocked story/task. Draft documentation alone is never completion of an operating control.

Proposed artifact names below are logical names in the protected workspace established by 39.2, not instructions to publish them in this repository. Reuse existing records wherever possible. The only document changed by this backlog-authoring task is `docs/development-story-prompts.md`.

### Story 39.1: Define SOC 2 Scope And Readiness Decision
**Status: Planned**

**Work type:** Governance; parent of the scope and readiness track  
**Proposed accountable role:** Executive sponsor and SOC 2 owner  
**Dependencies:** None; trigger 39.4 triage immediately for plausible exposure  
**Type I / Type II relevance:** Both  
**Required deliverables:** scope-decision, system-description, control-matrix, customer-demand-register

Prompt:

You are helping FeDril establish an evidence-backed, reviewer-supported scope and a measurable proceed/defer decision before committing to an examination. Execute Story 39.1, "Define SOC 2 Scope And Readiness Decision," using the Shared Execution Contract in this document and `AGENTS.md`. Read both before acting. Inspect existing implementation and evidence first; complete only the work authorized by this story and the user's execution request. Reuse verified controls and identify unavailable evidence, human approvals and external actions explicitly.

> Context:
>
> - Epic: SOC 2 Assurance Program
> - Parent traceability: Original Story 39.1 in `docs/development-phase-use-cases.md`; retain its original governance purpose.
> - User story: As the accountable executive sponsor and soc 2 owner, I want to establish an evidence-backed, reviewer-supported scope and a measurable proceed/defer decision before committing to an examination.
> - Acceptance criteria: Setup gate: candidate boundary, accountable sponsor, criteria decision and versioned draft matrix exist; unanswered facts are explicit. Final acceptance: authorized scope approval, qualified review, complete scoped mapping and measurable gates are recorded. No report or readiness claim follows from this story alone.

Tasks (stable IDs are `SOC2-39.1-T01` onward):

- [ ] `SOC2-39.1-T01` — Record the assessment metadata and reconcile the historical assessment with current architecture, production configuration, tests, existing governance evidence and customer commitments. Inventory what is verified versus unavailable.
- [ ] `SOC2-39.1-T02` — Define FeDril SaaS services, infrastructure, software, people, procedures, data flows, locations, environments, production-support systems, assets, subprocessors, customer responsibilities and exclusions. Include source control/CI/CD and supporting workforce controls even where not customer-facing. Record service commitments and system requirements.
- [ ] `SOC2-39.1-T03` — Use Security/common criteria as the baseline; assess Availability, Confidentiality, Processing Integrity and Privacy applicability separately through 39.17. Preserve No-CUI and document boundaries with customer enclaves, MSP-managed systems and C3PAO work. Exclusions must not remove FeDril's own dependencies or commitments.
- [ ] `SOC2-39.1-T04` — Obtain current authorized AICPA Trust Services Criteria and Description Criteria through appropriate access; map every scoped criterion at summary level to risks, controls, owners, evidence, frequencies and gaps. Do not copy proprietary criteria text or pretend the licensed mapping was reviewed when unavailable.
- [ ] `SOC2-39.1-T05` — Build a current-state matrix distinguishing control design, implementation and period evidence. Include evidence freshness, source reliability, known limitations, repository exposure and live-configuration verification needs.
- [ ] `SOC2-39.1-T06` — Define objective Type I and Type II proceed/defer gates, budget assumptions, verified customer/partner/procurement triggers and qualified reviewer input/conflicts. Record a Type I-first or direct-Type II route, scope version, approvers and review date.
- [ ] `SOC2-39.1-T07` — Reconcile the scope after 39.5–39.17; supply the approved description and mapping to 39.18/39.20 and record all subsequent significant changes.

Instructions:

1. Record task-level owner, reviewer, due date, priority, prerequisites, criterion/control mapping, Type I/II impact and evidence/verification outcome in the shared register.
2. Produce or update the required deliverables in the approved protected location. Keep only sanitized references in Git; obtain human review for decisions and signatures.
3. Apply the shared safety and change boundaries. Implement only verified, authorized gaps; preserve working controls and document remaining external or recurring work.
4. Validate every acceptance criterion against actual evidence, record design/implementation/operating conclusions separately, and hand off the next unblocked task with all blockers and residual risks. Do not mark this story accepted solely because prompts or templates were written.

#-----------------------------------------

### Story 39.2: Remediate Gaps And Collect Operating Evidence
**Status: Planned**

**Work type:** Governance and evidence operations; parent of 39.4–39.22  
**Proposed accountable role:** SOC 2 owner with control owners and independent reviewers  
**Dependencies:** 39.1 candidate scope; permits provisional setup while scope approval is pending  
**Type I / Type II relevance:** Both; operating evidence especially Type II  
**Required deliverables:** gap-register, task-register, evidence-index, evidence-calendar, exception-register

Prompt:

You are helping FeDril coordinate every remediation and evidence obligation without losing failed controls, overdue tasks or traceability. Execute Story 39.2, "Remediate Gaps And Collect Operating Evidence," using the Shared Execution Contract in this document and `AGENTS.md`. Read both before acting. Inspect existing implementation and evidence first; complete only the work authorized by this story and the user's execution request. Reuse verified controls and identify unavailable evidence, human approvals and external actions explicitly.

> Context:
>
> - Epic: SOC 2 Assurance Program
> - Parent traceability: Original Story 39.2 in `docs/development-phase-use-cases.md`; retain its original governance purpose.
> - User story: As the accountable soc 2 owner with control owners and independent reviewers, I want to coordinate every remediation and evidence obligation without losing failed controls, overdue tasks or traceability.
> - Acceptance criteria: Setup gate: protected storage, populated task/gap register and owned calendar are usable. Final period acceptance: every scoped control and gap has a reviewed disposition, evidence traceability and signed readiness decision; stale/missing evidence blocks affected conclusions.

Tasks (stable IDs are `SOC2-39.2-T01` onward):

- [ ] `SOC2-39.2-T01` — Seed the task/gap register with all 36 assessment checklist rows, all SOC2-GAP-001 through SOC2-GAP-015 entries and every task in this section. Preserve original findings as dated history; map current verification and new gaps rather than silently overwriting.
- [ ] `SOC2-39.2-T02` — For each task record risk/severity, why it matters, named owner/reviewer, dependency, target date, interim safeguard, evidence requirement, Type I/II impact, closure test and residual risk. Propose timing, obtain approval and distinguish commitments from estimates.
- [ ] `SOC2-39.2-T03` — Establish a protected evidence repository with least privilege, MFA, access reviews, encryption, version/integrity protection, backup, approved retention and reviewer access. Verify a save/read/deny/recover sample. Keep sensitive material and reports out of public Git.
- [ ] `SOC2-39.2-T04` — Build the per-control operating calendar: event-driven or periodic cadence, complete population, evidence source, collector, reviewer, collection deadline, review deadline, absence/failure alert and escalation. Include no-event periods without inventing events.
- [ ] `SOC2-39.2-T05` — Require evidence quality review: source authenticity, completeness/accuracy, population/sample basis, timestamp/period, environment, freshness, reviewer, rejection reason and corrective action. Passing tests cannot stand in for a human review.
- [ ] `SOC2-39.2-T06` — Govern exceptions, failures and significant changes with severity, escalation, expiring risk acceptance, corrective action and independent retest. Distinguish design closure, implementation closure and demonstrated period effectiveness; retain failures in the audit trail.
- [ ] `SOC2-39.2-T07` — Run readiness reviews at 39.18 and 39.22, preserve dated evidence-index snapshots, actual approvers and decisions, and maintain the program during 39.21/39.23.

Instructions:

1. Record task-level owner, reviewer, due date, priority, prerequisites, criterion/control mapping, Type I/II impact and evidence/verification outcome in the shared register.
2. Produce or update the required deliverables in the approved protected location. Keep only sanitized references in Git; obtain human review for decisions and signatures.
3. Apply the shared safety and change boundaries. Implement only verified, authorized gaps; preserve working controls and document remaining external or recurring work.
4. Validate every acceptance criterion against actual evidence, record design/implementation/operating conclusions separately, and hand off the next unblocked task with all blockers and residual risks. Do not mark this story accepted solely because prompts or templates were written.

#-----------------------------------------

### Story 39.3: Govern Independent Examination And Report Distribution
**Status: Planned**

**Work type:** Governance and independent-assurance coordination; parent of 39.19/39.22/39.23  
**Proposed accountable role:** Executive sponsor, legal/contract owner and SOC 2 owner  
**Dependencies:** 39.1/39.2 setup; formal examination requires later readiness gates  
**Type I / Type II relevance:** Both  
**Required deliverables:** examination-register, auditor-due-diligence, claims-register, report-distribution-register

Prompt:

You are helping FeDril govern independent examinations and restricted report use without treating internal artifacts as assurance reports. Execute Story 39.3, "Govern Independent Examination And Report Distribution," using the Shared Execution Contract in this document and `AGENTS.md`. Read both before acting. Inspect existing implementation and evidence first; complete only the work authorized by this story and the user's execution request. Reuse verified controls and identify unavailable evidence, human approvals and external actions explicitly.

> Context:
>
> - Epic: SOC 2 Assurance Program
> - Parent traceability: Original Story 39.3 in `docs/development-phase-use-cases.md`; retain its original governance purpose.
> - User story: As the accountable executive sponsor, legal/contract owner and soc 2 owner, I want to govern independent examinations and restricted report use without treating internal artifacts as assurance reports.
> - Acceptance criteria: Setup gate: governance templates, claims restrictions and engagement gates exist. Type-specific final acceptance requires the actual independent report reference and authorized review, or an explicit defer decision. A report cannot be manufactured by story completion.

Tasks (stable IDs are `SOC2-39.3-T01` onward):

- [ ] `SOC2-39.3-T01` — Create lifecycle records for not engaged, readiness consultation, engagement approved, examination in progress, report issued, superseded and renewal pending. Record proposed type, criteria, system, dates/period, business justification and budget without implying engagement occurred.
- [ ] `SOC2-39.3-T02` — Define the pre-engagement gate: stable scope, material gaps dispositioned, assertion preparation, reviewed evidence index, named management authority, customer demand and approved resources. Permit early CPA scoping consultation; it is not a formal examination.
- [ ] `SOC2-39.3-T03` — Before selecting a firm, verify current CPA licensing, relevant competence, peer-review standing where applicable, independence, conflicts and relationships with readiness/tool vendors. Management retains control ownership and decisions; do not assume the same advisor can independently attest all its work.
- [ ] `SOC2-39.3-T04` — Prepare engagement terms, confidentiality, secure auditor access, examination type/period, subservice treatment, requested populations, milestones, fees and escalation. Only authorized people execute agreements and management assertions.
- [ ] `SOC2-39.3-T05` — Create an auditor-request tracker with request ID, control, population, due date, owner, supplied version, reviewer and response. Preserve sampling integrity; do not select only successful examples or coach evidence fabrication.
- [ ] `SOC2-39.3-T06` — Implement a claims register with exact approved wording, source/report reference, actual scope/type/period, owner, approver, use channel and review date. Without a report, allow only truthful supported readiness wording. Do not describe FeDril as SOC 2 certified, government approved or authorized for CUI.
- [ ] `SOC2-39.3-T07` — Define restricted-use report storage/distribution by intended audience and report terms: recipient, purpose, confidentiality/NDA requirement where appropriate, approver, version, sent date, access expiry and revocation/supersession. Verify an actual issued report before activating report-specific claims; record renewal decisions.

Instructions:

1. Record task-level owner, reviewer, due date, priority, prerequisites, criterion/control mapping, Type I/II impact and evidence/verification outcome in the shared register.
2. Produce or update the required deliverables in the approved protected location. Keep only sanitized references in Git; obtain human review for decisions and signatures.
3. Apply the shared safety and change boundaries. Implement only verified, authorized gaps; preserve working controls and document remaining external or recurring work.
4. Validate every acceptance criterion against actual evidence, record design/implementation/operating conclusions separately, and hand off the next unblocked task with all blockers and residual risks. Do not mark this story accepted solely because prompts or templates were written.

#-----------------------------------------

### Story 39.4: Investigate Repository Exposure And Govern Containment
**Status: Planned**

**Work type:** Security investigation and separately authorized remediation  
**Proposed accountable role:** Security lead and engineering lead  
**Dependencies:** None for immediate triage; use provisional incident tracking until 39.2 exists  
**Type I / Type II relevance:** Both; P0 pending verification  
**Required deliverables:** restricted-exposure-investigation, containment-plan, public-information-boundary

Prompt:

You are helping FeDril resolve the suspected public-repository data exposure and prevent recurrence without spreading sensitive content. Execute Story 39.4, "Investigate Repository Exposure And Govern Containment," using the Shared Execution Contract in this document and `AGENTS.md`. Read both before acting. Inspect existing implementation and evidence first; complete only the work authorized by this story and the user's execution request. Reuse verified controls and identify unavailable evidence, human approvals and external actions explicitly.

> Context:
>
> - Epic: SOC 2 Assurance Program
> - Parent traceability: New delivery story under Stories 39.1 and 39.2; defined in this consolidated execution backlog.
> - User story: As the accountable security lead and engineering lead, I want to resolve the suspected public-repository data exposure and prevent recurrence without spreading sensitive content.
> - Acceptance criteria: Authorized investigation/disposition, containment verification and recurrence checks are recorded; all residual exposure is owned. An uninspected dump cannot be marked safe, and a draft cleanup plan cannot close the finding.

Tasks (stable IDs are `SOC2-39.4-T01` onward):

- [ ] `SOC2-39.4-T01` — Reverify repository visibility and the database dump identified in SOC2-GAP-001 using metadata and provenance first. Inventory relevant history, releases, artifacts and operational documents without printing database contents or secrets. A public repository is not itself a SOC 2 failure.
- [ ] `SOC2-39.4-T02` — Have an authorized custodian determine whether the dump is synthetic, contains personal/customer data or secrets, and whether CUI exposure is plausible. Record only sanitized findings in Git; unknown provenance remains a blocker, not a clean result.
- [ ] `SOC2-39.4-T03` — Preserve restricted incident evidence and obtain a security/legal decision on severity, containment, notification duties and investigation. If spill is suspected, use 39.13 and stop unnecessary processing or redistribution.
- [ ] `SOC2-39.4-T04` — Prepare exact-target removal/history-remediation and credential-rotation runbooks where justified, including downstream clones/forks/caches, dependency impact, backups and coordination. Execute only under separate explicit authorization; deletion of a current file does not erase past exposure.
- [ ] `SOC2-39.4-T05` — Approve a public-information boundary and repository visibility decision. Add scoped ignore/pre-commit/CI secret and prohibited-artifact checks, with synthetic fixtures and false-positive handling; ignore rules do not remove already tracked files.
- [ ] `SOC2-39.4-T06` — Validate approved containment, affected credential invalidation where applicable, prevention tests and incident disposition. Record unremovable-copy risk and unresolved actions.

Instructions:

1. Record task-level owner, reviewer, due date, priority, prerequisites, criterion/control mapping, Type I/II impact and evidence/verification outcome in the shared register.
2. Produce or update the required deliverables in the approved protected location. Keep only sanitized references in Git; obtain human review for decisions and signatures.
3. Apply the shared safety and change boundaries. Implement only verified, authorized gaps; preserve working controls and document remaining external or recurring work.
4. Validate every acceptance criterion against actual evidence, record design/implementation/operating conclusions separately, and hand off the next unblocked task with all blockers and residual risks. Do not mark this story accepted solely because prompts or templates were written.

#-----------------------------------------

### Story 39.5: Establish Governance Risk Policies And Management Oversight
**Status: Planned**

**Work type:** Organizational governance  
**Proposed accountable role:** Executive sponsor and security lead  
**Dependencies:** 39.1/39.2 setup; 39.4 risk information  
**Type I / Type II relevance:** Both  
**Required deliverables:** risk-register, policy-register, accountability-matrix, management-review-record

Prompt:

You are helping FeDril establish accountable organizational controls and a risk-based security program. Execute Story 39.5, "Establish Governance Risk Policies And Management Oversight," using the Shared Execution Contract in this document and `AGENTS.md`. Read both before acting. Inspect existing implementation and evidence first; complete only the work authorized by this story and the user's execution request. Reuse verified controls and identify unavailable evidence, human approvals and external actions explicitly.

> Context:
>
> - Epic: SOC 2 Assurance Program
> - Parent traceability: New delivery story under Stories 39.1 and 39.2; defined in this consolidated execution backlog.
> - User story: As the accountable executive sponsor and security lead, I want to establish accountable organizational controls and a risk-based security program.
> - Acceptance criteria: Named accountable people, approved risk treatment, communicated policies and an actual reviewed management record exist. Material residual risks have authorized disposition; missing approvals remain blockers.

Tasks (stable IDs are `SOC2-39.5-T01` onward):

- [ ] `SOC2-39.5-T01` — Appoint sponsor, program owner, control owners, reviewers and deputies; record competence, capacity, reporting paths, conflicts and founder-stage concentration risk. Use independent review or documented compensating safeguards for unavoidable conflicts.
- [ ] `SOC2-39.5-T02` — Perform an enterprise risk assessment covering threats, likelihood, impact, fraud, workforce, remote work, vendors, software supply chain, data boundary, continuity and significant change. Map risk treatment to controls, owners, deadlines and residual risk.
- [ ] `SOC2-39.5-T03` — Approve and communicate a proportionate policy baseline: information security, access/JML, acceptable use, endpoints/remote work, secure development/change, vulnerabilities, incidents, vendors, classification/retention, encryption/secrets, logging, backup/continuity and evidence handling.
- [ ] `SOC2-39.5-T04` — Set policy ownership, version, approval, effective date, review cycle and workforce acknowledgement. Draft policies are not approved or operating controls; exceptions require expiry and authority.
- [ ] `SOC2-39.5-T05` — Define internal/external security communications, confidential concern reporting, ethics/disciplinary handling, objectives and escalation. Obtain qualified legal/HR review where applicable.
- [ ] `SOC2-39.5-T06` — Run an initial management review of risks, gap priorities, funding and staffing; set risk-based review cadence and significant-change triggers. Track corrective actions and reassessments.

Instructions:

1. Record task-level owner, reviewer, due date, priority, prerequisites, criterion/control mapping, Type I/II impact and evidence/verification outcome in the shared register.
2. Produce or update the required deliverables in the approved protected location. Keep only sanitized references in Git; obtain human review for decisions and signatures.
3. Apply the shared safety and change boundaries. Implement only verified, authorized gaps; preserve working controls and document remaining external or recurring work.
4. Validate every acceptance criterion against actual evidence, record design/implementation/operating conclusions separately, and hand off the next unblocked task with all blockers and residual risks. Do not mark this story accepted solely because prompts or templates were written.

#-----------------------------------------

### Story 39.6: Implement Workforce Access Device Security And Training
**Status: Planned**

**Work type:** Workforce operations and identity/device configuration  
**Proposed accountable role:** IT/security and workforce/contractor manager  
**Dependencies:** 39.5 policy baseline; 39.2 evidence storage  
**Type I / Type II relevance:** Both; repeated reviews in Type II  
**Required deliverables:** identity-inventory, JML-procedure, access-review, device-register, training-register

Prompt:

You are helping FeDril control employee, contractor, privileged and service access throughout the workforce lifecycle. Execute Story 39.6, "Implement Workforce Access Device Security And Training," using the Shared Execution Contract in this document and `AGENTS.md`. Read both before acting. Inspect existing implementation and evidence first; complete only the work authorized by this story and the user's execution request. Reuse verified controls and identify unavailable evidence, human approvals and external actions explicitly.

> Context:
>
> - Epic: SOC 2 Assurance Program
> - Parent traceability: New delivery story under Stories 39.1 and 39.2; defined in this consolidated execution backlog.
> - User story: As the accountable it/security and workforce/contractor manager, I want to control employee, contractor, privileged and service access throughout the workforce lifecycle.
> - Acceptance criteria: Live identity/device evidence, lifecycle test, completed access review with removals and training/acknowledgement records are reviewed; unverified systems and exceptions remain explicit.

Tasks (stable IDs are `SOC2-39.6-T01` onward):

- [ ] `SOC2-39.6-T01` — Inventory all human/service identities and privileges in identity provider, Azure, GitHub, databases, storage, monitoring, support, HubSpot and evidence systems where in scope. Record owner, purpose, authentication, expiry and access dependency.
- [ ] `SOC2-39.6-T02` — Design and verify MFA and appropriate conditional-access enforcement for workforce/privileged access, governed break-glass accounts, secure recovery and service/workload identity treatment. Export sanitized live policy evidence; application JWT validation alone does not prove workforce MFA.
- [ ] `SOC2-39.6-T03` — Implement joiner/mover/leaver requests, manager authorization, least privilege, access expiry, prompt termination/revocation including sessions/tokens, asset return and evidence. Approve risk-based termination targets and test a synthetic lifecycle.
- [ ] `SOC2-39.6-T04` — Perform a complete initial access review against authoritative populations, including dormant accounts, administrators, bypass rights, external collaborators and support access. Record reviewer decisions and verify removals; schedule recurrence.
- [ ] `SOC2-39.6-T05` — Inventory endpoints and approved BYOD/remote-work arrangements; verify encryption, screen lock, patching, endpoint protection, secure disposal and lost-device response. Document home/office physical safeguards and inherited data-center controls.
- [ ] `SOC2-39.6-T06` — Obtain appropriate confidentiality/acceptable-use agreements, role-appropriate screening where lawful and justified, onboarding and recurring security training. Cover phishing, credential safety, No-CUI handling, incident reporting and privileged responsibilities.
- [ ] `SOC2-39.6-T07` — Record independent review or compensating oversight for founder-managed privileges; validate denied access after revocation and retain actual training completions, not attendance templates.

Instructions:

1. Record task-level owner, reviewer, due date, priority, prerequisites, criterion/control mapping, Type I/II impact and evidence/verification outcome in the shared register.
2. Produce or update the required deliverables in the approved protected location. Keep only sanitized references in Git; obtain human review for decisions and signatures.
3. Apply the shared safety and change boundaries. Implement only verified, authorized gaps; preserve working controls and document remaining external or recurring work.
4. Validate every acceptance criterion against actual evidence, record design/implementation/operating conclusions separately, and hand off the next unblocked task with all blockers and residual risks. Do not mark this story accepted solely because prompts or templates were written.

#-----------------------------------------

### Story 39.7: Verify Application Security And Tenant Data Boundaries
**Status: Planned**

**Work type:** Engineering verification and bounded remediation  
**Proposed accountable role:** Engineering lead and security reviewer  
**Dependencies:** 39.1 inventory, 39.2 tracking, 39.5 policies  
**Type I / Type II relevance:** Both  
**Required deliverables:** application-control-matrix, focused-test-evidence, remediation-links

Prompt:

You are helping FeDril prove the existing FeDril security boundaries and remediate actual gaps without duplicating implemented features. Execute Story 39.7, "Verify Application Security And Tenant Data Boundaries," using the Shared Execution Contract in this document and `AGENTS.md`. Read both before acting. Inspect existing implementation and evidence first; complete only the work authorized by this story and the user's execution request. Reuse verified controls and identify unavailable evidence, human approvals and external actions explicitly.

> Context:
>
> - Epic: SOC 2 Assurance Program
> - Parent traceability: New delivery story under Stories 39.1 and 39.2; defined in this consolidated execution backlog.
> - User story: As the accountable engineering lead and security reviewer, I want to prove the existing FeDril security boundaries and remediate actual gaps without duplicating implemented features.
> - Acceptance criteria: Complete affected-path coverage, negative/isolation tests and reviewer-validated fixes are recorded. Tests demonstrate their stated scope, not universal production effectiveness.

Tasks (stable IDs are `SOC2-39.7-T01` onward):

- [ ] `SOC2-39.7-T01` — Inventory API, browser, export, reporting, search, background-job, storage and support paths; map existing authorization and tenant enforcement to controls and tests.
- [ ] `SOC2-39.7-T02` — Verify authentication/session lifecycle: issuer/audience/signature validation, session/token expiry/revocation, recovery, rate limits, CSRF/CORS protections where applicable, secure cookies/headers and explicit disabling of dev-auth bypasses in production.
- [ ] `SOC2-39.7-T03` — Exercise allowed/denied role and tenant matrices including cross-tenant IDs, empty results, indirect references, concurrent/repeated requests and admin/support paths. Permissions must remain server-authoritative and fail closed.
- [ ] `SOC2-39.7-T04` — Verify No-CUI policy acknowledgement, upload metadata/content handling, quarantine/malware integration and rejection paths with synthetic data only. Document classification limitations; do not claim perfect CUI detection.
- [ ] `SOC2-39.7-T05` — Verify atomic append-only audit events for protected mutations, approvals, exports, upload decisions and rollback/failure behavior. Use real persistence/provider tests where mocks cannot prove the invariant.
- [ ] `SOC2-39.7-T06` — Implement only verified defects using existing architecture and tests; preserve API contracts and run high-risk verification for affected boundaries. Capture source and deployed-state evidence separately and link monitoring/retention work to 39.12/39.16.

Instructions:

1. Record task-level owner, reviewer, due date, priority, prerequisites, criterion/control mapping, Type I/II impact and evidence/verification outcome in the shared register.
2. Produce or update the required deliverables in the approved protected location. Keep only sanitized references in Git; obtain human review for decisions and signatures.
3. Apply the shared safety and change boundaries. Implement only verified, authorized gaps; preserve working controls and document remaining external or recurring work.
4. Validate every acceptance criterion against actual evidence, record design/implementation/operating conclusions separately, and hand off the next unblocked task with all blockers and residual risks. Do not mark this story accepted solely because prompts or templates were written.

#-----------------------------------------

### Story 39.8: Enforce Secure SDLC Change Approval And Separation Of Duties
**Status: Planned**

**Work type:** Engineering, source-control governance and change operations  
**Proposed accountable role:** Engineering lead with independent change reviewer  
**Dependencies:** 39.5 policies; 39.6 identity ownership  
**Type I / Type II relevance:** Both; each change during Type II  
**Required deliverables:** change-procedure, protection-baseline, release-evidence, emergency-change-log

Prompt:

You are helping FeDril make code, infrastructure, configuration and content changes authorized, tested and traceable. Execute Story 39.8, "Enforce Secure SDLC Change Approval And Separation Of Duties," using the Shared Execution Contract in this document and `AGENTS.md`. Read both before acting. Inspect existing implementation and evidence first; complete only the work authorized by this story and the user's execution request. Reuse verified controls and identify unavailable evidence, human approvals and external actions explicitly.

> Context:
>
> - Epic: SOC 2 Assurance Program
> - Parent traceability: New delivery story under Stories 39.1 and 39.2; defined in this consolidated execution backlog.
> - User story: As the accountable engineering lead with independent change reviewer, I want to make code, infrastructure, configuration and content changes authorized, tested and traceable.
> - Acceptance criteria: Actual protection settings and allowed/blocked workflow tests support the approved change model; a complete change sample links request to reviewer, commit, artifact, deployment and post-check.

Tasks (stable IDs are `SOC2-39.8-T01` onward):

- [ ] `SOC2-39.8-T01` — Define normal/emergency changes, risk assessment, tickets, security/privacy review, test requirements, migrations, approvals, deployment identity, rollback and post-change validation. Include IaC, production settings and security-relevant compliance content.
- [ ] `SOC2-39.8-T02` — Verify actual branch/ruleset/environment controls: required checks, independent reviewers, stale-review dismissal, direct-push limits, force-push/deletion rules, admin bypass and production approval. Protected=true alone is insufficient.
- [ ] `SOC2-39.8-T03` — Design enforceable segregation among author, approver and deployer where practicable. Document founder-stage conflicts, compensating external review and time-bounded exceptions rather than inventing a second approver.
- [ ] `SOC2-39.8-T04` — Harden CI/CD with least-privilege tokens/workload identity, trusted dependency/action references, safe handling of untrusted pull requests, protected secrets and traceable immutable release artifacts.
- [ ] `SOC2-39.8-T05` — Add risk-based design/threat review and secure coding checks. Reuse existing SCA/secret scanning and coordinate expanded scans with 39.11; record approved suppressions and scanner coverage.
- [ ] `SOC2-39.8-T06` — Test a rejected change and an approved synthetic/staging change through review, CI, deployment evidence and rollback. Rehearse emergency authorization and retrospective review; do not bypass protected workflows.

Instructions:

1. Record task-level owner, reviewer, due date, priority, prerequisites, criterion/control mapping, Type I/II impact and evidence/verification outcome in the shared register.
2. Produce or update the required deliverables in the approved protected location. Keep only sanitized references in Git; obtain human review for decisions and signatures.
3. Apply the shared safety and change boundaries. Implement only verified, authorized gaps; preserve working controls and document remaining external or recurring work.
4. Validate every acceptance criterion against actual evidence, record design/implementation/operating conclusions separately, and hand off the next unblocked task with all blockers and residual risks. Do not mark this story accepted solely because prompts or templates were written.

#-----------------------------------------

### Story 39.9: Harden Production Configuration Networks And Drift Detection
**Status: Planned**

**Work type:** Infrastructure engineering and operations  
**Proposed accountable role:** Platform/engineering lead with security reviewer  
**Dependencies:** 39.1 assets; 39.8 change control; coordinate 39.10  
**Type I / Type II relevance:** Both  
**Required deliverables:** configuration-baseline, network-flow-matrix, drift-runbook, rollout-plan

Prompt:

You are helping FeDril reconcile live production configuration with approved IaC and restrict unnecessary exposure. Execute Story 39.9, "Harden Production Configuration Networks And Drift Detection," using the Shared Execution Contract in this document and `AGENTS.md`. Read both before acting. Inspect existing implementation and evidence first; complete only the work authorized by this story and the user's execution request. Reuse verified controls and identify unavailable evidence, human approvals and external actions explicitly.

> Context:
>
> - Epic: SOC 2 Assurance Program
> - Parent traceability: New delivery story under Stories 39.1 and 39.2; defined in this consolidated execution backlog.
> - User story: As the accountable platform/engineering lead with security reviewer, I want to reconcile live production configuration with approved IaC and restrict unnecessary exposure.
> - Acceptance criteria: Reviewed live-versus-IaC baseline, authorized exposure decisions, safe connectivity/deny tests and a successful owned drift cycle exist; skipped jobs cannot pass.

Tasks (stable IDs are `SOC2-39.9-T01` onward):

- [ ] `SOC2-39.9-T01` — Inventory intended and live network flows for web/API, PostgreSQL, Redis, storage, identity, monitoring and build/deploy agents. Compare current IaC to cloud exports; unavailable live access is Requires Verification.
- [ ] `SOC2-39.9-T02` — Investigate the assessment's PostgreSQL public-access/private-endpoint mismatch. Prefer restricted/private database connectivity where feasible; validate DNS, application and migration connectivity before disabling public access under approved rollout.
- [ ] `SOC2-39.9-T03` — Review firewalls, ingress/egress, management endpoints, TLS, network segmentation, storage exposure, service ports and rate/abuse protections. Record justified exceptions, approved configuration and review dates.
- [ ] `SOC2-39.9-T04` — Verify workload configuration, supported software versions, privileged service settings, environmental separation and security baseline; protect remote IaC state with access control, encryption, locking and recovery.
- [ ] `SOC2-39.9-T05` — Make scheduled infrastructure drift checks actually execute: correct prerequisites, credentials and state configuration; alert on skipped/failed runs as well as detected drift. Assign triage and remediation ownership.
- [ ] `SOC2-39.9-T06` — Test connectivity, denied access, alert delivery and rollback in an authorized safe environment; obtain explicit deployment/change approval for production and reconcile live evidence afterward.

Instructions:

1. Record task-level owner, reviewer, due date, priority, prerequisites, criterion/control mapping, Type I/II impact and evidence/verification outcome in the shared register.
2. Produce or update the required deliverables in the approved protected location. Keep only sanitized references in Git; obtain human review for decisions and signatures.
3. Apply the shared safety and change boundaries. Implement only verified, authorized gaps; preserve working controls and document remaining external or recurring work.
4. Validate every acceptance criterion against actual evidence, record design/implementation/operating conclusions separately, and hand off the next unblocked task with all blockers and residual risks. Do not mark this story accepted solely because prompts or templates were written.

#-----------------------------------------

### Story 39.10: Govern Secrets Encryption And Key Lifecycle
**Status: Planned**

**Work type:** Security engineering and key operations  
**Proposed accountable role:** Platform/security lead  
**Dependencies:** 39.6 identity inventory; 39.8 change control; 39.9 topology  
**Type I / Type II relevance:** Both  
**Required deliverables:** secret-key-inventory, encryption-matrix, rotation-runbook, access-review-evidence

Prompt:

You are helping FeDril minimize long-lived credentials and verify encryption and recoverable key operations. Execute Story 39.10, "Govern Secrets Encryption And Key Lifecycle," using the Shared Execution Contract in this document and `AGENTS.md`. Read both before acting. Inspect existing implementation and evidence first; complete only the work authorized by this story and the user's execution request. Reuse verified controls and identify unavailable evidence, human approvals and external actions explicitly.

> Context:
>
> - Epic: SOC 2 Assurance Program
> - Parent traceability: New delivery story under Stories 39.1 and 39.2; defined in this consolidated execution backlog.
> - User story: As the accountable platform/security lead, I want to minimize long-lived credentials and verify encryption and recoverable key operations.
> - Acceptance criteria: Reviewed inventory and encryption matrix, approved live permissions, safe lifecycle test evidence and owned exceptions exist; no secrets appear in task records or reports.

Tasks (stable IDs are `SOC2-39.10-T01` onward):

- [ ] `SOC2-39.10-T01` — Inventory secrets/keys by purpose, system, custodian, storage, access, expiry and rotation trigger without recording secret values. Cover database, storage, CI/CD, third parties, backups and audit/evidence systems.
- [ ] `SOC2-39.10-T02` — Use managed/workload identity and scoped authorization where supported; move unavoidable secrets into a governed vault. Remove client-side or source-code secret dependencies after proving compatibility.
- [ ] `SOC2-39.10-T03` — Investigate storage shared-key use and alternatives; disable unnecessary shared-key access only after workload migration, backup/restore verification and approved rollout. Record any justified remaining use.
- [ ] `SOC2-39.10-T04` — Verify encryption in transit/at rest, certificate ownership/renewal, key permissions, recovery and backup dependencies. Do not mandate customer-managed keys unless risk or commitments justify their additional operational burden.
- [ ] `SOC2-39.10-T05` — Define routine and compromise-triggered rotation, revocation, credential leak response, dual-key transition where needed and rollback. Execute only authorized rotations; retain metadata evidence and prove retired credentials no longer work.
- [ ] `SOC2-39.10-T06` — Test vault denial, secret redaction in logs/errors/CI, expiry alerts, approved renewal/rotation and emergency recovery using synthetic/non-production materials where possible.

Instructions:

1. Record task-level owner, reviewer, due date, priority, prerequisites, criterion/control mapping, Type I/II impact and evidence/verification outcome in the shared register.
2. Produce or update the required deliverables in the approved protected location. Keep only sanitized references in Git; obtain human review for decisions and signatures.
3. Apply the shared safety and change boundaries. Implement only verified, authorized gaps; preserve working controls and document remaining external or recurring work.
4. Validate every acceptance criterion against actual evidence, record design/implementation/operating conclusions separately, and hand off the next unblocked task with all blockers and residual risks. Do not mark this story accepted solely because prompts or templates were written.

#-----------------------------------------

### Story 39.11: Operate Vulnerability Patch And Security Testing Controls
**Status: Planned**

**Work type:** Engineering and vulnerability operations  
**Proposed accountable role:** Security lead and engineering owners  
**Dependencies:** 39.5 policy, 39.8 pipelines, 39.9 assets  
**Type I / Type II relevance:** Both; recurring Type II evidence  
**Required deliverables:** vulnerability-policy, scan-coverage, remediation-queue, restricted-test-report

Prompt:

You are helping FeDril detect, prioritize, remediate and independently validate security weaknesses with measurable follow-through. Execute Story 39.11, "Operate Vulnerability Patch And Security Testing Controls," using the Shared Execution Contract in this document and `AGENTS.md`. Read both before acting. Inspect existing implementation and evidence first; complete only the work authorized by this story and the user's execution request. Reuse verified controls and identify unavailable evidence, human approvals and external actions explicitly.

> Context:
>
> - Epic: SOC 2 Assurance Program
> - Parent traceability: New delivery story under Stories 39.1 and 39.2; defined in this consolidated execution backlog.
> - User story: As the accountable security lead and engineering owners, I want to detect, prioritize, remediate and independently validate security weaknesses with measurable follow-through.
> - Acceptance criteria: Coverage, approved SLAs, actual scan execution, tracked disposition and retests exist; required independent testing is evidenced or explicitly blocked/deferred with gate impact.

Tasks (stable IDs are `SOC2-39.11-T01` onward):

- [ ] `SOC2-39.11-T01` — Define asset coverage, severity plus exploitability/exposure prioritization, response/remediation SLAs, owners, exception authority, overdue escalation and patch windows. Approve numeric targets rather than asserting SOC 2 prescribes them.
- [ ] `SOC2-39.11-T02` — Inventory existing dependency/secret scans and gaps in SAST, DAST, container/image, IaC and platform scanning; implement proportionate coverage and patch monitoring with failed/skipped-scan alerts and governed suppression.
- [ ] `SOC2-39.11-T03` — Include OS/runtime/framework/database dependencies and end-of-support risk. Link each finding to a task with first-seen date, deadline, risk, fix version, retest and residual risk.
- [ ] `SOC2-39.11-T04` — Define independent penetration-test scope covering authentication, tenant isolation, authorization, uploads/exports and exposed infrastructure; obtain authorization, rules of engagement, vendor qualification and data restrictions before testing.
- [ ] `SOC2-39.11-T05` — Perform approved testing and triage, fix material findings, then obtain retest evidence. Keep exploit details and reports restricted; unresolved risks must be visible to management and the service auditor.
- [ ] `SOC2-39.11-T06` — Demonstrate one full scan-to-remediation-to-rescan cycle and an overdue/failed-scan escalation; measure backlog age and SLA performance. Choose future cadence based on risk and commitments.

Instructions:

1. Record task-level owner, reviewer, due date, priority, prerequisites, criterion/control mapping, Type I/II impact and evidence/verification outcome in the shared register.
2. Produce or update the required deliverables in the approved protected location. Keep only sanitized references in Git; obtain human review for decisions and signatures.
3. Apply the shared safety and change boundaries. Implement only verified, authorized gaps; preserve working controls and document remaining external or recurring work.
4. Validate every acceptance criterion against actual evidence, record design/implementation/operating conclusions separately, and hand off the next unblocked task with all blockers and residual risks. Do not mark this story accepted solely because prompts or templates were written.

#-----------------------------------------

### Story 39.12: Validate Audit Integrity Logging Monitoring And Alert Response
**Status: Planned**

**Work type:** Engineering and security operations  
**Proposed accountable role:** Security operations owner and engineering lead  
**Dependencies:** 39.7 audit controls, 39.9/39.10 infrastructure; 39.13 escalation can be drafted in parallel  
**Type I / Type II relevance:** Both; continuous operation for Type II  
**Required deliverables:** logging-matrix, alert-catalog, retention-settings, triage-records

Prompt:

You are helping FeDril prove relevant events reach protected storage and produce timely owned responses. Execute Story 39.12, "Validate Audit Integrity Logging Monitoring And Alert Response," using the Shared Execution Contract in this document and `AGENTS.md`. Read both before acting. Inspect existing implementation and evidence first; complete only the work authorized by this story and the user's execution request. Reuse verified controls and identify unavailable evidence, human approvals and external actions explicitly.

> Context:
>
> - Epic: SOC 2 Assurance Program
> - Parent traceability: New delivery story under Stories 39.1 and 39.2; defined in this consolidated execution backlog.
> - User story: As the accountable security operations owner and engineering lead, I want to prove relevant events reach protected storage and produce timely owned responses.
> - Acceptance criteria: Live ingestion and protected retention are verified, synthetic events trigger owned alerts, and dated response records exist. A dashboard definition without telemetry or responder evidence does not pass.

Tasks (stable IDs are `SOC2-39.12-T01` onward):

- [ ] `SOC2-39.12-T01` — Map application audit/security events and identity, cloud, repository, pipeline, database, storage and malware telemetry to sources, time synchronization, collection path, owner and detection purpose.
- [ ] `SOC2-39.12-T02` — Verify actual ingestion, completeness indicators, retention settings, access restrictions and storage/integrity controls. Test that privileged operators cannot silently alter protected audit history; document remaining administrative capabilities.
- [ ] `SOC2-39.12-T03` — Prohibit secrets/raw customer data in logs; validate redaction and access controls with synthetic negative tests while retaining useful correlation and incident context.
- [ ] `SOC2-39.12-T04` — Configure and validate risk-based detections for auth abuse, privilege change, tenant-boundary anomalies, deployments, malicious uploads, unusual storage/network events and monitoring failure. Include telemetry gaps and failed/skipped security jobs.
- [ ] `SOC2-39.12-T05` — Assign alert delivery, on-call/deputy, risk-based acknowledgement/escalation targets, triage, incident linkage and closure. Test alert delivery and missed-acknowledgement escalation end to end.
- [ ] `SOC2-39.12-T06` — Retain an actual reviewed triage cycle and periodic detection tuning/coverage review; reconcile retention with 39.16 and evidence needs.

Instructions:

1. Record task-level owner, reviewer, due date, priority, prerequisites, criterion/control mapping, Type I/II impact and evidence/verification outcome in the shared register.
2. Produce or update the required deliverables in the approved protected location. Keep only sanitized references in Git; obtain human review for decisions and signatures.
3. Apply the shared safety and change boundaries. Implement only verified, authorized gaps; preserve working controls and document remaining external or recurring work.
4. Validate every acceptance criterion against actual evidence, record design/implementation/operating conclusions separately, and hand off the next unblocked task with all blockers and residual risks. Do not mark this story accepted solely because prompts or templates were written.

#-----------------------------------------

### Story 39.13: Implement Incident Response And No-CUI Spill Exercises
**Status: Planned**

**Work type:** Organizational/security operations  
**Proposed accountable role:** Incident commander, security lead and legal/contract owner  
**Dependencies:** 39.5 policy; 39.6 contacts; 39.12 detection integration; immediate 39.4 escalation allowed  
**Type I / Type II relevance:** Both  
**Required deliverables:** incident-plan, restricted-incident-register, tabletop-report, corrective-actions

Prompt:

You are helping FeDril make incident and prohibited-data response executable, rehearsed and legally governed. Execute Story 39.13, "Implement Incident Response And No-CUI Spill Exercises," using the Shared Execution Contract in this document and `AGENTS.md`. Read both before acting. Inspect existing implementation and evidence first; complete only the work authorized by this story and the user's execution request. Reuse verified controls and identify unavailable evidence, human approvals and external actions explicitly.

> Context:
>
> - Epic: SOC 2 Assurance Program
> - Parent traceability: New delivery story under Stories 39.1 and 39.2; defined in this consolidated execution backlog.
> - User story: As the accountable incident commander, security lead and legal/contract owner, I want to make incident and prohibited-data response executable, rehearsed and legally governed.
> - Acceptance criteria: Approved accessible plan, validated contacts, completed exercise and reviewed corrective actions exist; real incidents keep accurate contemporaneous records separate from simulations.

Tasks (stable IDs are `SOC2-39.13-T01` onward):

- [ ] `SOC2-39.13-T01` — Approve incident classifications, roles/deputies, communication routes, severity, escalation, investigation, containment, eradication, recovery and post-incident review.
- [ ] `SOC2-39.13-T02` — Map applicable contractual/legal notification decision owners and clocks with qualified review; do not invent a universal notification deadline or send external notices without authorization.
- [ ] `SOC2-39.13-T03` — Define No-CUI spill containment, access restriction, preservation, authorized disposition and customer coordination without redistributing suspected CUI or destroying required investigation evidence.
- [ ] `SOC2-39.13-T04` — Create secure evidence/chain-of-custody, incident timeline, decision/approval and communication templates, including third-party incidents and cloud/vendor coordination.
- [ ] `SOC2-39.13-T05` — Conduct a recorded tabletop covering leaked credentials/public artifact exposure, cross-tenant access, ransomware/service outage and monitoring escalation. Synthetic scenarios must be labeled as exercises.
- [ ] `SOC2-39.13-T06` — Track lessons, owners, deadlines and retests; run an incident-to-recovery handoff with 39.14. Feed significant changes and recurring training back into the program.

Instructions:

1. Record task-level owner, reviewer, due date, priority, prerequisites, criterion/control mapping, Type I/II impact and evidence/verification outcome in the shared register.
2. Produce or update the required deliverables in the approved protected location. Keep only sanitized references in Git; obtain human review for decisions and signatures.
3. Apply the shared safety and change boundaries. Implement only verified, authorized gaps; preserve working controls and document remaining external or recurring work.
4. Validate every acceptance criterion against actual evidence, record design/implementation/operating conclusions separately, and hand off the next unblocked task with all blockers and residual risks. Do not mark this story accepted solely because prompts or templates were written.

#-----------------------------------------

### Story 39.14: Prove Backup Restoration Business Continuity And Recovery
**Status: Planned**

**Work type:** Platform operations and continuity governance  
**Proposed accountable role:** Engineering lead and executive continuity owner  
**Dependencies:** 39.9 infrastructure, 39.10 keys, 39.13 incident handoff  
**Type I / Type II relevance:** Both; additional Availability obligations if scoped  
**Required deliverables:** BIA, backup-matrix, recovery-runbooks, restore-test, continuity-exercise

Prompt:

You are helping FeDril show that FeDril can recover its required services and evidence within approved objectives. Execute Story 39.14, "Prove Backup Restoration Business Continuity And Recovery," using the Shared Execution Contract in this document and `AGENTS.md`. Read both before acting. Inspect existing implementation and evidence first; complete only the work authorized by this story and the user's execution request. Reuse verified controls and identify unavailable evidence, human approvals and external actions explicitly.

> Context:
>
> - Epic: SOC 2 Assurance Program
> - Parent traceability: New delivery story under Stories 39.1 and 39.2; defined in this consolidated execution backlog.
> - User story: As the accountable engineering lead and executive continuity owner, I want to show that FeDril can recover its required services and evidence within approved objectives.
> - Acceptance criteria: Approved objectives, verified backup operation, measured representative restore and continuity exercise results are reviewed; unmet objectives remain gaps, not successful tests.

Tasks (stable IDs are `SOC2-39.14-T01` onward):

- [ ] `SOC2-39.14-T01` — Perform business-impact analysis and dependency mapping; approve RPO/RTO and service recovery priorities without inventing customer uptime commitments.
- [ ] `SOC2-39.14-T02` — Inventory backup coverage for databases, storage, configuration, keys, source/evidence records and recovery dependencies; approve retention, encryption, access separation, geographic resilience and deletion protection proportionate to risk.
- [ ] `SOC2-39.14-T03` — Verify backup jobs, restore permissions, integrity checks and failure alerts in live configuration; monitor and review actual failures and resolutions.
- [ ] `SOC2-39.14-T04` — Run an authorized isolated production-equivalent restore with sanitized data, measured recovery time/data loss and application validation. Include tenant isolation, permissions and audit-history consistency; staging evidence alone cannot prove production recovery.
- [ ] `SOC2-39.14-T05` — Exercise business continuity including loss of cloud region/vendor, founder/key-person unavailability, workforce access and communications. Verify deputy access and recovery prerequisites without exposing secrets.
- [ ] `SOC2-39.14-T06` — Document failures, corrective actions, retest, reviewer approval and repeat cadence; do not execute destructive production failover without explicit approval.

Instructions:

1. Record task-level owner, reviewer, due date, priority, prerequisites, criterion/control mapping, Type I/II impact and evidence/verification outcome in the shared register.
2. Produce or update the required deliverables in the approved protected location. Keep only sanitized references in Git; obtain human review for decisions and signatures.
3. Apply the shared safety and change boundaries. Implement only verified, authorized gaps; preserve working controls and document remaining external or recurring work.
4. Validate every acceptance criterion against actual evidence, record design/implementation/operating conclusions separately, and hand off the next unblocked task with all blockers and residual risks. Do not mark this story accepted solely because prompts or templates were written.

#-----------------------------------------

### Story 39.15: Govern Vendors Subprocessors And Inherited Controls
**Status: Planned**

**Work type:** Vendor risk and contractual governance  
**Proposed accountable role:** Security lead and procurement/legal owner  
**Dependencies:** 39.1 boundary, 39.5 risk method, 39.2 protected evidence  
**Type I / Type II relevance:** Both  
**Required deliverables:** vendor-register, vendor-review, shared-responsibility-matrix, contract-actions

Prompt:

You are helping FeDril establish accountable third-party oversight without assuming provider assurance transfers to FeDril. Execute Story 39.15, "Govern Vendors Subprocessors And Inherited Controls," using the Shared Execution Contract in this document and `AGENTS.md`. Read both before acting. Inspect existing implementation and evidence first; complete only the work authorized by this story and the user's execution request. Reuse verified controls and identify unavailable evidence, human approvals and external actions explicitly.

> Context:
>
> - Epic: SOC 2 Assurance Program
> - Parent traceability: New delivery story under Stories 39.1 and 39.2; defined in this consolidated execution backlog.
> - User story: As the accountable security lead and procurement/legal owner, I want to establish accountable third-party oversight without assuming provider assurance transfers to FeDril.
> - Acceptance criteria: Complete material-vendor population, current reviewed assurance/contract evidence and owned complementary controls exist; unavailable reports or unresolved vendor risks are explicitly dispositioned.

Tasks (stable IDs are `SOC2-39.15-T01` onward):

- [ ] `SOC2-39.15-T01` — Inventory cloud, identity, source-control, scanning, communication, support and other material vendors/subprocessors by service, owner, data access, dependency, location and risk tier.
- [ ] `SOC2-39.15-T02` — Review onboarding due diligence, contractual security/data terms, confidentiality, breach notification, service commitments, termination/deletion, subprocessors and concentration/exit risk with qualified owners.
- [ ] `SOC2-39.15-T03` — Obtain relevant provider assurance reports through authorized channels; review actual scope, period, opinion, exceptions, complementary user-entity/subservice controls and any bridge letters. Keep reports restricted.
- [ ] `SOC2-39.15-T04` — Map inherited physical/environmental and infrastructure controls to FeDril responsibilities and operating evidence. Decide subservice carve-out/inclusive presentation with the CPA; a provider SOC report is not FeDril's report.
- [ ] `SOC2-39.15-T05` — Assign reviews before onboarding, on material change and at an approved risk-based recurrence; track expired evidence, vendor incidents, issues, owners and follow-up.
- [ ] `SOC2-39.15-T06` — Complete initial reviews for material providers; document exceptions, exit/contingency plans and actual approvals, linking required FeDril controls back to delivery stories.

Instructions:

1. Record task-level owner, reviewer, due date, priority, prerequisites, criterion/control mapping, Type I/II impact and evidence/verification outcome in the shared register.
2. Produce or update the required deliverables in the approved protected location. Keep only sanitized references in Git; obtain human review for decisions and signatures.
3. Apply the shared safety and change boundaries. Implement only verified, authorized gaps; preserve working controls and document remaining external or recurring work.
4. Validate every acceptance criterion against actual evidence, record design/implementation/operating conclusions separately, and hand off the next unblocked task with all blockers and residual risks. Do not mark this story accepted solely because prompts or templates were written.

#-----------------------------------------

### Story 39.16: Enforce Data Classification Retention Disposal And Customer Responsibilities
**Status: Planned**

**Work type:** Data governance and bounded engineering  
**Proposed accountable role:** Security/privacy or legal owner and engineering lead  
**Dependencies:** 39.1 flows, 39.5 policies, 39.10 encryption, 39.14 backups, 39.15 vendors  
**Type I / Type II relevance:** Both; Confidentiality/Privacy additions if scoped  
**Required deliverables:** data-inventory, retention-matrix, customer-responsibility-matrix, disposal-test

Prompt:

You are helping FeDril control non-CUI data from collection through disposal without weakening immutable evidence or customer boundaries. Execute Story 39.16, "Enforce Data Classification Retention Disposal And Customer Responsibilities," using the Shared Execution Contract in this document and `AGENTS.md`. Read both before acting. Inspect existing implementation and evidence first; complete only the work authorized by this story and the user's execution request. Reuse verified controls and identify unavailable evidence, human approvals and external actions explicitly.

> Context:
>
> - Epic: SOC 2 Assurance Program
> - Parent traceability: New delivery story under Stories 39.1 and 39.2; defined in this consolidated execution backlog.
> - User story: As the accountable security/privacy or legal owner and engineering lead, I want to control non-CUI data from collection through disposal without weakening immutable evidence or customer boundaries.
> - Acceptance criteria: Approved classification/retention/responsibility records and tested lifecycle controls exist without loss of required audit integrity; real customer data deletion requires separate authorization.

Tasks (stable IDs are `SOC2-39.16-T01` onward):

- [ ] `SOC2-39.16-T01` — Inventory accounts, obligations, evidence references/metadata, customer documents if permitted, support records, audit logs, telemetry, exports and backups; classify data and identify owner, purpose, locations, third parties and prohibited inputs.
- [ ] `SOC2-39.16-T02` — Approve retention periods by record class based on legal/contract needs, audit evidence and business risk; define legal hold, disposal authorization and backup expiry. Do not invent universal SOC 2 retention periods.
- [ ] `SOC2-39.16-T03` — Implement or verify lifecycle enforcement for primary data, replicas, caches, exports, logs and vendor copies using synthetic records. Respect legal holds and established immutable audit/report contracts; escalate incompatible deletion requirements instead of silently hard-deleting.
- [ ] `SOC2-39.16-T04` — Document customer versus FeDril responsibilities for access review, references versus uploaded evidence, prohibited CUI, authorized enclaves, customer SSP treatment and spill reporting. Verify public wording against actual enforcement.
- [ ] `SOC2-39.16-T05` — Restrict support/export access and non-production data use, minimize collection, protect confidential non-CUI data and review applicable privacy obligations even when Privacy is not a scoped category.
- [ ] `SOC2-39.16-T06` — Verify approved deletion/expiry and access-denial outcomes across applicable stores with reviewer signoff; record backup/vendor remnants, implementation limitations and residual risk.

Instructions:

1. Record task-level owner, reviewer, due date, priority, prerequisites, criterion/control mapping, Type I/II impact and evidence/verification outcome in the shared register.
2. Produce or update the required deliverables in the approved protected location. Keep only sanitized references in Git; obtain human review for decisions and signatures.
3. Apply the shared safety and change boundaries. Implement only verified, authorized gaps; preserve working controls and document remaining external or recurring work.
4. Validate every acceptance criterion against actual evidence, record design/implementation/operating conclusions separately, and hand off the next unblocked task with all blockers and residual risks. Do not mark this story accepted solely because prompts or templates were written.

#-----------------------------------------

### Story 39.17: Decide Additional Trust Categories And Implement Conditional Controls
**Status: Planned**

**Work type:** Scope decision with conditional engineering/operations  
**Proposed accountable role:** Executive sponsor, security owner and qualified assurance reviewer  
**Dependencies:** 39.1 customer commitments; 39.14–39.16 evidence  
**Type I / Type II relevance:** Both if added; otherwise governed deferral  
**Required deliverables:** category-decision, expanded-control-matrix, conditional-test-evidence

Prompt:

You are helping FeDril prevent unjustified scope growth while covering any verified commitments that require more than the initial Security scope. Execute Story 39.17, "Decide Additional Trust Categories And Implement Conditional Controls," using the Shared Execution Contract in this document and `AGENTS.md`. Read both before acting. Inspect existing implementation and evidence first; complete only the work authorized by this story and the user's execution request. Reuse verified controls and identify unavailable evidence, human approvals and external actions explicitly.

> Context:
>
> - Epic: SOC 2 Assurance Program
> - Parent traceability: New delivery story under Stories 39.1 and 39.2; defined in this consolidated execution backlog.
> - User story: As the accountable executive sponsor, security owner and qualified assurance reviewer, I want to prevent unjustified scope growth while covering any verified commitments that require more than the initial Security scope.
> - Acceptance criteria: Every category has a documented decision. Included categories have criterion-level controls and tested acceptance requirements before the integrated gate; unsupported exclusions are not allowed. New commitments reopen scope and dependencies.

Tasks (stable IDs are `SOC2-39.17-T01` onward):

- [ ] `SOC2-39.17-T01` — Review contracts, questionnaires, product behavior and data sensitivity; record independent decisions for Availability, Confidentiality, Processing Integrity and Privacy. Security/common criteria remain the baseline.
- [ ] `SOC2-39.17-T02` — If Availability is selected, map service commitments to capacity/SLO monitoring, outage handling, recovery, resilience and tested reporting; reconcile the 39.14 controls rather than duplicating them.
- [ ] `SOC2-39.17-T03` — If Confidentiality is selected, map confidential non-CUI data to classification, permitted access/disclosure, encryption, retention and disposal across vendors and backups.
- [ ] `SOC2-39.17-T04` — If Processing Integrity is selected, create explicit completeness, validity, accuracy, timeliness and authorization controls for scoped processing with reconciliations and failure handling.
- [ ] `SOC2-39.17-T05` — If Privacy is selected, obtain qualified review of the privacy lifecycle, notices/choices, collection/use, disclosure, rights handling, retention and safeguards; implement applicable controls without claiming a category replaces legal compliance.
- [ ] `SOC2-39.17-T06` — Record for each category include/defer/exclude rationale, approved owner, obligations, evidence, gaps and revisit trigger; before adding it, expand scoped criterion/task coverage and obtain reviewer approval.

Instructions:

1. Record task-level owner, reviewer, due date, priority, prerequisites, criterion/control mapping, Type I/II impact and evidence/verification outcome in the shared register.
2. Produce or update the required deliverables in the approved protected location. Keep only sanitized references in Git; obtain human review for decisions and signatures.
3. Apply the shared safety and change boundaries. Implement only verified, authorized gaps; preserve working controls and document remaining external or recurring work.
4. Validate every acceptance criterion against actual evidence, record design/implementation/operating conclusions separately, and hand off the next unblocked task with all blockers and residual risks. Do not mark this story accepted solely because prompts or templates were written.

#-----------------------------------------

### Story 39.18: Validate Integrated Readiness And Type I Decision Gate
**Status: Planned**

**Work type:** Independent internal readiness review and management decision  
**Proposed accountable role:** Qualified readiness reviewer and executive sponsor  
**Dependencies:** 39.4–39.17 accepted or explicitly approved N/A; 39.1–39.3 current  
**Type I / Type II relevance:** Type I readiness and direct-Type II design/implementation entry  
**Required deliverables:** readiness-review, gap-disposition, approved-system-description, route-decision

Prompt:

You are helping FeDril make a defensible evidence-backed decision without equating checklist completion with auditor approval. Execute Story 39.18, "Validate Integrated Readiness And Type I Decision Gate," using the Shared Execution Contract in this document and `AGENTS.md`. Read both before acting. Inspect existing implementation and evidence first; complete only the work authorized by this story and the user's execution request. Reuse verified controls and identify unavailable evidence, human approvals and external actions explicitly.

> Context:
>
> - Epic: SOC 2 Assurance Program
> - Parent traceability: New delivery story under Stories 39.1 and 39.2; defined in this consolidated execution backlog.
> - User story: As the accountable qualified readiness reviewer and executive sponsor, I want to make a defensible evidence-backed decision without equating checklist completion with auditor approval.
> - Acceptance criteria: A complete reviewed scope/control/task crosswalk and authorized gate decision exist. Proceed requires current defensible evidence and resolved gate blockers, not a percentage score; defer records exact next remediation.

Tasks (stable IDs are `SOC2-39.18-T01` onward):

- [ ] `SOC2-39.18-T01` — Reconcile every scoped criterion, risk, assessment recommendation and story task to the control matrix. Verify the actual system description, commitments, subservice treatment and management responsibilities against current evidence and Description Criteria.
- [ ] `SOC2-39.18-T02` — Inspect design and implementation separately for technical and human controls; use representative live exports, actual approvals, test records and completed initial operations. Revalidate evidence freshness and reliability.
- [ ] `SOC2-39.18-T03` — Review all material gaps, exceptions, founder-stage conflicts, exposure decisions, failed tests, policy acknowledgements, vendors, incident exercise and recovery results. Explicitly distinguish missing evidence from proven control failure.
- [ ] `SOC2-39.18-T04` — Confirm accountable owners, protected evidence index, assertion preparation, budget, business demand and auditor due diligence. Risk acceptance cannot manufacture evidence or force a favorable auditor conclusion.
- [ ] `SOC2-39.18-T05` — Record signed proceed, defer or revise-scope decision with reasons, unresolved risks, date, reviewer and next review. Block progression where material design/implementation or evidence deficiencies undermine the proposed scope.
- [ ] `SOC2-39.18-T06` — Select Type I-first, direct Type II, or continued internal readiness. An issued Type I report is optional unless FeDril's actual obligations require it; obtain service-auditor agreement on the selected route.

Instructions:

1. Record task-level owner, reviewer, due date, priority, prerequisites, criterion/control mapping, Type I/II impact and evidence/verification outcome in the shared register.
2. Produce or update the required deliverables in the approved protected location. Keep only sanitized references in Git; obtain human review for decisions and signatures.
3. Do not perform or simulate a CPA attestation. Coordinate only authorized evidence and documentation work; record actual auditor and management actions separately.
4. Validate every acceptance criterion against actual evidence, record design/implementation/operating conclusions separately, and hand off the next unblocked task with all blockers and residual risks. Do not mark this story accepted solely because prompts or templates were written.

#-----------------------------------------

### Story 39.19: Coordinate Optional Type I Examination And Report Acceptance
**Status: Planned**

**Work type:** Independent CPA examination coordination; not agent attestation  
**Proposed accountable role:** Executive sponsor, SOC 2 owner and independent CPA  
**Dependencies:** 39.18 proceed for Type I; 39.3 engagement approval  
**Type I / Type II relevance:** Type I only; may be explicitly deferred/skipped for direct Type II  
**Required deliverables:** type-I-engagement-record, request-tracker, assertion, issued-report-reference

Prompt:

You are helping FeDril coordinate a point-in-time examination and accurately govern its actual result when commercially justified. Execute Story 39.19, "Coordinate Optional Type I Examination And Report Acceptance," using the Shared Execution Contract in this document and `AGENTS.md`. Read both before acting. Inspect existing implementation and evidence first; complete only the work authorized by this story and the user's execution request. Reuse verified controls and identify unavailable evidence, human approvals and external actions explicitly.

> Context:
>
> - Epic: SOC 2 Assurance Program
> - Parent traceability: New delivery story under Stories 39.2 and 39.3; defined in this consolidated execution backlog.
> - User story: As the accountable executive sponsor, soc 2 owner and independent cpa, I want to coordinate a point-in-time examination and accurately govern its actual result when commercially justified.
> - Acceptance criteria: Either a governed non-selection/defer decision or an actual independently issued and reviewed Type I report reference exists. Internal drafts and readiness reviews never count as issuance.

Tasks (stable IDs are `SOC2-39.19-T01` onward):

- [ ] `SOC2-39.19-T01` — Confirm selected Type I route, stable scope, as-of date, licensed independent CPA, terms, fees and authorized engagement. If direct Type II is selected, record approved not-selected disposition; do not block 39.20 on a missing Type I report.
- [ ] `SOC2-39.19-T02` — Finalize management's system description and assertion through authorized management and CPA coordination; only management signs its representations.
- [ ] `SOC2-39.19-T03` — Provide approved evidence/populations through restricted channels, preserve request history and resolve factual questions without backdating controls or changing historical evidence.
- [ ] `SOC2-39.19-T04` — Track examiner findings, management responses and corrective actions. Do not represent an exception as closed without appropriate validation or attempt to dictate the opinion.
- [ ] `SOC2-39.19-T05` — Verify the actual issued report, type, scope, date, opinion and use restrictions with authorized reviewers. Store securely and activate only exact approved claims through 39.3/39.23.
- [ ] `SOC2-39.19-T06` — Carry unresolved findings and changed controls into Type II preparation; do not infer operating effectiveness from Type I issuance.

Instructions:

1. Record task-level owner, reviewer, due date, priority, prerequisites, criterion/control mapping, Type I/II impact and evidence/verification outcome in the shared register.
2. Produce or update the required deliverables in the approved protected location. Keep only sanitized references in Git; obtain human review for decisions and signatures.
3. Do not perform or simulate a CPA attestation. Coordinate only authorized evidence and documentation work; record actual auditor and management actions separately.
4. Validate every acceptance criterion against actual evidence, record design/implementation/operating conclusions separately, and hand off the next unblocked task with all blockers and residual risks. Do not mark this story accepted solely because prompts or templates were written.

#-----------------------------------------

### Story 39.20: Establish Type II Period Populations And Evidence Entry Gate
**Status: Planned**

**Work type:** Operating-readiness planning and CPA coordination  
**Proposed accountable role:** SOC 2 owner, control owners and service auditor  
**Dependencies:** 39.18 design/implementation gate; 39.19 disposition, not necessarily issuance; 39.2 calendar  
**Type I / Type II relevance:** Type II  
**Required deliverables:** type-II-period-plan, population-register, evidence-entry-gate

Prompt:

You are helping FeDril ensure every scoped control can be tested over the agreed period before relying on period evidence. Execute Story 39.20, "Establish Type II Period Populations And Evidence Entry Gate," using the Shared Execution Contract in this document and `AGENTS.md`. Read both before acting. Inspect existing implementation and evidence first; complete only the work authorized by this story and the user's execution request. Reuse verified controls and identify unavailable evidence, human approvals and external actions explicitly.

> Context:
>
> - Epic: SOC 2 Assurance Program
> - Parent traceability: New delivery story under Stories 39.2 and 39.3; defined in this consolidated execution backlog.
> - User story: As the accountable soc 2 owner, control owners and service auditor, I want to ensure every scoped control can be tested over the agreed period before relying on period evidence.
> - Acceptance criteria: All scoped controls have workable populations, ownership, cadence, secure evidence and failure handling; management and CPA period assumptions are recorded, with unresolved material entry blockers preventing reliance.

Tasks (stable IDs are `SOC2-39.20-T01` onward):

- [ ] `SOC2-39.20-T01` — Agree proposed start/end dates, scope, criteria and commitments with management and service auditor. Do not invent a mandatory duration, retroactively start an unsupported period, or assume all controls must have identical frequencies.
- [ ] `SOC2-39.20-T02` — For every control record approved operating cadence, evidence capture/review frequency, named operator/deputy/reviewer, population source, completeness check and failure escalation.
- [ ] `SOC2-39.20-T03` — Build populations for access changes/reviews, changes/deployments, vulnerabilities, incidents, backups/restores, vendors, training, risks and exceptions. Agree sampling with the CPA; do not restrict populations to successful events.
- [ ] `SOC2-39.20-T04` — Address annual/low-frequency controls, no-event periods, new controls and scope changes with the auditor; simulations prove rehearsal, not actual period operation. Define handling of missing or stale evidence.
- [ ] `SOC2-39.20-T05` — Test evidence capture/retrieval and review, scheduling, missed-deadline escalation and backup. Confirm staffing, independent oversight and data protection.
- [ ] `SOC2-39.20-T06` — Approve or defer period entry; record blockers, period version and control effective dates. Preserve evidence collected before entry and assess its relevance honestly.

Instructions:

1. Record task-level owner, reviewer, due date, priority, prerequisites, criterion/control mapping, Type I/II impact and evidence/verification outcome in the shared register.
2. Produce or update the required deliverables in the approved protected location. Keep only sanitized references in Git; obtain human review for decisions and signatures.
3. Apply the shared safety and change boundaries. Implement only verified, authorized gaps; preserve working controls and document remaining external or recurring work.
4. Validate every acceptance criterion against actual evidence, record design/implementation/operating conclusions separately, and hand off the next unblocked task with all blockers and residual risks. Do not mark this story accepted solely because prompts or templates were written.

#-----------------------------------------

### Story 39.21: Operate Type II Controls And Review Failures Throughout The Period
**Status: Planned**

**Work type:** Recurring human and automated control operation  
**Proposed accountable role:** Control owners, evidence reviewers and executive sponsor  
**Dependencies:** 39.20 entry approved; all operating controls placed in service  
**Type I / Type II relevance:** Type II  
**Required deliverables:** period-evidence-index, recurring-review-records, failure-register, management-review

Prompt:

You are helping FeDril demonstrate actual repeated control operation across the review period, not just policies or a one-time run. Execute Story 39.21, "Operate Type II Controls And Review Failures Throughout The Period," using the Shared Execution Contract in this document and `AGENTS.md`. Read both before acting. Inspect existing implementation and evidence first; complete only the work authorized by this story and the user's execution request. Reuse verified controls and identify unavailable evidence, human approvals and external actions explicitly.

> Context:
>
> - Epic: SOC 2 Assurance Program
> - Parent traceability: New delivery story under Stories 39.2 and 39.3; defined in this consolidated execution backlog.
> - User story: As the accountable control owners, evidence reviewers and executive sponsor, I want to demonstrate actual repeated control operation across the review period, not just policies or a one-time run.
> - Acceptance criteria: Dated, reviewed control evidence spans the actual agreed period and complete populations; missed operations and deviations have honest disposition. No future operation, review or approval is pre-completed.

Tasks (stable IDs are `SOC2-39.21-T01` onward):

- [ ] `SOC2-39.21-T01` — Execute per-event onboarding/movers/leavers, access grants, changes, emergency approvals, incident response, vendor onboarding, vulnerability triage and disposal controls; capture dated full-population evidence.
- [ ] `SOC2-39.21-T02` — Perform scheduled access/vendor/risk reviews, training, restoration/continuity and incident exercises, secret/key reviews and policy acknowledgements at approved cadences; retain actual decisions and follow-through.
- [ ] `SOC2-39.21-T03` — Monitor backups, telemetry, alerts, vulnerability scans and infrastructure drift; investigate missed/failed/skipped jobs, overdue remediation and missing reviewers.
- [ ] `SOC2-39.21-T04` — Review evidence quality and completeness monthly or at the approved risk-based cadence; reconcile populations to source systems and protect versions, timestamps, rejected items and corrections.
- [ ] `SOC2-39.21-T05` — Hold management reviews at the approved cadence (quarterly is the initial FeDril proposal), covering incidents, vulnerabilities, access, vendors, recovery, metrics, risks, exceptions and scope changes with action closure.
- [ ] `SOC2-39.21-T06` — Escalate failures immediately by policy, apply approved safeguards, remediate and retest without deleting failed samples. With the CPA, assess effect on scope, opinion, period or additional testing; do not automatically restart or hide the period.
- [ ] `SOC2-39.21-T07` — Maintain period-close reconciliation and evidence handoff to 39.22; run this story repeatedly until the agreed coverage is supported. A single agent run cannot mark elapsed-period obligations complete.

Instructions:

1. Record task-level owner, reviewer, due date, priority, prerequisites, criterion/control mapping, Type I/II impact and evidence/verification outcome in the shared register.
2. Produce or update the required deliverables in the approved protected location. Keep only sanitized references in Git; obtain human review for decisions and signatures.
3. Apply the shared safety and change boundaries. Implement only verified, authorized gaps; preserve working controls and document remaining external or recurring work.
4. Validate every acceptance criterion against actual evidence, record design/implementation/operating conclusions separately, and hand off the next unblocked task with all blockers and residual risks. Do not mark this story accepted solely because prompts or templates were written.

#-----------------------------------------

### Story 39.22: Coordinate Type II Examination And Resolve Report Findings
**Status: Planned**

**Work type:** Readiness gate and independent CPA examination coordination  
**Proposed accountable role:** Executive sponsor, SOC 2 owner and independent CPA  
**Dependencies:** 39.21 sufficient actual period evidence; 39.3 current engagement governance  
**Type I / Type II relevance:** Type II  
**Required deliverables:** type-II-readiness-decision, examiner-request-log, management-assertion, issued-report-reference

Prompt:

You are helping FeDril support independent evaluation of the scoped period and preserve accurate findings and report status. Execute Story 39.22, "Coordinate Type II Examination And Resolve Report Findings," using the Shared Execution Contract in this document and `AGENTS.md`. Read both before acting. Inspect existing implementation and evidence first; complete only the work authorized by this story and the user's execution request. Reuse verified controls and identify unavailable evidence, human approvals and external actions explicitly.

> Context:
>
> - Epic: SOC 2 Assurance Program
> - Parent traceability: New delivery story under Stories 39.2 and 39.3; defined in this consolidated execution backlog.
> - User story: As the accountable executive sponsor, soc 2 owner and independent cpa, I want to support independent evaluation of the scoped period and preserve accurate findings and report status.
> - Acceptance criteria: An actual reviewed independent Type II report is required for issuance status; otherwise record defer/in-progress truthfully. Neither internal acceptance nor remediation guarantees an unmodified opinion.

Tasks (stable IDs are `SOC2-39.22-T01` onward):

- [ ] `SOC2-39.22-T01` — Reconcile complete period populations, scoped criteria/control versions, evidence freshness, reviewers, significant changes, prior findings and all failed controls. Obtain a management proceed/defer decision; unresolved material evidence gaps require escalation to the CPA.
- [ ] `SOC2-39.22-T02` — Confirm the independent engagement, system description for the period, management assertion/representations, subservice disclosures, commitments and period dates. Management retains responsibility.
- [ ] `SOC2-39.22-T03` — Provide complete requested populations and auditor-selected samples through approved channels with response ownership and version history; protect restricted data and never substitute favorable samples silently.
- [ ] `SOC2-39.22-T04` — Track tests/questions, exceptions, management responses, remediation commitments and subsequent events. Correct factual report errors through the CPA without altering its independent judgment.
- [ ] `SOC2-39.22-T05` — Verify actual report issuance, opinion, covered system/categories/period, deviations and permitted audience. Store the governed report reference and obtain authorized review before report-specific claims or disclosure.
- [ ] `SOC2-39.22-T06` — Transfer findings and ongoing obligations to 39.23; maintain continuous controls during fieldwork and after issuance. If no report is issued, retain the true status and next actions.

Instructions:

1. Record task-level owner, reviewer, due date, priority, prerequisites, criterion/control mapping, Type I/II impact and evidence/verification outcome in the shared register.
2. Produce or update the required deliverables in the approved protected location. Keep only sanitized references in Git; obtain human review for decisions and signatures.
3. Do not perform or simulate a CPA attestation. Coordinate only authorized evidence and documentation work; record actual auditor and management actions separately.
4. Validate every acceptance criterion against actual evidence, record design/implementation/operating conclusions separately, and hand off the next unblocked task with all blockers and residual risks. Do not mark this story accepted solely because prompts or templates were written.

#-----------------------------------------

### Story 39.23: Maintain Report Distribution Continuous Readiness And Renewal
**Status: Planned**

**Work type:** Ongoing governance and control operations  
**Proposed accountable role:** Executive sponsor, SOC 2 owner and approved disclosure owner  
**Dependencies:** 39.3 policy; actual 39.19/39.22 report for disclosure; readiness controls continue without issuance  
**Type I / Type II relevance:** Both report types; continuous Type II  
**Required deliverables:** disclosure-log, claims-review, renewal-plan, continuing-evidence-calendar

Prompt:

You are helping FeDril keep procurement claims accurate and control evidence current after the initial examination. Execute Story 39.23, "Maintain Report Distribution Continuous Readiness And Renewal," using the Shared Execution Contract in this document and `AGENTS.md`. Read both before acting. Inspect existing implementation and evidence first; complete only the work authorized by this story and the user's execution request. Reuse verified controls and identify unavailable evidence, human approvals and external actions explicitly.

> Context:
>
> - Epic: SOC 2 Assurance Program
> - Parent traceability: New delivery story under Stories 39.2 and 39.3; defined in this consolidated execution backlog.
> - User story: As the accountable executive sponsor, soc 2 owner and approved disclosure owner, I want to keep procurement claims accurate and control evidence current after the initial examination.
> - Acceptance criteria: Disclosures and claims are governed, renewal/period ownership is approved and ongoing control evidence continues. SOC 2 is an operating program, not a permanently completed software feature.

Tasks (stable IDs are `SOC2-39.23-T01` onward):

- [ ] `SOC2-39.23-T01` — Approve each report disclosure according to actual restricted-use terms and intended audience; record recipient, purpose, confidentiality conditions, approver, version, sent date and access revocation/supersession. Do not publish the report to public Git or a public trust page.
- [ ] `SOC2-39.23-T02` — Verify claims against actual type, system, criteria, period and report outcome. Review questionnaires and trust content before publication; never use SOC 2 certified or imply government/CUI authorization.
- [ ] `SOC2-39.23-T03` — Maintain corrective actions, control operations and period evidence without a gap after issuance. Review risks, scope, vendors, access, training and policies at approved intervals and on significant change.
- [ ] `SOC2-39.23-T04` — Track evidence completeness, overdue reviews, control failures, vulnerability age/SLAs, recovery outcomes and repeat findings with actual denominators; avoid unsupported maturity percentages.
- [ ] `SOC2-39.23-T05` — Plan renewal with budget, reviewer/auditor independence, next period, control/commitment changes and prior findings. Annual examination is a common commercial expectation, not an invented universal report-expiry rule.
- [ ] `SOC2-39.23-T06` — Distinguish a management-issued bridge letter from independent CPA assurance; obtain review and disclose its limited nature when requested. Revoke obsolete access and supersede stale claims while retaining historical records.
- [ ] `SOC2-39.23-T07` — Return scope changes to 39.1/39.17, gaps to 39.2 and period planning to 39.20; record explicit proceed/defer decisions and business-demand review rather than declaring SOC 2 permanently complete.

Instructions:

1. Record task-level owner, reviewer, due date, priority, prerequisites, criterion/control mapping, Type I/II impact and evidence/verification outcome in the shared register.
2. Produce or update the required deliverables in the approved protected location. Keep only sanitized references in Git; obtain human review for decisions and signatures.
3. Apply the shared safety and change boundaries. Implement only verified, authorized gaps; preserve working controls and document remaining external or recurring work.
4. Validate every acceptance criterion against actual evidence, record design/implementation/operating conclusions separately, and hand off the next unblocked task with all blockers and residual risks. Do not mark this story accepted solely because prompts or templates were written.

#-----------------------------------------

### Assessment Recommendation Coverage

This table covers every checklist row and gap in the dated assessment; 39.2 must retain task-level links as execution proceeds. Additional scope-specific controls discovered by the current licensed criterion mapping must be added before the readiness gate. This is not a claim that a generic checklist substitutes for that mapping.

| Assessment rank | Recommendation | Delivery stories | Original gap linkage |
| --- | --- | --- | --- |
| 1 | Database dump exposure | 39.4 | SOC2-GAP-001 |
| 2 | Boundary and criteria | 39.1, 39.17 | SOC2-GAP-002 |
| 3 | Control matrix | 39.1, 39.18 | SOC2-GAP-002 |
| 4 | Control/evidence ownership | 39.2, 39.5 | SOC2-GAP-002, SOC2-GAP-015 |
| 5 | Enterprise risk assessment | 39.5 | SOC2-GAP-004 |
| 6 | Policy baseline | 39.5 | SOC2-GAP-003 |
| 7 | Examination decision | 39.3, 39.18–39.22 | SOC2-GAP-002 |
| 8 | Workforce MFA | 39.6 | SOC2-GAP-005 |
| 9 | Joiner/mover/leaver | 39.6, 39.21 | SOC2-GAP-005 |
| 10 | Periodic access review | 39.6, 39.21 | SOC2-GAP-005 |
| 11 | Privileged inventory | 39.6 | SOC2-GAP-005 |
| 12 | Separation of duties | 39.5, 39.6, 39.8 | SOC2-GAP-007 |
| 13 | Repository exposure boundary | 39.4 | SOC2-GAP-001 |
| 14 | Production networking | 39.9 | SOC2-GAP-008 |
| 15 | Secrets and keys | 39.10 | SOC2-GAP-008 |
| 16 | Drift detection | 39.9, 39.12, 39.21 | SOC2-GAP-008 |
| 17 | Vulnerability program | 39.11 | SOC2-GAP-009 |
| 18 | Security testing | 39.7, 39.11 | SOC2-GAP-009 |
| 19 | Incident response | 39.13 | SOC2-GAP-010 |
| 20 | Vendor/subprocessor governance | 39.15 | SOC2-GAP-006 |
| 21 | Evidence store/integrity | 39.2 | SOC2-GAP-015 |
| 22 | Logging and monitoring | 39.12 | SOC2-GAP-012 |
| 23 | Backup and recovery | 39.14 | SOC2-GAP-011 |
| 24 | Change management | 39.8 | SOC2-GAP-007 |
| 25 | Branch/environment protections | 39.8 | SOC2-GAP-007 |
| 26 | Training | 39.6, 39.21 | SOC2-GAP-014 |
| 27 | Retention/disposal | 39.16 | SOC2-GAP-013 |
| 28 | Business continuity | 39.14 | SOC2-GAP-011 |
| 29 | Shared/inherited responsibility | 39.1, 39.15 | SOC2-GAP-006 |
| 30 | Customer claims | 39.3, 39.23 | SOC2-GAP-002 |
| 31 | Evidence calendar | 39.2, 39.20, 39.21 | SOC2-GAP-015 |
| 32 | Failures and exceptions | 39.2, 39.21, 39.22 | SOC2-GAP-015 |
| 33 | Management review | 39.5, 39.21 | SOC2-GAP-003, SOC2-GAP-004 |
| 34 | Continuous metrics | 39.21, 39.23 | SOC2-GAP-015 |
| 35 | Availability decision | 39.17, 39.14 | Scope-dependent; no separate original gap |
| 36 | Confidentiality decision | 39.17, 39.16 | Scope-dependent; no separate original gap |

Additional explicit coverage beyond the original gaps: system-description completeness (39.1/39.18), workforce devices/physical safeguards/agreements (39.6), existing tenant/session/audit/No-CUI control validation (39.7), system-produced evidence completeness and sampling (39.2/39.20/39.22), conditional Processing Integrity/Privacy decisions (39.17), independent CPA due diligence and assertions (39.3/39.19/39.22), and restricted-use reporting/renewal (39.23).

### Recurring-Control Calendar Starter

These are FeDril planning defaults, not asserted universal SOC 2 frequencies. Owners must approve risk- and contract-based timing in 39.2, and the CPA must agree period/sampling expectations in 39.20. All rows require named owner/deputy/reviewer, due/review dates, complete population, evidence location, failure escalation and retention. Add any additional scoped controls.

| Operation | Proposed starting cadence | Minimum evidence and validation |
| --- | --- | --- |
| JML and privileged grants/revocation | Each event; review completion promptly per approved SLA | Complete request population, approver, actual timestamps, removal verification |
| User/service/privileged access review | Quarterly and significant change | Source population, independent decisions, removal/retest evidence |
| Changes and emergency changes | Each change | Request, risk, independent approval, checks, release/rollback and post-review |
| Vulnerabilities and patching | CI per change; scheduled scans at approved risk cadence; SLA-driven remediation | Asset coverage, executed scans, failures, finding age, tickets and rescans |
| Telemetry/alerts | Continuous collection; response/review per severity and coverage model | Ingestion health, alerts, actual acknowledgement, escalation and closure |
| Infrastructure drift | Scheduled and after relevant changes | Executed result, skipped/failed-run alerts, reviewer and dispositions |
| Backups | Each scheduled job; daily health review proposed | Success/failure population, alert response, coverage and integrity evidence |
| Restore and continuity exercises | Quarterly restore proposal; continuity at least annually proposed and on material change | Approved objectives, representative scope, measured results, corrective retests |
| Incident response | Per incident; annual tabletop proposed and on material change | Actual incidents or labeled simulations, decisions, communications and closure |
| Vendor assurance review | Before onboarding; annual material-vendor review proposed; on significant change | Vendor population, current reports/contracts, exceptions and follow-up |
| Risk and policy review | Annual proposed and on significant change | Approved register, policy versions, residual risks, acknowledgements |
| Training and workforce agreements | Onboarding; annual refresher proposed; role/change triggers | Actual completion/acknowledgement population and overdue escalation |
| Secret/key/certificate lifecycle | Per approved expiry/rotation schedule; compromise immediately escalated | Inventory, access review, expiry alerts, authorized rotation/revocation tests |
| Data retention/disposal | Scheduled by record class; legal-hold/event triggers | Authorized disposition, lifecycle test, hold checks and vendor follow-up |
| Evidence quality | Monthly proposed; before each gate | Completeness/accuracy/freshness checks, rejected artifacts and correction |
| Management review | Quarterly proposed and material incidents/changes | Metrics, decisions, owners, due dates, risk acceptance and action closure |
| Security claims/report disclosure | Before every publication/disclosure; periodic revalidation | Approved exact wording, actual report scope/period, access and disclosure log |
| Renewal planning | Before next agreed reporting period; annual commercial review proposed | Approved next scope/period, funding, previous findings and continuous evidence |

### Program Sources And Interpretation

Framework references reviewed 2026-09-19:

- AICPA & CIMA, [System and Organization Controls: SOC Suite of Services](https://www.aicpa-cima.com/resources/landing/system-and-organization-controls-soc-suite-of-services): independent CPA assurance context and due-diligence concerns.
- AICPA & CIMA, [2017 Trust Services Criteria (With Revised Points of Focus – 2022)](https://www.aicpa-cima.com/resources/download/2017-trust-services-criteria-with-revised-points-of-focus-2022): obtain current authorized criteria for scoped mapping; the full gated criteria were not reviewed in this backlog update.
- AICPA & CIMA, [2018 SOC 2 Description Criteria (With Revised Implementation Guidance – 2022)](https://www.aicpa-cima.com/resources/download/get-description-criteria-for-your-organizations-soc-2-r-report): obtain current authorized description criteria for the system description; gated full text was not reviewed.
- PBMares (CPA firm; not identified as FeDril's selected auditor), [SOC 2 Reports – Frequently Asked Questions](https://www.pbmares.com/soc-2-reports-frequently-asked-questions/): explanatory guidance on point-in-time versus period reports, Security baseline, report restrictions and common timing. Not a substitute for the selected CPA's terms or AICPA standards.
- FeDril, [SOC 2 readiness assessment](soc2-readiness-assessment.md), dated 2026-09-19, reviewing main commit `6e38c20392c806c9c1355ea94f63954b0bdfca6e`: historical findings and recommendation IDs.
- FeDril, [source use cases](development-phase-use-cases.md), Stories 39.1–39.3, and [project instructions](../AGENTS.md): preserved governance purpose and security/change invariants.

The 23-story decomposition, proposed cadences, ordering and safeguards are implementation recommendations based on FeDril's assessment, not verbatim AICPA requirements. Validate current criteria, scope, control sufficiency and examination procedures with a qualified independent CPA. Verify legal/contract obligations with their owners. “Correct solution” means tested, risk-appropriate remediation with evidence and residual-risk disclosure, not a guaranteed report.

#-----------------------------------------

### SOC 2 Readiness Assessment Companion Prompt
**Status: Ready For Execution**

This companion prompt is not a new numbered user story. It refreshes the current-state assessment for parent Stories 39.1–39.3 and delivery Stories 39.4–39.23. Use the sequential stories above for implementation. Assessment is read-only with respect to application and infrastructure changes; its authorized output is a dated report. Record completed assessment runs in the report history; a reusable prompt's status is not a control-completion claim.

Prompt:

Act as a senior SOC 2 readiness advisor, security architect, and evidence reviewer. Perform an evidence-based SOC 2 readiness assessment for the FeDril application and produce a prioritized implementation checklist that is complete for the proposed FeDril scope and the evidence available at the assessment date.

Repository:

`/Users/devups/Development/CodexProjects/Gccs`

Existing SOC 2 story prompts:

`/Users/devups/Development/CodexProjects/Gccs/docs/development-story-prompts.md`

Relevant stories:

- Story 39.1: Define SOC 2 Scope And Readiness Decision
- Story 39.2: Remediate Gaps And Collect Operating Evidence
- Story 39.3: Govern Independent Examination And Report Distribution

## Execution boundaries and assessment record

This is a read-only assessment and documentation task. Do not implement controls, change application behavior, modify infrastructure, alter policies, or start an examination unless separately instructed and approved. Include the consolidated Stories 39.1–39.23 in coverage checks. Use the Shared Execution Contract's dimension-specific conclusions and protected-evidence rules; the statuses below are evidence labels, not interchangeable proof of design, implementation and operation.

At the beginning of the assessment, record:

- Assessment date and time zone
- Repository, branch, and exact commit SHA reviewed
- Environments and external systems for which evidence was available
- Evidence cutoff date
- Reviewer identity or role
- Evidence sources that were unavailable
- Material limitations caused by missing live configuration, cloud-console access, personnel records, contracts, vendor records, or auditor guidance

Write the final assessment to `docs/soc2-readiness-assessment.md` unless a different output path is explicitly provided. Do not overwrite historical assessment evidence without preserving its assessment date and reviewed commit.

## FeDril context

FeDril is currently positioned as a No-CUI compliance-readiness operations platform. SOC 2 is intended to establish commercial trust in the FeDril SaaS environment; it does not replace CMMC readiness, FedRAMP authorization, NIST SP 800-171 responsibilities, government approval, or authorization to store or process CUI.

Security is the required baseline Trust Services Criteria category for the candidate SOC 2 scope. Evaluate Availability and Confidentiality separately and recommend adding either category only when supported by verified customer commitments, contractual requirements, data sensitivity, or system behavior. Do not automatically scope Processing Integrity or Privacy, and do not scope all five categories by default.

No security or compliance program can be literally “fail-proof.” Interpret that request as designing the most defensible, sustainable, repeatable, evidence-backed SOC 2 program practicable for FeDril, with explicit residual risks and limitations.

## Required investigation

Before making recommendations, inspect available evidence across the repository, including:

- Application architecture and system-boundary documentation
- Backend, frontend, APIs, databases, storage, and background services
- Authentication, authorization, RBAC, session management, and privileged access
- Tenant isolation and data-access enforcement
- Audit logging, security logging, monitoring, and alerting
- CI/CD pipelines and deployment configuration
- Infrastructure-as-code and cloud-hosting configuration
- Secrets management and encryption configuration
- Secure development and code-review practices
- Automated testing and security testing
- Dependency, container, and vulnerability scanning
- Change-management records and approval controls
- Backup, restoration, resilience, and disaster-recovery evidence
- Incident-response documentation and testing
- Vendor and subprocessor records
- Risk assessments and risk treatment records
- Access-review and employee lifecycle procedures
- Security policies and operational procedures
- Evidence-retention and evidence-protection practices
- Existing SOC 2 scope, gap, evidence, examination, claims, or readiness documents
- Stories 39.1 through 39.3 and their underlying use cases
- Repository visibility, branch protection, required reviews, environment approvals, and segregation-of-duties configuration
- Whether public repository content exposes operational details, internal evidence, infrastructure names, security assumptions, or other information requiring risk acceptance or removal

Do not treat the existence of source code, a policy, a test, a UI screen, or a backlog story as proof that a control operates effectively.

For every control, evaluate three separate dimensions:

1. `Design`: whether the control is suitably designed for the proposed scope.
2. `Implementation`: whether the control has been placed in operation as of the assessment date.
3. `Operating Evidence`: whether dated evidence demonstrates that the control operated consistently for the relevant period.

Do not collapse these dimensions into one readiness status. For each dimension, use one of the following evidence statuses:

- `Verified Implemented`
- `Partially Implemented`
- `Documented But Not Verified`
- `Planned Only`
- `Missing`
- `Not Applicable`
- `Requires Verification`

Use `Verified Implemented` only when repository or operational evidence supports the conclusion. Identify the exact evidence using file paths, configuration names, tests, workflows, or governed records. Do not expose credentials, secrets, private keys, sensitive vulnerability details, customer data, or CUI.

For every evidence item, record its date or period, source, owner or custodian when known, environment, reviewer when known, and freshness. Mark evidence as stale when it no longer supports the current control design, environment, or examination period.

Use this evidence-strength order:

1. Independently verified live configuration or execution evidence
2. Dated operating records tied to the reviewed system and environment
3. Automated tests and CI/CD execution records
4. Current source code and infrastructure-as-code
5. Approved policies, procedures, and governance records
6. Draft documents, backlog stories, intended designs, or unsupported assertions

A stronger source can support but does not automatically replace a different required evidence type. For example, passing source-code tests do not prove that a quarterly access review occurred.

## Deliverables

### 1. Executive readiness assessment

Provide:

- Overall SOC 2 readiness level
- Recommended initial scope
- Recommended Trust Services Criteria categories
- Major readiness blockers
- Whether FeDril should proceed toward Type I, remain in remediation, or defer examination
- Whether customer, partner, or procurement demand currently justifies formal examination
- The five most important actions FeDril should take next
- Assumptions, limitations, and items requiring verification

Do not assign an unsupported percentage score. If you provide a maturity rating, define its scoring method.

### 2. Current FeDril control inventory

Identify all SOC 2-aligned controls that currently exist in FeDril.

Use a table containing:

| Priority | Control area | Existing control | Design status | Implementation status | Operating-evidence status | Control type | Evidence | Evidence date/period | Evidence limitation | Control owner | Type I relevance | Type II relevance | Recommended action |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |

Separate controls implemented in application code from organizational or operational controls. Clearly identify controls that require human operation, management review, recurring evidence, or external service-provider evidence.

Do not infer implementation solely from story text or intended design.

### 3. Prioritized SOC 2 readiness checklist

Produce a complete checklist for FeDril’s proposed scope, ordered from most important to least important.

Use the following priority model:

- `P0 — Scope or examination blocker`
- `P1 — Critical security or governance foundation`
- `P2 — Required control implementation or material remediation`
- `P3 — Operating-evidence and consistency improvement`
- `P4 — Optimization or future-scope enhancement`

Within each priority, order items by dependency, risk reduction, audit significance, and implementation urgency.

For every checklist item provide:

| Rank | Priority | Checklist item | Why it matters | Design status | Implementation status | Operating-evidence status | Required action | Accountable owner | Dependency | Required evidence | Type I, Type II, or both | Suggested timing | Completion criterion |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |

At minimum, evaluate:

- Governance and management oversight
- System description and system boundary
- Risk assessment and risk treatment
- Control ownership
- Security policies and standards
- Workforce onboarding and termination
- Authentication and MFA
- Authorization and least privilege
- Privileged access
- Periodic access reviews
- Tenant isolation
- Change management
- Pull-request and deployment approval
- Secure SDLC
- Vulnerability management
- Dependency and container security
- Patch management
- Penetration testing
- Security event logging
- Monitoring and alert response
- Audit-log integrity and retention
- Incident response
- Incident exercises and lessons learned
- Backup and restoration
- Business continuity and disaster recovery
- Encryption and key management
- Secrets management
- Data classification and retention
- Vendor and subprocessor management
- Vendor monitoring
- Physical and environmental controls inherited from hosting providers
- Availability commitments, if scoped
- Confidentiality commitments, if scoped
- Exception and risk-acceptance management
- Evidence collection, review, retention, and protection
- Control-failure remediation
- Management review
- Customer-facing security claims
- Report access and distribution
- Annual renewal and continuous readiness
- Repository visibility and protection
- Security architecture and production-configuration review
- Separation of duties and independent approval

### 4. Type I readiness requirements

Explain that a SOC 2 Type I report addresses control design and implementation as of a specified date.

Provide a Type I readiness table containing:

- Requirement
- Current FeDril status
- Missing evidence
- Required remediation
- Responsible owner
- Readiness gate
- Dependency
- Recommended completion order

Identify the minimum conditions FeDril should satisfy before engaging an independent CPA firm for a Type I examination. Separate early scoping/readiness consultation from examination commitment. Evaluate Type I-first versus direct Type II with the service auditor; an issued Type I report is not an automatic prerequisite for Type II.

Treat code, tests, infrastructure-as-code, and draft policies as control-design or implementation evidence only unless dated operating records establish actual execution and review.

### 5. Type II readiness requirements

Explain that a SOC 2 Type II report also evaluates operating effectiveness over a defined review period.

Provide a Type II readiness table containing:

- Control
- Required operating cadence
- Expected evidence
- Evidence owner
- Reviewer
- Proposed collection frequency
- Minimum practical observation needs
- Current evidence maturity
- Failure or exception handling
- Readiness gate

Do not invent a mandatory examination-period length. Distinguish common practice from formal requirements and recommend confirming the period with the selected service auditor.

Explicitly identify controls that must operate repeatedly, such as:

- Access reviews
- Joiner, mover, and leaver processing
- Change approvals
- Vulnerability scanning and remediation
- Incident monitoring and response
- Backup monitoring and restoration tests
- Vendor reviews
- Risk assessments
- Security training
- Management review
- Exception handling
- Evidence quality review

### 6. Gap and remediation plan

For every material gap provide:

- Gap identifier
- Related control area
- Severity
- Risk
- Root cause, if determinable
- Remediation action
- Interim safeguard
- Owner
- Target date recommendation
- Required evidence
- Type I impact
- Type II impact
- Closure and validation criteria
- Residual risk

Highlight critical-path dependencies and identify which gaps can be remediated in parallel.

Do not call any remediation “fail-proof.” Explain the remaining residual risk, manual dependency, third-party dependency, and failure-detection mechanism for every high-severity recommendation.

### 7. Sustainable SOC 2 operating model

Recommend a durable operating model covering:

- Named control owners
- Evidence owners and reviewers
- Evidence calendar
- Automated versus manual controls
- Evidence retention and secure storage
- Exception escalation
- Control-failure detection
- Corrective-action tracking
- Quarterly management review
- Annual scope review
- Significant-change assessment
- Vendor monitoring
- Security claims approval
- Independent examination preparation
- Continuous readiness metrics

Prefer lightweight controls appropriate for FeDril’s maturity and customer demand. Avoid unnecessary process, tooling, or audit expense that does not materially reduce risk or improve evidence reliability.

For each recommendation, state whether it should be implemented:

- Now
- Before Type I
- During the Type II observation period
- Before Type II examination completion
- Only when customer or contractual demand justifies it

### 8. Review of Stories 39.1–39.3

Review the existing prompts for Stories 39.1 through 39.3.

For each story, report:

- What the prompt already covers well
- Missing or ambiguous requirements
- Duplicated or conflicting requirements
- Risks created by the current wording
- Whether modification is needed
- Why the modification is or is not needed
- Exact proposed replacement or additional wording

Do not edit the story prompt file unless explicitly instructed. Provide diff-ready recommendations grouped by story. Check delivery Stories 39.4–39.23 for coverage before recommending duplicate work, and distinguish new requirements from tasks already present.

Specifically determine whether the stories adequately require:

- A current-state control inventory
- A prioritized readiness checklist
- Separate control-design, implementation, and operating-effectiveness conclusions
- Type I and Type II readiness gates
- Evidence-quality standards
- Repeated-control evidence cadences
- Control-owner accountability
- Exception and control-failure handling
- Auditor-engagement trigger criteria
- Residual-risk documentation
- Conservative customer-facing claims

### 9. Final recommended roadmap

Provide a phased roadmap:

1. Scope and governance
2. Critical remediation
3. Type I readiness
4. Optional Type I examination decision, or documented direct-Type II route
5. Type II operating period
6. Type II examination decision
7. Continuous readiness and renewal

For each phase identify objectives, dependencies, exit criteria, likely evidence, accountable roles, and the risks of proceeding prematurely.

Conclude with:

- Recommended immediate decision
- Next 30-day actions
- Next 60–90-day actions
- Conditions that should trigger engaging a CPA firm
- Conditions that should trigger deferring the examination
- The three changes, if any, that should be made first to Stories 39.1–39.3

## Evidence and source rules

Use current authoritative sources, prioritizing:

1. AICPA SOC and Trust Services Criteria materials
2. Guidance from the independent CPA firm selected by FeDril
3. Verified FeDril repository and operational evidence
4. Relevant service-organization and cloud-provider assurance reports
5. Reputable secondary implementation guidance only when needed

Cite authoritative sources with title, publisher, URL, and access date. Clearly label professional judgment, common practice, and anything requiring confirmation by a CPA, attorney, customer, or contract owner.

The checklist is complete only for the proposed scope and evidence available at the assessment date. It is not a substitute for the current licensed Trust Services Criteria, the service auditor's scoping judgment, legal advice, or examination procedures.

Do not reproduce proprietary Trust Services Criteria text. Map controls at an appropriate summary level and recommend obtaining licensed/current criteria through the proper channel.

Do not claim or imply that FeDril is:

- SOC 2 certified
- SOC 2 compliant
- Audit ready
- Government approved
- CMMC certified
- FedRAMP authorized
- Authorized to store or process CUI

unless the exact claim is supported by verified, governed evidence and appropriately reviewed.

End the report with an evidence index and a limitations register. Clearly state that repository inspection alone cannot establish SOC 2 readiness or Type II operating effectiveness.

#-----------------------------------------

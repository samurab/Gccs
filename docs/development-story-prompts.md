# Development Story Implementation Prompts

These prompts are designed to be copied into a fresh implementation thread, one story at a time. Each prompt points back to the source backlog in [development-phase-use-cases.md](development-phase-use-cases.md) and should be executed under the project guidance in [../AGENTS.md](../AGENTS.md).

## Shared Prompt Requirements

Use these requirements for every story:

- Treat the MVP as **CUI-ready by design with gated CUI acceptance**.
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
## Done ##
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

- Treat the MVP as **CUI-ready by design with gated CUI acceptance**.

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
> - Documentation points to the CUI-ready gated MVP posture.
>
> Implement Story 1.1, "Repository And Project Structure," from `docs/development-phase-use-cases.md`. Confirm and improve the solution organization across `apps/api`, `apps/web`, `src/Gccs.Domain`, `src/Gccs.Application`, `src/Gccs.Infrastructure`, `packages/compliance-content`, `docs`, and `infra`. Update documentation so a new developer understands ownership boundaries, local setup, build commands, and the CUI-ready gated MVP posture. Verify the solution builds cleanly and ensure compliance workflow logic is not trapped only in the UI.

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
## Done ##
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

- Treat the MVP as **CUI-ready by design with gated CUI acceptance**.

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
## Done ##
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

- Treat the MVP as **CUI-ready by design with gated CUI acceptance**.

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
## Done ##
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

- Treat the MVP as **CUI-ready by design with gated CUI acceptance**.

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
## Done ##
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

- Treat the MVP as **CUI-ready by design with gated CUI acceptance**.

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
## Done ##
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

- Treat the MVP as **CUI-ready by design with gated CUI acceptance**.

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
## Done ##
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

- Treat the MVP as **CUI-ready by design with gated CUI acceptance**.

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
## Done ##
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

- Treat the MVP as **CUI-ready by design with gated CUI acceptance**.

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
## Done ##
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

- Treat the MVP as **CUI-ready by design with gated CUI acceptance**.

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
## Done ##

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

- Treat the MVP as **CUI-ready by design with gated CUI acceptance**.

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
> - Notice copy states that the MVP supports CUI-ready workflows with gated CUI acceptance and that real CUI upload requires approved CUI-ready tenant status.
>
> Implement Story 4.1, "Data Handling Acknowledgement," from `docs/development-phase-use-cases.md`. Add data handling notice content to onboarding and upload flows, require acknowledgement before upload, store acknowledgement by user/tenant/timestamp/notice version, expose acknowledgement status, and audit the acknowledgement. The copy must clearly state the MVP supports CUI-ready workflows with gated CUI acceptance and that real CUI upload requires approved CUI-ready tenant status.

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
## Done ##

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

- Treat the MVP as **CUI-ready by design with gated CUI acceptance**.

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
## Done ##

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

- Treat the MVP as **CUI-ready by design with gated CUI acceptance**.

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
## Done ##

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

- Treat the MVP as **CUI-ready by design with gated CUI acceptance**.

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
## Done ##

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

- Treat the MVP as **CUI-ready by design with gated CUI acceptance**.

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
## Done ##

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

- Treat the MVP as **CUI-ready by design with gated CUI acceptance**.

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
## Done ##

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

- Treat the MVP as **CUI-ready by design with gated CUI acceptance**.

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
## Done ##

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

- Treat the MVP as **CUI-ready by design with gated CUI acceptance**.

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
## Done ##

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

- Treat the MVP as **CUI-ready by design with gated CUI acceptance**.

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
### Done ##

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

- Treat the MVP as **CUI-ready by design with gated CUI acceptance**.

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
### Done ##

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

- Treat the MVP as **CUI-ready by design with gated CUI acceptance**.

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
### Done ##

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

- Treat the MVP as **CUI-ready by design with gated CUI acceptance**.

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
### Done ##

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

- Treat the MVP as **CUI-ready by design with gated CUI acceptance**.

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
### Done ##

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
### Done ##

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

- Treat the MVP as **CUI-ready by design with gated CUI acceptance**.

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
### Done ##

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

- Treat the MVP as **CUI-ready by design with gated CUI acceptance**.

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
### Done ##

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

- Treat the MVP as **CUI-ready by design with gated CUI acceptance**.

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
### Done ##

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

- Treat the MVP as **CUI-ready by design with gated CUI acceptance**.

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
### Done ##

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

- Treat the MVP as **CUI-ready by design with gated CUI acceptance**.

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
### Done ##

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

- Treat the MVP as **CUI-ready by design with gated CUI acceptance**.

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
### Done ##

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

- Treat the MVP as **CUI-ready by design with gated CUI acceptance**.

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
### Done ##

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

- Treat the MVP as **CUI-ready by design with gated CUI acceptance**.

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
### Done ##

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

- Treat the MVP as **CUI-ready by design with gated CUI acceptance**.

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
### Done ##

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

- Treat the MVP as **CUI-ready by design with gated CUI acceptance**.

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
### Done ##

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

- Treat the MVP as **CUI-ready by design with gated CUI acceptance**.

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
### Done ##

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

- Treat the MVP as **CUI-ready by design with gated CUI acceptance**.

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
### Done ##

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

- Treat the MVP as **CUI-ready by design with gated CUI acceptance**.

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
### Done ##

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

- Treat the MVP as **CUI-ready by design with gated CUI acceptance**.

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
### Done ##

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

- Treat the MVP as **CUI-ready by design with gated CUI acceptance**.

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
### Done ##

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

- Treat the MVP as **CUI-ready by design with gated CUI acceptance**.

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
### Done ##

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

- Treat the MVP as **CUI-ready by design with gated CUI acceptance**.

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
### Done ##

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
### Done ##

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

- Treat the MVP as **CUI-ready by design with gated CUI acceptance**.

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
### Done ##

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

- Treat the MVP as **CUI-ready by design with gated CUI acceptance**.

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
### Done ##

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

- Treat the MVP as **CUI-ready by design with gated CUI acceptance**.

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
### Done ##

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

- Treat the MVP as **CUI-ready by design with gated CUI acceptance**.

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
### Done ##

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

- Treat the MVP as **CUI-ready by design with gated CUI acceptance**.

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
### Done ##

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

- Treat the MVP as **CUI-ready by design with gated CUI acceptance**.

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
### Done ##

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

- Treat the MVP as **CUI-ready by design with gated CUI acceptance**.

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
### Done ##

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

- Treat the MVP as **CUI-ready by design with gated CUI acceptance**.

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
### Done ##

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

- Treat the MVP as **CUI-ready by design with gated CUI acceptance**.

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
### Done ##

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

- Treat the MVP as **CUI-ready by design with gated CUI acceptance**.

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
## Done ##

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

- Treat the MVP as **CUI-ready by design with gated CUI acceptance**.

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
### Done ##

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

- Treat the MVP as **CUI-ready by design with gated CUI acceptance**.

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
## Done ##
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

Implement Story 18.1, "Extraction Job Intake," from `docs/development-phase-use-cases.md`. Add the extraction job model, API endpoint, queue/background worker stub, contract document UI action, and audit events for job creation, completion, and failure. Preserve tenant isolation, RBAC, validation, audit logging, and the CUI-ready gated MVP posture. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------

### Story 18.2: Text Extraction And Clause Candidate Detection
## Done ##
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
## Done ##
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
## Done ##
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
## Done ##
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
## Done ##
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
## Done ##
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
## Done ##
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
## Done ##
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
## Done ##
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
## Done ##
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
## Done ##
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
## Done ##
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
## Done ##
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
## Done ##
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
## Done ##
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
## Done ##
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
## Done ##
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
## Done ##
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
## Done ##
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
## Done ##
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
## Done ##
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
## Done ##
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
## Done ##
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
## Done ##
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
## Done ##
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
## Done ##
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
## Done ##
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
## Done ##
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
## Done ##
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
## Done ##
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
## Done ##
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
## Done ##
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
## Done ##
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
## Done ##
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
## Done ##
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
> - `CuiReady` tenants can use CUI workflows only when required classification and approval checks pass.
> - Direct API calls receive the same mode restrictions as UI actions.
> - Mode enforcement failures return a clear error and create an audit event.

Implement Story 1A.1.2, "Mode-Based Workflow Enforcement," from `docs/development-phase-use-cases.md`. Add centralized server-side mode enforcement across contract intake, evidence, notes, reports, and extraction jobs, with matching UI restrictions, standard errors, and audit events. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, review metadata, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------

## 1A.2 Data Classification Controls
### Story 1A.2.1: Classification Metadata Schema
## Done ##
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
## Done ##
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
## Done ##
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
## Done ##
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 1A CUI readiness gate story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: Synthetic CUI Demo Dataset
> - User story: As a customer success lead, I want demo tenants to be seeded with synthetic CUI workflows so that onboarding and training can show end-to-end behavior safely.
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
## Done ##
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 1A CUI readiness gate story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: CUI-Ready Tenant Approval Checklist
> - User story: As a platform admin, I want a CUI-ready approval checklist so that required readiness evidence is captured before enabling CUI workflows.
> - Acceptance criteria:
> - Checklist cannot be approved while required items are incomplete.
> - Each completed item records owner, reviewer, review date, and supporting note or evidence link.
> - Rejected checklists include rejection reason and remain linked to the tenant.
> - Approved checklist ID is required for a tenant mode change to `CuiReady`.
> - Checklist changes are audit logged.

Implement Story 1A.4.1, "Approval Checklist Model," from `docs/development-phase-use-cases.md`. Add the CUI-ready approval checklist model, states, item metadata, tenant linkage, API/UI workflows, and audit logging. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, review metadata, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------
### Story 1A.4.2: Approval Gate Enforcement
## Done ##
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
## Done ##
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
## Done ##
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 1A CUI readiness gate story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: Shared Responsibility Matrix Baseline
> - User story: As a tenant admin, I want to acknowledge the shared responsibility matrix so that CUI-ready operation has a recorded customer acceptance.
> - Acceptance criteria:
> - Tenant admin can view and acknowledge the current published matrix.
> - CUI-ready approval is blocked if the tenant has not acknowledged the current matrix version.
> - Acknowledgement history records version, user, timestamp, and tenant.
> - New matrix version marks prior acknowledgement as outdated for future approvals.
> - Matrix acknowledgement is audit logged.

Implement Story 1A.5.2, "Tenant Matrix Acknowledgement," from `docs/development-phase-use-cases.md`. Add tenant admin matrix acknowledgement, version history, current-version approval gate enforcement, change notifications, and audit logging. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, review metadata, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------

## 1A.6 Customer-Facing Data Handling Notices
### Story 1A.6.1: Versioned Notice Content
## Done ##
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
## Done ##
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
## Done ##
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
## Done ##
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
## Done ##
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
## Done ##
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
## Done ##
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
> - High or critical open findings block CUI-ready approval.
> - Accepted risks include approver, date, scope, expiration or review date, and mitigation note.
> - Security review changes are audit logged.

Implement Story 1A.9.1, "Security Review Checklist," from `docs/development-phase-use-cases.md`. Add the Phase 1A security review checklist, finding tracking, accepted risk metadata, CUI approval linkage, open finding reporting, and audit events. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, review metadata, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------
### Story 1A.9.2: Technical Control Verification
## Done ##
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
## Done ##
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 1A CUI readiness gate story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: Security Readiness Review
> - User story: As a security owner, I want incident response readiness checked before CUI workflows are enabled so that accidental CUI upload or data handling incidents can be handled immediately.
> - Acceptance criteria:
> - Required incident playbooks exist before `CuiReady` approval.
> - Each playbook identifies trigger, containment steps, notification path, evidence to collect, owner, and closure criteria.
> - Readiness review records tabletop date, participants, findings, and follow-up actions.
> - Open critical incident response gaps block CUI-ready approval.
> - Incident readiness approval is audit logged or source-control traceable.

Implement Story 1A.9.3, "Incident Response Readiness," from `docs/development-phase-use-cases.md`. Add incident response readiness playbooks, escalation owner records, tabletop checklist/evidence capture, approval linkage, reminders, and traceability. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, review metadata, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------

## Phase 3 - Advanced Compliance

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

## 31. eSRS Support
### Story 31.1: eSRS Applicability And Reporting Calendar
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 3 Advanced Compliance story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: eSRS Support
> - User story: As a contracts manager, I want to identify contracts with eSRS reporting obligations so that required reports appear on the compliance calendar.
> - Acceptance criteria:
> - Authorized user can mark a contract as eSRS-applicable with report type, period, due date, and source.
> - eSRS report obligations appear on the compliance calendar.
> - Missing source clause or rationale blocks activation of an eSRS obligation.
> - Overdue eSRS tasks are calculated from due date and status.
> - eSRS applicability changes are audit logged.

Implement Story 31.1, "eSRS Applicability And Reporting Calendar," from `docs/development-phase-use-cases.md`. Add eSRS applicability fields, source-backed activation validation, reporting period and due-date tracking, calendar/task integration, default ISR/SSR schedule support where applicable, overdue behavior, reminders, and audit logging. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, source traceability, review metadata, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------
### Story 31.2: Subcontracting Report Data Collection
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 3 Advanced Compliance story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: eSRS Support
> - User story: As a contracts manager, I want to collect subcontracting report data so that eSRS package preparation uses documented subcontractor and spend information.
> - Acceptance criteria:
> - User can create report data rows linked to subcontractor and contract records.
> - Validation rejects negative amounts, missing required categories, duplicate rows, and period mismatches.
> - Report data rows link to supporting evidence when provided.
> - Data rows cannot be included in a final package until reviewed or explicitly marked as accepted.
> - Data row changes are audit logged.

Implement Story 31.2, "Subcontracting Report Data Collection," from `docs/development-phase-use-cases.md`. Add eSRS report data rows linked to contracts, subcontractors, spend/category data, periods, plans, evidence, review states, import template support, validation for bad or duplicate data, package-inclusion gating, and audit logging. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, source traceability, review metadata, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

#-----------------------------------------
### Story 31.3: eSRS Report Package
Prompt:
You are helping me build a Government Contractor Compliance SaaS application.

First, inspect the existing codebase, architecture docs, API contracts, schema/migrations, tests, and `docs/development-phase-use-cases.md`. Then summarize the current implementation state for this Phase 3 Advanced Compliance story and propose a small implementation plan before editing files.

> Context:
>
> - Epic: eSRS Support
> - User story: As a contracts manager, I want to prepare an eSRS report package so that internal reviewers can verify data before external submission.
> - Acceptance criteria:
> - Authorized user can generate an eSRS preparation package for the current tenant.
> - Package includes contract, period, report type, subcontractor/spend summaries, exceptions, evidence references, and generated date.
> - Package states that GCCS has not submitted the report to eSRS.
> - Approved packages include reviewer and approval date.
> - Package generation and approval are audit logged.

Implement Story 31.3, "eSRS Report Package," from `docs/development-phase-use-cases.md`. Add eSRS preparation package generation, report metadata, subcontractor/spend summaries, exceptions, evidence references, preparation-only/not-submitted language, review workflow, package version/history, permissions, export behavior, and audit logging. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, source traceability, review metadata, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

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

Implement Story 38.1, "CUI Enclave Boundary Model," from `docs/development-phase-use-cases.md`. Add enclave records with tenant, environment, boundary description, data handling mode, approved workflows, storage location, compute boundary, network restrictions, logging destination, backup policy, and support access model, enclave approval to CUI-ready tenant approval, security review checklist, incident readiness, and shared responsibility matrix acknowledgement, status workflow for draft, under_review, approved, active, suspended, retired, and revoked, and related workflow controls. Preserve tenant isolation, server-side RBAC, validation, audit logging, CUI/data-handling guardrails, standard error behavior, source traceability, review metadata, enterprise identity controls, regulated-environment controls, key-management safety, and tenant-scoped data access. Add focused backend and frontend tests where behavior is affected, then run the relevant verification commands and report results.

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

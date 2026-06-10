# Development Story Implementation Prompts

These prompts are designed to be copied into a fresh implementation thread, one story at a time. Each prompt points back to the source backlog in [development-phase-use-cases.md](development-phase-use-cases.md) and should be executed under the project guidance in [../AGENTS.md](../AGENTS.md).

## Shared Prompt Requirements

Use these requirements for every story:

- Treat the MVP as **No-CUI / compliance management only**.
- Preserve tenant isolation, RBAC, audit logging, source traceability, and review metadata.
- Follow the existing project structure: React + Vite web app, ASP.NET Core API, application/domain/infrastructure layers, PostgreSQL persistence, and compliance content package.
- Read the referenced story, tasks, and acceptance criteria before editing.
- Keep changes scoped to the story unless a small supporting change is required.
- Add or update tests according to risk, especially for tenant boundaries, authorization, validation, and audit behavior.
- Update docs, API contracts, seed content, or UI states when the behavior changes.
- Verify the relevant build/test commands before handing off, or clearly report anything that could not be run.

## 1. Delivery Foundation

### Story 1.1: Repository And Project Structure

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

- Treat the MVP as **No-CUI / compliance management only**. 

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
> - Epic: Delivery Foundation
>
> - User story: As a technical lead, I want the application structure to separate web, API, application, domain, infrastructure, docs, and compliance content so that development stays maintainable.
>
> - Acceptance criteria:
>
> - A new developer can identify where frontend, backend, domain, persistence, infrastructure, and compliance content live.
> - The solution builds locally with documented commands.
> - No compliance workflow logic is embedded only in the UI.
> - Documentation points to the No-CUI MVP posture.
>
> Implement Story 1.1, "Repository And Project Structure," from `docs/development-phase-use-cases.md`. Confirm and improve the solution organization across `apps/api`, `apps/web`, `src/Gccs.Domain`, `src/Gccs.Application`, `src/Gccs.Infrastructure`, `packages/compliance-content`, `docs`, and `infra`. Update documentation so a new developer understands ownership boundaries, local setup, build commands, and the No-CUI MVP posture. Verify the solution builds cleanly and ensure compliance workflow logic is not trapped only in the UI.

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

- Treat the MVP as **No-CUI / compliance management only**. 

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

- Treat the MVP as **No-CUI / compliance management only**. 

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

- Treat the MVP as **No-CUI / compliance management only**. 

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

- Treat the MVP as **No-CUI / compliance management only**. 

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

- Treat the MVP as **No-CUI / compliance management only**. 

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

- Treat the MVP as **No-CUI / compliance management only**. 

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

- Treat the MVP as **No-CUI / compliance management only**. 

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

- Treat the MVP as **No-CUI / compliance management only**. 

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

## 4. No-CUI Controls

### Story 4.1: No-CUI Acknowledgement

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

- Treat the MVP as **No-CUI / compliance management only**. 

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
> - Epic: No-CUI Controls
>
> - User story: As a user, I want to understand the upload limitation before using the product so that I know what content is prohibited.
>
> - Acceptance criteria:
>
> - User sees a No-CUI notice before first upload.
> - User must acknowledge the notice before upload is enabled.
> - Acknowledgement is audit logged.
> - Notice copy states that the MVP is compliance management only and is not ready to store CUI.
>
> Implement Story 4.1, "No-CUI Acknowledgement," from `docs/development-phase-use-cases.md`. Add No-CUI notice content to onboarding and upload flows, require acknowledgement before upload, store acknowledgement by user/tenant/timestamp/notice version, expose acknowledgement status, and audit the acknowledgement. The copy must clearly state the MVP is compliance management only and not ready to store CUI.

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

- Treat the MVP as **No-CUI / compliance management only**. 

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
> - Epic: No-CUI Controls
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

- Treat the MVP as **No-CUI / compliance management only**. 

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

- Treat the MVP as **No-CUI / compliance management only**. 

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

- Treat the MVP as **No-CUI / compliance management only**. 

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

- Treat the MVP as **No-CUI / compliance management only**. 

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

- Treat the MVP as **No-CUI / compliance management only**. 

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

- Treat the MVP as **No-CUI / compliance management only**. 

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

- Treat the MVP as **No-CUI / compliance management only**. 

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

- Treat the MVP as **No-CUI / compliance management only**. 

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

- Treat the MVP as **No-CUI / compliance management only**. 

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

- Treat the MVP as **No-CUI / compliance management only**. 

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
> - Epic: Contract Intake
>
> - User story: As a contracts admin, I want to upload non-CUI contract documents and record document metadata so that source materials are available for review.
>
> - Acceptance criteria:
>
> - Upload is disabled until No-CUI acknowledgement is complete.
> - File metadata is linked to the contract.
> - Disallowed files are rejected.
> - Upload and delete actions are audit logged.
>
> Implement Story 8.2, "Contract Document Metadata And Upload," from `docs/development-phase-use-cases.md`. Add contract document metadata for solicitation, contract, subcontract, purchase order, SOW, flow-down attachment, wage determination, DD Form 254 metadata, and CUI marking guide metadata. Integrate No-CUI acknowledgement, file metadata, object storage reference, scan/validation status, rejection handling, and audit logging for upload/delete.

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

- Treat the MVP as **No-CUI / compliance management only**. 

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

- Treat the MVP as **No-CUI / compliance management only**. 

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

- Treat the MVP as **No-CUI / compliance management only**. 

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

- Treat the MVP as **No-CUI / compliance management only**. 

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

- Treat the MVP as **No-CUI / compliance management only**. 

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

- Treat the MVP as **No-CUI / compliance management only**. 

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

- Treat the MVP as **No-CUI / compliance management only**. 

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

- Treat the MVP as **No-CUI / compliance management only**. 

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

- Treat the MVP as **No-CUI / compliance management only**. 

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

- Treat the MVP as **No-CUI / compliance management only**. 

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

- Treat the MVP as **No-CUI / compliance management only**. 

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
> - User story: As a contributor, I want to upload approved non-CUI evidence files so that compliance proof is attached to the right work.
>
> - Acceptance criteria:
>
> - Upload requires No-CUI acknowledgement.
> - Files are not marked usable until validation and scan state allow it.
> - New file uploads create versions instead of overwriting history.
> - Upload, download, and delete actions are audit logged.
>
> Implement Story 12.2, "Evidence File Upload," from `docs/development-phase-use-cases.md`. Add file upload to evidence records with No-CUI acknowledgement, allowed file types, size limits, malware scan status, file version metadata, and download permissions. Files must not be usable until validation and scan state allow it, and upload/download/delete actions must be audit logged.

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

- Treat the MVP as **No-CUI / compliance management only**. 

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

- Treat the MVP as **No-CUI / compliance management only**. 

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

- Treat the MVP as **No-CUI / compliance management only**. 

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

- Treat the MVP as **No-CUI / compliance management only**. 

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

- Treat the MVP as **No-CUI / compliance management only**. 

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

- Treat the MVP as **No-CUI / compliance management only**. 

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

- Treat the MVP as **No-CUI / compliance management only**. 

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

- Treat the MVP as **No-CUI / compliance management only**. 

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

- Treat the MVP as **No-CUI / compliance management only**. 

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

- Treat the MVP as **No-CUI / compliance management only**. 

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

- Treat the MVP as **No-CUI / compliance management only**. 

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

- Treat the MVP as **No-CUI / compliance management only**. 

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

- Treat the MVP as **No-CUI / compliance management only**. 

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

- Treat the MVP as **No-CUI / compliance management only**. 

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

- Treat the MVP as **No-CUI / compliance management only**. 

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

- Treat the MVP as **No-CUI / compliance management only**. 

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

- Treat the MVP as **No-CUI / compliance management only**. 

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

- Treat the MVP as **No-CUI / compliance management only**. 

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

- Treat the MVP as **No-CUI / compliance management only**. 

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
> Implement Story 17.4, "Production Readiness Checklist," from `docs/development-phase-use-cases.md`. Create the MVP production readiness checklist covering No-CUI notice, terms, support path, prohibited upload guidance, backups, restore test, logs, alerts, rollback plan, malware scanning path or limitation, expert-reviewed content, release notes, known limitations, source URLs, review metadata, and staging rollback verification.

Instructions: 

1. Inspect the existing codebase, schema, API spec, and use-case docs before editing. 

2. Reuse existing project patterns and avoid broad refactors. 

3. Implement the smallest complete vertical slice that satisfies the acceptance criteria. 

4. Enforce tenant scoping, RBAC, validation, and audit logging where applicable. 

5. Add or update tests for the behavior. 

6. Update API/schema/docs only if the implementation changes the contract. 

7. Run the relevant verification commands and summarize results. 

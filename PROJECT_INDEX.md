# GCCS Master Project Index

GCCS is a Government Contractor Compliance SaaS for small U.S. businesses. The product promise is to help contractors know what applies, prove what they did, and stay ready for audits, renewals, bids, certifications, and prime contractor reviews.

The current build posture is **No-CUI / compliance management only**. Do not add customer CUI storage or workflows until the product has a CUI-ready architecture, shared responsibility matrix, operating controls, customer terms, and assessment posture.

## Current Stack

| Area | Technology | Primary location |
| --- | --- | --- |
| Backend API | ASP.NET Core on the repo-selected .NET runtime | `apps/api` |
| Domain model | C# records and enums | `src/Gccs.Domain` |
| Application layer | Services, DTOs, ports | `src/Gccs.Application` |
| Infrastructure | EF Core, persistence, adapters | `src/Gccs.Infrastructure` |
| Frontend app | React 19, Vite, TypeScript | `apps/web` |
| Database | PostgreSQL 17 | `infra/docker/docker-compose.yml` |
| Cache/jobs placeholder | Redis 8 | `infra/docker/docker-compose.yml` |
| Object storage placeholder | MinIO | `infra/docker/docker-compose.yml` |
| Malware scanning placeholder | ClamAV | `infra/docker/docker-compose.yml` |
| Compliance content | Source-backed seed data | `packages/compliance-content` |

## Repository Map

```text
apps/
  api/                         ASP.NET Core API surface and local settings.
  web/                         React + Vite authenticated SaaS workspace.
src/
  Gccs.Domain/                 Framework-independent compliance domain model.
  Gccs.Application/            Use cases, DTOs, and repository interfaces.
  Gccs.Infrastructure/         Persistence, dependency injection, and adapters.
docs/
  product-strategy.md          Product positioning, MVP posture, scope, and deferrals.
  mvp-execution-plan.md        Launch gates, data policy, acceptance criteria, support model.
  api/                         OpenAPI contract and API implementation guidance.
  architecture.md              System boundaries, No-CUI posture, planned services.
  database-models.md           EF Core schema, migration commands, model groups.
  mvp-roadmap.md               Phase 0-2 roadmap snapshot.
  software-delivery-plan.md    Delivery plan, requirements, roles, cadence.
  glossary-and-acronyms.md     Plain-English govcon, compliance, security, and app terminology.
  compliance-content-governance.md
                               Source-backed content governance and review rules.
  design-flow-diagrams.md      Product and design workflow diagrams.
  workflow-diagram.md          Operational workflow diagram.
infra/
  database/                    Generated development SQL schema.
  docker/                      Local Postgres, Redis, MinIO, and ClamAV services.
  terraform/                   Early infrastructure placeholder for dev.
packages/
  compliance-content/          Obligation library package and MVP seed data.
```

## Ownership Boundaries

| Boundary | Primary responsibility | Current implementation state |
| --- | --- | --- |
| `apps/web` | Authenticated React workspace, UI composition, client API calls, empty/loading/error states, and presentation concerns. | Renders the overview dashboard from the API with a No-CUI posture banner and posture-only fallback states when source data is unavailable. |
| `apps/api` | HTTP boundary, local/prod auth configuration, tenant context, RBAC policies, rate limiting, security headers, and endpoint routing. | Exposes health, compliance overview, and source-backed obligation endpoints with auth and permission policies. |
| `src/Gccs.Domain` | Framework-independent compliance model: tenants, users, roles, contracts, obligations, evidence, controls, reports, audit logs, and review metadata. | Domain records and enums exist for MVP modules and persistence mapping. |
| `src/Gccs.Application` | Use cases, DTOs, repository/storage ports, and compliance workflow orchestration. | Builds the compliance overview and defines the obligation repository port. |
| `src/Gccs.Infrastructure` | EF Core schema, migrations, repository adapters, local content adapters, and future infrastructure integrations. | Contains `GccsDbContext`, migrations, persistence models, dependency injection, and an in-memory obligation adapter. |
| `packages/compliance-content` | Governed source-backed obligation package with source URLs, review metadata, confidence, expert-review flags, and publication state. | MVP JSON seed data exists; runtime repository loading from the package is still future work. |
| `docs` | Product strategy, architecture, API contract, database model, governance, and delivery instructions. | Story 1.1 developer orientation and setup guidance are documented in `README.md` and this index. |
| `infra` | Local service composition, generated schema, and future cloud infrastructure as code. | Docker Compose provides PostgreSQL, Redis, MinIO, and ClamAV placeholders; Terraform dev placeholder exists. |

Compliance workflow logic should never exist only in `apps/web`. Tenant scoping, RBAC, No-CUI policy enforcement, audit logging decisions, source traceability, review metadata, and obligation applicability belong in the backend/application/domain boundary and must be covered by tests as the corresponding workflows are implemented.

## Product Modules

| Module | MVP intent | Current implementation signal |
| --- | --- | --- |
| Company compliance profile | UEI, CAGE, SAM, NAICS, certifications, role, locations, IT/data posture | Domain and persistence models exist; API/UI workflow not yet built. |
| Contract and clause intake | Capture contracts, solicitations, subcontracts, flow-downs, wage determinations, data requirements | Domain and persistence models exist; manual workflow pending. |
| Obligation dashboard | Map clauses to actions, owners, evidence, deadlines, risk, and source links | Seeded in API overview and in-memory repository. |
| Compliance calendar | Track renewals, reports, affirmations, training, deliverables, policy reviews | Domain and persistence models exist; UI workflow pending. |
| Evidence vault | Tag evidence by obligation, contract, control, vendor, employee, expiration | Domain and persistence models exist; upload/scan workflow pending. |
| CMMC readiness tracker | Level 1/2 readiness, controls, evidence, SSP, POA&M, assets, affirmations | Domain and persistence models exist; assessment workflow pending. |
| Subcontractor flow-down tracker | Track subcontractors, flow-down clauses, status, insurance, NDAs, CUI access, workshare | Domain and persistence models exist; workflow pending. |
| Basic reports | Obligation matrices, readiness reports, evidence packages, risk dashboards | Domain and persistence models exist; generation pending. |

## Backend Entry Points

| Path | Purpose |
| --- | --- |
| `apps/api/Program.cs` | Minimal API routes, CORS, OpenAPI, health, overview, obligation endpoints. |
| `src/Gccs.Application/Compliance/ComplianceOverviewService.cs` | Builds the MVP overview used by the frontend shell. |
| `src/Gccs.Application/Repositories/IObligationRepository.cs` | Obligation repository port. |
| `src/Gccs.Infrastructure/DependencyInjection.cs` | Wires infrastructure services, in-memory obligation data, and optional EF DbContext. |
| `src/Gccs.Infrastructure/Compliance/InMemoryObligationRepository.cs` | Seeded obligation repository used by current API endpoints. |
| `src/Gccs.Infrastructure/Persistence/GccsDbContext.cs` | EF Core DbContext for the development schema. |
| `src/Gccs.Infrastructure/Persistence/Models/` | Persistence entities for core, compliance content, and operations data. |
| `src/Gccs.Infrastructure/Persistence/Migrations/` | Initial EF Core development migration. |

### Implemented API Routes

| Route | Purpose |
| --- | --- |
| `GET /health` | Service health and current data posture. |
| `GET /api/compliance/overview` | Product promise, MVP modules, and priority obligations. |
| `GET /api/obligations` | List source-backed obligations from the current repository. |
| `GET /api/obligations/{id}` | Fetch one obligation by ID. |

The broader target API is documented in `docs/api/openapi.yaml`.

## Frontend Entry Points

| Path | Purpose |
| --- | --- |
| `apps/web/src/App.tsx` | Current authenticated workspace shell and overview dashboard. |
| `apps/web/src/lib/api.ts` | API client and fallback overview data. |
| `apps/web/src/components/ModuleCard.tsx` | MVP module display card. |
| `apps/web/styles/globals.css` | Global application styling. |
| `apps/web/vite.config.ts` | Vite configuration and path aliases. |

The frontend expects the API at `http://localhost:5062` unless `VITE_API_BASE_URL` is set.

## Domain Model Index

| Domain area | Primary files |
| --- | --- |
| Tenancy and identity | `src/Gccs.Domain/Tenancy/Tenant.cs`, `src/Gccs.Domain/Identity/User.cs`, `src/Gccs.Domain/Identity/Role.cs` |
| Company profile | `src/Gccs.Domain/Companies/CompanyProfile.cs` |
| Contracts | `src/Gccs.Domain/Contracts/Contract.cs` |
| Compliance content | `src/Gccs.Domain/Compliance/Clause.cs`, `Obligation.cs`, `ComplianceTask.cs`, `SourceReference.cs`, `ComplianceSource.cs`, `RiskLevel.cs`, `ApplicabilityDimension.cs`, `EvidenceExample.cs`, `MvpModule.cs` |
| Evidence | `src/Gccs.Domain/Evidence/EvidenceItem.cs` |
| CMMC | `src/Gccs.Domain/Cmmc/CmmcModels.cs` |
| Vendors and subcontractors | `src/Gccs.Domain/Vendors/Vendor.cs` |
| People and training | `src/Gccs.Domain/People/Employee.cs` |
| Labor | `src/Gccs.Domain/Labor/LaborModels.cs` |
| Reports | `src/Gccs.Domain/Reports/Report.cs` |
| Audit | `src/Gccs.Domain/Audit/AuditLogEntry.cs` |
| Shared value objects | `src/Gccs.Domain/Common/` |

## Compliance Content Index

| Path | Purpose |
| --- | --- |
| `packages/compliance-content/README.md` | Content package rules and obligation ontology. |
| `packages/compliance-content/obligations/mvp.json` | MVP source-backed obligation seed records. |
| `docs/compliance-content-governance.md` | Review cadence, guardrails, and record requirements. |

Every production obligation should include source name, source URL, last reviewed date, effective date when known, trigger conditions, applicability dimensions, required actions, evidence examples, flow-down requirements, risk level, confidence, and expert-review flags.

## Database Index

| Artifact | Path |
| --- | --- |
| EF DbContext | `src/Gccs.Infrastructure/Persistence/GccsDbContext.cs` |
| Persistence entities | `src/Gccs.Infrastructure/Persistence/Models/` |
| Initial migration | `src/Gccs.Infrastructure/Persistence/Migrations/20260610031239_InitialDevelopmentSchema.cs` |
| Clause review/versioning migration | `src/Gccs.Infrastructure/Persistence/Migrations/20260610051044_AddClauseReviewVersioning.cs` |
| SQL schema script | `infra/database/development-schema.sql` |
| EF tool manifest | `dotnet-tools.json` |

The development database uses the `gccs` PostgreSQL schema and stores enum values as readable strings. Tenant-scoped operational tables include `tenant_id` indexes, but tenant context and global query filters still need to be wired into API workflows.

## Local Development Commands

Install dependencies and build:

```bash
dotnet restore Gccs.slnx
dotnet build Gccs.slnx
npm install
npm run build:web
```

Run focused verification:

```bash
npm run test:api
npm run lint:web
npm run test:web
npm run build:web
```

Run all currently wired tests:

```bash
npm test
```

## Continuous Integration

Pull requests run `.github/workflows/ci.yml`. The required branch protection checks should be:

- `Backend validation`
- `Frontend validation`
- `Secret scan`

The backend job restores .NET dependencies, runs dependency vulnerability scans, builds `Gccs.slnx`, validates EF Core migrations with pending-model-change and idempotent script generation checks, and runs xUnit unit/integration tests with uploaded TRX results.

The frontend job restores packages with `npm ci`, runs dependency vulnerability scans, lints the React workspace, runs Vitest/React Testing Library tests with JUnit output, builds the Vite app, and uploads test results.

The secret-scan job runs repository secret scanning with Gitleaks. Failing build, lint, test, migration validation, dependency vulnerability scan, or secret scanning steps are blocking job failures so reviewers can see the affected project and step directly in CI logs.

Run local services:

```bash
docker compose -f infra/docker/docker-compose.yml up -d
```

Run the API:

```bash
dotnet run --project apps/api
```

Run the web app:

```bash
npm run dev:web
```

Apply the development migration:

```bash
dotnet tool restore
dotnet tool run dotnet-ef database update \
  --project src/Gccs.Infrastructure/Gccs.Infrastructure.csproj \
  --startup-project apps/api/Gccs.Api.csproj \
  --context GccsDbContext
```

Generate the SQL review script:

```bash
dotnet tool run dotnet-ef migrations script \
  --project src/Gccs.Infrastructure/Gccs.Infrastructure.csproj \
  --startup-project apps/api/Gccs.Api.csproj \
  --context GccsDbContext \
  --output infra/database/development-schema.sql
```

Validate the OpenAPI YAML parses:

```bash
ruby -ryaml -e 'doc = YAML.load_file("docs/api/openapi.yaml"); puts doc["openapi"]'
```

## Documentation Reading Order

1. `README.md` for the quick project orientation.
2. `PROJECT_INDEX.md` for the master map of the repo.
3. `docs/architecture.md` for system boundaries and No-CUI posture.
4. `docs/software-delivery-plan.md` for delivery requirements and team workflow.
5. `docs/database-models.md` for persistence details and migration commands.
6. `docs/api/README.md` and `docs/api/openapi.yaml` for API contract work.
7. `docs/compliance-content-governance.md` before changing obligation content.
8. `docs/mvp-roadmap.md` for phased delivery priorities.

## Story 1.1 Status

Story 1.1, "Repository And Project Structure," is satisfied when this index and `README.md` stay accurate, the solution builds with the documented commands, and compliance workflow logic remains outside UI-only code. The current codebase has the required directories/projects, documented ownership boundaries, No-CUI MVP posture, API/application/domain separation for the overview workflow, posture-only frontend fallback states, and tests guarding the API security boundary plus repository structure.

## Near-Term Implementation Priorities

1. Add tenant context, authentication posture, RBAC checks, and tenant-scoped query filters.
2. Replace the in-memory obligation repository with database-backed seed loading from the compliance content package.
3. Implement company profile CRUD and profile completeness scoring.
4. Implement contract intake with manual clause tagging before automated extraction.
5. Add task/calendar endpoints tied to obligations, contracts, controls, and evidence.
6. Build evidence metadata workflows with No-CUI attestation, upload intents, malware scan status, and version history.
7. Add CMMC Level 1 and Level 2 readiness workflows with control evidence mapping and POA&M tracking.
8. Add subcontractor profiles, flow-down clauses, and evidence request workflows.
9. Add report generation for obligation matrix, readiness, subcontractor status, and evidence packages.
10. Add tests around tenant isolation, obligation mapping, content source metadata, and upload guardrails.

## Working Rules For Future Changes

- Preserve the No-CUI MVP posture unless the work explicitly introduces a CUI-ready enclave.
- Do not publish compliance obligations without source URLs, review metadata, confidence, and expert-review handling.
- Treat AI output as draft and cite curated sources or tenant documents for generated compliance answers.
- Keep the domain layer framework-independent.
- Prefer application ports and infrastructure adapters over direct external calls from API handlers.
- Avoid generic GRC workflows when a govcon-specific obligation, evidence, or flow-down workflow is needed.
- Keep customer-facing compliance wording clearly separated from legal determinations.

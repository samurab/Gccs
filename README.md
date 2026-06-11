# GCCS - Government Contractor Compliance SaaS

GCCS is a starter application for a govcon-specific compliance operating system for small U.S. government contractors. The MVP posture is **No-CUI / compliance management only** until the platform is explicitly hardened and assessed for customer CUI handling.

## Project Structure

```text
apps/
  api/                 ASP.NET Core Web API on the repo-selected .NET runtime
  web/                 React + Vite frontend shell
src/
  Gccs.Domain/         Core compliance entities and value objects
  Gccs.Application/    Use cases, DTOs, service interfaces
  Gccs.Infrastructure/ Persistence/integration adapters
packages/
  compliance-content/  Source-backed obligation library seed data
docs/                  Product, architecture, and governance notes
infra/                 Local and future cloud infrastructure
```

## Ownership Boundaries

| Location | Owner of | Should not own |
| --- | --- | --- |
| `apps/web` | React screens, route shell, UI states, client API calls, accessibility, and presentation-only formatting. | Compliance applicability decisions, tenant authorization, RBAC enforcement, upload policy enforcement, audit decisions, or source-of-truth obligation content. |
| `apps/api` | HTTP endpoints, authentication, tenant context, RBAC policies, request validation, response shaping, and API composition. | Persistence details, external-service implementation details, or UI-only compliance logic. |
| `src/Gccs.Domain` | Framework-independent entities, value objects, enums, and domain rules for tenants, contracts, obligations, evidence, CMMC, reports, and audit concepts. | ASP.NET, EF Core, React, HTTP, database, or cloud SDK dependencies. |
| `src/Gccs.Application` | Use cases, DTOs, ports/interfaces, workflow orchestration, and compliance workflow behavior used by API and future workers. | Direct database, object storage, external API, or UI rendering code. |
| `src/Gccs.Infrastructure` | EF Core persistence, migrations, repository adapters, local seed adapters, and future integrations for storage, queue, search, AI, and external systems. | Customer-facing workflow decisions that belong in application/domain code. |
| `packages/compliance-content` | Source-backed obligation seed content with URLs, review metadata, confidence labels, and expert-review state. | Tenant-specific data or unreviewed customer-facing legal determinations. |
| `docs` | Product, architecture, API, database, governance, and delivery documentation. | Runtime workflow behavior. |
| `infra` | Local Docker services, generated SQL, and future IaC. | Application business logic or compliance content. |

Compliance workflow logic must be reusable from backend services and tests. The UI may explain and display obligations, but applicability, source traceability, tenant scoping, RBAC, No-CUI policy enforcement, and audit-worthy decisions belong in the API/application/domain/infrastructure boundary.

## Documentation

- [Architecture](docs/architecture.md)
- [Master project index](PROJECT_INDEX.md)
- [Product strategy](docs/product-strategy.md)
- [MVP execution plan](docs/mvp-execution-plan.md)
- [Design flow diagrams](docs/design-flow-diagrams.md)
- [Workflow diagram](docs/workflow-diagram.md)
- [MVP roadmap](docs/mvp-roadmap.md)
- [Development database models](docs/database-models.md)
- [Compliance content governance](docs/compliance-content-governance.md)
- [Glossary and acronyms](docs/glossary-and-acronyms.md)

## Local Development

Prerequisites:

- .NET SDK matching [`global.json`](global.json)
- Node.js and npm
- Docker, when local PostgreSQL/Redis/MinIO/ClamAV services are needed

### Backend

```bash
dotnet restore Gccs.slnx
dotnet build Gccs.slnx
dotnet run --project apps/api
```

Useful endpoints:

- `GET /health`
- `GET /api/compliance/overview`
- `GET /api/obligations`
- `GET /api/obligations/{id}`

All `/api` endpoints require authentication. In local development only, send
`X-Gccs-Dev-Auth: true` to use the development auth handler. Production requires
`Authentication:Authority` and `Authentication:Audience` for JWT bearer tokens.

### Frontend

```bash
npm install
npm run dev:web
```

The frontend expects the API at `http://localhost:5062` by default. Override with `VITE_API_BASE_URL`.

The authenticated SaaS app uses React + Vite. If SEO-heavy public content becomes a requirement later, add a separate marketing/content site, for example `www` on Next.js, while keeping this app at an application subdomain such as `app`.

When the API is unavailable, the web app may show posture-only empty states. Do not duplicate source-backed obligation records, applicability logic, review metadata, tenant authorization, RBAC decisions, No-CUI upload policy enforcement, or audit behavior in frontend fallback data.

### Tests

```bash
npm run test:api
npm run lint:web
npm run test:web
npm run build:web
npm test
```

The root `npm test` command runs the ASP.NET Core xUnit suite and the React Vitest suite. Backend user story work should add or update focused xUnit coverage, and frontend user story work should add or update Vitest tests with React Testing Library for user-visible behavior. For frontend changes, run lint, Vitest, and the production build before handoff.

### Local Services

```bash
docker compose -f infra/docker/docker-compose.yml up -d
```

This starts PostgreSQL, Redis, MinIO, and ClamAV placeholders for the MVP architecture.

Apply development migrations after services are running:

```bash
dotnet tool restore
dotnet tool run dotnet-ef database update \
  --project src/Gccs.Infrastructure/Gccs.Infrastructure.csproj \
  --startup-project apps/api/Gccs.Api.csproj \
  --context GccsDbContext
```

Validate the OpenAPI contract parses:

```bash
ruby -ryaml -e 'doc = YAML.load_file("docs/api/openapi.yaml"); puts doc["openapi"]'
```

## MVP Modules

- Company compliance profile
- Contract and clause intake
- Obligation dashboard
- Compliance calendar
- Evidence vault
- CMMC Level 1 and Level 2 readiness tracker
- Subcontractor flow-down tracker
- Basic reports
- Source-backed obligation library

## Compliance Content Note

This application structure is product and engineering scaffolding, not legal advice. Production content should be reviewed by qualified government contracts, labor, cybersecurity, CMMC, or finance experts depending on module scope.

The MVP is explicitly **No-CUI / compliance management only**. Do not add customer CUI storage, classified data handling, ITAR/export-controlled technical data handling, or CUI-ready marketing claims until a separate architecture, shared responsibility matrix, customer terms, support model, and assessment posture are approved.

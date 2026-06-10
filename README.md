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

## Local Development

### Backend

```bash
dotnet restore Gccs.slnx
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
cd apps/web
npm install
npm run dev
```

The frontend expects the API at `http://localhost:5062` by default. Override with `VITE_API_BASE_URL`.

The authenticated SaaS app uses React + Vite. If SEO-heavy public content becomes a requirement later, add a separate marketing/content site, for example `www` on Next.js, while keeping this app at an application subdomain such as `app`.

### Tests

```bash
npm test
```

The root test command runs the ASP.NET Core xUnit suite and the React Vitest suite. Backend user story work should add or update focused xUnit coverage, and frontend user story work should add or update Vitest tests with React Testing Library for user-visible behavior.

### Local Services

```bash
docker compose -f infra/docker/docker-compose.yml up -d
```

This starts PostgreSQL, Redis, MinIO, and ClamAV placeholders for the MVP architecture.

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

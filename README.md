# GCCS - Government Contractor Compliance SaaS

GCCS is a starter application for a govcon-specific compliance operating system for small U.S. government contractors. The MVP posture is **No-CUI / compliance management only** until the platform is explicitly hardened and assessed for customer CUI handling.

## Project Structure

```text
apps/
  api/                 ASP.NET Core 10 Web API
  web/                 Next.js frontend shell
src/
  Gccs.Domain/         Core compliance entities and value objects
  Gccs.Application/    Use cases, DTOs, service interfaces
  Gccs.Infrastructure/ Persistence/integration adapters
tests/
  Gccs.Api.Tests/      Backend tests
packages/
  compliance-content/  Source-backed obligation library seed data
docs/                  Product, architecture, and governance notes
infra/                 Local and future cloud infrastructure
```

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

### Frontend

```bash
cd apps/web
npm install
npm run dev
```

The frontend expects the API at `http://localhost:5217` by default. Override with `NEXT_PUBLIC_API_BASE_URL`.

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

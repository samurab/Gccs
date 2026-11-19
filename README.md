# FeDril - Government Contractor Compliance SaaS

FeDril is a starter application for a govcon-specific compliance operating system for small U.S. government contractors. The current MVP posture is **No-CUI / compliance management only**: users can manage compliance work, metadata, obligations, tasks, and evidence, but real customer CUI is prohibited until a future approved `CuiReady` posture is implemented.

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

Compliance workflow logic must be reusable from backend services and tests. The UI may explain and display obligations, but applicability, source traceability, tenant scoping, RBAC, CUI/data-handling policy enforcement, and audit-worthy decisions belong in the API/application/domain/infrastructure boundary.

## Documentation

- [Architecture](docs/architecture.md)
- [Master project index](PROJECT_INDEX.md)
- [Product strategy](docs/product-strategy.md)
- [MVP execution plan](docs/mvp-execution-plan.md)
- [Data requirements and source systems](docs/data-requirements-and-source-systems.md)
- [Dependency register](docs/dependency-register.md)
- [Definition of Ready](docs/definition-of-ready.md)
- [Security control implications](docs/security-control-implications.md)
- [Design flow diagrams](docs/design-flow-diagrams.md)
- [Workflow diagram](docs/workflow-diagram.md)
- [MVP roadmap](docs/mvp-roadmap.md)
- [MVP roadmap story traceability](docs/mvp-roadmap-story-traceability.md)
- [Development database models](docs/database-models.md)
- [Compliance content governance](docs/compliance-content-governance.md)
- [Glossary and acronyms](docs/glossary-and-acronyms.md)

## Local Development

Prerequisites:

- .NET SDK 10.0.402 matching [`global.json`](global.json); SDK roll-forward is disabled
- Node.js 24.22.1 and its bundled npm, matching the CI setup steps
- Terraform matching [`.terraform-version`](.terraform-version) when validating infrastructure
- Docker, when local PostgreSQL/Redis/Azurite/ClamAV services are needed

Install the exact SDK if `dotnet --version` reports that the requested SDK is unavailable; a newer feature band does not substitute for it. CI service images use content digests. Update runtime versions and image digests through a reviewed dependency change with compatibility checks; a pin alone does not establish security freshness. Runner images, system packages, browser downloads and action-internal tools retain separate update and verification requirements.

Start the complete local stack with one supervised command:

```bash
npm run dev
```

This starts or reuses the Docker dependencies, applies pending development migrations when the API is not already running, and supervises the API at `http://localhost:5062` and web app at `http://127.0.0.1:5173`. Each app recovers independently: a web crash leaves the API running, and an API crash leaves the web server running while API requests may fail until recovery completes. Recovery attempts for each app are serialized, with failed attempts retried after five seconds. If Docker becomes unavailable, the supervisor exits so the login job can restore the shared dependencies and start the apps again. Ctrl+C stops only apps started by that supervisor.

The launcher reuses healthy apps and waits for an occupied API/web port to become ready instead of starting another process. An occupied port that never becomes healthy produces an error. The web process uses strict port `5173`, and existing dependency containers are preserved with `--no-recreate`; intentional dependency configuration changes may therefore require a separately reviewed container replacement.

Recovery tests should use test-owned processes and isolated ports by default. A real API also needs isolated data and disabled external delivery. The login-managed services on ports `5062` and `5173` are shared with interactive UI sessions; stopping or hanging them for a test interrupts those sessions. Shared-stack failure tests require an explicitly authorized interruption window under [the local recovery test guidance](AGENTS.md#local-recovery-test-isolation). When investigating restarts, compare launchd and supervisor events with recovery-test records: intentional test stops can otherwise look like application instability. On November 18, 2026, repeated shared-stack test stops caused this symptom.

Run `npm run test:dev:recovery` for four alternating API-crash/web-hang and API-hang/web-crash startup rounds. The maintained runner builds the real API into a unique run directory, allocates separate HTTP and lease ports, and creates dedicated PostgreSQL, Redis and Azurite containers from locally available images. API configuration excludes user secrets and inherited service settings; delivery, retention, seed and processing workers are disabled. Vite uses a run-owned environment directory and cache. The runner checks that shared API/UI process identities and sampled health remain unchanged, verifies takeover by original waiters and clean URL exits, and stops only its own apps and dependency containers. It never stops or restarts launchd. Logs, synthetic data, build outputs and stopped containers are retained under the run's reported evidence path; removing them requires separate disposition approval. Docker, the local dependency images, .NET and Node must already be available. This tests startup recovery rather than production isolation or business-workflow behavior.

To cancel the isolated recovery test, press Ctrl+C and wait for its `EVIDENCE` path. The runner stops its owned apps, debugger helpers and dependency containers while retaining run files, containers and data. Repeated Ctrl+C signals leave cleanup running. A completed cancellation records `CANCELLED` and exits with code `130`; cleanup errors still record `FAIL`. Cancellation cleanup can take tens of seconds while processes and containers stop.

An unresponsive app started by this supervisor is replaced after its readiness timeout (Web: 30 seconds; API: 60 seconds), a fresh response check, and confirmation that Docker is reachable. Only that app's owned process group is terminated, with a five-second grace period before forced termination if necessary; replacement waits for that group to exit. A responsive error such as HTTP 503 does not trigger this hang termination. Reused processes with no owned child handle are left untouched, and may require their owner's intervention. Recovery attempts emit `AppRecovery` start/finish records in the supervisor log.

On the configured development Mac, the `com.fedril.local-stack` user LaunchAgent runs `tools/dev/start-at-login.sh` after sign-in. It waits for Docker Desktop, then starts the same supervised stack with explicit developer tool paths. Failed runs are configured to retry with a 60-second launchd throttle. An interrupted macOS update restart can leave the GUI session in launchd's `on-demand-only` mode, which defers automatic relaunch; a fresh sign-in is needed to retest automatic recovery in a normal session. Logs are in `~/Library/Logs/FeDrilLocalStack/stdout.log` and `stderr.log`. Inspect it with `launchctl print gui/$(id -u)/com.fedril.local-stack`; request a run with `launchctl kickstart gui/$(id -u)/com.fedril.local-stack` (without `-k`, which would interrupt a running supervisor). This setup requires the repository and tool paths to remain available. It does not install dependencies, pull source changes, or run a production deployment; a cold start builds the local checkout and applies its pending development migrations through the existing startup workflow.

### Backend

```bash
dotnet restore Gccs.slnx
dotnet build Gccs.slnx
dotnet run --project apps/api
```

When a healthy local API already serves the configured HTTP loopback URL, `dotnet run --project apps/api` prints that URL and exits successfully before migrations, seeding, or workers start. Simultaneous launches coordinate through an OS-held loopback lease before that work begins; extra attempts wait for the owner's healthy URL and then exit successfully. The lease stays with the owning process and is released by the OS on exit or crash. This applies only in Development during direct API execution. Unhealthy or unrelated listeners retain normal startup errors. HTTPS, non-loopback bindings, and explicit Kestrel endpoints retain normal startup behavior.

Useful endpoints:

- `GET /health`
- `GET /api/compliance/overview`
- `GET /api/obligations`
- `GET /api/obligations/{id}`

All `/api` endpoints require authentication. In local development only, send
`X-Gccs-Dev-Auth: true` to use the development auth handler. Production requires
the workforce `Authentication:Authority` and `Authentication:Audience` settings.
An environment can additionally configure `Authentication:Customer:Authority`
and `Authentication:Customer:Audience` to route customer APIs through a separate
External ID tenant while retaining workforce-only authentication for `/api/platform`.

### Frontend

```bash
npm install
npm run dev:web
```

`npm run dev:web`, `npm --workspace apps/web run dev`, and `npm run dev` from `apps/web` print the existing healthy FeDril web URL and exit successfully when it is already running. Simultaneous launches use an OS-held loopback lease, so one owner starts Vite and extra attempts wait for its healthy URL before exiting successfully. The check uses Vite's resolved HTTP loopback host, port, and base path, and accepts the normal `--host`, `--port`, and `--strictPort` options. Other CLI forms pass directly to Vite. It does not stop either app; unhealthy or unrelated listeners retain normal Vite startup behavior, including strict-port errors.

These development leases use a separate loopback TCP port, calculated as `((appPort - 1 + 32768) % 65535) + 1` (`37830` for the default API and `37941` for the web app). They create no lock files and need no stale-file removal. Host aliases on the same app port share a lease. On macOS and Linux, a separate Bash watchdog is armed before lease acquisition. Waiting attempts renew its deadline; an elected owner has 45 seconds to become healthy, after which the watchdog terminates only its own startup parent and the OS releases the lease for a waiter. Health disarms the watchdog while the owner retains its lease. The child checks its current parent identity before signaling and exits on pipe closure or parent loss. Waiters allow 90 seconds per observed owner. They renew the budget only when read-only `lsof` and `ps` checks identify a different listener PID/start time, including suspended owners. An unchanged owner or unavailable identity retains the bounded timeout; missing tools fall back to the original total wait. These identity checks never signal a port owner. Unavailable coordination ports and unrelated port owners still produce bounded errors rather than competing processes. Repeated actual ownership changes can extend the overall wait beyond 90 seconds. Windows retains lease-only coordination. This deadline covers coordinated development startup, not arbitrary startup commands or post-startup hangs; the local-stack supervisor handles hangs in its own running children. Startup commands that bypass these development entry points do not participate in coordination.

The frontend expects the API at `http://localhost:5062` by default. Override with `VITE_API_BASE_URL`.
Workforce authentication uses `VITE_MSAL_CLIENT_ID`, `VITE_MSAL_TENANT_ID`, and
`VITE_MSAL_API_SCOPE`. Customer External ID is enabled only when
`VITE_CUSTOMER_MSAL_CLIENT_ID`, `VITE_CUSTOMER_MSAL_TENANT_ID`,
`VITE_CUSTOMER_MSAL_TENANT_SUBDOMAIN`, and `VITE_CUSTOMER_MSAL_API_SCOPE` are all set.

The authenticated SaaS app uses React + Vite. If SEO-heavy public content becomes a requirement later, add a separate marketing/content site, for example `www` on Next.js, while keeping this app at an application subdomain such as `app`.

When the API is unavailable, the web app may show posture-only empty states. Do not duplicate source-backed obligation records, applicability logic, review metadata, tenant authorization, RBAC decisions, CUI/data-handling upload policy enforcement, or audit behavior in frontend fallback data.

### Tests

```bash
npm run test:api
npm run lint:web
npm run test:web
npm run build:web
npm test
```

The root `npm test` command runs the ASP.NET Core xUnit suite and the React Vitest suite. Backend user story work should add or update focused xUnit coverage, and frontend user story work should add or update Vitest tests with React Testing Library for user-visible behavior. For frontend changes, run lint, Vitest, and the production build before handoff.

### Continuous Integration

Pull requests run the GitHub Actions workflow in `.github/workflows/ci.yml`. The pipeline is split into clearly named backend, frontend, and secret-scan jobs so reviewers can identify the failing project and step from the check name and logs.

CI validates:

- Backend dependency restore, dependency vulnerability scans, solution build, EF Core migration validation, and xUnit unit/integration tests.
- Frontend dependency restore with `npm ci`, dependency vulnerability scans, ESLint, Vitest/React Testing Library tests, and production build.
- Repository secret scanning with Gitleaks.
- Backend and frontend test result artifacts for failed and successful runs.

Configure repository branch protection to require the `Backend validation`, `Frontend validation`, and `Secret scan` checks before merge. A failing build, lint, test, migration validation, dependency vulnerability scan, or secret scanning step should block pull request merge.

### Local Services

```bash
docker compose -f infra/docker/docker-compose.yml up -d --wait
```

This starts PostgreSQL, Redis, an Azure Blob-compatible Azurite endpoint, and the ClamAV malware-scanning placeholder for the MVP architecture. Blob containers are created privately on first write. The credentials in `infra/docker/docker-compose.yml`, `apps/api/appsettings.Development.json`, and `.env.example` are local-only development placeholders and must not be reused as production secrets.

To avoid common port conflicts with developer machines, the services are exposed on non-default host ports: PostgreSQL `15432`, Redis `16379`, Azurite Blob API `19000`, and ClamAV `13310`.

Local dependency containers use `restart: unless-stopped` to recover after Docker restarts or a container exits unexpectedly. Start Docker Desktop at login on macOS. An intentional container stop remains effective; resume existing containers with `docker compose -f infra/docker/docker-compose.yml start --wait`. To apply the restart policy to existing containers without recreating them, run `docker update --restart unless-stopped docker-postgres-1 docker-redis-1 docker-azurite-1 docker-clamav-1` for the default Compose project names.

Scheduled macOS updates can quit Docker Desktop even if another app subsequently interrupts the restart. If the API reports connection failures on port `15432`, reopen Docker Desktop and wait for its engine and dependency containers to become healthy. Complete the pending macOS update during a maintenance window. Container restart policies cover dependencies; the separately configured local-stack LaunchAgent starts the API and web app after sign-in. Machines without that agent must run `npm run dev` manually after restarting.

The API reads local dependency configuration from `apps/api/appsettings.Development.json` by default. If you prefer shell environment variables, use `.env.example` as the key template and export the values before running the API. When `LocalDependencies:Enabled` is `true`, startup fails with a clear error if PostgreSQL, Redis, object storage, or malware scanner configuration is missing.

Apply development migrations after services are running:

```bash
dotnet tool restore
dotnet tool run dotnet-ef database update \
  --project src/Gccs.Infrastructure/Gccs.Infrastructure.csproj \
  --startup-project apps/api/Gccs.Api.csproj \
  --context GccsDbContext
```

Check API dependency connectivity:

```bash
dotnet run --project apps/api
curl http://localhost:5062/health
```

The health response includes `postgresql`, `redis`, `object-storage`, and `malware-scanner` dependency states when local dependency checks are enabled.

Reset local service data and reapply migrations:

```bash
docker compose -f infra/docker/docker-compose.yml down -v
docker compose -f infra/docker/docker-compose.yml up -d --wait
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
- Antitrust/procurement-integrity checklist with source-backed, expert-review-required content
- Subcontractor flow-down tracker
- Basic reports
- Source-backed obligation library

## Compliance Content Note

This application structure is product and engineering scaffolding, not legal advice. Production content should be reviewed by qualified government contracts, labor, cybersecurity, CMMC, or finance experts depending on module scope.

The MVP is explicitly **No-CUI / compliance management only**. Do not add customer CUI storage, classified data handling, ITAR/export-controlled technical data handling, or production CUI handling claims until a separate architecture, shared responsibility matrix, customer terms, support model, and assessment posture are approved. Upload workflows should warn users and keep them prevented from uploading CUI.

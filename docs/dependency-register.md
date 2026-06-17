# Dependency Register

This register identifies the known GCCS dependencies for local development, tests, MVP runtime, source-backed compliance content, and deferred integrations. Use it when adding features, onboarding developers, reviewing deployment readiness, or deciding whether a dependency is required now or only planned.

## Runtime And Framework Dependencies

| Dependency | Version/source | Used by | Required now | Notes |
| --- | --- | --- | --- | --- |
| .NET SDK | `10.0.203` from `global.json` | API, domain, application, infrastructure, tests, tools | Yes | Roll-forward is `latestFeature`. |
| ASP.NET Core Web API | .NET shared framework plus `Microsoft.AspNetCore.*` packages | `apps/api` | Yes | API boundary, auth, OpenAPI, rate limits, tenant context. |
| React | `19.2.7` | `apps/web` | Yes | Authenticated SaaS workspace UI. |
| React DOM | `19.2.7` | `apps/web` | Yes | Browser rendering. |
| Vite | `latest` in web devDependencies | `apps/web` | Yes for web build/dev | Frontend dev server and production build. |
| TypeScript | `6.0.3` | `apps/web` | Yes | Frontend type checking. |
| PostgreSQL provider | `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.2` | `src/Gccs.Infrastructure` | Yes for persisted runtime | EF Core PostgreSQL adapter. |
| EF Core | `Microsoft.EntityFrameworkCore` `10.0.4` | `src/Gccs.Infrastructure` | Yes | Persistence and migrations. |
| JWT bearer auth | `Microsoft.AspNetCore.Authentication.JwtBearer` `10.0.7` | `apps/api` | Yes for production auth | Development auth shim is local-only. |
| OpenAPI | `Microsoft.AspNetCore.OpenApi` `10.0.7` | `apps/api` | Yes | API documentation contract. |
| Lucide React | `1.17.0` | `apps/web` | Yes | UI icons. |

## Local Infrastructure Dependencies

| Dependency | Local version/config | Used by | Required now | Notes |
| --- | --- | --- | --- | --- |
| PostgreSQL | Docker image `postgres:17`, host port `15432` | API persistence, migrations, tests with real DB when needed | Yes for local persistence | Default database `gccs`. |
| Redis | Docker image `redis:8`, host port `16379` | Cache/job placeholder | Required when `LocalDependencies:Enabled=true` | Health checked by API local dependency checks. |
| MinIO object storage | Docker image `minio/minio:latest`, host ports `19000/19001` | Evidence/document storage placeholder | Required when local dependency checks are enabled | Bucket `gccs-evidence-dev` created by `minio-init`. |
| MinIO client init | Docker image `minio/mc:latest` | Local bucket setup | Yes for local object-storage readiness | Keeps bucket health check available. |
| ClamAV | Docker image `clamav/clamav-debian:stable`, host port `13310` | Malware scanning placeholder | Required when local dependency checks are enabled | Production launch needs real scanner path or accepted exception. |
| Docker Compose | Local developer tool | Local services | Yes for full local stack | Defined in `infra/docker/docker-compose.yml`. |

## Test And Tooling Dependencies

| Dependency | Version/source | Used by | Required now | Notes |
| --- | --- | --- | --- | --- |
| dotnet-ef | `10.0.4` from `dotnet-tools.json` | Migrations and SQL scripts | Yes | Restored with `dotnet tool restore`. |
| xUnit | `2.9.3` | Backend tests | Yes | API/domain/application regression tests. |
| xUnit runner | `3.1.5` | Backend test execution | Yes | Visual Studio/test runner integration. |
| Microsoft.NET.Test.Sdk | `18.0.0` | Backend tests | Yes | Test host. |
| ASP.NET Core MVC testing | `Microsoft.AspNetCore.Mvc.Testing` `10.0.7` | API integration tests and verification tools | Yes | Test server and API harness. |
| EF Core InMemory | `10.0.4` | Tests and verification tools | Yes | Isolated persistence tests. |
| ESLint | `^9.39.4` | Frontend lint | Yes | Web quality gate. |
| Vitest | `^4.1.8` | Frontend tests | Yes | React/Vite test runner. |
| Testing Library React/Jest DOM/User Event | versions in `apps/web/package.json` | Frontend behavior tests | Yes | User-visible UI regression coverage. |
| jsdom | `^29.1.1` | Frontend tests | Yes | DOM test environment. |
| Gitleaks | CI tool reference | Secret scanning | CI dependency | Configured by workflow, not npm/NuGet. |

## Configuration Dependencies

| Configuration | Required where | Purpose |
| --- | --- | --- |
| `ConnectionStrings:GccsDatabase` | API, migrations, infrastructure repositories | PostgreSQL connection. |
| `Authentication:Authority` and `Authentication:Audience` | Production API | JWT validation. |
| `Security:DevelopmentAuth:Enabled` | Local development only | Enables dev auth shim. |
| `Cors:AllowedOrigins` | API | Restricts frontend origins. |
| `Security:RateLimiting` | API | Request rate limiting. |
| `LocalDependencies:Enabled` | Local/dev API | Enables dependency health checks. |
| `LocalDependencies:Redis:ConnectionString` | Local/dev API | Redis health/dependency config. |
| `LocalDependencies:ObjectStorage:*` | Local/dev API | Object storage endpoint, bucket, and credentials. |
| `LocalDependencies:MalwareScanner:*` | Local/dev API | Malware scanner host and port. |
| `VITE_API_BASE_URL` | Web app | API base URL for frontend calls. |

## Source And Compliance Content Dependencies

| Dependency/source | Used for | Required now | Notes |
| --- | --- | --- | --- |
| `packages/compliance-content/obligations/mvp.json` | MVP obligation seed content | Yes | Source-backed content package. |
| Acquisition.gov FAR/DFARS pages | Clause source URLs and review | Yes as governed source references | Used in content; production review required. |
| eCFR | 32 CFR Part 170 source reference | Yes as governed source reference | Used for CMMC-related obligations. |
| DoD CMMC resources | CMMC readiness reference | Yes as source reference | SME review required for customer-facing interpretation. |
| NIST CSRC | NIST SP 800-171 references | Yes as source reference | Rev. 2/Rev. 3 distinction must remain explicit. |
| SBA sources | Size standards, governing rules, certifications context | Identified | Direct integration deferred. |
| SAM.gov / GSA Entity API | Entity lookup and SAM profile assist | Deferred | Requires credentials/config, provenance, limits, stale-data handling. |
| NARA CUI Registry | CUI category reference | Identified | MVP must not store CUI; mapping integration deferred. |

## Planned Or Deferred Service Dependencies

| Dependency | Planned use | Status | Gate before enablement |
| --- | --- | --- | --- |
| Queue/background worker | Extraction, notifications, scanning, report jobs | Planned | Tenant-scoped job payloads, retry/poison handling, audit events. |
| Search index | Compliance content and tenant document metadata search | Planned | Tenant isolation, CUI/data-handling controls, source provenance. |
| AI/RAG service | Draft-only clause explanations, evidence suggestions, summaries | Deferred | Source citations, logging, review workflow, tenant CUI/data-handling decision. |
| Email provider | Invitations, reminders, notifications | Placeholder/local only | Provider selection, secrets management, delivery failure handling. |
| Production object storage | Evidence/document files | Planned | Encryption, malware scanning, retention/export/delete controls. |
| Production malware scanner | Upload scanning | Planned | Real scanner integration or explicit launch exception. |
| GovCloud/Government cloud | Regulated deployment tier | Deferred | Product decision, CUI-ready architecture, shared responsibility matrix. |
| SSO/SAML/SCIM | Enterprise identity | Deferred | Tier decision and identity-provider testing. |

## Dependency Rules

- Domain and application projects must not depend directly on EF Core, ASP.NET, React, object storage SDKs, queue SDKs, search SDKs, AI SDKs, or external API clients.
- Infrastructure adapters own database, storage, cache, queue, search, AI, and external API implementation details.
- Local-only credentials in `.env.example`, Docker Compose, and development appsettings must never be reused for production.
- Any new dependency that stores, processes, searches, exports, or transmits customer data must be reviewed for tenant isolation, RBAC, audit logging, CUI/data-handling posture, retention, and support impact.
- Any dependency used for compliance content or AI output must preserve source URL, review metadata, confidence, and customer-facing limitations.
- Production launch dependencies must appear in this register before launch approval.

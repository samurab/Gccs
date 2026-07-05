# Production Readiness Production Smoke Evidence

Story: PR-7.2 - Run Production Smoke Tests.

Smoke status: blocked; production deployment now reaches the deployed API health gate, but production runtime dependencies are not fully configured, so smoke tests cannot be truthfully marked passed.

Evidence date: 2026-07-03.

Evidence owner: QA owner.

Prerequisite deployment record: `docs/production-readiness-production-deployment-evidence.md`.

Approved production workflow: `.github/workflows/production.yml`.

Latest execution attempt: 2026-07-05.

## Architectural Assessment

Production smoke testing cannot be replaced by local unit tests, staging smoke evidence, or a documented CI/CD contract. That approach fails structurally because it does not exercise the production identity provider, production tenant data boundary, production secrets, production storage, production logs, production alerts, or the protected production environment.

Three failure modes addressed:

- Treating staging smoke results as production evidence can miss production-only identity, secret, storage, cache, migration, DNS, TLS, alert-routing, or RBAC failures.
- Recording a prose pass without command output, operator, timestamp, artifact, and evidence location can allow pilot onboarding after an unverified or failed production deployment.
- Running smoke tests with real customer content, CUI, secrets, or unrestricted logs would violate the No-CUI MVP posture and create unsupported compliance and incident-response exposure.

The corrected pattern is a production smoke evidence gate tied to the approved production workflow run. The gate must record pass/fail status for login, tenant access, RBAC denial, upload warning and blocking behavior, evidence upload, report generation, audit logging, logs, alerts, and `/health`, using synthetic or non-sensitive data only. Any critical failure blocks pilot onboarding.

## Required Smoke Setup

| Requirement | Required evidence | Current status |
| --- | --- | --- |
| Production deployment completed | Successful `.github/workflows/production.yml` run for launch candidate `gccs-no-cui-mvp-lc-2026-07-03` with `production-deployment-evidence` artifact attached. | Blocked at production `/health`; run `28723800683` deployed API and web artifacts but health returned `degraded`. |
| Smoke data posture | Synthetic or non-sensitive tenant, user, upload, evidence, report, and audit data only. | Required before execution. |
| Smoke operator | Named QA or engineering operator with production smoke authorization. | Required before execution. |
| Smoke identity coverage | Owner/Admin plus at least one restricted role or approved smoke identity for RBAC denial. | Required before execution. |
| Logs and alerts | Production log query, alert route, and health signal observation captured without secrets or raw customer documents. | Required before execution. |
| Evidence location | Sanitized smoke transcript, health output, audit event references, alert observation, and defect/blocker table. | Required before execution. |

## 2026-07-04 Dispatch Attempt

Attempted command:

```bash
gh workflow run .github/workflows/production.yml \
  --repo samurab/Gccs \
  --ref codex/production-readiness-pr-7-2-smoke-tests \
  -f launch_candidate_tag=gccs-no-cui-mvp-lc-2026-07-03
```

Result:

- Failed before deployment.
- GitHub API returned `HTTP 404: workflow .github/workflows/production.yml not found on the default branch`.
- `gh workflow list --repo samurab/Gccs --all` listed `Azure Static Web Apps CI/CD`, `CI`, and `Staging deployment`; it did not list `Production deployment`.
- `gh api repos/samurab/Gccs/environments` initially listed only `staging`; no `production` environment existed at that time.
- Repository secrets visible through the API are staging-only; no `AZURE_CREDENTIALS_GCCS_PRODUCTION`, `AZURE_STATIC_WEB_APPS_API_TOKEN_GCCS_PRODUCTION`, or `PRODUCTION_DATABASE_URL` secret is configured at the repository level.
- Production environment variable and secret queries returned `HTTP 404` because the `production` environment does not exist.

Disposition: PR-7.2 remains blocked. The production workflow must be present on the default branch, the protected GitHub `production` environment must exist, and required production variables/secrets must be configured before deployment or smoke testing can run.

## 2026-07-04 Environment Verification

Verification commands:

```bash
gh api repos/samurab/Gccs/environments
gh api repos/samurab/Gccs/environments/production/variables
gh api repos/samurab/Gccs/environments/production/secrets
gh workflow list --repo samurab/Gccs --all
gh pr view 2 --repo samurab/Gccs
```

Result:

- GitHub environment `Production` exists.
- Required production environment variables are present and non-empty: `PRODUCTION_API_APP_NAME`, `PRODUCTION_API_BASE_URL`, `PRODUCTION_WEB_BASE_URL`, `PRODUCTION_MSAL_CLIENT_ID`, `PRODUCTION_MSAL_TENANT_ID`, and `PRODUCTION_MSAL_API_SCOPE`.
- No production environment secrets are configured. Required missing secrets are `AZURE_CREDENTIALS_GCCS_PRODUCTION`, `AZURE_STATIC_WEB_APPS_API_TOKEN_GCCS_PRODUCTION`, and `PRODUCTION_DATABASE_URL`.
- `Production deployment` is still not visible in the remote workflow list because PR #2 is open and the workflow has not been merged to `main`.
- PR #2 is open, mergeable, and its CI checks passed; the skipped Azure Static Web Apps close job is expected for an open pull request.

Disposition: PR-7.2 remains blocked until PR #2 is merged to `main` and the three required production secrets are added to the GitHub `Production` environment.

## 2026-07-04 Production Deployment Runs

Run `28722707082`: failed before deployment. The production workflow was present on `main`, but the workflow checked out launch candidate tag `gccs-no-cui-mvp-lc-2026-07-03` before validating the production Terraform contract. That tag predates `infra/terraform/environments/production/main.tf`, so the guardrail failed before build, migration, or deployment. No production app deployment occurred.

Corrective action: PR #3 updated `.github/workflows/production.yml` to validate production deployment controls from `main` before checking out the approved launch candidate artifact source. PR #3 was merged on 2026-07-04.

Run `28722957153`: passed launch-candidate input validation, production deployment control validation, tag checkout, dependency restore, production artifact build, and idempotent migration script generation. It failed at `Apply production migrations through approved CI/CD` before Azure login or app deployment.

Failure detail: `psql` reported `could not translate host name "2798@gccs-postgres-production.postgres.database.azure.com" to address`. This indicates `PRODUCTION_DATABASE_URL` is malformed, most likely because the PostgreSQL password contains an unencoded `@` character before `2798`.

Disposition: PR-7.2 remains blocked until `PRODUCTION_DATABASE_URL` is corrected in the GitHub `Production` environment secret and the production deployment workflow completes successfully.

Run `28723353100`: passed launch-candidate input validation, production deployment control validation, tag checkout, dependency restore, production artifact build, idempotent migration script generation, and production migration application. It failed at `Login to Azure production subscription` before API or Static Web App deployment.

Failure detail: `azure/login@v2` reported `SyntaxError: Unexpected non-whitespace character after JSON at position 10 (line 1 column 11)`, which means `AZURE_CREDENTIALS_GCCS_PRODUCTION` is not valid service-principal JSON for the action.

Disposition: PR-7.2 remains blocked until `AZURE_CREDENTIALS_GCCS_PRODUCTION` is replaced with valid JSON output from `az ad sp create-for-rbac --sdk-auth` or an equivalent valid service principal credential for the production resource scope.

Run `28723713555`: passed launch-candidate validation, production control validation, artifact checkout, restore, build, migration generation, production migration application, Azure login, and API App Service deployment. It failed during Static Web App deployment with an unknown deployment exception. No production smoke approval or evidence artifact was produced.

Corrective action: the production Static Web App deployment token was refreshed from Azure Static Web Apps and stored in the GitHub `Production` environment secret `AZURE_STATIC_WEB_APPS_API_TOKEN_GCCS_PRODUCTION`.

Run `28723800683`: passed launch-candidate validation, production control validation, artifact checkout, restore, build, migration generation, production migration application, Azure login, API App Service deployment, and Static Web App deployment. It failed at `Run production health checks` because `GET /health` returned HTTP 503.

Health detail after production App Service non-secret runtime settings were applied and the app restarted: the API starts and returns structured health output with `status = degraded`, `dataPosture = No-CUI / compliance management only`, and unhealthy dependency signals for `postgresql`, `redis`, `object-storage`, and `background-jobs`.

Root cause: GitHub `PRODUCTION_DATABASE_URL` is sufficient for workflow migration execution only; it does not configure the deployed API. The production App Service also needs runtime settings for `ConnectionStrings__GccsDatabase`, `LocalDependencies__Redis__ConnectionString`, `Storage__AccountName` or `Storage__BlobServiceUri`, `Storage__UseManagedIdentity`, and storage container names. Azure verification also found no production Redis instance and no production storage account in `gccs-production-rg`.

Corrective action completed: non-secret App Service settings were applied for `AllowedHosts`, `Cors__AllowedOrigins__0`, `Authentication__Authority`, `Authentication__Audience`, `LocalDependencies__Enabled=false`, `Security__DevelopmentAuth__Enabled=false`, and log retention. The database password, Redis endpoint/key, and storage resource configuration remain unresolved production actions and are not stored in repository evidence.

Disposition: PR-7.2 remains blocked until production database, Redis, object storage, and background-job dependency health return `ok` through `/health`, then the full PR-7.2 smoke matrix is executed with synthetic or non-sensitive data only.

## Smoke Test Matrix

| Test case | Result | Evidence | Blocker disposition |
| --- | --- | --- | --- |
| TC-PR-7.2.1 | Blocked | Production login, tenant access, and RBAC denial tests require `GET /health` to pass first and require approved smoke identities. | Blocks pilot onboarding. |
| TC-PR-7.2.2 | Blocked | Upload warning/blocking, evidence upload, report generation, and audit logging smoke tests require healthy production database, scanner/storage path, tenant seed, and synthetic-only smoke files. | Blocks pilot onboarding. |
| TC-PR-7.2.3 | Blocked | Production logs and alerts are available for startup diagnosis, but health checks currently fail because PostgreSQL, Redis, object storage, and background-job coordination are not configured as healthy runtime dependencies. | Blocks pilot onboarding. |
| TC-PR-7.2.4 | Passed as gate | Pilot onboarding remains blocked when any critical production smoke test is blocked, failed, missing, or unreviewed. | Gate enforced by this artifact and `docs/production-readiness-pilot-onboarding.md`. |

## Required Manual Smoke Transcript

Attach the completed transcript here after production deployment. Do not include secrets, customer data, real CUI, raw file contents, unrestricted logs, or sensitive incident details.

| Field | Value |
| --- | --- |
| Production workflow run URL | `https://github.com/samurab/Gccs/actions/runs/28723800683` |
| Launch candidate tag | `gccs-no-cui-mvp-lc-2026-07-03` |
| Deployment artifact SHA | Launch candidate tag source `6c8927ec9cf79de977d76cb2594b87dd48f973bd`; deployed by workflow run `28723800683`. |
| Smoke start time UTC | Pending |
| Smoke operator | Pending |
| Production API base URL | `https://gccs-api-production-a7evdpg7fxd7e4e3.eastus-01.azurewebsites.net` |
| Production web base URL | `https://lemon-pond-093710c0f.7.azurestaticapps.net` |
| Synthetic smoke tenant ID | Pending non-sensitive identifier |
| Synthetic smoke users/roles | Pending non-sensitive identifiers |
| Health output location | Run `28723800683` failed health step; direct follow-up `GET /health` returned structured degraded output with unhealthy `postgresql`, `redis`, `object-storage`, and `background-jobs`. |
| Audit event references | Pending |
| Log/alert evidence location | Pending |
| Defects found | Missing production App Service runtime database setting and missing production Redis/storage resources or settings. |
| Final smoke result | Blocked |

## Pilot Onboarding Gate

Pilot onboarding must not start while any row in the smoke test matrix is `Blocked`, `Failed`, `Missing`, or `Unreviewed`. A production smoke pass requires all critical rows to be marked `Passed` with evidence locations, reviewer, and date.

Current gate result: blocked.

Required owner action: run the approved production deployment workflow, execute the PR-7.2 smoke matrix with synthetic or non-sensitive data only, attach sanitized evidence, then update this artifact from `Blocked` to the actual reviewed result.

## Hidden Risks And Edge Cases

- A production `/health` pass can still miss tenant-specific RBAC, upload, report, and audit regressions; the smoke matrix must exercise all listed workflows.
- Alert evidence can be misleading if notifications are routed to a test sink or disabled maintenance window; alert route and owner receipt must be recorded.
- Synthetic smoke tenants must be isolated from real customer tenants and must not reuse customer-like identifiers that could leak metadata.
- Upload smoke tests must use non-sensitive files and must not persist raw file contents in this artifact, logs, or audit notes.
- Production smoke may pass and later drift if secrets, identity scopes, feature flags, content packages, or infrastructure are changed outside the approved workflow.

## Automated Verification

Automated document validation is in `tests/Gccs.Api.Tests/ProductionReadinessChecklistTests.cs`:

- `TC_PR_7_2_Production_smoke_evidence_blocks_pilot_until_real_smoke_passes`
- `TC_PR_7_2_Smoke_gate_requires_no_cui_synthetic_data_and_operational_signals`

Suggested verification command:

```bash
dotnet test tests/Gccs.Api.Tests/Gccs.Api.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~ProductionReadinessChecklistTests"
```

## Consequences

- PR-7.2 is not complete because production smoke execution requires external production deployment access and operational telemetry.
- PR-7.3 remains blocked until this artifact records a reviewed production smoke pass.
- PR-8 stories remain blocked until controlled pilot onboarding begins with PR-7.2 evidence attached.
- The No-CUI posture remains unchanged; no real CUI, classified data, export-controlled data, credentials, sensitive personal data, or unrestricted logs are authorized for smoke testing.

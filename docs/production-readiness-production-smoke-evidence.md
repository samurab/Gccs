# Production Readiness Production Smoke Evidence

Story: PR-7.2 - Run Production Smoke Tests.

Smoke status: partially passed; production deployment, `/health`, login, tenant access, RBAC denial, No-CUI acknowledgement, upload guardrails, report generation, and audit visibility passed with synthetic-only production data. Byte-level evidence upload remains blocked because the production malware scanner endpoint is not configured and the app correctly fails closed.

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
| Production deployment completed | Successful `.github/workflows/production.yml` run for launch candidate `gccs-no-cui-mvp-lc-2026-07-03` with `production-deployment-evidence` artifact attached. | Passed in run `28746053336`; artifact records migration, API deploy, web deploy, and health checks passed. |
| Smoke data posture | Synthetic or non-sensitive tenant, user, upload, evidence, report, and audit data only. | Passed. Smoke tenant `GCCS Production Smoke Tenant` is `NoCui`; artifact records `containsCustomerData=false` and `containsCui=false`. |
| Smoke operator | Named QA or engineering operator with production smoke authorization. | Engineering/QA smoke executed from signed-in production browser session for the approved production smoke account. |
| Smoke identity coverage | Owner/Admin plus at least one restricted role or approved smoke identity for RBAC denial. | Passed. Account is restored to `Owner`; RBAC denial was verified through audited temporary `Contributor` downgrade returning `403 permission_denied`. |
| Logs and alerts | Production log query, alert route, and health signal observation captured without secrets or raw customer documents. | Partially passed. App Service filesystem application logs, HTTP logs, and failed-request tracing are enabled; dashboard alert and scanner-failure audit were observed. External alert owner receipt remains pending. |
| Evidence location | Sanitized smoke transcript, health output, audit event references, alert observation, and defect/blocker table. | Attached at `output/playwright/production-readiness/pr-7.2/authenticated-production-smoke.json`. |

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

Failure detail: `psql` reported that it could not translate a malformed host name derived from the database URL. This indicates `PRODUCTION_DATABASE_URL` was malformed, most likely because the PostgreSQL password contained an unencoded `@` character. The malformed host fragment is intentionally omitted from this evidence to avoid recording credential-like material.

Disposition: PR-7.2 remains blocked until `PRODUCTION_DATABASE_URL` is corrected in the GitHub `Production` environment secret and the production deployment workflow completes successfully.

Run `28723353100`: passed launch-candidate input validation, production deployment control validation, tag checkout, dependency restore, production artifact build, idempotent migration script generation, and production migration application. It failed at `Login to Azure production subscription` before API or Static Web App deployment.

Failure detail: `azure/login@v2` reported `SyntaxError: Unexpected non-whitespace character after JSON at position 10 (line 1 column 11)`, which means `AZURE_CREDENTIALS_GCCS_PRODUCTION` is not valid service-principal JSON for the action.

Disposition: PR-7.2 remains blocked until `AZURE_CREDENTIALS_GCCS_PRODUCTION` is replaced with valid JSON output from `az ad sp create-for-rbac --sdk-auth` or an equivalent valid service principal credential for the production resource scope.

Run `28723713555`: passed launch-candidate validation, production control validation, artifact checkout, restore, build, migration generation, production migration application, Azure login, and API App Service deployment. It failed during Static Web App deployment with an unknown deployment exception. No production smoke approval or evidence artifact was produced.

Corrective action: the production Static Web App deployment token was refreshed from Azure Static Web Apps and stored in the GitHub `Production` environment secret `AZURE_STATIC_WEB_APPS_API_TOKEN_GCCS_PRODUCTION`.

Run `28723800683`: passed launch-candidate validation, production control validation, artifact checkout, restore, build, migration generation, production migration application, Azure login, API App Service deployment, and Static Web App deployment. It failed at `Run production health checks` because `GET /health` returned HTTP 503.

Health detail after production App Service non-secret runtime settings were applied and the app restarted: the API starts and returns structured health output with `status = degraded`, `dataPosture = No-CUI / compliance management only`, and unhealthy dependency signals for `postgresql`, `redis`, `object-storage`, and `background-jobs`.

Root cause: GitHub `PRODUCTION_DATABASE_URL` is sufficient for workflow migration execution only; it does not configure the deployed API. The production App Service also needs runtime settings for `ConnectionStrings__GccsDatabase`, `LocalDependencies__Redis__ConnectionString`, `Storage__AccountName` or `Storage__BlobServiceUri`, `Storage__UseManagedIdentity`, and storage container names.

Corrective action completed: non-secret App Service settings were applied for `AllowedHosts`, `Cors__AllowedOrigins__0`, `Authentication__Authority`, `Authentication__Audience`, `LocalDependencies__Enabled=false`, `Security__DevelopmentAuth__Enabled=false`, and log retention. Production Redis `gccs-redis-production` was provisioned with private endpoint `10.0.2.5`, private DNS, and access-key authentication for the current app connection-string path. Production storage account `gccsprodstore01` was provisioned with containers `evidence`, `exports`, and `reports`, private endpoint `10.0.2.6`, private DNS, public network access disabled, App Service managed identity, and `Storage Blob Data Contributor`.

Current health status: `postgresql`, `redis`, `background-jobs`, and `object-storage` all return `ok` through `/health`. PR-7.2 remains blocked until the authenticated smoke workflow is executed with synthetic or non-sensitive data only.

Run `28746053336`: passed launch-candidate validation, production control validation, artifact checkout, restore, build, migration generation, production migration application, Azure login, API App Service deployment, Static Web App deployment, production health checks, evidence recording, and evidence upload. The `production-deployment-evidence` artifact records `result=deployment-and-health-checks-passed`. The `production-health.json` artifact records `status = ok`, `dataPosture = No-CUI / compliance management only`, and dependency statuses `ok` for `background-jobs`, `object-storage`, `postgresql`, and `redis`.

Current disposition: PR-7.2 production health smoke and authenticated workflow smoke mostly passed. The remaining PR-7.2 launch blocker is byte-level evidence upload: the API returns `503 malware_scanner_unavailable` and records scanner rejection because no production ClamAV-compatible malware scanner endpoint is configured. This is a correct fail-closed security outcome, but it blocks the story acceptance target for evidence upload and pilot onboarding.

## 2026-07-05 Authenticated Production Smoke

Sanitized evidence artifact: `output/playwright/production-readiness/pr-7.2/authenticated-production-smoke.json`.

Execution notes:

- The production API app registration was corrected to issue v2 access tokens by setting `requestedAccessTokenVersion = 2`; the signed-in browser then reacquired a v2 token with issuer `https://login.microsoftonline.com/8c934636-0c37-4a8f-9134-323bef993ef2/v2.0`.
- A synthetic No-CUI smoke tenant was bootstrapped in production as `GCCS Production Smoke Tenant` with tenant ID `8c934636-0c37-4a8f-9134-323bef993ef2`.
- The approved smoke/admin account resolved to user ID `09e188fa-befc-4b99-822b-d641767cb7b9` and was restored to `Owner` after smoke testing. The real email address is intentionally omitted from this committed artifact.
- RBAC denial was verified by temporarily downgrading the synthetic account to `Contributor`, confirming `/api/tenants/{tenantId}` returned `403 permission_denied`, then restoring `Owner`. The role changes were audit logged with correlation ID `PR-7.2-production-rbac-denial-smoke`.
- Smoke artifact records no token, no customer data, no CUI, and no raw file contents.

Observed pass signals:

- Login and API token validation passed with production MSAL and production API.
- Tenant access passed; `/api/me/access` returned `Owner`, `ManageTenant`, tenant ID `8c934636-0c37-4a8f-9134-323bef993ef2`, and the approved smoke account identity. The real email address is intentionally omitted from this committed artifact.
- Tenant detail passed; `/api/tenants/{tenantId}` returned `GCCS Production Smoke Tenant`, `status=Active`, `dataHandlingMode=NoCui`.
- Production UI loaded the smoke tenant and displayed `Active tenant: GCCS Production Smoke Tenant` and `Mode: NoCui`.
- No-CUI acknowledgement passed with notice version `no-cui-mvp-v1`.
- Synthetic evidence metadata creation passed with status `201`.
- Upload intent guardrail passed for allowed No-CUI metadata with status `201`, `validationStatus=accepted`, and `malwareScanStatus=scan-pending`.
- Missing No-CUI attestation was blocked with status `400` and validation key `noCuiAttestation`.
- Potential/real CUI upload metadata was blocked for the No-CUI tenant with status `403 tenant_data_handling_mode_restricted`.
- Compliance status report generation passed with status `201`; unauthenticated report generation returned `401 authentication_required`.
- Audit log read passed and showed report creation, evidence metadata creation, No-CUI acknowledgement, accepted upload intent, prohibited upload rejection, and malware-scan rejection entries.
- `/health` returned `status=ok` and dependency statuses `ok` for `background-jobs`, `object-storage`, `postgresql`, and `redis`.

Observed failure:

- Byte-level evidence file upload returned `503 malware_scanner_unavailable`.
- Audit log recorded `Evidence upload was rejected by malware scanning.`
- Production App Service settings do not include `MalwareScanning__Host` or `MalwareScanning__Port`.
- This preserves fail-closed upload behavior but blocks production smoke completion for evidence upload.

## Smoke Test Matrix

| Test case | Result | Evidence | Blocker disposition |
| --- | --- | --- | --- |
| TC-PR-7.2.1 | Passed | Authenticated production browser session for the approved production smoke account loaded the synthetic No-CUI tenant; RBAC denial returned `403 permission_denied` during audited temporary `Contributor` downgrade. | None for this row. |
| TC-PR-7.2.2 | Blocked for byte-level evidence upload | No-CUI acknowledgement, metadata creation, upload intent, blocked missing attestation, blocked potential CUI, compliance report generation, unauthenticated report denial, and audit visibility passed. Actual file upload returned `503 malware_scanner_unavailable` because no production scanner endpoint is configured. | Blocks pilot onboarding. |
| TC-PR-7.2.3 | Partially passed | Production workflow run `28746053336` recorded `/health` status `ok` with PostgreSQL, Redis, object storage, and background jobs all `ok`. App Service filesystem application logs, HTTP logs, failed request tracing, dashboard alert, and scanner-failure audit were observed. External alert owner receipt remains pending. | Blocks pilot onboarding until scanner/alert evidence is attached or formally excepted. |
| TC-PR-7.2.4 | Passed as gate | Pilot onboarding remains blocked when any critical production smoke test is blocked, failed, missing, or unreviewed. | Gate enforced by this artifact and `docs/production-readiness-pilot-onboarding.md`. |

## Required Manual Smoke Transcript

Attach the completed transcript here after production deployment. Do not include secrets, customer data, real CUI, raw file contents, unrestricted logs, or sensitive incident details.

| Field | Value |
| --- | --- |
| Production workflow run URL | `https://github.com/samurab/Gccs/actions/runs/28746053336` |
| Launch candidate tag | `gccs-no-cui-mvp-lc-2026-07-03` |
| Deployment artifact SHA | Launch candidate tag source `6c8927ec9cf79de977d76cb2594b87dd48f973bd`; deployed by workflow run `28746053336`. |
| Smoke start time UTC | `2026-07-05T16:33:37.294Z` |
| Smoke operator | Engineering/QA smoke session using approved production smoke account |
| Production API base URL | `https://gccs-api-production-a7evdpg7fxd7e4e3.eastus-01.azurewebsites.net` |
| Production web base URL | `https://lemon-pond-093710c0f.7.azurestaticapps.net` |
| Synthetic smoke tenant ID | `8c934636-0c37-4a8f-9134-323bef993ef2` (`GCCS Production Smoke Tenant`, `NoCui`) |
| Synthetic smoke users/roles | Approved production smoke account / `Owner`; temporary audited `Contributor` downgrade used only for RBAC denial verification and restored to `Owner` |
| Health output location | `production-health.json` artifact from run `28746053336`; status `ok`, service `gccs-api`, data posture `No-CUI / compliance management only`, dependencies `background-jobs`, `object-storage`, `postgresql`, and `redis` all `ok`. |
| Audit event references | Sanitized sampled audit entries are in `output/playwright/production-readiness/pr-7.2/authenticated-production-smoke.json`; RBAC role-change correlation ID `PR-7.2-production-rbac-denial-smoke`; bootstrap correlation ID `PR-7.2-production-smoke-bootstrap`. |
| Log/alert evidence location | App Service logging config observed: filesystem application logs `Information`, HTTP logs enabled, failed request tracing enabled. App dashboard alert `High-risk role assigned` and malware-scan rejection audit observed. External alert owner receipt remains pending. |
| Defects found | `PR72-PROD-SMOKE-002`: byte-level evidence upload fails closed with `503 malware_scanner_unavailable` because production malware scanner endpoint settings are absent. External alert owner receipt for scanner failure remains pending. |
| Final smoke result | Blocked by `PR72-PROD-SMOKE-002` |

## Pilot Onboarding Gate

Pilot onboarding must not start while any row in the smoke test matrix is `Blocked`, `Failed`, `Missing`, or `Unreviewed`. A production smoke pass requires all critical rows to be marked `Passed` with evidence locations, reviewer, and date.

Current gate result: blocked.

Required owner action: provision and configure a production ClamAV-compatible scanner endpoint through `MalwareScanning__Host` and `MalwareScanning__Port` (plus any required network path), rerun byte-level upload smoke with a synthetic non-sensitive file, and capture external alert owner receipt; or approve an explicit launch decision that byte-level evidence upload remains disabled while metadata/report smoke may pass.

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

- PR-7.2 is not complete because byte-level evidence upload fails closed without a production malware scanner endpoint and external alert owner receipt remains pending.
- PR-7.3 remains blocked until this artifact records a reviewed production smoke pass or a formal launch exception explicitly accepts disabled byte-level evidence upload.
- PR-8 stories remain blocked until controlled pilot onboarding begins with PR-7.2 evidence attached.
- The No-CUI posture remains unchanged; no real CUI, classified data, export-controlled data, credentials, sensitive personal data, or unrestricted logs are authorized for smoke testing.

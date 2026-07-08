# GCCS Production Deployment Runbook

This document explains how to deploy GCCS to production from GitHub to Azure. It is written for a new operator who has never deployed this application before.

Production is limited to the GCCS No-CUI MVP posture: **No-CUI / compliance management only**. Do not use this process to store, upload, process, report on, extract, export, or support real customer CUI, classified information, export-controlled data, ITAR data, sensitive government-furnished information, credentials, payroll, SSNs, bank or tax records, health or disability records, unrestricted security logs, or sensitive incident details.

## Architecture Assessment

The deployment process is not only a GitHub Actions button. It is a controlled release path across approvals, a launch candidate tag, GitHub protected environments, Azure runtime configuration, database migrations, API deployment, web deployment, dependency health checks, production smoke tests, alert evidence, and rollback readiness.

Three ways a simpler approach fails:

- Deploying from a branch or local workstation can release unapproved code, bypass protected environment review, skip launch-candidate evidence, and make rollback traceability weak or impossible.
- A successful API or web artifact upload can still fail at runtime when production App Service settings, database connectivity, Redis, object storage, authentication, CORS, managed identity, or malware scanner settings are missing.
- A green `/health` check alone does not prove login, tenant isolation, RBAC denial, No-CUI upload guardrails, report generation, audit logging, alert routing, or scanner-backed evidence upload.

The correct pattern is a gated production workflow:

1. Complete production-readiness evidence and approval records.
2. Deploy only an approved launch candidate tag.
3. Use the protected GitHub `production` environment.
4. Use production-only Azure resources, variables, and secrets.
5. Apply idempotent EF Core migrations through CI/CD.
6. Deploy the API and web app through `.github/workflows/production.yml`.
7. Verify `/health` and production dependencies.
8. Run authenticated production smoke tests with synthetic or non-sensitive data only.
9. Record evidence and keep rollback/support paths ready.

## Current Production Names

Verify these values before every deployment. They reflect the production readiness evidence available when this runbook was written.

| Item | Value |
| --- | --- |
| GitHub repository | `samurab/Gccs` |
| GitHub environment | `production` |
| Approved workflow | `.github/workflows/production.yml` |
| Approved launch candidate tag | `gccs-no-cui-mvp-lc-2026-07-08-2` |
| Launch candidate commit | `6c8927ec9cf79de977d76cb2594b87dd48f973bd` |
| API App Service | `gccs-api-production` |
| API base URL | `https://gccs-api-production-a7evdpg7fxd7e4e3.eastus-01.azurewebsites.net` |
| Web base URL | `https://lemon-pond-093710c0f.7.azurestaticapps.net` |
| Production database secret | `PRODUCTION_DATABASE_URL` |
| Azure deploy secret | `AZURE_CREDENTIALS_GCCS_PRODUCTION` |
| Static Web App token secret | `AZURE_STATIC_WEB_APPS_API_TOKEN_GCCS_PRODUCTION` |
| Data posture | `No-CUI / compliance management only` |
| Customer data mode | `no-cui-only` |

## Required Access

The operator needs:

- GitHub access to `samurab/Gccs`.
- Permission to run GitHub Actions workflows.
- Permission to view the `production` GitHub environment.
- Permission from the organization to request or approve the protected production deployment.
- Azure access to the production resource group and production App Service.
- Permission to view or update production App Service settings.
- Permission to read Azure Static Web App deployment token when rotating the token.
- Permission to verify logs, alerts, managed identity, storage, Redis, scanner, and database health.

Do not proceed if the operator cannot access the production workflow logs and production Azure resources. A blind deployment is not auditable.

## Required Tools

Install these locally if using command-line verification:

```bash
git --version
gh --version
az version
dotnet --version
node --version
npm --version
terraform version
```

Sign in before using Azure or GitHub commands:

```bash
gh auth status
az login
az account show
```

## Step 1 - Confirm The Local Repository State

Clone the repository if needed:

```bash
git clone https://github.com/samurab/Gccs.git
cd Gccs
```

Confirm you are in the repository root:

```bash
pwd
git status --short
```

If `git status --short` shows local changes, do not overwrite them unless you own those changes. Production deployment must use the GitHub workflow and approved tag, not local uncommitted files.

## Step 2 - Read The Launch Evidence

Read these documents before deploying:

```bash
sed -n '1,220p' docs/production-readiness-checklist.md
sed -n '1,220p' docs/production-readiness-launch-approval-record.md
sed -n '1,220p' docs/production-readiness-launch-candidate-tag.md
sed -n '1,220p' docs/production-readiness-production-deployment-evidence.md
sed -n '1,220p' docs/production-readiness-production-smoke-evidence.md
sed -n '1,220p' docs/production-readiness-launch-gap-decisions.md
```

Confirm:

- The deployment is still within the No-CUI MVP posture.
- The launch candidate tag is approved.
- No active launch blocker applies to production deployment.
- Any accepted risk is documented with owner, mitigation, contingency, and status.
- Production smoke requirements are understood before pilot onboarding or broader use.

Stop if a required approval, tag, smoke gate, rollback path, support path, or No-CUI control is missing or outdated.

## Step 3 - Confirm The Production Workflow Exists

The production workflow must exist on the repository default branch. GitHub cannot dispatch a workflow that only exists on an unmerged branch.

```bash
gh workflow list --repo samurab/Gccs --all
```

Expected workflow:

```text
Production deployment
```

Inspect the workflow:

```bash
sed -n '1,260p' .github/workflows/production.yml
```

The workflow must:

- Run only by `workflow_dispatch`.
- Require `launch_candidate_tag`.
- Use GitHub environment `production`.
- Validate the input tag against `gccs-no-cui-mvp-lc-2026-07-08-2`.
- Validate No-CUI guardrails.
- Build API and web artifacts.
- Generate an idempotent EF Core migration script.
- Apply migrations through `PRODUCTION_DATABASE_URL`.
- Log in to Azure using `AZURE_CREDENTIALS_GCCS_PRODUCTION`.
- Deploy the API App Service.
- Deploy the Static Web App.
- Call production `/health`.
- Upload `production-deployment-evidence`.

Stop if the workflow has been changed to deploy branches directly, skip the launch candidate check, skip migrations, skip health checks, or use staging secrets.

## Step 4 - Confirm The Production Environment Contract

Validate the production Terraform contract:

```bash
terraform -chdir=infra/terraform/environments/production init -backend=false
terraform -chdir=infra/terraform/environments/production validate
```

The current contract is not a full resource provisioning script. It is a machine-checkable declaration that production must include:

- API.
- Web app.
- PostgreSQL database.
- Object storage.
- Redis/cache.
- Queue or background job coordination.
- Production secrets.
- Background jobs.
- Migrations.
- Health checks.
- Logs.
- Alerts.
- Rollback pattern.
- No-CUI customer data mode.

Stop if the contract no longer states production and No-CUI only.

## Step 5 - Verify GitHub Production Variables

Open GitHub:

1. Go to `https://github.com/samurab/Gccs`.
2. Select `Settings`.
3. Select `Environments`.
4. Select `production`.
5. Review environment variables.

Required variables:

| Variable | Purpose |
| --- | --- |
| `PRODUCTION_API_APP_NAME` | Azure App Service target. |
| `PRODUCTION_API_BASE_URL` | API health check target and web build API URL. |
| `PRODUCTION_WEB_BASE_URL` | GitHub environment URL and production web URL. |
| `PRODUCTION_MSAL_CLIENT_ID` | Production web authentication client ID. |
| `PRODUCTION_MSAL_TENANT_ID` | Production web authentication tenant ID. |
| `PRODUCTION_MSAL_API_SCOPE` | Production API scope requested by the web app. |

Optional command-line verification:

```bash
gh api repos/samurab/Gccs/environments/production/variables
```

GitHub displays variable values. Do not paste sensitive-looking values into tickets or public documents.

## Step 6 - Verify GitHub Production Secrets

In the same GitHub `production` environment, confirm these secrets exist:

| Secret | Purpose |
| --- | --- |
| `AZURE_CREDENTIALS_GCCS_PRODUCTION` | Service principal JSON used by `azure/login@v2`. |
| `AZURE_STATIC_WEB_APPS_API_TOKEN_GCCS_PRODUCTION` | Azure Static Web App deployment token. |
| `PRODUCTION_DATABASE_URL` | PostgreSQL connection URL used by the migration step. |

Command-line verification:

```bash
gh api repos/samurab/Gccs/environments/production/secrets
```

GitHub will show secret names and update timestamps, not secret values.

Common secret failures:

- `AZURE_CREDENTIALS_GCCS_PRODUCTION` must be valid JSON for `azure/login@v2`.
- `PRODUCTION_DATABASE_URL` must be a valid PostgreSQL URL. Special characters in the password, especially `@`, must be URL-encoded.
- The Static Web App token must come from the production Static Web App, not staging.

Do not store production secret values in the repository, screenshots, docs, support tickets, or chat logs.

## Step 7 - Verify Azure Runtime Settings

The GitHub workflow deploys artifacts and runs migrations. The deployed API also needs App Service runtime settings. The database URL used by the workflow migration step does not automatically configure the running App Service.

Resolve the production App Service resource group:

```bash
PRODUCTION_API_APP_NAME="gccs-api-production"
PRODUCTION_RESOURCE_GROUP="$(az webapp list \
  --query "[?name=='$PRODUCTION_API_APP_NAME'].resourceGroup | [0]" \
  --output tsv)"

test -n "$PRODUCTION_RESOURCE_GROUP"
echo "$PRODUCTION_RESOURCE_GROUP"
```

List production settings:

```bash
az webapp config appsettings list \
  --resource-group "$PRODUCTION_RESOURCE_GROUP" \
  --name "$PRODUCTION_API_APP_NAME" \
  --output table
```

Required posture and environment settings:

| Setting | Expected value |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `DOTNET_ENVIRONMENT` | `Production` |
| `Gccs__DataPosture` | `No-CUI / compliance management only` |
| `Security__DevelopmentAuth__Enabled` | `false` |
| `LocalDependencies__Enabled` | `false` |

Required runtime dependency settings include:

- `ConnectionStrings__GccsDatabase`.
- Redis connection settings used by the API.
- Storage account, Blob service URI, managed identity, and container settings.
- Authentication authority and audience.
- CORS allowed origin for `PRODUCTION_WEB_BASE_URL`.
- Malware scanner settings when evidence upload is enabled.

Do not print full connection strings or secrets to documentation. Use Azure Portal or masked CLI output for verification.

Restart the App Service after changing settings:

```bash
az webapp restart \
  --resource-group "$PRODUCTION_RESOURCE_GROUP" \
  --name "$PRODUCTION_API_APP_NAME"
```

## Step 8 - Verify Managed Identity, Storage, Redis, And Scanner

Confirm the API App Service has a managed identity:

```bash
az webapp identity show \
  --resource-group "$PRODUCTION_RESOURCE_GROUP" \
  --name "$PRODUCTION_API_APP_NAME"
```

Confirm the production storage containers exist:

```bash
az storage container list \
  --account-name "gccsprodstore01" \
  --auth-mode login \
  --query "[].name" \
  --output table
```

Expected containers:

- `evidence`
- `exports`
- `reports`

Confirm Redis is reachable from the API network path and configured in App Service settings. Confirm the malware scanner endpoint is reachable from the API when byte-level evidence upload is enabled.

The production smoke evidence records the scanner-backed path as private ClamAV-compatible scanning. If scanner health is unavailable, uploads must fail closed and pilot onboarding must remain blocked until the failure is resolved or the affected upload path is disabled.

## Step 9 - Run Pre-Deployment Validation

Run these checks before dispatching production:

```bash
dotnet restore Gccs.slnx
npm ci
dotnet build Gccs.slnx --configuration Release
npm run lint:web
npm run test:web
npm run build:web
dotnet test tests/Gccs.Api.Tests/Gccs.Api.Tests.csproj --configuration Release --no-restore
```

If time is limited, at minimum verify the production readiness tests:

```bash
dotnet test tests/Gccs.Api.Tests/Gccs.Api.Tests.csproj \
  --configuration Release \
  --filter "FullyQualifiedName~ProductionReadinessChecklistTests"
```

Do not deploy if the failing test is related to tenant isolation, RBAC, audit logging, No-CUI policy, migrations, content provenance, health checks, or production readiness evidence.

## Step 10 - Dispatch The Production Deployment

Use the GitHub website:

1. Go to `https://github.com/samurab/Gccs/actions`.
2. Select `Production deployment`.
3. Select `Run workflow`.
4. Enter the approved launch candidate tag:

```text
gccs-no-cui-mvp-lc-2026-07-03
```

5. Start the workflow.
6. Complete any required production environment approval.

Or use GitHub CLI:

```bash
gh workflow run ".github/workflows/production.yml" \
  --repo samurab/Gccs \
  --ref main \
  -f launch_candidate_tag=gccs-no-cui-mvp-lc-2026-07-08-2
```

Watch the run:

```bash
gh run list --repo samurab/Gccs --workflow "Production deployment" --limit 5
gh run watch --repo samurab/Gccs
```

The workflow should pass:

1. Validate approved launch candidate input.
2. Checkout production deployment controls.
3. Validate production CI/CD and No-CUI deployment guardrails.
4. Checkout approved launch candidate artifact source.
5. Setup .NET SDK.
6. Setup Node.js.
7. Restore dependencies.
8. Build production artifacts.
9. Generate idempotent production migration script.
10. Apply production migrations through approved CI/CD.
11. Login to Azure production subscription.
12. Deploy production API App Service.
13. Deploy production Static Web App.
14. Run production health checks.
15. Record production deployment evidence.
16. Upload production deployment evidence.

If the workflow fails, stop. Do not rerun blindly. Open the failed step logs, identify the failed dependency, fix it, and record the failure in the deployment evidence.

## Step 11 - Download And Inspect Deployment Evidence

After a successful workflow run, download the evidence artifact:

```bash
RUN_ID="$(gh run list \
  --repo samurab/Gccs \
  --workflow "Production deployment" \
  --json databaseId,conclusion \
  --jq 'map(select(.conclusion=="success"))[0].databaseId')"

gh run download "$RUN_ID" \
  --repo samurab/Gccs \
  --name production-deployment-evidence \
  --dir output/production-deployment-evidence
```

Inspect:

```bash
cat output/production-deployment-evidence/production-deployment-evidence.txt
cat output/production-deployment-evidence/production-health.json
```

Required evidence values:

- `artifact_tag=gccs-no-cui-mvp-lc-2026-07-03`.
- `environment=production`.
- `data_posture=No-CUI / compliance management only`.
- `customer_data_mode=no-cui-only`.
- `result=deployment-and-health-checks-passed`.
- Health `status` is `ok`.
- Health `service` is `gccs-api`.
- Health includes `postgresql`.
- Health includes `redis`.
- Health includes `object-storage`.
- Health includes `background-jobs`.

## Step 12 - Manually Verify Production Health

Run:

```bash
curl --fail --show-error --silent \
  "https://gccs-api-production-a7evdpg7fxd7e4e3.eastus-01.azurewebsites.net/health"
```

Treat these as deployment failures:

- HTTP `500`, `502`, `503`, or timeout.
- `status` is not `ok`.
- `dataPosture` is not `No-CUI / compliance management only`.
- Any required dependency is missing.
- PostgreSQL, Redis, object storage, or background jobs are degraded.

## Step 13 - Verify The Production Web App

Open:

```text
https://lemon-pond-093710c0f.7.azurestaticapps.net
```

Expected:

- The web app loads over HTTPS.
- The browser does not call `localhost`.
- Authentication uses production MSAL settings.
- API calls target the production API base URL.
- The UI reflects the No-CUI posture where applicable.

If the web app loads but API calls fail, check:

- `PRODUCTION_API_BASE_URL`.
- `VITE_API_BASE_URL` during the workflow build.
- API CORS settings.
- Authentication authority and audience.
- `PRODUCTION_MSAL_CLIENT_ID`.
- `PRODUCTION_MSAL_TENANT_ID`.
- `PRODUCTION_MSAL_API_SCOPE`.

## Step 14 - Run Authenticated Production Smoke Tests

Production smoke must use only synthetic or non-sensitive data. Do not upload real customer files or real CUI.

Use `docs/production-readiness-production-smoke-evidence.md` as the evidence template.

Minimum smoke matrix:

| Area | Required result |
| --- | --- |
| Login | Approved smoke account can sign in through production identity. |
| Tenant access | Smoke user resolves to a synthetic or approved non-sensitive production tenant. |
| Tenant mode | Tenant is `NoCui`. |
| RBAC denial | Restricted role receives expected denial for protected action. |
| No-CUI acknowledgement | User can acknowledge the current No-CUI notice. |
| Allowed metadata | Safe non-sensitive evidence metadata can be created. |
| Byte upload | Safe synthetic byte-level upload returns clean scanner result when upload is enabled. |
| Missing attestation | Upload without No-CUI attestation is blocked. |
| Potential CUI | Potential/real CUI metadata is blocked for No-CUI tenant. |
| Reports | Allowed report generation succeeds; unauthenticated report generation fails. |
| Audit logs | Relevant upload, report, role, denial, and acknowledgement actions are auditable. |
| Health | `/health` remains `ok`. |
| Logs | API logging is enabled without secrets or raw customer documents. |
| Alerts | Production `Http5xx` alert and receiver evidence are current. |

Record:

- Workflow run URL.
- Launch candidate tag.
- Artifact SHA.
- Smoke start time.
- Smoke operator.
- Production API URL.
- Production web URL.
- Synthetic tenant ID.
- Synthetic user or role context.
- Sanitized health output.
- Audit event references.
- Log and alert evidence location.
- Defects found.
- Final pass/fail decision.

Pilot onboarding remains blocked if any critical smoke row is failed, blocked, missing, or unreviewed.

## Step 15 - Record The Deployment

Update `docs/production-readiness-production-deployment-evidence.md` after a deployment attempt.

Record:

- Date and time.
- Operator.
- Workflow run URL.
- Run ID.
- Branch used to dispatch, normally `main`.
- Launch candidate tag.
- Artifact SHA.
- API base URL.
- Web base URL.
- Migration result.
- API deployment result.
- Web deployment result.
- Health result.
- Evidence artifact name.
- Failures and corrective actions.
- Whether production smoke passed or remains pending.

Do not record:

- Connection strings.
- Service principal JSON.
- Static Web App tokens.
- Access tokens.
- Secret values.
- Raw customer documents.
- Raw production file contents.

## Step 16 - Keep Support And Monitoring Active

Confirm support paths in `docs/production-readiness-support-runbooks.md` are active for:

- Prohibited upload.
- Suspected CUI.
- Tenant exposure.
- Access issue.
- Evidence failure.
- Report failure.
- Content correction.
- Security incident.
- Backup restore.
- Rollback.

Confirm production monitoring covers:

- API health.
- HTTP 5xx.
- PostgreSQL connectivity.
- Redis connectivity.
- Object storage connectivity.
- Background job degradation.
- Upload and malware scanner failures.
- Migration failures.
- Log availability.
- Alert receiver delivery.

If alert receiver ownership, notification routing, alert rules, or action groups change, recapture evidence.

## Step 17 - Rollback If Deployment Fails

Application rollback and database recovery are different operations.

For an API or web deployment failure with no unsafe schema change:

1. Stop promotion and pilot onboarding.
2. Preserve GitHub Actions logs and artifacts.
3. Identify the last known-good launch candidate or workflow run.
4. Redeploy the last known-good approved artifact through the production workflow.
5. Recheck `/health`.
6. Rerun production smoke for affected workflows.
7. Record rollback timing, owner, result, and evidence.

For a migration or database issue:

1. Stop deployment.
2. Do not automatically run EF `Down()` paths in production.
3. Preserve the generated migration script.
4. Determine whether the current application can safely run against the migrated schema.
5. If data or schema state is unsafe, use the approved backup/restore path.
6. Record source database, restore point, target, approval, health output, and teardown or final state.

Rollback must preserve tenant isolation, RBAC, audit logging, No-CUI policy, evidence traceability, and support escalation.

## Troubleshooting

| Symptom | Likely cause | Corrective action |
| --- | --- | --- |
| Workflow cannot be dispatched | Workflow is not on default branch or user lacks permission. | Confirm `.github/workflows/production.yml` exists on `main` and operator can run Actions. |
| Launch candidate validation fails | Wrong tag entered. | Use `gccs-no-cui-mvp-lc-2026-07-03` or create a new approved tag through the launch process. |
| Terraform validation fails | Production contract changed or Terraform missing. | Restore the production contract or fix the invalid Terraform before deployment. |
| `psql` cannot parse database host | Malformed `PRODUCTION_DATABASE_URL`, often unencoded special character. | URL-encode password characters and update the GitHub production secret. |
| Azure login fails with JSON parse error | `AZURE_CREDENTIALS_GCCS_PRODUCTION` is not valid JSON. | Replace with valid service principal JSON. |
| API deploy passes but `/health` returns `503` | Runtime App Service settings or dependencies are missing. | Check database, Redis, storage, scanner, authentication, CORS, and managed identity settings. |
| Web app calls localhost | Production web build used wrong API base URL. | Fix `PRODUCTION_API_BASE_URL`, rerun deployment, and verify build output. |
| Browser CORS errors | API does not allow the production web origin. | Update CORS allowed origin and restart the API. |
| Evidence upload fails with scanner unavailable | Malware scanner endpoint is missing or unreachable. | Restore scanner endpoint or disable byte upload until fixed; uploads must fail closed. |
| Storage health fails | Managed identity or storage configuration is wrong. | Verify containers, private endpoint, role assignment, and App Service storage settings. |
| Redis health fails | Redis is missing, unreachable, or wrong key/connection string is configured. | Verify Redis resource, network path, access key, and App Service setting. |
| Smoke login fails | MSAL app registration, tenant, scope, or token version is wrong. | Verify production MSAL variables and API app registration. |
| RBAC denial does not occur | Role assignment or authorization policy regressed. | Block pilot onboarding; investigate tenant/RBAC tests and audit logs. |

## Hidden Risks, Edge Cases, And Dependencies

- GitHub secrets are write-only from the UI; names can be verified, values cannot.
- The production workflow migrates the database through `PRODUCTION_DATABASE_URL`, but the API runtime still needs its own `ConnectionStrings__GccsDatabase` setting.
- Managed identity and private endpoint changes can take time to propagate; immediate health checks may fail until Azure control-plane changes settle.
- A single scanner instance is not highly available. Broader production use needs scanner HA, restart monitoring, image/version governance, signature update monitoring, and alerting.
- A successful `/health` response does not prove tenant-specific authorization, upload, report, or audit behavior. Authenticated smoke tests are mandatory.
- Static Web App deployment can succeed while the frontend is built with stale API or MSAL values.
- Database rollback is not equivalent to application redeploy. Destructive or incompatible migrations require explicit restore or forward-fix decisions.
- Compliance content remains source-backed and review-gated. Do not publish high-risk withheld records without the required review decision.
- Production evidence must be sanitized. Do not commit email addresses, tokens, customer data, file contents, unrestricted logs, or secret fragments.
- Solo-controlled pilot approval does not replace broader production separation-of-duties approval if the organization expands beyond that posture.

## Final Success Criteria

Production deployment is complete only when all of these are true:

- The `Production deployment` workflow completed successfully.
- The deployed tag is the approved launch candidate.
- Migrations completed through CI/CD.
- API App Service deployment completed.
- Static Web App deployment completed.
- `/health` returns HTTP 200.
- Health JSON reports `status = ok`.
- Health JSON reports `dataPosture = No-CUI / compliance management only`.
- Health JSON includes PostgreSQL, Redis, object storage, and background job statuses.
- Production web app loads over HTTPS and calls the production API.
- Authenticated production smoke tests pass with synthetic or non-sensitive data only.
- Alerts and support routes are active and evidenced.
- Deployment evidence is recorded without secrets or prohibited data.
- Rollback path is understood and available.

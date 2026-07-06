# GitHub To Azure Staging Deployment Runbook

This runbook explains how to prepare, deploy, verify, and troubleshoot the GCCS staging environment from the GitHub repository to Azure. It is written for a new operator who has never deployed this application before.

Staging is the production-like release verification environment for the No-CUI MVP. It must use synthetic-only data and staging-only secrets. Do not put production customer data, real CUI, classified data, export-controlled data, production uploads, production unrestricted logs, or production secrets into staging.

## Current Architecture Assessment

The staging deployment is split across GitHub Actions, Azure resource configuration, application runtime settings, database migrations, and post-deployment smoke checks. Treating any one of those pieces as the whole deployment process will fail.

Three concrete failure modes:

- The GitHub workflow can deploy the API and web artifacts successfully while the deployed API still fails at runtime because App Service settings, database, Redis, storage, authentication, or managed identity permissions are missing.
- A green Static Web App deployment does not prove the API is reachable, the database migrated, tenant authentication works, or No-CUI guardrails are active.
- A generated EF Core migration script does not prove the migration was safely applied or reversible; rollback must be planned separately from application redeploy.

The correct pattern is a gated release path:

1. Provision Azure staging resources outside the repository workflow.
2. Configure the GitHub `staging` environment with staging-only variables and secrets.
3. Deploy API and web artifacts through `.github/workflows/staging.yml`.
4. Verify `/health` dependency signals and No-CUI posture.
5. Import source-backed compliance content explicitly when the staging database is empty or rebuilt.
6. Run synthetic-only staging workflow evidence before production readiness approval.

## Important Names And URLs

| Item | Value |
| --- | --- |
| GitHub repository | `samurab/Gccs` |
| GitHub Actions environment | `staging` |
| Approved staging workflow | `.github/workflows/staging.yml` |
| Static Web App workflow | `.github/workflows/azure-static-web-apps-mango-rock-016ff040f.yml` |
| Azure resource group | `gccs-staging-rg` |
| API App Service | `gccs-api-staging-19984` |
| API base URL | `https://gccs-api-staging-19984.azurewebsites.net` |
| Static Web App | `gccs-web-staging-19984` |
| Web base URL | `https://mango-rock-016ff040f.7.azurestaticapps.net` |
| PostgreSQL server | `gccs-pg-staging-19984` |
| PostgreSQL database | `gccs` |
| Storage account | `gccsstaging19984` |
| Storage containers | `evidence`, `exports`, `reports` |
| Redis/cache | `gccs-redis-staging-19984` |
| Data posture | `No-CUI / compliance management only` |
| Allowed staging data | Synthetic-only |

## Prerequisites

Install and confirm these tools before starting:

```bash
git --version
gh --version
az version
dotnet --version
node --version
npm --version
terraform version
```

Required access:

- GitHub access to `samurab/Gccs`.
- Permission to view and run GitHub Actions.
- Permission to manage GitHub repository environments, variables, and secrets.
- Azure subscription access for `gccs-staging-rg`.
- Permission to read and update the staging API App Service configuration.
- Permission to read the staging Static Web App deployment token.
- Permission to read staging database connection information.

Recommended local checkout:

```bash
git clone https://github.com/samurab/Gccs.git
cd Gccs
git status --short
```

If `git status --short` shows local changes, do not overwrite them unless you own those changes.

## Step 1 - Confirm Staging Guardrails

Read the staging environment document:

```bash
sed -n '1,220p' docs/staging-environment.md
```

Confirm these rules before doing anything else:

- Staging is No-CUI / compliance management only.
- Staging uses synthetic-only data.
- Staging must not contain production customer data.
- Staging must not contain real customer CUI.
- Staging must not reuse production secrets, production storage buckets, production database snapshots, production unrestricted logs, or production customer uploads.

Stop if any stakeholder asks you to bypass these rules. That would invalidate staging evidence and create a security/compliance risk.

## Step 2 - Confirm Azure Resources Exist

Sign in to Azure:

```bash
az login
az account show
```

Set the expected resource group:

```bash
RESOURCE_GROUP="gccs-staging-rg"
```

Confirm the resource group exists:

```bash
az group show --name "$RESOURCE_GROUP" --query "{name:name, location:location}" --output table
```

Confirm the API App Service exists:

```bash
az webapp show \
  --resource-group "$RESOURCE_GROUP" \
  --name "gccs-api-staging-19984" \
  --query "{name:name, state:state, defaultHostName:defaultHostName}" \
  --output table
```

Confirm the Static Web App exists:

```bash
az staticwebapp show \
  --resource-group "$RESOURCE_GROUP" \
  --name "gccs-web-staging-19984" \
  --query "{name:name, defaultHostname:defaultHostname}" \
  --output table
```

Confirm PostgreSQL, storage, and the Redis/cache resource exist:

```bash
az postgres flexible-server show \
  --resource-group "$RESOURCE_GROUP" \
  --name "gccs-pg-staging-19984" \
  --query "{name:name, state:state, version:version}" \
  --output table

az storage account show \
  --resource-group "$RESOURCE_GROUP" \
  --name "gccsstaging19984" \
  --query "{name:name, allowBlobPublicAccess:allowBlobPublicAccess, publicNetworkAccess:publicNetworkAccess}" \
  --output table

az resource list \
  --resource-group "$RESOURCE_GROUP" \
  --name "gccs-redis-staging-19984" \
  --query "[].{name:name, type:type, location:location}" \
  --output table
```

If a resource does not exist, stop and provision it through the approved Azure infrastructure process before running deployment.

## Step 3 - Validate The Repository Deployment Contract

From the repository root, confirm the staging workflow exists:

```bash
test -f .github/workflows/staging.yml
```

Inspect the workflow:

```bash
sed -n '1,220p' .github/workflows/staging.yml
```

The workflow must do these things:

- Run on `workflow_dispatch`.
- Run on pushes to `main` and `release/**`.
- Use the GitHub Actions `staging` environment.
- Build the API with `dotnet publish apps/api/Gccs.Api.csproj --configuration Release`.
- Build the web app with `npm run build:web`.
- Generate an idempotent EF Core migration script.
- Validate No-CUI and synthetic-only staging guardrails.
- Log in to Azure with `AZURE_CREDENTIALS_GCCS_STAGING`.
- Deploy the API to `gccs-api-staging-19984`.
- Deploy the web build to the Static Web App.
- Call `GET /health` after deployment.
- Upload `staging-smoke-test-results`.

Also confirm the staging Terraform contract exists:

```bash
terraform -chdir=infra/terraform/environments/staging init -backend=false
terraform -chdir=infra/terraform/environments/staging validate
```

This Terraform file is a contract for expected staging services. It is not a full resource provisioning script.

## Step 4 - Configure GitHub Staging Variables

Open GitHub:

1. Go to `https://github.com/samurab/Gccs`.
2. Select `Settings`.
3. Select `Environments`.
4. Select or create the environment named `staging`.
5. Add these environment variables:

| Variable | Value |
| --- | --- |
| `STAGING_API_BASE_URL` | `https://gccs-api-staging-19984.azurewebsites.net` |
| `STAGING_WEB_BASE_URL` | `https://mango-rock-016ff040f.7.azurestaticapps.net` |

Optional command-line verification:

```bash
gh api repos/samurab/Gccs/environments/staging/variables
```

The workflow uses these variables to build the web app with the correct API URL and to display the deployment URL in GitHub Actions.

## Step 5 - Configure GitHub Staging Secrets

In the same GitHub `staging` environment, add these secrets:

| Secret | Purpose |
| --- | --- |
| `AZURE_CREDENTIALS_GCCS_STAGING` | JSON credentials used by `azure/login@v2` to deploy to Azure. |
| `AZURE_STATIC_WEB_APPS_API_TOKEN_MANGO_ROCK_016FF040F` | Deployment token for the staging Azure Static Web App. |

Do not store production credentials in the staging environment.

The Azure credentials secret must be valid service principal JSON. A typical shape is:

```json
{
  "clientId": "<app-registration-client-id>",
  "clientSecret": "<service-principal-secret>",
  "subscriptionId": "<azure-subscription-id>",
  "tenantId": "<azure-tenant-id>"
}
```

The service principal must have enough permission to deploy the staging API App Service. Scope it to the staging resource group when possible.

To fetch the Static Web App deployment token from Azure:

```bash
az staticwebapp secrets list \
  --resource-group "gccs-staging-rg" \
  --name "gccs-web-staging-19984" \
  --query "properties.apiKey" \
  --output tsv
```

Store the returned value as `AZURE_STATIC_WEB_APPS_API_TOKEN_MANGO_ROCK_016FF040F`.

Optional command-line verification that the secret names exist:

```bash
gh api repos/samurab/Gccs/environments/staging/secrets
```

GitHub will not show secret values. It should show the secret names and update timestamps.

## Step 6 - Configure API App Service Runtime Settings

The GitHub workflow deploys the compiled API artifact. It does not fully configure every runtime dependency. The API App Service must have staging runtime settings before health checks can pass.

Inspect current settings:

```bash
az webapp config appsettings list \
  --resource-group "gccs-staging-rg" \
  --name "gccs-api-staging-19984" \
  --output table
```

Required non-secret posture and environment settings:

| Setting | Expected value |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Staging` |
| `DOTNET_ENVIRONMENT` | `Staging` |
| `Gccs__DataPosture` | `No-CUI / compliance management only` |
| `Security__DevelopmentAuth__Enabled` | `false` |
| `LocalDependencies__Enabled` | `false` |

Required dependency settings depend on the exact Azure resource configuration, but the deployed API must be able to resolve:

- `ConnectionStrings__GccsDatabase`
- Redis connection settings used by local dependency health and background job coordination.
- Azure Blob Storage settings under `Storage__*`.
- Authentication authority and audience settings.
- CORS origin for the staging web app.

Set only staging values. Example shape:

```bash
az webapp config appsettings set \
  --resource-group "gccs-staging-rg" \
  --name "gccs-api-staging-19984" \
  --settings \
    ASPNETCORE_ENVIRONMENT="Staging" \
    DOTNET_ENVIRONMENT="Staging" \
    Gccs__DataPosture="No-CUI / compliance management only" \
    Security__DevelopmentAuth__Enabled="false" \
    LocalDependencies__Enabled="false" \
    Cors__AllowedOrigins__0="https://mango-rock-016ff040f.7.azurestaticapps.net"
```

Do not paste secret values into logs, screenshots, tickets, or documentation.

Restart the API after configuration changes:

```bash
az webapp restart \
  --resource-group "gccs-staging-rg" \
  --name "gccs-api-staging-19984"
```

## Step 7 - Confirm Managed Identity And Storage Access

The API needs permission to use private staging storage containers.

Check whether the API has a managed identity:

```bash
az webapp identity show \
  --resource-group "gccs-staging-rg" \
  --name "gccs-api-staging-19984"
```

Confirm storage containers exist:

```bash
az storage container list \
  --account-name "gccsstaging19984" \
  --auth-mode login \
  --query "[].name" \
  --output table
```

Expected containers:

- `evidence`
- `exports`
- `reports`

If storage access fails, assign the App Service managed identity `Storage Blob Data Contributor` on the staging storage account:

```bash
API_PRINCIPAL_ID="$(az webapp identity show \
  --resource-group "gccs-staging-rg" \
  --name "gccs-api-staging-19984" \
  --query principalId \
  --output tsv)"

STORAGE_SCOPE="$(az storage account show \
  --resource-group "gccs-staging-rg" \
  --name "gccsstaging19984" \
  --query id \
  --output tsv)"

az role assignment create \
  --assignee "$API_PRINCIPAL_ID" \
  --role "Storage Blob Data Contributor" \
  --scope "$STORAGE_SCOPE"
```

## Step 8 - Run The Staging Deployment Workflow

Use the GitHub website:

1. Go to `https://github.com/samurab/Gccs/actions`.
2. Select `Staging deployment`.
3. Select `Run workflow`.
4. Select the branch to deploy, usually `main`.
5. Start the workflow.

Or run it with GitHub CLI:

```bash
gh workflow run "Staging deployment" --repo samurab/Gccs --ref main
```

Watch the run:

```bash
gh run list --repo samurab/Gccs --workflow "Staging deployment" --limit 5
gh run watch --repo samurab/Gccs
```

The workflow should pass these phases:

1. Checkout repository.
2. Setup .NET SDK.
3. Setup Node.js.
4. Restore dependencies.
5. Build staging artifacts.
6. Generate idempotent migration script.
7. Validate No-CUI and no-production-data guardrails.
8. Validate staging infrastructure contract.
9. Login to Azure.
10. Deploy staging API App Service.
11. Deploy staging Static Web App.
12. Record staging deployment inputs.
13. Run staging smoke tests.
14. Upload staging smoke test results.

If the workflow fails, do not rerun blindly. Open the failed step logs and fix the failed dependency.

## Step 9 - Verify The Smoke Test Artifact

After the workflow finishes, download the smoke artifact:

```bash
RUN_ID="$(gh run list \
  --repo samurab/Gccs \
  --workflow "Staging deployment" \
  --json databaseId,conclusion \
  --jq 'map(select(.conclusion=="success"))[0].databaseId')"

gh run download "$RUN_ID" \
  --repo samurab/Gccs \
  --name staging-smoke-test-results \
  --dir output/staging-smoke-test-results
```

Inspect the health output:

```bash
cat output/staging-smoke-test-results/staging-health.json
```

The JSON must show:

- `status` is `ok`.
- `service` is `gccs-api`.
- `dataPosture` is `No-CUI / compliance management only`.
- Dependency `postgresql` is present.
- Dependency `redis` is present.
- Dependency `object-storage` is present.
- Dependency `background-jobs` is present.

Manual health check:

```bash
curl --fail --show-error --silent \
  "https://gccs-api-staging-19984.azurewebsites.net/health"
```

If `/health` returns `503` or a dependency is degraded, treat the deployment as failed even if the API artifact upload completed.

## Step 10 - Verify The Web App

Open the staging web URL:

```text
https://mango-rock-016ff040f.7.azurestaticapps.net
```

Expected result:

- The web app loads.
- The browser does not call `localhost`.
- Authentication points to the staging Microsoft identity configuration.
- API calls target `https://gccs-api-staging-19984.azurewebsites.net`.

If the web app loads but API calls fail, check:

- `VITE_API_BASE_URL` in the workflow.
- App Service CORS setting `Cors__AllowedOrigins__0`.
- Authentication authority and audience settings.
- Browser console network errors.

## Step 11 - Import Staging Compliance Content When Needed

Staging workflow deployment does not automatically seed source-backed compliance content. If the staging database is new, rebuilt, or empty, run the content import tool before testing clause tagging and obligation generation.

Do not use production customer data or ad hoc content files.

From a trusted operator workstation:

```bash
ConnectionStrings__GccsDatabase="$GCCS_STAGING_DATABASE_CONNECTION_STRING" \
dotnet run --project tools/Gccs.ContentImport/Gccs.ContentImport.csproj -- \
  --package-root "$PWD/packages/compliance-content" \
  --confirm-staging true
```

Expected successful output includes:

- `Clauses created/updated`
- `Mappings created/updated`
- `Obligations created/updated`
- `Compliance content import completed successfully.`

Verify a known clause exists:

```bash
curl --fail --show-error --silent \
  "https://gccs-api-staging-19984.azurewebsites.net/api/clauses?query=52.204-21" \
  -H "Authorization: Bearer $GCCS_STAGING_ACCESS_TOKEN"
```

The response should include `far-52-204-21`.

## Step 12 - Run Synthetic-Only Workflow Verification

After deployment and content import, verify the application workflow with synthetic-only data:

1. Confirm tenant access.
2. Invite or verify a synthetic user.
3. Verify role access.
4. Complete a synthetic company profile.
5. Create a synthetic non-CUI contract.
6. Confirm an allowed non-sensitive upload works only after No-CUI acknowledgement.
7. Confirm a synthetic prohibited upload is blocked.
8. Tag a source-backed clause manually.
9. Generate obligations.
10. Create a task.
11. Create evidence metadata and upload a safe synthetic file.
12. Generate a report.
13. Export audit logs.

Use `docs/production-readiness-staging-workflow-evidence.md` as the evidence template and historical reference.

Never upload actual CUI to prove blocking behavior. Use safe synthetic prohibited examples only.

## Step 13 - Record Evidence

Update or attach evidence in the appropriate production-readiness document:

- Deployment and `/health` smoke evidence: `docs/production-readiness-staging-smoke-evidence.md`
- Full staging workflow evidence: `docs/production-readiness-staging-workflow-evidence.md`
- Deployment, migration, and rollback evidence: `docs/production-readiness-deployment-migration-rollback-evidence.md`

Record at minimum:

- GitHub Actions run ID.
- Run URL.
- Commit SHA deployed.
- Branch deployed.
- Run conclusion.
- Deployment date and time.
- API URL.
- Web URL.
- Smoke artifact name.
- `/health` JSON output.
- Operator or reviewer.
- Any failed dependency and corrective action.

Do not record secret values.

## Troubleshooting

| Symptom | Likely cause | Fix |
| --- | --- | --- |
| `azure/login@v2` fails with JSON parse error | `AZURE_CREDENTIALS_GCCS_STAGING` is not valid JSON. | Replace the secret with valid service principal JSON. |
| API deploy succeeds but `/health` returns `503` | Runtime dependencies are missing or unreachable. | Check App Service settings, database connection, Redis, storage, and managed identity permissions. |
| `/health` lacks `postgresql`, `redis`, `object-storage`, or `background-jobs` | Wrong build, wrong environment, or dependency health configuration mismatch. | Confirm the staging workflow deployed the latest API and `ASPNETCORE_ENVIRONMENT=Staging`. |
| Web app calls `localhost` | The web build used the wrong API base URL. | Confirm `VITE_API_BASE_URL` is set in the workflow and rebuild/redeploy. |
| Browser CORS errors | API does not allow the staging web origin. | Set `Cors__AllowedOrigins__0` to `https://mango-rock-016ff040f.7.azurestaticapps.net` and restart the API. |
| Clause tagging finds zero clauses | Compliance content was not imported into staging. | Run `tools/Gccs.ContentImport` with `--confirm-staging true`. |
| Uploads stay `scan-pending` | Malware scanning configuration is not fully available or intentionally deferred. | Check the current malware scanning launch decision before treating uploads as usable evidence. |
| Storage health fails | Storage configuration or managed identity permission is missing. | Verify storage settings and `Storage Blob Data Contributor` role assignment. |
| Static Web App deployment fails | Deployment token is missing, stale, or scoped to the wrong app. | Refresh the Static Web App deployment token and update the GitHub secret. |

## Rollback Pattern

Application rollback and database recovery are separate.

For a failed API or web deployment with no schema incompatibility:

1. Stop promotion.
2. Preserve GitHub Actions logs.
3. Identify the last known-good commit or run.
4. Redeploy the last known-good API and web artifacts through the approved workflow.
5. Rerun `/health`.
6. Rerun synthetic workflow smoke checks.
7. Record the rollback evidence.

For migration or database issues:

1. Stop deployment.
2. Do not run automatic EF `Down()` paths as a production-style rollback.
3. Preserve migration logs and generated SQL.
4. Assess whether the application can safely run against the current schema.
5. Use approved backup/restore recovery if schema or data state is unsafe.
6. Record owner, approver, mitigation, contingency, and final disposition.

## Hidden Risks, Edge Cases, And Dependencies

- The workflow states that database, object storage, cache, queue, and secrets are provisioned outside the repository workflow. A new Azure environment will not work until those resources and settings exist.
- GitHub secrets are write-only from the UI; you can verify names and timestamps, not values.
- Static Web App has a separate workflow that can deploy the frontend on `main` pushes. Avoid confusing that frontend-only path with the full staging deployment workflow.
- A successful artifact deploy is not enough. `/health` must prove PostgreSQL, Redis, object storage, and background job dependencies.
- Compliance content import is explicit and idempotent, but it still depends on the operator using the staging database connection string.
- Authentication tokens for protected API checks require the staging tenant, audience, and consent path to be configured.
- CORS and web build variables must agree. Otherwise the web UI can deploy successfully but fail at runtime.
- Managed identity role assignments can take time to propagate. Retry health checks after a short delay if storage access was just granted.
- Do not use production backups to populate staging unless they are sanitized through an approved process. The default rule is synthetic-only.
- Do not paste connection strings, service principal secrets, access tokens, or raw customer documents into GitHub issues, logs, documentation, or evidence files.

## Final Success Criteria

Staging is ready when all of these are true:

- GitHub `Staging deployment` workflow completes successfully.
- The deployed commit is known and recorded.
- `GET /health` returns HTTP 200.
- Health JSON reports `status = ok`.
- Health JSON reports `dataPosture = No-CUI / compliance management only`.
- Health JSON includes `postgresql`, `redis`, `object-storage`, and `background-jobs`.
- The web app loads from `https://mango-rock-016ff040f.7.azurestaticapps.net`.
- The web app calls the staging API, not localhost.
- Source-backed compliance content is imported when clause tagging or obligation generation is required.
- Synthetic-only workflow evidence is recorded.
- No production data, real CUI, production secrets, production uploads, or production unrestricted logs are used.

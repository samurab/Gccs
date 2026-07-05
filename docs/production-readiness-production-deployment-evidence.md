# Production Readiness Production Deployment Evidence

Story: PR-7.1 - Deploy Production Through Approved CI/CD.

Deployment status: passed through approved CI/CD path.

Evidence date: 2026-07-03.

Evidence owner: Engineering lead.

Approved launch candidate tag: `gccs-no-cui-mvp-lc-2026-07-03`.

Launch candidate record: `docs/production-readiness-launch-candidate-tag.md`.

Approved production workflow: `.github/workflows/production.yml`.

Production environment contract: `infra/terraform/environments/production/main.tf`.

## Architectural Assessment

Production deployment cannot reuse staging workflow variables, staging resources, or local operator commands. That approach fails structurally because staging secrets, staging URLs, synthetic-only assumptions, and staging rollback evidence do not prove a repeatable production release path.

Three failure modes addressed:

- Manual deployment can deploy the wrong commit or unreviewed artifact because no protected environment approval binds the operator, tag, and workflow run.
- A staging-derived deployment can silently target the wrong database, storage, cache, queue, or secret source because production dependencies are not independently declared.
- Prose-only deployment evidence cannot prove migrations, No-CUI posture, health checks, logs, alerts, operator, artifact, result, or rollback traceability.

The corrected pattern is a dedicated production workflow with a protected `production` GitHub environment, production-scoped variables and secrets, approved launch-candidate tag validation, idempotent EF Core migration execution, API and web deployment, post-deploy `/health` checks, and uploaded deployment evidence.

## Preconditions Checked

| Requirement | Result | Evidence |
| --- | --- | --- |
| Approved launch candidate artifact | Passed | Tag `gccs-no-cui-mvp-lc-2026-07-03` points to `6c8927ec9cf79de977d76cb2594b87dd48f973bd`; see `docs/production-readiness-launch-candidate-tag.md`. |
| Approved production CI/CD path | Passed | `.github/workflows/production.yml` requires `workflow_dispatch`, checks the input tag against the approved launch candidate, and runs in GitHub environment `production`. |
| Production environment configuration | Passed | `infra/terraform/environments/production/main.tf` declares the production environment contract and required services. |
| Production secrets source | Passed as contract | `.github/workflows/production.yml` resolves production-only GitHub environment/repository secrets: `AZURE_CREDENTIALS_GCCS_PRODUCTION`, `AZURE_STATIC_WEB_APPS_API_TOKEN_GCCS_PRODUCTION`, and `PRODUCTION_DATABASE_URL`. Secret values are not stored in the repository. |
| Production No-CUI posture validation | Passed | Workflow validates `Gccs__DataPosture=No-CUI / compliance management only` and `PRODUCTION_CUSTOMER_DATA_MODE=no-cui-only`; Terraform contract rejects non-production and non-No-CUI modes. |
| Production migrations | Passed as path | Workflow generates and applies an idempotent EF Core migration script through CI/CD before deploy health checks. |
| Production storage, cache, queue, and background jobs | Passed as contract | Production Terraform contract declares `database`, `object_storage`, `cache`, `queue`, `secrets`, and `background_jobs`; workflow checks these strings before deployment. |
| Production health checks, logs, and alerts | Passed as contract | Workflow checks `/health` for API, database, Redis, object storage, and background jobs; Terraform contract declares `health_checks`, `logs`, and `alerts`. |
| Deployment evidence capture | Passed as path | Workflow uploads `production-deployment-evidence`, containing deployment time, artifact tag/SHA, operator, environment, workflow run URL, No-CUI posture, result, health output, and migration script. |
| Restore rehearsal production-launch dependency | Still gated | `PR41-RESTORE-001` remains accepted only for launch-candidate tagging; production customer launch remains blocked until restore evidence is attached or separately dispositioned. |

## Required Production CI/CD Inputs

| Input | Source | Purpose |
| --- | --- | --- |
| `PRODUCTION_API_APP_NAME` | GitHub production environment variable | Azure App Service target. |
| `PRODUCTION_API_BASE_URL` | GitHub production environment variable | API health and frontend build target. |
| `PRODUCTION_WEB_BASE_URL` | GitHub production environment variable | GitHub environment URL and deployed web URL. |
| `PRODUCTION_MSAL_CLIENT_ID` | GitHub production environment variable | Production web authentication configuration. |
| `PRODUCTION_MSAL_TENANT_ID` | GitHub production environment variable | Production web authentication configuration. |
| `PRODUCTION_MSAL_API_SCOPE` | GitHub production environment variable | Production API scope. |
| `AZURE_CREDENTIALS_GCCS_PRODUCTION` | GitHub production environment secret | Azure deployment identity. |
| `AZURE_STATIC_WEB_APPS_API_TOKEN_GCCS_PRODUCTION` | GitHub production environment secret | Static Web App deployment token. |
| `PRODUCTION_DATABASE_URL` | GitHub production environment secret | Database migration target. |

## Smoke Verification

| Test case | Result | Evidence |
| --- | --- | --- |
| TC-PR-7.1.1 | Passed | Production workflow checks `launch_candidate_tag` equals `gccs-no-cui-mvp-lc-2026-07-03` and checks out that tag. |
| TC-PR-7.1.2 | Passed | Production deployment path is `.github/workflows/production.yml` using GitHub environment `production`; manual ad hoc deployment remains prohibited. |
| TC-PR-7.1.3 | Passed as repository-verifiable contract | Workflow and Terraform contract check secrets source, migrations, storage, cache, queue, background jobs, health checks, logs, alerts, and No-CUI data posture. |
| TC-PR-7.1.4 | Passed as CI/CD evidence path | Workflow records deployment time, artifact, operator, environment, result, workflow run URL, health output, and migration script into the `production-deployment-evidence` artifact. |

## Deployment Execution Record

No production deployment run was executed from this local Codex session. The repository now contains the approved CI/CD path and machine-checkable deployment contract. Actual deployment must be triggered through GitHub Actions `Production deployment` with the protected `production` environment configured and reviewed.

Manual production deployment remains prohibited. Do not deploy production manually or through the staging workflow.

2026-07-04 dispatch attempt: `gh workflow run .github/workflows/production.yml --repo samurab/Gccs --ref codex/production-readiness-pr-7-2-smoke-tests -f launch_candidate_tag=gccs-no-cui-mvp-lc-2026-07-03` failed before deployment with `HTTP 404` because GitHub requires the workflow to exist on the default branch. Remote repository inspection also showed no GitHub `production` environment and no production variables/secrets available through the API.

2026-07-04 follow-up verification: GitHub environment `Production` exists and required production environment variables are present, but required production secrets are still absent and PR #2 remains open. The production workflow still cannot be dispatched from `main` until PR #2 is merged.

2026-07-04 production run `28722707082`: failed before deployment because the workflow validated `infra/terraform/environments/production/main.tf` after checking out launch candidate tag `gccs-no-cui-mvp-lc-2026-07-03`; that tag does not contain the production Terraform contract. No migrations or app deployment ran.

2026-07-04 corrective PR #3: `.github/workflows/production.yml` now validates production deployment controls from `main` before checking out the approved launch candidate artifact source.

2026-07-04 production run `28722957153`: passed guardrails, artifact checkout, restore, build, and migration-script generation, then failed applying migrations because `PRODUCTION_DATABASE_URL` was malformed. The error resolved a credential-derived fragment as the host, indicating the database password in the URL contained an unencoded `@` character. The malformed host fragment is intentionally omitted from this evidence to avoid recording credential-like material. No Azure login or app deployment ran.

2026-07-04 production run `28723353100`: passed guardrails, artifact checkout, restore, build, migration-script generation, and production migration application, then failed Azure login. `azure/login@v2` could not parse `AZURE_CREDENTIALS_GCCS_PRODUCTION` as JSON. No API or Static Web App deployment ran.

2026-07-05 production run `28723713555`: passed migrations, Azure login, and API App Service deployment, then failed Static Web App deployment with an unknown deployment exception. The production Static Web App deployment token was refreshed from Azure and stored in the GitHub `Production` environment secret.

2026-07-05 production run `28723800683`: passed migrations, Azure login, API App Service deployment, and Static Web App deployment, then failed production health checks because the deployed API returned HTTP 503. Direct follow-up verification confirmed the API now starts after runtime settings were applied, but `/health` returns `status = degraded`.

Runtime configuration finding: the GitHub secret `PRODUCTION_DATABASE_URL` configures the migration step only. The deployed App Service also requires `ConnectionStrings__GccsDatabase` for API runtime persistence. Production Redis, object storage, and the App Service database runtime setting have since been configured and now report `ok` in `/health`.

2026-07-05 production run `28746053336`: passed launch-candidate validation, production control validation, artifact checkout, restore, build, idempotent migration script generation, production migration application, Azure login, API App Service deployment, Static Web App deployment, production health checks, deployment evidence recording, and deployment evidence upload.

Evidence artifact: `production-deployment-evidence` from run `28746053336` records `artifact_tag=gccs-no-cui-mvp-lc-2026-07-03`, `artifact_sha=6c8927ec9cf79de977d76cb2594b87dd48f973bd`, `operator=samurab`, `environment=production`, API app `gccs-api-production`, API base URL `https://gccs-api-production-a7evdpg7fxd7e4e3.eastus-01.azurewebsites.net`, web base URL `https://lemon-pond-093710c0f.7.azurestaticapps.net`, `data_posture=No-CUI / compliance management only`, `customer_data_mode=no-cui-only`, and `result=deployment-and-health-checks-passed`.

Health artifact: `production-health.json` from run `28746053336` records `status = ok`, service `gccs-api`, data posture `No-CUI / compliance management only`, and `background-jobs`, `object-storage`, `postgresql`, and `redis` all with `status = ok`.

Closed blocker: `PR71-PROD-DEPLOY-001` is closed for the repository CI/CD path by `.github/workflows/production.yml` and `infra/terraform/environments/production/main.tf`. It is not evidence that a production workflow run has completed.

## Residual Gate

| Gate ID | Owner | Severity | Required action | Mitigation until closed | Target date | Current status |
| --- | --- | --- | --- | --- | --- | --- |
| PR41-RESTORE-001 | Engineering lead | High | Attach actual restore rehearsal evidence or separately approve a production-launch exception. | Do not start production customer launch or make recoverability claims until restore evidence exists. | Before production customer launch | Open |

## Consequences

- PR-7.1 repository implementation is resolved: the approved production CI/CD path, production environment contract, No-CUI guardrails, migration path, health checks, and evidence capture are present.
- PR-7.1 production execution evidence is resolved by successful workflow run `28746053336`.
- PR-7.2 remains dependent on authenticated production smoke testing with synthetic or non-sensitive data only.
- PR-7.2 smoke gate and required evidence fields are recorded in `docs/production-readiness-production-smoke-evidence.md`.
- PR-7.3 and PR-8 remain blocked until PR-7.2 production smoke tests pass.
- The No-CUI posture remains unchanged; no production real-CUI capability is authorized.

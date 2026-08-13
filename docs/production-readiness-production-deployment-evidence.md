# Production Readiness Production Deployment Evidence

Story: PR-7.1 - Deploy Production Through Approved CI/CD.

Deployment status: current approved candidate deployed successfully through the protected production CI/CD path; historical successful deployment evidence is retained below.

Current candidate execution status: `launch-candidate-2026-08-12-3` deployed successfully in production workflow run `31658858453`.

Latest evidence date: 2026-08-12. Historical evidence dates are retained below.

Evidence owner: Engineering lead.

Approved launch candidate tag: `launch-candidate-2026-08-12-3`.

Approved launch candidate manifest: `docs/release/approved-launch-candidate.json`.

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
| Approved launch candidate artifact | Passed | Manifest `docs/release/approved-launch-candidate.json` approves tag `launch-candidate-2026-08-12-3` at `098ff130654e69ad768d24a3f5078d0c659f95d2`; see `docs/production-readiness-launch-candidate-tag.md`. |
| Approved production CI/CD path | Passed | Run `31658858453` validated the manifest input, immutable tag SHA, protected production environment, No-CUI guardrails, and exact-candidate checkout. Current candidate `launch-candidate-2026-08-12-3` completed protected production workflow execution in run `31658858453`. |
| Production environment configuration | Passed | `infra/terraform/environments/production/main.tf` declares the production contract. Post-deployment live App Service settings were `Production` for both environment keys, development auth was explicitly `false`, authentication authority and audience were configured, and no deployment slots were active. |
| Production secrets source | Passed | Current candidate `launch-candidate-2026-08-12-3` resolved the required production environment secrets in run `31658858453` without exposing their values. Secret values are not stored in this evidence or the repository. |
| Production No-CUI posture validation | Passed | Run `31658858453` validated `Gccs__DataPosture=No-CUI / compliance management only` and `PRODUCTION_CUSTOMER_DATA_MODE=no-cui-only`. |
| Production migrations | Passed | Run `31658858453` generated and successfully applied the idempotent production migration script. |
| Production storage, cache, queue, and background jobs | Passed | Exact-candidate workflow and independent post-deployment `/health` checks returned `ok` for PostgreSQL, Redis, object storage, and background jobs. |
| Production health checks, logs, and alerts | Passed for exact-candidate health; historical alert evidence retained | Run `31658858453` passed `/health`; PostgreSQL, Redis, object storage, and background jobs returned `ok`. Alert delivery and email delivery were not repeated for this candidate. |
| Deployment evidence capture | Passed | Artifact `9165532390` records deployment time, runtime tag/SHA, operator, environment, result, health output, and migration script. |
| Restore rehearsal production-launch dependency | Closed | `PR41-RESTORE-001` is closed by restored-server health evidence and teardown confirmation; claims remain limited to the tested staging point-in-time restore path. |

## Required Production CI/CD Inputs

| Input | Source | Purpose |
| --- | --- | --- |
| `PRODUCTION_API_APP_NAME` | GitHub production environment variable | Azure App Service target. |
| `PRODUCTION_RESOURCE_GROUP` | GitHub production environment variable | Azure resource-group target for application settings and deployment. |
| `PRODUCTION_API_BASE_URL` | GitHub production environment variable | API health and frontend build target. |
| `PRODUCTION_WEB_BASE_URL` | GitHub production environment variable | GitHub environment URL and deployed web URL. |
| `PRODUCTION_MSAL_CLIENT_ID` | GitHub production environment variable | Production web authentication configuration. |
| `PRODUCTION_MSAL_TENANT_ID` | GitHub production environment variable | Production web authentication configuration. |
| `PRODUCTION_MSAL_API_SCOPE` | GitHub production environment variable | Production API scope. |
| `DEMO_REQUESTS_ENDPOINT` | GitHub production environment variable | Production-only Azure Communication Services endpoint. |
| `DEMO_REQUESTS_SENDER_ADDRESS` | GitHub production environment variable | Sender on the linked production Azure-managed domain. |
| `DEMO_REQUESTS_RECIPIENT_ADDRESS` | GitHub production environment variable | Internal demo-request notification recipient. |
| `AZURE_CREDENTIALS_GCCS_PRODUCTION` | GitHub production environment secret | Azure deployment identity. |
| `AZURE_STATIC_WEB_APPS_API_TOKEN_GCCS_PRODUCTION` | GitHub production environment secret | Static Web App deployment token. |
| `PRODUCTION_DATABASE_URL` | GitHub production environment secret | Database migration target. |

## Smoke Verification

| Test case | Result | Evidence |
| --- | --- | --- |
| TC-PR-7.1.1 | Passed | Production workflow checks `launch_candidate_tag` against `docs/release/approved-launch-candidate.json`, verifies the tag commit, and checks out that tag. |
| TC-PR-7.1.2 | Passed | Production deployment path is `.github/workflows/production.yml` using GitHub environment `production`; manual ad hoc deployment remains prohibited. |
| TC-PR-7.1.3 | Passed for deployment runtime and repository contract | Run `31658858453` passed secrets resolution, migration application, dependency health, and No-CUI checks; workflow and Terraform retain logs/alerts contracts. |
| TC-PR-7.1.4 | Passed with candidate-specific artifact | Artifact `9165532390` records deployment time, runtime tag/SHA, operator, environment, result, workflow run URL, health output, and migration script. |

## Deployment Execution Record

### 2026-08-12 demo-delivery-observability deployment

Production workflow run `31658858453` completed successfully. Release controls ran from merged main commit `38e28bfa7420cd581857b638252fba8adbaed668`; the workflow validated and deployed immutable runtime tag `launch-candidate-2026-08-12-3` at `098ff130654e69ad768d24a3f5078d0c659f95d2`.

Run results:

- Approved tag/SHA validation, protected-environment review, production controls, and No-CUI guardrails passed.
- Production API and web builds, idempotent migration generation/application, API App Service deployment, Static Web App deployment, `/health`, and evidence upload passed.
- Evidence artifact `9165532390` records deployment evidence at `2026-08-13T01:54:23Z`, runtime tag/SHA, operator `samurab`, `customer_data_mode=no-cui-only`, and `result=deployment-and-health-checks-passed`.
- Workflow health checks returned `status=ok`; PostgreSQL, Redis, object storage, and background jobs each returned `status=ok`.

### 2026-08-11 demo-scheduling-delivery deployment

Production workflow run `31549410176` completed successfully. Release controls ran from merged main commit `a09baefdfbfd3b6ca1d0c27542edd48f60c15ceb`; the workflow validated and deployed immutable runtime tag `launch-candidate-2026-08-11-1` at `4bcda833236bb448da561f7c2637bf8eb35cd265`.

Run results:

- Approved tag/SHA validation, protected-environment review, production controls, and No-CUI guardrails passed.
- Production API and web builds, idempotent migration generation/application, API App Service deployment, Static Web App deployment, `/health`, and evidence upload passed.
- Evidence artifact `9123767294` records deployment evidence at `2026-08-12T00:18:16Z`, runtime tag/SHA, operator `samurab`, `customer_data_mode=no-cui-only`, and `result=deployment-and-health-checks-passed`.
- Workflow and independent health checks returned `status=ok`; PostgreSQL, Redis, object storage, and background jobs each returned `status=ok`.
- Independent live configuration inspection found no production deployment slots, `ASPNETCORE_ENVIRONMENT=Production`, `DOTNET_ENVIRONMENT=Production`, and `Security__DevelopmentAuth__Enabled=false`; authentication authority and audience were configured. A development-auth header-spoof request returned `401`.
- An unauthenticated production browser check rendered the public `Schedule a live demo` dialog, preferred date/time input, time-zone text, and submit control. No form was submitted.

Verification limits and environmental differences:

- No production sign-in was performed. The authenticated platform-operator calendar, tenant access, RBAC, upload, report, and audit workflows were not re-executed because no production smoke identity was supplied.
- No live demo request or operator response was created, so Azure Communication Services provider acceptance and external mailbox placement were not re-proven. Provider acceptance would not guarantee inbox delivery.
- Historical authenticated PR-7.2 and alert-route smoke remains evidence only for its tested candidate and environment state; it is not candidate-specific execution for this release.
- The deployment used no real customer data or CUI and does not authorize broader customer launch, CUI processing, certification, government approval, legal advice, or independent professional approval.
- Database rollback is not automatic. No destructive rollback or production restore exercise was performed during this deployment.

### 2026-08-09 corrective production-authentication-copy deployment

Production workflow run `31331760629` completed successfully. Release controls ran from merged main commit `b63b80f8dbbbf883840fc453ce115022beb61c9d`; the workflow validated and deployed immutable runtime tag `launch-candidate-2026-08-09-2` at `e0d04a454854949f66287af5245bdd03c684d5fb`.

Run results:

- Approved tag/SHA validation, protected-environment review, production controls, and No-CUI guardrails passed.
- Production API and web builds, idempotent migration generation/application, API App Service deployment, Static Web App deployment, `/health`, and evidence upload passed.
- Evidence artifact `9043177041` records deployment time `2026-08-09T19:33:27Z`, the runtime tag and SHA, operator `samurab`, `customer_data_mode=no-cui-only`, and `result=deployment-and-health-checks-passed`.
- Workflow and independent health checks returned `status=ok`; PostgreSQL, Redis, object storage, and background jobs each returned `status=ok`.
- The live production bundle contains `FeDril workspace` and `access your FeDril workspace`. A cache-busted in-app browser check rendered both corrected strings on the sign-in screen.

Verification limits and environmental differences:

- The first browser navigation reused an older cached bundle and displayed stale staging copy. Direct live-asset inspection and a cache-busted navigation confirmed the deployed bundle is corrected; cache behavior remains a client-side operational consideration during releases.
- No production sign-in was performed for this corrective verification. Tenant access, RBAC, upload, report, audit, alert-delivery, and email-delivery workflows were not re-executed because no production smoke credential or active authenticated session was available.
- Historical authenticated PR-7.2 smoke remains evidence for its tested candidate and environment state; it does not prove those workflows for this corrective candidate.
- The deployment used no real customer data or CUI and does not authorize broader customer launch, CUI processing, certification, government approval, legal advice, or independent professional approval.
- Database rollback is not automatic. No destructive rollback or production restore exercise was performed during this deployment.

### 2026-08-03 demo-scheduler and discovery-asset deployment

Production workflow run `30866422228` completed successfully in 4 minutes 56 seconds. Release controls ran from merged main commit `f91f09c1b06aa26c7495afc66a752179d193e361`; the workflow validated and deployed immutable runtime tag `launch-candidate-2026-08-03-2` at `fec0276b6d2cba3629a874f9cf76cd6e5f6a36da`.

Run results:

- Approved tag/SHA validation, protected-environment review, production controls, and No-CUI guardrails passed.
- Production API and web builds, idempotent migration generation/application, Azure login, demo-request delivery configuration, API App Service deployment, Static Web App deployment, `/health`, and evidence upload passed.
- Evidence artifact `8876352569` contains the health result, migration script, and deployment record.
- An independent live health request returned `status=ok`; PostgreSQL, Redis, object storage, and background jobs each returned `status=ok`.
- On `https://www.fedril.com/demo`, the post-video call to action opened one scheduler dialog, transferred focus to the first-name field, and exposed one `datetime-local` field while remaining on `/demo`.

Verification limits and environmental differences:

- No form was submitted; no customer data, sales lead, or email delivery was created by this release verification.
- Provider delivery and mailbox placement were not exercised and remain outside this presentation-only check.
- Authenticated platform/RBAC, upload, report, alert-delivery, and email-delivery scenarios were not repeated during this deployment. Protected CI and prior production evidence remain historical coverage for those paths.
- The deployment used no real customer data or CUI and does not authorize broader customer launch, CUI processing, certification, government approval, legal advice, or independent professional approval.
- Database rollback is not automatic. No destructive rollback or production restore exercise was performed during this deployment.

### 2026-08-03 demo-request operations deployment

Production workflow run `30780409892` completed successfully in 4 minutes 42 seconds. Release controls ran from merged main commit `09c6c841dc79f5f7d1d1994fdf40782f5ecb21ac`; the workflow validated and deployed immutable runtime tag `launch-candidate-2026-08-03-1` at `7f6ed7f6c4bad1b2962291b5b4984fb92265acb8`.

Run results:

- Approved tag/SHA validation, protected environment review, production controls, and No-CUI guardrails passed.
- Production API and web builds passed; the generated web bundle contained no localhost API or development testing-context references.
- The idempotent migration script was generated and applied successfully, including the three additive demo-request migrations.
- Production-only Azure Communication Services settings were applied through managed identity; no staging email resource or connection string was reused.
- API App Service deployment, Static Web App deployment, `/health`, evidence recording, and artifact upload passed.
- Evidence artifact `8843490287` contains the health result, migration script, and deployment record.
- An independent live request returned `status=ok`; PostgreSQL, Redis, object storage, and background jobs each returned `status=ok`. The production web endpoint returned HTTP `200`.
- A synthetic No-CUI scheduled request using a release-specific plus-address at the configured owner mailbox was accepted at `2026-08-03T02:59:28Z`. The address is intentionally omitted from committed evidence. Application Insights recorded two ACS send calls returning `202` and successful operation polling returning `200`, covering the internal notification and requester acknowledgement.

Verification limits and environmental differences:

- The synthetic email request is release verification only and must not be treated as a sales lead.
- Provider success proves accepted send operations, not inbox placement; recipient filtering or quarantine remains outside application control.
- The deployment did not use real customer data or CUI and does not authorize broader customer launch, CUI processing, certification, government approval, legal advice, or independent professional approval.
- Authenticated tenant-role, upload, report, and alert-delivery scenarios were not repeated during this deployment. Protected CI and prior production evidence remain historical coverage for those paths.
- Database rollback is not automatic. No destructive rollback or production restore exercise was performed during this deployment.

### 2026-08-01 application release deployment

Production workflow run `30723228364` completed successfully in 4 minutes 19 seconds. Release controls ran from merged main commit `9da5d5de532cca967994081a6315b66367e7021f`; the workflow validated and deployed immutable runtime tag `launch-candidate-2026-08-01-1` at `77cf94ec1c130b5b094822e95995101fa38e0af0`.

Run results:

- Approved tag/SHA validation, production controls, and No-CUI guardrails passed.
- Production API and web builds passed.
- Idempotent migration generation and PostgreSQL application passed. No EF Core migration file changed relative to `launch-candidate-2026-07-31-1`.
- Azure login, API App Service deployment, Static Web App deployment, production health checks, evidence recording, and evidence upload passed.
- Evidence artifact `8825546199` records deployment time `2026-08-01T23:27:43Z`, runtime tag and SHA, operator `samurab`, environment `production`, `customer_data_mode=no-cui-only`, and `result=deployment-and-health-checks-passed`.
- The workflow health artifact and an independent live request returned `status=ok`; PostgreSQL, Redis, object storage, and background jobs each returned `status=ok`.
- The production web endpoint returned HTTP `200` with title `FeDril | GovCon Compliance Readiness Software` and referenced the deployed favicon.

Verification limits and environmental differences:

- The deployment did not use real customer data or CUI and does not authorize broader customer launch, CUI processing, certification, government approval, legal advice, or independent professional approval.
- Authenticated workspace, tenant-role, upload, report, and alert-delivery scenarios were not repeated during this deployment. Earlier production smoke evidence remains historical coverage.
- Database rollback is not automatic. No destructive rollback or production restore exercise was performed during this deployment.

### 2026-07-31 FeDril solo-controlled No-CUI pilot deployment

Production workflow run `30645647404` completed successfully in 4 minutes 13 seconds. Release controls ran from merged main commit `7a96f31e3d258c71b1f5c68b5b579735ea806f3a`; the workflow validated and deployed immutable runtime tag `launch-candidate-2026-07-31-1` at `1af3296b9b92ae650087dd5ce15471b98354b787`.

Dispatch used the approved workflow only:

```bash
gh workflow run .github/workflows/production.yml \
  --repo samurab/Gccs \
  --ref main \
  -f launch_candidate_tag=launch-candidate-2026-07-31-1
```

The protected `Production` environment approval was limited to: "Approved for solo-controlled No-CUI pilot production deployment only; no broader customer launch or CUI authorization."

Run results:

- Approved tag/SHA validation, production controls, and No-CUI guardrails passed.
- Production API and web builds passed.
- Idempotent migration generation and PostgreSQL application passed. No candidate migration file changed relative to the prior production tag.
- Azure login, API App Service deployment, Static Web App deployment, production health checks, evidence recording, and evidence upload passed.
- Evidence artifact `8799397884` records deployment time `2026-07-31T16:09:29Z`, runtime tag and SHA, operator `samurab`, environment `production`, `customer_data_mode=no-cui-only`, and `result=deployment-and-health-checks-passed`.
- The workflow health artifact and an independent live request returned `status=ok`; PostgreSQL, Redis, object storage, and background jobs each returned `status=ok`.
- The production web endpoint returned HTTP `200` with title `FeDril | GovCon Compliance Readiness Software`. A rendered in-app browser check found FeDril branding, the No-CUI posture, the real-CUI warning, and non-certification/legal-advice disclaimers; it found no visible `GCCS` text and no browser warning or error log entries.

Verification limits and environmental differences:

- Release controls were read from main commit `7a96f31e3d258c71b1f5c68b5b579735ea806f3a`; runtime artifacts were built from tag commit `1af3296b9b92ae650087dd5ce15471b98354b787` by design.
- The deployment did not use real customer data or CUI. It did not authorize broader customer launch, CUI processing, or independent professional approval.
- Authenticated workspace, tenant-role, upload, report, and alert-delivery scenarios were not repeated during this branding deployment. Earlier PR-7.2 evidence remains historical coverage, not a claim that those scenarios were re-executed for this candidate.
- Database rollback is not automatic. No destructive rollback or production restore exercise was performed during this deployment.

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

Closed blocker: `PR71-PROD-DEPLOY-001` is closed for the repository CI/CD path and for candidate-specific execution by successful production workflow run `30645647404`. This does not authorize broader customer launch or CUI processing.

## Residual Gate

| Gate ID | Owner | Severity | Required action | Mitigation until closed | Target date | Current status |
| --- | --- | --- | --- | --- | --- | --- |
| PR41-RESTORE-001 | Engineering lead | High | Attach actual restore rehearsal evidence or separately approve a production-launch exception. | Do not overclaim beyond the tested staging point-in-time restore path. | Before production customer launch | Closed on 2026-07-05 |

## Consequences

- PR-7.1 repository implementation is resolved: the approved production CI/CD path, production environment contract, No-CUI guardrails, migration path, health checks, and evidence capture are present.
- PR-7.1 production execution evidence includes successful historical run `28746053336` and candidate-specific run `30645647404` for `launch-candidate-2026-07-31-1`.
- PR-7.2 authenticated production smoke evidence is attached in `docs/production-readiness-production-smoke-evidence.md` and `output/playwright/production-readiness/pr-7.2/authenticated-production-smoke.json`.
- PR-7.2 scanner-backed production smoke passed after private ClamAV scanner setup; byte-level evidence upload returned `201`, `malwareScanStatus=clean`, and `isUsable=true`.
- PR-7.3 may begin next, subject to controlled pilot prerequisites and No-CUI restrictions; restore evidence and alert-route external evidence `PR72-ALERT-ROUTE-001` are attached.
- The No-CUI posture remains unchanged; no production real-CUI capability is authorized.

# Production Readiness Production Deployment Evidence

Story: PR-7.1 - Deploy Production Through Approved CI/CD.

Deployment status: blocked.

Evidence date: 2026-07-03.

Evidence owner: Engineering lead.

Approved launch candidate tag: `gccs-no-cui-mvp-lc-2026-07-03`.

Launch candidate record: `docs/production-readiness-launch-candidate-tag.md`.

## Architectural Assessment

Production deployment cannot reuse the staging workflow by renaming variables at execution time. That would couple production to staging secrets, staging URLs, staging resource assumptions, and staging-only guardrails.

Three failure modes:

- A manual or staging-based deployment can push the wrong artifact, environment variables, or tenant data posture because production secrets and deployment targets are not independently declared.
- Missing production infrastructure contracts for database, storage, cache, queue, secrets, logs, alerts, and health checks make deployment success unverifiable.
- Without a production CI/CD workflow and approval environment, deployment evidence cannot prove operator, artifact, migration, environment, result, or rollback traceability.

The correct pattern is a dedicated production deployment workflow with a protected `production` environment, production-scoped repository or environment secrets, explicit No-CUI posture validation, idempotent migration generation, deployment artifact recording, post-deploy health checks, and production smoke evidence.

## Preconditions Checked

| Requirement | Result | Evidence |
| --- | --- | --- |
| Approved launch candidate artifact | Passed | Tag `gccs-no-cui-mvp-lc-2026-07-03` points to `6c8927ec9cf79de977d76cb2594b87dd48f973bd`; see `docs/production-readiness-launch-candidate-tag.md`. |
| Approved production CI/CD path | Blocked | `.github/workflows` contains staging/static-web/CI workflows, but no production deployment workflow. |
| Production environment configuration | Blocked | `infra/terraform/environments` contains `dev` and `staging`, but no `production` environment contract. |
| Production secrets source | Blocked | GitHub repository secrets list contains staging credentials only; no production Azure credentials, production Static Web App token, or production publish profile is present. |
| Production No-CUI posture validation | Blocked | No production workflow exists to assert `Gccs__DataPosture=No-CUI / compliance management only` and reject real customer CUI. |
| Production migrations | Blocked | Staging workflow generates an idempotent migration script, but no production migration job or approval gate exists. |
| Production storage, cache, queue, and background jobs | Blocked | No production infrastructure contract or deployment record exists for these dependencies. |
| Production health checks, logs, and alerts | Blocked | Staging checks exist; production health/log/alert targets are not declared in CI/CD or infrastructure docs. |
| Restore rehearsal production-launch dependency | Blocked | `PR41-RESTORE-001` remains accepted only for launch-candidate tagging; production customer launch remains blocked until restore evidence is attached or separately dispositioned. |

## Commands Run

```bash
ls -la .github/workflows
rg -n "production|prod|AZURE_CREDENTIALS.*PROD|PRODUCTION|deploy production|environment:\\s*production|name: production" .github/workflows docs/staging-environment.md docs/software-delivery-plan.md docs/production-readiness-plan.md docs/production-readiness-roadmap.md
find infra -maxdepth 5 -type f -print
rg -n "production|prod|staging|environment|database|object_storage|cache|queue|secrets" infra -g "*.*"
gh variable list --repo samurab/Gccs
gh secret list --repo samurab/Gccs
```

Observed production deployment inputs:

- Repository variables: `STAGING_API_BASE_URL`, `STAGING_WEB_BASE_URL`.
- Repository secrets: `AZURE_CREDENTIALS_GCCS_STAGING`, `AZURE_STATIC_WEB_APPS_API_TOKEN_MANGO_ROCK_016FF040F`, `AZURE_WEBAPP_PUBLISH_PROFILE_GCCS_API_STAGING`.
- Production variables/secrets: not found.
- Production workflow: not found.
- Production Terraform/environment file: not found.

## Smoke Verification

| Test case | Result | Evidence |
| --- | --- | --- |
| TC-PR-7.1.1 | Passed for precondition check; approved launch candidate artifact is recorded. | `docs/production-readiness-launch-candidate-tag.md`. |
| TC-PR-7.1.2 | Blocked; production deployment cannot run through approved CI/CD because no production workflow exists. | `.github/workflows` inspection. |
| TC-PR-7.1.3 | Blocked; production secrets source, migrations, storage, cache, background jobs, health checks, logs, alerts, and No-CUI data posture are not declared for production. | GitHub variable/secret listing and `infra/terraform/environments` inspection. |
| TC-PR-7.1.4 | Blocked; no production deployment time, artifact, operator, environment, result, or evidence location can be truthfully recorded because deployment did not run. | This artifact. |

## Blocker

| Blocker ID | Owner | Severity | Required action | Mitigation until closed | Target date | Current status |
| --- | --- | --- | --- | --- | --- | --- |
| PR71-PROD-DEPLOY-001 | Engineering lead | Critical | Create a protected production CI/CD workflow and production environment contract covering artifact source, production secrets, No-CUI posture, migrations, storage, cache, queue, background jobs, health checks, logs, alerts, rollback, and deployment evidence. | Do not deploy production manually or through staging workflow. Keep pilot onboarding and production smoke blocked. | Before PR-7.1 can complete | Open |

## Consequences

- PR-7.1 cannot complete until production deployment infrastructure and secrets are available.
- PR-7.2, PR-7.3, PR-8.1, PR-8.2, and PR-8.3 remain blocked because they depend on a real production deployment.
- Manual deployment would bypass the required evidence chain and create release, security, rollback, and audit risk.
- The No-CUI posture remains unchanged; no production real-CUI capability is authorized.

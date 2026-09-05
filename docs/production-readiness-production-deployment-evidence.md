# Production Readiness Production Deployment Evidence

Story: PR-7.1 - Deploy Production Through Approved CI/CD.

Deployment status: current approved candidate deployed successfully through the protected production CI/CD path; historical successful deployment evidence is retained below.

Current candidate execution status: `launch-candidate-2026-09-04-2` deployed successfully in production workflow run `33941169705`.

Latest evidence date: 2026-09-04. Historical evidence dates are retained below.

Evidence owner: Engineering lead.

Approved launch candidate tag: `launch-candidate-2026-09-04-2`.

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
| Approved launch candidate artifact | Passed | Manifest `docs/release/approved-launch-candidate.json` approves tag `launch-candidate-2026-09-04-2` at `c467e33dc2bf0e645ffe0a5ca9759a25f5060727`; see `docs/production-readiness-launch-candidate-tag.md`. |
| Approved production CI/CD path | Passed | PR #83 CI run `33938225225`, main CI run `33939297049`, main staging run `33939296991`, Static Web Apps run `33939297035`, approval PR #84 CI run `33940168257`, approval-main staging run `33941160974`, and protected production run `33941169705` passed. Current candidate `launch-candidate-2026-09-04-2` completed protected production workflow execution in run `33941169705`. |
| Production environment configuration | Passed | `infra/terraform/environments/production/main.tf` declares the production contract. Post-deployment live App Service settings were `Production` for both environment keys, development auth was explicitly `false`, authentication authority and audience were configured, and no deployment slots were active. |
| Production secrets source | Passed | Current candidate `launch-candidate-2026-09-04-2` resolved the required production environment secrets in run `33941169705` without exposing their values. The previously exposed Redis credential was invalidated through an alternate-key rotation before deployment. |
| Production No-CUI posture validation | Passed | Run `33941169705` validated the production No-CUI deployment guardrails. |
| Production migrations | Passed | Run `33941169705` generated and applied the idempotent production migration script through approved CI/CD. |
| Production storage, cache, queue, and background jobs | Passed | Run `33941169705` production health checks passed after API and web deployment. |
| Production health checks, logs, alerts, and HubSpot sync | Passed for candidate health; historical external-integration evidence retained | Run `33941169705` passed production health checks. Authenticated workflow smoke, alerts, email delivery, and HubSpot writes were not re-executed for this candidate. |
| Deployment evidence capture | Passed | Artifact `9961924098` records deployment time, runtime tag/SHA, operator, environment, result, health output, and migration script. |
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
| `HUBSPOT_PRIVATE_APP_TOKEN` | GitHub production environment secret | HubSpot contact and company synchronization authentication. |

## Smoke Verification

| Test case | Result | Evidence |
| --- | --- | --- |
| TC-PR-7.1.1 | Passed | Production workflow checks `launch_candidate_tag` against `docs/release/approved-launch-candidate.json`, verifies the tag commit, and checks out that tag. |
| TC-PR-7.1.2 | Passed | Production deployment path is `.github/workflows/production.yml` using GitHub environment `production`; manual ad hoc deployment remains prohibited. |
| TC-PR-7.1.3 | Passed for deployment runtime and repository contract | Run `32747227383` passed secrets resolution, migration application, dependency health, and No-CUI checks; workflow and Terraform retain logs/alerts contracts. |
| TC-PR-7.1.4 | Passed with candidate-specific artifact | Artifact `9527732298` records deployment time, runtime tag/SHA, operator, environment, result, workflow run URL, health output, and migration script. |

## Deployment Execution Record

### 2026-09-04 MSAL account-selection deployment

Production workflow run `33941169705` completed successfully at `2026-09-05T03:16:00Z`. Release controls ran from merged approval commit `3b7ccebc90004a6830740d964aeb47a015c782e0`; the workflow validated and deployed immutable runtime tag `launch-candidate-2026-09-04-2` at `c467e33dc2bf0e645ffe0a5ca9759a25f5060727`.

Run results:

- Approved tag/SHA validation, protected-environment review, No-CUI guardrails, production Terraform validation, and Terraform verification without backend or live changes passed.
- Production artifacts built, an idempotent migration script was generated, production migrations were applied through approved CI/CD, and production email delivery was configured.
- The API App Service and Static Web App deployed successfully.
- Production health checks passed.
- Evidence artifact `9961924098` records the exact runtime tag/SHA, operator, environment, result, health output, and migration script.

Verification limits and posture:

- This deployment preserves the No-CUI-only posture. It is not CMMC certification, FedRAMP authorization, government approval, legal advice, or permission to process CUI.
- Authenticated production user-flow smoke was not re-executed outside the workflow health checks in this evidence update.

### 2026-09-04 future FedRAMP foundation deployment

Production workflow run `33916926687` completed successfully at `2026-09-04T20:40:43Z`. Release controls ran from merged approval commit `ea633908a6fd09d372bff3d28783236ca0178116`; the workflow validated and deployed immutable runtime tag `launch-candidate-2026-09-04-1` at `55ed0dd049b43bec4c19d98b24cdd81224c19c90`.

Run results:

- Approved tag/SHA validation, protected-environment review, No-CUI guardrails, and the production Terraform configuration check passed.
- The idempotent migration applied, then the API App Service and Static Web App deployed successfully.
- `/health` returned `status=ok`; PostgreSQL, Redis, object storage, and background jobs each returned `status=ok`.
- Evidence artifact `9953670252` records the exact runtime tag/SHA, operator `samurab`, `customer_data_mode=no-cui-only`, and `result=deployment-and-health-checks-passed`.
- An unauthenticated request to `/api/enterprise/fedramp/control-mappings` returned HTTP `401`, confirming the deployed route did not expose tenant records anonymously.

Credential-rotation evidence:

- Before deployment, the only production web-app Redis consumer was identified and confirmed to use the primary key over TLS.
- A fresh secondary key was generated, the API switched to that key, and production Redis health returned `ok` before the old primary key was regenerated.
- Both keys that existed before rotation were invalidated. Secret values were not printed, committed, or included in evidence.

Verification limits and posture:

- No authenticated production smoke identity was available, so tenant-scoped create/update/package workflows and role denials were not re-executed against production. CI PostgreSQL integration tests and staging deployments remain the candidate-specific evidence for those paths.
- Production Terraform was validated but not applied or imported. Scheduled drift enforcement remains disabled until remote state, import review, and `PRODUCTION_TERRAFORM_STATE_READY=true` are approved.
- This deployment establishes only engineering preparation for a possible future FedRAMP effort. It is not FedRAMP authorization, FedRAMP Ready status, a 3PAO assessment, agency approval, or permission to process CUI.

### 2026-09-03 approved candidate deployment

Production workflow run `33836903225` completed successfully at `2026-09-04T04:33:20Z`. Release controls ran from main commit `7c4c0927786b2d5fa80d8701caba2880716461ef`; the workflow validated and deployed immutable runtime tag `launch-candidate-2026-09-03-1` at `f4547893b7d8683eeaa147fd0b1ca43a1fa88eda`.

Run results:

- Approved tag/SHA validation, protected-environment review, production controls, required-secret checks, and No-CUI guardrails passed.
- Production API and web builds, idempotent migration generation/application, API App Service deployment, Static Web App deployment, `/health`, and evidence upload passed.
- Evidence artifact `9923675595` records the runtime tag/SHA, operator `samurab`, `customer_data_mode=no-cui-only`, and `result=deployment-and-health-checks-passed`.
- PostgreSQL, Redis, object storage, and background jobs each returned `status=ok`.

Verification limits:

- No authenticated production smoke identity was used, so tenant authorization and FedRAMP preparation workflows were not exercised against production.
- This historical deployment predates the future FedRAMP foundation candidate and does not provide evidence for PR #80 behavior.
- No real customer data or CUI was used, and the run does not establish FedRAMP authorization, certification, government approval, or broader customer launch approval.

### 2026-08-24 HubSpot demo-request synchronization deployment

Production workflow run `32747227383` completed successfully. Release controls ran from merged main approval commit `36a7f21dccd6360df90d8663c63332a077c49151`; the workflow validated and deployed immutable runtime tag `launch-candidate-2026-08-24-1` at `08545dd7eaf6c66a387d9d7f262cf9cddde1d742`.

Run results:

- Approved tag/SHA validation, protected-environment review, production controls, required-secret presence, and No-CUI guardrails passed.
- Production API and web builds, idempotent migration generation/application, HubSpot-enabled demo-request configuration, API App Service deployment, Static Web App deployment, `/health`, and evidence upload passed.
- Evidence artifact `9527732298` records deployment evidence at `2026-08-24T15:55:03Z`, runtime tag/SHA, operator `samurab`, `customer_data_mode=no-cui-only`, and `result=deployment-and-health-checks-passed`.
- Workflow health checks returned `status=ok` for PostgreSQL, Redis, object storage, and background jobs.

Live synthetic HubSpot verification:

- At `2026-08-24T16:57:53Z`, production accepted one No-CUI synthetic `POST /api/public/demo-requests` with HTTP `202`. The synthetic address was `fedril.step6.production.verify.20260824165747@example.com`; the message explicitly identified the request as production verification and instructed operators not to contact it.
- HubSpot account `247124975` returned exactly one matching contact, ID `540138987226`, created at `2026-08-24T16:58:12Z`. The contact contained acquisition source `Book a Demo`, outreach permission `Manual Only`, relationship status `Demo Interest`, interest level `High`, prospecting status `Meeting Requested`, the expected operator next action, next follow-up date `2026-08-25`, and source detail containing request ID `7d677ee83260402ca3f1fea636002ae2` and referral `Step 6 production verification`.
- HubSpot returned exactly one company, ID `341642726076`, with domain `example.com`, associated to the synthetic contact. The deployed application excludes `example.com` from its own company-upsert path, so the observed company creation and association are attributed to HubSpot portal-level automatic company association rather than FeDril's company upsert. This is an evidence-based inference from the deployed code and live record timestamps, not a provider audit log.
- The connected FeDril primary Google Calendar returned zero matching events between `2026-08-24T16:50:00Z` and `2026-09-01T00:00:00Z` when searched by the synthetic contact name, email, and company. The requested time remained unconfirmed, as designed.

Verification limits and environmental differences:

- HubSpot synchronization is enabled in production and disabled in staging. The live synthetic request proves token validity for the exercised contact/company operations, the required custom properties and enumeration values, one successful asynchronous CRM delivery, and one association path. It does not prove broader rate-limit behavior, long-running retry behavior, or every existing-record merge permutation.
- HubSpot portal-level automatic company association can create a company for a generic or reserved email domain even when FeDril intentionally skips its own company-upsert path. This portal behavior should be reviewed before using generic-domain submissions as a strict no-company invariant.
- No authenticated production operator identity was used, so the internal demo-request delivery status, tenant-role flows, RBAC denial, and audit visibility were not re-executed against production. Provider acceptance and external mailbox placement for the acknowledgement and internal notification were not independently verified.
- Alert delivery, production restore, and rollback were not re-executed for this candidate; historical evidence remains applicable only to the paths and dates it tested.
- The deployment used no real customer data or CUI and authorizes only the solo-controlled No-CUI pilot scope. It does not authorize broader customer launch, CUI processing, certification, government approval, guaranteed CRM delivery, secure CUI storage, legal advice, or independent professional approval.

### 2026-08-23 demo launcher admin-persona deployment

Production workflow run `32668679239` completed successfully. Release controls ran from merged main approval commit `b8aaa9f9713c7e966cb7070aa2f8c513e05bf9a7`; the workflow validated and deployed immutable runtime tag `launch-candidate-2026-08-23-1` at `8bcd6300ab854af28e8988639e4c24046c311b22`.

Run results:

- Approved tag/SHA validation, protected-environment review, production controls, and No-CUI guardrails passed.
- Production API and web builds, idempotent migration generation/application, API App Service deployment, Static Web App deployment, `/health`, and evidence upload passed.
- Evidence artifact `9500813730` records deployment evidence at `2026-08-23T21:54:08Z`, runtime tag/SHA, operator `samurab`, `customer_data_mode=no-cui-only`, and `result=deployment-and-health-checks-passed`.
- Workflow health checks returned `status=ok` for the deployed production application.

Verification limits and environmental differences:

- The candidate change is limited to development/demo-video launcher defaults and release-control metadata. Production authentication, authorization, tenant isolation, persistence, migrations, and No-CUI behavior are unchanged by the candidate source commit.
- No authenticated production smoke identity was used in this release execution, so demo-request form submission, tenant admin onboarding, clause workflows, owner exports, RBAC denial, audit visibility, and interactive local demo persona behavior were not re-executed against production.
- Alert delivery, email delivery, production restore, and rollback were not re-executed for this candidate; historical evidence remains applicable only to the paths and dates it tested.
- The deployment used no real customer data or CUI and authorizes only the solo-controlled No-CUI pilot scope. It does not authorize broader customer launch, CUI processing, certification, government approval, secure CUI storage, legal advice, or independent professional approval.

### 2026-08-18 demo and clause workflow deployment

Production workflow run `32104303520` completed successfully. Release controls ran from merged main approval commit `83cca4f04f69b16cfb7477c24afd8d3c9dd0cab3`; the workflow validated and deployed immutable runtime tag `launch-candidate-2026-08-18-1` at `d49f594f8277aacb501c9ad4c8906960750eebf2`.

Run results:

- Approved tag/SHA validation, protected-environment review, production controls, and No-CUI guardrails passed.
- Production API and web builds, idempotent migration generation/application, API App Service deployment, Static Web App deployment, `/health`, and evidence upload passed.
- Evidence artifact `9312716657` records deployment evidence at `2026-08-18T05:53:28Z`, runtime tag/SHA, operator `samurab`, `customer_data_mode=no-cui-only`, and `result=deployment-and-health-checks-passed`.
- Workflow health checks returned `status=ok` for the deployed production application.

Verification limits and environmental differences:

- No authenticated production smoke identity was used in this release execution, so demo-request form submission, clause search, clause attachment, tenant admin onboarding, owner exports, RBAC denial, and audit visibility were not re-executed against production.
- Alert delivery, email delivery, production restore, and rollback were not re-executed for this candidate; historical evidence remains applicable only to the paths and dates it tested.
- The deployment used no real customer data or CUI and authorizes only the solo-controlled No-CUI pilot scope. It does not authorize broader customer launch, CUI processing, certification, government approval, secure CUI storage, legal advice, or independent professional approval.

### 2026-08-16 owner-report PDF export deployment

Production workflow run `31968286655` completed successfully. Release controls ran from merged main approval commit `2583d1dd013f715f6fb4473a39e7d792419c27a3`; the workflow validated and deployed immutable runtime tag `launch-candidate-2026-08-16-1` at `d0fa9503c0487aacd54443f971c04982501fe408`.

Run results:

- Approved tag/SHA validation, protected-environment review, production controls, and No-CUI guardrails passed.
- Production API and web builds, idempotent migration generation/application, API App Service deployment, Static Web App deployment, `/health`, and evidence upload passed.
- Evidence artifact `9269133023` records deployment evidence at `2026-08-16T19:45:00Z`, runtime tag/SHA, operator `samurab`, `customer_data_mode=no-cui-only`, and `result=deployment-and-health-checks-passed`.
- Workflow and independent health checks returned `status=ok`; PostgreSQL, Redis, object storage, and background jobs each returned `status=ok`; the independent production web request returned HTTP `200`.

Verification limits and environmental differences:

- No authenticated production smoke identity was used in this release execution, so Owner export creation, status polling, PDF download/print, cross-role denial, and audit visibility were not re-executed against production.
- Alert delivery, email delivery, production restore, and rollback were not re-executed for this candidate; historical evidence remains applicable only to the paths and dates it tested.
- The deployment used no real customer data or CUI and authorizes only the solo-controlled No-CUI pilot scope. It does not authorize broader customer launch, CUI processing, certification, government approval, secure CUI storage, legal advice, or independent professional approval.

### 2026-08-15 audit-workspace deployment

Production workflow run `31912729330` completed successfully. Release controls ran from merged main commit `ffa8a56d0de9b6d7a0c63a566270e3ad6aeb25d4`; the workflow validated and deployed immutable runtime tag `launch-candidate-2026-08-15-1` at `50b2dd279f216f816b92fdbaf2c4d4be025ce4ea`.

Run results:

- Approved tag/SHA validation, protected-environment review, production controls, and No-CUI guardrails passed.
- Production API and web builds, idempotent migration generation/application, API App Service deployment, Static Web App deployment, `/health`, and evidence upload passed.
- Evidence artifact `9254183165` records deployment evidence at `2026-08-15T22:46:49Z`, runtime tag/SHA, operator `samurab`, `customer_data_mode=no-cui-only`, and `result=deployment-and-health-checks-passed`.
- Workflow and independent health checks returned `status=ok`; PostgreSQL, Redis, object storage, and background jobs each returned `status=ok`.

Verification limits and environmental differences:

- No authenticated production smoke identity was used in this release execution, so login, tenant access, RBAC denial, upload, report, and audit workflows were not re-executed against production.
- Alert delivery, email delivery, production restore, and rollback were not re-executed for this candidate; historical evidence remains applicable only to the paths and dates it tested.
- The deployment used no real customer data or CUI and authorizes only the solo-controlled No-CUI pilot scope. It does not authorize broader customer launch, CUI processing, certification, government approval, legal advice, or independent professional approval.

### 2026-08-14 tenant-subscription-lifecycle deployment

Production workflow run `31855513515` completed successfully. Release controls ran from merged main commit `e44726a54827472ab2d96ced24259f48f40a1707`; the workflow validated and deployed immutable runtime tag `launch-candidate-2026-08-14-1` at `7c18da518ab8bd21acd42a59c4747431298c1e29`.

Run results:

- Approved tag/SHA validation, protected-environment review, production controls, and No-CUI guardrails passed.
- Production API and web builds, idempotent migration generation/application, API App Service deployment, Static Web App deployment, `/health`, and evidence upload passed.
- Evidence artifact `9238947445` records deployment evidence at `2026-08-15T01:08:55Z`, runtime tag/SHA, operator `samurab`, and production workflow result.
- Workflow health checks completed successfully before evidence upload.

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

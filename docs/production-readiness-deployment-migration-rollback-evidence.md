# Production Readiness Deployment, Migration, And Rollback Evidence

Story: PR-4.2 - Attach Deployment, Migration, And Rollback Evidence.

Evidence date: 2026-07-02.

Review status: Deployment, staging smoke, migration-script generation, and application rollback simulation evidence attached; database rollback remains constrained to forward migration validation plus backup/restore recovery unless a reviewed down-migration plan is approved.

Launch disposition: Ready for accountable approval with documented rollback limitations. Production launch still depends on PR-4.1 restore rehearsal, PR-4.3 malware scanning decision, expert content disposition, and final launch approvals.

## Architectural Assessment

Deployment success is not rollback readiness. A launch package fails structurally if it treats a passing App Service deploy or `/health` response as proof that schema migration failure, data corruption, or destructive migration risk can be recovered.

Three scale and correctness failures in that approach:

- Application rollback can redeploy previous API and web artifacts, but it cannot automatically reverse a schema migration that changed or removed data.
- A generated migration script can be syntactically valid while still lacking an execution record, reviewer, failure handling, or rollback decision.
- A green `/health` check proves dependency reachability only; it does not prove tenant workflows, migration compatibility, report exports, or audit history survived the release.

The correct release pattern is evidence separation:

- Deployment evidence proves the approved CI/CD path built and deployed launch-candidate artifacts.
- Migration evidence proves the exact EF Core idempotent script source, environment, result, reviewer, and failure handling.
- Rollback evidence proves application rollback behavior and explicitly records database rollback limits.
- Irreversible or destructive migration risk must have owner, mitigation, contingency, approver, and launch disposition.

## Deployment And Staging Smoke Evidence

| Field | Evidence |
| --- | --- |
| Approved deployment path | `.github/workflows/staging.yml` |
| Staging environment | GitHub Actions `staging` environment |
| Resource group | `gccs-staging-rg` |
| API app | `gccs-api-staging-19984` |
| Web app | `gccs-web-staging-19984` / `mango-rock-016ff040f.7.azurestaticapps.net` |
| Deployment run | GitHub Actions run `28534289128` |
| Deployed commit | `f550d3ed9001ed614853ba3895de3165e1280014` |
| Run result | `success` |
| Smoke artifact | `staging-smoke-test-results/staging-health.json` |
| Staging workflow evidence | `docs/production-readiness-staging-workflow-evidence.md` |
| Staging smoke evidence | `docs/production-readiness-staging-smoke-evidence.md` |
| Result | Passed |
| Reviewer | Engineering lead |

Required smoke signals attached in `docs/production-readiness-staging-smoke-evidence.md`:

- `service = gccs-api`
- `status = ok`
- `dataPosture = No-CUI / compliance management only`
- dependency `postgresql`
- dependency `redis`
- dependency `object-storage`
- dependency `background-jobs`

## Migration Evidence

| Field | Evidence |
| --- | --- |
| Script source | EF Core `GccsDbContext` migrations in `src/Gccs.Infrastructure/Persistence/Migrations` |
| Script generation command | `dotnet tool run dotnet-ef migrations script --idempotent --project src/Gccs.Infrastructure/Gccs.Infrastructure.csproj --startup-project apps/api/Gccs.Api.csproj --context GccsDbContext --output output/production-readiness/deployment-migration-rollback/gccs-staging-migrations.sql` |
| Generated script path | `output/production-readiness/deployment-migration-rollback/gccs-staging-migrations.sql` |
| Script line count | `5388` |
| Script SHA-256 | `5931c70b457735687b5d5e7e21ceb4e843ce2fb6cb9ef083577d7c77f69a9b62` |
| Environment | Local verification of the staging deployment migration command; staging workflow generates the same class of idempotent script in `$RUNNER_TEMP/gccs-staging-migrations.sql` |
| Result | Passed: script generation completed successfully and the deploy script contains no `DROP TABLE`, `DROP COLUMN`, `TRUNCATE`, or `DELETE FROM` statements |
| Reviewer | Engineering lead |
| Failure handling | If script generation fails, the staging workflow fails before deploy. If migration execution fails, stop rollout, preserve logs, do not run post-deploy smoke as pass evidence, redeploy previous application artifacts only if schema compatibility is confirmed, and use PR-4.1 restore recovery if data/schema state is unsafe. |

Migration verification command executed for this story:

```bash
dotnet tool run dotnet-ef migrations script --idempotent \
  --project src/Gccs.Infrastructure/Gccs.Infrastructure.csproj \
  --startup-project apps/api/Gccs.Api.csproj \
  --context GccsDbContext \
  --output output/production-readiness/deployment-migration-rollback/gccs-staging-migrations.sql
```

Destructive statement check executed for this story:

```bash
rg -n "DROP TABLE|DROP COLUMN|TRUNCATE|DELETE FROM" \
  output/production-readiness/deployment-migration-rollback/gccs-staging-migrations.sql
```

Result: no matches.

## Rollback Evidence

Application rollback simulation is documented in `docs/production-readiness-checklist.md`.

| Field | Evidence |
| --- | --- |
| Simulation date | `2026-06-15` |
| Tested rollback behavior | Re-deploy previous known-good API and web artifacts after simulated degraded health |
| Health confirmation | `/health` returns API status `ok` with database, cache, storage, and background job signals |
| Detection target | 5 minutes from failed smoke test |
| Decision target | 10 minutes from failed smoke test |
| Application recovery target | 30 minutes when no destructive migration is involved |
| Result | Simulated for application rollback |
| Reviewer | Engineering lead |
| Limitation | Application rollback does not reverse schema or data changes. Database recovery depends on forward-compatible migrations, reviewed migration rollback notes, or PR-4.1 backup/restore evidence. |

Migration rollback notes:

- Treat the generated idempotent script as the approved forward migration artifact for staging and launch-candidate validation.
- Do not run EF `Down()` paths automatically in production; many `Down()` methods contain table, column, foreign-key, or index drops that are destructive by design.
- If a migration introduces destructive or irreversible behavior, require owner and approver acceptance before launch candidate tagging.
- For failed forward migration before customer traffic, stop deployment and restore the previous database state from the approved backup/restore runbook when schema safety is uncertain.
- For failed application deployment with compatible schema, redeploy previous known-good API and web artifacts and rerun `/health` plus tenant workflow smoke checks.

## Irreversible Migration Risk Decision

No new migration is introduced by PR-4.2.

Current launch-candidate forward script check found no `DROP TABLE`, `DROP COLUMN`, `TRUNCATE`, or `DELETE FROM` statements in `output/production-readiness/deployment-migration-rollback/gccs-staging-migrations.sql`.

| Risk ID | Scope | Owner | Mitigation | Contingency | Approver | Current status |
| --- | --- | --- | --- | --- | --- | --- |
| PR42-MIGRATION-001 | Existing EF migration `Down()` paths include destructive operations and are not valid automatic production rollback evidence. | Engineering lead | Use idempotent forward migration scripts, inspect launch-candidate migration diff, block destructive migration without explicit acceptance, and keep PR-4.1 restore evidence as the database recovery control. | Stop deployment, preserve migration logs, redeploy application only when schema compatible, or restore database from approved backup/restore evidence. | Product owner and engineering lead before launch candidate tagging if destructive forward migration is introduced. | Accepted as a documented limitation for current launch-candidate evidence; no destructive forward statements found in the generated script. |

## Smoke Test Results

| Test case | Result | Evidence | Defect or blocker disposition |
| --- | --- | --- | --- |
| TC-PR-4.2.1 | Passed | Staging workflow and smoke evidence are linked from this artifact, checklist, and launch closure evidence. | None. |
| TC-PR-4.2.2 | Passed | Migration evidence identifies script source, generated script path, environment, result, reviewer, and failure handling. | None for generation evidence; actual production execution remains a launch operation. |
| TC-PR-4.2.3 | Passed with limitation | Application rollback simulation notes and migration rollback notes are attached. | Database rollback is not automatic; recovery depends on forward-compatible migration validation or PR-4.1 restore evidence. |
| TC-PR-4.2.4 | Passed | Irreversible migration risk has owner, mitigation, contingency, approver condition, and current disposition. | `PR42-MIGRATION-001` remains a documented limitation, not a launch approval. |

## Automated Test Coverage

Automated document and artifact validation is in `tests/Gccs.Api.Tests/ProductionReadinessChecklistTests.cs`:

- `TC_PR_4_2_Deployment_migration_and_rollback_evidence_is_attached`
- `TC_PR_4_2_Migration_evidence_identifies_script_environment_result_reviewer_and_failure_handling`
- `TC_PR_4_2_Irreversible_migration_risk_has_owner_mitigation_contingency_and_approver`

Targeted verification command:

```bash
dotnet test tests/Gccs.Api.Tests/Gccs.Api.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~ProductionReadinessChecklistTests"
```

## Hidden Risks And Dependencies

- A generated migration script is not the same as an executed staging migration transcript for every future launch candidate; rerun this gate after any model change.
- The rollback simulation proves application artifact recovery, not arbitrary database point-in-time recovery.
- Existing migration `Down()` methods are development rollback code, not approved production rollback playbooks.
- PR-4.1 restore rehearsal remains a hard dependency for database recovery confidence.
- Staging smoke must continue to use synthetic-only data and must not import production customer data, real CUI, production secrets, unrestricted logs, or customer uploads.

# Production Readiness Backup And Restore Evidence

Story: PR-4.1 - Attach Backup And Restore Evidence.

Evidence date: 2026-07-05.

Review status: Backup evidence captured and restore rehearsal passed on 2026-07-05.

Launch disposition: `PR41-RESTORE-001` is closed for the staging launch-candidate restore rehearsal. Production recoverability claims may reference only the tested staging point-in-time restore path and must not claim geo-disaster recovery.

## Architectural Assessment

Backup configuration is not recovery evidence. The launch gate fails if it treats an enabled backup policy, retention setting, or earliest restore timestamp as proof that GCCS can recover tenant data.

Three scale and correctness failures in that approach:

- Backup retention can be enabled while restore permissions, networking, firewall rules, extension compatibility, or migration state make the restored database unusable.
- A backup artifact can age out before launch approval; a seven-day retention window requires restore evidence to reference a recoverable point in time inside the retention window.
- A restore can succeed at the cloud-provider control plane while application-level smoke checks fail because connection strings, migrations, seed content, tenant data, or health dependencies are inconsistent.

The correct pattern is a two-part release gate:

- Backup evidence proves the staging launch candidate has recoverable backup configuration.
- Restore evidence proves a short-lived restored server was created from that backup, checked with synthetic-only data, reviewed, and deleted.

## Backup Evidence

| Field | Evidence |
| --- | --- |
| Source environment | Staging |
| Resource group | `gccs-staging-rg` |
| Source server | `gccs-pg-staging-19984` |
| Source provider | Azure PostgreSQL Flexible Server |
| Captured at | `2026-07-01T00:00:00-04:00` |
| Server state | `Ready` |
| PostgreSQL version | `17` |
| Location | `East US 2` |
| Backup retention | `7` days |
| Earliest restore date | `2026-06-27T18:41:38.308382+00:00` |
| Geo-redundant backup | `Disabled` |
| Evidence location | `output/production-readiness/backup-restore/staging-postgres-backup-config.json` |
| Result | Passed for backup configuration evidence only |
| Reviewer | Engineering lead |

Backup verification command:

```bash
az postgres flexible-server show \
  --resource-group gccs-staging-rg \
  --name gccs-pg-staging-19984 \
  --query "{name:name,resourceGroup:resourceGroup,location:location,state:state,version:version,sku:sku.name,tier:sku.tier,backup:backup,storage:storage,fullyQualifiedDomainName:fullyQualifiedDomainName}" \
  --output json
```

## Restore Evidence

Current status: Executed and passed on 2026-07-05.

Disposition: A short-lived restored PostgreSQL Flexible Server was verified with synthetic-only application health evidence and deleted after review. The evidence proves the tested staging point-in-time restore path only; it does not prove geo-redundant disaster recovery.

Completed restore evidence:

| Required field | Current value | Launch blocker |
| --- | --- | --- |
| Restore date | `2026-07-05`; restored server was originally created at `2026-07-02T02:14:45.709560+00:00` and revalidated before teardown. | No |
| Environment | Staging restored PostgreSQL Flexible Server `gccs-pg-staging-restore-202607020214`. | No |
| Data set | Synthetic-only staging launch-candidate data; no customer data, real CUI, credentials, raw uploads, or unrestricted logs were captured. | No |
| Command or pipeline reference | Azure PostgreSQL restore server evidence and temporary network access evidence in `output/production-readiness/backup-restore/`. | No |
| Result | Passed: restored database contained `gccs`; API `/health` returned `ok` with PostgreSQL, Redis, object storage, malware scanner, and background jobs `ok`. | No |
| Reviewer | Engineering lead. | No |
| Evidence location | `output/production-readiness/backup-restore/restore-rehearsal-summary.json`, `restore-health.json`, `restore-database-list.json`, `restore-server-before-teardown.json`, and `restore-server-after-teardown.err`. | No |

Restore rehearsal command template:

```bash
RESTORE_SERVER="gccs-pg-staging-restore-$(date +%Y%m%d%H%M)"

az postgres flexible-server restore \
  --resource-group gccs-staging-rg \
  --name "$RESTORE_SERVER" \
  --source-server gccs-pg-staging-19984 \
  --restore-time "REPLACE_WITH_UTC_RESTORE_TIME"
```

Required restore verification after server creation:

```bash
ConnectionStrings__GccsDatabase="$RESTORED_STAGING_DATABASE_CONNECTION_STRING" \
dotnet run --project tools/Gccs.ContentImport/Gccs.ContentImport.csproj -- \
  --package-root "$PWD/packages/compliance-content" \
  --confirm-staging true

curl --fail --show-error --silent "$RESTORED_STAGING_API_BASE_URL/health"
```

Teardown command:

```bash
az postgres flexible-server delete \
  --resource-group gccs-staging-rg \
  --name "$RESTORE_SERVER" \
  --yes
```

## Smoke Test Results

| Test case | Result | Evidence | Defect or blocker disposition |
| --- | --- | --- | --- |
| TC-PR-4.1.1 | Passed | Backup configuration artifact exists for the staging launch candidate. | None for backup configuration evidence. |
| TC-PR-4.1.2 | Passed | Point-in-time restore server evidence, restored `gccs` database list, and restored API health output are attached. Backup creation alone is rejected as restore proof. | `PR41-RESTORE-001` closed by executed restore rehearsal. |
| TC-PR-4.1.3 | Passed | Restore date, environment, data set, command or pipeline reference, result, reviewer, and evidence location are recorded from the restored server. | Required restore fields are complete for the tested staging restore path. |
| TC-PR-4.1.4 | Passed | Launch checklist and launch closure evidence no longer keep restore rehearsal as a production-customer-launch blocker. | Production launch must still pass all other gates and approvals. |

## Automated Test Coverage

Automated document validation is in `tests/Gccs.Api.Tests/ProductionReadinessChecklistTests.cs`:

- `TC_PR_4_1_Backup_configuration_and_restore_rehearsal_are_evidenced`
- `TC_PR_4_1_Restore_evidence_requires_execution_metadata_not_backup_assertions`
- `TC_PR_4_1_Restore_rehearsal_closes_production_launch_blocker`

Targeted verification command:

```bash
dotnet test tests/Gccs.Api.Tests/Gccs.Api.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~ProductionReadinessChecklistTests"
```

## Accepted Risk

| Blocker ID | Owner | Severity | Required action | Mitigation until closed | Target date | Current status |
| --- | --- | --- | --- | --- | --- | --- |
| PR41-RESTORE-001 | Engineering lead | High | Obtain restore-window approval, create a short-lived restored PostgreSQL server from the staging launch-candidate backup, run synthetic-only smoke checks, save the command output, assign reviewer signoff, and delete the restored server. | Do not overclaim beyond the tested staging point-in-time restore path; geo-redundant disaster recovery remains unproven. | Before production customer launch or before relying on restore capability, whichever comes first | Closed on 2026-07-05 by restored-server health evidence and teardown confirmation |

## Hidden Risks And Dependencies

- The seven-day backup retention window can invalidate this backup evidence if launch approval waits beyond the recoverable point-in-time range.
- Geo-redundant backup is disabled, so this evidence does not prove regional disaster recovery.
- A restored database smoke check must use synthetic-only staging data and must not import production customer data, real CUI, production secrets, unrestricted logs, or customer uploads.
- The restore rehearsal depended on Azure permissions, resource quota, paid server creation approval, temporary network access, and teardown confirmation; repeat rehearsals still depend on those controls.
- This artifact proves only the tested staging point-in-time restore path. It does not prove production customer-data restore, regional disaster recovery, long-retention backup strategy, or restore performance under incident pressure.

# Phase 1A remaining-controls implementation evidence — 2026-09-07

Overall status: **Partially implemented; not a deployment or CUI-readiness approval.**

Source: dirty working tree based on `18ca7b66a6698df619b57b56746e3b2f4e5e6856`.
Pre-existing changes were preserved. No commit, deployment, cloud restore, incident message, or
production mutation was performed. The only production request was the read-only health GET below.

## Changes in this increment

- Shared `TenantDataHandlingModePolicyService` calls `ICurrentDataHandlingNoticeGuard` before allowing
  workflows. Unknown classification no longer bypasses that policy. Report CSV exports also check
  current notice consent. Extraction/PDF workers recheck through their application/repository paths;
  extraction and PDF completion re-read applicable checks before persistence.
- Missing/outdated notice consent returns HTTP 428 and `workflowContext`. Authenticated tenant users
  can read/acknowledge their own notice. The web panel reopens on renewal, clears consent, and ignores
  a stale acknowledgement response after a renewal event. Original actions are not automatically replayed.
- Evidence/document deletion enqueues `ObjectCleanupEntity` inside the business/audit transaction.
  `EfObjectCleanupQueue` uses PostgreSQL `FOR UPDATE SKIP LOCKED`, a 30-second cancellation budget for
  each provider delete, bounded exponential retry delay, and completion audit in the same transaction.
  Provider exception text is not stored. A worker restart or completion-audit failure leaves work retryable.
- Migration `20260907230515_AddDurableObjectCleanup` creates the queue and tenant FK. Its down migration
  rejects rollback while pending cleanup exists. Down migration execution was not tested.
- Escalation creation validates tenant-owned typed references. Transitions lock the escalation row
  under PostgreSQL. Referral does not imply release; retained evidence metadata prevents a false
  `ContentRemoved` claim. Safe evidence release requires post-escalation administrator review and
  no active unsafe-classified file versions. Other release cases remain restricted.
- Updated affected synthetic test fixtures to include explicit current-notice acknowledgement data.
  The evidence PostgreSQL factory now uses the production `gccs.__EFMigrationsHistory` location.

## Verification

Environment: macOS; local .NET 10, Node/Vite/Vitest, Docker PostgreSQL 17.
Database: newly created `gccs_remaining_20260907`, not the normal developer or production database.

| Check | Result | Scope / limitation |
| --- | --- | --- |
| `dotnet build Gccs.slnx --configuration Release --no-restore` | Passed, zero warnings/errors | Solution build |
| Combined boundary/adjacent API tests | 64 passed, zero failed/skipped; final recorded duration 21 seconds | PostgreSQL evidence concurrency/rollback/cleanup plus InMemory API/domain suites |
| Frontend lint and TypeScript/Vite build | Passed | Not a browser smoke test |
| Frontend Vitest suite | 172 passed, 18 files; final run 12.52 seconds | Simulated DOM/unit tests, not real browser coverage |
| Notice renewal component test, verbose targeted run | 1 passed, 408 ms total | Explicit renewal clears consent and opens current notice; simulated DOM |
| `git diff --check` | Passed | Whitespace/diff hygiene |
| New cleanup migration | Applied in isolated PostgreSQL | Latest migration confirmed from history table |
| Local dump/restore | Passed | Synthetic tenant and pending cleanup intent survived into separate local database |

API test command:

```sh
GCCS_TEST_POSTGRES_CONNECTION='<isolated local PostgreSQL connection>' \
dotnet test tests/Gccs.Api.Tests --configuration Release --no-build \
  --filter 'FullyQualifiedName~EvidenceFileUploadTests|FullyQualifiedName~CuiSupportEscalation|FullyQualifiedName~DataHandlingNoticeAcknowledgementTests|FullyQualifiedName~ClassificationBoundaryValidationTests|FullyQualifiedName~TenantModeWorkflowEnforcementTests|FullyQualifiedName~ContentClassificationMetadataTests|FullyQualifiedName~ContentClassificationReviewTests|FullyQualifiedName~EvidencePackageReportTests|FullyQualifiedName~SimpleReportExportTests' \
  --logger 'trx;LogFileName=phase1a-remaining-final.trx'
```

Machine results: `tests/Gccs.Api.Tests/TestResults/phase1a-remaining-final.trx`.
Earlier iterations exposed missing notice fixture registrations and a stale 403-vs-400 assertion for
forged synthetic provenance; these were corrected and the combined selection rerun successfully.
Storage and scanner in the evidence fault tests are test doubles. This increment did **not** rerun
the complete real-stack browser/Azurite/ClamAV regression or a complete RBAC endpoint inventory.

## Local recovery rehearsal

After the test suite finished, the isolated source database had zero tenants and zero pending cleanup
records. Added one explicitly synthetic tenant and one cleanup marker (not an actual blob reference),
then streamed `pg_dump --no-owner --no-privileges` into a newly created database
`gccs_remaining_restore_20260907` with `psql -v ON_ERROR_STOP=1` and shell pipeline failure checking.
Dump/restore pipeline elapsed time was 0.323 seconds. This is **not** a measured production RTO.

Verified the restored tenant name, NoCui mode, cleanup container/object name, zero attempts, null
completion time, and 78 migration-history entries. Both local databases and the synthetic marker
remain for inspection. No cloud backup, point-in-time recovery, object-storage restoration, application
cutover, recovery credentials, or tenant/RBAC validation of a restored application was exercised.

## Read-only production observation

GET to the documented production API `/health` returned `status: ok` at
`2026-09-07T23:10:45.193453+00:00`, with No-CUI / compliance-management-only posture and reachable
PostgreSQL, object storage, Redis, and background-job coordination. The response did not report
malware-scanner status or a deployed SHA. This is connectivity evidence, not security-review or recovery evidence.

## Remaining limitations — do not claim completion

- Notice coverage of every metadata/note workflow and complete worker authorization inventory.
- Containment of every metadata/result read, in-flight access/race boundaries, and complete descendant invalidation.
- Version-bound review/release for documents, immutable reports, and unsafe legacy evidence file versions.
- Durable reconciliation of uploads orphaned by process termination and all external-side-effect paths.
  The new queue covers committed evidence/document deletions, not every upload compensation path.
- Proven real-provider fault behavior, full browser regression for the coordinated cutover, and
  complete allowed/denied tenant/RBAC matrix for all affected endpoints.
- Deployment-specific security review, monitored escalation/cleanup alerts, named primary/backup
  responders, approved RPO/RTO, and separately approved cloud recovery rehearsal.

See `docs/phase-1a-operations-approval.md` for the decision worksheet and rollout/rollback dependencies.
Do not drop the cleanup table or treat logical deletion as completed physical erasure. The service
remains No-CUI; neither this evidence nor a successful health GET authorizes real customer CUI.

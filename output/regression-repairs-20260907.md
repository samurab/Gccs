# Regression failure repairs — 2026-09-07

Status: named failure groups repaired and targeted verification passed. **Full regression remains incomplete.**

Source: dirty working tree based on `18ca7b66a6698df619b57b56746e3b2f4e5e6856`.
No deployment or cloud operation was performed. Existing retained synthetic resources were not removed.
New local PostgreSQL test databases `gccs_regression_repairs_20260907` and
`gccs_regression_repairs2_20260907` remain for inspection; individual tests clean up their own fixtures.

## Causes and corrections

1. **Migration configuration drift and concurrent bootstrap.** Test contexts used the default history
   schema while runtime/design-time contexts used `gccs`. Runtime, design-time, and PostgreSQL test
   builders now share `UseGccsPostgres`. Three configuration tests verify the resulting history SQL.
   A fresh concurrent run also exposed duplicate migration execution (an already-removed index).
   `PostgresTestDatabase` holds a database-scoped PostgreSQL session advisory lock before EF reads
   history and applies migrations. It is not an outer transaction around EF migrations. All test
   bootstrap callers use this helper. Fresh bootstrap then completed with only
   `gccs.__EFMigrationsHistory`, containing 78 entries.
2. **Phase 1A 403/400 assertion.** The prior increment already corrected the synthetic-provenance test:
   caller-forged imported-demo metadata is invalid input (400), while valid restricted classifications
   continue to exercise the mode-policy denial. The named regression passed again; authorization was
   not relaxed to satisfy the assertion.
3. **Coverage inventory drift.** The documentation has 673 cases, not 628: 628 software cases plus
   45 Phase 0/SOC 2 governed-evidence cases. The harness now accounts for both, routes the latter to
   human evidence review, and does not characterize them as automated product verification. Eleven
   prompt ranges were expanded into explicit case IDs without replacing the surrounding user edits.
4. **Secret-scanner false positives.** C# lambda parameters no longer count as assigned secrets;
   reserved `.invalid` addresses are accepted. Positive controls still reject quoted credentials,
   expression-bodied string credential properties, and nonreserved domain lookalikes. The scanner
   remains heuristic and scans tracked text files; this is not an exhaustive security audit.
5. **Health-test lifecycle/budget defects.** A 60-second test previously enclosed Docker startup
   allowed to take 180 seconds. The test now has a 240-second outer budget and a 20-second HTTP
   deadline, records phase timings, disposes its child host, and runs in a nonparallel collection.
   Both seed switches and unrelated application workers are disabled for the connectivity host.
   It uses the explicit test connection, or the local maintenance database for connectivity only.
   Docker absence now fails an explicitly selected LocalDocker test rather than silently passing.
   Timed-out child processes are reaped after termination.

The previous 12-minute interrupted broad run was not reproduced as a full run here. The health fixes
address confirmed budget/lifecycle problems; they do not establish that the entire API suite completes.

Two PostgreSQL audit-fault fixtures were updated with explicit synthetic current-notice consent so
the requests and worker reach the intended audit failure. Their absence-of-configuration behavior
now reports a skipped PostgreSQL test rather than a false successful return.

## Verification evidence

Environment: local macOS, .NET 10, PostgreSQL 17 in Docker, local Redis/Azurite/ClamAV.
No frontend implementation changed in this repair increment; no frontend/browser regression was run.

- Baseline named selection: 3 failed, 683 passed. Failures reproduced the 628/673 mismatch, missing
  range-interior IDs, and scanner false positives. Phase 1A status assertion already passed.
- Corrected named selection including Docker health: 693 passed, zero failed/skipped.
- Combined PostgreSQL/boundary/configuration/coverage/scanner/health selection: 726 passed,
  zero failed/skipped (26 seconds in the first successful combined run).
- Additional scanner edge-case rerun: 13 passed, zero failed/skipped (6 seconds).
- Solution Release build: passed with zero warnings/errors. `git diff --check` passed.
- Health in the combined run: Docker startup 0.69 s; host ready 1.22 s; HTTP 200 at 1.57 s.
  Assertions checked PostgreSQL, Redis, storage, scanner, and background-job coordination.
- Final rerun results: `tests/Gccs.Api.Tests/TestResults/regression-repairs-final.trx`.

The 726 checks include documentation-strategy theories. They are **not** 726 end-to-end product tests.
Storage/scanner fault tests use doubles where their existing fixtures specify them; the health test
checks actual local provider connectivity, not a complete upload/scan/extract browser workflow.

Command (set the connection to a dedicated synthetic local database, never production):

```sh
dotnet test tests/Gccs.Api.Tests --configuration Release --no-restore \
  --settings tests/Gccs.Api.Tests/regression.runsettings \
  --filter 'Category=PostgresIntegration|FullyQualifiedName~GccsPostgresConfigurationTests|FullyQualifiedName~EvidenceFileUploadTests|FullyQualifiedName~DevelopmentStoryRegressionCoverageTests|FullyQualifiedName~LocalDependencyConfigurationTests|FullyQualifiedName~TC_1A_1_2_2_NoCui_blocks_synthetic_demo_records' \
  --logger 'trx;LogFileName=regression-repairs-final.trx'
```

`GCCS_TEST_POSTGRES_CONNECTION` was set for PostgreSQL verification. The runsettings bound xUnit
parallelism to four threads and the overall session to 30 minutes; they do not skip tests. Omit the
filter for a future full API run. That run, complete browser/real-stack regression, dependency scans,
and deployment-specific verification remain outstanding.

## Compatibility and remaining risks

- Production history location remains `gccs.__EFMigrationsHistory`; no application schema migration
  was introduced by these repairs. No old history rows were copied, deleted, or marked applied.
- Retained databases with conflicting historical schemas are evidence, not approved migration targets.
  Do not repair them by copying history rows or using `EnsureCreated`; compare actual schema against
  migrations first, or use a fresh isolated test database.
- The migration advisory lock requires a normal dedicated PostgreSQL session. Do not put test
  bootstrap through transaction-mode connection pooling. Lock acquisition remains subject to command timeout.
- Cold Docker startup and the full-suite contention scenario have not been proved by the fast warm
  local health result. The new timings and deadlines make future failures attributable.
- No claim of certification, CUI approval, completed production recovery, or full regression is made.

# Staging regression — 2026-09-08

Status: local regression passed after the documented policy-note correction; staging deployment pending.
Internal engineering evidence, not a production or CUI approval.

## Scope and architecture

The existing worktree contained Story 12.2, interdependent Phase 1A integration,
tests, roadmap documentation, and a status workbook. Preserve these changes in
separate documentation and implementation commits on
`codex/evidence-phase1a-staging-regression`; do not merge or deploy production.

Three defects addressed during regression:

1. Legacy isolated API fixtures did not supply the newly required notice guard's
   persistence dependencies, producing HTTP 500 instead of exercising their feature.
   They now explicitly model acknowledged consent. Dedicated notice, upload,
   tenant-mode, and pilot tests retain the real guard.
2. Concurrent byte uploads exposed a non-thread-safe Dictionary in the test storage
   double. It now uses ConcurrentDictionary. The test asserts twelve distinct
   sequential PostgreSQL versions, twelve stored objects, and twelve audit records.
3. The demo reset PostgreSQL table lock was not schema-qualified. It now targets
   `gccs.tenants`; the SQL was executed inside a rolled-back verification transaction.

Production upload identity uses the created/selected evidence ID. Missing and
cross-tenant IDs cannot implicitly create metadata. Version allocation locks the
tenant-scoped evidence row; version and audit persistence share a transaction.

## Environment and commands

- macOS arm64, .NET 10 Release, local Docker PostgreSQL 17, Redis, Azurite, ClamAV.
- Backend database: isolated synthetic `gccs_staging_regression_20260908`.
- Migration database: isolated synthetic `gccs_migration_verification_20260908`.
- Browser stack: local React/Vite, ASP.NET API and PostgreSQL development seed data.
- `dotnet build Gccs.slnx --configuration Release --no-restore`: passed, zero warnings/errors.
- `npm run lint:web`, `npm run test:web`, `npm run build:web`: passed; 172 frontend tests.
- `npm run test:e2e`: 3 passed.
- `npm run test:e2e:real`: 5 passed, including create metadata → upload bytes → retrieve version/content.
- EF `has-pending-model-changes`: none. Idempotent migration SQL applied and reapplied
  to the fresh verification database, 78 recorded migrations.
- `npm audit --audit-level=high`: zero vulnerabilities.
- `dotnet list Gccs.slnx package --vulnerable --include-transitive`: none reported.
- Staged-change Gitleaks scan: no leaks. A broader local-directory scan also saw
  existing ignored runtime artifacts and unchanged documentation matches; these
  were not added to the commit. It is not recorded as a clean repository-history scan.
- Extraction corpus evaluation: precision 1.0, recall 1.0, passed.

The consolidated backend command uses `GCCS_TEST_POSTGRES_CONNECTION` (credentials
omitted) and `dotnet test tests/Gccs.Api.Tests --configuration Release --no-build
--settings tests/Gccs.Api.Tests/regression.runsettings --logger
'trx;LogFileName=staging-consolidated.trx'`.

Consolidated result: 1,678 passed, one repository-secret-policy failure, zero skipped,
1,679 total, duration 8 minutes 52 seconds. The policy failure was a cancellation-token
lambda example quoted in a historical Markdown note, not an exposed credential.
The example was reworded without changing the scanner. The final
`FullyQualifiedName~LocalDependencyConfigurationTests` recheck passed all 15 tests,
zero skipped, in 11 seconds (`staging-policy-final.trx`). All 1,679 cases therefore
have passing evidence across the consolidated run and focused corrective rerun;
this is not a claim of one entirely green consolidated invocation. All functional
tests, including real PostgreSQL and local Docker cases, passed.

Earlier exploratory runs were interrupted after managed stacks identified a macOS
configuration-file reload loop in test-host startup. The regression settings disable
hot reload only in tests and bound xUnit parallelism to four. Those interrupted runs
are not passing evidence. Build first, then execute the consolidated suite.

## Remaining operational dependencies

- No-CUI / compliance management only. No approval for real customer CUI.
- Apply the additive object-cleanup migration before starting the new worker.
  Do not roll it down while cleanup records remain; application rollback should
  retain the table and pending work.
- Object upload and database commit span external storage and PostgreSQL. Handled
  failures compensate, but a process crash can still leave an orphaned blob.
- Backend PostgreSQL tests use a storage double for deterministic failure injection;
  local browser and dependency tests cover the real local services, not Azure faults.
- Existing acknowledgements do not automatically satisfy a newer notice version;
  users may receive HTTP 428 and need to acknowledge the current notice.
- Staging authenticated workforce/customer journeys and external-provider fault
  injection require separate credentials and scenario evidence; health alone does
  not prove those journeys.

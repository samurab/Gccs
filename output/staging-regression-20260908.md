# Staging regression — 2026-09-08

Status: Implemented and deployed to No-CUI staging after the documented regression correction.
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

## Staging deployment and post-deployment checks

- Deployed implementation SHA: `8799c9dfa19a4bc873152fb7c22c622a1ca5b1ba`.
- Preserved planning/documentation commit: `e78bf4b3`.
- [Staging workflow run 34180299257](https://github.com/samurab/Gccs/actions/runs/34180299257):
  succeeded on 2026-09-08 UTC; deployment job duration 4 minutes 1 second.
- GitHub Actions built the artifacts, generated/applied the migration SQL, validated
  staging No-CUI/infrastructure rules, deployed the API and Static Web App, and passed
  the staging smoke tests. Artifact: `staging-smoke-test-results` (health JSON and SQL).
- Post-deployment API health: HTTP 200, status `ok`, No-CUI posture; PostgreSQL,
  Redis, object storage, and background-job dependencies all reported `ok`.
- Unauthenticated `/api/evidence-items`: HTTP 401, both before and after deployment.
- [Staging web](https://mango-rock-016ff040f.7.azurestaticapps.net): HTTP 200.
  Entry bundle changed from `index-BO4T_mXe.js` to `index-BgplctB9.js`.
  Served `App-DRQpre0P.js` contains the selected-evidence upload UI and notice panel;
  `api-BJNYkl2z.js` contains the notice-renewal event and no hard-coded evidence ID.
- Production was not deployed. No merge to main was performed. Subsequent changes
  to this evidence record do not alter the deployed implementation SHA.

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

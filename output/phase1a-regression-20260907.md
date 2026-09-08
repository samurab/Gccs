# Phase 1 / 1A real-stack regression — 2026-09-07

Disposition: **Failed / not release-cleared.** This is a local synthetic-data run,
not production validation or CUI readiness approval. No application fixes were
made during regression execution.

## Environment and provenance

- HEAD: `18ca7b66a6698df619b57b56746e3b2f4e5e6856`, plus the existing uncommitted
  working-tree integration changes. Results are not attributable to HEAD alone.
- .NET SDK 10.0.203; Node v26.5.1; PostgreSQL 17.10 in Docker.
- Real local Azurite on port 19000, Redis on 16379, ClamAV 1.5.2 on 13310
  (signature database 28114, dated 2026-09-05).
- Separate databases: `gccs_regression_20260907_1840` (broad suite),
  `gccs_browser_20260907_1840` (browser/provider checks),
  `gccs_rollback_20260907_1842` (isolated rollback reruns).
- Browser API: localhost:5063; web: localhost:5174. Development authentication
  exercised server-side seeded roles, not production Entra authentication.
- Dedicated storage containers named `regression-1840-contracts`,
  `regression-1840-evidence`, `regression-1840-reports`, and
  `regression-1840-exports`. Existing customer/development records were not targets.
- Temporary API processes on ports 5063 and 5065 were stopped after testing.
  Synthetic databases and blobs are retained for inspection; shared containers
  and services were not stopped or reconfigured.

## Results

| Check | Result | Scope |
| --- | --- | --- |
| Release solution build | Passed, zero warnings/errors; 17.1 s | `dotnet build Gccs.slnx --configuration Release --no-restore` |
| Frontend lint | Passed | `npm run lint:web` |
| Frontend tests | 172 passed, 18 files; 13.8 s | `npm run test:web` |
| Web production build | Passed | `npm run build:web` |
| Real-stack Chromium | 4 passed; 19.0 s | Upload/extraction; assignment persistence; Auditor and Contributor report UI/API restrictions |
| Isolated evidence/rollback suite | 16 passed, none skipped; 17 s | Evidence identities, versions, downloads, audit rollback, rejection audit, concurrent acknowledgements |
| Targeted boundary suite | 51 passed, 1 failed, none skipped; 8 s | Classification, notices, escalation, audit exports, tenant/authorization and report/extraction rollback tests |
| Tagged PostgreSQL integration rerun | 13 passed, none skipped; 4 s | Initialized isolated database; includes tests outside Phase 1/1A |
| Broad API suite | Failed and incomplete; stopped after approximately 12 minutes | Reported failures and a 60-second health-test timeout; no final aggregate totals |

TRX evidence is in `tests/Gccs.Api.Tests/TestResults/`:

- `phase1-isolated-rollback.trx`
- `phase1-1a-boundaries.trx`
- `phase1-postgres-initialized.trx`

The canceled broad run did not emit its requested `phase1-1a-real-regression.trx`.
Its reported failures are recorded below from the runner output; no total pass
count is inferred. SIGINT stopped the runner and its test host.

Browser command: `npm run test:e2e:real`, with
`ConnectionStrings__GccsDatabase` pointed to the browser database and the four
`Storage__Containers__*` overrides above. The existing config starts the Release
API with synthetic seeding and the extraction worker enabled. No request mocks
were used in these four real-stack browser tests.

API command: `dotnet test tests/Gccs.Api.Tests/Gccs.Api.Tests.csproj --no-restore
--logger 'trx;LogFileName=phase1-1a-real-regression.trx'`, with
`GCCS_TEST_POSTGRES_CONNECTION` pointing to the broad regression database.
Reruns use `--no-build` with `FullyQualifiedName~EvidenceFileUploadTests`, the
classification/tenant/authorization/report/extraction boundary filters, and
`Category=PostgresIntegration`, respectively. Passing totals overlap; do not sum
them as unique test coverage.

## Direct provider checks

- Real ClamAV rejected the harmless EICAR test signature through the contract
  upload API with HTTP 400. Document count stayed at one; PostgreSQL recorded a
  `Rejected` ContractDocument audit event.
- A second temporary API process used an unavailable scanner port (13311).
  A benign upload returned HTTP 503 `malware_scanner_unavailable`; document count
  stayed at one. The shared scanner was never stopped.
- Direct Azure SDK access to Azurite verified exactly one stored object and
  byte-for-byte equality with the 77-byte synthetic browser-uploaded document.
  Neither rejected scanner fixture was stored. The CLI initially rejected the
  emulator's API-version mismatch; the SDK check used supported API 2023-11-03.
- Cross-tenant direct contract read returned HTTP 404.
- Browser extraction consumed the stored document and produced a clause candidate;
  PostgreSQL recorded Uploaded/Rejected document audit events.

## Confirmed failures and limits

1. **Migration-history mismatch in test setup.** Application DI configures
   `gccs.__EFMigrationsHistory`; several PostgreSQL test factories use plain
   `UseNpgsql(connectionString)` and therefore default history placement.
   The broad test database had both `public.__EFMigrationsHistory` (6 rows)
   and `gccs.__EFMigrationsHistory` (71 rows) when inspected. Failures include
   PostgreSQL 42P07, `relation "clauses" already exists`. Isolated, consistently
   initialized reruns passed; this does not erase the broad-suite failure.
2. **Phase 1A response-contract mismatch.**
   `TenantModeWorkflowEnforcementTests.TC_1A_1_2_2_NoCui_blocks_synthetic_demo_records`
   expects 403 but receives 400. The request is rejected; this is not evidence
   of an accepted CUI upload. It reproduces outside the broad-suite setup failure.
3. **Documentation coverage failures.** The coverage harness reports missing
   `TC-0.1.2` in prompts and a case-inventory mismatch. These are broader than
   Phase 1/1A but keep the required repository regression red.
4. **Secret/data scanner false positives.** The repository scan flags three
   cancellation-token lambda expressions as assigned credentials and
   the synthetic `.invalid` email fixture as customer data. Inspection identifies
   these as test-scanner matching issues, not verified credential exposure.
5. **Health-test timeout and incomplete broad execution.**
   `LocalDependencyConfigurationTests.Api_health_reports_connectivity_for_local_database_cache_storage_and_scanner`
   reported a 60,000 ms timeout. The broad suite was bounded at approximately
   12 minutes and canceled rather than treated as passed. Runtime provider checks
   above completed separately; the timeout's root cause was not established.

Recommended next fixes: unify migration-history configuration in all PostgreSQL
test factories and isolate fixture setup; reconcile the 400/403 API contract with
the new provenance validation; repair documentation inventory and scanner matching;
diagnose the health-test timeout, then rerun the complete suite to completion.

Not proven: production identity/provider behavior; exhaustive Phase 1/1A browser
coverage (only four existing real-stack tests); full notice guard integration;
complete version-bound containment/release; external storage-failure rollback or
durable cleanup delivery; production restore/RPO/RTO; cloud infrastructure and
incident readiness. Most API tests still use in-memory dependencies; setting a
PostgreSQL variable does not convert every API test to a real-provider test.

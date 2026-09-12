# Story 31.3 Verification Record — SAM.gov SPR Report Package

Date: 2026-09-10

Implementation commits: `ce7c9f41`, `b1798ce0`, `9c87f113`

Verification level: High (tenant-scoped report, RBAC, export, audit, and lifecycle behavior)

## Current-State Classification

- **Implemented:** Tenant-scoped SPR preparation package generation for an active contract/report obligation and period.
- **Implemented:** Immutable, versioned snapshots with report type, period, spend/subcontractor summaries, exceptions, evidence references, schema provenance, generated date, review notes, and approval metadata.
- **Implemented:** Server-side `ViewReports`, `ManageReports`, and `ExportReports` authorization; cross-tenant package identifiers resolve as not found.
- **Implemented:** Draft, in-review, approved, superseded, and archived lifecycle with compare-and-set persistence and append-only audit events.
- **Implemented:** HTML and JSON export with reviewable string-valued report/status metadata, schema provenance, the preparation-only disclaimer, permission checks, and export audit logging.
- **Implemented:** Append-only, user-recorded external receipt history. Corrected outcomes require a link to a prior receipt; the prior receipt remains unchanged.
- **Do not claim:** FeDril submits, synchronizes, validates, or verifies a report with SAM.gov. Direct submission remains disabled without an authorized contractor-facing provider.

## Acceptance-Test Evidence

| Test case | Result | Evidence |
| --- | --- | --- |
| TC-31.3.1 | Pass | `EsrsReportPackageTests.TC_31_3_1_*`; browser smoke generated an ISR v1 snapshot containing one eligible row, a $12,501 spend summary, an exception, schema 1.0 provenance, and a generated timestamp. |
| TC-31.3.2 | Pass | `EsrsReportPackageTests.TC_31_3_2_*`; browser and HTML/JSON export checks displayed “FeDril has not submitted this report to SAM.gov.” |
| TC-31.3.3 | Pass | `EsrsReportPackageTests.TC_31_3_3_*` and `Review_lifecycle_*`; browser smoke moved v1 from Draft to InReview to Approved and displayed reviewer, approval date, and notes. |
| TC-31.3.4 | Pass | `SubcontractingReportDataApiTests` package authorization/cross-tenant cases; browser Auditor context showed read-only text and no generate, receipt, lifecycle, or export controls. |
| TC-31.3.5 | Pass | `EsrsReportPackageTests.TC_31_3_5_*`; the PostgreSQL concurrency test proved one successful lifecycle transition creates exactly one audit event. |
| TC-31.3.6 | Pass | `Export_is_traceable_*`, `Json_export_uses_reviewable_string_metadata_*`, and API export permission tests. HTML and JSON response headers/content were also inspected through the local API. |
| TC-31.3.7 | Pass | `Manual_receipts_are_append_only_*`, API tenant/evidence checks, and the frontend correction test. Browser smoke retained `SYNTH-SPR-31-3-001` and added linked correction `SYNTH-SPR-31-3-002`. |
| TC-31.3.8 | Pass | `Submission_capability_fails_closed_*` and API `spr_submission_unavailable` assertion; browser displayed direct submission as unavailable. |

## Commands and Results

- `GCCS_TEST_POSTGRES_CONNECTION=... dotnet test tests/Gccs.Api.Tests/Gccs.Api.Tests.csproj --filter 'FullyQualifiedName~EsrsReportPackageTests|FullyQualifiedName~SprReportPackagePostgresConcurrencyTests|FullyQualifiedName~PostgreSQL_audit_failure_rolls_back_report_row_and_evidence_links' --no-restore` — **13 passed, 0 failed, 0 skipped** against PostgreSQL 17.10 on `localhost:15432`.
- `GCCS_TEST_POSTGRES_CONNECTION=... dotnet test tests/Gccs.Api.Tests/Gccs.Api.Tests.csproj --filter 'FullyQualifiedName~SprReportPackagePostgresConcurrencyTests|FullyQualifiedName~PostgreSQL_audit_failure_rolls_back_report_row_and_evidence_links' --no-restore` — **2 passed, 0 failed, 0 skipped**; proves lifecycle compare-and-set concurrency and report/audit rollback.
- `npm test -- --run` in `apps/web` — **27 files passed, 223 tests passed**.
- `npm run lint` in `apps/web` — **passed**.
- `npm run build` in `apps/web` — **passed**.
- `dotnet test tests/Gccs.Api.Tests/Gccs.Api.Tests.csproj --filter FullyQualifiedName~DevelopmentStoryRegressionCoverageTests --no-restore` — **682 passed, 0 failed, 0 skipped**.
- `dotnet build Gccs.slnx --no-restore` — **passed with 0 warnings and 0 errors**.
- `git diff --check` — **passed**.
- Full backend suite — **not clean**: two `ContinuousIntegrationBaselineTests` failed because `.github/workflows/ci.yml` lacks the pre-existing expected step named `Verify report and audit transaction rollback`. The run was canceled after more than nine minutes while the remaining repository-wide tests continued consuming CPU. This is CI workflow-contract drift, not a Story 31.3 behavior failure; no passing result is claimed for the full backend suite.

## Browser Smoke Scope

The local React application at `http://127.0.0.1:5173` and API at `http://127.0.0.1:5062` were exercised with synthetic Tenant Alpha data only. The flow covered eligible source data, package generation/readback, not-submitted language, review and approval, reviewer metadata, append-only submitted/corrected receipt display, direct-submission disabled messaging, and Auditor fail-closed controls. Browser download-event capture did not observe the programmatic blob download; export status, media type, content disposition, content, permissions, and audit behavior are covered by direct API inspection and automated tests.

## Hidden Risks and Dependencies

- Package version allocation is database-enforced by a tenant/contract/type/period/version uniqueness constraint. Simultaneous generation can return a safe `409 spr_package_conflict`; callers must reload and retry. Generation is not idempotent because no client idempotency key is part of the current contract.
- Manual receipts are customer-reported metadata. FeDril does not independently confirm the external reference or outcome with SAM.gov.
- Schema correctness depends on a reviewed, published, effective SPR schema profile in `packages/compliance-content`; qualified compliance review remains an operational dependency.
- The No-CUI posture depends on users supplying synthetic, redacted, or otherwise permitted metadata. This story does not authorize storage of real CUI, classified, ITAR/export-controlled, or sensitive government-furnished content.

## Pre-Publication Checklist

- [x] UI exposes preparation, review, history, export controls, and preparation-only language.
- [x] API enforces tenant scope, permissions, lifecycle validation, and disabled direct submission.
- [x] Focused automated tests prove allowed, denied, cross-tenant, audit, rollback, and concurrency behavior.
- [x] Wording avoids certification, legal, government-approval, and SAM.gov-submission claims.
- [x] No-CUI posture and source/schema traceability remain explicit.
- [ ] Repair the unrelated CI workflow-contract drift before treating the repository-wide backend suite as clean.

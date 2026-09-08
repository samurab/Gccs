# Classified-content completion verification — 2026-09-08

Internal engineering evidence, not a customer assurance or CUI authorization statement.

## Story 1A.1.2: mode-based workflow enforcement

Base commit: `b0d0e839da463abfaf7550d44dc3a06eec78585a`.
Working branch: `codex/classified-content-enforcement`.
Verification level: High; full backend regression plus affected frontend and real-stack paths.

### Implemented

- Classified notes have a domain model, tenant-owned PostgreSQL persistence, application service, API, and editor in Evidence. Listing retrieves metadata only (the newest 200 records); detail retrieves text through the classification policy.
- Note creation/editing uses `ManageEvidence`; list/detail use `ViewEvidence`. Concurrent edits require the current revision. Note writes and their audit events share the existing application transaction.
- Note save, the four persisted report-generation endpoints, and extraction initiation require an explicit classification. Extraction confirmation must match the source document; evidence-package and CMMC outputs cannot be classified below included evidence.
- Note saves use the existing published general data-handling notice (`Onboarding`). Reports use `ReportGeneration`; extraction uses `ContractIntake`. No notice copy, version, or qualified-review evidence was invented.
- CUI approval comes from the tenant's latest persisted mode transition and its current approved, reviewed, non-expired checklist with completed required items. Client approval flags cannot authorize a workflow. Synthetic approval requires a tenant-owned imported demo record with persisted provenance.
- Missing bodies and incomplete classification objects produce standard 400 responses. Existing No-CUI, tenant isolation, server permissions, rejection auditing, and report snapshot immutability remain in place.

### Compatibility and migration

External callers must now send `classification: { classification: "Unclassified" }` (or another valid explicit selection) when initiating reports/extraction. Missing selection is no longer silently defaulted. Existing feature test fixtures were migrated to explicit confirmation; classification-boundary tests do not use that fixture helper.

Apply migration `20260908144153_AddClassifiedNotes` before deploying this API. Existing records are not assigned invented review history or approvals. Its downgrade refuses to drop a populated notes table. Application rollback must preserve this additive table and its history; removing populated records requires a separately approved, data-preserving plan.

### Executed evidence

Environment: local macOS, .NET 10, PostgreSQL 17 in Docker on port 15432; synthetic data only. Browser tests use development authentication, not staging authentication. PostgreSQL integration database: `gccs_staging_regression_20260908`; isolated browser database: `gccs_classified_browser_20260908`.

- Focused classification, note, extraction, and tenant-mode tests: 67 passed, zero skipped, 30 seconds (`/tmp/gccs-step1-final-focused.log`).
- Report source-classification and adjacent tests: 43 passed, zero skipped, 25 seconds (`/tmp/gccs-step1-source-classification.log`).
- Latest note/body-validation, endpoint authorization, and PostgreSQL report-rollback tests: 44 passed, zero skipped, 28 seconds (`/tmp/gccs-step1-body-validation.log`). This includes the metadata-only note query change.
- Final checkpoint: 723 passed, zero skipped, 27 seconds (`/tmp/gccs-step1-checkpoint.log`), including note behavior, endpoint authorization, approval checklists, and the development-story regression inventory. The inventory proves documented-case coverage strategy, not 723 independent feature workflows.
- Final classified-workflow suite: 42 passed, zero skipped, 28 seconds (`/tmp/gccs-step1-role-matrix.log`). Its six-role note matrix exercises list, detail, create, and update for Owner, Admin, Compliance Manager, Contributor, Advisor, and Auditor; denied writes leave note revisions/counts and note audit events unchanged.
- Vitest: 175 passed across 19 files, 12.27 seconds (`/tmp/gccs-step1-ui-verified.log`). Web ESLint and production build passed.
- Final real-stack Chromium rerun: five passed, 13.9 seconds (`/tmp/gccs-step1-final-browser.log`), with `PLAYWRIGHT_API_URL=http://127.0.0.1:5067` and the isolated PostgreSQL connection supplied to `npm run test:e2e:real`. API startup used the rebuilt Release binaries. Evidence creation/upload/retrieval, extraction, assignment persistence, and read-only report roles passed.
- Additional Playwright CLI smoke: note save returned 428 without the current general notice; acknowledgement then allowed save; a browser reload retrieved the persisted title/text. PostgreSQL inspection confirmed revision 1. A read-only role could open the note and had no save control. Screenshot: `output/playwright/classified-note-read-only.png` (local generated artifact).
- PostgreSQL note test: eight concurrent edits produced one success and seven 409 responses. Injected audit failure rolled back the subsequent edit; revision, history, and audit counts were unchanged.
- Migration applied to the isolated browser database. EF `migrations has-pending-model-changes` reported no model drift. A real downgrade attempt to `20260907230515_AddDurableObjectCleanup` was rejected by the populated-table guard; the synthetic note remained intact.
- Full backend regression: 1,709 passed, zero skipped, 10 minutes 17 seconds (`/tmp/gccs-step1-verified.log`; TRX: `tests/Gccs.Api.Tests/TestResults/classified-step1-verified.trx`). Subsequent small changes to note metadata projection, standard body/not-found errors, and endpoint names passed the focused/checkpoint suites above. Earlier failing runs exposed missing classification in legacy fixtures; those fixtures were corrected. Interrupted or failing runs are not counted as passes.

Backend commands use `dotnet test tests/Gccs.Api.Tests --configuration Release --settings tests/Gccs.Api.Tests/regression.runsettings` with focused `--filter` expressions or no filter for the complete suite. PostgreSQL runs set `GCCS_TEST_POSTGRES_CONNECTION` to the local test database. The settings disable development configuration reload watchers and bound test concurrency.

### Dependencies and remaining scope

- A recorded approval is an application workflow decision, not proof of a qualified external assessment, certified controls, or permission to place real CUI in this No-CUI environment.
- No real customer CUI was used. Cloud storage, staging identity, external reviewers, and production deployment are not validated by these local tests.
- Notes are not inputs to any existing report or extraction workflow. Unknown notes remain stored for review, not made eligible for downstream processing.
- Stories 1A.2.1 and 1A.2.2 remain in progress: general history/reclassification and its cross-content review UI are not claimed complete by this checkpoint.

# SPRS Readiness Report Release Gates

Date recorded: 2026-09-10.

Scope: Story 30.3, SPRS Readiness Report. This is an engineering and governance control record. It is not evidence that a score was submitted to SPRS, that the checked-in scoring methodology is approved, or that FeDril provides a certification or assessor determination.

## Current State

| Claim | Status | Evidence and limitation |
| --- | --- | --- |
| An authorized tenant user can generate an immutable SPRS readiness report from a published scoring rule set. | Implemented | `POST /api/reports/sprs-readiness`, `SprsReadinessReportService`, and `SprsReadinessReportTests`. The checked-in default scoring rule set is still draft, so production generation remains unavailable until qualified review and publication. |
| The report includes score, deductions, unresolved controls, POA&M references, evidence status, rule version/source hash, generated date, review metadata, and draft/not-submitted language. | Implemented | Persisted snapshot and PDF/HTML rendering are covered by `SprsReadinessReportTests` and frontend report tests. |
| Tenant scope, server-side RBAC, No-CUI classification checks, immutable history, export authorization, and audit logging apply to the report. | Implemented | API policies, application transaction, tenant-scoped repositories, classification policy, report history/export services, and focused tests. |
| PostgreSQL rolls back the calculation, report, and earlier audit event when the final report audit write fails. | Implemented in automated coverage | `ReportPostgresTransactionTests.Sprs_report_audit_failure_rolls_back_calculation_report_and_prior_audit_event`; execution requires `GCCS_TEST_POSTGRES_CONNECTION`. |
| The checked-in NIST SP 800-171 DoD Assessment Methodology rule transcription is approved for customer scoring. | Planned | The source-controlled package remains `draft` with no reviewer or review date. A qualified reviewer must complete the evidence below before publication. |
| FeDril submits or updates a score in SPRS. | Do not claim | No SPRS submission integration is implemented. Every report states that FeDril has not submitted the score to SPRS. |
| An SPRS readiness report proves compliance, certification, assessment success, or government approval. | Do not claim | The artifact is decision-support workflow guidance and remains draft/not-submitted. |

## Recommended Timing And Promotion Gates

The gates are sequential. A later gate does not override a failed or incomplete earlier gate.

### Gate 1: Pull Request And Continuous Integration

Run on every change to scoring rules, calculations, reports, report exports, tenant scope, permissions, classifications, or audit behavior.

1. Run the focused backend suites:

   ```bash
   dotnet test tests/Gccs.Api.Tests/Gccs.Api.Tests.csproj --filter "FullyQualifiedName~SprsScoringRuleBaselineTests|FullyQualifiedName~SprsScoreCalculation|FullyQualifiedName~SprsReadinessReportTests"
   ```

2. Run the focused frontend tests and build:

   ```bash
   npm --workspace apps/web run test:run -- src/pages/ReportsPage.test.tsx src/lib/api.test.ts
   npm run build:web
   ```

3. Run API build, migration drift, and OpenAPI validation:

   ```bash
   dotnet build Gccs.slnx --configuration Release
   dotnet tool run dotnet-ef migrations has-pending-model-changes --project src/Gccs.Infrastructure/Gccs.Infrastructure.csproj --startup-project apps/api/Gccs.Api.csproj --context GccsDbContext
   npm run lint:openapi
   ```

Exit criterion: all commands pass, the default scoring package remains draft unless Gate 2 evidence has been accepted, and the diff contains no unrelated or weakened security behavior.

### Gate 2: Qualified Scoring-Methodology Review

Complete before publishing a rule set or enabling report generation with the default content package in staging or production.

The compliance content owner prepares the review; a distinct qualified reviewer verifies:

- the authoritative source URL and downloaded source SHA-256;
- all 110 NIST SP 800-171 Rev. 2 requirement identifiers;
- fixed deductions, both conditional deduction option sets, the assessment-blocking rule, basic-safeguarding markers, and governed not-applicable conditions;
- effective date, methodology version, source comments, and assessment guidance;
- owner/reviewer separation, reviewer identity, review date, and last-reviewed date;
- that the report language remains readiness-only, draft, and not submitted to SPRS.

Required evidence record:

| Field | Required value |
| --- | --- |
| Rule-set ID and version | Exact source-controlled identifiers |
| Source URL and SHA-256 | Match the reviewed authoritative artifact |
| Content owner | Named person or accountable function |
| Qualified reviewer | Named person distinct from the content owner |
| Review completed at | ISO-8601 date |
| Inventory result | 110 unique requirements and categorized deduction totals reconciled |
| Exceptions | Each exception has an owner, disposition, and target date |
| Decision | Approved for publication or rejected; silence is not approval |

Exit criterion: the evidence is reviewed and retained, exceptions are closed or explicitly rejected, and the governed lifecycle changes from `draft` to `approved` and then `published`. Runtime code cannot write lifecycle changes to the source-controlled package; publication is a reviewed repository change.

### Gate 3: PostgreSQL Atomicity And Isolation

Complete before staging promotion and after any transaction, persistence, audit, report, or export change.

```bash
GCCS_TEST_POSTGRES_CONNECTION='Host=127.0.0.1;Port=15432;Database=gccs;Username=gccs;Password=gccs_dev_password' \
  dotnet test tests/Gccs.Api.Tests/Gccs.Api.Tests.csproj \
  --filter "Category=PostgresIntegration" \
  --settings tests/Gccs.Api.Tests/regression.runsettings
```

Exit criterion: PostgreSQL integration tests pass, including the SPRS second-audit-failure rollback test. A skipped test or absent connection string is incomplete evidence, not a pass.

### Gate 4: Staging Workflow Verification

Complete after Gates 1-3 and before production promotion. Use synthetic, unclassified data only.

Verify with two tenants and at least one allowed and one denied role:

- generate a report with `ManageReports` and receive the expected immutable snapshot;
- deny generation without `ManageReports` and prove no calculation, report, or success audit was written;
- return the standard tenant-safe not-found response for a cross-tenant assessment, report detail, history, archive, and export attempt;
- show loading, empty, success, validation-error, service-error, and authorization-denied UI states;
- verify history, detail, archive/restore with reason, PDF export, and download authorization;
- reject CUI-classified content for a No-CUI tenant without business writes;
- confirm rule version, source hash, generated date, leadership review metadata, and the exact not-submitted statement in UI and export;
- inspect audit records for generation and export without document contents, credentials, or other sensitive values.

Exit criterion: retain environment, commit SHA, database provider, actor roles, tenant IDs represented by redacted aliases, commands, result counts, timestamps, and any skipped scope. Do not use production customer data.

### Gate 5: Customer-Facing Claim Review

Complete after the staging artifact is fixed and before release notes, sales material, demos, or product copy describe the feature. Repeat after any external copy, methodology, submission behavior, or data-handling change.

Exit criterion: product/compliance and legal or contracting review records explicitly approve the exact surfaces in scope. Until then, only the `Implemented` claims in this document may be used, with their limitations.

## Dependencies And Hidden Risks

| Dependency or risk | Failure mode | Control | Owner | Timing/status |
| --- | --- | --- | --- | --- |
| Qualified scoring reviewer | Incorrect deductions or fabricated authority could produce materially wrong leadership guidance. | Gate 2 evidence and distinct owner/reviewer; default package remains draft and unusable. | Compliance content owner | Required before default staging/production generation; external dependency. |
| Authoritative methodology artifact | Source URL content can move while a URL remains unchanged. | Retain and compare SHA-256; version every reviewed rule set; never mutate historical report snapshots. | Compliance content owner | Recheck on every source or methodology change. |
| PostgreSQL test environment | In-memory behavior cannot prove relational rollback, constraints, or transaction semantics. | Run the `PostgresIntegration` category in the existing real-stack CI job and before staging. | Engineering/QA | Automated in CI; local execution depends on `GCCS_TEST_POSTGRES_CONNECTION`. |
| CI capacity and test-host contention | Unbounded test concurrency can cause host-start timeouts that hide real failures. | Existing deterministic four-way class sharding, `MaxParallelThreads=4`, bounded timeouts, hang diagnostics, and retained TRX artifacts. | Engineering | Continuous; investigate repeated timeouts rather than rerunning until green. |
| Tenant and role fixtures | A happy-path-only staging run can miss cross-tenant disclosure or client-only authorization. | Two-tenant matrix, server-side permission denial, tenant-safe not-found behavior, and no-write assertions. | QA/security | Required at Gate 4 and after boundary changes. |
| Report exports | PDFs can be copied outside application access controls and may contain customer compliance data. | Explicit export permission, audit event, immutable source snapshot, No-CUI classification, private object storage, retention and incident procedures. | Security/product | Review before staging and continuously after release. |
| Stale readiness snapshots | A valid historical report can become obsolete after controls, evidence, POA&M, or rules change. | Display generated date and rule version/hash; generate a new immutable report; never silently update an old artifact. | Product/compliance | User/process dependency; monitor after release. |
| SPRS submission boundary | Leadership may mistake readiness output for an official submission. | Exact not-submitted language in UI/export/audit metadata; no submission integration claim. | Product/legal | Continuous and re-reviewed after copy changes. |
| No-CUI posture | Reviewer notes or exports may contain prohibited real CUI despite workflow warnings. | Server-side classification and tenant-mode checks, acknowledgement, minimized audit metadata, user training, and incident runbook. | Security/customer owner | Continuous; controls reduce but do not eliminate misclassification risk. |
| Concurrency and retries | Repeated requests can create multiple legitimate immutable reports because generation is not idempotent. | Treat each successful generation as a separate timestamped artifact; monitor duplicate generation and add an idempotency contract before automated clients are supported. | Product/engineering | Accepted limitation for interactive workflow; resolve before automation/API clients. |

## Pre-Publication Checklist

- [ ] The current UI exposes the exact workflow described.
- [ ] The API enforces tenant scope, RBAC, validation, classification, audit, and standard errors.
- [ ] Focused in-memory, PostgreSQL, frontend, and staging tests prove the claims in scope.
- [ ] The default scoring rule set has qualified-review evidence and is published through the governed repository process.
- [ ] The wording avoids certification, legal/compliance, government-approval, official-submission, and audit-readiness overclaims.
- [ ] The report and supporting material preserve the No-CUI posture.
- [ ] The artifact still states that FeDril has not submitted the score to SPRS.

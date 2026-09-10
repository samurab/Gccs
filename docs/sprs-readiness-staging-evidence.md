# Story 30.3 Staging Workflow Evidence

Evidence status: **Blocked — not executed for Story 30.3.**

Recorded: 2026-09-10.

This record must not be represented as passed staging evidence. Local integration tests and CI checks prove implementation behavior, but they do not prove the deployed staging identity, configuration, database, or browser workflow.

## Blocking Dependencies

1. Deploy a commit containing Story 30.3 and its idempotency/classification hardening through `.github/workflows/staging.yml`.
2. Complete qualified review of the checked-in SPRS methodology and publish the reviewed rule set. The current default package remains `draft` and is intentionally unavailable for report generation.
3. Provide authenticated staging identities for an allowed report manager and a denied role in each of two synthetic-only staging tenants.
4. Confirm the staging environment remains No-CUI and contains no production customer data.

## Required Execution Record

Record the deployed commit SHA, workflow run, UTC start/end time, staging web/API identifiers, PostgreSQL provider/version, redacted tenant aliases, role names, rule-set ID/version/source SHA-256, and tester. Do not record access tokens, raw reviewer notes, document contents, or customer identifiers.

| Case | Required action | Passing result | Status |
| --- | --- | --- | --- |
| STG-30.3-01 | Generate with `ManageReports`, an approved published rule set, synthetic assessment data, and a new idempotency key. | HTTP 201, immutable report snapshot contains all Story 30.3 fields and `Idempotency-Replayed: false`. | Not run |
| STG-30.3-02 | Retry the identical request with the same key. | Same report and calculation IDs, `isReplay: true`, and no additional report, calculation, or success audit. | Not run |
| STG-30.3-03 | Reuse the key with changed input. | Standard HTTP 409 `idempotency_conflict`; rejected audit exists; no business write. | Not run |
| STG-30.3-04 | Send simultaneous identical requests with one key. | Both requests resolve to one immutable report; relational counts and audit counts remain singular for generation. | Not run |
| STG-30.3-05 | Attempt generation and view/export with denied roles. | Server-side denial; UI fails closed; no business write. | Not run |
| STG-30.3-06 | Attempt cross-tenant assessment, detail, history, archive, and export access. | Tenant-safe not-found/denial behavior with no cross-tenant identifiers or metadata. | Not run |
| STG-30.3-07 | Submit Unclassified notes containing an explicit synthetic restricted marking such as `CUI//SP-PRVCY`. | No-CUI policy rejects and audits without retaining the submitted note or marking in audit metadata. | Not run |
| STG-30.3-08 | Exercise history, detail, archive/restore with reason, PDF export, and authorized download. | Immutable snapshot and classification metadata remain intact; lifecycle/export events are audit logged. | Not run |
| STG-30.3-09 | Inspect UI and PDF. | Score context, rule version/hash, generated date, review metadata, and exact draft/not-submitted language are visible; no certification or submission claim appears. | Not run |

## Closure Rule

Change the evidence status to `Passed` only after every case has actual deployed-environment results and retained evidence. If any prerequisite is unavailable, leave the record `Blocked`; if a case fails, record the defect, owner, and retest requirement. Qualified review and publication decisions must be retained separately and must not be inferred from a successful engineering test.

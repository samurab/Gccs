# Story 6.3 Content Review State Verification

Date: 2026-06-15

## Scope

Implemented and verified compliance content review states, customer-facing publication filtering, expert-review publication checks, retired-content mapping exclusion, and audit logging for state changes.

## Test Cases

- TC-6.3.1: Passed. Seeded draft and published obligations and verified only published content appears in customer-facing obligation repository results.
- TC-6.3.2: Passed. Verified expert-review-required content cannot be published without `reviewerUserId` and `reviewedAt`.
- TC-6.3.3: Passed. Retired content is excluded from new mapping eligibility and hidden from customer-facing obligation lookup.
- TC-6.3.4: Passed. Draft to in-review to approved to published to retired state changes emit audit events with before/after metadata.

## Commands

```bash
dotnet test tests/Gccs.Api.Tests/Gccs.Api.Tests.csproj --filter "FullyQualifiedName~ComplianceContentReviewStateTests"
dotnet test tests/Gccs.Api.Tests/Gccs.Api.Tests.csproj --filter "FullyQualifiedName~ComplianceContentImportTests|FullyQualifiedName~ComplianceContentReviewStateTests"
dotnet test tests/Gccs.Api.Tests/Gccs.Api.Tests.csproj
```

## Results

- Focused Story 6.3 tests: Passed, 4/4.
- Combined Story 6.2/6.3 tests: Passed, 8/8.
- API regression suite: Passed, 335/335.

## Notes

No frontend behavior changed for this story, so Vitest/lint/build were not run.

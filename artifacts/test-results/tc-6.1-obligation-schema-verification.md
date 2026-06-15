# Story 6.1 Obligation Schema Verification

Date: 2026-06-15
Story: 6.1 Obligation Schema

## Setup

- Test layer: domain validator and in-memory obligation repository.
- Schema changes: obligation publication metadata columns for review state, review confidence, last reviewed date, next review date, reviewer, expert-review flag, and flow-down flag.
- Validation target: `ObligationPublicationValidator`.

## TC-6.1.1 Source URL Required

Steps:
1. Build a publish-ready obligation with a missing source URL.
2. Run publication validation.

Expected:
- Validation fails before publication.

Actual:
- Validation returned a `sourceUrl` error.
- `EnsureCanPublish` threw `ObligationPublicationValidationException`.

Result: Passed.

## TC-6.1.2 Last Reviewed Date Required

Steps:
1. Build a publish-ready obligation with default source last-reviewed date.
2. Run publication validation.

Expected:
- Validation fails before publication.

Actual:
- Validation returned a `lastReviewedAt` error.

Result: Passed.

## TC-6.1.3 Required Metadata Enforced

Steps:
1. Build a publish candidate missing trigger condition, required action, owner, flow-down requirement, source confidence, review confidence, and published review state.
2. Run publication validation.
3. Verify a valid obligation carries risk, flow-down flag, confidence, and published review state.

Expected:
- Invalid content fails with actionable errors.
- Valid published content identifies risk, owner, confidence, review state, and flow-down metadata.

Actual:
- Validation returned errors for trigger, required action, owner, flow-down requirement, source confidence, review confidence, and review state.
- Valid obligation passed validation with `RiskLevel.High`, `RequiresFlowDown=true`, and `ReviewState.Published`.

Result: Passed.

## TC-6.1.4 Evidence Examples Link

Steps:
1. Fetch `far-52-204-21` from the obligation repository.
2. Inspect evidence examples and review metadata.

Expected:
- Linked evidence examples are returned with the obligation.

Actual:
- Obligation returned evidence examples including `Access control policy`.
- Obligation returned published review metadata.

Result: Passed.

## Commands

- `dotnet test tests/Gccs.Api.Tests/Gccs.Api.Tests.csproj --filter "FullyQualifiedName~ObligationSchemaTests"`: Passed, 4 tests.
- `dotnet test tests/Gccs.Api.Tests/Gccs.Api.Tests.csproj`: Passed, 327 tests.

Defects: None open.

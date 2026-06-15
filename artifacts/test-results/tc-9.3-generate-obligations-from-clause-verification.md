# TC-9.3 Generate Obligations From Clause Verification

## Scope

- TC-9.3.1: Attach a clause with mapped templates and verify contract-specific obligations are generated.
- TC-9.3.2: Verify generated obligations link to contract/clause and include source URL, owner, action, evidence examples, risk, confidence, and review metadata.
- TC-9.3.3: For templates requiring default tasks, verify tasks are created and linked.
- TC-9.3.4: Re-run generation or reprocess the same attachment and verify duplicate obligations/tasks are not created.

## Implementation Summary

- Clause attachment now invokes obligation generation for published clause templates.
- Added an idempotent generation endpoint for reprocessing an attached contract clause.
- Generated mappings preserve the contract clause, source obligation metadata, and tenant scope.
- Default obligation tasks are created only once per tenant, contract, and obligation.

## Verification Results

- `dotnet test tests/Gccs.Api.Tests/Gccs.Api.Tests.csproj --filter "FullyQualifiedName~ContractClauseObligationGenerationTests"`: Passed, 4/4.
- `npm --workspace apps/web run lint && npm --workspace apps/web run build`: Passed.
- `dotnet test tests/Gccs.Api.Tests/Gccs.Api.Tests.csproj`: Passed, 371/371.
- `dotnet test tests/Gccs.Api.Tests/Gccs.Api.Tests.csproj --filter "FullyQualifiedName~DevelopmentStoryRegressionCoverageTests"`: Passed, 216/216 after rerunning sequentially when a parallel test run hit a transient MVC manifest file lock.
- `npm test`: Passed; API tests 371/371 and web tests 17/17.

## Result

Story 9.3 acceptance criteria and test cases are covered by focused API integration tests, regression coverage checks, and full project test verification.

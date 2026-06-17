

## Required Regression Rule

After implementing any user story:

1. Find the matching story in [development-story-test-cases.md](development-story-test-cases.md).
2. Create or update focused automated tests for every `TC-*` case under that story.
3. Run the narrowest relevant tests first.
4. Run the development-story regression coverage test so the story/test-case inventory still matches the docs.
5. Run the broader suite required by the files changed.
6. Report commands, pass/fail result, skipped checks, and any remaining manual verification.

The regression coverage harness is useful, but it is not a substitute for functional tests. It verifies that every documented `TC-*` case has an executable regression strategy; feature implementation still needs focused backend, frontend, integration, or smoke tests.

## Command Selection


| Change type | Minimum command |
| --- | --- |
| Backend/domain/application/API only | `dotnet test tests/Gccs.Api.Tests/Gccs.Api.Tests.csproj` |
| Frontend UI only | `npm run test:web` |
| Frontend behavior or styling changed | `npm run lint:web && npm run test:web && npm run build:web` |
| Backend and frontend changed | `npm test` |
| Local dependency, Docker, health, or environment behavior changed | `dotnet test tests/Gccs.Api.Tests/Gccs.Api.Tests.csproj --filter "FullyQualifiedName~LocalDependencyConfigurationTests"` plus the relevant broader suite |
| CI, migration, build, or release pipeline changed | `dotnet test tests/Gccs.Api.Tests/Gccs.Api.Tests.csproj --filter "FullyQualifiedName~ContinuousIntegrationBaselineTests"` plus the relevant broader suite |
| Security, RBAC, tenant isolation, upload, evidence, audit, or reporting changed | `dotnet test tests/Gccs.Api.Tests/Gccs.Api.Tests.csproj` and any matching frontend tests |



The implementation for Story <story id>: <story title> is complete.

Please execute the regression workflow for this story and please provide results of the test:

1. Read docs/development-story-test-cases.md and locate Story <story id>.
2. For every TC-* case under that story, confirm there is focused automated coverage or add/update it now.
3. Keep these invariants in scope for every functional case: tenant scoping, server-side RBAC, audit logging, CUI/data-handling controls, and standard API/UI error behavior.
4. Run the narrowest relevant test command for the changed files.
5. Run the development-story regression coverage test or the backend test suite that contains it.
6. If UI changed, also run lint, Vitest, and the web production build.
7. Report exact commands, pass/fail results, any skipped checks, and any manual verification still required.

Do not treat the manifest/coverage test as the only regression. Add focused functional tests for the story behavior before reporting completion.

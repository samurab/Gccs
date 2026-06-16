# Regression Test Execution Prompts

Use this document after every user story implementation. The best default is a **user-story-level regression prompt**, not one prompt per test case. Each story already groups the relevant `TC-*` cases in [development-story-test-cases.md](development-story-test-cases.md), so a story prompt keeps the workflow focused while still requiring every case for that story to be addressed.

Use a test-case-level prompt only when one case is risky, failed, newly discovered, or needs deeper implementation than the rest of the story.

## Required Regression Rule

Acceptance criteria must satisfy the testability standard in [development-phase-use-cases.md](development-phase-use-cases.md): each criterion needs an actor/system, action or input, observable result, and applicable invariant. If a story criterion cannot be mapped to a focused `TC-*` case, tighten the criterion or add the missing test case before implementation is considered complete.

After implementing any user story:

1. Find the matching story in [development-story-test-cases.md](development-story-test-cases.md).
2. Create or update focused automated tests for every `TC-*` case under that story.
3. Run the narrowest relevant tests first.
4. Run the development-story regression coverage test so the story/test-case inventory still matches the docs.
5. Run the broader suite required by the files changed.
6. Report commands, pass/fail result, skipped checks, and any remaining manual verification.

The regression coverage harness is useful, but it is not a substitute for functional tests. It verifies that every documented `TC-*` case has an executable regression strategy; feature implementation still needs focused backend, frontend, integration, or smoke tests.

## Command Selection

Use these defaults unless the story needs more:

| Change type | Minimum command |
| --- | --- |
| Backend/domain/application/API only | `dotnet test tests/Gccs.Api.Tests/Gccs.Api.Tests.csproj` |
| Frontend UI only | `npm run test:web` |
| Frontend behavior or styling changed | `npm run lint:web && npm run test:web && npm run build:web` |
| Backend and frontend changed | `npm test` |
| Local dependency, Docker, health, or environment behavior changed | `dotnet test tests/Gccs.Api.Tests/Gccs.Api.Tests.csproj --filter "FullyQualifiedName~LocalDependencyConfigurationTests"` plus the relevant broader suite |
| CI, migration, build, or release pipeline changed | `dotnet test tests/Gccs.Api.Tests/Gccs.Api.Tests.csproj --filter "FullyQualifiedName~ContinuousIntegrationBaselineTests"` plus the relevant broader suite |
| Security, RBAC, tenant isolation, upload, evidence, audit, or reporting changed | `dotnet test tests/Gccs.Api.Tests/Gccs.Api.Tests.csproj` and any matching frontend tests |

## User Story Regression Prompt

Copy this prompt after implementing a story:

```text
The implementation for Story <story id>: <story title> is complete.

Please execute the regression workflow for this story:

1. Read docs/development-story-test-cases.md and locate Story <story id>.
2. For every TC-* case under that story, confirm there is focused automated coverage or add/update it now.
3. Keep these invariants in scope for every functional case: tenant scoping, server-side RBAC, audit logging, No-CUI controls, and standard API/UI error behavior.
4. Run the narrowest relevant test command for the changed files.
5. Run the development-story regression coverage test or the backend test suite that contains it.
6. If UI changed, also run lint, Vitest, and the web production build.
7. Report exact commands, pass/fail results, any skipped checks, and any manual verification still required.

Do not treat the manifest/coverage test as the only regression. Add focused functional tests for the story behavior before reporting completion.
```

## Test Case Deep-Dive Prompt

Use this only for a specific case that needs extra attention:

```text
Please create or update regression coverage for <TC id> from docs/development-story-test-cases.md.

Requirements:

1. Read the parent story and all sibling TC-* cases so this test fits the intended workflow.
2. Implement the narrowest focused automated test for <TC id> using the repo's existing test patterns.
3. Include tenant scoping, server-side RBAC, audit logging, No-CUI controls, and standard error behavior where relevant.
4. Prefer backend xUnit tests for domain/application/API enforcement, Vitest/React Testing Library for UI behavior, and integration/smoke tests only when the workflow crosses boundaries.
5. Run the narrowest relevant test command, then run the development-story regression coverage test.
6. Report exact commands, results, and any remaining coverage gap.
```

## Full Story Batch Prompt

Use this when several stories were implemented together:

```text
The implementation includes these stories: <story ids and titles>.

Please run the regression workflow for the batch:

1. Read docs/development-story-test-cases.md and collect every TC-* case under the listed stories.
2. Confirm focused automated coverage exists for each case or add/update the missing tests.
3. Check shared invariants across the batch: tenant scoping, server-side RBAC, audit logging, No-CUI controls, and standard API/UI error behavior.
4. Run the narrowest tests for the changed areas, then run the relevant broader suite:
   - backend/API changes: dotnet test tests/Gccs.Api.Tests/Gccs.Api.Tests.csproj
   - frontend changes: npm run lint:web && npm run test:web && npm run build:web
   - both: npm test plus any build/lint commands not included
5. Report exact commands, pass/fail results, skipped checks, and unresolved manual verification.
```

## Pre-Handoff Regression Prompt

Use this before handing off a completed branch or PR:

```text
Please perform pre-handoff regression validation for the current branch.

1. Inspect the changed files and identify the affected development stories and TC-* cases.
2. Confirm every affected story has focused automated coverage for its relevant TC-* cases.
3. Run the development-story regression coverage test.
4. Run the full applicable local suite:
   - dotnet test tests/Gccs.Api.Tests/Gccs.Api.Tests.csproj
   - npm run lint:web, npm run test:web, and npm run build:web when frontend files changed
   - npm test when both backend and frontend behavior changed
5. Summarize changed files, affected stories, commands run, results, skipped checks, and residual risk.
```

## Recommended Practice

Use the **User Story Regression Prompt** after each story. Use the **Test Case Deep-Dive Prompt** only when a specific `TC-*` case fails, is high risk, or needs a targeted fix. This gives you story-level discipline without turning each implementation into a 212-prompt ceremony.

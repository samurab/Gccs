# Story 5.2 Audit Log Viewer Verification

Date: 2026-06-15
Story: 5.2 Audit Log Viewer

## Setup

- Local test host: `WebApplicationFactory<Program>` with EF Core in-memory databases per test case.
- Auth context: development auth headers with role or permission claims.
- UI test harness: React Testing Library with mocked GCCS API helpers.
- Audit viewer endpoint: `GET /api/audit-logs`.

## TC-5.2.1 Admin/Owner/Advisor Tenant Scope

Steps:
1. Seed audit events for two tenants.
2. Call `GET /api/audit-logs?page=1&pageSize=25` as Owner, Admin, and Advisor roles.
3. Inspect returned items.

Expected:
- Authorized roles can view audit events.
- Only current-tenant events are returned.

Actual:
- Each authorized role received `200 OK`.
- Response included only current-tenant audit events.
- Cross-tenant events were excluded.

Result: Passed.

## TC-5.2.2 Unauthorized Access Blocked

Steps:
1. Call `GET /api/audit-logs` as Contributor and Auditor roles.

Expected:
- Contributor and Auditor are denied.

Actual:
- Both requests returned `403 Forbidden`.

Result: Passed.

## TC-5.2.3 Pagination Works

Steps:
1. Seed six audit events.
2. Call page 1 and page 2 with `pageSize=2`.
3. Verify item count, next/previous flags, and newest-first stable ordering.

Expected:
- Page size is respected.
- Pagination flags are correct.
- Ordering is stable.

Actual:
- Page 1 returned two newest events with `hasNextPage=true`, `hasPreviousPage=false`.
- Page 2 returned the next two events with `hasNextPage=true`, `hasPreviousPage=true`.

Result: Passed.

## TC-5.2.4 Filters Correct And Tenant Scoped

Steps:
1. Seed events across tenants with different actor IDs, actions, entity types, and timestamps.
2. Filter by actor, action, entity type, and date range.
3. Inspect returned items.

Expected:
- Only matching current-tenant events are returned.

Actual:
- Response returned the single matching current-tenant event.
- Wrong action, entity, actor, and tenant events were excluded.
- UI renders audit rows, filter controls, and next/previous paging.

Result: Passed.

## Commands

- `dotnet test tests/Gccs.Api.Tests/Gccs.Api.Tests.csproj --filter "FullyQualifiedName~AuditLogViewerTests"`: Passed, 7 tests.
- `npm run test:web`: Passed, 9 tests.
- `dotnet test tests/Gccs.Api.Tests/Gccs.Api.Tests.csproj`: Passed, 323 tests.
- `npm run lint:web`: Passed.
- `npm run build:web`: Passed.
- `npm test`: Passed, 323 API tests and 9 web tests.

Defects: None open.

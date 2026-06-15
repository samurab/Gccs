# Story 5.1 Append-Only Audit Events Verification

Date: 2026-06-15
Story: 5.1 Append-Only Audit Events

## Setup

- Local test host: `WebApplicationFactory<Program>` with EF Core in-memory databases per test case.
- Auth context: development auth headers with story-specific tenant/user IDs and required permissions.
- Representative sensitive action: No-CUI acknowledgement, which writes a compliance-relevant audit event.
- Append-only guard route: `PUT`, `PATCH`, and `DELETE /api/audit-logs/{auditLogEntryId}`.

## TC-5.1.1 Sensitive Action Creates Event

Steps:
1. POST `/api/no-cui-acknowledgement` with `ManageEvidence`.
2. Inspect the persisted audit entry.

Expected:
- Sensitive action writes an audit event.
- Event includes tenant, actor, action, entity, timestamp, and summary.

Actual:
- Response was `200 OK`.
- Audit event included tenant ID, actor user ID, `AuditAction.Created`, entity type `NoCuiAcknowledgement`, entity ID, occurrence timestamp, and acknowledgement summary.

Result: Passed.

## TC-5.1.2 Audit Events Are Append-Only

Steps:
1. Seed an audit event.
2. Attempt `PUT`, `PATCH`, and `DELETE` on `/api/audit-logs/{auditLogEntryId}` with `ViewAuditLog`.
3. Re-read the persisted audit event.

Expected:
- Normal application APIs do not edit or delete audit events.
- API returns a clear append-only response.

Actual:
- All mutation attempts returned `405 Method Not Allowed`.
- Problem response included `audit_log_append_only`.
- Seeded audit event summary and action remained unchanged.

Result: Passed.

## TC-5.1.3 Critical Audit Failure Surfaces

Steps:
1. Replace `IAuditEventWriter` with a failing test writer.
2. POST `/api/no-cui-acknowledgement`.

Expected:
- Critical audit failure fails the request or surfaces a clear critical error.

Actual:
- Response was `500 Internal Server Error`.
- Problem response included `Critical audit failure` and `audit_write_failed`.

Result: Passed.

## TC-5.1.4 Request Metadata Captured

Steps:
1. POST `/api/no-cui-acknowledgement` with `X-Correlation-ID`, `X-Forwarded-For`, and `User-Agent`.
2. Inspect the audit entry.

Expected:
- Source IP, correlation ID, and request metadata are stored when available.

Actual:
- Audit entry stored source IP from `X-Forwarded-For`.
- Audit entry stored user agent and correlation ID.
- Metadata JSON retained the correlation ID for backward compatibility.

Result: Passed.

## Commands

- `dotnet test tests/Gccs.Api.Tests/Gccs.Api.Tests.csproj --filter "FullyQualifiedName~AuditAppendOnlyTests"`: Passed, 4 tests.
- `dotnet test tests/Gccs.Api.Tests/Gccs.Api.Tests.csproj`: Passed, 316 tests.

Note: An earlier parallel run of the narrow and full backend commands caused an MSBuild file-lock on `MvcTestingAppManifest.json`; rerunning the narrow command by itself passed.

Defects: None open.

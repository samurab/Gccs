# Story 4.2 Upload Guardrails Verification

Date: 2026-06-15
Story: 4.2 Upload Guardrails

## Setup

- Local test host: `WebApplicationFactory<Program>` with EF Core in-memory databases per test case.
- Auth context: development auth headers with `ManageEvidence` permission.
- No-CUI prerequisite: seeded current-version No-CUI acknowledgement for upload guardrail test users.
- Guardrail configuration: PDF, PNG, JPG/JPEG, TXT, CSV, DOCX, and XLSX allowed; maximum size `26214400` bytes.

## TC-4.2.1 Disallowed File Type Rejected

Steps:
1. POST `/api/evidence-items/{evidenceItemId}/upload-intents`.
2. Use `installer.exe`, content type `application/x-msdownload`, size `1024`.
3. Inspect response, evidence metadata, and audit log.

Expected:
- API rejects the upload server-side.
- No usable evidence item is created.
- Rejected upload attempt is audit logged.

Actual:
- Response was `400 Bad Request` with `Evidence upload rejected`.
- Response included `File type '.exe' is not allowed`.
- No `evidence_items` row existed for the tenant.
- `AuditAction.Rejected` event was written for `EvidenceUploadIntent`.

Result: Passed.

## TC-4.2.2 Oversized File Rejected

Steps:
1. POST a valid PDF upload intent with size `26214401`.
2. Inspect response and evidence metadata.

Expected:
- API rejects the upload server-side.
- No usable evidence item is created.

Actual:
- Response was `400 Bad Request`.
- Response included `File size exceeds`.
- No `evidence_items` row existed for the tenant.

Result: Passed.

## TC-4.2.3 Scan Status Recorded

Steps:
1. POST a valid PDF upload intent with content type `application/pdf` and size `2048`.
2. Inspect response and persisted evidence metadata.

Expected:
- Upload intent is created.
- Metadata records validation status and malware scan placeholder status.
- Evidence is not usable until later storage and scan workflows complete.

Actual:
- Response was `201 Created`.
- Response included `validationStatus: accepted` and `malwareScanStatus: scan-pending`.
- `evidence_items` row recorded original file name, content type, size, validation status, and scan status.
- Evidence status was `InReview` with no `storage_uri` or `file_hash`.

Result: Passed.

## TC-4.2.4 Failed Upload Audit

Steps:
1. POST `policy.pdf` with mismatched content type `image/png`.
2. Inspect response, evidence metadata, and audit log.

Expected:
- API rejects the failed validation.
- No usable evidence item is created.
- Rejected upload attempt is audit logged.

Actual:
- Response was `400 Bad Request`.
- No `evidence_items` row existed for the tenant.
- `AuditAction.Rejected` event was written with file name, content type, size, max size, allowed extensions, and validation error metadata.

Result: Passed.

## Commands

- `dotnet test tests/Gccs.Api.Tests/Gccs.Api.Tests.csproj --filter "FullyQualifiedName~NoCuiAcknowledgementTests"`: Passed, 8 tests.
- `npm run test:web`: Passed, 8 tests.
- `dotnet test tests/Gccs.Api.Tests/Gccs.Api.Tests.csproj`: Passed, 312 tests.
- `npm run lint:web`: Passed.
- `npm run build:web`: Passed.
- `npm test`: Passed, 312 API tests and 8 web tests.

Defects: None open.

# Story 8.2 Contract Document Metadata And Upload Verification

Date: 2026-06-15

## Scope

Implemented contract document metadata upload and delete support for tenant-scoped contracts. Uploads require the current No-CUI acknowledgement, validate file type/content type/size against existing No-CUI guardrails, store metadata with validation and malware scan placeholder status, link documents to contracts, and audit upload/delete/rejection events. The Contracts UI now shows a document metadata upload panel that is disabled until No-CUI acknowledgement is present.

## Test Cases

- TC-8.2.1: Passed. Contract document upload is disabled in the UI before acknowledgement and rejected by the API with HTTP 428.
- TC-8.2.2: Passed. Valid non-CUI document metadata persists with document type, storage reference, scan status, validation status, and contract link.
- TC-8.2.3: Passed. Unsupported document files are rejected and no usable document metadata is created.
- TC-8.2.4: Passed. Upload and delete actions write ContractDocument audit events.

## Commands

```bash
dotnet test tests/Gccs.Api.Tests/Gccs.Api.Tests.csproj --filter "FullyQualifiedName~ContractRecordTests"
npm --workspace apps/web run test:run -- App.test.tsx
dotnet ef migrations add AddContractDocumentUploadMetadata --project src/Gccs.Infrastructure --startup-project apps/api --output-dir Persistence/Migrations
dotnet test tests/Gccs.Api.Tests/Gccs.Api.Tests.csproj
npm --workspace apps/web run test:run
npm --workspace apps/web run build
npm --workspace apps/web run lint
npm test
```

## Results

- Focused contract API tests: Passed, 8/8.
- Focused web tests: Passed, 14/14.
- API regression suite: Passed, 355/355.
- Web tests: Passed, 14/14.
- Web build: Passed.
- Web lint: Passed.
- Root npm test: Passed.

## Notes

EF migration `20260615033018_AddContractDocumentUploadMetadata` was generated for contract document metadata columns. Actual object storage remains a metadata placeholder in the No-CUI MVP; accepted documents receive a `pending://` storage reference and `scan-pending` malware status.

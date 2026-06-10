# GCCS API Specification

The source-of-truth API contract is [`openapi.yaml`](openapi.yaml).

## Scope

The specification covers the MVP API for a No-CUI government contractor compliance SaaS:

- System health and compliance workspace overview
- Company compliance profile and SAM lookup job intake
- Contract, solicitation, subcontract, purchase order, and document intake
- Clause capture and contract-specific obligation evaluation
- Source-backed obligation library
- Compliance tasks and calendar events
- Evidence vault metadata, upload intents, versions, and reviews
- CMMC readiness assessments, control statuses, and POA&M item metadata
- Subcontractor profiles, flow-down clauses, and evidence requests
- Report generation and downloads
- Tenant audit logs
- Compliance source library references

## Contract Assumptions

- The API is tenant-scoped through the authenticated user context.
- `Authorization: Bearer <token>` is the default security model, even if local development starts with simplified auth.
- Local development may send `X-Gccs-Dev-Auth: true` to use the development-only auth handler. Optional headers are `X-Gccs-Dev-Tenant`, `X-Gccs-Dev-User`, `X-Gccs-Dev-Email`, and `X-Gccs-Dev-Permissions`.
- The MVP data posture is **No-CUI / compliance management only**.
- Document and evidence upload intents require a positive No-CUI attestation.
- All source-backed compliance records include source URL, source type, last-reviewed date, confidence, and expert-review flags where applicable.
- SPRS score calculation, eSRS integration, SSP generation, and full AI assistant workflows are deferred from the MVP unless a pilot deal requires explicit scope approval.
- Long-running work, such as SAM lookup, contract extraction, obligation evaluation, and report generation, returns `202 Accepted` with a job ID.
- Paged list endpoints use `page` and `pageSize`, with `pageSize` capped at 100.

## Suggested Implementation Order

1. Keep the existing `/health`, `/api/compliance/overview`, `/api/obligations`, and `/api/obligations/{obligationId}` endpoints aligned with the spec.
2. Add company profile and contract intake endpoints.
3. Add manual clause tagging and contract obligation matrix endpoints.
4. Add task/calendar endpoints and evidence metadata endpoints.
5. Add upload intents, malware scan status, and version tracking.
6. Add CMMC assessment, subcontractor, report, audit log, and source library endpoints.

## Validation

The spec is OpenAPI 3.1 YAML. A basic local parse/reference check can be run with:

```bash
ruby -ryaml -e 'doc = YAML.load_file("docs/api/openapi.yaml"); puts doc["openapi"]'
```

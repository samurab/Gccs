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
- Paged tenant-scoped task search with canonical lower-snake-case status codes
- Evidence vault metadata, stateless upload guardrail preflights, durable file versions, and reviews
- CMMC readiness assessments, control statuses, and POA&M item metadata
- Structured SSP section lifecycle management; deterministic, source-backed SSP narrative drafting, editing, comparison, and approval; and immutable SSP internal-review exports
- Subcontractor profiles, flow-down clauses, and evidence requests
- Tenant-scoped subcontracting report data collection, CSV import, evidence links, review decisions, immutable SPR preparation packages, audited exports, and append-only user-recorded external receipts
- Tenant-scoped labor applicability decisions with explicit SCA/DBA/FAR Part 22 fields, source and evidence links, review metadata, task generation, and guarded wage determination uploads
- Report generation and downloads
- Tenant audit logs
- Compliance source library references
- Source-bounded assistant questions with approved retrieval citations, mandatory review status, and unsupported-answer refusal
- Tenant-scoped AI interaction logs, append-only human review decisions, retention metadata, controlled export, and approved-output deliverable provenance
- External reviewer package dashboard, append-only comments/questions, and controlled eligible-package downloads

## Contract Assumptions

- The API is tenant-scoped through the authenticated user context.
- `Authorization: Bearer <token>` is the default security model, even if local development starts with simplified auth.
- Local development may send `X-Gccs-Dev-Auth: true` to use the development-only auth handler. Optional headers are `X-Gccs-Dev-Tenant`, `X-Gccs-Dev-User`, `X-Gccs-Dev-Email`, and `X-Gccs-Dev-Permissions`.
- The MVP data posture is **No-CUI / compliance management only**.
- Document upload intents and evidence upload preflights require a positive No-CUI attestation. An accepted evidence preflight does not create or increment a durable file version; successful byte upload does.
- All source-backed compliance records include source URL, source type, last-reviewed date, confidence, and expert-review flags where applicable.
- Assistant retrieval is server-authorized by source family, bounded after PostgreSQL full-text relevance ranking, and limited to published obligation content plus approved current-tenant document excerpts, report-package metadata, and explicitly allowed evidence metadata. Assistant output remains draft-only and is not a legal, certification, or government determination.
- Assistant prompts that trigger prohibited-data or unsupported-request guardrails are stored only as a redacted placeholder. AI output begins in `Draft` or `NeedsReview`; `ManageObligations` governs review decisions and `ExportReports` governs log export. Approved unexpired output must be explicitly linked to an existing current-tenant target before governed deliverable use; the declared type is additionally checked against `ManageReports` for reports/customer deliverables, `ManageObligations` for policies, or `ManageCmmc` for SSPs/POA&Ms. `AiOutputRetentionProcessing:RetentionDays` sets the deployment retention period from 1 through 3,650 days (365 by default); an enabled database-backed worker archives expired output hourly in bounded, concurrently claimed batches. Explicit provenance links do not detect unreported copy-and-paste use.
- Report, generated-policy, SSP narrative, POA&M, and SSP customer-package creation requests accept an optional `aiOutputId`. When supplied, approval, expiry, tenant validation, deliverable creation, provenance persistence, and audit writes share the application transaction; an invalid reference rolls the entire mutation back. Existing clients that do not declare AI use remain compatible.
- Structured SSP sections, deterministic source-backed narrative generation, tenant-scoped SSP review packages, and tenant-scoped draft SPRS calculation history are implemented. SPRS calculation requires `ManageCmmc`, history requires `ViewCmmc`, and only a published, effective, reviewed scoring rule set can be used. The checked-in 110-rule baseline remains draft, so production calculation is fail-closed until qualified review and publication. FeDril does not submit calculated values to SPRS. SSP export requests carry evidence and POA&M IDs only; the API resolves tenant ownership and evidence eligibility, snapshots approved narrative/source/reviewer metadata, requires `ExportReports`, and audit logs package generation. Packages are internal-review artifacts until a separate `ManageTenant` approval is recorded; sharing without that approval is blocked. SAM.gov Subcontracting Plan Reporting data collection and immutable preparation-package review are implemented through canonical `/subcontracting-plan-report-data` and `/subcontracting-plan-reports` routes. Manual receipts are user-recorded external history, not FeDril verification. The installed submission provider is disabled and direct submission fails closed; legacy `/esrs` routes remain compatibility aliases. `AiAssisted` is fail-closed until an approved provider adapter is configured and must not be represented as available AI functionality.
- Generated-policy creation and editing require explicit user-selected classification metadata. Existing clients that posted an empty generation body must now send `classification`; this is a deliberate fail-closed contract change, and existing stored policies migrate to `Unknown` until reviewed.
- Long-running work, such as SAM lookup, contract extraction, obligation evaluation, and report generation, returns `202 Accepted` with a job ID.
- External package-review routes derive tenant scope from a durable invitation and never accept a tenant ID. They require verified customer identity claims, recheck invitation/share expiration and revocation on every request, expose only active Unclassified/FCI package projections, and revalidate current referenced evidence before review or download. Generic completed reports become externally reviewable only through an explicit tenant-admin share; this is not an independent internal approval lifecycle. Reviewer messages are append-only and do not mutate source packages. Download permission comes from the invitation, and watermarking comes from server configuration.
- External-review shares capture a separate review deadline plus approval actor/time/reason and the approved source version/fingerprint. Tenant administrators can prepare immutable obligation-matrix and sanitized audit-history report snapshots with source-specific permissions; preparation does not itself grant portal access, and each package must still be invitation-scoped and explicitly shared.
- Paged list endpoints use `page` and `pageSize`, with `pageSize` capped at 100.
- Legacy PascalCase obligation task statuses remain readable during client migration; `statusCode` and task search use the canonical lower-snake-case values.

## Compliance task compatibility and pagination

- `GET /api/tasks` is a deprecated, unpaged compatibility endpoint. It returns
  `Deprecation: true` and a `Link` header identifying `GET /api/tasks/search` as
  its successor. Removal requires a coordinated major contract release after
  measured consumer migration.
- New consumers use `GET /api/tasks/search`. Offset requests are capped at
  100,000 skipped records. Cursor requests use the returned `nextCursor`, cannot
  include `page`, and are authenticated and bound to the tenant, filters, and
  page size. Cursors expire after 15 minutes; clients restart the search when a
  cursor is rejected.
- Task status input accepts deprecated PascalCase aliases during the migration
  window. New consumers send and read canonical lower-snake-case status codes.
- Operational retirement evidence is emitted through structured events
  `LegacyTaskListUsed` and `LegacyTaskStatusInputUsed`, and low-cardinality meter
  `Gccs.Api.TaskCompatibility`. Status values, tenant IDs, and user IDs are not
  metric labels. Remove compatibility behavior only after known consumers are
  migrated and production records no legacy traffic for the agreed observation
  window.
- Cursor authentication uses a dedicated HMAC key rather than an instance-local
  key ring. The production and staging workflows validate and install the same
  base64-encoded key on every API instance, and an optional previous key supports
  zero-downtime rotation across the 15-minute cursor lifetime.
- `.github/workflows/task-api-compatibility-observation.yml` queries the existing
  Application Insights resource weekly and emits a No-CUI retirement-evidence
  artifact. It fails closed when telemetry heartbeats are absent or any legacy
  usage occurred during the preceding 30 days.

## Suggested Implementation Order

1. Keep the existing `/health`, `/api/compliance/overview`, `/api/obligations`, and `/api/obligations/{obligationId}` endpoints aligned with the spec.
2. Add company profile and contract intake endpoints.
3. Add manual clause tagging and contract obligation matrix endpoints.
4. Add task/calendar endpoints and evidence metadata endpoints.
5. Add upload intents, malware scan status, and version tracking.
6. Add CMMC assessment, subcontractor, report, audit log, and source library endpoints.

## Validation

The spec is OpenAPI 3.1 YAML. Run semantic validation, including local `$ref` resolution, with:

```bash
npm run lint:openapi
```

For a syntax-only YAML parse:

```bash
ruby -ryaml -e 'doc = YAML.load_file("docs/api/openapi.yaml"); puts doc["openapi"]'
```

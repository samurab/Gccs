# Phase 1A integration completion — implementation and compatibility plan

Status: Partially implemented. No-CUI/sandbox integration is authorized and in progress;
production CUI enablement is not established by these changes.

## Implementation checkpoint — 2026-09-07

### Follow-up integration checkpoint

Implemented in the working tree after the original checkpoint:

- Audit export reads beyond the first 100 entries. The EF adapter streams one
  tenant-scoped ordered query. Matching results above 10,000 cause an explicit
  413 response rather than a partial successful export; callers must narrow the
  date range. A large asynchronous export facility remains future work.
- Current notice endpoints resolve the mode from the tenant repository rather
  than trusting the query/request. Future-effective notices are not selected.
  Workflow users can retrieve and acknowledge their own notices. The web
  workspace exposes version-aware explicit acknowledgement.
- Notice acknowledgement and its audit event share a transaction. PostgreSQL
  tenant-row locking protects the mode check and serializes duplicate submissions.
  A real PostgreSQL test proves rollback on audit failure and idempotency across
  eight concurrent acknowledgement attempts.
- Demo seed/reset and audit use one transaction. Because the legacy demo seed
  uses global catalog rows and fixed IDs, both operations now reject databases
  containing any tenant other than the selected DemoSandbox tenant. PostgreSQL
  table locking protects this isolation check during the operation. This does
  not provision or verify isolated storage, credentials, or outbound integrations.

Verification for this follow-up: 26 focused API tests, one additional PostgreSQL
transaction/concurrency test, 57 App tests, and one notice-panel test passed.
API/web builds, web lint, and diff whitespace checks passed. Authenticated browser
and deployment verification have not been completed for this follow-up.

Still incomplete: connecting the current-notice guard to every affected use case
and worker, full version-bound containment/release, durable external cleanup and
notification delivery, and deployment-specific security/recovery evidence.
Primary/backup incident contacts and approved RPO/RTO have been requested from
the operator; no approval or recovery evidence has been fabricated. Cloud
operations and deployment remain subject to separate target/cost approval.

Implemented in the working tree:

- Evidence uploads target an existing tenant-owned identity, allocate replacement
  versions under a PostgreSQL row lock, and persist metadata with their audit event.
- Multipart evidence uploads consume explicit classification. Undefined enums and
  caller-supplied reviewer/demo provenance are rejected at the affected intake services.
- Evidence download checks both version and current parent classification;
  extraction checks persisted document classification; report artifact access and
  evidence-inventory export check persisted classifications.
- Unresolved escalations are consulted by the shared tenant workflow policy when
  a content reference is supplied. Generic updates cannot resolve an escalation.
- Evidence review, metadata, acknowledgement, classification-review and escalation
  mutations use the shared transaction boundary. Policy rejection audit events
  survive business rollback in that boundary.

Verification: 214 relevant API tests passed with the local PostgreSQL test connection
configured, plus 3 classification validation tests; 57 App component tests passed.
API/web builds and web lint passed. These are development checks, not launch evidence.
Playwright opened the local workspace, but tenant context did not load in the
inspected snapshot; authenticated browser upload verification remains unproven.

Remaining integration and limitations:

- The original six-area readiness plan below is not complete. Versioned notice
  enforcement, a common classified-note model, durable security/incident approval
  workflows, and complete audit-export coverage remain open.
- Content checks are metadata-based, not automatic CUI detection. Full endpoint,
  role, concurrent mode-change and containment race coverage is not yet proven.
- Blob cleanup still uses compensation rather than a durable cleanup outbox.
- Escalation intake still needs authoritative reference validation and a complete
  affected-resource inventory. An unresolved flag is not proof of universal containment.
- Classification review and immutable file-version classification need a deliberate
  version-bound release workflow; reclassifying the parent does not release an
  Unknown file version automatically.

The following defect table records the initial inspection, before this checkpoint.

This internal engineering plan covers Stories 1A.2.1–1A.2.2 and
1A.6.1–1A.9.3, plus their dependencies on tenant approval and mode changes.
Inspection date: 2026-09-07. Existing uncommitted evidence identity and audit
transaction fixes remain in the workspace; they are not evidence that Phase 1A
integration is complete.

## Current implementation and defects

| Area | Status | Observed implementation | Missing integration |
| --- | --- | --- | --- |
| Classification | Partially implemented | Shared metadata/policy and evidence review; contract documents appear in the review queue. | Evidence multipart uploads ignore the posted classification object; review metadata can come from the caller; contract document review and all downstream classification checks need completion. |
| Notices | Partially implemented | Published catalog, acknowledgement API and persistence. | `EnsureAcknowledgedAsync` has no production callers. The catalog lacks `ClassifiedNote` and `ExtractionJob` workflow contexts. Ordinary workflow users lack access to notice APIs protected by `ManageTenant`. |
| Escalations | Partially implemented | Tenant-scoped records, status, resolutions and a blocked flag. | No processing/download/report/approval service consults that flag. Intake accepts arbitrary affected references. Assignment can change status without the status-transition validation. |
| Audit | Partially implemented | Event catalog, writer and tenant-scoped export API. | Required event metadata is inconsistent; export filters only the first 100 rows. Classification, notice and escalation mutations still need shared transactions. |
| Security review | Partially implemented | Static validators for review areas, findings and accepted risk. | No durable review workflow or authoritative connection to checklist approval and mode change. |
| Incident readiness | Partially implemented | Static playbook/tabletop/gap validators. | No durable records, approval workflow, review reminders or authoritative tenant approval gate. |

Evidence locations:

- `src/Gccs.Application/Common/ContentClassificationPolicy.cs`
- `src/Gccs.Application/Common/ContentClassificationReview.cs`
- `src/Gccs.Infrastructure/Common/EfContentClassificationReviewRepository.cs`
- `apps/api/Program.cs` evidence multipart, notice and escalation routes
- `src/Gccs.Application/Tenancy/DataHandlingNoticeAcknowledgements.cs`
- `packages/compliance-content/data-handling-notices/notices.json`
- `src/Gccs.Infrastructure/Tenancy/EfCuiSupportEscalationRepository.cs`
- `src/Gccs.Application/Audit/CuiAuditExportService.cs`
- `src/Gccs.Application/Tenancy/SecurityReviewChecklist.cs`
- `src/Gccs.Application/Tenancy/IncidentResponseReadiness.cs`
- `src/Gccs.Application/Tenancy/CuiReadyApprovalChecklists.cs`

## Architecture

1. Keep endpoints responsible for authentication, permission checks, tenant
   context, request parsing and the standard error response. Resolve current
   tenant mode on the server. Do not trust the mode in an acknowledgement request.
2. Put notice, classification, containment and readiness decisions in application
   services with persistence ports. Workers and internal callers use the same
   guards as API callers. Endpoint-only checks are insufficient.
3. Persist readiness reviews, findings, accepted risks, playbooks, contacts,
   tabletops and review dates. Store append-only decision history. An empty or
   expired review cannot establish readiness.
4. Link security and incident evidence to both checklist approval and subsequent
   mode changes. Revalidate when approval is consumed; a previously approved
   checklist is insufficient if its supporting evidence is stale or a blocking
   finding was reopened.
5. Commit successful business changes and audit events through the existing
   `IApplicationTransaction`. Record rejection events outside a rolled-back
   business transaction, without committing partial mutations.
6. Use a transactional outbox for notifications, reminders and storage cleanup.
   Retrying external delivery must not repeat the business transition or create
   duplicate audit events. Database atomicity alone does not prove delivery.

## Compatibility changes requiring approval

1. Existing legacy No-CUI acknowledgements do not prove acceptance of a particular
   published Phase 1A notice for a tenant, mode and workflow. Keep historical
   acknowledgements, but require renewed acknowledgement before affected writes.
   Return a structured 428 response identifying the current notice and workflow.
   Do not manufacture acknowledgement records during migration.
2. Affected content-creation requests must explicitly supply classification.
   Missing or undefined enum values return validation errors rather than silently
   becoming Unclassified. Update the web client and documented API examples in
   the same release; older clients must handle this response and resubmit.
3. Open escalations begin enforcing containment. Operations currently accepted
   against affected content can fail after cutover. Scope containment to the
   exact tenant-owned item and its relevant dependencies; preserve access needed
   by authorized incident responders through an explicit, audited workflow.
4. Existing checklist approvals remain historical records. They cannot establish
   future readiness without current structured security and incident evidence.
   Preserve the No-CUI product boundary; this work does not authorize real CUI.

Recommended decision: approve these requirements as one coordinated API/UI
cutover. Do not silently grandfather acknowledgements or readiness approvals.

## Ordered implementation slices

1. Classification: parse multipart classification; validate enum values; derive
   reviewer identity/time on the server; add atomic classification history; finish
   document review and downstream checks; add explicit classification UI states.
2. Notices: resolve effective published notices by current mode/workflow; make
   relevant notice retrieval/acknowledgement available to workflow users; enforce
   current acknowledgements in upload, classified-note, report and extraction
   services. Add acknowledgement/re-prompt UI and validate package governance
   before changing published notice content.
3. Escalations: validate affected references; complete categories and transitions;
   require actor/time/note and preserve resolution history; prevent assignment
   from bypassing resolution; enforce containment across download, export,
   extraction, report use and evidence approval; add UI entry points and outbox
   notification delivery.
4. Audit: standardize event type/result/classification/mode/entity metadata at
   producers; cover blocked paths; filter and paginate in persistence with stable
   ordering and bounded export processing; audit exports themselves.
5. Readiness: add durable security/incident records and migration; persist actual
   review evidence and accepted-risk expiry; add API/UI lifecycle workflows and
   server-derived reviewers; connect blocking findings, playbooks and tabletop
   freshness to checklist approval and mode changes; schedule review reminders
   through the durable job infrastructure.
6. Verify the whole affected boundary and record implementation status against
   the story acceptance criteria. Do not mark a story complete based on tests of
   its static validators alone.

## Migration, rollout and rollback

1. Inventory existing acknowledgements, classification values, unresolved
   escalation references and checklist approvals in the target environment.
   Report aggregate counts without exporting customer content. Do not rewrite
   ambiguous classifications or reassociate evidence automatically.
2. Add schema and indexes without deleting history. New readiness records start
   unreviewed; reviewer identities, approval dates and tabletop results cannot be
   seeded as customer facts.
3. Verify migration against a representative PostgreSQL restore. Introduce any
   new constraints only after checking existing values and reporting exceptions.
4. Ship notice-aware UI and API enforcement together. Document 400/404/409/428
   responses for old clients and the user steps for renewed acknowledgement.
5. Roll back application changes only to a version compatible with the additive
   schema. If enforcement cannot remain intact during rollback, temporarily
   disable affected mutations rather than reopening contained content. Preserve
   readiness and audit history; do not drop populated tables as a rollback step.

## Verification requirements

- Enumerate all affected endpoints, services and workers before editing. Include
  direct service calls and already queued jobs, not just representative routes.
- Test allowed/denied roles, missing permissions, same-tenant and cross-tenant
  references, missing records, malformed inputs and stale acknowledgements.
- Verify classification and containment on bytes, metadata, report snapshots,
  export artifacts, extraction inputs and evidence approval independently.
- Use PostgreSQL tests for audit failure, rollback, concurrent state changes,
  expired/reopened findings, uniqueness constraints and migration behavior.
- Fault-inject storage and notification failures; prove durable retries and no
  duplicate business transitions. Test cancellation and ambiguous responses.
- Exercise browser flows for notice renewal, rejection/escalation, authorized
  review, empty/loading/error states, readiness updates and permission denial.
- Build touched projects; execute relevant Phase 1A smoke, automated and adjacent
  regression suites. Record executed coverage and unavailable environments.

## External evidence and claims

Planned: software can record qualified notice review, incident contacts,
tabletops, backup/restore results and security approval. Their actual completion
requires authorized people and operational evidence. Do not fabricate those
records or mark them passed because the forms and validators exist.

Do not claim: certification, government approval, production CUI authorization,
or completion of a real incident exercise from this integration work alone.

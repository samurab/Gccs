# Procurement Integrity Risk Review Module Design

Status: future design only. Do not implement this module until it is explicitly approved.

## Purpose

The Procurement Integrity Risk Review module should help government contractors document procurement-integrity workflows around opportunities, bids, proposals, teaming, subcontractors, pricing communications, red flags, escalations, and approvals.

The module must be a workflow and evidence-management aid only. It must not provide legal advice, make final compliance conclusions automatically, certify compliance, or replace review by qualified legal/compliance personnel.

Initial use cases:

- Bid/no-bid compliance checklist.
- Teaming partner conflict review.
- Subcontractor and consultant communication log.
- Pricing communication attestation.
- Procurement red-flag checklist.
- Internal escalation workflow.
- Exportable procurement-integrity audit report.

## Fit In Existing Architecture

Use the existing Clean Architecture layout:

- Domain: procurement integrity entities, enums, and invariant rules.
- Application: DTOs, validation, workflow services, repository interfaces, audit orchestration, report/export use cases.
- Infrastructure: EF Core entities, mappings, migrations, tenant-scoped repositories, report export queries.
- API: thin minimal API endpoints with tenant context, RBAC, validation response shaping, and standard error contract.
- Web: future React pages/components only after backend workflows are approved.
- Compliance content: source-backed template/checklist metadata, not tenant-specific review data.

The reusable checklist engine can support checklist templates, but procurement integrity review records need first-class entities because they combine opportunity scope, communication records, attestations, conflicts, escalations, approvals, and report exports.

## Non-Goals

- No legal determinations.
- No automatic pass/fail procurement-integrity conclusion.
- No production CUI, classified, ITAR, export-controlled, or sensitive government-furnished information handling.
- No full module implementation in the MVP.
- No new AI-generated advice unless a future source-cited, draft-only, human-reviewed workflow is separately approved.

## Proposed Entities

### ProcurementIntegrityReview

Tenant-scoped aggregate root for a review tied to an opportunity, contract, solicitation, subcontract, or internal bid/no-bid decision.

Fields:

- `Id`
- `TenantId`
- `ReviewNumber`
- `Title`
- `RelatedContractId`
- `RelatedSolicitationId`
- `OpportunityName`
- `AgencyOrPrimeName`
- `ReviewType`: `BidNoBid`, `ProposalSubmission`, `Teaming`, `SubcontractorReview`, `PricingReview`, `PostAwardConcern`
- `Status`: `Draft`, `InReview`, `Escalated`, `Approved`, `Rejected`, `Closed`
- `RiskLevel`: `Low`, `Medium`, `High`, `Critical`
- `OwnerUserId`
- `ReviewerUserId`
- `LegalReviewerUserId`
- `DecisionSummary`
- `DecisionDisclaimer`
- `CreatedBy`
- `CreatedUtc`
- `UpdatedBy`
- `UpdatedUtc`
- `ReviewedBy`
- `ReviewedUtc`
- `ClosedBy`
- `ClosedUtc`

Rules:

- Must be scoped by `TenantId`.
- `Approved`, `Rejected`, and `Closed` require reviewer metadata and decision notes.
- `Critical` risk or unresolved red flags require escalation before approval.
- Decision wording must remain descriptive, not a legal conclusion.

### ProcurementIntegrityChecklistItem

Tenant-scoped item linked to a review.

Fields:

- `Id`
- `TenantId`
- `ReviewId`
- `TemplateItemKey`
- `Title`
- `QuestionText`
- `Response`: `NotAnswered`, `Yes`, `No`, `NotApplicable`, `Unknown`
- `RiskFlag`: `None`, `Low`, `Medium`, `High`, `Critical`
- `Notes`
- `EvidenceItemId`
- `OwnerUserId`
- `ReviewedBy`
- `ReviewedUtc`
- `CreatedUtc`
- `UpdatedUtc`

Rules:

- Evidence links must be tenant-validated.
- `Unknown`, high-risk, or critical answers should trigger review status changes or escalation prompts.

### ProcurementCommunicationLog

Tenant-scoped communication record for competitor, teaming, subcontractor, consultant, prime, agency, or internal pricing-related communications.

Fields:

- `Id`
- `TenantId`
- `ReviewId`
- `CommunicationType`: `Competitor`, `TeamingPartner`, `Subcontractor`, `Consultant`, `Prime`, `Agency`, `Internal`
- `CommunicationDate`
- `Participants`
- `OrganizationNames`
- `Topic`
- `Summary`
- `PricingDiscussed`
- `SourceSelectionInfoDiscussed`
- `NonPublicBidProposalInfoDiscussed`
- `FollowUpRequired`
- `EscalationId`
- `CreatedBy`
- `CreatedUtc`
- `UpdatedBy`
- `UpdatedUtc`

Rules:

- Do not store raw privileged legal advice unless a future legal hold/privacy design is approved.
- Do not log secrets or sensitive file contents in audit metadata.

### ProcurementPricingAttestation

Tenant-scoped attestation for independent price-determination and pricing communication review.

Fields:

- `Id`
- `TenantId`
- `ReviewId`
- `AttestationText`
- `AttestedByUserId`
- `AttestedUtc`
- `AttestationVersion`
- `ExceptionsNoted`
- `ExceptionSummary`
- `ReviewerUserId`
- `ReviewedUtc`
- `ReviewStatus`: `PendingReview`, `Accepted`, `Rejected`

Rules:

- Attestation text must be versioned.
- Exceptions require reviewer action before review approval.

### ProcurementConflictCheck

Tenant-scoped check for teaming partner, subcontractor, consultant, or employee conflicts.

Fields:

- `Id`
- `TenantId`
- `ReviewId`
- `SubjectType`: `TeamingPartner`, `Subcontractor`, `Consultant`, `Employee`, `Advisor`, `Other`
- `SubjectName`
- `RelatedEntityId`
- `ConflictType`: `OrganizationalConflict`, `CompetitorContact`, `FormerGovernmentEmployee`, `SourceSelectionAccess`, `PricingCommunication`, `Other`
- `Description`
- `RiskLevel`
- `ResolutionStatus`: `Open`, `Mitigated`, `Escalated`, `AcceptedByReviewer`, `Rejected`
- `MitigationPlan`
- `ReviewedBy`
- `ReviewedUtc`
- `CreatedBy`
- `CreatedUtc`
- `UpdatedBy`
- `UpdatedUtc`

### ProcurementEscalation

Tenant-scoped workflow for internal compliance/legal escalation.

Fields:

- `Id`
- `TenantId`
- `ReviewId`
- `EscalationReason`
- `Severity`
- `AssignedToUserId`
- `Status`: `Open`, `InReview`, `Resolved`, `Closed`
- `ResolutionSummary`
- `ResolvedBy`
- `ResolvedUtc`
- `CreatedBy`
- `CreatedUtc`
- `UpdatedBy`
- `UpdatedUtc`

### ProcurementReviewApproval

Append-style approval record for review decisions.

Fields:

- `Id`
- `TenantId`
- `ReviewId`
- `Decision`: `Approved`, `Rejected`, `NeedsMoreInformation`, `Escalated`
- `DecisionByUserId`
- `DecisionUtc`
- `DecisionNotes`
- `RequiredFollowUp`
- `FollowUpDueDate`

Rules:

- Approval records should not be updated or deleted through normal application flows.
- Corrections should be additive with a superseding approval or correction note.

## Proposed DTOs

Request DTOs:

- `CreateProcurementIntegrityReviewRequest`
- `UpdateProcurementIntegrityReviewRequest`
- `UpdateProcurementChecklistItemRequest`
- `CreateProcurementCommunicationLogRequest`
- `CreateProcurementPricingAttestationRequest`
- `CreateProcurementConflictCheckRequest`
- `UpdateProcurementConflictCheckRequest`
- `CreateProcurementEscalationRequest`
- `ResolveProcurementEscalationRequest`
- `SubmitProcurementReviewDecisionRequest`
- `ProcurementIntegrityReportExportRequest`

Response DTOs:

- `ProcurementIntegrityReviewSummaryDto`
- `ProcurementIntegrityReviewDetailDto`
- `ProcurementIntegrityChecklistItemDto`
- `ProcurementCommunicationLogDto`
- `ProcurementPricingAttestationDto`
- `ProcurementConflictCheckDto`
- `ProcurementEscalationDto`
- `ProcurementReviewApprovalDto`
- `ProcurementIntegrityReportExportDto`

Validation DTO rules:

- Reject invalid enum values.
- Reject missing review title, owner, or related opportunity/contract context.
- Reject approval without reviewer metadata and decision notes.
- Reject linked evidence, contract, subcontractor, or user references that are not in the current tenant.
- Reject final states when high-risk checklist items or escalations are unresolved.

## Proposed API Endpoints

Future route group: `/api/procurement-integrity`

- `GET /reviews`
- `POST /reviews`
- `GET /reviews/{reviewId}`
- `PUT /reviews/{reviewId}`
- `POST /reviews/{reviewId}/submit-for-review`
- `POST /reviews/{reviewId}/decisions`
- `GET /reviews/{reviewId}/checklist`
- `PUT /reviews/{reviewId}/checklist/{itemId}`
- `GET /reviews/{reviewId}/communications`
- `POST /reviews/{reviewId}/communications`
- `GET /reviews/{reviewId}/conflict-checks`
- `POST /reviews/{reviewId}/conflict-checks`
- `PUT /reviews/{reviewId}/conflict-checks/{conflictCheckId}`
- `POST /reviews/{reviewId}/pricing-attestations`
- `GET /reviews/{reviewId}/escalations`
- `POST /reviews/{reviewId}/escalations`
- `POST /reviews/{reviewId}/escalations/{escalationId}/resolve`
- `GET /reports/exports/procurement-integrity`

Endpoint rules:

- All endpoints must resolve `TenantId` from server-side tenant context.
- All read endpoints must filter by current tenant.
- All mutation endpoints must enforce RBAC and audit logging.
- Cross-tenant review ids must return the project-standard `404` or the existing feature-standard `403`.
- Exports must be tenant-scoped and audit-logged.

## RBAC Rules

Recommended future permissions:

- `ViewProcurementIntegrity`
- `ManageProcurementIntegrity`
- `ReviewProcurementIntegrity`
- `ExportProcurementIntegrity`

Initial role mapping:

- Owner/Admin: all procurement integrity permissions.
- Compliance Manager: view, manage, review, export.
- Contributor: view and manage draft records assigned to them; no approval/export by default.
- Auditor: view and export read-only reports.
- Advisor: view, manage, review, export only if tenant explicitly grants advisor access.

If new permissions are deferred, temporary MVP mapping could use existing permissions:

- View: `ViewContracts` plus `ViewObligations`.
- Manage: `ManageContracts` or `ManageObligations`.
- Review: `ReviewClauses` or `ManageObligations`.
- Export: `ViewReports`.

Do not rely on temporary mapping for production. Procurement integrity is sensitive enough to warrant explicit permissions before launch.

## Audit Log Events

Audit-log these actions:

- Review created.
- Review updated.
- Review submitted for review.
- Checklist item answered or changed.
- Communication log created or updated.
- Conflict check created or updated.
- Pricing attestation submitted.
- Pricing attestation reviewed.
- Escalation created.
- Escalation resolved.
- Review decision submitted.
- Review closed.
- Procurement integrity report exported.
- Failed authorization attempt, if the authorization pipeline supports event capture for this route group.

Recommended audit metadata:

- `reviewId`
- `reviewNumber`
- `reviewType`
- `status`
- `riskLevel`
- `relatedContractId`
- `entityType`
- `entityId`
- `correlationId`

Never include raw privileged legal advice, secrets, tokens, passwords, or file contents in audit metadata.

## Checklist Templates

The reusable checklist engine should eventually support these templates, but the module should also persist answers against `ProcurementIntegrityReview`.

### Bid/No-Bid Compliance Checklist

Items:

- Opportunity source and acquisition context recorded.
- Solicitation clauses and procurement-integrity triggers reviewed.
- Competitor contact reviewed.
- Independent price determination attestation required.
- Red flags reviewed before bid/no-bid decision.
- Required escalation completed before submission.

### Teaming Partner Conflict Review

Items:

- Teaming partner identity and role documented.
- Organizational conflict indicators reviewed.
- Nonpublic information access reviewed.
- Exclusivity or competitor restrictions reviewed.
- Flow-down communication expectations documented.
- Reviewer approval recorded.

### Subcontractor Communication Log Checklist

Items:

- Subcontractor communication participants recorded.
- Pricing discussion status recorded.
- Source-selection or nonpublic information discussion status recorded.
- Follow-up actions assigned.
- Escalation created for unclear or high-risk communication.

### Pricing Communication Attestation

Items:

- Independent price determination attested.
- Exceptions disclosed.
- Pricing communication restrictions acknowledged.
- Reviewer accepted or rejected attestation.
- Final submission approval recorded.

### Procurement Red-Flag Checklist

Items:

- Competitor communication red flags reviewed.
- Former government employee or advisor involvement reviewed.
- Source-selection information exposure reviewed.
- Unusual pricing coordination indicators reviewed.
- Consultant/subcontractor channel risks reviewed.
- Legal/compliance escalation completed where required.

## Report and Export Requirements

Add a future CSV export first unless a PDF reporting framework already exists.

Export types:

- Procurement integrity review summary.
- Review detail package.
- Communication log.
- Conflict check register.
- Pricing attestation register.
- Escalation register.
- Full procurement-integrity audit report.

Every export must include:

- Tenant name or tenant id.
- Generated date.
- Generated by.
- Report type.
- Applied filters.
- Review id or date range.
- Source obligation references where applicable.
- Review status and reviewer metadata.
- Audit log references or event count summary.

Export restrictions:

- Tenant-scoped only.
- RBAC-protected.
- Audit-logged.
- No file contents.
- No stack traces or internal exception details.
- No cross-tenant entity ids.

## Tenant Isolation Risks

Primary risks:

- Linking a review to another tenant's contract, solicitation, subcontractor, evidence, user, or audit event.
- Exporting communication logs or conflict checks across tenants.
- Reusing checklist instances without tenant-scoped review linkage.
- Advisor or consultant access leaking one client tenant to another.
- Search/indexing returning procurement records across tenant boundaries.
- Audit logs containing cross-tenant ids in metadata.

Required mitigations:

- Every table must include `TenantId` except global source-backed templates.
- Repositories must begin queries from current tenant scope.
- Link validation must check tenant ownership for every related entity.
- Cross-tenant ids must return `404` or feature-standard `403`.
- Tests must cover cross-tenant list, get, update, link, approval, and export scenarios.
- Export and report queries must filter by current tenant before applying user filters.

## Human Review Model

The module should support documented human review instead of automated conclusions.

Recommended states:

- `Draft`: user is collecting information.
- `InReview`: assigned reviewer is evaluating.
- `Escalated`: compliance/legal review is required.
- `Approved`: reviewer approved workflow documentation.
- `Rejected`: reviewer rejected or blocked the review.
- `Closed`: no further action in the system.

Disclaimers:

- Approval means the internal workflow documentation was reviewed in Gccs.
- Approval does not mean legal clearance, government approval, CMMC certification, procurement-integrity compliance certification, or legal advice.

## Implementation Phases

### Phase 0: Design and Content Governance

- Review this design with product, engineering, compliance SME, and legal advisor.
- Confirm source-backed content references and disclaimers.
- Decide whether explicit procurement-integrity permissions are required before any implementation.
- Add decision-log entry approving or rejecting module scope.

### Phase 1: Backend Foundation

- Add domain/application DTOs and validation.
- Add EF Core entities and migration.
- Add tenant-scoped repository.
- Add API endpoints.
- Add audit logging.
- Add focused API tests for tenant isolation, RBAC, audit logging, validation, empty states, and cross-tenant link rejection.

### Phase 2: Checklist and Review Workflow

- Add procurement checklist templates.
- Add review state transitions.
- Add escalation workflow.
- Add pricing attestation workflow.
- Add approval records as append-style history.

### Phase 3: Reporting and Export

- Add tenant-scoped CSV export.
- Add report filters by review type, status, risk level, date range, owner, reviewer, contract, and opportunity.
- Add export audit logging.
- Add tests for empty export, tenant scoping, RBAC, and metadata.

### Phase 4: Frontend

- Add list/detail pages.
- Add review wizard or stepper.
- Add communication log and conflict check forms.
- Add escalation and approval screens.
- Add loading, empty, success, error, and authorization-denied states.

### Phase 5: Advanced Controls

- Add search after tenant-safe indexing exists.
- Add optional reminders for unresolved escalations.
- Add dashboard alerts for high-risk unresolved procurement-integrity reviews.
- Add source-cited draft assistant only after AI governance approval.

## Testing Strategy

Required future tests:

- Tenant can create and list own procurement integrity reviews.
- Tenant cannot access another tenant's reviews.
- Tenant cannot link another tenant's contract, subcontractor, evidence, or user.
- Unauthorized user cannot create, update, approve, or export.
- Reviewer-only decision endpoint blocks contributors.
- Critical red flag blocks approval until escalation is resolved.
- Pricing attestation exception requires review.
- Audit log is created for create/update/decision/export.
- Empty review list returns empty array.
- Export succeeds for empty tenant with metadata and headers.
- Export is tenant-scoped and does not include other tenant rows.
- API errors use standard error response shape.

## Open Decisions

- Whether to create dedicated procurement-integrity permissions before implementation.
- Whether approval records should be strictly append-only at the database level or through service/API rules initially.
- Whether communication logs require legal privilege markings.
- Whether procurement-integrity records should support retention/legal hold rules in the first implementation.
- Whether templates should live in static application code, database seed data, or `packages/compliance-content`.
- Whether reports should include audit event references directly or only an audit event count and export correlation id.

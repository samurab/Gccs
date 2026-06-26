# Security Control Implications

This document explains what tenant isolation, RBAC, audit logging, and No-CUI and synthetic demo data handling mean for GCCS implementation, testing, release readiness, and support. These controls are product requirements, not optional engineering preferences.

## Control Summary

| Control | Practical implication | Non-negotiable rule |
| --- | --- | --- |
| Tenant isolation | Every customer workspace is a hard data boundary. Users, advisors, reports, evidence, contracts, tasks, and audit events must resolve through the active tenant context. | A user from tenant A must never read, update, export, report on, or infer tenant B data through API calls, UI fallback state, background jobs, imports, exports, or search. |
| RBAC | Roles decide what an authenticated tenant member can view, create, update, approve, export, or administer. | UI hiding is not security. Every protected action must be enforced server-side by permission policy. |
| Audit logging | Compliance-relevant and security-relevant actions must leave a durable trail. | Create, update, delete, approval, rejection, invitation, membership, upload, export, report generation, ownership changes, and policy acknowledgement events must be audit logged. |
| No-CUI production posture with synthetic CUI-ready demonstration workflows | The MVP supports CUI-aware workflows only through synthetic sandbox demonstrations; real CUI acceptance is disabled unless the tenant or deployment is explicitly approved for future `CuiReady` operation. | Upload, import, paste, extraction, AI, evidence, and report workflows must classify data, enforce tenant data handling mode, block prohibited content, and avoid claims of certification, authorization, or assessment success. |

## Tenant Isolation Implications

- All tenant-owned tables and DTOs must include tenant scope or be reachable only through a tenant-owned parent.
- Repository queries must filter by current tenant, not by caller-supplied tenant IDs alone.
- API routes must resolve tenant context before executing tenant-scoped use cases.
- Background jobs, report generation, notification delivery, imports, exports, search indexing, and future AI retrieval must carry tenant ID as part of their work item scope.
- Advisor and consultant access must be explicit tenant membership; cross-client dashboards may summarize clients but must not mix records across tenants.
- Error responses must not reveal whether another tenant's record exists.
- Tests must include cross-tenant read, update, delete, export, report, and direct API bypass attempts for sensitive workflows.

## RBAC Implications

- Roles are business-facing access profiles: Owner, Admin, Compliance Manager, Contributor, Auditor, and Advisor.
- Permission checks belong in the API/application boundary; frontend route and button visibility is only a usability layer.
- Role decisions must be least-privilege by default, especially for advisor, auditor, evidence approval, user management, audit log access, export, and report snapshot access.
- Auditor access is read-only unless a future story explicitly introduces a reviewed exception.
- Evidence approval, user invitations, tenant settings, audit log viewing, report exports, and obligation ownership assignment require explicit permissions.
- Tests must verify both allowed and denied behavior by role, including direct API calls that bypass hidden UI controls.

## Audit Logging Implications

Audit events must identify:

- Tenant ID.
- Actor user ID or system actor.
- Action.
- Entity type and entity ID.
- Timestamp.
- Summary.
- Request metadata when available, such as correlation ID, IP address, and user agent.
- Structured metadata for important changed fields, excluding prohibited sensitive content.

Audit logging is required for:

- Tenant creation, status changes, membership changes, and invitations.
- Data handling acknowledgement, upload allowed, upload blocked, upload delete, CUI classification, tenant mode changes, and future export/download events.
- Company profile, contract, clause attachment/removal, obligation status/owner, task, evidence, CMMC, subcontractor, notification preference, and report generation changes.
- Content import, review, approval, publication, retirement, and disputed content workflows.
- Security-sensitive failures where logging does not leak another tenant's data.

Audit logs must be append-only through normal application APIs. Corrections should be represented by new events, not mutation of historical events.

## CUI-Ready Gated Data Handling Implications

- The product may track CUI posture, CUI access flags, CMMC readiness metadata, CUI categories, and CUI marking guide metadata.
- Tenant data handling mode must be explicit: `DemoSandbox`, `NoCui`, or `CuiReady`.
- Upload workflows must require current data handling acknowledgement before accepting metadata or files.
- Users must see prohibited-content warnings and tenant data handling status near upload/import entry points.
- If a user marks content as possible or confirmed CUI in a tenant that is not approved as `CuiReady`, the workflow must block the upload.
- Demo tenants may use synthetic or redacted CUI examples to show full workflow behavior without real CUI.
- Contract packages, CUI marking guides, DD Form 254 references, wage determinations, evidence, screenshots, logs, and notes must be treated as high-risk intake surfaces.
- Future OCR, document extraction, AI/RAG, search indexing, and report generation must enforce tenant data handling mode before processing real CUI.
- Customer-facing copy must not imply FedRAMP, GovCloud, CMMC certification, assessment success, legal approval, government endorsement, or authorization to store real CUI unless formally approved.
- Support must have an escalation path for accidental prohibited uploads, suspected CUI, tenant exposure concerns, and evidence/report export issues.

### PR-0.3 Tenant Mode Boundary Review

Tenant mode boundaries are release controls and must be enforced by trusted server-side checks. UI notices, hidden buttons, onboarding copy, or customer acknowledgements are not sufficient enforcement.

| Tenant mode | Allowed behavior | Prohibited behavior | Server-side enforcement requirement |
| --- | --- | --- | --- |
| `DemoSandbox` | Synthetic or redacted demonstration records approved for demo import; seeded CUI-aware workflow examples that cannot be mistaken for customer data. | Real customer CUI, unapproved synthetic CUI, production customer uploads, and customer-facing claims that demo workflows authorize real CUI handling. | Demo seeding and workflow processing must verify tenant mode and approved demo metadata before storage, extraction, reporting, or export. |
| `NoCui` | Non-CUI customer metadata, non-sensitive files, FCI workflow tracking, source-backed obligation metadata, and compliance evidence that users classify as unclassified or non-CUI. | Real customer CUI, synthetic CUI demo records, prohibited sensitive content, CUI-marked evidence, CUI-bearing imports, CUI-bearing extraction, and CUI-bearing reports or exports. | API, repository, background job, import, extraction, evidence, report, and export paths must reject CUI-classified or synthetic-CUI-classified records even when called directly. |
| Future `CuiReady` | Real CUI only after separate approval, confirmed classification, customer terms, shared responsibility matrix, support model, evidence controls, and required signoff. | Treating future `CuiReady` as available by default, accepting unconfirmed classification, bypassing approval checks, or mixing demo synthetic CUI with production CUI workflows. | CUI workflows must require explicit tenant mode, confirmed classification, approval checks, audit logging, and separate launch approval before processing real CUI. |

Failure modes that block launch unless mitigated:

- Direct API bypass: a caller posts CUI-classified contract, evidence, import, extraction, report, or export requests without using the UI.
- Background processing bypass: a queued extraction, import, search indexing, report, export, or AI job processes a CUI-classified record after the initial upload guard was bypassed or seeded.
- Future posture leakage: `CuiReady` code paths, demo fixtures, customer copy, or tenant defaults make real CUI appear available before the separate approval gate.
- Legacy or direct database data: old rows, seed data, import scripts, or administrator tools introduce CUI-classified records that later report/export paths process without rechecking tenant mode.

## Implementation Checklist

Before a feature is complete, answer yes to each applicable question:

- Does every read/write path resolve the active tenant and filter data by tenant?
- Are all protected actions covered by server-side permission checks?
- Does the UI avoid showing actions the user cannot perform while still relying on the API for enforcement?
- Are compliance-relevant or security-relevant mutations audit logged?
- Does the audit event include useful metadata without storing prohibited sensitive content?
- Does the workflow preserve data classification warnings, CUI gating, and blocking controls for upload, import, paste, extraction, or evidence handling?
- Are reports and exports scoped to the tenant and filtered by the user's role?
- Are background jobs, notifications, searches, and generated artifacts tenant-scoped?
- Are direct API bypass, cross-tenant, denied-role, and CUI/data-handling guardrail tests present where risk warrants?

## Launch And Support Implications

- Launch is blocked if tenant isolation, server-side RBAC, audit logging, or CUI gating controls are known to be ineffective for MVP workflows.
- Production support must treat tenant isolation defects, unauthorized access, accidental prohibited uploads, accidental CUI uploads in unapproved tenants, and report/evidence exposure as security events.
- Release notes must call out the tenant data handling posture and known storage/scanning limitations.
- Any feature that changes what customer data can be stored, processed, searched, exported, or used by AI requires security and product review before release.

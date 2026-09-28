# FeDril SOC 2 System Boundary Index

Status: **Draft / Ready for review; not approved**
Scope version: `SOC2-SCOPE-DRAFT-0.2`
Record updated: September 28, 2026 (America/New_York)
Repository baseline reviewed: `9dbd825b26f12c7b3dde635734bca23e4da15593`
Authoritative Story 39 source SHA-256: `72129c1acd3825ae71d85c6879cf2b57761bcb9802bc8a140980cb9926559248`

This is a sanitized, point-in-time repository assessment, not a statement of current production configuration, operating effectiveness, SOC 2 readiness, or report issuance. Current release identity is governed by [`docs/release/approved-release.json`](../release/approved-release.json).

Available sources were the current repository, test sources, release manifest, workflow definitions, and read-only GitHub/Azure capability metadata. Live resource configuration, personnel records, contracts, provider assurance reports, customer demand records, protected evidence, authorized AICPA criteria, and qualified reviewer conclusions were unavailable or intentionally not copied into Git.

## Interpretation

- **Implemented** means supporting repository code or configuration was identified.
- **Partially implemented** means supporting code exists but deployment, configuration, coverage, or operation still requires verification.
- **Planned** means the capability is not established by the reviewed repository state.
- **Do not claim** marks statements that are unsupported or outside the product posture.

These labels do not prove that a feature is deployed, enabled, correctly configured, or operating over a SOC 2 examination period.

## Candidate Public Boundary

| Boundary | Repository assessment | Required verification outside this public index |
| --- | --- | --- |
| Browser and API | **Implemented:** React workspace and ASP.NET API with tenant-scoped authentication and authorization paths | Deployed route inventory, identity-provider configuration, complete allowed/denied role coverage, and current release evidence |
| Relational persistence | **Implemented:** PostgreSQL repositories and tenant-qualified persistence models | Deployed schema, privileged-access governance, backup/recovery operation, and complete tenant-path verification |
| File upload and storage | **Partially implemented:** guarded upload, scanning adapter, version metadata, and object-storage adapter | Current scanner/storage operation, recovery, retention, access review, and No-CUI enforcement evidence |
| Background processing | **Implemented:** database-backed jobs and API-hosted workers, conditional on configuration | Enabled worker inventory, failure handling, retries, and operating evidence |
| Governed retrieval | **Implemented:** PostgreSQL full-text retrieval over governed sources | Deployed enablement, source-review population, and human-review operation; external generated-answer service remains **Planned** |
| Email and external integrations | **Partially implemented:** adapters exist for selected workflows | Approved configuration, provider governance, delivery/use evidence, retention, and failure handling |
| Release operations | **Implemented:** CI, release manifests, immutable artifact promotion, and deployment workflows | Current approved manifest, workflow results, access review, and independent change evidence |
| Telemetry and recovery | **Partially implemented:** application telemetry and operational procedures exist | Alert receipt/review, retention, recovery objectives, restore results, and continuity evidence |

Redis is a configured dependency, but this index does not establish a production workload for it. Repository evidence establishes PostgreSQL-backed jobs and retrieval; it does not establish a separate queue or search service.

## Product And Data Posture

The current product posture is No-CUI compliance management. FeDril supports readiness workflows, evidence and obligation tracking, reporting, and auditability. It does not authorize storage or processing of real CUI, classified information, export-controlled technical data, or other prohibited sensitive content.

**Do not claim:** SOC 2 certification or report issuance, government approval, legal/accounting/labor advice, FedRAMP authorization, CMMC certification, guaranteed compliance outcomes, secure CUI storage, or autonomous compliance decisions.

Repository implementation does not establish contractual service levels, production residency, operating effectiveness, or qualified auditor conclusions. Customer-facing statements require current UI, API, authorization, test, deployment, and release evidence.

## Public Information Boundary

This public index may contain only high-level architecture, repository implementation status, limitations, and opaque evidence references. It must not contain:

- credentials, tokens, keys, connection strings, tenant or subscription identifiers;
- customer data, database contents, raw evidence, personnel records, or private contracts;
- detailed incident coordinates, retrievable artifact locations, exploit instructions, or sensitive configuration exports;
- protected assurance reports, signatures, access populations, or recovery information.

Detailed personnel, ownership, budget, infrastructure, incident, and evidence-custody records belong in an approved restricted workspace. Their absence from public Git is intentional and does not mean the underlying review is complete.

## Review Gates

Before this boundary is approved or used in customer-facing material:

1. Reconcile it to the current approved release manifest and deployed route/component inventory.
2. Verify server-side tenant isolation and RBAC for the complete affected endpoint inventory.
3. Verify No-CUI upload and data-handling controls in the deployed environment.
4. Review infrastructure, provider, retention, recovery, monitoring, and privileged-access evidence in a restricted workspace.
5. Obtain qualified security, legal/contract, management, and independent CPA review as applicable.
6. Record exact scope, period, criteria, exceptions, residual risk, approvers, and evidence references without publishing protected evidence.

Until those gates are completed, this document remains a draft planning index and must not be presented as assurance, certification, compliance, or audit-readiness evidence.

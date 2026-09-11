# Development Database Models

These artifacts turn the MVP domain model into a migration-ready PostgreSQL schema for the development phase. The schema supports CuiReady workflows while keeping real CUI storage tenant-gated until production authorization is approved.

## Artifact Set

- EF Core DbContext: `src/Gccs.Infrastructure/Persistence/GccsDbContext.cs`
- Persistence entities: `src/Gccs.Infrastructure/Persistence/Models/`
- Initial migration: `src/Gccs.Infrastructure/Persistence/Migrations/20260610031239_InitialDevelopmentSchema.cs`
- Clause review/versioning migration: `src/Gccs.Infrastructure/Persistence/Migrations/20260610051044_AddClauseReviewVersioning.cs`
- Tenant membership migration: `src/Gccs.Infrastructure/Persistence/Migrations/20260613213418_AddTenantMemberships.cs`
- Tenant invitation migration: `src/Gccs.Infrastructure/Persistence/Migrations/20260613221118_AddTenantInvitations.cs`
- No-CUI acknowledgement migration: `src/Gccs.Infrastructure/Persistence/Migrations/20260615003848_AddNoCuiAcknowledgements.cs`
- Tenant data handling mode history migration: `src/Gccs.Infrastructure/Persistence/Migrations/20260618205911_AddTenantDataHandlingModeHistory.cs`
- Evidence upload guardrails migration: `src/Gccs.Infrastructure/Persistence/Migrations/20260615005659_AddEvidenceUploadGuardrails.cs`
- Audit request metadata migration: `src/Gccs.Infrastructure/Persistence/Migrations/20260615010139_AddAuditRequestMetadata.cs`
- Obligation publication metadata migration: `src/Gccs.Infrastructure/Persistence/Migrations/20260615011257_AddObligationPublicationMetadata.cs`
- Clause tenant scope migration: `src/Gccs.Infrastructure/Persistence/Migrations/20260615040552_AddClauseTenantScope.cs`
- Contract clause attachment workflow migration: `src/Gccs.Infrastructure/Persistence/Migrations/20260615041300_AddContractClauseAttachmentWorkflow.cs`
- Content classification metadata migration: `src/Gccs.Infrastructure/Persistence/Migrations/20260618212037_AddContentClassificationMetadata.cs`
- Versioned CUI-readiness evidence migration: `src/Gccs.Infrastructure/Persistence/Migrations/20260908181858_AddVersionedCuiReadinessEvidence.cs`
- Durable SSP section migration: `src/Gccs.Infrastructure/Persistence/Migrations/20260909165957_AddDurableSspSections.cs`
- Durable SSP narrative migration: `src/Gccs.Infrastructure/Persistence/Migrations/20260909213508_AddDurableSspNarratives.cs`
- Durable SSP export package migration: `src/Gccs.Infrastructure/Persistence/Migrations/20260910150656_AddDurableSspExportPackages.cs`
- SPRS readiness report idempotency migration: `src/Gccs.Infrastructure/Persistence/Migrations/20260910215445_AddSprsReadinessReportIdempotency.cs`; nullable keys preserve historical reports while a filtered tenant/type/key unique index coordinates new generation requests.
- Durable subcontracting report data migration: `src/Gccs.Infrastructure/Persistence/Migrations/20260910232537_AddDurableSubcontractingReportData.cs`; tenant-qualified foreign keys bind rows to contracts, subcontractors, and evidence, reviewer metadata references the acting user, and a normalized unique key prevents duplicate report rows under concurrent writes.
- SAM.gov SPR metadata migration: `src/Gccs.Infrastructure/Persistence/Migrations/20260910234532_AddSamGovSprReportMetadata.cs`; nullable fields preserve legacy rows while canonical writes capture reporting role, fiscal year/period, UEI, PIID, conditional subcontract number, and explicit external eligibility evidence. Existing rows remain `NeedsVerification` rather than being silently marked ready.
- Generated-policy classification migration: `src/Gccs.Infrastructure/Persistence/Migrations/20260910011549_AddGeneratedPolicyClassification.cs`; existing generated policies and revisions migrate to `Unknown`/`SystemSuggested` and require review before governed reuse.
- Compliance task search index migration: staging and production first run `infra/database/predeploy-compliance-task-search-index.sql` as standalone concurrent DDL, then apply `src/Gccs.Infrastructure/Persistence/Migrations/20260909192654_AddComplianceTaskSearchIndex.cs`. The EF migration uses `IF NOT EXISTS`, so it is idempotent and does not rebuild the predeployed index; fresh development databases can create it transactionally while the table is empty.
- Generated SQL script: `infra/database/development-schema.sql`
- Local EF tool manifest: `dotnet-tools.json`

## Local Commands

Start local services:

```bash
docker compose -f infra/docker/docker-compose.yml up -d
```

Apply the development migration:

```bash
dotnet tool restore
dotnet tool run dotnet-ef database update \
  --project src/Gccs.Infrastructure/Gccs.Infrastructure.csproj \
  --startup-project apps/api/Gccs.Api.csproj \
  --context GccsDbContext
```

Generate a SQL review script:

```bash
dotnet tool run dotnet-ef migrations script \
  --project src/Gccs.Infrastructure/Gccs.Infrastructure.csproj \
  --startup-project apps/api/Gccs.Api.csproj \
  --context GccsDbContext \
  --output infra/database/development-schema.sql
```

Override the design-time connection string with `GCCS_DATABASE`. The development default matches the Docker Postgres service:

```text
Host=localhost;Port=15432;Database=gccs;Username=gccs;Password=gccs_dev_password
```

## Model Groups

Required fields, source systems, provenance rules, and deferred external integrations are identified in `docs/data-requirements-and-source-systems.md`. Schema changes should preserve that contract by storing source/provenance metadata for imported, extracted, governed, or reportable data.

| Group | Primary tables | Purpose |
| --- | --- | --- |
| Tenancy and RBAC | `tenants`, `tenant_data_handling_mode_history`, `cui_ready_approval_checklists`, `cui_ready_approval_checklist_items`, `cui_readiness_evidence`, `security_review_records`, `security_review_checklist_items`, `security_review_findings`, `accepted_security_risks`, `technical_readiness_records`, `executed_control_evidence`, `incident_readiness_records`, `incident_contacts`, `incident_playbooks`, `incident_tabletops`, `incident_follow_ups`, `readiness_approvals`, `readiness_history`, `users`, `tenant_memberships`, `tenant_invitations`, `no_cui_acknowledgements`, `roles`, `user_roles`, `role_permissions` | Tenant isolation, active data handling mode, mode-change history, fail-closed CUI-readiness approvals linked to versioned authoritative security, technical-control, and incident-readiness records, explicit tenant membership assignments, invitation onboarding workflow, user-scoped data handling acknowledgement records, MFA-ready user profile, and role permissions. |
| Company profile | `company_profiles`, `company_naics_codes`, `company_certifications`, `company_locations` | SAM/SBA profile data, NAICS size support, certifications, locations, IT posture, and data handling posture. |
| Compliance content | `clauses`, `obligations`, `mvp_modules` | Source-backed clause and obligation library with source URL, review metadata, confidence, and expert-review flags. |
| SSP builder | `ssp_sections`, `ssp_section_links`, `ssp_section_source_references`, `ssp_section_status_history`, `ssp_narratives`, `ssp_narrative_sources`, `ssp_export_packages`, `ssp_export_package_history` | Tenant-scoped structured sections and narrative versions; server-resolved source snapshots and fingerprints; immutable human- and machine-readable review packages; package-specific external-share approval; classification and reviewer metadata; optimistic versions; one current approved narrative per section; and append-only audit integration. |
| Contract intake | `contracts`, `solicitations`, `contract_documents`, `contract_clauses`, `contract_clause_obligations`, `contract_deliverables`, `contract_reporting_deadlines`, `esrs_applicabilities` | Contract records, document metadata with classification metadata, extracted/manual clauses, obligations, deliverables, source-backed SAM.gov SPR reporting periods, reporting dates, and flow-down signals. The `esrs_applicabilities` name remains for migration compatibility. |
| Calendar and work | `compliance_tasks` | Due dates, renewals, evidence requests, policy reviews, corrective actions, control assessment tasks, and task-backed SPR reminders. |
| Evidence vault | `evidence_items`, `evidence_file_versions`, `content_classification_history`, `evidence_obligations`, `evidence_contracts`, `evidence_controls`, `evidence_vendors`, `evidence_employees` | Folderless evidence tagging across obligations, contracts, controls, vendors, and employees, with classification metadata and reclassification history. |
| CMMC workspace | `controls`, `assessments`, `control_assessments`, `sprs_score_calculations`, `sprs_score_calculation_notes`, `poam_items`, `poam_evidence`, `assets`, `system_boundaries`, `system_boundary_assets`, `system_boundary_external_service_providers`, `system_boundary_evidence`, `annual_affirmations` | Level 1/2 readiness, immutable tenant-scoped draft SPRS calculation snapshots with reviewer notes stored separately, evidence mapping, POA&M, asset inventory, system boundaries, ESP responsibility support, and affirmation tracking. |
| Vendors and subcontractors | `vendors`, `subcontractors`, `flow_down_clauses`, `contract_subcontractors`, `subcontractor_evidence` | Supplier risk, subcontractor access posture, contract workshare, required flow-downs, and evidence collection. |
| People and labor | `employees`, `training_records`, `wage_determinations`, `labor_category_rates`, `labor_classifications`, `payroll_records` | Training, SCA/DBA-friendly wage records, labor category mapping, and payroll evidence references. |
| Reporting and audit | `reports`, `report_contracts`, `report_obligations`, `report_evidence`, `esrs_report_data_rows`, `esrs_report_data_evidence`, `audit_log_entries` | Generated reports, tenant-scoped SAM.gov SPR preparation inputs with identity, eligibility, evidence, and review metadata, report source scope, immutable activity/audit history, and tenant/type-scoped SPRS generation idempotency keys. Legacy table names remain compatibility identifiers. |

## Design Choices

- The database schema uses the `gccs` PostgreSQL schema and snake_case table/column names.
- Enums are stored as strings for readable migrations and safer future enum additions.
- Frequently queried relationships are relational joins; descriptive lists such as tags, source clause numbers, applicability dimensions, and evidence examples are JSONB during development.
- Source-backed compliance content keeps source name, source URL, last-reviewed date, effective date, confidence, and expert-review requirements as first-class data.
- Clause records keep text version, effective date, source hash, review state, review owner, optional tenant scope, and superseded/replaced metadata so source updates remain auditable while tenant-owned custom clauses stay isolated.
- Contract clause attachments keep source library id, attachment reason, source document reference, soft-removal timestamp/user/reason, and audit columns so manual tagging remains traceable.
- Obligation records carry publication review metadata, flow-down flags, trigger logic, required action text, owner, risk, confidence, and linked evidence examples before customer-facing publication.
- Evidence upload intents are stateless guardrail preflights: they return normalized file metadata, validation status, and a pending malware-scan status without changing the evidence item or consuming a file-version identity. A durable `evidence_file_versions` row is created only after file bytes pass validation and malware scanning and are written to object storage; its ID is the file-version identity returned by download, deletion, audit, and downstream evidence references.
- CUI-relevant content tables store classification, classification source, confidence, reviewer, review timestamp, reason, and approved-demo flags; reclassification changes append to `content_classification_history`.
- CUI-ready checklist items link to exact supporting-record ids and versions. Security review, executed backup/restore controls, and incident readiness are supplied by their relational, append-versioned readiness aggregates; generic JSON evidence can no longer satisfy those three gate slots. Support evidence remains append-versioned in `cui_readiness_evidence`. A replacement, rejection, expiry, open high/critical security finding, or open critical incident gap invalidates the earlier link. Current published notice and responsibility-matrix acknowledgements remain their authoritative supporting records.
- Tenant-scoped operational tables include `tenant_id` indexes to support later tenant isolation enforcement in repositories and query filters.
- Audit log entries are append-only through normal application APIs and record tenant, actor, action, entity, timestamp, IP address, user agent, correlation ID, summary, and structured metadata.
- Subcontracting report data is preparation-only. Canonical SPR writes require a matching source-backed applicability period, a contract-linked subcontractor, current-tenant evidence references, reporting-role UEI, matching contract PIID, current fiscal-year/period metadata, whole-dollar amounts, and documented external eligibility confirmation. Edits reset review eligibility; only SPR-ready rows that are `Reviewed` or explicitly `Accepted` can enter final package preparation.

## Next Database Work

- Add repository implementations that map between Domain records and persistence entities.
- Add tenant query filters once tenant context exists in the API.
- Add migration seed data for the source-backed MVP obligation library.
- Add object storage-backed evidence file versions after upload guardrails and scan placeholder metadata are in place.
- Add explicit retention/export/deletion tables before production onboarding.
- Add import/export tracking tables for CSV imports and customer evidence/audit exports before paid production onboarding.

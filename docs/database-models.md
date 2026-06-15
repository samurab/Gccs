# Development Database Models

These artifacts turn the MVP domain model into a migration-ready PostgreSQL schema for the development phase. The schema supports the current No-CUI posture and keeps CUI-ready concepts visible without making evidence storage production-authorized for CUI.

## Artifact Set

- EF Core DbContext: `src/Gccs.Infrastructure/Persistence/GccsDbContext.cs`
- Persistence entities: `src/Gccs.Infrastructure/Persistence/Models/`
- Initial migration: `src/Gccs.Infrastructure/Persistence/Migrations/20260610031239_InitialDevelopmentSchema.cs`
- Clause review/versioning migration: `src/Gccs.Infrastructure/Persistence/Migrations/20260610051044_AddClauseReviewVersioning.cs`
- Tenant membership migration: `src/Gccs.Infrastructure/Persistence/Migrations/20260613213418_AddTenantMemberships.cs`
- Tenant invitation migration: `src/Gccs.Infrastructure/Persistence/Migrations/20260613221118_AddTenantInvitations.cs`
- No-CUI acknowledgement migration: `src/Gccs.Infrastructure/Persistence/Migrations/20260615003848_AddNoCuiAcknowledgements.cs`
- Evidence upload guardrails migration: `src/Gccs.Infrastructure/Persistence/Migrations/20260615005659_AddEvidenceUploadGuardrails.cs`
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

| Group | Primary tables | Purpose |
| --- | --- | --- |
| Tenancy and RBAC | `tenants`, `users`, `tenant_memberships`, `tenant_invitations`, `no_cui_acknowledgements`, `roles`, `user_roles`, `role_permissions` | Tenant isolation, explicit tenant membership assignments, invitation onboarding workflow, user-scoped No-CUI acknowledgement records, MFA-ready user profile, and role permissions. |
| Company profile | `company_profiles`, `company_naics_codes`, `company_certifications`, `company_locations` | SAM/SBA profile data, NAICS size support, certifications, locations, IT posture, and data handling posture. |
| Compliance content | `clauses`, `obligations`, `mvp_modules` | Source-backed clause and obligation library with source URL, review metadata, confidence, and expert-review flags. |
| Contract intake | `contracts`, `solicitations`, `contract_documents`, `contract_clauses`, `contract_clause_obligations`, `contract_deliverables`, `contract_reporting_deadlines` | Contract records, document metadata, extracted/manual clauses, obligations, deliverables, reporting dates, and flow-down signals. |
| Calendar and work | `compliance_tasks` | Due dates, renewals, evidence requests, policy reviews, corrective actions, and control assessment tasks. |
| Evidence vault | `evidence_items`, `evidence_obligations`, `evidence_contracts`, `evidence_controls`, `evidence_vendors`, `evidence_employees` | Folderless evidence tagging across obligations, contracts, controls, vendors, and employees. |
| CMMC workspace | `controls`, `assessments`, `control_assessments`, `poam_items`, `poam_evidence`, `assets`, `system_boundaries`, `system_boundary_assets`, `system_boundary_external_service_providers`, `system_boundary_evidence`, `annual_affirmations` | Level 1/2 readiness, evidence mapping, POA&M, asset inventory, system boundaries, ESP responsibility support, and affirmation tracking. |
| Vendors and subcontractors | `vendors`, `subcontractors`, `flow_down_clauses`, `contract_subcontractors`, `subcontractor_evidence` | Supplier risk, subcontractor access posture, contract workshare, required flow-downs, and evidence collection. |
| People and labor | `employees`, `training_records`, `wage_determinations`, `labor_category_rates`, `labor_classifications`, `payroll_records` | Training, SCA/DBA-friendly wage records, labor category mapping, and payroll evidence references. |
| Reporting and audit | `reports`, `report_contracts`, `report_obligations`, `report_evidence`, `audit_log_entries` | Generated reports, report source scope, and immutable activity/audit history. |

## Design Choices

- The database schema uses the `gccs` PostgreSQL schema and snake_case table/column names.
- Enums are stored as strings for readable migrations and safer future enum additions.
- Frequently queried relationships are relational joins; descriptive lists such as tags, source clause numbers, applicability dimensions, and evidence examples are JSONB during development.
- Source-backed compliance content keeps source name, source URL, last-reviewed date, effective date, confidence, and expert-review requirements as first-class data.
- Clause records keep text version, effective date, source hash, review state, review owner, and superseded/replaced metadata so source updates remain auditable.
- Evidence files are represented by metadata and storage URI only. Upload intents now record original file name, content type, file size, validation status, and malware scan placeholder status before later object storage workflows make files usable.
- Tenant-scoped operational tables include `tenant_id` indexes to support later tenant isolation enforcement in repositories and query filters.

## Next Database Work

- Add repository implementations that map between Domain records and persistence entities.
- Add tenant query filters once tenant context exists in the API.
- Add migration seed data for the source-backed MVP obligation library.
- Add object storage-backed evidence file versions after upload guardrails and scan placeholder metadata are in place.
- Add explicit retention/export/deletion tables before production onboarding.
- Add import/export tracking tables for CSV imports and customer evidence/audit exports before paid production onboarding.

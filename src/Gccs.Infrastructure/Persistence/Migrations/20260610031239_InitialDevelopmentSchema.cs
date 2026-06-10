using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialDevelopmentSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "gccs");

            migrationBuilder.CreateTable(
                name: "clauses",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    source = table.Column<string>(type: "text", nullable: false),
                    number = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    plain_english_summary = table.Column<string>(type: "text", nullable: false),
                    applicability_logic = table.Column<string>(type: "text", nullable: false),
                    required_action_ids_json = table.Column<string>(type: "jsonb", nullable: false),
                    usually_requires_flow_down = table.Column<bool>(type: "boolean", nullable: false),
                    source_name = table.Column<string>(type: "text", nullable: false),
                    source_url = table.Column<string>(type: "text", nullable: false),
                    source_last_reviewed_at = table.Column<DateOnly>(type: "date", nullable: false),
                    source_effective_at = table.Column<DateOnly>(type: "date", nullable: true),
                    source_confidence = table.Column<string>(type: "text", nullable: false),
                    source_requires_expert_review = table.Column<bool>(type: "boolean", nullable: false),
                    last_reviewed_at = table.Column<DateOnly>(type: "date", nullable: false),
                    reviewed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    next_review_due_at = table.Column<DateOnly>(type: "date", nullable: true),
                    confidence = table.Column<string>(type: "text", nullable: false),
                    requires_expert_review = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clauses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "controls",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    framework = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    cmmc_level = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    family = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    requirement = table.Column<string>(type: "text", nullable: false),
                    assessment_objective = table.Column<string>(type: "text", nullable: false),
                    evidence_examples_json = table.Column<string>(type: "jsonb", nullable: false),
                    source_name = table.Column<string>(type: "text", nullable: false),
                    source_url = table.Column<string>(type: "text", nullable: false),
                    source_last_reviewed_at = table.Column<DateOnly>(type: "date", nullable: false),
                    source_effective_at = table.Column<DateOnly>(type: "date", nullable: true),
                    source_confidence = table.Column<string>(type: "text", nullable: false),
                    source_requires_expert_review = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_controls", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mvp_modules",
                schema: "gccs",
                columns: table => new
                {
                    key = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    purpose = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mvp_modules", x => x.key);
                });

            migrationBuilder.CreateTable(
                name: "obligations",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    source = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    plain_english_summary = table.Column<string>(type: "text", nullable: false),
                    trigger_condition = table.Column<string>(type: "text", nullable: false),
                    required_action = table.Column<string>(type: "text", nullable: false),
                    owner_function = table.Column<string>(type: "text", nullable: false),
                    risk_level = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    flow_down_requirement = table.Column<string>(type: "text", nullable: false),
                    applicability_json = table.Column<string>(type: "jsonb", nullable: false),
                    evidence_examples_json = table.Column<string>(type: "jsonb", nullable: false),
                    source_name = table.Column<string>(type: "text", nullable: false),
                    source_url = table.Column<string>(type: "text", nullable: false),
                    source_last_reviewed_at = table.Column<DateOnly>(type: "date", nullable: false),
                    source_effective_at = table.Column<DateOnly>(type: "date", nullable: true),
                    source_confidence = table.Column<string>(type: "text", nullable: false),
                    source_requires_expert_review = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_obligations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tenants",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    data_posture = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    trial_ends_at = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenants", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "annual_affirmations",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    level = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    due_at = table.Column<DateOnly>(type: "date", nullable: false),
                    submitted_at = table.Column<DateOnly>(type: "date", nullable: true),
                    submitted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    confirmation_reference = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_annual_affirmations", x => x.id);
                    table.ForeignKey(
                        name: "FK_annual_affirmations_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "assessments",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    level = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    started_at = table.Column<DateOnly>(type: "date", nullable: false),
                    completed_at = table.Column<DateOnly>(type: "date", nullable: true),
                    affirmation_due_at = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assessments", x => x.id);
                    table.ForeignKey(
                        name: "FK_assessments_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "assets",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    owner_function = table.Column<string>(type: "text", nullable: false),
                    stores_fci = table.Column<bool>(type: "boolean", nullable: false),
                    stores_cui = table.Column<bool>(type: "boolean", nullable: false),
                    system_boundary_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tags_json = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assets", x => x.id);
                    table.ForeignKey(
                        name: "FK_assets_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "audit_log_entries",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    entity_type = table.Column<string>(type: "text", nullable: false),
                    entity_id = table.Column<string>(type: "text", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ip_address = table.Column<string>(type: "text", nullable: false),
                    user_agent = table.Column<string>(type: "text", nullable: false),
                    summary = table.Column<string>(type: "text", nullable: false),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_log_entries", x => x.id);
                    table.ForeignKey(
                        name: "FK_audit_log_entries_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "company_profiles",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    legal_entity_name = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    doing_business_as = table.Column<string>(type: "text", nullable: true),
                    uei = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    cage_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    sam_registration_expires_at = table.Column<DateOnly>(type: "date", nullable: true),
                    contractor_role = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    products_and_services = table.Column<string>(type: "text", nullable: false),
                    employee_range = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    revenue_range = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    it_environment_description = table.Column<string>(type: "text", nullable: false),
                    uses_external_service_provider = table.Column<bool>(type: "boolean", nullable: false),
                    external_service_provider_name = table.Column<string>(type: "text", nullable: true),
                    key_systems_json = table.Column<string>(type: "jsonb", nullable: false),
                    agency_customers_json = table.Column<string>(type: "jsonb", nullable: false),
                    data_handling_posture = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_company_profiles", x => x.id);
                    table.ForeignKey(
                        name: "FK_company_profiles_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "compliance_tasks",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    risk_level = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    assigned_to_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    owner_function = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    due_at = table.Column<DateOnly>(type: "date", nullable: true),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: true),
                    obligation_id = table.Column<string>(type: "text", nullable: true),
                    control_id = table.Column<string>(type: "text", nullable: true),
                    evidence_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_compliance_tasks", x => x.id);
                    table.ForeignKey(
                        name: "FK_compliance_tasks_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "contracts",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_number = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    agency_or_prime_name = table.Column<string>(type: "text", nullable: false),
                    relationship = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    awarded_at = table.Column<DateOnly>(type: "date", nullable: true),
                    period_of_performance_start = table.Column<DateOnly>(type: "date", nullable: false),
                    period_of_performance_end = table.Column<DateOnly>(type: "date", nullable: false),
                    place_of_performance = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contracts", x => x.id);
                    table.ForeignKey(
                        name: "FK_contracts_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "employees",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_number = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    job_title = table.Column<string>(type: "text", nullable: false),
                    labor_category = table.Column<string>(type: "text", nullable: false),
                    handles_fci = table.Column<bool>(type: "boolean", nullable: false),
                    handles_cui = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employees", x => x.id);
                    table.ForeignKey(
                        name: "FK_employees_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "evidence_items",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    storage_uri = table.Column<string>(type: "text", nullable: true),
                    file_hash = table.Column<string>(type: "text", nullable: true),
                    effective_at = table.Column<DateOnly>(type: "date", nullable: true),
                    expires_at = table.Column<DateOnly>(type: "date", nullable: true),
                    tags_json = table.Column<string>(type: "jsonb", nullable: false),
                    approved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evidence_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_evidence_items_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "labor_classifications",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    labor_category = table.Column<string>(type: "text", nullable: false),
                    basis_for_classification = table.Column<string>(type: "text", nullable: false),
                    wage_determination_id = table.Column<Guid>(type: "uuid", nullable: true),
                    evidence_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_labor_classifications", x => x.id);
                    table.ForeignKey(
                        name: "FK_labor_classifications_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payroll_records",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    period_start = table.Column<DateOnly>(type: "date", nullable: false),
                    period_end = table.Column<DateOnly>(type: "date", nullable: false),
                    hours_worked = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    wage_paid = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    fringe_paid = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    evidence_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payroll_records", x => x.id);
                    table.ForeignKey(
                        name: "FK_payroll_records_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "poam_items",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    control_id = table.Column<string>(type: "text", nullable: false),
                    weakness = table.Column<string>(type: "text", nullable: false),
                    planned_remediation = table.Column<string>(type: "text", nullable: false),
                    risk_level = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_completion_at = table.Column<DateOnly>(type: "date", nullable: false),
                    completed_at = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_poam_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_poam_items_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "reports",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    generated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    generated_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    storage_uri = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reports", x => x.id);
                    table.ForeignKey(
                        name: "FK_reports_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.id);
                    table.ForeignKey(
                        name: "FK_roles_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "solicitations",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    solicitation_number = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    agency = table.Column<string>(type: "text", nullable: false),
                    response_due_at = table.Column<DateOnly>(type: "date", nullable: true),
                    expected_contract_kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    set_aside = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_solicitations", x => x.id);
                    table.ForeignKey(
                        name: "FK_solicitations_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "subcontractors",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    uei = table.Column<string>(type: "text", nullable: true),
                    cage_code = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    workshare_description = table.Column<string>(type: "text", nullable: false),
                    workshare_percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    has_fci_access = table.Column<bool>(type: "boolean", nullable: false),
                    has_cui_access = table.Column<bool>(type: "boolean", nullable: false),
                    required_cmmc_level = table.Column<string>(type: "text", nullable: true),
                    contact_name = table.Column<string>(type: "text", nullable: true),
                    contact_email = table.Column<string>(type: "text", nullable: true),
                    contact_phone = table.Column<string>(type: "text", nullable: true),
                    contact_title = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subcontractors", x => x.id);
                    table.ForeignKey(
                        name: "FK_subcontractors_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "system_boundaries",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_system_boundaries", x => x.id);
                    table.ForeignKey(
                        name: "FK_system_boundaries_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "training_records",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    training_name = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    assigned_at = table.Column<DateOnly>(type: "date", nullable: false),
                    completed_at = table.Column<DateOnly>(type: "date", nullable: true),
                    expires_at = table.Column<DateOnly>(type: "date", nullable: true),
                    evidence_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_training_records", x => x.id);
                    table.ForeignKey(
                        name: "FK_training_records_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    mfa_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    last_signed_in_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                    table.ForeignKey(
                        name: "FK_users_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vendors",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    risk_level = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    contact_name = table.Column<string>(type: "text", nullable: true),
                    contact_email = table.Column<string>(type: "text", nullable: true),
                    contact_phone = table.Column<string>(type: "text", nullable: true),
                    contact_title = table.Column<string>(type: "text", nullable: true),
                    has_fci_access = table.Column<bool>(type: "boolean", nullable: false),
                    has_cui_access = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vendors", x => x.id);
                    table.ForeignKey(
                        name: "FK_vendors_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "wage_determinations",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    determination_number = table.Column<string>(type: "text", nullable: false),
                    revision = table.Column<string>(type: "text", nullable: false),
                    place_of_performance = table.Column<string>(type: "text", nullable: false),
                    effective_at = table.Column<DateOnly>(type: "date", nullable: false),
                    expires_at = table.Column<DateOnly>(type: "date", nullable: true),
                    source_url = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wage_determinations", x => x.id);
                    table.ForeignKey(
                        name: "FK_wage_determinations_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "control_assessments",
                schema: "gccs",
                columns: table => new
                {
                    assessment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    control_id = table.Column<string>(type: "text", nullable: false),
                    implementation_status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    result = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    notes = table.Column<string>(type: "text", nullable: false),
                    assessed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    assessed_at = table.Column<DateOnly>(type: "date", nullable: true),
                    evidence_item_ids_json = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_control_assessments", x => new { x.assessment_id, x.control_id });
                    table.ForeignKey(
                        name: "FK_control_assessments_assessments_assessment_id",
                        column: x => x.assessment_id,
                        principalSchema: "gccs",
                        principalTable: "assessments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_control_assessments_controls_control_id",
                        column: x => x.control_id,
                        principalSchema: "gccs",
                        principalTable: "controls",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "company_certifications",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    issuer = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    effective_at = table.Column<DateOnly>(type: "date", nullable: true),
                    expires_at = table.Column<DateOnly>(type: "date", nullable: true),
                    reference_number = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_company_certifications", x => x.id);
                    table.ForeignKey(
                        name: "FK_company_certifications_company_profiles_company_profile_id",
                        column: x => x.company_profile_id,
                        principalSchema: "gccs",
                        principalTable: "company_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "company_locations",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    street1 = table.Column<string>(type: "text", nullable: false),
                    street2 = table.Column<string>(type: "text", nullable: true),
                    city = table.Column<string>(type: "text", nullable: false),
                    state_or_province = table.Column<string>(type: "text", nullable: false),
                    postal_code = table.Column<string>(type: "text", nullable: false),
                    country = table.Column<string>(type: "text", nullable: false),
                    is_place_of_performance = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_company_locations", x => x.id);
                    table.ForeignKey(
                        name: "FK_company_locations_company_profiles_company_profile_id",
                        column: x => x.company_profile_id,
                        principalSchema: "gccs",
                        principalTable: "company_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "company_naics_codes",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    title = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    size_standard = table.Column<string>(type: "text", nullable: true),
                    qualifies_as_small = table.Column<bool>(type: "boolean", nullable: true),
                    last_checked_at = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_company_naics_codes", x => x.id);
                    table.ForeignKey(
                        name: "FK_company_naics_codes_company_profiles_company_profile_id",
                        column: x => x.company_profile_id,
                        principalSchema: "gccs",
                        principalTable: "company_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "contract_clauses",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    clause_number = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    alternate = table.Column<string>(type: "text", nullable: true),
                    full_text = table.Column<string>(type: "text", nullable: true),
                    source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    requires_flow_down = table.Column<bool>(type: "boolean", nullable: false),
                    last_reviewed_at = table.Column<DateOnly>(type: "date", nullable: false),
                    reviewed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    next_review_due_at = table.Column<DateOnly>(type: "date", nullable: true),
                    confidence = table.Column<string>(type: "text", nullable: false),
                    requires_expert_review = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contract_clauses", x => x.id);
                    table.ForeignKey(
                        name: "FK_contract_clauses_contracts_contract_id",
                        column: x => x.contract_id,
                        principalSchema: "gccs",
                        principalTable: "contracts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "contract_deliverables",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    due_at = table.Column<DateOnly>(type: "date", nullable: true),
                    owner_function = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contract_deliverables", x => x.id);
                    table.ForeignKey(
                        name: "FK_contract_deliverables_contracts_contract_id",
                        column: x => x.contract_id,
                        principalSchema: "gccs",
                        principalTable: "contracts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "contract_documents",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    file_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    storage_uri = table.Column<string>(type: "text", nullable: true),
                    extracted_text_hash = table.Column<string>(type: "text", nullable: true),
                    uploaded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    uploaded_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contains_potential_cui = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contract_documents", x => x.id);
                    table.ForeignKey(
                        name: "FK_contract_documents_contracts_contract_id",
                        column: x => x.contract_id,
                        principalSchema: "gccs",
                        principalTable: "contracts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "contract_reporting_deadlines",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    due_at = table.Column<DateOnly>(type: "date", nullable: false),
                    recurrence = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    owner_function = table.Column<string>(type: "text", nullable: false),
                    source_clause_numbers_json = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contract_reporting_deadlines", x => x.id);
                    table.ForeignKey(
                        name: "FK_contract_reporting_deadlines_contracts_contract_id",
                        column: x => x.contract_id,
                        principalSchema: "gccs",
                        principalTable: "contracts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "evidence_contracts",
                schema: "gccs",
                columns: table => new
                {
                    evidence_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evidence_contracts", x => new { x.evidence_item_id, x.contract_id });
                    table.ForeignKey(
                        name: "FK_evidence_contracts_contracts_contract_id",
                        column: x => x.contract_id,
                        principalSchema: "gccs",
                        principalTable: "contracts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_evidence_contracts_evidence_items_evidence_item_id",
                        column: x => x.evidence_item_id,
                        principalSchema: "gccs",
                        principalTable: "evidence_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "evidence_controls",
                schema: "gccs",
                columns: table => new
                {
                    evidence_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    control_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evidence_controls", x => new { x.evidence_item_id, x.control_id });
                    table.ForeignKey(
                        name: "FK_evidence_controls_controls_control_id",
                        column: x => x.control_id,
                        principalSchema: "gccs",
                        principalTable: "controls",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_evidence_controls_evidence_items_evidence_item_id",
                        column: x => x.evidence_item_id,
                        principalSchema: "gccs",
                        principalTable: "evidence_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "evidence_employees",
                schema: "gccs",
                columns: table => new
                {
                    evidence_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evidence_employees", x => new { x.evidence_item_id, x.employee_id });
                    table.ForeignKey(
                        name: "FK_evidence_employees_employees_employee_id",
                        column: x => x.employee_id,
                        principalSchema: "gccs",
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_evidence_employees_evidence_items_evidence_item_id",
                        column: x => x.evidence_item_id,
                        principalSchema: "gccs",
                        principalTable: "evidence_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "evidence_obligations",
                schema: "gccs",
                columns: table => new
                {
                    evidence_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    obligation_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evidence_obligations", x => new { x.evidence_item_id, x.obligation_id });
                    table.ForeignKey(
                        name: "FK_evidence_obligations_evidence_items_evidence_item_id",
                        column: x => x.evidence_item_id,
                        principalSchema: "gccs",
                        principalTable: "evidence_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_evidence_obligations_obligations_obligation_id",
                        column: x => x.obligation_id,
                        principalSchema: "gccs",
                        principalTable: "obligations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "poam_evidence",
                schema: "gccs",
                columns: table => new
                {
                    poam_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    evidence_item_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_poam_evidence", x => new { x.poam_item_id, x.evidence_item_id });
                    table.ForeignKey(
                        name: "FK_poam_evidence_evidence_items_evidence_item_id",
                        column: x => x.evidence_item_id,
                        principalSchema: "gccs",
                        principalTable: "evidence_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_poam_evidence_poam_items_poam_item_id",
                        column: x => x.poam_item_id,
                        principalSchema: "gccs",
                        principalTable: "poam_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "report_contracts",
                schema: "gccs",
                columns: table => new
                {
                    report_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_entity_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_contracts", x => new { x.report_id, x.contract_id });
                    table.ForeignKey(
                        name: "FK_report_contracts_reports_report_entity_id",
                        column: x => x.report_entity_id,
                        principalSchema: "gccs",
                        principalTable: "reports",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "report_evidence",
                schema: "gccs",
                columns: table => new
                {
                    report_id = table.Column<Guid>(type: "uuid", nullable: false),
                    evidence_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_entity_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_evidence", x => new { x.report_id, x.evidence_item_id });
                    table.ForeignKey(
                        name: "FK_report_evidence_reports_report_entity_id",
                        column: x => x.report_entity_id,
                        principalSchema: "gccs",
                        principalTable: "reports",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "report_obligations",
                schema: "gccs",
                columns: table => new
                {
                    report_id = table.Column<Guid>(type: "uuid", nullable: false),
                    obligation_id = table.Column<string>(type: "text", nullable: false),
                    report_entity_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_obligations", x => new { x.report_id, x.obligation_id });
                    table.ForeignKey(
                        name: "FK_report_obligations_reports_report_entity_id",
                        column: x => x.report_entity_id,
                        principalSchema: "gccs",
                        principalTable: "reports",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                schema: "gccs",
                columns: table => new
                {
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_permissions", x => new { x.role_id, x.permission });
                    table.ForeignKey(
                        name: "FK_role_permissions_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "gccs",
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "contract_subcontractors",
                schema: "gccs",
                columns: table => new
                {
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subcontractor_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contract_subcontractors", x => new { x.contract_id, x.subcontractor_id });
                    table.ForeignKey(
                        name: "FK_contract_subcontractors_contracts_contract_id",
                        column: x => x.contract_id,
                        principalSchema: "gccs",
                        principalTable: "contracts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_contract_subcontractors_subcontractors_subcontractor_id",
                        column: x => x.subcontractor_id,
                        principalSchema: "gccs",
                        principalTable: "subcontractors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "flow_down_clauses",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    subcontractor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    clause_number = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    sent_at = table.Column<DateOnly>(type: "date", nullable: true),
                    signed_at = table.Column<DateOnly>(type: "date", nullable: true),
                    signed_evidence_item_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_flow_down_clauses", x => x.id);
                    table.ForeignKey(
                        name: "FK_flow_down_clauses_subcontractors_subcontractor_id",
                        column: x => x.subcontractor_id,
                        principalSchema: "gccs",
                        principalTable: "subcontractors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "subcontractor_evidence",
                schema: "gccs",
                columns: table => new
                {
                    subcontractor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    evidence_item_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subcontractor_evidence", x => new { x.subcontractor_id, x.evidence_item_id });
                    table.ForeignKey(
                        name: "FK_subcontractor_evidence_evidence_items_evidence_item_id",
                        column: x => x.evidence_item_id,
                        principalSchema: "gccs",
                        principalTable: "evidence_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_subcontractor_evidence_subcontractors_subcontractor_id",
                        column: x => x.subcontractor_id,
                        principalSchema: "gccs",
                        principalTable: "subcontractors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "system_boundary_assets",
                schema: "gccs",
                columns: table => new
                {
                    system_boundary_id = table.Column<Guid>(type: "uuid", nullable: false),
                    asset_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_system_boundary_assets", x => new { x.system_boundary_id, x.asset_id });
                    table.ForeignKey(
                        name: "FK_system_boundary_assets_assets_asset_id",
                        column: x => x.asset_id,
                        principalSchema: "gccs",
                        principalTable: "assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_system_boundary_assets_system_boundaries_system_boundary_id",
                        column: x => x.system_boundary_id,
                        principalSchema: "gccs",
                        principalTable: "system_boundaries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "system_boundary_evidence",
                schema: "gccs",
                columns: table => new
                {
                    system_boundary_id = table.Column<Guid>(type: "uuid", nullable: false),
                    evidence_item_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_system_boundary_evidence", x => new { x.system_boundary_id, x.evidence_item_id });
                    table.ForeignKey(
                        name: "FK_system_boundary_evidence_evidence_items_evidence_item_id",
                        column: x => x.evidence_item_id,
                        principalSchema: "gccs",
                        principalTable: "evidence_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_system_boundary_evidence_system_boundaries_system_boundary_~",
                        column: x => x.system_boundary_id,
                        principalSchema: "gccs",
                        principalTable: "system_boundaries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                schema: "gccs",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "FK_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "gccs",
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "gccs",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "evidence_vendors",
                schema: "gccs",
                columns: table => new
                {
                    evidence_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vendor_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evidence_vendors", x => new { x.evidence_item_id, x.vendor_id });
                    table.ForeignKey(
                        name: "FK_evidence_vendors_evidence_items_evidence_item_id",
                        column: x => x.evidence_item_id,
                        principalSchema: "gccs",
                        principalTable: "evidence_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_evidence_vendors_vendors_vendor_id",
                        column: x => x.vendor_id,
                        principalSchema: "gccs",
                        principalTable: "vendors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "system_boundary_external_service_providers",
                schema: "gccs",
                columns: table => new
                {
                    system_boundary_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vendor_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_system_boundary_external_service_providers", x => new { x.system_boundary_id, x.vendor_id });
                    table.ForeignKey(
                        name: "FK_system_boundary_external_service_providers_system_boundarie~",
                        column: x => x.system_boundary_id,
                        principalSchema: "gccs",
                        principalTable: "system_boundaries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_system_boundary_external_service_providers_vendors_vendor_id",
                        column: x => x.vendor_id,
                        principalSchema: "gccs",
                        principalTable: "vendors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "labor_category_rates",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    wage_determination_id = table.Column<Guid>(type: "uuid", nullable: false),
                    labor_category = table.Column<string>(type: "text", nullable: false),
                    hourly_wage = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    fringe_benefit_rate = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_labor_category_rates", x => x.id);
                    table.ForeignKey(
                        name: "FK_labor_category_rates_wage_determinations_wage_determination~",
                        column: x => x.wage_determination_id,
                        principalSchema: "gccs",
                        principalTable: "wage_determinations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "contract_clause_obligations",
                schema: "gccs",
                columns: table => new
                {
                    contract_clause_id = table.Column<Guid>(type: "uuid", nullable: false),
                    obligation_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contract_clause_obligations", x => new { x.contract_clause_id, x.obligation_id });
                    table.ForeignKey(
                        name: "FK_contract_clause_obligations_contract_clauses_contract_claus~",
                        column: x => x.contract_clause_id,
                        principalSchema: "gccs",
                        principalTable: "contract_clauses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_contract_clause_obligations_obligations_obligation_id",
                        column: x => x.obligation_id,
                        principalSchema: "gccs",
                        principalTable: "obligations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_annual_affirmations_tenant_id_status_due_at",
                schema: "gccs",
                table: "annual_affirmations",
                columns: new[] { "tenant_id", "status", "due_at" });

            migrationBuilder.CreateIndex(
                name: "IX_assessments_created_at_updated_at",
                schema: "gccs",
                table: "assessments",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_assessments_tenant_id_status_level",
                schema: "gccs",
                table: "assessments",
                columns: new[] { "tenant_id", "status", "level" });

            migrationBuilder.CreateIndex(
                name: "IX_assets_created_at_updated_at",
                schema: "gccs",
                table: "assets",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_assets_tenant_id_system_boundary_id",
                schema: "gccs",
                table: "assets",
                columns: new[] { "tenant_id", "system_boundary_id" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_entries_tenant_id_entity_type_entity_id",
                schema: "gccs",
                table: "audit_log_entries",
                columns: new[] { "tenant_id", "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_entries_tenant_id_occurred_at",
                schema: "gccs",
                table: "audit_log_entries",
                columns: new[] { "tenant_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_clauses_source_number",
                schema: "gccs",
                table: "clauses",
                columns: new[] { "source", "number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_company_certifications_company_profile_id_type",
                schema: "gccs",
                table: "company_certifications",
                columns: new[] { "company_profile_id", "type" });

            migrationBuilder.CreateIndex(
                name: "IX_company_locations_company_profile_id",
                schema: "gccs",
                table: "company_locations",
                column: "company_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_company_naics_codes_company_profile_id_code",
                schema: "gccs",
                table: "company_naics_codes",
                columns: new[] { "company_profile_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_company_profiles_created_at_updated_at",
                schema: "gccs",
                table: "company_profiles",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_company_profiles_tenant_id",
                schema: "gccs",
                table: "company_profiles",
                column: "tenant_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_company_profiles_uei",
                schema: "gccs",
                table: "company_profiles",
                column: "uei");

            migrationBuilder.CreateIndex(
                name: "IX_compliance_tasks_created_at_updated_at",
                schema: "gccs",
                table: "compliance_tasks",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_compliance_tasks_tenant_id_contract_id",
                schema: "gccs",
                table: "compliance_tasks",
                columns: new[] { "tenant_id", "contract_id" });

            migrationBuilder.CreateIndex(
                name: "IX_compliance_tasks_tenant_id_obligation_id",
                schema: "gccs",
                table: "compliance_tasks",
                columns: new[] { "tenant_id", "obligation_id" });

            migrationBuilder.CreateIndex(
                name: "IX_compliance_tasks_tenant_id_status_due_at",
                schema: "gccs",
                table: "compliance_tasks",
                columns: new[] { "tenant_id", "status", "due_at" });

            migrationBuilder.CreateIndex(
                name: "IX_contract_clause_obligations_obligation_id",
                schema: "gccs",
                table: "contract_clause_obligations",
                column: "obligation_id");

            migrationBuilder.CreateIndex(
                name: "IX_contract_clauses_contract_id_clause_number",
                schema: "gccs",
                table: "contract_clauses",
                columns: new[] { "contract_id", "clause_number" });

            migrationBuilder.CreateIndex(
                name: "IX_contract_deliverables_contract_id_due_at",
                schema: "gccs",
                table: "contract_deliverables",
                columns: new[] { "contract_id", "due_at" });

            migrationBuilder.CreateIndex(
                name: "IX_contract_documents_contract_id_type",
                schema: "gccs",
                table: "contract_documents",
                columns: new[] { "contract_id", "type" });

            migrationBuilder.CreateIndex(
                name: "IX_contract_reporting_deadlines_contract_id_due_at",
                schema: "gccs",
                table: "contract_reporting_deadlines",
                columns: new[] { "contract_id", "due_at" });

            migrationBuilder.CreateIndex(
                name: "IX_contract_subcontractors_subcontractor_id",
                schema: "gccs",
                table: "contract_subcontractors",
                column: "subcontractor_id");

            migrationBuilder.CreateIndex(
                name: "IX_contracts_created_at_updated_at",
                schema: "gccs",
                table: "contracts",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_contracts_tenant_id_contract_number",
                schema: "gccs",
                table: "contracts",
                columns: new[] { "tenant_id", "contract_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_contracts_tenant_id_status",
                schema: "gccs",
                table: "contracts",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_control_assessments_control_id",
                schema: "gccs",
                table: "control_assessments",
                column: "control_id");

            migrationBuilder.CreateIndex(
                name: "IX_controls_framework_cmmc_level",
                schema: "gccs",
                table: "controls",
                columns: new[] { "framework", "cmmc_level" });

            migrationBuilder.CreateIndex(
                name: "IX_employees_created_at_updated_at",
                schema: "gccs",
                table: "employees",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_employees_tenant_id_email",
                schema: "gccs",
                table: "employees",
                columns: new[] { "tenant_id", "email" });

            migrationBuilder.CreateIndex(
                name: "IX_employees_tenant_id_employee_number",
                schema: "gccs",
                table: "employees",
                columns: new[] { "tenant_id", "employee_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_evidence_contracts_contract_id",
                schema: "gccs",
                table: "evidence_contracts",
                column: "contract_id");

            migrationBuilder.CreateIndex(
                name: "IX_evidence_controls_control_id",
                schema: "gccs",
                table: "evidence_controls",
                column: "control_id");

            migrationBuilder.CreateIndex(
                name: "IX_evidence_employees_employee_id",
                schema: "gccs",
                table: "evidence_employees",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_evidence_items_created_at_updated_at",
                schema: "gccs",
                table: "evidence_items",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_evidence_items_tenant_id_expires_at",
                schema: "gccs",
                table: "evidence_items",
                columns: new[] { "tenant_id", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "IX_evidence_items_tenant_id_status",
                schema: "gccs",
                table: "evidence_items",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_evidence_obligations_obligation_id",
                schema: "gccs",
                table: "evidence_obligations",
                column: "obligation_id");

            migrationBuilder.CreateIndex(
                name: "IX_evidence_vendors_vendor_id",
                schema: "gccs",
                table: "evidence_vendors",
                column: "vendor_id");

            migrationBuilder.CreateIndex(
                name: "IX_flow_down_clauses_subcontractor_id_clause_number",
                schema: "gccs",
                table: "flow_down_clauses",
                columns: new[] { "subcontractor_id", "clause_number" });

            migrationBuilder.CreateIndex(
                name: "IX_labor_category_rates_wage_determination_id",
                schema: "gccs",
                table: "labor_category_rates",
                column: "wage_determination_id");

            migrationBuilder.CreateIndex(
                name: "IX_labor_classifications_created_at_updated_at",
                schema: "gccs",
                table: "labor_classifications",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_labor_classifications_tenant_id_employee_id_contract_id",
                schema: "gccs",
                table: "labor_classifications",
                columns: new[] { "tenant_id", "employee_id", "contract_id" });

            migrationBuilder.CreateIndex(
                name: "IX_obligations_source",
                schema: "gccs",
                table: "obligations",
                column: "source");

            migrationBuilder.CreateIndex(
                name: "IX_payroll_records_created_at_updated_at",
                schema: "gccs",
                table: "payroll_records",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_payroll_records_tenant_id_employee_id_period_start_period_e~",
                schema: "gccs",
                table: "payroll_records",
                columns: new[] { "tenant_id", "employee_id", "period_start", "period_end" });

            migrationBuilder.CreateIndex(
                name: "IX_poam_evidence_evidence_item_id",
                schema: "gccs",
                table: "poam_evidence",
                column: "evidence_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_poam_items_created_at_updated_at",
                schema: "gccs",
                table: "poam_items",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_poam_items_tenant_id_status_target_completion_at",
                schema: "gccs",
                table: "poam_items",
                columns: new[] { "tenant_id", "status", "target_completion_at" });

            migrationBuilder.CreateIndex(
                name: "IX_report_contracts_report_entity_id",
                schema: "gccs",
                table: "report_contracts",
                column: "report_entity_id");

            migrationBuilder.CreateIndex(
                name: "IX_report_evidence_report_entity_id",
                schema: "gccs",
                table: "report_evidence",
                column: "report_entity_id");

            migrationBuilder.CreateIndex(
                name: "IX_report_obligations_report_entity_id",
                schema: "gccs",
                table: "report_obligations",
                column: "report_entity_id");

            migrationBuilder.CreateIndex(
                name: "IX_reports_created_at_updated_at",
                schema: "gccs",
                table: "reports",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_reports_tenant_id_type_status",
                schema: "gccs",
                table: "reports",
                columns: new[] { "tenant_id", "type", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_roles_created_at_updated_at",
                schema: "gccs",
                table: "roles",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_roles_tenant_id_name",
                schema: "gccs",
                table: "roles",
                columns: new[] { "tenant_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_solicitations_created_at_updated_at",
                schema: "gccs",
                table: "solicitations",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_solicitations_tenant_id_solicitation_number",
                schema: "gccs",
                table: "solicitations",
                columns: new[] { "tenant_id", "solicitation_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subcontractor_evidence_evidence_item_id",
                schema: "gccs",
                table: "subcontractor_evidence",
                column: "evidence_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_subcontractors_created_at_updated_at",
                schema: "gccs",
                table: "subcontractors",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_subcontractors_tenant_id_name",
                schema: "gccs",
                table: "subcontractors",
                columns: new[] { "tenant_id", "name" });

            migrationBuilder.CreateIndex(
                name: "IX_subcontractors_tenant_id_uei",
                schema: "gccs",
                table: "subcontractors",
                columns: new[] { "tenant_id", "uei" });

            migrationBuilder.CreateIndex(
                name: "IX_system_boundaries_created_at_updated_at",
                schema: "gccs",
                table: "system_boundaries",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_system_boundaries_tenant_id_status",
                schema: "gccs",
                table: "system_boundaries",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_system_boundary_assets_asset_id",
                schema: "gccs",
                table: "system_boundary_assets",
                column: "asset_id");

            migrationBuilder.CreateIndex(
                name: "IX_system_boundary_evidence_evidence_item_id",
                schema: "gccs",
                table: "system_boundary_evidence",
                column: "evidence_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_system_boundary_external_service_providers_vendor_id",
                schema: "gccs",
                table: "system_boundary_external_service_providers",
                column: "vendor_id");

            migrationBuilder.CreateIndex(
                name: "IX_tenants_created_at_updated_at",
                schema: "gccs",
                table: "tenants",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_training_records_created_at_updated_at",
                schema: "gccs",
                table: "training_records",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_training_records_tenant_id_employee_id_status",
                schema: "gccs",
                table: "training_records",
                columns: new[] { "tenant_id", "employee_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_training_records_tenant_id_expires_at",
                schema: "gccs",
                table: "training_records",
                columns: new[] { "tenant_id", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_role_id",
                schema: "gccs",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_created_at_updated_at",
                schema: "gccs",
                table: "users",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_users_tenant_id_email",
                schema: "gccs",
                table: "users",
                columns: new[] { "tenant_id", "email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vendors_created_at_updated_at",
                schema: "gccs",
                table: "vendors",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_vendors_tenant_id_type",
                schema: "gccs",
                table: "vendors",
                columns: new[] { "tenant_id", "type" });

            migrationBuilder.CreateIndex(
                name: "IX_wage_determinations_created_at_updated_at",
                schema: "gccs",
                table: "wage_determinations",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_wage_determinations_tenant_id_determination_number_revision",
                schema: "gccs",
                table: "wage_determinations",
                columns: new[] { "tenant_id", "determination_number", "revision" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "annual_affirmations",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "audit_log_entries",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "clauses",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "company_certifications",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "company_locations",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "company_naics_codes",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "compliance_tasks",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "contract_clause_obligations",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "contract_deliverables",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "contract_documents",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "contract_reporting_deadlines",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "contract_subcontractors",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "control_assessments",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "evidence_contracts",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "evidence_controls",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "evidence_employees",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "evidence_obligations",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "evidence_vendors",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "flow_down_clauses",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "labor_category_rates",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "labor_classifications",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "mvp_modules",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "payroll_records",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "poam_evidence",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "report_contracts",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "report_evidence",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "report_obligations",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "role_permissions",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "solicitations",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "subcontractor_evidence",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "system_boundary_assets",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "system_boundary_evidence",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "system_boundary_external_service_providers",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "training_records",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "user_roles",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "company_profiles",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "contract_clauses",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "assessments",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "controls",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "employees",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "obligations",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "wage_determinations",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "poam_items",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "reports",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "subcontractors",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "assets",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "evidence_items",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "system_boundaries",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "vendors",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "roles",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "users",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "contracts",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "tenants",
                schema: "gccs");
        }
    }
}

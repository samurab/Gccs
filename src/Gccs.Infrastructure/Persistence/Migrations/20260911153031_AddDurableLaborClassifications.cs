using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDurableLaborClassifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_employees_tenant_id_id",
                schema: "gccs",
                table: "employees",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.CreateTable(
                name: "labor_categories",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    wage_determination_classification = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    hourly_wage = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    fringe_rate = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    fringe_description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    effective_start = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_end = table.Column<DateOnly>(type: "date", nullable: true),
                    source_reference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_labor_categories", x => x.id);
                    table.UniqueConstraint("AK_labor_categories_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("CK_labor_categories_effective_dates", "effective_end IS NULL OR effective_end >= effective_start");
                    table.CheckConstraint("CK_labor_categories_nonnegative_rates", "hourly_wage >= 0 AND fringe_rate >= 0");
                    table.ForeignKey(
                        name: "FK_labor_categories_contracts_tenant_id_contract_id",
                        columns: x => new { x.tenant_id, x.contract_id },
                        principalSchema: "gccs",
                        principalTable: "contracts",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_labor_categories_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "labor_employee_assignments",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    labor_category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_location = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    effective_start = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_end = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    source_reference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    review_status = table.Column<int>(type: "integer", nullable: false),
                    review_notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    reviewed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_labor_employee_assignments", x => x.id);
                    table.UniqueConstraint("AK_labor_employee_assignments_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("CK_labor_employee_assignments_effective_dates", "effective_end IS NULL OR effective_end >= effective_start");
                    table.CheckConstraint("CK_labor_employee_assignments_review_metadata", "(review_status = 0 AND reviewed_by_user_id IS NULL AND reviewed_at IS NULL) OR (review_status IN (1, 2) AND review_notes IS NOT NULL AND reviewed_by_user_id IS NOT NULL AND reviewed_at IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_labor_employee_assignments_contracts_tenant_id_contract_id",
                        columns: x => new { x.tenant_id, x.contract_id },
                        principalSchema: "gccs",
                        principalTable: "contracts",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_labor_employee_assignments_employees_tenant_id_employee_id",
                        columns: x => new { x.tenant_id, x.employee_id },
                        principalSchema: "gccs",
                        principalTable: "employees",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_labor_employee_assignments_labor_categories_tenant_id_labor~",
                        columns: x => new { x.tenant_id, x.labor_category_id },
                        principalSchema: "gccs",
                        principalTable: "labor_categories",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_labor_employee_assignments_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "labor_classification_evidence",
                schema: "gccs",
                columns: table => new
                {
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    evidence_item_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_labor_classification_evidence", x => new { x.tenant_id, x.assignment_id, x.evidence_item_id });
                    table.ForeignKey(
                        name: "FK_labor_classification_evidence_evidence_items_tenant_id_evid~",
                        columns: x => new { x.tenant_id, x.evidence_item_id },
                        principalSchema: "gccs",
                        principalTable: "evidence_items",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_labor_classification_evidence_labor_employee_assignments_te~",
                        columns: x => new { x.tenant_id, x.assignment_id },
                        principalSchema: "gccs",
                        principalTable: "labor_employee_assignments",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_labor_classification_evidence_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "labor_classification_history",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    prior_category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    prior_category_title = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    new_category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    new_category_title = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_labor_classification_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_labor_classification_history_labor_employee_assignments_ten~",
                        columns: x => new { x.tenant_id, x.assignment_id },
                        principalSchema: "gccs",
                        principalTable: "labor_employee_assignments",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_labor_classification_history_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_labor_categories_created_at_updated_at",
                schema: "gccs",
                table: "labor_categories",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_labor_categories_tenant_id_contract_id_is_active",
                schema: "gccs",
                table: "labor_categories",
                columns: new[] { "tenant_id", "contract_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_labor_classification_evidence_tenant_id_evidence_item_id",
                schema: "gccs",
                table: "labor_classification_evidence",
                columns: new[] { "tenant_id", "evidence_item_id" });

            migrationBuilder.CreateIndex(
                name: "IX_labor_classification_history_tenant_id_assignment_id_change~",
                schema: "gccs",
                table: "labor_classification_history",
                columns: new[] { "tenant_id", "assignment_id", "changed_at" });

            migrationBuilder.CreateIndex(
                name: "IX_labor_employee_assignments_created_at_updated_at",
                schema: "gccs",
                table: "labor_employee_assignments",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_labor_employee_assignments_tenant_id_contract_id_status",
                schema: "gccs",
                table: "labor_employee_assignments",
                columns: new[] { "tenant_id", "contract_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_labor_employee_assignments_tenant_id_employee_id_contract_id",
                schema: "gccs",
                table: "labor_employee_assignments",
                columns: new[] { "tenant_id", "employee_id", "contract_id" });

            migrationBuilder.CreateIndex(
                name: "IX_labor_employee_assignments_tenant_id_labor_category_id",
                schema: "gccs",
                table: "labor_employee_assignments",
                columns: new[] { "tenant_id", "labor_category_id" });

            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS btree_gist;");
            migrationBuilder.Sql("""
                ALTER TABLE gccs.labor_employee_assignments
                ADD CONSTRAINT labor_assignment_no_overlap
                EXCLUDE USING gist (
                    tenant_id WITH =,
                    employee_id WITH =,
                    contract_id WITH =,
                    daterange(effective_start, COALESCE(effective_end, 'infinity'::date), '[]') WITH &&
                ) WHERE (status = 0);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "labor_classification_evidence",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "labor_classification_history",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "labor_employee_assignments",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "labor_categories",
                schema: "gccs");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_employees_tenant_id_id",
                schema: "gccs",
                table: "employees");
        }
    }
}

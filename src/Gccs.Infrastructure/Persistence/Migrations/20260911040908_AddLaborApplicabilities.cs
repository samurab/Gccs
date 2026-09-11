using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLaborApplicabilities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "labor_applicabilities",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sca_applicable = table.Column<bool>(type: "boolean", nullable: false),
                    dba_applicable = table.Column<bool>(type: "boolean", nullable: false),
                    other_far_part22_obligations = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    place_of_performance = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    contract_period_start = table.Column<DateOnly>(type: "date", nullable: false),
                    contract_period_end = table.Column<DateOnly>(type: "date", nullable: false),
                    wage_determination_reference = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    wage_determination_evidence_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_contract_clause_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_clause = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    rationale = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    owner_function = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    review_status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    review_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    reviewed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_labor_applicabilities", x => x.id);
                    table.UniqueConstraint("AK_labor_applicabilities_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("CK_labor_applicabilities_contract_period", "contract_period_end >= contract_period_start");
                    table.ForeignKey(
                        name: "FK_labor_applicabilities_compliance_tasks_task_id",
                        column: x => x.task_id,
                        principalSchema: "gccs",
                        principalTable: "compliance_tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_labor_applicabilities_contract_clauses_source_contract_clau~",
                        column: x => x.source_contract_clause_id,
                        principalSchema: "gccs",
                        principalTable: "contract_clauses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_labor_applicabilities_contracts_tenant_id_contract_id",
                        columns: x => new { x.tenant_id, x.contract_id },
                        principalSchema: "gccs",
                        principalTable: "contracts",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_labor_applicabilities_evidence_items_wage_determination_evi~",
                        column: x => x.wage_determination_evidence_item_id,
                        principalSchema: "gccs",
                        principalTable: "evidence_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_labor_applicabilities_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_labor_applicabilities_created_at_updated_at",
                schema: "gccs",
                table: "labor_applicabilities",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_labor_applicabilities_source_contract_clause_id",
                schema: "gccs",
                table: "labor_applicabilities",
                column: "source_contract_clause_id");

            migrationBuilder.CreateIndex(
                name: "IX_labor_applicabilities_task_id",
                schema: "gccs",
                table: "labor_applicabilities",
                column: "task_id",
                unique: true,
                filter: "task_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_labor_applicabilities_tenant_id_contract_id_status",
                schema: "gccs",
                table: "labor_applicabilities",
                columns: new[] { "tenant_id", "contract_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_labor_applicabilities_tenant_id_contract_period_end",
                schema: "gccs",
                table: "labor_applicabilities",
                columns: new[] { "tenant_id", "contract_period_end" });

            migrationBuilder.CreateIndex(
                name: "IX_labor_applicabilities_wage_determination_evidence_item_id",
                schema: "gccs",
                table: "labor_applicabilities",
                column: "wage_determination_evidence_item_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "labor_applicabilities",
                schema: "gccs");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDurableEsrsApplicabilities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "esrs_applicabilities",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    agency = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    subcontracting_plan_type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    prime_or_lower_tier_role = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    report_type = table.Column<int>(type: "integer", nullable: false),
                    period_start = table.Column<DateOnly>(type: "date", nullable: false),
                    period_end = table.Column<DateOnly>(type: "date", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    source_clause = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: true),
                    rationale = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    owner_function = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    assigned_to_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reviewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_esrs_applicabilities", x => x.id);
                    table.ForeignKey(
                        name: "FK_esrs_applicabilities_compliance_tasks_task_id",
                        column: x => x.task_id,
                        principalSchema: "gccs",
                        principalTable: "compliance_tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_esrs_applicabilities_contracts_contract_id",
                        column: x => x.contract_id,
                        principalSchema: "gccs",
                        principalTable: "contracts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_esrs_applicabilities_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_esrs_applicabilities_contract_id",
                schema: "gccs",
                table: "esrs_applicabilities",
                column: "contract_id");

            migrationBuilder.CreateIndex(
                name: "IX_esrs_applicabilities_created_at_updated_at",
                schema: "gccs",
                table: "esrs_applicabilities",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_esrs_applicabilities_task_id",
                schema: "gccs",
                table: "esrs_applicabilities",
                column: "task_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_esrs_applicabilities_tenant_id_contract_id_due_date",
                schema: "gccs",
                table: "esrs_applicabilities",
                columns: new[] { "tenant_id", "contract_id", "due_date" });

            migrationBuilder.CreateIndex(
                name: "IX_esrs_applicabilities_tenant_id_contract_id_report_type_peri~",
                schema: "gccs",
                table: "esrs_applicabilities",
                columns: new[] { "tenant_id", "contract_id", "report_type", "period_start", "period_end" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "esrs_applicabilities",
                schema: "gccs");
        }
    }
}

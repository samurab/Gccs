using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContractSizeChecks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "contract_size_checks",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    naics_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    result = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    metric = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    threshold = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    unit = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    entered_value = table.Column<decimal>(type: "numeric", nullable: true),
                    missing_information_json = table.Column<string>(type: "jsonb", nullable: false),
                    source_url = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: true),
                    source_effective_at = table.Column<DateOnly>(type: "date", nullable: true),
                    source_last_reviewed_at = table.Column<DateOnly>(type: "date", nullable: true),
                    expert_review_task_id = table.Column<Guid>(type: "uuid", nullable: true),
                    run_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    run_by_user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contract_size_checks", x => x.id);
                    table.ForeignKey(
                        name: "FK_contract_size_checks_compliance_tasks_expert_review_task_id",
                        column: x => x.expert_review_task_id,
                        principalSchema: "gccs",
                        principalTable: "compliance_tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_contract_size_checks_contracts_contract_id",
                        column: x => x.contract_id,
                        principalSchema: "gccs",
                        principalTable: "contracts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_contract_size_checks_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_contract_size_checks_contract_id",
                schema: "gccs",
                table: "contract_size_checks",
                column: "contract_id");

            migrationBuilder.CreateIndex(
                name: "IX_contract_size_checks_expert_review_task_id",
                schema: "gccs",
                table: "contract_size_checks",
                column: "expert_review_task_id");

            migrationBuilder.CreateIndex(
                name: "IX_contract_size_checks_tenant_id_contract_id_run_at",
                schema: "gccs",
                table: "contract_size_checks",
                columns: new[] { "tenant_id", "contract_id", "run_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contract_size_checks",
                schema: "gccs");
        }
    }
}

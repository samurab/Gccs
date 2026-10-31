using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddObligationApplicabilityEvaluations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "obligation_applicability_evaluations",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_clause_id = table.Column<Guid>(type: "uuid", nullable: false),
                    obligation_id = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    previous_evaluation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_rule_id = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    state = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    explanation = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    facts_used_json = table.Column<string>(type: "jsonb", nullable: false),
                    missing_facts_json = table.Column<string>(type: "jsonb", nullable: false),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: false),
                    evaluated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    evaluated_by_user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_obligation_applicability_evaluations", x => x.id);
                    table.ForeignKey(
                        name: "FK_obligation_applicability_evaluations_contract_clause_obliga~",
                        columns: x => new { x.contract_clause_id, x.obligation_id },
                        principalSchema: "gccs",
                        principalTable: "contract_clause_obligations",
                        principalColumns: new[] { "contract_clause_id", "obligation_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_obligation_applicability_evaluations_obligation_applicabili~",
                        column: x => x.previous_evaluation_id,
                        principalSchema: "gccs",
                        principalTable: "obligation_applicability_evaluations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_obligation_applicability_evaluations_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_obligation_applicability_evaluations_contract_clause_id_obl~",
                schema: "gccs",
                table: "obligation_applicability_evaluations",
                columns: new[] { "contract_clause_id", "obligation_id" });

            migrationBuilder.CreateIndex(
                name: "IX_obligation_applicability_evaluations_previous_evaluation_id",
                schema: "gccs",
                table: "obligation_applicability_evaluations",
                column: "previous_evaluation_id");

            migrationBuilder.CreateIndex(
                name: "IX_obligation_applicability_evaluations_tenant_id_contract_cla~",
                schema: "gccs",
                table: "obligation_applicability_evaluations",
                columns: new[] { "tenant_id", "contract_clause_id", "obligation_id", "evaluated_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "obligation_applicability_evaluations",
                schema: "gccs");
        }
    }
}

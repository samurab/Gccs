using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDurableSprsScoreCalculations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sprs_score_calculations",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assessment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule_set_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    rule_set_version = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    rule_set_source_url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    rule_set_source_sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    maximum_score = table.Column<int>(type: "integer", nullable: false),
                    score = table.Column<int>(type: "integer", nullable: false),
                    total_deduction = table.Column<int>(type: "integer", nullable: false),
                    line_items_json = table.Column<string>(type: "jsonb", nullable: false),
                    unresolved_gaps_json = table.Column<string>(type: "jsonb", nullable: false),
                    generated_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    generated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sprs_score_calculations", x => x.id);
                    table.ForeignKey(
                        name: "FK_sprs_score_calculations_assessments_assessment_id",
                        column: x => x.assessment_id,
                        principalSchema: "gccs",
                        principalTable: "assessments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sprs_score_calculations_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sprs_score_calculation_notes",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    calculation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    classification = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    classification_source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    classification_confidence = table.Column<decimal>(type: "numeric", nullable: true),
                    classification_reviewed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    classification_reviewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    classification_reason = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: true),
                    classification_is_approved_demo_content = table.Column<bool>(type: "boolean", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sprs_score_calculation_notes", x => x.id);
                    table.ForeignKey(
                        name: "FK_sprs_score_calculation_notes_sprs_score_calculations_calcul~",
                        column: x => x.calculation_id,
                        principalSchema: "gccs",
                        principalTable: "sprs_score_calculations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_sprs_score_calculation_notes_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sprs_score_calculation_notes_calculation_id",
                schema: "gccs",
                table: "sprs_score_calculation_notes",
                column: "calculation_id");

            migrationBuilder.CreateIndex(
                name: "IX_sprs_score_calculation_notes_tenant_id_calculation_id_creat~",
                schema: "gccs",
                table: "sprs_score_calculation_notes",
                columns: new[] { "tenant_id", "calculation_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_sprs_score_calculations_assessment_id",
                schema: "gccs",
                table: "sprs_score_calculations",
                column: "assessment_id");

            migrationBuilder.CreateIndex(
                name: "IX_sprs_score_calculations_tenant_id_assessment_id_generated_at",
                schema: "gccs",
                table: "sprs_score_calculations",
                columns: new[] { "tenant_id", "assessment_id", "generated_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sprs_score_calculation_notes",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "sprs_score_calculations",
                schema: "gccs");
        }
    }
}

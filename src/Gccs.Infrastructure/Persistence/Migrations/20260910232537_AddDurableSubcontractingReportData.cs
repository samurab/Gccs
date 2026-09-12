using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDurableSubcontractingReportData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_subcontractors_tenant_id_id",
                schema: "gccs",
                table: "subcontractors",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_evidence_items_tenant_id_id",
                schema: "gccs",
                table: "evidence_items",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_contracts_tenant_id_id",
                schema: "gccs",
                table: "contracts",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.CreateTable(
                name: "esrs_report_data_rows",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subcontractor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    report_period_start = table.Column<DateOnly>(type: "date", nullable: false),
                    report_period_end = table.Column<DateOnly>(type: "date", nullable: false),
                    row_period_start = table.Column<DateOnly>(type: "date", nullable: false),
                    row_period_end = table.Column<DateOnly>(type: "date", nullable: false),
                    socioeconomic_category = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    socioeconomic_category_key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    plan_category = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    plan_category_key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    source_reference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    review_status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    reviewed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reviewer_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_esrs_report_data_rows", x => x.id);
                    table.UniqueConstraint("AK_esrs_report_data_rows_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("CK_esrs_report_data_rows_amount_nonnegative", "amount >= 0");
                    table.CheckConstraint("CK_esrs_report_data_rows_report_period", "report_period_end >= report_period_start");
                    table.CheckConstraint("CK_esrs_report_data_rows_row_period", "row_period_end >= row_period_start AND row_period_start >= report_period_start AND row_period_end <= report_period_end");
                    table.ForeignKey(
                        name: "FK_esrs_report_data_rows_contracts_tenant_id_contract_id",
                        columns: x => new { x.tenant_id, x.contract_id },
                        principalSchema: "gccs",
                        principalTable: "contracts",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_esrs_report_data_rows_subcontractors_tenant_id_subcontracto~",
                        columns: x => new { x.tenant_id, x.subcontractor_id },
                        principalSchema: "gccs",
                        principalTable: "subcontractors",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_esrs_report_data_rows_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_esrs_report_data_rows_users_reviewed_by_user_id",
                        column: x => x.reviewed_by_user_id,
                        principalSchema: "gccs",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "esrs_report_data_evidence",
                schema: "gccs",
                columns: table => new
                {
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_data_row_id = table.Column<Guid>(type: "uuid", nullable: false),
                    evidence_item_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_esrs_report_data_evidence", x => new { x.tenant_id, x.report_data_row_id, x.evidence_item_id });
                    table.ForeignKey(
                        name: "FK_esrs_report_data_evidence_esrs_report_data_rows_tenant_id_r~",
                        columns: x => new { x.tenant_id, x.report_data_row_id },
                        principalSchema: "gccs",
                        principalTable: "esrs_report_data_rows",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_esrs_report_data_evidence_evidence_items_tenant_id_evidence~",
                        columns: x => new { x.tenant_id, x.evidence_item_id },
                        principalSchema: "gccs",
                        principalTable: "evidence_items",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_esrs_report_data_evidence_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_esrs_report_data_evidence_tenant_id_evidence_item_id",
                schema: "gccs",
                table: "esrs_report_data_evidence",
                columns: new[] { "tenant_id", "evidence_item_id" });

            migrationBuilder.CreateIndex(
                name: "IX_esrs_report_data_rows_created_at_updated_at",
                schema: "gccs",
                table: "esrs_report_data_rows",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_esrs_report_data_rows_reviewed_by_user_id",
                schema: "gccs",
                table: "esrs_report_data_rows",
                column: "reviewed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_esrs_report_data_rows_tenant_id_contract_id_report_type_rep~",
                schema: "gccs",
                table: "esrs_report_data_rows",
                columns: new[] { "tenant_id", "contract_id", "report_type", "report_period_start", "report_period_end" });

            migrationBuilder.CreateIndex(
                name: "IX_esrs_report_data_rows_tenant_id_contract_id_subcontractor_i~",
                schema: "gccs",
                table: "esrs_report_data_rows",
                columns: new[] { "tenant_id", "contract_id", "subcontractor_id", "report_type", "report_period_start", "report_period_end", "row_period_start", "row_period_end", "socioeconomic_category_key", "plan_category_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_esrs_report_data_rows_tenant_id_subcontractor_id",
                schema: "gccs",
                table: "esrs_report_data_rows",
                columns: new[] { "tenant_id", "subcontractor_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "esrs_report_data_evidence",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "esrs_report_data_rows",
                schema: "gccs");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_subcontractors_tenant_id_id",
                schema: "gccs",
                table: "subcontractors");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_evidence_items_tenant_id_id",
                schema: "gccs",
                table: "evidence_items");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_contracts_tenant_id_id",
                schema: "gccs",
                table: "contracts");
        }
    }
}

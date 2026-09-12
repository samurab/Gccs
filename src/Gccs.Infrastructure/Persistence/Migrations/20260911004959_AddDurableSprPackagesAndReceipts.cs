using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDurableSprPackagesAndReceipts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "spr_report_packages",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    period_start = table.Column<DateOnly>(type: "date", nullable: false),
                    period_end = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    not_submitted_disclaimer = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    snapshot_json = table.Column<string>(type: "jsonb", nullable: false),
                    reviewer_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    reviewer_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    review_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    generated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_spr_report_packages", x => x.id);
                    table.UniqueConstraint("AK_spr_report_packages_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "FK_spr_report_packages_contracts_tenant_id_contract_id",
                        columns: x => new { x.tenant_id, x.contract_id },
                        principalSchema: "gccs",
                        principalTable: "contracts",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_spr_report_packages_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_spr_report_packages_users_reviewer_user_id",
                        column: x => x.reviewer_user_id,
                        principalSchema: "gccs",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "spr_manual_submission_receipts",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    package_id = table.Column<Guid>(type: "uuid", nullable: false),
                    submitted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    confirmation_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    outcome = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    evidence_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    supersedes_receipt_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recorded_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_spr_manual_submission_receipts", x => x.id);
                    table.UniqueConstraint("AK_spr_manual_submission_receipts_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "FK_spr_manual_submission_receipts_evidence_items_tenant_id_evi~",
                        columns: x => new { x.tenant_id, x.evidence_item_id },
                        principalSchema: "gccs",
                        principalTable: "evidence_items",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_spr_manual_submission_receipts_spr_manual_submission_receip~",
                        columns: x => new { x.tenant_id, x.supersedes_receipt_id },
                        principalSchema: "gccs",
                        principalTable: "spr_manual_submission_receipts",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_spr_manual_submission_receipts_spr_report_packages_tenant_i~",
                        columns: x => new { x.tenant_id, x.package_id },
                        principalSchema: "gccs",
                        principalTable: "spr_report_packages",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_spr_manual_submission_receipts_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_spr_manual_submission_receipts_users_recorded_by_user_id",
                        column: x => x.recorded_by_user_id,
                        principalSchema: "gccs",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_spr_manual_submission_receipts_recorded_by_user_id",
                schema: "gccs",
                table: "spr_manual_submission_receipts",
                column: "recorded_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_spr_manual_submission_receipts_tenant_id_evidence_item_id",
                schema: "gccs",
                table: "spr_manual_submission_receipts",
                columns: new[] { "tenant_id", "evidence_item_id" });

            migrationBuilder.CreateIndex(
                name: "IX_spr_manual_submission_receipts_tenant_id_package_id_recorde~",
                schema: "gccs",
                table: "spr_manual_submission_receipts",
                columns: new[] { "tenant_id", "package_id", "recorded_at" });

            migrationBuilder.CreateIndex(
                name: "IX_spr_manual_submission_receipts_tenant_id_supersedes_receipt~",
                schema: "gccs",
                table: "spr_manual_submission_receipts",
                columns: new[] { "tenant_id", "supersedes_receipt_id" });

            migrationBuilder.CreateIndex(
                name: "IX_spr_report_packages_created_at_updated_at",
                schema: "gccs",
                table: "spr_report_packages",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_spr_report_packages_reviewer_user_id",
                schema: "gccs",
                table: "spr_report_packages",
                column: "reviewer_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_spr_report_packages_tenant_id_contract_id_report_type_perio~",
                schema: "gccs",
                table: "spr_report_packages",
                columns: new[] { "tenant_id", "contract_id", "report_type", "period_start", "period_end", "version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "spr_manual_submission_receipts",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "spr_report_packages",
                schema: "gccs");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDurableSspExportPackages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ssp_export_packages",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_name = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    generated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    package_version = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    system_boundary = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    reviewer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    format = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    disclaimer = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    human_readable_report = table.Column<string>(type: "text", nullable: false),
                    machine_readable_metadata = table.Column<string>(type: "jsonb", nullable: false),
                    sections_json = table.Column<string>(type: "jsonb", nullable: false),
                    evidence_references_json = table.Column<string>(type: "jsonb", nullable: false),
                    poam_references_json = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    external_share_approved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    external_share_approved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    external_share_approval_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    shared_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    shared_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    shared_recipient = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    shared_purpose = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ssp_export_packages", x => x.id);
                    table.UniqueConstraint("AK_ssp_export_packages_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "FK_ssp_export_packages_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ssp_export_package_history",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    package_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_name = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ssp_export_package_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_ssp_export_package_history_ssp_export_packages_tenant_id_pa~",
                        columns: x => new { x.tenant_id, x.package_id },
                        principalSchema: "gccs",
                        principalTable: "ssp_export_packages",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ssp_export_package_history_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ssp_export_package_history_tenant_id_package_id_occurred_at",
                schema: "gccs",
                table: "ssp_export_package_history",
                columns: new[] { "tenant_id", "package_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_ssp_export_packages_created_at_updated_at",
                schema: "gccs",
                table: "ssp_export_packages",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_ssp_export_packages_tenant_id_generated_at",
                schema: "gccs",
                table: "ssp_export_packages",
                columns: new[] { "tenant_id", "generated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_ssp_export_packages_tenant_id_package_version",
                schema: "gccs",
                table: "ssp_export_packages",
                columns: new[] { "tenant_id", "package_version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ssp_export_package_history",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "ssp_export_packages",
                schema: "gccs");
        }
    }
}

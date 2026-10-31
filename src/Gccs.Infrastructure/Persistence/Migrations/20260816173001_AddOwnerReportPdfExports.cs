using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOwnerReportPdfExports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_reports_tenant_id_id",
                schema: "gccs",
                table: "reports",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.CreateTable(
                name: "report_exports",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_id = table.Column<Guid>(type: "uuid", nullable: false),
                    format = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    render_version = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    object_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    file_name = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    content_length = table.Column<long>(type: "bigint", nullable: true),
                    e_tag = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    requested_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    processing_attempt_count = table.Column<int>(type: "integer", nullable: false),
                    processing_lease_id = table.Column<Guid>(type: "uuid", nullable: true),
                    processing_lease_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    failure_code = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_exports", x => x.id);
                    table.ForeignKey(
                        name: "FK_report_exports_reports_tenant_id_report_id",
                        columns: x => new { x.tenant_id, x.report_id },
                        principalSchema: "gccs",
                        principalTable: "reports",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_report_exports_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_report_exports_created_at_updated_at",
                schema: "gccs",
                table: "report_exports",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_report_exports_status_requested_at",
                schema: "gccs",
                table: "report_exports",
                columns: new[] { "status", "requested_at" });

            migrationBuilder.CreateIndex(
                name: "IX_report_exports_tenant_id_report_id_format_render_version",
                schema: "gccs",
                table: "report_exports",
                columns: new[] { "tenant_id", "report_id", "format", "render_version" },
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO gccs.role_permissions (role_id, permission)
                SELECT id, 'ExportReports'
                FROM gccs.roles
                WHERE name = 'Owner'
                ON CONFLICT (role_id, permission) DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM gccs.role_permissions
                WHERE permission = 'ExportReports'
                  AND role_id IN (
                      SELECT id
                      FROM gccs.roles
                      WHERE name = 'Owner'
                  );
                """);

            migrationBuilder.DropTable(
                name: "report_exports",
                schema: "gccs");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_reports_tenant_id_id",
                schema: "gccs",
                table: "reports");
        }
    }
}

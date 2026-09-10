using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenSspExportReviewPackages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "language_policy_version",
                schema: "gccs",
                table: "ssp_export_packages",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE gccs.ssp_export_packages SET language_policy_version = 'legacy-29.3.0' WHERE language_policy_version IS NULL;");

            migrationBuilder.AlterColumn<string>(
                name: "language_policy_version",
                schema: "gccs",
                table: "ssp_export_packages",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "ssp_export_policies",
                schema: "gccs",
                columns: table => new
                {
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    require_independent_approval = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ssp_export_policies", x => x.tenant_id);
                    table.ForeignKey(
                        name: "FK_ssp_export_policies_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ssp_export_policies_created_at_updated_at",
                schema: "gccs",
                table: "ssp_export_policies",
                columns: new[] { "created_at", "updated_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ssp_export_policies",
                schema: "gccs");

            migrationBuilder.DropColumn(
                name: "language_policy_version",
                schema: "gccs",
                table: "ssp_export_packages");
        }
    }
}

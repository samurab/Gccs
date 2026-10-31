using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNoCuiAcknowledgements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_audit_log_entries_tenants_tenant_id",
                schema: "gccs",
                table: "audit_log_entries");

            migrationBuilder.CreateTable(
                name: "no_cui_acknowledgements",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    notice_version = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    notice_copy = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    acknowledged_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_no_cui_acknowledgements", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_no_cui_acknowledgements_created_at_updated_at",
                schema: "gccs",
                table: "no_cui_acknowledgements",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_no_cui_acknowledgements_tenant_id_user_id_notice_version",
                schema: "gccs",
                table: "no_cui_acknowledgements",
                columns: new[] { "tenant_id", "user_id", "notice_version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "no_cui_acknowledgements",
                schema: "gccs");

            migrationBuilder.AddForeignKey(
                name: "FK_audit_log_entries_tenants_tenant_id",
                schema: "gccs",
                table: "audit_log_entries",
                column: "tenant_id",
                principalSchema: "gccs",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

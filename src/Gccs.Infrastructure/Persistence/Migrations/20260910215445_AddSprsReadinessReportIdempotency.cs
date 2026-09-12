using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSprsReadinessReportIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "idempotency_key",
                schema: "gccs",
                table: "reports",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "request_fingerprint",
                schema: "gccs",
                table: "reports",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_reports_tenant_id_type_idempotency_key",
                schema: "gccs",
                table: "reports",
                columns: new[] { "tenant_id", "type", "idempotency_key" },
                unique: true,
                filter: "idempotency_key IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_reports_tenant_id_type_idempotency_key",
                schema: "gccs",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "idempotency_key",
                schema: "gccs",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "request_fingerprint",
                schema: "gccs",
                table: "reports");
        }
    }
}

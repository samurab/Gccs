using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEvidenceRequestDashboardPriority : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "priority",
                schema: "gccs",
                table: "evidence_requests",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_evidence_requests_tenant_id_priority",
                schema: "gccs",
                table: "evidence_requests",
                columns: new[] { "tenant_id", "priority" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_evidence_requests_tenant_id_priority",
                schema: "gccs",
                table: "evidence_requests");

            migrationBuilder.DropColumn(
                name: "priority",
                schema: "gccs",
                table: "evidence_requests");
        }
    }
}

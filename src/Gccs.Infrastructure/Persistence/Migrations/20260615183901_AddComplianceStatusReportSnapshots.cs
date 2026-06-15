using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddComplianceStatusReportSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "export_html",
                schema: "gccs",
                table: "reports",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "snapshot_json",
                schema: "gccs",
                table: "reports",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "export_html",
                schema: "gccs",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "snapshot_json",
                schema: "gccs",
                table: "reports");
        }
    }
}

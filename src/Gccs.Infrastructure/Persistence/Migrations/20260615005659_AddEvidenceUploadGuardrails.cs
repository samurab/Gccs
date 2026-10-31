using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEvidenceUploadGuardrails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "content_type",
                schema: "gccs",
                table: "evidence_items",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "malware_scan_status",
                schema: "gccs",
                table: "evidence_items",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "original_file_name",
                schema: "gccs",
                table: "evidence_items",
                type: "character varying(240)",
                maxLength: 240,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "size_bytes",
                schema: "gccs",
                table: "evidence_items",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "upload_validation_status",
                schema: "gccs",
                table: "evidence_items",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "content_type",
                schema: "gccs",
                table: "evidence_items");

            migrationBuilder.DropColumn(
                name: "malware_scan_status",
                schema: "gccs",
                table: "evidence_items");

            migrationBuilder.DropColumn(
                name: "original_file_name",
                schema: "gccs",
                table: "evidence_items");

            migrationBuilder.DropColumn(
                name: "size_bytes",
                schema: "gccs",
                table: "evidence_items");

            migrationBuilder.DropColumn(
                name: "upload_validation_status",
                schema: "gccs",
                table: "evidence_items");
        }
    }
}

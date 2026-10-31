using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContractDocumentUploadMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "content_type",
                schema: "gccs",
                table: "contract_documents",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "malware_scan_status",
                schema: "gccs",
                table: "contract_documents",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "notice_version",
                schema: "gccs",
                table: "contract_documents",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "size_bytes",
                schema: "gccs",
                table: "contract_documents",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "validation_status",
                schema: "gccs",
                table: "contract_documents",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "content_type",
                schema: "gccs",
                table: "contract_documents");

            migrationBuilder.DropColumn(
                name: "malware_scan_status",
                schema: "gccs",
                table: "contract_documents");

            migrationBuilder.DropColumn(
                name: "notice_version",
                schema: "gccs",
                table: "contract_documents");

            migrationBuilder.DropColumn(
                name: "size_bytes",
                schema: "gccs",
                table: "contract_documents");

            migrationBuilder.DropColumn(
                name: "validation_status",
                schema: "gccs",
                table: "contract_documents");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExpandedSubcontractorProfileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "certifications_json",
                schema: "gccs",
                table: "subcontractors",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "naics_codes_json",
                schema: "gccs",
                table: "subcontractors",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "owner_function",
                schema: "gccs",
                table: "subcontractors",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "certifications_json",
                schema: "gccs",
                table: "subcontractors");

            migrationBuilder.DropColumn(
                name: "naics_codes_json",
                schema: "gccs",
                table: "subcontractors");

            migrationBuilder.DropColumn(
                name: "owner_function",
                schema: "gccs",
                table: "subcontractors");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCmmcResponsibilityMatrix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "owner_function",
                schema: "gccs",
                table: "control_assessments",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "responsibility_notes",
                schema: "gccs",
                table: "control_assessments",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "responsibility_provider",
                schema: "gccs",
                table: "control_assessments",
                type: "character varying(240)",
                maxLength: 240,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "responsibility_type",
                schema: "gccs",
                table: "control_assessments",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "owner_function",
                schema: "gccs",
                table: "control_assessments");

            migrationBuilder.DropColumn(
                name: "responsibility_notes",
                schema: "gccs",
                table: "control_assessments");

            migrationBuilder.DropColumn(
                name: "responsibility_provider",
                schema: "gccs",
                table: "control_assessments");

            migrationBuilder.DropColumn(
                name: "responsibility_type",
                schema: "gccs",
                table: "control_assessments");
        }
    }
}

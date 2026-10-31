using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCmmcControlReadinessLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "asset_ids_json",
                schema: "gccs",
                table: "control_assessments",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "poam_item_ids_json",
                schema: "gccs",
                table: "control_assessments",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "task_ids_json",
                schema: "gccs",
                table: "control_assessments",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "asset_ids_json",
                schema: "gccs",
                table: "control_assessments");

            migrationBuilder.DropColumn(
                name: "poam_item_ids_json",
                schema: "gccs",
                table: "control_assessments");

            migrationBuilder.DropColumn(
                name: "task_ids_json",
                schema: "gccs",
                table: "control_assessments");
        }
    }
}

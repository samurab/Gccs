using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContractRecordFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "place_of_performance",
                schema: "gccs",
                table: "contracts",
                type: "character varying(240)",
                maxLength: 240,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "agency_or_prime_name",
                schema: "gccs",
                table: "contracts",
                type: "character varying(240)",
                maxLength: 240,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "data_handling_posture",
                schema: "gccs",
                table: "contracts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "description",
                schema: "gccs",
                table: "contracts",
                type: "character varying(1200)",
                maxLength: 1200,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "data_handling_posture",
                schema: "gccs",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "description",
                schema: "gccs",
                table: "contracts");

            migrationBuilder.AlterColumn<string>(
                name: "place_of_performance",
                schema: "gccs",
                table: "contracts",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(240)",
                oldMaxLength: 240);

            migrationBuilder.AlterColumn<string>(
                name: "agency_or_prime_name",
                schema: "gccs",
                table: "contracts",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(240)",
                oldMaxLength: 240);
        }
    }
}

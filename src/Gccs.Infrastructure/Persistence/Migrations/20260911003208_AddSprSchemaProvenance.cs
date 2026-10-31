using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSprSchemaProvenance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "spr_schema_definition_sha256",
                schema: "gccs",
                table: "esrs_report_data_rows",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "spr_schema_profile_id",
                schema: "gccs",
                table: "esrs_report_data_rows",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "spr_schema_source_url",
                schema: "gccs",
                table: "esrs_report_data_rows",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "spr_schema_version",
                schema: "gccs",
                table: "esrs_report_data_rows",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_esrs_report_data_rows_spr_schema",
                schema: "gccs",
                table: "esrs_report_data_rows",
                sql: "spr_schema_profile_id IS NULL OR (spr_schema_version IS NOT NULL AND spr_schema_source_url IS NOT NULL AND length(spr_schema_definition_sha256) = 64)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_esrs_report_data_rows_spr_schema",
                schema: "gccs",
                table: "esrs_report_data_rows");

            migrationBuilder.DropColumn(
                name: "spr_schema_definition_sha256",
                schema: "gccs",
                table: "esrs_report_data_rows");

            migrationBuilder.DropColumn(
                name: "spr_schema_profile_id",
                schema: "gccs",
                table: "esrs_report_data_rows");

            migrationBuilder.DropColumn(
                name: "spr_schema_source_url",
                schema: "gccs",
                table: "esrs_report_data_rows");

            migrationBuilder.DropColumn(
                name: "spr_schema_version",
                schema: "gccs",
                table: "esrs_report_data_rows");
        }
    }
}

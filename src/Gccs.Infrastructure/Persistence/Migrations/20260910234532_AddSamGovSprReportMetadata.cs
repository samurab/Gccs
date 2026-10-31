using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSamGovSprReportMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "prime_contract_piid",
                schema: "gccs",
                table: "esrs_report_data_rows",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reporting_entity_uei",
                schema: "gccs",
                table: "esrs_report_data_rows",
                type: "character varying(12)",
                maxLength: 12,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "reporting_fiscal_year",
                schema: "gccs",
                table: "esrs_report_data_rows",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reporting_period",
                schema: "gccs",
                table: "esrs_report_data_rows",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reporting_role",
                schema: "gccs",
                table: "esrs_report_data_rows",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "spr_eligibility_basis",
                schema: "gccs",
                table: "esrs_report_data_rows",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "spr_eligibility_confirmed",
                schema: "gccs",
                table: "esrs_report_data_rows",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "subcontract_number",
                schema: "gccs",
                table: "esrs_report_data_rows",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_esrs_report_data_rows_spr_readiness",
                schema: "gccs",
                table: "esrs_report_data_rows",
                sql: "spr_eligibility_confirmed = FALSE OR (reporting_role IS NOT NULL AND reporting_fiscal_year IS NOT NULL AND reporting_period IS NOT NULL AND reporting_entity_uei IS NOT NULL AND prime_contract_piid IS NOT NULL AND spr_eligibility_basis IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_esrs_report_data_rows_spr_uei",
                schema: "gccs",
                table: "esrs_report_data_rows",
                sql: "reporting_entity_uei IS NULL OR (length(reporting_entity_uei) = 12 AND reporting_entity_uei ~ '^[A-Z0-9]{12}$')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_esrs_report_data_rows_spr_readiness",
                schema: "gccs",
                table: "esrs_report_data_rows");

            migrationBuilder.DropCheckConstraint(
                name: "CK_esrs_report_data_rows_spr_uei",
                schema: "gccs",
                table: "esrs_report_data_rows");

            migrationBuilder.DropColumn(
                name: "prime_contract_piid",
                schema: "gccs",
                table: "esrs_report_data_rows");

            migrationBuilder.DropColumn(
                name: "reporting_entity_uei",
                schema: "gccs",
                table: "esrs_report_data_rows");

            migrationBuilder.DropColumn(
                name: "reporting_fiscal_year",
                schema: "gccs",
                table: "esrs_report_data_rows");

            migrationBuilder.DropColumn(
                name: "reporting_period",
                schema: "gccs",
                table: "esrs_report_data_rows");

            migrationBuilder.DropColumn(
                name: "reporting_role",
                schema: "gccs",
                table: "esrs_report_data_rows");

            migrationBuilder.DropColumn(
                name: "spr_eligibility_basis",
                schema: "gccs",
                table: "esrs_report_data_rows");

            migrationBuilder.DropColumn(
                name: "spr_eligibility_confirmed",
                schema: "gccs",
                table: "esrs_report_data_rows");

            migrationBuilder.DropColumn(
                name: "subcontract_number",
                schema: "gccs",
                table: "esrs_report_data_rows");
        }
    }
}

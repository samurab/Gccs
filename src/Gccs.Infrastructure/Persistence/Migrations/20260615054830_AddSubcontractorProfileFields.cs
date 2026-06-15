using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSubcontractorProfileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cmmc_status",
                schema: "gccs",
                table: "subcontractors",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "Unknown");

            migrationBuilder.AddColumn<bool>(
                name: "has_export_controlled_access",
                schema: "gccs",
                table: "subcontractors",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateOnly>(
                name: "insurance_expires_at",
                schema: "gccs",
                table: "subcontractors",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "nda_status",
                schema: "gccs",
                table: "subcontractors",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "NotOnFile");

            migrationBuilder.AddColumn<string>(
                name: "role_description",
                schema: "gccs",
                table: "subcontractors",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "small_business_status",
                schema: "gccs",
                table: "subcontractors",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "Unknown");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cmmc_status",
                schema: "gccs",
                table: "subcontractors");

            migrationBuilder.DropColumn(
                name: "has_export_controlled_access",
                schema: "gccs",
                table: "subcontractors");

            migrationBuilder.DropColumn(
                name: "insurance_expires_at",
                schema: "gccs",
                table: "subcontractors");

            migrationBuilder.DropColumn(
                name: "nda_status",
                schema: "gccs",
                table: "subcontractors");

            migrationBuilder.DropColumn(
                name: "role_description",
                schema: "gccs",
                table: "subcontractors");

            migrationBuilder.DropColumn(
                name: "small_business_status",
                schema: "gccs",
                table: "subcontractors");
        }
    }
}

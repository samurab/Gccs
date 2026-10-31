using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSubcontractorSamMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "sam_exclusion_status",
                schema: "gccs",
                table: "subcontractors",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sam_naics_json",
                schema: "gccs",
                table: "subcontractors",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<DateOnly>(
                name: "sam_registration_expires_at",
                schema: "gccs",
                table: "subcontractors",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sam_registration_status",
                schema: "gccs",
                table: "subcontractors",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "sam_retrieved_at",
                schema: "gccs",
                table: "subcontractors",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sam_source",
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
                name: "sam_exclusion_status",
                schema: "gccs",
                table: "subcontractors");

            migrationBuilder.DropColumn(
                name: "sam_naics_json",
                schema: "gccs",
                table: "subcontractors");

            migrationBuilder.DropColumn(
                name: "sam_registration_expires_at",
                schema: "gccs",
                table: "subcontractors");

            migrationBuilder.DropColumn(
                name: "sam_registration_status",
                schema: "gccs",
                table: "subcontractors");

            migrationBuilder.DropColumn(
                name: "sam_retrieved_at",
                schema: "gccs",
                table: "subcontractors");

            migrationBuilder.DropColumn(
                name: "sam_source",
                schema: "gccs",
                table: "subcontractors");
        }
    }
}

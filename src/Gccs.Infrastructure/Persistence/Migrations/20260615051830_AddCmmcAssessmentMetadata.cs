using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCmmcAssessmentMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "company_profile_id",
                schema: "gccs",
                table: "assessments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "contract_ids_json",
                schema: "gccs",
                table: "assessments",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "framework",
                schema: "gccs",
                table: "assessments",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "CMMC");

            migrationBuilder.AddColumn<string>(
                name: "name",
                schema: "gccs",
                table: "assessments",
                type: "character varying(240)",
                maxLength: 240,
                nullable: false,
                defaultValue: "CMMC readiness assessment");

            migrationBuilder.AddColumn<string>(
                name: "owner_function",
                schema: "gccs",
                table: "assessments",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "Compliance");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "company_profile_id",
                schema: "gccs",
                table: "assessments");

            migrationBuilder.DropColumn(
                name: "contract_ids_json",
                schema: "gccs",
                table: "assessments");

            migrationBuilder.DropColumn(
                name: "framework",
                schema: "gccs",
                table: "assessments");

            migrationBuilder.DropColumn(
                name: "name",
                schema: "gccs",
                table: "assessments");

            migrationBuilder.DropColumn(
                name: "owner_function",
                schema: "gccs",
                table: "assessments");
        }
    }
}

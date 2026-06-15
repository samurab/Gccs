using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCmmcPoamRemediationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "assessment_id",
                schema: "gccs",
                table: "poam_items",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "owner_function",
                schema: "gccs",
                table: "poam_items",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "Security");

            migrationBuilder.AddColumn<Guid>(
                name: "remediation_task_id",
                schema: "gccs",
                table: "poam_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_poam_items_assessment_id_control_id",
                schema: "gccs",
                table: "poam_items",
                columns: new[] { "assessment_id", "control_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_poam_items_assessment_id_control_id",
                schema: "gccs",
                table: "poam_items");

            migrationBuilder.DropColumn(
                name: "assessment_id",
                schema: "gccs",
                table: "poam_items");

            migrationBuilder.DropColumn(
                name: "owner_function",
                schema: "gccs",
                table: "poam_items");

            migrationBuilder.DropColumn(
                name: "remediation_task_id",
                schema: "gccs",
                table: "poam_items");
        }
    }
}

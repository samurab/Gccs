using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCmmcControlAssessmentDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "esp_name",
                schema: "gccs",
                table: "control_assessments",
                type: "character varying(240)",
                maxLength: 240,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "esp_responsible",
                schema: "gccs",
                table: "control_assessments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "implementation_details",
                schema: "gccs",
                table: "control_assessments",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "inherited_from",
                schema: "gccs",
                table: "control_assessments",
                type: "character varying(240)",
                maxLength: 240,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_inherited",
                schema: "gccs",
                table: "control_assessments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "control_assessment_history",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    assessment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    control_id = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    result = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    changed_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_control_assessment_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_control_assessment_history_control_assessments_assessment_i~",
                        columns: x => new { x.assessment_id, x.control_id },
                        principalSchema: "gccs",
                        principalTable: "control_assessments",
                        principalColumns: new[] { "assessment_id", "control_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_control_assessment_history_assessment_id_control_id_changed~",
                schema: "gccs",
                table: "control_assessment_history",
                columns: new[] { "assessment_id", "control_id", "changed_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "control_assessment_history",
                schema: "gccs");

            migrationBuilder.DropColumn(
                name: "esp_name",
                schema: "gccs",
                table: "control_assessments");

            migrationBuilder.DropColumn(
                name: "esp_responsible",
                schema: "gccs",
                table: "control_assessments");

            migrationBuilder.DropColumn(
                name: "implementation_details",
                schema: "gccs",
                table: "control_assessments");

            migrationBuilder.DropColumn(
                name: "inherited_from",
                schema: "gccs",
                table: "control_assessments");

            migrationBuilder.DropColumn(
                name: "is_inherited",
                schema: "gccs",
                table: "control_assessments");
        }
    }
}

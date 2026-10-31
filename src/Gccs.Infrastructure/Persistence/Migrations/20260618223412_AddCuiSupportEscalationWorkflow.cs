using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCuiSupportEscalationWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "status_changed_at",
                schema: "gccs",
                table: "cui_support_escalations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "status_changed_by_user_id",
                schema: "gccs",
                table: "cui_support_escalations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status_note",
                schema: "gccs",
                table: "cui_support_escalations",
                type: "character varying(1200)",
                maxLength: 1200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "cui_support_escalation_resolutions",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    escalation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    resolution_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    summary = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: false),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    resolved_by_user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cui_support_escalation_resolutions", x => x.id);
                    table.ForeignKey(
                        name: "FK_cui_support_escalation_resolutions_cui_support_escalations_~",
                        column: x => x.escalation_id,
                        principalSchema: "gccs",
                        principalTable: "cui_support_escalations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cui_support_escalation_resolutions_escalation_id_resolved_at",
                schema: "gccs",
                table: "cui_support_escalation_resolutions",
                columns: new[] { "escalation_id", "resolved_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cui_support_escalation_resolutions",
                schema: "gccs");

            migrationBuilder.DropColumn(
                name: "status_changed_at",
                schema: "gccs",
                table: "cui_support_escalations");

            migrationBuilder.DropColumn(
                name: "status_changed_by_user_id",
                schema: "gccs",
                table: "cui_support_escalations");

            migrationBuilder.DropColumn(
                name: "status_note",
                schema: "gccs",
                table: "cui_support_escalations");
        }
    }
}

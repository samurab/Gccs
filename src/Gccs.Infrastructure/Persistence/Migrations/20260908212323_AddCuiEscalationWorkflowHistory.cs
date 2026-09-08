using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCuiEscalationWorkflowHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "sla_due_at",
                schema: "gccs",
                table: "cui_support_escalations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "cui_support_escalation_events",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    escalation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    note = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cui_support_escalation_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_cui_support_escalation_events_cui_support_escalations_escal~",
                        column: x => x.escalation_id,
                        principalSchema: "gccs",
                        principalTable: "cui_support_escalations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cui_support_escalation_events_escalation_id_occurred_at",
                schema: "gccs",
                table: "cui_support_escalation_events",
                columns: new[] { "escalation_id", "occurred_at" });

            migrationBuilder.Sql("""
                UPDATE gccs.cui_support_escalations
                SET sla_due_at = created_at + CASE severity
                    WHEN 'Critical' THEN INTERVAL '1 hour'
                    WHEN 'High' THEN INTERVAL '4 hours'
                    WHEN 'Medium' THEN INTERVAL '24 hours'
                    ELSE INTERVAL '72 hours' END;

                INSERT INTO gccs.cui_support_escalation_events
                    (id, escalation_id, status, note, occurred_at, actor_user_id)
                SELECT gen_random_uuid(), id, 'Submitted', 'Escalation submitted (migrated history baseline).', created_at,
                       COALESCE(created_by_user_id, '00000000-0000-0000-0000-000000000000'::uuid)
                FROM gccs.cui_support_escalations;

                INSERT INTO gccs.cui_support_escalation_events
                    (id, escalation_id, status, note, occurred_at, actor_user_id)
                SELECT gen_random_uuid(), id, status, COALESCE(status_note, 'Legacy workflow transition.'),
                       COALESCE(status_changed_at, updated_at, created_at),
                       COALESCE(status_changed_by_user_id, updated_by_user_id, created_by_user_id, '00000000-0000-0000-0000-000000000000'::uuid)
                FROM gccs.cui_support_escalations
                WHERE status <> 'Submitted';
                """);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "sla_due_at", schema: "gccs", table: "cui_support_escalations",
                type: "timestamp with time zone", nullable: false, oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone", oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cui_support_escalation_events",
                schema: "gccs");

            migrationBuilder.DropColumn(
                name: "sla_due_at",
                schema: "gccs",
                table: "cui_support_escalations");
        }
    }
}

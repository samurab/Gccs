using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDemoAppointmentConfirmations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "demo_appointment_event_id",
                schema: "gccs",
                table: "demo_request_deliveries",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "demo_appointments",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    demo_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    confirmed_start_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    confirmed_end_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    confirmed_time_zone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    host_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    meeting_method = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    meeting_join_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    confirmed_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    confirmed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_demo_appointments", x => x.id);
                    table.CheckConstraint("ck_demo_appointments_duration", "duration_minutes = 30");
                    table.CheckConstraint("ck_demo_appointments_time_range", "confirmed_end_at > confirmed_start_at");
                    table.ForeignKey(
                        name: "FK_demo_appointments_demo_requests_demo_request_id",
                        column: x => x.demo_request_id,
                        principalSchema: "gccs",
                        principalTable: "demo_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "demo_appointment_events",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    demo_appointment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    demo_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    previous_status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    new_status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    confirmed_start_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    confirmed_end_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    confirmed_time_zone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    host_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    meeting_method = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    meeting_join_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ip_address = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_demo_appointment_events", x => x.id);
                    table.CheckConstraint("ck_demo_appointment_events_duration", "duration_minutes = 30");
                    table.CheckConstraint("ck_demo_appointment_events_time_range", "confirmed_end_at > confirmed_start_at");
                    table.ForeignKey(
                        name: "FK_demo_appointment_events_demo_appointments_demo_appointment_~",
                        column: x => x.demo_appointment_id,
                        principalSchema: "gccs",
                        principalTable: "demo_appointments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_demo_appointment_events_demo_requests_demo_request_id",
                        column: x => x.demo_request_id,
                        principalSchema: "gccs",
                        principalTable: "demo_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_demo_request_deliveries_demo_appointment_event_id",
                schema: "gccs",
                table: "demo_request_deliveries",
                column: "demo_appointment_event_id");

            migrationBuilder.CreateIndex(
                name: "IX_demo_appointment_events_demo_appointment_id_occurred_at",
                schema: "gccs",
                table: "demo_appointment_events",
                columns: new[] { "demo_appointment_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_demo_appointment_events_demo_request_id_occurred_at",
                schema: "gccs",
                table: "demo_appointment_events",
                columns: new[] { "demo_request_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_demo_appointments_demo_request_id",
                schema: "gccs",
                table: "demo_appointments",
                column: "demo_request_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_demo_appointments_host_user_id_status_confirmed_start_at_co~",
                schema: "gccs",
                table: "demo_appointments",
                columns: new[] { "host_user_id", "status", "confirmed_start_at", "confirmed_end_at" });

            migrationBuilder.AddForeignKey(
                name: "FK_demo_request_deliveries_demo_appointment_events_demo_appoin~",
                schema: "gccs",
                table: "demo_request_deliveries",
                column: "demo_appointment_event_id",
                principalSchema: "gccs",
                principalTable: "demo_appointment_events",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_demo_request_deliveries_demo_appointment_events_demo_appoin~",
                schema: "gccs",
                table: "demo_request_deliveries");

            migrationBuilder.DropTable(
                name: "demo_appointment_events",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "demo_appointments",
                schema: "gccs");

            migrationBuilder.DropIndex(
                name: "IX_demo_request_deliveries_demo_appointment_event_id",
                schema: "gccs",
                table: "demo_request_deliveries");

            migrationBuilder.DropColumn(
                name: "demo_appointment_event_id",
                schema: "gccs",
                table: "demo_request_deliveries");
        }
    }
}

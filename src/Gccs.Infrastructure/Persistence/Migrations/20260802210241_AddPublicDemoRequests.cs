using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicDemoRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "demo_requests",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    phone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    company = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    referral_source = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    employee_count = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    consent_notice_version = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    deduplication_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    received_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_demo_requests", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "demo_request_deliveries",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    demo_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    next_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lease_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    provider_message_id = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    failure_code = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_demo_request_deliveries", x => x.id);
                    table.ForeignKey(
                        name: "FK_demo_request_deliveries_demo_requests_demo_request_id",
                        column: x => x.demo_request_id,
                        principalSchema: "gccs",
                        principalTable: "demo_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_demo_request_deliveries_demo_request_id",
                schema: "gccs",
                table: "demo_request_deliveries",
                column: "demo_request_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_demo_request_deliveries_status_next_attempt_at_lease_until",
                schema: "gccs",
                table: "demo_request_deliveries",
                columns: new[] { "status", "next_attempt_at", "lease_until" });

            migrationBuilder.CreateIndex(
                name: "IX_demo_requests_deduplication_key",
                schema: "gccs",
                table: "demo_requests",
                column: "deduplication_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_demo_requests_received_at",
                schema: "gccs",
                table: "demo_requests",
                column: "received_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "demo_request_deliveries",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "demo_requests",
                schema: "gccs");
        }
    }
}

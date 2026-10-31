using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDemoFollowUpResponses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "demo_follow_up_request_id",
                schema: "gccs",
                table: "demo_request_deliveries",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "demo_follow_up_requests",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    demo_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    template_version = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    no_cui_notice_version = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    requested_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    responded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_demo_follow_up_requests", x => x.id);
                    table.UniqueConstraint("AK_demo_follow_up_requests_id_demo_request_id", x => new { x.id, x.demo_request_id });
                    table.ForeignKey(
                        name: "FK_demo_follow_up_requests_demo_requests_demo_request_id",
                        column: x => x.demo_request_id,
                        principalSchema: "gccs",
                        principalTable: "demo_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "demo_follow_up_responses",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    demo_follow_up_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    demo_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workflows_json = table.Column<string>(type: "jsonb", nullable: false),
                    other_workflow = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    goals = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    challenges = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    current_process = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    additional_context = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    no_cui_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    no_cui_notice_version = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    submitted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ip_address = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_demo_follow_up_responses", x => x.id);
                    table.CheckConstraint("ck_demo_follow_up_responses_no_cui", "no_cui_confirmed = TRUE");
                    table.ForeignKey(
                        name: "FK_demo_follow_up_responses_demo_follow_up_requests_demo_follo~",
                        columns: x => new { x.demo_follow_up_request_id, x.demo_request_id },
                        principalSchema: "gccs",
                        principalTable: "demo_follow_up_requests",
                        principalColumns: new[] { "id", "demo_request_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_demo_follow_up_responses_demo_requests_demo_request_id",
                        column: x => x.demo_request_id,
                        principalSchema: "gccs",
                        principalTable: "demo_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_demo_request_deliveries_demo_follow_up_request_id_demo_requ~",
                schema: "gccs",
                table: "demo_request_deliveries",
                columns: new[] { "demo_follow_up_request_id", "demo_request_id" });

            migrationBuilder.CreateIndex(
                name: "IX_demo_follow_up_requests_demo_request_id_requested_at",
                schema: "gccs",
                table: "demo_follow_up_requests",
                columns: new[] { "demo_request_id", "requested_at" });

            migrationBuilder.CreateIndex(
                name: "IX_demo_follow_up_requests_demo_request_id_status",
                schema: "gccs",
                table: "demo_follow_up_requests",
                columns: new[] { "demo_request_id", "status" },
                unique: true,
                filter: "status = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_demo_follow_up_requests_token_hash",
                schema: "gccs",
                table: "demo_follow_up_requests",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_demo_follow_up_responses_demo_follow_up_request_id",
                schema: "gccs",
                table: "demo_follow_up_responses",
                column: "demo_follow_up_request_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_demo_follow_up_responses_demo_follow_up_request_id_demo_req~",
                schema: "gccs",
                table: "demo_follow_up_responses",
                columns: new[] { "demo_follow_up_request_id", "demo_request_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_demo_follow_up_responses_demo_request_id_submitted_at",
                schema: "gccs",
                table: "demo_follow_up_responses",
                columns: new[] { "demo_request_id", "submitted_at" });

            migrationBuilder.AddForeignKey(
                name: "FK_demo_request_deliveries_demo_follow_up_requests_demo_follow~",
                schema: "gccs",
                table: "demo_request_deliveries",
                columns: new[] { "demo_follow_up_request_id", "demo_request_id" },
                principalSchema: "gccs",
                principalTable: "demo_follow_up_requests",
                principalColumns: new[] { "id", "demo_request_id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_demo_request_deliveries_demo_follow_up_requests_demo_follow~",
                schema: "gccs",
                table: "demo_request_deliveries");

            migrationBuilder.DropTable(
                name: "demo_follow_up_responses",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "demo_follow_up_requests",
                schema: "gccs");

            migrationBuilder.DropIndex(
                name: "IX_demo_request_deliveries_demo_follow_up_request_id_demo_requ~",
                schema: "gccs",
                table: "demo_request_deliveries");

            migrationBuilder.DropColumn(
                name: "demo_follow_up_request_id",
                schema: "gccs",
                table: "demo_request_deliveries");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignmentEmailDeliveries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "assignment_email_deliveries",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    notification_delivery_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipient_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    recipient_display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    link_url = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    next_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lease_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    provider_message_id = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    failure_code = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assignment_email_deliveries", x => x.id);
                    table.ForeignKey(
                        name: "FK_assignment_email_deliveries_notification_deliveries_notific~",
                        column: x => x.notification_delivery_id,
                        principalSchema: "gccs",
                        principalTable: "notification_deliveries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_assignment_email_deliveries_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_assignment_email_deliveries_created_at_updated_at",
                schema: "gccs",
                table: "assignment_email_deliveries",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_assignment_email_deliveries_notification_delivery_id",
                schema: "gccs",
                table: "assignment_email_deliveries",
                column: "notification_delivery_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_assignment_email_deliveries_status_next_attempt_at_lease_un~",
                schema: "gccs",
                table: "assignment_email_deliveries",
                columns: new[] { "status", "next_attempt_at", "lease_until" });

            migrationBuilder.CreateIndex(
                name: "IX_assignment_email_deliveries_tenant_id_user_id",
                schema: "gccs",
                table: "assignment_email_deliveries",
                columns: new[] { "tenant_id", "user_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "assignment_email_deliveries",
                schema: "gccs");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInvitationDeliveryWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tenant_invitations_invitation_token",
                schema: "gccs",
                table: "tenant_invitations");

            migrationBuilder.DropColumn(
                name: "invitation_token",
                schema: "gccs",
                table: "tenant_invitations");

            migrationBuilder.AddColumn<int>(
                name: "delivery_attempt_count",
                schema: "gccs",
                table: "tenant_invitations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "delivery_failure_code",
                schema: "gccs",
                table: "tenant_invitations",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "delivery_lease_until",
                schema: "gccs",
                table: "tenant_invitations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "delivery_provider_message_id",
                schema: "gccs",
                table: "tenant_invitations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "delivery_status",
                schema: "gccs",
                table: "tenant_invitations",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "invitation_token_hash",
                schema: "gccs",
                table: "tenant_invitations",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_delivery_attempt_at",
                schema: "gccs",
                table: "tenant_invitations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "next_delivery_attempt_at",
                schema: "gccs",
                table: "tenant_invitations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE gccs.tenant_invitations
                SET delivery_status = CASE
                        WHEN notification_sent_at IS NOT NULL THEN 'Sent'
                        ELSE 'Queued'
                    END,
                    next_delivery_attempt_at = CASE
                        WHEN notification_sent_at IS NULL AND status = 'Pending' THEN CURRENT_TIMESTAMP
                        ELSE NULL
                    END,
                    notification_placeholder = CASE
                        WHEN notification_sent_at IS NOT NULL THEN 'Owner invitation email was sent.'
                        ELSE 'Owner invitation is queued for delivery.'
                    END;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_invitations_delivery_status_next_delivery_attempt_at~",
                schema: "gccs",
                table: "tenant_invitations",
                columns: new[] { "delivery_status", "next_delivery_attempt_at", "delivery_lease_until" });

            migrationBuilder.CreateIndex(
                name: "IX_tenant_invitations_invitation_token_hash",
                schema: "gccs",
                table: "tenant_invitations",
                column: "invitation_token_hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tenant_invitations_delivery_status_next_delivery_attempt_at~",
                schema: "gccs",
                table: "tenant_invitations");

            migrationBuilder.DropIndex(
                name: "IX_tenant_invitations_invitation_token_hash",
                schema: "gccs",
                table: "tenant_invitations");

            migrationBuilder.DropColumn(
                name: "delivery_attempt_count",
                schema: "gccs",
                table: "tenant_invitations");

            migrationBuilder.DropColumn(
                name: "delivery_failure_code",
                schema: "gccs",
                table: "tenant_invitations");

            migrationBuilder.DropColumn(
                name: "delivery_lease_until",
                schema: "gccs",
                table: "tenant_invitations");

            migrationBuilder.DropColumn(
                name: "delivery_provider_message_id",
                schema: "gccs",
                table: "tenant_invitations");

            migrationBuilder.DropColumn(
                name: "delivery_status",
                schema: "gccs",
                table: "tenant_invitations");

            migrationBuilder.DropColumn(
                name: "invitation_token_hash",
                schema: "gccs",
                table: "tenant_invitations");

            migrationBuilder.DropColumn(
                name: "last_delivery_attempt_at",
                schema: "gccs",
                table: "tenant_invitations");

            migrationBuilder.DropColumn(
                name: "next_delivery_attempt_at",
                schema: "gccs",
                table: "tenant_invitations");

            migrationBuilder.AddColumn<string>(
                name: "invitation_token",
                schema: "gccs",
                table: "tenant_invitations",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_invitations_invitation_token",
                schema: "gccs",
                table: "tenant_invitations",
                column: "invitation_token",
                unique: true);
        }
    }
}

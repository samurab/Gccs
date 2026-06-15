using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignmentNotificationMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_notification_deliveries_tenant_id_source_task_id_category",
                schema: "gccs",
                table: "notification_deliveries");

            migrationBuilder.AddColumn<string>(
                name: "link_url",
                schema: "gccs",
                table: "notification_deliveries",
                type: "character varying(400)",
                maxLength: 400,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "read_at",
                schema: "gccs",
                table: "notification_deliveries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source_type",
                schema: "gccs",
                table: "notification_deliveries",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_notification_deliveries_tenant_id_source_task_id_category_user_id",
                schema: "gccs",
                table: "notification_deliveries",
                columns: new[] { "tenant_id", "source_task_id", "category", "user_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_notification_deliveries_tenant_id_source_task_id_category_user_id",
                schema: "gccs",
                table: "notification_deliveries");

            migrationBuilder.DropColumn(
                name: "link_url",
                schema: "gccs",
                table: "notification_deliveries");

            migrationBuilder.DropColumn(
                name: "read_at",
                schema: "gccs",
                table: "notification_deliveries");

            migrationBuilder.DropColumn(
                name: "source_type",
                schema: "gccs",
                table: "notification_deliveries");

            migrationBuilder.CreateIndex(
                name: "IX_notification_deliveries_tenant_id_source_task_id_category",
                schema: "gccs",
                table: "notification_deliveries",
                columns: new[] { "tenant_id", "source_task_id", "category" },
                unique: true);
        }
    }
}

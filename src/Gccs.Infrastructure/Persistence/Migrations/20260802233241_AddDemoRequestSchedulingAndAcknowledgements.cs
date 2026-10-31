using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDemoRequestSchedulingAndAcknowledgements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_demo_request_deliveries_demo_request_id",
                schema: "gccs",
                table: "demo_request_deliveries");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "preferred_start_at",
                schema: "gccs",
                table: "demo_requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "preferred_time_zone",
                schema: "gccs",
                table: "demo_requests",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "delivery_kind",
                schema: "gccs",
                table: "demo_request_deliveries",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "InternalNotification");

            migrationBuilder.CreateIndex(
                name: "IX_demo_request_deliveries_demo_request_id_delivery_kind",
                schema: "gccs",
                table: "demo_request_deliveries",
                columns: new[] { "demo_request_id", "delivery_kind" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_demo_request_deliveries_demo_request_id_delivery_kind",
                schema: "gccs",
                table: "demo_request_deliveries");

            migrationBuilder.DropColumn(
                name: "preferred_start_at",
                schema: "gccs",
                table: "demo_requests");

            migrationBuilder.DropColumn(
                name: "preferred_time_zone",
                schema: "gccs",
                table: "demo_requests");

            migrationBuilder.DropColumn(
                name: "delivery_kind",
                schema: "gccs",
                table: "demo_request_deliveries");

            migrationBuilder.CreateIndex(
                name: "IX_demo_request_deliveries_demo_request_id",
                schema: "gccs",
                table: "demo_request_deliveries",
                column: "demo_request_id",
                unique: true);
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformTenantCancellation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cancellation_reason",
                schema: "gccs",
                table: "platform_tenant_onboardings",
                type: "character varying(600)",
                maxLength: 600,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "cancelled_at",
                schema: "gccs",
                table: "platform_tenant_onboardings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "cancelled_by_user_id",
                schema: "gccs",
                table: "platform_tenant_onboardings",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cancellation_reason",
                schema: "gccs",
                table: "platform_tenant_onboardings");

            migrationBuilder.DropColumn(
                name: "cancelled_at",
                schema: "gccs",
                table: "platform_tenant_onboardings");

            migrationBuilder.DropColumn(
                name: "cancelled_by_user_id",
                schema: "gccs",
                table: "platform_tenant_onboardings");
        }
    }
}

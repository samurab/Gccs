using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReportArchiveLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "archive_reason",
                schema: "gccs",
                table: "reports",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "archived_at",
                schema: "gccs",
                table: "reports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "archived_by_user_id",
                schema: "gccs",
                table: "reports",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status_before_archive",
                schema: "gccs",
                table: "reports",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "archive_reason",
                schema: "gccs",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "archived_at",
                schema: "gccs",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "archived_by_user_id",
                schema: "gccs",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "status_before_archive",
                schema: "gccs",
                table: "reports");
        }
    }
}

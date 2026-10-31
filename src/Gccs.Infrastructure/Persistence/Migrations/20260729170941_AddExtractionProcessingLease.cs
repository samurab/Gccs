using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExtractionProcessingLease : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_processing_attempt_at",
                schema: "gccs",
                table: "extraction_jobs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "processing_attempt_count",
                schema: "gccs",
                table: "extraction_jobs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "processing_lease_id",
                schema: "gccs",
                table: "extraction_jobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "processing_lease_until",
                schema: "gccs",
                table: "extraction_jobs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_extraction_jobs_status_processing_lease_until_requested_at",
                schema: "gccs",
                table: "extraction_jobs",
                columns: new[] { "status", "processing_lease_until", "requested_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_extraction_jobs_status_processing_lease_until_requested_at",
                schema: "gccs",
                table: "extraction_jobs");

            migrationBuilder.DropColumn(
                name: "last_processing_attempt_at",
                schema: "gccs",
                table: "extraction_jobs");

            migrationBuilder.DropColumn(
                name: "processing_attempt_count",
                schema: "gccs",
                table: "extraction_jobs");

            migrationBuilder.DropColumn(
                name: "processing_lease_id",
                schema: "gccs",
                table: "extraction_jobs");

            migrationBuilder.DropColumn(
                name: "processing_lease_until",
                schema: "gccs",
                table: "extraction_jobs");
        }
    }
}

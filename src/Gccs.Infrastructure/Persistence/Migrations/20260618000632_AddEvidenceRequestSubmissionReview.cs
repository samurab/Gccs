using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEvidenceRequestSubmissionReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "review_comment",
                schema: "gccs",
                table: "evidence_requests",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "reviewed_at",
                schema: "gccs",
                table: "evidence_requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "submission_comment",
                schema: "gccs",
                table: "evidence_requests",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "submitted_at",
                schema: "gccs",
                table: "evidence_requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "submitted_evidence_item_id",
                schema: "gccs",
                table: "evidence_requests",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "review_comment",
                schema: "gccs",
                table: "evidence_requests");

            migrationBuilder.DropColumn(
                name: "reviewed_at",
                schema: "gccs",
                table: "evidence_requests");

            migrationBuilder.DropColumn(
                name: "submission_comment",
                schema: "gccs",
                table: "evidence_requests");

            migrationBuilder.DropColumn(
                name: "submitted_at",
                schema: "gccs",
                table: "evidence_requests");

            migrationBuilder.DropColumn(
                name: "submitted_evidence_item_id",
                schema: "gccs",
                table: "evidence_requests");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClauseCandidateReviewMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "decision_note",
                schema: "gccs",
                table: "clause_candidates",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "decision_reason",
                schema: "gccs",
                table: "clause_candidates",
                type: "character varying(600)",
                maxLength: 600,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "reviewed_at",
                schema: "gccs",
                table: "clause_candidates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "reviewed_by_user_id",
                schema: "gccs",
                table: "clause_candidates",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "decision_note",
                schema: "gccs",
                table: "clause_candidates");

            migrationBuilder.DropColumn(
                name: "decision_reason",
                schema: "gccs",
                table: "clause_candidates");

            migrationBuilder.DropColumn(
                name: "reviewed_at",
                schema: "gccs",
                table: "clause_candidates");

            migrationBuilder.DropColumn(
                name: "reviewed_by_user_id",
                schema: "gccs",
                table: "clause_candidates");
        }
    }
}

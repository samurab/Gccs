using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddObligationPublicationMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "confidence",
                schema: "gccs",
                table: "obligations",
                type: "text",
                nullable: false,
                defaultValue: "unknown");

            migrationBuilder.AddColumn<DateOnly>(
                name: "last_reviewed_at",
                schema: "gccs",
                table: "obligations",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "next_review_due_at",
                schema: "gccs",
                table: "obligations",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "requires_expert_review",
                schema: "gccs",
                table: "obligations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "requires_flow_down",
                schema: "gccs",
                table: "obligations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "review_state",
                schema: "gccs",
                table: "obligations",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "Draft");

            migrationBuilder.AddColumn<Guid>(
                name: "reviewed_by_user_id",
                schema: "gccs",
                table: "obligations",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "confidence",
                schema: "gccs",
                table: "obligations");

            migrationBuilder.DropColumn(
                name: "last_reviewed_at",
                schema: "gccs",
                table: "obligations");

            migrationBuilder.DropColumn(
                name: "next_review_due_at",
                schema: "gccs",
                table: "obligations");

            migrationBuilder.DropColumn(
                name: "requires_expert_review",
                schema: "gccs",
                table: "obligations");

            migrationBuilder.DropColumn(
                name: "requires_flow_down",
                schema: "gccs",
                table: "obligations");

            migrationBuilder.DropColumn(
                name: "review_state",
                schema: "gccs",
                table: "obligations");

            migrationBuilder.DropColumn(
                name: "reviewed_by_user_id",
                schema: "gccs",
                table: "obligations");
        }
    }
}

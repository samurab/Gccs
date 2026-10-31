using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClauseReviewVersioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "review_state",
                schema: "gccs",
                table: "contract_clauses",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "Draft");

            migrationBuilder.AddColumn<string>(
                name: "source_hash",
                schema: "gccs",
                table: "contract_clauses",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "clause_effective_at",
                schema: "gccs",
                table: "clauses",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "clause_text_version",
                schema: "gccs",
                table: "clauses",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "current");

            migrationBuilder.AddColumn<string>(
                name: "review_state",
                schema: "gccs",
                table: "clauses",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "Draft");

            migrationBuilder.AddColumn<string>(
                name: "source_hash",
                schema: "gccs",
                table: "clauses",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "superseded_at",
                schema: "gccs",
                table: "clauses",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "superseded_by_clause_id",
                schema: "gccs",
                table: "clauses",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "review_state",
                schema: "gccs",
                table: "contract_clauses");

            migrationBuilder.DropColumn(
                name: "source_hash",
                schema: "gccs",
                table: "contract_clauses");

            migrationBuilder.DropColumn(
                name: "clause_effective_at",
                schema: "gccs",
                table: "clauses");

            migrationBuilder.DropColumn(
                name: "clause_text_version",
                schema: "gccs",
                table: "clauses");

            migrationBuilder.DropColumn(
                name: "review_state",
                schema: "gccs",
                table: "clauses");

            migrationBuilder.DropColumn(
                name: "source_hash",
                schema: "gccs",
                table: "clauses");

            migrationBuilder.DropColumn(
                name: "superseded_at",
                schema: "gccs",
                table: "clauses");

            migrationBuilder.DropColumn(
                name: "superseded_by_clause_id",
                schema: "gccs",
                table: "clauses");
        }
    }
}

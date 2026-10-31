using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContractClauseAttachmentWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "attachment_reason",
                schema: "gccs",
                table: "contract_clauses",
                type: "character varying(600)",
                maxLength: 600,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "clause_library_id",
                schema: "gccs",
                table: "contract_clauses",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_at",
                schema: "gccs",
                table: "contract_clauses",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_user_id",
                schema: "gccs",
                table: "contract_clauses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "removal_reason",
                schema: "gccs",
                table: "contract_clauses",
                type: "character varying(600)",
                maxLength: 600,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "removed_at",
                schema: "gccs",
                table: "contract_clauses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "removed_by_user_id",
                schema: "gccs",
                table: "contract_clauses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source_document_reference",
                schema: "gccs",
                table: "contract_clauses",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source_url",
                schema: "gccs",
                table: "contract_clauses",
                type: "character varying(600)",
                maxLength: 600,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                schema: "gccs",
                table: "contract_clauses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_user_id",
                schema: "gccs",
                table: "contract_clauses",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_contract_clauses_contract_id_clause_library_id_removed_at",
                schema: "gccs",
                table: "contract_clauses",
                columns: new[] { "contract_id", "clause_library_id", "removed_at" });

            migrationBuilder.CreateIndex(
                name: "IX_contract_clauses_created_at_updated_at",
                schema: "gccs",
                table: "contract_clauses",
                columns: new[] { "created_at", "updated_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_contract_clauses_contract_id_clause_library_id_removed_at",
                schema: "gccs",
                table: "contract_clauses");

            migrationBuilder.DropIndex(
                name: "IX_contract_clauses_created_at_updated_at",
                schema: "gccs",
                table: "contract_clauses");

            migrationBuilder.DropColumn(
                name: "attachment_reason",
                schema: "gccs",
                table: "contract_clauses");

            migrationBuilder.DropColumn(
                name: "clause_library_id",
                schema: "gccs",
                table: "contract_clauses");

            migrationBuilder.DropColumn(
                name: "created_at",
                schema: "gccs",
                table: "contract_clauses");

            migrationBuilder.DropColumn(
                name: "created_by_user_id",
                schema: "gccs",
                table: "contract_clauses");

            migrationBuilder.DropColumn(
                name: "removal_reason",
                schema: "gccs",
                table: "contract_clauses");

            migrationBuilder.DropColumn(
                name: "removed_at",
                schema: "gccs",
                table: "contract_clauses");

            migrationBuilder.DropColumn(
                name: "removed_by_user_id",
                schema: "gccs",
                table: "contract_clauses");

            migrationBuilder.DropColumn(
                name: "source_document_reference",
                schema: "gccs",
                table: "contract_clauses");

            migrationBuilder.DropColumn(
                name: "source_url",
                schema: "gccs",
                table: "contract_clauses");

            migrationBuilder.DropColumn(
                name: "updated_at",
                schema: "gccs",
                table: "contract_clauses");

            migrationBuilder.DropColumn(
                name: "updated_by_user_id",
                schema: "gccs",
                table: "contract_clauses");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAiRetrievalFullTextSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "search_vector",
                schema: "gccs",
                table: "spr_report_packages",
                type: "tsvector",
                nullable: false)
                .Annotation("Npgsql:TsVectorConfig", "english")
                .Annotation("Npgsql:TsVectorProperties", new[] { "report_type", "not_submitted_disclaimer", "reviewer_name", "review_notes" });

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "search_vector",
                schema: "gccs",
                table: "obligations",
                type: "tsvector",
                nullable: false)
                .Annotation("Npgsql:TsVectorConfig", "english")
                .Annotation("Npgsql:TsVectorProperties", new[] { "id", "title", "source", "source_name", "plain_english_summary", "required_action" });

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "search_vector",
                schema: "gccs",
                table: "evidence_items",
                type: "tsvector",
                nullable: false)
                .Annotation("Npgsql:TsVectorConfig", "english")
                .Annotation("Npgsql:TsVectorProperties", new[] { "name", "description", "owner_function" });

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "search_vector",
                schema: "gccs",
                table: "clause_candidates",
                type: "tsvector",
                nullable: false)
                .Annotation("Npgsql:TsVectorConfig", "english")
                .Annotation("Npgsql:TsVectorProperties", new[] { "normalized_citation", "detected_title", "raw_extracted_text" });

            migrationBuilder.CreateIndex(
                name: "IX_spr_report_packages_search_vector",
                schema: "gccs",
                table: "spr_report_packages",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_obligations_search_vector",
                schema: "gccs",
                table: "obligations",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_evidence_items_search_vector",
                schema: "gccs",
                table: "evidence_items",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_clause_candidates_search_vector",
                schema: "gccs",
                table: "clause_candidates",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_spr_report_packages_search_vector",
                schema: "gccs",
                table: "spr_report_packages");

            migrationBuilder.DropIndex(
                name: "IX_obligations_search_vector",
                schema: "gccs",
                table: "obligations");

            migrationBuilder.DropIndex(
                name: "IX_evidence_items_search_vector",
                schema: "gccs",
                table: "evidence_items");

            migrationBuilder.DropIndex(
                name: "IX_clause_candidates_search_vector",
                schema: "gccs",
                table: "clause_candidates");

            migrationBuilder.DropColumn(
                name: "search_vector",
                schema: "gccs",
                table: "spr_report_packages");

            migrationBuilder.DropColumn(
                name: "search_vector",
                schema: "gccs",
                table: "obligations");

            migrationBuilder.DropColumn(
                name: "search_vector",
                schema: "gccs",
                table: "evidence_items");

            migrationBuilder.DropColumn(
                name: "search_vector",
                schema: "gccs",
                table: "clause_candidates");
        }
    }
}

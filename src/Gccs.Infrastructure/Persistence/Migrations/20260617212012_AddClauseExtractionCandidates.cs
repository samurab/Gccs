using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClauseExtractionCandidates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "clause_candidates",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    extraction_job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    normalized_citation = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    raw_extracted_text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    detected_title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    confidence = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    location_metadata = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    match_method = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    clause_library_id = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    review_status = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clause_candidates", x => x.id);
                    table.ForeignKey(
                        name: "FK_clause_candidates_contract_documents_source_document_id",
                        column: x => x.source_document_id,
                        principalSchema: "gccs",
                        principalTable: "contract_documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_clause_candidates_extraction_jobs_extraction_job_id",
                        column: x => x.extraction_job_id,
                        principalSchema: "gccs",
                        principalTable: "extraction_jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_clause_candidates_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_clause_candidates_extraction_job_id_normalized_citation",
                schema: "gccs",
                table: "clause_candidates",
                columns: new[] { "extraction_job_id", "normalized_citation" });

            migrationBuilder.CreateIndex(
                name: "IX_clause_candidates_source_document_id",
                schema: "gccs",
                table: "clause_candidates",
                column: "source_document_id");

            migrationBuilder.CreateIndex(
                name: "IX_clause_candidates_tenant_id_source_document_id",
                schema: "gccs",
                table: "clause_candidates",
                columns: new[] { "tenant_id", "source_document_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "clause_candidates",
                schema: "gccs");
        }
    }
}

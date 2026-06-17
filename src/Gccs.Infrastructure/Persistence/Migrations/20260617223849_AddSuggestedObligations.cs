using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSuggestedObligations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "suggested_obligations",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    source_url = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    generated_summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    proposed_title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    proposed_owner_function = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    required_action = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: false),
                    risk_level = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    evidence_suggestions_json = table.Column<string>(type: "jsonb", nullable: false),
                    source_citations_json = table.Column<string>(type: "jsonb", nullable: false),
                    confidence = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    prompt_version = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    model_identifier = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    retrieved_source_references_json = table.Column<string>(type: "jsonb", nullable: false),
                    review_status = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reviewed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    review_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_suggested_obligations", x => x.id);
                    table.ForeignKey(
                        name: "FK_suggested_obligations_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_suggested_obligations_tenant_id_review_status_created_at",
                schema: "gccs",
                table: "suggested_obligations",
                columns: new[] { "tenant_id", "review_status", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "suggested_obligations",
                schema: "gccs");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDurableSspNarratives : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ssp_narratives",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    section_id = table.Column<Guid>(type: "uuid", nullable: false),
                    generated_text = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: false),
                    edited_text = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: true),
                    approved_text = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: true),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ai_assisted = table.Column<bool>(type: "boolean", nullable: false),
                    draft_only = table.Column<bool>(type: "boolean", nullable: false),
                    reviewer_notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    reviewer_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewer = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    review_date = table.Column<DateOnly>(type: "date", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    classification = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    classification_source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    classification_confidence = table.Column<decimal>(type: "numeric", nullable: true),
                    classification_reviewed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    classification_reviewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    classification_reason = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: true),
                    classification_is_approved_demo_content = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ssp_narratives", x => x.id);
                    table.UniqueConstraint("AK_ssp_narratives_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "FK_ssp_narratives_ssp_sections_tenant_id_section_id",
                        columns: x => new { x.tenant_id, x.section_id },
                        principalSchema: "gccs",
                        principalTable: "ssp_sections",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ssp_narratives_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ssp_narrative_sources",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    narrative_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    record_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    label = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    summary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    source_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    fingerprint = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    classification = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ssp_narrative_sources", x => x.id);
                    table.ForeignKey(
                        name: "FK_ssp_narrative_sources_ssp_narratives_tenant_id_narrative_id",
                        columns: x => new { x.tenant_id, x.narrative_id },
                        principalSchema: "gccs",
                        principalTable: "ssp_narratives",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ssp_narrative_sources_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ssp_narrative_sources_tenant_id_narrative_id_source_type_re~",
                schema: "gccs",
                table: "ssp_narrative_sources",
                columns: new[] { "tenant_id", "narrative_id", "source_type", "record_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ssp_narrative_sources_tenant_id_source_type_record_id",
                schema: "gccs",
                table: "ssp_narrative_sources",
                columns: new[] { "tenant_id", "source_type", "record_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ssp_narratives_tenant_id_section_id",
                schema: "gccs",
                table: "ssp_narratives",
                columns: new[] { "tenant_id", "section_id" },
                unique: true,
                filter: "status = 'Approved'");

            migrationBuilder.CreateIndex(
                name: "IX_ssp_narratives_tenant_id_section_id_status",
                schema: "gccs",
                table: "ssp_narratives",
                columns: new[] { "tenant_id", "section_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_ssp_narratives_tenant_id_section_id_updated_at",
                schema: "gccs",
                table: "ssp_narratives",
                columns: new[] { "tenant_id", "section_id", "updated_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ssp_narrative_sources",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "ssp_narratives",
                schema: "gccs");
        }
    }
}

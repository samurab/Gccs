using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDurableSspSections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ssp_sections",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    section_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    owner = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    reviewer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    review_date = table.Column<DateOnly>(type: "date", nullable: true),
                    approval_rationale = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ssp_sections", x => x.id);
                    table.UniqueConstraint("AK_ssp_sections_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "FK_ssp_sections_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ssp_section_links",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    section_id = table.Column<Guid>(type: "uuid", nullable: false),
                    record_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    record_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    relationship = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ssp_section_links", x => x.id);
                    table.ForeignKey(
                        name: "FK_ssp_section_links_ssp_sections_tenant_id_section_id",
                        columns: x => new { x.tenant_id, x.section_id },
                        principalSchema: "gccs",
                        principalTable: "ssp_sections",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ssp_section_links_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ssp_section_source_references",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    section_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    source_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    last_reviewed_at = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ssp_section_source_references", x => x.id);
                    table.ForeignKey(
                        name: "FK_ssp_section_source_references_ssp_sections_tenant_id_sectio~",
                        columns: x => new { x.tenant_id, x.section_id },
                        principalSchema: "gccs",
                        principalTable: "ssp_sections",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ssp_section_source_references_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ssp_section_status_history",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    section_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ssp_section_status_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_ssp_section_status_history_ssp_sections_tenant_id_section_id",
                        columns: x => new { x.tenant_id, x.section_id },
                        principalSchema: "gccs",
                        principalTable: "ssp_sections",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ssp_section_status_history_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ssp_section_links_tenant_id_record_type_record_id",
                schema: "gccs",
                table: "ssp_section_links",
                columns: new[] { "tenant_id", "record_type", "record_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ssp_section_links_tenant_id_section_id_record_type_record_id",
                schema: "gccs",
                table: "ssp_section_links",
                columns: new[] { "tenant_id", "section_id", "record_type", "record_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ssp_section_source_references_tenant_id_section_id",
                schema: "gccs",
                table: "ssp_section_source_references",
                columns: new[] { "tenant_id", "section_id" });

            migrationBuilder.CreateIndex(
                name: "IX_ssp_section_status_history_tenant_id_section_id_changed_at",
                schema: "gccs",
                table: "ssp_section_status_history",
                columns: new[] { "tenant_id", "section_id", "changed_at" });

            migrationBuilder.CreateIndex(
                name: "IX_ssp_sections_created_at_updated_at",
                schema: "gccs",
                table: "ssp_sections",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_ssp_sections_tenant_id_section_type",
                schema: "gccs",
                table: "ssp_sections",
                columns: new[] { "tenant_id", "section_type" });

            migrationBuilder.CreateIndex(
                name: "IX_ssp_sections_tenant_id_status",
                schema: "gccs",
                table: "ssp_sections",
                columns: new[] { "tenant_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ssp_section_links",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "ssp_section_source_references",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "ssp_section_status_history",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "ssp_sections",
                schema: "gccs");
        }
    }
}

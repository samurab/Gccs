using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPolicyTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "policy_templates",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    category = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    placeholders_json = table.Column<string>(type: "jsonb", nullable: false),
                    source_references_json = table.Column<string>(type: "jsonb", nullable: false),
                    version = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    owner_function = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    last_reviewed_at = table.Column<DateOnly>(type: "date", nullable: true),
                    reviewer_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    requires_expert_review = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_policy_templates", x => x.id);
                    table.ForeignKey(
                        name: "FK_policy_templates_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "policy_template_versions",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    body_preview = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_policy_template_versions", x => x.id);
                    table.ForeignKey(
                        name: "FK_policy_template_versions_policy_templates_template_id",
                        column: x => x.template_id,
                        principalSchema: "gccs",
                        principalTable: "policy_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_policy_template_versions_template_id_version",
                schema: "gccs",
                table: "policy_template_versions",
                columns: new[] { "template_id", "version" });

            migrationBuilder.CreateIndex(
                name: "IX_policy_templates_created_at_updated_at",
                schema: "gccs",
                table: "policy_templates",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_policy_templates_tenant_id_status_category",
                schema: "gccs",
                table: "policy_templates",
                columns: new[] { "tenant_id", "status", "category" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "policy_template_versions",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "policy_templates",
                schema: "gccs");
        }
    }
}

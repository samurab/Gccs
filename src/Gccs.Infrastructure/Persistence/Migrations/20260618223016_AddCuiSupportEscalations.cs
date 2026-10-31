using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCuiSupportEscalations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cui_support_escalations",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_workflow = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    affected_entity_type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    affected_entity_id = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    severity = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    owner = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: true),
                    description = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: false),
                    is_affected_content_blocked = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cui_support_escalations", x => x.id);
                    table.ForeignKey(
                        name: "FK_cui_support_escalations_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cui_support_escalations_created_at_updated_at",
                schema: "gccs",
                table: "cui_support_escalations",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_cui_support_escalations_tenant_id_affected_entity_type_affe~",
                schema: "gccs",
                table: "cui_support_escalations",
                columns: new[] { "tenant_id", "affected_entity_type", "affected_entity_id" });

            migrationBuilder.CreateIndex(
                name: "IX_cui_support_escalations_tenant_id_status_created_at",
                schema: "gccs",
                table: "cui_support_escalations",
                columns: new[] { "tenant_id", "status", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cui_support_escalations",
                schema: "gccs");
        }
    }
}

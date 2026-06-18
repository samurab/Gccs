using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCuiReadyApprovalChecklists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cui_ready_approval_checklists",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    state = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    rejection_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    reviewed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cui_ready_approval_checklists", x => x.id);
                    table.ForeignKey(
                        name: "FK_cui_ready_approval_checklists_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cui_ready_approval_checklist_items",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    checklist_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    section = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    owner = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: true),
                    evidence_link = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: true),
                    reviewer_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_at = table.Column<DateOnly>(type: "date", nullable: true),
                    notes = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cui_ready_approval_checklist_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_cui_ready_approval_checklist_items_cui_ready_approval_check~",
                        column: x => x.checklist_id,
                        principalSchema: "gccs",
                        principalTable: "cui_ready_approval_checklists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cui_ready_approval_checklist_items_checklist_id_item_key",
                schema: "gccs",
                table: "cui_ready_approval_checklist_items",
                columns: new[] { "checklist_id", "item_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cui_ready_approval_checklists_created_at_updated_at",
                schema: "gccs",
                table: "cui_ready_approval_checklists",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_cui_ready_approval_checklists_tenant_id_state_created_at",
                schema: "gccs",
                table: "cui_ready_approval_checklists",
                columns: new[] { "tenant_id", "state", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cui_ready_approval_checklist_items",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "cui_ready_approval_checklists",
                schema: "gccs");
        }
    }
}

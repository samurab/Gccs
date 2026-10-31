using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReusableComplianceChecklists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "compliance_checklist_instances",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    name = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    checklist_type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    review_status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_compliance_checklist_instances", x => x.id);
                    table.ForeignKey(
                        name: "FK_compliance_checklist_instances_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "compliance_checklist_items",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    checklist_id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_item_key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    title = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    description = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    review_status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    reviewed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    control_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    evidence_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    poam_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_compliance_checklist_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_compliance_checklist_items_compliance_checklist_instances_c~",
                        column: x => x.checklist_id,
                        principalSchema: "gccs",
                        principalTable: "compliance_checklist_instances",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_compliance_checklist_items_evidence_items_evidence_item_id",
                        column: x => x.evidence_item_id,
                        principalSchema: "gccs",
                        principalTable: "evidence_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_compliance_checklist_items_poam_items_poam_item_id",
                        column: x => x.poam_item_id,
                        principalSchema: "gccs",
                        principalTable: "poam_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_compliance_checklist_instances_created_at_updated_at",
                schema: "gccs",
                table: "compliance_checklist_instances",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_compliance_checklist_instances_tenant_id_template_key_creat~",
                schema: "gccs",
                table: "compliance_checklist_instances",
                columns: new[] { "tenant_id", "template_key", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_compliance_checklist_items_checklist_id_template_item_key",
                schema: "gccs",
                table: "compliance_checklist_items",
                columns: new[] { "checklist_id", "template_item_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_compliance_checklist_items_evidence_item_id",
                schema: "gccs",
                table: "compliance_checklist_items",
                column: "evidence_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_compliance_checklist_items_poam_item_id",
                schema: "gccs",
                table: "compliance_checklist_items",
                column: "poam_item_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "compliance_checklist_items",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "compliance_checklist_instances",
                schema: "gccs");
        }
    }
}

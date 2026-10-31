using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVersionedCuiReadinessEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "supporting_record_id",
                schema: "gccs",
                table: "cui_ready_approval_checklist_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "supporting_version",
                schema: "gccs",
                table: "cui_ready_approval_checklist_items",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "cui_readiness_evidence",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    state = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    reviewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reviewed_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    source_reference = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    review_notes = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: false),
                    details_json = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cui_readiness_evidence", x => x.id);
                    table.ForeignKey(
                        name: "FK_cui_readiness_evidence_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cui_readiness_evidence_tenant_id_kind_version",
                schema: "gccs",
                table: "cui_readiness_evidence",
                columns: new[] { "tenant_id", "kind", "version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cui_readiness_evidence",
                schema: "gccs");

            migrationBuilder.DropColumn(
                name: "supporting_record_id",
                schema: "gccs",
                table: "cui_ready_approval_checklist_items");

            migrationBuilder.DropColumn(
                name: "supporting_version",
                schema: "gccs",
                table: "cui_ready_approval_checklist_items");
        }
    }
}

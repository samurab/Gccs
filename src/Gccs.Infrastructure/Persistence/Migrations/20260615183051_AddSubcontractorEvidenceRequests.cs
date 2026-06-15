using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSubcontractorEvidenceRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "subcontractor_evidence_requests",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subcontractor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_item = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    requested_evidence_types_json = table.Column<string>(type: "jsonb", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    recipient_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    recipient_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    obligation_id = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    related_flow_down_clause_id = table.Column<Guid>(type: "uuid", nullable: true),
                    received_evidence_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subcontractor_evidence_requests", x => x.id);
                    table.ForeignKey(
                        name: "FK_subcontractor_evidence_requests_evidence_items_received_evi~",
                        column: x => x.received_evidence_item_id,
                        principalSchema: "gccs",
                        principalTable: "evidence_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_subcontractor_evidence_requests_flow_down_clauses_related_f~",
                        column: x => x.related_flow_down_clause_id,
                        principalSchema: "gccs",
                        principalTable: "flow_down_clauses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_subcontractor_evidence_requests_obligations_obligation_id",
                        column: x => x.obligation_id,
                        principalSchema: "gccs",
                        principalTable: "obligations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_subcontractor_evidence_requests_subcontractors_subcontracto~",
                        column: x => x.subcontractor_id,
                        principalSchema: "gccs",
                        principalTable: "subcontractors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_subcontractor_evidence_requests_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_subcontractor_evidence_requests_created_at_updated_at",
                schema: "gccs",
                table: "subcontractor_evidence_requests",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_subcontractor_evidence_requests_obligation_id",
                schema: "gccs",
                table: "subcontractor_evidence_requests",
                column: "obligation_id");

            migrationBuilder.CreateIndex(
                name: "IX_subcontractor_evidence_requests_received_evidence_item_id",
                schema: "gccs",
                table: "subcontractor_evidence_requests",
                column: "received_evidence_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_subcontractor_evidence_requests_related_flow_down_clause_id",
                schema: "gccs",
                table: "subcontractor_evidence_requests",
                column: "related_flow_down_clause_id");

            migrationBuilder.CreateIndex(
                name: "IX_subcontractor_evidence_requests_subcontractor_id_due_date",
                schema: "gccs",
                table: "subcontractor_evidence_requests",
                columns: new[] { "subcontractor_id", "due_date" });

            migrationBuilder.CreateIndex(
                name: "IX_subcontractor_evidence_requests_tenant_id_status_due_date",
                schema: "gccs",
                table: "subcontractor_evidence_requests",
                columns: new[] { "tenant_id", "status", "due_date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "subcontractor_evidence_requests",
                schema: "gccs");
        }
    }
}

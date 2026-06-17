using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClauseObligationMappings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "clause_obligation_mappings",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    clause_id = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    obligation_id = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    trigger_condition = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: false),
                    required_action = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: false),
                    source_url = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    confidence = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    requires_expert_review = table.Column<bool>(type: "boolean", nullable: false),
                    review_state = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "Draft"),
                    last_reviewed_at = table.Column<DateOnly>(type: "date", nullable: false),
                    reviewed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    previous_mapping_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clause_obligation_mappings", x => x.id);
                    table.ForeignKey(
                        name: "FK_clause_obligation_mappings_clauses_clause_id",
                        column: x => x.clause_id,
                        principalSchema: "gccs",
                        principalTable: "clauses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_clause_obligation_mappings_obligations_obligation_id",
                        column: x => x.obligation_id,
                        principalSchema: "gccs",
                        principalTable: "obligations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_clause_obligation_mappings_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_clause_obligation_mappings_clause_id_review_state",
                schema: "gccs",
                table: "clause_obligation_mappings",
                columns: new[] { "clause_id", "review_state" });

            migrationBuilder.CreateIndex(
                name: "IX_clause_obligation_mappings_obligation_id",
                schema: "gccs",
                table: "clause_obligation_mappings",
                column: "obligation_id");

            migrationBuilder.CreateIndex(
                name: "IX_clause_obligation_mappings_tenant_id_clause_id_obligation_id",
                schema: "gccs",
                table: "clause_obligation_mappings",
                columns: new[] { "tenant_id", "clause_id", "obligation_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "clause_obligation_mappings",
                schema: "gccs");
        }
    }
}

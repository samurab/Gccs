using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSubcontractorFlowDownTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "acknowledged_at",
                schema: "gccs",
                table: "flow_down_clauses",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "contract_clause_id",
                schema: "gccs",
                table: "flow_down_clauses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "contract_id",
                schema: "gccs",
                table: "flow_down_clauses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_at",
                schema: "gccs",
                table: "flow_down_clauses",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_user_id",
                schema: "gccs",
                table: "flow_down_clauses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "obligation_id",
                schema: "gccs",
                table: "flow_down_clauses",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                schema: "gccs",
                table: "flow_down_clauses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_user_id",
                schema: "gccs",
                table: "flow_down_clauses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "waived_at",
                schema: "gccs",
                table: "flow_down_clauses",
                type: "date",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_flow_down_clauses_contract_clause_id",
                schema: "gccs",
                table: "flow_down_clauses",
                column: "contract_clause_id");

            migrationBuilder.CreateIndex(
                name: "IX_flow_down_clauses_contract_id_clause_number",
                schema: "gccs",
                table: "flow_down_clauses",
                columns: new[] { "contract_id", "clause_number" });

            migrationBuilder.CreateIndex(
                name: "IX_flow_down_clauses_created_at_updated_at",
                schema: "gccs",
                table: "flow_down_clauses",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_flow_down_clauses_obligation_id",
                schema: "gccs",
                table: "flow_down_clauses",
                column: "obligation_id");

            migrationBuilder.CreateIndex(
                name: "IX_flow_down_clauses_signed_evidence_item_id",
                schema: "gccs",
                table: "flow_down_clauses",
                column: "signed_evidence_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_flow_down_clauses_subcontractor_id_contract_id",
                schema: "gccs",
                table: "flow_down_clauses",
                columns: new[] { "subcontractor_id", "contract_id" });

            migrationBuilder.AddForeignKey(
                name: "FK_flow_down_clauses_contract_clauses_contract_clause_id",
                schema: "gccs",
                table: "flow_down_clauses",
                column: "contract_clause_id",
                principalSchema: "gccs",
                principalTable: "contract_clauses",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_flow_down_clauses_contracts_contract_id",
                schema: "gccs",
                table: "flow_down_clauses",
                column: "contract_id",
                principalSchema: "gccs",
                principalTable: "contracts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_flow_down_clauses_evidence_items_signed_evidence_item_id",
                schema: "gccs",
                table: "flow_down_clauses",
                column: "signed_evidence_item_id",
                principalSchema: "gccs",
                principalTable: "evidence_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_flow_down_clauses_obligations_obligation_id",
                schema: "gccs",
                table: "flow_down_clauses",
                column: "obligation_id",
                principalSchema: "gccs",
                principalTable: "obligations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_flow_down_clauses_contract_clauses_contract_clause_id",
                schema: "gccs",
                table: "flow_down_clauses");

            migrationBuilder.DropForeignKey(
                name: "FK_flow_down_clauses_contracts_contract_id",
                schema: "gccs",
                table: "flow_down_clauses");

            migrationBuilder.DropForeignKey(
                name: "FK_flow_down_clauses_evidence_items_signed_evidence_item_id",
                schema: "gccs",
                table: "flow_down_clauses");

            migrationBuilder.DropForeignKey(
                name: "FK_flow_down_clauses_obligations_obligation_id",
                schema: "gccs",
                table: "flow_down_clauses");

            migrationBuilder.DropIndex(
                name: "IX_flow_down_clauses_contract_clause_id",
                schema: "gccs",
                table: "flow_down_clauses");

            migrationBuilder.DropIndex(
                name: "IX_flow_down_clauses_contract_id_clause_number",
                schema: "gccs",
                table: "flow_down_clauses");

            migrationBuilder.DropIndex(
                name: "IX_flow_down_clauses_created_at_updated_at",
                schema: "gccs",
                table: "flow_down_clauses");

            migrationBuilder.DropIndex(
                name: "IX_flow_down_clauses_obligation_id",
                schema: "gccs",
                table: "flow_down_clauses");

            migrationBuilder.DropIndex(
                name: "IX_flow_down_clauses_signed_evidence_item_id",
                schema: "gccs",
                table: "flow_down_clauses");

            migrationBuilder.DropIndex(
                name: "IX_flow_down_clauses_subcontractor_id_contract_id",
                schema: "gccs",
                table: "flow_down_clauses");

            migrationBuilder.DropColumn(
                name: "acknowledged_at",
                schema: "gccs",
                table: "flow_down_clauses");

            migrationBuilder.DropColumn(
                name: "contract_clause_id",
                schema: "gccs",
                table: "flow_down_clauses");

            migrationBuilder.DropColumn(
                name: "contract_id",
                schema: "gccs",
                table: "flow_down_clauses");

            migrationBuilder.DropColumn(
                name: "created_at",
                schema: "gccs",
                table: "flow_down_clauses");

            migrationBuilder.DropColumn(
                name: "created_by_user_id",
                schema: "gccs",
                table: "flow_down_clauses");

            migrationBuilder.DropColumn(
                name: "obligation_id",
                schema: "gccs",
                table: "flow_down_clauses");

            migrationBuilder.DropColumn(
                name: "updated_at",
                schema: "gccs",
                table: "flow_down_clauses");

            migrationBuilder.DropColumn(
                name: "updated_by_user_id",
                schema: "gccs",
                table: "flow_down_clauses");

            migrationBuilder.DropColumn(
                name: "waived_at",
                schema: "gccs",
                table: "flow_down_clauses");
        }
    }
}

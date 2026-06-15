using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClauseTenantScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_clauses_source_number",
                schema: "gccs",
                table: "clauses");

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                schema: "gccs",
                table: "clauses",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_clauses_tenant_id_review_state",
                schema: "gccs",
                table: "clauses",
                columns: new[] { "tenant_id", "review_state" });

            migrationBuilder.CreateIndex(
                name: "IX_clauses_tenant_id_source_number",
                schema: "gccs",
                table: "clauses",
                columns: new[] { "tenant_id", "source", "number" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_clauses_tenants_tenant_id",
                schema: "gccs",
                table: "clauses",
                column: "tenant_id",
                principalSchema: "gccs",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_clauses_tenants_tenant_id",
                schema: "gccs",
                table: "clauses");

            migrationBuilder.DropIndex(
                name: "IX_clauses_tenant_id_review_state",
                schema: "gccs",
                table: "clauses");

            migrationBuilder.DropIndex(
                name: "IX_clauses_tenant_id_source_number",
                schema: "gccs",
                table: "clauses");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                schema: "gccs",
                table: "clauses");

            migrationBuilder.CreateIndex(
                name: "IX_clauses_source_number",
                schema: "gccs",
                table: "clauses",
                columns: new[] { "source", "number" },
                unique: true);
        }
    }
}

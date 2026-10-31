using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPreferredTenantWorkspace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "preferred_tenant_id",
                schema: "gccs",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_preferred_tenant_id",
                schema: "gccs",
                table: "users",
                column: "preferred_tenant_id");

            migrationBuilder.AddForeignKey(
                name: "FK_users_tenants_preferred_tenant_id",
                schema: "gccs",
                table: "users",
                column: "preferred_tenant_id",
                principalSchema: "gccs",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_users_tenants_preferred_tenant_id",
                schema: "gccs",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_preferred_tenant_id",
                schema: "gccs",
                table: "users");

            migrationBuilder.DropColumn(
                name: "preferred_tenant_id",
                schema: "gccs",
                table: "users");
        }
    }
}

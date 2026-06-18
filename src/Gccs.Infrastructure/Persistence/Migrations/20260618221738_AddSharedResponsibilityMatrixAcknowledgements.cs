using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSharedResponsibilityMatrixAcknowledgements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "shared_responsibility_matrix_acknowledgements",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    matrix_id = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    matrix_version = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    matrix_title = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    acknowledged_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    acknowledged_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shared_responsibility_matrix_acknowledgements", x => x.id);
                    table.ForeignKey(
                        name: "FK_shared_responsibility_matrix_acknowledgements_tenants_tenan~",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_shared_responsibility_matrix_acknowledgements_created_at_up~",
                schema: "gccs",
                table: "shared_responsibility_matrix_acknowledgements",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_shared_responsibility_matrix_acknowledgements_tenant_id_ack~",
                schema: "gccs",
                table: "shared_responsibility_matrix_acknowledgements",
                columns: new[] { "tenant_id", "acknowledged_at" });

            migrationBuilder.CreateIndex(
                name: "IX_shared_responsibility_matrix_acknowledgements_tenant_id_mat~",
                schema: "gccs",
                table: "shared_responsibility_matrix_acknowledgements",
                columns: new[] { "tenant_id", "matrix_id", "matrix_version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "shared_responsibility_matrix_acknowledgements",
                schema: "gccs");
        }
    }
}

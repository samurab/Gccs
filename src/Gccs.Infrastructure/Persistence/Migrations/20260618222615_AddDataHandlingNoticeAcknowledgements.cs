using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDataHandlingNoticeAcknowledgements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "data_handling_notice_acknowledgements",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    workflow_context = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    notice_id = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    notice_version = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    acknowledged_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_data_handling_notice_acknowledgements", x => x.id);
                    table.ForeignKey(
                        name: "FK_data_handling_notice_acknowledgements_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_data_handling_notice_acknowledgements_created_at_updated_at",
                schema: "gccs",
                table: "data_handling_notice_acknowledgements",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_data_handling_notice_acknowledgements_tenant_id_user_id_ack~",
                schema: "gccs",
                table: "data_handling_notice_acknowledgements",
                columns: new[] { "tenant_id", "user_id", "acknowledged_at" });

            migrationBuilder.CreateIndex(
                name: "IX_data_handling_notice_acknowledgements_tenant_id_user_id_mod~",
                schema: "gccs",
                table: "data_handling_notice_acknowledgements",
                columns: new[] { "tenant_id", "user_id", "mode", "workflow_context", "notice_id", "notice_version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "data_handling_notice_acknowledgements",
                schema: "gccs");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDurableObjectCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "object_cleanup",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    container = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    object_name = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    next_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_error_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_object_cleanup", x => x.id);
                    table.ForeignKey(
                        name: "FK_object_cleanup_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_object_cleanup_completed_at_next_attempt_at",
                schema: "gccs",
                table: "object_cleanup",
                columns: new[] { "completed_at", "next_attempt_at" });

            migrationBuilder.CreateIndex(
                name: "IX_object_cleanup_tenant_id_id",
                schema: "gccs",
                table: "object_cleanup",
                columns: new[] { "tenant_id", "id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM gccs.object_cleanup WHERE completed_at IS NULL) THEN
                        RAISE EXCEPTION 'Cannot remove durable cleanup storage while cleanup is pending';
                    END IF;
                END $$;
                """);
            migrationBuilder.DropTable(
                name: "object_cleanup",
                schema: "gccs");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovedPackagePortalReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "portal_package_review_messages",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invitation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shared_package_id = table.Column<Guid>(type: "uuid", nullable: false),
                    package_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    body = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_portal_package_review_messages", x => x.id);
                    table.ForeignKey(
                        name: "FK_portal_package_review_messages_external_portal_invitations_~",
                        columns: x => new { x.tenant_id, x.invitation_id },
                        principalSchema: "gccs",
                        principalTable: "external_portal_invitations",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_portal_package_review_messages_shared_portal_packages_tenan~",
                        columns: x => new { x.tenant_id, x.shared_package_id },
                        principalSchema: "gccs",
                        principalTable: "shared_portal_packages",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_portal_package_review_messages_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_portal_package_review_messages_tenant_id_actor_user_id_crea~",
                schema: "gccs",
                table: "portal_package_review_messages",
                columns: new[] { "tenant_id", "actor_user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_portal_package_review_messages_tenant_id_invitation_id",
                schema: "gccs",
                table: "portal_package_review_messages",
                columns: new[] { "tenant_id", "invitation_id" });

            migrationBuilder.CreateIndex(
                name: "IX_portal_package_review_messages_tenant_id_shared_package_id_~",
                schema: "gccs",
                table: "portal_package_review_messages",
                columns: new[] { "tenant_id", "shared_package_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "portal_package_review_messages",
                schema: "gccs");
        }
    }
}

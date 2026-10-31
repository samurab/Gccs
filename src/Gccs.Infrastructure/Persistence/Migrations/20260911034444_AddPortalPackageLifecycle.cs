using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPortalPackageLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "shared_portal_packages",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    package_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invitation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    state = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reminder_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reminder_sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    supersedes_shared_package_id = table.Column<Guid>(type: "uuid", nullable: true),
                    replacement_shared_package_id = table.Column<Guid>(type: "uuid", nullable: true),
                    replacement_package_id = table.Column<Guid>(type: "uuid", nullable: true),
                    revocation_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shared_portal_packages", x => x.id);
                    table.UniqueConstraint("AK_shared_portal_packages_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "FK_shared_portal_packages_shared_portal_packages_tenant_id_rep~",
                        columns: x => new { x.tenant_id, x.replacement_shared_package_id },
                        principalSchema: "gccs",
                        principalTable: "shared_portal_packages",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shared_portal_packages_shared_portal_packages_tenant_id_sup~",
                        columns: x => new { x.tenant_id, x.supersedes_shared_package_id },
                        principalSchema: "gccs",
                        principalTable: "shared_portal_packages",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shared_portal_packages_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "portal_package_activities",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shared_package_id = table.Column<Guid>(type: "uuid", nullable: false),
                    activity_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    detail = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_portal_package_activities", x => x.id);
                    table.ForeignKey(
                        name: "FK_portal_package_activities_shared_portal_packages_tenant_id_~",
                        columns: x => new { x.tenant_id, x.shared_package_id },
                        principalSchema: "gccs",
                        principalTable: "shared_portal_packages",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_portal_package_activities_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_portal_package_activities_tenant_id_occurred_at",
                schema: "gccs",
                table: "portal_package_activities",
                columns: new[] { "tenant_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_portal_package_activities_tenant_id_shared_package_id_activ~",
                schema: "gccs",
                table: "portal_package_activities",
                columns: new[] { "tenant_id", "shared_package_id", "activity_type" });

            migrationBuilder.CreateIndex(
                name: "IX_shared_portal_packages_created_at_updated_at",
                schema: "gccs",
                table: "shared_portal_packages",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_shared_portal_packages_state_reminder_at_reminder_sent_at",
                schema: "gccs",
                table: "shared_portal_packages",
                columns: new[] { "state", "reminder_at", "reminder_sent_at" });

            migrationBuilder.CreateIndex(
                name: "IX_shared_portal_packages_tenant_id_invitation_id_package_id",
                schema: "gccs",
                table: "shared_portal_packages",
                columns: new[] { "tenant_id", "invitation_id", "package_id" },
                unique: true,
                filter: "state = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_shared_portal_packages_tenant_id_invitation_id_version",
                schema: "gccs",
                table: "shared_portal_packages",
                columns: new[] { "tenant_id", "invitation_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_shared_portal_packages_tenant_id_replacement_shared_package~",
                schema: "gccs",
                table: "shared_portal_packages",
                columns: new[] { "tenant_id", "replacement_shared_package_id" });

            migrationBuilder.CreateIndex(
                name: "IX_shared_portal_packages_tenant_id_state_expires_at",
                schema: "gccs",
                table: "shared_portal_packages",
                columns: new[] { "tenant_id", "state", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "IX_shared_portal_packages_tenant_id_supersedes_shared_package_~",
                schema: "gccs",
                table: "shared_portal_packages",
                columns: new[] { "tenant_id", "supersedes_shared_package_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "portal_package_activities",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "shared_portal_packages",
                schema: "gccs");
        }
    }
}

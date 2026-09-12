using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalPortalAccessModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "external_portal_invitations",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    role = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    can_download = table.Column<bool>(type: "boolean", nullable: false),
                    strong_authentication_required = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    external_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_accessed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revocation_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    resend_count = table.Column<int>(type: "integer", nullable: false),
                    last_resent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_external_portal_invitations", x => x.id);
                    table.UniqueConstraint("AK_external_portal_invitations_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "FK_external_portal_invitations_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "external_portal_access_history",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invitation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    package_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: true),
                    allowed = table.Column<bool>(type: "boolean", nullable: false),
                    result_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_external_portal_access_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_external_portal_access_history_external_portal_invitations_~",
                        columns: x => new { x.tenant_id, x.invitation_id },
                        principalSchema: "gccs",
                        principalTable: "external_portal_invitations",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_external_portal_access_history_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "external_portal_invitation_contract_scopes",
                schema: "gccs",
                columns: table => new
                {
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invitation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_external_portal_invitation_contract_scopes", x => new { x.tenant_id, x.invitation_id, x.contract_id });
                    table.ForeignKey(
                        name: "FK_external_portal_invitation_contract_scopes_contracts_tenant~",
                        columns: x => new { x.tenant_id, x.contract_id },
                        principalSchema: "gccs",
                        principalTable: "contracts",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_external_portal_invitation_contract_scopes_external_portal_~",
                        columns: x => new { x.tenant_id, x.invitation_id },
                        principalSchema: "gccs",
                        principalTable: "external_portal_invitations",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_external_portal_invitation_contract_scopes_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "external_portal_invitation_package_scopes",
                schema: "gccs",
                columns: table => new
                {
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invitation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    package_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_external_portal_invitation_package_scopes", x => new { x.tenant_id, x.invitation_id, x.package_id });
                    table.ForeignKey(
                        name: "FK_external_portal_invitation_package_scopes_external_portal_i~",
                        columns: x => new { x.tenant_id, x.invitation_id },
                        principalSchema: "gccs",
                        principalTable: "external_portal_invitations",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_external_portal_invitation_package_scopes_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_external_portal_access_history_tenant_id_actor_user_id_occu~",
                schema: "gccs",
                table: "external_portal_access_history",
                columns: new[] { "tenant_id", "actor_user_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_external_portal_access_history_tenant_id_invitation_id_occu~",
                schema: "gccs",
                table: "external_portal_access_history",
                columns: new[] { "tenant_id", "invitation_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_external_portal_invitation_contract_scopes_tenant_id_contra~",
                schema: "gccs",
                table: "external_portal_invitation_contract_scopes",
                columns: new[] { "tenant_id", "contract_id" });

            migrationBuilder.CreateIndex(
                name: "IX_external_portal_invitation_package_scopes_tenant_id_package~",
                schema: "gccs",
                table: "external_portal_invitation_package_scopes",
                columns: new[] { "tenant_id", "package_id" });

            migrationBuilder.CreateIndex(
                name: "IX_external_portal_invitations_created_at_updated_at",
                schema: "gccs",
                table: "external_portal_invitations",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_external_portal_invitations_tenant_id_email_status",
                schema: "gccs",
                table: "external_portal_invitations",
                columns: new[] { "tenant_id", "email", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_external_portal_invitations_tenant_id_expires_at",
                schema: "gccs",
                table: "external_portal_invitations",
                columns: new[] { "tenant_id", "expires_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "external_portal_access_history",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "external_portal_invitation_contract_scopes",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "external_portal_invitation_package_scopes",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "external_portal_invitations",
                schema: "gccs");
        }
    }
}

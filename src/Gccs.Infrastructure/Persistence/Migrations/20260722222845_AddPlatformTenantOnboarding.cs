using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformTenantOnboarding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "platform_tenant_onboardings",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invitation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    request_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    onboarding_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    customer_reference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    owner_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    owner_display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    plan_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    subscription_reference = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    commercial_approval_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    setup_reason = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_platform_tenant_onboardings", x => x.id);
                    table.ForeignKey(
                        name: "FK_platform_tenant_onboardings_tenant_invitations_invitation_id",
                        column: x => x.invitation_id,
                        principalSchema: "gccs",
                        principalTable: "tenant_invitations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_platform_tenant_onboardings_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_platform_tenant_onboardings_created_at_updated_at",
                schema: "gccs",
                table: "platform_tenant_onboardings",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_platform_tenant_onboardings_customer_reference",
                schema: "gccs",
                table: "platform_tenant_onboardings",
                column: "customer_reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_platform_tenant_onboardings_idempotency_key",
                schema: "gccs",
                table: "platform_tenant_onboardings",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_platform_tenant_onboardings_invitation_id",
                schema: "gccs",
                table: "platform_tenant_onboardings",
                column: "invitation_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_platform_tenant_onboardings_subscription_reference",
                schema: "gccs",
                table: "platform_tenant_onboardings",
                column: "subscription_reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_platform_tenant_onboardings_tenant_id",
                schema: "gccs",
                table: "platform_tenant_onboardings",
                column: "tenant_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "platform_tenant_onboardings",
                schema: "gccs");
        }
    }
}

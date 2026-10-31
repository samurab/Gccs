using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantSubscriptionLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tenant_subscriptions",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    plan = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    plan_code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    starts_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ends_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    grace_ends_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    external_customer_reference = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    external_subscription_reference = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    status_reason = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_subscriptions", x => x.id);
                    table.ForeignKey(
                        name: "FK_tenant_subscriptions_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tenant_subscription_transitions",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subscription_id = table.Column<Guid>(type: "uuid", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    request_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    transition = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    result_json = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_subscription_transitions", x => x.id);
                    table.ForeignKey(
                        name: "FK_tenant_subscription_transitions_tenant_subscriptions_subscr~",
                        column: x => x.subscription_id,
                        principalSchema: "gccs",
                        principalTable: "tenant_subscriptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tenant_subscription_transitions_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tenant_subscription_transitions_subscription_id",
                schema: "gccs",
                table: "tenant_subscription_transitions",
                column: "subscription_id");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_subscription_transitions_tenant_id_idempotency_key",
                schema: "gccs",
                table: "tenant_subscription_transitions",
                columns: new[] { "tenant_id", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_subscriptions_created_at_updated_at",
                schema: "gccs",
                table: "tenant_subscriptions",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_tenant_subscriptions_external_subscription_reference",
                schema: "gccs",
                table: "tenant_subscriptions",
                column: "external_subscription_reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_subscriptions_tenant_id",
                schema: "gccs",
                table: "tenant_subscriptions",
                column: "tenant_id",
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO gccs.tenant_subscriptions (
                    id, tenant_id, tenant_kind, plan, plan_code, status,
                    starts_at, ends_at, grace_ends_at,
                    external_customer_reference, external_subscription_reference,
                    status_reason, version, created_at, created_by_user_id)
                SELECT
                    onboarding.id,
                    onboarding.tenant_id,
                    'ContractorWorkspace',
                    CASE WHEN onboarding.onboarding_type = 'Pilot' THEN 'PilotEvaluation' ELSE 'CommercialStandard' END,
                    CASE WHEN onboarding.onboarding_type = 'Pilot' THEN 'PILOT-EVALUATION' ELSE onboarding.plan_code END,
                    CASE
                        WHEN onboarding.status = 'Cancelled' THEN 'Cancelled'
                        WHEN onboarding.status = 'PendingOwnerAcceptance' THEN 'Pending'
                        ELSE 'Active'
                    END,
                    onboarding.created_at,
                    CASE
                        WHEN onboarding.onboarding_type = 'Pilot' AND tenant.trial_ends_at IS NOT NULL
                        THEN (tenant.trial_ends_at + 1)::timestamp AT TIME ZONE 'UTC'
                        ELSE NULL
                    END,
                    CASE
                        WHEN onboarding.onboarding_type = 'Pilot' AND tenant.trial_ends_at IS NOT NULL
                        THEN (tenant.trial_ends_at + 8)::timestamp AT TIME ZONE 'UTC'
                        ELSE NULL
                    END,
                    onboarding.customer_reference,
                    onboarding.subscription_reference,
                    'Backfilled from platform tenant onboarding.',
                    1,
                    onboarding.created_at,
                    onboarding.created_by_user_id
                FROM gccs.platform_tenant_onboardings AS onboarding
                INNER JOIN gccs.tenants AS tenant ON tenant.id = onboarding.tenant_id;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tenant_subscription_transitions",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "tenant_subscriptions",
                schema: "gccs");
        }
    }
}

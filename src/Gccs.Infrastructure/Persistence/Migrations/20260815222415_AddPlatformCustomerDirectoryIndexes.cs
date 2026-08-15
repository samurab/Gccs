using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformCustomerDirectoryIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_tenants_name",
                schema: "gccs",
                table: "tenants",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "IX_tenant_subscriptions_plan_status_ends_at",
                schema: "gccs",
                table: "tenant_subscriptions",
                columns: new[] { "plan", "status", "ends_at" });

            migrationBuilder.CreateIndex(
                name: "IX_platform_tenant_onboardings_owner_email",
                schema: "gccs",
                table: "platform_tenant_onboardings",
                column: "owner_email");

            migrationBuilder.CreateIndex(
                name: "IX_platform_tenant_onboardings_status_onboarding_type_created_~",
                schema: "gccs",
                table: "platform_tenant_onboardings",
                columns: new[] { "status", "onboarding_type", "created_at" });

            migrationBuilder.Sql("""
                CREATE INDEX "IX_platform_customers_tenant_name_prefix"
                ON gccs.tenants (upper(name) text_pattern_ops);
                CREATE INDEX "IX_platform_customers_reference_prefix"
                ON gccs.platform_tenant_onboardings (upper(customer_reference) text_pattern_ops);
                CREATE INDEX "IX_platform_customers_owner_email_prefix"
                ON gccs.platform_tenant_onboardings (upper(owner_email) text_pattern_ops);
                CREATE INDEX "IX_platform_customers_subscription_reference_prefix"
                ON gccs.tenant_subscriptions (upper(external_subscription_reference) text_pattern_ops);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP INDEX gccs."IX_platform_customers_tenant_name_prefix";
                DROP INDEX gccs."IX_platform_customers_reference_prefix";
                DROP INDEX gccs."IX_platform_customers_owner_email_prefix";
                DROP INDEX gccs."IX_platform_customers_subscription_reference_prefix";
                """);

            migrationBuilder.DropIndex(
                name: "IX_tenants_name",
                schema: "gccs",
                table: "tenants");

            migrationBuilder.DropIndex(
                name: "IX_tenant_subscriptions_plan_status_ends_at",
                schema: "gccs",
                table: "tenant_subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_platform_tenant_onboardings_owner_email",
                schema: "gccs",
                table: "platform_tenant_onboardings");

            migrationBuilder.DropIndex(
                name: "IX_platform_tenant_onboardings_status_onboarding_type_created_~",
                schema: "gccs",
                table: "platform_tenant_onboardings");
        }
    }
}

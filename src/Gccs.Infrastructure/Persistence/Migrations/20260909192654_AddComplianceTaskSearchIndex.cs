using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddComplianceTaskSearchIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS "IX_compliance_tasks_tenant_id_assigned_to_user_id_due_at_id"
                ON gccs.compliance_tasks (tenant_id, assigned_to_user_id, due_at, id);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS gccs."IX_compliance_tasks_tenant_id_assigned_to_user_id_due_at_id";
                """);
        }
    }
}

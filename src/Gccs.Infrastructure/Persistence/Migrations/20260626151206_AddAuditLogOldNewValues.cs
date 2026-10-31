using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLogOldNewValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "new_value",
                schema: "gccs",
                table: "audit_log_entries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "old_value",
                schema: "gccs",
                table: "audit_log_entries",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "new_value",
                schema: "gccs",
                table: "audit_log_entries");

            migrationBuilder.DropColumn(
                name: "old_value",
                schema: "gccs",
                table: "audit_log_entries");
        }
    }
}

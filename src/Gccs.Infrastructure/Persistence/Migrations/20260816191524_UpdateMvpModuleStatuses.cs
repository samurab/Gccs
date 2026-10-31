using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMvpModuleStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "gccs",
                table: "mvp_modules",
                keyColumn: "key",
                keyValue: "calendar",
                column: "status",
                value: "active");

            migrationBuilder.UpdateData(
                schema: "gccs",
                table: "mvp_modules",
                keyColumn: "key",
                keyValue: "cmmc",
                column: "status",
                value: "active");

            migrationBuilder.UpdateData(
                schema: "gccs",
                table: "mvp_modules",
                keyColumn: "key",
                keyValue: "company-profile",
                column: "status",
                value: "active");

            migrationBuilder.UpdateData(
                schema: "gccs",
                table: "mvp_modules",
                keyColumn: "key",
                keyValue: "evidence-vault",
                column: "status",
                value: "active");

            migrationBuilder.UpdateData(
                schema: "gccs",
                table: "mvp_modules",
                keyColumn: "key",
                keyValue: "reports",
                column: "status",
                value: "active");

            migrationBuilder.UpdateData(
                schema: "gccs",
                table: "mvp_modules",
                keyColumn: "key",
                keyValue: "subcontractors",
                column: "status",
                value: "active");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "gccs",
                table: "mvp_modules",
                keyColumn: "key",
                keyValue: "calendar",
                column: "status",
                value: "planned");

            migrationBuilder.UpdateData(
                schema: "gccs",
                table: "mvp_modules",
                keyColumn: "key",
                keyValue: "cmmc",
                column: "status",
                value: "planned");

            migrationBuilder.UpdateData(
                schema: "gccs",
                table: "mvp_modules",
                keyColumn: "key",
                keyValue: "company-profile",
                column: "status",
                value: "planned");

            migrationBuilder.UpdateData(
                schema: "gccs",
                table: "mvp_modules",
                keyColumn: "key",
                keyValue: "evidence-vault",
                column: "status",
                value: "planned");

            migrationBuilder.UpdateData(
                schema: "gccs",
                table: "mvp_modules",
                keyColumn: "key",
                keyValue: "reports",
                column: "status",
                value: "planned");

            migrationBuilder.UpdateData(
                schema: "gccs",
                table: "mvp_modules",
                keyColumn: "key",
                keyValue: "subcontractors",
                column: "status",
                value: "planned");
        }
    }
}

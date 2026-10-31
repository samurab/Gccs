using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDemoRequestPreferredStartCalendarIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_demo_requests_preferred_start_at",
                schema: "gccs",
                table: "demo_requests",
                column: "preferred_start_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_demo_requests_preferred_start_at",
                schema: "gccs",
                table: "demo_requests");
        }
    }
}

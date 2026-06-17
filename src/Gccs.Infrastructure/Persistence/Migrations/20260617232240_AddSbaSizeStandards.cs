using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSbaSizeStandards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sba_size_standards",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    naics_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    metric = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    threshold = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    unit = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    source_url = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    effective_at = table.Column<DateOnly>(type: "date", nullable: false),
                    last_reviewed_at = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "Draft"),
                    reviewed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sba_size_standards", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sba_size_standards_naics_code_status",
                schema: "gccs",
                table: "sba_size_standards",
                columns: new[] { "naics_code", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sba_size_standards",
                schema: "gccs");
        }
    }
}

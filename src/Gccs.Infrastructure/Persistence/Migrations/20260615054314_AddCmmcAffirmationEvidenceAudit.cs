using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCmmcAffirmationEvidenceAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_at",
                schema: "gccs",
                table: "annual_affirmations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_user_id",
                schema: "gccs",
                table: "annual_affirmations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "evidence_item_ids_json",
                schema: "gccs",
                table: "annual_affirmations",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                schema: "gccs",
                table: "annual_affirmations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by_user_id",
                schema: "gccs",
                table: "annual_affirmations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_annual_affirmations_created_at_updated_at",
                schema: "gccs",
                table: "annual_affirmations",
                columns: new[] { "created_at", "updated_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_annual_affirmations_created_at_updated_at",
                schema: "gccs",
                table: "annual_affirmations");

            migrationBuilder.DropColumn(
                name: "created_at",
                schema: "gccs",
                table: "annual_affirmations");

            migrationBuilder.DropColumn(
                name: "created_by_user_id",
                schema: "gccs",
                table: "annual_affirmations");

            migrationBuilder.DropColumn(
                name: "evidence_item_ids_json",
                schema: "gccs",
                table: "annual_affirmations");

            migrationBuilder.DropColumn(
                name: "updated_at",
                schema: "gccs",
                table: "annual_affirmations");

            migrationBuilder.DropColumn(
                name: "updated_by_user_id",
                schema: "gccs",
                table: "annual_affirmations");
        }
    }
}

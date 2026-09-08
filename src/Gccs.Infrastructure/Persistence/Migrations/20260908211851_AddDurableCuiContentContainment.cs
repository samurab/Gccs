using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDurableCuiContentContainment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_use_blocked",
                schema: "gccs",
                table: "reports",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "use_blocked_at",
                schema: "gccs",
                table: "reports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_use_blocked",
                schema: "gccs",
                table: "extraction_jobs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "use_blocked_at",
                schema: "gccs",
                table: "extraction_jobs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_use_blocked",
                schema: "gccs",
                table: "evidence_items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "use_blocked_at",
                schema: "gccs",
                table: "evidence_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_use_blocked",
                schema: "gccs",
                table: "evidence_file_versions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "use_blocked_at",
                schema: "gccs",
                table: "evidence_file_versions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_use_blocked",
                schema: "gccs",
                table: "contract_documents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "use_blocked_at",
                schema: "gccs",
                table: "contract_documents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_use_blocked",
                schema: "gccs",
                table: "classified_notes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "use_blocked_at",
                schema: "gccs",
                table: "classified_notes",
                type: "timestamp with time zone",
                nullable: true);

            foreach (var (table, entityType) in new[]
            {
                ("evidence_items", "EvidenceItem"), ("evidence_file_versions", "EvidenceFileVersion"),
                ("classified_notes", "ClassifiedNote"), ("contract_documents", "ContractDocument"),
                ("extraction_jobs", "ExtractionJob"), ("reports", "Report")
            })
            {
                migrationBuilder.Sql($$"""
                    UPDATE gccs.{{table}} AS content
                    SET is_use_blocked = TRUE, use_blocked_at = active.first_blocked_at
                    FROM (SELECT affected_entity_id::uuid AS id, MIN(created_at) AS first_blocked_at
                          FROM gccs.cui_support_escalations
                          WHERE affected_entity_type = '{{entityType}}' AND is_affected_content_blocked
                          GROUP BY affected_entity_id) AS active
                    WHERE content.id = active.id;
                    """);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_use_blocked",
                schema: "gccs",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "use_blocked_at",
                schema: "gccs",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "is_use_blocked",
                schema: "gccs",
                table: "extraction_jobs");

            migrationBuilder.DropColumn(
                name: "use_blocked_at",
                schema: "gccs",
                table: "extraction_jobs");

            migrationBuilder.DropColumn(
                name: "is_use_blocked",
                schema: "gccs",
                table: "evidence_items");

            migrationBuilder.DropColumn(
                name: "use_blocked_at",
                schema: "gccs",
                table: "evidence_items");

            migrationBuilder.DropColumn(
                name: "is_use_blocked",
                schema: "gccs",
                table: "evidence_file_versions");

            migrationBuilder.DropColumn(
                name: "use_blocked_at",
                schema: "gccs",
                table: "evidence_file_versions");

            migrationBuilder.DropColumn(
                name: "is_use_blocked",
                schema: "gccs",
                table: "contract_documents");

            migrationBuilder.DropColumn(
                name: "use_blocked_at",
                schema: "gccs",
                table: "contract_documents");

            migrationBuilder.DropColumn(
                name: "is_use_blocked",
                schema: "gccs",
                table: "classified_notes");

            migrationBuilder.DropColumn(
                name: "use_blocked_at",
                schema: "gccs",
                table: "classified_notes");
        }
    }
}

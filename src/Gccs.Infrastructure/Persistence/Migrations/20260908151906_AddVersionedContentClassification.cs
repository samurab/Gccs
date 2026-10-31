using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVersionedContentClassification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "classification_revision",
                schema: "gccs",
                table: "reports",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "classification_revision",
                schema: "gccs",
                table: "extraction_jobs",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "classification_revision",
                schema: "gccs",
                table: "evidence_items",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "classification_revision",
                schema: "gccs",
                table: "evidence_file_versions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "classification_revision",
                schema: "gccs",
                table: "contract_documents",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "previous_metadata_json",
                schema: "gccs",
                table: "content_classification_history",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "revision",
                schema: "gccs",
                table: "content_classification_history",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "classification_revision",
                schema: "gccs",
                table: "classified_notes",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "report_classifications",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    classification_revision = table.Column<long>(type: "bigint", nullable: false),
                    classification = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    classification_source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    classification_confidence = table.Column<decimal>(type: "numeric", nullable: true),
                    classification_reviewed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    classification_reviewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    classification_reason = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: true),
                    classification_is_approved_demo_content = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_classifications", x => x.id);
                    table.ForeignKey(
                        name: "FK_report_classifications_reports_id",
                        column: x => x.id,
                        principalSchema: "gccs",
                        principalTable: "reports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM gccs.report_classifications) OR
                       EXISTS (SELECT 1 FROM gccs.content_classification_history WHERE previous_metadata_json IS NOT NULL OR revision IS NOT NULL) THEN
                        RAISE EXCEPTION 'Classification review data exists; preserve it and approve a data-preserving rollback before removing this schema.';
                    END IF;
                END $$;
                """);
            migrationBuilder.DropTable(
                name: "report_classifications",
                schema: "gccs");

            migrationBuilder.DropColumn(
                name: "classification_revision",
                schema: "gccs",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "classification_revision",
                schema: "gccs",
                table: "extraction_jobs");

            migrationBuilder.DropColumn(
                name: "classification_revision",
                schema: "gccs",
                table: "evidence_items");

            migrationBuilder.DropColumn(
                name: "classification_revision",
                schema: "gccs",
                table: "evidence_file_versions");

            migrationBuilder.DropColumn(
                name: "classification_revision",
                schema: "gccs",
                table: "contract_documents");

            migrationBuilder.DropColumn(
                name: "previous_metadata_json",
                schema: "gccs",
                table: "content_classification_history");

            migrationBuilder.DropColumn(
                name: "revision",
                schema: "gccs",
                table: "content_classification_history");

            migrationBuilder.DropColumn(
                name: "classification_revision",
                schema: "gccs",
                table: "classified_notes");
        }
    }
}

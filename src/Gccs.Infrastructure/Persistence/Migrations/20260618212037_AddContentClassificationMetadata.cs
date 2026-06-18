using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContentClassificationMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "classification",
                schema: "gccs",
                table: "reports",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "classification_confidence",
                schema: "gccs",
                table: "reports",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "classification_is_approved_demo_content",
                schema: "gccs",
                table: "reports",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "classification_reason",
                schema: "gccs",
                table: "reports",
                type: "character varying(600)",
                maxLength: 600,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "classification_reviewed_at",
                schema: "gccs",
                table: "reports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "classification_reviewed_by_user_id",
                schema: "gccs",
                table: "reports",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "classification_source",
                schema: "gccs",
                table: "reports",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "classification",
                schema: "gccs",
                table: "extraction_jobs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "classification_confidence",
                schema: "gccs",
                table: "extraction_jobs",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "classification_is_approved_demo_content",
                schema: "gccs",
                table: "extraction_jobs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "classification_reason",
                schema: "gccs",
                table: "extraction_jobs",
                type: "character varying(600)",
                maxLength: 600,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "classification_reviewed_at",
                schema: "gccs",
                table: "extraction_jobs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "classification_reviewed_by_user_id",
                schema: "gccs",
                table: "extraction_jobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "classification_source",
                schema: "gccs",
                table: "extraction_jobs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "classification",
                schema: "gccs",
                table: "evidence_items",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "classification_confidence",
                schema: "gccs",
                table: "evidence_items",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "classification_is_approved_demo_content",
                schema: "gccs",
                table: "evidence_items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "classification_reason",
                schema: "gccs",
                table: "evidence_items",
                type: "character varying(600)",
                maxLength: 600,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "classification_reviewed_at",
                schema: "gccs",
                table: "evidence_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "classification_reviewed_by_user_id",
                schema: "gccs",
                table: "evidence_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "classification_source",
                schema: "gccs",
                table: "evidence_items",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "classification",
                schema: "gccs",
                table: "evidence_file_versions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "classification_confidence",
                schema: "gccs",
                table: "evidence_file_versions",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "classification_is_approved_demo_content",
                schema: "gccs",
                table: "evidence_file_versions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "classification_reason",
                schema: "gccs",
                table: "evidence_file_versions",
                type: "character varying(600)",
                maxLength: 600,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "classification_reviewed_at",
                schema: "gccs",
                table: "evidence_file_versions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "classification_reviewed_by_user_id",
                schema: "gccs",
                table: "evidence_file_versions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "classification_source",
                schema: "gccs",
                table: "evidence_file_versions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "classification",
                schema: "gccs",
                table: "contract_documents",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "classification_confidence",
                schema: "gccs",
                table: "contract_documents",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "classification_is_approved_demo_content",
                schema: "gccs",
                table: "contract_documents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "classification_reason",
                schema: "gccs",
                table: "contract_documents",
                type: "character varying(600)",
                maxLength: 600,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "classification_reviewed_at",
                schema: "gccs",
                table: "contract_documents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "classification_reviewed_by_user_id",
                schema: "gccs",
                table: "contract_documents",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "classification_source",
                schema: "gccs",
                table: "contract_documents",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "content_classification_history",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    entity_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    previous_classification = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    new_classification = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    confidence = table.Column<decimal>(type: "numeric", nullable: true),
                    reviewed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reason = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: true),
                    changed_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_classification_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_content_classification_history_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_content_classification_history_tenant_id_entity_type_entity~",
                schema: "gccs",
                table: "content_classification_history",
                columns: new[] { "tenant_id", "entity_type", "entity_id", "changed_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "content_classification_history",
                schema: "gccs");

            migrationBuilder.DropColumn(
                name: "classification",
                schema: "gccs",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "classification_confidence",
                schema: "gccs",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "classification_is_approved_demo_content",
                schema: "gccs",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "classification_reason",
                schema: "gccs",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "classification_reviewed_at",
                schema: "gccs",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "classification_reviewed_by_user_id",
                schema: "gccs",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "classification_source",
                schema: "gccs",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "classification",
                schema: "gccs",
                table: "extraction_jobs");

            migrationBuilder.DropColumn(
                name: "classification_confidence",
                schema: "gccs",
                table: "extraction_jobs");

            migrationBuilder.DropColumn(
                name: "classification_is_approved_demo_content",
                schema: "gccs",
                table: "extraction_jobs");

            migrationBuilder.DropColumn(
                name: "classification_reason",
                schema: "gccs",
                table: "extraction_jobs");

            migrationBuilder.DropColumn(
                name: "classification_reviewed_at",
                schema: "gccs",
                table: "extraction_jobs");

            migrationBuilder.DropColumn(
                name: "classification_reviewed_by_user_id",
                schema: "gccs",
                table: "extraction_jobs");

            migrationBuilder.DropColumn(
                name: "classification_source",
                schema: "gccs",
                table: "extraction_jobs");

            migrationBuilder.DropColumn(
                name: "classification",
                schema: "gccs",
                table: "evidence_items");

            migrationBuilder.DropColumn(
                name: "classification_confidence",
                schema: "gccs",
                table: "evidence_items");

            migrationBuilder.DropColumn(
                name: "classification_is_approved_demo_content",
                schema: "gccs",
                table: "evidence_items");

            migrationBuilder.DropColumn(
                name: "classification_reason",
                schema: "gccs",
                table: "evidence_items");

            migrationBuilder.DropColumn(
                name: "classification_reviewed_at",
                schema: "gccs",
                table: "evidence_items");

            migrationBuilder.DropColumn(
                name: "classification_reviewed_by_user_id",
                schema: "gccs",
                table: "evidence_items");

            migrationBuilder.DropColumn(
                name: "classification_source",
                schema: "gccs",
                table: "evidence_items");

            migrationBuilder.DropColumn(
                name: "classification",
                schema: "gccs",
                table: "evidence_file_versions");

            migrationBuilder.DropColumn(
                name: "classification_confidence",
                schema: "gccs",
                table: "evidence_file_versions");

            migrationBuilder.DropColumn(
                name: "classification_is_approved_demo_content",
                schema: "gccs",
                table: "evidence_file_versions");

            migrationBuilder.DropColumn(
                name: "classification_reason",
                schema: "gccs",
                table: "evidence_file_versions");

            migrationBuilder.DropColumn(
                name: "classification_reviewed_at",
                schema: "gccs",
                table: "evidence_file_versions");

            migrationBuilder.DropColumn(
                name: "classification_reviewed_by_user_id",
                schema: "gccs",
                table: "evidence_file_versions");

            migrationBuilder.DropColumn(
                name: "classification_source",
                schema: "gccs",
                table: "evidence_file_versions");

            migrationBuilder.DropColumn(
                name: "classification",
                schema: "gccs",
                table: "contract_documents");

            migrationBuilder.DropColumn(
                name: "classification_confidence",
                schema: "gccs",
                table: "contract_documents");

            migrationBuilder.DropColumn(
                name: "classification_is_approved_demo_content",
                schema: "gccs",
                table: "contract_documents");

            migrationBuilder.DropColumn(
                name: "classification_reason",
                schema: "gccs",
                table: "contract_documents");

            migrationBuilder.DropColumn(
                name: "classification_reviewed_at",
                schema: "gccs",
                table: "contract_documents");

            migrationBuilder.DropColumn(
                name: "classification_reviewed_by_user_id",
                schema: "gccs",
                table: "contract_documents");

            migrationBuilder.DropColumn(
                name: "classification_source",
                schema: "gccs",
                table: "contract_documents");
        }
    }
}

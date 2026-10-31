using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGeneratedPolicyClassification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "classification",
                schema: "gccs",
                table: "policy_revisions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "Unknown");

            migrationBuilder.AddColumn<decimal>(
                name: "classification_confidence",
                schema: "gccs",
                table: "policy_revisions",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "classification_is_approved_demo_content",
                schema: "gccs",
                table: "policy_revisions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "classification_reason",
                schema: "gccs",
                table: "policy_revisions",
                type: "character varying(600)",
                maxLength: 600,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "classification_reviewed_at",
                schema: "gccs",
                table: "policy_revisions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "classification_reviewed_by_user_id",
                schema: "gccs",
                table: "policy_revisions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "classification_revision",
                schema: "gccs",
                table: "policy_revisions",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<string>(
                name: "classification_source",
                schema: "gccs",
                table: "policy_revisions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "SystemSuggested");

            migrationBuilder.AddColumn<string>(
                name: "classification",
                schema: "gccs",
                table: "generated_policies",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "Unknown");

            migrationBuilder.AddColumn<decimal>(
                name: "classification_confidence",
                schema: "gccs",
                table: "generated_policies",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "classification_is_approved_demo_content",
                schema: "gccs",
                table: "generated_policies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "classification_reason",
                schema: "gccs",
                table: "generated_policies",
                type: "character varying(600)",
                maxLength: 600,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "classification_reviewed_at",
                schema: "gccs",
                table: "generated_policies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "classification_reviewed_by_user_id",
                schema: "gccs",
                table: "generated_policies",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "classification_revision",
                schema: "gccs",
                table: "generated_policies",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            migrationBuilder.AddColumn<string>(
                name: "classification_source",
                schema: "gccs",
                table: "generated_policies",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "SystemSuggested");

            migrationBuilder.Sql(
                """
                UPDATE gccs.evidence_items AS evidence
                SET classification = 'Unknown',
                    classification_source = 'SystemSuggested',
                    classification_reason = 'Legacy generated-policy evidence requires classification review before governed reuse.',
                    classification_is_approved_demo_content = FALSE,
                    classification_revision = CASE WHEN classification_revision < 1 THEN 1 ELSE classification_revision + 1 END
                WHERE evidence.id IN (
                    SELECT policy.evidence_item_id
                    FROM gccs.generated_policies AS policy
                    WHERE policy.evidence_item_id IS NOT NULL
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "classification",
                schema: "gccs",
                table: "policy_revisions");

            migrationBuilder.DropColumn(
                name: "classification_confidence",
                schema: "gccs",
                table: "policy_revisions");

            migrationBuilder.DropColumn(
                name: "classification_is_approved_demo_content",
                schema: "gccs",
                table: "policy_revisions");

            migrationBuilder.DropColumn(
                name: "classification_reason",
                schema: "gccs",
                table: "policy_revisions");

            migrationBuilder.DropColumn(
                name: "classification_reviewed_at",
                schema: "gccs",
                table: "policy_revisions");

            migrationBuilder.DropColumn(
                name: "classification_reviewed_by_user_id",
                schema: "gccs",
                table: "policy_revisions");

            migrationBuilder.DropColumn(
                name: "classification_revision",
                schema: "gccs",
                table: "policy_revisions");

            migrationBuilder.DropColumn(
                name: "classification_source",
                schema: "gccs",
                table: "policy_revisions");

            migrationBuilder.DropColumn(
                name: "classification",
                schema: "gccs",
                table: "generated_policies");

            migrationBuilder.DropColumn(
                name: "classification_confidence",
                schema: "gccs",
                table: "generated_policies");

            migrationBuilder.DropColumn(
                name: "classification_is_approved_demo_content",
                schema: "gccs",
                table: "generated_policies");

            migrationBuilder.DropColumn(
                name: "classification_reason",
                schema: "gccs",
                table: "generated_policies");

            migrationBuilder.DropColumn(
                name: "classification_reviewed_at",
                schema: "gccs",
                table: "generated_policies");

            migrationBuilder.DropColumn(
                name: "classification_reviewed_by_user_id",
                schema: "gccs",
                table: "generated_policies");

            migrationBuilder.DropColumn(
                name: "classification_revision",
                schema: "gccs",
                table: "generated_policies");

            migrationBuilder.DropColumn(
                name: "classification_source",
                schema: "gccs",
                table: "generated_policies");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAiOutputLoggingAndReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "classification",
                schema: "gccs",
                table: "assistant_answers",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "Unclassified");

            migrationBuilder.AddColumn<string>(
                name: "model_configuration_json",
                schema: "gccs",
                table: "assistant_answers",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<string>(
                name: "prompt",
                schema: "gccs",
                table: "assistant_answers",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "[legacy prompt unavailable]");

            migrationBuilder.AddColumn<string>(
                name: "prompt_metadata_json",
                schema: "gccs",
                table: "assistant_answers",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<bool>(
                name: "prompt_was_redacted",
                schema: "gccs",
                table: "assistant_answers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "rejection_reason",
                schema: "gccs",
                table: "assistant_answers",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "result",
                schema: "gccs",
                table: "assistant_answers",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "retain_until",
                schema: "gccs",
                table: "assistant_answers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP + INTERVAL '365 days'");

            migrationBuilder.AddColumn<string>(
                name: "retrieval_policy_json",
                schema: "gccs",
                table: "assistant_answers",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<int>(
                name: "review_state",
                schema: "gccs",
                table: "assistant_answers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "version",
                schema: "gccs",
                table: "assistant_answers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "assistant_output_reviews",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    answer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    previous_state = table.Column<int>(type: "integer", nullable: false),
                    new_state = table.Column<int>(type: "integer", nullable: false),
                    reviewer_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assistant_output_reviews", x => x.id);
                    table.ForeignKey(
                        name: "FK_assistant_output_reviews_assistant_answers_tenant_id_answer~",
                        columns: x => new { x.tenant_id, x.answer_id },
                        principalSchema: "gccs",
                        principalTable: "assistant_answers",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_assistant_output_reviews_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql("""
                UPDATE gccs.assistant_answers
                SET result = status,
                    prompt = '[legacy prompt unavailable]',
                    prompt_was_redacted = TRUE,
                    retain_until = created_at + INTERVAL '365 days'
                WHERE result = '';
                """);

            migrationBuilder.CreateTable(
                name: "assistant_output_usages",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    answer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    deliverable_type = table.Column<int>(type: "integer", nullable: false),
                    deliverable_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    linked_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    linked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assistant_output_usages", x => x.id);
                    table.ForeignKey(
                        name: "FK_assistant_output_usages_assistant_answers_tenant_id_answer_~",
                        columns: x => new { x.tenant_id, x.answer_id },
                        principalSchema: "gccs",
                        principalTable: "assistant_answers",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_assistant_output_usages_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_assistant_output_reviews_tenant_id_answer_id_created_at",
                schema: "gccs",
                table: "assistant_output_reviews",
                columns: new[] { "tenant_id", "answer_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_assistant_output_usages_tenant_id_answer_id",
                schema: "gccs",
                table: "assistant_output_usages",
                columns: new[] { "tenant_id", "answer_id" });

            migrationBuilder.CreateIndex(
                name: "IX_assistant_output_usages_tenant_id_deliverable_type_delivera~",
                schema: "gccs",
                table: "assistant_output_usages",
                columns: new[] { "tenant_id", "deliverable_type", "deliverable_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "assistant_output_reviews",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "assistant_output_usages",
                schema: "gccs");

            migrationBuilder.DropColumn(
                name: "classification",
                schema: "gccs",
                table: "assistant_answers");

            migrationBuilder.DropColumn(
                name: "model_configuration_json",
                schema: "gccs",
                table: "assistant_answers");

            migrationBuilder.DropColumn(
                name: "prompt",
                schema: "gccs",
                table: "assistant_answers");

            migrationBuilder.DropColumn(
                name: "prompt_metadata_json",
                schema: "gccs",
                table: "assistant_answers");

            migrationBuilder.DropColumn(
                name: "prompt_was_redacted",
                schema: "gccs",
                table: "assistant_answers");

            migrationBuilder.DropColumn(
                name: "rejection_reason",
                schema: "gccs",
                table: "assistant_answers");

            migrationBuilder.DropColumn(
                name: "result",
                schema: "gccs",
                table: "assistant_answers");

            migrationBuilder.DropColumn(
                name: "retain_until",
                schema: "gccs",
                table: "assistant_answers");

            migrationBuilder.DropColumn(
                name: "retrieval_policy_json",
                schema: "gccs",
                table: "assistant_answers");

            migrationBuilder.DropColumn(
                name: "review_state",
                schema: "gccs",
                table: "assistant_answers");

            migrationBuilder.DropColumn(
                name: "version",
                schema: "gccs",
                table: "assistant_answers");
        }
    }
}

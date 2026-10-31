using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGuardedAssistantExperience : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "assistant_answers",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workflow_context = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    answer = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: false),
                    citations_json = table.Column<string>(type: "jsonb", nullable: false),
                    support_status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    draft_label = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    requires_review = table.Column<bool>(type: "boolean", nullable: false),
                    escalation_recommended = table.Column<bool>(type: "boolean", nullable: false),
                    blocked_reason = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    human_review_status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    reviewed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    review_decision = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    review_notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assistant_answers", x => x.id);
                    table.UniqueConstraint("AK_assistant_answers_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "FK_assistant_answers_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "assistant_draft_actions",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    answer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    title = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assistant_draft_actions", x => x.id);
                    table.ForeignKey(
                        name: "FK_assistant_draft_actions_assistant_answers_tenant_id_answer_~",
                        columns: x => new { x.tenant_id, x.answer_id },
                        principalSchema: "gccs",
                        principalTable: "assistant_answers",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_assistant_draft_actions_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "assistant_feedback",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    answer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    feedback_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assistant_feedback", x => x.id);
                    table.ForeignKey(
                        name: "FK_assistant_feedback_assistant_answers_tenant_id_answer_id",
                        columns: x => new { x.tenant_id, x.answer_id },
                        principalSchema: "gccs",
                        principalTable: "assistant_answers",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_assistant_feedback_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_assistant_answers_tenant_id_created_at",
                schema: "gccs",
                table: "assistant_answers",
                columns: new[] { "tenant_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_assistant_draft_actions_tenant_id_answer_id_created_at",
                schema: "gccs",
                table: "assistant_draft_actions",
                columns: new[] { "tenant_id", "answer_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_assistant_feedback_tenant_id_answer_id_created_at",
                schema: "gccs",
                table: "assistant_feedback",
                columns: new[] { "tenant_id", "answer_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_expert_review_items_tenant_id_source_type_source_id",
                schema: "gccs",
                table: "expert_review_items",
                columns: new[] { "tenant_id", "source_type", "source_id" },
                unique: true,
                filter: "status = 'open' AND source_type = 'assistant_answer'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_expert_review_items_tenant_id_source_type_source_id",
                schema: "gccs",
                table: "expert_review_items");

            migrationBuilder.DropTable(
                name: "assistant_draft_actions",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "assistant_feedback",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "assistant_answers",
                schema: "gccs");

        }
    }
}

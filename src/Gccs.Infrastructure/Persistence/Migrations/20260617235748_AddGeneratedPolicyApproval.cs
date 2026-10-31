using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGeneratedPolicyApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "approved_at",
                schema: "gccs",
                table: "generated_policies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "approved_by_user_id",
                schema: "gccs",
                table: "generated_policies",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "evidence_item_id",
                schema: "gccs",
                table: "generated_policies",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "review_due_at",
                schema: "gccs",
                table: "generated_policies",
                type: "date",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "policy_revisions",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    generated_policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    preserved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    preserved_by_user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_policy_revisions", x => x.id);
                    table.ForeignKey(
                        name: "FK_policy_revisions_generated_policies_generated_policy_id",
                        column: x => x.generated_policy_id,
                        principalSchema: "gccs",
                        principalTable: "generated_policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_generated_policies_evidence_item_id",
                schema: "gccs",
                table: "generated_policies",
                column: "evidence_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_policy_revisions_generated_policy_id_preserved_at",
                schema: "gccs",
                table: "policy_revisions",
                columns: new[] { "generated_policy_id", "preserved_at" });

            migrationBuilder.AddForeignKey(
                name: "FK_generated_policies_evidence_items_evidence_item_id",
                schema: "gccs",
                table: "generated_policies",
                column: "evidence_item_id",
                principalSchema: "gccs",
                principalTable: "evidence_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_generated_policies_evidence_items_evidence_item_id",
                schema: "gccs",
                table: "generated_policies");

            migrationBuilder.DropTable(
                name: "policy_revisions",
                schema: "gccs");

            migrationBuilder.DropIndex(
                name: "IX_generated_policies_evidence_item_id",
                schema: "gccs",
                table: "generated_policies");

            migrationBuilder.DropColumn(
                name: "approved_at",
                schema: "gccs",
                table: "generated_policies");

            migrationBuilder.DropColumn(
                name: "approved_by_user_id",
                schema: "gccs",
                table: "generated_policies");

            migrationBuilder.DropColumn(
                name: "evidence_item_id",
                schema: "gccs",
                table: "generated_policies");

            migrationBuilder.DropColumn(
                name: "review_due_at",
                schema: "gccs",
                table: "generated_policies");
        }
    }
}

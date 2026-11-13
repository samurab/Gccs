using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDurableObjectWriteIntents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "object_write_intent_id",
                schema: "gccs",
                table: "report_exports",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "expected_e_tag",
                schema: "gccs",
                table: "object_cleanup",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "object_write_intent_id",
                schema: "gccs",
                table: "object_cleanup",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "object_write_intent_id",
                schema: "gccs",
                table: "evidence_file_versions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "object_write_intent_tenant_id",
                schema: "gccs",
                table: "evidence_file_versions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "object_write_intent_id",
                schema: "gccs",
                table: "contract_documents",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "object_write_intent_tenant_id",
                schema: "gccs",
                table: "contract_documents",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "object_write_intents",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    container = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    object_name = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    state = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    generation = table.Column<long>(type: "bigint", nullable: false),
                    lease_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    resource_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                    resource_lease_id = table.Column<Guid>(type: "uuid", nullable: true),
                    e_tag = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    result_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_object_write_intents", x => x.id);
                    table.UniqueConstraint("AK_object_write_intents_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "FK_object_write_intents_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_report_exports_tenant_id_object_write_intent_id",
                schema: "gccs",
                table: "report_exports",
                columns: new[] { "tenant_id", "object_write_intent_id" });

            migrationBuilder.CreateIndex(
                name: "IX_object_cleanup_object_write_intent_id",
                schema: "gccs",
                table: "object_cleanup",
                column: "object_write_intent_id",
                unique: true,
                filter: "object_write_intent_id IS NOT NULL AND completed_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_object_cleanup_tenant_id_object_write_intent_id",
                schema: "gccs",
                table: "object_cleanup",
                columns: new[] { "tenant_id", "object_write_intent_id" });

            migrationBuilder.CreateIndex(
                name: "IX_evidence_file_versions_object_write_intent_tenant_id_eviden~",
                schema: "gccs",
                table: "evidence_file_versions",
                columns: new[] { "object_write_intent_tenant_id", "evidence_item_id" });

            migrationBuilder.CreateIndex(
                name: "IX_evidence_file_versions_object_write_intent_tenant_id_object~",
                schema: "gccs",
                table: "evidence_file_versions",
                columns: new[] { "object_write_intent_tenant_id", "object_write_intent_id" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_evidence_file_versions_intent_pair",
                schema: "gccs",
                table: "evidence_file_versions",
                sql: "(object_write_intent_id IS NULL) = (object_write_intent_tenant_id IS NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_contract_documents_object_write_intent_tenant_id_contract_id",
                schema: "gccs",
                table: "contract_documents",
                columns: new[] { "object_write_intent_tenant_id", "contract_id" });

            migrationBuilder.CreateIndex(
                name: "IX_contract_documents_object_write_intent_tenant_id_object_wri~",
                schema: "gccs",
                table: "contract_documents",
                columns: new[] { "object_write_intent_tenant_id", "object_write_intent_id" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_contract_documents_intent_pair",
                schema: "gccs",
                table: "contract_documents",
                sql: "(object_write_intent_id IS NULL) = (object_write_intent_tenant_id IS NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_object_write_intents_state_lease_until",
                schema: "gccs",
                table: "object_write_intents",
                columns: new[] { "state", "lease_until" });

            migrationBuilder.CreateIndex(
                name: "IX_object_write_intents_tenant_id_container_object_name",
                schema: "gccs",
                table: "object_write_intents",
                columns: new[] { "tenant_id", "container", "object_name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_contract_documents_contracts_object_write_intent_tenant_id_~",
                schema: "gccs",
                table: "contract_documents",
                columns: new[] { "object_write_intent_tenant_id", "contract_id" },
                principalSchema: "gccs",
                principalTable: "contracts",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_contract_documents_object_write_intents_object_write_intent~",
                schema: "gccs",
                table: "contract_documents",
                columns: new[] { "object_write_intent_tenant_id", "object_write_intent_id" },
                principalSchema: "gccs",
                principalTable: "object_write_intents",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_evidence_file_versions_evidence_items_object_write_intent_t~",
                schema: "gccs",
                table: "evidence_file_versions",
                columns: new[] { "object_write_intent_tenant_id", "evidence_item_id" },
                principalSchema: "gccs",
                principalTable: "evidence_items",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_evidence_file_versions_object_write_intents_object_write_in~",
                schema: "gccs",
                table: "evidence_file_versions",
                columns: new[] { "object_write_intent_tenant_id", "object_write_intent_id" },
                principalSchema: "gccs",
                principalTable: "object_write_intents",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_object_cleanup_object_write_intents_tenant_id_object_write_~",
                schema: "gccs",
                table: "object_cleanup",
                columns: new[] { "tenant_id", "object_write_intent_id" },
                principalSchema: "gccs",
                principalTable: "object_write_intents",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_report_exports_object_write_intents_tenant_id_object_write_~",
                schema: "gccs",
                table: "report_exports",
                columns: new[] { "tenant_id", "object_write_intent_id" },
                principalSchema: "gccs",
                principalTable: "object_write_intents",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_contract_documents_contracts_object_write_intent_tenant_id_~",
                schema: "gccs",
                table: "contract_documents");

            migrationBuilder.DropForeignKey(
                name: "FK_contract_documents_object_write_intents_object_write_intent~",
                schema: "gccs",
                table: "contract_documents");

            migrationBuilder.DropForeignKey(
                name: "FK_evidence_file_versions_evidence_items_object_write_intent_t~",
                schema: "gccs",
                table: "evidence_file_versions");

            migrationBuilder.DropForeignKey(
                name: "FK_evidence_file_versions_object_write_intents_object_write_in~",
                schema: "gccs",
                table: "evidence_file_versions");

            migrationBuilder.DropForeignKey(
                name: "FK_object_cleanup_object_write_intents_tenant_id_object_write_~",
                schema: "gccs",
                table: "object_cleanup");

            migrationBuilder.DropForeignKey(
                name: "FK_report_exports_object_write_intents_tenant_id_object_write_~",
                schema: "gccs",
                table: "report_exports");

            migrationBuilder.DropTable(
                name: "object_write_intents",
                schema: "gccs");

            migrationBuilder.DropIndex(
                name: "IX_report_exports_tenant_id_object_write_intent_id",
                schema: "gccs",
                table: "report_exports");

            migrationBuilder.DropIndex(
                name: "IX_object_cleanup_object_write_intent_id",
                schema: "gccs",
                table: "object_cleanup");

            migrationBuilder.DropIndex(
                name: "IX_object_cleanup_tenant_id_object_write_intent_id",
                schema: "gccs",
                table: "object_cleanup");

            migrationBuilder.DropIndex(
                name: "IX_evidence_file_versions_object_write_intent_tenant_id_eviden~",
                schema: "gccs",
                table: "evidence_file_versions");

            migrationBuilder.DropIndex(
                name: "IX_evidence_file_versions_object_write_intent_tenant_id_object~",
                schema: "gccs",
                table: "evidence_file_versions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_evidence_file_versions_intent_pair",
                schema: "gccs",
                table: "evidence_file_versions");

            migrationBuilder.DropIndex(
                name: "IX_contract_documents_object_write_intent_tenant_id_contract_id",
                schema: "gccs",
                table: "contract_documents");

            migrationBuilder.DropIndex(
                name: "IX_contract_documents_object_write_intent_tenant_id_object_wri~",
                schema: "gccs",
                table: "contract_documents");

            migrationBuilder.DropCheckConstraint(
                name: "ck_contract_documents_intent_pair",
                schema: "gccs",
                table: "contract_documents");

            migrationBuilder.DropColumn(
                name: "object_write_intent_id",
                schema: "gccs",
                table: "report_exports");

            migrationBuilder.DropColumn(
                name: "expected_e_tag",
                schema: "gccs",
                table: "object_cleanup");

            migrationBuilder.DropColumn(
                name: "object_write_intent_id",
                schema: "gccs",
                table: "object_cleanup");

            migrationBuilder.DropColumn(
                name: "object_write_intent_id",
                schema: "gccs",
                table: "evidence_file_versions");

            migrationBuilder.DropColumn(
                name: "object_write_intent_tenant_id",
                schema: "gccs",
                table: "evidence_file_versions");

            migrationBuilder.DropColumn(
                name: "object_write_intent_id",
                schema: "gccs",
                table: "contract_documents");

            migrationBuilder.DropColumn(
                name: "object_write_intent_tenant_id",
                schema: "gccs",
                table: "contract_documents");
        }
    }
}

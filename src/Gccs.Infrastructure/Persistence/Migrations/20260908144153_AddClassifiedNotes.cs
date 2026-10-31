using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClassifiedNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "classified_notes",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    body = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: false),
                    revision = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_classified_notes", x => x.id);
                    table.ForeignKey(
                        name: "FK_classified_notes_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_classified_notes_tenant_id_updated_at",
                schema: "gccs",
                table: "classified_notes",
                columns: new[] { "tenant_id", "updated_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM gccs.classified_notes) THEN
                        RAISE EXCEPTION 'Cannot remove classified notes containing customer records; export and approve a data-preserving rollback first.';
                    END IF;
                END $$;
                """);
            migrationBuilder.DropTable(
                name: "classified_notes",
                schema: "gccs");
        }
    }
}

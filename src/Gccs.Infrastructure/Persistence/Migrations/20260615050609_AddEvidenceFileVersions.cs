using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEvidenceFileVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "evidence_file_versions",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    evidence_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    file_name = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    content_type = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    validation_status = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    malware_scan_status = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    storage_uri = table.Column<string>(type: "text", nullable: true),
                    file_hash = table.Column<string>(type: "text", nullable: true),
                    uploaded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    uploaded_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evidence_file_versions", x => x.id);
                    table.ForeignKey(
                        name: "FK_evidence_file_versions_evidence_items_evidence_item_id",
                        column: x => x.evidence_item_id,
                        principalSchema: "gccs",
                        principalTable: "evidence_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_evidence_file_versions_evidence_item_id_version_number",
                schema: "gccs",
                table: "evidence_file_versions",
                columns: new[] { "evidence_item_id", "version_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "evidence_file_versions",
                schema: "gccs");
        }
    }
}

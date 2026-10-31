using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNormalizedCuiAuditDimensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "classification",
                schema: "gccs",
                table: "audit_log_entries",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "event_type",
                schema: "gccs",
                table: "audit_log_entries",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "mode",
                schema: "gccs",
                table: "audit_log_entries",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "result",
                schema: "gccs",
                table: "audit_log_entries",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE gccs.audit_log_entries
                SET classification = NULLIF(metadata_json ->> 'classification', ''),
                    mode = NULLIF(COALESCE(metadata_json ->> 'mode', metadata_json ->> 'afterDataHandlingMode', metadata_json ->> 'dataHandlingMode'), ''),
                    result = LOWER(COALESCE(NULLIF(metadata_json ->> 'result', ''), CASE WHEN action = 'Rejected' THEN 'rejected' ELSE 'succeeded' END)),
                    event_type = LOWER(TRIM(BOTH '-' FROM REGEXP_REPLACE(
                        COALESCE(NULLIF(metadata_json ->> 'eventType', ''), entity_type || '-' || action),
                        '[^A-Za-z0-9]+', '-', 'g')));
                """);

            migrationBuilder.AlterColumn<string>(
                name: "event_type",
                schema: "gccs",
                table: "audit_log_entries",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "result",
                schema: "gccs",
                table: "audit_log_entries",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_entries_tenant_id_event_type_occurred_at",
                schema: "gccs",
                table: "audit_log_entries",
                columns: new[] { "tenant_id", "event_type", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_entries_tenant_id_result_occurred_at",
                schema: "gccs",
                table: "audit_log_entries",
                columns: new[] { "tenant_id", "result", "occurred_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_audit_log_entries_tenant_id_event_type_occurred_at",
                schema: "gccs",
                table: "audit_log_entries");

            migrationBuilder.DropIndex(
                name: "IX_audit_log_entries_tenant_id_result_occurred_at",
                schema: "gccs",
                table: "audit_log_entries");

            migrationBuilder.DropColumn(
                name: "classification",
                schema: "gccs",
                table: "audit_log_entries");

            migrationBuilder.DropColumn(
                name: "event_type",
                schema: "gccs",
                table: "audit_log_entries");

            migrationBuilder.DropColumn(
                name: "mode",
                schema: "gccs",
                table: "audit_log_entries");

            migrationBuilder.DropColumn(
                name: "result",
                schema: "gccs",
                table: "audit_log_entries");
        }
    }
}

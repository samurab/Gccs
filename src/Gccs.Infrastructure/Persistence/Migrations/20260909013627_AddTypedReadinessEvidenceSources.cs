using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTypedReadinessEvidenceSources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "evidence_file_version_id",
                schema: "gccs",
                table: "incident_tabletops",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "evidence_source_type",
                schema: "gccs",
                table: "incident_tabletops",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Legacy");

            migrationBuilder.AddColumn<string>(
                name: "external_uri",
                schema: "gccs",
                table: "incident_tabletops",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sha256_digest",
                schema: "gccs",
                table: "incident_tabletops",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "evidence_file_version_id",
                schema: "gccs",
                table: "executed_control_evidence",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "evidence_source_type",
                schema: "gccs",
                table: "executed_control_evidence",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Legacy");

            migrationBuilder.AddColumn<string>(
                name: "external_uri",
                schema: "gccs",
                table: "executed_control_evidence",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sha256_digest",
                schema: "gccs",
                table: "executed_control_evidence",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_incident_tabletops_evidence_file_version_id",
                schema: "gccs",
                table: "incident_tabletops",
                column: "evidence_file_version_id");

            migrationBuilder.CreateIndex(
                name: "IX_executed_control_evidence_evidence_file_version_id",
                schema: "gccs",
                table: "executed_control_evidence",
                column: "evidence_file_version_id");

            migrationBuilder.AddForeignKey(
                name: "FK_executed_control_evidence_evidence_file_versions_evidence_f~",
                schema: "gccs",
                table: "executed_control_evidence",
                column: "evidence_file_version_id",
                principalSchema: "gccs",
                principalTable: "evidence_file_versions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_incident_tabletops_evidence_file_versions_evidence_file_ver~",
                schema: "gccs",
                table: "incident_tabletops",
                column: "evidence_file_version_id",
                principalSchema: "gccs",
                principalTable: "evidence_file_versions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_executed_control_evidence_evidence_file_versions_evidence_f~",
                schema: "gccs",
                table: "executed_control_evidence");

            migrationBuilder.DropForeignKey(
                name: "FK_incident_tabletops_evidence_file_versions_evidence_file_ver~",
                schema: "gccs",
                table: "incident_tabletops");

            migrationBuilder.DropIndex(
                name: "IX_incident_tabletops_evidence_file_version_id",
                schema: "gccs",
                table: "incident_tabletops");

            migrationBuilder.DropIndex(
                name: "IX_executed_control_evidence_evidence_file_version_id",
                schema: "gccs",
                table: "executed_control_evidence");

            migrationBuilder.DropColumn(
                name: "evidence_file_version_id",
                schema: "gccs",
                table: "incident_tabletops");

            migrationBuilder.DropColumn(
                name: "evidence_source_type",
                schema: "gccs",
                table: "incident_tabletops");

            migrationBuilder.DropColumn(
                name: "external_uri",
                schema: "gccs",
                table: "incident_tabletops");

            migrationBuilder.DropColumn(
                name: "sha256_digest",
                schema: "gccs",
                table: "incident_tabletops");

            migrationBuilder.DropColumn(
                name: "evidence_file_version_id",
                schema: "gccs",
                table: "executed_control_evidence");

            migrationBuilder.DropColumn(
                name: "evidence_source_type",
                schema: "gccs",
                table: "executed_control_evidence");

            migrationBuilder.DropColumn(
                name: "external_uri",
                schema: "gccs",
                table: "executed_control_evidence");

            migrationBuilder.DropColumn(
                name: "sha256_digest",
                schema: "gccs",
                table: "executed_control_evidence");
        }
    }
}

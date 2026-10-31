using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDurableFedRampReadiness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fedramp_control_mappings",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    control_id = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    family = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    baseline = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    owner = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    implementation_status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    implementation_summary = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: false),
                    inherited_provider = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    gap_rationale = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    source_reference = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    review_state = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    reviewer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    review_date = table.Column<DateOnly>(type: "date", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fedramp_control_mappings", x => x.id);
                    table.UniqueConstraint("AK_fedramp_control_mappings_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "FK_fedramp_control_mappings_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fedramp_readiness_packages",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    generated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    package_version = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    scope = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    environment = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    reviewer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    authorization_language = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    gaps_json = table.Column<string>(type: "jsonb", nullable: false),
                    accepted_risks_json = table.Column<string>(type: "jsonb", nullable: false),
                    readiness_summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    last_actor = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    shared_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fedramp_readiness_packages", x => x.id);
                    table.UniqueConstraint("AK_fedramp_readiness_packages_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "FK_fedramp_readiness_packages_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fedramp_control_mapping_history",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mapping_id = table.Column<Guid>(type: "uuid", nullable: false),
                    previous_state = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    new_state = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    reviewer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    review_date = table.Column<DateOnly>(type: "date", nullable: false),
                    review_notes = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: false),
                    changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    changed_by_user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fedramp_control_mapping_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_fedramp_control_mapping_history_fedramp_control_mappings_te~",
                        columns: x => new { x.tenant_id, x.mapping_id },
                        principalSchema: "gccs",
                        principalTable: "fedramp_control_mappings",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_fedramp_control_mapping_history_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fedramp_evidence_links",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mapping_id = table.Column<Guid>(type: "uuid", nullable: false),
                    label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    reference = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    evidence_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fedramp_evidence_links", x => x.id);
                    table.ForeignKey(
                        name: "FK_fedramp_evidence_links_fedramp_control_mappings_tenant_id_m~",
                        columns: x => new { x.tenant_id, x.mapping_id },
                        principalSchema: "gccs",
                        principalTable: "fedramp_control_mappings",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_fedramp_evidence_links_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fedramp_gaps",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mapping_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rationale = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    severity = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    owner = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    target_date = table.Column<DateOnly>(type: "date", nullable: false),
                    is_open = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fedramp_gaps", x => x.id);
                    table.ForeignKey(
                        name: "FK_fedramp_gaps_fedramp_control_mappings_tenant_id_mapping_id",
                        columns: x => new { x.tenant_id, x.mapping_id },
                        principalSchema: "gccs",
                        principalTable: "fedramp_control_mappings",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_fedramp_gaps_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fedramp_package_records",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    package_id = table.Column<Guid>(type: "uuid", nullable: false),
                    record_type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    record_id = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    title = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    restricted = table.Column<bool>(type: "boolean", nullable: false),
                    prohibited = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fedramp_package_records", x => x.id);
                    table.ForeignKey(
                        name: "FK_fedramp_package_records_fedramp_readiness_packages_tenant_i~",
                        columns: x => new { x.tenant_id, x.package_id },
                        principalSchema: "gccs",
                        principalTable: "fedramp_readiness_packages",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_fedramp_package_records_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fedramp_readiness_package_history",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    package_id = table.Column<Guid>(type: "uuid", nullable: false),
                    previous_status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    new_status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    actor = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    notes = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: true),
                    changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    changed_by_user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fedramp_readiness_package_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_fedramp_readiness_package_history_fedramp_readiness_package~",
                        columns: x => new { x.tenant_id, x.package_id },
                        principalSchema: "gccs",
                        principalTable: "fedramp_readiness_packages",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_fedramp_readiness_package_history_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fedramp_control_mapping_history_tenant_id_mapping_id_change~",
                schema: "gccs",
                table: "fedramp_control_mapping_history",
                columns: new[] { "tenant_id", "mapping_id", "changed_at" });

            migrationBuilder.CreateIndex(
                name: "IX_fedramp_control_mappings_created_at_updated_at",
                schema: "gccs",
                table: "fedramp_control_mappings",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_fedramp_control_mappings_tenant_id_control_id_baseline",
                schema: "gccs",
                table: "fedramp_control_mappings",
                columns: new[] { "tenant_id", "control_id", "baseline" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fedramp_control_mappings_tenant_id_family_review_state",
                schema: "gccs",
                table: "fedramp_control_mappings",
                columns: new[] { "tenant_id", "family", "review_state" });

            migrationBuilder.CreateIndex(
                name: "IX_fedramp_evidence_links_tenant_id_mapping_id_reference",
                schema: "gccs",
                table: "fedramp_evidence_links",
                columns: new[] { "tenant_id", "mapping_id", "reference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fedramp_gaps_tenant_id_mapping_id_is_open_severity",
                schema: "gccs",
                table: "fedramp_gaps",
                columns: new[] { "tenant_id", "mapping_id", "is_open", "severity" });

            migrationBuilder.CreateIndex(
                name: "IX_fedramp_package_records_tenant_id_package_id_record_type_re~",
                schema: "gccs",
                table: "fedramp_package_records",
                columns: new[] { "tenant_id", "package_id", "record_type", "record_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fedramp_readiness_package_history_tenant_id_package_id_chan~",
                schema: "gccs",
                table: "fedramp_readiness_package_history",
                columns: new[] { "tenant_id", "package_id", "changed_at" });

            migrationBuilder.CreateIndex(
                name: "IX_fedramp_readiness_packages_created_at_updated_at",
                schema: "gccs",
                table: "fedramp_readiness_packages",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_fedramp_readiness_packages_tenant_id_package_version",
                schema: "gccs",
                table: "fedramp_readiness_packages",
                columns: new[] { "tenant_id", "package_version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fedramp_readiness_packages_tenant_id_status_generated_at",
                schema: "gccs",
                table: "fedramp_readiness_packages",
                columns: new[] { "tenant_id", "status", "generated_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fedramp_control_mapping_history",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "fedramp_evidence_links",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "fedramp_gaps",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "fedramp_package_records",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "fedramp_readiness_package_history",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "fedramp_control_mappings",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "fedramp_readiness_packages",
                schema: "gccs");
        }
    }
}

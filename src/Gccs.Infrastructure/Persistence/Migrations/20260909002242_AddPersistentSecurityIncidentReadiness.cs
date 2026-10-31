using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPersistentSecurityIncidentReadiness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "incident_readiness_records",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    review_due_at = table.Column<DateOnly>(type: "date", nullable: false),
                    review_basis = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    state = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    approved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approval_notes = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_incident_readiness_records", x => x.id);
                    table.ForeignKey(
                        name: "FK_incident_readiness_records_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "readiness_approvals",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    record_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    record_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    approved_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    notes = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_readiness_approvals", x => x.id);
                    table.ForeignKey(
                        name: "FK_readiness_approvals_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "readiness_history",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    record_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    record_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    action = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    summary = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_readiness_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_readiness_history_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "security_review_records",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    state = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    approved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approval_notes = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_security_review_records", x => x.id);
                    table.ForeignKey(
                        name: "FK_security_review_records_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "technical_readiness_records",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    state = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    approved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approval_notes = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_technical_readiness_records", x => x.id);
                    table.ForeignKey(
                        name: "FK_technical_readiness_records_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "incident_contacts",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    logical_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    readiness_id = table.Column<Guid>(type: "uuid", nullable: false),
                    function = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    contact = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    escalation_role = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_incident_contacts", x => x.id);
                    table.ForeignKey(
                        name: "FK_incident_contacts_incident_readiness_records_readiness_id",
                        column: x => x.readiness_id,
                        principalSchema: "gccs",
                        principalTable: "incident_readiness_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_incident_contacts_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "incident_playbooks",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    logical_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    readiness_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    trigger = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: false),
                    containment_steps_json = table.Column<string>(type: "jsonb", nullable: false),
                    notification_path = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: false),
                    evidence_to_collect_json = table.Column<string>(type: "jsonb", nullable: false),
                    owner = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    closure_criteria = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_incident_playbooks", x => x.id);
                    table.ForeignKey(
                        name: "FK_incident_playbooks_incident_readiness_records_readiness_id",
                        column: x => x.readiness_id,
                        principalSchema: "gccs",
                        principalTable: "incident_readiness_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_incident_playbooks_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "incident_tabletops",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    logical_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    readiness_id = table.Column<Guid>(type: "uuid", nullable: false),
                    executed_at = table.Column<DateOnly>(type: "date", nullable: false),
                    environment = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    participants_json = table.Column<string>(type: "jsonb", nullable: false),
                    findings_json = table.Column<string>(type: "jsonb", nullable: false),
                    evidence_reference = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    reviewer_user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_incident_tabletops", x => x.id);
                    table.ForeignKey(
                        name: "FK_incident_tabletops_incident_readiness_records_readiness_id",
                        column: x => x.readiness_id,
                        principalSchema: "gccs",
                        principalTable: "incident_readiness_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_incident_tabletops_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "security_review_checklist_items",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    review_id = table.Column<Guid>(type: "uuid", nullable: false),
                    area = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    reviewer_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_at = table.Column<DateOnly>(type: "date", nullable: true),
                    evidence_link = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: true),
                    rationale = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_security_review_checklist_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_security_review_checklist_items_security_review_records_rev~",
                        column: x => x.review_id,
                        principalSchema: "gccs",
                        principalTable: "security_review_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_security_review_checklist_items_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "security_review_findings",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    logical_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    review_id = table.Column<Guid>(type: "uuid", nullable: false),
                    area = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    severity = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    summary = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: false),
                    remediation_owner = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    due_at = table.Column<DateOnly>(type: "date", nullable: true),
                    closure_notes = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_security_review_findings", x => x.id);
                    table.ForeignKey(
                        name: "FK_security_review_findings_security_review_records_review_id",
                        column: x => x.review_id,
                        principalSchema: "gccs",
                        principalTable: "security_review_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_security_review_findings_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "executed_control_evidence",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    logical_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    readiness_id = table.Column<Guid>(type: "uuid", nullable: false),
                    control_type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    executed_at = table.Column<DateOnly>(type: "date", nullable: false),
                    environment = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    reviewer_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    result = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    evidence_reference = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    expires_at = table.Column<DateOnly>(type: "date", nullable: true),
                    notes = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_executed_control_evidence", x => x.id);
                    table.ForeignKey(
                        name: "FK_executed_control_evidence_technical_readiness_records_readi~",
                        column: x => x.readiness_id,
                        principalSchema: "gccs",
                        principalTable: "technical_readiness_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_executed_control_evidence_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "incident_follow_ups",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    logical_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    readiness_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tabletop_id = table.Column<Guid>(type: "uuid", nullable: false),
                    severity = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    summary = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: false),
                    owner = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    due_at = table.Column<DateOnly>(type: "date", nullable: false),
                    closure_notes = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_incident_follow_ups", x => x.id);
                    table.ForeignKey(
                        name: "FK_incident_follow_ups_incident_readiness_records_readiness_id",
                        column: x => x.readiness_id,
                        principalSchema: "gccs",
                        principalTable: "incident_readiness_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_incident_follow_ups_incident_tabletops_tabletop_id",
                        column: x => x.tabletop_id,
                        principalSchema: "gccs",
                        principalTable: "incident_tabletops",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_incident_follow_ups_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "accepted_security_risks",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    logical_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    review_id = table.Column<Guid>(type: "uuid", nullable: false),
                    finding_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approver_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    accepted_at = table.Column<DateOnly>(type: "date", nullable: false),
                    scope = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: false),
                    expires_at = table.Column<DateOnly>(type: "date", nullable: true),
                    review_at = table.Column<DateOnly>(type: "date", nullable: true),
                    mitigation_note = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accepted_security_risks", x => x.id);
                    table.ForeignKey(
                        name: "FK_accepted_security_risks_security_review_findings_finding_id",
                        column: x => x.finding_id,
                        principalSchema: "gccs",
                        principalTable: "security_review_findings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_accepted_security_risks_security_review_records_review_id",
                        column: x => x.review_id,
                        principalSchema: "gccs",
                        principalTable: "security_review_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_accepted_security_risks_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_accepted_security_risks_finding_id",
                schema: "gccs",
                table: "accepted_security_risks",
                column: "finding_id");

            migrationBuilder.CreateIndex(
                name: "IX_accepted_security_risks_review_id_logical_id",
                schema: "gccs",
                table: "accepted_security_risks",
                columns: new[] { "review_id", "logical_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_accepted_security_risks_tenant_id_review_id",
                schema: "gccs",
                table: "accepted_security_risks",
                columns: new[] { "tenant_id", "review_id" });

            migrationBuilder.CreateIndex(
                name: "IX_executed_control_evidence_readiness_id_control_type",
                schema: "gccs",
                table: "executed_control_evidence",
                columns: new[] { "readiness_id", "control_type" });

            migrationBuilder.CreateIndex(
                name: "IX_executed_control_evidence_readiness_id_logical_id",
                schema: "gccs",
                table: "executed_control_evidence",
                columns: new[] { "readiness_id", "logical_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_executed_control_evidence_tenant_id",
                schema: "gccs",
                table: "executed_control_evidence",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_incident_contacts_readiness_id_function",
                schema: "gccs",
                table: "incident_contacts",
                columns: new[] { "readiness_id", "function" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_incident_contacts_readiness_id_logical_id",
                schema: "gccs",
                table: "incident_contacts",
                columns: new[] { "readiness_id", "logical_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_incident_contacts_tenant_id",
                schema: "gccs",
                table: "incident_contacts",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_incident_follow_ups_readiness_id_logical_id",
                schema: "gccs",
                table: "incident_follow_ups",
                columns: new[] { "readiness_id", "logical_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_incident_follow_ups_tabletop_id",
                schema: "gccs",
                table: "incident_follow_ups",
                column: "tabletop_id");

            migrationBuilder.CreateIndex(
                name: "IX_incident_follow_ups_tenant_id_status_severity",
                schema: "gccs",
                table: "incident_follow_ups",
                columns: new[] { "tenant_id", "status", "severity" });

            migrationBuilder.CreateIndex(
                name: "IX_incident_playbooks_readiness_id_key",
                schema: "gccs",
                table: "incident_playbooks",
                columns: new[] { "readiness_id", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_incident_playbooks_readiness_id_logical_id",
                schema: "gccs",
                table: "incident_playbooks",
                columns: new[] { "readiness_id", "logical_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_incident_playbooks_tenant_id",
                schema: "gccs",
                table: "incident_playbooks",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_incident_readiness_records_tenant_id_state",
                schema: "gccs",
                table: "incident_readiness_records",
                columns: new[] { "tenant_id", "state" });

            migrationBuilder.CreateIndex(
                name: "IX_incident_readiness_records_tenant_id_version",
                schema: "gccs",
                table: "incident_readiness_records",
                columns: new[] { "tenant_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_incident_tabletops_readiness_id_logical_id",
                schema: "gccs",
                table: "incident_tabletops",
                columns: new[] { "readiness_id", "logical_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_incident_tabletops_tenant_id_executed_at",
                schema: "gccs",
                table: "incident_tabletops",
                columns: new[] { "tenant_id", "executed_at" });

            migrationBuilder.CreateIndex(
                name: "IX_readiness_approvals_tenant_id_record_type_record_id_version",
                schema: "gccs",
                table: "readiness_approvals",
                columns: new[] { "tenant_id", "record_type", "record_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_readiness_history_tenant_id_occurred_at",
                schema: "gccs",
                table: "readiness_history",
                columns: new[] { "tenant_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_security_review_checklist_items_review_id_area",
                schema: "gccs",
                table: "security_review_checklist_items",
                columns: new[] { "review_id", "area" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_security_review_checklist_items_tenant_id",
                schema: "gccs",
                table: "security_review_checklist_items",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_security_review_findings_review_id_logical_id",
                schema: "gccs",
                table: "security_review_findings",
                columns: new[] { "review_id", "logical_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_security_review_findings_tenant_id_status_severity",
                schema: "gccs",
                table: "security_review_findings",
                columns: new[] { "tenant_id", "status", "severity" });

            migrationBuilder.CreateIndex(
                name: "IX_security_review_records_tenant_id_state",
                schema: "gccs",
                table: "security_review_records",
                columns: new[] { "tenant_id", "state" });

            migrationBuilder.CreateIndex(
                name: "IX_security_review_records_tenant_id_version",
                schema: "gccs",
                table: "security_review_records",
                columns: new[] { "tenant_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_technical_readiness_records_tenant_id_state",
                schema: "gccs",
                table: "technical_readiness_records",
                columns: new[] { "tenant_id", "state" });

            migrationBuilder.CreateIndex(
                name: "IX_technical_readiness_records_tenant_id_version",
                schema: "gccs",
                table: "technical_readiness_records",
                columns: new[] { "tenant_id", "version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "accepted_security_risks",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "executed_control_evidence",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "incident_contacts",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "incident_follow_ups",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "incident_playbooks",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "readiness_approvals",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "readiness_history",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "security_review_checklist_items",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "security_review_findings",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "technical_readiness_records",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "incident_tabletops",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "security_review_records",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "incident_readiness_records",
                schema: "gccs");
        }
    }
}

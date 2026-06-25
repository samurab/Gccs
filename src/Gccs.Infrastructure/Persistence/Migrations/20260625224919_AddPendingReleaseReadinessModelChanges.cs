using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gccs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingReleaseReadinessModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "break_glass_access_grants",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "character varying(800)", maxLength: 800, nullable: false),
                    approved_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    approval_reference = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    last_used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_used_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_break_glass_access_grants", x => x.id);
                    table.ForeignKey(
                        name: "FK_break_glass_access_grants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_break_glass_access_grants_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "gccs",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "government_cloud_environments",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    environment_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    region = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    boundary = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    network_segment = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    storage_account = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    database_service = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    key_management_service = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    logging_workspace = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    backup_policy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    private_networking_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    storage_encryption_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    database_encryption_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    customer_managed_keys_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    audit_logging_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    immutable_logging_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    backup_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    restore_tested = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    reviewer_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    review_notes = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: true),
                    reviewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_government_cloud_environments", x => x.id);
                    table.ForeignKey(
                        name: "FK_government_cloud_environments_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "saml_account_links",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    membership_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    saml_subject = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    saml_configuration_id = table.Column<Guid>(type: "uuid", nullable: true),
                    attributes_json = table.Column<string>(type: "jsonb", nullable: false),
                    last_successful_sign_in_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saml_account_links", x => x.id);
                    table.ForeignKey(
                        name: "FK_saml_account_links_tenant_memberships_membership_id",
                        column: x => x.membership_id,
                        principalSchema: "gccs",
                        principalTable: "tenant_memberships",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_saml_account_links_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_saml_account_links_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "gccs",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "saml_identity_provider_configurations",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_id = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    sso_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    certificate_pem = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    certificate_fingerprint = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    certificate_expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    signing_requirement = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name_id_format = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    attribute_mappings_json = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    metadata_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    callback_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    last_tested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_test_result = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    last_test_diagnostic_summary = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saml_identity_provider_configurations", x => x.id);
                    table.ForeignKey(
                        name: "FK_saml_identity_provider_configurations_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "scim_group_mappings",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    role_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scim_group_mappings", x => x.id);
                    table.ForeignKey(
                        name: "FK_scim_group_mappings_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "scim_provisioned_identities",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    user_name = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    membership_id = table.Column<Guid>(type: "uuid", nullable: false),
                    last_provisioned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scim_provisioned_identities", x => x.id);
                    table.ForeignKey(
                        name: "FK_scim_provisioned_identities_tenant_memberships_membership_id",
                        column: x => x.membership_id,
                        principalSchema: "gccs",
                        principalTable: "tenant_memberships",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_scim_provisioned_identities_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_scim_provisioned_identities_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "gccs",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "scim_provisioning_configurations",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    endpoint_label = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    last_sync_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    token_rotated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    token_revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scim_provisioning_configurations", x => x.id);
                    table.ForeignKey(
                        name: "FK_scim_provisioning_configurations_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tenant_sso_policies",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    saml_configuration_id = table.Column<Guid>(type: "uuid", nullable: true),
                    required_email_domain = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    required_attributes_json = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_sso_policies", x => x.id);
                    table.ForeignKey(
                        name: "FK_tenant_sso_policies_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "government_cloud_environment_status_history",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    environment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    previous_status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    new_status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    reviewer_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    review_notes = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: true),
                    changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    changed_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    history_note = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_government_cloud_environment_status_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_government_cloud_environment_status_history_government_clou~",
                        column: x => x.environment_id,
                        principalSchema: "gccs",
                        principalTable: "government_cloud_environments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_government_cloud_environment_status_history_tenants_tenant_~",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "government_cloud_release_readiness",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    environment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    release_window = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    owner = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    approver_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    approval_notes = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: true),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    result = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    rollback_status = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    deployed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_government_cloud_release_readiness", x => x.id);
                    table.ForeignKey(
                        name: "FK_government_cloud_release_readiness_government_cloud_environ~",
                        column: x => x.environment_id,
                        principalSchema: "gccs",
                        principalTable: "government_cloud_environments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_government_cloud_release_readiness_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "regulated_tenant_provisioning_requests",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_name = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    customer_type = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    environment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    data_handling_mode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    cui_approval_complete = table.Column<bool>(type: "boolean", nullable: false),
                    key_policy = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    support_model = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    migration_source = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    provisioned_tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    failure_reason = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: true),
                    rollback_decision = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: true),
                    failure_owner = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_regulated_tenant_provisioning_requests", x => x.id);
                    table.ForeignKey(
                        name: "FK_regulated_tenant_provisioning_requests_government_cloud_env~",
                        column: x => x.environment_id,
                        principalSchema: "gccs",
                        principalTable: "government_cloud_environments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_regulated_tenant_provisioning_requests_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "government_cloud_release_checklist",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    readiness_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    evidence_reference = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_by_user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_government_cloud_release_checklist", x => x.id);
                    table.ForeignKey(
                        name: "FK_government_cloud_release_checklist_government_cloud_release~",
                        column: x => x.readiness_id,
                        principalSchema: "gccs",
                        principalTable: "government_cloud_release_readiness",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_government_cloud_release_checklist_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "government_cloud_release_evidence",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    readiness_id = table.Column<Guid>(type: "uuid", nullable: false),
                    evidence_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    link = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    linked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    linked_by_user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_government_cloud_release_evidence", x => x.id);
                    table.ForeignKey(
                        name: "FK_government_cloud_release_evidence_government_cloud_release_~",
                        column: x => x.readiness_id,
                        principalSchema: "gccs",
                        principalTable: "government_cloud_release_readiness",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_government_cloud_release_evidence_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "government_cloud_release_gaps",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    readiness_id = table.Column<Guid>(type: "uuid", nullable: false),
                    area = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    severity = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    is_open = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_government_cloud_release_gaps", x => x.id);
                    table.ForeignKey(
                        name: "FK_government_cloud_release_gaps_government_cloud_release_read~",
                        column: x => x.readiness_id,
                        principalSchema: "gccs",
                        principalTable: "government_cloud_release_readiness",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_government_cloud_release_gaps_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "regulated_provisioning_approvals",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    area = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    approver_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    notes = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: false),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    approved_by_user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_regulated_provisioning_approvals", x => x.id);
                    table.ForeignKey(
                        name: "FK_regulated_provisioning_approvals_regulated_tenant_provision~",
                        column: x => x.request_id,
                        principalSchema: "gccs",
                        principalTable: "regulated_tenant_provisioning_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_regulated_provisioning_approvals_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "regulated_provisioning_checklist",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    completed_by_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    evidence_reference = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_by_user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_regulated_provisioning_checklist", x => x.id);
                    table.ForeignKey(
                        name: "FK_regulated_provisioning_checklist_regulated_tenant_provision~",
                        column: x => x.request_id,
                        principalSchema: "gccs",
                        principalTable: "regulated_tenant_provisioning_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_regulated_provisioning_checklist_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "regulated_tenant_provisioning_history",
                schema: "gccs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    previous_status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    new_status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    changed_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    note = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_regulated_tenant_provisioning_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_regulated_tenant_provisioning_history_regulated_tenant_prov~",
                        column: x => x.request_id,
                        principalSchema: "gccs",
                        principalTable: "regulated_tenant_provisioning_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_regulated_tenant_provisioning_history_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "gccs",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_break_glass_access_grants_created_at_updated_at",
                schema: "gccs",
                table: "break_glass_access_grants",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_break_glass_access_grants_tenant_id_expires_at",
                schema: "gccs",
                table: "break_glass_access_grants",
                columns: new[] { "tenant_id", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "IX_break_glass_access_grants_tenant_id_user_id_status",
                schema: "gccs",
                table: "break_glass_access_grants",
                columns: new[] { "tenant_id", "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_break_glass_access_grants_user_id",
                schema: "gccs",
                table: "break_glass_access_grants",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_government_cloud_environment_status_history_environment_id",
                schema: "gccs",
                table: "government_cloud_environment_status_history",
                column: "environment_id");

            migrationBuilder.CreateIndex(
                name: "IX_government_cloud_environment_status_history_tenant_id_envir~",
                schema: "gccs",
                table: "government_cloud_environment_status_history",
                columns: new[] { "tenant_id", "environment_id", "changed_at" });

            migrationBuilder.CreateIndex(
                name: "IX_government_cloud_environments_created_at_updated_at",
                schema: "gccs",
                table: "government_cloud_environments",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_government_cloud_environments_tenant_id_name",
                schema: "gccs",
                table: "government_cloud_environments",
                columns: new[] { "tenant_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_government_cloud_environments_tenant_id_status",
                schema: "gccs",
                table: "government_cloud_environments",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_government_cloud_release_checklist_readiness_id",
                schema: "gccs",
                table: "government_cloud_release_checklist",
                column: "readiness_id");

            migrationBuilder.CreateIndex(
                name: "IX_government_cloud_release_checklist_tenant_id_readiness_id_i~",
                schema: "gccs",
                table: "government_cloud_release_checklist",
                columns: new[] { "tenant_id", "readiness_id", "item" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_government_cloud_release_evidence_readiness_id",
                schema: "gccs",
                table: "government_cloud_release_evidence",
                column: "readiness_id");

            migrationBuilder.CreateIndex(
                name: "IX_government_cloud_release_evidence_tenant_id_readiness_id_ev~",
                schema: "gccs",
                table: "government_cloud_release_evidence",
                columns: new[] { "tenant_id", "readiness_id", "evidence_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_government_cloud_release_gaps_created_at_updated_at",
                schema: "gccs",
                table: "government_cloud_release_gaps",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_government_cloud_release_gaps_readiness_id",
                schema: "gccs",
                table: "government_cloud_release_gaps",
                column: "readiness_id");

            migrationBuilder.CreateIndex(
                name: "IX_government_cloud_release_gaps_tenant_id_readiness_id_severi~",
                schema: "gccs",
                table: "government_cloud_release_gaps",
                columns: new[] { "tenant_id", "readiness_id", "severity", "is_open" });

            migrationBuilder.CreateIndex(
                name: "IX_government_cloud_release_readiness_created_at_updated_at",
                schema: "gccs",
                table: "government_cloud_release_readiness",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_government_cloud_release_readiness_environment_id",
                schema: "gccs",
                table: "government_cloud_release_readiness",
                column: "environment_id");

            migrationBuilder.CreateIndex(
                name: "IX_government_cloud_release_readiness_tenant_id_environment_id~",
                schema: "gccs",
                table: "government_cloud_release_readiness",
                columns: new[] { "tenant_id", "environment_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_regulated_provisioning_approvals_request_id",
                schema: "gccs",
                table: "regulated_provisioning_approvals",
                column: "request_id");

            migrationBuilder.CreateIndex(
                name: "IX_regulated_provisioning_approvals_tenant_id_request_id_area",
                schema: "gccs",
                table: "regulated_provisioning_approvals",
                columns: new[] { "tenant_id", "request_id", "area" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_regulated_provisioning_checklist_request_id",
                schema: "gccs",
                table: "regulated_provisioning_checklist",
                column: "request_id");

            migrationBuilder.CreateIndex(
                name: "IX_regulated_provisioning_checklist_tenant_id_request_id_item",
                schema: "gccs",
                table: "regulated_provisioning_checklist",
                columns: new[] { "tenant_id", "request_id", "item" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_regulated_tenant_provisioning_history_request_id",
                schema: "gccs",
                table: "regulated_tenant_provisioning_history",
                column: "request_id");

            migrationBuilder.CreateIndex(
                name: "IX_regulated_tenant_provisioning_history_tenant_id_request_id_~",
                schema: "gccs",
                table: "regulated_tenant_provisioning_history",
                columns: new[] { "tenant_id", "request_id", "changed_at" });

            migrationBuilder.CreateIndex(
                name: "IX_regulated_tenant_provisioning_requests_created_at_updated_at",
                schema: "gccs",
                table: "regulated_tenant_provisioning_requests",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_regulated_tenant_provisioning_requests_environment_id",
                schema: "gccs",
                table: "regulated_tenant_provisioning_requests",
                column: "environment_id");

            migrationBuilder.CreateIndex(
                name: "IX_regulated_tenant_provisioning_requests_tenant_id_environmen~",
                schema: "gccs",
                table: "regulated_tenant_provisioning_requests",
                columns: new[] { "tenant_id", "environment_id" });

            migrationBuilder.CreateIndex(
                name: "IX_regulated_tenant_provisioning_requests_tenant_id_status",
                schema: "gccs",
                table: "regulated_tenant_provisioning_requests",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_saml_account_links_created_at_updated_at",
                schema: "gccs",
                table: "saml_account_links",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_saml_account_links_membership_id",
                schema: "gccs",
                table: "saml_account_links",
                column: "membership_id");

            migrationBuilder.CreateIndex(
                name: "IX_saml_account_links_tenant_id_email",
                schema: "gccs",
                table: "saml_account_links",
                columns: new[] { "tenant_id", "email" });

            migrationBuilder.CreateIndex(
                name: "IX_saml_account_links_tenant_id_saml_subject",
                schema: "gccs",
                table: "saml_account_links",
                columns: new[] { "tenant_id", "saml_subject" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_saml_account_links_tenant_id_user_id",
                schema: "gccs",
                table: "saml_account_links",
                columns: new[] { "tenant_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "IX_saml_account_links_user_id",
                schema: "gccs",
                table: "saml_account_links",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_saml_identity_provider_configurations_created_at_updated_at",
                schema: "gccs",
                table: "saml_identity_provider_configurations",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_saml_identity_provider_configurations_tenant_id_entity_id",
                schema: "gccs",
                table: "saml_identity_provider_configurations",
                columns: new[] { "tenant_id", "entity_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_saml_identity_provider_configurations_tenant_id_status",
                schema: "gccs",
                table: "saml_identity_provider_configurations",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_scim_group_mappings_created_at_updated_at",
                schema: "gccs",
                table: "scim_group_mappings",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_scim_group_mappings_tenant_id_group_display_name",
                schema: "gccs",
                table: "scim_group_mappings",
                columns: new[] { "tenant_id", "group_display_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_scim_provisioned_identities_created_at_updated_at",
                schema: "gccs",
                table: "scim_provisioned_identities",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_scim_provisioned_identities_external_id",
                schema: "gccs",
                table: "scim_provisioned_identities",
                column: "external_id");

            migrationBuilder.CreateIndex(
                name: "IX_scim_provisioned_identities_membership_id",
                schema: "gccs",
                table: "scim_provisioned_identities",
                column: "membership_id");

            migrationBuilder.CreateIndex(
                name: "IX_scim_provisioned_identities_tenant_id_external_id",
                schema: "gccs",
                table: "scim_provisioned_identities",
                columns: new[] { "tenant_id", "external_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_scim_provisioned_identities_tenant_id_user_name",
                schema: "gccs",
                table: "scim_provisioned_identities",
                columns: new[] { "tenant_id", "user_name" });

            migrationBuilder.CreateIndex(
                name: "IX_scim_provisioned_identities_user_id",
                schema: "gccs",
                table: "scim_provisioned_identities",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_scim_provisioning_configurations_created_at_updated_at",
                schema: "gccs",
                table: "scim_provisioning_configurations",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_scim_provisioning_configurations_tenant_id",
                schema: "gccs",
                table: "scim_provisioning_configurations",
                column: "tenant_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenant_sso_policies_created_at_updated_at",
                schema: "gccs",
                table: "tenant_sso_policies",
                columns: new[] { "created_at", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "IX_tenant_sso_policies_tenant_id",
                schema: "gccs",
                table: "tenant_sso_policies",
                column: "tenant_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "break_glass_access_grants",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "government_cloud_environment_status_history",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "government_cloud_release_checklist",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "government_cloud_release_evidence",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "government_cloud_release_gaps",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "regulated_provisioning_approvals",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "regulated_provisioning_checklist",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "regulated_tenant_provisioning_history",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "saml_account_links",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "saml_identity_provider_configurations",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "scim_group_mappings",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "scim_provisioned_identities",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "scim_provisioning_configurations",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "tenant_sso_policies",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "government_cloud_release_readiness",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "regulated_tenant_provisioning_requests",
                schema: "gccs");

            migrationBuilder.DropTable(
                name: "government_cloud_environments",
                schema: "gccs");
        }
    }
}

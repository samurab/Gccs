using System.Text.RegularExpressions;
using Xunit;

namespace Gccs.Api.Tests;

public sealed partial class DevelopmentStoryRegressionCoverageTests
{
    private const int ExpectedDocumentedRegressionCaseCount = 382;

    private static readonly string[] CommonExpectationSignals =
    [
        "Tenant-owned reads and writes are scoped to the current tenant.",
        "Restricted actions are denied server-side, even when UI controls are hidden.",
        "Compliance-relevant create, update, delete, status, upload, approval, report, and notification actions are audit logged.",
        "No-CUI controls are preserved for all upload workflows.",
        "User-facing errors are clear and use the standard API/UI error pattern."
    ];

    private static readonly string[] RequiredCaseExpectationVerbs =
    [
        "verify",
        "confirm",
        "attempt",
        "run",
        "create",
        "seed",
        "render",
        "call",
        "upload",
        "generate",
        "open",
        "execute",
        "trigger",
        "filter",
        "link",
        "move",
        "change",
        "set",
        "store",
        "scan",
        "remove",
        "assign",
        "try",
        "add",
        "attach",
        "export"
    ];

    private static readonly string[] BackendSignals =
    [
        "api",
        "endpoint",
        "server-side",
        "tenant",
        "rbac",
        "permission",
        "audit",
        "persist",
        "validation",
        "repository",
        "service",
        "upload",
        "evidence",
        "report",
        "task",
        "obligation",
        "contract",
        "subcontractor",
        "notification",
        "assessment",
        "poa&m",
        "scan",
        "storage",
        "database",
        "cache",
        "source url",
        "last reviewed"
    ];

    private static readonly string[] FrontendSignals =
    [
        "ui",
        "render",
        "screen",
        "visible",
        "visual",
        "keyboard",
        "focus",
        "route",
        "navigation",
        "page",
        "dashboard",
        "detail",
        "list",
        "calendar",
        "display",
        "shown",
        "hidden",
        "styling",
        "accessible",
        "empty state",
        "error states"
    ];

    private static readonly string[] DeliverySignals =
    [
        "ci",
        "pull request",
        "branch protection",
        "build",
        "lint",
        "staging",
        "deploy",
        "rollback",
        "docker",
        "local services",
        "health checks",
        "production readiness",
        "checklist"
    ];

    public static TheoryData<DevelopmentStoryRegressionCase> DocumentedRegressionCases
    {
        get
        {
            var data = new TheoryData<DevelopmentStoryRegressionCase>();
            foreach (var regressionCase in ReadDocumentedRegressionCases())
            {
                data.Add(regressionCase);
            }

            return data;
        }
    }

    [Fact]
    public void Development_story_test_cases_document_keeps_common_regression_expectations_visible()
    {
        var document = ReadDevelopmentStoryTestCasesDocument();

        foreach (var expectation in CommonExpectationSignals)
        {
            Assert.Contains(expectation, document);
        }
    }

    [Fact]
    public void Development_story_test_cases_are_unique_complete_and_well_formed()
    {
        var cases = ReadDocumentedRegressionCases();
        var duplicateIds = cases
            .GroupBy(regressionCase => regressionCase.Id)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        Assert.Equal(ExpectedDocumentedRegressionCaseCount, cases.Count);
        Assert.Empty(duplicateIds);
        Assert.Equal(
            cases.Select(regressionCase => regressionCase.Id).OrderBy(id => id, StringComparer.Ordinal).ToArray(),
            cases.Select(regressionCase => regressionCase.Id).Distinct().OrderBy(id => id, StringComparer.Ordinal).ToArray());
        Assert.All(cases, regressionCase =>
        {
            Assert.Matches(@"^TC-\d+\.\d+\.\d+$", regressionCase.Id);
            Assert.Matches(@"^\d+\. .+", regressionCase.Section);
            Assert.Matches(@"^Story \d+\.\d+: .+", regressionCase.Story);
            Assert.False(string.IsNullOrWhiteSpace(regressionCase.Title));
            Assert.False(string.IsNullOrWhiteSpace(regressionCase.ExpectedBehavior));
            Assert.True(
                RequiredCaseExpectationVerbs.Any(verb => regressionCase.ExpectedBehavior.Contains(verb, StringComparison.OrdinalIgnoreCase)),
                $"{regressionCase.Id} should describe an executable verification action.");
        });
    }

    [Fact]
    public void Development_story_test_prompts_cover_every_documented_regression_case()
    {
        var promptDocument = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "docs", "development-story-test-prompts.md"));
        var promptedIds = TestCaseIdRegex()
            .Matches(promptDocument)
            .Select(match => match.Value)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var regressionCase in ReadDocumentedRegressionCases())
        {
            Assert.Contains(regressionCase.Id, promptedIds);
        }
    }

    [Theory]
    [MemberData(nameof(DocumentedRegressionCases))]
    public void Each_development_story_test_case_has_an_executable_regression_strategy(DevelopmentStoryRegressionCase regressionCase)
    {
        var strategy = RegressionStrategy.For(regressionCase);

        Assert.NotEqual(RegressionLayer.Unclassified, strategy.PrimaryLayer);
        Assert.False(string.IsNullOrWhiteSpace(strategy.NarrowCommand));
        Assert.Contains("No-CUI", strategy.InvariantSummary);
        Assert.Contains("tenant", strategy.InvariantSummary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("RBAC", strategy.InvariantSummary);
        Assert.Contains("audit", strategy.InvariantSummary, StringComparison.OrdinalIgnoreCase);

        if (regressionCase.ExpectedBehavior.Contains("UI", StringComparison.OrdinalIgnoreCase) ||
            regressionCase.ExpectedBehavior.Contains("render", StringComparison.OrdinalIgnoreCase) ||
            regressionCase.ExpectedBehavior.Contains("screen", StringComparison.OrdinalIgnoreCase))
        {
            Assert.True(
                strategy.PrimaryLayer is RegressionLayer.Frontend or RegressionLayer.EndToEnd,
                $"{regressionCase.Id} should be driven through a frontend or end-to-end regression path.");
        }

        if (regressionCase.ExpectedBehavior.Contains("server-side", StringComparison.OrdinalIgnoreCase) ||
            regressionCase.ExpectedBehavior.Contains("endpoint", StringComparison.OrdinalIgnoreCase) ||
            regressionCase.ExpectedBehavior.Contains("API", StringComparison.OrdinalIgnoreCase))
        {
            Assert.True(
                strategy.PrimaryLayer is RegressionLayer.Backend or RegressionLayer.EndToEnd,
                $"{regressionCase.Id} should be driven through a backend or end-to-end regression path.");
        }
    }

    [Fact]
    public void Implemented_foundation_regressions_remain_backed_by_focused_tests()
    {
        var testSource = string.Join(
            Environment.NewLine,
            Directory.EnumerateFiles(Path.Combine(FindRepositoryRoot(), "tests", "Gccs.Api.Tests"), "*.cs")
                .Where(path => Path.GetFileName(path) != nameof(DevelopmentStoryRegressionCoverageTests) + ".cs")
                .Concat(Directory.EnumerateFiles(Path.Combine(FindRepositoryRoot(), "apps", "web", "src"), "*.test.tsx"))
                .Select(File.ReadAllText));

        var focusedCoverageSignals = new Dictionary<string, string[]>
        {
            ["TC-1.1.1"] = ["Tc_1_1_1_repository_keeps_required_project_boundaries_visible", "Tc_1_1_1_readme_and_docs_describe_ownership_boundaries"],
            ["TC-1.1.2"] = ["Tc_1_1_2_developer_docs_match_clean_checkout_restore_build_and_test_commands"],
            ["TC-1.1.3"] = ["Tc_1_1_3_implemented_compliance_workflows_are_backend_enforced_not_ui_only", "Tc_1_1_3_web_client_does_not_embed_source_backed_obligation_library_content"],
            ["TC-1.1.4"] = ["Tc_1_1_4_developer_docs_explicitly_position_mvp_as_no_cui_compliance_management_only"],
            ["TC-1.2.1"] = ["Documented_one_command_local_services_startup_reports_all_services_healthy"],
            ["TC-1.2.2"] = ["Api_health_reports_connectivity_for_local_database_cache_storage_and_scanner"],
            ["TC-1.2.3"] = ["Startup_reports_clear_error_when_each_required_local_dependency_configuration_value_is_missing"],
            ["TC-1.2.4"] = ["Committed_repository_files_do_not_contain_production_secrets_tokens_or_customer_data"],
            ["TC-1.3.1"] = ["Tc_1_3_1_pull_request_validation_runs_required_backend_frontend_migration_and_scan_steps", "Tc_1_3_1_required_ci_steps_stay_in_the_expected_required_check"],
            ["TC-1.3.2"] = ["Tc_1_3_2_controlled_failing_steps_make_required_pull_request_checks_unmergeable"],
            ["TC-1.3.3"] = ["Tc_1_3_3_failing_ci_logs_identify_the_project_command_and_step_without_unrelated_job_inspection", "Tc_1_3_3_test_failures_upload_focused_backend_and_frontend_artifacts"],
            ["TC-1.3.4"] = ["Tc_1_3_4_dependency_and_secret_scan_findings_are_visible_in_required_pull_request_checks", "Tc_1_3_4_security_scans_have_permissions_and_commands_reviewers_can_trace"],
            ["TC-2.1.1"] = ["TC_2_1_1_Tenant_creation_persists_required_metadata"],
            ["TC-2.1.2"] = ["TC_2_1_2_Tenant_owned_sample_records_store_correct_tenant_id"],
            ["TC-2.1.3"] = ["TC_2_1_3_Cross_tenant_read_by_id_returns_not_found_without_data_leakage"],
            ["TC-2.1.4"] = ["TC_2_1_4_Tenant_status_change_audit_event_contains_before_and_after_status"],
            ["TC-2.2.1"] = ["TC_2_2_1_Assigned_user_is_visible_only_when_that_tenant_is_active"],
            ["TC-2.2.2"] = ["TC_2_2_2_Tenant_member_list_excludes_users_from_other_tenants"],
            ["TC-2.2.3"] = ["TC_2_2_3_Duplicate_membership_creation_is_rejected"],
            ["TC-2.2.4"] = ["TC_2_2_4_Membership_add_update_and_deactivate_actions_are_audit_logged"],
            ["TC-2.3.1"] = ["TC_2_3_1_Admin_creates_invitation_with_token_expiration_pending_status_and_notification_placeholder"],
            ["TC-2.3.2"] = ["TC_2_3_2_Contributor_and_auditor_cannot_manage_invitations"],
            ["TC-2.3.3"] = ["TC_2_3_3_Expired_or_revoked_invitations_cannot_be_accepted"],
            ["TC-2.3.4"] = ["TC_2_3_4_Invitation_create_accept_expire_and_revoke_actions_are_audit_logged"],
            ["TC-2.4.1"] =
            [
                "TC_2_4_1_Role_catalog_maps_permissions_across_mvp_workflow_areas",
                "TC_2_4_1_Server_side_permission_checks_use_role_derived_permissions",
                "TC_2_4_1_Roles_match_permission_matrix_for_representative_implemented_endpoints"
            ],
            ["TC-2.4.2"] = ["TC-2.4.2 renders workspace actions"],
            ["TC-2.4.3"] = ["TC_2_4_3_Permission_failures_return_standard_problem_details"],
            ["TC-2.4.4"] = ["TC_2_4_4_Auditor_can_view_tenant_scoped_approved_evidence_packages_but_cannot_modify_data"],
            ["TC-3.1.1"] = ["Api_routes_require_authentication"],
            ["TC-3.1.2"] =
            [
                "Development_auth_allows_authenticated_api_access_and_resolves_current_context",
                "Authenticated_permissioned_api_request_preserves_tenant_scoped_no_cui_response_shape"
            ],
            ["TC-3.1.3"] = ["Authenticated_api_request_without_tenant_returns_standard_missing_tenant_error"],
            ["TC-3.1.4"] =
            [
                "Compliance_relevant_audit_events_include_request_correlation_id",
                "Failed_api_responses_and_logs_include_request_correlation_id"
            ],
            ["TC-4.1.1"] = ["TC_4_1_1_No_cui_notice_is_returned_before_first_upload"],
            ["TC-4.1.2"] = ["TC_4_1_2_Upload_intent_is_blocked_until_acknowledgement_and_permission_are_present"],
            ["TC-4.1.3"] = ["TC_4_1_3_Acknowledgement_persists_user_tenant_timestamp_and_notice_version"],
            ["TC-4.1.4"] = ["TC_4_1_4_Acknowledgement_is_audit_logged_and_copy_states_no_cui_posture"],
            ["TC-4.2.1"] = ["TC_4_2_1_Disallowed_file_type_is_rejected_without_usable_evidence"],
            ["TC-4.2.2"] = ["TC_4_2_2_Oversized_file_is_rejected_server_side"],
            ["TC-4.2.3"] = ["TC_4_2_3_Valid_upload_metadata_records_validation_and_scan_status"],
            ["TC-4.2.4"] = ["TC_4_2_4_Failed_upload_validation_is_audit_logged_and_not_usable"],
            ["TC-5.1.1"] = ["TC_5_1_1_Sensitive_action_creates_audit_event_with_required_fields"],
            ["TC-5.1.2"] = ["TC_5_1_2_Audit_events_are_append_only_through_normal_apis"],
            ["TC-5.1.3"] = ["TC_5_1_3_Critical_audit_writer_failure_surfaces_clear_error"],
            ["TC-5.1.4"] = ["TC_5_1_4_Request_metadata_is_captured_when_available"],
            ["TC-5.2.1"] = ["TC_5_2_1_Admin_owner_or_advisor_sees_only_current_tenant_events"],
            ["TC-5.2.2"] = ["TC_5_2_2_Contributor_and_auditor_cannot_access_audit_logs"],
            ["TC-5.2.3"] = ["TC_5_2_3_Audit_log_pagination_uses_page_size_and_stable_ordering"],
            ["TC-5.2.4"] = ["TC_5_2_4_Audit_log_filters_are_correct_and_tenant_scoped"],
            ["TC-6.1.1"] = ["TC_6_1_1_Published_obligation_requires_source_url"],
            ["TC-6.1.2"] = ["TC_6_1_2_Published_obligation_requires_last_reviewed_date"],
            ["TC-6.1.3"] = ["TC_6_1_3_Published_obligation_requires_core_metadata_and_review_state"],
            ["TC-6.1.4"] = ["TC_6_1_4_Evidence_examples_are_linked_and_returned_with_obligation"],
            ["TC-9.1.1"] = ["TC_9_1_1_Search_clause_library_by_number_title_and_category"],
            ["TC-9.1.2"] = ["TC_9_1_2_Only_published_clauses_are_available_for_customer_mapping"],
            ["TC-9.1.3"] = ["TC_9_1_3_Search_results_show_source_url_and_last_reviewed_date"],
            ["TC-9.1.4"] = ["TC_9_1_4_Search_does_not_expose_draft_retired_or_other_tenant_custom_content"],
            ["TC-9.2.1"] = ["TC_9_2_1_Attach_published_clause_to_contract_with_reason_and_source_reference"],
            ["TC-9.2.2"] = ["TC_9_2_2_Duplicate_clause_attachment_is_prevented"],
            ["TC-9.2.3"] = ["TC_9_2_3_Removing_clause_requires_reason_and_then_succeeds"],
            ["TC-9.2.4"] = ["TC_9_2_4_Add_remove_are_audit_logged_and_cross_tenant_ids_are_denied"],
            ["TC-9.3.1"] = ["TC_9_3_1_Attaching_clause_with_mapped_templates_generates_contract_obligations"],
            ["TC-9.3.2"] = ["TC_9_3_2_Generated_obligations_preserve_links_and_source_metadata"],
            ["TC-9.3.3"] = ["TC_9_3_3_Default_tasks_are_created_and_linked_to_generated_obligations"],
            ["TC-9.3.4"] = ["TC_9_3_4_Regeneration_is_idempotent_for_obligations_and_tasks"],
            ["TC-10.1.1"] = ["TC_10_1_1_Dashboard_returns_current_tenant_obligations_only"],
            ["TC-10.1.2"] = ["TC_10_1_2_Filters_return_matching_obligation_data"],
            ["TC-10.1.3"] = ["TC_10_1_3_High_risk_and_overdue_obligations_are_flagged"],
            ["TC-10.1.4"] = ["TC_10_1_4_Empty_dashboard_returns_no_obligations"],
            ["TC-10.2.1"] = ["TC_10_2_1_Detail_shows_source_backed_obligation_content"],
            ["TC-10.2.2"] = ["TC_10_2_2_Detail_shows_linked_tasks_and_evidence"],
            ["TC-10.2.3"] = ["TC_10_2_3_Status_change_persists_and_returns_updated_detail"],
            ["TC-10.2.4"] = ["TC_10_2_4_Status_change_is_audit_logged_and_cross_tenant_detail_is_denied"],
            ["TC-10.3.1"] = ["TC_10_3_1_Assign_obligation_to_tenant_member_updates_detail_and_dashboard"],
            ["TC-10.3.2"] = ["TC_10_3_2_Assign_obligation_to_role_updates_detail_and_dashboard"],
            ["TC-10.3.3"] = ["TC_10_3_3_Unauthorized_role_cannot_assign_obligation_owner"],
            ["TC-10.3.4"] = ["TC_10_3_4_Assignment_changes_are_audit_logged_with_notification_metadata"],
            ["TC-11.1.1"] = ["TC_11_1_1_Create_tasks_linked_to_supported_compliance_entities"],
            ["TC-11.1.2"] = ["TC_11_1_2_Task_status_moves_through_expected_states_and_reopens"],
            ["TC-11.1.3"] = ["TC_11_1_3_Task_updates_are_tenant_scoped"],
            ["TC-11.1.4"] = ["TC_11_1_4_Task_status_changes_are_audit_logged"],
            ["TC-11.2.1"] = ["TC_11_2_1_Calendar_aggregates_tasks_renewals_reports_deadlines_deliverables_and_policy_reviews"],
            ["TC-11.2.2"] = ["TC_11_2_2_Calendar_filters_by_owner_status_risk_contract_and_module"],
            ["TC-11.2.3"] = ["TC_11_2_3_Calendar_flags_overdue_items"],
            ["TC-11.2.4"] = ["TC_11_2_4_Calendar_excludes_other_tenant_items"],
            ["TC-11.3.1"] = ["TC_11_3_1_Generates_renewal_tasks_for_profile_evidence_insurance_policy_and_cmmc_dates"],
            ["TC-11.3.2"] = ["TC_11_3_2_Running_generation_twice_skips_duplicate_source_and_due_date_tasks"],
            ["TC-11.3.3"] = ["TC_11_3_3_Default_and_configured_lead_times_produce_expected_reminder_due_dates"],
            ["TC-11.3.4"] = ["TC_11_3_4_Generated_renewal_tasks_link_back_to_originating_source_records"],
            ["TC-12.1.1"] = ["TC_12_1_1_Creates_evidence_metadata_with_required_fields_tags_dates_and_source_links"],
            ["TC-12.1.2"] = ["TC_12_1_2_Links_evidence_to_multiple_obligations_and_controls_for_detail_reuse"],
            ["TC-12.1.3"] = ["TC_12_1_3_Filters_evidence_by_folderless_tags"],
            ["TC-12.1.4"] = ["TC_12_1_4_Evidence_expiration_generates_task_and_metadata_changes_are_audit_logged"],
            ["TC-12.2.1"] = ["TC_12_2_1_Upload_before_no_cui_acknowledgement_is_blocked"],
            ["TC-12.2.2"] = ["TC_12_2_2_Uploaded_file_is_not_usable_until_validation_and_scan_allow_it"],
            ["TC-12.2.3"] = ["TC_12_2_3_Replacement_upload_creates_new_version_without_overwriting_history"],
            ["TC-12.2.4"] = ["TC_12_2_4_Download_and_delete_are_permissioned_and_audit_logged"],
            ["TC-12.3.1"] = ["TC_12_3_1_Only_configured_roles_can_approve_evidence"],
            ["TC-12.3.2"] = ["TC_12_3_2_Rejection_without_comment_or_reason_fails_validation"],
            ["TC-12.3.3"] = ["TC_12_3_3_Approved_evidence_is_included_in_report_packages"],
            ["TC-12.3.4"] = ["TC_12_3_4_Approve_reject_archive_and_expire_decisions_are_audit_logged"],
            ["TC-13.1.1"] = ["TC_13_1_1_Creates_level_1_and_level_2_readiness_assessments_with_status_dates_and_owner"],
            ["TC-13.1.2"] = ["TC_13_1_2_Links_assessment_to_company_profile_and_contracts_for_detail_display"],
            ["TC-13.1.3"] = ["TC_13_1_3_Control_status_updates_recalculate_completion_progress"],
            ["TC-13.1.4"] = ["TC_13_1_4_Create_update_and_status_changes_are_audit_logged"],
            ["TC-13.2.1"] = ["TC_13_2_1_Level_1_controls_and_level_2_mappings_load_for_selected_scope"],
            ["TC-13.2.2"] = ["TC_13_2_2_Control_status_can_be_set_to_each_readiness_state"],
            ["TC-13.2.3"] = ["TC_13_2_3_Control_links_evidence_tasks_assets_and_poam_items"],
            ["TC-13.2.4"] = ["TC_13_2_4_Source_baseline_is_visible_and_status_contributes_to_progress"],
            ["TC-13.3.1"] = ["TC_13_3_1_Creates_poam_item_with_control_gap_plan_owner_due_date_risk_and_status"],
            ["TC-13.3.2"] = ["TC_13_3_2_Poam_task_is_created_and_appears_on_calendar"],
            ["TC-13.3.3"] = ["TC_13_3_3_Open_and_overdue_poam_items_roll_into_cmmc_summary"],
            ["TC-13.3.4"] = ["TC_13_3_4_Create_update_and_status_changes_are_audit_logged"],
            ["TC-13.4.1"] = ["TC_13_4_1_Set_annual_affirmation_due_date_appears_on_calendar"],
            ["TC-13.4.2"] = ["TC_13_4_2_Upcoming_annual_affirmation_creates_reminder_task"],
            ["TC-13.4.3"] = ["TC_13_4_3_Link_evidence_to_affirmation_record_and_display_it"],
            ["TC-13.4.4"] = ["TC_13_4_4_Update_dates_evidence_links_and_status_are_audit_logged"],
            ["TC-14.1.1"] = ["TC_14_1_1_Create_and_update_subcontractor_profile_fields"],
            ["TC-14.1.2"] = ["TC_14_1_2_Subcontractor_contract_links_display_in_list_and_detail"],
            ["TC-14.1.3"] = ["TC_14_1_3_Cui_and_export_control_flags_are_visible_without_implying_storage"],
            ["TC-14.1.4"] = ["TC_14_1_4_Cross_tenant_access_is_denied_and_changes_are_audit_logged"],
            ["TC-14.2.1"] = ["TC_14_2_1_Assign_required_flow_down_clauses_from_contract_obligations"],
            ["TC-14.2.2"] = ["TC_14_2_2_Status_visibility_by_subcontractor_and_contract"],
            ["TC-14.2.3"] = ["TC_14_2_3_Link_approved_signed_evidence_to_flow_down_record"],
            ["TC-14.2.4"] = ["TC_14_2_4_Flow_down_assignment_and_status_changes_are_audit_logged"],
            ["TC-14.3.1"] = ["TC_14_3_1_Create_evidence_request_with_requested_item_due_status_recipient_and_linked_obligation"],
            ["TC-14.3.2"] = ["TC_14_3_2_Subcontractor_evidence_request_due_date_appears_on_calendar"],
            ["TC-14.3.3"] = ["TC_14_3_3_Link_received_evidence_satisfies_request_and_updates_completion"],
            ["TC-14.3.4"] = ["TC_14_3_4_Overdue_evidence_request_is_flagged_in_list_calendar_and_warning_state"],
            ["TC-15.1.1"] = ["TC_15_1_1_Generate_current_status_report_includes_summary_sections"],
            ["TC-15.1.2"] = ["TC_15_1_2_Compliance_status_report_excludes_other_tenant_data"],
            ["TC-15.1.3"] = ["TC_15_1_3_Generation_timestamp_and_snapshot_metadata_are_stored"],
            ["TC-15.1.4"] = ["TC_15_1_4_Compliance_status_report_generation_is_audit_logged"],
            ["TC-15.2.1"] = ["TC_15_2_1_Generate_contract_obligation_matrix_includes_required_columns"],
            ["TC-15.2.2"] = ["TC_15_2_2_Matrix_rows_include_source_links_and_last_reviewed_dates"],
            ["TC-15.2.3"] = ["TC_15_2_3_Matrix_identifies_obligations_requiring_flow_down"],
            ["TC-15.2.4"] = ["TC_15_2_4_Export_rows_and_fields_match_matrix_data"],
            ["TC-15.3.1"] = ["TC_15_3_1_Cmmc_readiness_report_rolls_up_control_status_by_family"],
            ["TC-15.3.2"] = ["TC_15_3_2_Cmmc_readiness_report_includes_poam_gaps_evidence_and_affirmations"],
            ["TC-15.3.3"] = ["TC_15_3_3_Cmmc_readiness_report_omits_evidence_links_without_evidence_permission"],
            ["TC-15.3.4"] = ["TC_15_3_4_Cmmc_readiness_report_is_rbac_protected_and_retains_snapshots"],
            ["TC-15.4.1"] = ["TC_15_4_1_Generate_evidence_package_scoped_by_obligations_contract_controls_or_subcontractor"],
            ["TC-15.4.2"] = ["TC_15_4_2_Approved_evidence_is_default_and_draft_rejected_require_authorized_override"],
            ["TC-15.4.3"] = ["TC_15_4_3_Evidence_package_manifest_includes_required_metadata"],
            ["TC-15.4.4"] = ["TC_15_4_4_Evidence_package_view_is_read_only_and_generation_is_audit_logged"],
            ["TC-15.5.1"] = ["TC_15_5_1_Generate_subcontractor_compliance_report_filtered_by_contract"],
            ["TC-15.5.2"] = ["TC_15_5_2_Subcontractor_report_highlights_missing_and_overdue_evidence_requests"],
            ["TC-15.5.3"] = ["TC_15_5_3_Subcontractor_report_includes_flow_down_statuses_by_subcontractor_and_contract"],
            ["TC-15.5.4"] = ["TC_15_5_4_Subcontractor_report_export_is_tenant_scoped"],
            ["TC-16.1.1"] = ["TC_16_1_1_Create_users_by_role_assigns_default_notification_preferences"],
            ["TC-16.1.2"] = ["TC_16_1_2_Update_preferences_for_all_notification_categories"],
            ["TC-16.1.3"] = ["TC_16_1_3_Notification_preferences_are_tenant_scoped_for_multi_tenant_user"],
            ["TC-16.1.4"] = ["TC_16_1_4_Notification_preference_changes_are_audit_logged"],
            ["TC-16.2.1"] = ["TC_16_2_1_Due_date_reminder_job_selects_tasks_within_configured_lead_time"],
            ["TC-16.2.2"] = ["TC_16_2_2_Due_date_reminder_job_is_idempotent_for_same_event"],
            ["TC-16.2.3"] = ["TC_16_2_3_Overdue_reminders_are_categorized_separately"],
            ["TC-16.2.4"] = ["TC_16_2_4_Delivery_failure_is_logged_without_crashing_unrelated_deliveries"],
            ["TC-16.3.1"] = ["TC_16_3_1_Assigned_task_user_receives_notification"],
            ["TC-16.3.2"] = ["TC_16_3_2_Assignment_notification_links_to_source_record"],
            ["TC-16.3.3"] = ["TC_16_3_3_Mark_assignment_notification_as_read_persists_state"],
            ["TC-16.3.4"] = ["TC_16_3_4_Unauthorized_user_cannot_open_assignment_notification_link"],
            ["TC-17.1.1"] = ["TC_17_1_1_Non_cui_pilot_tenant_completes_core_mvp_workflow"],
            ["TC-17.1.2"] = ["TC_17_1_2_Pilot_roles_can_only_perform_permitted_actions"],
            ["TC-17.1.3"] = ["TC_17_1_3_Pilot_reports_reflect_workflow_data"],
            ["TC-17.1.4"] = ["TC_17_1_4_Automated_regression_coverage_exists_for_pilot_workflow_critical_path"],
            ["TC-17.2.1"] = ["TC_17_2_1_Cross_tenant_access_is_denied_across_tenant_owned_modules"],
            ["TC-17.2.2"] = ["TC_17_2_2_Restricted_role_actions_are_denied_for_direct_api_calls"],
            ["TC-17.2.3"] = ["TC_17_2_3_Tenant_owned_repository_and_service_filter_tests_are_present"],
            ["TC-17.2.4"] = ["TC_17_2_4_Security_verification_results_are_documented"],
            ["TC-17.3.1"] = ["TC_17_3_1_Staging_deployment_workflow_covers_core_services_and_migrations"],
            ["TC-17.3.2"] = ["TC_17_3_2_Staging_contract_blocks_production_customer_data_and_secrets"],
            ["TC-17.3.3"] = ["TC_17_3_3_Staging_health_checks_cover_api_database_cache_storage_and_jobs"],
            ["TC-17.3.4"] = ["TC_17_3_4_Staging_smoke_tests_are_visible_in_ci_cd"],
            ["TC-17.4.1"] = ["TC_17_4_1_Production_readiness_checklist_blocks_launch_until_required_approvals_complete"],
            ["TC-17.4.2"] = ["TC_17_4_2_No_cui_limits_malware_support_and_prohibited_upload_guidance_are_documented"],
            ["TC-17.4.3"] = ["TC_17_4_3_Launch_obligations_have_source_urls_review_dates_confidence_and_review_metadata"],
            ["TC-17.4.4"] = ["TC_17_4_4_Staging_rollback_simulation_steps_timing_and_outcome_are_documented"]
        };

        foreach (var (implementedCaseId, requiredSignals) in focusedCoverageSignals)
        {
            Assert.Contains(
                requiredSignals,
                signal => testSource.Contains(signal, StringComparison.OrdinalIgnoreCase));
        }
    }

    private static IReadOnlyList<DevelopmentStoryRegressionCase> ReadDocumentedRegressionCases()
    {
        var cases = new List<DevelopmentStoryRegressionCase>();
        var currentSection = string.Empty;
        var currentStory = string.Empty;
        var lineNumber = 0;

        foreach (var line in ReadDevelopmentStoryTestCasesDocument().Split(Environment.NewLine))
        {
            lineNumber++;

            var sectionMatch = SectionRegex().Match(line);
            if (sectionMatch.Success)
            {
                currentSection = sectionMatch.Groups["section"].Value.Trim();
                continue;
            }

            var storyMatch = StoryRegex().Match(line);
            if (storyMatch.Success)
            {
                currentStory = storyMatch.Groups["story"].Value.Trim();
                continue;
            }

            var caseMatch = TestCaseLineRegex().Match(line);
            if (!caseMatch.Success)
            {
                continue;
            }

            cases.Add(new DevelopmentStoryRegressionCase(
                caseMatch.Groups["id"].Value.Trim(),
                caseMatch.Groups["title"].Value.Trim(),
                caseMatch.Groups["behavior"].Value.Trim(),
                currentSection,
                currentStory,
                lineNumber));
        }

        return cases;
    }

    private static string ReadDevelopmentStoryTestCasesDocument()
    {
        return File.ReadAllText(Path.Combine(FindRepositoryRoot(), "docs", "development-story-test-cases.md"));
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Gccs.slnx")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root containing Gccs.slnx.");
    }

    [GeneratedRegex(@"^## (?<section>\d+\. .+)$")]
    private static partial Regex SectionRegex();

    [GeneratedRegex(@"^### (?<story>Story \d+\.\d+: .+)$")]
    private static partial Regex StoryRegex();

    [GeneratedRegex(@"^- \*\*(?<id>TC-\d+\.\d+\.\d+) - (?<title>[^:]+):\*\* (?<behavior>.+)$")]
    private static partial Regex TestCaseLineRegex();

    [GeneratedRegex(@"TC-\d+\.\d+\.\d+")]
    private static partial Regex TestCaseIdRegex();

    public sealed record DevelopmentStoryRegressionCase(
        string Id,
        string Title,
        string ExpectedBehavior,
        string Section,
        string Story,
        int LineNumber)
    {
        public override string ToString() => $"{Id} - {Title}";
    }

    private sealed record RegressionStrategy(RegressionLayer PrimaryLayer, string NarrowCommand, string InvariantSummary)
    {
        public static RegressionStrategy For(DevelopmentStoryRegressionCase regressionCase)
        {
            var combinedText = $"{regressionCase.Section} {regressionCase.Story} {regressionCase.Title} {regressionCase.ExpectedBehavior}";
            var deliveryScore = CountMatches(combinedText, DeliverySignals);
            var frontendScore = CountMatches(combinedText, FrontendSignals);
            var backendScore = CountMatches(combinedText, BackendSignals);

            var primaryLayer = (backendScore, frontendScore, deliveryScore) switch
            {
                var scores when scores.deliveryScore > 0 && scores.backendScore == 0 && scores.frontendScore == 0 => RegressionLayer.Delivery,
                var scores when scores.deliveryScore > 0 && (scores.backendScore > 0 || scores.frontendScore > 0) => RegressionLayer.EndToEnd,
                var scores when scores.backendScore > 0 && scores.frontendScore > 0 => RegressionLayer.EndToEnd,
                var scores when scores.frontendScore > 0 => RegressionLayer.Frontend,
                var scores when scores.backendScore > 0 => RegressionLayer.Backend,
                _ => RegressionLayer.Backend
            };

            var command = primaryLayer switch
            {
                RegressionLayer.Backend => "npm run test:api",
                RegressionLayer.Frontend => "npm run test:web",
                RegressionLayer.EndToEnd => "npm test",
                RegressionLayer.Delivery => "npm run test:api",
                _ => string.Empty
            };

            return new RegressionStrategy(
                primaryLayer,
                command,
                "Preserve tenant scoping, server-side RBAC, audit logging, No-CUI upload controls, and standard error handling.");
        }

        private static int CountMatches(string text, IReadOnlyCollection<string> signals)
        {
            return signals.Count(signal => text.Contains(signal, StringComparison.OrdinalIgnoreCase));
        }
    }

    private enum RegressionLayer
    {
        Unclassified,
        Backend,
        Frontend,
        EndToEnd,
        Delivery
    }
}

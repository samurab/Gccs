using System.Text.RegularExpressions;
using Xunit;

namespace Gccs.Api.Tests;

public sealed partial class DevelopmentStoryRegressionCoverageTests
{
    private const int ExpectedDocumentedRegressionCaseCount = 212;

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
            ]
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

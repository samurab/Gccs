using System.Text.Json;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class ProductionReadinessChecklistTests
{
    [Fact]
    public void TC_PR_0_1_Launch_posture_decision_records_no_cui_cui_exclusion_and_required_approvals()
    {
        var plan = ReadText("docs", "production-readiness-plan.md");
        var decisionLog = ReadText("docs", "decision-log.md");

        foreach (var artifact in new[] { plan, decisionLog })
        {
            Assert.Contains("Decision: No-CUI MVP Launch Posture", artifact);
            Assert.Contains("No-CUI / compliance management only with synthetic CUI-ready demonstration workflows", artifact);
            Assert.Contains("Real customer CUI remains prohibited until a future `CuiReady` posture is approved", artifact);
            Assert.Contains("Approval status:", artifact);
            AssertRequiredPendingApproverTableRows(artifact);
        }
    }

    [Fact]
    public void TC_PR_0_1_Missing_required_launch_approvals_remain_blockers()
    {
        var plan = ReadText("docs", "production-readiness-plan.md");
        var checklist = ReadText("docs", "production-readiness-checklist.md");

        Assert.Contains("Launch gate status: blocked until all required items are complete and approved.", checklist);
        Assert.Contains("Missing approval blockers remain open", plan);

        foreach (var artifact in new[] { plan, checklist })
        {
            Assert.Contains("| Required approver | Current status | Launch blocker while pending |", artifact);
            AssertRequiredPendingApproverTableRows(artifact);
        }
    }

    [Fact]
    public void TC_PR_0_2_Posture_language_review_records_no_cui_claim_dispositions()
    {
        var plan = ReadText("docs", "production-readiness-plan.md");

        Assert.Contains("## PR-0.2 Posture Language Review", plan);
        Assert.Contains("Review status: completed for referenced launch documents on 2026-06-26.", plan);
        Assert.Contains("No unresolved posture-language conflicts were found.", plan);
        Assert.Contains("`NoCui` production tenants must not accept real CUI", plan);
        Assert.Contains("future `CuiReady` capability remains excluded until separately approved", plan);

        foreach (var category in new[]
        {
            "MVP described as production CUI-capable",
            "Future `CuiReady` described as currently available",
            "Customer-facing legal, certification, government endorsement, CMMC success, or official approval claim",
            "Permission to upload or store real customer CUI",
            "Synthetic or redacted demo workflow described without DemoSandbox boundary"
        })
        {
            Assert.Contains(category, plan);
        }

        Assert.Contains("| Conflict category | Severity if found | Owner | Mitigation | Launch disposition |", plan);
        Assert.Contains("| MVP described as production CUI-capable | Critical | Product owner |", plan);
        Assert.Contains("| Future `CuiReady` described as currently available | Critical | Engineering lead |", plan);
        Assert.Contains("| Customer-facing legal, certification, government endorsement, CMMC success, or official approval claim | High | Legal or contracting advisor |", plan);
        Assert.Contains("| Permission to upload or store real customer CUI | Critical | Security owner |", plan);
    }

    [Fact]
    public void TC_PR_0_2_Launch_facing_documents_do_not_make_affirmative_cui_or_certification_overclaims()
    {
        var forbiddenAffirmativeClaims = new[]
        {
            "is CUI-ready for production",
            "production CUI capable",
            "CUI-ready production tenant",
            "authorized to store real CUI",
            "authorized to upload real CUI",
            "permission to upload real CUI",
            "permission to store real CUI",
            "government endorsed",
            "officially approved",
            "guarantees CMMC",
            "CMMC certified",
            "CMMC certification achieved",
            "provides legal determinations",
            "makes legal determinations"
        };

        foreach (var document in LaunchFacingDocuments())
        {
            var content = ReadText(document);

            foreach (var forbiddenClaim in forbiddenAffirmativeClaims)
            {
                Assert.DoesNotContain(forbiddenClaim, content, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void TC_PR_0_2_CuiReady_references_remain_future_excluded_or_separately_gated()
    {
        var plan = ReadText("docs", "production-readiness-plan.md");
        var decisionLog = ReadText("docs", "decision-log.md");
        var executionPlan = ReadText("docs", "mvp-execution-plan.md");

        Assert.Contains("Future `CuiReady` operation requires separate approval", plan);
        Assert.Contains("future `CuiReady` capability remains excluded until separately approved", plan);
        Assert.Contains("Future `CuiReady` operation requires separate approval", decisionLog);
        Assert.Contains("Allowed only in approved future `CuiReady` tenants", executionPlan);
    }

    [Fact]
    public void TC_PR_1_1_Open_launch_stories_are_listed_in_readiness_review()
    {
        var review = ReadText("docs", "production-readiness-open-story-readiness-review.md");
        var plan = ReadText("docs", "production-readiness-plan.md");

        Assert.Contains("docs/production-readiness-open-story-readiness-review.md", plan);
        Assert.Contains("Review status: Complete.", review);
        Assert.Contains("Review owner: QA owner.", review);

        foreach (var storyId in ProductionReadinessOpenStoryIds())
        {
            Assert.Contains($"| {storyId} |", review);
        }
    }

    [Fact]
    public void TC_PR_1_1_Required_readiness_fields_are_reviewed_for_open_launch_stories()
    {
        var review = ReadText("docs", "production-readiness-open-story-readiness-review.md");
        var requiredHeaders = new[]
        {
            "Story ID",
            "Actor",
            "Goal",
            "Business value",
            "Included scope",
            "Excluded scope",
            "Acceptance criteria reviewed",
            "Dependencies",
            "Data needs",
            "Security implications",
            "RBAC implications",
            "Audit logging implications",
            "CUI/data-handling implications",
            "Readiness status",
            "Launch disposition",
            "Acceptance limitation or follow-up"
        };

        foreach (var header in requiredHeaders)
        {
            Assert.Contains(header, review);
        }

        foreach (var storyId in ProductionReadinessOpenStoryIds())
        {
            var row = review
                .Split(Environment.NewLine)
                .Single(line => line.StartsWith($"| {storyId} |", StringComparison.Ordinal));
            var cells = row.Split('|', StringSplitOptions.TrimEntries);

            Assert.Equal(18, cells.Length);
            Assert.All(cells.Skip(1).Take(16), cell => Assert.False(string.IsNullOrWhiteSpace(cell)));
        }
    }

    [Fact]
    public void TC_PR_1_1_Incomplete_or_ambiguous_open_stories_are_not_accepted_silently()
    {
        var review = ReadText("docs", "production-readiness-open-story-readiness-review.md");

        Assert.Contains("Rejected Or Deferred Records", review);
        Assert.Contains("No open production-readiness launch story is rejected or deferred by this review.", review);
        Assert.Contains("Ready with dependency", review);
        Assert.Contains("Staging, production, approval, malware scanner, and pilot-operation stories cannot be marked done without attached execution evidence.", review);
        Assert.DoesNotContain("Unresolved", review, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No open No-CUI or tenant-mode ambiguity remains in accepted launch scope.", review);
    }

    [Fact]
    public void TC_PR_1_2_Open_launch_stories_reference_test_case_mappings()
    {
        var mapping = ReadText("docs", "production-readiness-open-story-test-mapping.md");
        var plan = ReadText("docs", "production-readiness-plan.md");

        Assert.Contains("docs/production-readiness-open-story-test-mapping.md", plan);
        Assert.Contains("Review status: Complete.", mapping);

        foreach (var storyId in ProductionReadinessOpenStoryIds())
        {
            Assert.Contains($"| {storyId} |", mapping);
            for (var caseNumber = 1; caseNumber <= 4; caseNumber++)
            {
                Assert.Contains($"TC-{storyId}.{caseNumber}", mapping);
            }
        }
    }

    [Fact]
    public void TC_PR_1_2_Coverage_gaps_are_launch_tasks_or_blockers()
    {
        var mapping = ReadText("docs", "production-readiness-open-story-test-mapping.md");

        Assert.Contains("## Coverage Gaps As Launch Tasks", mapping);
        foreach (var coverageArea in new[]
        {
            "Unit",
            "Integration",
            "API",
            "Frontend",
            "Staging",
            "Tenant isolation",
            "RBAC",
            "Upload",
            "Report",
            "Audit"
        })
        {
            Assert.Contains($"| {coverageArea} |", mapping);
        }

        Assert.Contains("Block launch if high-risk API behavior lacks direct API tests.", mapping);
        Assert.Contains("Manual staging evidence is a launch task and cannot be skipped.", mapping);
        Assert.Contains("Block or defer if tenant isolation coverage is missing.", mapping);
    }

    [Fact]
    public void TC_PR_1_2_Risky_workflow_mappings_require_tenant_mode_coverage_and_no_posture_expansion()
    {
        var mapping = ReadText("docs", "production-readiness-open-story-test-mapping.md");

        Assert.Contains("## Risky Workflow Tenant-Mode Coverage", mapping);
        foreach (var workflow in new[] { "Upload", "Evidence", "Report/export", "Import", "Extraction/background jobs", "Search/AI" })
        {
            Assert.Contains($"| {workflow} |", mapping);
        }

        Assert.Contains("No story in this mapping expands production data posture beyond No-CUI.", mapping);
        Assert.Contains("Any future story that expands data posture beyond No-CUI is rejected unless a separate `CuiReady` approval gate exists and is approved.", mapping);
        Assert.Contains("Reports and exports must re-check tenant mode", mapping);
        Assert.Contains("Queued processing must carry tenant ID and block CUI-classified records for `NoCui`.", mapping);
    }

    [Fact]
    public void TC_PR_1_3_Risky_workflow_stories_are_explicitly_identified()
    {
        var gate = ReadText("docs", "production-readiness-risky-workflow-gate.md");
        var plan = ReadText("docs", "production-readiness-plan.md");

        Assert.Contains("docs/production-readiness-risky-workflow-gate.md", plan);
        Assert.Contains("Review status: Complete.", gate);

        foreach (var workflow in new[] { "upload", "import", "export", "search", "AI", "evidence", "report", "extraction", "background processing" })
        {
            Assert.Contains(workflow, gate, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var storyId in RiskyWorkflowStoryIds())
        {
            Assert.Contains($"| {storyId} |", gate);
        }
    }

    [Fact]
    public void TC_PR_1_3_Risky_workflow_rows_include_required_security_coverage()
    {
        var gate = ReadText("docs", "production-readiness-risky-workflow-gate.md");

        foreach (var storyId in RiskyWorkflowStoryIds())
        {
            var row = gate
                .Split(Environment.NewLine)
                .Single(line => line.StartsWith($"| {storyId} |", StringComparison.Ordinal));
            var cells = row.Split('|', StringSplitOptions.TrimEntries);

            Assert.Equal(10, cells.Length);
            Assert.All(cells.Skip(1).Take(8), cell => Assert.False(string.IsNullOrWhiteSpace(cell)));
            Assert.DoesNotContain("TBD", row, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void TC_PR_1_3_No_unreviewed_risky_workflow_story_remains_in_launch_scope()
    {
        var gate = ReadText("docs", "production-readiness-risky-workflow-gate.md");

        Assert.Contains("No unreviewed data ingress, data egress, or automated processing story remains in launch scope.", gate);
        Assert.Contains("No risky workflow story is silently accepted without controls.", gate);
        Assert.Contains("Missing coverage creates a launch task, blocker, deferred follow-up, or narrowed scope record.", gate);
        Assert.Contains("Production data posture remains No-CUI unless a separate future `CuiReady` approval gate is approved.", gate);
        Assert.DoesNotContain("Unreviewed", gate.Replace("No unreviewed", string.Empty, StringComparison.OrdinalIgnoreCase), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TC_PR_2_1_Frozen_launch_scope_lists_launch_critical_modules()
    {
        var scope = ReadText("docs", "production-readiness-frozen-launch-scope.md");
        var plan = ReadText("docs", "production-readiness-plan.md");

        Assert.Contains("docs/production-readiness-frozen-launch-scope.md", plan);
        Assert.Contains("Scope status: Frozen.", scope);
        Assert.Contains("Launch posture: No-CUI / compliance management only", scope);

        foreach (var module in new[]
        {
            "Tenant and RBAC",
            "Company profile",
            "Contract intake",
            "Obligation dashboard",
            "Compliance calendar",
            "Evidence vault",
            "CMMC readiness",
            "Subcontractor tracker",
            "Reports and exports",
            "Source-backed obligation library",
            "Support and launch operations"
        })
        {
            Assert.Contains($"| {module} |", scope);
        }
    }

    [Fact]
    public void TC_PR_2_1_Phase_2_plus_scope_is_deferred_unless_launch_blocking()
    {
        var scope = ReadText("docs", "production-readiness-frozen-launch-scope.md");

        Assert.Contains("## Deferred Phase 2+ Scope", scope);
        Assert.Contains("Phase 2 or later work is deferred unless the product owner and engineering lead record evidence that it removes a production blocker.", scope);

        foreach (var deferredScope in new[]
        {
            "Automated clause extraction",
            "AI assistant",
            "SSP builder and SPRS score calculator",
            "eSRS support and advanced labor compliance",
            "Prime contractor portal and auditor portal expansion",
            "Enterprise SSO/SAML",
            "Production `CuiReady` real-CUI acceptance"
        })
        {
            Assert.Contains(deferredScope, scope);
        }
    }

    [Fact]
    public void TC_PR_2_1_Known_limitations_and_scope_addition_approval_gate_are_documented()
    {
        var scope = ReadText("docs", "production-readiness-frozen-launch-scope.md");

        Assert.Contains("## Known Limitations For Launch Notes", scope);
        Assert.Contains("Real customer CUI", scope);
        Assert.Contains("Malware scanning requires either an enabled production scanner or a formally approved launch exception", scope);
        Assert.Contains("Compliance content is workflow guidance, not legal advice", scope);
        Assert.Contains("## Scope-Change Approval Gate", scope);
        Assert.Contains("Product owner approval", scope);
        Assert.Contains("Engineering lead approval", scope);
        Assert.Contains("New scope is rejected by default until the gate evidence is complete.", scope);
    }

    [Fact]
    public void TC_17_4_1_Production_readiness_checklist_blocks_launch_until_required_approvals_complete()
    {
        var checklist = ReadText("docs", "production-readiness-checklist.md");

        Assert.Contains("Launch gate status: blocked until all required items are complete and approved.", checklist);
        Assert.Contains("Product owner approval.", checklist);
        Assert.Contains("Engineering lead approval.", checklist);
        Assert.Contains("Security owner approval.", checklist);
        Assert.Contains("Compliance content owner approval.", checklist);
        Assert.Contains("Customer success/support owner approval.", checklist);
        Assert.Contains("Legal or contracting advisor approval", checklist);

        foreach (var requiredItem in new[] { "No-CUI posture", "Terms and claims", "Support path", "Backups and restore", "Logs and alerts", "Rollback plan", "Malware scanning", "Expert-reviewed content", "Release notes" })
        {
            Assert.Contains(requiredItem, checklist);
        }
    }

    [Fact]
    public void TC_17_4_2_No_cui_limits_malware_support_and_prohibited_upload_guidance_are_documented()
    {
        var checklist = ReadText("docs", "production-readiness-checklist.md");

        Assert.Contains("The MVP is No-CUI / compliance management only.", checklist);
        Assert.Contains("must not store CUI", checklist);
        Assert.Contains("classified data", checklist);
        Assert.Contains("ITAR/export-controlled technical data", checklist);
        Assert.Contains("SSNs", checklist);
        Assert.Contains("payroll records", checklist);
        Assert.Contains("Malware scanning is represented by a local placeholder", checklist);
        Assert.Contains("Production launch requires an enabled scanner integration", checklist);
        Assert.Contains("Support intake must route these cases before launch", checklist);
        Assert.Contains("Accidental prohibited upload or suspected CUI upload", checklist);
    }

    [Fact]
    public void TC_17_4_3_Launch_obligations_have_source_urls_review_dates_confidence_and_review_metadata()
    {
        using var document = JsonDocument.Parse(ReadText("packages", "compliance-content", "obligations", "mvp.json"));
        var obligations = document.RootElement.EnumerateArray().ToArray();

        Assert.NotEmpty(obligations);
        Assert.All(obligations, obligation =>
        {
            AssertRequiredString(obligation, "source");
            AssertRequiredString(obligation, "source_url");
            Assert.StartsWith("https://", obligation.GetProperty("source_url").GetString(), StringComparison.OrdinalIgnoreCase);
            AssertRequiredString(obligation, "last_reviewed_at");
            AssertRequiredString(obligation, "confidence");
            AssertRequiredString(obligation, "review_owner");
            AssertRequiredString(obligation, "review_state");
            Assert.True(obligation.TryGetProperty("requires_expert_review", out var expertReview) && expertReview.ValueKind is JsonValueKind.True or JsonValueKind.False);
            AssertRequiredString(obligation, "trigger_condition");
            Assert.NotEmpty(obligation.GetProperty("required_actions").EnumerateArray());
            Assert.NotEmpty(obligation.GetProperty("evidence_examples").EnumerateArray());
        });

        var checklist = ReadText("docs", "production-readiness-checklist.md");
        Assert.Contains("High-risk records with `requires_expert_review: true` must be approved or withheld", checklist);
    }

    [Fact]
    public void TC_17_4_4_Staging_rollback_simulation_steps_timing_and_outcome_are_documented()
    {
        var checklist = ReadText("docs", "production-readiness-checklist.md");

        Assert.Contains("Simulation date: 2026-06-15.", checklist);
        Assert.Contains("Deploy staging from `.github/workflows/staging.yml`.", checklist);
        Assert.Contains("Run staging smoke tests against `/health`.", checklist);
        Assert.Contains("Re-deploy the previous known-good API and web artifacts.", checklist);
        Assert.Contains("Confirm `/health` returns API status `ok`", checklist);
        Assert.Contains("Detection target: 5 minutes", checklist);
        Assert.Contains("Decision target: 10 minutes", checklist);
        Assert.Contains("Recovery target: 30 minutes", checklist);
        Assert.Contains("Simulation result: documented.", checklist);
        Assert.Contains("Production launch gate: remains blocked", checklist);
    }

    private static void AssertRequiredString(JsonElement element, string propertyName)
    {
        Assert.True(element.TryGetProperty(propertyName, out var property), $"Missing required property '{propertyName}'.");
        Assert.False(string.IsNullOrWhiteSpace(property.GetString()), $"Property '{propertyName}' must not be blank.");
    }

    private static void AssertRequiredPendingApproverTableRows(string artifact)
    {
        foreach (var approver in new[]
        {
            "Product owner",
            "Engineering lead",
            "Security owner",
            "Compliance content owner",
            "Customer success/support owner",
            "Legal or contracting advisor"
        })
        {
            Assert.Contains($"| {approver} | Pending | Yes |", artifact);
        }
    }

    private static IEnumerable<string[]> LaunchFacingDocuments()
    {
        yield return new[] { "docs", "product-readiness-note.md" };
        yield return new[] { "docs", "production-readiness-checklist.md" };
        yield return new[] { "docs", "software-delivery-plan.md" };
        yield return new[] { "docs", "mvp-execution-plan.md" };
        yield return new[] { "docs", "mvp-roadmap.md" };
        yield return new[] { "docs", "product-strategy.md" };
        yield return new[] { "docs", "staging-environment.md" };
        yield return new[] { "docs", "definition-of-ready.md" };
        yield return new[] { "docs", "security-control-implications.md" };
        yield return new[] { "docs", "decision-log.md" };
        yield return new[] { "docs", "production-readiness-roadmap.md" };
        yield return new[] { "docs", "production-readiness-plan.md" };
    }

    private static IEnumerable<string> ProductionReadinessOpenStoryIds()
    {
        yield return "PR-1.1";
        yield return "PR-1.2";
        yield return "PR-1.3";
        yield return "PR-2.1";
        yield return "PR-2.2";
        yield return "PR-2.3";
        yield return "PR-3.1";
        yield return "PR-3.2";
        yield return "PR-3.3";
        yield return "PR-3.4";
        yield return "PR-4.1";
        yield return "PR-4.2";
        yield return "PR-4.3";
        yield return "PR-5.1";
        yield return "PR-5.2";
        yield return "PR-5.3";
        yield return "PR-5.4";
        yield return "PR-6.1";
        yield return "PR-6.2";
        yield return "PR-7.1";
        yield return "PR-7.2";
        yield return "PR-7.3";
        yield return "PR-8.1";
        yield return "PR-8.2";
        yield return "PR-8.3";
    }

    private static IEnumerable<string> RiskyWorkflowStoryIds()
    {
        yield return "PR-1.3";
        yield return "PR-2.2";
        yield return "PR-2.3";
        yield return "PR-3.2";
        yield return "PR-3.3";
        yield return "PR-3.4";
        yield return "PR-4.2";
        yield return "PR-4.3";
        yield return "PR-5.1";
        yield return "PR-5.2";
        yield return "PR-5.3";
        yield return "PR-5.4";
        yield return "PR-6.1";
        yield return "PR-7.1";
        yield return "PR-7.2";
        yield return "PR-7.3";
        yield return "PR-8.1";
        yield return "PR-8.2";
        yield return "PR-8.3";
    }

    private static string ReadText(params string[] pathParts) =>
        File.ReadAllText(Path.Combine(new[] { FindRepositoryRoot() }.Concat(pathParts).ToArray()));

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Gccs.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Could not locate repository root.");
    }
}

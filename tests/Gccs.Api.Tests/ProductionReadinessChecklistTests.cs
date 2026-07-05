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
    public void TC_PR_0_1_Required_launch_approval_gate_tracks_current_state()
    {
        var plan = ReadText("docs", "production-readiness-plan.md");
        var checklist = ReadText("docs", "production-readiness-checklist.md");

        Assert.Contains("Launch gate status: blocked until all required items are complete and approved.", checklist);
        Assert.Contains("Required product, engineering, security, compliance content, support, and legal/contracting approvals are complete", plan);

        Assert.Contains("| Required approver | Current status | Launch blocker while pending |", plan);
        AssertRequiredPendingApproverTableRows(plan);
        Assert.Contains("| Required approver | Current status | Launch blocker while pending |", checklist);
        AssertRequiredApprovedApproverTableRows(checklist);
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
    public void TC_PR_5_2_1_Customer_facing_claim_review_records_search_scope_and_no_affirmative_overclaims()
    {
        using var review = JsonDocument.Parse(ReadText("output", "production-readiness", "customer-claims-review.json"));

        Assert.Equal("PR-5.2", review.RootElement.GetProperty("story").GetString());
        Assert.Equal("completed-with-launch-approval-pending", review.RootElement.GetProperty("reviewStatus").GetString());
        Assert.True(review.RootElement.GetProperty("launchApprovalBlocker").GetBoolean());

        var surfaces = review.RootElement
            .GetProperty("customerFacingSurfaces")
            .EnumerateArray()
            .Select(surface => surface.GetProperty("surface").GetString()!)
            .ToArray();

        Assert.Equal(
            ["product_copy", "onboarding", "upload_flows", "reports", "support_materials", "release_notes", "pilot_onboarding"],
            surfaces);

        foreach (var document in ClaimReviewDocuments())
        {
            var content = ReadText(document);
            foreach (var forbiddenClaim in ForbiddenAffirmativeCustomerClaims())
            {
                Assert.DoesNotContain(forbiddenClaim, content, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void TC_PR_5_2_2_No_cui_launch_limits_are_present_in_onboarding_upload_support_and_release_materials()
    {
        var onboarding = ReadText("apps", "web", "src", "lib", "api.ts");
        var uploadFlow = ReadText("apps", "web", "src", "App.tsx");
        var supportAndRelease = ReadText("docs", "product-readiness-note.md") + Environment.NewLine +
            ReadText("docs", "production-readiness-checklist.md") + Environment.NewLine +
            ReadText("docs", "production-readiness-customer-claims-review.md");

        foreach (var content in new[] { onboarding, uploadFlow, supportAndRelease })
        {
            Assert.Contains("No-CUI", content, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("CUI", content, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("classified", content, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("export-controlled", content, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("not ready to store CUI", onboarding, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("I confirm this file does not contain CUI", uploadFlow, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Release notes", supportAndRelease, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("support paths", supportAndRelease, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TC_PR_5_2_3_Cmmc_guidance_and_reports_preserve_draft_only_or_workflow_guidance_language()
    {
        var app = ReadText("apps", "web", "src", "App.tsx");
        var productReadiness = ReadText("docs", "product-readiness-note.md");
        var stagingEvidence = ReadText("docs", "production-readiness-staging-upload-report-evidence.md");

        Assert.Contains("Reports are workflow guidance only", app);
        Assert.Contains("not legal advice, certification decisions, assessor determinations", app);
        Assert.Contains("draft-only guidance", productReadiness, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CMMC reports must avoid pass/fail or certification language", productReadiness, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CMMC readiness report generated with draft/readiness language", stagingEvidence, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TC_PR_5_2_4_Customer_facing_claim_review_status_is_recorded_before_launch_approval()
    {
        using var review = JsonDocument.Parse(ReadText("output", "production-readiness", "customer-claims-review.json"));
        var checklist = ReadText("docs", "production-readiness-checklist.md");
        var closure = ReadText("docs", "production-readiness-launch-closure-evidence.md");

        Assert.Equal("legal-or-contracting-advisor", review.RootElement.GetProperty("requiredReviewer").GetString());
        Assert.NotEmpty(review.RootElement.GetProperty("acceptedClaimRisks").EnumerateArray());
        Assert.NotEmpty(review.RootElement.GetProperty("blockers").EnumerateArray());
        Assert.Contains("solo-controlled pilot legal/contracting approval scope recorded in PR-6.1 approval record", checklist);
        Assert.Contains("Customer-facing claims", closure);
        Assert.Contains("legal/contracting scope", closure);
    }

    [Fact]
    public void TC_PR_5_3_1_Required_support_runbooks_exist()
    {
        var runbooks = ReadText("docs", "production-readiness-support-runbooks.md");

        foreach (var topic in SupportRunbookTopics())
        {
            Assert.Contains($"## Runbook: {topic}", runbooks);
        }
    }

    [Fact]
    public void TC_PR_5_3_2_Each_support_runbook_has_owner_triage_escalation_severity_and_evidence()
    {
        var runbooks = ReadText("docs", "production-readiness-support-runbooks.md");

        foreach (var topic in SupportRunbookTopics())
        {
            var section = ExtractRunbookSection(runbooks, topic);
            Assert.Contains("Owner:", section);
            Assert.Contains("Triage steps:", section);
            Assert.Contains("Escalation path:", section);
            Assert.Contains("Severity guidance:", section);
            Assert.Contains("Evidence to capture:", section);
        }
    }

    [Fact]
    public void TC_PR_5_3_3_Prohibited_upload_and_suspected_cui_runbooks_preserve_no_cui_containment()
    {
        var runbooks = ReadText("docs", "production-readiness-support-runbooks.md");

        foreach (var topic in new[] { "Prohibited Upload", "Suspected CUI" })
        {
            var section = ExtractRunbookSection(runbooks, topic);
            Assert.Contains("No-CUI containment:", section);
            Assert.Contains("block", section, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("escalation", section, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("No-CUI posture", section, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("do not", section, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void TC_PR_5_3_4_Support_routing_is_linked_from_launch_materials()
    {
        var runbooks = ReadText("docs", "production-readiness-support-runbooks.md");
        var checklist = ReadText("docs", "production-readiness-checklist.md");
        var closure = ReadText("docs", "production-readiness-launch-closure-evidence.md");

        Assert.Contains("## Support Routing", runbooks);
        Assert.Contains("docs/production-readiness-support-runbooks.md", checklist);
        Assert.Contains("Support runbooks", closure);
        Assert.Contains("prohibited upload, suspected CUI, tenant exposure", closure, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TC_PR_5_4_1_Pilot_onboarding_states_no_cui_limits_prohibited_data_support_and_synthetic_demo_scope()
    {
        var onboarding = ReadText("docs", "production-readiness-pilot-onboarding.md");

        Assert.Contains("No-CUI / compliance management only", onboarding);
        Assert.Contains("Real customer CUI", onboarding);
        Assert.Contains("Classified information", onboarding);
        Assert.Contains("ITAR or export-controlled technical data", onboarding);
        Assert.Contains("Support Paths", onboarding);
        Assert.Contains("Known Limitations", onboarding);
        Assert.Contains("Synthetic demo workflows", onboarding);
        Assert.Contains("do not authorize production storage", onboarding, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TC_PR_5_4_2_Release_notes_include_required_launch_sections()
    {
        var releaseNotes = ReadText("docs", "production-readiness-release-notes.md");

        foreach (var requiredSection in new[]
        {
            "Launch Posture",
            "Scope",
            "Exclusions",
            "Known Risks",
            "Support Paths",
            "Staging Smoke Results",
            "Rollback Plan",
            "Content Scope"
        })
        {
            Assert.Contains($"## {requiredSection}", releaseNotes);
        }

        Assert.Contains("No-CUI / compliance management only", releaseNotes);
        Assert.Contains("docs/production-readiness-staging-smoke-evidence.md", releaseNotes);
        Assert.Contains("docs/production-readiness-deployment-migration-rollback-evidence.md", releaseNotes);
        Assert.Contains("Only `published` obligations are customer-facing", releaseNotes);
    }

    [Fact]
    public void TC_PR_5_4_3_Known_risks_include_owner_mitigation_contingency_target_status_and_approver()
    {
        var riskLog = ReadText("docs", "production-readiness-launch-gap-decisions.md");

        foreach (var header in new[] { "Owner", "Mitigation", "Contingency", "Target date", "Current status", "Approver" })
        {
            Assert.Contains(header, riskLog);
        }

        foreach (var riskId in new[] { "PR43-MALWARE-001", "PR51-HIGH-RISK-001", "PR52-CLAIM-001", "PR53-SUPPORT-001" })
        {
            var row = riskLog
                .Split(Environment.NewLine)
                .Single(line => line.StartsWith($"| {riskId} |", StringComparison.Ordinal));
            var cells = row.Split('|', StringSplitOptions.TrimEntries);

            Assert.Equal(13, cells.Length);
            Assert.All(cells.Skip(1).Take(11), cell => Assert.False(string.IsNullOrWhiteSpace(cell)));
        }
    }

    [Fact]
    public void TC_PR_5_4_4_Support_onboarding_release_notes_and_known_risks_have_review_status_before_launch_approval()
    {
        var onboarding = ReadText("docs", "production-readiness-pilot-onboarding.md");
        var releaseNotes = ReadText("docs", "production-readiness-release-notes.md");
        var runbooks = ReadText("docs", "production-readiness-support-runbooks.md");
        var riskLog = ReadText("docs", "production-readiness-launch-gap-decisions.md");
        var closure = ReadText("docs", "production-readiness-launch-closure-evidence.md");

        Assert.Contains("Review status: launch-ready draft", onboarding);
        Assert.Contains("Release note status: launch-ready draft", releaseNotes);
        Assert.Contains("Review status: support runbooks finalized", runbooks);
        Assert.Contains("Known-Risk Acceptance Log", riskLog);
        Assert.Contains("Pilot onboarding, release notes, and known risks", closure);
        Assert.Contains("final solo-controlled pilot approval scopes", closure, StringComparison.OrdinalIgnoreCase);
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
    public void TC_PR_2_2_Completed_launch_stories_have_dod_evidence()
    {
        var review = ReadText("docs", "production-readiness-completed-story-dod-review.md");
        var plan = ReadText("docs", "production-readiness-plan.md");

        Assert.Contains("docs/production-readiness-completed-story-dod-review.md", plan);
        Assert.Contains("Review status: Complete.", review);

        foreach (var storyId in new[] { "PR-0.1", "PR-0.2", "PR-0.3", "PR-1.1", "PR-1.2", "PR-1.3", "PR-2.1" })
        {
            Assert.Contains($"| {storyId} |", review);
            Assert.Contains($"| {storyId} |", review);
        }

        Assert.Contains("Acceptance evidence", review);
        Assert.Contains("Test evidence", review);
        Assert.Contains("DoD disposition", review);
    }

    [Fact]
    public void TC_PR_2_2_Protected_workflows_have_tenant_rbac_and_audit_review_evidence()
    {
        var review = ReadText("docs", "production-readiness-completed-story-dod-review.md");

        foreach (var phrase in new[]
        {
            "Tenant isolation review",
            "RBAC review",
            "Audit logging evidence",
            "Tenant and RBAC",
            "Contract intake and upload",
            "Evidence vault and reports",
            "Tenant isolation, RBAC, and audit logging are release-blocking controls",
            "Uploads are server-side guarded by acknowledgement, classification, tenant mode, and audit events"
        })
        {
            Assert.Contains(phrase, review);
        }
    }

    [Fact]
    public void TC_PR_2_2_Missing_dod_items_are_listed_for_disposition()
    {
        var review = ReadText("docs", "production-readiness-completed-story-dod-review.md");

        Assert.Contains("## Completion Gaps For PR-2.3 Disposition", review);
        foreach (var gapId in new[] { "DOD-GAP-001", "DOD-GAP-002", "DOD-GAP-003" })
        {
            Assert.Contains($"| {gapId} |", review);
        }

        Assert.Contains("validation failure, permission denial, empty state, error state, and basic accessibility", review);
        Assert.Contains("Launch blocker until scanner is enabled or exception approved.", review);
        Assert.Contains("PR-2.3 must convert each listed gap", review);
    }

    [Fact]
    public void TC_PR_2_3_Completion_gaps_have_launch_decisions()
    {
        var decisions = ReadText("docs", "production-readiness-launch-gap-decisions.md");
        var plan = ReadText("docs", "production-readiness-plan.md");

        Assert.Contains("docs/production-readiness-launch-gap-decisions.md", plan);
        Assert.Contains("Review status: Complete.", decisions);

        foreach (var gapId in new[] { "DOD-GAP-001", "DOD-GAP-002", "DOD-GAP-003" })
        {
            Assert.Contains($"| {gapId} |", decisions);
        }

        Assert.Contains("Launch blocker", decisions);
        Assert.Contains("Deferred follow-up", decisions);
    }

    [Fact]
    public void TC_PR_2_3_Gap_decisions_include_required_risk_metadata()
    {
        var decisions = ReadText("docs", "production-readiness-launch-gap-decisions.md");

        foreach (var header in new[]
        {
            "Classification",
            "Owner",
            "Severity",
            "Mitigation",
            "Contingency",
            "Approver",
            "Target date",
            "Current status"
        })
        {
            Assert.Contains(header, decisions);
        }

        foreach (var gapId in new[] { "DOD-GAP-001", "DOD-GAP-002", "DOD-GAP-003" })
        {
            var row = decisions
                .Split(Environment.NewLine)
                .Single(line => line.StartsWith($"| {gapId} |", StringComparison.Ordinal));
            var cells = row.Split('|', StringSplitOptions.TrimEntries);

            Assert.Equal(13, cells.Length);
            Assert.All(cells.Skip(1).Take(11), cell => Assert.False(string.IsNullOrWhiteSpace(cell)));
        }
    }

    [Fact]
    public void TC_PR_2_3_Deferred_items_preserve_no_cui_and_claim_controls()
    {
        var decisions = ReadText("docs", "production-readiness-launch-gap-decisions.md");

        Assert.Contains("No deferred item in this log expands the No-CUI posture", decisions);
        Assert.Contains("Does not authorize real CUI or prohibited upload handling", decisions);
        Assert.Contains("If external scanner evidence is not attached before exception expiration", decisions);
        Assert.Contains("`PR43-MALWARE-001` is accepted for the No-CUI MVP launch candidate only.", decisions);
    }

    [Fact]
    public void TC_PR_3_1_Staging_deployment_evidence_references_approved_pipeline_and_result()
    {
        var evidence = ReadText("docs", "production-readiness-staging-smoke-evidence.md");
        var plan = ReadText("docs", "production-readiness-plan.md");

        Assert.Contains("docs/production-readiness-staging-smoke-evidence.md", plan);
        Assert.Contains("Story: PR-3.1 - Deploy And Smoke Test Staging.", evidence);
        Assert.Contains("Approved deployment path: `.github/workflows/staging.yml`.", evidence);
        Assert.Contains("Evidence status: Passed", evidence);
        Assert.Contains("Run conclusion | `success`", evidence);
        Assert.Contains("staging-smoke-test-results/staging-health.json", evidence);
        Assert.Contains("STAGE-GAP-001", evidence);
        Assert.Contains("Closed on 2026-07-01", evidence);
    }

    [Fact]
    public void TC_PR_3_1_Staging_smoke_requires_health_dependency_and_data_posture_signals()
    {
        var evidence = ReadText("docs", "production-readiness-staging-smoke-evidence.md");

        foreach (var signal in new[]
        {
            "service = gccs-api",
            "status = ok",
            "dataPosture = No-CUI / compliance management only",
            "dependency `postgresql`",
            "dependency `redis`",
            "dependency `object-storage`",
            "dependency `background-jobs`"
        })
        {
            Assert.Contains(signal, evidence);
        }
    }

    [Fact]
    public void TC_PR_3_1_Staging_data_guardrails_and_staging_credentials_are_documented()
    {
        var evidence = ReadText("docs", "production-readiness-staging-smoke-evidence.md");

        foreach (var guardrail in new[]
        {
            "No production customer data.",
            "No real customer CUI.",
            "No production secrets.",
            "No production uploads.",
            "No production unrestricted logs.",
            "Synthetic-only staging data.",
            "No-CUI / compliance management only posture."
        })
        {
            Assert.Contains(guardrail, evidence);
        }

        Assert.Contains("GitHub staging variables configured", evidence);
        Assert.Contains("GitHub staging Azure credentials configured", evidence);
        Assert.Contains("No secret value is recorded in this evidence file.", evidence);
    }

    [Fact]
    public void TC_PR_3_2_Staging_workflow_evidence_artifact_is_linked_and_passed_after_final_rerun()
    {
        var evidence = ReadText("docs", "production-readiness-staging-workflow-evidence.md");
        var plan = ReadText("docs", "production-readiness-plan.md");
        var checklist = ReadText("docs", "production-readiness-checklist.md");

        Assert.Contains("docs/production-readiness-staging-workflow-evidence.md", plan);
        Assert.Contains("docs/production-readiness-staging-workflow-evidence.md", checklist);
        Assert.Contains("Story: PR-3.2 - Execute End-To-End MVP Workflow In Staging.", evidence);
        Assert.Contains("Evidence status: Passed", evidence);
        Assert.Contains("Staging resource group: `gccs-staging-rg`.", evidence);
        Assert.Contains("Data handling posture: No-CUI / compliance management only.", evidence);
        Assert.Contains("Authenticated Staging Run - 2026-07-02", evidence);
        Assert.Contains("Staging Content Import And Final Rerun - 2026-07-02", evidence);
        Assert.Contains("output/playwright/production-readiness/pr-3.2/staging-content-import-summary.txt", evidence);
        Assert.Contains("output/playwright/production-readiness/pr-3.2/authenticated-api-transcript.json", evidence);
        Assert.Contains("output/playwright/production-readiness/pr-3.2/authenticated-corrective-api-transcript.json", evidence);
        Assert.Contains("output/playwright/production-readiness/pr-3.2/authenticated-final-rerun.json", evidence);
        Assert.Contains("output/playwright/production-readiness/pr-3.2/authenticated-upload-intent-audit.json", evidence);
        Assert.Contains("output/playwright/production-readiness/pr-3.2/evidence-package-corrected.json", evidence);
        Assert.Contains("STAGE-WF-001", evidence);
        Assert.Contains("| STAGE-WF-001 |", evidence);
        Assert.Contains("| QA owner | High |", evidence);
        Assert.Contains("Closed for PR-3.2", evidence);
        Assert.Contains("Ready for approval", checklist);
    }

    [Fact]
    public void TC_PR_3_2_Required_workflow_steps_are_captured_for_manual_staging_run()
    {
        var evidence = ReadText("docs", "production-readiness-staging-workflow-evidence.md");

        foreach (var workflowStep in new[]
        {
            "Tenant creation or verification",
            "User invite",
            "Role assignment",
            "Company profile",
            "Contract creation",
            "Allowed upload",
            "Blocked CUI/prohibited upload",
            "Blocked upload audit",
            "Manual clause tagging",
            "Obligation generation",
            "Task creation",
            "Evidence upload",
            "Report generation",
            "Audit log export"
        })
        {
            Assert.Contains(workflowStep, evidence);
        }

        Assert.Contains("This table records the synthetic-only staging execution used to close PR-3.2.", evidence);
        Assert.DoesNotContain("production customer data is allowed", evidence, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("real customer CUI is allowed", evidence, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TC_PR_3_2_Automated_coverage_and_smoke_results_are_mapped_to_test_cases()
    {
        var evidence = ReadText("docs", "production-readiness-staging-workflow-evidence.md");
        var pilotWorkflowTests = ReadText("tests", "Gccs.Api.Tests", "PilotWorkflowTests.cs");
        var noCuiTests = ReadText("tests", "Gccs.Api.Tests", "NoCuiAcknowledgementTests.cs");

        foreach (var testCase in new[] { "TC-PR-3.2.1", "TC-PR-3.2.2", "TC-PR-3.2.3", "TC-PR-3.2.4" })
        {
            Assert.Contains(testCase, evidence);
        }

        Assert.Contains("TC_17_1_1_Non_cui_pilot_tenant_completes_core_mvp_workflow", pilotWorkflowTests);
        Assert.Contains("TC_17_1_3_Pilot_reports_reflect_workflow_data", pilotWorkflowTests);
        Assert.Contains("TC_4_2_2A_Upload_without_per_file_no_cui_attestation_is_rejected_and_audit_logged", noCuiTests);
        Assert.Contains("TC_4_2_4_Failed_upload_validation_is_audit_logged_and_not_usable", noCuiTests);
        Assert.Contains("Authenticated staging evidence proves the PR-3.2 workflow, but production launch still depends on other readiness checklist items.", evidence);
        Assert.Contains("Empty staging compliance content previously blocked clause tagging and obligation generation; rerun the import after any staging database rebuild.", evidence);
        Assert.Contains("TC-PR-3.2.1 | Passed", evidence);
        Assert.Contains("TC-PR-3.2.3 | Passed", evidence);
    }

    [Fact]
    public void TC_PR_3_2_Staging_compliance_content_import_runbook_is_staging_safe()
    {
        var staging = ReadText("docs", "staging-environment.md");
        var solution = ReadText("Gccs.slnx");
        var importer = ReadText("tools", "Gccs.ContentImport", "Program.cs");

        Assert.Contains("## Compliance Content Import", staging);
        Assert.Contains("Do not enable the Development tenant bootstrapper in Azure", staging);
        Assert.Contains("tools/Gccs.ContentImport/Gccs.ContentImport.csproj", staging);
        Assert.Contains("--confirm-staging true", staging);
        Assert.Contains("packages/compliance-content", staging);
        Assert.Contains("far-52-204-21", staging);
        Assert.Contains("tools/Gccs.ContentImport/Gccs.ContentImport.csproj", solution);
        Assert.Contains("Refusing to import without --confirm-staging true", importer);
        Assert.DoesNotContain("ASPNETCORE_ENVIRONMENT=Development", staging, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TC_PR_3_3_Staging_security_evidence_records_automated_and_full_role_matrix_coverage()
    {
        var evidence = ReadText("docs", "production-readiness-staging-security-evidence.md");
        var checklist = ReadText("docs", "production-readiness-checklist.md");
        var closure = ReadText("docs", "production-readiness-launch-closure-evidence.md");
        var gapLog = ReadText("docs", "production-readiness-launch-gap-decisions.md");
        using var ownerProbes = JsonDocument.Parse(ReadText("output", "playwright", "production-readiness", "pr-3.3", "owner-session-probes.json"));
        using var adminCycle = JsonDocument.Parse(ReadText("output", "playwright", "production-readiness", "pr-3.3", "admin-cycle-and-cleanup.json"));
        using var orphanCleanup = JsonDocument.Parse(ReadText("output", "playwright", "production-readiness", "pr-3.3", "orphan-tenant-cleanup.json"));
        using var firewallCleanup = JsonDocument.Parse(ReadText("output", "playwright", "production-readiness", "pr-3.3", "orphan-tenant-firewall-cleanup.json"));
        using var ownerMatrix = JsonDocument.Parse(ReadText("output", "playwright", "production-readiness", "pr-3.3", "role-matrix-owner.json"));
        using var adminMatrix = JsonDocument.Parse(ReadText("output", "playwright", "production-readiness", "pr-3.3", "role-matrix-admin.json"));
        using var complianceManagerMatrix = JsonDocument.Parse(ReadText("output", "playwright", "production-readiness", "pr-3.3", "role-matrix-compliance-manager.json"));
        using var contributorMatrix = JsonDocument.Parse(ReadText("output", "playwright", "production-readiness", "pr-3.3", "role-matrix-contributor.json"));
        using var auditorMatrix = JsonDocument.Parse(ReadText("output", "playwright", "production-readiness", "pr-3.3", "role-matrix-auditor.json"));
        using var advisorMatrix = JsonDocument.Parse(ReadText("output", "playwright", "production-readiness", "pr-3.3", "role-matrix-advisor.json"));
        using var noMutation = JsonDocument.Parse(ReadText("output", "playwright", "production-readiness", "pr-3.3", "role-matrix-no-mutation-summary.json"));
        using var fixtureCleanup = JsonDocument.Parse(ReadText("output", "playwright", "production-readiness", "pr-3.3", "role-matrix-fixture-cleanup.json"));
        using var roleMatrixFirewallCleanup = JsonDocument.Parse(ReadText("output", "playwright", "production-readiness", "pr-3.3", "role-matrix-firewall-cleanup.json"));
        using var localSecretCleanup = JsonDocument.Parse(ReadText("output", "playwright", "production-readiness", "pr-3.3", "role-matrix-local-secret-cleanup.json"));

        Assert.Contains("Story: PR-3.3 - Verify Tenant Isolation And RBAC In Staging.", evidence);
        Assert.Contains("Evidence status: Passed.", evidence);
        Assert.Contains("dotnet test tests/Gccs.Api.Tests/Gccs.Api.Tests.csproj --configuration Release --no-restore --filter", evidence);
        Assert.Contains("Ten tests passed with zero failures.", evidence);
        Assert.Contains("output/playwright/production-readiness/pr-3.3/role-matrix-owner.json", evidence);
        Assert.Contains("output/playwright/production-readiness/pr-3.3/role-matrix-admin.json", evidence);
        Assert.Contains("output/playwright/production-readiness/pr-3.3/role-matrix-compliance-manager.json", evidence);
        Assert.Contains("output/playwright/production-readiness/pr-3.3/role-matrix-contributor.json", evidence);
        Assert.Contains("output/playwright/production-readiness/pr-3.3/role-matrix-auditor.json", evidence);
        Assert.Contains("output/playwright/production-readiness/pr-3.3/role-matrix-advisor.json", evidence);
        Assert.Contains("output/playwright/production-readiness/pr-3.3/role-matrix-no-mutation-summary.json", evidence);
        Assert.Contains("output/playwright/production-readiness/pr-3.3/orphan-tenant-cleanup.json", evidence);
        Assert.Contains("All six role artifacts have `passed: true`.", evidence);
        Assert.Contains("PR33-STAGE-001", evidence);
        Assert.Contains("PR33-STAGE-002", evidence);
        Assert.Contains("PR-3.3 is complete. The production readiness sequence can proceed to PR-3.4 next", evidence);

        Assert.Contains("Staging tenant isolation and RBAC", checklist);
        Assert.Contains("Ready for approval", checklist);
        Assert.Contains("docs/production-readiness-staging-security-evidence.md", closure);
        Assert.Contains("role-matrix-owner.json", closure);
        Assert.Contains("DOD-GAP-007", gapLog);
        Assert.Contains("DOD-GAP-008", gapLog);
        Assert.Contains("Closed Gaps", gapLog);
        Assert.DoesNotContain("DOD-GAP-007`: PR-3.3 authenticated staging tenant isolation and RBAC evidence remains incomplete", gapLog);
        Assert.Equal("Owner", ownerProbes.RootElement.GetProperty("roleContext").GetString());
        Assert.False(ownerProbes.RootElement.GetProperty("tokenCapturedInArtifact").GetBoolean());
        Assert.Contains(ownerProbes.RootElement.GetProperty("calls").EnumerateArray(), call =>
            call.GetProperty("name").GetString() == "current access" &&
            call.GetProperty("status").GetInt32() == 200);
        Assert.Contains(ownerProbes.RootElement.GetProperty("limitations").EnumerateArray(), limitation =>
            limitation.GetString() == "Admin, Compliance Manager, Contributor, Auditor, and Advisor direct API role contexts remain untested in live staging.");
        Assert.Equal("Not valid PR-3.3 role evidence.", adminCycle.RootElement.GetProperty("result").GetString());
        Assert.False(adminCycle.RootElement.GetProperty("tokenCapturedInArtifact").GetBoolean());
        Assert.Contains(adminCycle.RootElement.GetProperty("remainingBlockers").EnumerateArray(), blocker =>
            blocker.GetString() == "Provide database or Azure/admin access to locate and archive the accidental orphan tenant named PR-3.3 blocked admin tenant.");
        Assert.True(orphanCleanup.RootElement.GetProperty("archived").GetBoolean());
        Assert.True(orphanCleanup.RootElement.GetProperty("statusUpdated").GetBoolean());
        Assert.True(orphanCleanup.RootElement.GetProperty("auditInserted").GetBoolean());
        Assert.Equal("Archived", orphanCleanup.RootElement.GetProperty("tenant").GetProperty("Status").GetString());
        Assert.Equal(0, orphanCleanup.RootElement.GetProperty("tenant").GetProperty("MembershipCount").GetInt64());
        Assert.Contains(orphanCleanup.RootElement.GetProperty("auditRows").EnumerateArray(), audit =>
            audit.GetProperty("CorrelationId").GetString() == "pr-3.3-orphan-tenant-cleanup" &&
            audit.GetProperty("Action").GetString() == "Archived");
        Assert.Empty(firewallCleanup.RootElement.EnumerateArray());
        foreach (var matrix in new[] { ownerMatrix, adminMatrix, complianceManagerMatrix, contributorMatrix, auditorMatrix, advisorMatrix })
        {
            Assert.True(matrix.RootElement.GetProperty("passed").GetBoolean());
            Assert.False(matrix.RootElement.GetProperty("tokenCapturedInArtifact").GetBoolean());
            Assert.Contains(matrix.RootElement.GetProperty("calls").EnumerateArray(), call =>
                call.GetProperty("name").GetString() == "cross-tenant evidence update" &&
                (call.GetProperty("status").GetInt32() == 404 || call.GetProperty("status").GetInt32() == 403));
        }

        Assert.True(noMutation.RootElement.GetProperty("noMutationObserved").GetBoolean());
        Assert.Equal("Archived", fixtureCleanup.RootElement.GetProperty("fixtureStatus").GetProperty("TenantStatus").GetString());
        Assert.Empty(roleMatrixFirewallCleanup.RootElement.EnumerateArray());
        Assert.True(localSecretCleanup.RootElement.GetProperty("localSecretRemoved").GetBoolean());
        Assert.True(localSecretCleanup.RootElement.GetProperty("temporaryHelperRemoved").GetBoolean());
    }

    [Fact]
    public void TC_PR_3_4_Upload_and_report_staging_evidence_records_authenticated_pass()
    {
        var evidence = ReadText("docs", "production-readiness-staging-upload-report-evidence.md");
        var checklist = ReadText("docs", "production-readiness-checklist.md");
        var closure = ReadText("docs", "production-readiness-launch-closure-evidence.md");
        var gapLog = ReadText("docs", "production-readiness-launch-gap-decisions.md");
        using var health = JsonDocument.Parse(ReadText("output", "playwright", "production-readiness", "pr-3.4", "staging-health.json"));
        using var authenticatedSmoke = JsonDocument.Parse(ReadText("output", "playwright", "production-readiness", "pr-3.4", "authenticated-upload-report-smoke.json"));
        using var authBlocker = JsonDocument.Parse(ReadText("output", "playwright", "production-readiness", "pr-3.4", "authentication-blocker.json"));

        Assert.Contains("Story: PR-3.4 - Verify Upload Guardrails And Report Controls In Staging.", evidence);
        Assert.Contains("Evidence status: Complete.", evidence);
        Assert.Contains("No-CUI acknowledgement", evidence);
        Assert.Contains("Accepted upload and blocked upload attempts were visible in tenant audit logs.", evidence);
        Assert.Contains("Contract obligation matrix report and export included source metadata", evidence);
        Assert.Contains("no affirmative prohibited claims", evidence);
        Assert.Contains("PR34-STAGE-001", evidence);
        Assert.Contains("Closed on 2026-07-02", evidence);
        Assert.Contains("PR-3.4 is complete. The production readiness sequence can proceed to PR-4.1 next", evidence);

        Assert.Contains("Staging upload guardrails and report controls", checklist);
        Assert.Contains("Ready for approval", checklist);
        Assert.Contains("docs/production-readiness-staging-upload-report-evidence.md", closure);
        Assert.Contains("authenticated-upload-report-smoke.json", closure);
        Assert.Contains("DOD-GAP-009", gapLog);
        Assert.Contains("PR-3.4 closed `DOD-GAP-009`; the production readiness sequence can continue to PR-4.1.", gapLog);

        Assert.Equal("PR-3.4", health.RootElement.GetProperty("story").GetString());
        Assert.Equal(200, health.RootElement.GetProperty("status").GetInt32());
        Assert.False(health.RootElement.GetProperty("tokenCapturedInArtifact").GetBoolean());
        Assert.False(health.RootElement.GetProperty("containsCustomerData").GetBoolean());
        Assert.False(health.RootElement.GetProperty("containsCui").GetBoolean());

        Assert.Equal("passed", authenticatedSmoke.RootElement.GetProperty("result").GetString());
        Assert.False(authenticatedSmoke.RootElement.GetProperty("tokenCapturedInArtifact").GetBoolean());
        Assert.False(authenticatedSmoke.RootElement.GetProperty("containsCustomerData").GetBoolean());
        Assert.False(authenticatedSmoke.RootElement.GetProperty("containsCui").GetBoolean());
        Assert.True(authenticatedSmoke.RootElement.GetProperty("noCuiAcknowledgement").GetProperty("isAcknowledged").GetBoolean());
        Assert.Equal(201, authenticatedSmoke.RootElement.GetProperty("uploadGuardrails").GetProperty("allowedUpload").GetProperty("status").GetInt32());
        Assert.Equal(403, authenticatedSmoke.RootElement.GetProperty("uploadGuardrails").GetProperty("realCuiBlocked").GetProperty("status").GetInt32());
        Assert.True(authenticatedSmoke.RootElement.GetProperty("audit").GetProperty("uploadedAuditCount").GetInt32() >= 1);
        Assert.True(authenticatedSmoke.RootElement.GetProperty("audit").GetProperty("rejectedAuditCount").GetInt32() >= 1);
        Assert.True(authenticatedSmoke.RootElement.GetProperty("reports").GetProperty("complianceStatus").GetProperty("tenantMatches").GetBoolean());
        Assert.True(authenticatedSmoke.RootElement.GetProperty("reports").GetProperty("obligationMatrix").GetProperty("csvHeadersIncludeSourceMetadata").GetBoolean());
        Assert.True(authenticatedSmoke.RootElement.GetProperty("reports").GetProperty("cmmcReadiness").GetProperty("hasDraftReadinessLanguage").GetBoolean());
        Assert.All(
            authenticatedSmoke.RootElement.GetProperty("checks").EnumerateArray(),
            check => Assert.True(check.GetProperty("passed").GetBoolean(), check.GetProperty("name").GetString()));

        Assert.Equal("blocked", authBlocker.RootElement.GetProperty("result").GetString());
        Assert.Equal("PR34-STAGE-001", authBlocker.RootElement.GetProperty("blockerId").GetString());
        Assert.False(authBlocker.RootElement.GetProperty("tokenCapturedInArtifact").GetBoolean());
        Assert.False(authBlocker.RootElement.GetProperty("containsCustomerData").GetBoolean());
        Assert.False(authBlocker.RootElement.GetProperty("containsCui").GetBoolean());
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

        foreach (var requiredItem in new[] { "No-CUI posture", "Terms and claims", "Support path", "Prohibited uploads", "Staging MVP workflow", "Backups and restore", "Logs and alerts", "Rollback plan", "Malware scanning", "Expert-reviewed content", "Release notes" })
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

    [Fact]
    public void TC_PR_4_1_Backup_configuration_and_restore_rehearsal_are_evidenced()
    {
        var evidence = ReadText("docs", "production-readiness-backup-restore-evidence.md");
        var closure = ReadText("docs", "production-readiness-launch-closure-evidence.md");
        var checklist = ReadText("docs", "production-readiness-checklist.md");
        using var backupConfig = JsonDocument.Parse(ReadText("output", "production-readiness", "backup-restore", "staging-postgres-backup-config.json"));
        using var restoreSummary = JsonDocument.Parse(ReadText("output", "production-readiness", "backup-restore", "restore-rehearsal-summary.json"));

        Assert.Contains("Story: PR-4.1 - Attach Backup And Restore Evidence.", evidence);
        Assert.Contains("Backup evidence captured and restore rehearsal passed on 2026-07-05", evidence);
        Assert.Contains("output/production-readiness/backup-restore/staging-postgres-backup-config.json", evidence);
        Assert.Contains("Restore rehearsal passed on 2026-07-05", checklist);
        Assert.Contains("docs/production-readiness-backup-restore-evidence.md", checklist);
        Assert.Contains("docs/production-readiness-backup-restore-evidence.md", closure);
        Assert.Contains("Restore rehearsal passed on 2026-07-05", closure);
        Assert.Contains("az postgres flexible-server restore", closure);
        Assert.Contains("az postgres flexible-server delete", closure);
        Assert.Equal("gccs-pg-staging-19984", backupConfig.RootElement.GetProperty("name").GetString());
        Assert.Equal("Ready", backupConfig.RootElement.GetProperty("state").GetString());
        Assert.Equal(7, backupConfig.RootElement.GetProperty("backup").GetProperty("backupRetentionDays").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(backupConfig.RootElement.GetProperty("backup").GetProperty("earliestRestoreDate").GetString()));
        Assert.Equal("passed", restoreSummary.RootElement.GetProperty("result").GetString());
        Assert.True(restoreSummary.RootElement.GetProperty("teardownConfirmed").GetBoolean());
        Assert.False(restoreSummary.RootElement.GetProperty("containsCustomerData").GetBoolean());
        Assert.False(restoreSummary.RootElement.GetProperty("containsCui").GetBoolean());
        Assert.False(restoreSummary.RootElement.GetProperty("secretsCaptured").GetBoolean());
    }

    [Fact]
    public void TC_PR_4_1_Restore_evidence_requires_execution_metadata_not_backup_assertions()
    {
        var evidence = ReadText("docs", "production-readiness-backup-restore-evidence.md");

        foreach (var requiredField in new[]
        {
            "Restore date",
            "Environment",
            "Data set",
            "Command or pipeline reference",
            "Result",
            "Reviewer",
            "Evidence location"
        })
        {
            Assert.Contains(requiredField, evidence);
            Assert.Contains($"| {requiredField} |", evidence);
        }

        Assert.Contains("Backup configuration is not recovery evidence.", evidence);
        Assert.Contains("Backup creation alone is rejected as restore proof.", evidence);
        Assert.Contains("Current status: Executed and passed on 2026-07-05", evidence);
        Assert.Contains("Point-in-time restore server evidence", evidence);
        Assert.Contains("TC-PR-4.1.2 | Passed", evidence);
        Assert.Contains("TC-PR-4.1.3 | Passed", evidence);
    }

    [Fact]
    public void TC_PR_4_1_Restore_rehearsal_closes_production_launch_blocker()
    {
        var evidence = ReadText("docs", "production-readiness-backup-restore-evidence.md");
        var checklist = ReadText("docs", "production-readiness-checklist.md");
        var closure = ReadText("docs", "production-readiness-launch-closure-evidence.md");

        Assert.Contains("PR41-RESTORE-001", evidence);
        Assert.Contains("closed for the staging launch-candidate restore rehearsal", evidence, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Restore rehearsal passed on 2026-07-05", checklist);
        Assert.Contains("no longer blocked by `PR41-RESTORE-001`", closure);
        Assert.Contains("TC-PR-4.1.4 | Passed", evidence);
        Assert.Contains("restore rehearsal passed", evidence, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TC_PR_4_2_Deployment_migration_and_rollback_evidence_is_attached()
    {
        var evidence = ReadText("docs", "production-readiness-deployment-migration-rollback-evidence.md");
        var checklist = ReadText("docs", "production-readiness-checklist.md");
        var closure = ReadText("docs", "production-readiness-launch-closure-evidence.md");

        Assert.Contains("Story: PR-4.2 - Attach Deployment, Migration, And Rollback Evidence.", evidence);
        Assert.Contains("docs/production-readiness-staging-smoke-evidence.md", evidence);
        Assert.Contains("docs/production-readiness-staging-workflow-evidence.md", evidence);
        Assert.Contains("GitHub Actions run `28534289128`", evidence);
        Assert.Contains("staging-smoke-test-results/staging-health.json", evidence);
        Assert.Contains("docs/production-readiness-deployment-migration-rollback-evidence.md", checklist);
        Assert.Contains("docs/production-readiness-deployment-migration-rollback-evidence.md", closure);
        Assert.Contains("output/production-readiness/deployment-migration-rollback/gccs-staging-migrations.sql", checklist);
        Assert.Contains("Application rollback simulation is documented", evidence);
        Assert.Contains("TC-PR-4.2.1 | Passed", evidence);
        Assert.Contains("TC-PR-4.2.3 | Passed with limitation", evidence);
    }

    [Fact]
    public void TC_PR_4_2_Migration_evidence_identifies_script_environment_result_reviewer_and_failure_handling()
    {
        var evidence = ReadText("docs", "production-readiness-deployment-migration-rollback-evidence.md");
        var migrationScript = ReadText("output", "production-readiness", "deployment-migration-rollback", "gccs-staging-migrations.sql");

        foreach (var requiredField in new[]
        {
            "Script source",
            "Script generation command",
            "Generated script path",
            "Environment",
            "Result",
            "Reviewer",
            "Failure handling"
        })
        {
            Assert.Contains(requiredField, evidence);
            Assert.Contains($"| {requiredField} |", evidence);
        }

        Assert.Contains("5931c70b457735687b5d5e7e21ceb4e843ce2fb6cb9ef083577d7c77f69a9b62", evidence);
        Assert.Contains("dotnet tool run dotnet-ef migrations script --idempotent", evidence);
        Assert.Contains("CREATE TABLE IF NOT EXISTS", migrationScript);
        Assert.Contains("20260626194212_AddReusableComplianceChecklists", migrationScript);
        Assert.DoesNotContain("DROP TABLE", migrationScript, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DROP COLUMN", migrationScript, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("TRUNCATE", migrationScript, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DELETE FROM", migrationScript, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TC_PR_4_2_Irreversible_migration_risk_has_owner_mitigation_contingency_and_approver()
    {
        var evidence = ReadText("docs", "production-readiness-deployment-migration-rollback-evidence.md");

        foreach (var requiredField in new[]
        {
            "Risk ID",
            "Owner",
            "Mitigation",
            "Contingency",
            "Approver",
            "Current status"
        })
        {
            Assert.Contains(requiredField, evidence);
        }

        Assert.Contains("PR42-MIGRATION-001", evidence);
        Assert.Contains("Engineering lead", evidence);
        Assert.Contains("Product owner and engineering lead", evidence);
        Assert.Contains("Database rollback is not automatic", evidence);
        Assert.Contains("Do not run EF `Down()` paths automatically in production", evidence);
        Assert.Contains("TC-PR-4.2.4 | Passed", evidence);
    }

    [Fact]
    public void TC_PR_4_3_Malware_scanning_launch_path_requires_scanner_evidence_or_signed_exception()
    {
        var decision = ReadText("docs", "production-readiness-malware-scanning-decision.md");
        var closure = ReadText("docs", "production-readiness-launch-closure-evidence.md");
        var gapLog = ReadText("docs", "production-readiness-launch-gap-decisions.md");

        Assert.Contains("Story: PR-4.3 - Decide Malware Scanning Launch Path.", decision);
        Assert.Contains("Scanner control path enabled; launch exception approved", decision);
        Assert.Contains("ClamAV-compatible fail-closed scanner path", decision);
        Assert.Contains("TC-PR-4.3.1 | Passed with limitation", decision);
        Assert.Contains("Production malware scanning control path is enabled.", closure);
        Assert.Contains("scanner-unavailable uploads fail closed", closure);
        Assert.Contains("| Enable scanner | Scanner configuration", closure);
        Assert.Contains("| Launch exception | Exception scope", closure);
        Assert.Contains("Security owner and product owner", closure);
        Assert.Contains("docs/production-readiness-malware-scanning-decision.md", closure);
        Assert.Contains("Closed on 2026-07-02 by approved exception and enabled fail-closed scanner path", gapLog);
        Assert.Contains("disable production file upload paths", gapLog);
    }

    [Fact]
    public void TC_PR_4_3_Draft_exception_records_controls_workflows_owner_expiration_and_approvers()
    {
        var decision = ReadText("docs", "production-readiness-malware-scanning-decision.md");

        foreach (var requiredField in new[]
        {
            "Exception ID",
            "Scope",
            "Affected workflows",
            "Owner",
            "Required approvers",
            "Expiration date",
            "Compensating controls",
            "Rollback or disable plan",
            "Support path",
            "Known-risk log",
            "Current status"
        })
        {
            Assert.Contains(requiredField, decision);
            Assert.Contains($"| {requiredField} |", decision);
        }

        Assert.Contains("PR43-MALWARE-001", decision);
        Assert.Contains("Evidence file upload", decision);
        Assert.Contains("contract document upload", decision);
        Assert.Contains("Product owner and security owner", decision);
        Assert.Contains("Before production customer launch, or 30 days after exception approval", decision);
        Assert.Contains("No-CUI posture", decision);
        Assert.Contains("scanner-before-storage enforcement", ReadText("docs", "production-readiness-launch-gap-decisions.md"));
        Assert.Contains("TC-PR-4.3.2 | Passed", decision);
    }

    [Fact]
    public void TC_PR_4_3_Known_risk_log_records_approved_exception_and_resolves_blocker()
    {
        var decision = ReadText("docs", "production-readiness-malware-scanning-decision.md");
        var checklist = ReadText("docs", "production-readiness-checklist.md");
        var gapLog = ReadText("docs", "production-readiness-launch-gap-decisions.md");

        Assert.Contains("PR43-MALWARE-001", gapLog);
        Assert.Contains("Known-Risk Acceptance Log", gapLog);
        Assert.Contains("Approved on 2026-07-02", gapLog);
        Assert.Contains("PR-4.3 launch blocker closed", gapLog);
        Assert.Contains("Exception approved on 2026-07-02", checklist);
        Assert.Contains("TC-PR-4.3.3 | Passed", decision);
        Assert.Contains("TC-PR-4.3.4 | Passed", decision);
        Assert.Contains("Current state has an approved exception.", decision);
        Assert.DoesNotContain("owner approval pending", decision, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TC_PR_5_1_Expert_content_approval_artifact_identifies_pending_high_risk_records()
    {
        var closure = ReadText("docs", "production-readiness-launch-closure-evidence.md");
        using var summary = JsonDocument.Parse(ReadText("output", "production-readiness", "expert-content", "staging-content-review-summary.json"));

        Assert.Contains("High-risk review decisions recorded", ReadText("docs", "production-readiness-checklist.md"));
        Assert.Contains("Only `published` obligations are customer-facing", closure);
        Assert.Equal(10, summary.RootElement.GetProperty("total").GetInt32());
        Assert.Equal(9, summary.RootElement.GetProperty("highRiskOrExpertReviewRequiredCount").GetInt32());
        Assert.Equal(5, summary.RootElement.GetProperty("pendingExpertReviewCount").GetInt32());
        Assert.Equal(
            "output/production-readiness/expert-content/high-risk-obligation-review.json",
            summary.RootElement.GetProperty("highRiskReviewEvidence").GetString());
    }

    [Fact]
    public void TC_PR_6_1_Final_launch_approvals_are_recorded_before_launch_candidate_tagging()
    {
        var closure = ReadText("docs", "production-readiness-launch-closure-evidence.md");
        var checklist = ReadText("docs", "production-readiness-checklist.md");
        var approvalRecord = ReadText("docs", "production-readiness-launch-approval-record.md");

        Assert.Contains("Launch candidate tagging is allowed for solo-controlled pilot testing and project completion", closure);
        AssertRequiredApprovedApproverTableRows(closure);
        AssertRequiredApprovedApproverTableRows(checklist);
        AssertRequiredApprovedApproverApprovalRows(approvalRecord);
        Assert.Contains("PR-6.1 is complete for solo-controlled pilot launch-candidate tagging", closure);
        Assert.Contains("PR-6.2 launch candidate tagging decision: approved to proceed for solo-controlled pilot testing and project completion only.", approvalRecord);
        Assert.Contains("Missing, pending, or incomplete approval metadata blocks PR-6.2 launch candidate tagging.", approvalRecord);
        Assert.Contains("docs/production-readiness-approval-posture-addendum.md", approvalRecord);
        Assert.Contains("docs/production-readiness-launch-approval-record.md", checklist);
        Assert.Contains("docs/production-readiness-launch-approval-record.md", closure);
        Assert.Contains("TC-PR-6.1.1 | Passed", approvalRecord);
        Assert.Contains("TC-PR-6.1.2 | Passed", approvalRecord);
        Assert.Contains("TC-PR-6.1.3 | Passed", approvalRecord);
        Assert.Contains("TC-PR-6.1.4 | Passed", approvalRecord);
    }

    [Fact]
    public void TC_PR_6_1_Approval_record_links_required_evidence_and_exception_log()
    {
        var approvalRecord = ReadText("docs", "production-readiness-launch-approval-record.md");
        var riskLog = ReadText("docs", "production-readiness-launch-gap-decisions.md");

        foreach (var path in new[]
        {
            "docs/production-readiness-plan.md",
            "docs/production-readiness-checklist.md",
            "docs/production-readiness-launch-closure-evidence.md",
            "docs/production-readiness-approval-posture-addendum.md",
            "docs/production-readiness-staging-workflow-evidence.md",
            "docs/production-readiness-staging-security-evidence.md",
            "docs/production-readiness-staging-upload-report-evidence.md",
            "docs/production-readiness-backup-restore-evidence.md",
            "docs/production-readiness-deployment-migration-rollback-evidence.md",
            "docs/production-readiness-malware-scanning-decision.md",
            "docs/production-readiness-customer-claims-review.md",
            "docs/production-readiness-support-runbooks.md",
            "docs/production-readiness-pilot-onboarding.md",
            "docs/production-readiness-release-notes.md",
            "docs/production-readiness-launch-gap-decisions.md",
            "output/production-readiness/customer-claims-review.json",
            "output/production-readiness/expert-content/high-risk-obligation-review.json"
        })
        {
            Assert.Contains(path, approvalRecord);
        }

        Assert.Contains("DOD-GAP-006", approvalRecord);
        Assert.Contains("PR43-MALWARE-001", approvalRecord);
        Assert.Contains("PR41-RESTORE-001", approvalRecord);
        Assert.Contains("PR52-CLAIM-001", riskLog);
        Assert.Contains("PR53-SUPPORT-001", riskLog);
        Assert.Contains("docs/production-readiness-launch-approval-record.md", riskLog);
        Assert.Contains("Production customer launch is no longer blocked by PR41 restore evidence", riskLog);
    }

    [Fact]
    public void TC_PR_6_2_Launch_candidate_tag_preconditions_are_complete()
    {
        var tagRecord = ReadText("docs", "production-readiness-launch-candidate-tag.md");
        var checklist = ReadText("docs", "production-readiness-checklist.md");
        var closure = ReadText("docs", "production-readiness-launch-closure-evidence.md");
        var riskLog = ReadText("docs", "production-readiness-launch-gap-decisions.md");

        Assert.Contains("Tag status: created.", tagRecord);
        Assert.Contains("Launch candidate tag: `gccs-no-cui-mvp-lc-2026-07-03`.", tagRecord);
        Assert.Contains("Tagged commit: `6c8927ec9cf79de977d76cb2594b87dd48f973bd`.", tagRecord);
        Assert.Contains("GitHub Actions staging workflow run `28635229630` completed successfully", tagRecord);
        Assert.Contains("Required launch approvals complete | Passed for solo-controlled pilot testing", tagRecord);
        Assert.Contains("Evidence package gathered | Passed", tagRecord);
        Assert.Contains("Approved build and deployment path passed | Passed", tagRecord);
        Assert.Contains("Created as `gccs-no-cui-mvp-lc-2026-07-03`", checklist);
        Assert.Contains("docs/production-readiness-launch-candidate-tag.md", closure);
        Assert.Contains("DOD-GAP-002", riskLog);
        Assert.Contains("Closed on 2026-07-03 by `docs/production-readiness-launch-candidate-tag.md`", riskLog);
    }

    [Fact]
    public void TC_PR_6_2_Tag_record_links_release_artifacts_and_missing_evidence_block_rule()
    {
        var tagRecord = ReadText("docs", "production-readiness-launch-candidate-tag.md");

        foreach (var requiredLink in new[]
        {
            "docs/production-readiness-release-notes.md",
            "docs/production-readiness-launch-gap-decisions.md",
            "docs/production-readiness-support-runbooks.md",
            "docs/production-readiness-staging-smoke-evidence.md",
            "docs/production-readiness-staging-workflow-evidence.md",
            "docs/production-readiness-staging-security-evidence.md",
            "docs/production-readiness-staging-upload-report-evidence.md",
            "docs/production-readiness-deployment-migration-rollback-evidence.md",
            "packages/compliance-content/obligations/mvp.json"
        })
        {
            Assert.Contains(requiredLink, tagRecord);
        }

        Assert.Contains("Build artifact source", tagRecord);
        Assert.Contains("API deployment artifact", tagRecord);
        Assert.Contains("Web deployment artifact", tagRecord);
        Assert.Contains("Migration artifact", tagRecord);
        Assert.Contains("Smoke artifact", tagRecord);
        Assert.Contains("Missing-evidence tag block rule retained | Passed", tagRecord);
        Assert.Contains("no tag may be created or retained if required links are removed", tagRecord);
        Assert.Contains("TC-PR-6.2.4 | Passed", tagRecord);
    }

    [Fact]
    public void TC_PR_7_1_Production_deployment_uses_approved_ci_cd_and_launch_candidate()
    {
        var deployment = ReadText("docs", "production-readiness-production-deployment-evidence.md");
        var checklist = ReadText("docs", "production-readiness-checklist.md");
        var closure = ReadText("docs", "production-readiness-launch-closure-evidence.md");
        var riskLog = ReadText("docs", "production-readiness-launch-gap-decisions.md");
        var workflow = ReadText(".github", "workflows", "production.yml");

        Assert.Contains("Deployment status: passed through approved CI/CD path.", deployment);
        Assert.Contains("Approved launch candidate tag: `gccs-no-cui-mvp-lc-2026-07-03`.", deployment);
        Assert.Contains("Approved production workflow: `.github/workflows/production.yml`.", deployment);
        Assert.Contains("Production environment contract: `infra/terraform/environments/production/main.tf`.", deployment);
        Assert.Contains("Approved production CI/CD path | Passed", deployment);
        Assert.Contains("Production environment configuration | Passed", deployment);
        Assert.Contains("Production secrets source | Passed as contract", deployment);
        Assert.Contains("PR71-PROD-DEPLOY-001", deployment);
        Assert.Contains("Passed in workflow run `28746053336`; deployment evidence artifact records migration, API deploy, web deploy, and `/health` pass", checklist);
        Assert.Contains("docs/production-readiness-production-deployment-evidence.md", closure);
        Assert.Contains(".github/workflows/production.yml", closure);
        Assert.Contains("Closed on 2026-07-03 by `.github/workflows/production.yml`", riskLog);
        Assert.Contains("PR71-PROD-DEPLOY-001", riskLog);
        Assert.Contains("workflow_dispatch:", workflow);
        Assert.Contains("launch_candidate_tag:", workflow);
        Assert.Contains("APPROVED_LAUNCH_CANDIDATE_TAG: gccs-no-cui-mvp-lc-2026-07-03", workflow);
        Assert.Contains("ref: ${{ github.event.inputs.launch_candidate_tag }}", workflow);
        Assert.Contains("name: production", workflow);
    }

    [Fact]
    public void TC_PR_7_1_Deployment_record_preserves_no_cui_and_verifies_production_controls()
    {
        var deployment = ReadText("docs", "production-readiness-production-deployment-evidence.md");
        var workflow = ReadText(".github", "workflows", "production.yml");
        var productionContract = ReadText("infra", "terraform", "environments", "production", "main.tf");

        Assert.Contains("No-CUI / compliance management only", deployment);
        Assert.Contains("do not deploy production manually", deployment, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Do not deploy production manually or through the staging workflow.", deployment);
        Assert.Contains("TC-PR-7.1.1 | Passed", deployment);
        Assert.Contains("TC-PR-7.1.2 | Passed", deployment);
        Assert.Contains("TC-PR-7.1.3 | Passed as repository-verifiable contract", deployment);
        Assert.Contains("TC-PR-7.1.4 | Passed as CI/CD evidence path", deployment);

        foreach (var requiredText in new[]
        {
            "Gccs__DataPosture: No-CUI / compliance management only",
            "PRODUCTION_CUSTOMER_DATA_MODE: no-cui-only",
            "AZURE_CREDENTIALS_GCCS_PRODUCTION",
            "AZURE_STATIC_WEB_APPS_API_TOKEN_GCCS_PRODUCTION",
            "PRODUCTION_DATABASE_URL",
            "Generate idempotent production migration script",
            "Apply production migrations through approved CI/CD",
            "Run production health checks",
            "Record production deployment evidence",
            "production-deployment-evidence"
        })
        {
            Assert.Contains(requiredText, workflow);
        }

        foreach (var requiredText in new[]
        {
            "No-CUI / compliance management only",
            "no-cui-only",
            "database",
            "object_storage",
            "cache",
            "queue",
            "secrets",
            "background_jobs",
            "health_checks",
            "logs",
            "alerts"
        })
        {
            Assert.Contains(requiredText, productionContract);
        }
    }

    [Fact]
    public void TC_PR_7_2_Production_smoke_evidence_records_scanner_backed_pass()
    {
        var smoke = ReadText("docs", "production-readiness-production-smoke-evidence.md");
        using var authenticatedSmoke = JsonDocument.Parse(ReadText("output", "playwright", "production-readiness", "pr-7.2", "authenticated-production-smoke.json"));
        var onboarding = ReadText("docs", "production-readiness-pilot-onboarding.md");
        var checklist = ReadText("docs", "production-readiness-checklist.md");
        var closure = ReadText("docs", "production-readiness-launch-closure-evidence.md");
        var riskLog = ReadText("docs", "production-readiness-launch-gap-decisions.md");

        Assert.Contains("Smoke status: passed for PR-7.2 scanner-backed production smoke", smoke);
        Assert.Contains("Current gate result: passed for PR-7.2", smoke);
        Assert.Contains("Pilot onboarding must not start while any row in the smoke test matrix is `Blocked`, `Failed`, `Missing`, or `Unreviewed`.", smoke);
        Assert.Contains("TC-PR-7.2.1 | Passed", smoke);
        Assert.Contains("TC-PR-7.2.2 | Passed", smoke);
        Assert.Contains("File upload returned `201`, `malwareScanStatus=clean`, and `isUsable=true`", smoke);
        Assert.Contains("TC-PR-7.2.3 | Passed", smoke);
        Assert.Contains("TC-PR-7.2.4 | Passed as gate", smoke);
        Assert.Contains("Production byte-level evidence upload failed closed with `503 malware_scanner_unavailable`", riskLog);
        Assert.Contains("PR72-PROD-SMOKE-001", riskLog);
        Assert.Contains("PR72-PROD-SMOKE-002", riskLog);
        Assert.Contains("PR72-ALERT-ROUTE-001", riskLog);
        Assert.Contains("Closed on 2026-07-05 by signed-in production smoke session", riskLog);
        Assert.Contains("Pilot onboarding may begin only after `docs/production-readiness-production-smoke-evidence.md` records a reviewed PR-7.2 production smoke pass", onboarding);
        Assert.Contains("Production scanner evidence is attached for PR-7.2", onboarding);
        Assert.Contains("Passed on 2026-07-05 with scanner-backed byte upload", checklist);
        Assert.Contains("Production smoke tests | PR-7.2 | Passed on 2026-07-05 with synthetic-only data after scanner setup.", closure);
        Assert.Equal("passed", authenticatedSmoke.RootElement.GetProperty("result").GetString());
        Assert.False(authenticatedSmoke.RootElement.GetProperty("tokenCapturedInArtifact").GetBoolean());
        Assert.False(authenticatedSmoke.RootElement.GetProperty("containsCustomerData").GetBoolean());
        Assert.False(authenticatedSmoke.RootElement.GetProperty("containsCui").GetBoolean());
        Assert.Equal("Owner", authenticatedSmoke.RootElement.GetProperty("signedIn").GetProperty("roles")[0].GetString());
        Assert.Equal(403, authenticatedSmoke.RootElement.GetProperty("rbacDenial").GetProperty("deniedStatus").GetInt32());
        Assert.Equal(201, authenticatedSmoke.RootElement.GetProperty("uploadGuardrails").GetProperty("fileUpload").GetProperty("status").GetInt32());
        Assert.Equal("clean", authenticatedSmoke.RootElement.GetProperty("uploadGuardrails").GetProperty("fileUpload").GetProperty("malwareScanStatus").GetString());
        Assert.True(authenticatedSmoke.RootElement.GetProperty("uploadGuardrails").GetProperty("fileUpload").GetProperty("isUsable").GetBoolean());
        Assert.Equal(JsonValueKind.Null, authenticatedSmoke.RootElement.GetProperty("blocker").ValueKind);
        Assert.Equal("verified_by_action_group_delivery_receipt", authenticatedSmoke.RootElement.GetProperty("alerts").GetProperty("externalAlertRouteOwnerReceipt").GetString());
        Assert.Contains("PR-7.3 controlled pilot onboarding is authorized", riskLog);
    }

    [Fact]
    public void TC_PR_7_2_Smoke_gate_requires_no_cui_synthetic_data_and_operational_signals()
    {
        var smoke = ReadText("docs", "production-readiness-production-smoke-evidence.md");
        var deployment = ReadText("docs", "production-readiness-production-deployment-evidence.md");

        foreach (var requiredText in new[]
        {
            "Synthetic or non-sensitive tenant, user, upload, evidence, report, and audit data only.",
            "login, tenant access, RBAC denial, upload warning and blocking behavior, evidence upload, report generation, audit logging, logs, alerts, and `/health`",
            "Production workflow run URL",
            "output/playwright/production-readiness/pr-7.2/authenticated-production-smoke.json",
            "Synthetic smoke tenant ID",
            "Health output location",
            "Audit event references",
            "Log/alert evidence location",
            "MalwareScanning__Host",
            "gccs-clamav-production",
            "gccs-api-production-http5xx",
            "production-alert-email-receipt.json",
            "PR72-PROD-SMOKE-002",
            "PR72-ALERT-ROUTE-001",
            "Do not include secrets, customer data, real CUI, raw file contents, unrestricted logs, or sensitive incident details.",
            "no real CUI, classified data, export-controlled data, credentials, sensitive personal data, or unrestricted logs are authorized for smoke testing"
        })
        {
            Assert.Contains(requiredText, smoke);
        }

        Assert.Contains("PR-7.2 authenticated production smoke evidence is attached", deployment);
        Assert.Contains("PR-7.2 scanner-backed production smoke passed", deployment);
        Assert.Contains("PR72-ALERT-ROUTE-001", deployment);
    }

    [Fact]
    public void TC_PR_7_3_Controlled_pilot_onboarding_records_smoke_gate_and_non_sensitive_cohort()
    {
        var evidence = ReadText("docs", "production-readiness-pilot-onboarding-evidence.md");
        var smoke = ReadText("docs", "production-readiness-production-smoke-evidence.md");
        var checklist = ReadText("docs", "production-readiness-checklist.md");
        var closure = ReadText("docs", "production-readiness-launch-closure-evidence.md");
        var riskLog = ReadText("docs", "production-readiness-launch-gap-decisions.md");
        using var evidenceJson = JsonDocument.Parse(ReadText("output", "playwright", "production-readiness", "pr-7.3", "pilot-onboarding-evidence.json"));

        Assert.Contains("Story: PR-7.3 - Onboard Controlled Pilot Customers.", evidence);
        Assert.Contains("controlled pilot onboarding authorized", evidence, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Current gate result: passed for PR-7.2", smoke);
        Assert.Contains("PILOT-001", evidence);
        Assert.Contains("PILOT-002", evidence);
        Assert.DoesNotContain("@", evidence);
        Assert.DoesNotContain(".com", evidence, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Controlled pilot onboarding", checklist);
        Assert.Contains("Controlled pilot onboarding | PR-7.3", closure);
        Assert.Contains("PR-7.3 controlled pilot onboarding is recorded", riskLog);
        Assert.Equal("controlled_pilot_onboarding_authorized", evidenceJson.RootElement.GetProperty("result").GetString());
        Assert.False(evidenceJson.RootElement.GetProperty("tokenCapturedInArtifact").GetBoolean());
        Assert.False(evidenceJson.RootElement.GetProperty("containsCustomerData").GetBoolean());
        Assert.False(evidenceJson.RootElement.GetProperty("containsCui").GetBoolean());
        Assert.False(evidenceJson.RootElement.GetProperty("containsRealCustomerIdentifiers").GetBoolean());
        Assert.Equal("passed", evidenceJson.RootElement.GetProperty("productionSmokeGate").GetProperty("status").GetString());
    }

    [Fact]
    public void TC_PR_7_3_Pilot_tenants_record_no_cui_roles_support_acknowledgement_and_first_use_monitoring()
    {
        var evidence = ReadText("docs", "production-readiness-pilot-onboarding-evidence.md");
        var onboarding = ReadText("docs", "production-readiness-pilot-onboarding.md");
        var runbooks = ReadText("docs", "production-readiness-support-runbooks.md");
        using var evidenceJson = JsonDocument.Parse(ReadText("output", "playwright", "production-readiness", "pr-7.3", "pilot-onboarding-evidence.json"));

        foreach (var requiredText in new[]
        {
            "No-CUI guidance, prohibited data examples, support paths, known limitations, synthetic demo scope",
            "NoCui",
            "Owner, Admin, Compliance Manager, Contributor, Auditor, Advisor",
            "Required before evidence/document upload and per-file attestation",
            "Monitor first company profile, contract metadata, obligation/task, allowed evidence, report, and audit-log events",
            "TC-PR-7.3.1 | Passed",
            "TC-PR-7.3.2 | Passed",
            "TC-PR-7.3.3 | Passed",
            "TC-PR-7.3.4 | Passed"
        })
        {
            Assert.Contains(requiredText, evidence);
        }

        Assert.Contains("Support Paths", onboarding);
        Assert.Contains("Known Limitations", onboarding);
        Assert.Contains("Runbook: Prohibited Upload", runbooks);
        Assert.Contains("Runbook: Suspected CUI", runbooks);
        Assert.Contains("Runbook: Tenant Exposure", runbooks);

        var pilotCohort = evidenceJson.RootElement.GetProperty("pilotCohort");
        Assert.Equal(2, pilotCohort.GetArrayLength());

        foreach (var pilot in pilotCohort.EnumerateArray())
        {
            Assert.StartsWith("PILOT-", pilot.GetProperty("pilotId").GetString(), StringComparison.Ordinal);
            Assert.Equal("NoCui", pilot.GetProperty("tenantMode").GetString());
            Assert.True(pilot.GetProperty("onboardingMaterialsDelivered").GetBoolean());
            Assert.True(pilot.GetProperty("noCuiGuidanceDelivered").GetBoolean());
            Assert.True(pilot.GetProperty("prohibitedDataExamplesDelivered").GetBoolean());
            Assert.True(pilot.GetProperty("supportPathsDelivered").GetBoolean());
            Assert.True(pilot.GetProperty("knownLimitationsDelivered").GetBoolean());
            Assert.True(pilot.GetProperty("acknowledgementRequiredBeforeUpload").GetBoolean());
            Assert.True(pilot.GetProperty("supportRouteActive").GetBoolean());
            Assert.Equal("active", pilot.GetProperty("firstWorkflowMonitoring").GetProperty("status").GetString());
            Assert.Contains(pilot.GetProperty("rolesVerified").EnumerateArray(), role => role.GetString() == "Owner");
            Assert.Contains(pilot.GetProperty("rolesVerified").EnumerateArray(), role => role.GetString() == "Advisor");
        }
    }

    [Fact]
    public void TC_PR_8_1_Daily_pilot_monitoring_covers_required_production_and_support_signals()
    {
        var monitoring = ReadText("docs", "production-readiness-pilot-monitoring.md");
        var checklist = ReadText("docs", "production-readiness-checklist.md");
        var closure = ReadText("docs", "production-readiness-launch-closure-evidence.md");
        using var monitoringJson = JsonDocument.Parse(ReadText("output", "playwright", "production-readiness", "pr-8.1", "pilot-monitoring-evidence.json"));

        foreach (var requiredSignal in new[]
        {
            "Audit logs",
            "Upload blocks",
            "Permission denials",
            "Report failures",
            "Support tickets",
            "Content disputes",
            "Health checks",
            "Alerts",
            "Failed jobs"
        })
        {
            Assert.Contains(requiredSignal, monitoring, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(requiredSignal, checklist, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("Day-zero pilot monitoring", closure);
        Assert.Equal("day_zero_pilot_monitoring_established", monitoringJson.RootElement.GetProperty("result").GetString());
        Assert.False(monitoringJson.RootElement.GetProperty("tokenCapturedInArtifact").GetBoolean());
        Assert.False(monitoringJson.RootElement.GetProperty("containsCustomerData").GetBoolean());
        Assert.False(monitoringJson.RootElement.GetProperty("containsCui").GetBoolean());
        Assert.Equal(9, monitoringJson.RootElement.GetProperty("monitoringSignals").GetArrayLength());
    }

    [Fact]
    public void TC_PR_8_1_Findings_have_ownership_and_regressions_are_tracked_in_risk_log()
    {
        var monitoring = ReadText("docs", "production-readiness-pilot-monitoring.md");
        var riskLog = ReadText("docs", "production-readiness-launch-gap-decisions.md");
        using var monitoringJson = JsonDocument.Parse(ReadText("output", "playwright", "production-readiness", "pr-8.1", "pilot-monitoring-evidence.json"));

        foreach (var header in new[] { "Severity", "Owner", "Mitigation", "Target date", "Status", "Risk or backlog link" })
        {
            Assert.Contains(header, monitoring);
        }

        foreach (var findingId in new[] { "PR81-MONITOR-001", "PR81-MONITOR-002" })
        {
            Assert.Contains(findingId, monitoring);
            Assert.Contains(findingId, riskLog);
        }

        foreach (var finding in monitoringJson.RootElement.GetProperty("findings").EnumerateArray())
        {
            AssertRequiredString(finding, "id");
            AssertRequiredString(finding, "signal");
            AssertRequiredString(finding, "severity");
            AssertRequiredString(finding, "owner");
            AssertRequiredString(finding, "mitigation");
            AssertRequiredString(finding, "targetDate");
            AssertRequiredString(finding, "status");
            AssertRequiredString(finding, "riskOrBacklogLink");
        }
    }

    [Fact]
    public void TC_PR_8_1_High_risk_pilot_signals_escalate_through_runbooks()
    {
        var monitoring = ReadText("docs", "production-readiness-pilot-monitoring.md");
        var runbooks = ReadText("docs", "production-readiness-support-runbooks.md");
        using var monitoringJson = JsonDocument.Parse(ReadText("output", "playwright", "production-readiness", "pr-8.1", "pilot-monitoring-evidence.json"));

        foreach (var requiredRunbook in new[]
        {
            "Runbook: Prohibited Upload",
            "Runbook: Suspected CUI",
            "Runbook: Tenant Exposure",
            "Runbook: Access Issue",
            "Runbook: Evidence Failure",
            "Runbook: Report Failure",
            "Runbook: Content Correction",
            "Runbook: Security Incident",
            "Runbook: Backup Restore",
            "Runbook: Rollback"
        })
        {
            Assert.Contains(requiredRunbook, runbooks);
        }

        foreach (var escalationSignal in new[]
        {
            "Suspected CUI",
            "tenant isolation",
            "data-handling",
            "overclaim",
            "legal or contracting advisor"
        })
        {
            Assert.Contains(escalationSignal, monitoring, StringComparison.OrdinalIgnoreCase);
        }

        var escalationCoverage = monitoringJson.RootElement.GetProperty("escalationCoverage");
        Assert.True(escalationCoverage.GetProperty("security").GetBoolean());
        Assert.True(escalationCoverage.GetProperty("tenantIsolation").GetBoolean());
        Assert.True(escalationCoverage.GetProperty("dataHandling").GetBoolean());
        Assert.True(escalationCoverage.GetProperty("suspectedCui").GetBoolean());
        Assert.True(escalationCoverage.GetProperty("overclaim").GetBoolean());
        Assert.Equal(10, escalationCoverage.GetProperty("runbooks").GetArrayLength());
    }

    [Fact]
    public void TC_PR_8_2_Post_launch_readiness_review_records_date_participants_agenda_findings_and_decisions()
    {
        var review = ReadText("docs", "production-readiness-post-launch-review.md");
        using var reviewJson = JsonDocument.Parse(ReadText("output", "playwright", "production-readiness", "pr-8.2", "post-launch-readiness-review.json"));

        Assert.Contains("Story: PR-8.2 - Hold Post-Launch Readiness Review.", review);
        Assert.Contains("Review date: 2026-07-05.", review);
        Assert.Contains("## Participants", review);
        Assert.Contains("## Agenda", review);
        Assert.Contains("## Findings And Decisions", review);

        foreach (var participant in new[]
        {
            "Product owner",
            "Customer success/support owner",
            "Engineering lead",
            "Security owner",
            "Compliance content owner",
            "Legal or contracting advisor"
        })
        {
            Assert.Contains(participant, review);
        }

        Assert.Equal("post_launch_readiness_review_recorded", reviewJson.RootElement.GetProperty("result").GetString());
        Assert.Equal("2026-07-05", reviewJson.RootElement.GetProperty("review").GetProperty("date").GetString());
        Assert.True(reviewJson.RootElement.GetProperty("review").GetProperty("participants").GetArrayLength() >= 6);
        Assert.True(reviewJson.RootElement.GetProperty("review").GetProperty("agenda").GetArrayLength() >= 6);
        Assert.False(reviewJson.RootElement.GetProperty("tokenCapturedInArtifact").GetBoolean());
        Assert.False(reviewJson.RootElement.GetProperty("containsCustomerData").GetBoolean());
        Assert.False(reviewJson.RootElement.GetProperty("containsCui").GetBoolean());
    }

    [Fact]
    public void TC_PR_8_2_Review_covers_pilot_signals_and_assigns_regressions()
    {
        var review = ReadText("docs", "production-readiness-post-launch-review.md");
        using var reviewJson = JsonDocument.Parse(ReadText("output", "playwright", "production-readiness", "pr-8.2", "post-launch-readiness-review.json"));

        foreach (var reviewedSignal in new[]
        {
            "incidents",
            "defects",
            "support tickets",
            "upload blocks",
            "permission denials",
            "content disputes",
            "report failures",
            "customer feedback"
        })
        {
            Assert.Contains(reviewedSignal, review, StringComparison.OrdinalIgnoreCase);
            Assert.True(reviewJson.RootElement.GetProperty("reviewedSignals").TryGetProperty(ToCamelCaseKey(reviewedSignal), out _));
        }

        foreach (var decision in reviewJson.RootElement.GetProperty("decisions").EnumerateArray())
        {
            AssertRequiredString(decision, "findingId");
            AssertRequiredString(decision, "severity");
            AssertRequiredString(decision, "owner");
            AssertRequiredString(decision, "mitigation");
            AssertRequiredString(decision, "dueDate");
            AssertRequiredString(decision, "decision");
            AssertRequiredString(decision, "followUpAction");
        }

        Assert.Contains("PR81-MONITOR-001", review);
        Assert.Contains("PR81-MONITOR-002", review);
    }

    [Fact]
    public void TC_PR_8_2_Material_findings_update_launch_artifacts()
    {
        var review = ReadText("docs", "production-readiness-post-launch-review.md");
        var checklist = ReadText("docs", "production-readiness-checklist.md");
        var closure = ReadText("docs", "production-readiness-launch-closure-evidence.md");
        var riskLog = ReadText("docs", "production-readiness-launch-gap-decisions.md");
        using var reviewJson = JsonDocument.Parse(ReadText("output", "playwright", "production-readiness", "pr-8.2", "post-launch-readiness-review.json"));

        Assert.Contains("Artifact Update Decisions", review);
        Assert.Contains("Post-launch readiness review", checklist);
        Assert.Contains("Post-launch readiness review | PR-8.2", closure);
        Assert.Contains("PR-8.2 post-launch readiness review is recorded", riskLog);

        var artifactUpdates = reviewJson.RootElement.GetProperty("artifactUpdates").EnumerateArray().Select(element => element.GetString()).ToArray();
        Assert.Contains("docs/production-readiness-checklist.md", artifactUpdates);
        Assert.Contains("docs/production-readiness-launch-closure-evidence.md", artifactUpdates);
        Assert.Contains("docs/production-readiness-launch-gap-decisions.md", artifactUpdates);
    }

    [Fact]
    public void TC_PR_8_3_Launch_findings_are_converted_into_definition_of_ready_backlog_items()
    {
        var gate = ReadText("docs", "production-readiness-phase-2-gate.md");
        var definitionOfReady = ReadText("docs", "definition-of-ready.md");
        using var gateJson = JsonDocument.Parse(ReadText("output", "playwright", "production-readiness", "pr-8.3", "phase-2-gate.json"));

        Assert.Contains("Definition-of-Ready Backlog Items", gate);
        Assert.Contains("clear user story", definitionOfReady, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("testable acceptance criteria", definitionOfReady, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Tenant isolation, RBAC, audit logging, and CUI/data-handling implications", definitionOfReady);

        foreach (var backlogId in new[] { "PR83-BACKLOG-001", "PR83-BACKLOG-002" })
        {
            Assert.Contains(backlogId, gate);
            Assert.Contains("Closed on 2026-07-05", gate);
        }

        foreach (var backlogItem in gateJson.RootElement.GetProperty("backlogItems").EnumerateArray())
        {
            AssertRequiredString(backlogItem, "id");
            AssertRequiredString(backlogItem, "owner");
            AssertRequiredString(backlogItem, "targetDate");
            AssertRequiredString(backlogItem, "readyStatus");
            Assert.True(backlogItem.GetProperty("definitionOfReadyFieldsPresent").GetBoolean());
            Assert.NotEmpty(backlogItem.GetProperty("sourceFindings").EnumerateArray());
        }
    }

    [Fact]
    public void TC_PR_8_3_Phase_2_is_eligible_only_after_external_control_evidence_is_closed()
    {
        var gate = ReadText("docs", "production-readiness-phase-2-gate.md");
        var riskLog = ReadText("docs", "production-readiness-launch-gap-decisions.md");
        using var gateJson = JsonDocument.Parse(ReadText("output", "playwright", "production-readiness", "pr-8.3", "phase-2-gate.json"));

        Assert.Contains("Gate status: **Approved for solo-controlled pilot testing**.", gate);
        Assert.Contains("Decision: Phase 2 Govcon Intelligence is approved for solo-controlled pilot testing and project completion only.", gate);
        Assert.Contains("PR83-PHASE2-GATE-001", riskLog);
        Assert.Equal("phase_2_approved_for_solo_controlled_pilot_testing", gateJson.RootElement.GetProperty("result").GetString());
        Assert.Equal("Approved for solo-controlled pilot testing", gateJson.RootElement.GetProperty("gateStatus").GetString());
        Assert.Equal("solo_controlled_pilot_testing_only", gateJson.RootElement.GetProperty("approval").GetProperty("approvalType").GetString());
        Assert.True(gateJson.RootElement.GetProperty("approval").GetProperty("doesNotReplaceProductionSeparationOfDuties").GetBoolean());
        Assert.False(gateJson.RootElement.GetProperty("approval").GetProperty("authorizesBroaderCustomerLaunch").GetBoolean());
        Assert.False(gateJson.RootElement.GetProperty("approval").GetProperty("authorizesCuiProcessing").GetBoolean());
        Assert.False(gateJson.RootElement.GetProperty("approval").GetProperty("weakensFutureProductionApprovalRequirements").GetBoolean());

        foreach (var blockedCapability in new[]
        {
            "Automated clause extraction",
            "AI-suggested obligations",
            "Search indexing",
            "Applicability automation",
            "Expanded upload, import, paste, extraction, report export, search, or AI processing"
        })
        {
            Assert.Contains(blockedCapability, gate, StringComparison.OrdinalIgnoreCase);
        }

        var failedCriteria = gateJson.RootElement.GetProperty("stabilityCriteria")
            .EnumerateArray()
            .Where(criteria => criteria.GetProperty("passFail").GetString() == "Fail")
            .Select(criteria => criteria.GetProperty("controlArea").GetString())
            .ToArray();

        Assert.Empty(failedCriteria);
        Assert.Contains("This approval does not replace production separation of duties", gate);
    }

    [Fact]
    public void TC_PR_8_3_Stability_criteria_identify_evidence_owner_approvers_and_status()
    {
        var gate = ReadText("docs", "production-readiness-phase-2-gate.md");
        using var gateJson = JsonDocument.Parse(ReadText("output", "playwright", "production-readiness", "pr-8.3", "phase-2-gate.json"));

        foreach (var controlArea in new[]
        {
            "Tenant isolation",
            "RBAC",
            "Upload controls",
            "Reports",
            "Audit logging",
            "Support",
            "Content governance",
            "Customer claims",
            "No-CUI posture",
            "Restore readiness",
            "Alert owner receipt"
        })
        {
            Assert.Contains(controlArea, gate);
        }

        Assert.Contains("Required evidence", gate);
        Assert.Contains("Required approvers", gate);
        Assert.Contains("Pass/fail", gate);

        foreach (var criteria in gateJson.RootElement.GetProperty("stabilityCriteria").EnumerateArray())
        {
            AssertRequiredString(criteria, "controlArea");
            AssertRequiredString(criteria, "owner");
            AssertRequiredString(criteria, "passFail");
            Assert.NotEmpty(criteria.GetProperty("requiredApprovers").EnumerateArray());
        }
    }

    [Fact]
    public void TC_PR_8_3_Gate_status_is_recorded_before_govcon_intelligence_proceeds()
    {
        var gate = ReadText("docs", "production-readiness-phase-2-gate.md");
        var checklist = ReadText("docs", "production-readiness-checklist.md");
        var closure = ReadText("docs", "production-readiness-launch-closure-evidence.md");
        var roadmap = ReadText("docs", "mvp-roadmap.md");
        using var gateJson = JsonDocument.Parse(ReadText("output", "playwright", "production-readiness", "pr-8.3", "phase-2-gate.json"));

        Assert.Contains("Phase 2 - Govcon Intelligence", roadmap);
        Assert.Contains("Phase 2 gate", checklist);
        Assert.Contains("Phase 2 gate | PR-8.3", closure);
        Assert.Contains("before Govcon Intelligence work proceeds", gate);
        Assert.Contains("PR83-BACKLOG-001 and PR83-BACKLOG-002 are completed or separately dispositioned", gate);
        Assert.Equal("Approved for solo-controlled pilot testing", gateJson.RootElement.GetProperty("gateStatus").GetString());
        Assert.Contains("solo-controlled pilot testing and project completion only", gate);
        Assert.False(gateJson.RootElement.GetProperty("tokenCapturedInArtifact").GetBoolean());
        Assert.False(gateJson.RootElement.GetProperty("containsCustomerData").GetBoolean());
        Assert.False(gateJson.RootElement.GetProperty("containsCui").GetBoolean());
    }

    private static void AssertRequiredString(JsonElement element, string propertyName)
    {
        Assert.True(element.TryGetProperty(propertyName, out var property), $"Missing required property '{propertyName}'.");
        Assert.False(string.IsNullOrWhiteSpace(property.GetString()), $"Property '{propertyName}' must not be blank.");
    }

    private static string ToCamelCaseKey(string signal) =>
        signal switch
        {
            "support tickets" => "supportTickets",
            "upload blocks" => "uploadBlocks",
            "permission denials" => "permissionDenials",
            "content disputes" => "contentDisputes",
            "report failures" => "reportFailures",
            "customer feedback" => "customerFeedback",
            _ => signal
        };

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

    private static void AssertRequiredApprovedApproverTableRows(string artifact)
    {
        foreach (var (approver, scope) in RequiredApproverScopes())
        {
            Assert.Contains($"| {approver} | Approved on 2026-07-03 by accountable solo-controlled pilot approver for {scope} | No for solo-controlled pilot testing; yes for broader production launch |", artifact);
        }
    }

    private static void AssertRequiredApprovedApproverApprovalRows(string artifact)
    {
        Assert.Contains("| Required approver | Approval status | Approval date | Approver | Scope | Limitations | Unresolved exceptions | Evidence reviewed | Launch blocker while pending |", artifact);

        foreach (var (approver, scope) in RequiredApproverScopes())
        {
            Assert.Contains($"| {approver} | Approved for solo-controlled pilot testing | 2026-07-03 | User acting as accountable solo-controlled pilot approver for {scope} |", artifact);
        }

        Assert.Contains("This approval does not replace production separation of duties", artifact);
        Assert.Contains("does not authorize broader customer launch", artifact);
        Assert.Contains("does not authorize CUI processing", artifact);
        Assert.Contains("does not weaken future production approval requirements", artifact);

        foreach (var field in new[]
        {
            "Approval date",
            "Approver",
            "Scope",
            "Limitations",
            "Unresolved exceptions",
            "Evidence reviewed"
        })
        {
            Assert.Contains(field, artifact);
        }
    }

    private static IEnumerable<string> RequiredApprovers()
    {
        yield return "Product owner";
        yield return "Engineering lead";
        yield return "Security owner";
        yield return "Compliance content owner";
        yield return "Customer success/support owner";
        yield return "Legal or contracting advisor";
    }

    private static IEnumerable<(string Approver, string Scope)> RequiredApproverScopes()
    {
        yield return ("Product owner", "product-owner scope");
        yield return ("Engineering lead", "engineering scope");
        yield return ("Security owner", "security scope");
        yield return ("Compliance content owner", "compliance-content scope");
        yield return ("Customer success/support owner", "support scope");
        yield return ("Legal or contracting advisor", "legal/contracting scope");
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

    private static IEnumerable<string[]> ClaimReviewDocuments()
    {
        foreach (var document in LaunchFacingDocuments())
        {
            yield return document;
        }

        yield return new[] { "docs", "production-readiness-customer-claims-review.md" };
        yield return new[] { "docs", "production-readiness-launch-closure-evidence.md" };
        yield return new[] { "apps", "web", "src", "App.tsx" };
        yield return new[] { "apps", "web", "src", "lib", "api.ts" };
        yield return new[] { "packages", "compliance-content", "data-handling-notices", "notices.json" };
    }

    private static IEnumerable<string> ForbiddenAffirmativeCustomerClaims()
    {
        yield return "GCCS provides legal advice";
        yield return "makes legal determinations";
        yield return "guarantees CMMC";
        yield return "CMMC certified";
        yield return "CMMC certification achieved";
        yield return "CMMC approval granted";
        yield return "official assessment success achieved";
        yield return "government endorsed";
        yield return "government endorsement granted";
        yield return "officially approved by the government";
        yield return "authorized to store real CUI";
        yield return "authorized to upload real CUI";
        yield return "permission to store real CUI";
        yield return "permission to upload real CUI";
        yield return "real customer CUI is allowed";
    }

    private static IEnumerable<string> SupportRunbookTopics()
    {
        yield return "Prohibited Upload";
        yield return "Suspected CUI";
        yield return "Tenant Exposure";
        yield return "Access Issue";
        yield return "Evidence Failure";
        yield return "Report Failure";
        yield return "Content Correction";
        yield return "Security Incident";
        yield return "Backup Restore";
        yield return "Rollback";
    }

    private static string ExtractRunbookSection(string runbooks, string topic)
    {
        var marker = $"## Runbook: {topic}";
        var start = runbooks.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Missing runbook section for {topic}.");

        var next = runbooks.IndexOf("## Runbook:", start + marker.Length, StringComparison.Ordinal);
        return next < 0 ? runbooks[start..] : runbooks[start..next];
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

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

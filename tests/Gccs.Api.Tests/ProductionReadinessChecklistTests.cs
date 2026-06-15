using System.Text.Json;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class ProductionReadinessChecklistTests
{
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

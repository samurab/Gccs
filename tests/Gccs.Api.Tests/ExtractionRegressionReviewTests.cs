using System.Text.Json;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class ExtractionRegressionReviewTests
{
    private static readonly HashSet<string> AllowedClassifications = new(StringComparer.OrdinalIgnoreCase)
    {
        "parser",
        "matcher",
        "library",
        "label",
        "source_quality",
        "expected_limitation"
    };

    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "open",
        "in_progress",
        "resolved",
        "accepted_risk"
    };

    private static readonly HashSet<string> UpdateLinkTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "matcher",
        "library",
        "parser",
        "label"
    };

    [Fact]
    public void TC_28_3_1_Reviewed_failures_have_classification_owner_status_and_resolution_note()
    {
        using var records = LoadReviewRecords();

        foreach (var record in records.RootElement.GetProperty("records").EnumerateArray())
        {
            Assert.Contains(record.GetProperty("classification").GetString() ?? string.Empty, AllowedClassifications);
            Assert.False(string.IsNullOrWhiteSpace(record.GetProperty("owner").GetString()));
            Assert.Contains(record.GetProperty("status").GetString() ?? string.Empty, AllowedStatuses);
            Assert.False(string.IsNullOrWhiteSpace(record.GetProperty("resolutionNote").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(record.GetProperty("failureType").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(record.GetProperty("citation").GetString()));
        }
    }

    [Fact]
    public void TC_28_3_2_Follow_up_tasks_can_be_created_from_failures()
    {
        using var records = LoadReviewRecords();

        foreach (var record in records.RootElement.GetProperty("records").EnumerateArray())
        {
            var task = record.GetProperty("followUpTask");
            Assert.False(string.IsNullOrWhiteSpace(task.GetProperty("taskId").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(task.GetProperty("title").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(task.GetProperty("status").GetString()));
        }
    }

    [Fact]
    public void TC_28_3_3_Resolved_failures_link_to_applicable_updates()
    {
        using var records = LoadReviewRecords();
        var resolved = records.RootElement.GetProperty("records")
            .EnumerateArray()
            .Where(record => string.Equals(record.GetProperty("status").GetString(), "resolved", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.NotEmpty(resolved);
        foreach (var record in resolved)
        {
            var links = record.GetProperty("resolutionLinks").EnumerateArray().ToArray();
            Assert.NotEmpty(links);
            Assert.Contains(links, link => UpdateLinkTypes.Contains(link.GetProperty("type").GetString() ?? string.Empty));
            Assert.All(links, link => Assert.False(string.IsNullOrWhiteSpace(link.GetProperty("reference").GetString())));
        }
    }

    [Fact]
    public void TC_28_3_4_Release_summary_shows_open_risks_and_metric_trends()
    {
        var root = FindReviewRoot();
        using var summary = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "release-summary.json")));
        var markdown = File.ReadAllText(Path.Combine(root, "release-summary.md"));

        Assert.NotEmpty(summary.RootElement.GetProperty("metricTrends").EnumerateArray());
        Assert.NotEmpty(summary.RootElement.GetProperty("openRisks").EnumerateArray());
        Assert.False(string.IsNullOrWhiteSpace(summary.RootElement.GetProperty("releaseReadinessNote").GetString()));
        Assert.Contains("Metric Trends", markdown, StringComparison.Ordinal);
        Assert.Contains("Open Risks", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public void TC_28_3_5_Regression_review_records_are_traceable()
    {
        using var records = LoadReviewRecords();

        foreach (var record in records.RootElement.GetProperty("records").EnumerateArray())
        {
            var auditTrail = record.GetProperty("auditTrail").EnumerateArray().ToArray();
            Assert.NotEmpty(auditTrail);
            Assert.Contains(auditTrail, audit => audit.GetProperty("action").GetString() == "created");
            Assert.All(auditTrail, audit =>
            {
                Assert.False(string.IsNullOrWhiteSpace(audit.GetProperty("actor").GetString()));
                Assert.True(DateTimeOffset.TryParse(audit.GetProperty("at").GetString(), out _));
            });
        }
    }

    private static JsonDocument LoadReviewRecords() =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(FindReviewRoot(), "review-records.json")));

    private static string FindReviewRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "tests", "fixtures", "extraction-regression-review");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate tests/fixtures/extraction-regression-review.");
    }
}

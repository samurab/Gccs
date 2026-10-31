using System.Text.Json;
using Gccs.Domain.Common;
using Gccs.Infrastructure.Compliance;
using Gccs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class ProductionReadinessHighRiskObligationContentTests
{
    [Fact]
    public void TC_PR_5_1_1_High_risk_obligation_records_are_listed_for_review()
    {
        using var obligations = ReadJson(PackagePath);
        using var review = ReadJson(ReviewEvidencePath);

        var highRiskIds = GetHighRiskOrExpertReviewIds(obligations.RootElement).Order(StringComparer.Ordinal).ToArray();
        var reviewedIds = review.RootElement
            .GetProperty("records")
            .EnumerateArray()
            .Select(record => RequiredString(record, "id"))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(highRiskIds, reviewedIds);
        Assert.NotEmpty(reviewedIds);
    }

    [Fact]
    public void TC_PR_5_1_2_Published_obligations_include_required_source_and_review_metadata()
    {
        using var obligations = ReadJson(PackagePath);

        var published = obligations.RootElement
            .EnumerateArray()
            .Where(record => RequiredString(record, "review_state") == "published")
            .ToArray();

        Assert.NotEmpty(published);
        foreach (var record in published)
        {
            AssertRequiredText(record, "source");
            AssertRequiredText(record, "source_url");
            AssertRequiredText(record, "trigger_condition");
            AssertRequiredText(record, "confidence");
            AssertRequiredText(record, "review_owner");
            AssertRequiredText(record, "review_state");
            AssertRequiredText(record, "last_reviewed_at");
            Assert.NotEmpty(record.GetProperty("required_actions").EnumerateArray());
            Assert.NotEmpty(record.GetProperty("evidence_examples").EnumerateArray());
        }
    }

    [Fact]
    public async Task TC_PR_5_1_3_High_risk_records_without_publication_state_are_hidden_from_customer_facing_views()
    {
        await using var dbContext = CreateDbContext();
        var importer = new ComplianceContentImporter(dbContext);

        var report = await importer.ImportDirectoryAsync(ComplianceContentRoot);
        var repository = new EfObligationRepository(dbContext);
        var customerFacingObligations = await repository.ListAsync();

        Assert.True(report.Succeeded, string.Join(Environment.NewLine, report.Errors.Select(error => error.Message)));
        Assert.DoesNotContain(customerFacingObligations, obligation => obligation.Review.State != ReviewState.Published);

        using var review = ReadJson(ReviewEvidencePath);
        foreach (var record in review.RootElement.GetProperty("records").EnumerateArray())
        {
            var id = RequiredString(record, "id");
            var isCustomerFacing = record.GetProperty("customerFacingProduction").GetBoolean();
            var obligation = await repository.FindByIdAsync(id);

            if (isCustomerFacing)
            {
                Assert.NotNull(obligation);
                Assert.Equal(ReviewState.Published, obligation.Review.State);
            }
            else
            {
                Assert.Null(obligation);
            }
        }
    }

    [Fact]
    public void TC_PR_5_1_4_Content_approval_or_hiding_decisions_include_owner_date_and_rationale()
    {
        using var review = ReadJson(ReviewEvidencePath);

        foreach (var record in review.RootElement.GetProperty("records").EnumerateArray())
        {
            AssertRequiredText(record, "decision");
            AssertRequiredText(record, "decisionOwner");
            AssertRequiredText(record, "decisionDate");
            AssertRequiredText(record, "rationale");
            Assert.True(DateOnly.TryParse(RequiredString(record, "decisionDate"), out _));
            Assert.True(record.TryGetProperty("customerFacingProduction", out var customerFacing) && customerFacing.ValueKind is JsonValueKind.True or JsonValueKind.False);
        }

        Assert.NotEmpty(review.RootElement.GetProperty("blockers").EnumerateArray());
        foreach (var blocker in review.RootElement.GetProperty("blockers").EnumerateArray())
        {
            AssertRequiredText(blocker, "owner");
            AssertRequiredText(blocker, "targetDate");
            AssertRequiredText(blocker, "mitigation");
        }
    }

    private static GccsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<GccsDbContext>()
            .UseInMemoryDatabase($"pr-5-1-high-risk-obligations-{Guid.NewGuid():N}")
            .Options;

        return new GccsDbContext(options);
    }

    private static IReadOnlyList<string> GetHighRiskOrExpertReviewIds(JsonElement root) =>
        root.EnumerateArray()
            .Where(record =>
                RequiredString(record, "risk_level") is "high" or "critical" ||
                (record.TryGetProperty("requires_expert_review", out var requiresExpertReview) && requiresExpertReview.GetBoolean()))
            .Select(record => RequiredString(record, "id"))
            .ToArray();

    private static JsonDocument ReadJson(string path) =>
        JsonDocument.Parse(File.ReadAllText(path));

    private static void AssertRequiredText(JsonElement record, string propertyName) =>
        Assert.False(string.IsNullOrWhiteSpace(RequiredString(record, propertyName)), $"{RequiredString(record, "id")} is missing {propertyName}.");

    private static string RequiredString(JsonElement record, string propertyName)
    {
        Assert.True(record.TryGetProperty(propertyName, out var value), $"Missing property {propertyName}.");
        Assert.Equal(JsonValueKind.String, value.ValueKind);
        return value.GetString() ?? string.Empty;
    }

    private static string PackagePath => Path.Combine(RepositoryRoot, "packages", "compliance-content", "obligations", "mvp.json");

    private static string ComplianceContentRoot => Path.Combine(RepositoryRoot, "packages", "compliance-content");

    private static string ReviewEvidencePath => Path.Combine(RepositoryRoot, "output", "production-readiness", "expert-content", "high-risk-obligation-review.json");

    private static string RepositoryRoot
    {
        get
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current is not null && !File.Exists(Path.Combine(current.FullName, "Gccs.slnx")))
            {
                current = current.Parent;
            }

            if (current is null)
            {
                throw new DirectoryNotFoundException("Could not locate repository root from test output directory.");
            }

            return current.FullName;
        }
    }
}

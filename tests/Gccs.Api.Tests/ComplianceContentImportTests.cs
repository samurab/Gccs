using Gccs.Domain.Common;
using Gccs.Infrastructure.Compliance;
using Gccs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class ComplianceContentImportTests
{
    [Fact]
    public async Task TC_6_2_1_Import_valid_compliance_content_preserves_source_and_review_metadata()
    {
        await using var dbContext = CreateDbContext();
        var importer = new ComplianceContentImporter(dbContext);

        var report = await importer.ImportDirectoryAsync(GetComplianceContentPackageRoot());

        Assert.True(report.Succeeded, string.Join(Environment.NewLine, report.Errors.Select(error => error.Message)));
        Assert.True(report.ClausesCreated > 0);
        Assert.True(report.ObligationsCreated > 0);

        var clause = await dbContext.Clauses.SingleAsync(clause => clause.Id == "far-52-204-21");
        var obligation = await dbContext.Obligations.SingleAsync(obligation => obligation.Id == "far-52-204-21");
        var procurementIntegrityObligation = await dbContext.Obligations.SingleAsync(obligation => obligation.Id == "far-part-3-antitrust-procurement-integrity");

        Assert.Equal("https://www.acquisition.gov/far/52.204-21", clause.SourceUrl);
        Assert.Equal(new DateOnly(2026, 6, 3), clause.LastReviewedAt);
        Assert.Equal(ReviewState.Published, clause.ReviewState);
        Assert.Equal("https://www.acquisition.gov/far/52.204-21", obligation.SourceUrl);
        Assert.Equal(new DateOnly(2026, 6, 3), obligation.LastReviewedAt);
        Assert.Equal(ReviewState.Published, obligation.ReviewState);
        Assert.Contains("Access control policy", obligation.EvidenceExamplesJson);
        Assert.Equal("https://www.acquisition.gov/far/part-3", procurementIntegrityObligation.SourceUrl);
        Assert.Equal(new DateOnly(2026, 6, 25), procurementIntegrityObligation.LastReviewedAt);
        Assert.Equal(ReviewState.InReview, procurementIntegrityObligation.ReviewState);
        Assert.True(procurementIntegrityObligation.RequiresExpertReview);
        Assert.Contains("Independent price determination", procurementIntegrityObligation.EvidenceExamplesJson);
    }

    [Fact]
    public async Task TC_6_2_2_Import_schema_invalid_json_returns_actionable_errors()
    {
        await using var dbContext = CreateDbContext();
        var importer = new ComplianceContentImporter(dbContext);
        var filePath = WriteTempContent("""
[
  {
    "id": "bad-obligation",
    "source": "FAR 52.204-21",
    "title": "Missing required metadata",
    "trigger_condition": "Contract involves FCI.",
    "required_actions": ["Apply controls."],
    "evidence_examples": ["Access control policy"],
    "risk_level": "high",
    "last_reviewed_at": "not-a-date",
    "confidence": "high",
    "review_owner": "compliance-content-owner",
    "review_state": "approved"
  }
]
""");

        var report = await importer.ImportFileAsync(filePath);

        Assert.False(report.Succeeded);
        Assert.Contains(report.Errors, error =>
            error.File == filePath &&
            error.Path == "$[0].source_url" &&
            error.Field == "source_url");
        Assert.Contains(report.Errors, error =>
            error.File == filePath &&
            error.Path == "$[0].last_reviewed_at" &&
            error.Field == "last_reviewed_at");
        Assert.Empty(dbContext.Obligations);
    }

    [Fact]
    public async Task TC_6_2_3_Re_running_import_does_not_create_duplicates()
    {
        await using var dbContext = CreateDbContext();
        var importer = new ComplianceContentImporter(dbContext);
        var packageRoot = GetComplianceContentPackageRoot();

        var firstReport = await importer.ImportDirectoryAsync(packageRoot);
        var secondReport = await importer.ImportDirectoryAsync(packageRoot);

        Assert.True(firstReport.Succeeded);
        Assert.True(secondReport.Succeeded);
        Assert.Equal(0, secondReport.ClausesCreated);
        Assert.Equal(0, secondReport.ObligationsCreated);
        Assert.True(secondReport.ClausesUpdated > 0);
        Assert.True(secondReport.ObligationsUpdated > 0);
        Assert.Equal(await dbContext.Clauses.Select(clause => clause.Id).Distinct().CountAsync(), await dbContext.Clauses.CountAsync());
        Assert.Equal(await dbContext.Obligations.Select(obligation => obligation.Id).Distinct().CountAsync(), await dbContext.Obligations.CountAsync());
    }

    [Fact]
    public async Task TC_6_2_4_Import_reports_include_success_logs_and_failure_details()
    {
        await using var dbContext = CreateDbContext();
        var importer = new ComplianceContentImporter(dbContext);

        var successReport = await importer.ImportDirectoryAsync(GetComplianceContentPackageRoot());
        var failureReport = await importer.ImportFileAsync(WriteTempContent("""{ "not": "an array" }"""));

        Assert.True(successReport.Succeeded);
        Assert.Contains(successReport.Logs, log => log.Contains("Processing", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(successReport.Logs, log => log.Contains("Imported", StringComparison.OrdinalIgnoreCase));
        Assert.False(failureReport.Succeeded);
        Assert.Contains(failureReport.Errors, error => error.File.EndsWith(".json", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(failureReport.Logs, log => log.Contains("Import failed", StringComparison.OrdinalIgnoreCase));
    }

    private static GccsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<GccsDbContext>()
            .UseInMemoryDatabase($"compliance-content-import-{Guid.NewGuid():N}")
            .Options;

        return new GccsDbContext(options);
    }

    private static string GetComplianceContentPackageRoot()
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

        return Path.Combine(current.FullName, "packages", "compliance-content");
    }

    private static string WriteTempContent(string content)
    {
        var directory = Path.Combine(Path.GetTempPath(), $"gccs-content-import-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var filePath = Path.Combine(directory, "invalid.json");
        File.WriteAllText(filePath, content);
        return filePath;
    }
}

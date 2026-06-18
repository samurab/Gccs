using Gccs.Application.Demo;
using Gccs.Domain.Common;
using Gccs.Infrastructure.Demo;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class SyntheticDemoDatasetTests
{
    [Fact]
    public async Task TC_1A_3_1_1_Dataset_contains_no_real_sensitive_data()
    {
        var service = CreateService();
        var dataset = await service.GetAsync(GetDemoContentPackageRoot());

        var sampleText = string.Join(" ", dataset.Records.Select(record => record.SampleText));
        Assert.DoesNotContain("real customer CUI", sampleText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("classified", sampleText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("export-controlled technical data", sampleText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("customer proprietary", sampleText, StringComparison.OrdinalIgnoreCase);
        Assert.All(dataset.Records, record => Assert.Contains("Fictional", record.SampleText, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task TC_1A_3_1_2_Synthetic_records_are_classified_and_versioned()
    {
        var service = CreateService();
        var dataset = await service.GetAsync(GetDemoContentPackageRoot());

        Assert.All(dataset.Records, record =>
        {
            Assert.Equal(dataset.Metadata.Version, record.DatasetVersion);
            Assert.Equal(ContentClassification.SyntheticCui, record.Classification.Classification);
            Assert.Equal(ContentClassificationSource.ImportedDemoSeed, record.Classification.Source);
            Assert.True(record.Classification.IsApprovedDemoContent);
            Assert.Equal("Synthetic demo data", record.SyntheticLabel);
        });
    }

    [Fact]
    public async Task TC_1A_3_1_3_Dataset_includes_demo_ui_workflow_labels()
    {
        var service = CreateService();
        var dataset = await service.GetAsync(GetDemoContentPackageRoot());

        Assert.Contains(dataset.Records, record => record.RecordType == "Company" && record.SyntheticLabel == "Synthetic demo data");
        Assert.Contains(dataset.Records, record => record.RecordType == "Contract" && record.SyntheticLabel == "Synthetic demo data");
        Assert.Contains(dataset.Records, record => record.RecordType == "Evidence" && record.SyntheticLabel == "Synthetic demo data");
        Assert.Contains(dataset.Records, record => record.RecordType == "Cmmc" && record.SyntheticLabel == "Synthetic demo data");
        Assert.Contains(dataset.Records, record => record.RecordType == "Subcontractor" && record.SyntheticLabel == "Synthetic demo data");
        Assert.Contains(dataset.Records, record => record.RecordType == "Report" && record.SyntheticLabel == "Synthetic demo data");
    }

    [Fact]
    public async Task TC_1A_3_1_4_Dataset_metadata_is_complete()
    {
        var service = CreateService();
        var dataset = await service.GetAsync(GetDemoContentPackageRoot());

        Assert.Equal("gccs-synthetic-cui-demo", dataset.Metadata.DatasetId);
        Assert.False(string.IsNullOrWhiteSpace(dataset.Metadata.Purpose));
        Assert.NotEmpty(dataset.Metadata.Limitations);
        Assert.Equal("GCCS Compliance Content Owner", dataset.Metadata.Owner);
        Assert.False(string.IsNullOrWhiteSpace(dataset.Metadata.SourceBasis));
        Assert.Equal(new DateOnly(2026, 6, 18), dataset.Metadata.ReviewedAt);
        Assert.Equal("Phase 1A Compliance SME", dataset.Metadata.ApprovedReviewer);
        Assert.Equal("Approved", dataset.Metadata.ReviewStatus);
    }

    [Fact]
    public async Task TC_1A_3_1_5_Review_required_before_seed_import()
    {
        var service = CreateService();
        var packageRoot = CreateTempDataset("""
{
  "metadata": {
    "datasetId": "unapproved-demo",
    "version": "2026.06.unapproved",
    "name": "Unapproved Dataset",
    "purpose": "Negative test fixture.",
    "limitations": ["Do not import."],
    "owner": "Test Owner",
    "sourceBasis": "Unit test fixture.",
    "reviewedAt": null,
    "approvedReviewer": "",
    "reviewStatus": "Draft",
    "approvedForImport": false
  },
  "records": []
}
""");

        var result = await service.PrecheckAsync(packageRoot);

        Assert.False(result.Allowed);
        Assert.Contains(result.Errors, error => error.Contains("review status must be Approved", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Errors, error => error.Contains("approved for import", StringComparison.OrdinalIgnoreCase));
    }

    private static SyntheticDemoDatasetService CreateService() =>
        new(new FileSyntheticDemoDatasetRepository());

    private static string GetDemoContentPackageRoot()
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

        return Path.Combine(current.FullName, "packages", "demo-content");
    }

    private static string CreateTempDataset(string json)
    {
        var packageRoot = Path.Combine(Path.GetTempPath(), $"gccs-demo-dataset-{Guid.NewGuid():N}");
        var datasetDirectory = Path.Combine(packageRoot, "synthetic-cui");
        Directory.CreateDirectory(datasetDirectory);
        File.WriteAllText(Path.Combine(datasetDirectory, "dataset.json"), json);
        return packageRoot;
    }
}

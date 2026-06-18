using Gccs.Application.Tenancy;
using Gccs.Infrastructure.Tenancy;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class SharedResponsibilityMatrixTests
{
    [Fact]
    public async Task TC_1A_5_1_1_Matrix_includes_all_cui_readiness_categories()
    {
        var matrix = await CreateService().GetPublishedAsync(FindComplianceContentPackageRoot());

        Assert.All(SharedResponsibilityMatrixService.RequiredCategoryKeys, category =>
            Assert.Contains(matrix.Rows, row => row.Category == category));
    }

    [Fact]
    public async Task TC_1A_5_1_2_Each_row_has_required_owner_and_review_metadata()
    {
        var matrix = await CreateService().GetPublishedAsync(FindComplianceContentPackageRoot());

        Assert.All(matrix.Rows, row =>
        {
            Assert.False(string.IsNullOrWhiteSpace(row.Responsibility));
            Assert.False(string.IsNullOrWhiteSpace(row.Notes));
            Assert.False(string.IsNullOrWhiteSpace(row.SourceReference));
            Assert.False(string.IsNullOrWhiteSpace(row.ReviewOwner));
            Assert.False(string.IsNullOrWhiteSpace(row.Version));
            Assert.NotEqual(default, row.EffectiveAt);
        });
    }

    [Fact]
    public void TC_1A_5_1_3_Matrix_cannot_publish_without_owner_or_review_metadata()
    {
        var invalid = new SharedResponsibilityMatrixDto(
            MatrixId: "gccs-cui-ready-baseline",
            Version: "2026.06.phase1a",
            Title: "GCCS CUI-Ready Shared Responsibility Matrix",
            State: "Published",
            EffectiveAt: new DateOnly(2026, 6, 18),
            ReviewOwner: "",
            ReviewedAt: default,
            SourceReference: "Phase 1A CUI readiness baseline",
            Rows:
            [
                new SharedResponsibilityMatrixRowDto(
                    Category: "tenant-administration",
                    Responsibility: "Shared",
                    Notes: "",
                    SourceReference: "Phase 1A approval checklist",
                    EffectiveAt: default,
                    ReviewOwner: "",
                    Version: "")
            ]);

        var exception = Assert.Throws<SharedResponsibilityMatrixValidationException>(() =>
            SharedResponsibilityMatrixService.ValidateForPublish(invalid));

        Assert.Contains(exception.Errors, error => error.Contains("reviewOwner", StringComparison.Ordinal));
        Assert.Contains(exception.Errors, error => error.Contains("effectiveAt", StringComparison.Ordinal));
        Assert.Contains(exception.Errors, error => error.Contains("Required category", StringComparison.Ordinal));
    }

    [Fact]
    public async Task TC_1A_5_1_4_Published_matrix_is_visible_for_settings_and_checklist_surfaces()
    {
        var matrix = await CreateService().GetPublishedAsync(FindComplianceContentPackageRoot());

        Assert.Equal("Published", matrix.State);
        Assert.Equal("2026.06.phase1a", matrix.Version);
        Assert.Contains(matrix.Rows, row => row.Category == "shared-responsibility-matrix" || row.Category == "support");
    }

    [Fact]
    public async Task TC_1A_5_1_5_Source_controlled_matrix_has_publication_traceability()
    {
        var packageRoot = FindComplianceContentPackageRoot();
        var matrixPath = Path.Combine(packageRoot, "shared-responsibility-matrix", "baseline.json");
        var matrix = await CreateService().GetPublishedAsync(packageRoot);

        Assert.True(File.Exists(matrixPath));
        Assert.Equal("Published", matrix.State);
        Assert.False(string.IsNullOrWhiteSpace(matrix.SourceReference));
        Assert.False(string.IsNullOrWhiteSpace(matrix.ReviewOwner));
        Assert.NotEqual(default, matrix.EffectiveAt);
        Assert.NotEqual(default, matrix.ReviewedAt);
    }

    private static SharedResponsibilityMatrixService CreateService() =>
        new(new FileSharedResponsibilityMatrixRepository());

    private static string FindComplianceContentPackageRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "Gccs.slnx")))
        {
            current = current.Parent;
        }

        if (current is null)
        {
            throw new DirectoryNotFoundException("Could not locate repository root for compliance content tests.");
        }

        return Path.Combine(current.FullName, "packages", "compliance-content");
    }
}

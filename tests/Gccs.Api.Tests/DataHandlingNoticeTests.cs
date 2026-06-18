using Gccs.Application.Tenancy;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Tenancy;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class DataHandlingNoticeTests
{
    [Fact]
    public async Task TC_1A_6_1_1_Published_notice_exists_for_each_mode()
    {
        var notices = await CreateService().ListPublishedAsync(FindComplianceContentPackageRoot());

        Assert.Contains(notices, notice => notice.Mode == TenantDataPosture.DemoSandbox);
        Assert.Contains(notices, notice => notice.Mode == TenantDataPosture.NoCui);
        Assert.Contains(notices, notice => notice.Mode == TenantDataPosture.CuiReady);
    }

    [Fact]
    public void TC_1A_6_1_2_Notice_publish_metadata_is_required()
    {
        var invalid = new DataHandlingNoticeCatalogDto(
        [
            new DataHandlingNoticeDto(
                NoticeId: "no-cui-invalid",
                Version: "2026.06.phase1a",
                Mode: TenantDataPosture.NoCui,
                WorkflowContexts: ["EvidenceUpload"],
                Title: "No-CUI Data Handling Notice",
                Body: "Real customer CUI upload is prohibited.",
                State: "Published",
                Owner: "",
                Reviewer: "",
                ReviewedAt: default,
                EffectiveAt: default,
                SourceReference: "")
        ]);

        var exception = Assert.Throws<DataHandlingNoticeValidationException>(() =>
            DataHandlingNoticeService.ValidateCatalogForPublish(invalid));

        Assert.Contains(exception.Errors, error => error.Contains("owner", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(exception.Errors, error => error.Contains("reviewer", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(exception.Errors, error => error.Contains("reviewedAt", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(exception.Errors, error => error.Contains("effectiveAt", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task TC_1A_6_1_3_No_cui_notice_prohibits_real_customer_cui_upload()
    {
        var notice = await CreateService().GetPublishedAsync(
            FindComplianceContentPackageRoot(),
            TenantDataPosture.NoCui,
            "EvidenceUpload");

        Assert.NotNull(notice);
        Assert.Contains("Real customer CUI upload is prohibited", notice.Body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TC_1A_6_1_4_Cui_ready_notice_limits_cui_to_approved_workflows_and_customer_responsibilities()
    {
        var notice = await CreateService().GetPublishedAsync(
            FindComplianceContentPackageRoot(),
            TenantDataPosture.CuiReady,
            "EvidenceUpload");

        Assert.NotNull(notice);
        Assert.Contains("approved tenant workflows", notice.Body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Customers remain responsible", notice.Body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TC_1A_6_1_5_Notice_retrieval_matches_mode_and_context()
    {
        var service = CreateService();
        var packageRoot = FindComplianceContentPackageRoot();

        var demo = await service.GetPublishedAsync(packageRoot, TenantDataPosture.DemoSandbox, "Onboarding");
        var noCui = await service.GetPublishedAsync(packageRoot, TenantDataPosture.NoCui, "ContractIntake");
        var cuiReady = await service.GetPublishedAsync(packageRoot, TenantDataPosture.CuiReady, "Support");

        Assert.Equal("demo-sandbox-general", demo?.NoticeId);
        Assert.Equal("no-cui-general", noCui?.NoticeId);
        Assert.Equal("cui-ready-general", cuiReady?.NoticeId);
    }

    private static DataHandlingNoticeService CreateService() =>
        new(new FileDataHandlingNoticeRepository());

    private static string FindComplianceContentPackageRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "Gccs.slnx")))
        {
            current = current.Parent;
        }

        if (current is null)
        {
            throw new DirectoryNotFoundException("Could not locate repository root for data handling notice tests.");
        }

        return Path.Combine(current.FullName, "packages", "compliance-content");
    }
}

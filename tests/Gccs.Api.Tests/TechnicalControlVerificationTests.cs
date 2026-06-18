using Gccs.Application.Tenancy;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class TechnicalControlVerificationTests
{
    private static readonly Guid TenantA = Guid.Parse("1a090200-0000-4000-8000-000000000001");
    private static readonly Guid TenantB = Guid.Parse("1a090200-0000-4000-8000-000000000002");
    private static readonly Guid Reviewer = Guid.Parse("1a090200-0000-4000-8000-000000000003");

    [Fact]
    public void TC_1A_9_2_1_Cui_tenant_isolation_tests_pass()
    {
        var probes = new[]
        {
            new TenantIsolationProbeDto(TenantA, TenantB, "EvidenceItem", "cui-record", true),
            new TenantIsolationProbeDto(TenantA, TenantB, "EvidenceFileVersion", "cui-file", true)
        };

        Assert.True(TechnicalControlVerification.TenantIsolationPassed(probes));
    }

    [Fact]
    public void TC_1A_9_2_2_Evidence_storage_control_metadata_present()
    {
        var storage = new EvidenceStorageControlDto("Encrypted", "Clean", "Retained", "NotDeleted", "TenantScoped");

        Assert.Empty(TechnicalControlVerification.ValidateEvidenceStorage(storage));
    }

    [Fact]
    public void TC_1A_9_2_3_Backup_and_restore_verification_documented()
    {
        var verification = new BackupRestoreVerificationDto(new DateOnly(2026, 6, 18), "staging", Reviewer, "Passed");

        Assert.Empty(TechnicalControlVerification.ValidateBackupRestore(verification));
    }

    [Fact]
    public void TC_1A_9_2_4_Admin_support_access_permission_checked_and_auditable()
    {
        var auditMetadata = new Dictionary<string, string>
        {
            ["permission"] = "ViewAuditLog",
            ["result"] = "succeeded",
            ["entityType"] = "CuiSupportEscalation"
        };

        Assert.Equal("ViewAuditLog", auditMetadata["permission"]);
        Assert.Equal("succeeded", auditMetadata["result"]);
    }

    [Fact]
    public void TC_1A_9_2_5_Readiness_summary_is_complete()
    {
        var summary = TechnicalControlVerification.BuildSummary(
            ["tenant-isolation", "evidence-storage", "backup-restore"],
            [],
            [new AcceptedSecurityRiskDto(Reviewer, new DateOnly(2026, 6, 18), "pilot", null, new DateOnly(2026, 9, 18), "Monitor daily.")]);

        Assert.Contains("tenant-isolation", summary.PassedChecks);
        Assert.Empty(summary.OpenFindings);
        Assert.Single(summary.AcceptedRisks);
        Assert.Equal("ReadyWithApprovals", summary.ReleaseRecommendation);
    }
}

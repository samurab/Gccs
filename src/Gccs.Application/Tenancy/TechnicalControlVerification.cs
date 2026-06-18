namespace Gccs.Application.Tenancy;

public static class TechnicalControlVerification
{
    public static bool TenantIsolationPassed(IReadOnlyList<TenantIsolationProbeDto> probes) =>
        probes.Count > 0 && probes.All(probe => probe.WasBlocked);

    public static IReadOnlyList<string> ValidateEvidenceStorage(EvidenceStorageControlDto storage)
    {
        var errors = new List<string>();
        AddIf(errors, string.IsNullOrWhiteSpace(storage.EncryptionState), "Encryption state is required.");
        AddIf(errors, string.IsNullOrWhiteSpace(storage.ScanState), "Scan state is required.");
        AddIf(errors, string.IsNullOrWhiteSpace(storage.RetentionState), "Retention state is required.");
        AddIf(errors, string.IsNullOrWhiteSpace(storage.DeletionState), "Deletion state is required.");
        AddIf(errors, string.IsNullOrWhiteSpace(storage.AccessControlState), "Storage access-control metadata is required.");
        return errors;
    }

    public static IReadOnlyList<string> ValidateBackupRestore(BackupRestoreVerificationDto verification)
    {
        var errors = new List<string>();
        AddIf(errors, verification.VerifiedAt == default, "Backup restore verification date is required.");
        AddIf(errors, string.IsNullOrWhiteSpace(verification.Environment), "Backup restore environment is required.");
        AddIf(errors, verification.ReviewerUserId == Guid.Empty, "Backup restore reviewer is required.");
        AddIf(errors, string.IsNullOrWhiteSpace(verification.Result), "Backup restore result is required.");
        return errors;
    }

    public static SecurityReadinessSummaryDto BuildSummary(
        IReadOnlyList<string> passedChecks,
        IReadOnlyList<SecurityReviewFindingDto> openFindings,
        IReadOnlyList<AcceptedSecurityRiskDto> acceptedRisks)
    {
        var blocksRelease = SecurityReviewChecklist.BlocksCuiReadyApproval(openFindings);
        return new SecurityReadinessSummaryDto(
            passedChecks,
            openFindings,
            acceptedRisks,
            blocksRelease ? "DoNotRelease" : "ReadyWithApprovals");
    }

    private static void AddIf(ICollection<string> errors, bool condition, string message)
    {
        if (condition)
        {
            errors.Add(message);
        }
    }
}

public sealed record TenantIsolationProbeDto(
    Guid SourceTenantId,
    Guid TargetTenantId,
    string EntityType,
    string EntityId,
    bool WasBlocked);

public sealed record EvidenceStorageControlDto(
    string EncryptionState,
    string ScanState,
    string RetentionState,
    string DeletionState,
    string AccessControlState);

public sealed record BackupRestoreVerificationDto(
    DateOnly VerifiedAt,
    string Environment,
    Guid ReviewerUserId,
    string Result);

public sealed record SecurityReadinessSummaryDto(
    IReadOnlyList<string> PassedChecks,
    IReadOnlyList<SecurityReviewFindingDto> OpenFindings,
    IReadOnlyList<AcceptedSecurityRiskDto> AcceptedRisks,
    string ReleaseRecommendation);

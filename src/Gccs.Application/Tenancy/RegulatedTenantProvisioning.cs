using Gccs.Application.Audit;
using Gccs.Application.Security;
using Gccs.Domain.Audit;
using Gccs.Domain.Tenancy;

namespace Gccs.Application.Tenancy;

public sealed class RegulatedTenantProvisioningService(
    IRegulatedTenantProvisioningRepository repository,
    ICurrentTenantContext tenantContext,
    IAuditEventWriter auditEventWriter)
{
    private static readonly RegulatedProvisioningApprovalArea[] RequiredApprovals =
    [
        RegulatedProvisioningApprovalArea.Security,
        RegulatedProvisioningApprovalArea.Engineering,
        RegulatedProvisioningApprovalArea.CustomerSuccess,
        RegulatedProvisioningApprovalArea.LegalCompliance,
        RegulatedProvisioningApprovalArea.Product
    ];

    private static readonly RegulatedProvisioningChecklistItem[] RequiredChecklistItems =
    [
        RegulatedProvisioningChecklistItem.TenantIsolation,
        RegulatedProvisioningChecklistItem.Storage,
        RegulatedProvisioningChecklistItem.Encryption,
        RegulatedProvisioningChecklistItem.Logging,
        RegulatedProvisioningChecklistItem.Monitoring,
        RegulatedProvisioningChecklistItem.Backup,
        RegulatedProvisioningChecklistItem.Restore,
        RegulatedProvisioningChecklistItem.AccessPolicy,
        RegulatedProvisioningChecklistItem.SupportAccess
    ];

    public Task<IReadOnlyList<RegulatedTenantProvisioningRequestDto>> ListAsync(CancellationToken cancellationToken = default) =>
        repository.ListCurrentTenantAsync(cancellationToken);

    public async Task<RegulatedTenantProvisioningRequestDto> CreateAsync(
        CreateRegulatedTenantProvisioningRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateCreateRequest(request);
        var environment = await repository.GetEnvironmentForCurrentTenantAsync(request.EnvironmentId, cancellationToken);
        if (environment is null || environment.Status is not EnvironmentReadinessStatus.Approved and not EnvironmentReadinessStatus.Deployed)
        {
            throw new RegulatedTenantProvisioningValidationException("Regulated tenant provisioning requires an approved target environment.");
        }

        var created = await repository.CreateForCurrentTenantAsync(request, actorUserId, cancellationToken);
        await WriteAuditAsync(created, actorUserId, AuditAction.Created, "Regulated tenant provisioning request was created.", cancellationToken);
        return created;
    }

    public async Task<RegulatedTenantProvisioningRequestDto?> ApproveAsync(
        Guid requestId,
        RegulatedProvisioningApprovalRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateReviewMetadata(request.ApproverName, request.Notes);
        var updated = await repository.MarkApprovalCompleteAsync(requestId, request.Area, request.ApproverName.Trim(), request.Notes.Trim(), actorUserId, cancellationToken);
        if (updated is not null)
        {
            await WriteAuditAsync(updated, actorUserId, AuditAction.Approved, $"Provisioning approval '{request.Area}' was completed.", cancellationToken);
        }

        return updated;
    }

    public async Task<RegulatedTenantProvisioningRequestDto?> CompleteChecklistItemAsync(
        Guid requestId,
        RegulatedProvisioningChecklistRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateReviewMetadata(request.CompletedByName, request.EvidenceReference);
        var updated = await repository.MarkChecklistItemCompleteAsync(requestId, request.Item, request.CompletedByName.Trim(), request.EvidenceReference.Trim(), actorUserId, cancellationToken);
        if (updated is not null)
        {
            await WriteAuditAsync(updated, actorUserId, AuditAction.Updated, $"Provisioning checklist item '{request.Item}' was completed.", cancellationToken);
        }

        return updated;
    }

    public async Task<RegulatedTenantProvisioningRequestDto?> StartProvisioningAsync(
        Guid requestId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var provisioning = await repository.GetCurrentTenantAsync(requestId, cancellationToken);
        if (provisioning is null)
        {
            return null;
        }

        var missingApprovals = RequiredApprovals.Except(provisioning.CompletedApprovals).ToArray();
        var missingChecklist = RequiredChecklistItems.Except(provisioning.CompletedChecklistItems).ToArray();
        if (missingApprovals.Length > 0 || missingChecklist.Length > 0)
        {
            await WriteAuditAsync(provisioning, actorUserId, AuditAction.Rejected, "Provisioning start was blocked by incomplete approvals or checklist items.", cancellationToken);
            throw new RegulatedTenantProvisioningValidationException("Provisioning cannot start until all required approvals and checklist items are complete.");
        }

        var started = await repository.UpdateStatusAsync(requestId, RegulatedProvisioningStatus.Provisioning, actorUserId, "Provisioning started.", cancellationToken);
        if (started is not null)
        {
            await WriteAuditAsync(started, actorUserId, AuditAction.Updated, "Regulated tenant provisioning started.", cancellationToken);
        }

        return started;
    }

    public async Task<RegulatedTenantProvisioningRequestDto?> CompleteProvisioningAsync(
        Guid requestId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var provisioning = await repository.GetCurrentTenantAsync(requestId, cancellationToken);
        if (provisioning is null)
        {
            return null;
        }

        if (provisioning.Status is not RegulatedProvisioningStatus.Provisioning and not RegulatedProvisioningStatus.Validation)
        {
            throw new RegulatedTenantProvisioningValidationException("Provisioning must be in progress before creating the regulated tenant.");
        }

        var environment = await repository.GetEnvironmentForCurrentTenantAsync(provisioning.EnvironmentId, cancellationToken);
        if (environment is null || environment.Status is not EnvironmentReadinessStatus.Approved and not EnvironmentReadinessStatus.Deployed)
        {
            throw new RegulatedTenantProvisioningValidationException("Regulated tenant can only be created in an approved target environment.");
        }

        var completed = await repository.CreateTenantAndMarkReadyAsync(requestId, actorUserId, cancellationToken);
        if (completed is not null)
        {
            await WriteAuditAsync(completed, actorUserId, AuditAction.Created, "Regulated tenant was provisioned in the approved target environment.", cancellationToken);
        }

        return completed;
    }

    public Task<RegulatedTenantProvisioningRequestDto?> MarkValidationAsync(Guid requestId, Guid actorUserId, CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(requestId, RegulatedProvisioningStatus.Validation, actorUserId, "Provisioning moved to validation.", AuditAction.Updated, cancellationToken);

    public Task<RegulatedTenantProvisioningRequestDto?> SuspendAsync(Guid requestId, Guid actorUserId, CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(requestId, RegulatedProvisioningStatus.Suspended, actorUserId, "Provisioning request was suspended.", AuditAction.Updated, cancellationToken);

    public Task<RegulatedTenantProvisioningRequestDto?> RetireAsync(Guid requestId, Guid actorUserId, CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(requestId, RegulatedProvisioningStatus.Retired, actorUserId, "Provisioning request was retired.", AuditAction.Archived, cancellationToken);

    public async Task<RegulatedTenantProvisioningRequestDto?> RecordFailureAsync(
        Guid requestId,
        RegulatedProvisioningFailureRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateReviewMetadata(request.Owner, request.FailureReason);
        var updated = await repository.RecordFailureAsync(requestId, request.FailureReason.Trim(), request.RollbackDecision.Trim(), request.Owner.Trim(), actorUserId, cancellationToken);
        if (updated is not null)
        {
            await WriteAuditAsync(updated, actorUserId, AuditAction.Rejected, "Regulated tenant provisioning failed.", cancellationToken);
        }

        return updated;
    }

    private async Task<RegulatedTenantProvisioningRequestDto?> ChangeStatusAsync(
        Guid requestId,
        RegulatedProvisioningStatus status,
        Guid actorUserId,
        string summary,
        AuditAction action,
        CancellationToken cancellationToken)
    {
        var updated = await repository.UpdateStatusAsync(requestId, status, actorUserId, summary, cancellationToken);
        if (updated is not null)
        {
            await WriteAuditAsync(updated, actorUserId, action, summary, cancellationToken);
        }

        return updated;
    }

    private static void ValidateCreateRequest(CreateRegulatedTenantProvisioningRequest request)
    {
        NormalizeRequired(request.TenantName, nameof(request.TenantName), 240);
        NormalizeRequired(request.CustomerType, nameof(request.CustomerType), 160);
        NormalizeRequired(request.KeyPolicy, nameof(request.KeyPolicy), 240);
        NormalizeRequired(request.SupportModel, nameof(request.SupportModel), 240);
        NormalizeRequired(request.MigrationSource, nameof(request.MigrationSource), 240);
        if (request.EnvironmentId == Guid.Empty)
        {
            throw new RegulatedTenantProvisioningValidationException("Target environment is required.");
        }
    }

    private static void ValidateReviewMetadata(string reviewerName, string notes)
    {
        NormalizeRequired(reviewerName, nameof(reviewerName), 200);
        NormalizeRequired(notes, nameof(notes), 1200);
    }

    private static string NormalizeRequired(string? value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new RegulatedTenantProvisioningValidationException($"{fieldName} is required.");
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new RegulatedTenantProvisioningValidationException($"{fieldName} must be {maxLength} characters or fewer.");
        }

        return normalized;
    }

    private Task WriteAuditAsync(
        RegulatedTenantProvisioningRequestDto request,
        Guid actorUserId,
        AuditAction action,
        string summary,
        CancellationToken cancellationToken) =>
        auditEventWriter.WriteAsync(
            tenantContext.TenantId,
            actorUserId,
            action,
            "RegulatedTenantProvisioningRequest",
            request.Id.ToString(),
            summary,
            new Dictionary<string, string>
            {
                ["status"] = request.Status.ToString(),
                ["environmentId"] = request.EnvironmentId.ToString(),
                ["targetTenantId"] = request.ProvisionedTenantId?.ToString() ?? string.Empty
            },
            cancellationToken);
}

public interface IRegulatedTenantProvisioningRepository
{
    Task<IReadOnlyList<RegulatedTenantProvisioningRequestDto>> ListCurrentTenantAsync(CancellationToken cancellationToken = default);
    Task<RegulatedTenantProvisioningRequestDto?> GetCurrentTenantAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task<GovernmentCloudEnvironmentDto?> GetEnvironmentForCurrentTenantAsync(Guid environmentId, CancellationToken cancellationToken = default);
    Task<RegulatedTenantProvisioningRequestDto> CreateForCurrentTenantAsync(CreateRegulatedTenantProvisioningRequest request, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<RegulatedTenantProvisioningRequestDto?> MarkApprovalCompleteAsync(Guid requestId, RegulatedProvisioningApprovalArea area, string approverName, string notes, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<RegulatedTenantProvisioningRequestDto?> MarkChecklistItemCompleteAsync(Guid requestId, RegulatedProvisioningChecklistItem item, string completedByName, string evidenceReference, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<RegulatedTenantProvisioningRequestDto?> UpdateStatusAsync(Guid requestId, RegulatedProvisioningStatus status, Guid actorUserId, string historyNote, CancellationToken cancellationToken = default);
    Task<RegulatedTenantProvisioningRequestDto?> CreateTenantAndMarkReadyAsync(Guid requestId, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<RegulatedTenantProvisioningRequestDto?> RecordFailureAsync(Guid requestId, string failureReason, string rollbackDecision, string owner, Guid actorUserId, CancellationToken cancellationToken = default);
}

public sealed record CreateRegulatedTenantProvisioningRequest(
    string TenantName,
    string CustomerType,
    Guid EnvironmentId,
    TenantDataPosture DataHandlingMode,
    bool CuiApprovalComplete,
    string KeyPolicy,
    string SupportModel,
    string MigrationSource);

public sealed record RegulatedProvisioningApprovalRequest(RegulatedProvisioningApprovalArea Area, string ApproverName, string Notes);
public sealed record RegulatedProvisioningChecklistRequest(RegulatedProvisioningChecklistItem Item, string CompletedByName, string EvidenceReference);
public sealed record RegulatedProvisioningFailureRequest(string FailureReason, string RollbackDecision, string Owner);

public sealed record RegulatedTenantProvisioningRequestDto(
    Guid Id,
    Guid TenantId,
    string TenantName,
    string CustomerType,
    Guid EnvironmentId,
    TenantDataPosture DataHandlingMode,
    bool CuiApprovalComplete,
    string KeyPolicy,
    string SupportModel,
    string MigrationSource,
    RegulatedProvisioningStatus Status,
    Guid? ProvisionedTenantId,
    string? FailureReason,
    string? RollbackDecision,
    string? FailureOwner,
    RegulatedProvisioningApprovalArea[] CompletedApprovals,
    RegulatedProvisioningChecklistItem[] CompletedChecklistItems,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public enum RegulatedProvisioningStatus
{
    Requested,
    Approved,
    Provisioning,
    Validation,
    Ready,
    Failed,
    Suspended,
    Retired
}

public enum RegulatedProvisioningApprovalArea
{
    Security,
    Engineering,
    CustomerSuccess,
    LegalCompliance,
    Product
}

public enum RegulatedProvisioningChecklistItem
{
    TenantIsolation,
    Storage,
    Encryption,
    Logging,
    Monitoring,
    Backup,
    Restore,
    AccessPolicy,
    SupportAccess
}

public sealed class RegulatedTenantProvisioningValidationException(string message) : InvalidOperationException(message);

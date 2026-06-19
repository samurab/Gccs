using Gccs.Application.Audit;
using Gccs.Application.Security;
using Gccs.Domain.Audit;

namespace Gccs.Application.Tenancy;

public sealed class GovernmentCloudEnvironmentService(
    IGovernmentCloudEnvironmentRepository repository,
    ICurrentTenantContext tenantContext,
    IAuditEventWriter auditEventWriter)
{
    private static readonly HashSet<string> GovernmentRegionAllowlist = new(StringComparer.OrdinalIgnoreCase)
    {
        "us-gov-west-1",
        "us-gov-east-1",
        "usgovvirginia",
        "usgovarizona",
        "usgovtexas"
    };

    public Task<IReadOnlyList<GovernmentCloudEnvironmentDto>> ListAsync(CancellationToken cancellationToken = default) =>
        repository.ListCurrentTenantAsync(cancellationToken);

    public async Task<GovernmentCloudEnvironmentDto> CreateAsync(
        UpsertGovernmentCloudEnvironmentRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateRequestShape(request);
        var now = DateTimeOffset.UtcNow;
        var environment = new GovernmentCloudEnvironmentModel(
            Guid.NewGuid(),
            tenantContext.TenantId,
            request.Name.Trim(),
            request.EnvironmentType,
            request.Region.Trim(),
            request.Boundary.Trim(),
            request.NetworkSegment.Trim(),
            request.StorageAccount.Trim(),
            request.DatabaseService.Trim(),
            request.KeyManagementService.Trim(),
            request.LoggingWorkspace.Trim(),
            request.BackupPolicy.Trim(),
            request.PrivateNetworkingEnabled,
            request.StorageEncryptionEnabled,
            request.DatabaseEncryptionEnabled,
            request.CustomerManagedKeysEnabled,
            request.AuditLoggingEnabled,
            request.ImmutableLoggingEnabled,
            request.BackupEnabled,
            request.RestoreTested,
            EnvironmentReadinessStatus.Draft,
            null,
            null,
            null,
            now,
            actorUserId,
            null,
            null);

        var created = await repository.AddToCurrentTenantAsync(environment, actorUserId, "Environment created.", cancellationToken);
        await WriteAuditAsync(created, actorUserId, AuditAction.Created, "Environment configuration was created.", cancellationToken);
        return created;
    }

    public async Task<GovernmentCloudEnvironmentDto?> UpdateAsync(
        Guid environmentId,
        UpsertGovernmentCloudEnvironmentRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateRequestShape(request);
        var updated = await repository.UpdateInCurrentTenantAsync(environmentId, request, actorUserId, "Environment configuration was updated.", cancellationToken);
        if (updated is not null)
        {
            await WriteAuditAsync(updated, actorUserId, AuditAction.Updated, "Environment configuration was updated.", cancellationToken);
        }

        return updated;
    }

    public Task<GovernmentCloudEnvironmentDto?> SubmitForReviewAsync(
        Guid environmentId,
        ReviewGovernmentCloudEnvironmentRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(environmentId, EnvironmentReadinessStatus.UnderReview, request, actorUserId, "Environment was submitted for review.", AuditAction.Updated, cancellationToken);

    public async Task<GovernmentCloudEnvironmentDto?> ApproveAsync(
        Guid environmentId,
        ReviewGovernmentCloudEnvironmentRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var environment = await repository.GetCurrentTenantAsync(environmentId, cancellationToken);
        if (environment is null)
        {
            return null;
        }

        ValidateApproval(environment, request);
        return await ChangeStatusAsync(environmentId, EnvironmentReadinessStatus.Approved, request, actorUserId, "Environment was approved for regulated tenant deployment.", AuditAction.Approved, cancellationToken);
    }

    public Task<GovernmentCloudEnvironmentDto?> BlockAsync(
        Guid environmentId,
        ReviewGovernmentCloudEnvironmentRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(environmentId, EnvironmentReadinessStatus.Blocked, request, actorUserId, "Environment was blocked.", AuditAction.Rejected, cancellationToken);

    public Task<GovernmentCloudEnvironmentDto?> MarkDeployedAsync(
        Guid environmentId,
        ReviewGovernmentCloudEnvironmentRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(environmentId, EnvironmentReadinessStatus.Deployed, request, actorUserId, "Environment was marked deployed.", AuditAction.Updated, cancellationToken);

    public Task<GovernmentCloudEnvironmentDto?> RetireAsync(
        Guid environmentId,
        ReviewGovernmentCloudEnvironmentRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(environmentId, EnvironmentReadinessStatus.Retired, request, actorUserId, "Environment was retired.", AuditAction.Archived, cancellationToken);

    public async Task<RegulatedEnvironmentSelectionResult?> SelectForRegulatedTenantDeploymentAsync(
        Guid environmentId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var environment = await repository.GetCurrentTenantAsync(environmentId, cancellationToken);
        if (environment is null)
        {
            return null;
        }

        if (environment.Status is not EnvironmentReadinessStatus.Approved and not EnvironmentReadinessStatus.Deployed)
        {
            await WriteAuditAsync(environment, actorUserId, AuditAction.Rejected, "Environment selection was denied because it is not approved.", cancellationToken);
            return new RegulatedEnvironmentSelectionResult(false, "environment_not_approved", "Only approved or deployed environments can be selected for regulated tenant deployment.", environment.Id, environment.Status);
        }

        await WriteAuditAsync(environment, actorUserId, AuditAction.Viewed, "Environment was selected for regulated tenant deployment.", cancellationToken);
        return new RegulatedEnvironmentSelectionResult(true, "environment_selected", "Environment can be selected for regulated tenant deployment.", environment.Id, environment.Status);
    }

    private async Task<GovernmentCloudEnvironmentDto?> ChangeStatusAsync(
        Guid environmentId,
        EnvironmentReadinessStatus status,
        ReviewGovernmentCloudEnvironmentRequest request,
        Guid actorUserId,
        string historyNote,
        AuditAction auditAction,
        CancellationToken cancellationToken)
    {
        ValidateReviewMetadata(request);
        var updated = await repository.UpdateStatusInCurrentTenantAsync(
            environmentId,
            status,
            NormalizeRequired(request.ReviewerName, nameof(request.ReviewerName), 200),
            NormalizeRequired(request.ReviewNotes, nameof(request.ReviewNotes), 1200),
            actorUserId,
            historyNote,
            cancellationToken);

        if (updated is not null)
        {
            await WriteAuditAsync(updated, actorUserId, auditAction, historyNote, cancellationToken);
        }

        return updated;
    }

    private static void ValidateApproval(GovernmentCloudEnvironmentDto environment, ReviewGovernmentCloudEnvironmentRequest request)
    {
        ValidateReviewMetadata(request);
        List<string> errors = [];
        if (environment.EnvironmentType is EnvironmentDeploymentType.GovCloud or EnvironmentDeploymentType.GovernmentCloud)
        {
            if (!GovernmentRegionAllowlist.Contains(environment.Region))
            {
                errors.Add("Government cloud region is not allowlisted.");
            }

            if (!environment.PrivateNetworkingEnabled)
            {
                errors.Add("Private networking is required.");
            }

            if (!environment.StorageEncryptionEnabled || !environment.DatabaseEncryptionEnabled || !environment.CustomerManagedKeysEnabled)
            {
                errors.Add("Storage, database, and customer-managed key encryption controls are required.");
            }

            if (!environment.AuditLoggingEnabled || !environment.ImmutableLoggingEnabled)
            {
                errors.Add("Audit and immutable logging controls are required.");
            }

            if (!environment.BackupEnabled || !environment.RestoreTested)
            {
                errors.Add("Backup policy and restore testing are required.");
            }
        }

        if (errors.Count > 0)
        {
            throw new GovernmentCloudEnvironmentValidationException(string.Join(" ", errors));
        }
    }

    private static void ValidateRequestShape(UpsertGovernmentCloudEnvironmentRequest request)
    {
        NormalizeRequired(request.Name, nameof(request.Name), 200);
        NormalizeRequired(request.Region, nameof(request.Region), 80);
        NormalizeRequired(request.Boundary, nameof(request.Boundary), 200);
        NormalizeRequired(request.NetworkSegment, nameof(request.NetworkSegment), 200);
        NormalizeRequired(request.StorageAccount, nameof(request.StorageAccount), 200);
        NormalizeRequired(request.DatabaseService, nameof(request.DatabaseService), 200);
        NormalizeRequired(request.KeyManagementService, nameof(request.KeyManagementService), 200);
        NormalizeRequired(request.LoggingWorkspace, nameof(request.LoggingWorkspace), 200);
        NormalizeRequired(request.BackupPolicy, nameof(request.BackupPolicy), 200);
    }

    private static void ValidateReviewMetadata(ReviewGovernmentCloudEnvironmentRequest request)
    {
        NormalizeRequired(request.ReviewerName, nameof(request.ReviewerName), 200);
        NormalizeRequired(request.ReviewNotes, nameof(request.ReviewNotes), 1200);
    }

    private static string NormalizeRequired(string? value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new GovernmentCloudEnvironmentValidationException($"{fieldName} is required.");
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new GovernmentCloudEnvironmentValidationException($"{fieldName} must be {maxLength} characters or fewer.");
        }

        return normalized;
    }

    private Task WriteAuditAsync(
        GovernmentCloudEnvironmentDto environment,
        Guid actorUserId,
        AuditAction action,
        string summary,
        CancellationToken cancellationToken) =>
        auditEventWriter.WriteAsync(
            tenantContext.TenantId,
            actorUserId,
            action,
            "GovernmentCloudEnvironment",
            environment.Id.ToString(),
            summary,
            new Dictionary<string, string>
            {
                ["environmentType"] = environment.EnvironmentType.ToString(),
                ["status"] = environment.Status.ToString(),
                ["region"] = environment.Region
            },
            cancellationToken);
}

public interface IGovernmentCloudEnvironmentRepository
{
    Task<IReadOnlyList<GovernmentCloudEnvironmentDto>> ListCurrentTenantAsync(CancellationToken cancellationToken = default);
    Task<GovernmentCloudEnvironmentDto?> GetCurrentTenantAsync(Guid environmentId, CancellationToken cancellationToken = default);
    Task<GovernmentCloudEnvironmentDto> AddToCurrentTenantAsync(GovernmentCloudEnvironmentModel environment, Guid actorUserId, string historyNote, CancellationToken cancellationToken = default);
    Task<GovernmentCloudEnvironmentDto?> UpdateInCurrentTenantAsync(Guid environmentId, UpsertGovernmentCloudEnvironmentRequest request, Guid actorUserId, string historyNote, CancellationToken cancellationToken = default);
    Task<GovernmentCloudEnvironmentDto?> UpdateStatusInCurrentTenantAsync(Guid environmentId, EnvironmentReadinessStatus status, string reviewerName, string reviewNotes, Guid actorUserId, string historyNote, CancellationToken cancellationToken = default);
}

public sealed record GovernmentCloudEnvironmentModel(
    Guid Id,
    Guid TenantId,
    string Name,
    EnvironmentDeploymentType EnvironmentType,
    string Region,
    string Boundary,
    string NetworkSegment,
    string StorageAccount,
    string DatabaseService,
    string KeyManagementService,
    string LoggingWorkspace,
    string BackupPolicy,
    bool PrivateNetworkingEnabled,
    bool StorageEncryptionEnabled,
    bool DatabaseEncryptionEnabled,
    bool CustomerManagedKeysEnabled,
    bool AuditLoggingEnabled,
    bool ImmutableLoggingEnabled,
    bool BackupEnabled,
    bool RestoreTested,
    EnvironmentReadinessStatus Status,
    string? ReviewerName,
    string? ReviewNotes,
    DateTimeOffset? ReviewedAt,
    DateTimeOffset CreatedAt,
    Guid CreatedByUserId,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedByUserId);

public sealed record GovernmentCloudEnvironmentDto(
    Guid Id,
    Guid TenantId,
    string Name,
    EnvironmentDeploymentType EnvironmentType,
    string Region,
    string Boundary,
    string NetworkSegment,
    string StorageAccount,
    string DatabaseService,
    string KeyManagementService,
    string LoggingWorkspace,
    string BackupPolicy,
    bool PrivateNetworkingEnabled,
    bool StorageEncryptionEnabled,
    bool DatabaseEncryptionEnabled,
    bool CustomerManagedKeysEnabled,
    bool AuditLoggingEnabled,
    bool ImmutableLoggingEnabled,
    bool BackupEnabled,
    bool RestoreTested,
    EnvironmentReadinessStatus Status,
    string? ReviewerName,
    string? ReviewNotes,
    DateTimeOffset? ReviewedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record UpsertGovernmentCloudEnvironmentRequest(
    string Name,
    EnvironmentDeploymentType EnvironmentType,
    string Region,
    string Boundary,
    string NetworkSegment,
    string StorageAccount,
    string DatabaseService,
    string KeyManagementService,
    string LoggingWorkspace,
    string BackupPolicy,
    bool PrivateNetworkingEnabled,
    bool StorageEncryptionEnabled,
    bool DatabaseEncryptionEnabled,
    bool CustomerManagedKeysEnabled,
    bool AuditLoggingEnabled,
    bool ImmutableLoggingEnabled,
    bool BackupEnabled,
    bool RestoreTested);

public sealed record ReviewGovernmentCloudEnvironmentRequest(string ReviewerName, string ReviewNotes);
public sealed record RegulatedEnvironmentSelectionResult(bool Allowed, string ReasonCode, string Message, Guid EnvironmentId, EnvironmentReadinessStatus Status);

public enum EnvironmentDeploymentType
{
    Commercial,
    Staging,
    GovCloud,
    GovernmentCloud
}

public enum EnvironmentReadinessStatus
{
    Draft,
    UnderReview,
    Approved,
    Blocked,
    Deployed,
    Retired
}

public sealed class GovernmentCloudEnvironmentValidationException(string message) : InvalidOperationException(message);

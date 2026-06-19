using Gccs.Application.Audit;
using Gccs.Application.Security;
using Gccs.Domain.Audit;

namespace Gccs.Application.Tenancy;

public sealed class GovernmentCloudReleaseReadinessService(
    IGovernmentCloudReleaseReadinessRepository repository,
    ICurrentTenantContext tenantContext,
    IAuditEventWriter auditEventWriter)
{
    private static readonly GovernmentCloudReleaseChecklistItem[] RequiredChecklist =
    [
        GovernmentCloudReleaseChecklistItem.Migrations,
        GovernmentCloudReleaseChecklistItem.SmokeTests,
        GovernmentCloudReleaseChecklistItem.SecurityScans,
        GovernmentCloudReleaseChecklistItem.DependencyReview,
        GovernmentCloudReleaseChecklistItem.Backup,
        GovernmentCloudReleaseChecklistItem.Restore,
        GovernmentCloudReleaseChecklistItem.Monitoring,
        GovernmentCloudReleaseChecklistItem.IncidentResponse,
        GovernmentCloudReleaseChecklistItem.SupportCoverage,
        GovernmentCloudReleaseChecklistItem.RollbackPlan
    ];

    public async Task<GovernmentCloudReleaseReadinessDto> CreateAsync(
        CreateGovernmentCloudReleaseReadinessRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateCreate(request);
        var environment = await repository.GetEnvironmentAsync(request.EnvironmentId, cancellationToken);
        if (environment is null || environment.Status is not EnvironmentReadinessStatus.Approved and not EnvironmentReadinessStatus.Deployed)
        {
            throw new GovernmentCloudReleaseReadinessValidationException("Release readiness requires an approved government cloud environment.");
        }

        var created = await repository.CreateAsync(request, actorUserId, cancellationToken);
        await WriteAuditAsync(created, actorUserId, AuditAction.Created, "Government cloud release readiness record was created.", cancellationToken);
        return created;
    }

    public async Task<GovernmentCloudReleaseReadinessDto?> CompleteChecklistAsync(
        Guid readinessId,
        CompleteGovernmentCloudReleaseChecklistRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateText(request.EvidenceReference, nameof(request.EvidenceReference), 600);
        var updated = await repository.CompleteChecklistAsync(readinessId, request.Item, request.EvidenceReference.Trim(), actorUserId, cancellationToken);
        if (updated is not null)
        {
            await WriteAuditAsync(updated, actorUserId, AuditAction.Updated, $"Release checklist item '{request.Item}' was completed.", cancellationToken);
        }

        return updated;
    }

    public async Task<GovernmentCloudReleaseReadinessDto?> LinkEvidenceAsync(
        Guid readinessId,
        GovernmentCloudReleaseEvidenceRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateText(request.Link, nameof(request.Link), 600);
        var updated = await repository.LinkEvidenceAsync(readinessId, request.EvidenceType, request.Link.Trim(), actorUserId, cancellationToken);
        if (updated is not null)
        {
            await WriteAuditAsync(updated, actorUserId, AuditAction.Updated, $"Release evidence '{request.EvidenceType}' was linked.", cancellationToken);
        }

        return updated;
    }

    public async Task<GovernmentCloudReleaseReadinessDto?> AddGapAsync(
        Guid readinessId,
        GovernmentCloudReleaseGapRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateText(request.Description, nameof(request.Description), 1000);
        var updated = await repository.AddGapAsync(readinessId, request.Area, request.Severity, request.Description.Trim(), actorUserId, cancellationToken);
        if (updated is not null)
        {
            await WriteAuditAsync(updated, actorUserId, AuditAction.Updated, $"Release gap '{request.Area}' was recorded.", cancellationToken);
        }

        return updated;
    }

    public async Task<GovernmentCloudReleaseReadinessDto?> ApproveAsync(
        Guid readinessId,
        GovernmentCloudReleaseApprovalRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateText(request.ApproverName, nameof(request.ApproverName), 200);
        ValidateText(request.ApprovalNotes, nameof(request.ApprovalNotes), 1200);
        var readiness = await repository.GetAsync(readinessId, cancellationToken);
        if (readiness is null)
        {
            return null;
        }

        var missing = RequiredChecklist.Except(readiness.CompletedChecklist).ToArray();
        if (missing.Length > 0)
        {
            await WriteAuditAsync(readiness, actorUserId, AuditAction.Rejected, "Government cloud release approval was blocked by incomplete readiness checklist.", cancellationToken);
            throw new GovernmentCloudReleaseReadinessValidationException("Release approval requires all readiness checklist items to be complete.");
        }

        if (readiness.OpenCriticalGaps.Length > 0)
        {
            await WriteAuditAsync(readiness, actorUserId, AuditAction.Rejected, "Government cloud release approval was blocked by open critical gaps.", cancellationToken);
            throw new GovernmentCloudReleaseReadinessValidationException("Open critical security, migration, backup, restore, or incident response gaps block release approval.");
        }

        if (!RequiredEvidencePresent(readiness))
        {
            throw new GovernmentCloudReleaseReadinessValidationException("Runbook, alert routing, access review, vulnerability scan, backup restore, and incident drill evidence are required.");
        }

        var approved = await repository.ApproveAsync(readinessId, request.ApproverName.Trim(), request.ApprovalNotes.Trim(), actorUserId, cancellationToken);
        if (approved is not null)
        {
            await WriteAuditAsync(approved, actorUserId, AuditAction.Approved, "Government cloud release was approved.", cancellationToken);
        }

        return approved;
    }

    public async Task<GovernmentCloudReleaseReadinessDto?> DeployAsync(
        Guid readinessId,
        GovernmentCloudReleaseDeploymentRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateText(request.Result, nameof(request.Result), 400);
        ValidateText(request.RollbackStatus, nameof(request.RollbackStatus), 400);
        var readiness = await repository.GetAsync(readinessId, cancellationToken);
        if (readiness is null)
        {
            return null;
        }

        if (readiness.Status is not GovernmentCloudReleaseStatus.Approved)
        {
            throw new GovernmentCloudReleaseReadinessValidationException("Only approved government cloud releases can be deployed.");
        }

        var deployed = await repository.DeployAsync(readinessId, request.Result.Trim(), request.RollbackStatus.Trim(), actorUserId, cancellationToken);
        if (deployed is not null)
        {
            await WriteAuditAsync(deployed, actorUserId, AuditAction.Updated, "Government cloud release was deployed.", cancellationToken);
        }

        return deployed;
    }

    private static bool RequiredEvidencePresent(GovernmentCloudReleaseReadinessDto readiness)
    {
        var required = Enum.GetValues<GovernmentCloudReleaseEvidenceType>();
        return required.All(item => readiness.EvidenceLinks.Any(link => link.EvidenceType == item));
    }

    private static void ValidateCreate(CreateGovernmentCloudReleaseReadinessRequest request)
    {
        if (request.EnvironmentId == Guid.Empty)
        {
            throw new GovernmentCloudReleaseReadinessValidationException("Environment ID is required.");
        }

        ValidateText(request.Version, nameof(request.Version), 120);
        ValidateText(request.ReleaseWindow, nameof(request.ReleaseWindow), 240);
        ValidateText(request.Owner, nameof(request.Owner), 200);
    }

    private static void ValidateText(string? value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new GovernmentCloudReleaseReadinessValidationException($"{fieldName} is required.");
        }

        if (value.Trim().Length > maxLength)
        {
            throw new GovernmentCloudReleaseReadinessValidationException($"{fieldName} must be {maxLength} characters or fewer.");
        }
    }

    private Task WriteAuditAsync(GovernmentCloudReleaseReadinessDto readiness, Guid actorUserId, AuditAction action, string summary, CancellationToken cancellationToken) =>
        auditEventWriter.WriteAsync(
            tenantContext.TenantId,
            actorUserId,
            action,
            "GovernmentCloudReleaseReadiness",
            readiness.Id.ToString(),
            summary,
            new Dictionary<string, string>
            {
                ["environmentId"] = readiness.EnvironmentId.ToString(),
                ["version"] = readiness.Version,
                ["status"] = readiness.Status.ToString()
            },
            cancellationToken);
}

public interface IGovernmentCloudReleaseReadinessRepository
{
    Task<GovernmentCloudEnvironmentDto?> GetEnvironmentAsync(Guid environmentId, CancellationToken cancellationToken = default);
    Task<GovernmentCloudReleaseReadinessDto?> GetAsync(Guid readinessId, CancellationToken cancellationToken = default);
    Task<GovernmentCloudReleaseReadinessDto> CreateAsync(CreateGovernmentCloudReleaseReadinessRequest request, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<GovernmentCloudReleaseReadinessDto?> CompleteChecklistAsync(Guid readinessId, GovernmentCloudReleaseChecklistItem item, string evidenceReference, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<GovernmentCloudReleaseReadinessDto?> LinkEvidenceAsync(Guid readinessId, GovernmentCloudReleaseEvidenceType evidenceType, string link, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<GovernmentCloudReleaseReadinessDto?> AddGapAsync(Guid readinessId, GovernmentCloudReleaseGapArea area, GovernmentCloudReleaseGapSeverity severity, string description, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<GovernmentCloudReleaseReadinessDto?> ApproveAsync(Guid readinessId, string approverName, string approvalNotes, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<GovernmentCloudReleaseReadinessDto?> DeployAsync(Guid readinessId, string result, string rollbackStatus, Guid actorUserId, CancellationToken cancellationToken = default);
}

public sealed record CreateGovernmentCloudReleaseReadinessRequest(Guid EnvironmentId, string Version, string ReleaseWindow, string Owner);
public sealed record CompleteGovernmentCloudReleaseChecklistRequest(GovernmentCloudReleaseChecklistItem Item, string EvidenceReference);
public sealed record GovernmentCloudReleaseEvidenceRequest(GovernmentCloudReleaseEvidenceType EvidenceType, string Link);
public sealed record GovernmentCloudReleaseGapRequest(GovernmentCloudReleaseGapArea Area, GovernmentCloudReleaseGapSeverity Severity, string Description);
public sealed record GovernmentCloudReleaseApprovalRequest(string ApproverName, string ApprovalNotes);
public sealed record GovernmentCloudReleaseDeploymentRequest(string Result, string RollbackStatus);

public sealed record GovernmentCloudReleaseReadinessDto(
    Guid Id,
    Guid TenantId,
    Guid EnvironmentId,
    string Version,
    string ReleaseWindow,
    string Owner,
    GovernmentCloudReleaseStatus Status,
    string? ApproverName,
    DateTimeOffset? ApprovedAt,
    string? Result,
    string? RollbackStatus,
    DateTimeOffset? DeployedAt,
    GovernmentCloudReleaseChecklistItem[] CompletedChecklist,
    GovernmentCloudReleaseEvidenceLinkDto[] EvidenceLinks,
    GovernmentCloudReleaseGapArea[] OpenCriticalGaps,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record GovernmentCloudReleaseEvidenceLinkDto(GovernmentCloudReleaseEvidenceType EvidenceType, string Link);

public enum GovernmentCloudReleaseStatus
{
    Draft,
    Approved,
    Deployed,
    Blocked
}

public enum GovernmentCloudReleaseChecklistItem
{
    Migrations,
    SmokeTests,
    SecurityScans,
    DependencyReview,
    Backup,
    Restore,
    Monitoring,
    IncidentResponse,
    SupportCoverage,
    RollbackPlan
}

public enum GovernmentCloudReleaseEvidenceType
{
    Runbook,
    AlertRouting,
    AccessReview,
    VulnerabilityScan,
    BackupRestore,
    IncidentDrill
}

public enum GovernmentCloudReleaseGapArea
{
    Security,
    Migration,
    Backup,
    Restore,
    IncidentResponse
}

public enum GovernmentCloudReleaseGapSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public sealed class GovernmentCloudReleaseReadinessValidationException(string message) : InvalidOperationException(message);

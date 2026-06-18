using Gccs.Application.Audit;
using Gccs.Domain.Audit;

namespace Gccs.Application.Tenancy;

public sealed class CuiSupportEscalationService(
    ICuiSupportEscalationRepository repository,
    IAuditEventWriter auditEventWriter)
{
    public Task<IReadOnlyList<CuiSupportEscalationDto>> ListAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        repository.ListAsync(tenantId, cancellationToken);

    public async Task<CuiSupportEscalationDto> CreateAsync(
        Guid tenantId,
        CreateCuiSupportEscalationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateCreate(request);
        var escalation = await repository.CreateAsync(tenantId, request, actorUserId, DateTimeOffset.UtcNow, cancellationToken);
        await WriteAuditAsync(escalation, actorUserId, AuditAction.Created, "created", cancellationToken);
        return escalation;
    }

    public async Task<CuiSupportEscalationDto?> UpdateSupportFieldsAsync(
        Guid tenantId,
        Guid escalationId,
        UpdateCuiSupportEscalationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateUpdate(request);
        var escalation = await repository.UpdateSupportFieldsAsync(tenantId, escalationId, request, actorUserId, DateTimeOffset.UtcNow, cancellationToken);
        if (escalation is not null)
        {
            await WriteAuditAsync(escalation, actorUserId, AuditAction.Updated, "updated", cancellationToken);
        }

        return escalation;
    }

    private static void ValidateCreate(CreateCuiSupportEscalationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SourceWorkflow))
        {
            throw new CuiSupportEscalationValidationException("Escalation source workflow is required.");
        }

        if (string.IsNullOrWhiteSpace(request.AffectedEntityType) || string.IsNullOrWhiteSpace(request.AffectedEntityId))
        {
            throw new CuiSupportEscalationValidationException("Affected item reference is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            throw new CuiSupportEscalationValidationException("Escalation description is required.");
        }
    }

    private static void ValidateUpdate(UpdateCuiSupportEscalationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Owner))
        {
            throw new CuiSupportEscalationValidationException("Escalation owner is required.");
        }
    }

    private Task WriteAuditAsync(
        CuiSupportEscalationDto escalation,
        Guid actorUserId,
        AuditAction action,
        string lifecycleAction,
        CancellationToken cancellationToken) =>
        auditEventWriter.WriteAsync(
            escalation.TenantId,
            actorUserId,
            action,
            "CuiSupportEscalation",
            escalation.Id.ToString(),
            $"CUI support escalation {lifecycleAction}.",
            new Dictionary<string, string>
            {
                ["tenantId"] = escalation.TenantId.ToString(),
                ["sourceWorkflow"] = escalation.SourceWorkflow,
                ["affectedEntityType"] = escalation.AffectedEntityType,
                ["affectedEntityId"] = escalation.AffectedEntityId,
                ["category"] = escalation.Category.ToString(),
                ["severity"] = escalation.Severity.ToString(),
                ["status"] = escalation.Status.ToString(),
                ["isAffectedContentBlocked"] = escalation.IsAffectedContentBlocked.ToString(),
                ["lifecycleAction"] = lifecycleAction
            },
            cancellationToken);
}

public interface ICuiSupportEscalationRepository
{
    Task<IReadOnlyList<CuiSupportEscalationDto>> ListAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<CuiSupportEscalationDto> CreateAsync(
        Guid tenantId,
        CreateCuiSupportEscalationRequest request,
        Guid actorUserId,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken = default);

    Task<CuiSupportEscalationDto?> UpdateSupportFieldsAsync(
        Guid tenantId,
        Guid escalationId,
        UpdateCuiSupportEscalationRequest request,
        Guid actorUserId,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken = default);
}

public sealed class CuiSupportEscalationValidationException(string message) : InvalidOperationException(message);

public sealed record CreateCuiSupportEscalationRequest(
    string SourceWorkflow,
    string AffectedEntityType,
    string AffectedEntityId,
    CuiSupportEscalationCategory Category,
    CuiSupportEscalationSeverity Severity,
    string Description);

public sealed record UpdateCuiSupportEscalationRequest(
    string Owner,
    CuiSupportEscalationSeverity Severity,
    CuiSupportEscalationStatus Status);

public sealed record CuiSupportEscalationDto(
    Guid Id,
    Guid TenantId,
    string SourceWorkflow,
    string AffectedEntityType,
    string AffectedEntityId,
    CuiSupportEscalationCategory Category,
    CuiSupportEscalationSeverity Severity,
    CuiSupportEscalationStatus Status,
    string? Owner,
    string Description,
    bool IsAffectedContentBlocked,
    DateTimeOffset CreatedAt,
    Guid CreatedByUserId,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedByUserId);

public enum CuiSupportEscalationCategory
{
    SuspectedCui,
    ProhibitedData,
    ClassificationQuestion
}

public enum CuiSupportEscalationSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public enum CuiSupportEscalationStatus
{
    Submitted,
    Triage,
    Contained,
    Resolved
}

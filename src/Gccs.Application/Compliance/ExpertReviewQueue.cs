using Gccs.Application.Audit;
using Gccs.Application.Notifications;
using Gccs.Domain.Audit;

namespace Gccs.Application.Compliance;

public sealed class ExpertReviewQueueService(
    IExpertReviewQueueRepository repository,
    IAuditEventWriter auditEventWriter,
    IEnumerable<IAssignmentNotificationRepository> notificationRepositories)
{
    private IAssignmentNotificationRepository? Notifications => notificationRepositories.FirstOrDefault();

    public async Task<ExpertReviewItemDto> EscalateAsync(
        EscalateExpertReviewRequest request,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(request);
        ValidateEscalation(normalized);
        var item = await repository.CreateEscalationAsync(normalized, tenantId, actorUserId, cancellationToken);
        if (item.AssignedExpertUserId is { } assignedExpertUserId && Notifications is not null)
        {
            await Notifications.EmitExpertReviewAssignmentAsync(
                item.TenantId,
                item.Id,
                assignedExpertUserId,
                item.Topic,
                actorUserId,
                cancellationToken);
        }

        await WriteAuditAsync(item, actorUserId, AuditAction.Created, "Expert review item was escalated.", cancellationToken);
        return item;
    }

    public Task<IReadOnlyList<ExpertReviewItemDto>> ListAsync(
        ExpertReviewQueueQuery query,
        CancellationToken cancellationToken = default) =>
        repository.ListAsync(query, cancellationToken);

    public async Task<ExpertReviewItemDto?> ResolveAsync(
        Guid itemId,
        ResolveExpertReviewRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(request);
        ValidateResolution(normalized);
        var item = await repository.ResolveAsync(itemId, normalized, actorUserId, cancellationToken);
        if (item is not null)
        {
            await WriteAuditAsync(item, actorUserId, AuditAction.Updated, "Expert review item was resolved.", cancellationToken);
        }

        return item;
    }

    private static EscalateExpertReviewRequest Normalize(EscalateExpertReviewRequest request) =>
        request with
        {
            SourceType = request.SourceType.Trim(),
            Reason = request.Reason.Trim(),
            Priority = request.Priority.Trim(),
            Topic = request.Topic.Trim()
        };

    private static ResolveExpertReviewRequest Normalize(ResolveExpertReviewRequest request) =>
        request with
        {
            Decision = request.Decision.Trim(),
            Notes = request.Notes.Trim()
        };

    private static void ValidateEscalation(EscalateExpertReviewRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        AddIf(errors, request.SourceId == Guid.Empty, "sourceId", "Source id is required.");
        AddIf(errors, request.SourceType is not ("clause_candidate" or "suggested_obligation"), "sourceType", "Source type must be clause_candidate or suggested_obligation.");
        AddIf(errors, string.IsNullOrWhiteSpace(request.Reason), "reason", "Escalation reason is required.");
        AddIf(errors, string.IsNullOrWhiteSpace(request.Priority), "priority", "Priority is required.");
        AddIf(errors, string.IsNullOrWhiteSpace(request.Topic), "topic", "Topic is required.");
        ThrowIfInvalid(errors);
    }

    private static void ValidateResolution(ResolveExpertReviewRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        AddIf(errors, string.IsNullOrWhiteSpace(request.Decision), "decision", "Resolution decision is required.");
        AddIf(errors, string.IsNullOrWhiteSpace(request.Notes), "notes", "Resolution notes are required.");
        ThrowIfInvalid(errors);
    }

    private static void AddIf(Dictionary<string, string[]> errors, bool condition, string key, string message)
    {
        if (condition)
        {
            errors[key] = [message];
        }
    }

    private static void ThrowIfInvalid(Dictionary<string, string[]> errors)
    {
        if (errors.Count > 0)
        {
            throw new ExpertReviewValidationException(errors);
        }
    }

    private Task WriteAuditAsync(
        ExpertReviewItemDto item,
        Guid actorUserId,
        AuditAction action,
        string summary,
        CancellationToken cancellationToken) =>
        auditEventWriter.WriteAsync(
            item.TenantId,
            actorUserId,
            action,
            "ExpertReviewItem",
            item.Id.ToString(),
            summary,
            new Dictionary<string, string>
            {
                ["sourceType"] = item.SourceType,
                ["sourceId"] = item.SourceId.ToString(),
                ["status"] = item.Status,
                ["priority"] = item.Priority,
                ["assignedExpertUserId"] = item.AssignedExpertUserId?.ToString() ?? string.Empty,
                ["resolutionDecision"] = item.ResolutionDecision ?? string.Empty
            },
            cancellationToken);
}

public interface IExpertReviewQueueRepository
{
    Task<ExpertReviewItemDto> CreateEscalationAsync(EscalateExpertReviewRequest request, Guid tenantId, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExpertReviewItemDto>> ListAsync(ExpertReviewQueueQuery query, CancellationToken cancellationToken = default);
    Task<ExpertReviewItemDto?> ResolveAsync(Guid itemId, ResolveExpertReviewRequest request, Guid actorUserId, CancellationToken cancellationToken = default);
}

public sealed record ExpertReviewQueueQuery(
    string? Status,
    string? SourceType,
    Guid? AssignedExpertUserId,
    string? Priority);

public sealed record EscalateExpertReviewRequest(
    string SourceType,
    Guid SourceId,
    string Reason,
    string Priority,
    string Topic,
    Guid? AssignedExpertUserId,
    DateOnly? DueAt);

public sealed record ResolveExpertReviewRequest(
    string Decision,
    string Notes);

public sealed record ExpertReviewItemDto(
    Guid Id,
    Guid TenantId,
    string SourceType,
    Guid SourceId,
    string Reason,
    string Priority,
    string Topic,
    Guid? AssignedExpertUserId,
    DateOnly? DueAt,
    string Status,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt,
    Guid? ResolvedByUserId,
    DateTimeOffset? ResolvedAt,
    string? ResolutionDecision,
    string? ResolutionNotes);

public sealed class ExpertReviewValidationException(IReadOnlyDictionary<string, string[]> errors)
    : InvalidOperationException("Expert review input is invalid.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}

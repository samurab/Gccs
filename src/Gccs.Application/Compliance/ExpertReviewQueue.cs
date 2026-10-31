using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Application.Notifications;
using Gccs.Domain.Audit;

namespace Gccs.Application.Compliance;

public sealed class ExpertReviewQueueService(
    IExpertReviewQueueRepository repository,
    IAuditEventWriter auditEventWriter,
    IEnumerable<IAssignmentNotificationRepository> notificationRepositories,
    IApplicationTransaction transaction)
{
    private IAssignmentNotificationRepository? Notifications => notificationRepositories.FirstOrDefault();

    public async Task<ExpertReviewItemDto> EscalateAsync(
        EscalateExpertReviewRequest request,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default) =>
        (await EscalateWithResultAsync(request, tenantId, actorUserId, cancellationToken)).Item;

    public Task<ExpertReviewEscalationResult> EscalateWithResultAsync(
        EscalateExpertReviewRequest request,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(request);
        ValidateEscalation(normalized);
        return transaction.ExecuteAsync(async token =>
        {
            if (!await repository.SourceExistsAsync(normalized.SourceType, normalized.SourceId, tenantId, token))
                throw new ExpertReviewSourceNotFoundException();

            var existing = await repository.FindOpenAsync(normalized.SourceType, normalized.SourceId, tenantId, token);
            if (existing is not null)
                return new ExpertReviewEscalationResult(existing, Created: false);

            var item = await repository.CreateEscalationAsync(normalized, tenantId, actorUserId, token);
            if (item.AssignedExpertUserId is { } assignedExpertUserId && Notifications is not null)
            {
                await Notifications.EmitExpertReviewAssignmentAsync(
                    item.TenantId,
                    item.Id,
                    assignedExpertUserId,
                    item.Topic,
                    actorUserId,
                    token);
            }

            await WriteAuditAsync(item, actorUserId, AuditAction.Created, "Expert review item was escalated.", token);
            return new ExpertReviewEscalationResult(item, Created: true);
        }, cancellationToken);
    }

    public Task<IReadOnlyList<ExpertReviewItemDto>> ListAsync(
        ExpertReviewQueueQuery query,
        CancellationToken cancellationToken = default) =>
        repository.ListAsync(query, cancellationToken);

    public Task<ExpertReviewItemDto?> AssignAsync(
        Guid itemId,
        AssignExpertReviewRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateAssignment(request);
        return transaction.ExecuteAsync(async token =>
        {
            if (!await repository.IsActiveTenantMemberAsync(request.AssignedExpertUserId, token))
                throw ExpertReviewValidationException.For("assignedExpertUserId", "Assigned expert must be an active member of the current tenant.");

            var item = await repository.AssignAsync(itemId, request, actorUserId, token);
            if (item is null) return null;
            if (Notifications is not null)
                await Notifications.EmitExpertReviewAssignmentAsync(
                    item.TenantId, item.Id, request.AssignedExpertUserId, item.Topic, actorUserId, token);
            await WriteAuditAsync(item, actorUserId, AuditAction.Updated, "Expert review item was assigned.", token);
            return item;
        }, cancellationToken);
    }

    public Task<ExpertReviewItemDto?> ResolveAsync(
        Guid itemId,
        ResolveExpertReviewRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(request);
        ValidateResolution(normalized);
        return transaction.ExecuteAsync(async token =>
        {
            var item = await repository.ResolveAsync(itemId, normalized, actorUserId, token);
            if (item is not null)
                await WriteAuditAsync(item, actorUserId, AuditAction.Updated, "Expert review item was resolved.", token);

            return item;
        }, cancellationToken);
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

    private static void ValidateAssignment(AssignExpertReviewRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        AddIf(errors, request.AssignedExpertUserId == Guid.Empty, "assignedExpertUserId", "Assigned expert is required.");
        AddIf(errors, request.DueAt is { } dueAt && dueAt < DateOnly.FromDateTime(DateTime.UtcNow), "dueAt", "Due date cannot be in the past.");
        ThrowIfInvalid(errors);
    }

    private static void ValidateEscalation(EscalateExpertReviewRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        AddIf(errors, request.SourceId == Guid.Empty, "sourceId", "Source id is required.");
        AddIf(errors, request.SourceType is not ("clause_candidate" or "suggested_obligation" or "assistant_answer"),
            "sourceType", "Source type must be clause_candidate, suggested_obligation, or assistant_answer.");
        AddIf(errors, string.IsNullOrWhiteSpace(request.Reason), "reason", "Escalation reason is required.");
        AddIf(errors, string.IsNullOrWhiteSpace(request.Priority), "priority", "Priority is required.");
        AddIf(errors, string.IsNullOrWhiteSpace(request.Topic), "topic", "Topic is required.");
        ThrowIfInvalid(errors);
    }

    private static void ValidateResolution(ResolveExpertReviewRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        AddIf(errors, request.Decision is not ("accepted_as_reviewed_draft" or "revision_required" or "rejected"),
            "decision", "Resolution decision must be accepted_as_reviewed_draft, revision_required, or rejected.");
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
    Task<bool> SourceExistsAsync(string sourceType, Guid sourceId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<ExpertReviewItemDto?> FindOpenAsync(string sourceType, Guid sourceId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<ExpertReviewItemDto> CreateEscalationAsync(EscalateExpertReviewRequest request, Guid tenantId, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExpertReviewItemDto>> ListAsync(ExpertReviewQueueQuery query, CancellationToken cancellationToken = default);
    Task<bool> IsActiveTenantMemberAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ExpertReviewItemDto?> AssignAsync(Guid itemId, AssignExpertReviewRequest request, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<ExpertReviewItemDto?> ResolveAsync(Guid itemId, ResolveExpertReviewRequest request, Guid actorUserId, CancellationToken cancellationToken = default);
}

public sealed record ExpertReviewEscalationResult(ExpertReviewItemDto Item, bool Created);

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
public sealed record AssignExpertReviewRequest(Guid AssignedExpertUserId, DateOnly? DueAt);

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
    public static ExpertReviewValidationException For(string key, string message) =>
        new(new Dictionary<string, string[]> { [key] = [message] });
}

public sealed class ExpertReviewSourceNotFoundException : InvalidOperationException
{
    public ExpertReviewSourceNotFoundException() : base("Expert review source was not found.") { }
}

public sealed class ExpertReviewDuplicateException : InvalidOperationException
{
    public ExpertReviewDuplicateException() : base("An open expert review item already exists for this source.") { }
}

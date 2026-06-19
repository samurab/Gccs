using Gccs.Application.Audit;
using Gccs.Domain.Audit;
using Gccs.Domain.Common;

namespace Gccs.Application.Ai;

public sealed class AiOutputReviewService(
    IAiOutputReviewRepository repository,
    IAuditEventWriter auditEventWriter)
{
    public async Task<AiInteractionLogDto> LogInteractionAsync(
        AiInteractionLogRequest request,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (request.Classification is ContentClassification.Prohibited)
        {
            throw new AiOutputReviewException("AI logs cannot store prohibited data.");
        }

        var log = await repository.CreateAsync(request, tenantId, actorUserId, cancellationToken);
        await WriteAuditAsync(log, actorUserId, AuditAction.Created, "AI interaction log was created.", cancellationToken);
        return log;
    }

    public async Task<IReadOnlyList<AiInteractionLogDto>> ListAsync(
        Guid tenantId,
        bool hasReviewPermission,
        CancellationToken cancellationToken = default)
    {
        EnsureReviewPermission(hasReviewPermission);
        return await repository.ListAsync(tenantId, cancellationToken);
    }

    public async Task EnsureApprovedForDeliverableAsync(
        Guid logId,
        AiDeliverableType deliverableType,
        CancellationToken cancellationToken = default)
    {
        var log = await repository.FindAsync(logId, cancellationToken);
        if (log is null || log.State != AiOutputReviewState.Approved)
        {
            throw new AiOutputReviewException($"{deliverableType} AI output must be human approved before use.");
        }
    }

    public async Task<AiInteractionLogDto?> ReviewAsync(
        Guid logId,
        AiOutputReviewDecisionRequest request,
        Guid reviewerUserId,
        CancellationToken cancellationToken = default)
    {
        EnsureReviewPermission(request.HasReviewPermission);
        var updated = await repository.UpdateStateAsync(logId, request.State, request.Note, request.Reason, reviewerUserId, cancellationToken);
        if (updated is not null)
        {
            await WriteAuditAsync(updated, reviewerUserId, AuditAction.Updated, $"AI output was {request.State}.", cancellationToken);
        }

        return updated;
    }

    public async Task<AiOutputExportDto> ExportAsync(
        Guid tenantId,
        bool hasReviewPermission,
        CancellationToken cancellationToken = default)
    {
        EnsureReviewPermission(hasReviewPermission);
        var logs = await repository.ListAsync(tenantId, cancellationToken);
        return new AiOutputExportDto(tenantId, logs.Count, DateTimeOffset.UtcNow, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(365).Date));
    }

    private static void EnsureReviewPermission(bool hasReviewPermission)
    {
        if (!hasReviewPermission)
        {
            throw new AiOutputReviewException("AI review permission is required.");
        }
    }

    private async Task WriteAuditAsync(
        AiInteractionLogDto log,
        Guid actorUserId,
        AuditAction action,
        string summary,
        CancellationToken cancellationToken)
    {
        await auditEventWriter.WriteAsync(
            log.TenantId,
            actorUserId,
            action,
            "AiInteractionLog",
            log.Id.ToString(),
            summary,
            new Dictionary<string, string>
            {
                ["workflowContext"] = log.WorkflowContext,
                ["state"] = log.State.ToString(),
                ["retrievedSources"] = string.Join("|", log.RetrievedSourceIds),
                ["classification"] = log.Classification.ToString()
            },
            cancellationToken);
    }
}

public interface IAiOutputReviewRepository
{
    Task<AiInteractionLogDto> CreateAsync(AiInteractionLogRequest request, Guid tenantId, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<AiInteractionLogDto?> FindAsync(Guid logId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AiInteractionLogDto>> ListAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<AiInteractionLogDto?> UpdateStateAsync(Guid logId, AiOutputReviewState state, string? note, string? reason, Guid reviewerUserId, CancellationToken cancellationToken = default);
}

public sealed record AiInteractionLogRequest(
    string Prompt,
    string PromptMetadata,
    string ModelConfiguration,
    IReadOnlyList<string> RetrievedSourceIds,
    string GeneratedOutput,
    string WorkflowContext,
    ContentClassification Classification);

public sealed record AiOutputReviewDecisionRequest(
    AiOutputReviewState State,
    string? Note,
    string? Reason,
    bool HasReviewPermission = true);

public sealed record AiInteractionLogDto(
    Guid Id,
    Guid TenantId,
    Guid ActorUserId,
    string Prompt,
    string PromptMetadata,
    string ModelConfiguration,
    IReadOnlyList<string> RetrievedSourceIds,
    string GeneratedOutput,
    string WorkflowContext,
    ContentClassification Classification,
    AiOutputReviewState State,
    Guid? ReviewerUserId,
    string? ReviewNote,
    string? RejectionReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReviewedAt,
    DateOnly RetainUntil);

public sealed record AiOutputExportDto(
    Guid TenantId,
    int LogCount,
    DateTimeOffset ExportedAt,
    DateOnly RetainUntil);

public enum AiOutputReviewState
{
    Draft,
    NeedsReview,
    Approved,
    Rejected,
    Superseded,
    Archived
}

public enum AiDeliverableType
{
    Report,
    Policy,
    Ssp,
    Poam,
    CustomerDeliverable
}

public sealed class AiOutputReviewException(string message) : InvalidOperationException(message);

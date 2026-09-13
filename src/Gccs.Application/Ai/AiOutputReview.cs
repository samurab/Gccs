using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Domain.Audit;

namespace Gccs.Application.Ai;

public sealed class AiOutputReviewService(IGuardedAssistantRepository repository, IAuditEventWriter auditEventWriter,
    IApplicationTransaction transaction, TimeProvider timeProvider, IAiOutputRetentionRepository? retentionRepository = null)
{
    public Task<IReadOnlyList<GuardedAssistantAnswerDto>> ListAsync(
        Guid tenantId,
        bool includeArchived,
        IReadOnlyCollection<string>? allowedWorkflowContexts = null,
        CancellationToken cancellationToken = default) =>
        repository.ListAnswersAsync(tenantId, includeArchived, allowedWorkflowContexts, cancellationToken);

    public Task<IReadOnlyList<AiOutputReviewHistoryDto>> HistoryAsync(Guid answerId, Guid tenantId,
        CancellationToken cancellationToken = default) => repository.ListReviewHistoryAsync(answerId, tenantId, cancellationToken);

    public Task<AiOutputReviewResultDto?> ReviewAsync(Guid answerId, Guid tenantId, AiOutputReviewDecisionRequest request,
        Guid reviewerUserId, CancellationToken cancellationToken = default)
    {
        return transaction.ExecuteAsync(async token =>
        {
            try
            {
                ValidateDecision(request);
                var current = await repository.FindAnswerAsync(answerId, tenantId, token);
                if (current is null) return null;
                EnsureTransition(current.ReviewState, request.State);
                var result = await repository.ReviewAnswerAsync(answerId, tenantId, request, reviewerUserId, token);
                if (result is null) return null;
                await auditEventWriter.WriteChangeAsync(tenantId, reviewerUserId, AuditAction.Updated,
                    "AiInteractionLog", answerId.ToString(), "AI output review decision was recorded.",
                    current.ReviewState.ToString(), request.State.ToString(), new Dictionary<string, string>
                    {
                        ["workflowContext"] = current.WorkflowContext, ["previousState"] = current.ReviewState.ToString(),
                        ["state"] = request.State.ToString(), ["result"] = "accepted"
                    }, token);
                return result;
            }
            catch (AiOutputReviewValidationException exception)
            {
                await auditEventWriter.WriteAsync(tenantId, reviewerUserId, AuditAction.Rejected, "AiInteractionLog",
                    answerId.ToString(), "AI output review decision was rejected.", new Dictionary<string, string>
                {
                    ["state"] = request.State.ToString(), ["result"] = "rejected", ["validationField"] = exception.Field
                }, token);
                throw;
            }
        }, cancellationToken);
    }

    public async Task<AiOutputExportDto> ExportAsync(
        Guid tenantId,
        bool includeArchived,
        IReadOnlyCollection<string> allowedWorkflowContexts,
        CancellationToken cancellationToken = default)
    {
        var logs = await repository.ListAnswersAsync(
            tenantId, includeArchived, allowedWorkflowContexts, cancellationToken);
        return new(tenantId, logs.Count, timeProvider.GetUtcNow(), logs.Select(log => new AiOutputExportRecordDto(
            log.Id, log.ActorUserId, log.Prompt, log.PromptWasRedacted, log.PromptMetadata,
            log.ModelConfiguration, log.Citations, log.Answer, log.WorkflowContext, log.Classification,
            log.Result, log.ReviewState, log.ReviewedByUserId, log.ReviewedAt, log.ReviewNotes,
            log.RejectionReason, log.CreatedAt, log.RetainUntil)).ToArray());
    }

    public Task<AiOutputUsageDto?> LinkDeliverableAsync(Guid answerId, Guid tenantId, AiOutputUsageRequest request,
        Guid actorUserId, CancellationToken cancellationToken = default)
    {
        ValidateUsage(request);
        return transaction.ExecuteAsync(async token =>
        {
            var answer = await repository.FindAnswerAsync(answerId, tenantId, token);
            if (answer is null) return null;
            try
            {
                EnsureApprovedForDeliverable(answer, request.DeliverableType);
                if (!await repository.DeliverableExistsAsync(tenantId, request, token))
                    throw new AiOutputReviewValidationException("deliverableId", "The tenant-scoped deliverable was not found.");
            }
            catch (AiOutputReviewValidationException exception)
            {
                await auditEventWriter.WriteAsync(tenantId, actorUserId, AuditAction.Rejected, "AiOutputUsage",
                    answerId.ToString(), "AI output deliverable use was rejected.", new Dictionary<string, string>
                    {
                        ["deliverableType"] = request.DeliverableType.ToString(), ["result"] = "rejected",
                        ["validationField"] = exception.Field
                    }, token);
                throw;
            }
            var usage = await repository.LinkDeliverableAsync(answerId, tenantId, request, actorUserId, token);
            if (usage is not null)
                await auditEventWriter.WriteAsync(tenantId, actorUserId, AuditAction.Created, "AiOutputUsage",
                    usage.Id.ToString(), "Approved AI output was linked to a governed deliverable.",
                    new Dictionary<string, string> { ["answerId"] = answerId.ToString(),
                        ["deliverableType"] = request.DeliverableType.ToString(), ["deliverableId"] = request.DeliverableId.Trim(),
                        ["state"] = answer.ReviewState.ToString() }, token);
            return usage;
        }, cancellationToken);
    }

    public async Task<AiOutputUsageDto> RequireDeliverableLinkAsync(Guid answerId, Guid tenantId,
        AiDeliverableType deliverableType, Guid deliverableId, Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var usage = await LinkDeliverableAsync(answerId, tenantId,
            new AiOutputUsageRequest(deliverableType, deliverableId.ToString()), actorUserId, cancellationToken);
        return usage ?? throw new AiOutputReviewValidationException(
            "aiOutputId", "The referenced AI output was not found in the current tenant scope.");
    }

    public void EnsureApprovedForDeliverable(GuardedAssistantAnswerDto answer, AiDeliverableType deliverableType)
    {
        if (answer.ReviewState != AiOutputReviewState.Approved || answer.RetainUntil <= timeProvider.GetUtcNow())
            throw new AiOutputReviewValidationException("answerId", $"{deliverableType} use requires current, human-approved AI output.");
    }

    public Task<int> ArchiveExpiredAsync(int batchSize = 100, CancellationToken cancellationToken = default)
    {
        if (retentionRepository is null) return Task.FromResult(0);
        var size = Math.Clamp(batchSize, 1, 500);
        return transaction.ExecuteAsync(async token =>
        {
            var archived = await retentionRepository.ArchiveExpiredAsync(timeProvider.GetUtcNow(), size, token);
            foreach (var item in archived)
                await auditEventWriter.WriteChangeAsync(item.TenantId, Guid.Empty, AuditAction.Updated,
                    "AiInteractionLog", item.AnswerId.ToString(), "Expired AI output was archived by retention policy.",
                    item.PreviousState.ToString(), AiOutputReviewState.Archived.ToString(),
                    new Dictionary<string, string> { ["state"] = "Archived", ["result"] = "retention-archived" }, token);
            return archived.Count;
        }, cancellationToken);
    }

    private static void ValidateDecision(AiOutputReviewDecisionRequest request)
    {
        if (request.State is not (AiOutputReviewState.Approved or AiOutputReviewState.Rejected or AiOutputReviewState.Superseded or AiOutputReviewState.Archived))
            throw new AiOutputReviewValidationException("state", "Reviewers may approve, reject, supersede, or archive output.");
        if (request.ExpectedVersion < 0) throw new AiOutputReviewValidationException("expectedVersion", "A valid current version is required.");
        if (string.IsNullOrWhiteSpace(request.Note) || request.Note.Trim().Length > 1_000)
            throw new AiOutputReviewValidationException("note", "A review note between 1 and 1,000 characters is required.");
        if (request.State == AiOutputReviewState.Rejected && string.IsNullOrWhiteSpace(request.Reason))
            throw new AiOutputReviewValidationException("reason", "A rejection reason is required.");
        if (request.Reason?.Trim().Length > 1_000) throw new AiOutputReviewValidationException("reason", "The reason cannot exceed 1,000 characters.");
        if (AssistantPromptGuard.GetBlockedReason(request.Note) is not null ||
            (!string.IsNullOrWhiteSpace(request.Reason) && AssistantPromptGuard.GetBlockedReason(request.Reason) is not null))
            throw new AiOutputReviewValidationException("note", "Review text cannot contain prohibited data or unsupported requests.");
    }

    private static void EnsureTransition(AiOutputReviewState current, AiOutputReviewState next)
    {
        var allowed = current switch
        {
            AiOutputReviewState.Draft or AiOutputReviewState.NeedsReview => next is AiOutputReviewState.Approved or AiOutputReviewState.Rejected or AiOutputReviewState.Superseded or AiOutputReviewState.Archived,
            AiOutputReviewState.Approved or AiOutputReviewState.Rejected => next is AiOutputReviewState.Superseded or AiOutputReviewState.Archived,
            AiOutputReviewState.Superseded => next is AiOutputReviewState.Archived,
            _ => false
        };
        if (!allowed) throw new AiOutputReviewValidationException("state", $"Transition from {current} to {next} is not allowed.");
    }

    private static void ValidateUsage(AiOutputUsageRequest request)
    {
        if (!Enum.IsDefined(request.DeliverableType)) throw new AiOutputReviewValidationException("deliverableType", "A supported deliverable type is required.");
        if (string.IsNullOrWhiteSpace(request.DeliverableId) || request.DeliverableId.Trim().Length > 200)
            throw new AiOutputReviewValidationException("deliverableId", "A deliverable id between 1 and 200 characters is required.");
    }
}

public sealed record AiOutputReviewDecisionRequest(AiOutputReviewState State, string? Note, string? Reason, long ExpectedVersion);
public sealed record AiOutputReviewResultDto(GuardedAssistantAnswerDto Answer, AiOutputReviewHistoryDto Review);
public sealed record AiOutputReviewHistoryDto(Guid Id, Guid TenantId, Guid AnswerId, AiOutputReviewState PreviousState,
    AiOutputReviewState NewState, Guid? ReviewerUserId, string? Note, string? RejectionReason, DateTimeOffset CreatedAt);
public sealed record AiOutputUsageRequest(AiDeliverableType DeliverableType, string DeliverableId);
public sealed record AiOutputUsageDto(Guid Id, Guid TenantId, Guid AnswerId, AiDeliverableType DeliverableType,
    string DeliverableId, Guid LinkedByUserId, DateTimeOffset LinkedAt);
public sealed record AiOutputExportDto(Guid TenantId, int LogCount, DateTimeOffset ExportedAt, IReadOnlyList<AiOutputExportRecordDto> Logs);
public sealed record AiOutputExportRecordDto(Guid Id, Guid ActorUserId, string Prompt, bool PromptWasRedacted,
    string PromptMetadata, string ModelConfiguration, IReadOnlyList<AiCitationDto> RetrievedSources, string GeneratedOutput,
    string WorkflowContext, Gccs.Domain.Common.ContentClassification Classification, string Result, AiOutputReviewState State,
    Guid? ReviewerUserId, DateTimeOffset? ReviewedAt, string? ReviewNote, string? RejectionReason,
    DateTimeOffset CreatedAt, DateTimeOffset RetainUntil);

public enum AiOutputReviewState { Draft, NeedsReview, Approved, Rejected, Superseded, Archived }
public enum AiDeliverableType { Report, Policy, Ssp, Poam, CustomerDeliverable }

public interface IAiOutputRetentionRepository
{
    Task<IReadOnlyList<AiOutputRetentionArchiveDto>> ArchiveExpiredAsync(DateTimeOffset now, int batchSize,
        CancellationToken cancellationToken = default);
}
public sealed record AiOutputRetentionArchiveDto(Guid TenantId, Guid AnswerId, AiOutputReviewState PreviousState);

public sealed class AiOutputReviewValidationException(string field, string message) : InvalidOperationException(message)
{
    public string Field { get; } = field;
}
public sealed class AiOutputReviewConflictException() : InvalidOperationException("The AI output changed before this decision was saved.");

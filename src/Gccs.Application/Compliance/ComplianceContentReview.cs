using Gccs.Application.Audit;
using Gccs.Domain.Audit;
using Gccs.Domain.Common;

namespace Gccs.Application.Compliance;

public sealed class ComplianceContentReviewService(
    IComplianceContentReviewRepository repository,
    IAuditEventWriter auditEventWriter)
{
    public async Task<ComplianceContentReviewDto?> ChangeObligationStateAsync(
        string obligationId,
        ChangeComplianceContentReviewStateRequest request,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var current = await repository.FindObligationReviewAsync(obligationId, cancellationToken);
        if (current is null)
        {
            return null;
        }

        ValidateTransition(current, request);

        var updated = await repository.UpdateObligationReviewStateAsync(
            obligationId,
            request.State,
            request.ReviewerUserId,
            request.ReviewedAt,
            cancellationToken);

        if (updated is null)
        {
            return null;
        }

        await auditEventWriter.WriteAsync(
            tenantId,
            actorUserId,
            AuditAction.Updated,
            "Obligation",
            obligationId,
            $"Obligation '{obligationId}' review state changed from {current.State} to {updated.State}.",
            new Dictionary<string, string>
            {
                ["beforeState"] = current.State.ToString(),
                ["afterState"] = updated.State.ToString(),
                ["requiresExpertReview"] = updated.RequiresExpertReview.ToString(),
                ["reviewerUserId"] = updated.ReviewerUserId?.ToString() ?? string.Empty,
                ["reviewedAt"] = updated.LastReviewedAt.ToString("O")
            },
            cancellationToken);

        return updated;
    }

    public Task<bool> CanUseObligationForNewMappingAsync(string obligationId, CancellationToken cancellationToken = default) =>
        repository.CanUseObligationForNewMappingAsync(obligationId, cancellationToken);

    private static void ValidateTransition(
        ComplianceContentReviewDto current,
        ChangeComplianceContentReviewStateRequest request)
    {
        if (request.State is ReviewState.Published &&
            current.RequiresExpertReview &&
            (request.ReviewerUserId is null || request.ReviewedAt is null))
        {
            throw new ComplianceContentReviewException(
                "Expert-review-required content cannot be published without reviewerUserId and reviewedAt.");
        }

        if (request.State is ReviewState.Published && request.ReviewedAt is null)
        {
            throw new ComplianceContentReviewException(
                "Published content requires reviewedAt.");
        }
    }
}

public interface IComplianceContentReviewRepository
{
    Task<ComplianceContentReviewDto?> FindObligationReviewAsync(
        string obligationId,
        CancellationToken cancellationToken = default);

    Task<ComplianceContentReviewDto?> UpdateObligationReviewStateAsync(
        string obligationId,
        ReviewState state,
        Guid? reviewerUserId,
        DateOnly? reviewedAt,
        CancellationToken cancellationToken = default);

    Task<bool> CanUseObligationForNewMappingAsync(
        string obligationId,
        CancellationToken cancellationToken = default);
}

public sealed record ChangeComplianceContentReviewStateRequest(
    ReviewState State,
    Guid? ReviewerUserId,
    DateOnly? ReviewedAt);

public sealed record ComplianceContentReviewDto(
    string ObligationId,
    ReviewState State,
    bool RequiresExpertReview,
    Guid? ReviewerUserId,
    DateOnly LastReviewedAt);

public sealed class ComplianceContentReviewException(string message) : InvalidOperationException(message);

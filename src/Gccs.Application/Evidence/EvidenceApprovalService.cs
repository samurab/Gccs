using Gccs.Application.Audit;
using Gccs.Domain.Audit;
using Gccs.Domain.Evidence;

namespace Gccs.Application.Evidence;

public sealed class EvidenceApprovalService(
    IEvidenceMetadataRepository repository,
    IAuditEventWriter auditEventWriter)
{
    public async Task<EvidenceReviewDto?> ReviewAsync(
        Guid evidenceItemId,
        EvidenceReviewRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var comment = NormalizeComment(request.Comment);
        Validate(request.Decision, comment);

        var reviewedAt = DateTimeOffset.UtcNow;
        var review = await repository.ApplyCurrentTenantReviewAsync(
            evidenceItemId,
            request.Decision,
            comment,
            actorUserId,
            reviewedAt,
            cancellationToken);

        if (review is null)
        {
            return null;
        }

        await auditEventWriter.WriteAsync(
            review.TenantId,
            actorUserId,
            ToAuditAction(request.Decision),
            "EvidenceItem",
            review.EvidenceItemId.ToString(),
            $"Evidence review decision '{request.Decision}' was recorded.",
            new Dictionary<string, string>
            {
                ["decision"] = request.Decision.ToString(),
                ["status"] = review.Status.ToString(),
                ["comment"] = comment ?? string.Empty,
                ["eligibleForReports"] = review.EligibleForReports.ToString(),
                ["reviewedAt"] = review.ReviewedAt.ToString("O")
            },
            cancellationToken);

        return review;
    }

    private static string? NormalizeComment(string? comment)
    {
        var normalized = comment?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static void Validate(EvidenceReviewDecision decision, string? comment)
    {
        if ((decision is EvidenceReviewDecision.Reject or EvidenceReviewDecision.RequestChanges) &&
            string.IsNullOrWhiteSpace(comment))
        {
            throw new EvidenceReviewValidationException("Rejection and request-changes decisions require a comment or reason.");
        }

        if (comment?.Length > 1000)
        {
            throw new EvidenceReviewValidationException("Evidence review comments must be 1000 characters or fewer.");
        }
    }

    private static AuditAction ToAuditAction(EvidenceReviewDecision decision) =>
        decision switch
        {
            EvidenceReviewDecision.Approve => AuditAction.Approved,
            EvidenceReviewDecision.Reject or EvidenceReviewDecision.RequestChanges => AuditAction.Rejected,
            EvidenceReviewDecision.Archive => AuditAction.Archived,
            EvidenceReviewDecision.Expire => AuditAction.Expired,
            _ => AuditAction.Updated
        };
}

public sealed class EvidenceReviewValidationException(string message) : InvalidOperationException(message);

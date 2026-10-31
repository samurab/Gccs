using Gccs.Domain.Common;

namespace Gccs.Domain.Compliance;

public static class ObligationPublicationValidator
{
    public static void EnsureCanPublish(Obligation obligation)
    {
        var errors = ValidateForPublication(obligation);
        if (errors.Count > 0)
        {
            throw new ObligationPublicationValidationException(errors);
        }
    }

    public static IReadOnlyList<string> ValidateForPublication(Obligation obligation)
    {
        var errors = new List<string>();

        RequireText(obligation.Id, "id", errors);
        RequireText(obligation.Source, "source", errors);
        RequireText(obligation.Title, "title", errors);
        RequireText(obligation.TriggerCondition, "triggerCondition", errors);
        RequireText(obligation.RequiredAction, "requiredAction", errors);
        RequireText(obligation.OwnerFunction, "ownerFunction", errors);
        RequireText(obligation.FlowDownRequirement, "flowDownRequirement", errors);

        if (obligation.SourceReference.Url is null || !obligation.SourceReference.Url.IsAbsoluteUri)
        {
            errors.Add("sourceUrl is required before publication.");
        }

        if (obligation.SourceReference.LastReviewedAt == default)
        {
            errors.Add("lastReviewedAt is required before publication.");
        }

        RequireText(obligation.SourceReference.Confidence, "sourceConfidence", errors);
        RequireText(obligation.Review.Confidence, "reviewConfidence", errors);

        if (obligation.Review.LastReviewedAt == default)
        {
            errors.Add("review.lastReviewedAt is required before publication.");
        }

        if ((obligation.Review.RequiresExpertReview || obligation.SourceReference.RequiresExpertReview) &&
            obligation.Review.ReviewedByUserId is null)
        {
            errors.Add("reviewedByUserId is required before expert-review-required content can be published.");
        }

        if (obligation.Review.State is not ReviewState.Published)
        {
            errors.Add("reviewState must be Published before customer-facing publication.");
        }

        if (obligation.EvidenceExamples.Count == 0)
        {
            errors.Add("At least one evidence example must be linked before publication.");
        }

        foreach (var evidenceExample in obligation.EvidenceExamples)
        {
            RequireText(evidenceExample.Name, "evidenceExample.name", errors);
            RequireText(evidenceExample.Description, "evidenceExample.description", errors);
            RequireText(evidenceExample.Owner, "evidenceExample.owner", errors);
        }

        return errors;
    }

    private static void RequireText(string? value, string fieldName, ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{fieldName} is required before publication.");
        }
    }
}

public sealed class ObligationPublicationValidationException(IReadOnlyList<string> errors)
    : InvalidOperationException("Obligation content cannot be published until required metadata is complete.")
{
    public IReadOnlyList<string> Errors { get; } = errors;
}

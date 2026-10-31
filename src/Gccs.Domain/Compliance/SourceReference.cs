namespace Gccs.Domain.Compliance;

public sealed record SourceReference(
    string Source,
    Uri SourceUrl,
    DateOnly LastReviewedAt,
    DateOnly? EffectiveAt,
    string ApplicabilityLogic,
    string Confidence,
    bool RequiresExpertReview);

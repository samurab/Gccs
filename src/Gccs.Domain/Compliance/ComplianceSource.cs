namespace Gccs.Domain.Compliance;

public sealed record ComplianceSource(
    string Name,
    Uri Url,
    DateOnly LastReviewedAt,
    DateOnly? EffectiveAt,
    string Confidence,
    bool RequiresExpertReview);

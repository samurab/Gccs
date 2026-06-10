namespace Gccs.Domain.Common;

public sealed record ReviewMetadata(
    DateOnly LastReviewedAt,
    Guid? ReviewedByUserId,
    DateOnly? NextReviewDueAt,
    string Confidence,
    bool RequiresExpertReview,
    ReviewState State);

public enum ReviewState
{
    Draft,
    NeedsReview,
    Approved,
    Rejected,
    CustomerDisputed,
    Published,
    Retired
}

using Gccs.Domain.Common;
namespace Gccs.Infrastructure.Persistence.Models;

public sealed class ClassifiedNoteEntity : IClassifiedContentEntity, IContainableContentEntity
{
    public long ClassificationRevision { get; set; }
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public long Revision { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ContentClassification Classification { get; set; }
    public ContentClassificationSource ClassificationSource { get; set; }
    public decimal? ClassificationConfidence { get; set; }
    public Guid? ClassificationReviewedByUserId { get; set; }
    public DateTimeOffset? ClassificationReviewedAt { get; set; }
    public string? ClassificationReason { get; set; }
    public bool ClassificationIsApprovedDemoContent { get; set; }
    public bool IsUseBlocked { get; set; }
    public DateTimeOffset? UseBlockedAt { get; set; }
}

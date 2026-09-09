using Gccs.Domain.Compliance;

namespace Gccs.Infrastructure.Persistence.Models;

public sealed class SspSectionEntity : AuditedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public SspSectionType SectionType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public SspSectionStatus Status { get; set; }
    public string? Reviewer { get; set; }
    public DateOnly? ReviewDate { get; set; }
    public string? ApprovalRationale { get; set; }
    public bool IsRequired { get; set; } = true;
    public long Version { get; set; }
    public TenantEntity? Tenant { get; set; }
    public ICollection<SspSectionLinkEntity> LinkedRecords { get; set; } = [];
    public ICollection<SspSectionSourceReferenceEntity> SourceReferences { get; set; } = [];
    public ICollection<SspSectionHistoryEntity> History { get; set; } = [];
}

public sealed class SspSectionLinkEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid SectionId { get; set; }
    public SspLinkedRecordType RecordType { get; set; }
    public string RecordId { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public SspSectionEntity? Section { get; set; }
}

public sealed class SspSectionSourceReferenceEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid SectionId { get; set; }
    public string Source { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public DateOnly LastReviewedAt { get; set; }
    public SspSectionEntity? Section { get; set; }
}

public sealed class SspSectionHistoryEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid SectionId { get; set; }
    public SspSectionStatus Status { get; set; }
    public Guid ActorUserId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public DateTimeOffset ChangedAt { get; set; }
    public string? Notes { get; set; }
    public SspSectionEntity? Section { get; set; }
}

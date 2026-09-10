using Gccs.Domain.Compliance;
using Gccs.Domain.Common;

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
    public ICollection<SspNarrativeEntity> Narratives { get; set; } = [];
}

public sealed class SspNarrativeEntity : AuditedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid SectionId { get; set; }
    public string GeneratedText { get; set; } = string.Empty;
    public string? EditedText { get; set; }
    public string? ApprovedText { get; set; }
    public SspNarrativeStatus Status { get; set; }
    public bool AiAssisted { get; set; }
    public bool DraftOnly { get; set; } = true;
    public string? ReviewerNotes { get; set; }
    public Guid? ReviewerUserId { get; set; }
    public string? Reviewer { get; set; }
    public DateOnly? ReviewDate { get; set; }
    public long Version { get; set; }
    public ContentClassification Classification { get; set; }
    public ContentClassificationSource ClassificationSource { get; set; }
    public decimal? ClassificationConfidence { get; set; }
    public Guid? ClassificationReviewedByUserId { get; set; }
    public DateTimeOffset? ClassificationReviewedAt { get; set; }
    public string? ClassificationReason { get; set; }
    public bool ClassificationIsApprovedDemoContent { get; set; }
    public SspSectionEntity? Section { get; set; }
    public ICollection<SspNarrativeSourceEntity> Sources { get; set; } = [];
}

public sealed class SspNarrativeSourceEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid NarrativeId { get; set; }
    public SspNarrativeSourceType SourceType { get; set; }
    public string RecordId { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public string Fingerprint { get; set; } = string.Empty;
    public ContentClassification Classification { get; set; }
    public SspNarrativeEntity? Narrative { get; set; }
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

public sealed class SspExportPackageEntity : AuditedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public DateTimeOffset GeneratedAt { get; set; }
    public string PackageVersion { get; set; } = string.Empty;
    public string SystemBoundary { get; set; } = string.Empty;
    public string Reviewer { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public string LanguagePolicyVersion { get; set; } = string.Empty;
    public string Disclaimer { get; set; } = string.Empty;
    public string HumanReadableReport { get; set; } = string.Empty;
    public string MachineReadableMetadata { get; set; } = "{}";
    public string SectionsJson { get; set; } = "[]";
    public string EvidenceReferencesJson { get; set; } = "[]";
    public string PoamReferencesJson { get; set; } = "[]";
    public string Status { get; set; } = string.Empty;
    public Guid? ExternalShareApprovedByUserId { get; set; }
    public DateTimeOffset? ExternalShareApprovedAt { get; set; }
    public string? ExternalShareApprovalReason { get; set; }
    public Guid? SharedByUserId { get; set; }
    public DateTimeOffset? SharedAt { get; set; }
    public string? SharedRecipient { get; set; }
    public string? SharedPurpose { get; set; }
    public long Version { get; set; }
    public TenantEntity? Tenant { get; set; }
    public ICollection<SspExportPackageHistoryEntity> History { get; set; } = [];
}

public sealed class SspExportPolicyEntity : AuditedEntity
{
    public Guid TenantId { get; set; }
    public bool RequireIndependentApproval { get; set; } = true;
    public long Version { get; set; }
    public TenantEntity? Tenant { get; set; }
}

public sealed class SspExportPackageHistoryEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid PackageId { get; set; }
    public string Action { get; set; } = string.Empty;
    public Guid ActorUserId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
    public string? Notes { get; set; }
    public SspExportPackageEntity? Package { get; set; }
}

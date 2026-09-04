using Gccs.Application.Compliance;

namespace Gccs.Infrastructure.Persistence.Models;

public sealed class FedRampControlMappingEntity : AuditedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string ControlId { get; set; } = string.Empty;
    public string Family { get; set; } = string.Empty;
    public string Baseline { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public FedRampImplementationStatus ImplementationStatus { get; set; }
    public string ImplementationSummary { get; set; } = string.Empty;
    public string? InheritedProvider { get; set; }
    public string? GapRationale { get; set; }
    public string SourceReference { get; set; } = string.Empty;
    public FedRampReviewState ReviewState { get; set; }
    public string? Reviewer { get; set; }
    public DateOnly? ReviewDate { get; set; }
    public long Version { get; set; }

    public TenantEntity? Tenant { get; set; }
    public ICollection<FedRampEvidenceLinkEntity> EvidenceLinks { get; set; } = [];
    public ICollection<FedRampGapEntity> Gaps { get; set; } = [];
    public ICollection<FedRampControlMappingHistoryEntity> History { get; set; } = [];
}

public sealed class FedRampEvidenceLinkEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid MappingId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public FedRampEvidenceType EvidenceType { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedByUserId { get; set; }

    public FedRampControlMappingEntity? Mapping { get; set; }
}

public sealed class FedRampGapEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid MappingId { get; set; }
    public string Rationale { get; set; } = string.Empty;
    public FedRampGapSeverity Severity { get; set; }
    public string Owner { get; set; } = string.Empty;
    public DateOnly TargetDate { get; set; }
    public bool IsOpen { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedByUserId { get; set; }

    public FedRampControlMappingEntity? Mapping { get; set; }
}

public sealed class FedRampControlMappingHistoryEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid MappingId { get; set; }
    public FedRampReviewState PreviousState { get; set; }
    public FedRampReviewState NewState { get; set; }
    public string Reviewer { get; set; } = string.Empty;
    public DateOnly ReviewDate { get; set; }
    public string ReviewNotes { get; set; } = string.Empty;
    public DateTimeOffset ChangedAt { get; set; }
    public Guid ChangedByUserId { get; set; }

    public FedRampControlMappingEntity? Mapping { get; set; }
}

public sealed class FedRampReadinessPackageEntity : AuditedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public DateTimeOffset GeneratedAt { get; set; }
    public string PackageVersion { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public string Reviewer { get; set; } = string.Empty;
    public string AuthorizationLanguage { get; set; } = string.Empty;
    public string GapsJson { get; set; } = "[]";
    public string AcceptedRisksJson { get; set; } = "[]";
    public string ReadinessSummary { get; set; } = string.Empty;
    public FedRampReadinessPackageStatus Status { get; set; }
    public string? LastActor { get; set; }
    public DateTimeOffset? SharedAt { get; set; }
    public long Version { get; set; }

    public TenantEntity? Tenant { get; set; }
    public ICollection<FedRampPackageRecordEntity> IncludedRecords { get; set; } = [];
    public ICollection<FedRampReadinessPackageHistoryEntity> History { get; set; } = [];
}

public sealed class FedRampPackageRecordEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid PackageId { get; set; }
    public string RecordType { get; set; } = string.Empty;
    public string RecordId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public FedRampPackageRecordStatus Status { get; set; }
    public bool Restricted { get; set; }
    public bool Prohibited { get; set; }

    public FedRampReadinessPackageEntity? Package { get; set; }
}

public sealed class FedRampReadinessPackageHistoryEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid PackageId { get; set; }
    public FedRampReadinessPackageStatus PreviousStatus { get; set; }
    public FedRampReadinessPackageStatus NewStatus { get; set; }
    public string Actor { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
    public Guid ChangedByUserId { get; set; }

    public FedRampReadinessPackageEntity? Package { get; set; }
}

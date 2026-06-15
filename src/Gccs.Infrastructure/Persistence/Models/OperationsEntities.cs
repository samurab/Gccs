using Gccs.Domain.Cmmc;
using Gccs.Domain.Compliance;
using Gccs.Domain.Evidence;
using Gccs.Domain.Labor;
using Gccs.Domain.People;
using Gccs.Domain.Reports;
using Gccs.Domain.Vendors;

namespace Gccs.Infrastructure.Persistence.Models;

public sealed class EvidenceItemEntity : AuditedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public EvidenceType Type { get; set; }
    public string OwnerFunction { get; set; } = string.Empty;
    public EvidenceStatus Status { get; set; }
    public string? StorageUri { get; set; }
    public string? FileHash { get; set; }
    public string? OriginalFileName { get; set; }
    public string? ContentType { get; set; }
    public long? SizeBytes { get; set; }
    public string? UploadValidationStatus { get; set; }
    public string? MalwareScanStatus { get; set; }
    public DateOnly? EffectiveAt { get; set; }
    public DateOnly? ExpiresAt { get; set; }
    public string TagsJson { get; set; } = "[]";
    public Guid? ApprovedByUserId { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }

    public ICollection<EvidenceObligationEntity> Obligations { get; set; } = [];
    public ICollection<EvidenceContractEntity> Contracts { get; set; } = [];
    public ICollection<EvidenceControlEntity> Controls { get; set; } = [];
    public ICollection<EvidenceVendorEntity> Vendors { get; set; } = [];
    public ICollection<EvidenceEmployeeEntity> Employees { get; set; } = [];
    public ICollection<EvidenceFileVersionEntity> FileVersions { get; set; } = [];
}

public sealed class EvidenceFileVersionEntity
{
    public Guid Id { get; set; }
    public Guid EvidenceItemId { get; set; }
    public int VersionNumber { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string ValidationStatus { get; set; } = string.Empty;
    public string MalwareScanStatus { get; set; } = string.Empty;
    public string? StorageUri { get; set; }
    public string? FileHash { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
    public Guid UploadedByUserId { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }

    public EvidenceItemEntity? EvidenceItem { get; set; }
}

public sealed class EvidenceObligationEntity
{
    public Guid EvidenceItemId { get; set; }
    public string ObligationId { get; set; } = string.Empty;

    public EvidenceItemEntity? EvidenceItem { get; set; }
    public ObligationEntity? Obligation { get; set; }
}

public sealed class EvidenceContractEntity
{
    public Guid EvidenceItemId { get; set; }
    public Guid ContractId { get; set; }

    public EvidenceItemEntity? EvidenceItem { get; set; }
    public ContractEntity? Contract { get; set; }
}

public sealed class EvidenceControlEntity
{
    public Guid EvidenceItemId { get; set; }
    public string ControlId { get; set; } = string.Empty;

    public EvidenceItemEntity? EvidenceItem { get; set; }
    public ControlEntity? Control { get; set; }
}

public sealed class EvidenceVendorEntity
{
    public Guid EvidenceItemId { get; set; }
    public Guid VendorId { get; set; }

    public EvidenceItemEntity? EvidenceItem { get; set; }
    public VendorEntity? Vendor { get; set; }
}

public sealed class EvidenceEmployeeEntity
{
    public Guid EvidenceItemId { get; set; }
    public Guid EmployeeId { get; set; }

    public EvidenceItemEntity? EvidenceItem { get; set; }
    public EmployeeEntity? Employee { get; set; }
}

public sealed class ControlEntity
{
    public string Id { get; set; } = string.Empty;
    public ControlFramework Framework { get; set; }
    public CmmcLevel CmmcLevel { get; set; }
    public string Family { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Requirement { get; set; } = string.Empty;
    public string AssessmentObjective { get; set; } = string.Empty;
    public string EvidenceExamplesJson { get; set; } = "[]";
    public string SourceName { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public DateOnly SourceLastReviewedAt { get; set; }
    public DateOnly? SourceEffectiveAt { get; set; }
    public string SourceConfidence { get; set; } = "unknown";
    public bool SourceRequiresExpertReview { get; set; }
}

public sealed class AssessmentEntity : AuditedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public AssessmentType Type { get; set; }
    public CmmcLevel Level { get; set; }
    public AssessmentStatus Status { get; set; }
    public DateOnly StartedAt { get; set; }
    public DateOnly? CompletedAt { get; set; }
    public DateOnly? AffirmationDueAt { get; set; }

    public ICollection<ControlAssessmentEntity> Controls { get; set; } = [];
}

public sealed class ControlAssessmentEntity
{
    public Guid AssessmentId { get; set; }
    public string ControlId { get; set; } = string.Empty;
    public ControlImplementationStatus ImplementationStatus { get; set; }
    public AssessmentResult Result { get; set; }
    public string Notes { get; set; } = string.Empty;
    public Guid? AssessedByUserId { get; set; }
    public DateOnly? AssessedAt { get; set; }
    public string EvidenceItemIdsJson { get; set; } = "[]";

    public AssessmentEntity? Assessment { get; set; }
    public ControlEntity? Control { get; set; }
}

public sealed class PoamItemEntity : AuditedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string ControlId { get; set; } = string.Empty;
    public string Weakness { get; set; } = string.Empty;
    public string PlannedRemediation { get; set; } = string.Empty;
    public RiskLevel RiskLevel { get; set; }
    public PoamStatus Status { get; set; }
    public Guid? OwnerUserId { get; set; }
    public DateOnly TargetCompletionAt { get; set; }
    public DateOnly? CompletedAt { get; set; }

    public ICollection<PoamEvidenceEntity> EvidenceItems { get; set; } = [];
}

public sealed class PoamEvidenceEntity
{
    public Guid PoamItemId { get; set; }
    public Guid EvidenceItemId { get; set; }

    public PoamItemEntity? PoamItem { get; set; }
    public EvidenceItemEntity? EvidenceItem { get; set; }
}

public sealed class AssetEntity : AuditedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public AssetType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public string OwnerFunction { get; set; } = string.Empty;
    public bool StoresFci { get; set; }
    public bool StoresCui { get; set; }
    public Guid? SystemBoundaryId { get; set; }
    public string TagsJson { get; set; } = "[]";
}

public sealed class SystemBoundaryEntity : AuditedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public BoundaryStatus Status { get; set; }

    public ICollection<SystemBoundaryAssetEntity> Assets { get; set; } = [];
    public ICollection<SystemBoundaryExternalServiceProviderEntity> ExternalServiceProviders { get; set; } = [];
    public ICollection<SystemBoundaryEvidenceEntity> EvidenceItems { get; set; } = [];
}

public sealed class SystemBoundaryAssetEntity
{
    public Guid SystemBoundaryId { get; set; }
    public Guid AssetId { get; set; }

    public SystemBoundaryEntity? SystemBoundary { get; set; }
    public AssetEntity? Asset { get; set; }
}

public sealed class SystemBoundaryExternalServiceProviderEntity
{
    public Guid SystemBoundaryId { get; set; }
    public Guid VendorId { get; set; }

    public SystemBoundaryEntity? SystemBoundary { get; set; }
    public VendorEntity? Vendor { get; set; }
}

public sealed class SystemBoundaryEvidenceEntity
{
    public Guid SystemBoundaryId { get; set; }
    public Guid EvidenceItemId { get; set; }

    public SystemBoundaryEntity? SystemBoundary { get; set; }
    public EvidenceItemEntity? EvidenceItem { get; set; }
}

public sealed class AnnualAffirmationEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public CmmcLevel Level { get; set; }
    public DateOnly DueAt { get; set; }
    public DateOnly? SubmittedAt { get; set; }
    public Guid? SubmittedByUserId { get; set; }
    public string? ConfirmationReference { get; set; }
    public AffirmationStatus Status { get; set; }
}

public sealed class VendorEntity : AuditedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public VendorType Type { get; set; }
    public VendorRiskLevel RiskLevel { get; set; }
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactTitle { get; set; }
    public bool HasFciAccess { get; set; }
    public bool HasCuiAccess { get; set; }
}

public sealed class SubcontractorEntity : AuditedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Uei { get; set; }
    public string? CageCode { get; set; }
    public SubcontractorStatus Status { get; set; }
    public string WorkshareDescription { get; set; } = string.Empty;
    public decimal? WorksharePercentage { get; set; }
    public bool HasFciAccess { get; set; }
    public bool HasCuiAccess { get; set; }
    public string? RequiredCmmcLevel { get; set; }
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactTitle { get; set; }

    public ICollection<FlowDownClauseEntity> FlowDownClauses { get; set; } = [];
    public ICollection<ContractSubcontractorEntity> Contracts { get; set; } = [];
    public ICollection<SubcontractorEvidenceEntity> EvidenceItems { get; set; } = [];
}

public sealed class FlowDownClauseEntity
{
    public Guid Id { get; set; }
    public Guid SubcontractorId { get; set; }
    public string ClauseNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public FlowDownStatus Status { get; set; }
    public DateOnly? SentAt { get; set; }
    public DateOnly? SignedAt { get; set; }
    public Guid? SignedEvidenceItemId { get; set; }

    public SubcontractorEntity? Subcontractor { get; set; }
}

public sealed class ContractSubcontractorEntity
{
    public Guid ContractId { get; set; }
    public Guid SubcontractorId { get; set; }

    public ContractEntity? Contract { get; set; }
    public SubcontractorEntity? Subcontractor { get; set; }
}

public sealed class SubcontractorEvidenceEntity
{
    public Guid SubcontractorId { get; set; }
    public Guid EvidenceItemId { get; set; }

    public SubcontractorEntity? Subcontractor { get; set; }
    public EvidenceItemEntity? EvidenceItem { get; set; }
}

public sealed class EmployeeEntity : AuditedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public EmploymentStatus Status { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string LaborCategory { get; set; } = string.Empty;
    public bool HandlesFci { get; set; }
    public bool HandlesCui { get; set; }
}

public sealed class TrainingRecordEntity : AuditedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid EmployeeId { get; set; }
    public string TrainingName { get; set; } = string.Empty;
    public TrainingType Type { get; set; }
    public TrainingStatus Status { get; set; }
    public DateOnly AssignedAt { get; set; }
    public DateOnly? CompletedAt { get; set; }
    public DateOnly? ExpiresAt { get; set; }
    public Guid? EvidenceItemId { get; set; }
}

public sealed class WageDeterminationEntity : AuditedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string DeterminationNumber { get; set; } = string.Empty;
    public string Revision { get; set; } = string.Empty;
    public string PlaceOfPerformance { get; set; } = string.Empty;
    public DateOnly EffectiveAt { get; set; }
    public DateOnly? ExpiresAt { get; set; }
    public string? SourceUrl { get; set; }

    public ICollection<LaborCategoryRateEntity> Rates { get; set; } = [];
}

public sealed class LaborCategoryRateEntity
{
    public Guid Id { get; set; }
    public Guid WageDeterminationId { get; set; }
    public string LaborCategory { get; set; } = string.Empty;
    public decimal HourlyWage { get; set; }
    public decimal FringeBenefitRate { get; set; }
    public string Currency { get; set; } = "USD";

    public WageDeterminationEntity? WageDetermination { get; set; }
}

public sealed class LaborClassificationEntity : AuditedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid ContractId { get; set; }
    public string LaborCategory { get; set; } = string.Empty;
    public string BasisForClassification { get; set; } = string.Empty;
    public Guid? WageDeterminationId { get; set; }
    public Guid? EvidenceItemId { get; set; }
}

public sealed class PayrollRecordEntity : AuditedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid ContractId { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public decimal HoursWorked { get; set; }
    public decimal WagePaid { get; set; }
    public decimal FringePaid { get; set; }
    public Guid? EvidenceItemId { get; set; }
}

public sealed class ReportEntity : AuditedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public ReportType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public ReportStatus Status { get; set; }
    public DateTimeOffset GeneratedAt { get; set; }
    public Guid GeneratedByUserId { get; set; }
    public string? StorageUri { get; set; }

    public ICollection<ReportContractEntity> Contracts { get; set; } = [];
    public ICollection<ReportObligationEntity> Obligations { get; set; } = [];
    public ICollection<ReportEvidenceEntity> EvidenceItems { get; set; } = [];
}

public sealed class ReportContractEntity
{
    public Guid ReportId { get; set; }
    public Guid ContractId { get; set; }
}

public sealed class ReportObligationEntity
{
    public Guid ReportId { get; set; }
    public string ObligationId { get; set; } = string.Empty;
}

public sealed class ReportEvidenceEntity
{
    public Guid ReportId { get; set; }
    public Guid EvidenceItemId { get; set; }
}

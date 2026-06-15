using Gccs.Domain.Audit;
using Gccs.Domain.Companies;
using Gccs.Domain.Compliance;
using Gccs.Domain.Common;
using Gccs.Domain.Contracts;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;

namespace Gccs.Infrastructure.Persistence.Models;

public abstract class AuditedEntity
{
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
}

public sealed class TenantEntity : AuditedEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public TenantStatus Status { get; set; }
    public TenantDataPosture DataPosture { get; set; }
    public DateOnly? TrialEndsAt { get; set; }

    public ICollection<UserEntity> Users { get; set; } = [];
    public ICollection<TenantMembershipEntity> Memberships { get; set; } = [];
    public ICollection<TenantInvitationEntity> Invitations { get; set; } = [];
    public ICollection<RoleEntity> Roles { get; set; } = [];
}

public sealed class UserEntity : AuditedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public UserStatus Status { get; set; }
    public bool MfaEnabled { get; set; }
    public DateTimeOffset? LastSignedInAt { get; set; }

    public TenantEntity? Tenant { get; set; }
    public ICollection<UserRoleEntity> UserRoles { get; set; } = [];
    public ICollection<TenantMembershipEntity> Memberships { get; set; } = [];
}

public sealed class TenantMembershipEntity : AuditedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public MembershipStatus Status { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public DateTimeOffset? LastAccessedAt { get; set; }

    public TenantEntity? Tenant { get; set; }
    public UserEntity? User { get; set; }
}

public sealed class TenantInvitationEntity : AuditedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string InvitationToken { get; set; } = string.Empty;
    public TenantInvitationStatus Status { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
    public Guid? AcceptedByUserId { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public Guid? RevokedByUserId { get; set; }
    public DateTimeOffset? NotificationSentAt { get; set; }
    public string NotificationPlaceholder { get; set; } = string.Empty;

    public TenantEntity? Tenant { get; set; }
}

public sealed class NoCuiAcknowledgementEntity : AuditedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public string NoticeVersion { get; set; } = string.Empty;
    public string NoticeCopy { get; set; } = string.Empty;
    public DateTimeOffset AcknowledgedAt { get; set; }
}

public sealed class NotificationPreferenceEntity : AuditedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public bool AssignmentNotificationsEnabled { get; set; }
    public bool DueSoonNotificationsEnabled { get; set; }
    public bool OverdueNotificationsEnabled { get; set; }
    public bool EvidenceRequestNotificationsEnabled { get; set; }
    public bool CertificationRenewalNotificationsEnabled { get; set; }
    public bool CmmcAffirmationNotificationsEnabled { get; set; }
}

public sealed class RoleEntity : AuditedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;

    public TenantEntity? Tenant { get; set; }
    public ICollection<UserRoleEntity> UserRoles { get; set; } = [];
    public ICollection<RolePermissionEntity> Permissions { get; set; } = [];
}

public sealed class UserRoleEntity
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }

    public UserEntity? User { get; set; }
    public RoleEntity? Role { get; set; }
}

public sealed class RolePermissionEntity
{
    public Guid RoleId { get; set; }
    public Permission Permission { get; set; }

    public RoleEntity? Role { get; set; }
}

public sealed class CompanyProfileEntity : AuditedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string LegalEntityName { get; set; } = string.Empty;
    public string? DoingBusinessAs { get; set; }
    public string? Uei { get; set; }
    public string? CageCode { get; set; }
    public DateOnly? SamRegistrationExpiresAt { get; set; }
    public ContractorRole ContractorRole { get; set; }
    public string ProductsAndServices { get; set; } = string.Empty;
    public CompanyRange EmployeeRange { get; set; }
    public CompanyRange RevenueRange { get; set; }
    public string ItEnvironmentDescription { get; set; } = string.Empty;
    public bool UsesExternalServiceProvider { get; set; }
    public string? ExternalServiceProviderName { get; set; }
    public string KeySystemsJson { get; set; } = "[]";
    public string AgencyCustomersJson { get; set; } = "[]";
    public DataHandlingPosture DataHandlingPosture { get; set; }

    public TenantEntity? Tenant { get; set; }
    public ICollection<CompanyNaicsCodeEntity> NaicsCodes { get; set; } = [];
    public ICollection<CompanyCertificationEntity> Certifications { get; set; } = [];
    public ICollection<CompanyLocationEntity> Locations { get; set; } = [];
}

public sealed class CompanyNaicsCodeEntity
{
    public Guid Id { get; set; }
    public Guid CompanyProfileId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public string? SizeStandard { get; set; }
    public bool? QualifiesAsSmall { get; set; }
    public DateOnly? LastCheckedAt { get; set; }

    public CompanyProfileEntity? CompanyProfile { get; set; }
}

public sealed class CompanyCertificationEntity
{
    public Guid Id { get; set; }
    public Guid CompanyProfileId { get; set; }
    public CertificationType Type { get; set; }
    public CertificationStatus Status { get; set; }
    public string Issuer { get; set; } = string.Empty;
    public DateOnly? EffectiveAt { get; set; }
    public DateOnly? ExpiresAt { get; set; }
    public string? ReferenceNumber { get; set; }

    public CompanyProfileEntity? CompanyProfile { get; set; }
}

public sealed class CompanyLocationEntity
{
    public Guid Id { get; set; }
    public Guid CompanyProfileId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Street1 { get; set; } = string.Empty;
    public string? Street2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string StateOrProvince { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public bool IsPlaceOfPerformance { get; set; }

    public CompanyProfileEntity? CompanyProfile { get; set; }
}

public sealed class AuditLogEntryEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? ActorUserId { get; set; }
    public AuditAction Action { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string MetadataJson { get; set; } = "{}";
}

public sealed class ComplianceTaskEntity : AuditedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ComplianceTaskType Type { get; set; }
    public ComplianceTaskStatus Status { get; set; }
    public RiskLevel RiskLevel { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public string OwnerFunction { get; set; } = string.Empty;
    public DateOnly? DueAt { get; set; }
    public Guid? ContractId { get; set; }
    public string? ObligationId { get; set; }
    public string? ControlId { get; set; }
    public Guid? EvidenceItemId { get; set; }
}

public sealed class ContractEntity : AuditedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string AgencyOrPrimeName { get; set; } = string.Empty;
    public ContractorRelationship Relationship { get; set; }
    public ContractKind Kind { get; set; }
    public ContractStatus Status { get; set; }
    public DateOnly? AwardedAt { get; set; }
    public DateOnly PeriodOfPerformanceStart { get; set; }
    public DateOnly PeriodOfPerformanceEnd { get; set; }
    public string PlaceOfPerformance { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DataHandlingPosture DataHandlingPosture { get; set; }

    public ICollection<ContractDocumentEntity> Documents { get; set; } = [];
    public ICollection<ContractClauseEntity> Clauses { get; set; } = [];
    public ICollection<ContractDeliverableEntity> Deliverables { get; set; } = [];
    public ICollection<ContractReportingDeadlineEntity> ReportingDeadlines { get; set; } = [];
    public ICollection<ContractSubcontractorEntity> Subcontractors { get; set; } = [];
}

public sealed class SolicitationEntity : AuditedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string SolicitationNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Agency { get; set; } = string.Empty;
    public DateOnly? ResponseDueAt { get; set; }
    public ContractKind ExpectedContractKind { get; set; }
    public string SetAside { get; set; } = string.Empty;
}

public sealed class ContractDocumentEntity
{
    public Guid Id { get; set; }
    public Guid ContractId { get; set; }
    public ContractDocumentType Type { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string? StorageUri { get; set; }
    public string? ExtractedTextHash { get; set; }
    public string ValidationStatus { get; set; } = string.Empty;
    public string MalwareScanStatus { get; set; } = string.Empty;
    public string NoticeVersion { get; set; } = string.Empty;
    public DateTimeOffset UploadedAt { get; set; }
    public Guid UploadedByUserId { get; set; }
    public bool ContainsPotentialCui { get; set; }

    public ContractEntity? Contract { get; set; }
}

public sealed class ContractClauseEntity : AuditedEntity
{
    public Guid Id { get; set; }
    public Guid ContractId { get; set; }
    public string ClauseLibraryId { get; set; } = string.Empty;
    public string ClauseNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Alternate { get; set; }
    public string? FullText { get; set; }
    public ClauseSource Source { get; set; }
    public string SourceUrl { get; set; } = string.Empty;
    public string? SourceHash { get; set; }
    public string AttachmentReason { get; set; } = string.Empty;
    public string? SourceDocumentReference { get; set; }
    public DateTimeOffset? RemovedAt { get; set; }
    public Guid? RemovedByUserId { get; set; }
    public string? RemovalReason { get; set; }
    public bool RequiresFlowDown { get; set; }
    public DateOnly LastReviewedAt { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public DateOnly? NextReviewDueAt { get; set; }
    public string Confidence { get; set; } = "unknown";
    public bool RequiresExpertReview { get; set; }
    public ReviewState ReviewState { get; set; } = ReviewState.Draft;

    public ContractEntity? Contract { get; set; }
    public ICollection<ContractClauseObligationEntity> Obligations { get; set; } = [];
}

public sealed class ContractClauseObligationEntity
{
    public Guid ContractClauseId { get; set; }
    public string ObligationId { get; set; } = string.Empty;

    public ContractClauseEntity? ContractClause { get; set; }
    public ObligationEntity? Obligation { get; set; }
}

public sealed class ContractDeliverableEntity
{
    public Guid Id { get; set; }
    public Guid ContractId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateOnly? DueAt { get; set; }
    public string OwnerFunction { get; set; } = string.Empty;
    public DeliverableStatus Status { get; set; }

    public ContractEntity? Contract { get; set; }
}

public sealed class ContractReportingDeadlineEntity
{
    public Guid Id { get; set; }
    public Guid ContractId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateOnly DueAt { get; set; }
    public RecurrencePattern Recurrence { get; set; }
    public string OwnerFunction { get; set; } = string.Empty;
    public string SourceClauseNumbersJson { get; set; } = "[]";

    public ContractEntity? Contract { get; set; }
}

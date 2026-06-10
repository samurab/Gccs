using Gccs.Domain.Common;

namespace Gccs.Domain.Companies;

public sealed record CompanyProfile(
    Guid Id,
    Guid TenantId,
    string LegalEntityName,
    string? DoingBusinessAs,
    string? Uei,
    string? CageCode,
    DateOnly? SamRegistrationExpiresAt,
    IReadOnlyList<NaicsCode> NaicsCodes,
    IReadOnlyList<Certification> Certifications,
    IReadOnlyList<string> AgencyCustomers,
    ContractorRole ContractorRole,
    string ProductsAndServices,
    CompanyRange EmployeeRange,
    CompanyRange RevenueRange,
    IReadOnlyList<PhysicalLocation> Locations,
    ItEnvironmentSummary ItEnvironment,
    DataHandlingPosture DataHandlingPosture,
    EntityAudit Audit);

public sealed record NaicsCode(
    string Code,
    string Title,
    bool IsPrimary,
    string? SizeStandard,
    bool? QualifiesAsSmall,
    DateOnly? LastCheckedAt);

public sealed record Certification(
    Guid Id,
    CertificationType Type,
    CertificationStatus Status,
    string Issuer,
    DateOnly? EffectiveAt,
    DateOnly? ExpiresAt,
    string? ReferenceNumber);

public sealed record ItEnvironmentSummary(
    string Description,
    bool UsesExternalServiceProvider,
    string? ExternalServiceProviderName,
    IReadOnlyList<string> KeySystems);

public enum ContractorRole
{
    Unknown,
    Prime,
    Subcontractor,
    Both
}

public enum CompanyRange
{
    Unknown,
    Micro,
    Small,
    MidSize,
    Large
}

public enum DataHandlingPosture
{
    Unknown,
    NoFciOrCui,
    FciOnly,
    Cui,
    Classified,
    ExportControlled
}

public enum CertificationType
{
    EightA,
    Wosb,
    Edwosb,
    HubZone,
    Sdvosb,
    Sdb,
    Other
}

public enum CertificationStatus
{
    Draft,
    Active,
    ExpiringSoon,
    Expired,
    Revoked,
    Unknown
}

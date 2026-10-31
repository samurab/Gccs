using Gccs.Domain.Companies;

namespace Gccs.Application.Companies;

public sealed record CompanyProfileDto(
    Guid Id,
    Guid TenantId,
    string LegalEntityName,
    string? DoingBusinessAs,
    string? Uei,
    string? CageCode,
    DateOnly? SamRegistrationExpiresAt,
    IReadOnlyList<CompanyNaicsCodeDto> NaicsCodes,
    IReadOnlyList<CompanyCertificationDto> Certifications,
    IReadOnlyList<string> AgencyCustomers,
    ContractorRole ContractorRole,
    string ProductsAndServices,
    CompanyRange EmployeeRange,
    CompanyRange RevenueRange,
    IReadOnlyList<CompanyLocationDto> Locations,
    ItEnvironmentSummaryDto ItEnvironment,
    DataHandlingPosture DataHandlingPosture,
    int CompletionPercentage,
    bool IsComplete,
    IReadOnlyDictionary<string, string[]> ValidationErrors,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record UpsertCompanyProfileRequest(
    string LegalEntityName,
    string? DoingBusinessAs,
    string? Uei,
    string? CageCode,
    DateOnly? SamRegistrationExpiresAt,
    IReadOnlyList<CompanyNaicsCodeDto> NaicsCodes,
    IReadOnlyList<CompanyCertificationDto> Certifications,
    IReadOnlyList<string> AgencyCustomers,
    ContractorRole ContractorRole,
    string ProductsAndServices,
    CompanyRange EmployeeRange,
    CompanyRange RevenueRange,
    IReadOnlyList<CompanyLocationDto> Locations,
    ItEnvironmentSummaryDto ItEnvironment,
    DataHandlingPosture DataHandlingPosture,
    bool CompleteProfile);

public sealed record CompanyNaicsCodeDto(
    string Code,
    string Title,
    bool IsPrimary,
    string? SizeStandard,
    bool? QualifiesAsSmall,
    DateOnly? LastCheckedAt);

public sealed record CompanyCertificationDto(
    Guid? Id,
    CertificationType Type,
    CertificationStatus Status,
    string Issuer,
    DateOnly? EffectiveAt,
    DateOnly? ExpiresAt,
    string? ReferenceNumber);

public sealed record CompanyLocationDto(
    string Name,
    string Street1,
    string? Street2,
    string City,
    string StateOrProvince,
    string PostalCode,
    string Country,
    bool IsPlaceOfPerformance);

public sealed record ItEnvironmentSummaryDto(
    string Description,
    bool UsesExternalServiceProvider,
    string? ExternalServiceProviderName,
    IReadOnlyList<string> KeySystems);

using Gccs.Application.Audit;
using Gccs.Domain.Audit;
using Gccs.Domain.Companies;

namespace Gccs.Application.Companies;

public sealed class CompanyProfileService(
    ICompanyProfileRepository repository,
    IAuditEventWriter auditEventWriter)
{
    public async Task<CompanyProfileDto?> GetCurrentTenantProfileAsync(CancellationToken cancellationToken = default)
    {
        var profile = await repository.FindCurrentTenantProfileAsync(cancellationToken);
        return profile is null ? null : WithCompletion(profile);
    }

    public async Task<CompanyProfileDto> SaveCurrentTenantProfileAsync(
        UpsertCompanyProfileRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(request);
        var validationErrors = Validate(normalized);

        if (request.CompleteProfile && validationErrors.Count > 0)
        {
            throw new CompanyProfileValidationException(validationErrors);
        }

        var saved = await repository.UpsertCurrentTenantProfileAsync(normalized, actorUserId, cancellationToken);
        var completed = WithCompletion(saved);
        var wasCreated = completed.UpdatedAt is null;

        await auditEventWriter.WriteAsync(
            completed.TenantId,
            actorUserId,
            wasCreated ? AuditAction.Created : AuditAction.Updated,
            "CompanyProfile",
            completed.Id.ToString(),
            wasCreated
                ? $"Company profile '{completed.LegalEntityName}' was created."
                : $"Company profile '{completed.LegalEntityName}' was updated.",
            new Dictionary<string, string>
            {
                ["legalEntityName"] = completed.LegalEntityName,
                ["completionPercentage"] = completed.CompletionPercentage.ToString(),
                ["isComplete"] = completed.IsComplete.ToString(),
                ["dataHandlingPosture"] = completed.DataHandlingPosture.ToString()
            },
            cancellationToken);

        return completed;
    }

    private static CompanyProfileDto WithCompletion(CompanyProfileDto profile)
    {
        var errors = Validate(ToRequest(profile));
        var completionPercentage = CalculateCompletionPercentage(profile);
        return profile with
        {
            CompletionPercentage = completionPercentage,
            IsComplete = errors.Count == 0,
            ValidationErrors = errors
        };
    }

    private static UpsertCompanyProfileRequest Normalize(UpsertCompanyProfileRequest request) =>
        request with
        {
            LegalEntityName = request.LegalEntityName.Trim(),
            DoingBusinessAs = NormalizeOptional(request.DoingBusinessAs),
            Uei = NormalizeOptional(request.Uei)?.ToUpperInvariant(),
            CageCode = NormalizeOptional(request.CageCode)?.ToUpperInvariant(),
            ProductsAndServices = request.ProductsAndServices.Trim(),
            NaicsCodes = request.NaicsCodes.Select(naics => naics with
            {
                Code = naics.Code.Trim(),
                Title = naics.Title.Trim(),
                SizeStandard = NormalizeOptional(naics.SizeStandard)
            }).Where(naics => !string.IsNullOrWhiteSpace(naics.Code)).ToArray(),
            Certifications = request.Certifications.Select(certification => certification with
            {
                Issuer = certification.Issuer.Trim(),
                ReferenceNumber = NormalizeOptional(certification.ReferenceNumber)
            }).Where(certification => !string.IsNullOrWhiteSpace(certification.Issuer)).ToArray(),
            AgencyCustomers = request.AgencyCustomers
                .Select(customer => customer.Trim())
                .Where(customer => !string.IsNullOrWhiteSpace(customer))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            Locations = request.Locations.Select(location => location with
            {
                Name = location.Name.Trim(),
                Street1 = location.Street1.Trim(),
                Street2 = NormalizeOptional(location.Street2),
                City = location.City.Trim(),
                StateOrProvince = location.StateOrProvince.Trim(),
                PostalCode = location.PostalCode.Trim(),
                Country = location.Country.Trim()
            }).Where(location => !string.IsNullOrWhiteSpace(location.Name)).ToArray(),
            ItEnvironment = request.ItEnvironment with
            {
                Description = request.ItEnvironment.Description.Trim(),
                ExternalServiceProviderName = NormalizeOptional(request.ItEnvironment.ExternalServiceProviderName),
                KeySystems = request.ItEnvironment.KeySystems
                    .Select(system => system.Trim())
                    .Where(system => !string.IsNullOrWhiteSpace(system))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray()
            }
        };

    private static IReadOnlyDictionary<string, string[]> Validate(UpsertCompanyProfileRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        AddIf(errors, string.IsNullOrWhiteSpace(request.LegalEntityName), "legalEntityName", "Legal entity name is required.");
        AddIf(errors, request.LegalEntityName.Length > 240, "legalEntityName", "Legal entity name must be 240 characters or fewer.");
        AddIf(errors, string.IsNullOrWhiteSpace(request.Uei), "uei", "UEI is required before profile completion.");
        AddIf(errors, string.IsNullOrWhiteSpace(request.CageCode), "cageCode", "CAGE code is required before profile completion.");
        AddIf(errors, request.SamRegistrationExpiresAt is null, "samRegistrationExpiresAt", "SAM expiration date is required before profile completion.");
        AddIf(errors, request.NaicsCodes.Count == 0, "naicsCodes", "At least one NAICS code is required before profile completion.");
        AddIf(errors, request.ContractorRole is ContractorRole.Unknown, "contractorRole", "Contractor role is required before profile completion.");
        AddIf(errors, string.IsNullOrWhiteSpace(request.ProductsAndServices), "productsAndServices", "Products and services are required before profile completion.");
        AddIf(errors, request.EmployeeRange is CompanyRange.Unknown, "employeeRange", "Employee range is required before profile completion.");
        AddIf(errors, request.RevenueRange is CompanyRange.Unknown, "revenueRange", "Revenue range is required before profile completion.");
        AddIf(errors, request.Locations.Count == 0, "locations", "At least one location is required before profile completion.");
        AddIf(errors, string.IsNullOrWhiteSpace(request.ItEnvironment.Description), "itEnvironment.description", "IT environment summary is required before profile completion.");
        AddIf(errors, request.DataHandlingPosture is DataHandlingPosture.Unknown, "dataHandlingPosture", "FCI/CUI posture is required before profile completion.");

        return errors;
    }

    private static int CalculateCompletionPercentage(CompanyProfileDto profile)
    {
        var completed = 0;
        const int total = 13;

        completed += string.IsNullOrWhiteSpace(profile.LegalEntityName) ? 0 : 1;
        completed += string.IsNullOrWhiteSpace(profile.Uei) ? 0 : 1;
        completed += string.IsNullOrWhiteSpace(profile.CageCode) ? 0 : 1;
        completed += profile.SamRegistrationExpiresAt is null ? 0 : 1;
        completed += profile.NaicsCodes.Count == 0 ? 0 : 1;
        completed += profile.ContractorRole is ContractorRole.Unknown ? 0 : 1;
        completed += string.IsNullOrWhiteSpace(profile.ProductsAndServices) ? 0 : 1;
        completed += profile.EmployeeRange is CompanyRange.Unknown ? 0 : 1;
        completed += profile.RevenueRange is CompanyRange.Unknown ? 0 : 1;
        completed += profile.Locations.Count == 0 ? 0 : 1;
        completed += string.IsNullOrWhiteSpace(profile.ItEnvironment.Description) ? 0 : 1;
        completed += profile.DataHandlingPosture is DataHandlingPosture.Unknown ? 0 : 1;
        completed += profile.AgencyCustomers.Count == 0 && profile.Certifications.Count == 0 ? 0 : 1;

        return (int)Math.Round(completed / (double)total * 100, MidpointRounding.AwayFromZero);
    }

    private static UpsertCompanyProfileRequest ToRequest(CompanyProfileDto profile) =>
        new(
            profile.LegalEntityName,
            profile.DoingBusinessAs,
            profile.Uei,
            profile.CageCode,
            profile.SamRegistrationExpiresAt,
            profile.NaicsCodes,
            profile.Certifications,
            profile.AgencyCustomers,
            profile.ContractorRole,
            profile.ProductsAndServices,
            profile.EmployeeRange,
            profile.RevenueRange,
            profile.Locations,
            profile.ItEnvironment,
            profile.DataHandlingPosture,
            profile.IsComplete);

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void AddIf(IDictionary<string, string[]> errors, bool condition, string field, string message)
    {
        if (condition)
        {
            errors[field] = [message];
        }
    }
}

public interface ICompanyProfileRepository
{
    Task<CompanyProfileDto?> FindCurrentTenantProfileAsync(CancellationToken cancellationToken = default);

    Task<CompanyProfileDto> UpsertCurrentTenantProfileAsync(
        UpsertCompanyProfileRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}

public sealed class CompanyProfileValidationException(IReadOnlyDictionary<string, string[]> errors)
    : InvalidOperationException("Company profile is missing required completion fields.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}

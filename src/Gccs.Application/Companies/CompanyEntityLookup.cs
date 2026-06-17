using System.Text.Json;
using Gccs.Application.Audit;
using Gccs.Application.SamGov;
using Gccs.Domain.Audit;
using Gccs.Domain.Companies;

namespace Gccs.Application.Companies;

public sealed class CompanyEntityLookupService(
    ISamGovEntityLookupClient samGovClient,
    ICompanyProfileRepository repository,
    IAuditEventWriter auditEventWriter)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<CompanyEntityLookupResultDto>> SearchAsync(
        CompanyEntityLookupRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalized = request with
        {
            Uei = NormalizeOptional(request.Uei)?.ToUpperInvariant(),
            LegalBusinessName = NormalizeOptional(request.LegalBusinessName)
        };
        if (string.IsNullOrWhiteSpace(normalized.Uei) && string.IsNullOrWhiteSpace(normalized.LegalBusinessName))
        {
            throw new CompanyEntityLookupValidationException("Provide a UEI or legal business name.");
        }

        var result = await samGovClient.SearchAsync(
            new SamGovEntitySearchRequest(normalized.Uei, normalized.LegalBusinessName),
            cancellationToken);
        if (!result.IsSuccess || string.IsNullOrWhiteSpace(result.PayloadJson))
        {
            throw new CompanyEntityLookupUnavailableException(result.Error?.Message ?? "SAM.gov lookup is unavailable.");
        }

        return ParseResults(result.PayloadJson, DateTimeOffset.UtcNow);
    }

    public async Task<CompanyProfileDto> ApplyAsync(
        ApplyCompanyEntityLookupRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var profile = await repository.FindCurrentTenantProfileAsync(cancellationToken);
        var merged = Merge(profile, request, out var changedFields, out var conflicts);
        if (conflicts.Count > 0 && !request.ConfirmOverwrite)
        {
            throw new CompanyEntityLookupConflictException(conflicts);
        }

        var saved = await repository.UpsertCurrentTenantProfileAsync(merged, actorUserId, cancellationToken);
        await auditEventWriter.WriteAsync(
            saved.TenantId,
            actorUserId,
            AuditAction.Updated,
            "CompanyProfileSamLookup",
            saved.Id.ToString(),
            $"Applied SAM.gov entity data to company profile '{saved.LegalEntityName}'.",
            new Dictionary<string, string>
            {
                ["source"] = request.Result.Source,
                ["retrievedAt"] = request.Result.RetrievedAt.ToString("O"),
                ["fields"] = string.Join(",", changedFields)
            },
            cancellationToken);

        return saved;
    }

    private static UpsertCompanyProfileRequest Merge(
        CompanyProfileDto? profile,
        ApplyCompanyEntityLookupRequest request,
        out IReadOnlyList<string> changedFields,
        out IReadOnlyDictionary<string, string[]> conflicts)
    {
        var result = request.Result;
        var selected = request.SelectedFields.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var changes = new List<string>();
        var conflictBuilder = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var merged = profile is null
            ? EmptyProfileRequest()
            : ToRequest(profile);

        merged = ApplyString(merged, selected, "legalEntityName", result.LegalBusinessName, profile?.LegalEntityName, conflictBuilder, changes, request.ConfirmOverwrite);
        merged = ApplyString(merged, selected, "uei", result.Uei, profile?.Uei, conflictBuilder, changes, request.ConfirmOverwrite);
        merged = ApplyString(merged, selected, "cageCode", result.CageCode, profile?.CageCode, conflictBuilder, changes, request.ConfirmOverwrite);
        if (selected.Contains("samRegistrationExpiresAt") && result.SamRegistrationExpiresAt is { } expiresAt)
        {
            if (profile?.SamRegistrationExpiresAt is { } existing && existing != expiresAt && !request.ConfirmOverwrite)
            {
                conflictBuilder["samRegistrationExpiresAt"] = [$"Existing value '{existing:yyyy-MM-dd}' differs from SAM.gov value '{expiresAt:yyyy-MM-dd}'."];
            }
            else
            {
                merged = merged with { SamRegistrationExpiresAt = expiresAt };
                changes.Add("samRegistrationExpiresAt");
            }
        }

        if (selected.Contains("address") && result.Address is { } address)
        {
            merged = merged with
            {
                Locations =
                [
                    new CompanyLocationDto(
                        "SAM.gov physical address",
                        address.Street1,
                        address.Street2,
                        address.City,
                        address.StateOrProvince,
                        address.PostalCode,
                        address.Country,
                        true)
                ]
            };
            changes.Add("address");
        }

        if (selected.Contains("naics") && result.NaicsCodes.Count > 0)
        {
            merged = merged with { NaicsCodes = result.NaicsCodes };
            changes.Add("naics");
        }

        changedFields = changes.Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToArray();
        conflicts = conflictBuilder;
        return merged;
    }

    private static UpsertCompanyProfileRequest ApplyString(
        UpsertCompanyProfileRequest request,
        ISet<string> selected,
        string field,
        string? samValue,
        string? existingValue,
        IDictionary<string, string[]> conflicts,
        ICollection<string> changes,
        bool confirmOverwrite)
    {
        if (!selected.Contains(field) || string.IsNullOrWhiteSpace(samValue))
        {
            return request;
        }

        if (!string.IsNullOrWhiteSpace(existingValue) &&
            !string.Equals(existingValue, samValue, StringComparison.OrdinalIgnoreCase) &&
            !confirmOverwrite)
        {
            conflicts[field] = [$"Existing value '{existingValue}' differs from SAM.gov value '{samValue}'."];
            return request;
        }

        changes.Add(field);
        return field switch
        {
            "legalEntityName" => request with { LegalEntityName = samValue },
            "uei" => request with { Uei = samValue.ToUpperInvariant() },
            "cageCode" => request with { CageCode = samValue.ToUpperInvariant() },
            _ => request
        };
    }

    private static IReadOnlyList<CompanyEntityLookupResultDto> ParseResults(string payloadJson, DateTimeOffset retrievedAt)
    {
        using var document = JsonDocument.Parse(payloadJson);
        var root = document.RootElement;
        if (root.TryGetProperty("entityData", out var entityData) && entityData.ValueKind == JsonValueKind.Array)
        {
            return entityData.EnumerateArray().Select(item => ParseEntity(item, retrievedAt)).ToArray();
        }

        if (root.TryGetProperty("entityRegistration", out var registration))
        {
            return [ParseEntity(registration, retrievedAt)];
        }

        return [ParseEntity(root, retrievedAt)];
    }

    private static CompanyEntityLookupResultDto ParseEntity(JsonElement element, DateTimeOffset retrievedAt) =>
        new(
            ReadString(element, "legalBusinessName") ?? ReadString(element, "legalEntityName") ?? string.Empty,
            ReadString(element, "ueiSAM") ?? ReadString(element, "uei") ?? string.Empty,
            ReadString(element, "cageCode"),
            ReadString(element, "registrationStatus") ?? ReadString(element, "status"),
            ReadDate(element, "expirationDate") ?? ReadDate(element, "samRegistrationExpiresAt"),
            ReadAddress(element),
            ReadNaics(element),
            "SAM.gov",
            retrievedAt);

    private static CompanyEntityAddressDto? ReadAddress(JsonElement element)
    {
        var address = element.TryGetProperty("physicalAddress", out var physicalAddress) ? physicalAddress : element;
        var street1 = ReadString(address, "addressLine1") ?? ReadString(address, "street1");
        var city = ReadString(address, "city");
        if (string.IsNullOrWhiteSpace(street1) || string.IsNullOrWhiteSpace(city))
        {
            return null;
        }

        return new CompanyEntityAddressDto(
            street1,
            ReadString(address, "addressLine2"),
            city,
            ReadString(address, "stateOrProvince") ?? ReadString(address, "state") ?? string.Empty,
            ReadString(address, "zipCode") ?? ReadString(address, "postalCode") ?? string.Empty,
            ReadString(address, "countryCode") ?? ReadString(address, "country") ?? "US");
    }

    private static IReadOnlyList<CompanyNaicsCodeDto> ReadNaics(JsonElement element)
    {
        if (!element.TryGetProperty("naicsCode", out var naics) && !element.TryGetProperty("naicsCodes", out naics))
        {
            return [];
        }

        if (naics.ValueKind == JsonValueKind.String)
        {
            return [new CompanyNaicsCodeDto(naics.GetString() ?? string.Empty, string.Empty, true, null, null, DateOnly.FromDateTime(DateTime.UtcNow))];
        }

        if (naics.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return naics.EnumerateArray()
            .Select((item, index) => item.ValueKind == JsonValueKind.String
                ? new CompanyNaicsCodeDto(item.GetString() ?? string.Empty, string.Empty, index == 0, null, null, DateOnly.FromDateTime(DateTime.UtcNow))
                : new CompanyNaicsCodeDto(
                    ReadString(item, "code") ?? string.Empty,
                    ReadString(item, "title") ?? string.Empty,
                    index == 0,
                    null,
                    null,
                    DateOnly.FromDateTime(DateTime.UtcNow)))
            .Where(naicsCode => !string.IsNullOrWhiteSpace(naicsCode.Code))
            .ToArray();
    }

    private static string? ReadString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static DateOnly? ReadDate(JsonElement element, string propertyName) =>
        DateOnly.TryParse(ReadString(element, propertyName), out var date) ? date : null;

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

    private static UpsertCompanyProfileRequest EmptyProfileRequest() =>
        new(
            string.Empty,
            null,
            null,
            null,
            null,
            [],
            [],
            [],
            ContractorRole.Unknown,
            string.Empty,
            CompanyRange.Unknown,
            CompanyRange.Unknown,
            [],
            new ItEnvironmentSummaryDto(string.Empty, false, null, []),
            DataHandlingPosture.Unknown,
            false);

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed record CompanyEntityLookupRequest(string? Uei, string? LegalBusinessName);

public sealed record ApplyCompanyEntityLookupRequest(
    CompanyEntityLookupResultDto Result,
    IReadOnlyList<string> SelectedFields,
    bool ConfirmOverwrite);

public sealed record CompanyEntityLookupResultDto(
    string LegalBusinessName,
    string Uei,
    string? CageCode,
    string? RegistrationStatus,
    DateOnly? SamRegistrationExpiresAt,
    CompanyEntityAddressDto? Address,
    IReadOnlyList<CompanyNaicsCodeDto> NaicsCodes,
    string Source,
    DateTimeOffset RetrievedAt);

public sealed record CompanyEntityAddressDto(
    string Street1,
    string? Street2,
    string City,
    string StateOrProvince,
    string PostalCode,
    string Country);

public sealed class CompanyEntityLookupValidationException(string message) : InvalidOperationException(message);

public sealed class CompanyEntityLookupUnavailableException(string message) : InvalidOperationException(message);

public sealed class CompanyEntityLookupConflictException(IReadOnlyDictionary<string, string[]> conflicts) : InvalidOperationException("SAM.gov data conflicts with existing profile values.")
{
    public IReadOnlyDictionary<string, string[]> Conflicts { get; } = conflicts;
}

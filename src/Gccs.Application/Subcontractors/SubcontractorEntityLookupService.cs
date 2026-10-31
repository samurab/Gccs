using System.Text.Json;
using Gccs.Application.Audit;
using Gccs.Application.SamGov;
using Gccs.Domain.Audit;

namespace Gccs.Application.Subcontractors;

public sealed class SubcontractorEntityLookupService(
    ISamGovEntityLookupClient samGovClient,
    ISubcontractorRepository repository,
    IAuditEventWriter auditEventWriter)
{
    public async Task<IReadOnlyList<SubcontractorEntityLookupResultDto>> SearchAsync(
        SubcontractorEntityLookupRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Uei) && string.IsNullOrWhiteSpace(request.LegalBusinessName))
        {
            throw new SubcontractorEntityLookupValidationException("Provide a UEI or legal business name.");
        }

        var result = await samGovClient.SearchAsync(
            new SamGovEntitySearchRequest(request.Uei?.Trim(), request.LegalBusinessName?.Trim()),
            cancellationToken);
        if (!result.IsSuccess || string.IsNullOrWhiteSpace(result.PayloadJson))
        {
            throw new SubcontractorEntityLookupUnavailableException(result.Error?.Message ?? "SAM.gov lookup is unavailable.");
        }

        return ParseResults(result.PayloadJson, DateTimeOffset.UtcNow);
    }

    public async Task<SubcontractorDto?> ApplyAsync(
        Guid subcontractorId,
        ApplySubcontractorEntityLookupRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var updated = await repository.ApplySamDataAsync(subcontractorId, request, actorUserId, cancellationToken);
        if (updated is null)
        {
            return null;
        }

        await auditEventWriter.WriteAsync(
            updated.TenantId,
            actorUserId,
            AuditAction.Updated,
            "SubcontractorSamLookup",
            updated.Id.ToString(),
            $"Applied SAM.gov entity data to subcontractor '{updated.Name}'.",
            new Dictionary<string, string>
            {
                ["source"] = request.Result.Source,
                ["retrievedAt"] = request.Result.RetrievedAt.ToString("O"),
                ["fields"] = string.Join(",", request.SelectedFields)
            },
            cancellationToken);

        return updated;
    }

    private static IReadOnlyList<SubcontractorEntityLookupResultDto> ParseResults(string payloadJson, DateTimeOffset retrievedAt)
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

    private static SubcontractorEntityLookupResultDto ParseEntity(JsonElement element, DateTimeOffset retrievedAt) =>
        new(
            ReadString(element, "legalBusinessName") ?? ReadString(element, "legalEntityName") ?? string.Empty,
            ReadString(element, "ueiSAM") ?? ReadString(element, "uei") ?? string.Empty,
            ReadString(element, "cageCode"),
            ReadString(element, "registrationStatus") ?? ReadString(element, "status"),
            ReadDate(element, "expirationDate") ?? ReadDate(element, "samRegistrationExpiresAt"),
            ReadNaics(element),
            ReadString(element, "exclusionStatus") ?? ReadString(element, "exclusions"),
            "SAM.gov",
            retrievedAt);

    private static IReadOnlyList<SubcontractorSamNaicsCodeDto> ReadNaics(JsonElement element)
    {
        if (!element.TryGetProperty("naicsCode", out var naics) && !element.TryGetProperty("naicsCodes", out naics))
        {
            return [];
        }

        if (naics.ValueKind == JsonValueKind.String)
        {
            return [new SubcontractorSamNaicsCodeDto(naics.GetString() ?? string.Empty, string.Empty)];
        }

        if (naics.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return naics.EnumerateArray()
            .Select(item => item.ValueKind == JsonValueKind.String
                ? new SubcontractorSamNaicsCodeDto(item.GetString() ?? string.Empty, string.Empty)
                : new SubcontractorSamNaicsCodeDto(ReadString(item, "code") ?? string.Empty, ReadString(item, "title") ?? string.Empty))
            .Where(naicsCode => !string.IsNullOrWhiteSpace(naicsCode.Code))
            .ToArray();
    }

    private static string? ReadString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static DateOnly? ReadDate(JsonElement element, string propertyName) =>
        DateOnly.TryParse(ReadString(element, propertyName), out var date) ? date : null;
}

public sealed class SubcontractorEntityLookupValidationException(string message) : InvalidOperationException(message);

public sealed class SubcontractorEntityLookupUnavailableException(string message) : InvalidOperationException(message);

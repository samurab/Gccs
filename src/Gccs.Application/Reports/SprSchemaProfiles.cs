using System.Security.Cryptography;
using System.Text;

namespace Gccs.Application.Reports;

public sealed class SprSchemaProfileService(ISprSchemaProfileRepository repository)
{
    public async Task<IReadOnlyList<SprSchemaProfileDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var profiles = await repository.ListAsync(cancellationToken);
        SprSchemaProfileGovernance.ValidatePackage(profiles);
        return profiles;
    }

    public async Task<SprSchemaProfileDto> GetCurrentPublishedAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var profiles = await ListAsync(cancellationToken);
        var profile = profiles
            .Where(candidate => candidate.State == SprSchemaProfileState.Published &&
                candidate.EffectiveDate <= today &&
                (candidate.RetiredDate is null || candidate.RetiredDate > today))
            .OrderByDescending(candidate => candidate.EffectiveDate)
            .ThenByDescending(candidate => candidate.Version, StringComparer.Ordinal)
            .FirstOrDefault();
        return profile ?? throw new SprSchemaProfileValidationException(
            "No reviewed, published, and currently effective SAM.gov SPR schema profile is available.");
    }
}

public interface ISprSchemaProfileRepository
{
    Task<IReadOnlyList<SprSchemaProfileDto>> ListAsync(CancellationToken cancellationToken = default);
}

public static class SprSchemaProfileGovernance
{
    public static void ValidatePackage(IReadOnlyList<SprSchemaProfileDto> profiles)
    {
        if (profiles.Count == 0) throw new SprSchemaProfileValidationException("At least one SPR schema profile is required.");
        if (profiles.GroupBy(profile => profile.Id, StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
            throw new SprSchemaProfileValidationException("SPR schema profile IDs must be unique.");
        if (profiles.Where(profile => profile.State == SprSchemaProfileState.Published)
            .GroupBy(profile => profile.Version, StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
            throw new SprSchemaProfileValidationException("Published SPR schema profile versions must be unique.");
        foreach (var profile in profiles.Where(profile => profile.State == SprSchemaProfileState.Published)) ValidatePublished(profile);
    }

    public static void ValidatePublished(SprSchemaProfileDto profile)
    {
        if (string.IsNullOrWhiteSpace(profile.Id) || string.IsNullOrWhiteSpace(profile.Version) ||
            string.IsNullOrWhiteSpace(profile.SourceName) || !IsHttps(profile.SourceUrl) ||
            profile.SourcePublishedDate is null || profile.EffectiveDate is null || profile.LastReviewedAt is null ||
            string.IsNullOrWhiteSpace(profile.Owner) || string.IsNullOrWhiteSpace(profile.Reviewer) ||
            profile.ReviewDate is null || !IsSha256(profile.DefinitionSha256))
            throw new SprSchemaProfileValidationException(
                "Published SPR schema profiles require identity, version, HTTPS source, publication/effective/review dates, distinct owner and reviewer, and a definition SHA-256.");
        if (string.Equals(profile.Owner.Trim(), profile.Reviewer.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new SprSchemaProfileValidationException("Published SPR schema profiles require a reviewer distinct from the content owner.");
        if (profile.LastReviewedAt < profile.ReviewDate || profile.RetiredDate <= profile.EffectiveDate)
            throw new SprSchemaProfileValidationException("SPR schema profile review and lifecycle dates are invalid.");
        if (profile.PriorFiscalYearsAllowed < 0 || profile.Categories.Count == 0 ||
            profile.Categories.Any(string.IsNullOrWhiteSpace) ||
            profile.Categories.Distinct(StringComparer.OrdinalIgnoreCase).Count() != profile.Categories.Count ||
            profile.ReportingPeriods.Count == 0 || profile.ReportingPeriods.Distinct().Count() != profile.ReportingPeriods.Count)
            throw new SprSchemaProfileValidationException("Published SPR schema profiles require valid fiscal-year, category, and reporting-period rules.");
        if (!string.Equals(profile.DefinitionSha256, ComputeDefinitionSha256(profile), StringComparison.OrdinalIgnoreCase))
            throw new SprSchemaProfileValidationException("The SPR schema profile definition does not match its recorded SHA-256.");
    }

    public static string ComputeDefinitionSha256(SprSchemaProfileDto profile)
    {
        var canonical = $"{profile.PriorFiscalYearsAllowed}|{profile.WholeDollarAmounts.ToString().ToLowerInvariant()}|" +
            $"{profile.EligibilityConfirmationRequired.ToString().ToLowerInvariant()}|{string.Join(',', profile.ReportingPeriods)}|" +
            string.Join('\n', profile.Categories);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static bool IsHttps(string value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
    private static bool IsSha256(string value) => value.Length == 64 && value.All(Uri.IsHexDigit);
}

public sealed record SprSchemaProfileDto(
    string Id,
    string Version,
    SprSchemaProfileState State,
    string SourceName,
    string SourceUrl,
    DateOnly? SourcePublishedDate,
    DateOnly? EffectiveDate,
    DateOnly? RetiredDate,
    DateOnly? LastReviewedAt,
    string Owner,
    string? Reviewer,
    DateOnly? ReviewDate,
    string DefinitionSha256,
    int PriorFiscalYearsAllowed,
    bool WholeDollarAmounts,
    bool EligibilityConfirmationRequired,
    IReadOnlyList<SprReportingPeriod> ReportingPeriods,
    IReadOnlyList<string> Categories);

public sealed record SprSchemaReferenceDto(string Id, string Version, string SourceUrl, string DefinitionSha256);

public enum SprSchemaProfileState { Draft, Approved, Published, Superseded, Retired }

public sealed class SprSchemaProfileValidationException(string message) : InvalidOperationException(message);

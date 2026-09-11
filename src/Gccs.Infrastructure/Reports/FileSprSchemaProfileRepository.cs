using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Reports;

namespace Gccs.Infrastructure.Reports;

public sealed class FileSprSchemaProfileRepository : ISprSchemaProfileRepository
{
    private const string RelativePath = "packages/compliance-content/spr/schema-profiles.json";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public async Task<IReadOnlyList<SprSchemaProfileDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var path = ResolvePath();
        await using var stream = File.OpenRead(path);
        var package = await JsonSerializer.DeserializeAsync<SprSchemaProfilePackage>(stream, JsonOptions, cancellationToken) ??
            throw new InvalidOperationException($"SPR schema profile package '{path}' could not be parsed.");
        return package.Profiles;
    }

    private static string ResolvePath()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, RelativePath);
            if (File.Exists(candidate)) return candidate;
            current = current.Parent;
        }
        var candidateFromWorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), RelativePath);
        return File.Exists(candidateFromWorkingDirectory)
            ? candidateFromWorkingDirectory
            : throw new FileNotFoundException($"Could not locate '{RelativePath}' from '{AppContext.BaseDirectory}'.");
    }

    private sealed record SprSchemaProfilePackage(IReadOnlyList<SprSchemaProfileDto> Profiles);
}

public sealed class InMemorySprSchemaProfileRepository(IReadOnlyList<SprSchemaProfileDto>? profiles = null) : ISprSchemaProfileRepository
{
    public static SprSchemaProfileDto PublishedProfile { get; } = new(
        "gsa-spr-fdd-2026-03-06", "1.0", SprSchemaProfileState.Published,
        "GSA Subcontracting Plan Reporting Functional Data Dictionary",
        "https://www.fsd.gov/gsafsd_sp/en/subcontract-plan-reporting-functional-data-dictionary?id=kb_article_view&sysparm_article=KB0093498",
        new DateOnly(2026, 3, 6), new DateOnly(2026, 3, 6), null, new DateOnly(2026, 9, 10),
        "Compliance Content Owner", "Compliance Content Reviewer", new DateOnly(2026, 9, 10),
        "6aba419769c9ab56bc662b05a53e815ebcd11467ecd57b38fbc5a697da7927bb", 9, true, true,
        [SprReportingPeriod.March31, SprReportingPeriod.September30, SprReportingPeriod.Final],
        ["Small Business Concerns (SB)", "Other Than Small Business Concerns (OTSB)",
            "Small Disadvantaged Business (SDB)", "Women-Owned Small Business (WOSB)", "HBCU/MSI",
            "HUBZone Small Business", "Veteran-Owned Small Business (VOSB)",
            "Service-Disabled Veteran-Owned Small Business (SDVOSB)", "ANC/Indian Tribe"]);

    private readonly IReadOnlyList<SprSchemaProfileDto> values = profiles ?? [PublishedProfile];
    public Task<IReadOnlyList<SprSchemaProfileDto>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult(values);
}

using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Cmmc;

namespace Gccs.Infrastructure.Cmmc;

public sealed class FileSprsScoringRuleRepository : ISprsScoringRuleRepository
{
    private const string RelativePath = "packages/compliance-content/sprs/scoring-rules.json";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public async Task<IReadOnlyList<SprsScoringRuleSetDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var package = await ReadPackageAsync(cancellationToken);
        return package.RuleSets;
    }

    public async Task<SprsScoringRuleSetDto?> FindAsync(string ruleSetId, CancellationToken cancellationToken = default)
    {
        var package = await ReadPackageAsync(cancellationToken);
        return package.RuleSets.FirstOrDefault(ruleSet =>
            string.Equals(ruleSet.Id, ruleSetId, StringComparison.OrdinalIgnoreCase));
    }

    public Task<SprsScoringRuleSetDto> UpdateStateAsync(
        string ruleSetId,
        SprsScoringRuleSetState state,
        string? reviewer,
        DateOnly? reviewDate,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException(
            "File-backed SPRS scoring rule state changes are source-control reviewed. Use a persistence-backed repository for runtime workflow state changes.");
    }

    private static async Task<SprsScoringRulePackage> ReadPackageAsync(CancellationToken cancellationToken)
    {
        var path = ResolvePackagePath();
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<SprsScoringRulePackage>(stream, JsonOptions, cancellationToken) ??
            throw new InvalidOperationException($"SPRS scoring rule package '{path}' could not be parsed.");
    }

    private static string ResolvePackagePath()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, RelativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        var workingDirectoryCandidate = Path.Combine(Directory.GetCurrentDirectory(), RelativePath);
        if (File.Exists(workingDirectoryCandidate))
        {
            return workingDirectoryCandidate;
        }

        throw new FileNotFoundException($"Could not locate '{RelativePath}' from '{AppContext.BaseDirectory}'.");
    }

    private sealed record SprsScoringRulePackage(IReadOnlyList<SprsScoringRuleSetDto> RuleSets);
}

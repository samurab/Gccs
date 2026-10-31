using System.Text.RegularExpressions;

namespace Gccs.Application.Compliance;

public static partial class ClauseReferenceNormalizer
{
    public static string NormalizeExact(string reference)
    {
        var normalized = reference.Trim();
        var match = ClauseCitationRegex().Match(normalized);
        return match.Success
            ? $"{match.Groups["base"].Value}-{match.Groups["suffix"].Value}"
            : normalized;
    }

    [GeneratedRegex(
        @"^(?:(?:FAR|DFARS)\s+)?(?<base>\d{2,3}\.\d{3})[.-](?<suffix>\d+)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ClauseCitationRegex();
}

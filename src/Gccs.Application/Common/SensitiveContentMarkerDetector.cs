using System.Text.RegularExpressions;

namespace Gccs.Application.Common;

public static partial class SensitiveContentMarkerDetector
{
    public static bool ContainsExplicitRestrictedMarking(string? value) =>
        !string.IsNullOrWhiteSpace(value) && ExplicitRestrictedMarking().IsMatch(value);

    [GeneratedRegex(
        @"^[\t ]*(?:CONTROLLED[\t ]+UNCLASSIFIED[\t ]+INFORMATION|CUI//[A-Z0-9,/_-]+|\[CUI\]|DISTRIBUTION[\t ]+STATEMENT[\t ]+[B-F](?:\b.*)?|NOFORN|ITAR(?:[\t ]+CONTROLLED)?|EXPORT[\t ]+CONTROLLED)[\t ]*$",
        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex ExplicitRestrictedMarking();
}

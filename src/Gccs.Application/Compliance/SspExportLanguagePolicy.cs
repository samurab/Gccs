using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Gccs.Application.Compliance;

public sealed partial class SspExportLanguagePolicy
{
    public const string Version = "2026-09-10.1";
    private static readonly string[] CompactProhibitedClaims =
    [
        "certified", "certification", "certify", "iscompliant", "arecompliant", "fullycompliant",
        "complianceguaranteed", "assessmentdetermination", "assessordetermination", "governmentapproved",
        "governmentapproval", "governmentendorsed", "governmentendorsement", "authorizationgranted",
        "authorizationapproved", "authorizedto", "systemisauthorized", "systemhasbeenauthorized"
    ];

    public void EnsureReviewOnly(IEnumerable<string?> values)
    {
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value)) continue;
            var normalized = Normalize(value);
            var compact = string.Concat(normalized.Where(char.IsLetterOrDigit));
            if (ProhibitedClaimPattern().IsMatch(normalized) || CompactProhibitedClaims.Any(compact.Contains))
                throw new SspExportPackageValidationException(
                    "SSP package content contains prohibited certification, compliance, assessment, authorization, or government-endorsement language.");
        }
    }

    internal static string Normalize(string value)
    {
        var compatibilityNormalized = value.Normalize(NormalizationForm.FormKC).Normalize(NormalizationForm.FormD);
        var output = new StringBuilder(compatibilityNormalized.Length);
        var previousWasSeparator = true;
        foreach (var rune in compatibilityNormalized.EnumerateRunes())
        {
            var category = Rune.GetUnicodeCategory(rune);
            if (category is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark or UnicodeCategory.EnclosingMark)
                continue;
            var isSeparator = Rune.IsWhiteSpace(rune) || category is
                UnicodeCategory.DashPunctuation or
                UnicodeCategory.ConnectorPunctuation or
                UnicodeCategory.OtherPunctuation or
                UnicodeCategory.MathSymbol or
                UnicodeCategory.ModifierSymbol or
                UnicodeCategory.OtherSymbol or
                UnicodeCategory.Control or
                UnicodeCategory.Format;
            if (isSeparator)
            {
                if (!previousWasSeparator) output.Append(' ');
                previousWasSeparator = true;
                continue;
            }

            output.Append(rune.ToString().ToLowerInvariant());
            previousWasSeparator = false;
        }
        return output.ToString().Trim();
    }

    [GeneratedRegex(
        @"\b(?:certified|certification|certify|cmmc\s+certified|(?:is|are|fully)\s+compliant|compliance\s+guaranteed|(?:assessment|assessor)\s+determination|government\s+(?:approved|approval|endorsed|endorsement)|authorization\s+(?:granted|approved)|authorized\s+to|system\s+(?:is|has\s+been)\s+authorized)\b",
        RegexOptions.CultureInvariant)]
    private static partial Regex ProhibitedClaimPattern();
}

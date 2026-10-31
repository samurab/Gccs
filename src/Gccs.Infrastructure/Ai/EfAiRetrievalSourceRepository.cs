using Gccs.Application.Ai;
using Gccs.Application.Reports;
using Gccs.Application.Security;
using Gccs.Domain.Common;
using Gccs.Domain.Contracts;
using Gccs.Domain.Evidence;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Ai;

public sealed class EfAiRetrievalSourceRepository(
    GccsDbContext dbContext,
    ICurrentTenantContext tenantContext) : IAiRetrievalSourceRepository
{
    private const int MaximumPerSourceFamily = 32;
    public async Task<AiRetrievalSourceBatch> SearchSourcesAsync(
        AiRetrievalSourceQuery query,
        CancellationToken cancellationToken = default)
    {
        EnsureCurrentTenant(query.TenantId);
        var dataPosture = await dbContext.Tenants.AsNoTracking()
            .Where(tenant => tenant.Id == tenantContext.TenantId)
            .Select(tenant => (TenantDataPosture?)tenant.DataPosture)
            .SingleOrDefaultAsync(cancellationToken) ??
            throw new InvalidOperationException("AI retrieval requires an existing current tenant.");

        var maximum = Math.Clamp(query.MaximumCandidates, 1, AiRetrievalAssistantService.MaximumCandidateCount);
        var allowedTenantClassifications = dataPosture == TenantDataPosture.DemoSandbox
            ? new[] { ContentClassification.Unclassified }
            : new[] { ContentClassification.Unclassified, ContentClassification.Fci };
        var rankedFamilies = new List<IReadOnlyList<AiRetrievalSourceDto>>(4);

        if (query.SourceAccess.HasFlag(AiRetrievalSourceAccess.ComplianceLibrary))
            rankedFamilies.Add(await SearchPublishedObligationsAsync(query.Question, cancellationToken));
        if (query.SourceAccess.HasFlag(AiRetrievalSourceAccess.TenantDocument))
            rankedFamilies.Add(await SearchApprovedDocumentExcerptsAsync(query.Question, allowedTenantClassifications, cancellationToken));
        if (query.SourceAccess.HasFlag(AiRetrievalSourceAccess.ApprovedReport))
            rankedFamilies.Add(await SearchApprovedReportPackagesAsync(query.Question, cancellationToken));
        if (query.SourceAccess.HasFlag(AiRetrievalSourceAccess.EvidenceMetadata))
            rankedFamilies.Add(await SearchApprovedEvidenceMetadataAsync(query.Question, allowedTenantClassifications, cancellationToken));

        return new AiRetrievalSourceBatch(
            dataPosture,
            InterleaveRankedFamilies(rankedFamilies, maximum),
            SupportsPostgresFullTextSearch ? "postgresql-fts-gin-minimum-match-v1" : "deterministic-token-fallback-v1");
    }

    private async Task<IReadOnlyList<AiRetrievalSourceDto>> SearchPublishedObligationsAsync(
        string question,
        CancellationToken cancellationToken)
    {
        var searchQuery = BuildFullTextQuery(question);
        var eligible = dbContext.Obligations.AsNoTracking()
            .Where(obligation => obligation.ReviewState == ReviewState.Published);
        var ordered = SupportsPostgresFullTextSearch
            ? eligible
                .Where(obligation => obligation.SearchVector.Matches(EF.Functions.WebSearchToTsQuery("english", searchQuery)))
                .OrderByDescending(obligation => obligation.SearchVector.Rank(EF.Functions.WebSearchToTsQuery("english", searchQuery)))
                .ThenByDescending(obligation => obligation.LastReviewedAt)
            : eligible.OrderByDescending(obligation => obligation.LastReviewedAt);
        var ranked = ordered.ThenBy(obligation => obligation.Id);
        var candidates = SupportsPostgresFullTextSearch ? ranked.Take(MaximumPerSourceFamily) : ranked;
        var rows = await candidates
            .Select(obligation => new
            {
                obligation.Id,
                obligation.Title,
                obligation.Source,
                obligation.SourceName,
                obligation.SourceUrl,
                obligation.PlainEnglishSummary,
                obligation.RequiredAction,
                obligation.SourceEffectiveAt,
                obligation.LastReviewedAt
            })
            .ToArrayAsync(cancellationToken);

        if (!SupportsPostgresFullTextSearch)
            rows = rows.Where(row => MatchesQuestion(question, row.Id, row.Title, row.Source, row.SourceName,
                    row.PlainEnglishSummary, row.RequiredAction))
                .Take(MaximumPerSourceFamily).ToArray();

        return rows.Select(row => new AiRetrievalSourceDto(
            $"obligation:{row.Id}",
            null,
            row.Title,
            "ComplianceLibrary",
            row.SourceUrl,
            null,
            "plainEnglishSummary",
            (row.SourceEffectiveAt ?? row.LastReviewedAt).ToString("yyyy-MM-dd"),
            row.LastReviewedAt,
            ContentClassification.Unclassified,
            IsApproved: true,
            IsPublishedLibraryContent: true,
            Summary: JoinSummary(row.PlainEnglishSummary, row.RequiredAction),
            Keywords: Keywords("obligation", row.Id, row.Title, row.Source, row.SourceName),
            SourceKind: AiRetrievalSourceKind.ComplianceLibrary)).ToArray();
    }

    private async Task<IReadOnlyList<AiRetrievalSourceDto>> SearchApprovedDocumentExcerptsAsync(
        string question,
        IReadOnlyCollection<ContentClassification> allowedClassifications,
        CancellationToken cancellationToken)
    {
        var searchQuery = BuildFullTextQuery(question);
        var eligible = dbContext.Set<ClauseCandidateEntity>().AsNoTracking()
            .Where(candidate => candidate.TenantId == tenantContext.TenantId &&
                candidate.ReviewStatus == "accepted" &&
                candidate.ReviewedByUserId != null && candidate.ReviewedAt != null &&
                candidate.ExtractionJob != null && candidate.ExtractionJob.Status == ExtractionJobStatus.Completed &&
                !candidate.ExtractionJob.IsUseBlocked && allowedClassifications.Contains(candidate.ExtractionJob.Classification) &&
                candidate.SourceDocument != null && candidate.SourceDocument.Contract != null &&
                candidate.SourceDocument.Contract.TenantId == tenantContext.TenantId &&
                candidate.SourceDocument.ValidationStatus == "accepted" &&
                candidate.SourceDocument.MalwareScanStatus == "clean" &&
                !candidate.SourceDocument.IsUseBlocked && allowedClassifications.Contains(candidate.SourceDocument.Classification));
        var ordered = SupportsPostgresFullTextSearch
            ? eligible
                .Where(candidate => candidate.SearchVector.Matches(EF.Functions.WebSearchToTsQuery("english", searchQuery)))
                .OrderByDescending(candidate => candidate.SearchVector.Rank(EF.Functions.WebSearchToTsQuery("english", searchQuery)))
                .ThenByDescending(candidate => candidate.ReviewedAt)
            : eligible.OrderByDescending(candidate => candidate.ReviewedAt);
        var ranked = ordered.ThenBy(candidate => candidate.Id);
        var candidates = SupportsPostgresFullTextSearch ? ranked.Take(MaximumPerSourceFamily) : ranked;
        var rows = await candidates
            .Select(candidate => new
            {
                candidate.Id,
                candidate.SourceDocumentId,
                candidate.NormalizedCitation,
                candidate.RawExtractedText,
                candidate.LocationMetadata,
                candidate.ReviewedAt,
                candidate.SourceDocument!.FileName,
                candidate.SourceDocument.ExtractedTextHash,
                candidate.SourceDocument.Classification
            })
            .ToArrayAsync(cancellationToken);

        if (!SupportsPostgresFullTextSearch)
            rows = rows.Where(row => MatchesQuestion(question, row.NormalizedCitation, row.RawExtractedText, row.FileName))
                .Take(MaximumPerSourceFamily).ToArray();

        return rows.Select(row => new AiRetrievalSourceDto(
            $"contract-document-excerpt:{row.Id}",
            tenantContext.TenantId,
            $"{row.FileName}: {row.NormalizedCitation}",
            "TenantDocument",
            null,
            $"contract-document:{row.SourceDocumentId}",
            row.LocationMetadata,
            string.IsNullOrWhiteSpace(row.ExtractedTextHash) ? row.ReviewedAt!.Value.ToString("O") : row.ExtractedTextHash,
            row.ReviewedAt.HasValue ? DateOnly.FromDateTime(row.ReviewedAt.Value.UtcDateTime) : null,
            row.Classification,
            IsApproved: true,
            IsPublishedLibraryContent: false,
            Summary: row.RawExtractedText,
            Keywords: Keywords("contract", "document", row.NormalizedCitation, row.FileName),
            SourceKind: AiRetrievalSourceKind.TenantDocument)).ToArray();
    }

    private async Task<IReadOnlyList<AiRetrievalSourceDto>> SearchApprovedReportPackagesAsync(
        string question,
        CancellationToken cancellationToken)
    {
        var searchQuery = BuildFullTextQuery(question);
        var eligible = dbContext.SprReportPackages.AsNoTracking()
            .Where(package => package.TenantId == tenantContext.TenantId &&
                package.Status == EsrsReportPackageStatus.Approved && package.ReviewerUserId != null && package.ApprovedAt != null);
        var genericReportQuestion = IsGenericReportQuestion(question);
        var ordered = SupportsPostgresFullTextSearch && !genericReportQuestion
            ? eligible
                .Where(package => package.SearchVector.Matches(EF.Functions.WebSearchToTsQuery("english", searchQuery)))
                .OrderByDescending(package => package.SearchVector.Rank(EF.Functions.WebSearchToTsQuery("english", searchQuery)))
                .ThenByDescending(package => package.ApprovedAt)
            : eligible.OrderByDescending(package => package.ApprovedAt);
        var ranked = ordered.ThenBy(package => package.Id);
        var candidates = SupportsPostgresFullTextSearch ? ranked.Take(MaximumPerSourceFamily) : ranked;
        var rows = await candidates
            .Select(package => new
            {
                package.Id,
                package.ReportType,
                package.PeriodStart,
                package.PeriodEnd,
                package.Version,
                package.ApprovedAt
            })
            .ToArrayAsync(cancellationToken);

        if (!SupportsPostgresFullTextSearch)
            rows = rows.Where(row => MatchesQuestion(question, "report", "package", row.ReportType.ToString(),
                    row.PeriodStart.ToString("yyyy-MM-dd"), row.PeriodEnd.ToString("yyyy-MM-dd")))
                .Take(MaximumPerSourceFamily).ToArray();

        return rows.Select(row => new AiRetrievalSourceDto(
            $"approved-report:{row.Id}",
            tenantContext.TenantId,
            $"Approved {row.ReportType} preparation package",
            "ApprovedReport",
            null,
            $"spr-report-package:{row.Id}",
            "approved-package-metadata",
            row.Version.ToString(System.Globalization.CultureInfo.InvariantCulture),
            row.ApprovedAt.HasValue ? DateOnly.FromDateTime(row.ApprovedAt.Value.UtcDateTime) : null,
            ContentClassification.Unclassified,
            IsApproved: true,
            IsPublishedLibraryContent: false,
            Summary: $"The approved {row.ReportType} preparation package covers {row.PeriodStart:yyyy-MM-dd} through {row.PeriodEnd:yyyy-MM-dd}.",
            Keywords: Keywords("report", "package", row.ReportType.ToString()),
            SourceKind: AiRetrievalSourceKind.ApprovedReport)).ToArray();
    }

    private async Task<IReadOnlyList<AiRetrievalSourceDto>> SearchApprovedEvidenceMetadataAsync(
        string question,
        IReadOnlyCollection<ContentClassification> allowedClassifications,
        CancellationToken cancellationToken)
    {
        var searchQuery = BuildFullTextQuery(question);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var eligible = dbContext.EvidenceItems.AsNoTracking()
            .Where(evidence => evidence.TenantId == tenantContext.TenantId &&
                evidence.Status == EvidenceStatus.Approved && evidence.ApprovedByUserId != null && evidence.ApprovedAt != null &&
                (evidence.ExpiresAt == null || evidence.ExpiresAt >= today) &&
                !evidence.IsUseBlocked && allowedClassifications.Contains(evidence.Classification) &&
                (evidence.UploadValidationStatus == null || evidence.UploadValidationStatus == "accepted") &&
                (evidence.MalwareScanStatus == null || evidence.MalwareScanStatus == "clean"));
        var ordered = SupportsPostgresFullTextSearch
            ? eligible
                .Where(evidence => evidence.SearchVector.Matches(EF.Functions.WebSearchToTsQuery("english", searchQuery)))
                .OrderByDescending(evidence => evidence.SearchVector.Rank(EF.Functions.WebSearchToTsQuery("english", searchQuery)))
                .ThenByDescending(evidence => evidence.ApprovedAt)
            : eligible.OrderByDescending(evidence => evidence.ApprovedAt);
        var ranked = ordered.ThenBy(evidence => evidence.Id);
        var candidates = SupportsPostgresFullTextSearch ? ranked.Take(MaximumPerSourceFamily) : ranked;
        var rows = await candidates
            .Select(evidence => new
            {
                evidence.Id,
                evidence.Name,
                evidence.Description,
                evidence.Type,
                evidence.Classification,
                evidence.ClassificationRevision,
                evidence.ApprovedAt
            })
            .ToArrayAsync(cancellationToken);

        if (!SupportsPostgresFullTextSearch)
            rows = rows.Where(row => MatchesQuestion(question, "evidence", row.Name, row.Description, row.Type.ToString()))
                .Take(MaximumPerSourceFamily).ToArray();

        return rows.Select(row => new AiRetrievalSourceDto(
            $"evidence-metadata:{row.Id}",
            tenantContext.TenantId,
            row.Name,
            "EvidenceMetadata",
            null,
            $"evidence:{row.Id}",
            "approved-metadata",
            $"classification-{row.ClassificationRevision}",
            row.ApprovedAt.HasValue ? DateOnly.FromDateTime(row.ApprovedAt.Value.UtcDateTime) : null,
            row.Classification,
            IsApproved: true,
            IsPublishedLibraryContent: false,
            Summary: JoinSummary($"Approved {row.Type} evidence metadata: {row.Name}.", row.Description),
            Keywords: Keywords("evidence", row.Type.ToString(), row.Name),
            SourceKind: AiRetrievalSourceKind.EvidenceMetadata)).ToArray();
    }

    private void EnsureCurrentTenant(Guid tenantId)
    {
        if (tenantId != tenantContext.TenantId)
            throw new InvalidOperationException("AI retrieval tenant scope does not match the current tenant.");
    }

    private static IReadOnlyList<AiRetrievalSourceDto> InterleaveRankedFamilies(
        IReadOnlyList<IReadOnlyList<AiRetrievalSourceDto>> families,
        int maximum)
    {
        var merged = new List<AiRetrievalSourceDto>(maximum);
        for (var rank = 0; merged.Count < maximum && families.Any(family => rank < family.Count); rank++)
        {
            foreach (var family in families)
            {
                if (rank < family.Count)
                    merged.Add(family[rank]);
                if (merged.Count == maximum)
                    break;
            }
        }
        return merged;
    }

    private bool SupportsPostgresFullTextSearch =>
        dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.Ordinal) == true;

    private static bool IsGenericReportQuestion(string question)
    {
        var terms = Tokenize(question);
        return terms.Overlaps(["report", "package"]) &&
               terms.All(term => GenericReportTerms.Contains(term));
    }

    private static readonly HashSet<string> GenericReportTerms = new(
        ["approved", "find", "latest", "package", "report", "show", "the"],
        StringComparer.Ordinal);

    private static bool MatchesQuestion(string question, params string?[] values)
    {
        var terms = Tokenize(question);
        var searchable = Tokenize(string.Join(" ", values.Where(value => !string.IsNullOrWhiteSpace(value))));
        return terms.Any(searchable.Contains);
    }

    private static string BuildFullTextQuery(string question)
    {
        var terms = Tokenize(question)
            .Where(term => !FullTextStopTerms.Contains(term) && term.All(char.IsLetterOrDigit))
            .Take(12)
            .ToArray();
        if (terms.Length < 2)
            return string.Join(" ", terms);

        var pairs = new List<string>(terms.Length * (terms.Length - 1) / 2);
        for (var left = 0; left < terms.Length - 1; left++)
        for (var right = left + 1; right < terms.Length; right++)
            pairs.Add($"{terms[left]} {terms[right]}");
        return string.Join(" OR ", pairs);
    }

    private static readonly HashSet<string> FullTextStopTerms = new(
        ["about", "and", "answer", "approved", "does", "explain", "for", "from", "how", "into", "the", "this", "what", "with"],
        StringComparer.Ordinal);

    private static HashSet<string> Tokenize(string value) => value
        .Split([' ', '/', ':', '-', '_', '.', ',', '(', ')', '?', '!'], StringSplitOptions.RemoveEmptyEntries)
        .Select(token => token.Trim().ToLowerInvariant())
        .Where(token => token.Length >= 3)
        .ToHashSet(StringComparer.Ordinal);

    private static string JoinSummary(string first, string second) =>
        string.Join(" ", new[] { first, second }.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()));

    private static IReadOnlyList<string> Keywords(params string?[] values) => values
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .SelectMany(value => new[] { value!.Trim().ToLowerInvariant() }
            .Concat(value.Split([' ', '/', ':', '-', '_', '.', ',', '(', ')'], StringSplitOptions.RemoveEmptyEntries)
                .Select(token => token.Trim().ToLowerInvariant())))
        .Where(value => value.Length >= 3)
        .Distinct(StringComparer.Ordinal)
        .Take(32)
        .ToArray();
}

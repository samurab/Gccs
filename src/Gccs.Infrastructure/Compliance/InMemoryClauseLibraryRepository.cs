using Gccs.Application.Compliance;
using Gccs.Domain.Common;
using Gccs.Infrastructure.Persistence.Models;

namespace Gccs.Infrastructure.Compliance;

public sealed class InMemoryClauseLibraryRepository : IClauseLibraryRepository
{
    private readonly IReadOnlyList<ClauseEntity> _clauses;

    public InMemoryClauseLibraryRepository()
        : this(DefaultClauses)
    {
    }

    public InMemoryClauseLibraryRepository(IReadOnlyList<ClauseEntity> clauses)
    {
        _clauses = clauses;
    }

    public Task<IReadOnlyList<ClauseLibraryItemDto>> SearchAsync(
        ClauseLibrarySearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var results = _clauses
            .Where(clause => (request.IncludeDrafts || clause.ReviewState == ReviewState.Published) &&
                (clause.TenantId == null || clause.TenantId == request.TenantId))
            .Where(clause => MatchesQuery(clause, request.Query))
            .Where(clause => string.IsNullOrWhiteSpace(request.SourceFamily) ||
                clause.Source.Contains(request.SourceFamily, StringComparison.OrdinalIgnoreCase))
            .Where(clause => request.RequiresFlowDown is null || clause.UsuallyRequiresFlowDown == request.RequiresFlowDown)
            .Select(ToDto)
            .Where(clause => string.IsNullOrWhiteSpace(request.Category) ||
                string.Equals(clause.Category, request.Category, StringComparison.OrdinalIgnoreCase))
            .Where(clause => string.IsNullOrWhiteSpace(request.ObligationArea) ||
                string.Equals(clause.Category, request.ObligationArea, StringComparison.OrdinalIgnoreCase))
            .OrderBy(clause => clause.Source)
            .ThenBy(clause => clause.Number)
            .ToArray();

        return Task.FromResult<IReadOnlyList<ClauseLibraryItemDto>>(results);
    }

    public Task<ClauseLibraryDetailDto?> FindDetailAsync(
        string clauseId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var clause = _clauses.FirstOrDefault(item =>
            item.Id == clauseId &&
            (item.TenantId == null || item.TenantId == tenantId));
        if (clause is null)
        {
            return Task.FromResult<ClauseLibraryDetailDto?>(null);
        }

        var history = _clauses
            .Where(item =>
                item.Source == clause.Source &&
                item.Number == clause.Number &&
                (item.TenantId == null || item.TenantId == tenantId))
            .OrderByDescending(item => item.ClauseEffectiveAt ?? item.LastReviewedAt)
            .ThenByDescending(item => item.LastReviewedAt)
            .Select(ToDto)
            .ToArray();

        return Task.FromResult<ClauseLibraryDetailDto?>(new ClauseLibraryDetailDto(ToDto(clause), history));
    }

    private static bool MatchesQuery(ClauseEntity clause, string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        return Contains(clause.Number, query) ||
            Contains(clause.Title, query) ||
            Contains(clause.Source, query) ||
            Contains(clause.PlainEnglishSummary, query);
    }

    private static ClauseLibraryItemDto ToDto(ClauseEntity clause) =>
        new(
            clause.Id,
            clause.Source,
            clause.Number,
            clause.Title,
            EfClauseLibraryRepository.ClassifyCategory(clause),
            clause.PlainEnglishSummary,
            clause.SourceUrl,
            clause.LastReviewedAt,
            clause.ReviewedByUserId,
            clause.ReviewState.ToString(),
            clause.ClauseTextVersion,
            clause.ClauseEffectiveAt,
            clause.SupersededByClauseId,
            clause.SupersededAt,
            clause.Confidence,
            clause.UsuallyRequiresFlowDown,
            clause.ReviewState == ReviewState.Published);

    private static bool Contains(string value, string query) =>
        value.Contains(query, StringComparison.OrdinalIgnoreCase);

    private static readonly ClauseEntity[] DefaultClauses =
    [
        CreateClause(
            "far-52-204-21",
            "FAR 52.204-21",
            "52.204-21",
            "Basic Safeguarding of Covered Contractor Information Systems",
            "Apply baseline safeguards when contractor systems process, store, or transmit Federal Contract Information.",
            "https://www.acquisition.gov/far/52.204-21"),
        CreateClause(
            "far-52-204-25",
            "FAR 52.204-25",
            "52.204-25",
            "Prohibition on Certain Telecommunications and Video Surveillance Services or Equipment",
            "Screen covered telecom and video surveillance equipment or services for prohibited sources.",
            "https://www.acquisition.gov/far/52.204-25"),
        CreateClause(
            "far-52-204-27",
            "FAR 52.204-27",
            "52.204-27",
            "Prohibition on a ByteDance Covered Application",
            "Prevent covered ByteDance applications on certain government or contractor information technology.",
            "https://www.acquisition.gov/far/52.204-27"),
        CreateClause(
            "far-52-222-41",
            "FAR 52.222-41",
            "52.222-41",
            "Service Contract Labor Standards",
            "Identify covered service work and preserve wage determination, labor category, and payroll evidence.",
            "https://www.acquisition.gov/far/52.222-41")
    ];

    private static ClauseEntity CreateClause(
        string id,
        string source,
        string number,
        string title,
        string summary,
        string sourceUrl) =>
        new()
        {
            Id = id,
            Source = source,
            Number = number,
            Title = title,
            PlainEnglishSummary = summary,
            ApplicabilityLogic = "Appears in a solicitation, contract, subcontract, or flow-down attachment.",
            ClauseTextVersion = "current",
            RequiredActionIdsJson = "[]",
            SourceName = source,
            SourceUrl = sourceUrl,
            SourceLastReviewedAt = new DateOnly(2026, 6, 3),
            SourceConfidence = "high",
            LastReviewedAt = new DateOnly(2026, 6, 3),
            Confidence = "high",
            ReviewState = ReviewState.Published
        };
}

namespace Gccs.Application.Compliance;

public sealed class ClauseLibraryService(IClauseLibraryRepository repository)
{
    public static readonly IReadOnlySet<string> SupportedCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "FAR",
        "DFARS",
        "CMMC",
        "Labor",
        "Telecom",
        "ByteDance",
        "Custom"
    };

    public Task<IReadOnlyList<ClauseLibraryItemDto>> SearchAsync(
        ClauseLibrarySearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedQuery = request.Query?.Trim();
        var normalizedCategory = request.Category?.Trim();

        if (normalizedQuery?.Length > 160)
        {
            throw new ClauseLibrarySearchValidationException("Clause search query must be 160 characters or fewer.");
        }

        if (!string.IsNullOrWhiteSpace(normalizedCategory) && !SupportedCategories.Contains(normalizedCategory))
        {
            throw new ClauseLibrarySearchValidationException(
                $"Clause category must be one of: {string.Join(", ", SupportedCategories)}.");
        }

        return repository.SearchAsync(
            new ClauseLibrarySearchRequest(
                string.IsNullOrWhiteSpace(normalizedQuery) ? null : normalizedQuery,
                string.IsNullOrWhiteSpace(normalizedCategory) ? null : normalizedCategory,
                request.TenantId),
            cancellationToken);
    }

    public Task<ClauseLibraryDetailDto?> FindDetailAsync(
        string clauseId,
        Guid tenantId,
        CancellationToken cancellationToken = default) =>
        repository.FindDetailAsync(clauseId.Trim(), tenantId, cancellationToken);
}

public interface IClauseLibraryRepository
{
    Task<IReadOnlyList<ClauseLibraryItemDto>> SearchAsync(
        ClauseLibrarySearchRequest request,
        CancellationToken cancellationToken = default);

    Task<ClauseLibraryDetailDto?> FindDetailAsync(
        string clauseId,
        Guid tenantId,
        CancellationToken cancellationToken = default);
}

public sealed record ClauseLibrarySearchRequest(
    string? Query,
    string? Category,
    Guid TenantId);

public sealed record ClauseLibraryItemDto(
    string Id,
    string Source,
    string Number,
    string Title,
    string Category,
    string PlainEnglishSummary,
    string SourceUrl,
    DateOnly LastReviewedAt,
    Guid? ReviewedByUserId,
    string ReviewState,
    string ClauseTextVersion,
    DateOnly? ClauseEffectiveAt,
    string? SupersededByClauseId,
    DateOnly? SupersededAt,
    bool IsMappable);

public sealed record ClauseLibraryDetailDto(
    ClauseLibraryItemDto Clause,
    IReadOnlyList<ClauseLibraryItemDto> VersionHistory);

public sealed class ClauseLibrarySearchValidationException(string message) : ArgumentException(message);

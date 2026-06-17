using Gccs.Application.Compliance;
using Gccs.Domain.Common;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Compliance;

public sealed class EfClauseLibraryRepository(GccsDbContext dbContext) : IClauseLibraryRepository
{
    public async Task<IReadOnlyList<ClauseLibraryItemDto>> SearchAsync(
        ClauseLibrarySearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Clauses
            .AsNoTracking()
            .Where(clause =>
                clause.ReviewState == ReviewState.Published &&
                (clause.TenantId == null || clause.TenantId == request.TenantId));

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var searchTerm = request.Query.Trim().ToLowerInvariant();
            query = query.Where(clause =>
                clause.Number.ToLower().Contains(searchTerm) ||
                clause.Title.ToLower().Contains(searchTerm) ||
                clause.Source.ToLower().Contains(searchTerm) ||
                clause.PlainEnglishSummary.ToLower().Contains(searchTerm));
        }

        var clauses = await query
            .OrderBy(clause => clause.Source)
            .ThenBy(clause => clause.Number)
            .Take(100)
            .ToArrayAsync(cancellationToken);

        var results = clauses
            .Select(ToDto)
            .Where(clause => string.IsNullOrWhiteSpace(request.Category) ||
                string.Equals(clause.Category, request.Category, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return results;
    }

    public async Task<ClauseLibraryDetailDto?> FindDetailAsync(
        string clauseId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var clause = await dbContext.Clauses
            .AsNoTracking()
            .FirstOrDefaultAsync(item =>
                item.Id == clauseId &&
                (item.TenantId == null || item.TenantId == tenantId),
                cancellationToken);

        if (clause is null)
        {
            return null;
        }

        var history = await dbContext.Clauses
            .AsNoTracking()
            .Where(item =>
                item.Source == clause.Source &&
                item.Number == clause.Number &&
                (item.TenantId == null || item.TenantId == tenantId))
            .OrderByDescending(item => item.ClauseEffectiveAt ?? item.LastReviewedAt)
            .ThenByDescending(item => item.LastReviewedAt)
            .Select(item => ToDto(item))
            .ToArrayAsync(cancellationToken);

        return new ClauseLibraryDetailDto(ToDto(clause), history);
    }

    private static ClauseLibraryItemDto ToDto(ClauseEntity clause) =>
        new(
            clause.Id,
            clause.Source,
            clause.Number,
            clause.Title,
            ClassifyCategory(clause),
            clause.PlainEnglishSummary,
            clause.SourceUrl,
            clause.LastReviewedAt,
            clause.ReviewedByUserId,
            clause.ReviewState.ToString(),
            clause.ClauseTextVersion,
            clause.ClauseEffectiveAt,
            clause.SupersededByClauseId,
            clause.SupersededAt,
            true);

    internal static string ClassifyCategory(ClauseEntity clause)
    {
        var combined = $"{clause.Source} {clause.Number} {clause.Title} {clause.PlainEnglishSummary}";

        if (ContainsAny(combined, "dfars"))
        {
            return "DFARS";
        }

        if (ContainsAny(combined, "cmmc", "nist sp 800-171", "32 cfr part 170"))
        {
            return "CMMC";
        }

        if (ContainsAny(combined, "52.222", "labor", "wage", "service contract", "davis-bacon"))
        {
            return "Labor";
        }

        if (ContainsAny(combined, "52.204-25", "telecommunications", "video surveillance", "huawei", "zte"))
        {
            return "Telecom";
        }

        if (ContainsAny(combined, "52.204-27", "bytedance", "tiktok"))
        {
            return "ByteDance";
        }

        if (ContainsAny(combined, "custom"))
        {
            return "Custom";
        }

        return "FAR";
    }

    private static bool ContainsAny(string value, params string[] needles) =>
        needles.Any(needle => value.Contains(needle, StringComparison.OrdinalIgnoreCase));
}

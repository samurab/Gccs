using Gccs.Domain.Audit;

namespace Gccs.Application.Audit;

public sealed class CuiAuditExportService(
    IAuditLogRepository repository,
    IAuditEventWriter auditEventWriter)
{
    public async Task<CuiAuditExportDto> ExportAsync(
        Guid tenantId,
        Guid actorUserId,
        CuiAuditExportRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.From > request.To)
            throw new ArgumentException("The from date must be before the to date.", nameof(request));
        var query = new AuditLogQuery(
            Page: 1,
            PageSize: 100,
            request.ActorUserId,
            null,
            request.EntityType,
            request.From,
            request.To);
        var filtered = new List<AuditLogEntryDto>();
        await foreach (var item in repository.ReadCurrentTenantExportAsync(query, cancellationToken))
        {
            if (item.TenantId != tenantId)
                throw new InvalidOperationException("Audit export tenant scope does not match the active tenant.");
            if (Matches(item, request)) filtered.Add(item);
            if (filtered.Count > 10000)
                throw new CuiAuditExportLimitException("The export exceeds 10,000 matching events. Narrow the date range; no partial export was generated.");
        }
        var export = new CuiAuditExportDto(
            tenantId,
            actorUserId,
            DateTimeOffset.UtcNow,
            request,
            filtered);

        await auditEventWriter.WriteAsync(
            tenantId,
            actorUserId,
            AuditAction.Exported,
            "CuiAuditExport",
            $"{tenantId}:{export.GeneratedAt:O}",
            "CUI audit export generated.",
            new Dictionary<string, string>
            {
                ["result"] = "succeeded",
                ["eventType"] = request.EventType ?? string.Empty,
                ["classification"] = request.Classification ?? string.Empty,
                ["mode"] = request.Mode ?? string.Empty,
                ["entityType"] = request.EntityType ?? string.Empty,
                ["exportedCount"] = filtered.Count.ToString()
            },
            cancellationToken);

        return export;
    }

    private static bool Matches(AuditLogEntryDto item, CuiAuditExportRequest request) =>
        MatchesMetadata(item, "eventType", request.EventType) &&
        MatchesMetadata(item, "classification", request.Classification) &&
        MatchesMetadata(item, "mode", request.Mode) &&
        MatchesMetadata(item, "result", request.Result);

    private static bool MatchesMetadata(AuditLogEntryDto item, string key, string? value) =>
        string.IsNullOrWhiteSpace(value) ||
        item.Metadata.TryGetValue(key, out var actual) &&
        string.Equals(actual, value, StringComparison.OrdinalIgnoreCase);
}

public sealed class CuiAuditExportLimitException(string message) : InvalidOperationException(message);

public sealed record CuiAuditExportRequest(
    string? EventType,
    string? Classification,
    string? Mode,
    Guid? ActorUserId,
    string? EntityType,
    DateTimeOffset? From,
    DateTimeOffset? To,
    string? Result);

public sealed record CuiAuditExportDto(
    Guid TenantId,
    Guid GeneratedByUserId,
    DateTimeOffset GeneratedAt,
    CuiAuditExportRequest Filters,
    IReadOnlyList<AuditLogEntryDto> Events);

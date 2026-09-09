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
        var effectiveRequest = request with
        {
            Action = AuditLogFilterNormalizer.Action(request.Action)?.ToString(),
            EventType = AuditLogFilterNormalizer.EventType(request.EventType),
            Classification = AuditLogFilterNormalizer.Classification(request.Classification),
            Mode = AuditLogFilterNormalizer.Mode(request.Mode),
            EntityType = AuditLogFilterNormalizer.Text(request.EntityType),
            Result = AuditLogFilterNormalizer.Result(request.Result)
        };
        var query = new AuditLogQuery(
            Page: 1,
            PageSize: 100,
            effectiveRequest.ActorUserId,
            AuditLogFilterNormalizer.Action(effectiveRequest.Action),
            effectiveRequest.EventType,
            effectiveRequest.Classification,
            effectiveRequest.Mode,
            effectiveRequest.Result,
            effectiveRequest.EntityType,
            effectiveRequest.From,
            effectiveRequest.To);
        var filtered = new List<AuditLogEntryDto>();
        await foreach (var item in repository.ReadCurrentTenantExportAsync(query, cancellationToken))
        {
            if (item.TenantId != tenantId)
                throw new InvalidOperationException("Audit export tenant scope does not match the active tenant.");
            filtered.Add(item);
            if (filtered.Count > 10000)
                throw new CuiAuditExportLimitException("The export exceeds 10,000 matching events. Narrow the date range; no partial export was generated.");
        }
        var export = new CuiAuditExportDto(
            tenantId,
            actorUserId,
            DateTimeOffset.UtcNow,
            effectiveRequest,
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
                ["eventType"] = Phase1ACuiAuditEvents.Export,
                ["filterEventType"] = effectiveRequest.EventType ?? string.Empty,
                ["filterClassification"] = effectiveRequest.Classification ?? string.Empty,
                ["filterMode"] = effectiveRequest.Mode ?? string.Empty,
                ["filterEntityType"] = effectiveRequest.EntityType ?? string.Empty,
                ["filterResult"] = effectiveRequest.Result ?? string.Empty,
                ["filterAction"] = effectiveRequest.Action ?? string.Empty,
                ["exportedCount"] = filtered.Count.ToString()
            },
            cancellationToken);

        return export;
    }

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
    string? Result,
    string? Action = null);

public sealed record CuiAuditExportDto(
    Guid TenantId,
    Guid GeneratedByUserId,
    DateTimeOffset GeneratedAt,
    CuiAuditExportRequest Filters,
    IReadOnlyList<AuditLogEntryDto> Events);

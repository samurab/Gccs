using System.Text.Json;
using Gccs.Application.Audit;
using Gccs.Application.Security;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Audit;

public sealed class EfAuditLogRepository(
    GccsDbContext dbContext,
    ICurrentTenantContext tenantContext) : IAuditLogRepository
{
    public async Task<PagedResultDto<AuditLogEntryDto>> ListCurrentTenantAsync(
        AuditLogQuery query,
        CancellationToken cancellationToken = default)
    {
        var entries = dbContext.AuditLogEntries
            .AsNoTracking()
            .Where(entry => entry.TenantId == tenantContext.TenantId);

        if (query.ActorUserId is not null)
        {
            entries = entries.Where(entry => entry.ActorUserId == query.ActorUserId);
        }

        if (query.Action is not null)
        {
            entries = entries.Where(entry => entry.Action == query.Action);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityType))
        {
            entries = entries.Where(entry => entry.EntityType == query.EntityType);
        }

        if (query.From is not null)
        {
            entries = entries.Where(entry => entry.OccurredAt >= query.From);
        }

        if (query.To is not null)
        {
            entries = entries.Where(entry => entry.OccurredAt <= query.To);
        }

        var totalCount = await entries.CountAsync(cancellationToken);
        var entities = await entries
            .OrderByDescending(entry => entry.OccurredAt)
            .ThenByDescending(entry => entry.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResultDto<AuditLogEntryDto>(
            entities.Select(ToDto).ToArray(),
            query.Page,
            query.PageSize,
            totalCount,
            query.Page * query.PageSize < totalCount,
            query.Page > 1);
    }

    private static AuditLogEntryDto ToDto(AuditLogEntryEntity entry) =>
        new(
            entry.Id,
            entry.TenantId,
            entry.ActorUserId,
            entry.Action.ToString(),
            entry.EntityType,
            entry.EntityId,
            entry.OccurredAt,
            entry.IpAddress,
            entry.UserAgent,
            entry.CorrelationId,
            entry.Summary,
            entry.OldValue,
            entry.NewValue,
            ParseMetadata(entry.MetadataJson));

    private static IReadOnlyDictionary<string, string> ParseMetadata(string metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return new Dictionary<string, string>();
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(metadataJson) ??
                new Dictionary<string, string>();
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>
            {
                ["raw"] = metadataJson
            };
        }
    }
}

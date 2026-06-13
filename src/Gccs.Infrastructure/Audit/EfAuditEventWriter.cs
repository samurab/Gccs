using System.Text.Json;
using Gccs.Application.Audit;
using Gccs.Domain.Audit;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;

namespace Gccs.Infrastructure.Audit;

public sealed class EfAuditEventWriter(GccsDbContext dbContext, IAuditRequestMetadata requestMetadata) : IAuditEventWriter
{
    public async Task WriteAsync(
        Guid tenantId,
        Guid actorUserId,
        AuditAction action,
        string entityType,
        string entityId,
        string summary,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        dbContext.AuditLogEntries.Add(new AuditLogEntryEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ActorUserId = actorUserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OccurredAt = DateTimeOffset.UtcNow,
            IpAddress = requestMetadata.IpAddress,
            UserAgent = requestMetadata.UserAgent,
            Summary = summary,
            MetadataJson = JsonSerializer.Serialize(metadata ?? new Dictionary<string, string>())
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

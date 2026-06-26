using System.Text.Json;
using System.Text.Json.Nodes;
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
        await WriteCoreAsync(
            tenantId,
            actorUserId,
            action,
            entityType,
            entityId,
            summary,
            metadata,
            oldValue: null,
            newValue: null,
            cancellationToken);
    }

    public async Task WriteChangeAsync(
        Guid tenantId,
        Guid actorUserId,
        AuditAction action,
        string entityType,
        string entityId,
        string summary,
        string? oldValue,
        string? newValue,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        await WriteCoreAsync(
            tenantId,
            actorUserId,
            action,
            entityType,
            entityId,
            summary,
            metadata,
            oldValue,
            newValue,
            cancellationToken);
    }

    private async Task WriteCoreAsync(
        Guid tenantId,
        Guid actorUserId,
        AuditAction action,
        string entityType,
        string entityId,
        string summary,
        IReadOnlyDictionary<string, string>? metadata,
        string? oldValue,
        string? newValue,
        CancellationToken cancellationToken)
    {
        var eventMetadata = SanitizeMetadata(metadata);
        if (!string.IsNullOrWhiteSpace(requestMetadata.CorrelationId))
        {
            eventMetadata["correlationId"] = requestMetadata.CorrelationId;
        }

        try
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
                CorrelationId = requestMetadata.CorrelationId,
                Summary = summary,
                OldValue = SanitizeValue(oldValue),
                NewValue = SanitizeValue(newValue),
                MetadataJson = JsonSerializer.Serialize(eventMetadata)
            });

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not AuditWriteException)
        {
            throw new AuditWriteException("A critical audit event could not be written.", exception);
        }
    }

    private static Dictionary<string, string> SanitizeMetadata(IReadOnlyDictionary<string, string>? metadata)
    {
        var sanitized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (metadata is null)
        {
            return sanitized;
        }

        foreach (var item in metadata)
        {
            if (IsSensitiveKey(item.Key))
            {
                sanitized[item.Key] = "[redacted]";
                continue;
            }

            sanitized[item.Key] = item.Value;
        }

        return sanitized;
    }

    private static string? SanitizeValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        try
        {
            var node = JsonNode.Parse(value);
            RedactSensitiveJson(node);
            return node?.ToJsonString() ?? value;
        }
        catch (JsonException)
        {
            return value;
        }
    }

    private static void RedactSensitiveJson(JsonNode? node)
    {
        if (node is JsonObject jsonObject)
        {
            foreach (var propertyName in jsonObject.Select(property => property.Key).ToArray())
            {
                if (IsSensitiveKey(propertyName))
                {
                    jsonObject[propertyName] = "[redacted]";
                    continue;
                }

                RedactSensitiveJson(jsonObject[propertyName]);
            }

            return;
        }

        if (node is JsonArray jsonArray)
        {
            foreach (var item in jsonArray)
            {
                RedactSensitiveJson(item);
            }
        }
    }

    private static bool IsSensitiveKey(string key) =>
        key.Contains("password", StringComparison.OrdinalIgnoreCase) ||
        key.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
        key.Contains("token", StringComparison.OrdinalIgnoreCase) ||
        key.Contains("fileContent", StringComparison.OrdinalIgnoreCase) ||
        key.Contains("contentBytes", StringComparison.OrdinalIgnoreCase);
}

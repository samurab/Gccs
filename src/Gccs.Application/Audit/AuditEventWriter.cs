using Gccs.Domain.Audit;

namespace Gccs.Application.Audit;

public interface IAuditEventWriter
{
    Task WriteAsync(
        Guid tenantId,
        Guid actorUserId,
        AuditAction action,
        string entityType,
        string entityId,
        string summary,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);

    Task WriteChangeAsync(
        Guid tenantId,
        Guid actorUserId,
        AuditAction action,
        string entityType,
        string entityId,
        string summary,
        string? oldValue,
        string? newValue,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default) =>
        WriteAsync(
            tenantId,
            actorUserId,
            action,
            entityType,
            entityId,
            summary,
            metadata,
            cancellationToken);
}

public sealed class AuditWriteException(string message, Exception? innerException = null)
    : InvalidOperationException(message, innerException);

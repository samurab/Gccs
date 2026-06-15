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
}

public sealed class AuditWriteException(string message, Exception? innerException = null)
    : InvalidOperationException(message, innerException);

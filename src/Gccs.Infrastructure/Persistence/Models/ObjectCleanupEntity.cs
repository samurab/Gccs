using Gccs.Application.Storage;

namespace Gccs.Infrastructure.Persistence.Models;

public sealed class ObjectCleanupEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ActorUserId { get; set; }
    public ObjectStorageContainer Container { get; set; }
    public string ObjectName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset NextAttemptAt { get; set; }
    public int Attempts { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? LastErrorCode { get; set; }
}

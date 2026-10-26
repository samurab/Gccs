using Gccs.Application.Storage;
using Gccs.Domain.Storage;

namespace Gccs.Infrastructure.Persistence.Models;

public sealed class ObjectWriteIntentEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ActorUserId { get; set; }
    public ObjectStorageContainer Container { get; set; }
    public string ObjectName { get; set; } = string.Empty;
    public ObjectWriteIntentState State { get; set; }
    public Guid OwnerId { get; set; }
    public long Generation { get; set; }
    public DateTimeOffset LeaseUntil { get; set; }
    public string ResourceType { get; set; } = string.Empty;
    public Guid ResourceId { get; set; }
    public Guid? ResourceLeaseId { get; set; }
    public string? ETag { get; set; }
    public string? ResultCode { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

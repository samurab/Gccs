namespace Gccs.Application.Tenancy;

public interface IContentContainmentRepository
{
    Task<bool> IsBlockedAsync(Guid tenantId, string entityType, string entityId, CancellationToken cancellationToken = default);
}

public sealed class ContentContainedException(string message, string entityType, string entityId) : InvalidOperationException(message)
{
    public string EntityType { get; } = entityType;
    public string EntityId { get; } = entityId;
}

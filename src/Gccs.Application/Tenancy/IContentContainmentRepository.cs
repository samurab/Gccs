namespace Gccs.Application.Tenancy;

public interface IContentContainmentRepository
{
    Task<bool> IsBlockedAsync(Guid tenantId, string entityType, string entityId, CancellationToken cancellationToken = default);
}

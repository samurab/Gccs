namespace Gccs.Api.Security;

public interface ITenantContext
{
    Guid TenantId { get; }
    Guid UserId { get; }
    string UserEmail { get; }
}

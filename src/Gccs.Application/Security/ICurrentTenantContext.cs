namespace Gccs.Application.Security;

public interface ICurrentTenantContext
{
    Guid TenantId { get; }
    Guid UserId { get; }
    string UserEmail { get; }
}

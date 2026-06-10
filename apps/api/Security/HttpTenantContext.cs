using System.Security.Claims;

namespace Gccs.Api.Security;

public sealed class HttpTenantContext(IHttpContextAccessor httpContextAccessor) : ITenantContext
{
    public Guid TenantId => GetRequiredGuid(ApiSecurityExtensions.TenantIdClaimType);

    public Guid UserId => GetRequiredGuid(ClaimTypes.NameIdentifier);

    public string UserEmail =>
        httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email) ??
        throw new InvalidOperationException("The authenticated user email claim is missing.");

    private Guid GetRequiredGuid(string claimType)
    {
        var value = httpContextAccessor.HttpContext?.User.FindFirstValue(claimType);
        if (!Guid.TryParse(value, out var id))
        {
            throw new InvalidOperationException($"The authenticated user claim '{claimType}' is missing or invalid.");
        }

        return id;
    }
}

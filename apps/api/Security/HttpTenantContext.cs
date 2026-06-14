using System.Security.Claims;

namespace Gccs.Api.Security;

public sealed class HttpTenantContext(IHttpContextAccessor httpContextAccessor) : ITenantContext
{
    public Guid TenantId => GetRequiredGuid(
        ApiSecurityExtensions.TenantIdClaimType,
        claimType => new MissingTenantContextException($"The authenticated user claim '{claimType}' is missing or invalid."));

    public Guid UserId => GetRequiredGuid(
        ClaimTypes.NameIdentifier,
        claimType => new InvalidUserContextException($"The authenticated user claim '{claimType}' is missing or invalid."));

    public string UserEmail =>
        httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email) ??
        throw new InvalidUserContextException("The authenticated user email claim is missing.");

    private Guid GetRequiredGuid(string claimType, Func<string, ApiContextException> exceptionFactory)
    {
        var value = httpContextAccessor.HttpContext?.User.FindFirstValue(claimType);
        if (!Guid.TryParse(value, out var id))
        {
            throw exceptionFactory(claimType);
        }

        return id;
    }
}

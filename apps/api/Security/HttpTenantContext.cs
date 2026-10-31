using System.Security.Claims;

namespace Gccs.Api.Security;

public sealed class HttpTenantContext(IHttpContextAccessor httpContextAccessor) : ITenantContext
{
    private BackgroundTenantIdentity? _backgroundIdentity;

    public Guid TenantId
    {
        get
        {
            if (_backgroundIdentity is not null)
            {
                return _backgroundIdentity.TenantId;
            }

            var selectedTenant = httpContextAccessor.HttpContext?.Request.Headers[ApiSecurityExtensions.TenantSelectionHeader].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(selectedTenant))
            {
                if (Guid.TryParse(selectedTenant, out var selectedTenantId))
                {
                    return selectedTenantId;
                }

                throw new MissingTenantContextException($"The tenant selection header '{ApiSecurityExtensions.TenantSelectionHeader}' is invalid.");
            }

            var principal = httpContextAccessor.HttpContext?.User;
            if (principal?.HasClaim(
                    ApiSecurityExtensions.AuthenticationPlaneClaimType,
                    ApiSecurityExtensions.DevelopmentAuthenticationPlane) is true)
            {
                return GetRequiredGuid(
                    ApiSecurityExtensions.TenantIdClaimType,
                    claimType => new MissingTenantContextException($"The authenticated user claim '{claimType}' is missing or invalid."));
            }

            throw new MissingTenantContextException(
                $"The tenant selection header '{ApiSecurityExtensions.TenantSelectionHeader}' is required.");
        }
    }

    public Guid UserId => _backgroundIdentity?.UserId ?? GetRequiredGuid(
        ClaimTypes.NameIdentifier,
        claimType => new InvalidUserContextException($"The authenticated user claim '{claimType}' is missing or invalid."));

    public string UserEmail =>
        _backgroundIdentity?.UserEmail ??
        httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email) ??
        throw new InvalidUserContextException("The authenticated user email claim is missing.");

    public void InitializeBackground(Guid tenantId, Guid userId, string userEmail)
    {
        if (httpContextAccessor.HttpContext is not null)
        {
            throw new InvalidOperationException("Background tenant context cannot replace an active HTTP request context.");
        }

        if (_backgroundIdentity is not null)
        {
            throw new InvalidOperationException("Background tenant context is already initialized for this scope.");
        }

        if (tenantId == Guid.Empty || userId == Guid.Empty || string.IsNullOrWhiteSpace(userEmail))
        {
            throw new ArgumentException("Background tenant, user, and email values are required.");
        }

        _backgroundIdentity = new BackgroundTenantIdentity(tenantId, userId, userEmail.Trim());
    }

    private Guid GetRequiredGuid(string claimType, Func<string, ApiContextException> exceptionFactory)
    {
        var value = httpContextAccessor.HttpContext?.User.FindFirstValue(claimType);
        if (!Guid.TryParse(value, out var id))
        {
            throw exceptionFactory(claimType);
        }

        return id;
    }

    private sealed record BackgroundTenantIdentity(Guid TenantId, Guid UserId, string UserEmail);
}

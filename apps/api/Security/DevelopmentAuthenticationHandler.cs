using System.Security.Claims;
using System.Text.Encodings.Web;
using Gccs.Domain.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Gccs.Api.Security;

public sealed class DevelopmentAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string FallbackTenantId = "11111111-1111-1111-1111-111111111111";
    public const string FallbackUserId = "22222222-2222-2222-2222-222222222222";

    public string DefaultTenantId { get; set; } = FallbackTenantId;
    public string DefaultUserId { get; set; } = FallbackUserId;
    public string DefaultEmail { get; set; } = "developer@gccs.local";
}

public sealed class DevelopmentAuthenticationHandler(
    IOptionsMonitor<DevelopmentAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<DevelopmentAuthenticationOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey("X-Gccs-Dev-Auth"))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var tenantId = Request.Headers["X-Gccs-Dev-Tenant"].FirstOrDefault() ?? Options.DefaultTenantId;
        var userId = Request.Headers["X-Gccs-Dev-User"].FirstOrDefault() ?? Options.DefaultUserId;
        var email = Request.Headers["X-Gccs-Dev-Email"].FirstOrDefault() ?? Options.DefaultEmail;
        var permissions = Request.Headers["X-Gccs-Dev-Permissions"].FirstOrDefault();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Email, email),
            new(ApiSecurityExtensions.TenantIdClaimType, tenantId)
        };

        var requestedPermissions = string.IsNullOrWhiteSpace(permissions)
            ? Enum.GetNames<Permission>()
            : permissions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var permission in requestedPermissions)
        {
            claims.Add(new Claim(ApiSecurityExtensions.PermissionClaimType, permission));
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

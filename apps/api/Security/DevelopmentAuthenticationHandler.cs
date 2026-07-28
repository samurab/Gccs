using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
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
    public string DefaultPlatformPermissions { get; set; } = string.Empty;
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

        var tenantHeader = Request.Headers["X-Gccs-Dev-Tenant"].FirstOrDefault();
        var userHeader = Request.Headers["X-Gccs-Dev-User"].FirstOrDefault();
        var emailHeader = Request.Headers["X-Gccs-Dev-Email"].FirstOrDefault();
        var tenantId = tenantHeader is null ? Options.DefaultTenantId : tenantHeader;
        var email = emailHeader ?? Options.DefaultEmail;
        var userId = userHeader is null
            ? emailHeader is null
                ? Options.DefaultUserId
                : CreateDevelopmentUserId(email)
            : userHeader;
        var roleName = Request.Headers["X-Gccs-Dev-Role"].FirstOrDefault();
        var permissions = Request.Headers["X-Gccs-Dev-Permissions"].FirstOrDefault();
        var platformPermissions = Request.Headers["X-Gccs-Dev-Platform-Permissions"].FirstOrDefault() ??
            Options.DefaultPlatformPermissions;
        var canonicalRoleName = RoleCatalog.TryNormalizeRoleName(
            roleName ?? (string.IsNullOrWhiteSpace(permissions) ? RoleCatalog.Owner : string.Empty),
            out var normalizedRoleName)
            ? normalizedRoleName
            : null;

        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email)
        };

        if (!string.IsNullOrWhiteSpace(userId) &&
            !string.Equals(userId, "none", StringComparison.OrdinalIgnoreCase))
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
        }

        if (!string.IsNullOrWhiteSpace(tenantId) &&
            !string.Equals(tenantId, "none", StringComparison.OrdinalIgnoreCase))
        {
            claims.Add(new Claim(ApiSecurityExtensions.TenantIdClaimType, tenantId));
        }

        if (canonicalRoleName is not null)
        {
            claims.Add(new Claim(ApiSecurityExtensions.RoleNameClaimType, canonicalRoleName));
        }

        IEnumerable<Permission> rolePermissions = canonicalRoleName is null
            ? Array.Empty<Permission>()
            : RoleCatalog.GetPermissions(canonicalRoleName);
        var requestedPermissions = rolePermissions
            .Select(permission => permission.ToString())
            .Concat(string.IsNullOrWhiteSpace(permissions)
                ? Array.Empty<string>()
                : permissions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var permission in requestedPermissions)
        {
            claims.Add(new Claim(ApiSecurityExtensions.PermissionClaimType, permission));
        }

        foreach (var permission in platformPermissions.Split(
                     ',',
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            claims.Add(new Claim(PlatformAuthorization.PermissionClaimType, permission));
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private static string CreateDevelopmentUserId(string email)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"gccs-development-user:{normalizedEmail}"));
        hash[6] = (byte)((hash[6] & 0x0f) | 0x80);
        hash[8] = (byte)((hash[8] & 0x3f) | 0x80);
        return new Guid(hash.AsSpan(0, 16)).ToString();
    }
}

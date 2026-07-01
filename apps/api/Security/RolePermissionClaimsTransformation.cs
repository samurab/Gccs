using System.Security.Claims;
using Gccs.Domain.Identity;
using Microsoft.AspNetCore.Authentication;

namespace Gccs.Api.Security;

public sealed class RolePermissionClaimsTransformation : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var identity = principal.Identities.FirstOrDefault(identity => identity.IsAuthenticated);
        if (identity is null)
        {
            return Task.FromResult(principal);
        }

        var existingPermissions = principal
            .FindAll(ApiSecurityExtensions.PermissionClaimType)
            .Select(claim => claim.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var roleNames = principal
            .FindAll(ApiSecurityExtensions.RoleNameClaimType)
            .Concat(principal.FindAll(ClaimTypes.Role))
            .Select(claim => claim.Value)
            .Where(roleName => !string.IsNullOrWhiteSpace(roleName))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var roleName in roleNames)
        {
            foreach (var permission in RoleCatalog.GetPermissions(roleName))
            {
                var permissionName = permission.ToString();
                if (existingPermissions.Add(permissionName))
                {
                    identity.AddClaim(new Claim(ApiSecurityExtensions.PermissionClaimType, permissionName));
                }
            }
        }

        return Task.FromResult(principal);
    }
}

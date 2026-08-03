using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Gccs.Api.Security;

public static class PlatformAuthorization
{
    public const string PermissionClaimType = "gccs_platform_permission";
    public const string ProvisionTenants = "ProvisionTenants";
    public const string ProvisionTenantsPolicy = "Platform.ProvisionTenants";
    public const string ManageDemoRequests = "ManageDemoRequests";
    public const string ManageDemoRequestsPolicy = "Platform.ManageDemoRequests";
    public const string PlatformOperatorRole = "Gccs.PlatformOperator";

    public static bool CanProvisionTenants(ClaimsPrincipal user) =>
        user.HasClaim(PermissionClaimType, ProvisionTenants) ||
        user.IsInRole(PlatformOperatorRole) ||
        user.HasClaim("roles", PlatformOperatorRole);

    public static bool CanManageDemoRequests(ClaimsPrincipal user) =>
        user.HasClaim(PermissionClaimType, ManageDemoRequests) ||
        user.IsInRole(PlatformOperatorRole) || user.HasClaim("roles", PlatformOperatorRole);
}

public sealed class AllowWithoutTenantMembershipAttribute : Attribute
{
}

public static class PlatformAuthorizationEndpointExtensions
{
    public static IEndpointConventionBuilder RequireTenantProvisioningPermission(
        this IEndpointConventionBuilder builder) =>
        builder.RequireAuthorization(PlatformAuthorization.ProvisionTenantsPolicy);

    public static IEndpointConventionBuilder RequireDemoRequestManagementPermission(
        this IEndpointConventionBuilder builder) =>
        builder.RequireAuthorization(PlatformAuthorization.ManageDemoRequestsPolicy);

    public static TBuilder AllowWithoutTenantMembership<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.WithMetadata(new AllowWithoutTenantMembershipAttribute());
        return builder;
    }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Gccs.Api.Security;

public static class PlatformAuthorization
{
    public const string PermissionClaimType = "gccs_platform_permission";
    public const string ProvisionTenants = "ProvisionTenants";
    public const string ProvisionTenantsPolicy = "Platform.ProvisionTenants";
    public const string ViewPlatformCustomers = "ViewPlatformCustomers";
    public const string ViewPlatformCustomersPolicy = "Platform.ViewCustomers";
    public const string ManageTenantOnboarding = "ManageTenantOnboarding";
    public const string ManageTenantOnboardingPolicy = "Platform.ManageTenantOnboarding";
    public const string ManageTenantSubscriptions = "ManageTenantSubscriptions";
    public const string ManageTenantSubscriptionsPolicy = "Platform.ManageTenantSubscriptions";
    public const string ManageDemoRequests = "ManageDemoRequests";
    public const string ManageDemoRequestsPolicy = "Platform.ManageDemoRequests";
    public const string PlatformOperatorRole = "Gccs.PlatformOperator";

    public static bool CanProvisionTenants(ClaimsPrincipal user) =>
        user.HasClaim(PermissionClaimType, ProvisionTenants) ||
        user.HasClaim(PermissionClaimType, ManageTenantOnboarding) ||
        IsPlatformOperator(user);

    public static bool CanViewPlatformCustomers(ClaimsPrincipal user) =>
        user.HasClaim(PermissionClaimType, ViewPlatformCustomers) ||
        CanManageTenantOnboarding(user) ||
        CanManageTenantSubscriptions(user) ||
        IsPlatformOperator(user);

    public static bool CanManageTenantOnboarding(ClaimsPrincipal user) =>
        user.HasClaim(PermissionClaimType, ManageTenantOnboarding) ||
        user.HasClaim(PermissionClaimType, ProvisionTenants) ||
        IsPlatformOperator(user);

    public static bool CanManageTenantSubscriptions(ClaimsPrincipal user) =>
        user.HasClaim(PermissionClaimType, ManageTenantSubscriptions) ||
        user.HasClaim(PermissionClaimType, ProvisionTenants) ||
        IsPlatformOperator(user);

    public static bool CanManageDemoRequests(ClaimsPrincipal user) =>
        user.HasClaim(PermissionClaimType, ManageDemoRequests) ||
        IsPlatformOperator(user);

    private static bool IsPlatformOperator(ClaimsPrincipal user) =>
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

    public static IEndpointConventionBuilder RequirePlatformCustomerViewPermission(
        this IEndpointConventionBuilder builder) =>
        builder.RequireAuthorization(PlatformAuthorization.ViewPlatformCustomersPolicy);

    public static IEndpointConventionBuilder RequireTenantOnboardingManagementPermission(
        this IEndpointConventionBuilder builder) =>
        builder.RequireAuthorization(PlatformAuthorization.ManageTenantOnboardingPolicy);

    public static IEndpointConventionBuilder RequireTenantSubscriptionManagementPermission(
        this IEndpointConventionBuilder builder) =>
        builder.RequireAuthorization(PlatformAuthorization.ManageTenantSubscriptionsPolicy);

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

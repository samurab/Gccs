using System.Security.Claims;
using Gccs.Api.Security;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class AuthenticationBoundaryTests
{
    [Theory]
    [InlineData("/api/platform/me/access", true, ApiSecurityExtensions.WorkforceJwtAuthenticationScheme)]
    [InlineData("/api/platform/tenants", true, ApiSecurityExtensions.WorkforceJwtAuthenticationScheme)]
    [InlineData("/api/invitations/token", true, ApiSecurityExtensions.CustomerJwtAuthenticationScheme)]
    [InlineData("/api/me/tenants", true, ApiSecurityExtensions.CustomerJwtAuthenticationScheme)]
    [InlineData("/api/compliance/overview", true, ApiSecurityExtensions.CustomerJwtAuthenticationScheme)]
    [InlineData("/api/compliance/overview", false, ApiSecurityExtensions.WorkforceJwtAuthenticationScheme)]
    public void Route_selection_keeps_platform_and_customer_identity_planes_separate(
        string path,
        bool customerAuthenticationConfigured,
        string expectedScheme)
    {
        Assert.Equal(
            expectedScheme,
            ApiSecurityExtensions.SelectJwtAuthenticationScheme(path, customerAuthenticationConfigured));
    }

    [Fact]
    public void Customer_claim_normalization_uses_verified_email_and_removes_tenant_and_role_claims()
    {
        var objectId = Guid.Parse("9258c71b-259d-4cdc-b05d-ce5609cd3be1");
        var identity = new ClaimsIdentity(
        [
            new Claim("oid", objectId.ToString()),
            new Claim("tid", Guid.NewGuid().ToString()),
            new Claim(ApiSecurityExtensions.TenantIdClaimType, Guid.NewGuid().ToString()),
            new Claim("email", "customer@example.com"),
            new Claim("roles", PlatformAuthorization.PlatformOperatorRole),
            new Claim(ApiSecurityExtensions.AuthenticationPlaneClaimType, ApiSecurityExtensions.WorkforceAuthenticationPlane)
        ], ApiSecurityExtensions.CustomerJwtAuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        ApiSecurityExtensions.NormalizeMicrosoftEntraClaims(
            principal,
            ApiSecurityExtensions.CustomerAuthenticationPlane,
            allowUsernameEmailFallback: false);

        Assert.Equal(objectId.ToString(), principal.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal("customer@example.com", principal.FindFirstValue(ClaimTypes.Email));
        Assert.Equal(
            ApiSecurityExtensions.CustomerAuthenticationPlane,
            principal.FindFirstValue(ApiSecurityExtensions.AuthenticationPlaneClaimType));
        Assert.False(principal.HasClaim(claim => claim.Type == ApiSecurityExtensions.TenantIdClaimType));
        Assert.False(principal.HasClaim(claim => claim.Type == ClaimTypes.Role));
        Assert.False(PlatformAuthorization.CanProvisionTenants(principal));
    }

    [Fact]
    public void Customer_claim_normalization_does_not_treat_mutable_username_as_verified_email()
    {
        var identity = new ClaimsIdentity(
        [
            new Claim("oid", Guid.NewGuid().ToString()),
            new Claim("preferred_username", "unverified@example.com"),
            new Claim(ClaimTypes.Email, "untrusted-uri-claim@example.com")
        ], ApiSecurityExtensions.CustomerJwtAuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        ApiSecurityExtensions.NormalizeMicrosoftEntraClaims(
            principal,
            ApiSecurityExtensions.CustomerAuthenticationPlane,
            allowUsernameEmailFallback: false);

        Assert.Null(principal.FindFirstValue(ClaimTypes.Email));
    }

    [Fact]
    public void Claim_normalization_fails_closed_when_oid_is_missing_or_invalid()
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim("oid", "not-a-guid"),
            new Claim("email", "customer@example.com")
        ], ApiSecurityExtensions.CustomerJwtAuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        ApiSecurityExtensions.NormalizeMicrosoftEntraClaims(
            principal,
            ApiSecurityExtensions.CustomerAuthenticationPlane,
            allowUsernameEmailFallback: false);

        Assert.Null(principal.FindFirstValue(ClaimTypes.NameIdentifier));
    }

    [Fact]
    public void Workforce_claim_normalization_preserves_platform_operator_compatibility()
    {
        var identity = new ClaimsIdentity(
        [
            new Claim("oid", Guid.NewGuid().ToString()),
            new Claim("preferred_username", "operator@example.com"),
            new Claim("roles", PlatformAuthorization.PlatformOperatorRole)
        ], ApiSecurityExtensions.WorkforceJwtAuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        ApiSecurityExtensions.NormalizeMicrosoftEntraClaims(
            principal,
            ApiSecurityExtensions.WorkforceAuthenticationPlane,
            allowUsernameEmailFallback: true);

        Assert.Equal("operator@example.com", principal.FindFirstValue(ClaimTypes.Email));
        Assert.True(principal.IsInRole(PlatformAuthorization.PlatformOperatorRole));
        Assert.True(PlatformAuthorization.CanProvisionTenants(principal));
    }

    [Fact]
    public void Production_customer_tenant_context_requires_server_authorized_selection_header()
    {
        var tenantId = Guid.NewGuid();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Email, "customer@example.com"),
            new Claim(ApiSecurityExtensions.AuthenticationPlaneClaimType, ApiSecurityExtensions.CustomerAuthenticationPlane),
            new Claim(ApiSecurityExtensions.TenantIdClaimType, tenantId.ToString())
        ], ApiSecurityExtensions.CustomerJwtAuthenticationScheme));
        var httpContext = new DefaultHttpContext { User = principal };
        var tenantContext = new HttpTenantContext(new HttpContextAccessor { HttpContext = httpContext });

        Assert.Throws<MissingTenantContextException>(() => tenantContext.TenantId);

        httpContext.Request.Headers[ApiSecurityExtensions.TenantSelectionHeader] = tenantId.ToString();
        Assert.Equal(tenantId, tenantContext.TenantId);
    }
}

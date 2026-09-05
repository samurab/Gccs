using System.Security.Claims;
using Gccs.Api.Security;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class RolePermissionClaimsTransformationTests
{
    [Fact]
    public async Task TransformAsync_MaterializesRoleClaimsBeforeAddingPermissionClaims()
    {
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Role, "Owner")
            ],
            authenticationType: "Bearer");
        var principal = new ClaimsPrincipal(identity);
        var transformation = new RolePermissionClaimsTransformation();

        var transformed = await transformation.TransformAsync(principal);

        Assert.Contains(
            transformed.FindAll(ApiSecurityExtensions.PermissionClaimType),
            claim => claim.Value == "ManageUsers");
    }

    [Fact]
    public void NormalizeMicrosoftEntraClaims_MapsObjectIdentifierButNotDirectoryTenantToWorkspace()
    {
        var tenantId = Guid.Parse("8c934636-0c37-4a8f-9134-323bef993ef2");
        var userId = Guid.Parse("09e188fa-befc-4b99-822b-d641767cb7b9");
        var identity = new ClaimsIdentity(
            [
                new Claim("http://schemas.microsoft.com/identity/claims/tenantid", tenantId.ToString()),
                new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", userId.ToString()),
                new Claim(ClaimTypes.NameIdentifier, "mapped-subject")
            ],
            authenticationType: "Bearer");
        var principal = new ClaimsPrincipal(identity);

        ApiSecurityExtensions.NormalizeMicrosoftEntraClaims(
            principal,
            ApiSecurityExtensions.WorkforceAuthenticationPlane,
            allowUsernameEmailFallback: true);

        Assert.Null(principal.FindFirstValue(ApiSecurityExtensions.TenantIdClaimType));
        Assert.Equal(userId.ToString(), principal.FindFirstValue(ClaimTypes.NameIdentifier));
    }

    [Fact]
    public void NormalizeMicrosoftEntraClaims_PreservesSignedEmailInsteadOfGuestUserPrincipalName()
    {
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Email, "alice@example.com"),
                new Claim("preferred_username", "alice_example.com#EXT#@tenant.example.onmicrosoft.com")
            ],
            authenticationType: "Bearer");
        var principal = new ClaimsPrincipal(identity);

        ApiSecurityExtensions.NormalizeMicrosoftEntraClaims(
            principal,
            ApiSecurityExtensions.WorkforceAuthenticationPlane,
            allowUsernameEmailFallback: true);

        Assert.Equal("alice@example.com", principal.FindFirstValue(ClaimTypes.Email));
        Assert.DoesNotContain(
            principal.FindAll(ClaimTypes.Email),
            claim => claim.Value.Contains("#EXT#", StringComparison.OrdinalIgnoreCase));
    }
}

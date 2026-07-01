using System.Security.Claims;
using System.Reflection;
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
    public void NormalizeMicrosoftEntraClaims_MapsInboundTenantAndObjectIdentifierClaims()
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

        typeof(ApiSecurityExtensions)
            .GetMethod("NormalizeMicrosoftEntraClaims", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, [principal]);

        Assert.Equal(tenantId.ToString(), principal.FindFirstValue(ApiSecurityExtensions.TenantIdClaimType));
        Assert.Equal(userId.ToString(), principal.FindFirstValue(ClaimTypes.NameIdentifier));
    }
}

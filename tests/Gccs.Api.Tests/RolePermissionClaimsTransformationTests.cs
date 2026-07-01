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
}

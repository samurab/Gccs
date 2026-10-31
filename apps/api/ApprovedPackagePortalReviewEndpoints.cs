using System.Security.Claims;
using Gccs.Api.Security;
using Gccs.Application.Portals;

namespace Gccs.Api;

public static class ApprovedPackagePortalReviewEndpoints
{
    public static RouteGroupBuilder MapApprovedPackagePortalReviewEndpoints(this RouteGroupBuilder api)
    {
        var portal = api.MapGroup("/external-portal/invitations/{invitationId:guid}/packages")
            .AllowWithoutTenantMembership()
            .RequireAuthorization(policy => policy.RequireAssertion(context =>
                context.User.HasClaim(ApiSecurityExtensions.AuthenticationPlaneClaimType, ApiSecurityExtensions.CustomerAuthenticationPlane) ||
                context.User.HasClaim(ApiSecurityExtensions.AuthenticationPlaneClaimType, ApiSecurityExtensions.DevelopmentAuthenticationPlane)));

        portal.MapGet("/", async (
            Guid invitationId, ClaimsPrincipal user, ApprovedPackagePortalReviewService service,
            HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            if (!TryGetIdentity(user, out var identity)) return InvalidIdentity(httpContext);
            try
            {
                var packages = await service.ListPackagesAsync(invitationId, identity!, cancellationToken);
                return Results.Ok(packages);
            }
            catch (PortalPackageAccessDeniedException) { return AccessDenied(httpContext); }
            catch (ExternalPortalAccessConflictException exception) { return Conflict(httpContext, exception); }
        }).WithName("ListApprovedPortalPackages");

        portal.MapGet("/{sharedPackageId:guid}", async (
            Guid invitationId, Guid sharedPackageId, ClaimsPrincipal user,
            ApprovedPackagePortalReviewService service,
            HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            if (!TryGetIdentity(user, out var identity)) return InvalidIdentity(httpContext);
            try
            {
                var package = await service.GetPackageAsync(invitationId, sharedPackageId, identity!, cancellationToken);
                return Results.Ok(package);
            }
            catch (PortalPackageAccessDeniedException) { return AccessDenied(httpContext); }
            catch (ExternalPortalAccessConflictException exception) { return Conflict(httpContext, exception); }
        }).WithName("GetApprovedPortalPackage");

        portal.MapPost("/{sharedPackageId:guid}/messages", async (
            Guid invitationId, Guid sharedPackageId, PortalPackageReviewMessageRequest request,
            ClaimsPrincipal user, ApprovedPackagePortalReviewService service,
            HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            if (!TryGetIdentity(user, out var identity)) return InvalidIdentity(httpContext);
            try
            {
                var message = await service.AddMessageAsync(
                    invitationId, sharedPackageId, request, identity!, cancellationToken);
                return Results.Created(
                    $"/api/external-portal/invitations/{invitationId}/packages/{sharedPackageId}", message);
            }
            catch (PortalPackageValidationException exception)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["body"] = [exception.Message] });
            }
            catch (PortalPackageAccessDeniedException) { return AccessDenied(httpContext); }
            catch (ExternalPortalAccessConflictException exception) { return Conflict(httpContext, exception); }
        }).WithName("CreatePortalPackageReviewMessage");

        portal.MapGet("/{sharedPackageId:guid}/download", async (
            Guid invitationId, Guid sharedPackageId, ClaimsPrincipal user,
            ApprovedPackagePortalReviewService service,
            HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            if (!TryGetIdentity(user, out var identity)) return InvalidIdentity(httpContext);
            try
            {
                var download = await service.DownloadAsync(invitationId, sharedPackageId, identity!, cancellationToken);
                httpContext.Response.Headers["X-FeDril-Package-Id"] = download.Metadata.PackageId.ToString();
                httpContext.Response.Headers["X-FeDril-Package-Version"] = download.Metadata.Version.ToString();
                httpContext.Response.Headers["X-FeDril-Watermark-Applied"] = (download.Metadata.Watermark is not null).ToString().ToLowerInvariant();
                return Results.File(download.Content, download.ContentType, download.FileName);
            }
            catch (PortalPackageAccessDeniedException) { return AccessDenied(httpContext); }
            catch (ExternalPortalAccessConflictException exception) { return Conflict(httpContext, exception); }
        }).WithName("DownloadApprovedPortalPackage");

        return api;
    }

    private static bool TryGetIdentity(ClaimsPrincipal user, out PortalReviewerIdentity? identity)
    {
        identity = null;
        if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var actorUserId)) return false;
        var email = user.FindFirstValue(ClaimTypes.Email)?.Trim();
        if (string.IsNullOrWhiteSpace(email)) return false;
        identity = new PortalReviewerIdentity(actorUserId, email, ExternalPortalAccessEndpoints.HasStrongAuthentication(user));
        return true;
    }

    private static IResult InvalidIdentity(HttpContext httpContext) =>
        ApiProblemDetails.Create(httpContext, "Invalid portal identity",
            "The authenticated portal identity is missing required verified claims.",
            StatusCodes.Status401Unauthorized, "invalid_portal_identity");

    private static IResult AccessDenied(HttpContext httpContext) =>
        ApiProblemDetails.Create(httpContext, "Resource not found",
            "The requested package is unavailable or outside the active portal scope.",
            StatusCodes.Status404NotFound, "resource_not_found");

    private static IResult Conflict(HttpContext httpContext, Exception exception) =>
        ApiProblemDetails.Create(httpContext, "Portal package conflict", exception.Message,
            StatusCodes.Status409Conflict, "portal_package_conflict");
}

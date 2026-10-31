using Gccs.Api.Security;
using Gccs.Application.Portals;
using Gccs.Domain.Identity;

namespace Gccs.Api;

public static class PortalReviewPackagePreparationEndpoints
{
    public static RouteGroupBuilder MapPortalReviewPackagePreparationEndpoints(this RouteGroupBuilder api)
    {
        api.MapPost("/portal/review-packages/prepare", async (
            PreparePortalReviewPackageRequest request,
            PortalReviewPackagePreparationService service,
            ITenantContext tenantContext,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var additionalPermission = request.SourceType switch
            {
                PortalReviewPreparationSource.ContractObligationMatrix => Permission.ViewObligations,
                PortalReviewPreparationSource.AuditLogExport => Permission.ViewAuditLog,
                _ => (Permission?)null
            };
            if (additionalPermission is not null &&
                !httpContext.User.HasClaim(ApiSecurityExtensions.PermissionClaimType, additionalPermission.Value.ToString()))
                return ApiProblemDetails.Create(httpContext, "Forbidden",
                    "The requested package source is not available to this user.",
                    StatusCodes.Status403Forbidden, "forbidden");
            try
            {
                var package = await service.PrepareAsync(
                    request, tenantContext.TenantId, tenantContext.UserId, cancellationToken);
                return Results.Created($"/api/reports/{package.PackageId}", package);
            }
            catch (PortalPackageValidationException exception)
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]> { ["package"] = [exception.Message] },
                    title: "Portal review package invalid", detail: exception.Message,
                    statusCode: StatusCodes.Status400BadRequest);
            }
            catch (PortalReviewPackageSourceNotFoundException)
            {
                return ApiProblemDetails.Create(httpContext, "Resource not found",
                    "The requested package source was not found.",
                    StatusCodes.Status404NotFound, "resource_not_found");
            }
        })
        .RequirePermission(Permission.ManageReports)
        .WithName("PreparePortalReviewPackage");

        return api;
    }
}

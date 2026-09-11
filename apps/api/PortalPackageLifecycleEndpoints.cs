using Gccs.Api.Security;
using Gccs.Application.Portals;
using Gccs.Domain.Identity;

namespace Gccs.Api;

public static class PortalPackageLifecycleEndpoints
{
    public static RouteGroupBuilder MapPortalPackageLifecycleEndpoints(this RouteGroupBuilder api)
    {
        var packages = api.MapGroup("/portal/shared-packages");

        packages.MapGet("/", async (
            PortalPackageLifecycleService service,
            ITenantContext tenantContext,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.ListAsync(tenantContext.TenantId, cancellationToken)))
            .RequirePermission(Permission.ManageUsers)
            .WithName("ListSharedPortalPackages");

        packages.MapPost("/", async (
            SharedPortalPackageRequest request,
            PortalPackageLifecycleService service,
            ITenantContext tenantContext,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
            await ExecuteAsync(
                () => service.ShareAsync(request, tenantContext.TenantId, tenantContext.UserId, cancellationToken),
                httpContext,
                created: true))
            .RequirePermission(Permission.ManageUsers)
            .WithName("SharePortalPackage");

        packages.MapPost("/{sharedPackageId:guid}/expire", async (
            Guid sharedPackageId,
            PortalPackageLifecycleService service,
            ITenantContext tenantContext,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
            await ExecuteNullableAsync(
                () => service.ExpireAsync(sharedPackageId, tenantContext.TenantId, tenantContext.UserId, cancellationToken),
                httpContext))
            .RequirePermission(Permission.ManageUsers)
            .WithName("ExpireSharedPortalPackage");

        packages.MapPost("/{sharedPackageId:guid}/revoke", async (
            Guid sharedPackageId,
            RevokeSharedPortalPackageRequest request,
            PortalPackageLifecycleService service,
            ITenantContext tenantContext,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
            await ExecuteNullableAsync(
                () => service.RevokeAsync(sharedPackageId, tenantContext.TenantId, request.Reason, tenantContext.UserId, cancellationToken),
                httpContext))
            .RequirePermission(Permission.ManageUsers)
            .WithName("RevokeSharedPortalPackage");

        packages.MapPost("/{sharedPackageId:guid}/supersede", async (
            Guid sharedPackageId,
            SupersedeSharedPortalPackageRequest request,
            PortalPackageLifecycleService service,
            ITenantContext tenantContext,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
            await ExecuteNullableAsync(
                () => service.SupersedeAsync(sharedPackageId, tenantContext.TenantId, request.ReplacementSharedPackageId, tenantContext.UserId, cancellationToken),
                httpContext))
            .RequirePermission(Permission.ManageUsers)
            .WithName("SupersedeSharedPortalPackage");

        packages.MapPost("/{sharedPackageId:guid}/reissue", async (
            Guid sharedPackageId,
            ReissueSharedPortalPackageRequest request,
            PortalPackageLifecycleService service,
            ITenantContext tenantContext,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
            await ExecuteNullableAsync(
                () => service.ReissueAsync(sharedPackageId, tenantContext.TenantId, request, tenantContext.UserId, cancellationToken),
                httpContext,
                created: true))
            .RequirePermission(Permission.ManageUsers)
            .WithName("ReissueSharedPortalPackage");

        packages.MapPost("/{sharedPackageId:guid}/archive", async (
            Guid sharedPackageId,
            PortalPackageLifecycleService service,
            ITenantContext tenantContext,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
            await ExecuteNullableAsync(
                () => service.ArchiveAsync(sharedPackageId, tenantContext.TenantId, tenantContext.UserId, cancellationToken),
                httpContext))
            .RequirePermission(Permission.ManageUsers)
            .WithName("ArchiveSharedPortalPackage");

        packages.MapGet("/activity-report", async (
            PortalPackageLifecycleService service,
            ITenantContext tenantContext,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.GenerateActivityReportAsync(tenantContext.TenantId, cancellationToken)))
            .RequirePermission(Permission.ViewAuditLog)
            .WithName("GetPortalPackageActivityReport");

        return api;
    }

    private static async Task<IResult> ExecuteAsync<T>(
        Func<Task<T>> action,
        HttpContext httpContext,
        bool created = false)
    {
        try
        {
            var result = await action();
            return created ? Results.Created(uri: (string?)null, value: result) : Results.Ok(result);
        }
        catch (PortalPackageLifecycleConflictException exception)
        {
            return ApiProblemDetails.Create(httpContext, "Lifecycle conflict", exception.Message,
                StatusCodes.Status409Conflict, "portal_package_lifecycle_conflict");
        }
        catch (PortalPackageLifecycleException exception)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["sharedPackage"] = [exception.Message]
            });
        }
    }

    private static async Task<IResult> ExecuteNullableAsync<T>(
        Func<Task<T?>> action,
        HttpContext httpContext,
        bool created = false) where T : class
    {
        try
        {
            var result = await action();
            if (result is null)
                return ApiProblemDetails.Create(httpContext, "Resource not found",
                    "Shared portal package was not found in the current tenant scope.",
                    StatusCodes.Status404NotFound, "resource_not_found");
            return created ? Results.Created(uri: (string?)null, value: result) : Results.Ok(result);
        }
        catch (PortalPackageLifecycleConflictException exception)
        {
            return ApiProblemDetails.Create(httpContext, "Lifecycle conflict", exception.Message,
                StatusCodes.Status409Conflict, "portal_package_lifecycle_conflict");
        }
        catch (PortalPackageLifecycleException exception)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["sharedPackage"] = [exception.Message]
            });
        }
    }
}

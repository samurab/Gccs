using System.Security.Claims;
using Gccs.Api.Security;
using Gccs.Application.Common;
using Gccs.Application.Portals;
using Gccs.Domain.Identity;

namespace Gccs.Api;

public static class ExternalPortalAccessEndpoints
{
    public static RouteGroupBuilder MapExternalPortalAccessEndpoints(this RouteGroupBuilder api)
    {
        var invitations = api.MapGroup("/portal/invitations");

        invitations.MapGet("/", async (
            ExternalPortalAccessService service, ITenantContext tenantContext, CancellationToken cancellationToken) =>
            Results.Ok(await service.ListAsync(tenantContext.TenantId, cancellationToken)))
            .RequirePermission(Permission.ManageUsers)
            .WithName("ListExternalPortalInvitations");

        invitations.MapPost("/", async (
            ExternalPortalInvitationRequest request, ExternalPortalAccessService service,
            IApplicationTransaction transaction, ITenantContext tenantContext,
            HttpContext httpContext, CancellationToken cancellationToken) =>
            await ExecuteAsync(
                () => transaction.ExecuteAsync(
                    token => service.InviteAsync(request, tenantContext.TenantId, tenantContext.UserId, token),
                    cancellationToken),
                httpContext, created: true))
            .RequirePermission(Permission.ManageUsers)
            .WithName("CreateExternalPortalInvitation");

        invitations.MapPost("/{invitationId:guid}/resend", async (
            Guid invitationId, ExternalPortalAccessService service, IApplicationTransaction transaction,
            ITenantContext tenantContext,
            HttpContext httpContext, CancellationToken cancellationToken) =>
            await ExecuteAsync(
                () => transaction.ExecuteAsync(
                    token => service.ResendAsync(invitationId, tenantContext.TenantId, tenantContext.UserId, token),
                    cancellationToken),
                httpContext))
            .RequirePermission(Permission.ManageUsers)
            .WithName("ResendExternalPortalInvitation");

        invitations.MapPost("/{invitationId:guid}/extend", async (
            Guid invitationId, ExtendExternalPortalInvitationRequest request, ExternalPortalAccessService service,
            IApplicationTransaction transaction, ITenantContext tenantContext,
            HttpContext httpContext, CancellationToken cancellationToken) =>
            await ExecuteAsync(
                () => transaction.ExecuteAsync(
                    token => service.ExtendAsync(invitationId, tenantContext.TenantId, request.ExpiresAt,
                        tenantContext.UserId, token), cancellationToken), httpContext))
            .RequirePermission(Permission.ManageUsers)
            .WithName("ExtendExternalPortalInvitation");

        invitations.MapPost("/{invitationId:guid}/revoke", async (
            Guid invitationId, RevokeExternalPortalInvitationRequest request, ExternalPortalAccessService service,
            IApplicationTransaction transaction, ITenantContext tenantContext,
            HttpContext httpContext, CancellationToken cancellationToken) =>
            await ExecuteAsync(
                () => transaction.ExecuteAsync(
                    token => service.RevokeAsync(invitationId, tenantContext.TenantId, request.Reason,
                        tenantContext.UserId, token), cancellationToken), httpContext))
            .RequirePermission(Permission.ManageUsers)
            .WithName("RevokeExternalPortalInvitation");

        invitations.MapGet("/{invitationId:guid}/access-history", async (
            Guid invitationId, ExternalPortalAccessService service, ITenantContext tenantContext,
            HttpContext httpContext, CancellationToken cancellationToken) =>
            await ExecuteAsync(
                () => service.ListAccessHistoryAsync(invitationId, tenantContext.TenantId, cancellationToken),
                httpContext))
            .RequirePermission(Permission.ManageUsers)
            .WithName("ListExternalPortalAccessHistory");

        api.MapGet("/external-portal/invitations/{invitationId:guid}/access", async (
            Guid invitationId, Guid packageId, Guid? contractId, bool? download, ClaimsPrincipal user,
            ExternalPortalAccessService service, IApplicationTransaction transaction,
            TimeProvider timeProvider, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var actorUserId) ||
                string.IsNullOrWhiteSpace(user.FindFirstValue(ClaimTypes.Email)))
                return ApiProblemDetails.Create(httpContext, "Invalid portal identity",
                    "The authenticated portal identity is missing required verified claims.",
                    StatusCodes.Status401Unauthorized, "invalid_portal_identity");

            try
            {
                var result = await transaction.ExecuteAsync(
                    token => service.ValidateAccessAsync(new ExternalPortalAccessRequest(
                        invitationId, packageId, contractId, actorUserId, user.FindFirstValue(ClaimTypes.Email),
                        HasStrongAuthentication(user), timeProvider.GetUtcNow(), download is true), token), cancellationToken);
                return result.Allowed
                    ? Results.Ok(result)
                    : ApiProblemDetails.Create(httpContext, "Portal access unavailable",
                        "The invitation is unavailable or the requested resource is outside its active scope.",
                        StatusCodes.Status403Forbidden, "portal_access_denied");
            }
            catch (ExternalPortalAccessConflictException exception)
            {
                return ApiProblemDetails.Create(httpContext, "Portal access conflict", exception.Message,
                    StatusCodes.Status409Conflict, "portal_access_conflict");
            }
        })
        .AllowWithoutTenantMembership()
        .RequireAuthorization(policy => policy.RequireAssertion(context =>
            context.User.HasClaim(ApiSecurityExtensions.AuthenticationPlaneClaimType, ApiSecurityExtensions.CustomerAuthenticationPlane) ||
            context.User.HasClaim(ApiSecurityExtensions.AuthenticationPlaneClaimType, ApiSecurityExtensions.DevelopmentAuthenticationPlane)))
        .WithName("ValidateExternalPortalAccess");

        return api;
    }

    internal static bool HasStrongAuthentication(ClaimsPrincipal user)
    {
        var methods = user.FindAll("amr").Concat(user.FindAll("http://schemas.microsoft.com/claims/authnmethodsreferences"));
        return methods.Any(claim => claim.Value.Split(' ', ',', StringSplitOptions.RemoveEmptyEntries)
            .Any(value => string.Equals(value.Trim('[', ']', '"'), "mfa", StringComparison.OrdinalIgnoreCase))) ||
            user.FindAll("acr").Any(claim => claim.Value is "2" or "3");
    }

    private static async Task<IResult> ExecuteAsync<T>(
        Func<Task<T>> action, HttpContext httpContext, bool created = false)
    {
        try
        {
            var result = await action();
            return result is null
                ? ApiProblemDetails.Create(httpContext, "Resource not found", "Portal invitation was not found.",
                    StatusCodes.Status404NotFound, "resource_not_found")
                : created ? Results.Created($"/api/portal/invitations/{GetId(result)}", result) : Results.Ok(result);
        }
        catch (ExternalPortalInvitationNotFoundException)
        {
            return ApiProblemDetails.Create(httpContext, "Resource not found", "Portal invitation was not found.",
                StatusCodes.Status404NotFound, "resource_not_found");
        }
        catch (ExternalPortalAccessConflictException exception)
        {
            return ApiProblemDetails.Create(httpContext, "Portal invitation conflict", exception.Message,
                StatusCodes.Status409Conflict, "portal_invitation_conflict");
        }
        catch (ExternalPortalInvitationStateException exception)
        {
            return ApiProblemDetails.Create(httpContext, "Portal invitation state conflict", exception.Message,
                StatusCodes.Status409Conflict, "portal_invitation_state_conflict");
        }
        catch (ExternalPortalAccessException exception)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["invitation"] = [exception.Message] });
        }
    }

    private static Guid GetId<T>(T value) =>
        value is ExternalPortalInvitationDto invitation ? invitation.Id : Guid.Empty;
}

public sealed record ExtendExternalPortalInvitationRequest(DateTimeOffset ExpiresAt);
public sealed record RevokeExternalPortalInvitationRequest(string Reason);

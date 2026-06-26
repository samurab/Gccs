using System.Security.Claims;
using Gccs.Application.Audit;
using Gccs.Domain.Audit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace Gccs.Api.Security;

public sealed class ProblemDetailsAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Forbidden)
        {
            await TryWriteFailedAuthorizationAuditAsync(context);
            await ApiProblemDetails
                .Create(
                    context,
                    "Permission denied",
                    "You do not have permission to perform this tenant-scoped action.",
                    StatusCodes.Status403Forbidden,
                    "permission_denied")
                .ExecuteAsync(context);
            return;
        }

        if (authorizeResult.Challenged)
        {
            await ApiProblemDetails
                .Create(
                    context,
                    "Authentication required",
                    "Authentication is required to access this tenant-scoped API.",
                    StatusCodes.Status401Unauthorized,
                    "authentication_required")
                .ExecuteAsync(context);
            return;
        }

        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }

    private static async Task TryWriteFailedAuthorizationAuditAsync(HttpContext context)
    {
        if (!TryReadGuid(context.User, ApiSecurityExtensions.TenantIdClaimType, out var tenantId) ||
            !TryReadGuid(context.User, ClaimTypes.NameIdentifier, out var userId))
        {
            return;
        }

        try
        {
            var auditEventWriter = context.RequestServices.GetService<IAuditEventWriter>();
            if (auditEventWriter is null)
            {
                return;
            }

            await auditEventWriter.WriteAsync(
                tenantId,
                userId,
                AuditAction.Rejected,
                "Authorization",
                context.Request.Path.Value ?? string.Empty,
                "Authorization attempt was denied.",
                new Dictionary<string, string>
                {
                    ["method"] = context.Request.Method,
                    ["path"] = context.Request.Path.Value ?? string.Empty
                },
                context.RequestAborted);
        }
        catch (Exception)
        {
            // Failed-authorization audit is best-effort because permission denial must not become a 500.
        }
    }

    private static bool TryReadGuid(ClaimsPrincipal user, string claimType, out Guid value) =>
        Guid.TryParse(user.FindFirstValue(claimType), out value);
}

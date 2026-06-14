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
}

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
            await Results.Problem(
                    title: "Permission denied",
                    detail: "You do not have permission to perform this tenant-scoped action.",
                    statusCode: StatusCodes.Status403Forbidden,
                    extensions: new Dictionary<string, object?>
                    {
                        ["errorCode"] = "permission_denied"
                    })
                .ExecuteAsync(context);
            return;
        }

        if (authorizeResult.Challenged)
        {
            await Results.Problem(
                    title: "Authentication required",
                    detail: "Authentication is required to access this tenant-scoped API.",
                    statusCode: StatusCodes.Status401Unauthorized,
                    extensions: new Dictionary<string, object?>
                    {
                        ["errorCode"] = "authentication_required"
                    })
                .ExecuteAsync(context);
            return;
        }

        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }
}

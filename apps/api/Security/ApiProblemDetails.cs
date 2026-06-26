namespace Gccs.Api.Security;

public static class ApiProblemDetails
{
    public static void Customize(ProblemDetailsContext context)
    {
        var problem = context.ProblemDetails;
        var statusCode = problem.Status ?? context.HttpContext.Response.StatusCode;

        problem.Status ??= statusCode;
        problem.Title = string.IsNullOrWhiteSpace(problem.Title)
            ? DefaultTitle(statusCode)
            : problem.Title;
        problem.Detail = string.IsNullOrWhiteSpace(problem.Detail)
            ? DefaultDetail(statusCode)
            : problem.Detail;

        problem.Extensions.TryAdd("errorCode", DefaultErrorCode(statusCode));
        problem.Extensions.TryAdd("traceId", context.HttpContext.TraceIdentifier);
        problem.Extensions.TryAdd("correlationId", ApiCorrelation.Get(context.HttpContext));
    }

    public static IResult Create(
        HttpContext httpContext,
        string title,
        string detail,
        int statusCode,
        string errorCode,
        IReadOnlyDictionary<string, object?>? additionalExtensions = null)
    {
        var correlationId = ApiCorrelation.Get(httpContext);
        var extensions = new Dictionary<string, object?>
        {
            ["errorCode"] = errorCode,
            ["traceId"] = httpContext.TraceIdentifier,
            ["correlationId"] = correlationId
        };

        if (additionalExtensions is not null)
        {
            foreach (var extension in additionalExtensions)
            {
                extensions[extension.Key] = extension.Value;
            }
        }

        return Results.Problem(
            title: title,
            detail: detail,
            statusCode: statusCode,
            extensions: extensions);
    }

    private static string DefaultTitle(int statusCode) =>
        statusCode switch
        {
            StatusCodes.Status400BadRequest => "Validation failed",
            StatusCodes.Status401Unauthorized => "Authentication required",
            StatusCodes.Status403Forbidden => "Permission denied",
            StatusCodes.Status404NotFound => "Resource not found",
            StatusCodes.Status409Conflict => "Conflict",
            StatusCodes.Status429TooManyRequests => "Too many API requests",
            StatusCodes.Status500InternalServerError => "API request failed",
            _ => "API request failed"
        };

    private static string DefaultDetail(int statusCode) =>
        statusCode switch
        {
            StatusCodes.Status400BadRequest => "The request did not pass validation.",
            StatusCodes.Status401Unauthorized => "Authentication is required to access this API.",
            StatusCodes.Status403Forbidden => "You do not have permission to perform this action.",
            StatusCodes.Status404NotFound => "The requested resource was not found.",
            StatusCodes.Status409Conflict => "The request conflicts with the current resource state.",
            StatusCodes.Status429TooManyRequests => "The API rate limit was reached. Wait briefly and try again.",
            StatusCodes.Status500InternalServerError => "The API request could not be completed.",
            _ => "The API request could not be completed."
        };

    private static string DefaultErrorCode(int statusCode) =>
        statusCode switch
        {
            StatusCodes.Status400BadRequest => "validation_failed",
            StatusCodes.Status401Unauthorized => "authentication_required",
            StatusCodes.Status403Forbidden => "permission_denied",
            StatusCodes.Status404NotFound => "resource_not_found",
            StatusCodes.Status409Conflict => "conflict",
            StatusCodes.Status429TooManyRequests => "rate_limit_exceeded",
            StatusCodes.Status500InternalServerError => "api_request_failed",
            _ => "api_request_failed"
        };
}

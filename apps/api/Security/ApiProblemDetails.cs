namespace Gccs.Api.Security;

public static class ApiProblemDetails
{
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
}

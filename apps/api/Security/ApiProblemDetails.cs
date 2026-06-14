namespace Gccs.Api.Security;

public static class ApiProblemDetails
{
    public static IResult Create(
        HttpContext httpContext,
        string title,
        string detail,
        int statusCode,
        string errorCode)
    {
        var correlationId = ApiCorrelation.Get(httpContext);

        return Results.Problem(
            title: title,
            detail: detail,
            statusCode: statusCode,
            extensions: new Dictionary<string, object?>
            {
                ["errorCode"] = errorCode,
                ["traceId"] = httpContext.TraceIdentifier,
                ["correlationId"] = correlationId
            });
    }
}

namespace Gccs.Api.Security;

public static class ApiCorrelation
{
    public const string HeaderName = "X-Correlation-ID";
    private const string ItemKey = "__gccs_correlation_id";

    public static string Ensure(HttpContext httpContext)
    {
        if (httpContext.Items.TryGetValue(ItemKey, out var existing) &&
            existing is string existingCorrelationId &&
            !string.IsNullOrWhiteSpace(existingCorrelationId))
        {
            return existingCorrelationId;
        }

        var requestedCorrelationId = httpContext.Request.Headers[HeaderName].FirstOrDefault();
        var correlationId = string.IsNullOrWhiteSpace(requestedCorrelationId)
            ? Guid.NewGuid().ToString("N")
            : requestedCorrelationId.Trim();

        httpContext.TraceIdentifier = correlationId;
        httpContext.Items[ItemKey] = correlationId;

        return correlationId;
    }

    public static string Get(HttpContext httpContext) => Ensure(httpContext);
}

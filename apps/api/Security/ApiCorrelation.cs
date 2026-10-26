namespace Gccs.Api.Security;

public static class ApiCorrelation
{
    public const string HeaderName = "X-Correlation-ID";
    private const string ItemKey = "__gccs_correlation_id";
    private const int MaximumLength = 120;

    public static string Ensure(HttpContext httpContext)
    {
        if (httpContext.Items.TryGetValue(ItemKey, out var existing) &&
            existing is string existingCorrelationId &&
            IsValid(existingCorrelationId))
        {
            httpContext.TraceIdentifier = existingCorrelationId;
            return existingCorrelationId;
        }

        var requestedCorrelationId = httpContext.Request.Headers[HeaderName].FirstOrDefault();
        var normalizedCorrelationId = requestedCorrelationId?.Trim();
        var correlationId = IsValid(normalizedCorrelationId)
            ? normalizedCorrelationId!
            : Guid.NewGuid().ToString("N");

        httpContext.TraceIdentifier = correlationId;
        httpContext.Items[ItemKey] = correlationId;

        return correlationId;
    }

    public static string Get(HttpContext httpContext) => Ensure(httpContext);

    private static bool IsValid(string? correlationId)
    {
        if (string.IsNullOrEmpty(correlationId) || correlationId.Length > MaximumLength)
        {
            return false;
        }

        foreach (var character in correlationId)
        {
            if (!char.IsAsciiLetterOrDigit(character) && character is not ('-' or '_' or '.' or ':'))
            {
                return false;
            }
        }

        return true;
    }
}

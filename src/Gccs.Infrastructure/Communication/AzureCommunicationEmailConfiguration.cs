namespace Gccs.Infrastructure.Communication;

public static class AzureCommunicationEmailConfiguration
{
    public static bool HasHttpsEndpoint(bool useManagedIdentity, string? endpoint, string? connectionString)
    {
        if (useManagedIdentity)
        {
            return IsHttps(endpoint);
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return false;
        }

        var endpoints = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2, StringSplitOptions.TrimEntries))
            .Where(part => part.Length == 2 && string.Equals(part[0], "endpoint", StringComparison.OrdinalIgnoreCase))
            .Select(part => part[1])
            .ToArray();

        // Reject duplicates rather than relying on another parser's first/last-value choice.
        return endpoints.Length == 1 && IsHttps(endpoints[0]);
    }

    private static bool IsHttps(string? endpoint) =>
        Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
}

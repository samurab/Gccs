using System.Text.Json;

namespace Gccs.Api.LocalDevelopment;

internal static class ExistingDevelopmentApi
{
    public static async Task<DevelopmentStartupLease?> CoordinateStartupAsync(IConfiguration configuration, IHostEnvironment environment)
    {
        var urls = GetLocalUrls(configuration, environment);
        if (urls is null) return null;
        return await DevelopmentStartupLease.AcquireAsync(
            urls.Select(url => url.Port), () => FindHealthyUrlAsync(configuration, environment));
    }

    private static List<Uri>? GetLocalUrls(IConfiguration configuration, IHostEnvironment environment)
    {
        // Explicit Kestrel endpoints can override URLs; do not probe a different listener.
        if (!environment.IsDevelopment() || configuration.GetSection("Kestrel:Endpoints").GetChildren().Any())
        {
            return null;
        }

        var addresses = configuration["urls"]?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (addresses is null || addresses.Length == 0)
        {
            return null;
        }

        var urls = new List<Uri>();
        foreach (var address in addresses)
        {
            if (!Uri.TryCreate(address, UriKind.Absolute, out var url) ||
                url.Scheme != Uri.UriSchemeHttp || !url.IsLoopback || url.Port == 0 ||
                url.UserInfo.Length != 0 || url.AbsolutePath != "/" || url.Query.Length != 0 || url.Fragment.Length != 0)
            {
                return null;
            }
            urls.Add(url);
        }

        return urls;
    }

    public static async Task<string?> FindHealthyUrlAsync(IConfiguration configuration, IHostEnvironment environment)
    {
        var urls = GetLocalUrls(configuration, environment);
        if (urls is null) return null;

        using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, UseProxy = false })
        {
            Timeout = TimeSpan.FromSeconds(2),
            MaxResponseContentBufferSize = 16_384
        };
        foreach (var url in urls)
        {
            try
            {
                using var response = await client.GetAsync(new Uri(url, "/health"));
                if (!response.IsSuccessStatusCode) return null;
                using var health = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var root = health.RootElement;
                if (root.ValueKind != JsonValueKind.Object ||
                    !root.TryGetProperty("service", out var service) || service.ValueKind != JsonValueKind.String || service.GetString() != "gccs-api" ||
                    !root.TryGetProperty("status", out var status) || status.ValueKind != JsonValueKind.String || status.GetString() != "ok")
                {
                    return null;
                }
            }
            catch (Exception error) when (error is HttpRequestException or TaskCanceledException or JsonException)
            {
                return null;
            }
        }

        return string.Join("; ", urls.Select(url => url.GetLeftPart(UriPartial.Authority)));
    }
}

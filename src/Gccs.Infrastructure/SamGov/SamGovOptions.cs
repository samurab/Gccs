namespace Gccs.Infrastructure.SamGov;

public sealed class SamGovOptions
{
    public const string SectionName = "SamGov";

    public string BaseUrl { get; set; } = "https://api.sam.gov";

    public string ApiKey { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 10;

    public int MaxRetries { get; set; } = 2;

    public int RateLimitPerMinute { get; set; } = 60;

    public TimeSpan Timeout => TimeSpan.FromSeconds(Math.Clamp(TimeoutSeconds, 1, 120));

    public int RetryCount => Math.Clamp(MaxRetries, 0, 5);

    public string RedactedApiKey => string.IsNullOrWhiteSpace(ApiKey) ? "<not-configured>" : "<redacted>";
}

using System.Net;
using Gccs.Application.SamGov;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Gccs.Infrastructure.SamGov;

public sealed class SamGovEntityLookupClient(
    HttpClient httpClient,
    IOptions<SamGovOptions> options,
    ILogger<SamGovEntityLookupClient> logger) : ISamGovEntityLookupClient
{
    public const string UserSafeFailureMessage = "SAM.gov lookup is temporarily unavailable. Please try again later.";
    private const string ConfigurationErrorCode = "samgov_configuration_error";
    private const string UnavailableErrorCode = "samgov_unavailable";

    public async Task<SamGovEntityLookupResult> LookupByUeiAsync(
        string uei,
        CancellationToken cancellationToken = default) =>
        await SearchAsync(new SamGovEntitySearchRequest(uei, null), cancellationToken);

    public async Task<SamGovEntityLookupResult> SearchAsync(
        SamGovEntitySearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var configured = options.Value;
        if (string.IsNullOrWhiteSpace(configured.ApiKey))
        {
            logger.LogWarning("SAM.gov lookup is not configured: API key {ApiKey}.", configured.RedactedApiKey);
            return SamGovEntityLookupResult.Failure(ConfigurationErrorCode, UserSafeFailureMessage);
        }

        if (!Uri.TryCreate(configured.BaseUrl, UriKind.Absolute, out var baseUri))
        {
            logger.LogWarning("SAM.gov lookup is not configured: invalid base URL.");
            return SamGovEntityLookupResult.Failure(ConfigurationErrorCode, UserSafeFailureMessage);
        }

        var normalizedUei = request.Uei?.Trim();
        var normalizedName = request.LegalBusinessName?.Trim();
        for (var attempt = 0; attempt <= configured.RetryCount; attempt++)
        {
            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(configured.Timeout);
                using var httpRequest = new HttpRequestMessage(HttpMethod.Get, BuildLookupUri(baseUri, normalizedUei, normalizedName, configured.ApiKey));
                using var response = await httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, timeout.Token);

                if (response.IsSuccessStatusCode)
                {
                    return SamGovEntityLookupResult.Success(await response.Content.ReadAsStringAsync(timeout.Token));
                }

                if (!ShouldRetry(response.StatusCode) || attempt == configured.RetryCount)
                {
                    logger.LogWarning(
                        "SAM.gov lookup failed with status {StatusCode} on attempt {Attempt}.",
                        (int)response.StatusCode,
                        attempt + 1);
                    return SamGovEntityLookupResult.Failure(UnavailableErrorCode, UserSafeFailureMessage);
                }
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                if (attempt == configured.RetryCount)
                {
                    logger.LogWarning("SAM.gov lookup timed out after {TimeoutSeconds} seconds.", configured.TimeoutSeconds);
                    return SamGovEntityLookupResult.Failure(UnavailableErrorCode, UserSafeFailureMessage);
                }
            }
            catch (HttpRequestException exception)
            {
                if (attempt == configured.RetryCount)
                {
                    logger.LogWarning(
                        "SAM.gov lookup request failed after retries ({ExceptionType}).",
                        exception.GetType().Name);
                    return SamGovEntityLookupResult.Failure(UnavailableErrorCode, UserSafeFailureMessage);
                }
            }
        }

        return SamGovEntityLookupResult.Failure(UnavailableErrorCode, UserSafeFailureMessage);
    }

    public static Uri BuildLookupUri(Uri baseUri, string? uei, string? legalBusinessName, string apiKey)
    {
        var query = !string.IsNullOrWhiteSpace(uei)
            ? $"ueiSAM={Uri.EscapeDataString(uei)}"
            : $"legalBusinessName={Uri.EscapeDataString(legalBusinessName ?? string.Empty)}";
        var builder = new UriBuilder(new Uri(baseUri, "/entity-information/v3/entities"))
        {
            Query = $"{query}&api_key={Uri.EscapeDataString(apiKey)}"
        };
        return builder.Uri;
    }

    public static string RedactSensitiveValue(string value, SamGovOptions options) =>
        string.IsNullOrWhiteSpace(options.ApiKey)
            ? value
            : value.Replace(options.ApiKey, "<redacted>", StringComparison.Ordinal);

    private static bool ShouldRetry(HttpStatusCode statusCode) =>
        statusCode == HttpStatusCode.TooManyRequests ||
        (int)statusCode >= 500;
}

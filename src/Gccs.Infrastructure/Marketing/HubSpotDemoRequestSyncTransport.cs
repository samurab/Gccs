using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Gccs.Application.Marketing;
using Microsoft.Extensions.Options;

namespace Gccs.Infrastructure.Marketing;

public sealed class HubSpotDemoRequestSyncTransport(
    IOptions<DemoRequestOptions> options,
    HttpClient httpClient) : IDemoRequestCrmSyncTransport
{
    private static readonly HashSet<string> GenericEmailDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "aol.com", "gmail.com", "googlemail.com", "hotmail.com", "icloud.com", "live.com",
        "mail.com", "me.com", "msn.com", "outlook.com", "proton.me", "protonmail.com",
        "yahoo.com", "ymail.com"
    };
    private static readonly HashSet<string> ProtectedRelationshipStatuses = new(StringComparer.Ordinal)
    {
        "Meeting Scheduled", "Active Conversation", "Pilot Interest", "Partner Interest",
        "Customer", "Partner", "Not Interested", "Do Not Contact"
    };
    private static readonly HashSet<string> ProtectedProspectingStatuses = new(StringComparer.Ordinal)
    {
        "Meeting Scheduled", "Converted to Opportunity"
    };
    private static readonly HashSet<string> ProtectedInterestLevels = new(StringComparer.Ordinal)
    {
        "Very High"
    };
    private static readonly string[] ContactMergeProperties =
    [
        "fedril_acquisition_source", "fedril_source_detail", "fedril_outreach_permission",
        "fedril_relationship_status", "fedril_interest_level", "fedril_prospecting_status"
    ];

    private readonly DemoRequestOptions _options = options.Value;

    public bool IsConfigured =>
        _options.Enabled &&
        _options.HubSpot.Enabled &&
        Uri.TryCreate(_options.HubSpot.BaseUrl, UriKind.Absolute, out var baseUri) &&
        baseUri.Scheme == Uri.UriSchemeHttps &&
        !string.IsNullOrWhiteSpace(_options.HubSpot.PrivateAppToken);

    public async Task<DemoRequestDeliveryResult> SyncAsync(
        ClaimedDemoRequestDelivery request,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("HubSpot demo-request synchronization is not configured.");
        }

        var contactId = await UpsertContactAsync(request, cancellationToken);

        var domain = GetBusinessDomain(request.Email);
        if (domain is not null)
        {
            var companyId = await UpsertAsync(
                "companies",
                "domain",
                domain,
                new Dictionary<string, string>
                {
                    ["name"] = request.Company,
                    ["domain"] = domain
                },
                cancellationToken);
            await AssociateAsync(contactId, companyId, cancellationToken);
        }

        return new DemoRequestDeliveryResult(DemoRequestDeliveryDisposition.Sent, $"hubspot-contact:{contactId}");
    }

    internal static IReadOnlyDictionary<string, string> BuildContactProperties(
        ClaimedDemoRequestDelivery request,
        IReadOnlyDictionary<string, string>? existing = null)
    {
        var properties = new Dictionary<string, string>
        {
            ["email"] = request.Email.ToLowerInvariant(),
            ["firstname"] = request.FirstName,
            ["lastname"] = request.LastName,
            ["company"] = request.Company,
            ["fedril_acquisition_source"] = "Book a Demo",
            ["fedril_source_detail"] = BuildSourceDetail(request),
            ["fedril_outreach_permission"] = "Manual Only",
            ["fedril_relationship_status"] = "Demo Interest",
            ["fedril_interest_level"] = "High",
            ["fedril_prospecting_status"] = "Meeting Requested",
            ["fedril_next_action"] = "Review requested demo time and send a confirmation or alternate-time response.",
            ["fedril_next_followup_date"] = NextBusinessDate(request.ReceivedAt).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
        };
        if (!string.IsNullOrWhiteSpace(request.Phone)) properties["phone"] = request.Phone;
        if (existing is null) return properties;

        PreserveWhenSet(properties, existing, "fedril_acquisition_source");
        PreserveWhenSet(properties, existing, "fedril_outreach_permission");
        PreserveWhen(properties, existing, "fedril_relationship_status", ProtectedRelationshipStatuses);
        PreserveWhen(properties, existing, "fedril_interest_level", ProtectedInterestLevels);
        PreserveWhen(properties, existing, "fedril_prospecting_status", ProtectedProspectingStatuses);
        if (existing.TryGetValue("fedril_source_detail", out var sourceDetail) && !string.IsNullOrWhiteSpace(sourceDetail))
        {
            var newDetail = properties["fedril_source_detail"];
            properties["fedril_source_detail"] = sourceDetail.Contains(request.RequestId.ToString("N"), StringComparison.OrdinalIgnoreCase)
                ? sourceDetail
                : KeepNewest($"{sourceDetail} | {newDetail}", 1000);
        }
        return properties;
    }

    internal static string? GetBusinessDomain(string email)
    {
        var separator = email.LastIndexOf('@');
        if (separator < 1 || separator == email.Length - 1) return null;
        var domain = email[(separator + 1)..].Trim().TrimEnd('.').ToLowerInvariant();
        return domain.Contains('.', StringComparison.Ordinal) && !GenericEmailDomains.Contains(domain)
            ? domain
            : null;
    }

    private async Task<string> UpsertAsync(
        string objectType,
        string idProperty,
        string idValue,
        IReadOnlyDictionary<string, string> properties,
        CancellationToken cancellationToken)
    {
        var existing = await FindAsync(objectType, idProperty, idValue, [], cancellationToken);
        if (existing is not null)
        {
            return existing.Id;
        }

        using var response = await SendRawAsync(HttpMethod.Post, $"crm/v3/objects/{objectType}", new { properties }, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            existing = await FindAsync(objectType, idProperty, idValue, [], cancellationToken);
            if (existing is not null)
            {
                return existing.Id;
            }
        }

        await EnsureSuccessAsync(response, objectType + " create", cancellationToken);
        return await ReadIdAsync(response, cancellationToken);
    }

    private async Task<string> UpsertContactAsync(
        ClaimedDemoRequestDelivery request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.ToLowerInvariant();
        var existing = await FindAsync("contacts", "email", email, ContactMergeProperties, cancellationToken);
        if (existing is not null)
        {
            var properties = BuildContactProperties(request, existing.Properties);
            await SendAsync(HttpMethod.Patch, $"crm/v3/objects/contacts/{Uri.EscapeDataString(existing.Id)}", new { properties }, cancellationToken);
            return existing.Id;
        }

        var createProperties = BuildContactProperties(request);
        using var response = await SendRawAsync(HttpMethod.Post, "crm/v3/objects/contacts", new { properties = createProperties }, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            existing = await FindAsync("contacts", "email", email, ContactMergeProperties, cancellationToken);
            if (existing is not null)
            {
                var properties = BuildContactProperties(request, existing.Properties);
                await SendAsync(HttpMethod.Patch, $"crm/v3/objects/contacts/{Uri.EscapeDataString(existing.Id)}", new { properties }, cancellationToken);
                return existing.Id;
            }
        }

        await EnsureSuccessAsync(response, "contacts create", cancellationToken);
        return await ReadIdAsync(response, cancellationToken);
    }

    private async Task<ExistingObject?> FindAsync(
        string objectType,
        string idProperty,
        string idValue,
        IReadOnlyList<string> properties,
        CancellationToken cancellationToken)
    {
        var propertyQuery = properties.Count == 0
            ? string.Empty
            : "&properties=" + Uri.EscapeDataString(string.Join(',', properties));
        using var response = await SendRawAsync(
            HttpMethod.Get,
            $"crm/v3/objects/{objectType}/{Uri.EscapeDataString(idValue)}?idProperty={Uri.EscapeDataString(idProperty)}{propertyQuery}",
            null,
            cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        await EnsureSuccessAsync(response, objectType + " lookup", cancellationToken);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (!document.RootElement.TryGetProperty("id", out var id) || string.IsNullOrWhiteSpace(id.GetString()))
            throw new InvalidOperationException("HubSpot returned a successful response without a CRM object ID.");
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        if (document.RootElement.TryGetProperty("properties", out var propertyObject))
        {
            foreach (var property in propertyObject.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.String) values[property.Name] = property.Value.GetString()!;
            }
        }
        return new ExistingObject(id.GetString()!, values);
    }

    private Task AssociateAsync(string contactId, string companyId, CancellationToken cancellationToken) =>
        SendAsync(
            HttpMethod.Put,
            $"crm/v3/objects/contacts/{Uri.EscapeDataString(contactId)}/associations/companies/{Uri.EscapeDataString(companyId)}/contact_to_company",
            null,
            cancellationToken);

    private async Task SendAsync(HttpMethod method, string relativePath, object? body, CancellationToken cancellationToken)
    {
        using var response = await SendRawAsync(method, relativePath, body, cancellationToken);
        await EnsureSuccessAsync(response, relativePath, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendRawAsync(
        HttpMethod method,
        string relativePath,
        object? body,
        CancellationToken cancellationToken)
    {
        var baseUrl = _options.HubSpot.BaseUrl.TrimEnd('/') + "/";
        var message = new HttpRequestMessage(method, new Uri(new Uri(baseUrl), relativePath));
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.HubSpot.PrivateAppToken);
        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (body is not null) message.Content = JsonContent.Create(body);
        try
        {
            return await httpClient.SendAsync(message, cancellationToken);
        }
        finally
        {
            message.Dispose();
        }
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        string operation,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;
        var correlationId = response.Headers.TryGetValues("x-hubspot-correlation-id", out var values)
            ? values.FirstOrDefault()
            : null;
        await response.Content.LoadIntoBufferAsync(cancellationToken);
        throw new HttpRequestException(
            $"HubSpot {operation} failed with HTTP {(int)response.StatusCode}; correlation={correlationId ?? "unavailable"}.",
            null,
            response.StatusCode);
    }

    private static async Task<string> ReadIdAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (!document.RootElement.TryGetProperty("id", out var id) || string.IsNullOrWhiteSpace(id.GetString()))
        {
            throw new InvalidOperationException("HubSpot returned a successful response without a CRM object ID.");
        }
        return id.GetString()!;
    }

    private static string BuildSourceDetail(ClaimedDemoRequestDelivery request)
    {
        var referral = string.IsNullOrWhiteSpace(request.ReferralSource) ? "not provided" : request.ReferralSource;
        return $"Website demo request {request.RequestId:N}; referral: {referral}";
    }

    private static void PreserveWhenSet(
        IDictionary<string, string> target,
        IReadOnlyDictionary<string, string> existing,
        string propertyName)
    {
        if (existing.TryGetValue(propertyName, out var value) && !string.IsNullOrWhiteSpace(value))
            target.Remove(propertyName);
    }

    private static void PreserveWhen(
        IDictionary<string, string> target,
        IReadOnlyDictionary<string, string> existing,
        string propertyName,
        IReadOnlySet<string> protectedValues)
    {
        if (existing.TryGetValue(propertyName, out var value) && protectedValues.Contains(value))
            target.Remove(propertyName);
    }

    private static string KeepNewest(string value, int maximumLength) =>
        value.Length <= maximumLength ? value : value[^maximumLength..];

    private static DateTime NextBusinessDate(DateTimeOffset receivedAt)
    {
        var date = receivedAt.UtcDateTime.Date.AddDays(1);
        while (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) date = date.AddDays(1);
        return date;
    }

    private sealed record ExistingObject(string Id, IReadOnlyDictionary<string, string> Properties);
}

using System.Net;
using System.Text;
using System.Text.Json;
using Gccs.Application.Marketing;
using Gccs.Infrastructure.Marketing;
using Gccs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class HubSpotDemoRequestSyncTests
{
    [Fact]
    public async Task Enabled_hubspot_sync_is_enqueued_atomically_with_the_demo_request()
    {
        var databaseOptions = new DbContextOptionsBuilder<GccsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using var context = new GccsDbContext(databaseOptions);
        var repository = new EfDemoRequestRepository(
            context,
            Options.Create(new DemoRequestOptions { HubSpot = new HubSpotDemoRequestOptions { Enabled = true } }));
        var now = new DateTimeOffset(2026, 8, 24, 15, 0, 0, TimeSpan.Zero);
        var record = new DemoRequestRecord(
            Guid.NewGuid(), "Avery", "Ng", "avery@northstar.example", "555-0101", "Northstar Systems",
            "Industry event", "11-50", "Evidence readiness workflow", now.AddDays(4), "America/New_York",
            DemoRequestService.ConsentNoticeVersion, now, Guid.NewGuid().ToString("N"));

        await repository.CreateIfNewAsync(record);

        var deliveries = await context.DemoRequestDeliveries
            .Where(item => item.DemoRequestId == record.Id)
            .Select(item => item.DeliveryKind)
            .OrderBy(item => item)
            .ToArrayAsync();
        Assert.Equal(["HubSpotSync", "InternalNotification", "RequesterAcknowledgement"], deliveries);
    }

    [Fact]
    public async Task New_business_contact_upserts_contact_and_company_then_associates_them()
    {
        var handler = new RecordingHandler(
            Response(HttpStatusCode.NotFound),
            Response(HttpStatusCode.Created, "{\"id\":\"101\"}"),
            Response(HttpStatusCode.NotFound),
            Response(HttpStatusCode.Created, "{\"id\":\"202\"}"),
            Response(HttpStatusCode.NoContent));
        var transport = CreateTransport(handler);
        var request = CreateRequest("avery@northstar.example") with
        {
            ReceivedAt = new DateTimeOffset(2026, 8, 21, 15, 0, 0, TimeSpan.Zero),
            DeliveryKind = "HubSpotSync"
        };

        var result = await transport.SyncAsync(request);

        Assert.Equal(DemoRequestDeliveryDisposition.Sent, result.Disposition);
        Assert.Equal("hubspot-contact:101", result.ProviderMessageId);
        Assert.Collection(
            handler.Requests,
            item => Assert.StartsWith("GET /crm/v3/objects/contacts/avery%40northstar.example?idProperty=email&properties=", item.Target, StringComparison.Ordinal),
            item =>
            {
                Assert.Equal("POST /crm/v3/objects/contacts", item.Target);
                using var document = JsonDocument.Parse(item.Body!);
                var properties = document.RootElement.GetProperty("properties");
                Assert.Equal("Book a Demo", properties.GetProperty("fedril_acquisition_source").GetString());
                Assert.Equal("Manual Only", properties.GetProperty("fedril_outreach_permission").GetString());
                Assert.Equal("Meeting Requested", properties.GetProperty("fedril_prospecting_status").GetString());
                Assert.Equal("2026-08-24", properties.GetProperty("fedril_next_followup_date").GetString());
                Assert.Contains(request.RequestId.ToString("N"), properties.GetProperty("fedril_source_detail").GetString());
            },
            item => Assert.Equal("GET /crm/v3/objects/companies/northstar.example?idProperty=domain", item.Target),
            item => Assert.Equal("POST /crm/v3/objects/companies", item.Target),
            item => Assert.Equal("PUT /crm/v3/objects/contacts/101/associations/companies/202/contact_to_company", item.Target));
        Assert.All(handler.Requests, item => Assert.Equal("Bearer test-private-token", item.Authorization));
    }

    [Fact]
    public async Task Existing_contact_with_generic_email_is_updated_without_creating_a_company()
    {
        var handler = new RecordingHandler(
            Response(HttpStatusCode.OK, "{\"id\":\"303\",\"properties\":{\"fedril_acquisition_source\":\"Referral\",\"fedril_outreach_permission\":\"Do Not Contact\",\"fedril_relationship_status\":\"Customer\",\"fedril_interest_level\":\"Very High\",\"fedril_prospecting_status\":\"Converted to Opportunity\"}}"),
            Response(HttpStatusCode.OK, "{\"id\":\"303\"}"));
        var transport = CreateTransport(handler);

        var result = await transport.SyncAsync(CreateRequest("avery@gmail.com") with { DeliveryKind = "HubSpotSync" });

        Assert.Equal("hubspot-contact:303", result.ProviderMessageId);
        Assert.Collection(
            handler.Requests,
            item => Assert.StartsWith("GET /crm/v3/objects/contacts/avery%40gmail.com", item.Target, StringComparison.Ordinal),
            item =>
            {
                Assert.Equal("PATCH /crm/v3/objects/contacts/303", item.Target);
                using var document = JsonDocument.Parse(item.Body!);
                var properties = document.RootElement.GetProperty("properties");
                Assert.False(properties.TryGetProperty("fedril_acquisition_source", out _));
                Assert.False(properties.TryGetProperty("fedril_outreach_permission", out _));
                Assert.False(properties.TryGetProperty("fedril_relationship_status", out _));
                Assert.False(properties.TryGetProperty("fedril_interest_level", out _));
                Assert.False(properties.TryGetProperty("fedril_prospecting_status", out _));
                Assert.True(properties.TryGetProperty("fedril_next_action", out _));
            });
    }

    [Fact]
    public void Enabled_hubspot_sync_requires_a_private_app_token()
    {
        var options = new DemoRequestOptions
        {
            Enabled = true,
            Provider = DemoRequestOptions.DevelopmentCaptureProvider,
            HubSpot = new HubSpotDemoRequestOptions
            {
                Enabled = true,
                BaseUrl = "https://api.hubapi.com"
            }
        };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            DemoRequestOptions.ValidateEnabledConfiguration(options, isDevelopment: true));

        Assert.Contains("private-app token", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static HubSpotDemoRequestSyncTransport CreateTransport(HttpMessageHandler handler) =>
        new(
            Options.Create(new DemoRequestOptions
            {
                Enabled = true,
                HubSpot = new HubSpotDemoRequestOptions
                {
                    Enabled = true,
                    BaseUrl = "https://api.hubapi.com",
                    PrivateAppToken = "test-private-token"
                }
            }),
            new HttpClient(handler));

    private static ClaimedDemoRequestDelivery CreateRequest(string email) => new(
        Guid.NewGuid(), Guid.NewGuid(), "Avery", "Ng", email, "555-0101", "Northstar Systems",
        "Industry event", "11-50", "Evidence readiness workflow",
        new DateTimeOffset(2026, 8, 28, 17, 0, 0, TimeSpan.Zero), "America/New_York",
        new DateTimeOffset(2026, 8, 24, 15, 0, 0, TimeSpan.Zero), 1, "HubSpotSync");

    private static HttpResponseMessage Response(HttpStatusCode statusCode, string? json = null) => new(statusCode)
    {
        Content = json is null ? null : new StringContent(json, Encoding.UTF8, "application/json")
    };

    private sealed class RecordingHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new(responses);
        public List<RecordedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add(new RecordedRequest(
                $"{request.Method} {request.RequestUri!.PathAndQuery}",
                body,
                request.Headers.Authorization?.ToString()));
            return _responses.Dequeue();
        }
    }

    private sealed record RecordedRequest(string Target, string? Body, string? Authorization);
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Gccs.Application.Marketing;
using Gccs.Infrastructure.Marketing;
using Gccs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class DemoRequestTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public DemoRequestTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Anonymous_valid_request_is_normalized_and_accepted_without_tenant_context()
    {
        var repository = new StubRepository();
        await using var factory = CreateFactory(repository);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/public/demo-requests", ValidRequest() with
        {
            FirstName = "  Avery  ",
            Company = " Northstar   Systems "
        });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.NotNull(repository.Created);
        Assert.Equal("Avery", repository.Created.FirstName);
        Assert.Equal("Northstar Systems", repository.Created.Company);
        Assert.Equal(DemoRequestService.ConsentNoticeVersion, repository.Created.ConsentNoticeVersion);
    }

    [Fact]
    public async Task Invalid_or_unconsented_request_returns_standard_validation_problem_without_writing()
    {
        var repository = new StubRepository();
        await using var factory = CreateFactory(repository);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/public/demo-requests", ValidRequest() with
        {
            Email = "not-an-email",
            PrivacyConsent = false,
            PreferredStartAt = DateTimeOffset.UtcNow.AddMinutes(30),
            PreferredTimeZone = "Invalid/Zone"
        });
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Demo request invalid", payload.RootElement.GetProperty("title").GetString());
        Assert.True(payload.RootElement.GetProperty("errors").TryGetProperty("email", out _));
        Assert.True(payload.RootElement.GetProperty("errors").TryGetProperty("privacyConsent", out _));
        Assert.True(payload.RootElement.GetProperty("errors").TryGetProperty("preferredStartAt", out _));
        Assert.True(payload.RootElement.GetProperty("errors").TryGetProperty("preferredTimeZone", out _));
        Assert.Null(repository.Created);
    }

    [Fact]
    public async Task Honeypot_submission_returns_generic_receipt_without_persisting()
    {
        var repository = new StubRepository();
        var service = new DemoRequestService(repository, TimeProvider.System);

        var result = await service.SubmitAsync(ValidRequest() with { Website = "https://spam.example" });

        Assert.Equal("Received", result.Status);
        Assert.Null(repository.Created);
    }

    [Fact]
    public async Task Public_endpoint_rate_limits_repeated_requests_before_unbounded_writes()
    {
        var repository = new StubRepository();
        await using var factory = CreateFactory(repository, permitLimit: 2);
        using var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Accepted, (await client.PostAsJsonAsync("/api/public/demo-requests", ValidRequest())).StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, (await client.PostAsJsonAsync("/api/public/demo-requests", ValidRequest())).StatusCode);
        var rejected = await client.PostAsJsonAsync("/api/public/demo-requests", ValidRequest());

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.Contains("rate_limit_exceeded", await rejected.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Operations_inbox_requires_dedicated_platform_permission()
    {
        await using var factory = CreateFactory(new StubRepository());
        using var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/platform/demo-requests")).StatusCode);
        using var denied = new HttpRequestMessage(HttpMethod.Get, "/api/platform/demo-requests");
        denied.Headers.Add("X-Gccs-Dev-Auth", "true");
        denied.Headers.Add("X-Gccs-Dev-Tenant", "none");
        denied.Headers.Add("X-Gccs-Dev-Platform-Permissions", "ProvisionTenants");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(denied)).StatusCode);

        using var allowed = new HttpRequestMessage(HttpMethod.Get, "/api/platform/demo-requests?page=1&pageSize=25");
        allowed.Headers.Add("X-Gccs-Dev-Auth", "true");
        allowed.Headers.Add("X-Gccs-Dev-Tenant", "none");
        allowed.Headers.Add("X-Gccs-Dev-Platform-Permissions", "ManageDemoRequests");
        var response = await client.SendAsync(allowed);
        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, responseBody);
        Assert.Contains("\"totalCount\":0", responseBody);
    }

    [Fact]
    public async Task Operator_response_requires_permission_and_allowlisted_template()
    {
        var repository = new StubRepository { QueueResponseResult = true };
        await using var factory = CreateFactory(repository);
        using var client = factory.CreateClient();
        var id = Guid.NewGuid();
        using var denied = new HttpRequestMessage(HttpMethod.Post, $"/api/platform/demo-requests/{id}/responses");
        denied.Headers.Add("X-Gccs-Dev-Auth", "true"); denied.Headers.Add("X-Gccs-Dev-Tenant", "none"); denied.Headers.Add("X-Gccs-Dev-Platform-Permissions", "ProvisionTenants");
        denied.Content = JsonContent.Create(new QueueDemoRequestResponse("ReviewingRequestedTime"));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(denied)).StatusCode);

        using var allowed = new HttpRequestMessage(HttpMethod.Post, $"/api/platform/demo-requests/{id}/responses");
        allowed.Headers.Add("X-Gccs-Dev-Auth", "true"); allowed.Headers.Add("X-Gccs-Dev-Tenant", "none"); allowed.Headers.Add("X-Gccs-Dev-User", "71717171-7171-7171-7171-717171717171"); allowed.Headers.Add("X-Gccs-Dev-Platform-Permissions", "ManageDemoRequests");
        allowed.Content = JsonContent.Create(new QueueDemoRequestResponse("ReviewingRequestedTime"));
        Assert.Equal(HttpStatusCode.Accepted, (await client.SendAsync(allowed)).StatusCode);
        Assert.Equal("ReviewingRequestedTime", repository.QueuedTemplateKey);
        Assert.Equal(Guid.Parse("71717171-7171-7171-7171-717171717171"), repository.QueuedByUserId);
    }

    [Fact]
    public async Task Provider_failure_schedules_retry_and_terminal_attempt_stops_retrying()
    {
        var claim = CreateClaim(attempt: 1);
        var repository = new StubRepository { Claim = claim };
        var sender = new StubSender(new InvalidOperationException("provider down"));
        var service = new DemoRequestDeliveryService(
            repository,
            sender,
            new DemoRequestDeliverySettings(TimeSpan.FromMinutes(5), 2),
            TimeProvider.System);

        Assert.True(await service.ProcessNextAsync());
        Assert.NotNull(repository.RetryAt);

        repository.Claim = claim with { AttemptNumber = 2 };
        await service.ProcessNextAsync();
        Assert.Null(repository.RetryAt);
    }

    [Fact]
    public void Notification_html_encodes_untrusted_contact_fields_and_preserves_no_cui_warning()
    {
        var content = AzureCommunicationDemoRequestEmailSender.CreateContent(CreateClaim(attempt: 1) with
        {
            FirstName = "<script>alert(1)</script>",
            Company = "Acme & Sons",
            Message = "<img src=x onerror=alert(1)>"
        });

        Assert.DoesNotContain("<script>", content.Html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("&lt;script&gt;", content.Html);
        Assert.Contains("Acme &amp; Sons", content.Html);
        Assert.Contains("Do not request CUI", content.Html);
    }

    [Fact]
    public void Requester_acknowledgement_repeats_preferred_time_without_claiming_confirmation()
    {
        var content = AzureCommunicationDemoRequestEmailSender.CreateAcknowledgementContent(
            CreateClaim(1) with { DeliveryKind = "RequesterAcknowledgement" });
        Assert.Contains("America/New_York", content.PlainText);
        Assert.Contains("not a confirmed calendar reservation", content.PlainText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("confirmed meeting", content.PlainText, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("ReviewingRequestedTime")]
    [InlineData("RequestMoreDetails")]
    [InlineData("RequestedTimeUnavailable")]
    public void Operator_response_templates_are_fixed_and_preserve_no_cui_warning(string templateKey)
    {
        var content = AzureCommunicationDemoRequestEmailSender.CreateOperatorResponseContent(
            CreateClaim(1) with { FirstName = "<script>alert(1)</script>" }, templateKey);
        Assert.DoesNotContain("<script>", content.Html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Do not reply with CUI", content.Html);
    }

    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task Concurrent_duplicate_capture_persists_exactly_one_request_and_one_outbox_delivery()
    {
        var connectionString = Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION")!;
        var key = Convert.ToHexString(Guid.NewGuid().ToByteArray()).PadRight(64, '0');
        var first = CreateRecord(Guid.NewGuid(), key);
        var second = CreateRecord(Guid.NewGuid(), key);
        var options = new DbContextOptionsBuilder<GccsDbContext>().UseNpgsql(connectionString).Options;

        await using (var migrationContext = new GccsDbContext(options)) await migrationContext.Database.MigrateAsync();
        try
        {
            await using var firstContext = new GccsDbContext(options);
            await using var secondContext = new GccsDbContext(options);
            await Task.WhenAll(
                new EfDemoRequestRepository(firstContext).CreateIfNewAsync(first),
                new EfDemoRequestRepository(secondContext).CreateIfNewAsync(second));

            await using var verification = new GccsDbContext(options);
            Assert.Equal(1, await verification.DemoRequests.CountAsync(item => item.DeduplicationKey == key));
            var requestId = await verification.DemoRequests.Where(item => item.DeduplicationKey == key).Select(item => item.Id).SingleAsync();
            Assert.Equal(2, await verification.DemoRequestDeliveries.CountAsync(item => item.DemoRequestId == requestId));
            Assert.Equal(2, await verification.DemoRequestDeliveries.Where(item => item.DemoRequestId == requestId).Select(item => item.DeliveryKind).Distinct().CountAsync());
            var actorId = Guid.NewGuid();
            var responseRepository = new EfDemoRequestRepository(verification);
            Assert.True(await responseRepository.QueueOperatorResponseAsync(requestId, "RequestMoreDetails", actorId, DateTimeOffset.UtcNow));
            Assert.False(await responseRepository.QueueOperatorResponseAsync(requestId, "RequestMoreDetails", actorId, DateTimeOffset.UtcNow));
            Assert.Contains(await verification.DemoRequestDeliveries.AsNoTracking().ToArrayAsync(), delivery =>
                delivery.DemoRequestId == requestId && delivery.DeliveryKind == "OperatorResponse:RequestMoreDetails" && delivery.RequestedByUserId == actorId);

            await verification.DemoRequests.Where(item => item.Id == requestId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.ReceivedAt, DateTimeOffset.UtcNow.AddDays(-400)));
            await verification.DemoRequestDeliveries.Where(item => item.DemoRequestId == requestId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.Status, "Sent"));
            Assert.Equal(1, await new EfDemoRequestRepository(verification).DeleteExpiredAsync(DateTimeOffset.UtcNow.AddDays(-365)));
            Assert.False(await verification.DemoRequests.AnyAsync(item => item.Id == requestId));
            Assert.False(await verification.DemoRequestDeliveries.AnyAsync(item => item.DemoRequestId == requestId));
        }
        finally
        {
            await using var cleanup = new GccsDbContext(options);
            var requestIds = await cleanup.DemoRequests.Where(item => item.DeduplicationKey == key).Select(item => item.Id).ToArrayAsync();
            await cleanup.DemoRequestDeliveries.Where(item => requestIds.Contains(item.DemoRequestId)).ExecuteDeleteAsync();
            await cleanup.DemoRequests.Where(item => requestIds.Contains(item.Id)).ExecuteDeleteAsync();
        }
    }

    private WebApplicationFactory<Program> CreateFactory(StubRepository repository, int permitLimit = 20) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.UseSetting("DemoRequests:Enabled", "true");
            builder.UseSetting("Security:DemoRequestRateLimiting:PermitLimit", permitLimit.ToString());
            builder.UseSetting("Security:DemoRequestRateLimiting:WindowMinutes", "10");
            builder.UseSetting("Security:DevelopmentAuth:DefaultPlatformPermissions", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IDemoRequestRepository>();
                services.AddSingleton<IDemoRequestRepository>(repository);
            });
        });

    private static SubmitDemoRequest ValidRequest() => new(
        "Avery", "Ng", "avery@example.com", "555-0101", "Northstar Systems",
        "Industry event", "11-50", "Evidence readiness workflow", DateTimeOffset.UtcNow.AddDays(2), "America/New_York", true, null);

    private static ClaimedDemoRequestDelivery CreateClaim(int attempt) => new(
        Guid.NewGuid(), Guid.NewGuid(), "Avery", "Ng", "avery@example.com", "555-0101",
        "Northstar Systems", "Industry event", "11-50", "Evidence readiness workflow",
        DateTimeOffset.UtcNow.AddDays(2), "America/New_York", DateTimeOffset.UtcNow, attempt, "InternalNotification");

    private static DemoRequestRecord CreateRecord(Guid id, string key) => new(
        id, "Avery", "Ng", "avery@example.com", null, "Northstar Systems", null, "11-50",
        "Evidence readiness workflow", DateTimeOffset.UtcNow.AddDays(2), "America/New_York", DemoRequestService.ConsentNoticeVersion, DateTimeOffset.UtcNow, key);

    private sealed class StubSender(Exception? exception = null) : IDemoRequestEmailSender
    {
        public bool IsConfigured => true;
        public Task<DemoRequestEmailSendResult> SendAsync(ClaimedDemoRequestDelivery request, CancellationToken cancellationToken = default) =>
            exception is null
                ? Task.FromResult(new DemoRequestEmailSendResult("provider-id"))
                : Task.FromException<DemoRequestEmailSendResult>(exception);
    }

    private sealed class StubRepository : IDemoRequestRepository
    {
        public DemoRequestRecord? Created { get; private set; }
        public ClaimedDemoRequestDelivery? Claim { get; set; }
        public DateTimeOffset? RetryAt { get; private set; }
        public bool? QueueResponseResult { get; set; }
        public string? QueuedTemplateKey { get; private set; }
        public Guid? QueuedByUserId { get; private set; }

        public Task CreateIfNewAsync(DemoRequestRecord request, CancellationToken cancellationToken = default)
        {
            Created ??= request;
            return Task.CompletedTask;
        }

        public Task<ClaimedDemoRequestDelivery?> TryClaimNextDeliveryAsync(DateTimeOffset now, TimeSpan leaseDuration, CancellationToken cancellationToken = default) => Task.FromResult(Claim);
        public Task MarkDeliverySentAsync(Guid deliveryId, string providerMessageId, DateTimeOffset sentAt, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task MarkDeliveryFailedAsync(Guid deliveryId, string failureCode, DateTimeOffset attemptedAt, DateTimeOffset? retryAt, CancellationToken cancellationToken = default)
        {
            RetryAt = retryAt;
            return Task.CompletedTask;
        }
        public Task<int> DeleteExpiredAsync(DateTimeOffset receivedBefore, CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<DemoRequestOperationsPage> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default) =>
            Task.FromResult(new DemoRequestOperationsPage([], page, pageSize, 0, false, page > 1));
        public Task<bool?> QueueOperatorResponseAsync(Guid requestId, string templateKey, Guid actorUserId, DateTimeOffset now, CancellationToken cancellationToken = default)
        {
            QueuedTemplateKey = templateKey; QueuedByUserId = actorUserId; return Task.FromResult(QueueResponseResult);
        }
    }
}

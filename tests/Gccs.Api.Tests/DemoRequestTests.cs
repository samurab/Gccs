using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Gccs.Api;
using Gccs.Application.Audit;
using Gccs.Application.Marketing;
using Gccs.Infrastructure.Marketing;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
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
        using var scope = factory.Services.CreateScope();
        var transport = scope.ServiceProvider.GetRequiredService<IDemoRequestDeliveryTransport>();
        Assert.IsType<DevelopmentCaptureDemoRequestDeliveryTransport>(transport);
        Assert.True(transport.IsConfigured);
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
    public async Task Preferred_time_exactly_two_hours_ahead_is_accepted()
    {
        var now = new DateTimeOffset(2026, 8, 11, 16, 0, 0, TimeSpan.Zero);
        var repository = new StubRepository();
        var service = new DemoRequestService(repository, new FixedTimeProvider(now));

        await service.SubmitAsync(ValidRequest() with { PreferredStartAt = now.AddHours(2) });

        Assert.Equal(now.AddHours(2), repository.Created?.PreferredStartAt);
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
    public async Task Platform_access_reports_the_server_configured_development_capture_mode()
    {
        await using var factory = CreateFactory(new StubRepository());
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/platform/me/access");
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", "none");
        request.Headers.Add("X-Gccs-Dev-Platform-Permissions", "ManageDemoRequests");

        using var response = await client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"demoRequestDeliveryMode\":\"DevelopmentCapture\"", responseBody);
    }

    [Fact]
    public async Task Requested_time_calendar_requires_permission_and_enforces_a_bounded_half_open_range()
    {
        var requestId = Guid.NewGuid();
        var start = new DateTimeOffset(2026, 8, 10, 14, 0, 0, TimeSpan.Zero);
        var repository = new StubRepository
        {
            CalendarItems =
            [
                new DemoRequestCalendarItem(
                    requestId, "Avery", "Ng", "Northstar Systems", start,
                    "America/New_York", start.AddDays(-2), "Sent", "Requested")
            ]
        };
        await using var factory = CreateFactory(repository);
        using var client = factory.CreateClient();
        var path = "/api/platform/demo-requests/calendar?from=2026-08-01T00%3A00%3A00Z&to=2026-09-01T00%3A00%3A00Z";

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(path)).StatusCode);
        using var denied = new HttpRequestMessage(HttpMethod.Get, path);
        denied.Headers.Add("X-Gccs-Dev-Auth", "true");
        denied.Headers.Add("X-Gccs-Dev-Tenant", "none");
        denied.Headers.Add("X-Gccs-Dev-Platform-Permissions", "ProvisionTenants");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(denied)).StatusCode);

        using var allowed = new HttpRequestMessage(HttpMethod.Get, path);
        allowed.Headers.Add("X-Gccs-Dev-Auth", "true");
        allowed.Headers.Add("X-Gccs-Dev-Tenant", "none");
        allowed.Headers.Add("X-Gccs-Dev-Platform-Permissions", "ManageDemoRequests");
        using var response = await client.SendAsync(allowed);
        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(requestId.ToString(), responseBody);
        Assert.Contains("\"schedulingStatus\":\"Requested\"", responseBody);
        Assert.Equal(new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero), repository.CalendarFrom);
        Assert.Equal(new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero), repository.CalendarTo);

        using var invalid = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/platform/demo-requests/calendar?from=2026-01-01T00%3A00%3A00Z&to=2026-06-01T00%3A00%3A00Z");
        invalid.Headers.Add("X-Gccs-Dev-Auth", "true");
        invalid.Headers.Add("X-Gccs-Dev-Tenant", "none");
        invalid.Headers.Add("X-Gccs-Dev-Platform-Permissions", "ManageDemoRequests");
        using var invalidResponse = await client.SendAsync(invalid);
        Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);
        Assert.Contains("calendar_range_invalid", await invalidResponse.Content.ReadAsStringAsync());
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
    public async Task Detail_request_requires_permission_and_queues_a_hashed_single_use_link()
    {
        var repository = new StubRepository();
        var followUps = new StubFollowUpRepository();
        await using var factory = CreateFactory(repository, followUpRepository: followUps);
        using var client = factory.CreateClient();
        var id = Guid.NewGuid();

        using var denied = new HttpRequestMessage(HttpMethod.Post, $"/api/platform/demo-requests/{id}/responses");
        denied.Headers.Add("X-Gccs-Dev-Auth", "true");
        denied.Headers.Add("X-Gccs-Dev-Tenant", "none");
        denied.Headers.Add("X-Gccs-Dev-Platform-Permissions", "ProvisionTenants");
        denied.Content = JsonContent.Create(new QueueDemoRequestResponse("RequestMoreDetails"));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(denied)).StatusCode);
        Assert.Null(followUps.QueueCommand);

        using var allowed = new HttpRequestMessage(HttpMethod.Post, $"/api/platform/demo-requests/{id}/responses");
        allowed.Headers.Add("X-Gccs-Dev-Auth", "true");
        allowed.Headers.Add("X-Gccs-Dev-Tenant", "none");
        allowed.Headers.Add("X-Gccs-Dev-User", "71717171-7171-7171-7171-717171717171");
        allowed.Headers.Add("X-Gccs-Dev-Platform-Permissions", "ManageDemoRequests");
        allowed.Content = JsonContent.Create(new QueueDemoRequestResponse("RequestMoreDetails"));
        using var response = await client.SendAsync(allowed);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal(id, followUps.QueueCommand?.DemoRequestId);
        Assert.Equal(64, followUps.QueueCommand?.TokenHash.Length);
        Assert.DoesNotContain("v1.", followUps.QueueCommand!.TokenHash, StringComparison.Ordinal);
        Assert.Equal(Guid.Parse("71717171-7171-7171-7171-717171717171"), followUps.QueueCommand.RequestedByUserId);
        Assert.Contains("\"followUpRequestId\"", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Public_detail_form_validates_token_input_no_cui_acknowledgement_and_replay()
    {
        var repository = new StubRepository();
        var followUps = new StubFollowUpRepository();
        await using var factory = CreateFactory(repository, followUpRepository: followUps);
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var codec = scope.ServiceProvider.GetRequiredService<DemoFollowUpTokenCodec>();
        var requestId = Guid.NewGuid();
        var demoRequestId = Guid.NewGuid();
        var requestedAt = DateTimeOffset.UtcNow;
        var expiresAt = requestedAt.AddHours(2);
        var accessCode = codec.Create(requestId, expiresAt);
        followUps.Access = new DemoFollowUpAccessRecord(
            requestId,
            demoRequestId,
            DemoFollowUpCatalog.Pending,
            expiresAt,
            requestedAt,
            null);

        using var contextResponse = await client.PostAsJsonAsync(
            "/api/public/demo-request-details/context",
            new DemoFollowUpTokenRequest(accessCode));
        Assert.Equal(HttpStatusCode.OK, contextResponse.StatusCode);
        Assert.Contains("\"status\":\"Pending\"", await contextResponse.Content.ReadAsStringAsync());

        var invalid = ValidFollowUpSubmission(accessCode) with { NoCuiConfirmed = false };
        using var invalidResponse = await client.PostAsJsonAsync("/api/public/demo-request-details/responses", invalid);
        Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);
        Assert.Null(followUps.ResponseCommand);
        Assert.Contains("noCuiConfirmed", await invalidResponse.Content.ReadAsStringAsync());

        using var accepted = await client.PostAsJsonAsync(
            "/api/public/demo-request-details/responses",
            ValidFollowUpSubmission(accessCode));
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        Assert.Equal(["CmmcReadiness", "EvidenceManagement"], followUps.ResponseCommand?.Workflows);
        Assert.Equal(DemoFollowUpCatalog.NoCuiNoticeVersion, followUps.ResponseCommand?.NoCuiNoticeVersion);

        followUps.Access = followUps.Access with { Status = DemoFollowUpCatalog.Responded, RespondedAt = DateTimeOffset.UtcNow };
        using var replay = await client.PostAsJsonAsync(
            "/api/public/demo-request-details/responses",
            ValidFollowUpSubmission(accessCode));
        Assert.Equal(HttpStatusCode.Conflict, replay.StatusCode);

        using var tampered = await client.PostAsJsonAsync(
            "/api/public/demo-request-details/context",
            new DemoFollowUpTokenRequest(accessCode + "x"));
        Assert.Equal(HttpStatusCode.NotFound, tampered.StatusCode);
    }

    [Fact]
    public async Task Expired_detail_link_is_read_only_and_returns_gone_on_submission()
    {
        var followUps = new StubFollowUpRepository();
        await using var factory = CreateFactory(new StubRepository(), followUpRepository: followUps);
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var codec = scope.ServiceProvider.GetRequiredService<DemoFollowUpTokenCodec>();
        var requestId = Guid.NewGuid();
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        var accessCode = codec.Create(requestId, expiresAt);
        followUps.Access = new DemoFollowUpAccessRecord(
            requestId, Guid.NewGuid(), DemoFollowUpCatalog.Pending, expiresAt,
            expiresAt.AddHours(-2), null);

        using var contextResponse = await client.PostAsJsonAsync(
            "/api/public/demo-request-details/context",
            new DemoFollowUpTokenRequest(accessCode));
        Assert.Equal(HttpStatusCode.OK, contextResponse.StatusCode);
        Assert.Contains("\"status\":\"Expired\"", await contextResponse.Content.ReadAsStringAsync());

        using var submitResponse = await client.PostAsJsonAsync(
            "/api/public/demo-request-details/responses",
            ValidFollowUpSubmission(accessCode));
        Assert.Equal(HttpStatusCode.Gone, submitResponse.StatusCode);
        Assert.Null(followUps.ResponseCommand);
    }

    [Fact]
    public async Task Appointment_confirmation_requires_permission_and_uses_authenticated_operator_as_host()
    {
        var repository = new StubRepository();
        var appointments = new StubAppointmentRepository();
        await using var factory = CreateFactory(repository, appointmentRepository: appointments);
        using var client = factory.CreateClient();
        var id = Guid.NewGuid();
        var localStart = DateTime.UtcNow.AddDays(3).ToString("yyyy-MM-dd'T'HH:mm");

        using var denied = new HttpRequestMessage(HttpMethod.Post, $"/api/platform/demo-requests/{id}/appointment-confirmation");
        denied.Headers.Add("X-Gccs-Dev-Auth", "true");
        denied.Headers.Add("X-Gccs-Dev-Tenant", "none");
        denied.Headers.Add("X-Gccs-Dev-Platform-Permissions", "ProvisionTenants");
        denied.Content = JsonContent.Create(new ConfirmDemoAppointment(localStart, "America/New_York", "ConnectionDetailsToFollow", null));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(denied)).StatusCode);
        Assert.Null(appointments.Command);

        using var allowed = new HttpRequestMessage(HttpMethod.Post, $"/api/platform/demo-requests/{id}/appointment-confirmation");
        allowed.Headers.Add("X-Gccs-Dev-Auth", "true");
        allowed.Headers.Add("X-Gccs-Dev-Tenant", "none");
        allowed.Headers.Add("X-Gccs-Dev-User", "71717171-7171-7171-7171-717171717171");
        allowed.Headers.Add("X-Gccs-Dev-Platform-Permissions", "ManageDemoRequests");
        allowed.Content = JsonContent.Create(new ConfirmDemoAppointment(localStart, "America/New_York", "ConnectionDetailsToFollow", null));
        using var response = await client.SendAsync(allowed);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(id, appointments.Command?.DemoRequestId);
        Assert.Equal(Guid.Parse("71717171-7171-7171-7171-717171717171"), appointments.Command?.HostUserId);
        Assert.Contains("\"schedulingStatus\":\"Confirmed\"", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Appointment_service_rejects_ambiguous_daylight_saving_time_and_unsafe_meeting_links()
    {
        var service = new DemoAppointmentService(
            new StubAppointmentRepository(),
            new FixedTimeProvider(new DateTimeOffset(2026, 8, 12, 0, 0, 0, TimeSpan.Zero)));

        var ambiguous = await Assert.ThrowsAsync<DemoAppointmentValidationException>(() => service.ConfirmAsync(
            Guid.NewGuid(),
            new ConfirmDemoAppointment("2026-11-01T01:30", "America/New_York", "ConnectionDetailsToFollow", null),
            Guid.NewGuid()));
        Assert.Contains("ambiguous", ambiguous.Errors["confirmedLocalStart"].Single(), StringComparison.OrdinalIgnoreCase);

        var unsafeLink = await Assert.ThrowsAsync<DemoAppointmentValidationException>(() => service.ConfirmAsync(
            Guid.NewGuid(),
            new ConfirmDemoAppointment("2026-08-15T13:30", "America/New_York", "MicrosoftTeams", "https://user:password@example.com/meeting"),
            Guid.NewGuid()));
        Assert.Contains("without embedded credentials", unsafeLink.Errors["meetingJoinUrl"].Single(), StringComparison.OrdinalIgnoreCase);
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
    public async Task Development_capture_completes_without_claiming_email_was_sent()
    {
        var claim = CreateClaim(attempt: 1);
        var repository = new StubRepository { Claim = claim };
        var transport = new StubSender(result: new DemoRequestDeliveryResult(DemoRequestDeliveryDisposition.Captured));
        var service = new DemoRequestDeliveryService(
            repository,
            transport,
            new DemoRequestDeliverySettings(TimeSpan.FromMinutes(5), 2),
            TimeProvider.System);

        Assert.True(await service.ProcessNextAsync());
        Assert.Equal(DemoRequestDeliveryDisposition.Captured, repository.Completion?.Disposition);
        Assert.Null(repository.Completion?.ProviderMessageId);
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

    [Theory]
    [InlineData("RequesterAcknowledgement")]
    [InlineData("OperatorResponse:ReviewingRequestedTime")]
    [InlineData("OperatorResponse:RequestMoreDetails")]
    [InlineData("OperatorResponse:RequestedTimeUnavailable")]
    public void Requester_facing_messages_route_replies_to_the_monitored_operations_inbox(string deliveryKind)
    {
        var options = new DemoRequestOptions
        {
            SenderAddress = "donotreply@example.com",
            RecipientAddress = "demo-operations@example.com"
        };

        var message = AzureCommunicationDemoRequestEmailSender.CreateMessage(
            options,
            CreateClaim(1) with { DeliveryKind = deliveryKind });

        Assert.Equal("donotreply@example.com", message.SenderAddress);
        Assert.Collection(message.ReplyTo, replyTo => Assert.Equal("demo-operations@example.com", replyTo.Address));
    }

    [Fact]
    public void Internal_notification_does_not_redirect_operator_replies_back_to_the_operations_inbox()
    {
        var options = new DemoRequestOptions
        {
            SenderAddress = "donotreply@example.com",
            RecipientAddress = "demo-operations@example.com"
        };

        var message = AzureCommunicationDemoRequestEmailSender.CreateMessage(options, CreateClaim(1));

        Assert.Empty(message.ReplyTo);
    }

    [Fact]
    public void Appointment_confirmation_uses_the_persisted_snapshot_and_routes_replies_to_operations()
    {
        var options = new DemoRequestOptions
        {
            SenderAddress = "donotreply@example.com",
            RecipientAddress = "demo-operations@example.com"
        };
        var start = new DateTimeOffset(2026, 8, 15, 18, 0, 0, TimeSpan.Zero);
        var message = AzureCommunicationDemoRequestEmailSender.CreateMessage(
            options,
            CreateClaim(1) with
            {
                DeliveryKind = $"AppointmentConfirmed:{Guid.NewGuid():N}",
                ConfirmedStartAt = start,
                ConfirmedEndAt = start.AddMinutes(30),
                ConfirmedTimeZone = "America/New_York",
                DurationMinutes = 30,
                MeetingMethod = "MicrosoftTeams",
                MeetingJoinUrl = "https://teams.microsoft.com/meeting?context=one&tenant=two"
            });

        Assert.Contains("Saturday, August 15, 2026 at 2:00 PM", message.Content.PlainText);
        Assert.Contains("30-minute", message.Content.PlainText);
        Assert.Contains("Do not share CUI", message.Content.PlainText);
        Assert.Contains("context=one&amp;tenant=two", message.Content.Html);
        Assert.Collection(message.ReplyTo, replyTo => Assert.Equal("demo-operations@example.com", replyTo.Address));
    }

    [Fact]
    public void Detail_request_email_uses_fragment_token_specific_examples_and_no_cui_boundary()
    {
        var options = new DemoRequestOptions
        {
            SenderAddress = "donotreply@example.com",
            RecipientAddress = "demo-operations@example.com",
            PublicWebBaseUrl = "https://fedril.example",
            FollowUpTokenSigningKey = "test-follow-up-signing-key-at-least-32-characters",
            FollowUpTokenLifetimeHours = 72
        };
        var followUpRequestId = Guid.NewGuid();
        var expiresAt = new DateTimeOffset(2026, 8, 15, 18, 0, 0, TimeSpan.Zero);
        var message = AzureCommunicationDemoRequestEmailSender.CreateMessage(
            options,
            CreateClaim(1) with
            {
                DeliveryKind = $"DemoFollowUpRequested:{followUpRequestId:N}",
                FollowUpRequestId = followUpRequestId,
                FollowUpExpiresAt = expiresAt
            });

        Assert.Contains("/demo-request-details#" + "tok" + "en=v1.", message.Content.PlainText);
        Assert.DoesNotContain("?token=", message.Content.PlainText, StringComparison.Ordinal);
        Assert.Contains("contract and clause intake", message.Content.PlainText);
        Assert.Contains("subcontractor flow-down", message.Content.PlainText);
        Assert.Contains("Do not include CUI", message.Content.PlainText);
        Assert.Collection(message.ReplyTo, replyTo => Assert.Equal("demo-operations@example.com", replyTo.Address));
    }

    [Fact]
    public async Task Appointment_events_are_append_only_through_tracked_persistence()
    {
        var options = new DbContextOptionsBuilder<GccsDbContext>()
            .UseInMemoryDatabase($"demo-appointment-events-{Guid.NewGuid():N}")
            .Options;
        await using var context = new GccsDbContext(options);
        var entity = new DemoAppointmentEventEntity
        {
            Id = Guid.NewGuid(), DemoAppointmentId = Guid.NewGuid(), DemoRequestId = Guid.NewGuid(),
            EventType = "Confirmed", PreviousStatus = "Requested", NewStatus = "Confirmed",
            ConfirmedStartAt = DateTimeOffset.UtcNow.AddDays(2), ConfirmedEndAt = DateTimeOffset.UtcNow.AddDays(2).AddMinutes(30),
            ConfirmedTimeZone = "America/New_York", DurationMinutes = 30, HostUserId = Guid.NewGuid(),
            MeetingMethod = "ConnectionDetailsToFollow", ActorUserId = Guid.NewGuid(), OccurredAt = DateTimeOffset.UtcNow
        };
        context.DemoAppointmentEvents.Add(entity);
        await context.SaveChangesAsync();

        entity.NewStatus = "Changed";
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        Assert.Contains("append-only", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Demo_follow_up_responses_are_append_only_through_tracked_persistence()
    {
        var options = new DbContextOptionsBuilder<GccsDbContext>()
            .UseInMemoryDatabase($"demo-follow-up-responses-{Guid.NewGuid():N}")
            .Options;
        await using var context = new GccsDbContext(options);
        var entity = new DemoFollowUpResponseEntity
        {
            Id = Guid.NewGuid(),
            DemoFollowUpRequestId = Guid.NewGuid(),
            DemoRequestId = Guid.NewGuid(),
            WorkflowsJson = "[\"EvidenceManagement\"]",
            Goals = "Prepare a focused demonstration.",
            Challenges = "Evidence is fragmented.",
            NoCuiConfirmed = true,
            NoCuiNoticeVersion = DemoFollowUpCatalog.NoCuiNoticeVersion,
            SubmittedAt = DateTimeOffset.UtcNow,
            IpAddress = "127.0.0.1",
            UserAgent = "test",
            CorrelationId = "test"
        };
        context.DemoFollowUpResponses.Add(entity);
        await context.SaveChangesAsync();

        entity.Goals = "Changed";
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        Assert.Contains("append-only", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task Concurrent_same_host_confirmations_persist_one_appointment_event_and_outbox_record()
    {
        var connectionString = Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION")!;
        var firstRequest = CreateRecord(Guid.NewGuid(), Convert.ToHexString(Guid.NewGuid().ToByteArray()).PadRight(64, '0'));
        var secondRequest = CreateRecord(Guid.NewGuid(), Convert.ToHexString(Guid.NewGuid().ToByteArray()).PadRight(64, '0'));
        var options = new DbContextOptionsBuilder<GccsDbContext>().UseNpgsql(connectionString).Options;
        var hostUserId = Guid.NewGuid();
        var start = DateTimeOffset.UtcNow.AddDays(10);

        await using (var setup = new GccsDbContext(options))
        {
            await setup.Database.MigrateAsync();
            await new EfDemoRequestRepository(setup).CreateIfNewAsync(firstRequest);
            await new EfDemoRequestRepository(setup).CreateIfNewAsync(secondRequest);
        }

        try
        {
            await using var firstContext = new GccsDbContext(options);
            await using var secondContext = new GccsDbContext(options);
            var firstCommand = AppointmentCommand(firstRequest.Id, hostUserId, start);
            var secondCommand = AppointmentCommand(secondRequest.Id, hostUserId, start.AddMinutes(10));

            var results = await Task.WhenAll(
                new EfDemoAppointmentRepository(firstContext, new StubAuditRequestMetadata()).ConfirmAsync(firstCommand),
                new EfDemoAppointmentRepository(secondContext, new StubAuditRequestMetadata()).ConfirmAsync(secondCommand));

            Assert.Single(results, result => result.Disposition == DemoAppointmentConfirmationDisposition.Confirmed);
            Assert.Single(results, result => result.Disposition == DemoAppointmentConfirmationDisposition.HostConflict);

            var confirmedCommand = results[0].Disposition == DemoAppointmentConfirmationDisposition.Confirmed
                ? firstCommand
                : secondCommand;
            await using (var repeatContext = new GccsDbContext(options))
            {
                var repeated = await new EfDemoAppointmentRepository(
                    repeatContext,
                    new StubAuditRequestMetadata()).ConfirmAsync(confirmedCommand);

                Assert.Equal(DemoAppointmentConfirmationDisposition.AlreadyConfirmed, repeated.Disposition);
            }

            await using var verification = new GccsDbContext(options);
            var requestIds = new[] { firstRequest.Id, secondRequest.Id };
            Assert.Equal(1, await verification.DemoAppointments.CountAsync(item => requestIds.Contains(item.DemoRequestId)));
            Assert.Equal(1, await verification.DemoAppointmentEvents.CountAsync(item => requestIds.Contains(item.DemoRequestId)));
            Assert.Equal(1, await verification.DemoRequestDeliveries.CountAsync(item => requestIds.Contains(item.DemoRequestId) && item.DemoAppointmentEventId != null));
        }
        finally
        {
            await using var cleanup = new GccsDbContext(options);
            var requestIds = new[] { firstRequest.Id, secondRequest.Id };
            await cleanup.DemoRequestDeliveries.Where(item => requestIds.Contains(item.DemoRequestId)).ExecuteDeleteAsync();
            await cleanup.DemoAppointmentEvents.Where(item => requestIds.Contains(item.DemoRequestId)).ExecuteDeleteAsync();
            await cleanup.DemoAppointments.Where(item => requestIds.Contains(item.DemoRequestId)).ExecuteDeleteAsync();
            await cleanup.DemoRequests.Where(item => requestIds.Contains(item.Id)).ExecuteDeleteAsync();
        }
    }

    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task Concurrent_follow_up_submissions_persist_one_append_only_response_and_platform_projection()
    {
        var connectionString = Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION")!;
        var demoRequest = CreateRecord(Guid.NewGuid(), Convert.ToHexString(Guid.NewGuid().ToByteArray()).PadRight(64, '0'));
        var options = new DbContextOptionsBuilder<GccsDbContext>().UseNpgsql(connectionString).Options;
        var requestedAt = DateTimeOffset.UtcNow;
        var followUpRequestId = Guid.NewGuid();
        var expiresAt = requestedAt.AddHours(2);
        var codec = new DemoFollowUpTokenCodec(new DemoFollowUpSecuritySettings(
            "https://fedril.example",
            System.Text.Encoding.UTF8.GetBytes("test-follow-up-signing-key-at-least-32-characters"),
            TimeSpan.FromHours(72)));
        var accessCode = codec.Create(followUpRequestId, expiresAt);
        var tokenHash = DemoFollowUpTokenCodec.Hash(accessCode);

        await using (var setup = new GccsDbContext(options))
        {
            await setup.Database.MigrateAsync();
            await new EfDemoRequestRepository(setup).CreateIfNewAsync(demoRequest);
            var queued = await new EfDemoFollowUpRepository(setup, new StubAuditRequestMetadata()).QueueRequestAsync(
                new DemoFollowUpQueueCommand(
                    followUpRequestId,
                    demoRequest.Id,
                    tokenHash,
                    DemoFollowUpCatalog.TemplateVersion,
                    DemoFollowUpCatalog.NoCuiNoticeVersion,
                    expiresAt,
                    Guid.NewGuid(),
                    requestedAt));
            Assert.Equal(DemoFollowUpQueueDisposition.Queued, queued.Disposition);
        }

        try
        {
            await using (var duplicateQueueContext = new GccsDbContext(options))
            {
                var duplicate = await new EfDemoFollowUpRepository(duplicateQueueContext, new StubAuditRequestMetadata()).QueueRequestAsync(
                    new DemoFollowUpQueueCommand(
                        Guid.NewGuid(), demoRequest.Id, new string('A', 64), DemoFollowUpCatalog.TemplateVersion,
                        DemoFollowUpCatalog.NoCuiNoticeVersion, expiresAt.AddHours(1), Guid.NewGuid(), requestedAt.AddMinutes(1)));
                Assert.Equal(DemoFollowUpQueueDisposition.AlreadyPending, duplicate.Disposition);
                Assert.Equal(followUpRequestId, duplicate.FollowUpRequestId);
            }

            var firstCommand = FollowUpResponseCommand(followUpRequestId, demoRequest.Id, requestedAt.AddMinutes(5));
            var secondCommand = FollowUpResponseCommand(followUpRequestId, demoRequest.Id, requestedAt.AddMinutes(5));
            await using var firstContext = new GccsDbContext(options);
            await using var secondContext = new GccsDbContext(options);
            var results = await Task.WhenAll(
                new EfDemoFollowUpRepository(firstContext, new StubAuditRequestMetadata()).SubmitResponseAsync(tokenHash, firstCommand),
                new EfDemoFollowUpRepository(secondContext, new StubAuditRequestMetadata()).SubmitResponseAsync(tokenHash, secondCommand));

            Assert.Single(results, result => result == DemoFollowUpSubmissionDisposition.Accepted);
            Assert.Single(results, result => result == DemoFollowUpSubmissionDisposition.AlreadyResponded);

            await using var verification = new GccsDbContext(options);
            Assert.Equal(1, await verification.DemoFollowUpRequests.CountAsync(item => item.DemoRequestId == demoRequest.Id));
            Assert.Equal(1, await verification.DemoFollowUpResponses.CountAsync(item => item.DemoRequestId == demoRequest.Id));
            Assert.Equal(1, await verification.DemoRequestDeliveries.CountAsync(item => item.DemoRequestId == demoRequest.Id && item.DemoFollowUpRequestId != null));
            Assert.Equal(DemoFollowUpCatalog.Responded, await verification.DemoFollowUpRequests.Where(item => item.Id == followUpRequestId).Select(item => item.Status).SingleAsync());
            var operations = await new EfDemoRequestRepository(verification).ListAsync(1, 100);
            var projectedRequest = Assert.Single(operations.Items, item => item.Id == demoRequest.Id);
            var projected = Assert.Single(projectedRequest.FollowUpRequests!);
            Assert.Equal(DemoFollowUpCatalog.Responded, projected.Status);
            Assert.Equal("Prepare a focused demonstration.", projected.Goals);
            Assert.Equal(["EvidenceManagement"], projected.Workflows);

            var nextRequestId = Guid.NewGuid();
            await using (var nextQueueContext = new GccsDbContext(options))
            {
                var next = await new EfDemoFollowUpRepository(nextQueueContext, new StubAuditRequestMetadata()).QueueRequestAsync(
                    new DemoFollowUpQueueCommand(
                        nextRequestId, demoRequest.Id, new string('B', 64), DemoFollowUpCatalog.TemplateVersion,
                        DemoFollowUpCatalog.NoCuiNoticeVersion, expiresAt.AddHours(2), Guid.NewGuid(), requestedAt.AddMinutes(6)));
                Assert.Equal(DemoFollowUpQueueDisposition.Queued, next.Disposition);
            }

            await using var finalVerification = new GccsDbContext(options);
            Assert.Equal(2, await finalVerification.DemoFollowUpRequests.CountAsync(item => item.DemoRequestId == demoRequest.Id));
            Assert.Equal(2, await finalVerification.DemoRequestDeliveries.CountAsync(item => item.DemoRequestId == demoRequest.Id && item.DemoFollowUpRequestId != null));
            Assert.Equal(1, await finalVerification.DemoFollowUpResponses.CountAsync(item => item.DemoRequestId == demoRequest.Id));
        }
        finally
        {
            await using var cleanup = new GccsDbContext(options);
            await cleanup.DemoRequestDeliveries.Where(item => item.DemoRequestId == demoRequest.Id).ExecuteDeleteAsync();
            await cleanup.DemoFollowUpResponses.Where(item => item.DemoRequestId == demoRequest.Id).ExecuteDeleteAsync();
            await cleanup.DemoFollowUpRequests.Where(item => item.DemoRequestId == demoRequest.Id).ExecuteDeleteAsync();
            await cleanup.DemoRequests.Where(item => item.Id == demoRequest.Id).ExecuteDeleteAsync();
        }
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
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.Status, item => item.DeliveryKind == "RequesterAcknowledgement" ? "Queued" : "Sent"));
            Assert.Equal(0, await new EfDemoRequestRepository(verification).DeleteExpiredAsync(DateTimeOffset.UtcNow.AddDays(-365)));
            Assert.True(await verification.DemoRequests.AnyAsync(item => item.Id == requestId));

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

    private WebApplicationFactory<Program> CreateFactory(
        StubRepository repository,
        int permitLimit = 20,
        StubAppointmentRepository? appointmentRepository = null,
        StubFollowUpRepository? followUpRepository = null) =>
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
                services.RemoveAll<IDemoAppointmentRepository>();
                services.AddSingleton<IDemoAppointmentRepository>(appointmentRepository ?? new StubAppointmentRepository());
                services.RemoveAll<IDemoFollowUpRepository>();
                services.AddSingleton<IDemoFollowUpRepository>(followUpRepository ?? new StubFollowUpRepository());
            });
        });

    private static SubmitDemoRequest ValidRequest() => new(
        "Avery", "Ng", "avery@example.com", "555-0101", "Northstar Systems",
        "Industry event", "11-50", "Evidence readiness workflow", DateTimeOffset.UtcNow.AddDays(2), "America/New_York", true, null);

    private static SubmitDemoFollowUpResponse ValidFollowUpSubmission(string token) => new(
        token,
        ["EvidenceManagement", "CmmcReadiness"],
        null,
        "Understand how to organize readiness work.",
        "Evidence is tracked in disconnected spreadsheets.",
        "Spreadsheet and shared drive",
        "Use synthetic examples only.",
        true,
        null);

    private static ClaimedDemoRequestDelivery CreateClaim(int attempt) => new(
        Guid.NewGuid(), Guid.NewGuid(), "Avery", "Ng", "avery@example.com", "555-0101",
        "Northstar Systems", "Industry event", "11-50", "Evidence readiness workflow",
        DateTimeOffset.UtcNow.AddDays(2), "America/New_York", DateTimeOffset.UtcNow, attempt, "InternalNotification");

    private static DemoRequestRecord CreateRecord(Guid id, string key) => new(
        id, "Avery", "Ng", "avery@example.com", null, "Northstar Systems", null, "11-50",
        "Evidence readiness workflow", DateTimeOffset.UtcNow.AddDays(2), "America/New_York", DemoRequestService.ConsentNoticeVersion, DateTimeOffset.UtcNow, key);

    private static DemoAppointmentConfirmationCommand AppointmentCommand(Guid requestId, Guid hostUserId, DateTimeOffset start) => new(
        Guid.NewGuid(), Guid.NewGuid(), requestId, start, start.AddMinutes(30), "America/New_York", 30,
        hostUserId, "ConnectionDetailsToFollow", null, DateTimeOffset.UtcNow);

    private static DemoFollowUpResponseCommand FollowUpResponseCommand(
        Guid followUpRequestId,
        Guid demoRequestId,
        DateTimeOffset submittedAt) => new(
            Guid.NewGuid(),
            followUpRequestId,
            demoRequestId,
            ["EvidenceManagement"],
            null,
            "Prepare a focused demonstration.",
            "Evidence is fragmented.",
            "Spreadsheet",
            null,
            DemoFollowUpCatalog.NoCuiNoticeVersion,
            submittedAt);

    private sealed class StubSender(
        Exception? exception = null,
        DemoRequestDeliveryResult? result = null) : IDemoRequestDeliveryTransport
    {
        public bool IsConfigured => true;
        public Task<DemoRequestDeliveryResult> DeliverAsync(ClaimedDemoRequestDelivery request, CancellationToken cancellationToken = default) =>
            exception is null
                ? Task.FromResult(result ?? new DemoRequestDeliveryResult(DemoRequestDeliveryDisposition.Sent, "provider-id"))
                : Task.FromException<DemoRequestDeliveryResult>(exception);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class StubRepository : IDemoRequestRepository
    {
        public DemoRequestRecord? Created { get; private set; }
        public ClaimedDemoRequestDelivery? Claim { get; set; }
        public DateTimeOffset? RetryAt { get; private set; }
        public bool? QueueResponseResult { get; set; }
        public string? QueuedTemplateKey { get; private set; }
        public Guid? QueuedByUserId { get; private set; }
        public IReadOnlyList<DemoRequestCalendarItem> CalendarItems { get; set; } = [];
        public DateTimeOffset? CalendarFrom { get; private set; }
        public DateTimeOffset? CalendarTo { get; private set; }
        public DemoRequestDeliveryResult? Completion { get; private set; }

        public Task CreateIfNewAsync(DemoRequestRecord request, CancellationToken cancellationToken = default)
        {
            Created ??= request;
            return Task.CompletedTask;
        }

        public Task<ClaimedDemoRequestDelivery?> TryClaimNextDeliveryAsync(DateTimeOffset now, TimeSpan leaseDuration, CancellationToken cancellationToken = default) => Task.FromResult(Claim);
        public Task MarkDeliveryCompletedAsync(Guid deliveryId, DemoRequestDeliveryResult result, DateTimeOffset completedAt, CancellationToken cancellationToken = default)
        {
            Completion = result;
            return Task.CompletedTask;
        }
        public Task MarkDeliveryFailedAsync(Guid deliveryId, string failureCode, DateTimeOffset attemptedAt, DateTimeOffset? retryAt, CancellationToken cancellationToken = default)
        {
            RetryAt = retryAt;
            return Task.CompletedTask;
        }
        public Task<int> DeleteExpiredAsync(DateTimeOffset receivedBefore, CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<DemoRequestOperationsPage> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default) =>
            Task.FromResult(new DemoRequestOperationsPage([], page, pageSize, 0, false, page > 1));
        public Task<IReadOnlyList<DemoRequestCalendarItem>> ListCalendarAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
        {
            CalendarFrom = from;
            CalendarTo = to;
            return Task.FromResult(CalendarItems);
        }
        public Task<bool?> QueueOperatorResponseAsync(Guid requestId, string templateKey, Guid actorUserId, DateTimeOffset now, CancellationToken cancellationToken = default)
        {
            QueuedTemplateKey = templateKey; QueuedByUserId = actorUserId; return Task.FromResult(QueueResponseResult);
        }
    }

    private sealed class StubAppointmentRepository : IDemoAppointmentRepository
    {
        public DemoAppointmentConfirmationCommand? Command { get; private set; }
        public DemoAppointmentConfirmationDisposition Disposition { get; set; } = DemoAppointmentConfirmationDisposition.Confirmed;

        public Task<DemoAppointmentConfirmationWriteResult> ConfirmAsync(
            DemoAppointmentConfirmationCommand command,
            CancellationToken cancellationToken = default)
        {
            Command = command;
            return Task.FromResult(new DemoAppointmentConfirmationWriteResult(
                Disposition,
                Disposition == DemoAppointmentConfirmationDisposition.Confirmed ? command.AppointmentId : null));
        }
    }

    private sealed class StubFollowUpRepository : IDemoFollowUpRepository
    {
        public DemoFollowUpQueueCommand? QueueCommand { get; private set; }
        public DemoFollowUpQueueDisposition QueueDisposition { get; set; } = DemoFollowUpQueueDisposition.Queued;
        public DemoFollowUpAccessRecord? Access { get; set; }
        public DemoFollowUpResponseCommand? ResponseCommand { get; private set; }
        public DemoFollowUpSubmissionDisposition SubmissionDisposition { get; set; } = DemoFollowUpSubmissionDisposition.Accepted;

        public Task<DemoFollowUpQueueWriteResult> QueueRequestAsync(
            DemoFollowUpQueueCommand command,
            CancellationToken cancellationToken = default)
        {
            QueueCommand = command;
            return Task.FromResult(new DemoFollowUpQueueWriteResult(
                QueueDisposition,
                command.FollowUpRequestId,
                command.ExpiresAt));
        }

        public Task<DemoFollowUpAccessRecord?> GetAccessAsync(
            Guid followUpRequestId,
            string tokenHash,
            CancellationToken cancellationToken = default) => Task.FromResult(Access);

        public Task<DemoFollowUpSubmissionDisposition> SubmitResponseAsync(
            string tokenHash,
            DemoFollowUpResponseCommand command,
            CancellationToken cancellationToken = default)
        {
            ResponseCommand = command;
            return Task.FromResult(SubmissionDisposition);
        }
    }

    private sealed record StubAuditRequestMetadata(
        string IpAddress = "127.0.0.1",
        string UserAgent = "test-agent",
        string CorrelationId = "test-correlation") : IAuditRequestMetadata;
}

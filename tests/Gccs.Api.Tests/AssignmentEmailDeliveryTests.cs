using Gccs.Application.Notifications;
using Gccs.Infrastructure.Notifications;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class AssignmentEmailDeliveryTests
{
    [Fact]
    public void Assignment_email_content_uses_external_brand_and_preserves_no_cui_boundary()
    {
        var content = AzureCommunicationAssignmentEmailSender.CreateContent(new AssignmentEmailMessage(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            "assignee@example.test",
            "Assigned <User>",
            "https://app.example.test/#/calendar?owner=<assigned>"));

        Assert.Equal("FeDril obligation task assigned", content.Subject);
        Assert.Contains("A FeDril obligation task has been assigned to you.", content.Html);
        Assert.Contains("A FeDril obligation task has been assigned to you.", content.PlainText);
        Assert.DoesNotContain("GCCS", content.Html, StringComparison.Ordinal);
        Assert.DoesNotContain("GCCS", content.PlainText, StringComparison.Ordinal);
        Assert.Contains("Do not reply with or upload CUI", content.Html);
        Assert.Contains("Do not reply with or upload CUI", content.PlainText);
        Assert.Contains("Assigned &lt;User&gt;", content.Html);
        Assert.Contains("owner=&lt;assigned&gt;", content.Html);
    }

    [Fact]
    public async Task Configured_delivery_sends_safe_link_and_marks_outbox_sent()
    {
        var delivery = CreateClaim(attemptNumber: 1);
        var repository = new StubRepository(delivery);
        var sender = new StubSender(isConfigured: true);
        var service = CreateService(repository, sender);

        var processed = await service.ProcessNextAsync();

        Assert.True(processed);
        Assert.NotNull(sender.Message);
        Assert.Equal("https://app.example.test/app#/calendar", sender.Message.AssignmentUrl);
        Assert.Equal(delivery.DeliveryId, repository.SentDeliveryId);
        Assert.Equal("provider-message-id", repository.ProviderMessageId);
        Assert.Null(repository.FailedDeliveryId);
    }

    [Fact]
    public async Task Provider_failure_schedules_retry_without_failing_assignment_flow()
    {
        var delivery = CreateClaim(attemptNumber: 2);
        var repository = new StubRepository(delivery);
        var sender = new StubSender(isConfigured: true, failure: new InvalidOperationException("provider unavailable"));
        var service = CreateService(repository, sender);

        var processed = await service.ProcessNextAsync();

        Assert.True(processed);
        Assert.Equal(delivery.DeliveryId, repository.FailedDeliveryId);
        Assert.Equal(nameof(InvalidOperationException), repository.FailureCode);
        Assert.NotNull(repository.RetryAt);
        Assert.Null(repository.SentDeliveryId);
    }

    [Fact]
    public async Task Maximum_attempt_failure_is_terminal()
    {
        var delivery = CreateClaim(attemptNumber: 3);
        var repository = new StubRepository(delivery);
        var sender = new StubSender(isConfigured: true, failure: new InvalidOperationException("provider unavailable"));
        var service = CreateService(repository, sender, maximumAttempts: 3);

        await service.ProcessNextAsync();

        Assert.Equal(delivery.DeliveryId, repository.FailedDeliveryId);
        Assert.Null(repository.RetryAt);
    }

    [Fact]
    public async Task Unconfigured_sender_does_not_claim_outbox_work()
    {
        var repository = new StubRepository(CreateClaim(attemptNumber: 1));
        var service = CreateService(repository, new StubSender(isConfigured: false));

        var processed = await service.ProcessNextAsync();

        Assert.False(processed);
        Assert.Equal(0, repository.ClaimCount);
    }

    private static AssignmentEmailDeliveryService CreateService(
        StubRepository repository,
        StubSender sender,
        int maximumAttempts = 3) =>
        new(
            repository,
            sender,
            new AssignmentEmailDeliverySettings(
                "https://app.example.test",
                TimeSpan.FromMinutes(5),
                maximumAttempts));

    private static ClaimedAssignmentEmailDelivery CreateClaim(int attemptNumber) =>
        new(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            "assignee@example.test",
            "Assigned User",
            "/#/calendar",
            attemptNumber);

    private sealed class StubSender(bool isConfigured, Exception? failure = null) : IAssignmentEmailSender
    {
        public bool IsConfigured { get; } = isConfigured;
        public AssignmentEmailMessage? Message { get; private set; }

        public Task<AssignmentEmailSendResult> SendAsync(
            AssignmentEmailMessage message,
            CancellationToken cancellationToken = default)
        {
            Message = message;
            if (failure is not null)
            {
                throw failure;
            }

            return Task.FromResult(new AssignmentEmailSendResult("provider-message-id"));
        }
    }

    private sealed class StubRepository(ClaimedAssignmentEmailDelivery? delivery) : IAssignmentEmailDeliveryRepository
    {
        public int ClaimCount { get; private set; }
        public Guid? SentDeliveryId { get; private set; }
        public string? ProviderMessageId { get; private set; }
        public Guid? FailedDeliveryId { get; private set; }
        public string? FailureCode { get; private set; }
        public DateTimeOffset? RetryAt { get; private set; }

        public Task<ClaimedAssignmentEmailDelivery?> TryClaimNextAsync(
            DateTimeOffset now,
            TimeSpan leaseDuration,
            CancellationToken cancellationToken = default)
        {
            ClaimCount++;
            return Task.FromResult(delivery);
        }

        public Task MarkSentAsync(
            Guid deliveryId,
            string providerMessageId,
            DateTimeOffset sentAt,
            CancellationToken cancellationToken = default)
        {
            SentDeliveryId = deliveryId;
            ProviderMessageId = providerMessageId;
            return Task.CompletedTask;
        }

        public Task MarkFailedAsync(
            Guid deliveryId,
            string failureCode,
            DateTimeOffset attemptedAt,
            DateTimeOffset? retryAt,
            CancellationToken cancellationToken = default)
        {
            FailedDeliveryId = deliveryId;
            FailureCode = failureCode;
            RetryAt = retryAt;
            return Task.CompletedTask;
        }
    }
}

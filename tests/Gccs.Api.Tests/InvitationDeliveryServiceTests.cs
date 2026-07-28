using Gccs.Application.Identity;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class InvitationDeliveryServiceTests
{
    [Fact]
    public async Task Queued_invitation_is_sent_with_raw_token_only_in_activation_url_and_hash_in_repository()
    {
        var repository = new FakeDeliveryRepository(CreateClaim(attemptNumber: 1));
        var sender = new CapturingSender();
        var service = CreateService(repository, sender, maximumAttempts: 3);

        var processed = await service.ProcessNextAsync();

        Assert.True(processed);
        Assert.NotNull(sender.Message);
        Assert.StartsWith("https://app.example.test/invitations/accept?token=", sender.Message.ActivationUrl, StringComparison.Ordinal);
        var rawToken = new Uri(sender.Message.ActivationUrl).Query["?token=".Length..];
        Assert.NotEqual(rawToken, repository.TokenHash);
        Assert.Equal(64, repository.TokenHash?.Length);
        Assert.Equal(1, sender.Message.AttemptNumber);
        Assert.Equal("provider-message-1", repository.ProviderMessageId);
        Assert.Null(repository.FailureCode);
    }

    [Fact]
    public async Task Transient_failure_is_scheduled_for_retry_without_persisting_exception_message()
    {
        var repository = new FakeDeliveryRepository(CreateClaim(attemptNumber: 2));
        var sender = new CapturingSender { Exception = new InvalidOperationException("secret provider detail") };
        var service = CreateService(repository, sender, maximumAttempts: 3);

        Assert.True(await service.ProcessNextAsync());

        Assert.Equal(nameof(InvalidOperationException), repository.FailureCode);
        Assert.NotNull(repository.RetryAt);
        Assert.DoesNotContain("secret", repository.FailureCode, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Final_failure_is_not_retried_after_maximum_attempts()
    {
        var repository = new FakeDeliveryRepository(CreateClaim(attemptNumber: 3));
        var sender = new CapturingSender { Exception = new InvalidOperationException("provider unavailable") };
        var service = CreateService(repository, sender, maximumAttempts: 3);

        Assert.True(await service.ProcessNextAsync());

        Assert.Equal(nameof(InvalidOperationException), repository.FailureCode);
        Assert.Null(repository.RetryAt);
    }

    private static InvitationDeliveryService CreateService(
        FakeDeliveryRepository repository,
        CapturingSender sender,
        int maximumAttempts) =>
        new(repository, sender, new InvitationDeliverySettings("https://app.example.test", TimeSpan.FromMinutes(5), maximumAttempts));

    private static ClaimedInvitationDelivery CreateClaim(int attemptNumber) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Aegis Workspace",
            "owner@example.test",
            "Pilot Owner",
            "Owner",
            DateTimeOffset.UtcNow.AddDays(7),
            attemptNumber);

    private sealed class CapturingSender : IInvitationEmailSender
    {
        public bool IsConfigured => true;
        public Exception? Exception { get; init; }
        public InvitationEmailMessage? Message { get; private set; }

        public Task<InvitationEmailSendResult> SendAsync(InvitationEmailMessage message, CancellationToken cancellationToken = default)
        {
            Message = message;
            if (Exception is not null)
            {
                throw Exception;
            }

            return Task.FromResult(new InvitationEmailSendResult("provider-message-1"));
        }
    }

    private sealed class FakeDeliveryRepository(ClaimedInvitationDelivery claim) : IInvitationDeliveryRepository
    {
        private bool _claimed;

        public string? TokenHash { get; private set; }
        public string? ProviderMessageId { get; private set; }
        public string? FailureCode { get; private set; }
        public DateTimeOffset? RetryAt { get; private set; }

        public Task<ClaimedInvitationDelivery?> TryClaimNextAsync(DateTimeOffset now, TimeSpan leaseDuration, CancellationToken cancellationToken = default)
        {
            if (_claimed)
            {
                return Task.FromResult<ClaimedInvitationDelivery?>(null);
            }

            _claimed = true;
            return Task.FromResult<ClaimedInvitationDelivery?>(claim);
        }

        public Task SetTokenHashAsync(Guid invitationId, string tokenHash, CancellationToken cancellationToken = default)
        {
            TokenHash = tokenHash;
            return Task.CompletedTask;
        }

        public Task MarkSentAsync(Guid invitationId, string providerMessageId, DateTimeOffset sentAt, CancellationToken cancellationToken = default)
        {
            ProviderMessageId = providerMessageId;
            return Task.CompletedTask;
        }

        public Task MarkFailedAsync(Guid invitationId, string failureCode, DateTimeOffset attemptedAt, DateTimeOffset? retryAt, CancellationToken cancellationToken = default)
        {
            FailureCode = failureCode;
            RetryAt = retryAt;
            return Task.CompletedTask;
        }
    }
}

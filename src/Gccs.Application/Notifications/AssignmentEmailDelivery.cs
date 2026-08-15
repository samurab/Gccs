namespace Gccs.Application.Notifications;

public sealed record ClaimedAssignmentEmailDelivery(
    Guid DeliveryId,
    Guid TenantId,
    Guid UserId,
    string RecipientEmail,
    string RecipientDisplayName,
    string LinkUrl,
    int AttemptNumber);

public sealed record AssignmentEmailMessage(
    Guid DeliveryId,
    string RecipientEmail,
    string RecipientDisplayName,
    string AssignmentUrl);

public sealed record AssignmentEmailSendResult(string ProviderMessageId);

public interface IAssignmentEmailSender
{
    bool IsConfigured { get; }

    Task<AssignmentEmailSendResult> SendAsync(
        AssignmentEmailMessage message,
        CancellationToken cancellationToken = default);
}

public interface IAssignmentEmailDeliveryRepository
{
    Task<ClaimedAssignmentEmailDelivery?> TryClaimNextAsync(
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default);

    Task MarkSentAsync(
        Guid deliveryId,
        string providerMessageId,
        DateTimeOffset sentAt,
        CancellationToken cancellationToken = default);

    Task MarkFailedAsync(
        Guid deliveryId,
        string failureCode,
        DateTimeOffset attemptedAt,
        DateTimeOffset? retryAt,
        CancellationToken cancellationToken = default);
}

public sealed class AssignmentEmailDeliveryService(
    IAssignmentEmailDeliveryRepository repository,
    IAssignmentEmailSender emailSender,
    AssignmentEmailDeliverySettings settings)
{
    public bool IsConfigured => emailSender.IsConfigured;

    public async Task<bool> ProcessNextAsync(CancellationToken cancellationToken = default)
    {
        if (!emailSender.IsConfigured)
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow;
        var delivery = await repository.TryClaimNextAsync(now, settings.LeaseDuration, cancellationToken);
        if (delivery is null)
        {
            return false;
        }

        try
        {
            var assignmentUrl = $"{settings.PublicWebBaseUrl.TrimEnd('/')}{AssignmentNotificationRoutes.NormalizeWorkspaceLink(delivery.LinkUrl)}";
            var result = await emailSender.SendAsync(
                new AssignmentEmailMessage(
                    delivery.DeliveryId,
                    delivery.RecipientEmail,
                    delivery.RecipientDisplayName,
                    assignmentUrl),
                cancellationToken);
            await repository.MarkSentAsync(delivery.DeliveryId, result.ProviderMessageId, DateTimeOffset.UtcNow, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            var attemptedAt = DateTimeOffset.UtcNow;
            DateTimeOffset? retryAt = delivery.AttemptNumber >= settings.MaximumAttempts
                ? null
                : attemptedAt.Add(ComputeRetryDelay(delivery.AttemptNumber));
            await repository.MarkFailedAsync(
                delivery.DeliveryId,
                NormalizeFailureCode(exception),
                attemptedAt,
                retryAt,
                cancellationToken);
        }

        return true;
    }

    private static TimeSpan ComputeRetryDelay(int attemptNumber) =>
        TimeSpan.FromMinutes(Math.Min(Math.Pow(2, Math.Max(0, attemptNumber - 1)), 60));

    private static string NormalizeFailureCode(Exception exception)
    {
        var code = exception.GetType().Name;
        return code.Length <= 120 ? code : code[..120];
    }
}

public sealed record AssignmentEmailDeliverySettings(
    string PublicWebBaseUrl,
    TimeSpan LeaseDuration,
    int MaximumAttempts);

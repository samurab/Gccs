using System.Security.Cryptography;

namespace Gccs.Application.Identity;

public sealed record ClaimedInvitationDelivery(
    Guid InvitationId,
    Guid TenantId,
    string TenantDisplayName,
    string RecipientEmail,
    string RecipientDisplayName,
    string RoleName,
    DateTimeOffset ExpiresAt,
    int AttemptNumber);

public sealed record InvitationEmailMessage(
    Guid InvitationId,
    string RecipientEmail,
    string RecipientDisplayName,
    string TenantDisplayName,
    string RoleName,
    DateTimeOffset ExpiresAt,
    string ActivationUrl,
    int AttemptNumber);

public sealed record InvitationEmailSendResult(string ProviderMessageId);

public interface IInvitationEmailSender
{
    bool IsConfigured { get; }

    Task<InvitationEmailSendResult> SendAsync(
        InvitationEmailMessage message,
        CancellationToken cancellationToken = default);
}

public interface IInvitationDeliveryRepository
{
    Task<ClaimedInvitationDelivery?> TryClaimNextAsync(
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default);

    Task SetTokenHashAsync(
        Guid invitationId,
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task MarkSentAsync(
        Guid invitationId,
        string providerMessageId,
        DateTimeOffset sentAt,
        CancellationToken cancellationToken = default);

    Task MarkFailedAsync(
        Guid invitationId,
        string failureCode,
        DateTimeOffset attemptedAt,
        DateTimeOffset? retryAt,
        CancellationToken cancellationToken = default);
}

public sealed class InvitationDeliveryService(
    IInvitationDeliveryRepository repository,
    IInvitationEmailSender emailSender,
    InvitationDeliverySettings settings)
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

        var token = GenerateToken();
        await repository.SetTokenHashAsync(
            delivery.InvitationId,
            TenantInvitationService.HashToken(token),
            cancellationToken);

        try
        {
            var activationUrl = $"{settings.PublicWebBaseUrl.TrimEnd('/')}/invitations/accept?token={Uri.EscapeDataString(token)}";
            var result = await emailSender.SendAsync(
                new InvitationEmailMessage(
                    delivery.InvitationId,
                    delivery.RecipientEmail,
                    delivery.RecipientDisplayName,
                    delivery.TenantDisplayName,
                    delivery.RoleName,
                    delivery.ExpiresAt,
                    activationUrl,
                    delivery.AttemptNumber),
                cancellationToken);
            await repository.MarkSentAsync(delivery.InvitationId, result.ProviderMessageId, DateTimeOffset.UtcNow, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            var attemptedAt = DateTimeOffset.UtcNow;
            DateTimeOffset? retryAt = delivery.AttemptNumber >= settings.MaximumAttempts
                ? null
                : attemptedAt.Add(ComputeRetryDelay(delivery.AttemptNumber));
            await repository.MarkFailedAsync(
                delivery.InvitationId,
                NormalizeFailureCode(exception),
                attemptedAt,
                retryAt,
                cancellationToken);
        }

        return true;
    }

    private static string GenerateToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static TimeSpan ComputeRetryDelay(int attemptNumber) =>
        TimeSpan.FromMinutes(Math.Min(Math.Pow(2, Math.Max(0, attemptNumber - 1)), 60));

    private static string NormalizeFailureCode(Exception exception)
    {
        var code = exception.GetType().Name;
        return code.Length <= 120 ? code : code[..120];
    }
}

public sealed record InvitationDeliverySettings(
    string PublicWebBaseUrl,
    TimeSpan LeaseDuration,
    int MaximumAttempts);

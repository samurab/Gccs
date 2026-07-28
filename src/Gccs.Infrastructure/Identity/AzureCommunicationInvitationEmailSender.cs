using System.Net;
using Azure;
using Azure.Communication.Email;
using Azure.Identity;
using Gccs.Application.Identity;
using Microsoft.Extensions.Options;

namespace Gccs.Infrastructure.Identity;

public sealed class InvitationEmailOptions
{
    public const string SectionName = "InvitationDelivery";

    public bool Enabled { get; set; }
    public string Provider { get; set; } = "AzureCommunicationServices";
    public string PublicWebBaseUrl { get; set; } = "http://localhost:5173";
    public string Endpoint { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    public bool UseManagedIdentity { get; set; } = true;
    public string SenderAddress { get; set; } = string.Empty;
    public int PollIntervalSeconds { get; set; } = 5;
    public int LeaseMinutes { get; set; } = 5;
    public int MaximumAttempts { get; set; } = 5;
}

public sealed class AzureCommunicationInvitationEmailSender : IInvitationEmailSender
{
    private readonly InvitationEmailOptions _options;
    private readonly EmailClient? _client;

    public AzureCommunicationInvitationEmailSender(IOptions<InvitationEmailOptions> options)
    {
        _options = options.Value;
        if (!IsConfigured)
        {
            return;
        }

        _client = _options.UseManagedIdentity
            ? new EmailClient(new Uri(_options.Endpoint), new DefaultAzureCredential())
            : new EmailClient(_options.ConnectionString);
    }

    public bool IsConfigured =>
        _options.Enabled &&
        string.Equals(_options.Provider, "AzureCommunicationServices", StringComparison.OrdinalIgnoreCase) &&
        !string.IsNullOrWhiteSpace(_options.SenderAddress) &&
        (_options.UseManagedIdentity
            ? Uri.TryCreate(_options.Endpoint, UriKind.Absolute, out _)
            : !string.IsNullOrWhiteSpace(_options.ConnectionString));

    public async Task<InvitationEmailSendResult> SendAsync(
        InvitationEmailMessage message,
        CancellationToken cancellationToken = default)
    {
        if (_client is null)
        {
            throw new InvalidOperationException("Invitation email delivery is not configured.");
        }

        var tenantName = WebUtility.HtmlEncode(message.TenantDisplayName);
        var recipientName = WebUtility.HtmlEncode(message.RecipientDisplayName);
        var roleName = WebUtility.HtmlEncode(message.RoleName);
        var activationUrl = WebUtility.HtmlEncode(message.ActivationUrl);
        var expiration = WebUtility.HtmlEncode(message.ExpiresAt.ToString("f"));
        var replacementNotice = message.AttemptNumber > 1
            ? "<p><strong>This updated email replaces every earlier invitation email. Only the link below is valid.</strong></p>"
            : string.Empty;
        var html = $"""
            <html><body>
            <p>Hello {recipientName},</p>
            <p>You have been invited to join <strong>{tenantName}</strong> as {roleName}.</p>
            {replacementNotice}
            <p><a href="{activationUrl}">Accept invitation</a></p>
            <p>This single-use link expires {expiration}. Sign in with {WebUtility.HtmlEncode(message.RecipientEmail)}.</p>
            <p>Do not forward this email. FeDril is a No-CUI compliance-management service; do not upload CUI or prohibited sensitive data.</p>
            </body></html>
            """;
        var replacementPlainText = message.AttemptNumber > 1
            ? "IMPORTANT: This updated email replaces every earlier invitation email. Only the link below is valid.\n\n"
            : string.Empty;
        var plainText = $"""
            Hello {message.RecipientDisplayName},

            You have been invited to join {message.TenantDisplayName} as {message.RoleName}.
            {replacementPlainText}Accept the invitation: {message.ActivationUrl}

            This single-use link expires {message.ExpiresAt:f}. Sign in with {message.RecipientEmail}.
            Do not forward this email. FeDril is a No-CUI compliance-management service; do not upload CUI or prohibited sensitive data.
            """;
        var subject = message.AttemptNumber > 1
            ? $"UPDATED invitation link for {message.TenantDisplayName}"
            : $"Invitation to join {message.TenantDisplayName}";
        var content = new EmailContent(subject)
        {
            Html = html,
            PlainText = plainText
        };
        var recipients = new EmailRecipients([new EmailAddress(message.RecipientEmail, message.RecipientDisplayName)]);
        var email = new EmailMessage(_options.SenderAddress, recipients, content);
        var operation = await _client.SendAsync(WaitUntil.Completed, email, cancellationToken);
        return new InvitationEmailSendResult(operation.Id);
    }
}

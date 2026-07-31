using System.Net;
using Azure;
using Azure.Communication.Email;
using Azure.Identity;
using Gccs.Application.Notifications;
using Gccs.Infrastructure.Identity;
using Microsoft.Extensions.Options;

namespace Gccs.Infrastructure.Notifications;

public sealed class AzureCommunicationAssignmentEmailSender : IAssignmentEmailSender
{
    private readonly InvitationEmailOptions _options;
    private readonly EmailClient? _client;

    public AzureCommunicationAssignmentEmailSender(IOptions<InvitationEmailOptions> options)
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

    public async Task<AssignmentEmailSendResult> SendAsync(
        AssignmentEmailMessage message,
        CancellationToken cancellationToken = default)
    {
        if (_client is null)
        {
            throw new InvalidOperationException("Assignment email delivery is not configured.");
        }

        var content = CreateContent(message);
        var recipients = new EmailRecipients([new EmailAddress(message.RecipientEmail, message.RecipientDisplayName)]);
        var email = new EmailMessage(_options.SenderAddress, recipients, content);
        var operation = await _client.SendAsync(WaitUntil.Completed, email, cancellationToken);
        return new AssignmentEmailSendResult(operation.Id);
    }

    public static EmailContent CreateContent(AssignmentEmailMessage message)
    {
        var recipientName = WebUtility.HtmlEncode(message.RecipientDisplayName);
        var assignmentUrl = WebUtility.HtmlEncode(message.AssignmentUrl);
        var html = $"""
            <html><body>
            <p>Hello {recipientName},</p>
            <p>A FeDril obligation task has been assigned to you.</p>
            <p><a href="{assignmentUrl}">Open assigned task</a></p>
            <p>This message contains workflow metadata only. Do not reply with or upload CUI, classified information, export-controlled data, or other prohibited sensitive data.</p>
            </body></html>
            """;
        var plainText = $"""
            Hello {message.RecipientDisplayName},

            A FeDril obligation task has been assigned to you.
            Open assigned task: {message.AssignmentUrl}

            This message contains workflow metadata only. Do not reply with or upload CUI, classified information, export-controlled data, or other prohibited sensitive data.
            """;
        return new EmailContent("FeDril obligation task assigned")
        {
            Html = html,
            PlainText = plainText
        };
    }
}

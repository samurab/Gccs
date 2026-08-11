using System.Net;
using Azure;
using Azure.Communication.Email;
using Azure.Identity;
using Gccs.Application.Marketing;
using Microsoft.Extensions.Options;

namespace Gccs.Infrastructure.Marketing;

public sealed class DemoRequestOptions
{
    public const string SectionName = "DemoRequests";
    public const string AzureCommunicationServicesProvider = "AzureCommunicationServices";
    public const string DevelopmentCaptureProvider = "DevelopmentCapture";
    public bool Enabled { get; set; }
    public string Provider { get; set; } = "AzureCommunicationServices";
    public string Endpoint { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    public bool UseManagedIdentity { get; set; } = true;
    public string SenderAddress { get; set; } = string.Empty;
    public string RecipientAddress { get; set; } = string.Empty;
    public int PollIntervalSeconds { get; set; } = 5;
    public int LeaseMinutes { get; set; } = 5;
    public int MaximumAttempts { get; set; } = 5;
    public int RetentionDays { get; set; } = 365;

    public static void ValidateEnabledConfiguration(DemoRequestOptions options, bool isDevelopment)
    {
        if (!options.Enabled) return;

        if (string.Equals(options.Provider, DevelopmentCaptureProvider, StringComparison.OrdinalIgnoreCase))
        {
            if (!isDevelopment)
            {
                throw new InvalidOperationException("The DevelopmentCapture demo-request provider is permitted only in the Development environment.");
            }

            return;
        }

        if (!string.Equals(options.Provider, AzureCommunicationServicesProvider, StringComparison.OrdinalIgnoreCase) ||
            !System.Net.Mail.MailAddress.TryCreate(options.RecipientAddress, out _) ||
            !System.Net.Mail.MailAddress.TryCreate(options.SenderAddress, out _) ||
            (options.UseManagedIdentity &&
                (!Uri.TryCreate(options.Endpoint, UriKind.Absolute, out var endpointUri) || endpointUri.Scheme != Uri.UriSchemeHttps)) ||
            (!options.UseManagedIdentity && string.IsNullOrWhiteSpace(options.ConnectionString)))
        {
            throw new InvalidOperationException("Enabled demo requests require a supported provider and valid delivery configuration.");
        }
    }
}

public sealed class AzureCommunicationDemoRequestEmailSender : IDemoRequestDeliveryTransport
{
    private readonly DemoRequestOptions _options;
    private readonly EmailClient? _client;

    public AzureCommunicationDemoRequestEmailSender(IOptions<DemoRequestOptions> options)
    {
        _options = options.Value;
        if (IsConfigured)
        {
            _client = _options.UseManagedIdentity
                ? new EmailClient(new Uri(_options.Endpoint), new DefaultAzureCredential())
                : new EmailClient(_options.ConnectionString);
        }
    }

    public bool IsConfigured => _options.Enabled &&
        string.Equals(_options.Provider, DemoRequestOptions.AzureCommunicationServicesProvider, StringComparison.OrdinalIgnoreCase) &&
        !string.IsNullOrWhiteSpace(_options.SenderAddress) &&
        !string.IsNullOrWhiteSpace(_options.RecipientAddress) &&
        (_options.UseManagedIdentity ? Uri.TryCreate(_options.Endpoint, UriKind.Absolute, out _) : !string.IsNullOrWhiteSpace(_options.ConnectionString));

    public async Task<DemoRequestDeliveryResult> DeliverAsync(ClaimedDemoRequestDelivery request, CancellationToken cancellationToken = default)
    {
        if (_client is null) throw new InvalidOperationException("Demo-request email delivery is not configured.");
        var isRequesterEmail = request.DeliveryKind == "RequesterAcknowledgement" || request.DeliveryKind.StartsWith("OperatorResponse:", StringComparison.Ordinal);
        var content = request.DeliveryKind == "RequesterAcknowledgement"
            ? CreateAcknowledgementContent(request)
            : request.DeliveryKind.StartsWith("OperatorResponse:", StringComparison.Ordinal)
                ? CreateOperatorResponseContent(request, request.DeliveryKind["OperatorResponse:".Length..])
                : CreateContent(request);
        var recipient = isRequesterEmail ? request.Email : _options.RecipientAddress;
        var recipients = new EmailRecipients([new EmailAddress(recipient)]);
        var operation = await _client.SendAsync(WaitUntil.Completed, new EmailMessage(_options.SenderAddress, recipients, content), cancellationToken);
        return new DemoRequestDeliveryResult(DemoRequestDeliveryDisposition.Sent, operation.Id);
    }

    public static EmailContent CreateContent(ClaimedDemoRequestDelivery request)
    {
        static string Encode(string? value) => WebUtility.HtmlEncode(value ?? "Not provided");
        var html = $"""
            <html><body>
            <h1>New FeDril live demo request</h1>
            <dl>
              <dt>Name</dt><dd>{Encode(request.FirstName)} {Encode(request.LastName)}</dd>
              <dt>Work email</dt><dd>{Encode(request.Email)}</dd>
              <dt>Phone</dt><dd>{Encode(request.Phone)}</dd>
              <dt>Company</dt><dd>{Encode(request.Company)}</dd>
              <dt>Company size</dt><dd>{Encode(request.EmployeeCount)}</dd>
              <dt>Referral source</dt><dd>{Encode(request.ReferralSource)}</dd>
            </dl>
            <h2>How FeDril can help</h2><p>{Encode(request.Message)}</p>
            <p><strong>Preferred demo time:</strong> {Encode(FormatPreferredTime(request))}</p>
            <p>Submitted {request.ReceivedAt:u}. Treat this as business-contact data. Do not request CUI or other prohibited sensitive content by reply.</p>
            </body></html>
            """;
        var plain = $"""
            New FeDril live demo request

            Name: {request.FirstName} {request.LastName}
            Work email: {request.Email}
            Phone: {request.Phone ?? "Not provided"}
            Company: {request.Company}
            Company size: {request.EmployeeCount ?? "Not provided"}
            Referral source: {request.ReferralSource ?? "Not provided"}

            How FeDril can help:
            {request.Message ?? "Not provided"}

            Preferred demo time: {FormatPreferredTime(request)}

            Submitted {request.ReceivedAt:u}. Treat this as business-contact data. Do not request CUI or other prohibited sensitive content by reply.
            """;
        return new EmailContent($"FeDril demo request — {request.Company}") { Html = html, PlainText = plain };
    }

    public static EmailContent CreateAcknowledgementContent(ClaimedDemoRequestDelivery request)
    {
        var time = FormatPreferredTime(request);
        var html = $"<html><body><h1>We received your FeDril demo request</h1><p>Thank you, {WebUtility.HtmlEncode(request.FirstName)}. We recorded your preferred time: <strong>{WebUtility.HtmlEncode(time)}</strong>.</p><p>This is an acknowledgement, not a confirmed calendar reservation. The FeDril team will confirm availability separately.</p><p>Do not reply with CUI, FCI, classified information, credentials, or other sensitive content.</p></body></html>";
        var plain = $"We received your FeDril demo request.\n\nThank you, {request.FirstName}. We recorded your preferred time: {time}.\n\nThis is an acknowledgement, not a confirmed calendar reservation. The FeDril team will confirm availability separately.\n\nDo not reply with CUI, FCI, classified information, credentials, or other sensitive content.";
        return new EmailContent("We received your FeDril demo request") { Html = html, PlainText = plain };
    }

    public static EmailContent CreateOperatorResponseContent(ClaimedDemoRequestDelivery request, string templateKey)
    {
        var name = WebUtility.HtmlEncode(request.FirstName);
        var time = WebUtility.HtmlEncode(FormatPreferredTime(request));
        var (subject, htmlBody, plainBody) = templateKey switch
        {
            "ReviewingRequestedTime" => ("FeDril demo request — reviewing your preferred time", $"<p>Thank you, {name}. We are reviewing availability for <strong>{time}</strong> and will confirm separately.</p>", $"Thank you, {request.FirstName}. We are reviewing availability for {FormatPreferredTime(request)} and will confirm separately."),
            "RequestMoreDetails" => ("A question about your FeDril demo request", $"<p>Thank you, {name}. To tailor the demonstration, please reply with the compliance-management workflows or readiness challenges you would most like to discuss.</p>", $"Thank you, {request.FirstName}. To tailor the demonstration, please reply with the compliance-management workflows or readiness challenges you would most like to discuss."),
            "RequestedTimeUnavailable" => ("FeDril demo request — alternate time needed", $"<p>Thank you, {name}. We are unavailable at <strong>{time}</strong>. Please reply with another preferred time and time zone.</p>", $"Thank you, {request.FirstName}. We are unavailable at {FormatPreferredTime(request)}. Please reply with another preferred time and time zone."),
            _ => throw new InvalidOperationException("Unsupported demo-response template.")
        };
        const string warning = "Do not reply with CUI, FCI, classified information, credentials, or other sensitive content.";
        return new EmailContent(subject) { Html = $"<html><body>{htmlBody}<p>{warning}</p></body></html>", PlainText = $"{plainBody}\n\n{warning}" };
    }

    private static string FormatPreferredTime(ClaimedDemoRequestDelivery request)
    {
        if (request.PreferredStartAt is null || string.IsNullOrWhiteSpace(request.PreferredTimeZone)) return "the requested time (legacy request; time not recorded)";
        var zone = TimeZoneInfo.FindSystemTimeZoneById(request.PreferredTimeZone);
        var local = TimeZoneInfo.ConvertTime(request.PreferredStartAt.Value, zone);
        return $"{local:dddd, MMMM d, yyyy 'at' h:mm tt} ({request.PreferredTimeZone})";
    }
}

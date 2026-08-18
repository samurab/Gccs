using System.Net;
using System.Text;
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
    public const string DevelopmentRequesterEmailProvider = "DevelopmentRequesterEmail";
    public bool Enabled { get; set; }
    public string Provider { get; set; } = "AzureCommunicationServices";
    public string Endpoint { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    public bool UseManagedIdentity { get; set; } = true;
    public string SenderAddress { get; set; } = string.Empty;
    public string RecipientAddress { get; set; } = string.Empty;
    public string PublicWebBaseUrl { get; set; } = string.Empty;
    public string FollowUpTokenSigningKey { get; set; } = string.Empty;
    public int FollowUpTokenLifetimeHours { get; set; } = 72;
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

        if (string.Equals(options.Provider, DevelopmentRequesterEmailProvider, StringComparison.OrdinalIgnoreCase))
        {
            if (!isDevelopment)
            {
                throw new InvalidOperationException("The DevelopmentRequesterEmail demo-request provider is permitted only in the Development environment.");
            }

            if (!System.Net.Mail.MailAddress.TryCreate(options.SenderAddress, out _) ||
                !IsValidPublicWebUri(options.PublicWebBaseUrl, allowLoopbackHttp: true) ||
                options.FollowUpTokenSigningKey.Length < 32 ||
                options.FollowUpTokenLifetimeHours is < 1 or > 168 ||
                (options.UseManagedIdentity &&
                    (!Uri.TryCreate(options.Endpoint, UriKind.Absolute, out var developmentEndpointUri) || developmentEndpointUri.Scheme != Uri.UriSchemeHttps)) ||
                (!options.UseManagedIdentity && string.IsNullOrWhiteSpace(options.ConnectionString)))
            {
                throw new InvalidOperationException("Enabled development requester email requires valid Azure Communication Services delivery configuration, a loopback HTTP or HTTPS public web URL, and a follow-up signing key.");
            }

            return;
        }

        if (!string.Equals(options.Provider, AzureCommunicationServicesProvider, StringComparison.OrdinalIgnoreCase) ||
            !System.Net.Mail.MailAddress.TryCreate(options.RecipientAddress, out _) ||
            !System.Net.Mail.MailAddress.TryCreate(options.SenderAddress, out _) ||
            !Uri.TryCreate(options.PublicWebBaseUrl, UriKind.Absolute, out var publicWebUri) ||
            publicWebUri.Scheme != Uri.UriSchemeHttps ||
            !string.IsNullOrEmpty(publicWebUri.UserInfo) ||
            !string.IsNullOrEmpty(publicWebUri.Query) ||
            !string.IsNullOrEmpty(publicWebUri.Fragment) ||
            options.FollowUpTokenSigningKey.Length < 32 ||
            options.FollowUpTokenLifetimeHours is < 1 or > 168 ||
            (options.UseManagedIdentity &&
                (!Uri.TryCreate(options.Endpoint, UriKind.Absolute, out var endpointUri) || endpointUri.Scheme != Uri.UriSchemeHttps)) ||
            (!options.UseManagedIdentity && string.IsNullOrWhiteSpace(options.ConnectionString)))
        {
            throw new InvalidOperationException("Enabled demo requests require a supported provider, valid delivery configuration, HTTPS public web URL, and follow-up signing key.");
        }
    }

    private static bool IsValidPublicWebUri(string value, bool allowLoopbackHttp)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment))
        {
            return false;
        }

        return uri.Scheme == Uri.UriSchemeHttps ||
            (allowLoopbackHttp && uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback);
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
        (string.Equals(_options.Provider, DemoRequestOptions.AzureCommunicationServicesProvider, StringComparison.OrdinalIgnoreCase) ||
         string.Equals(_options.Provider, DemoRequestOptions.DevelopmentRequesterEmailProvider, StringComparison.OrdinalIgnoreCase)) &&
        !string.IsNullOrWhiteSpace(_options.SenderAddress) &&
        (_options.UseManagedIdentity ? Uri.TryCreate(_options.Endpoint, UriKind.Absolute, out _) : !string.IsNullOrWhiteSpace(_options.ConnectionString));

    public async Task<DemoRequestDeliveryResult> DeliverAsync(ClaimedDemoRequestDelivery request, CancellationToken cancellationToken = default)
    {
        if (_client is null) throw new InvalidOperationException("Demo-request email delivery is not configured.");
        var operation = await _client.SendAsync(WaitUntil.Completed, CreateMessage(_options, request), cancellationToken);
        return new DemoRequestDeliveryResult(DemoRequestDeliveryDisposition.Sent, operation.Id);
    }

    public static EmailMessage CreateMessage(DemoRequestOptions options, ClaimedDemoRequestDelivery request)
    {
        var isRequesterEmail = IsRequesterEmail(request.DeliveryKind);
        var content = request.DeliveryKind switch
        {
            "RequesterAcknowledgement" => CreateAcknowledgementContent(request),
            _ when request.DeliveryKind.StartsWith("OperatorResponse:", StringComparison.Ordinal) =>
                CreateOperatorResponseContent(request, request.DeliveryKind["OperatorResponse:".Length..]),
            _ when request.DeliveryKind.StartsWith("AppointmentConfirmed:", StringComparison.Ordinal) =>
                CreateAppointmentConfirmationContent(request),
            _ when request.DeliveryKind.StartsWith("DemoFollowUpRequested:", StringComparison.Ordinal) =>
                CreateFollowUpRequestContent(options, request),
            _ => CreateContent(request)
        };
        var recipient = isRequesterEmail ? request.Email : options.RecipientAddress;
        var message = new EmailMessage(
            options.SenderAddress,
            new EmailRecipients([new EmailAddress(recipient)]),
            content);

        // Azure Communication Services requires a verified sender, which may be a DoNotReply address.
        // Route replies from requester-facing messages to the monitored demo-operations inbox instead.
        if (isRequesterEmail && System.Net.Mail.MailAddress.TryCreate(options.RecipientAddress, out _))
        {
            message.ReplyTo.Add(new EmailAddress(options.RecipientAddress));
        }

        return message;
    }

    public static bool IsRequesterEmail(string deliveryKind) =>
        deliveryKind == "RequesterAcknowledgement" ||
        deliveryKind.StartsWith("OperatorResponse:", StringComparison.Ordinal) ||
        deliveryKind.StartsWith("AppointmentConfirmed:", StringComparison.Ordinal) ||
        deliveryKind.StartsWith("DemoFollowUpRequested:", StringComparison.Ordinal);

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

    public static EmailContent CreateFollowUpRequestContent(
        DemoRequestOptions options,
        ClaimedDemoRequestDelivery request)
    {
        if (request.FollowUpRequestId is null || request.FollowUpExpiresAt is null ||
            string.IsNullOrWhiteSpace(options.PublicWebBaseUrl) ||
            options.FollowUpTokenSigningKey.Length < 32)
        {
            throw new InvalidOperationException("Demo follow-up delivery is missing its secure-link configuration or immutable request snapshot.");
        }

        var settings = new DemoFollowUpSecuritySettings(
            options.PublicWebBaseUrl.TrimEnd('/'),
            Encoding.UTF8.GetBytes(options.FollowUpTokenSigningKey),
            TimeSpan.FromHours(Math.Clamp(options.FollowUpTokenLifetimeHours, 1, 168)));
        var accessCode = new DemoFollowUpTokenCodec(settings).Create(
            request.FollowUpRequestId.Value,
            request.FollowUpExpiresAt.Value);
        var formUrl = $"{settings.PublicWebBaseUrl}/demo-request-details#{"token"}={Uri.EscapeDataString(accessCode)}";
        var encodedName = WebUtility.HtmlEncode(request.FirstName);
        var encodedUrl = WebUtility.HtmlEncode(formUrl);
        var expires = request.FollowUpExpiresAt.Value.ToUniversalTime();
        const string examples = "contract and clause intake; obligation and deadline tracking; CMMC readiness workflows; evidence organization; subcontractor flow-down tracking; or reporting preparation";
        const string warning = "Provide only non-sensitive business-process information. Do not include CUI, FCI, classified information, export-controlled or ITAR data, credentials, contract documents, security configurations, or other sensitive content.";
        var html = $"<html><body><h1>Help us tailor your FeDril demonstration</h1><p>Thank you, {encodedName}. Please use the secure form below to identify the workflows and challenges you want the demonstration to address.</p><p>Examples include {examples}.</p><p><a href=\"{encodedUrl}\">Provide demo details</a></p><p>This single-use link expires {expires:u}.</p><p>{warning}</p></body></html>";
        var plain = $"Help us tailor your FeDril demonstration.\n\nThank you, {request.FirstName}. Use the secure form to identify the workflows and challenges you want the demonstration to address.\n\nExamples include {examples}.\n\nProvide demo details: {formUrl}\n\nThis single-use link expires {expires:u}.\n\n{warning}";
        return new EmailContent("Provide details for your FeDril demonstration") { Html = html, PlainText = plain };
    }

    public static EmailContent CreateAppointmentConfirmationContent(ClaimedDemoRequestDelivery request)
    {
        var confirmedTime = FormatConfirmedTime(request);
        var method = FormatMeetingMethod(request.MeetingMethod);
        var encodedName = WebUtility.HtmlEncode(request.FirstName);
        var encodedTime = WebUtility.HtmlEncode(confirmedTime);
        var encodedMethod = WebUtility.HtmlEncode(method);
        var joinHtml = request.MeetingJoinUrl is null
            ? string.Empty
            : $"<p><strong>Join:</strong> <a href=\"{WebUtility.HtmlEncode(request.MeetingJoinUrl)}\">Open the meeting</a></p>";
        var joinPlain = request.MeetingJoinUrl is null ? string.Empty : $"\nJoin: {request.MeetingJoinUrl}";
        const string warning = "This demonstration is for non-sensitive compliance-management workflows. Do not share CUI, FCI, classified information, credentials, contract documents, or other sensitive content during scheduling, by email, or in the meeting.";

        var html = $"<html><body><h1>Your FeDril live demonstration is confirmed</h1><p>Thank you, {encodedName}. Your 30-minute demonstration is confirmed for <strong>{encodedTime}</strong>.</p><p><strong>Meeting method:</strong> {encodedMethod}</p>{joinHtml}<p>{warning}</p></body></html>";
        var plain = $"Your FeDril live demonstration is confirmed.\n\nThank you, {request.FirstName}. Your 30-minute demonstration is confirmed for {confirmedTime}.\n\nMeeting method: {method}{joinPlain}\n\n{warning}";
        return new EmailContent($"FeDril demo confirmed — {FormatConfirmedSubjectTime(request)}") { Html = html, PlainText = plain };
    }

    private static string FormatPreferredTime(ClaimedDemoRequestDelivery request)
    {
        if (request.PreferredStartAt is null || string.IsNullOrWhiteSpace(request.PreferredTimeZone)) return "the requested time (legacy request; time not recorded)";
        var zone = TimeZoneInfo.FindSystemTimeZoneById(request.PreferredTimeZone);
        var local = TimeZoneInfo.ConvertTime(request.PreferredStartAt.Value, zone);
        return $"{local:dddd, MMMM d, yyyy 'at' h:mm tt} ({request.PreferredTimeZone})";
    }

    private static string FormatConfirmedTime(ClaimedDemoRequestDelivery request)
    {
        if (request.ConfirmedStartAt is null || string.IsNullOrWhiteSpace(request.ConfirmedTimeZone) ||
            request.DurationMinutes != DemoAppointmentCatalog.DurationMinutes)
            throw new InvalidOperationException("Confirmed appointment delivery is missing its immutable scheduling snapshot.");

        var zone = TimeZoneInfo.FindSystemTimeZoneById(request.ConfirmedTimeZone);
        var local = TimeZoneInfo.ConvertTime(request.ConfirmedStartAt.Value, zone);
        return $"{local:dddd, MMMM d, yyyy 'at' h:mm tt} ({request.ConfirmedTimeZone})";
    }

    private static string FormatConfirmedSubjectTime(ClaimedDemoRequestDelivery request)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(request.ConfirmedTimeZone!);
        var local = TimeZoneInfo.ConvertTime(request.ConfirmedStartAt!.Value, zone);
        return $"{local:MMMM d 'at' h:mm tt}";
    }

    private static string FormatMeetingMethod(string? meetingMethod) => meetingMethod switch
    {
        "MicrosoftTeams" => "Microsoft Teams",
        "GoogleMeet" => "Google Meet",
        "Zoom" => "Zoom",
        "Phone" => "Phone",
        "ConnectionDetailsToFollow" => "Connection details will follow",
        _ => throw new InvalidOperationException("Confirmed appointment delivery has an unsupported meeting method.")
    };
}

namespace Gccs.Infrastructure.Persistence.Models;

public sealed class DemoRequestEntity
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Company { get; set; } = string.Empty;
    public string? ReferralSource { get; set; }
    public string? EmployeeCount { get; set; }
    public string? Message { get; set; }
    public DateTimeOffset? PreferredStartAt { get; set; }
    public string? PreferredTimeZone { get; set; }
    public string ConsentNoticeVersion { get; set; } = string.Empty;
    public string DeduplicationKey { get; set; } = string.Empty;
    public DateTimeOffset ReceivedAt { get; set; }
}

public sealed class DemoRequestDeliveryEntity
{
    public Guid Id { get; set; }
    public Guid DemoRequestId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string DeliveryKind { get; set; } = "InternalNotification";
    public Guid? RequestedByUserId { get; set; }
    public int AttemptCount { get; set; }
    public DateTimeOffset? NextAttemptAt { get; set; }
    public DateTimeOffset? LeaseUntil { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public string? ProviderMessageId { get; set; }
    public string? FailureCode { get; set; }
    public Guid? DemoAppointmentEventId { get; set; }
    public Guid? DemoFollowUpRequestId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DemoRequestEntity? DemoRequest { get; set; }
    public DemoAppointmentEventEntity? DemoAppointmentEvent { get; set; }
    public DemoFollowUpRequestEntity? DemoFollowUpRequest { get; set; }
}

public sealed class DemoAppointmentEntity
{
    public Guid Id { get; set; }
    public Guid DemoRequestId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset ConfirmedStartAt { get; set; }
    public DateTimeOffset ConfirmedEndAt { get; set; }
    public string ConfirmedTimeZone { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public Guid HostUserId { get; set; }
    public string MeetingMethod { get; set; } = string.Empty;
    public string? MeetingJoinUrl { get; set; }
    public Guid ConfirmedByUserId { get; set; }
    public DateTimeOffset ConfirmedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DemoRequestEntity? DemoRequest { get; set; }
}

public sealed class DemoAppointmentEventEntity
{
    public Guid Id { get; set; }
    public Guid DemoAppointmentId { get; set; }
    public Guid DemoRequestId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? PreviousStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public DateTimeOffset ConfirmedStartAt { get; set; }
    public DateTimeOffset ConfirmedEndAt { get; set; }
    public string ConfirmedTimeZone { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public Guid HostUserId { get; set; }
    public string MeetingMethod { get; set; } = string.Empty;
    public string? MeetingJoinUrl { get; set; }
    public Guid ActorUserId { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public DemoAppointmentEntity? DemoAppointment { get; set; }
    public DemoRequestEntity? DemoRequest { get; set; }
}

public sealed class DemoFollowUpRequestEntity
{
    public Guid Id { get; set; }
    public Guid DemoRequestId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string TemplateVersion { get; set; } = string.Empty;
    public string NoCuiNoticeVersion { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public Guid RequestedByUserId { get; set; }
    public DateTimeOffset RequestedAt { get; set; }
    public DateTimeOffset? RespondedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DemoRequestEntity? DemoRequest { get; set; }
    public DemoFollowUpResponseEntity? Response { get; set; }
}

public sealed class DemoFollowUpResponseEntity
{
    public Guid Id { get; set; }
    public Guid DemoFollowUpRequestId { get; set; }
    public Guid DemoRequestId { get; set; }
    public string WorkflowsJson { get; set; } = "[]";
    public string? OtherWorkflow { get; set; }
    public string Goals { get; set; } = string.Empty;
    public string Challenges { get; set; } = string.Empty;
    public string? CurrentProcess { get; set; }
    public string? AdditionalContext { get; set; }
    public bool NoCuiConfirmed { get; set; }
    public string NoCuiNoticeVersion { get; set; } = string.Empty;
    public DateTimeOffset SubmittedAt { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public DemoFollowUpRequestEntity? DemoFollowUpRequest { get; set; }
    public DemoRequestEntity? DemoRequest { get; set; }
}

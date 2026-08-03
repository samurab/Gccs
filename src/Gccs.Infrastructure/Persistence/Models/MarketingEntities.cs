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
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DemoRequestEntity? DemoRequest { get; set; }
}

namespace Gccs.Domain.Audit;

public sealed record AuditLogEntry(
    Guid Id,
    Guid TenantId,
    Guid? ActorUserId,
    AuditAction Action,
    string EntityType,
    string EntityId,
    DateTimeOffset OccurredAt,
    string IpAddress,
    string UserAgent,
    string Summary,
    string? OldValue,
    string? NewValue,
    IReadOnlyDictionary<string, string> Metadata);

public enum AuditAction
{
    Created,
    Viewed,
    Updated,
    Deleted,
    Uploaded,
    Downloaded,
    Approved,
    Rejected,
    Archived,
    Expired,
    Exported,
    SignedIn,
    SignedOut,
    PermissionChanged
}

using Gccs.Domain.Audit;
using Gccs.Domain.Common;
using Gccs.Domain.Tenancy;

namespace Gccs.Application.Audit;

public sealed record AuditLogQueryRequest(
    int Page,
    int PageSize,
    Guid? ActorUserId,
    string? Action,
    string? EventType,
    string? Classification,
    string? Mode,
    string? Result,
    string? EntityType,
    DateTimeOffset? From,
    DateTimeOffset? To);

public sealed record AuditLogQuery(
    int Page,
    int PageSize,
    Guid? ActorUserId,
    AuditAction? Action,
    string? EventType,
    string? Classification,
    string? Mode,
    string? Result,
    string? EntityType,
    DateTimeOffset? From,
    DateTimeOffset? To);

public sealed record AuditLogEntryDto(
    Guid Id,
    Guid TenantId,
    Guid? ActorUserId,
    string Action,
    string EventType,
    string? Classification,
    string? Mode,
    string Result,
    string EntityType,
    string EntityId,
    DateTimeOffset OccurredAt,
    string IpAddress,
    string UserAgent,
    string CorrelationId,
    string Summary,
    string? OldValue,
    string? NewValue,
    IReadOnlyDictionary<string, string> Metadata);

public sealed record PagedResultDto<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    bool HasNextPage,
    bool HasPreviousPage);

public static class AuditLogFilterNormalizer
{
    public static AuditAction? Action(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (Enum.TryParse<AuditAction>(value.Trim(), true, out var parsed)) return parsed;
        throw new ArgumentException("Audit action filter is not recognized.", nameof(value));
    }

    public static string? EventType(string? value) => Normalize(value, Phase1ACuiAuditEvents.NormalizeEventType);

    public static string? Classification(string? value) => NormalizeEnum<ContentClassification>(value);

    public static string? Mode(string? value) => NormalizeEnum<TenantDataPosture>(value);

    public static string? Result(string? value) => Normalize(value, normalized => normalized.ToLowerInvariant());

    public static string? Text(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? Normalize(string? value, Func<string, string> transform) =>
        string.IsNullOrWhiteSpace(value) ? null : transform(value.Trim());

    private static string? NormalizeEnum<TEnum>(string? value) where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return Enum.TryParse<TEnum>(value.Trim(), true, out var parsed) ? parsed.ToString() : value.Trim();
    }
}

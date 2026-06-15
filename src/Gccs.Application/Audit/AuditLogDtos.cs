using Gccs.Domain.Audit;

namespace Gccs.Application.Audit;

public sealed record AuditLogQueryRequest(
    int Page,
    int PageSize,
    Guid? ActorUserId,
    string? Action,
    string? EntityType,
    DateTimeOffset? From,
    DateTimeOffset? To);

public sealed record AuditLogQuery(
    int Page,
    int PageSize,
    Guid? ActorUserId,
    AuditAction? Action,
    string? EntityType,
    DateTimeOffset? From,
    DateTimeOffset? To);

public sealed record AuditLogEntryDto(
    Guid Id,
    Guid TenantId,
    Guid? ActorUserId,
    string Action,
    string EntityType,
    string EntityId,
    DateTimeOffset OccurredAt,
    string IpAddress,
    string UserAgent,
    string CorrelationId,
    string Summary,
    IReadOnlyDictionary<string, string> Metadata);

public sealed record PagedResultDto<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    bool HasNextPage,
    bool HasPreviousPage);

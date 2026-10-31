using Gccs.Domain.Common;
using Gccs.Domain.Compliance;

namespace Gccs.Application.Tasks;

public sealed record ComplianceTaskDto(
    Guid Id,
    Guid TenantId,
    string Title,
    string Description,
    ComplianceTaskType Type,
    string Status,
    RiskLevel Priority,
    Guid? AssignedToUserId,
    string OwnerFunction,
    DateOnly? DueAt,
    string LinkedEntityType,
    string? LinkedEntityId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record CreateComplianceTaskRequest(
    string Title,
    string Description,
    string Status,
    RiskLevel Priority,
    Guid? AssignedToUserId,
    string OwnerFunction,
    DateOnly? DueAt,
    string LinkedEntityType,
    string? LinkedEntityId);

public sealed record UpdateComplianceTaskRequest(
    string? Title,
    string? Description,
    string? Status,
    RiskLevel? Priority,
    Guid? AssignedToUserId,
    string? OwnerFunction,
    DateOnly? DueAt,
    string? LinkedEntityType,
    string? LinkedEntityId);

public sealed record ComplianceTaskSearchQuery(
    string? Status,
    Guid? OwnerUserId,
    DateOnly? DueFrom,
    DateOnly? DueTo,
    int Page = 1,
    int PageSize = 25,
    string? Cursor = null,
    bool PageWasSpecified = false);

public sealed record ComplianceTaskCursor(
    DateOnly? DueAt,
    DateTimeOffset CreatedAt,
    Guid Id);

public sealed record ComplianceTaskPageDto(
    IReadOnlyList<ComplianceTaskDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    string? NextCursor = null,
    bool HasMore = false);

public interface IComplianceTaskCursorCodec
{
    string Protect(Guid tenantId, string filterFingerprint, ComplianceTaskCursor cursor);

    bool TryUnprotect(
        string protectedCursor,
        Guid tenantId,
        string filterFingerprint,
        out ComplianceTaskCursor? cursor);
}

public interface IComplianceTaskSearchRepository
{
    Task<ComplianceTaskPageDto> SearchCurrentTenantAsync(
        ComplianceTaskStatus? status,
        ComplianceTaskSearchQuery query,
        ComplianceTaskCursor? cursor,
        CancellationToken cancellationToken = default);
}

public interface IComplianceTaskRepository
{
    Task<IReadOnlyList<ComplianceTaskDto>> ListCurrentTenantAsync(CancellationToken cancellationToken = default);

    Task<ComplianceTaskDto?> FindCurrentTenantAsync(
        Guid taskId,
        CancellationToken cancellationToken = default);

    Task<bool> IsActiveCurrentTenantMemberAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<ComplianceTaskDto?> CreateAsync(
        CreateComplianceTaskRequest request,
        ComplianceTaskStatus status,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<ComplianceTaskDto?> UpdateAsync(
        Guid taskId,
        UpdateComplianceTaskRequest request,
        ComplianceTaskStatus? status,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}

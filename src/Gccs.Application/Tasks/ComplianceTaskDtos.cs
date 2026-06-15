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

public interface IComplianceTaskRepository
{
    Task<IReadOnlyList<ComplianceTaskDto>> ListCurrentTenantAsync(CancellationToken cancellationToken = default);

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

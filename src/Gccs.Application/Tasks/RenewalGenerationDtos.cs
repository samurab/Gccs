using Gccs.Domain.Common;
using Gccs.Domain.Compliance;

namespace Gccs.Application.Tasks;

public sealed record GenerateRenewalTasksRequest(int? LeadTimeDays);

public sealed record RenewalTaskGenerationResult(
    int LeadTimeDays,
    int CreatedCount,
    int SkippedDuplicateCount,
    IReadOnlyList<RenewalTaskGenerationItem> Items);

public sealed record RenewalTaskGenerationItem(
    Guid? TaskId,
    string SourceType,
    string SourceId,
    string Title,
    ComplianceTaskType TaskType,
    RiskLevel RiskLevel,
    DateOnly SourceDueAt,
    DateOnly ReminderDueAt,
    string LinkedEntityType,
    string? LinkedEntityId,
    bool Created);

public interface IRenewalTaskRepository
{
    Task<RenewalTaskGenerationResult> GenerateForCurrentTenantAsync(
        int leadTimeDays,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}

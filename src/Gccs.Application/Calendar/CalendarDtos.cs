using Gccs.Domain.Compliance;

namespace Gccs.Application.Calendar;

public sealed record CalendarEventQuery(
    DateOnly From,
    DateOnly? To,
    string? Owner,
    string? Status,
    RiskLevel? Risk,
    Guid? ContractId,
    string? Module);

public sealed record CalendarEventDto(
    string Id,
    string Title,
    DateOnly Date,
    string Category,
    string Status,
    RiskLevel RiskLevel,
    string OwnerFunction,
    string Module,
    string? RelatedEntityType,
    string? RelatedEntityId,
    Guid? ContractId,
    bool IsOverdue);

public interface ICalendarRepository
{
    Task<IReadOnlyList<CalendarEventDto>> ListCurrentTenantAsync(
        CalendarEventQuery query,
        CancellationToken cancellationToken = default);
}

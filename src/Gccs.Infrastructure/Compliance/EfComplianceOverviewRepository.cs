using Gccs.Application.Compliance;
using Gccs.Application.Security;
using Gccs.Domain.Cmmc;
using Gccs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Compliance;

public sealed class EfComplianceOverviewRepository(
    GccsDbContext dbContext,
    ICurrentTenantContext tenantContext) : IComplianceOverviewRepository
{
    private const int RecentAuditEventCount = 10;

    public async Task<ComplianceOverviewDto> GetCurrentTenantOverviewAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.TenantId;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var controlStatuses = await dbContext.ControlAssessments
            .AsNoTracking()
            .Where(control => control.Assessment != null && control.Assessment.TenantId == tenantId)
            .GroupBy(control => control.ImplementationStatus)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToArrayAsync(cancellationToken);

        var poamStatuses = await dbContext.PoamItems
            .AsNoTracking()
            .Where(item => item.TenantId == tenantId)
            .GroupBy(item => new
            {
                IsOpen = item.Status != PoamStatus.Closed && item.Status != PoamStatus.AcceptedRisk,
                IsOverdue = item.TargetCompletionAt < today && item.Status != PoamStatus.Closed && item.Status != PoamStatus.AcceptedRisk
            })
            .Select(group => new { group.Key.IsOpen, group.Key.IsOverdue, Count = group.Count() })
            .ToArrayAsync(cancellationToken);

        var evidenceItems = await dbContext.EvidenceItems
            .AsNoTracking()
            .CountAsync(item => item.TenantId == tenantId, cancellationToken);

        var recentAuditEvents = await dbContext.AuditLogEntries
            .AsNoTracking()
            .Where(entry => entry.TenantId == tenantId)
            .OrderByDescending(entry => entry.OccurredAt)
            .ThenByDescending(entry => entry.Id)
            .Take(RecentAuditEventCount)
            .Select(entry => new RecentAuditEventDto(
                entry.Id,
                entry.ActorUserId,
                entry.Action.ToString(),
                entry.EntityType,
                entry.EntityId,
                entry.OccurredAt,
                entry.CorrelationId,
                entry.Summary))
            .ToArrayAsync(cancellationToken);

        var controlCountByStatus = controlStatuses.ToDictionary(item => item.Status, item => item.Count);

        return new ComplianceOverviewDto(
            tenantId,
            controlStatuses.Sum(item => item.Count),
            controlCountByStatus.GetValueOrDefault(ControlImplementationStatus.Implemented),
            controlCountByStatus.GetValueOrDefault(ControlImplementationStatus.PartiallyImplemented),
            controlCountByStatus.GetValueOrDefault(ControlImplementationStatus.NotStarted),
            poamStatuses.Where(item => item.IsOpen).Sum(item => item.Count),
            poamStatuses.Where(item => item.IsOverdue).Sum(item => item.Count),
            evidenceItems,
            recentAuditEvents);
    }
}

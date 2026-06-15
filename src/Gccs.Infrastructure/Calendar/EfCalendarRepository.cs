using Gccs.Application.Calendar;
using Gccs.Application.Security;
using Gccs.Domain.Cmmc;
using Gccs.Domain.Compliance;
using Gccs.Domain.Vendors;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Calendar;

public sealed class EfCalendarRepository(
    GccsDbContext dbContext,
    ICurrentTenantContext tenantContext) : ICalendarRepository
{
    public async Task<IReadOnlyList<CalendarEventDto>> ListCurrentTenantAsync(
        CalendarEventQuery query,
        CancellationToken cancellationToken = default)
    {
        var to = query.To ?? query.From.AddMonths(1);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var events = new List<CalendarEventDto>();

        var tasks = await dbContext.ComplianceTasks
            .AsNoTracking()
            .Where(task =>
                task.TenantId == tenantContext.TenantId &&
                task.DueAt.HasValue &&
                task.DueAt.Value >= query.From &&
                task.DueAt.Value <= to)
            .ToArrayAsync(cancellationToken);
        events.AddRange(tasks.Select(task => new CalendarEventDto(
            $"task:{task.Id}",
            task.Title,
            task.DueAt!.Value,
            "task",
            ToStatus(task.Status),
            task.RiskLevel,
            task.OwnerFunction,
            ToModule(task),
            ReadLink(task).Type,
            ReadLink(task).Id,
            task.ContractId,
            IsOverdue(task.DueAt, task.Status, today))));

        var deliverables = await dbContext.Set<ContractDeliverableEntity>()
            .AsNoTracking()
            .Include(deliverable => deliverable.Contract)
            .Where(deliverable =>
                deliverable.Contract != null &&
                deliverable.Contract.TenantId == tenantContext.TenantId &&
                deliverable.DueAt.HasValue &&
                deliverable.DueAt.Value >= query.From &&
                deliverable.DueAt.Value <= to)
            .ToArrayAsync(cancellationToken);
        events.AddRange(deliverables.Select(deliverable => new CalendarEventDto(
            $"deliverable:{deliverable.Id}",
            deliverable.Name,
            deliverable.DueAt!.Value,
            "deliverable",
            deliverable.Status.ToString(),
            RiskLevel.Medium,
            deliverable.OwnerFunction,
            "Contract",
            "contract",
            deliverable.ContractId.ToString(),
            deliverable.ContractId,
            deliverable.DueAt.HasValue &&
                deliverable.DueAt.Value < today &&
                deliverable.Status.ToString() is not "Submitted" and not "Accepted")));

        var deadlines = await dbContext.Set<ContractReportingDeadlineEntity>()
            .AsNoTracking()
            .Include(deadline => deadline.Contract)
            .Where(deadline =>
                deadline.Contract != null &&
                deadline.Contract.TenantId == tenantContext.TenantId &&
                deadline.DueAt >= query.From &&
                deadline.DueAt <= to)
            .ToArrayAsync(cancellationToken);
        events.AddRange(deadlines.Select(deadline => new CalendarEventDto(
            $"deadline:{deadline.Id}",
            deadline.Name,
            deadline.DueAt,
            "reporting_deadline",
            "open",
            RiskLevel.High,
            deadline.OwnerFunction,
            "Reports",
            "contract",
            deadline.ContractId.ToString(),
            deadline.ContractId,
            deadline.DueAt < today)));

        var reports = await dbContext.Reports
            .AsNoTracking()
            .Where(report =>
                report.TenantId == tenantContext.TenantId &&
                DateOnly.FromDateTime(report.GeneratedAt.UtcDateTime) >= query.From &&
                DateOnly.FromDateTime(report.GeneratedAt.UtcDateTime) <= to)
            .ToArrayAsync(cancellationToken);
        events.AddRange(reports.Select(report => new CalendarEventDto(
            $"report:{report.Id}",
            report.Title,
            DateOnly.FromDateTime(report.GeneratedAt.UtcDateTime),
            "report",
            report.Status.ToString(),
            RiskLevel.Low,
            "reports",
            "Reports",
            "report",
            report.Id.ToString(),
            null,
            false)));

        var affirmations = await dbContext.AnnualAffirmations
            .AsNoTracking()
            .Where(affirmation =>
                affirmation.TenantId == tenantContext.TenantId &&
                affirmation.DueAt >= query.From &&
                affirmation.DueAt <= to)
            .ToArrayAsync(cancellationToken);
        events.AddRange(affirmations.Select(affirmation => new CalendarEventDto(
            $"cmmc-affirmation:{affirmation.Id}",
            $"CMMC {FormatCmmcLevel(affirmation.Level)} annual affirmation",
            affirmation.DueAt,
            "cmmc_affirmation",
            affirmation.Status.ToString(),
            RiskLevel.High,
            "Security",
            "CMMC",
            "cmmc-affirmation",
            affirmation.Id.ToString(),
            null,
            affirmation.DueAt < today && affirmation.Status is not AffirmationStatus.Submitted and not AffirmationStatus.NotRequired)));

        var subcontractorEvidenceRequests = await dbContext.SubcontractorEvidenceRequests
            .AsNoTracking()
            .Include(request => request.RelatedFlowDownClause)
            .Where(request =>
                request.TenantId == tenantContext.TenantId &&
                request.DueDate >= query.From &&
                request.DueDate <= to)
            .ToArrayAsync(cancellationToken);
        events.AddRange(subcontractorEvidenceRequests.Select(request => new CalendarEventDto(
            $"subcontractor-evidence-request:{request.Id}",
            request.RequestedItem,
            request.DueDate,
            "subcontractor_evidence_request",
            ToStatus(request.Status, request.DueDate, today),
            request.DueDate < today ? RiskLevel.High : RiskLevel.Medium,
            "Subcontractors",
            "Subcontractors",
            "subcontractor_evidence_request",
            request.Id.ToString(),
            request.RelatedFlowDownClause?.ContractId,
            IsOverdue(request, today))));

        return ApplyFilters(events, query)
            .OrderBy(calendarEvent => calendarEvent.Date)
            .ThenByDescending(calendarEvent => calendarEvent.IsOverdue)
            .ThenBy(calendarEvent => calendarEvent.Title)
            .ToArray();
    }

    private static IEnumerable<CalendarEventDto> ApplyFilters(IEnumerable<CalendarEventDto> events, CalendarEventQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.Owner))
        {
            events = events.Where(item => item.OwnerFunction.Contains(query.Owner.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            events = events.Where(item => string.Equals(item.Status, query.Status.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (query.Risk.HasValue)
        {
            events = events.Where(item => item.RiskLevel == query.Risk.Value);
        }

        if (query.ContractId.HasValue)
        {
            events = events.Where(item => item.ContractId == query.ContractId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Module))
        {
            events = events.Where(item => string.Equals(item.Module, query.Module.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        return events;
    }

    private static bool IsOverdue(DateOnly? dueAt, ComplianceTaskStatus status, DateOnly today) =>
        dueAt.HasValue && dueAt.Value < today && status is not ComplianceTaskStatus.Done and not ComplianceTaskStatus.Canceled;

    private static string ToStatus(ComplianceTaskStatus status) =>
        status switch
        {
            ComplianceTaskStatus.InProgress => "in_progress",
            ComplianceTaskStatus.WaitingForReview => "waiting_for_review",
            ComplianceTaskStatus.Done => "completed",
            ComplianceTaskStatus.Canceled => "canceled",
            _ => status.ToString().ToLowerInvariant()
        };

    private static string ToStatus(SubcontractorEvidenceRequestStatus status, DateOnly dueDate, DateOnly today) =>
        IsOpenEvidenceRequest(status) && dueDate < today
            ? "overdue"
            : status switch
            {
                SubcontractorEvidenceRequestStatus.Satisfied => "satisfied",
                SubcontractorEvidenceRequestStatus.Cancelled => "cancelled",
                _ => status.ToString().ToLowerInvariant()
            };

    private static bool IsOverdue(SubcontractorEvidenceRequestEntity request, DateOnly today) =>
        request.DueDate < today && IsOpenEvidenceRequest(request.Status);

    private static bool IsOpenEvidenceRequest(SubcontractorEvidenceRequestStatus status) =>
        status is not SubcontractorEvidenceRequestStatus.Satisfied and not SubcontractorEvidenceRequestStatus.Cancelled;

    private static string ToModule(ComplianceTaskEntity task) =>
        task.Type switch
        {
            ComplianceTaskType.Renewal => "Renewals",
            ComplianceTaskType.PolicyReview => "Policy reviews",
            ComplianceTaskType.Report => "Reports",
            ComplianceTaskType.CalendarReminder => "Contract",
            _ => ReadLink(task).Type switch
            {
                "obligation" => "Obligations",
                "contract" => "Contract",
                "control" => "CMMC",
                "evidence" => "Evidence",
                "subcontractor" => "Subcontractors",
                _ => "Tasks"
            }
        };

    private static string FormatCmmcLevel(CmmcLevel level) =>
        level switch
        {
            CmmcLevel.Level1 => "Level 1",
            CmmcLevel.Level2 => "Level 2",
            CmmcLevel.Level3 => "Level 3",
            _ => level.ToString()
        };

    private static (string Type, string? Id) ReadLink(ComplianceTaskEntity entity)
    {
        if (!string.IsNullOrWhiteSpace(entity.ObligationId))
        {
            return ("obligation", entity.ObligationId);
        }

        if (!string.IsNullOrWhiteSpace(entity.ControlId))
        {
            return ("control", entity.ControlId);
        }

        if (entity.EvidenceItemId.HasValue)
        {
            return ("evidence", entity.EvidenceItemId.Value.ToString());
        }

        return entity.ContractId.HasValue
            ? ("contract", entity.ContractId.Value.ToString())
            : ("task", entity.Id.ToString());
    }
}

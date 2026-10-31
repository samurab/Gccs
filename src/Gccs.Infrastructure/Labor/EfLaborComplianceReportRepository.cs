using System.Text.Json;
using Gccs.Application.Labor;
using Gccs.Application.Common;
using Gccs.Application.Security;
using Gccs.Domain.Evidence;
using Gccs.Domain.Reports;
using Gccs.Infrastructure.Common;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Labor;

public sealed class EfLaborComplianceReportRepository(
    GccsDbContext db,
    ICurrentTenantContext tenantContext) : ILaborComplianceReportRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<LaborDashboardDto?> GetDashboardAsync(
        LaborDashboardQuery query,
        bool includeSensitiveEmployeeData,
        CancellationToken cancellationToken = default)
    {
        if (query.ContractId.HasValue && !await db.Contracts.AsNoTracking().AnyAsync(
            x => x.Id == query.ContractId && x.TenantId == tenantContext.TenantId, cancellationToken)) return null;

        var asOf = query.AsOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var applicabilityQuery = db.LaborApplicabilities.AsNoTracking()
            .Include(x => x.Task)
            .Include(x => x.SourceContractClause)
            .Where(x => x.TenantId == tenantContext.TenantId);
        var categoryQuery = db.LaborCategories.AsNoTracking()
            .Where(x => x.TenantId == tenantContext.TenantId);
        var assignmentQuery = db.LaborEmployeeAssignments.AsNoTracking()
            .Include(x => x.Employee)
            .Include(x => x.Category)
            .Include(x => x.EvidenceLinks).ThenInclude(x => x.EvidenceItem)
            .Where(x => x.TenantId == tenantContext.TenantId);

        if (query.ContractId.HasValue)
        {
            applicabilityQuery = applicabilityQuery.Where(x => x.ContractId == query.ContractId);
            categoryQuery = categoryQuery.Where(x => x.ContractId == query.ContractId);
            assignmentQuery = assignmentQuery.Where(x => x.ContractId == query.ContractId);
        }
        if (query.EmployeeId.HasValue) assignmentQuery = assignmentQuery.Where(x => x.EmployeeId == query.EmployeeId);
        if (query.LaborCategoryId.HasValue) assignmentQuery = assignmentQuery.Where(x => x.LaborCategoryId == query.LaborCategoryId);
        if (!string.IsNullOrWhiteSpace(query.Location))
        {
            var location = query.Location.Trim();
            applicabilityQuery = applicabilityQuery.Where(x => x.PlaceOfPerformance.Contains(location));
            assignmentQuery = assignmentQuery.Where(x => x.WorkLocation.Contains(location));
        }

        var applicability = await applicabilityQuery.OrderBy(x => x.ContractPeriodEnd).ThenBy(x => x.Id).ToArrayAsync(cancellationToken);
        var categories = await categoryQuery.OrderBy(x => x.Title).ThenBy(x => x.Id).ToArrayAsync(cancellationToken);
        var assignments = await assignmentQuery.OrderBy(x => x.Category!.Title).ThenBy(x => x.Id).ToArrayAsync(cancellationToken);

        var wageEvidenceIds = applicability.Where(x => x.WageDeterminationEvidenceItemId.HasValue)
            .Select(x => x.WageDeterminationEvidenceItemId!.Value).Distinct().ToArray();
        var wageEvidence = await db.EvidenceItems.AsNoTracking()
            .Where(x => x.TenantId == tenantContext.TenantId && wageEvidenceIds.Contains(x.Id) && !x.IsUseBlocked)
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var obligationDtos = applicability.Select(x => new LaborObligationReportDto(
            x.Id, x.ContractId, LaborStandard(x), x.SourceClause ?? x.SourceContractClause?.ClauseNumber,
            x.WageDeterminationReference, x.PlaceOfPerformance, x.Status, x.ReviewStatus,
            x.ReviewNotes, x.Task?.DueAt, x.WageDeterminationEvidenceItemId)).ToArray();
        var categoryDtos = categories.Select(x => new LaborCategoryReportDto(
            x.Id, x.ContractId, x.Title, x.WageDeterminationClassification, x.HourlyWage,
            x.FringeRate, x.SourceReference, x.IsActive)).ToArray();
        var assignmentDtos = assignments.Select(x => new LaborAssignmentReportDto(
            x.Id, x.ContractId, x.EmployeeId,
            includeSensitiveEmployeeData ? x.Employee?.Name : null,
            includeSensitiveEmployeeData ? x.Employee?.Email : null,
            x.LaborCategoryId, x.Category?.Title ?? string.Empty, x.WorkLocation, x.Status, x.ReviewStatus,
            includeSensitiveEmployeeData ? x.ReviewNotes : null, x.SourceReference,
            x.EvidenceLinks.Where(link => link.EvidenceItem is { IsUseBlocked: false })
                .Select(link => new LaborEvidenceReferenceDto(x.Id, link.EvidenceItemId, link.EvidenceType,
                    link.EvidenceItem!.Name, link.EvidenceItem.Status.ToString())).ToArray())).ToArray();

        var gaps = BuildGaps(obligationDtos, assignmentDtos);
        var overdue = obligationDtos.Where(x => x.Status == LaborApplicabilityStatus.Active && x.ReviewDueAt < asOf)
            .Select(x => new LaborOverdueItemDto(x.ContractId, x.Id, "LaborObligation",
                "Labor applicability review is overdue.", x.ReviewDueAt!.Value))
            .Concat(assignmentDtos.Where(x => x.Status == LaborAssignmentStatus.Active &&
                    assignments.Single(a => a.Id == x.Id).EffectiveEnd is { } end && end < asOf)
                .Select(x => new LaborOverdueItemDto(x.ContractId, x.Id, "LaborAssignment",
                    "Labor assignment effective period has ended.", assignments.Single(a => a.Id == x.Id).EffectiveEnd!.Value)))
            .OrderBy(x => x.DueDate).ToArray();

        ApplyDueRange(query, ref obligationDtos, ref assignmentDtos, overdue, assignments);
        ApplyStatus(query, ref obligationDtos, ref assignmentDtos, ref gaps, ref overdue);
        if (query.MissingEvidenceOnly)
        {
            obligationDtos = obligationDtos.Where(x => x.WageDeterminationEvidenceItemId is null).ToArray();
            assignmentDtos = assignmentDtos.Where(x => x.EvidenceReferences.Count == 0).ToArray();
        }

        var evidence = assignmentDtos.SelectMany(x => x.EvidenceReferences)
            .Concat(obligationDtos.Where(x => x.WageDeterminationEvidenceItemId.HasValue)
                .Select(x => wageEvidence.TryGetValue(x.WageDeterminationEvidenceItemId!.Value, out var item)
                    ? new LaborEvidenceReferenceDto(x.Id, item.Id, LaborEvidenceType.WageDetermination, item.Name, item.Status.ToString())
                    : null).OfType<LaborEvidenceReferenceDto>()).ToArray();
        var statusCounts = evidence.GroupBy(x => x.Status).ToDictionary(x => x.Key, x => x.Count(), StringComparer.Ordinal);

        return new LaborDashboardDto(tenantContext.TenantId, query, obligationDtos, categoryDtos,
            assignmentDtos, gaps, overdue, statusCounts);
    }

    public async Task<LaborComplianceReportDto?> GenerateAsync(
        LaborComplianceReportRequest request,
        Guid actorUserId,
        bool includeSensitiveEmployeeData,
        ContentClassificationRequest classification,
        CancellationToken cancellationToken = default)
    {
        var filters = request.Filters ?? new LaborDashboardQuery(ContractId: request.ContractId);
        if (!filters.ContractId.HasValue && request.ContractId.HasValue) filters = filters with { ContractId = request.ContractId };
        var dashboard = await GetDashboardAsync(filters, includeSensitiveEmployeeData, cancellationToken);
        if (dashboard is null) return null;

        var generatedAt = DateTimeOffset.UtcNow;
        var evidence = dashboard.Assignments.SelectMany(x => x.EvidenceReferences)
            .Concat(dashboard.Obligations.Where(x => x.WageDeterminationEvidenceItemId.HasValue)
                .Select(x => new LaborEvidenceReferenceDto(x.Id, x.WageDeterminationEvidenceItemId!.Value,
                    LaborEvidenceType.WageDetermination, x.WageDeterminationReference ?? "Wage determination", "Linked")))
            .GroupBy(x => x.EvidenceItemId).Select(x => x.First()).ToArray();
        var snapshot = new LaborComplianceSnapshotDto(
            generatedAt, request.ContractId ?? filters.ContractId,
            dashboard.Gaps.Count == 0 && dashboard.OverdueItems.Count == 0 ? "ReadyForReview" : "ActionRequired",
            LaborComplianceReportService.WorkflowDisclaimer, request.ReviewerNotes?.Trim(), filters,
            dashboard.Obligations, dashboard.Categories, dashboard.Assignments, dashboard.Gaps,
            dashboard.OverdueItems, evidence, dashboard.EvidenceStatusCounts, includeSensitiveEmployeeData);
        var entity = new ReportEntity
        {
            Id = Guid.NewGuid(), TenantId = tenantContext.TenantId, Type = ReportType.LaborCompliance,
            Title = filters.ContractId.HasValue ? "Labor compliance report - contract" : "Labor compliance report",
            Status = ReportStatus.Complete, GeneratedAt = generatedAt, GeneratedByUserId = actorUserId,
            SnapshotJson = JsonSerializer.Serialize(snapshot, JsonOptions), ExportHtml = string.Empty,
            CreatedAt = generatedAt, CreatedByUserId = actorUserId
        };
        ClassificationMetadata.Apply(entity, classification);
        entity.Contracts = dashboard.Obligations.Select(x => x.ContractId)
            .Concat(dashboard.Assignments.Select(x => x.ContractId)).Distinct()
            .Select(id => new ReportContractEntity { ReportId = entity.Id, ContractId = id }).ToArray();
        entity.EvidenceItems = evidence.Select(x => new ReportEvidenceEntity
            { ReportId = entity.Id, EvidenceItemId = x.EvidenceItemId }).ToArray();
        db.Reports.Add(entity);
        db.ContentClassificationHistory.Add(ClassificationMetadata.History(entity, entity.TenantId, "Report", actorUserId, generatedAt));
        await db.SaveChangesAsync(cancellationToken);
        return new LaborComplianceReportDto(entity.Id, entity.TenantId, entity.Type, entity.Status, entity.Title,
            entity.GeneratedAt, entity.GeneratedByUserId, snapshot, ClassificationMetadata.Read(entity));
    }

    private static LaborGapDto[] BuildGaps(IReadOnlyList<LaborObligationReportDto> obligations, IReadOnlyList<LaborAssignmentReportDto> assignments)
    {
        var gaps = obligations.Where(x => x.WageDeterminationEvidenceItemId is null)
            .Select(x => new LaborGapDto(x.ContractId, x.Id, "Missing wage determination evidence.", "Evidence"))
            .Concat(assignments.Where(x => x.EvidenceReferences.Count == 0)
                .Select(x => new LaborGapDto(x.ContractId, x.Id, "Assignment has no linked labor evidence.", "Evidence")))
            .Concat(assignments.Where(x => x.ReviewStatus != LaborClassificationReviewStatus.Reviewed)
                .Select(x => new LaborGapDto(x.ContractId, x.Id, "Assignment classification review is incomplete.", "Review")))
            .ToList();
        foreach (var contractId in obligations.Where(x => x.Status == LaborApplicabilityStatus.Active).Select(x => x.ContractId).Distinct())
            if (!assignments.Any(x => x.ContractId == contractId && x.Status == LaborAssignmentStatus.Active))
                gaps.Add(new LaborGapDto(contractId, null, "No active employee labor assignments.", "Assignment"));
        return gaps.ToArray();
    }

    private static void ApplyDueRange(LaborDashboardQuery query, ref LaborObligationReportDto[] obligations,
        ref LaborAssignmentReportDto[] assignmentDtos, LaborOverdueItemDto[] overdue, LaborEmployeeAssignmentEntity[] assignments)
    {
        if (!query.DueFrom.HasValue && !query.DueTo.HasValue) return;
        static bool InRange(DateOnly? value, DateOnly? from, DateOnly? to) => value.HasValue &&
            (!from.HasValue || value >= from) && (!to.HasValue || value <= to);
        obligations = obligations.Where(x => InRange(x.ReviewDueAt, query.DueFrom, query.DueTo)).ToArray();
        var matchingAssignmentIds = assignments.Where(x => InRange(x.EffectiveEnd, query.DueFrom, query.DueTo)).Select(x => x.Id).ToHashSet();
        assignmentDtos = assignmentDtos.Where(x => matchingAssignmentIds.Contains(x.Id)).ToArray();
    }

    private static void ApplyStatus(LaborDashboardQuery query, ref LaborObligationReportDto[] obligations,
        ref LaborAssignmentReportDto[] assignments, ref LaborGapDto[] gaps, ref LaborOverdueItemDto[] overdue)
    {
        if (!Enum.TryParse<LaborDashboardStatus>(query.Status, true, out var status)) return;
        if (status == LaborDashboardStatus.Gap)
        {
            var ids = gaps.Where(x => x.SourceRecordId.HasValue).Select(x => x.SourceRecordId!.Value).ToHashSet();
            obligations = obligations.Where(x => ids.Contains(x.Id)).ToArray(); assignments = assignments.Where(x => ids.Contains(x.Id)).ToArray();
            return;
        }
        if (status == LaborDashboardStatus.Overdue)
        {
            var ids = overdue.Select(x => x.SourceRecordId).ToHashSet();
            obligations = obligations.Where(x => ids.Contains(x.Id)).ToArray(); assignments = assignments.Where(x => ids.Contains(x.Id)).ToArray();
            return;
        }
        obligations = obligations.Where(x => string.Equals(x.Status.ToString(), status.ToString(), StringComparison.OrdinalIgnoreCase) ||
            string.Equals(x.ReviewStatus.ToString(), status.ToString(), StringComparison.OrdinalIgnoreCase)).ToArray();
        assignments = assignments.Where(x => string.Equals(x.Status.ToString(), status.ToString(), StringComparison.OrdinalIgnoreCase) ||
            string.Equals(x.ReviewStatus.ToString(), status.ToString(), StringComparison.OrdinalIgnoreCase)).ToArray();
    }

    private static string LaborStandard(LaborApplicabilityEntity x) => string.Join(", ", new[]
    {
        x.ScaApplicable ? "SCA" : null, x.DbaApplicable ? "DBA" : null,
        x.OtherFarPart22Obligations is not null ? "other FAR Part 22" : null
    }.Where(value => value is not null));
}

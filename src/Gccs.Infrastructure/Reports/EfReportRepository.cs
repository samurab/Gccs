using System.Text;
using System.Text.Json;
using Gccs.Application.Reports;
using Gccs.Application.Security;
using Gccs.Domain.Cmmc;
using Gccs.Domain.Common;
using Gccs.Domain.Compliance;
using Gccs.Domain.Evidence;
using Gccs.Domain.Reports;
using Gccs.Domain.Vendors;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Reports;

public sealed class EfReportRepository(
    GccsDbContext dbContext,
    ICurrentTenantContext tenantContext) : IReportRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<ApprovedEvidencePackageDto>> ListApprovedEvidencePackagesAsync(
        CancellationToken cancellationToken = default)
    {
        var reports = await dbContext.Reports
            .AsNoTracking()
            .Where(report =>
                report.TenantId == tenantContext.TenantId &&
                report.Type == ReportType.PrimeEvidencePackage &&
                report.Status == ReportStatus.Complete)
            .OrderByDescending(report => report.GeneratedAt)
            .Select(report => new
            {
                report.Id,
                report.TenantId,
                report.Type,
                report.Title,
                report.Status,
                report.GeneratedAt,
                report.GeneratedByUserId
            })
            .ToListAsync(cancellationToken);

        var reportIds = reports.Select(report => report.Id).ToArray();
        var evidenceByReportId = await dbContext.Set<ReportEvidenceEntity>()
            .AsNoTracking()
            .Where(link => reportIds.Contains(link.ReportId))
            .Join(
                dbContext.EvidenceItems.AsNoTracking().Where(evidence =>
                    evidence.TenantId == tenantContext.TenantId &&
                    evidence.Status == EvidenceStatus.Approved),
                link => link.EvidenceItemId,
                evidence => evidence.Id,
                (link, evidence) => new
                {
                    link.ReportId,
                    Item = new ApprovedEvidencePackageItemDto(
                        evidence.Id,
                        evidence.Name,
                        evidence.Type,
                        evidence.Status,
                        evidence.ApprovedAt,
                        evidence.ApprovedByUserId)
                })
            .ToListAsync(cancellationToken);

        var evidenceLookup = evidenceByReportId
            .GroupBy(item => item.ReportId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<ApprovedEvidencePackageItemDto>)group.Select(item => item.Item).ToArray());

        return reports
            .Select(report => new ApprovedEvidencePackageDto(
                report.Id,
                report.TenantId,
                report.Type,
                report.Title,
                report.Status,
                report.GeneratedAt,
                report.GeneratedByUserId,
                evidenceLookup.GetValueOrDefault(report.Id, [])))
            .ToArray();
    }

    public async Task<ComplianceStatusReportDto> GenerateComplianceStatusReportAsync(
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var generatedAt = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(generatedAt.UtcDateTime);

        var tenantObligationIds = await dbContext.Set<ContractClauseObligationEntity>()
            .AsNoTracking()
            .Join(
                dbContext.Set<ContractClauseEntity>().AsNoTracking(),
                link => link.ContractClauseId,
                clause => clause.Id,
                (link, clause) => new { link.ObligationId, clause.ContractId })
            .Join(
                dbContext.Contracts.AsNoTracking().Where(contract => contract.TenantId == tenantContext.TenantId),
                item => item.ContractId,
                contract => contract.Id,
                (item, _) => item.ObligationId)
            .Distinct()
            .ToArrayAsync(cancellationToken);

        var obligations = await dbContext.Obligations
            .AsNoTracking()
            .Where(obligation => tenantObligationIds.Contains(obligation.Id))
            .ToArrayAsync(cancellationToken);

        var tasks = await dbContext.ComplianceTasks
            .AsNoTracking()
            .Where(task => task.TenantId == tenantContext.TenantId)
            .ToArrayAsync(cancellationToken);

        var evidenceStatusCounts = await dbContext.EvidenceItems
            .AsNoTracking()
            .Where(evidence => evidence.TenantId == tenantContext.TenantId)
            .GroupBy(evidence => evidence.Status)
            .Select(group => new { Status = group.Key.ToString(), Count = group.Count() })
            .ToDictionaryAsync(group => group.Status, group => group.Count, cancellationToken);

        var assessments = await dbContext.Assessments
            .AsNoTracking()
            .Include(assessment => assessment.Controls)
            .Where(assessment => assessment.TenantId == tenantContext.TenantId)
            .ToArrayAsync(cancellationToken);

        var openFlowDownGaps = await dbContext.FlowDownClauses
            .AsNoTracking()
            .Include(flowDown => flowDown.Subcontractor)
            .CountAsync(flowDown =>
                flowDown.Subcontractor != null &&
                flowDown.Subcontractor.TenantId == tenantContext.TenantId &&
                flowDown.Status != FlowDownStatus.Signed &&
                flowDown.Status != FlowDownStatus.Waived &&
                flowDown.Status != FlowDownStatus.NotApplicable,
                cancellationToken);

        var openEvidenceRequestGaps = await dbContext.SubcontractorEvidenceRequests
            .AsNoTracking()
            .CountAsync(request =>
                request.TenantId == tenantContext.TenantId &&
                request.DueDate < today &&
                request.Status != SubcontractorEvidenceRequestStatus.Satisfied &&
                request.Status != SubcontractorEvidenceRequestStatus.Cancelled,
                cancellationToken);

        var overdueTasks = tasks.Count(task =>
            task.DueAt.HasValue &&
            task.DueAt.Value < today &&
            task.Status is not ComplianceTaskStatus.Done and not ComplianceTaskStatus.Canceled);

        var obligationStatusCounts = tasks
            .Where(task => !string.IsNullOrWhiteSpace(task.ObligationId))
            .GroupBy(task => task.Status.ToString())
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        var highRiskObligations = obligations
            .Where(obligation => obligation.RiskLevel is RiskLevel.High or RiskLevel.Critical)
            .ToArray();
        var highRiskItems = highRiskObligations
            .Select(obligation => $"Obligation: {obligation.Title}")
            .Concat(tasks
                .Where(task => task.RiskLevel is RiskLevel.High or RiskLevel.Critical)
                .Select(task => $"Task: {task.Title}"))
            .Take(20)
            .ToArray();

        var controls = assessments.SelectMany(assessment => assessment.Controls).ToArray();
        var snapshot = new ComplianceStatusReportSnapshotDto(
            generatedAt,
            obligations.Length,
            highRiskObligations.Length,
            overdueTasks,
            obligationStatusCounts,
            evidenceStatusCounts,
            assessments.Length,
            controls.Count(control => control.ImplementationStatus == ControlImplementationStatus.Implemented),
            controls.Length,
            openFlowDownGaps + openEvidenceRequestGaps,
            highRiskItems);
        var exportHtml = BuildHtml(snapshot);

        var entity = new ReportEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId,
            Type = ReportType.ComplianceStatus,
            Title = "Compliance status report",
            Status = ReportStatus.Complete,
            GeneratedAt = generatedAt,
            GeneratedByUserId = actorUserId,
            SnapshotJson = JsonSerializer.Serialize(snapshot, JsonOptions),
            ExportHtml = exportHtml,
            CreatedAt = generatedAt,
            CreatedByUserId = actorUserId
        };

        entity.Obligations = tenantObligationIds
            .Select(obligationId => new ReportObligationEntity
            {
                ReportId = entity.Id,
                ObligationId = obligationId
            })
            .ToArray();

        dbContext.Reports.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity, snapshot);
    }

    public async Task<CmmcReadinessReportDto?> GenerateCmmcReadinessReportAsync(
        Guid assessmentId,
        Guid actorUserId,
        bool includeEvidenceLinks,
        CancellationToken cancellationToken = default)
    {
        var assessment = await dbContext.Assessments
            .AsNoTracking()
            .Include(candidate => candidate.Controls)
            .SingleOrDefaultAsync(
                candidate => candidate.Id == assessmentId && candidate.TenantId == tenantContext.TenantId,
                cancellationToken);
        if (assessment is null)
        {
            return null;
        }

        var generatedAt = DateTimeOffset.UtcNow;
        var scopedControls = await QueryControlsForLevel(assessment.Level)
            .OrderBy(control => control.Family)
            .ThenBy(control => control.Id)
            .ToArrayAsync(cancellationToken);
        var statusLookup = assessment.Controls.ToDictionary(control => control.ControlId, StringComparer.OrdinalIgnoreCase);
        var controlRows = scopedControls.Select(control =>
        {
            statusLookup.TryGetValue(control.Id, out var status);
            return new CmmcControlReportRow(
                control,
                status?.ImplementationStatus ?? ControlImplementationStatus.NotStarted,
                status?.Result ?? AssessmentResult.NotAssessed,
                ReadGuidArray(status?.EvidenceItemIdsJson ?? "[]"));
        }).ToArray();
        var progress = controlRows
            .GroupBy(row => row.Control.Family)
            .Select(group => new CmmcFamilyProgressDto(
                group.Key,
                group.Count(),
                group.Count(row => row.Status == ControlImplementationStatus.Implemented),
                group.Count(row => row.Status == ControlImplementationStatus.PartiallyImplemented),
                group.Count(row => row.Status == ControlImplementationStatus.NotStarted),
                group.Count(row => row.Status == ControlImplementationStatus.NeedsReview),
                group.Count(row => row.Status == ControlImplementationStatus.NotApplicable)))
            .OrderBy(item => item.Family)
            .ToArray();
        var gaps = controlRows
            .Where(row => row.Status is ControlImplementationStatus.NotStarted or ControlImplementationStatus.PartiallyImplemented or ControlImplementationStatus.NeedsReview)
            .Select(row => new CmmcControlGapDto(
                row.Control.Id,
                row.Control.Family,
                row.Control.Title,
                row.Status,
                row.Result))
            .ToArray();
        var poamItems = await dbContext.PoamItems
            .AsNoTracking()
            .Where(item =>
                item.TenantId == tenantContext.TenantId &&
                item.AssessmentId == assessment.Id &&
                item.Status != PoamStatus.Closed &&
                item.Status != PoamStatus.AcceptedRisk)
            .OrderBy(item => item.TargetCompletionAt)
            .Select(item => new CmmcReportPoamItemDto(
                item.Id,
                item.ControlId,
                item.Weakness,
                item.RiskLevel,
                item.Status,
                item.TargetCompletionAt))
            .ToArrayAsync(cancellationToken);
        var evidenceLinks = includeEvidenceLinks
            ? await BuildEvidenceLinksAsync(controlRows, cancellationToken)
            : [];
        var affirmations = await dbContext.AnnualAffirmations
            .AsNoTracking()
            .Where(affirmation => affirmation.TenantId == tenantContext.TenantId)
            .OrderBy(affirmation => affirmation.DueAt)
            .Select(affirmation => new CmmcReportAffirmationDto(
                affirmation.Id,
                affirmation.Level,
                affirmation.DueAt,
                affirmation.SubmittedAt,
                affirmation.Status))
            .ToArrayAsync(cancellationToken);
        var historyCount = await dbContext.Reports
            .AsNoTracking()
            .CountAsync(
                report => report.TenantId == tenantContext.TenantId && report.Type == ReportType.CmmcReadiness,
                cancellationToken) + 1;
        var snapshot = new CmmcReadinessSnapshotDto(
            assessment.Id,
            assessment.Name,
            assessment.Level,
            generatedAt,
            progress,
            gaps,
            poamItems,
            evidenceLinks,
            affirmations,
            historyCount);
        var exportHtml = BuildHtml(snapshot);
        var entity = new ReportEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId,
            Type = ReportType.CmmcReadiness,
            Title = $"CMMC readiness report - {assessment.Name}",
            Status = ReportStatus.Complete,
            GeneratedAt = generatedAt,
            GeneratedByUserId = actorUserId,
            SnapshotJson = JsonSerializer.Serialize(snapshot, JsonOptions),
            ExportHtml = exportHtml,
            CreatedAt = generatedAt,
            CreatedByUserId = actorUserId
        };

        dbContext.Reports.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new CmmcReadinessReportDto(
            entity.Id,
            entity.TenantId,
            entity.Type,
            entity.Status,
            entity.Title,
            entity.GeneratedAt,
            entity.GeneratedByUserId,
            snapshot,
            entity.ExportHtml);
    }

    private static ComplianceStatusReportDto ToDto(ReportEntity entity, ComplianceStatusReportSnapshotDto snapshot) =>
        new(
            entity.Id,
            entity.TenantId,
            entity.Type,
            entity.Status,
            entity.Title,
            entity.GeneratedAt,
            entity.GeneratedByUserId,
            snapshot,
            entity.ExportHtml);

    private static string BuildHtml(ComplianceStatusReportSnapshotDto snapshot)
    {
        var html = new StringBuilder();
        html.Append("<!doctype html><html><head><meta charset=\"utf-8\"><title>Compliance status report</title></head><body>");
        html.Append("<h1>Compliance status report</h1>");
        html.Append("<p>Generated at ").Append(snapshot.GeneratedAt.ToString("O")).Append("</p>");
        html.Append("<ul>");
        html.Append("<li>Total obligations: ").Append(snapshot.TotalObligations).Append("</li>");
        html.Append("<li>High-risk obligations: ").Append(snapshot.HighRiskObligations).Append("</li>");
        html.Append("<li>Overdue tasks: ").Append(snapshot.OverdueTasks).Append("</li>");
        html.Append("<li>CMMC controls: ").Append(snapshot.CmmcControlsImplemented).Append(" of ").Append(snapshot.CmmcControlsTotal).Append("</li>");
        html.Append("<li>Subcontractor gaps: ").Append(snapshot.SubcontractorGaps).Append("</li>");
        html.Append("</ul>");
        html.Append("</body></html>");
        return html.ToString();
    }

    private static string BuildHtml(CmmcReadinessSnapshotDto snapshot)
    {
        var html = new StringBuilder();
        html.Append("<!doctype html><html><head><meta charset=\"utf-8\"><title>CMMC readiness report</title></head><body>");
        html.Append("<h1>CMMC readiness report</h1>");
        html.Append("<p>Target level: ").Append(snapshot.TargetLevel).Append("</p>");
        html.Append("<p>Generated at ").Append(snapshot.GeneratedAt.ToString("O")).Append("</p>");
        html.Append("<p>Open gaps: ").Append(snapshot.OpenGaps.Count).Append("</p>");
        html.Append("<p>Open POA&M items: ").Append(snapshot.OpenPoamItems.Count).Append("</p>");
        html.Append("</body></html>");
        return html.ToString();
    }

    private IQueryable<ControlEntity> QueryControlsForLevel(CmmcLevel level)
    {
        var controls = dbContext.Controls.AsNoTracking();
        return level switch
        {
            CmmcLevel.Level1 => controls.Where(control => control.CmmcLevel == CmmcLevel.Level1),
            CmmcLevel.Level2 => controls.Where(control => control.CmmcLevel == CmmcLevel.Level1 || control.CmmcLevel == CmmcLevel.Level2),
            _ => controls
        };
    }

    private async Task<IReadOnlyList<CmmcReportEvidenceLinkDto>> BuildEvidenceLinksAsync(
        IEnumerable<CmmcControlReportRow> controlRows,
        CancellationToken cancellationToken)
    {
        var pairs = controlRows
            .SelectMany(row => row.EvidenceItemIds.Select(id => new { EvidenceItemId = id, row.Control.Id }))
            .ToArray();
        var evidenceIds = pairs.Select(pair => pair.EvidenceItemId).Distinct().ToArray();
        var evidence = await dbContext.EvidenceItems
            .AsNoTracking()
            .Where(item => item.TenantId == tenantContext.TenantId && evidenceIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.Name, cancellationToken);
        return pairs
            .Where(pair => evidence.ContainsKey(pair.EvidenceItemId))
            .Select(pair => new CmmcReportEvidenceLinkDto(pair.EvidenceItemId, evidence[pair.EvidenceItemId], pair.Id))
            .ToArray();
    }

    private static IReadOnlyList<Guid> ReadGuidArray(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<Guid[]>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private sealed record CmmcControlReportRow(
        ControlEntity Control,
        ControlImplementationStatus Status,
        AssessmentResult Result,
        IReadOnlyList<Guid> EvidenceItemIds);
}

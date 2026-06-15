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
}

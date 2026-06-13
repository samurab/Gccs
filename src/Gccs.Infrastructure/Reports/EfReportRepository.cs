using Gccs.Application.Reports;
using Gccs.Application.Security;
using Gccs.Domain.Evidence;
using Gccs.Domain.Reports;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Reports;

public sealed class EfReportRepository(
    GccsDbContext dbContext,
    ICurrentTenantContext tenantContext) : IReportRepository
{
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
}

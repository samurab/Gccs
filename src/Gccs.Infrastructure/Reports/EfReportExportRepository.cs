using Gccs.Application.Reports;
using Gccs.Application.Security;
using Gccs.Domain.Reports;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Reports;

public sealed class EfReportExportRepository(
    GccsDbContext dbContext,
    ICurrentTenantContext tenantContext) : IReportExportRepository
{
    public async Task<ReportExportRequestResult?> RequestPdfAsync(
        Guid reportId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var report = await dbContext.Reports
            .AsNoTracking()
            .Where(candidate =>
                candidate.Id == reportId &&
                candidate.TenantId == tenantContext.TenantId &&
                (candidate.Type == ReportType.ComplianceStatus ||
                 candidate.Type == ReportType.CmmcReadiness ||
                 candidate.Type == ReportType.SubcontractorCompliance) &&
                (candidate.Status == ReportStatus.Complete || candidate.Status == ReportStatus.Archived))
            .Select(candidate => new { candidate.Id, candidate.Title })
            .SingleOrDefaultAsync(cancellationToken);
        if (report is null)
        {
            return null;
        }

        var existing = await FindCurrentAsync(reportId, cancellationToken);
        if (existing is not null)
        {
            if (existing.Status != ReportExportStatus.Failed)
            {
                return new ReportExportRequestResult(ToDto(existing), false, false);
            }

            var (requeued, queued) = await RequeueFailedAsync(
                existing.Id,
                existing.ReportId,
                actorUserId,
                cancellationToken);
            return new ReportExportRequestResult(ToDto(requeued), false, queued);
        }

        var now = DateTimeOffset.UtcNow;
        var entityId = Guid.NewGuid();
        var entity = new ReportExportEntity
        {
            Id = entityId,
            TenantId = tenantContext.TenantId,
            ReportId = report.Id,
            Format = "pdf",
            RenderVersion = ReportExportConstants.RenderVersion,
            Status = ReportExportStatus.Queued,
            ObjectName = ReportExportService.BuildPendingObjectName(report.Id, entityId),
            FileName = BuildFileName(report.Title, report.Id),
            ContentType = "application/pdf",
            RequestedByUserId = actorUserId,
            RequestedAt = now,
            CreatedAt = now,
            CreatedByUserId = actorUserId
        };
        if (dbContext.Database.IsNpgsql())
        {
            var status = entity.Status.ToString();
            var inserted = await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO gccs.report_exports
                    (id, tenant_id, report_id, format, render_version, status, object_name, file_name,
                     content_type, requested_by_user_id, requested_at, processing_attempt_count,
                     created_at, created_by_user_id)
                VALUES
                    ({entity.Id}, {entity.TenantId}, {entity.ReportId}, {entity.Format}, {entity.RenderVersion},
                     {status}, {entity.ObjectName}, {entity.FileName}, {entity.ContentType},
                     {entity.RequestedByUserId}, {entity.RequestedAt}, 0, {entity.CreatedAt}, {entity.CreatedByUserId})
                ON CONFLICT (tenant_id, report_id, format, render_version) DO NOTHING
                """,
                cancellationToken);
            var persisted = await FindCurrentAsync(reportId, cancellationToken)
                ?? throw new InvalidOperationException("The PDF report export could not be persisted.");
            return new ReportExportRequestResult(ToDto(persisted), inserted == 1, inserted == 1);
        }

        dbContext.ReportExports.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new ReportExportRequestResult(ToDto(entity), true, true);
    }

    public async Task<ReportExportDto?> GetAsync(Guid exportId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.ReportExports
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.Id == exportId && candidate.TenantId == tenantContext.TenantId,
                cancellationToken);
        return entity is null ? null : ToDto(entity);
    }

    public Task<ReportExportContentLocator?> GetContentLocatorAsync(
        Guid exportId,
        CancellationToken cancellationToken = default) =>
        dbContext.ReportExports
            .AsNoTracking()
            .Where(candidate => candidate.Id == exportId && candidate.TenantId == tenantContext.TenantId)
            .Select(candidate => new ReportExportContentLocator(
                candidate.TenantId,
                candidate.ReportId,
                candidate.Status,
                candidate.ObjectName))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<ClaimedReportExport?> TryClaimNextAsync(
        DateTimeOffset now,
        TimeSpan leaseDuration,
        int maximumAttempts,
        CancellationToken cancellationToken = default)
    {
        if (!dbContext.Database.IsRelational())
        {
            var candidate = await dbContext.ReportExports
                .OrderBy(item => item.RequestedAt)
                .FirstOrDefaultAsync(item =>
                    item.ProcessingAttemptCount < maximumAttempts &&
                    (item.Status == ReportExportStatus.Queued ||
                     (item.Status == ReportExportStatus.Processing &&
                      (item.ProcessingLeaseUntil == null || item.ProcessingLeaseUntil < now))),
                    cancellationToken);
            if (candidate is null)
            {
                return null;
            }

            var leaseId = Guid.NewGuid();
            candidate.Status = ReportExportStatus.Processing;
            candidate.ProcessingLeaseId = leaseId;
            candidate.ProcessingLeaseUntil = now.Add(leaseDuration);
            candidate.ProcessingAttemptCount++;
            candidate.UpdatedAt = now;
            await dbContext.SaveChangesAsync(cancellationToken);
            var email = await dbContext.Users
                .AsNoTracking()
                .Where(user => user.Id == candidate.RequestedByUserId && user.TenantId == candidate.TenantId)
                .Select(user => user.Email)
                .SingleOrDefaultAsync(cancellationToken);
            return new ClaimedReportExport(
                candidate.Id,
                candidate.TenantId,
                candidate.ReportId,
                candidate.RequestedByUserId,
                string.IsNullOrWhiteSpace(email) ? "report-export-worker@example.com" : email,
                leaseId,
                candidate.ProcessingAttemptCount);
        }

        var candidateIds = await dbContext.ReportExports
            .AsNoTracking()
            .Where(item =>
                item.ProcessingAttemptCount < maximumAttempts &&
                (item.Status == ReportExportStatus.Queued ||
                 (item.Status == ReportExportStatus.Processing &&
                  (item.ProcessingLeaseUntil == null || item.ProcessingLeaseUntil < now))))
            .OrderBy(item => item.RequestedAt)
            .Select(item => item.Id)
            .Take(10)
            .ToArrayAsync(cancellationToken);

        foreach (var candidateId in candidateIds)
        {
            var leaseId = Guid.NewGuid();
            var claimed = await dbContext.ReportExports
                .Where(item =>
                    item.Id == candidateId &&
                    item.ProcessingAttemptCount < maximumAttempts &&
                    (item.Status == ReportExportStatus.Queued ||
                     (item.Status == ReportExportStatus.Processing &&
                      (item.ProcessingLeaseUntil == null || item.ProcessingLeaseUntil < now))))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(item => item.Status, ReportExportStatus.Processing)
                    .SetProperty(item => item.ProcessingLeaseId, leaseId)
                    .SetProperty(item => item.ProcessingLeaseUntil, now.Add(leaseDuration))
                    .SetProperty(item => item.ProcessingAttemptCount, item => item.ProcessingAttemptCount + 1)
                    .SetProperty(item => item.UpdatedAt, now),
                    cancellationToken);
            if (claimed != 1)
            {
                continue;
            }

            var item = await dbContext.ReportExports
                .AsNoTracking()
                .Where(export => export.Id == candidateId && export.ProcessingLeaseId == leaseId)
                .Select(export => new
                {
                    export.Id,
                    export.TenantId,
                    export.ReportId,
                    export.RequestedByUserId,
                    export.ProcessingAttemptCount
                })
                .SingleAsync(cancellationToken);
            var email = await dbContext.Users
                .AsNoTracking()
                .Where(user => user.Id == item.RequestedByUserId && user.TenantId == item.TenantId)
                .Select(user => user.Email)
                .SingleOrDefaultAsync(cancellationToken);
            return new ClaimedReportExport(
                item.Id,
                item.TenantId,
                item.ReportId,
                item.RequestedByUserId,
                string.IsNullOrWhiteSpace(email) ? "report-export-worker@example.com" : email,
                leaseId,
                item.ProcessingAttemptCount);
        }

        return null;
    }

    public Task<ReportExportDto?> MarkReadyAsync(
        Guid exportId,
        Guid leaseId,
        string objectName,
        long contentLength,
        string? etag,
        CancellationToken cancellationToken = default) =>
        SetTerminalStateAsync(exportId, leaseId, ReportExportStatus.Ready, objectName, contentLength, etag, null, cancellationToken);

    public Task<ReportExportDto?> MarkFailedAsync(
        Guid exportId,
        Guid leaseId,
        string failureCode,
        CancellationToken cancellationToken = default) =>
        SetTerminalStateAsync(exportId, leaseId, ReportExportStatus.Failed, null, null, null, failureCode, cancellationToken);

    public async Task<ReportExportDto?> RecordProcessingFailureAsync(
        Guid exportId,
        Guid leaseId,
        int maximumAttempts,
        string failureCode,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.ReportExports.SingleOrDefaultAsync(
            candidate =>
                candidate.Id == exportId &&
                candidate.TenantId == tenantContext.TenantId &&
                candidate.Status == ReportExportStatus.Processing &&
                candidate.ProcessingLeaseId == leaseId,
            cancellationToken);
        if (entity is null)
        {
            return null;
        }

        var terminal = entity.ProcessingAttemptCount >= Math.Clamp(maximumAttempts, 1, 10);
        var now = DateTimeOffset.UtcNow;
        entity.Status = terminal ? ReportExportStatus.Failed : ReportExportStatus.Queued;
        entity.FailureCode = failureCode;
        entity.CompletedAt = terminal ? now : null;
        entity.ProcessingLeaseId = null;
        entity.ProcessingLeaseUntil = null;
        entity.UpdatedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    private async Task<ReportExportDto?> SetTerminalStateAsync(
        Guid exportId,
        Guid leaseId,
        ReportExportStatus status,
        string? objectName,
        long? contentLength,
        string? etag,
        string? failureCode,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.ReportExports.SingleOrDefaultAsync(
            candidate =>
                candidate.Id == exportId &&
                candidate.TenantId == tenantContext.TenantId &&
                candidate.Status == ReportExportStatus.Processing &&
                candidate.ProcessingLeaseId == leaseId,
            cancellationToken);
        if (entity is null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        entity.Status = status;
        if (!string.IsNullOrWhiteSpace(objectName))
        {
            entity.ObjectName = objectName;
        }
        entity.ContentLength = contentLength;
        entity.ETag = etag;
        entity.FailureCode = failureCode;
        entity.CompletedAt = now;
        entity.ProcessingLeaseId = null;
        entity.ProcessingLeaseUntil = null;
        entity.UpdatedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    private Task<ReportExportEntity?> FindCurrentAsync(Guid reportId, CancellationToken cancellationToken) =>
        dbContext.ReportExports
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate =>
                candidate.TenantId == tenantContext.TenantId &&
                candidate.ReportId == reportId &&
                candidate.Format == "pdf" &&
                candidate.RenderVersion == ReportExportConstants.RenderVersion,
                cancellationToken);

    private async Task<(ReportExportEntity Export, bool Queued)> RequeueFailedAsync(
        Guid exportId,
        Guid reportId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var pendingObjectName = ReportExportService.BuildPendingObjectName(reportId, exportId);
        if (dbContext.Database.IsRelational())
        {
            var updated = await dbContext.ReportExports
                .Where(candidate =>
                    candidate.Id == exportId &&
                    candidate.TenantId == tenantContext.TenantId &&
                    candidate.Status == ReportExportStatus.Failed)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(candidate => candidate.Status, ReportExportStatus.Queued)
                    .SetProperty(candidate => candidate.ObjectName, pendingObjectName)
                    .SetProperty(candidate => candidate.ContentLength, (long?)null)
                    .SetProperty(candidate => candidate.ETag, (string?)null)
                    .SetProperty(candidate => candidate.RequestedByUserId, actorUserId)
                    .SetProperty(candidate => candidate.RequestedAt, now)
                    .SetProperty(candidate => candidate.ProcessingAttemptCount, 0)
                    .SetProperty(candidate => candidate.ProcessingLeaseId, (Guid?)null)
                    .SetProperty(candidate => candidate.ProcessingLeaseUntil, (DateTimeOffset?)null)
                    .SetProperty(candidate => candidate.CompletedAt, (DateTimeOffset?)null)
                    .SetProperty(candidate => candidate.FailureCode, (string?)null)
                    .SetProperty(candidate => candidate.UpdatedAt, now)
                    .SetProperty(candidate => candidate.UpdatedByUserId, actorUserId),
                    cancellationToken);
            var persisted = await dbContext.ReportExports
                .AsNoTracking()
                .SingleAsync(candidate =>
                    candidate.Id == exportId && candidate.TenantId == tenantContext.TenantId,
                    cancellationToken);
            return (persisted, updated == 1);
        }

        var entity = await dbContext.ReportExports.SingleAsync(
            candidate => candidate.Id == exportId && candidate.TenantId == tenantContext.TenantId,
            cancellationToken);
        if (entity.Status != ReportExportStatus.Failed)
        {
            return (entity, false);
        }

        entity.Status = ReportExportStatus.Queued;
        entity.ObjectName = pendingObjectName;
        entity.ContentLength = null;
        entity.ETag = null;
        entity.RequestedByUserId = actorUserId;
        entity.RequestedAt = now;
        entity.ProcessingAttemptCount = 0;
        entity.ProcessingLeaseId = null;
        entity.ProcessingLeaseUntil = null;
        entity.CompletedAt = null;
        entity.FailureCode = null;
        entity.UpdatedAt = now;
        entity.UpdatedByUserId = actorUserId;
        await dbContext.SaveChangesAsync(cancellationToken);
        return (entity, true);
    }

    private static ReportExportDto ToDto(ReportExportEntity entity) => new(
        entity.Id,
        entity.TenantId,
        entity.ReportId,
        entity.Status,
        entity.Format,
        entity.FileName,
        entity.ContentType,
        entity.ContentLength,
        entity.RequestedAt,
        entity.CompletedAt,
        entity.FailureCode);

    private static string BuildFileName(string title, Guid reportId)
    {
        var stem = new string(title
            .Trim()
            .ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray());
        while (stem.Contains("--", StringComparison.Ordinal))
        {
            stem = stem.Replace("--", "-", StringComparison.Ordinal);
        }

        stem = stem.Trim('-');
        if (stem.Length > 160)
        {
            stem = stem[..160].TrimEnd('-');
        }

        return $"{(string.IsNullOrEmpty(stem) ? "report" : stem)}-{reportId:N}.pdf";
    }
}

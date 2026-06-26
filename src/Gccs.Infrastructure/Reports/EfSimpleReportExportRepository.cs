using Gccs.Application.Reports;
using Gccs.Application.Security;
using Gccs.Domain.Cmmc;
using Gccs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Reports;

public sealed class EfSimpleReportExportRepository(
    GccsDbContext dbContext,
    ICurrentTenantContext tenantContext) : ISimpleReportExportRepository
{
    public async Task<SimpleReportExportData> GetExportDataAsync(
        SimpleReportExportQuery query,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.TenantId;
        var tenantName = await dbContext.Tenants
            .AsNoTracking()
            .Where(tenant => tenant.Id == tenantId)
            .Select(tenant => tenant.Name)
            .SingleOrDefaultAsync(cancellationToken) ?? tenantId.ToString();

        return query.ReportType switch
        {
            SimpleReportExportType.ComplianceOverview => await ComplianceOverviewAsync(tenantId, tenantName, cancellationToken),
            SimpleReportExportType.PoamList => await PoamListAsync(tenantId, tenantName, cancellationToken),
            SimpleReportExportType.EvidenceInventory => await EvidenceInventoryAsync(tenantId, tenantName, cancellationToken),
            SimpleReportExportType.AuditLog => await AuditLogAsync(tenantId, tenantName, cancellationToken),
            _ => throw new SimpleReportExportValidationException("Report type is not supported.")
        };
    }

    private async Task<SimpleReportExportData> ComplianceOverviewAsync(
        Guid tenantId,
        string tenantName,
        CancellationToken cancellationToken)
    {
        var controlStatuses = await dbContext.ControlAssessments
            .AsNoTracking()
            .Where(control => control.Assessment != null && control.Assessment.TenantId == tenantId)
            .GroupBy(control => control.ImplementationStatus)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToArrayAsync(cancellationToken);

        var poamStatuses = await dbContext.PoamItems
            .AsNoTracking()
            .Where(item => item.TenantId == tenantId)
            .GroupBy(item => item.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToArrayAsync(cancellationToken);

        var evidenceStatuses = await dbContext.EvidenceItems
            .AsNoTracking()
            .Where(item => item.TenantId == tenantId)
            .GroupBy(item => item.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToArrayAsync(cancellationToken);

        var controlCountByStatus = controlStatuses.ToDictionary(item => item.Status.ToString(), item => item.Count);
        var poamCountByStatus = poamStatuses.ToDictionary(item => item.Status.ToString(), item => item.Count);
        var evidenceCountByStatus = evidenceStatuses.ToDictionary(item => item.Status.ToString(), item => item.Count);

        var overduePoams = await dbContext.PoamItems.AsNoTracking().CountAsync(
            item => item.TenantId == tenantId &&
                item.TargetCompletionAt < DateOnly.FromDateTime(DateTime.UtcNow) &&
                item.Status != PoamStatus.Closed &&
                item.Status != PoamStatus.AcceptedRisk,
            cancellationToken);

        var rows = new List<IReadOnlyList<string>>
        {
            new[] { "ControlsTotal", controlStatuses.Sum(item => item.Count).ToString() },
            new[] { "ControlsImplemented", controlCountByStatus.GetValueOrDefault(ControlImplementationStatus.Implemented.ToString()).ToString() },
            new[] { "ControlsInProgress", controlCountByStatus.GetValueOrDefault(ControlImplementationStatus.PartiallyImplemented.ToString()).ToString() },
            new[] { "ControlsNotStarted", controlCountByStatus.GetValueOrDefault(ControlImplementationStatus.NotStarted.ToString()).ToString() },
            new[] { "OpenPoams", poamStatuses.Where(item => item.Status is not (PoamStatus.Closed or PoamStatus.AcceptedRisk)).Sum(item => item.Count).ToString() },
            new[] { "OverduePoams", overduePoams.ToString() },
            new[] { "EvidenceItems", evidenceStatuses.Sum(item => item.Count).ToString() }
        };

        rows.AddRange(poamCountByStatus.OrderBy(item => item.Key).Select(item => new[] { $"PoamStatus:{item.Key}", item.Value.ToString() }));
        rows.AddRange(evidenceCountByStatus.OrderBy(item => item.Key).Select(item => new[] { $"EvidenceStatus:{item.Key}", item.Value.ToString() }));

        return new SimpleReportExportData(
            tenantId,
            tenantName,
            SimpleReportExportType.ComplianceOverview,
            ["Metric", "Value"],
            rows);
    }

    private async Task<SimpleReportExportData> PoamListAsync(
        Guid tenantId,
        string tenantName,
        CancellationToken cancellationToken)
    {
        var items = await dbContext.PoamItems
            .AsNoTracking()
            .Where(item => item.TenantId == tenantId)
            .OrderBy(item => item.TargetCompletionAt)
            .ThenBy(item => item.ControlId)
            .Select(item => new
            {
                item.Id,
                item.AssessmentId,
                item.ControlId,
                item.Weakness,
                item.PlannedRemediation,
                item.RiskLevel,
                item.Status,
                item.OwnerUserId,
                item.OwnerFunction,
                item.TargetCompletionAt,
                item.CompletedAt,
                item.CreatedAt,
                item.CreatedByUserId
            })
            .ToArrayAsync(cancellationToken);
        var rows = items
            .Select(item => (IReadOnlyList<string>)
            [
                item.Id.ToString(),
                item.AssessmentId.ToString(),
                item.ControlId,
                item.Weakness,
                item.PlannedRemediation,
                item.RiskLevel.ToString(),
                item.Status.ToString(),
                item.OwnerUserId?.ToString() ?? string.Empty,
                item.OwnerFunction,
                item.TargetCompletionAt.ToString("O"),
                item.CompletedAt?.ToString("O") ?? string.Empty,
                item.CreatedAt.ToString("O"),
                item.CreatedByUserId?.ToString() ?? string.Empty
            ])
            .ToArray();

        return new SimpleReportExportData(
            tenantId,
            tenantName,
            SimpleReportExportType.PoamList,
            ["Id", "AssessmentId", "ControlId", "Title", "RemediationPlan", "Severity", "Status", "OwnerUserId", "OwnerFunction", "DueDate", "CompletedAt", "CreatedAt", "CreatedBy"],
            rows);
    }

    private async Task<SimpleReportExportData> EvidenceInventoryAsync(
        Guid tenantId,
        string tenantName,
        CancellationToken cancellationToken)
    {
        var items = await dbContext.EvidenceItems
            .AsNoTracking()
            .Where(item => item.TenantId == tenantId)
            .OrderBy(item => item.Name)
            .ThenBy(item => item.Id)
            .Select(item => new
            {
                item.Id,
                item.Name,
                item.Type,
                item.Status,
                item.OwnerFunction,
                item.OriginalFileName,
                item.ContentType,
                item.SizeBytes,
                item.UploadValidationStatus,
                item.MalwareScanStatus,
                item.EffectiveAt,
                item.ExpiresAt,
                item.Classification,
                item.ClassificationSource,
                item.ApprovedByUserId,
                item.ApprovedAt,
                item.CreatedAt,
                item.CreatedByUserId
            })
            .ToArrayAsync(cancellationToken);
        var rows = items
            .Select(item => (IReadOnlyList<string>)
            [
                item.Id.ToString(),
                item.Name,
                item.Type.ToString(),
                item.Status.ToString(),
                item.OwnerFunction,
                item.OriginalFileName ?? string.Empty,
                item.ContentType ?? string.Empty,
                item.SizeBytes?.ToString() ?? string.Empty,
                item.UploadValidationStatus ?? string.Empty,
                item.MalwareScanStatus ?? string.Empty,
                item.EffectiveAt?.ToString("O") ?? string.Empty,
                item.ExpiresAt?.ToString("O") ?? string.Empty,
                item.Classification.ToString(),
                item.ClassificationSource.ToString(),
                item.ApprovedByUserId?.ToString() ?? string.Empty,
                item.ApprovedAt?.ToString("O") ?? string.Empty,
                item.CreatedAt.ToString("O"),
                item.CreatedByUserId?.ToString() ?? string.Empty
            ])
            .ToArray();

        return new SimpleReportExportData(
            tenantId,
            tenantName,
            SimpleReportExportType.EvidenceInventory,
            ["Id", "Title", "Type", "Status", "OwnerFunction", "FileName", "FileType", "FileSize", "UploadValidationStatus", "MalwareScanStatus", "EffectiveAt", "ExpiresAt", "Classification", "ClassificationSource", "ReviewedBy", "ReviewedAt", "CreatedAt", "CreatedBy"],
            rows);
    }

    private async Task<SimpleReportExportData> AuditLogAsync(
        Guid tenantId,
        string tenantName,
        CancellationToken cancellationToken)
    {
        var items = await dbContext.AuditLogEntries
            .AsNoTracking()
            .Where(item => item.TenantId == tenantId)
            .OrderByDescending(item => item.OccurredAt)
            .ThenByDescending(item => item.Id)
            .Take(500)
            .Select(item => new
            {
                item.Id,
                item.ActorUserId,
                item.Action,
                item.EntityType,
                item.EntityId,
                item.OccurredAt,
                item.CorrelationId,
                item.Summary
            })
            .ToArrayAsync(cancellationToken);
        var rows = items
            .Select(item => (IReadOnlyList<string>)
            [
                item.Id.ToString(),
                item.ActorUserId?.ToString() ?? string.Empty,
                item.Action.ToString(),
                item.EntityType,
                item.EntityId,
                item.OccurredAt.ToString("O"),
                item.CorrelationId,
                item.Summary
            ])
            .ToArray();

        return new SimpleReportExportData(
            tenantId,
            tenantName,
            SimpleReportExportType.AuditLog,
            ["Id", "ActorUserId", "Action", "EntityType", "EntityId", "OccurredAt", "CorrelationId", "Summary"],
            rows);
    }
}

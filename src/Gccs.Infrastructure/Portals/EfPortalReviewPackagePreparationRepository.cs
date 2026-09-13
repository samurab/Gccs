using System.Net;
using System.Text;
using System.Text.Json;
using Gccs.Application.Portals;
using Gccs.Application.Reports;
using Gccs.Application.Common;
using Gccs.Domain.Common;
using Gccs.Domain.Evidence;
using Gccs.Domain.Reports;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Portals;

public sealed class EfPortalReviewPackagePreparationRepository(
    GccsDbContext dbContext,
    IContractObligationMatrixRepository obligationMatrixRepository) : IPortalReviewPackagePreparationRepository
{
    private const int MaximumAuditRows = 10_000;

    public Task<PreparedPortalReviewPackageDto> CreateAsync(
        PreparePortalReviewPackageRequest request, Guid tenantId, Guid actorUserId,
        DateTimeOffset generatedAt, CancellationToken cancellationToken = default) =>
        request.SourceType switch
        {
            PortalReviewPreparationSource.ContractObligationMatrix =>
                CreateObligationMatrixAsync(request, tenantId, actorUserId, generatedAt, cancellationToken),
            PortalReviewPreparationSource.AuditLogExport =>
                CreateAuditLogAsync(request, tenantId, actorUserId, generatedAt, cancellationToken),
            _ => throw new PortalPackageValidationException("The package source type is unsupported.")
        };

    private async Task<PreparedPortalReviewPackageDto> CreateObligationMatrixAsync(
        PreparePortalReviewPackageRequest request, Guid tenantId, Guid actorUserId,
        DateTimeOffset generatedAt, CancellationToken cancellationToken)
    {
        var export = await obligationMatrixRepository.ExportCurrentTenantAsync(
            request.ContractId!.Value, cancellationToken)
            ?? throw new PortalReviewPackageSourceNotFoundException("The contract was not found in the current tenant.");
        var evidenceIds = export.Rows.SelectMany(row => row.EvidenceItemIds).Distinct().Order().ToArray();
        if (!await EvidenceIsSafeAsync(tenantId, evidenceIds, generatedAt, cancellationToken))
            throw new PortalPackageValidationException("The obligation matrix contains evidence that is not eligible for external review.");
        if (export.Rows.Any(row => new[]
            {
                row.ContractNumber, row.ContractTitle, row.ClauseNumber, row.ClauseTitle,
                row.ObligationTitle, row.RequiredAction, row.OwnerFunction
            }.Concat(row.EvidenceNames).Any(SensitiveContentMarkerDetector.ContainsExplicitRestrictedMarking)))
            throw new PortalPackageValidationException("The obligation matrix contains a prohibited data marking.");

        var report = CreateReport(request, tenantId, actorUserId, generatedAt,
            ReportType.ContractObligationMatrix, JsonSerializer.Serialize(export.Rows),
            $"<main><h1>{WebUtility.HtmlEncode(request.Title)}</h1><pre>{WebUtility.HtmlEncode(export.Csv)}</pre></main>");
        report.Contracts.Add(new ReportContractEntity { ReportId = report.Id, ContractId = export.ContractId });
        foreach (var evidenceId in evidenceIds)
            report.EvidenceItems.Add(new ReportEvidenceEntity { ReportId = report.Id, EvidenceItemId = evidenceId });
        dbContext.Reports.Add(report);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(report, request.SourceType, export.ContractId, evidenceIds);
    }

    private async Task<PreparedPortalReviewPackageDto> CreateAuditLogAsync(
        PreparePortalReviewPackageRequest request, Guid tenantId, Guid actorUserId,
        DateTimeOffset generatedAt, CancellationToken cancellationToken)
    {
        var total = await dbContext.AuditLogEntries.AsNoTracking()
            .CountAsync(row => row.TenantId == tenantId, cancellationToken);
        if (total > MaximumAuditRows)
            throw new PortalPackageValidationException(
                $"Audit-log preparation is limited to {MaximumAuditRows:N0} records; narrow or archive the source history first.");
        var rows = await dbContext.AuditLogEntries.AsNoTracking()
            .Where(row => row.TenantId == tenantId)
            .OrderBy(row => row.OccurredAt).ThenBy(row => row.Id)
            .Select(row => new { row.OccurredAt, Action = row.Action.ToString(), row.EntityType, row.EntityId, row.Summary })
            .ToArrayAsync(cancellationToken);
        if (rows.Any(row =>
                SensitiveContentMarkerDetector.ContainsExplicitRestrictedMarking(row.EntityType) ||
                SensitiveContentMarkerDetector.ContainsExplicitRestrictedMarking(row.Summary)))
            throw new PortalPackageValidationException("The audit-log package contains a prohibited data marking.");
        var csv = new StringBuilder("occurredAt,action,entityType,entityId,summary\n");
        foreach (var row in rows)
            csv.AppendLine(string.Join(',', Escape(row.OccurredAt.ToString("O")), Escape(row.Action),
                Escape(row.EntityType), Escape(row.EntityId), Escape(row.Summary)));
        var report = CreateReport(request, tenantId, actorUserId, generatedAt,
            ReportType.AuditTrail, JsonSerializer.Serialize(rows),
            $"<main><h1>{WebUtility.HtmlEncode(request.Title)}</h1><pre>{WebUtility.HtmlEncode(csv.ToString())}</pre></main>");
        dbContext.Reports.Add(report);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(report, request.SourceType, null, []);
    }

    private static ReportEntity CreateReport(
        PreparePortalReviewPackageRequest request, Guid tenantId, Guid actorUserId,
        DateTimeOffset generatedAt, ReportType reportType, string snapshotJson, string exportHtml) => new()
    {
        Id = Guid.NewGuid(), TenantId = tenantId, Type = reportType, Title = request.Title,
        Status = ReportStatus.Complete, GeneratedAt = generatedAt, GeneratedByUserId = actorUserId,
        SnapshotJson = snapshotJson, ExportHtml = exportHtml, Classification = request.Classification,
        ClassificationSource = ContentClassificationSource.UserSelected,
        ClassificationReviewedByUserId = actorUserId, ClassificationReviewedAt = generatedAt,
        ClassificationReason = "Prepared for a separate external-review approval decision.",
        CreatedAt = generatedAt, CreatedByUserId = actorUserId
    };

    private async Task<bool> EvidenceIsSafeAsync(
        Guid tenantId, Guid[] evidenceIds, DateTimeOffset asOf, CancellationToken cancellationToken)
    {
        if (evidenceIds.Length == 0) return true;
        var today = DateOnly.FromDateTime(asOf.UtcDateTime);
        var count = await dbContext.EvidenceItems.AsNoTracking().CountAsync(item =>
            item.TenantId == tenantId && evidenceIds.Contains(item.Id) &&
            item.Status == EvidenceStatus.Approved && !item.IsUseBlocked &&
            (item.ExpiresAt == null || item.ExpiresAt >= today) &&
            (item.Classification == ContentClassification.Unclassified || item.Classification == ContentClassification.Fci),
            cancellationToken);
        return count == evidenceIds.Length;
    }

    private static PreparedPortalReviewPackageDto ToDto(
        ReportEntity report, PortalReviewPreparationSource sourceType, Guid? contractId, Guid[] evidenceIds) =>
        new(report.Id, sourceType, report.Title, report.Classification, contractId, evidenceIds, report.GeneratedAt);

    private static string Escape(string? value)
    {
        value ??= string.Empty;
        return value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
            : value;
    }
}

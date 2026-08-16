using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Application.Storage;
using Gccs.Domain.Audit;

namespace Gccs.Application.Reports;

public sealed class ReportExportService(
    IReportExportRepository exports,
    IReportRepository reports,
    IReportPdfRenderer renderer,
    IObjectStorageService objectStorage,
    IAuditEventWriter auditEventWriter,
    IApplicationTransaction transaction)
{
    public Task<ReportExportDto?> RequestPdfAsync(
        Guid reportId,
        Guid actorUserId,
        CancellationToken cancellationToken = default) =>
        transaction.ExecuteAsync(async transactionCancellationToken =>
        {
            var result = await exports.RequestPdfAsync(reportId, actorUserId, transactionCancellationToken);
            if (result is null || !result.Queued)
            {
                return result?.Export;
            }

            await auditEventWriter.WriteAsync(
                result.Export.TenantId,
                actorUserId,
                result.Created ? AuditAction.Created : AuditAction.Updated,
                "ReportExport",
                result.Export.Id.ToString(),
                result.Created
                    ? "A PDF report export was queued."
                    : "A failed PDF report export was queued again.",
                new Dictionary<string, string>
                {
                    ["reportId"] = result.Export.ReportId.ToString(),
                    ["format"] = result.Export.Format
                },
                transactionCancellationToken);
            return result.Export;
        }, cancellationToken);

    public Task<ReportExportDto?> GetAsync(Guid exportId, CancellationToken cancellationToken = default) =>
        exports.GetAsync(exportId, cancellationToken);

    public async Task<ObjectStorageReadResult?> OpenContentAsync(
        Guid exportId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var locator = await exports.GetContentLocatorAsync(exportId, cancellationToken);
        if (locator is null || locator.Status != Domain.Reports.ReportExportStatus.Ready)
        {
            return null;
        }

        var stored = await objectStorage.OpenReadAsync(
            new ObjectStorageReadRequest(
                locator.TenantId,
                ObjectStorageContainer.Reports,
                locator.ObjectName),
            cancellationToken);
        if (stored is null)
        {
            return null;
        }

        try
        {
            await auditEventWriter.WriteAsync(
                locator.TenantId,
                actorUserId,
                AuditAction.Downloaded,
                "ReportExport",
                exportId.ToString(),
                "A PDF report export was downloaded or opened for printing.",
                new Dictionary<string, string>
                {
                    ["reportId"] = locator.ReportId.ToString(),
                    ["format"] = "pdf"
                },
                cancellationToken);
            return stored;
        }
        catch
        {
            await stored.DisposeAsync();
            throw;
        }
    }

    public async Task<ReportExportDto?> ProcessAsync(
        ClaimedReportExport claimed,
        CancellationToken cancellationToken = default)
    {
        var report = await reports.GetReportArtifactAsync(claimed.ReportId, cancellationToken);
        if (report is null)
        {
            return await MarkFailedAsync(claimed, "report_not_found", cancellationToken);
        }

        var rendered = renderer.Render(report);
        const int maximumPdfBytes = 10 * 1024 * 1024;
        if (rendered.Content.Length == 0 || rendered.Content.Length > maximumPdfBytes)
        {
            return await MarkFailedAsync(claimed, "pdf_size_limit_exceeded", cancellationToken);
        }

        var objectName = BuildObjectName(claimed.ReportId, claimed.ExportId, claimed.LeaseId);
        await using var content = new MemoryStream(rendered.Content, writable: false);
        var stored = await objectStorage.UploadAsync(
            new ObjectStorageWriteRequest(
                claimed.TenantId,
                ObjectStorageContainer.Reports,
                objectName,
                content,
                rendered.ContentType,
                new Dictionary<string, string>
                {
                    ["reportId"] = claimed.ReportId.ToString(),
                    ["renderVersion"] = ReportExportConstants.RenderVersion
                }),
            cancellationToken);

        try
        {
            var ready = await transaction.ExecuteAsync(async transactionCancellationToken =>
            {
                var persisted = await exports.MarkReadyAsync(
                    claimed.ExportId,
                    claimed.LeaseId,
                    objectName,
                    rendered.Content.LongLength,
                    stored.ETag,
                    transactionCancellationToken);
                if (persisted is null)
                {
                    return null;
                }

                await auditEventWriter.WriteAsync(
                    persisted.TenantId,
                    claimed.RequestedByUserId,
                    AuditAction.Exported,
                    "ReportExport",
                    persisted.Id.ToString(),
                    "A PDF report export was generated.",
                    new Dictionary<string, string>
                    {
                        ["reportId"] = persisted.ReportId.ToString(),
                        ["format"] = persisted.Format,
                        ["renderVersion"] = ReportExportConstants.RenderVersion
                    },
                    transactionCancellationToken);
                return persisted;
            }, cancellationToken);

            if (ready is not null)
            {
                return ready;
            }

            await DeleteAttemptObjectAsync(claimed.TenantId, objectName, cancellationToken);
            return null;
        }
        catch
        {
            await DeleteAttemptObjectAsync(claimed.TenantId, objectName, CancellationToken.None);
            throw;
        }
    }

    public Task<ReportExportDto?> MarkFailedAsync(
        ClaimedReportExport claimed,
        string failureCode,
        CancellationToken cancellationToken = default) =>
        transaction.ExecuteAsync(async transactionCancellationToken =>
        {
            var failed = await exports.MarkFailedAsync(
                claimed.ExportId,
                claimed.LeaseId,
                failureCode,
                transactionCancellationToken);
            if (failed is not null)
            {
                await WriteFailureAuditAsync(failed, claimed.RequestedByUserId, failureCode, transactionCancellationToken);
            }
            return failed;
        }, cancellationToken);

    public Task<ReportExportDto?> RecordProcessingFailureAsync(
        ClaimedReportExport claimed,
        int maximumAttempts,
        string failureCode,
        CancellationToken cancellationToken = default) =>
        transaction.ExecuteAsync(async transactionCancellationToken =>
        {
            var result = await exports.RecordProcessingFailureAsync(
                claimed.ExportId,
                claimed.LeaseId,
                maximumAttempts,
                failureCode,
                transactionCancellationToken);
            if (result?.Status == Domain.Reports.ReportExportStatus.Failed)
            {
                await WriteFailureAuditAsync(result, claimed.RequestedByUserId, failureCode, transactionCancellationToken);
            }
            return result;
        }, cancellationToken);

    public static string BuildPendingObjectName(Guid reportId, Guid exportId) =>
        $"reports/{reportId:D}/exports/{ReportExportConstants.RenderVersion}/pending-{exportId:D}.pdf";

    public static string BuildObjectName(Guid reportId, Guid exportId, Guid leaseId) =>
        $"reports/{reportId:D}/exports/{ReportExportConstants.RenderVersion}/{exportId:D}-{leaseId:D}.pdf";

    private Task<bool> DeleteAttemptObjectAsync(
        Guid tenantId,
        string objectName,
        CancellationToken cancellationToken) =>
        objectStorage.DeleteAsync(
            new ObjectStorageReadRequest(tenantId, ObjectStorageContainer.Reports, objectName),
            cancellationToken);

    private Task WriteFailureAuditAsync(
        ReportExportDto export,
        Guid actorUserId,
        string failureCode,
        CancellationToken cancellationToken) =>
        auditEventWriter.WriteAsync(
            export.TenantId,
            actorUserId,
            AuditAction.Updated,
            "ReportExport",
            export.Id.ToString(),
            "A PDF report export failed after bounded processing attempts.",
            new Dictionary<string, string>
            {
                ["reportId"] = export.ReportId.ToString(),
                ["format"] = export.Format,
                ["failureCode"] = failureCode
            },
            cancellationToken);
}

public static class ReportExportConstants
{
    public const string RenderVersion = "pdf-v3";
}

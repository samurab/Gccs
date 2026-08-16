using Gccs.Domain.Reports;

namespace Gccs.Application.Reports;

public sealed record ReportExportDto(
    Guid Id,
    Guid TenantId,
    Guid ReportId,
    ReportExportStatus Status,
    string Format,
    string FileName,
    string ContentType,
    long? ContentLength,
    DateTimeOffset RequestedAt,
    DateTimeOffset? CompletedAt,
    string? FailureCode);

public sealed record ReportExportRequestResult(
    ReportExportDto Export,
    bool Created,
    bool Queued);

public sealed record ReportExportContentLocator(
    Guid TenantId,
    Guid ReportId,
    ReportExportStatus Status,
    string ObjectName);

public sealed record ClaimedReportExport(
    Guid ExportId,
    Guid TenantId,
    Guid ReportId,
    Guid RequestedByUserId,
    string RequestedByEmail,
    Guid LeaseId,
    int AttemptNumber);

public sealed record RenderedReportPdf(byte[] Content, string ContentType);

public interface IReportPdfRenderer
{
    RenderedReportPdf Render(ReportArtifactDetailDto report);
}

public interface IReportExportRepository
{
    Task<ReportExportRequestResult?> RequestPdfAsync(
        Guid reportId,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<ReportExportDto?> GetAsync(Guid exportId, CancellationToken cancellationToken = default);

    Task<ReportExportContentLocator?> GetContentLocatorAsync(
        Guid exportId,
        CancellationToken cancellationToken = default);

    Task<ClaimedReportExport?> TryClaimNextAsync(
        DateTimeOffset now,
        TimeSpan leaseDuration,
        int maximumAttempts,
        CancellationToken cancellationToken = default);

    Task<ReportExportDto?> MarkReadyAsync(
        Guid exportId,
        Guid leaseId,
        string objectName,
        long contentLength,
        string? etag,
        CancellationToken cancellationToken = default);

    Task<ReportExportDto?> MarkFailedAsync(
        Guid exportId,
        Guid leaseId,
        string failureCode,
        CancellationToken cancellationToken = default);

    Task<ReportExportDto?> RecordProcessingFailureAsync(
        Guid exportId,
        Guid leaseId,
        int maximumAttempts,
        string failureCode,
        CancellationToken cancellationToken = default);
}

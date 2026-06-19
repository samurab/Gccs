using Gccs.Application.Audit;
using Gccs.Domain.Audit;

namespace Gccs.Application.Reports;

public sealed class EsrsReportPackageService(
    SubcontractingReportDataService reportDataService,
    IEsrsReportPackageRepository repository,
    IAuditEventWriter auditEventWriter)
{
    public const string NotSubmittedDisclaimer =
        "GCCS has not submitted this report to eSRS. This package is preparation-only for customer review.";

    public async Task<EsrsReportPackageDto> GenerateAsync(
        EsrsReportPackageGenerateRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission(request.HasReportPermission);
        var packageRows = await reportDataService.PreparePackageRowsAsync(
            new SubcontractingReportPackageRowsRequest(
                request.TenantId,
                request.ContractId,
                request.ReportType,
                request.PeriodStart,
                request.PeriodEnd,
                FinalPackage: true),
            cancellationToken);
        var package = await repository.CreateAsync(
            request,
            BuildSnapshot(request, packageRows),
            actorUserId,
            cancellationToken);
        await WriteAuditAsync(package, actorUserId, AuditAction.Created, "eSRS report package was generated.", cancellationToken);
        return package;
    }

    public async Task<EsrsReportPackageDto?> FindAsync(
        Guid packageId,
        bool hasReportPermission,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission(hasReportPermission);
        return await repository.FindAsync(packageId, cancellationToken);
    }

    public async Task<EsrsReportPackageDto?> ApproveAsync(
        Guid packageId,
        EsrsReportPackageReviewRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission(request.HasReportPermission);
        var approved = await repository.UpdateStatusAsync(
            packageId,
            EsrsReportPackageStatus.Approved,
            request.ReviewerName,
            request.ReviewNotes,
            actorUserId,
            cancellationToken);
        if (approved is not null)
        {
            await WriteAuditAsync(approved, actorUserId, AuditAction.Approved, "eSRS report package was approved.", cancellationToken);
        }

        return approved;
    }

    public async Task<EsrsReportPackageDto?> SupersedeAsync(
        Guid packageId,
        EsrsReportPackageReviewRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission(request.HasReportPermission);
        var superseded = await repository.UpdateStatusAsync(
            packageId,
            EsrsReportPackageStatus.Superseded,
            request.ReviewerName,
            request.ReviewNotes,
            actorUserId,
            cancellationToken);
        if (superseded is not null)
        {
            await WriteAuditAsync(superseded, actorUserId, AuditAction.Updated, "eSRS report package was superseded.", cancellationToken);
        }

        return superseded;
    }

    public async Task<EsrsReportPackageDto?> ArchiveAsync(
        Guid packageId,
        EsrsReportPackageReviewRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        EnsurePermission(request.HasReportPermission);
        var archived = await repository.UpdateStatusAsync(
            packageId,
            EsrsReportPackageStatus.Archived,
            request.ReviewerName,
            request.ReviewNotes,
            actorUserId,
            cancellationToken);
        if (archived is not null)
        {
            await WriteAuditAsync(archived, actorUserId, AuditAction.Archived, "eSRS report package was archived.", cancellationToken);
        }

        return archived;
    }

    private static EsrsReportPackageSnapshotDto BuildSnapshot(
        EsrsReportPackageGenerateRequest request,
        IReadOnlyList<SubcontractingReportPackageRowDto> packageRows)
    {
        var spendSummaries = packageRows
            .GroupBy(row => row.SocioeconomicCategory)
            .Select(group => new EsrsSpendSummaryDto(
                group.Key,
                group.Sum(row => row.Amount),
                group.Select(row => row.SubcontractorId).Distinct().Count()))
            .OrderBy(summary => summary.SocioeconomicCategory)
            .ToArray();
        var evidenceReferences = packageRows
            .SelectMany(row => row.SupportingEvidenceItemIds.Select(evidenceId => new EsrsPackageEvidenceReferenceDto(row.RowId, evidenceId)))
            .ToArray();
        var exceptions = packageRows.Count == 0
            ? new[] { "No reviewed or accepted subcontracting report data rows were available for this period." }
            : packageRows.Where(row => row.SupportingEvidenceItemIds.Count == 0)
                .Select(row => $"Row {row.RowId} has no supporting evidence.")
                .ToArray();

        return new EsrsReportPackageSnapshotDto(
            request.ContractId,
            request.ReportType,
            request.PeriodStart,
            request.PeriodEnd,
            packageRows.Count,
            packageRows.Sum(row => row.Amount),
            spendSummaries,
            evidenceReferences,
            exceptions);
    }

    private static void EnsurePermission(bool hasReportPermission)
    {
        if (!hasReportPermission)
        {
            throw new EsrsReportPackageException("Report permission is required.");
        }
    }

    private async Task WriteAuditAsync(
        EsrsReportPackageDto package,
        Guid actorUserId,
        AuditAction action,
        string summary,
        CancellationToken cancellationToken)
    {
        await auditEventWriter.WriteAsync(
            package.TenantId,
            actorUserId,
            action,
            "EsrsReportPackage",
            package.Id.ToString(),
            summary,
            new Dictionary<string, string>
            {
                ["contractId"] = package.ContractId.ToString(),
                ["reportType"] = package.ReportType.ToString(),
                ["status"] = package.Status.ToString(),
                ["version"] = package.Version.ToString(),
                ["totalSpend"] = package.Snapshot.TotalSpend.ToString("0.00")
            },
            cancellationToken);
    }
}

public interface IEsrsReportPackageRepository
{
    Task<EsrsReportPackageDto> CreateAsync(
        EsrsReportPackageGenerateRequest request,
        EsrsReportPackageSnapshotDto snapshot,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<EsrsReportPackageDto?> FindAsync(Guid packageId, CancellationToken cancellationToken = default);

    Task<EsrsReportPackageDto?> UpdateStatusAsync(
        Guid packageId,
        EsrsReportPackageStatus status,
        string reviewerName,
        string? reviewNotes,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}

public sealed record EsrsReportPackageGenerateRequest(
    Guid TenantId,
    Guid ContractId,
    EsrsReportType ReportType,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    bool HasReportPermission = true);

public sealed record EsrsReportPackageReviewRequest(
    string ReviewerName,
    string? ReviewNotes,
    bool HasReportPermission = true);

public sealed record EsrsReportPackageDto(
    Guid Id,
    Guid TenantId,
    Guid ContractId,
    EsrsReportType ReportType,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    EsrsReportPackageStatus Status,
    int Version,
    string NotSubmittedDisclaimer,
    EsrsReportPackageSnapshotDto Snapshot,
    string? ReviewerName,
    DateTimeOffset? ApprovedAt,
    string? ReviewNotes,
    DateTimeOffset GeneratedAt,
    DateTimeOffset? UpdatedAt);

public sealed record EsrsReportPackageSnapshotDto(
    Guid ContractId,
    EsrsReportType ReportType,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    int RowCount,
    decimal TotalSpend,
    IReadOnlyList<EsrsSpendSummaryDto> SpendSummaries,
    IReadOnlyList<EsrsPackageEvidenceReferenceDto> EvidenceReferences,
    IReadOnlyList<string> Exceptions);

public sealed record EsrsSpendSummaryDto(
    string SocioeconomicCategory,
    decimal TotalSpend,
    int SubcontractorCount);

public sealed record EsrsPackageEvidenceReferenceDto(
    Guid RowId,
    Guid EvidenceItemId);

public enum EsrsReportPackageStatus
{
    Draft,
    InReview,
    Approved,
    Superseded,
    Archived
}

public sealed class EsrsReportPackageException(string message) : InvalidOperationException(message);

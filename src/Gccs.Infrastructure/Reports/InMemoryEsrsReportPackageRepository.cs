using Gccs.Application.Reports;

namespace Gccs.Infrastructure.Reports;

public sealed class InMemoryEsrsReportPackageRepository(Guid tenantId) : IEsrsReportPackageRepository
{
    private readonly List<EsrsReportPackageDto> _packages = [];
    private readonly List<SprManualSubmissionReceiptDto> receipts = [];

    public Task<EsrsReportPackageDto> CreateAsync(
        EsrsReportPackageGenerateRequest request,
        EsrsReportPackageSnapshotDto snapshot,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var nextVersion = _packages
            .Where(package =>
                package.TenantId == tenantId &&
                package.ContractId == request.ContractId &&
                package.ReportType == request.ReportType &&
                package.PeriodStart == request.PeriodStart &&
                package.PeriodEnd == request.PeriodEnd)
            .Select(package => package.Version)
            .DefaultIfEmpty(0)
            .Max() + 1;
        var package = new EsrsReportPackageDto(
            Guid.NewGuid(),
            tenantId,
            request.ContractId,
            request.ReportType,
            request.PeriodStart,
            request.PeriodEnd,
            EsrsReportPackageStatus.Draft,
            nextVersion,
            EsrsReportPackageService.NotSubmittedDisclaimer,
            snapshot,
            null,
            null,
            null,
            DateTimeOffset.UtcNow,
            null,
            null);
        _packages.Add(package);
        return Task.FromResult(package);
    }

    public Task<EsrsReportPackageDto?> FindAsync(Guid packageId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_packages.SingleOrDefault(package => package.Id == packageId));

    public Task<IReadOnlyList<EsrsReportPackageDto>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<EsrsReportPackageDto>>(_packages.OrderByDescending(package => package.GeneratedAt).ToArray());

    public Task<EsrsReportPackageDto?> UpdateStatusAsync(
        Guid packageId,
        EsrsReportPackageStatus expectedStatus,
        EsrsReportPackageStatus status,
        string reviewerName,
        string? reviewNotes,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var existing = _packages.SingleOrDefault(package => package.Id == packageId);
        if (existing is null)
        {
            return Task.FromResult<EsrsReportPackageDto?>(null);
        }
        if (existing.Status != expectedStatus)
            throw new EsrsReportPackageConflictException("The SPR package status changed. Reload it and try again.");

        var now = DateTimeOffset.UtcNow;
        var updated = existing with
        {
            Status = status,
            ReviewerName = reviewerName.Trim(),
            ReviewNotes = string.IsNullOrWhiteSpace(reviewNotes) ? null : reviewNotes.Trim(),
            ReviewerUserId = actorUserId,
            ApprovedAt = status == EsrsReportPackageStatus.Approved ? now : existing.ApprovedAt,
            UpdatedAt = now
        };
        _packages.Remove(existing);
        _packages.Add(updated);
        return Task.FromResult<EsrsReportPackageDto?>(updated);
    }

    public Task<SprManualSubmissionReceiptDto> CreateManualSubmissionReceiptAsync(Guid packageId,
        SprManualSubmissionReceiptRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var package = _packages.Single(item => item.Id == packageId);
        if (request.SupersedesReceiptId is not null && receipts.All(item => item.Id != request.SupersedesReceiptId || item.PackageId != packageId))
            throw new EsrsReportPackageException("The receipt to supersede was not found for this package.");
        var receipt = new SprManualSubmissionReceiptDto(Guid.NewGuid(), package.TenantId, packageId, request.SubmittedAt,
            request.ConfirmationReference.Trim(), request.Outcome, string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            request.EvidenceItemId, request.SupersedesReceiptId, actorUserId, DateTimeOffset.UtcNow);
        receipts.Add(receipt);
        return Task.FromResult(receipt);
    }

    public Task<IReadOnlyList<SprManualSubmissionReceiptDto>> ListManualSubmissionReceiptsAsync(Guid packageId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SprManualSubmissionReceiptDto>>(receipts.Where(item => item.PackageId == packageId)
            .OrderByDescending(item => item.RecordedAt).ToArray());
}

using Gccs.Application.Reports;

namespace Gccs.Infrastructure.Reports;

public sealed class InMemoryEsrsReportPackageRepository : IEsrsReportPackageRepository
{
    private readonly List<EsrsReportPackageDto> _packages = [];

    public Task<EsrsReportPackageDto> CreateAsync(
        EsrsReportPackageGenerateRequest request,
        EsrsReportPackageSnapshotDto snapshot,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var nextVersion = _packages
            .Where(package =>
                package.TenantId == request.TenantId &&
                package.ContractId == request.ContractId &&
                package.ReportType == request.ReportType &&
                package.PeriodStart == request.PeriodStart &&
                package.PeriodEnd == request.PeriodEnd)
            .Select(package => package.Version)
            .DefaultIfEmpty(0)
            .Max() + 1;
        var package = new EsrsReportPackageDto(
            Guid.NewGuid(),
            request.TenantId,
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
            null);
        _packages.Add(package);
        return Task.FromResult(package);
    }

    public Task<EsrsReportPackageDto?> FindAsync(Guid packageId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_packages.SingleOrDefault(package => package.Id == packageId));

    public Task<EsrsReportPackageDto?> UpdateStatusAsync(
        Guid packageId,
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

        var now = DateTimeOffset.UtcNow;
        var updated = existing with
        {
            Status = status,
            ReviewerName = reviewerName.Trim(),
            ReviewNotes = string.IsNullOrWhiteSpace(reviewNotes) ? null : reviewNotes.Trim(),
            ApprovedAt = status == EsrsReportPackageStatus.Approved ? now : existing.ApprovedAt,
            UpdatedAt = now
        };
        _packages.Remove(existing);
        _packages.Add(updated);
        return Task.FromResult<EsrsReportPackageDto?>(updated);
    }
}

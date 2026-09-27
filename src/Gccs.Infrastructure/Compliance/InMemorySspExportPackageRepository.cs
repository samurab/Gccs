using System.Collections.Concurrent;
using Gccs.Application.Compliance;

namespace Gccs.Infrastructure.Compliance;

public sealed class InMemorySspExportPackageRepository : ISspExportPackageRepository
{
    private readonly ConcurrentDictionary<Guid, List<SspExportPackageDto>> packages = new();

    public Task<IReadOnlyList<SspExportPackageDto>> ListAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var records = packages.GetOrAdd(tenantId, _ => []);
        lock (records)
            return Task.FromResult<IReadOnlyList<SspExportPackageDto>>(records.OrderByDescending(item => item.GeneratedAt).ToArray());
    }

    public Task<SspExportPackageDto?> GetAsync(Guid tenantId, Guid packageId, CancellationToken cancellationToken = default)
    {
        var records = packages.GetOrAdd(tenantId, _ => []);
        lock (records)
            return Task.FromResult(records.SingleOrDefault(item => item.Id == packageId));
    }

    public Task<bool> PackageVersionExistsAsync(Guid tenantId, string packageVersion, CancellationToken cancellationToken = default)
    {
        var records = packages.GetOrAdd(tenantId, _ => []);
        lock (records)
            return Task.FromResult(records.Any(item => string.Equals(item.PackageVersion, packageVersion, StringComparison.OrdinalIgnoreCase)));
    }

    public Task<SspExportPackageDto> CreateAsync(SspExportPackageDto package, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var records = packages.GetOrAdd(package.TenantId, _ => []);
        lock (records)
        {
            if (records.Any(item => string.Equals(item.PackageVersion, package.PackageVersion, StringComparison.OrdinalIgnoreCase)))
                throw new SspExportPackageValidationException("Package version already exists for the current tenant.");
            records.Add(package);
        }
        return Task.FromResult(package);
    }

    public Task<SspExportPackageDto?> ApproveExternalShareAsync(Guid tenantId, Guid packageId, string reason, Guid actorUserId, string actorName, DateTimeOffset approvedAt, CancellationToken cancellationToken = default) =>
        UpdateAsync(tenantId, packageId, package =>
        {
            if (package.Status != SspExportPackageStatus.InternalReview)
                throw new SspExportPackageValidationException("Only an internal-review SSP package can receive external-share approval.");
            return package with
            {
                Status = SspExportPackageStatus.ExternalShareApproved,
                ExternalShareApprovedByUserId = actorUserId,
                ExternalShareApprovedAt = approvedAt,
                ExternalShareApprovalReason = reason,
                History = package.History.Append(new SspExportHistoryDto(Guid.NewGuid(), "ExternalShareApproved", actorUserId, actorName, approvedAt, reason)).ToArray()
            };
        });

    public Task<SspExportPackageDto?> RecordExternalShareAsync(Guid tenantId, Guid packageId, string recipient, string purpose, Guid actorUserId, string actorName, DateTimeOffset sharedAt, CancellationToken cancellationToken = default) =>
        UpdateAsync(tenantId, packageId, package =>
        {
            if (package.ExternalShareApprovedAt is null || package.Status != SspExportPackageStatus.ExternalShareApproved)
                throw new SspExportPackageValidationException("An external share may be recorded once and requires explicit approval for this package.");
            return package with
            {
                Status = SspExportPackageStatus.Shared,
                SharedByUserId = actorUserId,
                SharedAt = sharedAt,
                SharedRecipient = recipient,
                SharedPurpose = purpose,
                History = package.History.Append(new SspExportHistoryDto(Guid.NewGuid(), "Shared", actorUserId, actorName, sharedAt, purpose)).ToArray()
            };
        });

    private Task<SspExportPackageDto?> UpdateAsync(Guid tenantId, Guid packageId, Func<SspExportPackageDto, SspExportPackageDto> update)
    {
        var records = packages.GetOrAdd(tenantId, _ => []);
        lock (records)
        {
            var index = records.FindIndex(item => item.Id == packageId);
            if (index < 0) return Task.FromResult<SspExportPackageDto?>(null);
            records[index] = update(records[index]);
            return Task.FromResult<SspExportPackageDto?>(records[index]);
        }
    }
}

public sealed class UnavailableSspExportSourceRepository : ISspExportSourceRepository
{
    public Task<SspExportSourceSnapshot> ResolveAsync(Guid tenantId, Guid[] evidenceItemIds, Guid[] poamItemIds, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("SSP export source resolution requires persistent tenant data.");
}

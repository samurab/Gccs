using System.Collections.Concurrent;
using Gccs.Application.Compliance;

namespace Gccs.Infrastructure.Compliance;

public sealed class InMemoryFedRampReadinessExportPackageRepository : IFedRampReadinessExportPackageRepository
{
    private readonly ConcurrentDictionary<Guid, List<FedRampReadinessPackageDto>> _records = new();

    public Task<FedRampReadinessPackageDto?> GetAsync(Guid tenantId, Guid packageId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_records.GetOrAdd(tenantId, _ => []).SingleOrDefault(record => record.Id == packageId));

    public Task<FedRampReadinessPackageDto> CreateAsync(Guid tenantId, CreateFedRampReadinessPackageRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var included = request.Records
            .Where(record => record.TenantId == tenantId && !record.Restricted && !record.Prohibited && record.Status is FedRampPackageRecordStatus.Approved or FedRampPackageRecordStatus.Published)
            .ToArray();
        var language = request.GovernanceAuthorizedFedRampClaim
            ? "Governance has approved this package language for FedRAMP authorization status."
            : "Readiness only: this package does not claim FedRAMP authorization.";
        var package = new FedRampReadinessPackageDto(Guid.NewGuid(), tenantId, DateTimeOffset.UtcNow, request.PackageVersion, request.Scope, request.Environment, request.Reviewer, language, request.Gaps, request.AcceptedRisks, request.ReadinessSummary, included, FedRampReadinessPackageStatus.Draft, null, null);
        _records.GetOrAdd(tenantId, _ => []).Add(package);
        return Task.FromResult(package);
    }

    public Task<FedRampReadinessPackageDto?> ChangeStatusAsync(Guid tenantId, Guid packageId, FedRampReadinessPackageStatusRequest request, Guid actorUserId, CancellationToken cancellationToken = default) =>
        UpdateAsync(tenantId, packageId, package => package with { Status = request.Status, LastActor = request.ActorName });

    public Task<FedRampReadinessPackageDto?> ShareAsync(Guid tenantId, Guid packageId, FedRampReadinessPackageShareRequest request, Guid actorUserId, CancellationToken cancellationToken = default) =>
        UpdateAsync(tenantId, packageId, package => package with { Status = FedRampReadinessPackageStatus.Shared, SharedAt = DateTimeOffset.UtcNow, LastActor = request.Recipient });

    private Task<FedRampReadinessPackageDto?> UpdateAsync(Guid tenantId, Guid packageId, Func<FedRampReadinessPackageDto, FedRampReadinessPackageDto> update)
    {
        var records = _records.GetOrAdd(tenantId, _ => []);
        var index = records.FindIndex(record => record.Id == packageId);
        if (index < 0)
        {
            return Task.FromResult<FedRampReadinessPackageDto?>(null);
        }

        records[index] = update(records[index]);
        return Task.FromResult<FedRampReadinessPackageDto?>(records[index]);
    }
}

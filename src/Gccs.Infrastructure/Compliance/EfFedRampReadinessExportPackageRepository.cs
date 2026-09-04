using System.Text.Json;
using Gccs.Application.Compliance;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Compliance;

public sealed class EfFedRampReadinessExportPackageRepository(GccsDbContext dbContext) : IFedRampReadinessExportPackageRepository
{
    public const string ReadinessOnlyLanguage = "Readiness only: this package does not claim FedRAMP authorization.";

    public async Task<FedRampReadinessPackageDto?> GetAsync(Guid tenantId, Guid packageId, CancellationToken cancellationToken = default)
    {
        var package = await Query(tenantId).SingleOrDefaultAsync(candidate => candidate.Id == packageId, cancellationToken);
        return package is null ? null : ToDto(package);
    }

    public async Task<FedRampReadinessPackageDto> CreateAsync(Guid tenantId, CreateFedRampReadinessPackageRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var package = new FedRampReadinessPackageEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            GeneratedAt = now,
            PackageVersion = request.PackageVersion.Trim(),
            Scope = request.Scope.Trim(),
            Environment = request.Environment.Trim(),
            Reviewer = request.Reviewer.Trim(),
            AuthorizationLanguage = ReadinessOnlyLanguage,
            GapsJson = JsonSerializer.Serialize(request.Gaps),
            AcceptedRisksJson = JsonSerializer.Serialize(request.AcceptedRisks),
            ReadinessSummary = request.ReadinessSummary.Trim(),
            Status = FedRampReadinessPackageStatus.Draft,
            Version = 1,
            CreatedAt = now,
            CreatedByUserId = actorUserId,
            IncludedRecords = request.Records
                .Where(record => record.TenantId == tenantId && !record.Restricted && !record.Prohibited && record.Status is FedRampPackageRecordStatus.Approved or FedRampPackageRecordStatus.Published)
                .Select(record => new FedRampPackageRecordEntity
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    RecordType = record.RecordType.Trim(),
                    RecordId = record.RecordId.Trim(),
                    Title = record.Title.Trim(),
                    Status = record.Status,
                    Restricted = false,
                    Prohibited = false
                }).ToList()
        };

        dbContext.FedRampReadinessPackages.Add(package);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(package);
    }

    public Task<FedRampReadinessPackageDto?> ChangeStatusAsync(Guid tenantId, Guid packageId, FedRampReadinessPackageStatusRequest request, Guid actorUserId, CancellationToken cancellationToken = default) =>
        UpdateAsync(tenantId, packageId, request.Status, request.ActorName, request.Notes, actorUserId, sharedAt: null, cancellationToken);

    public Task<FedRampReadinessPackageDto?> ShareAsync(Guid tenantId, Guid packageId, FedRampReadinessPackageShareRequest request, Guid actorUserId, CancellationToken cancellationToken = default) =>
        UpdateAsync(tenantId, packageId, FedRampReadinessPackageStatus.Shared, request.Recipient, request.Purpose, actorUserId, DateTimeOffset.UtcNow, cancellationToken);

    private async Task<FedRampReadinessPackageDto?> UpdateAsync(Guid tenantId, Guid packageId, FedRampReadinessPackageStatus status, string actor, string? notes, Guid actorUserId, DateTimeOffset? sharedAt, CancellationToken cancellationToken)
    {
        var package = await Query(tenantId).SingleOrDefaultAsync(candidate => candidate.Id == packageId, cancellationToken);
        if (package is null)
        {
            return null;
        }

        var previousStatus = package.Status;
        package.Status = status;
        package.LastActor = actor.Trim();
        package.SharedAt = sharedAt ?? package.SharedAt;
        package.Version++;
        package.UpdatedAt = DateTimeOffset.UtcNow;
        package.UpdatedByUserId = actorUserId;
        var history = new FedRampReadinessPackageHistoryEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PackageId = packageId,
            PreviousStatus = previousStatus,
            NewStatus = status,
            Actor = actor.Trim(),
            Notes = notes?.Trim(),
            ChangedAt = DateTimeOffset.UtcNow,
            ChangedByUserId = actorUserId
        };
        dbContext.FedRampReadinessPackageHistory.Add(history);
        package.History.Add(history);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(package);
    }

    private IQueryable<FedRampReadinessPackageEntity> Query(Guid tenantId) =>
        dbContext.FedRampReadinessPackages
            .Where(package => package.TenantId == tenantId)
            .Include(package => package.IncludedRecords);

    private static FedRampReadinessPackageDto ToDto(FedRampReadinessPackageEntity package) =>
        new(
            package.Id,
            package.TenantId,
            package.GeneratedAt,
            package.PackageVersion,
            package.Scope,
            package.Environment,
            package.Reviewer,
            package.AuthorizationLanguage,
            JsonSerializer.Deserialize<string[]>(package.GapsJson) ?? [],
            JsonSerializer.Deserialize<string[]>(package.AcceptedRisksJson) ?? [],
            package.ReadinessSummary,
            package.IncludedRecords.OrderBy(record => record.RecordType).ThenBy(record => record.RecordId).Select(record => new FedRampPackageRecordDto(record.RecordType, record.RecordId, record.Title, record.Status, record.Restricted, record.Prohibited, record.TenantId)).ToArray(),
            package.Status,
            package.LastActor,
            package.SharedAt);
}

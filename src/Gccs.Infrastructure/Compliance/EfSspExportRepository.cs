using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Compliance;
using Gccs.Domain.Common;
using Gccs.Domain.Evidence;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Gccs.Infrastructure.Compliance;

public sealed class EfSspExportSourceRepository(GccsDbContext dbContext, TimeProvider timeProvider) : ISspExportSourceRepository
{
    public async Task<SspExportSourceSnapshot> ResolveAsync(
        Guid tenantId,
        Guid[] evidenceItemIds,
        Guid[] poamItemIds,
        CancellationToken cancellationToken = default)
    {
        await LockExportInputsAsync(tenantId, evidenceItemIds, poamItemIds, cancellationToken);
        var tenantName = await dbContext.Tenants
            .AsNoTracking()
            .Where(tenant => tenant.Id == tenantId)
            .Select(tenant => tenant.Name)
            .SingleOrDefaultAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(tenantName))
            throw new SspExportPackageValidationException("The current tenant is unavailable for SSP export.");

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var evidence = evidenceItemIds.Length == 0
            ? []
            : await dbContext.EvidenceItems
                .AsNoTracking()
                .Where(item => item.TenantId == tenantId &&
                    evidenceItemIds.Contains(item.Id) &&
                    item.Status == EvidenceStatus.Approved &&
                    item.ApprovedAt != null &&
                    item.ApprovedByUserId != null &&
                    !item.IsUseBlocked &&
                    (item.ExpiresAt == null || item.ExpiresAt >= today) &&
                    (item.Classification == ContentClassification.Unclassified || item.Classification == ContentClassification.Fci))
                .OrderBy(item => item.Name)
                .Select(item => new SspExportEvidenceReferenceDto(
                    item.Id,
                    item.Name,
                    item.Status,
                    item.Classification,
                    item.OwnerFunction,
                    item.ApprovedAt!.Value,
                    item.ApprovedByUserId!.Value,
                    item.EffectiveAt,
                    item.ExpiresAt))
                .ToArrayAsync(cancellationToken);

        var poamItems = poamItemIds.Length == 0
            ? []
            : await dbContext.PoamItems
                .AsNoTracking()
                .Where(item => item.TenantId == tenantId && poamItemIds.Contains(item.Id))
                .OrderBy(item => item.TargetCompletionAt)
                .ThenBy(item => item.ControlId)
                .Select(item => new SspExportPoamReferenceDto(
                    item.Id,
                    item.AssessmentId,
                    item.ControlId,
                    item.Weakness,
                    item.PlannedRemediation,
                    item.Status,
                    item.OwnerFunction,
                    item.TargetCompletionAt))
                .ToArrayAsync(cancellationToken);

        return new SspExportSourceSnapshot(tenantName, evidence, poamItems);
    }

    private async Task LockExportInputsAsync(Guid tenantId, Guid[] evidenceItemIds, Guid[] poamItemIds, CancellationToken cancellationToken)
    {
        if (!dbContext.Database.IsRelational() || dbContext.Database.CurrentTransaction is null) return;
        await dbContext.SspSections
            .FromSqlInterpolated($"SELECT * FROM gccs.ssp_sections WHERE tenant_id = {tenantId} FOR SHARE")
            .AsNoTracking().LoadAsync(cancellationToken);
        await dbContext.SspNarratives
            .FromSqlInterpolated($"SELECT * FROM gccs.ssp_narratives WHERE tenant_id = {tenantId} AND status = 'Approved' FOR SHARE")
            .AsNoTracking().LoadAsync(cancellationToken);
        if (evidenceItemIds.Length > 0)
            await dbContext.EvidenceItems
                .FromSqlInterpolated($"SELECT * FROM gccs.evidence_items WHERE tenant_id = {tenantId} AND id = ANY ({evidenceItemIds}) FOR SHARE")
                .AsNoTracking().LoadAsync(cancellationToken);
        if (poamItemIds.Length > 0)
            await dbContext.PoamItems
                .FromSqlInterpolated($"SELECT * FROM gccs.poam_items WHERE tenant_id = {tenantId} AND id = ANY ({poamItemIds}) FOR SHARE")
                .AsNoTracking().LoadAsync(cancellationToken);
    }
}

public sealed class EfSspExportPackageRepository(GccsDbContext dbContext) : ISspExportPackageRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<IReadOnlyList<SspExportPackageDto>> ListAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        (await Query(tenantId)
            .OrderByDescending(item => item.GeneratedAt)
            .ThenByDescending(item => item.Id)
            .ToArrayAsync(cancellationToken))
        .Select(ToDto)
        .ToArray();

    public async Task<SspExportPackageDto?> GetAsync(Guid tenantId, Guid packageId, CancellationToken cancellationToken = default)
    {
        var package = await Query(tenantId).SingleOrDefaultAsync(item => item.Id == packageId, cancellationToken);
        return package is null ? null : ToDto(package);
    }

    public Task<bool> PackageVersionExistsAsync(Guid tenantId, string packageVersion, CancellationToken cancellationToken = default) =>
        dbContext.SspExportPackages.AsNoTracking().AnyAsync(
            item => item.TenantId == tenantId && item.PackageVersion == packageVersion,
            cancellationToken);

    public async Task<SspExportPackageDto> CreateAsync(SspExportPackageDto package, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var entity = new SspExportPackageEntity
        {
            Id = package.Id,
            TenantId = package.TenantId,
            TenantName = package.TenantName,
            GeneratedAt = package.GeneratedAt,
            PackageVersion = package.PackageVersion,
            SystemBoundary = package.SystemBoundary,
            Reviewer = package.Reviewer,
            Format = package.Format.ToString(),
            Disclaimer = package.Disclaimer,
            HumanReadableReport = package.HumanReadableReport,
            MachineReadableMetadata = package.MachineReadableMetadata.GetRawText(),
            SectionsJson = JsonSerializer.Serialize(package.Sections, JsonOptions),
            EvidenceReferencesJson = JsonSerializer.Serialize(package.IncludedEvidence, JsonOptions),
            PoamReferencesJson = JsonSerializer.Serialize(package.PoamReferences, JsonOptions),
            Status = package.Status.ToString(),
            Version = 1,
            CreatedAt = package.GeneratedAt,
            CreatedByUserId = actorUserId,
            History = package.History.Select(history => ToEntity(package.TenantId, package.Id, history)).ToArray()
        };
        dbContext.SspExportPackages.Add(entity);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new SspExportPackageValidationException("Package version already exists for the current tenant.");
        }
        return ToDto(entity);
    }

    public async Task<SspExportPackageDto?> ApproveExternalShareAsync(
        Guid tenantId,
        Guid packageId,
        string reason,
        Guid actorUserId,
        string actorName,
        DateTimeOffset approvedAt,
        CancellationToken cancellationToken = default)
    {
        var entity = await QueryTracked(tenantId).SingleOrDefaultAsync(item => item.Id == packageId, cancellationToken);
        if (entity is null) return null;
        if (!Enum.TryParse<SspExportPackageStatus>(entity.Status, out var status) || status != SspExportPackageStatus.InternalReview)
            throw new SspExportPackageValidationException("Only an internal-review SSP package can receive external-share approval.");
        entity.Status = SspExportPackageStatus.ExternalShareApproved.ToString();
        entity.ExternalShareApprovedByUserId = actorUserId;
        entity.ExternalShareApprovedAt = approvedAt;
        entity.ExternalShareApprovalReason = reason;
        entity.Version++;
        entity.UpdatedAt = approvedAt;
        entity.UpdatedByUserId = actorUserId;
        entity.History.Add(new SspExportPackageHistoryEntity
        {
            Id = Guid.NewGuid(), TenantId = tenantId, PackageId = packageId, Action = "ExternalShareApproved",
            ActorUserId = actorUserId, ActorName = actorName, OccurredAt = approvedAt, Notes = reason
        });
        await SaveLifecycleAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<SspExportPackageDto?> ShareAsync(
        Guid tenantId,
        Guid packageId,
        string recipient,
        string purpose,
        Guid actorUserId,
        string actorName,
        DateTimeOffset sharedAt,
        CancellationToken cancellationToken = default)
    {
        var entity = await QueryTracked(tenantId).SingleOrDefaultAsync(item => item.Id == packageId, cancellationToken);
        if (entity is null) return null;
        if (entity.ExternalShareApprovedAt is null ||
            !Enum.TryParse<SspExportPackageStatus>(entity.Status, out var status) ||
            status != SspExportPackageStatus.ExternalShareApproved)
            throw new SspExportPackageValidationException("An external share may be recorded once and requires explicit approval for this package.");
        entity.Status = SspExportPackageStatus.Shared.ToString();
        entity.SharedByUserId = actorUserId;
        entity.SharedAt = sharedAt;
        entity.SharedRecipient = recipient;
        entity.SharedPurpose = purpose;
        entity.Version++;
        entity.UpdatedAt = sharedAt;
        entity.UpdatedByUserId = actorUserId;
        entity.History.Add(new SspExportPackageHistoryEntity
        {
            Id = Guid.NewGuid(), TenantId = tenantId, PackageId = packageId, Action = "Shared",
            ActorUserId = actorUserId, ActorName = actorName, OccurredAt = sharedAt, Notes = purpose
        });
        await SaveLifecycleAsync(cancellationToken);
        return ToDto(entity);
    }

    private async Task SaveLifecycleAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new SspExportPackageValidationException("The SSP package lifecycle changed. Reload the package and retry.");
        }
    }

    private IQueryable<SspExportPackageEntity> Query(Guid tenantId) =>
        dbContext.SspExportPackages.AsNoTracking().Where(item => item.TenantId == tenantId).Include(item => item.History);

    private IQueryable<SspExportPackageEntity> QueryTracked(Guid tenantId) =>
        dbContext.SspExportPackages.Where(item => item.TenantId == tenantId).Include(item => item.History);

    private static SspExportPackageHistoryEntity ToEntity(Guid tenantId, Guid packageId, SspExportHistoryDto history) => new()
    {
        Id = history.Id,
        TenantId = tenantId,
        PackageId = packageId,
        Action = history.Action,
        ActorUserId = history.ActorUserId,
        ActorName = history.ActorName,
        OccurredAt = history.OccurredAt,
        Notes = history.Notes
    };

    private static SspExportPackageDto ToDto(SspExportPackageEntity entity) => new(
        entity.Id,
        entity.TenantId,
        entity.TenantName,
        entity.GeneratedAt,
        entity.PackageVersion,
        entity.SystemBoundary,
        entity.Reviewer,
        Enum.Parse<SspExportFormat>(entity.Format),
        entity.Disclaimer,
        entity.HumanReadableReport,
        JsonSerializer.Deserialize<JsonElement>(entity.MachineReadableMetadata, JsonOptions),
        Deserialize<SspExportSectionDto>(entity.SectionsJson),
        Deserialize<SspExportEvidenceReferenceDto>(entity.EvidenceReferencesJson),
        Deserialize<SspExportPoamReferenceDto>(entity.PoamReferencesJson),
        Enum.Parse<SspExportPackageStatus>(entity.Status),
        entity.ExternalShareApprovedByUserId,
        entity.ExternalShareApprovedAt,
        entity.ExternalShareApprovalReason,
        entity.SharedByUserId,
        entity.SharedAt,
        entity.SharedRecipient,
        entity.SharedPurpose,
        entity.History.OrderBy(item => item.OccurredAt).ThenBy(item => item.Id).Select(item =>
            new SspExportHistoryDto(item.Id, item.Action, item.ActorUserId, item.ActorName, item.OccurredAt, item.Notes)).ToArray());

    private static T[] Deserialize<T>(string json) => JsonSerializer.Deserialize<T[]>(json, JsonOptions) ?? [];
}

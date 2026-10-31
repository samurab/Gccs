using Gccs.Application.Portals;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Gccs.Infrastructure.Portals;

public sealed class EfPortalPackageLifecycleRepository(GccsDbContext dbContext) : IPortalPackageLifecycleRepository
{
    public async Task<SharedPortalPackageDto> CreateAsync(
        SharedPortalPackageRequest request,
        PortalPackageApprovalMetadataDto approvalMetadata,
        Guid tenantId,
        Guid actorUserId,
        DateTimeOffset approvedAt,
        CancellationToken cancellationToken = default)
    {
        var version = await dbContext.SharedPortalPackages
            .Where(package => package.TenantId == tenantId && package.InvitationId == request.InvitationId)
            .Select(package => (int?)package.Version)
            .MaxAsync(cancellationToken) + 1 ?? 1;
        var now = DateTimeOffset.UtcNow;
        var entity = new SharedPortalPackageEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PackageId = request.PackageId,
            InvitationId = request.InvitationId,
            Version = version,
            State = SharedPortalPackageState.Active,
            ExpiresAt = request.ExpiresAt,
            ReviewDueAt = request.ReviewDueAt ?? request.ExpiresAt,
            ExternalReviewApprovedAt = approvedAt,
            ExternalReviewApprovedByUserId = actorUserId,
            ExternalReviewApprovalReason = NormalizeApprovalReason(request.ApprovalReason),
            ApprovedSourceVersion = approvalMetadata.SourceVersion,
            ApprovedSourceFingerprint = approvalMetadata.SourceFingerprint,
            ReminderAt = request.ExpiresAt.AddDays(-request.ExpirationReminderDays),
            CreatedAt = now,
            CreatedByUserId = actorUserId
        };
        dbContext.SharedPortalPackages.Add(entity);
        await SaveAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<IReadOnlyList<SharedPortalPackageDto>> ListAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default) =>
        (await dbContext.SharedPortalPackages.AsNoTracking()
            .Where(package => package.TenantId == tenantId)
            .OrderByDescending(package => package.CreatedAt)
            .ToArrayAsync(cancellationToken)).Select(ToDto).ToArray();

    public async Task<SharedPortalPackageDto?> FindAsync(
        Guid sharedPackageId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.SharedPortalPackages.AsNoTracking()
            .SingleOrDefaultAsync(package => package.TenantId == tenantId && package.Id == sharedPackageId, cancellationToken);
        return entity is null ? null : ToDto(entity);
    }

    public async Task<IReadOnlyList<SharedPortalPackageDto>> ListAccessibleAsync(
        Guid tenantId,
        Guid invitationId,
        DateTimeOffset asOf,
        CancellationToken cancellationToken = default) =>
        (await dbContext.SharedPortalPackages.AsNoTracking()
            .Where(package => package.TenantId == tenantId &&
                package.InvitationId == invitationId &&
                package.State == SharedPortalPackageState.Active &&
                package.ExpiresAt > asOf)
            .ToArrayAsync(cancellationToken)).Select(ToDto).ToArray();

    public async Task<SharedPortalPackageDto?> SetStateAsync(
        Guid sharedPackageId,
        Guid tenantId,
        SharedPortalPackageState expectedState,
        SharedPortalPackageState state,
        string? reason,
        Guid actorUserId,
        DateTimeOffset changedAt,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.SharedPortalPackages.Where(package =>
            package.TenantId == tenantId && package.Id == sharedPackageId && package.State == expectedState);
        var affected = dbContext.Database.IsRelational()
            ? await query.ExecuteUpdateAsync(setters => setters
                .SetProperty(package => package.State, state)
                .SetProperty(package => package.RevocationReason,
                    state == SharedPortalPackageState.Revoked ? reason : null)
                .SetProperty(package => package.RevokedAt,
                    state == SharedPortalPackageState.Revoked ? changedAt : null)
                .SetProperty(package => package.UpdatedAt, changedAt)
                .SetProperty(package => package.UpdatedByUserId, actorUserId), cancellationToken)
            : await UpdateTrackedAsync(query, entity =>
            {
                entity.State = state;
                if (state == SharedPortalPackageState.Revoked)
                {
                    entity.RevocationReason = reason;
                    entity.RevokedAt = changedAt;
                }
                entity.UpdatedAt = changedAt;
                entity.UpdatedByUserId = actorUserId;
            }, cancellationToken);
        if (affected == 0)
            return await ExistsAsync(sharedPackageId, tenantId, cancellationToken)
                ? throw Conflict()
                : null;

        var updated = await FindEntityAsync(sharedPackageId, tenantId, cancellationToken);
        AddActivity(updated!, ToActivity(state), actorUserId, reason, changedAt);
        await SaveAsync(cancellationToken);
        return ToDto(updated!);
    }

    public async Task<SharedPortalPackageDto?> SupersedeAsync(
        Guid sharedPackageId,
        Guid tenantId,
        SharedPortalPackageState expectedState,
        SharedPortalPackageDto replacement,
        Guid actorUserId,
        DateTimeOffset changedAt,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.SharedPortalPackages.Where(package =>
            package.TenantId == tenantId && package.Id == sharedPackageId && package.State == expectedState);
        var affected = dbContext.Database.IsRelational()
            ? await query.ExecuteUpdateAsync(setters => setters
                .SetProperty(package => package.State, SharedPortalPackageState.Superseded)
                .SetProperty(package => package.ReplacementSharedPackageId, replacement.Id)
                .SetProperty(package => package.ReplacementPackageId, replacement.PackageId)
                .SetProperty(package => package.UpdatedAt, changedAt)
                .SetProperty(package => package.UpdatedByUserId, actorUserId), cancellationToken)
            : await UpdateTrackedAsync(query, entity =>
            {
                entity.State = SharedPortalPackageState.Superseded;
                entity.ReplacementSharedPackageId = replacement.Id;
                entity.ReplacementPackageId = replacement.PackageId;
                entity.UpdatedAt = changedAt;
                entity.UpdatedByUserId = actorUserId;
            }, cancellationToken);
        if (affected == 0)
            return await ExistsAsync(sharedPackageId, tenantId, cancellationToken)
                ? throw Conflict()
                : null;

        var updated = await FindEntityAsync(sharedPackageId, tenantId, cancellationToken);
        AddActivity(updated!, PortalPackageActivityType.Supersede, actorUserId, replacement.Id.ToString(), changedAt);
        await SaveAsync(cancellationToken);
        return ToDto(updated!);
    }

    public async Task<SharedPortalPackageDto> ReissueAsync(
        SharedPortalPackageDto existing,
        ReissueSharedPortalPackageRequest request,
        PortalPackageApprovalMetadataDto approvalMetadata,
        Guid actorUserId,
        DateTimeOffset changedAt,
        CancellationToken cancellationToken = default)
    {
        var current = await dbContext.SharedPortalPackages.SingleOrDefaultAsync(package =>
            package.TenantId == existing.TenantId && package.Id == existing.Id && package.State == existing.State,
            cancellationToken) ?? throw Conflict();
        var nextVersion = await dbContext.SharedPortalPackages
            .Where(package => package.TenantId == current.TenantId && package.InvitationId == current.InvitationId)
            .Select(package => (int?)package.Version)
            .MaxAsync(cancellationToken) + 1 ?? 1;
        var replacement = new SharedPortalPackageEntity
        {
            Id = Guid.NewGuid(),
            TenantId = current.TenantId,
            PackageId = request.ReplacementPackageId,
            InvitationId = current.InvitationId,
            Version = nextVersion,
            State = SharedPortalPackageState.Active,
            ExpiresAt = request.ExpiresAt,
            ReviewDueAt = request.ReviewDueAt ?? request.ExpiresAt,
            ExternalReviewApprovedAt = changedAt,
            ExternalReviewApprovedByUserId = actorUserId,
            ExternalReviewApprovalReason = NormalizeApprovalReason(request.ApprovalReason),
            ApprovedSourceVersion = approvalMetadata.SourceVersion,
            ApprovedSourceFingerprint = approvalMetadata.SourceFingerprint,
            ReminderAt = request.ExpiresAt.AddDays(-request.ExpirationReminderDays),
            SupersedesSharedPackageId = current.Id,
            CreatedAt = changedAt,
            CreatedByUserId = actorUserId
        };
        if (current.State == SharedPortalPackageState.Active)
            current.State = SharedPortalPackageState.Superseded;
        current.ReplacementSharedPackageId = replacement.Id;
        current.ReplacementPackageId = replacement.PackageId;
        current.UpdatedAt = changedAt;
        current.UpdatedByUserId = actorUserId;
        dbContext.SharedPortalPackages.Add(replacement);
        AddActivity(current, PortalPackageActivityType.Reissue, actorUserId, replacement.Id.ToString(), changedAt);
        await SaveAsync(cancellationToken);
        return ToDto(replacement);
    }

    public Task<bool> CanAccessAsync(
        Guid sharedPackageId,
        Guid tenantId,
        Guid invitationId,
        Guid packageId,
        DateTimeOffset asOf,
        CancellationToken cancellationToken = default) =>
        dbContext.SharedPortalPackages.AsNoTracking().AnyAsync(package =>
            package.Id == sharedPackageId &&
            package.TenantId == tenantId &&
            package.InvitationId == invitationId &&
            package.PackageId == packageId &&
            package.State == SharedPortalPackageState.Active &&
            package.ExpiresAt > asOf,
            cancellationToken);

    public async Task RecordActivityAsync(
        Guid sharedPackageId,
        Guid tenantId,
        PortalPackageActivityType type,
        Guid actorUserId,
        string? detail,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken = default)
    {
        var package = await dbContext.SharedPortalPackages.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == sharedPackageId && candidate.TenantId == tenantId, cancellationToken)
            ?? throw new PortalPackageAccessDeniedException("The shared package is unavailable.");
        AddActivity(package, type, actorUserId, detail, occurredAt);
        await SaveAsync(cancellationToken);
    }

    public async Task<PortalPackageActivityReportDto> GenerateActivityReportAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var activities = await dbContext.PortalPackageActivities.AsNoTracking()
            .Where(activity => activity.TenantId == tenantId)
            .OrderByDescending(activity => activity.OccurredAt)
            .Select(activity => new PortalPackageActivityDto(
                activity.Id, activity.SharedPackageId, activity.TenantId, activity.ActivityType,
                activity.ActorUserId, activity.OccurredAt, activity.Detail))
            .ToArrayAsync(cancellationToken);
        return new(tenantId, activities);
    }

    public async Task<IReadOnlyList<SharedPortalPackageDto>> ListDueForMaintenanceAsync(
        DateTimeOffset asOf,
        CancellationToken cancellationToken = default) =>
        (await dbContext.SharedPortalPackages.AsNoTracking()
            .Where(package => package.State == SharedPortalPackageState.Active &&
                (package.ExpiresAt <= asOf || (package.ReminderSentAt == null && package.ReminderAt <= asOf)))
            .OrderBy(package => package.ExpiresAt)
            .Take(500)
            .ToArrayAsync(cancellationToken)).Select(ToDto).ToArray();

    public async Task<bool> MarkReminderSentAsync(
        Guid sharedPackageId,
        Guid tenantId,
        DateTimeOffset sentAt,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.SharedPortalPackages.Where(package =>
            package.TenantId == tenantId && package.Id == sharedPackageId &&
            package.State == SharedPortalPackageState.Active && package.ReminderSentAt == null);
        var affected = dbContext.Database.IsRelational()
            ? await query.ExecuteUpdateAsync(setters => setters
                .SetProperty(package => package.ReminderSentAt, sentAt)
                .SetProperty(package => package.UpdatedAt, sentAt), cancellationToken)
            : await UpdateTrackedAsync(query, entity =>
            {
                entity.ReminderSentAt = sentAt;
                entity.UpdatedAt = sentAt;
            }, cancellationToken);
        if (affected == 0) return false;
        var entity = await FindEntityAsync(sharedPackageId, tenantId, cancellationToken);
        AddActivity(entity!, PortalPackageActivityType.ExpirationReminder, Guid.Empty,
            $"expires:{entity!.ExpiresAt:O}", sentAt);
        await SaveAsync(cancellationToken);
        return true;
    }

    private async Task<SharedPortalPackageEntity?> FindEntityAsync(Guid id, Guid tenantId, CancellationToken cancellationToken) =>
        await dbContext.SharedPortalPackages.AsNoTracking()
            .SingleOrDefaultAsync(package => package.Id == id && package.TenantId == tenantId, cancellationToken);

    private Task<bool> ExistsAsync(Guid id, Guid tenantId, CancellationToken cancellationToken) =>
        dbContext.SharedPortalPackages.AsNoTracking()
            .AnyAsync(package => package.Id == id && package.TenantId == tenantId, cancellationToken);

    private async Task<int> UpdateTrackedAsync(
        IQueryable<SharedPortalPackageEntity> query,
        Action<SharedPortalPackageEntity> update,
        CancellationToken cancellationToken)
    {
        var entity = await query.SingleOrDefaultAsync(cancellationToken);
        if (entity is null) return 0;
        update(entity);
        await SaveAsync(cancellationToken);
        return 1;
    }

    private void AddActivity(
        SharedPortalPackageEntity package,
        PortalPackageActivityType type,
        Guid actorUserId,
        string? detail,
        DateTimeOffset occurredAt) =>
        dbContext.PortalPackageActivities.Add(new PortalPackageActivityEntity
        {
            Id = Guid.NewGuid(),
            TenantId = package.TenantId,
            SharedPackageId = package.Id,
            ActivityType = type,
            ActorUserId = actorUserId,
            OccurredAt = occurredAt,
            Detail = detail
        });

    private static SharedPortalPackageDto ToDto(SharedPortalPackageEntity entity) => new(
        entity.Id, entity.TenantId, entity.PackageId, entity.InvitationId, entity.Version, entity.State,
        entity.ExpiresAt, entity.ReviewDueAt, entity.ExternalReviewApprovedAt,
        entity.ExternalReviewApprovedByUserId, entity.ExternalReviewApprovalReason,
        entity.ApprovedSourceVersion, entity.ApprovedSourceFingerprint,
        entity.ReminderAt, entity.ReminderSentAt, entity.SupersedesSharedPackageId,
        entity.ReplacementSharedPackageId, entity.ReplacementPackageId, entity.RevocationReason,
        entity.RevokedAt, entity.CreatedAt, entity.UpdatedAt);

    private static string NormalizeApprovalReason(string? reason) =>
        string.IsNullOrWhiteSpace(reason)
            ? "Explicitly approved for external portal review when shared."
            : reason.Trim();

    private static PortalPackageActivityType ToActivity(SharedPortalPackageState state) =>
        state switch
        {
            SharedPortalPackageState.Expired => PortalPackageActivityType.Expiration,
            SharedPortalPackageState.Revoked => PortalPackageActivityType.Revocation,
            SharedPortalPackageState.Superseded => PortalPackageActivityType.Supersede,
            SharedPortalPackageState.Archived => PortalPackageActivityType.Archive,
            _ => throw new PortalPackageLifecycleException("The requested state is not a lifecycle activity.")
        };

    private static PortalPackageLifecycleConflictException Conflict() =>
        new("The shared package changed. Reload it and try again.");

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw Conflict();
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new PortalPackageLifecycleConflictException("A shared package version conflict occurred. Reload and try again.");
        }
    }
}

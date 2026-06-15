using Gccs.Application.Notifications;
using Gccs.Domain.Identity;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Notifications;

public sealed class EfNotificationPreferenceRepository(GccsDbContext dbContext) : INotificationPreferenceRepository
{
    public async Task<NotificationPreferenceDto> GetOrCreateAsync(
        Guid tenantId,
        Guid userId,
        string roleName,
        CancellationToken cancellationToken = default)
    {
        var entity = await FindAsync(tenantId, userId, cancellationToken);
        if (entity is not null)
        {
            return ToDto(entity);
        }

        var defaults = DefaultsFor(roleName);
        entity = new NotificationPreferenceEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            RoleName = NormalizeRoleName(roleName),
            AssignmentNotificationsEnabled = defaults.AssignmentNotificationsEnabled,
            DueSoonNotificationsEnabled = defaults.DueSoonNotificationsEnabled,
            OverdueNotificationsEnabled = defaults.OverdueNotificationsEnabled,
            EvidenceRequestNotificationsEnabled = defaults.EvidenceRequestNotificationsEnabled,
            CertificationRenewalNotificationsEnabled = defaults.CertificationRenewalNotificationsEnabled,
            CmmcAffirmationNotificationsEnabled = defaults.CmmcAffirmationNotificationsEnabled,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = userId
        };
        dbContext.NotificationPreferences.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<NotificationPreferenceDto> UpdateAsync(
        Guid tenantId,
        Guid userId,
        string roleName,
        NotificationPreferenceUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        var entity = await FindAsync(tenantId, userId, cancellationToken) ??
            (await CreateTrackedAsync(tenantId, userId, roleName, cancellationToken));
        entity.AssignmentNotificationsEnabled = request.AssignmentNotificationsEnabled;
        entity.DueSoonNotificationsEnabled = request.DueSoonNotificationsEnabled;
        entity.OverdueNotificationsEnabled = request.OverdueNotificationsEnabled;
        entity.EvidenceRequestNotificationsEnabled = request.EvidenceRequestNotificationsEnabled;
        entity.CertificationRenewalNotificationsEnabled = request.CertificationRenewalNotificationsEnabled;
        entity.CmmcAffirmationNotificationsEnabled = request.CmmcAffirmationNotificationsEnabled;
        entity.RoleName = NormalizeRoleName(roleName);
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedByUserId = userId;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    private async Task<NotificationPreferenceEntity> CreateTrackedAsync(
        Guid tenantId,
        Guid userId,
        string roleName,
        CancellationToken cancellationToken)
    {
        var defaults = DefaultsFor(roleName);
        var entity = new NotificationPreferenceEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            RoleName = NormalizeRoleName(roleName),
            AssignmentNotificationsEnabled = defaults.AssignmentNotificationsEnabled,
            DueSoonNotificationsEnabled = defaults.DueSoonNotificationsEnabled,
            OverdueNotificationsEnabled = defaults.OverdueNotificationsEnabled,
            EvidenceRequestNotificationsEnabled = defaults.EvidenceRequestNotificationsEnabled,
            CertificationRenewalNotificationsEnabled = defaults.CertificationRenewalNotificationsEnabled,
            CmmcAffirmationNotificationsEnabled = defaults.CmmcAffirmationNotificationsEnabled,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = userId
        };
        dbContext.NotificationPreferences.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity;
    }

    private Task<NotificationPreferenceEntity?> FindAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken) =>
        dbContext.NotificationPreferences.SingleOrDefaultAsync(
            preference => preference.TenantId == tenantId && preference.UserId == userId,
            cancellationToken);

    private static NotificationPreferenceUpdateRequest DefaultsFor(string roleName)
    {
        var normalized = NormalizeRoleName(roleName);
        return normalized switch
        {
            RoleCatalog.Auditor => new(false, true, true, false, false, false),
            RoleCatalog.Contributor => new(true, true, true, true, false, false),
            _ => new(true, true, true, true, true, true)
        };
    }

    private static string NormalizeRoleName(string roleName) =>
        RoleCatalog.TryNormalizeRoleName(roleName, out var normalized) ? normalized : RoleCatalog.Contributor;

    private static NotificationPreferenceDto ToDto(NotificationPreferenceEntity entity) =>
        new(
            entity.Id,
            entity.TenantId,
            entity.UserId,
            entity.RoleName,
            entity.AssignmentNotificationsEnabled,
            entity.DueSoonNotificationsEnabled,
            entity.OverdueNotificationsEnabled,
            entity.EvidenceRequestNotificationsEnabled,
            entity.CertificationRenewalNotificationsEnabled,
            entity.CmmcAffirmationNotificationsEnabled,
            entity.CreatedAt,
            entity.UpdatedAt);
}

using System.Text.Json;
using Gccs.Application.Audit;
using Gccs.Application.Identity;
using Gccs.Application.Tenancy;
using Gccs.Domain.Audit;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Tenancy;

public sealed class EfPilotTenantProvisioningRepository(
    GccsDbContext dbContext,
    IAuditRequestMetadata requestMetadata) : IPilotTenantProvisioningRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<PilotTenantProvisioningResultDto> ProvisionAsync(
        PilotTenantProvisioningRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var ownerEmail = request.OwnerEmail.Trim().ToLowerInvariant();
        var ownerDisplayName = request.OwnerDisplayName.Trim();
        var roleName = request.OwnerRoleName.Trim();
        var tenantName = request.DisplayName.Trim();
        var setupReason = request.SetupReason?.Trim() ??
            "Pilot tenant provisioned with No-CUI compliance management posture.";
        var now = DateTimeOffset.UtcNow;
        var tenantId = Guid.NewGuid();
        var membershipId = Guid.NewGuid();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var tenant = new TenantEntity
        {
            Id = tenantId,
            Name = tenantName,
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            TrialEndsAt = request.TrialEndsAt,
            CreatedAt = now,
            CreatedByUserId = actorUserId
        };
        dbContext.Tenants.Add(tenant);

        var user = await dbContext.Users.SingleOrDefaultAsync(
            candidate => candidate.Id == request.OwnerUserId,
            cancellationToken);
        if (user is null)
        {
            user = new UserEntity
            {
                Id = request.OwnerUserId,
                TenantId = tenantId,
                Email = ownerEmail,
                DisplayName = ownerDisplayName,
                Status = UserStatus.Active,
                MfaEnabled = false,
                LastSignedInAt = null,
                CreatedAt = now,
                CreatedByUserId = actorUserId
            };
            dbContext.Users.Add(user);
        }
        else
        {
            user.Email = ownerEmail;
            user.DisplayName = ownerDisplayName;
            user.Status = UserStatus.Active;
            user.UpdatedAt = now;
            user.UpdatedByUserId = actorUserId;
        }

        var role = new RoleEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = roleName,
            CreatedAt = now,
            CreatedByUserId = actorUserId
        };
        dbContext.Roles.Add(role);

        foreach (var permission in RoleCatalog.GetPermissions(roleName))
        {
            dbContext.Set<RolePermissionEntity>().Add(new RolePermissionEntity
            {
                RoleId = role.Id,
                Permission = permission
            });
        }

        dbContext.TenantMemberships.Add(new TenantMembershipEntity
        {
            Id = membershipId,
            TenantId = tenantId,
            UserId = request.OwnerUserId,
            Status = MembershipStatus.Active,
            RoleName = roleName,
            LastAccessedAt = null,
            CreatedAt = now,
            CreatedByUserId = actorUserId
        });

        dbContext.Set<UserRoleEntity>().Add(new UserRoleEntity
        {
            UserId = request.OwnerUserId,
            RoleId = role.Id
        });

        dbContext.TenantDataHandlingModeHistory.Add(new TenantDataHandlingModeHistoryEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PreviousMode = null,
            NewMode = TenantDataPosture.NoCui,
            ActorUserId = actorUserId,
            ChangedAt = now,
            Reason = setupReason,
            ApprovalRecordReference = "pilot-provisioning"
        });

        AddAudit(
            tenantId,
            actorUserId,
            AuditAction.Created,
            "Tenant",
            tenantId.ToString(),
            $"Pilot tenant '{tenantName}' was provisioned.",
            new Dictionary<string, string>
            {
                ["status"] = TenantStatus.Active.ToString(),
                ["dataHandlingMode"] = TenantDataPosture.NoCui.ToString(),
                ["ownerUserId"] = request.OwnerUserId.ToString(),
                ["ownerRoleName"] = roleName,
                ["setupReason"] = setupReason
            },
            now);

        AddAudit(
            tenantId,
            actorUserId,
            AuditAction.Created,
            "TenantMembership",
            membershipId.ToString(),
            $"Pilot owner '{ownerEmail}' was added to tenant '{tenantName}'.",
            new Dictionary<string, string>
            {
                ["userId"] = request.OwnerUserId.ToString(),
                ["membershipStatus"] = MembershipStatus.Active.ToString(),
                ["roleName"] = roleName
            },
            now);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new PilotTenantProvisioningResultDto(
            new TenantDto(
                tenant.Id,
                tenant.Name,
                tenant.Status,
                tenant.DataPosture,
                tenant.DataPosture,
                tenant.TrialEndsAt,
                tenant.CreatedAt,
                tenant.UpdatedAt),
            new TenantMemberDto(
                membershipId,
                tenant.Id,
                user.Id,
                user.Email,
                user.DisplayName,
                user.Status,
                MembershipStatus.Active,
                roleName,
                user.MfaEnabled,
                user.LastSignedInAt,
                null,
                now,
                null),
            TenantDataPosture.NoCui,
            setupReason);
    }

    private void AddAudit(
        Guid tenantId,
        Guid actorUserId,
        AuditAction action,
        string entityType,
        string entityId,
        string summary,
        IReadOnlyDictionary<string, string> metadata,
        DateTimeOffset occurredAt)
    {
        var eventMetadata = new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(requestMetadata.CorrelationId))
        {
            eventMetadata["correlationId"] = requestMetadata.CorrelationId;
        }

        dbContext.AuditLogEntries.Add(new AuditLogEntryEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ActorUserId = actorUserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OccurredAt = occurredAt,
            IpAddress = requestMetadata.IpAddress,
            UserAgent = requestMetadata.UserAgent,
            CorrelationId = requestMetadata.CorrelationId,
            Summary = summary,
            MetadataJson = JsonSerializer.Serialize(eventMetadata, JsonOptions)
        });
    }
}

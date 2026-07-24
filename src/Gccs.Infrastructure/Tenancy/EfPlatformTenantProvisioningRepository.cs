using System.Security.Cryptography;
using System.Text.Json;
using Gccs.Application.Audit;
using Gccs.Application.Tenancy;
using Gccs.Domain.Audit;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Tenancy;

public sealed class EfPlatformTenantProvisioningRepository(
    GccsDbContext dbContext,
    IAuditRequestMetadata requestMetadata) : IPlatformTenantProvisioningRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const int InvitationLifetimeDays = 7;

    public async Task<ExistingPlatformTenantProvisioningDto?> FindByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var onboarding = await Onboardings()
            .SingleOrDefaultAsync(candidate => candidate.IdempotencyKey == idempotencyKey, cancellationToken);

        return onboarding is null
            ? null
            : new ExistingPlatformTenantProvisioningDto(
                onboarding.RequestFingerprint,
                ToResult(onboarding, false));
    }

    public async Task<PlatformTenantProvisioningResultDto> ProvisionAsync(
        PlatformTenantProvisioningRequest request,
        string idempotencyKey,
        string requestFingerprint,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var onboardingType = Enum.Parse<TenantOnboardingType>(request.OnboardingType, true);
        var now = DateTimeOffset.UtcNow;
        var tenantId = Guid.NewGuid();
        var onboardingId = Guid.NewGuid();
        var invitationId = Guid.NewGuid();
        var invitationExpiresAt = now.AddDays(InvitationLifetimeDays);

        var tenant = new TenantEntity
        {
            Id = tenantId,
            Name = request.DisplayName,
            Status = TenantStatus.PendingActivation,
            DataPosture = TenantDataPosture.NoCui,
            TrialEndsAt = request.TrialEndsAt,
            CreatedAt = now,
            CreatedByUserId = actorUserId
        };

        var invitation = new TenantInvitationEntity
        {
            Id = invitationId,
            TenantId = tenantId,
            Email = request.OwnerEmail,
            RoleName = RoleCatalog.Owner,
            InvitationTokenHash = null,
            Status = TenantInvitationStatus.Pending,
            ExpiresAt = invitationExpiresAt,
            NotificationSentAt = null,
            NotificationPlaceholder = "Owner invitation is queued for delivery.",
            DeliveryStatus = InvitationDeliveryStatus.Queued,
            DeliveryAttemptCount = 0,
            NextDeliveryAttemptAt = now,
            CreatedAt = now,
            CreatedByUserId = actorUserId
        };

        var onboarding = new PlatformTenantOnboardingEntity
        {
            Id = onboardingId,
            TenantId = tenantId,
            InvitationId = invitationId,
            IdempotencyKey = idempotencyKey,
            RequestFingerprint = requestFingerprint,
            OnboardingType = onboardingType,
            Status = TenantOnboardingStatus.PendingOwnerAcceptance,
            CustomerReference = request.CustomerReference,
            OwnerEmail = request.OwnerEmail,
            OwnerDisplayName = request.OwnerDisplayName,
            PlanCode = request.PlanCode,
            SubscriptionReference = request.SubscriptionReference,
            CommercialApprovalConfirmed = request.CommercialApprovalConfirmed,
            SetupReason = request.SetupReason,
            CreatedAt = now,
            CreatedByUserId = actorUserId,
            Tenant = tenant,
            Invitation = invitation
        };

        dbContext.Tenants.Add(tenant);
        dbContext.TenantInvitations.Add(invitation);
        dbContext.PlatformTenantOnboardings.Add(onboarding);
        AddOwnerRole(tenantId, actorUserId, now);
        AddModeHistory(tenantId, actorUserId, request.SetupReason, now);
        AddAuditEntries(onboarding, tenant, invitation, actorUserId, now);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return ToResult(onboarding, false);
        }
        catch (DbUpdateException)
        {
            dbContext.ChangeTracker.Clear();

            var idempotentResult = await FindByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
            if (idempotentResult is not null)
            {
                if (!string.Equals(idempotentResult.RequestFingerprint, requestFingerprint, StringComparison.Ordinal))
                {
                    throw new TenantProvisioningConflictException(
                        "The idempotency key has already been used for a different tenant provisioning request.");
                }

                return idempotentResult.Result with { IsReplay = true };
            }

            var duplicateReference = await dbContext.PlatformTenantOnboardings
                .AsNoTracking()
                .AnyAsync(candidate =>
                    candidate.CustomerReference == request.CustomerReference ||
                    (request.SubscriptionReference != null && candidate.SubscriptionReference == request.SubscriptionReference),
                    cancellationToken);
            if (duplicateReference)
            {
                throw new TenantProvisioningConflictException(
                    "The customer or subscription reference has already been provisioned.");
            }

            throw;
        }
    }

    public async Task<PlatformTenantOnboardingPageDto> ListAsync(
        int page,
        int pageSize,
        TenantOnboardingStatus? status,
        CancellationToken cancellationToken = default)
    {
        var query = Onboardings();
        if (status.HasValue)
        {
            query = query.Where(candidate => candidate.Status == status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(candidate => candidate.CreatedAt)
            .ThenBy(candidate => candidate.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PlatformTenantOnboardingPageDto(
            items.Select(item => ToResult(item, false)).ToArray(),
            page,
            pageSize,
            totalCount,
            page * pageSize < totalCount,
            page > 1);
    }

    public async Task<PlatformTenantProvisioningResultDto?> CancelAsync(
        Guid onboardingId,
        string reason,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var onboarding = await dbContext.PlatformTenantOnboardings
            .Include(candidate => candidate.Tenant)
            .Include(candidate => candidate.Invitation)
            .SingleOrDefaultAsync(candidate => candidate.Id == onboardingId, cancellationToken);
        if (onboarding is null)
        {
            return null;
        }

        if (onboarding.Status is TenantOnboardingStatus.Cancelled)
        {
            if (string.Equals(onboarding.CancellationReason, reason, StringComparison.Ordinal))
            {
                return ToResult(onboarding, false);
            }

            throw new TenantOnboardingCancellationConflictException(
                "The tenant onboarding has already been cancelled with a different reason.");
        }

        var tenant = onboarding.Tenant ?? throw new InvalidOperationException("Provisioned tenant was not loaded.");
        var invitation = onboarding.Invitation ?? throw new InvalidOperationException("Owner invitation was not loaded.");
        if (onboarding.Status is not TenantOnboardingStatus.PendingOwnerAcceptance ||
            tenant.Status is not TenantStatus.PendingActivation ||
            invitation.Status is not TenantInvitationStatus.Pending)
        {
            throw new TenantOnboardingCancellationConflictException(
                "Only a pending tenant onboarding with an unaccepted Owner invitation can be cancelled.");
        }

        var now = DateTimeOffset.UtcNow;
        var previousDeliveryStatus = invitation.DeliveryStatus;
        onboarding.Status = TenantOnboardingStatus.Cancelled;
        onboarding.CancelledAt = now;
        onboarding.CancelledByUserId = actorUserId;
        onboarding.CancellationReason = reason;
        onboarding.UpdatedAt = now;
        onboarding.UpdatedByUserId = actorUserId;

        tenant.Status = TenantStatus.Archived;
        tenant.UpdatedAt = now;
        tenant.UpdatedByUserId = actorUserId;

        invitation.Status = TenantInvitationStatus.Revoked;
        invitation.RevokedAt = now;
        invitation.RevokedByUserId = actorUserId;
        invitation.InvitationTokenHash = null;
        invitation.DeliveryStatus = InvitationDeliveryStatus.Cancelled;
        invitation.NextDeliveryAttemptAt = null;
        invitation.DeliveryLeaseUntil = null;
        invitation.NotificationPlaceholder = "Owner invitation delivery was cancelled by a platform operator.";
        invitation.UpdatedAt = now;
        invitation.UpdatedByUserId = actorUserId;

        AddAudit(
            tenant.Id,
            actorUserId,
            AuditAction.Archived,
            "PlatformTenantOnboarding",
            onboarding.Id.ToString(),
            $"Tenant onboarding for customer reference '{onboarding.CustomerReference}' was cancelled.",
            new Dictionary<string, string>
            {
                ["reason"] = reason,
                ["previousOnboardingStatus"] = TenantOnboardingStatus.PendingOwnerAcceptance.ToString(),
                ["onboardingStatus"] = onboarding.Status.ToString(),
                ["previousTenantStatus"] = TenantStatus.PendingActivation.ToString(),
                ["tenantStatus"] = tenant.Status.ToString()
            },
            now);

        AddAudit(
            tenant.Id,
            actorUserId,
            AuditAction.Archived,
            "TenantInvitation",
            invitation.Id.ToString(),
            "The initial Owner invitation was revoked because tenant onboarding was cancelled.",
            new Dictionary<string, string>
            {
                ["reason"] = reason,
                ["status"] = invitation.Status.ToString(),
                ["previousDeliveryStatus"] = previousDeliveryStatus.ToString(),
                ["deliveryStatus"] = invitation.DeliveryStatus.ToString()
            },
            now);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return ToResult(onboarding, false);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            dbContext.ChangeTracker.Clear();
            throw new TenantOnboardingCancellationConflictException(
                "The tenant onboarding changed while cancellation was in progress. Refresh and try again.",
                exception);
        }
    }

    private IQueryable<PlatformTenantOnboardingEntity> Onboardings() =>
        dbContext.PlatformTenantOnboardings
            .AsNoTracking()
            .Include(candidate => candidate.Tenant)
            .Include(candidate => candidate.Invitation);

    private void AddOwnerRole(Guid tenantId, Guid actorUserId, DateTimeOffset now)
    {
        var role = new RoleEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = RoleCatalog.Owner,
            CreatedAt = now,
            CreatedByUserId = actorUserId
        };
        dbContext.Roles.Add(role);

        foreach (var permission in RoleCatalog.GetPermissions(RoleCatalog.Owner))
        {
            dbContext.Set<RolePermissionEntity>().Add(new RolePermissionEntity
            {
                RoleId = role.Id,
                Permission = permission
            });
        }
    }

    private void AddModeHistory(Guid tenantId, Guid actorUserId, string reason, DateTimeOffset now) =>
        dbContext.TenantDataHandlingModeHistory.Add(new TenantDataHandlingModeHistoryEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PreviousMode = null,
            NewMode = TenantDataPosture.NoCui,
            ActorUserId = actorUserId,
            ChangedAt = now,
            Reason = reason,
            ApprovalRecordReference = "platform-tenant-onboarding"
        });

    private void AddAuditEntries(
        PlatformTenantOnboardingEntity onboarding,
        TenantEntity tenant,
        TenantInvitationEntity invitation,
        Guid actorUserId,
        DateTimeOffset now)
    {
        AddAudit(
            tenant.Id,
            actorUserId,
            AuditAction.Created,
            "PlatformTenantOnboarding",
            onboarding.Id.ToString(),
            $"{onboarding.OnboardingType} tenant onboarding was created for customer reference '{onboarding.CustomerReference}'.",
            new Dictionary<string, string>
            {
                ["onboardingType"] = onboarding.OnboardingType.ToString(),
                ["onboardingStatus"] = onboarding.Status.ToString(),
                ["tenantStatus"] = tenant.Status.ToString(),
                ["dataHandlingMode"] = tenant.DataPosture.ToString(),
                ["customerReference"] = onboarding.CustomerReference
            },
            now);

        AddAudit(
            tenant.Id,
            actorUserId,
            AuditAction.Created,
            "TenantInvitation",
            invitation.Id.ToString(),
            "The initial Owner invitation was created and is pending delivery.",
            new Dictionary<string, string>
            {
                ["roleName"] = invitation.RoleName,
                ["status"] = invitation.Status.ToString(),
                ["expiresAt"] = invitation.ExpiresAt.ToString("O")
            },
            now);
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
        var auditMetadata = new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(requestMetadata.CorrelationId))
        {
            auditMetadata["correlationId"] = requestMetadata.CorrelationId;
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
            MetadataJson = JsonSerializer.Serialize(auditMetadata, JsonOptions)
        });
    }

    private static PlatformTenantProvisioningResultDto ToResult(
        PlatformTenantOnboardingEntity onboarding,
        bool isReplay)
    {
        var tenant = onboarding.Tenant ?? throw new InvalidOperationException("Provisioned tenant was not loaded.");
        var invitation = onboarding.Invitation ?? throw new InvalidOperationException("Owner invitation was not loaded.");

        return new PlatformTenantProvisioningResultDto(
            onboarding.Id,
            tenant.Id,
            tenant.Name,
            onboarding.OnboardingType,
            onboarding.Status,
            tenant.Status,
            tenant.DataPosture,
            onboarding.CustomerReference,
            onboarding.OwnerEmail,
            onboarding.OwnerDisplayName,
            RoleCatalog.Owner,
            invitation.Id,
            invitation.Status,
            invitation.DeliveryStatus,
            invitation.NotificationSentAt,
            invitation.ExpiresAt,
            tenant.TrialEndsAt,
            onboarding.PlanCode,
            onboarding.SubscriptionReference,
            onboarding.SetupReason,
            onboarding.CreatedAt,
            onboarding.CancelledAt,
            onboarding.CancelledByUserId,
            onboarding.CancellationReason,
            isReplay);
    }
}

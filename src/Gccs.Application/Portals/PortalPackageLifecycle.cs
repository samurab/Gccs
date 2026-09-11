using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Domain.Audit;
using Gccs.Domain.Common;

namespace Gccs.Application.Portals;

public sealed class PortalPackageLifecycleService(
    IPortalPackageLifecycleRepository repository,
    IPortalPackageShareEligibilityValidator eligibilityValidator,
    IAuditEventWriter auditEventWriter,
    IApplicationTransaction transaction,
    TimeProvider timeProvider)
{
    public async Task<SharedPortalPackageDto> ShareAsync(
        SharedPortalPackageRequest request,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateShare(request, timeProvider.GetUtcNow());
        await eligibilityValidator.ValidateAsync(
            request.PackageId, request.InvitationId, tenantId, timeProvider.GetUtcNow(), cancellationToken);
        if ((await repository.ListAccessibleAsync(tenantId, request.InvitationId, timeProvider.GetUtcNow(), cancellationToken))
            .Any(package => package.PackageId == request.PackageId))
            throw new PortalPackageLifecycleConflictException("The source package already has an active share for this invitation.");
        return await transaction.ExecuteAsync(async token =>
        {
            var package = await repository.CreateAsync(request, tenantId, actorUserId, token);
            await WriteAuditAsync(package, actorUserId, AuditAction.Created, "Shared portal package was activated.", token);
            return package;
        }, cancellationToken);
    }

    public Task<IReadOnlyList<SharedPortalPackageDto>> ListAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default) =>
        repository.ListAsync(tenantId, cancellationToken);

    public Task<IReadOnlyList<SharedPortalPackageDto>> ListAccessibleAsync(
        Guid tenantId,
        Guid invitationId,
        DateTimeOffset asOf,
        CancellationToken cancellationToken = default) =>
        repository.ListAccessibleAsync(tenantId, invitationId, asOf, cancellationToken);

    public Task<bool> CanAccessAsync(
        Guid sharedPackageId,
        Guid tenantId,
        Guid invitationId,
        Guid packageId,
        DateTimeOffset asOf,
        CancellationToken cancellationToken = default) =>
        repository.CanAccessAsync(sharedPackageId, tenantId, invitationId, packageId, asOf, cancellationToken);

    public Task<PortalPackageActivityReportDto> GenerateActivityReportAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default) =>
        repository.GenerateActivityReportAsync(tenantId, cancellationToken);

    public async Task RecordActivityAsync(
        Guid sharedPackageId,
        Guid tenantId,
        Guid invitationId,
        Guid packageId,
        PortalPackageActivityType type,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (type is not (PortalPackageActivityType.Access or PortalPackageActivityType.Comment or PortalPackageActivityType.Download))
            throw new PortalPackageLifecycleException("Only portal access, comment, and download activity can be recorded directly.");

        var now = timeProvider.GetUtcNow();
        if (!await repository.CanAccessAsync(sharedPackageId, tenantId, invitationId, packageId, now, cancellationToken))
            throw new PortalPackageAccessDeniedException("The shared package is unavailable or outside the invitation scope.");

        await repository.RecordActivityAsync(sharedPackageId, tenantId, type, actorUserId, null, now, cancellationToken);
    }

    public Task<SharedPortalPackageDto?> ExpireAsync(
        Guid sharedPackageId,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(sharedPackageId, tenantId, SharedPortalPackageState.Active, SharedPortalPackageState.Expired,
            null, "Shared portal package was expired.", AuditAction.Expired, actorUserId, cancellationToken);

    public Task<SharedPortalPackageDto?> RevokeAsync(
        Guid sharedPackageId,
        Guid tenantId,
        string reason,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var normalizedReason = NormalizeRequired(reason, 500, "A revocation reason");
        return TransitionAsync(sharedPackageId, tenantId, SharedPortalPackageState.Active, SharedPortalPackageState.Revoked,
            normalizedReason, "Shared portal package was revoked.", AuditAction.PermissionChanged, actorUserId, cancellationToken);
    }

    public async Task<SharedPortalPackageDto?> ArchiveAsync(
        Guid sharedPackageId,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        return await transaction.ExecuteAsync(async token =>
        {
            var existing = await repository.FindAsync(sharedPackageId, tenantId, token);
            if (existing is null) return null;
            if (existing.State == SharedPortalPackageState.Archived)
                throw new PortalPackageLifecycleException("The shared package is already archived.");

            var package = await repository.SetStateAsync(
                sharedPackageId, tenantId, existing.State, SharedPortalPackageState.Archived, null,
                actorUserId, timeProvider.GetUtcNow(), token);
            await WriteAuditAsync(package!, actorUserId, AuditAction.Archived, "Shared portal package was archived.", token);
            return package;
        }, cancellationToken);
    }

    public async Task<SharedPortalPackageDto?> SupersedeAsync(
        Guid sharedPackageId,
        Guid tenantId,
        Guid replacementSharedPackageId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (sharedPackageId == replacementSharedPackageId)
            throw new PortalPackageLifecycleException("A shared package cannot supersede itself.");

        return await transaction.ExecuteAsync(async token =>
        {
            var existing = await repository.FindAsync(sharedPackageId, tenantId, token);
            if (existing is null) return null;
            if (existing.State != SharedPortalPackageState.Active)
                throw InvalidTransition(existing.State, SharedPortalPackageState.Superseded);
            var replacement = await repository.FindAsync(replacementSharedPackageId, tenantId, token)
                ?? throw new PortalPackageLifecycleException("The replacement shared package was not found in the current tenant.");
            if (replacement.State != SharedPortalPackageState.Active || replacement.ExpiresAt <= timeProvider.GetUtcNow())
                throw new PortalPackageLifecycleException("The replacement shared package must be active and unexpired.");
            if (replacement.InvitationId != existing.InvitationId)
                throw new PortalPackageLifecycleException("The replacement must use the same portal invitation.");

            var package = await repository.SupersedeAsync(
                sharedPackageId, tenantId, SharedPortalPackageState.Active, replacement,
                actorUserId, timeProvider.GetUtcNow(), token);
            await WriteAuditAsync(package!, actorUserId, AuditAction.Updated, "Shared portal package was superseded.", token);
            return package;
        }, cancellationToken);
    }

    public async Task<SharedPortalPackageDto?> ReissueAsync(
        Guid sharedPackageId,
        Guid tenantId,
        ReissueSharedPortalPackageRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateReissue(request, timeProvider.GetUtcNow());
        return await transaction.ExecuteAsync(async token =>
        {
            var existing = await repository.FindAsync(sharedPackageId, tenantId, token);
            if (existing is null) return null;
            if (existing.State == SharedPortalPackageState.Archived)
                throw new PortalPackageLifecycleException("An archived shared package cannot be reissued.");
            if (existing.ReplacementSharedPackageId is not null)
                throw new PortalPackageLifecycleConflictException("The shared package already has a replacement version.");
            await eligibilityValidator.ValidateAsync(
                request.ReplacementPackageId, existing.InvitationId, tenantId, timeProvider.GetUtcNow(), token);

            var package = await repository.ReissueAsync(
                existing, request, actorUserId, timeProvider.GetUtcNow(), token);
            if (existing.State == SharedPortalPackageState.Active)
            {
                var superseded = await repository.FindAsync(existing.Id, tenantId, token)
                    ?? throw new PortalPackageLifecycleConflictException("The superseded package could not be reloaded.");
                await WriteAuditAsync(superseded, actorUserId, AuditAction.Updated,
                    "Shared portal package was superseded by a reissue.", token);
            }
            await WriteAuditAsync(package, actorUserId, AuditAction.Created, "Shared portal package was reissued.", token);
            return package;
        }, cancellationToken);
    }

    public async Task<PortalPackageMaintenanceResultDto> ProcessDueAsync(
        DateTimeOffset asOf,
        CancellationToken cancellationToken = default)
    {
        var due = await repository.ListDueForMaintenanceAsync(asOf, cancellationToken);
        var reminders = 0;
        var expirations = 0;
        foreach (var package in due)
        {
            if (package.State != SharedPortalPackageState.Active) continue;
            if (package.ExpiresAt <= asOf)
            {
                try
                {
                    await transaction.ExecuteAsync(async token =>
                    {
                        var expired = await repository.SetStateAsync(package.Id, package.TenantId,
                            SharedPortalPackageState.Active, SharedPortalPackageState.Expired, null,
                            Guid.Empty, asOf, token);
                        if (expired is not null)
                        {
                            await WriteAuditAsync(expired, Guid.Empty, AuditAction.Expired,
                                "Shared portal package expired automatically.", token);
                            expirations++;
                        }
                        return true;
                    }, cancellationToken);
                }
                catch (PortalPackageLifecycleConflictException)
                {
                    // Another request completed the terminal transition first.
                }
            }
            else if (package.ReminderAt <= asOf && package.ReminderSentAt is null)
            {
                if (await repository.MarkReminderSentAsync(package.Id, package.TenantId, asOf, cancellationToken))
                    reminders++;
            }
        }

        return new(reminders, expirations, asOf);
    }

    private async Task<SharedPortalPackageDto?> TransitionAsync(
        Guid sharedPackageId,
        Guid tenantId,
        SharedPortalPackageState expected,
        SharedPortalPackageState state,
        string? reason,
        string summary,
        AuditAction action,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        return await transaction.ExecuteAsync(async token =>
        {
            var existing = await repository.FindAsync(sharedPackageId, tenantId, token);
            if (existing is null) return null;
            if (existing.State != expected) throw InvalidTransition(existing.State, state);
            var package = await repository.SetStateAsync(sharedPackageId, tenantId, expected, state, reason,
                actorUserId, timeProvider.GetUtcNow(), token);
            await WriteAuditAsync(package!, actorUserId, action, summary, token);
            return package;
        }, cancellationToken);
    }

    private static void ValidateShare(SharedPortalPackageRequest request, DateTimeOffset now)
    {
        if (request.PackageId == Guid.Empty) throw new PortalPackageLifecycleException("A source package is required.");
        if (request.InvitationId == Guid.Empty) throw new PortalPackageLifecycleException("A portal invitation is required.");
        if (request.ExpiresAt <= now) throw new PortalPackageLifecycleException("Package expiration must be in the future.");
        if (request.ExpirationReminderDays is < 1 or > 30)
            throw new PortalPackageLifecycleException("Expiration reminder days must be between 1 and 30.");
    }

    private static void ValidateReissue(ReissueSharedPortalPackageRequest request, DateTimeOffset now)
    {
        if (request.ReplacementPackageId == Guid.Empty)
            throw new PortalPackageLifecycleException("A replacement source package is required.");
        if (request.ExpiresAt <= now) throw new PortalPackageLifecycleException("Package expiration must be in the future.");
        if (request.ExpirationReminderDays is < 1 or > 30)
            throw new PortalPackageLifecycleException("Expiration reminder days must be between 1 and 30.");
    }

    private static string NormalizeRequired(string value, int maximumLength, string field)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0 || normalized.Length > maximumLength)
            throw new PortalPackageLifecycleException($"{field} is required and cannot exceed {maximumLength} characters.");
        return normalized;
    }

    private static PortalPackageLifecycleException InvalidTransition(
        SharedPortalPackageState current,
        SharedPortalPackageState requested) =>
        new($"Shared package transition from {current} to {requested} is not allowed.");

    private async Task WriteAuditAsync(
        SharedPortalPackageDto package,
        Guid actorUserId,
        AuditAction action,
        string summary,
        CancellationToken cancellationToken)
    {
        await auditEventWriter.WriteAsync(
            package.TenantId,
            actorUserId,
            action,
            "SharedPortalPackage",
            package.Id.ToString(),
            summary,
            new Dictionary<string, string>
            {
                ["packageId"] = package.PackageId.ToString(),
                ["invitationId"] = package.InvitationId.ToString(),
                ["version"] = package.Version.ToString(),
                ["state"] = package.State.ToString(),
                ["replacementSharedPackageId"] = package.ReplacementSharedPackageId?.ToString() ?? string.Empty,
                ["replacementPackageId"] = package.ReplacementPackageId?.ToString() ?? string.Empty,
                ["revocationReason"] = package.RevocationReason ?? string.Empty,
                ["systemInitiated"] = (actorUserId == Guid.Empty).ToString()
            },
            cancellationToken);
    }
}

public interface IPortalPackageShareEligibilityValidator
{
    Task ValidateAsync(
        Guid packageId,
        Guid invitationId,
        Guid tenantId,
        DateTimeOffset asOf,
        CancellationToken cancellationToken = default);
}

public sealed class PortalPackageShareEligibilityValidator(
    IExternalPortalAccessRepository invitationRepository,
    IPortalPackageRepository packageRepository) : IPortalPackageShareEligibilityValidator
{
    public async Task ValidateAsync(
        Guid packageId,
        Guid invitationId,
        Guid tenantId,
        DateTimeOffset asOf,
        CancellationToken cancellationToken = default)
    {
        var invitation = await invitationRepository.FindInvitationAsync(invitationId, cancellationToken);
        if (invitation is null ||
            invitation.TenantId != tenantId ||
            invitation.Status == ExternalPortalInvitationStatus.Revoked ||
            invitation.ExpiresAt <= asOf ||
            !invitation.PackageIds.Contains(packageId))
            throw new PortalPackageLifecycleException("The portal invitation is unavailable or does not include the source package.");

        var package = await packageRepository.FindPackageAsync(packageId, cancellationToken);
        if (package is null ||
            package.TenantId != tenantId ||
            package.Status != PortalPackageStatus.Approved ||
            package.ContainsInternalNotes ||
            package.Classification is not (ContentClassification.Unclassified or ContentClassification.Fci))
            throw new PortalPackageLifecycleException("The source package is not approved and eligible for No-CUI external sharing.");
    }
}

public interface IPortalPackageLifecycleRepository
{
    Task<SharedPortalPackageDto> CreateAsync(SharedPortalPackageRequest request, Guid tenantId, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SharedPortalPackageDto>> ListAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<SharedPortalPackageDto?> FindAsync(Guid sharedPackageId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SharedPortalPackageDto>> ListAccessibleAsync(Guid tenantId, Guid invitationId, DateTimeOffset asOf, CancellationToken cancellationToken = default);
    Task<SharedPortalPackageDto?> SetStateAsync(Guid sharedPackageId, Guid tenantId, SharedPortalPackageState expectedState, SharedPortalPackageState state, string? reason, Guid actorUserId, DateTimeOffset changedAt, CancellationToken cancellationToken = default);
    Task<SharedPortalPackageDto?> SupersedeAsync(Guid sharedPackageId, Guid tenantId, SharedPortalPackageState expectedState, SharedPortalPackageDto replacement, Guid actorUserId, DateTimeOffset changedAt, CancellationToken cancellationToken = default);
    Task<SharedPortalPackageDto> ReissueAsync(SharedPortalPackageDto existing, ReissueSharedPortalPackageRequest request, Guid actorUserId, DateTimeOffset changedAt, CancellationToken cancellationToken = default);
    Task<bool> CanAccessAsync(Guid sharedPackageId, Guid tenantId, Guid invitationId, Guid packageId, DateTimeOffset asOf, CancellationToken cancellationToken = default);
    Task RecordActivityAsync(Guid sharedPackageId, Guid tenantId, PortalPackageActivityType type, Guid actorUserId, string? detail, DateTimeOffset occurredAt, CancellationToken cancellationToken = default);
    Task<PortalPackageActivityReportDto> GenerateActivityReportAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SharedPortalPackageDto>> ListDueForMaintenanceAsync(DateTimeOffset asOf, CancellationToken cancellationToken = default);
    Task<bool> MarkReminderSentAsync(Guid sharedPackageId, Guid tenantId, DateTimeOffset sentAt, CancellationToken cancellationToken = default);
}

public sealed record SharedPortalPackageRequest(Guid PackageId, Guid InvitationId, DateTimeOffset ExpiresAt, int ExpirationReminderDays = 7);
public sealed record ReissueSharedPortalPackageRequest(Guid ReplacementPackageId, DateTimeOffset ExpiresAt, int ExpirationReminderDays = 7);
public sealed record RevokeSharedPortalPackageRequest(string Reason);
public sealed record SupersedeSharedPortalPackageRequest(Guid ReplacementSharedPackageId);

public sealed record SharedPortalPackageDto(
    Guid Id,
    Guid TenantId,
    Guid PackageId,
    Guid InvitationId,
    int Version,
    SharedPortalPackageState State,
    DateTimeOffset ExpiresAt,
    DateTimeOffset ReminderAt,
    DateTimeOffset? ReminderSentAt,
    Guid? SupersedesSharedPackageId,
    Guid? ReplacementSharedPackageId,
    Guid? ReplacementPackageId,
    string? RevocationReason,
    DateTimeOffset? RevokedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record PortalPackageActivityReportDto(Guid TenantId, IReadOnlyList<PortalPackageActivityDto> Activities);
public sealed record PortalPackageActivityDto(Guid Id, Guid SharedPackageId, Guid TenantId, PortalPackageActivityType ActivityType, Guid ActorUserId, DateTimeOffset OccurredAt, string? Detail);
public sealed record PortalPackageMaintenanceResultDto(int RemindersCreated, int PackagesExpired, DateTimeOffset ProcessedAt);

public enum SharedPortalPackageState { Active, Superseded, Expired, Revoked, Archived }
public enum PortalPackageActivityType { Access, Comment, Download, ExpirationReminder, Expiration, Supersede, Revocation, Reissue, Archive }

public sealed class PortalPackageLifecycleException(string message) : InvalidOperationException(message);
public sealed class PortalPackageLifecycleConflictException(string message) : InvalidOperationException(message);
public sealed class PortalPackageAccessDeniedException(string message) : InvalidOperationException(message);

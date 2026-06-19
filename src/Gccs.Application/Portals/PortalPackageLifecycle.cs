using Gccs.Application.Audit;
using Gccs.Domain.Audit;

namespace Gccs.Application.Portals;

public sealed class PortalPackageLifecycleService(
    IPortalPackageLifecycleRepository repository,
    IAuditEventWriter auditEventWriter)
{
    public async Task<SharedPortalPackageDto> ShareAsync(
        SharedPortalPackageRequest request,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var package = await repository.CreateAsync(request, tenantId, actorUserId, cancellationToken);
        await WriteAuditAsync(package, actorUserId, AuditAction.Created, "Shared portal package was activated.", cancellationToken);
        return package;
    }

    public Task<bool> CanAccessAsync(Guid sharedPackageId, DateTimeOffset asOf, CancellationToken cancellationToken = default) =>
        repository.CanAccessAsync(sharedPackageId, asOf, cancellationToken);

    public Task<PortalPackageActivityReportDto> GenerateActivityReportAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        repository.GenerateActivityReportAsync(tenantId, cancellationToken);

    public Task RecordActivityAsync(Guid sharedPackageId, PortalPackageActivityType type, Guid actorUserId, CancellationToken cancellationToken = default) =>
        repository.RecordActivityAsync(sharedPackageId, type, actorUserId, cancellationToken);

    public Task<SharedPortalPackageDto?> ExpireAsync(Guid sharedPackageId, Guid actorUserId, CancellationToken cancellationToken = default) =>
        TransitionAsync(sharedPackageId, SharedPortalPackageState.Expired, null, "Shared portal package was expired.", AuditAction.Expired, actorUserId, cancellationToken);

    public Task<SharedPortalPackageDto?> RevokeAsync(Guid sharedPackageId, string reason, Guid actorUserId, CancellationToken cancellationToken = default) =>
        TransitionAsync(sharedPackageId, SharedPortalPackageState.Revoked, reason, "Shared portal package was revoked.", AuditAction.PermissionChanged, actorUserId, cancellationToken);

    public Task<SharedPortalPackageDto?> ArchiveAsync(Guid sharedPackageId, Guid actorUserId, CancellationToken cancellationToken = default) =>
        TransitionAsync(sharedPackageId, SharedPortalPackageState.Archived, null, "Shared portal package was archived.", AuditAction.Archived, actorUserId, cancellationToken);

    public async Task<SharedPortalPackageDto?> SupersedeAsync(
        Guid sharedPackageId,
        Guid replacementPackageId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var package = await repository.SupersedeAsync(sharedPackageId, replacementPackageId, actorUserId, cancellationToken);
        if (package is not null)
        {
            await WriteAuditAsync(package, actorUserId, AuditAction.Updated, "Shared portal package was superseded.", cancellationToken);
        }

        return package;
    }

    public async Task<SharedPortalPackageDto?> ReissueAsync(
        Guid sharedPackageId,
        DateTimeOffset expiresAt,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var package = await repository.ReissueAsync(sharedPackageId, expiresAt, actorUserId, cancellationToken);
        if (package is not null)
        {
            await WriteAuditAsync(package, actorUserId, AuditAction.Updated, "Shared portal package was reissued.", cancellationToken);
        }

        return package;
    }

    private async Task<SharedPortalPackageDto?> TransitionAsync(
        Guid sharedPackageId,
        SharedPortalPackageState state,
        string? reason,
        string summary,
        AuditAction action,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var package = await repository.SetStateAsync(sharedPackageId, state, reason, actorUserId, cancellationToken);
        if (package is not null)
        {
            await WriteAuditAsync(package, actorUserId, action, summary, cancellationToken);
        }

        return package;
    }

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
                ["state"] = package.State.ToString(),
                ["replacementPackageId"] = package.ReplacementPackageId?.ToString() ?? string.Empty,
                ["revocationReason"] = package.RevocationReason ?? string.Empty
            },
            cancellationToken);
    }
}

public interface IPortalPackageLifecycleRepository
{
    Task<SharedPortalPackageDto> CreateAsync(SharedPortalPackageRequest request, Guid tenantId, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<SharedPortalPackageDto?> SetStateAsync(Guid sharedPackageId, SharedPortalPackageState state, string? reason, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<SharedPortalPackageDto?> SupersedeAsync(Guid sharedPackageId, Guid replacementPackageId, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<SharedPortalPackageDto?> ReissueAsync(Guid sharedPackageId, DateTimeOffset expiresAt, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<bool> CanAccessAsync(Guid sharedPackageId, DateTimeOffset asOf, CancellationToken cancellationToken = default);
    Task RecordActivityAsync(Guid sharedPackageId, PortalPackageActivityType type, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<PortalPackageActivityReportDto> GenerateActivityReportAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public sealed record SharedPortalPackageRequest(
    Guid PackageId,
    Guid InvitationId,
    DateTimeOffset ExpiresAt);

public sealed record SharedPortalPackageDto(
    Guid Id,
    Guid TenantId,
    Guid PackageId,
    Guid InvitationId,
    SharedPortalPackageState State,
    DateTimeOffset ExpiresAt,
    Guid? ReplacementPackageId,
    string? RevocationReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record PortalPackageActivityReportDto(
    Guid TenantId,
    IReadOnlyList<PortalPackageActivityDto> Activities);

public sealed record PortalPackageActivityDto(
    Guid SharedPackageId,
    PortalPackageActivityType ActivityType,
    Guid ActorUserId,
    DateTimeOffset OccurredAt,
    string? Detail);

public enum SharedPortalPackageState
{
    Active,
    Superseded,
    Expired,
    Revoked,
    Archived
}

public enum PortalPackageActivityType
{
    Access,
    Comment,
    Download,
    Expiration,
    Supersede,
    Revocation,
    Reissue,
    Archive
}

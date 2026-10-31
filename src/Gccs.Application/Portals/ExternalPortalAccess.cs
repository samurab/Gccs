using Gccs.Application.Audit;
using Gccs.Domain.Audit;

namespace Gccs.Application.Portals;

public sealed class ExternalPortalAccessService
{
    private readonly IExternalPortalAccessRepository _repository;
    private readonly IExternalPortalScopeValidator _scopeValidator;
    private readonly IAuditEventWriter _auditEventWriter;
    private readonly TimeProvider _timeProvider;

    public ExternalPortalAccessService(
        IExternalPortalAccessRepository repository,
        IAuditEventWriter auditEventWriter,
        IExternalPortalScopeValidator? scopeValidator = null,
        TimeProvider? timeProvider = null)
    {
        _repository = repository;
        _auditEventWriter = auditEventWriter;
        _scopeValidator = scopeValidator ?? PermissiveExternalPortalScopeValidator.Instance;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ExternalPortalInvitationDto> InviteAsync(
        ExternalPortalInvitationRequest request,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeAndValidate(request, _timeProvider.GetUtcNow());
        await _scopeValidator.ValidateAsync(tenantId, normalized.PackageIds, normalized.ContractIds, cancellationToken);
        var invitation = await _repository.CreateInvitationAsync(normalized, tenantId, actorUserId, cancellationToken);
        await WriteAuditAsync(invitation, actorUserId, AuditAction.Created, "External portal invitation was created.", cancellationToken);
        return invitation;
    }

    public Task<IReadOnlyList<ExternalPortalInvitationDto>> ListAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        _repository.ListInvitationsAsync(tenantId, cancellationToken);

    public async Task<IReadOnlyList<ExternalPortalAccessHistoryDto>> ListAccessHistoryAsync(
        Guid invitationId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        if (await _repository.FindInvitationAsync(invitationId, tenantId, cancellationToken) is null)
            throw new ExternalPortalInvitationNotFoundException();
        return await _repository.ListAccessHistoryAsync(invitationId, tenantId, cancellationToken);
    }

    public async Task<ExternalPortalAccessResultDto> ValidateAccessAsync(
        ExternalPortalAccessRequest request,
        CancellationToken cancellationToken = default)
    {
        var invitation = await _repository.FindInvitationForAccessAsync(request.InvitationId, cancellationToken);
        if (invitation is null)
            return Denied();

        var denialCode = GetDenialCode(invitation, request);
        if (denialCode is not null)
        {
            await _repository.RecordAccessAsync(
                invitation.Id, invitation.TenantId, request.ActorUserId, request.PackageId, request.ContractId,
                false, denialCode, request.AsOf, false, cancellationToken);
            await WriteAccessAuditAsync(invitation, request.ActorUserId, false, denialCode, request, cancellationToken);
            return Denied();
        }

        var updated = await _repository.RecordAccessAsync(
            invitation.Id, invitation.TenantId, request.ActorUserId, request.PackageId, request.ContractId,
            true, "access_granted", request.AsOf, true, cancellationToken)
            ?? throw new ExternalPortalAccessConflictException("The portal invitation changed. Sign in again and retry.");
        await WriteAccessAuditAsync(updated, request.ActorUserId, true, "access_granted", request, cancellationToken);
        return new(true, "Access granted.", updated);
    }

    public bool IsInvitationIdentityEligible(
        ExternalPortalInvitationDto invitation,
        Guid actorUserId,
        string? actorEmail,
        bool strongAuthenticationSatisfied,
        DateTimeOffset asOf)
    {
        if (invitation.PackageIds.Count == 0) return false;
        var identityProbe = new ExternalPortalAccessRequest(
            invitation.Id, invitation.PackageIds[0], null, actorUserId, actorEmail,
            strongAuthenticationSatisfied, asOf);
        return GetDenialCode(invitation, identityProbe) is null;
    }

    // Compatibility overload for internal portal-review paths that predate authenticated identity binding.
    public Task<ExternalPortalAccessResultDto> ValidateAccessAsync(
        Guid invitationId, Guid packageId, Guid? contractId, DateTimeOffset asOf, Guid actorUserId,
        CancellationToken cancellationToken = default) =>
        ValidateAccessAsync(new ExternalPortalAccessRequest(
            invitationId, packageId, contractId, actorUserId, null, true, asOf), cancellationToken);

    public Task EnsureReadOnlyAsync() =>
        throw new ExternalPortalAccessException("External portal users have read-only access and cannot modify tenant workspace data.");

    public async Task<ExternalPortalInvitationDto?> ResendAsync(
        Guid invitationId, Guid tenantId, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var invitation = await GetManageableAsync(invitationId, tenantId, cancellationToken);
        EnsureUsableForAdministration(invitation, _timeProvider.GetUtcNow(), permitExpired: false);
        var updated = await _repository.ResendAsync(
            invitation.Id, tenantId, invitation.Version, actorUserId, _timeProvider.GetUtcNow(), cancellationToken) ?? throw Conflict();
        await WriteAuditAsync(updated, actorUserId, AuditAction.Updated, "External portal invitation resend was requested.", cancellationToken);
        return updated;
    }

    public Task<ExternalPortalInvitationDto?> ResendAsync(Guid invitationId, Guid actorUserId, CancellationToken cancellationToken = default) =>
        ResendUnscopedCompatibilityAsync(invitationId, actorUserId, cancellationToken);

    public async Task<ExternalPortalInvitationDto?> ExtendAsync(
        Guid invitationId, Guid tenantId, DateTimeOffset expiresAt, Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        if (expiresAt <= now)
            throw new ExternalPortalAccessException("Portal invitation expiration must be in the future.");

        var invitation = await GetManageableAsync(invitationId, tenantId, cancellationToken);
        EnsureUsableForAdministration(invitation, now, permitExpired: true);
        var updated = await _repository.ExtendAsync(
            invitation.Id, tenantId, invitation.Version, expiresAt, actorUserId, now, cancellationToken) ?? throw Conflict();
        await WriteAuditAsync(updated, actorUserId, AuditAction.Updated, "External portal invitation was extended.", cancellationToken);
        return updated;
    }

    public Task<ExternalPortalInvitationDto?> ExtendAsync(
        Guid invitationId, DateTimeOffset expiresAt, Guid actorUserId, CancellationToken cancellationToken = default) =>
        ExtendUnscopedCompatibilityAsync(invitationId, expiresAt, actorUserId, cancellationToken);

    public async Task<ExternalPortalInvitationDto?> RevokeAsync(
        Guid invitationId, Guid tenantId, string reason, Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var normalizedReason = NormalizeRequired(reason, "Revocation reason", 500);
        var invitation = await GetManageableAsync(invitationId, tenantId, cancellationToken);
        if (invitation.Status == ExternalPortalInvitationStatus.Revoked)
            throw new ExternalPortalInvitationStateException("The portal invitation is already revoked.");

        var updated = await _repository.RevokeAsync(
            invitation.Id, tenantId, invitation.Version, normalizedReason, actorUserId, _timeProvider.GetUtcNow(), cancellationToken) ?? throw Conflict();
        await WriteAuditAsync(updated, actorUserId, AuditAction.PermissionChanged, "External portal invitation was revoked.", cancellationToken);
        return updated;
    }

    public Task<ExternalPortalInvitationDto?> RevokeAsync(Guid invitationId, Guid actorUserId, CancellationToken cancellationToken = default) =>
        RevokeUnscopedCompatibilityAsync(invitationId, actorUserId, cancellationToken);

    private async Task<ExternalPortalInvitationDto> GetManageableAsync(
        Guid invitationId, Guid tenantId, CancellationToken cancellationToken) =>
        await _repository.FindInvitationAsync(invitationId, tenantId, cancellationToken) ?? throw new ExternalPortalInvitationNotFoundException();

    private static string? GetDenialCode(ExternalPortalInvitationDto invitation, ExternalPortalAccessRequest request)
    {
        if (invitation.Status == ExternalPortalInvitationStatus.Revoked) return "invitation_revoked";
        if (invitation.ExpiresAt <= request.AsOf) return "invitation_expired";
        if (!string.IsNullOrWhiteSpace(request.ActorEmail) &&
            !string.Equals(invitation.Email, request.ActorEmail.Trim(), StringComparison.OrdinalIgnoreCase)) return "identity_email_mismatch";
        if (invitation.ExternalUserId is not null && invitation.ExternalUserId != request.ActorUserId) return "identity_subject_mismatch";
        if (invitation.StrongAuthenticationRequired && !request.StrongAuthenticationSatisfied) return "strong_authentication_required";
        if (request.RequiresDownloadPermission && !invitation.CanDownload) return "download_not_permitted";
        if (!invitation.PackageIds.Contains(request.PackageId)) return "package_out_of_scope";
        if (request.ContractId is not null && !invitation.ContractIds.Contains(request.ContractId.Value)) return "contract_out_of_scope";
        return null;
    }

    private static ExternalPortalInvitationRequest NormalizeAndValidate(ExternalPortalInvitationRequest request, DateTimeOffset now)
    {
        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        if (email.Length is < 3 or > 320 || !email.Contains('@'))
            throw new ExternalPortalAccessException("Portal invitation requires a valid email address.");
        if (!Enum.IsDefined(request.Role))
            throw new ExternalPortalAccessException("Portal invitation role is invalid.");

        var packageIds = request.PackageIds.Where(id => id != Guid.Empty).Distinct().ToArray();
        var contractIds = request.ContractIds.Where(id => id != Guid.Empty).Distinct().ToArray();
        if (packageIds.Length == 0)
            throw new ExternalPortalAccessException("Portal invitation requires at least one package.");
        if (packageIds.Length > 100 || contractIds.Length > 100)
            throw new ExternalPortalAccessException("Portal invitation scope cannot exceed 100 packages or 100 contracts.");
        if (request.ExpiresAt <= now)
            throw new ExternalPortalAccessException("Portal invitation expiration must be in the future.");

        return request with { Email = email, PackageIds = packageIds, ContractIds = contractIds };
    }

    private static void EnsureUsableForAdministration(
        ExternalPortalInvitationDto invitation, DateTimeOffset now, bool permitExpired)
    {
        if (invitation.Status == ExternalPortalInvitationStatus.Revoked)
            throw new ExternalPortalInvitationStateException("A revoked portal invitation cannot be changed.");
        if (!permitExpired && invitation.ExpiresAt <= now)
            throw new ExternalPortalInvitationStateException("An expired portal invitation must be extended before it can be resent.");
    }

    private Task WriteAuditAsync(
        ExternalPortalInvitationDto invitation, Guid actorUserId, AuditAction action, string summary,
        CancellationToken cancellationToken) =>
        _auditEventWriter.WriteAsync(
            invitation.TenantId, actorUserId, action, "ExternalPortalInvitation", invitation.Id.ToString(), summary,
            new Dictionary<string, string>
            {
                ["role"] = invitation.Role.ToString(),
                ["status"] = invitation.Status.ToString(),
                ["packageCount"] = invitation.PackageIds.Count.ToString(),
                ["contractCount"] = invitation.ContractIds.Count.ToString(),
                ["strongAuthenticationRequired"] = invitation.StrongAuthenticationRequired.ToString()
            }, cancellationToken);

    private Task WriteAccessAuditAsync(
        ExternalPortalInvitationDto invitation, Guid actorUserId, bool allowed, string resultCode,
        ExternalPortalAccessRequest request, CancellationToken cancellationToken) =>
        _auditEventWriter.WriteAsync(
            invitation.TenantId, actorUserId, allowed ? AuditAction.Viewed : AuditAction.Rejected,
            "ExternalPortalInvitation", invitation.Id.ToString(),
            allowed ? "External portal access was granted." : "External portal access was denied.",
            new Dictionary<string, string>
            {
                ["result"] = resultCode,
                ["packageId"] = request.PackageId.ToString(),
                ["contractId"] = request.ContractId?.ToString() ?? string.Empty
            }, cancellationToken);

    private async Task<ExternalPortalInvitationDto?> ResendUnscopedCompatibilityAsync(Guid id, Guid actor, CancellationToken token)
    {
        var invitation = await _repository.FindInvitationForAccessAsync(id, token);
        return invitation is null ? null : await ResendAsync(id, invitation.TenantId, actor, token);
    }

    private async Task<ExternalPortalInvitationDto?> ExtendUnscopedCompatibilityAsync(Guid id, DateTimeOffset expiresAt, Guid actor, CancellationToken token)
    {
        var invitation = await _repository.FindInvitationForAccessAsync(id, token);
        return invitation is null ? null : await ExtendAsync(id, invitation.TenantId, expiresAt, actor, token);
    }

    private async Task<ExternalPortalInvitationDto?> RevokeUnscopedCompatibilityAsync(Guid id, Guid actor, CancellationToken token)
    {
        var invitation = await _repository.FindInvitationForAccessAsync(id, token);
        return invitation is null ? null : await RevokeAsync(id, invitation.TenantId, "Revoked by a tenant administrator.", actor, token);
    }

    private static ExternalPortalAccessResultDto Denied() => new(false, "Portal access is unavailable.", null);

    private static string NormalizeRequired(string? value, string field, int maximumLength)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0 || normalized.Length > maximumLength)
            throw new ExternalPortalAccessException($"{field} is required and cannot exceed {maximumLength} characters.");
        return normalized;
    }

    private static ExternalPortalAccessConflictException Conflict() =>
        new("The portal invitation changed. Reload it and try again.");
}

public interface IExternalPortalAccessRepository
{
    Task<ExternalPortalInvitationDto> CreateInvitationAsync(ExternalPortalInvitationRequest request, Guid tenantId, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExternalPortalInvitationDto>> ListInvitationsAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<ExternalPortalInvitationDto?> FindInvitationAsync(Guid invitationId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<ExternalPortalInvitationDto?> FindInvitationForAccessAsync(Guid invitationId, CancellationToken cancellationToken = default);
    Task<ExternalPortalInvitationDto?> ResendAsync(Guid invitationId, Guid tenantId, long expectedVersion, Guid actorUserId, DateTimeOffset changedAt, CancellationToken cancellationToken = default);
    Task<ExternalPortalInvitationDto?> ExtendAsync(Guid invitationId, Guid tenantId, long expectedVersion, DateTimeOffset expiresAt, Guid actorUserId, DateTimeOffset changedAt, CancellationToken cancellationToken = default);
    Task<ExternalPortalInvitationDto?> RevokeAsync(Guid invitationId, Guid tenantId, long expectedVersion, string reason, Guid actorUserId, DateTimeOffset changedAt, CancellationToken cancellationToken = default);
    Task<ExternalPortalInvitationDto?> RecordAccessAsync(Guid invitationId, Guid tenantId, Guid actorUserId, Guid packageId, Guid? contractId, bool allowed, string resultCode, DateTimeOffset occurredAt, bool bindExternalUser, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExternalPortalAccessHistoryDto>> ListAccessHistoryAsync(Guid invitationId, Guid tenantId, CancellationToken cancellationToken = default);
}

public interface IExternalPortalScopeValidator
{
    Task ValidateAsync(Guid tenantId, IReadOnlyList<Guid> packageIds, IReadOnlyList<Guid> contractIds, CancellationToken cancellationToken = default);
}

public sealed class PermissiveExternalPortalScopeValidator : IExternalPortalScopeValidator
{
    public static PermissiveExternalPortalScopeValidator Instance { get; } = new();
    private PermissiveExternalPortalScopeValidator() { }
    public Task ValidateAsync(Guid tenantId, IReadOnlyList<Guid> packageIds, IReadOnlyList<Guid> contractIds, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

public sealed record ExternalPortalInvitationRequest(
    string Email, ExternalPortalRole Role, IReadOnlyList<Guid> PackageIds, IReadOnlyList<Guid> ContractIds,
    DateTimeOffset ExpiresAt, bool CanDownload, bool StrongAuthenticationRequired);

public sealed record ExternalPortalInvitationDto(
    Guid Id, Guid TenantId, string Email, ExternalPortalRole Role, IReadOnlyList<Guid> PackageIds,
    IReadOnlyList<Guid> ContractIds, DateTimeOffset ExpiresAt, bool CanDownload, bool StrongAuthenticationRequired,
    ExternalPortalInvitationStatus Status, Guid? ExternalUserId, DateTimeOffset? LastAccessedAt,
    DateTimeOffset? RevokedAt, string? RevocationReason, int ResendCount, DateTimeOffset? LastResentAt,
    long Version, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt);

public sealed record ExternalPortalAccessRequest(
    Guid InvitationId, Guid PackageId, Guid? ContractId, Guid ActorUserId, string? ActorEmail,
    bool StrongAuthenticationSatisfied, DateTimeOffset AsOf, bool RequiresDownloadPermission = false);

public sealed record ExternalPortalAccessResultDto(bool Allowed, string Reason, ExternalPortalInvitationDto? Invitation);

public sealed record ExternalPortalAccessHistoryDto(
    Guid Id, Guid InvitationId, Guid TenantId, Guid ActorUserId, Guid PackageId, Guid? ContractId,
    bool Allowed, string ResultCode, DateTimeOffset OccurredAt);

public enum ExternalPortalRole
{
    PrimeReviewer,
    AuditorReviewer,
    AdvisorReviewer,
    PackageRecipient,
    Auditor = AuditorReviewer
}

public enum ExternalPortalInvitationStatus { Pending, Accepted, Revoked }

public sealed class ExternalPortalAccessException(string message) : InvalidOperationException(message);
public sealed class ExternalPortalInvitationStateException(string message) : InvalidOperationException(message);
public sealed class ExternalPortalAccessConflictException(string message) : InvalidOperationException(message);
public sealed class ExternalPortalInvitationNotFoundException() : InvalidOperationException("Portal invitation was not found.");

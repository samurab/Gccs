using Gccs.Domain.Identity;

namespace Gccs.Application.Identity;

public interface ITenantInvitationRepository
{
    Task<IReadOnlyList<TenantInvitationDto>> ListCurrentTenantInvitationsAsync(
        CancellationToken cancellationToken = default);

    Task<bool> CurrentTenantPendingInvitationExistsAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<TenantInvitationDto> AddToCurrentTenantAsync(
        TenantInvitation invitation,
        CancellationToken cancellationToken = default);

    Task<TenantInvitationDto?> FindByTokenAsync(
        string token,
        CancellationToken cancellationToken = default);

    Task<TenantInvitationDto?> AcceptAsync(
        Guid invitationId,
        Guid userId,
        string email,
        string displayName,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<TenantInvitationDto?> ExpireInCurrentTenantScopeAsync(
        Guid invitationId,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<TenantInvitationDto?> RevokeInCurrentTenantScopeAsync(
        Guid invitationId,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}

using Gccs.Domain.Identity;

namespace Gccs.Application.Identity;

public interface ITenantMembershipRepository
{
    Task<IReadOnlyList<TenantMemberDto>> ListCurrentTenantMembersAsync(CancellationToken cancellationToken = default);

    Task<bool> CurrentTenantMembershipExistsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<TenantMemberDto> AddToCurrentTenantAsync(
        User user,
        TenantMembership membership,
        CancellationToken cancellationToken = default);

    Task<TenantMemberDto?> UpdateStatusInCurrentTenantScopeAsync(
        Guid membershipId,
        MembershipStatus status,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}

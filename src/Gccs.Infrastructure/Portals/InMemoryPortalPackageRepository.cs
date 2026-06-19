using Gccs.Application.Portals;

namespace Gccs.Infrastructure.Portals;

public sealed class InMemoryPortalPackageRepository : IPortalPackageRepository
{
    private readonly List<ExternalPortalInvitationDto> _invitations = [];
    private readonly List<PortalPackageDto> _packages = [];
    private readonly List<PortalPackageCommentDto> _comments = [];

    public IReadOnlyList<PortalPackageCommentDto> Comments => _comments;

    public void SeedInvitation(ExternalPortalInvitationDto invitation) => _invitations.Add(invitation);

    public void SeedPackages(params PortalPackageDto[] packages) => _packages.AddRange(packages);

    public Task<ExternalPortalInvitationDto?> FindInvitationAsync(Guid invitationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_invitations.SingleOrDefault(invitation => invitation.Id == invitationId));

    public Task<IReadOnlyList<PortalPackageDto>> ListPackagesAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PortalPackageDto>>(_packages.Where(package => package.TenantId == tenantId).ToArray());

    public Task<PortalPackageDto?> FindPackageAsync(Guid packageId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_packages.SingleOrDefault(package => package.Id == packageId));

    public Task<PortalPackageCommentDto> AddCommentAsync(
        PortalPackageCommentRequest request,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var comment = new PortalPackageCommentDto(
            Guid.NewGuid(),
            tenantId,
            request.PackageId,
            actorUserId,
            request.Kind,
            request.Body.Trim(),
            DateTimeOffset.UtcNow);
        _comments.Add(comment);
        return Task.FromResult(comment);
    }
}

using Gccs.Application.Portals;

namespace Gccs.Infrastructure.Portals;

public sealed class InMemoryPortalPackageRepository : IPortalPackageRepository
{
    private readonly object _gate = new();
    private readonly List<ExternalPortalInvitationDto> _invitations = [];
    private readonly List<PortalPackageDto> _packages = [];
    private readonly List<StoredMessage> _messages = [];
    private readonly Dictionary<Guid, PortalPackageArtifactDto> _artifacts = [];

    public IReadOnlyList<PortalPackageReviewMessageDto> Messages
    {
        get { lock (_gate) return _messages.Select(item => item.Message).ToArray(); }
    }

    public void SeedInvitation(ExternalPortalInvitationDto invitation)
    {
        lock (_gate) _invitations.Add(invitation);
    }

    public void SeedPackages(params PortalPackageDto[] packages)
    {
        lock (_gate)
        {
            _packages.AddRange(packages);
            foreach (var package in packages)
                _artifacts[package.Id] = new PortalPackageArtifactDto(
                    package, $"{package.Id}.html", "text/html; charset=utf-8", $"<main><h1>{package.Title}</h1></main>");
        }
    }

    public void AddPreparedPackage(PortalPackageDto package, string artifact)
    {
        lock (_gate)
        {
            _packages.Add(package);
            _artifacts[package.Id] = new PortalPackageArtifactDto(
                package, $"fedril-{package.Id:N}.html", "text/html; charset=utf-8", artifact);
        }
    }

    public void ReplacePackage(PortalPackageDto package)
    {
        lock (_gate)
        {
            var index = _packages.FindIndex(item => item.TenantId == package.TenantId && item.Id == package.Id);
            if (index < 0) throw new InvalidOperationException("Package not found.");
            _packages[index] = package;
            _artifacts[package.Id] = new PortalPackageArtifactDto(
                package, $"{package.Id}.html", "text/html; charset=utf-8",
                $"<main><h1>{System.Net.WebUtility.HtmlEncode(package.Title)}</h1></main>");
        }
    }

    public Task<ExternalPortalInvitationDto?> FindInvitationAsync(
        Guid invitationId, CancellationToken cancellationToken = default)
    {
        lock (_gate) return Task.FromResult(_invitations.SingleOrDefault(item => item.Id == invitationId));
    }

    public Task<IReadOnlyList<PortalPackageDto>> ListPackagesAsync(
        Guid tenantId, IReadOnlyCollection<Guid> packageIds, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var ids = packageIds.ToHashSet();
            return Task.FromResult<IReadOnlyList<PortalPackageDto>>(
                _packages.Where(item => item.TenantId == tenantId && ids.Contains(item.Id)).ToArray());
        }
    }

    public Task<PortalPackageDto?> FindPackageAsync(
        Guid tenantId, Guid packageId, CancellationToken cancellationToken = default)
    {
        lock (_gate) return Task.FromResult(_packages.SingleOrDefault(item => item.TenantId == tenantId && item.Id == packageId));
    }

    public Task<IReadOnlyList<PortalPackageReviewMessageDto>> ListMessagesAsync(
        Guid tenantId, Guid sharedPackageId, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyList<PortalPackageReviewMessageDto>>(
                _messages.Where(item => item.TenantId == tenantId && item.SharedPackageId == sharedPackageId)
                    .Select(item => item.Message).OrderBy(item => item.CreatedAt).ToArray());
        }
    }

    public Task<PortalPackageReviewMessageDto> AddMessageAsync(
        Guid tenantId, Guid invitationId, Guid sharedPackageId, Guid packageId, Guid actorUserId,
        PortalCommentKind kind, string body, DateTimeOffset createdAt, CancellationToken cancellationToken = default)
    {
        var message = new PortalPackageReviewMessageDto(
            Guid.NewGuid(), kind, body, createdAt);
        lock (_gate) _messages.Add(new StoredMessage(tenantId, sharedPackageId, message));
        return Task.FromResult(message);
    }

    public Task<PortalPackageArtifactDto?> GetArtifactAsync(
        Guid tenantId, Guid packageId, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            return Task.FromResult(_artifacts.TryGetValue(packageId, out var artifact) && artifact.Package.TenantId == tenantId
                ? artifact
                : null);
        }
    }

    private sealed record StoredMessage(
        Guid TenantId, Guid SharedPackageId, PortalPackageReviewMessageDto Message);
}

public sealed class InMemoryPortalReviewPackagePreparationRepository(
    InMemoryPortalPackageRepository packageRepository) : IPortalReviewPackagePreparationRepository
{
    public Task<PreparedPortalReviewPackageDto> CreateAsync(
        PreparePortalReviewPackageRequest request, Guid tenantId, Guid actorUserId,
        DateTimeOffset generatedAt, CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid();
        var contractId = request.SourceType == PortalReviewPreparationSource.ContractObligationMatrix
            ? request.ContractId : null;
        var package = new PortalPackageDto(
            id, tenantId, contractId, request.Title, 1, PortalPackageStatus.Approved,
            request.Classification, false, [], generatedAt)
        {
            SourceKind = $"Report:{(request.SourceType == PortalReviewPreparationSource.ContractObligationMatrix ? "ContractObligationMatrix" : "AuditTrail")}",
            IsExternallySafe = true,
            DownloadAvailable = true
        };
        packageRepository.AddPreparedPackage(package,
            $"<main><h1>{System.Net.WebUtility.HtmlEncode(request.Title)}</h1><p>Development-only prepared package.</p></main>");
        return Task.FromResult(new PreparedPortalReviewPackageDto(
            id, request.SourceType, request.Title, request.Classification, contractId, [], generatedAt));
    }
}

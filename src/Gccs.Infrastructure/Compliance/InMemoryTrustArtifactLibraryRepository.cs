using System.Collections.Concurrent;
using Gccs.Application.Compliance;

namespace Gccs.Infrastructure.Compliance;

public sealed class InMemoryTrustArtifactLibraryRepository : ITrustArtifactLibraryRepository
{
    private readonly ConcurrentDictionary<Guid, List<TrustArtifactDto>> _records = new();

    public Task<TrustArtifactDto?> GetAsync(Guid tenantId, Guid artifactId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_records.GetOrAdd(tenantId, _ => []).SingleOrDefault(record => record.Id == artifactId));

    public Task<TrustArtifactDto> CreateAsync(Guid tenantId, CreateTrustArtifactRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var artifact = new TrustArtifactDto(Guid.NewGuid(), tenantId, request.ArtifactType, request.Owner.Trim(), request.Version.Trim(), TrustArtifactStatus.Draft, request.Audience, request.EffectiveDate, null, request.ExpirationDate, null, null, request.SourceFile.Trim(), request.NdaRequired, request.AllowedTenantTier.Trim(), request.AllowedEnvironment.Trim(), DateTimeOffset.UtcNow, null);
        _records.GetOrAdd(tenantId, _ => []).Add(artifact);
        return Task.FromResult(artifact);
    }

    public Task<TrustArtifactDto?> ChangeStatusAsync(Guid tenantId, Guid artifactId, TrustArtifactStatusRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var records = _records.GetOrAdd(tenantId, _ => []);
        var index = records.FindIndex(record => record.Id == artifactId);
        if (index < 0)
        {
            return Task.FromResult<TrustArtifactDto?>(null);
        }

        records[index] = records[index] with
        {
            Status = request.Status,
            ReviewDate = request.ReviewDate ?? records[index].ReviewDate,
            ReviewedBy = request.ReviewedBy ?? records[index].ReviewedBy,
            ApprovedBy = request.ApprovedBy ?? records[index].ApprovedBy,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        return Task.FromResult<TrustArtifactDto?>(records[index]);
    }

    public Task RecordShareAsync(Guid tenantId, Guid artifactId, TrustArtifactShareRequest request, Guid actorUserId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

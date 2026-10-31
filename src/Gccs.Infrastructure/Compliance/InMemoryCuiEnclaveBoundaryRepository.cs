using System.Collections.Concurrent;
using Gccs.Application.Compliance;

namespace Gccs.Infrastructure.Compliance;

public sealed class InMemoryCuiEnclaveBoundaryRepository : ICuiEnclaveBoundaryRepository
{
    private readonly ConcurrentDictionary<Guid, List<CuiEnclaveBoundaryDto>> _records = new();

    public Task<CuiEnclaveBoundaryDto?> GetAsync(Guid tenantId, Guid enclaveId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_records.GetOrAdd(tenantId, _ => []).SingleOrDefault(record => record.Id == enclaveId));

    public Task<CuiEnclaveBoundaryDto> CreateAsync(Guid tenantId, CreateCuiEnclaveBoundaryRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var enclave = new CuiEnclaveBoundaryDto(
            Guid.NewGuid(),
            tenantId,
            request.Environment,
            request.BoundaryDescription,
            request.DataHandlingMode,
            request.ApprovedWorkflows,
            request.StorageLocation,
            request.ComputeBoundary,
            request.NetworkRestrictions,
            request.LoggingDestination,
            request.BackupPolicy,
            request.SupportAccessModel,
            request.Reviewer,
            CuiEnclaveStatus.Draft,
            null,
            null,
            now,
            null);

        _records.GetOrAdd(tenantId, _ => []).Add(enclave);
        return Task.FromResult(enclave);
    }

    public Task<CuiEnclaveBoundaryDto?> ChangeStatusAsync(Guid tenantId, Guid enclaveId, CuiEnclaveStatusRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var records = _records.GetOrAdd(tenantId, _ => []);
        var index = records.FindIndex(record => record.Id == enclaveId);
        if (index < 0)
        {
            return Task.FromResult<CuiEnclaveBoundaryDto?>(null);
        }

        records[index] = records[index] with
        {
            Status = request.Status,
            LastActor = request.ActorName,
            LastNotes = request.Notes,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        return Task.FromResult<CuiEnclaveBoundaryDto?>(records[index]);
    }
}

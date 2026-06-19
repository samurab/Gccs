using System.Collections.Concurrent;
using Gccs.Application.Compliance;

namespace Gccs.Infrastructure.Compliance;

public sealed class InMemoryCuiEnclaveAccessControlRepository : ICuiEnclaveAccessControlRepository
{
    private readonly ConcurrentDictionary<Guid, List<CuiEnclaveSupportAccessDto>> _supportAccess = new();
    private readonly ConcurrentDictionary<Guid, List<CuiEnclaveEmergencyAccessDto>> _emergencyAccess = new();

    public Task<CuiEnclaveSupportAccessDto> CreateSupportAccessAsync(Guid tenantId, CuiEnclaveSupportAccessRequest request, CancellationToken cancellationToken = default)
    {
        var grantedAt = DateTimeOffset.UtcNow;
        var access = new CuiEnclaveSupportAccessDto(Guid.NewGuid(), tenantId, request.EnclaveId, request.Reason, request.Scope, request.Approver, grantedAt, grantedAt.AddMinutes(request.DurationMinutes), request.SessionLog, false);
        _supportAccess.GetOrAdd(tenantId, _ => []).Add(access);
        return Task.FromResult(access);
    }

    public Task<CuiEnclaveSupportAccessDto?> ExpireSupportAccessAsync(Guid tenantId, Guid accessId, CancellationToken cancellationToken = default)
    {
        var records = _supportAccess.GetOrAdd(tenantId, _ => []);
        var index = records.FindIndex(record => record.Id == accessId);
        if (index < 0)
        {
            return Task.FromResult<CuiEnclaveSupportAccessDto?>(null);
        }

        records[index] = records[index] with { Expired = true, ExpiresAt = DateTimeOffset.UtcNow };
        return Task.FromResult<CuiEnclaveSupportAccessDto?>(records[index]);
    }

    public Task<CuiEnclaveExportDto> CreateExportAsync(Guid tenantId, CuiEnclaveExportRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(new CuiEnclaveExportDto(Guid.NewGuid(), tenantId, request.EnclaveId, request.PackageType, request.Recipient, request.Watermarked, request.Encrypted, DateTimeOffset.UtcNow));

    public Task<CuiEnclaveEmergencyAccessDto> CreateEmergencyAccessAsync(Guid tenantId, CuiEnclaveEmergencyAccessRequest request, CancellationToken cancellationToken = default)
    {
        var grantedAt = DateTimeOffset.UtcNow;
        var access = new CuiEnclaveEmergencyAccessDto(Guid.NewGuid(), tenantId, request.EnclaveId, request.IncidentId, request.Approver, grantedAt, grantedAt.AddMinutes(request.DurationMinutes), request.PostAccessReviewRequired, null);
        _emergencyAccess.GetOrAdd(tenantId, _ => []).Add(access);
        return Task.FromResult(access);
    }

    public Task<CuiEnclaveEmergencyAccessDto?> CompleteEmergencyReviewAsync(Guid tenantId, Guid accessId, string reviewer, CancellationToken cancellationToken = default)
    {
        var records = _emergencyAccess.GetOrAdd(tenantId, _ => []);
        var index = records.FindIndex(record => record.Id == accessId);
        if (index < 0)
        {
            return Task.FromResult<CuiEnclaveEmergencyAccessDto?>(null);
        }

        records[index] = records[index] with { PostAccessReviewer = reviewer };
        return Task.FromResult<CuiEnclaveEmergencyAccessDto?>(records[index]);
    }
}

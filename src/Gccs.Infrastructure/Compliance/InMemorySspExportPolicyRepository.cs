using System.Collections.Concurrent;
using Gccs.Application.Compliance;

namespace Gccs.Infrastructure.Compliance;

public sealed class InMemorySspExportPolicyRepository : ISspExportPolicyRepository
{
    private readonly ConcurrentDictionary<Guid, SspExportPolicyDto> policies = new();

    public Task<SspExportPolicyDto> GetAsync(Guid tenantId, bool lockForDecision, CancellationToken cancellationToken = default) =>
        Task.FromResult(policies.TryGetValue(tenantId, out var policy) ? policy : new SspExportPolicyDto(true, 0, null, null));

    public Task<SspExportPolicyDto> UpdateAsync(Guid tenantId, bool requireIndependentApproval, long expectedVersion, Guid actorUserId, DateTimeOffset updatedAt, CancellationToken cancellationToken = default)
    {
        lock (policies)
        {
            var current = policies.TryGetValue(tenantId, out var existing) ? existing : new SspExportPolicyDto(true, 0, null, null);
            if (current.Version != expectedVersion)
                throw new SspExportPackageValidationException("The SSP export policy changed. Reload the policy and retry.");
            var updated = new SspExportPolicyDto(requireIndependentApproval, current.Version + 1, updatedAt, actorUserId);
            policies[tenantId] = updated;
            return Task.FromResult(updated);
        }
    }
}

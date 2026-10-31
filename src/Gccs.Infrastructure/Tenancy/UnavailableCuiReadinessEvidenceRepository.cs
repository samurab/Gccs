using Gccs.Application.Tenancy;

namespace Gccs.Infrastructure.Tenancy;

// Keeps non-database test/demo hosts composable without granting readiness approval.
public sealed class UnavailableCuiReadinessEvidenceRepository : ICuiReadinessEvidenceRepository
{
    public Task LockTenantAsync(Guid tenantId, CancellationToken ct) => Task.CompletedTask;
    public Task<IReadOnlyList<CuiReadinessEvidenceDto>> ListAsync(Guid tenantId, CancellationToken ct) => throw Unavailable();
    public Task<CuiReadinessEvidenceDto> RecordAsync(Guid tenantId, Guid actor, RecordCuiReadinessEvidenceRequest request, CancellationToken ct) => throw Unavailable();
    public Task<IReadOnlyList<CuiReadinessSupportingRecord>> SourcesAsync(Guid tenantId, CancellationToken ct) => throw Unavailable();
    public Task<string?> ValidateLinksAsync(Guid tenantId, CuiReadyApprovalChecklistDto checklist, CancellationToken ct) =>
        Task.FromResult<string?>("Supporting evidence verification requires configured persistence.");
    private static CuiReadyApprovalChecklistValidationException Unavailable() => new("Readiness evidence persistence is unavailable.");
}

using Gccs.Application.Labor;
using Gccs.Application.Tenancy;
using Gccs.Domain.Common;

namespace Gccs.Infrastructure.Labor;

public sealed class TenantLaborWageDeterminationUploadGuard(TenantDataHandlingModePolicyService policy)
    : ILaborWageDeterminationUploadGuard
{
    public Task EnsureAllowedAsync(
        WageDeterminationUploadRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default) =>
        policy.EnsureAllowedAsync(
            new TenantDataHandlingModePolicyRequest(
                TenantDataHandlingWorkflow.EvidenceUpload,
                request.ContainsPotentialCui || request.Classification is ContentClassification.Cui,
                ContainsSyntheticCui: request.Classification is ContentClassification.SyntheticCui,
                ClassificationConfirmed: request.Classification is not ContentClassification.Unknown,
                EntityType: "WageDetermination",
                EntityId: request.ContractId.ToString()),
            actorUserId,
            cancellationToken);
}

using Gccs.Application.Common;
using Gccs.Application.Security;
using Gccs.Domain.Tenancy;

namespace Gccs.Application.Tenancy;

public sealed class CuiReadinessNoticeService(DataHandlingNoticeService notices, DataHandlingNoticePackage package,
    DataHandlingNoticeAcknowledgementService acknowledgements, ICuiReadinessEvidenceRepository evidence,
    ICurrentTenantContext context, IApplicationTransaction transaction)
{
    public async Task<DataHandlingNoticeDto> GetAsync(CancellationToken ct) =>
        await notices.GetPublishedAsync(package.Root, TenantDataPosture.CuiReady, "Onboarding", ct)
        ?? throw new DataHandlingNoticeAcknowledgementRequiredException("No current CuiReady onboarding notice is published.");
    public Task<DataHandlingNoticeAcknowledgementDto> AcknowledgeAsync(AcknowledgeDataHandlingNoticeRequest request, CancellationToken ct) =>
        transaction.ExecuteAsync(async token =>
        {
            await evidence.LockTenantAsync(context.TenantId, token);
            return await acknowledgements.AcknowledgeReadinessAsync(context.TenantId, context.UserId,
                await GetAsync(token), request, token);
        }, ct);
}

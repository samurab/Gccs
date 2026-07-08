using Gccs.Application.Security;

namespace Gccs.Application.NoCui;

public sealed class NoCuiAcknowledgementStatusService(
    INoCuiAcknowledgementRepository repository,
    ICurrentTenantContext tenantContext)
{
    public async Task<NoCuiAcknowledgementStatusDto> GetCurrentStatusAsync(
        CancellationToken cancellationToken = default)
    {
        var acknowledgement = await repository.FindCurrentUserAcknowledgementAsync(
            NoCuiNotice.CurrentVersion,
            cancellationToken);

        return acknowledgement ?? new NoCuiAcknowledgementStatusDto(
            false,
            NoCuiNotice.CurrentVersion,
            NoCuiNotice.Copy,
            tenantContext.TenantId,
            null,
            null);
    }
}

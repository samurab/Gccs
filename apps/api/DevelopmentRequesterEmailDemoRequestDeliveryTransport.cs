using Gccs.Application.Marketing;
using Gccs.Infrastructure.Marketing;
using Microsoft.Extensions.Options;

namespace Gccs.Api;

public sealed class DevelopmentRequesterEmailDemoRequestDeliveryTransport(
    IOptions<DemoRequestOptions> options,
    IHostEnvironment environment,
    AzureCommunicationDemoRequestEmailSender emailSender) : IDemoRequestDeliveryTransport
{
    public bool IsConfigured =>
        environment.IsDevelopment() &&
        options.Value.Enabled &&
        string.Equals(
            options.Value.Provider,
            DemoRequestOptions.DevelopmentRequesterEmailProvider,
            StringComparison.OrdinalIgnoreCase) &&
        emailSender.IsConfigured;

    public Task<DemoRequestDeliveryResult> DeliverAsync(
        ClaimedDemoRequestDelivery request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Development requester email delivery is not configured.");
        }

        return AzureCommunicationDemoRequestEmailSender.IsRequesterEmail(request.DeliveryKind)
            ? emailSender.DeliverAsync(request, cancellationToken)
            : Task.FromResult(new DemoRequestDeliveryResult(DemoRequestDeliveryDisposition.Captured));
    }
}

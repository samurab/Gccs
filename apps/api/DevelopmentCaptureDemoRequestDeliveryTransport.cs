using Gccs.Application.Marketing;
using Gccs.Infrastructure.Marketing;
using Microsoft.Extensions.Options;

namespace Gccs.Api;

public sealed class DevelopmentCaptureDemoRequestDeliveryTransport(
    IOptions<DemoRequestOptions> options,
    IHostEnvironment environment) : IDemoRequestDeliveryTransport
{
    public bool IsConfigured =>
        environment.IsDevelopment() &&
        options.Value.Enabled &&
        string.Equals(
            options.Value.Provider,
            DemoRequestOptions.DevelopmentCaptureProvider,
            StringComparison.OrdinalIgnoreCase);

    public Task<DemoRequestDeliveryResult> DeliverAsync(
        ClaimedDemoRequestDelivery request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Development demo-request capture is not configured.");
        }

        return Task.FromResult(new DemoRequestDeliveryResult(DemoRequestDeliveryDisposition.Captured));
    }
}

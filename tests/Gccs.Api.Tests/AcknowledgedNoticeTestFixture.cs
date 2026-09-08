using Gccs.Application.Tenancy;
using Microsoft.Extensions.DependencyInjection;

namespace Gccs.Api.Tests;

internal static class AcknowledgedNoticeTestFixture
{
    // Legacy feature tests isolate their own behavior with consent already satisfied.
    // EvidenceFileUploadTests and the Phase 1A notice/policy suites use the real guard,
    // including missing/outdated consent, tenant isolation and PostgreSQL rollback.
    internal static void AddAcknowledgedNoticeFixture(this IServiceCollection services) =>
        services.AddScoped<ICurrentDataHandlingNoticeGuard, AcknowledgedNoticeGuard>();

    private sealed class AcknowledgedNoticeGuard : ICurrentDataHandlingNoticeGuard
    {
        public Task EnsureAsync(string workflow, Guid actorUserId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }
}

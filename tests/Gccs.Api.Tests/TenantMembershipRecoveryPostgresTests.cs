using System.Net;
using System.Net.Http.Json;
using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Application.Identity;
using Gccs.Application.Security;
using Gccs.Domain.Audit;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Common;
using Gccs.Infrastructure.Identity;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class TenantMembershipRecoveryPostgresTests
{
    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task Audit_failure_rolls_back_membership_deactivation()
    {
        var options = Options();
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var targetMembershipId = Guid.NewGuid();

        await SeedAsync(
            options,
            [Tenant(tenantId)],
            [User(actorUserId, tenantId, "recovery-admin"), User(targetUserId, tenantId, "recovery-contributor")],
            [
                Membership(Guid.NewGuid(), tenantId, actorUserId, RoleCatalog.Admin),
                Membership(targetMembershipId, tenantId, targetUserId, RoleCatalog.Contributor)
            ]);

        try
        {
            await using var harness = CreateHarness(options, tenantId, actorUserId, new FailingAuditWriter());

            await Assert.ThrowsAsync<AuditWriteException>(() => harness.Service.UpdateStatusAsync(
                targetMembershipId,
                new UpdateTenantMembershipStatusRequest(MembershipStatus.Deactivated, "Synthetic rollback verification."),
                actorUserId));

            await using var verification = new GccsDbContext(options);
            Assert.Equal(
                MembershipStatus.Active,
                (await verification.TenantMemberships.AsNoTracking().SingleAsync(item => item.Id == targetMembershipId)).Status);
            Assert.False(await verification.AuditLogEntries.AnyAsync(item => item.TenantId == tenantId));
        }
        finally
        {
            await CleanupAsync(options, tenantId);
        }
    }

    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task Rejected_deactivations_leave_memberships_and_audit_unchanged()
    {
        var options = Options();
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var contributorUserId = Guid.NewGuid();
        var otherTenantUserId = Guid.NewGuid();
        var ownerMembershipId = Guid.NewGuid();
        var adminMembershipId = Guid.NewGuid();
        var contributorMembershipId = Guid.NewGuid();
        var otherTenantMembershipId = Guid.NewGuid();

        await SeedAsync(
            options,
            [Tenant(tenantId), Tenant(otherTenantId)],
            [
                User(actorUserId, tenantId, "recovery-admin"),
                User(ownerUserId, tenantId, "recovery-owner"),
                User(contributorUserId, tenantId, "recovery-contributor"),
                User(otherTenantUserId, otherTenantId, "recovery-other-tenant")
            ],
            [
                Membership(adminMembershipId, tenantId, actorUserId, RoleCatalog.Admin),
                Membership(ownerMembershipId, tenantId, ownerUserId, RoleCatalog.Owner),
                Membership(contributorMembershipId, tenantId, contributorUserId, RoleCatalog.Contributor),
                Membership(otherTenantMembershipId, otherTenantId, otherTenantUserId, RoleCatalog.Contributor)
            ]);

        try
        {
            await using var harness = CreateHarness(options, tenantId, actorUserId);

            await Assert.ThrowsAsync<TenantMembershipStatusChangeDeniedException>(() => harness.Service.UpdateStatusAsync(
                ownerMembershipId,
                new UpdateTenantMembershipStatusRequest(MembershipStatus.Deactivated, "Synthetic owner rejection."),
                actorUserId));
            await Assert.ThrowsAsync<TenantMembershipStatusChangeDeniedException>(() => harness.Service.UpdateStatusAsync(
                adminMembershipId,
                new UpdateTenantMembershipStatusRequest(MembershipStatus.Deactivated, "Synthetic last-admin rejection."),
                actorUserId));
            Assert.Null(await harness.Service.UpdateStatusAsync(
                otherTenantMembershipId,
                new UpdateTenantMembershipStatusRequest(MembershipStatus.Deactivated, "Synthetic cross-tenant rejection."),
                actorUserId));
            await Assert.ThrowsAsync<ArgumentException>(() => harness.Service.UpdateStatusAsync(
                contributorMembershipId,
                new UpdateTenantMembershipStatusRequest(MembershipStatus.Deactivated),
                actorUserId));

            await using var verification = new GccsDbContext(options);
            var statuses = await verification.TenantMemberships
                .AsNoTracking()
                .Where(item => item.TenantId == tenantId || item.TenantId == otherTenantId)
                .Select(item => item.Status)
                .ToArrayAsync();
            Assert.All(statuses, status => Assert.Equal(MembershipStatus.Active, status));
            Assert.False(await verification.AuditLogEntries.AnyAsync(
                item => item.TenantId == tenantId || item.TenantId == otherTenantId));
        }
        finally
        {
            await CleanupAsync(options, tenantId, otherTenantId);
        }
    }

    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task Concurrent_admin_deactivation_requests_retain_one_active_admin_and_return_conflict()
    {
        var options = Options();
        var tenantId = Guid.NewGuid();
        var firstAdminUserId = Guid.NewGuid();
        var secondAdminUserId = Guid.NewGuid();
        var actorUserId = firstAdminUserId;
        var firstMembershipId = Guid.NewGuid();
        var secondMembershipId = Guid.NewGuid();

        await SeedAsync(
            options,
            [Tenant(tenantId)],
            [User(firstAdminUserId, tenantId, "recovery-admin-one"), User(secondAdminUserId, tenantId, "recovery-admin-two")],
            [
                Membership(firstMembershipId, tenantId, firstAdminUserId, RoleCatalog.Admin),
                Membership(secondMembershipId, tenantId, secondAdminUserId, RoleCatalog.Admin)
            ]);

        try
        {
            var barrier = new TwoTransactionBarrier();
            await using var factory = CreatePostgresFactory(barrier);
            using var firstClient = factory.CreateClient();
            using var secondClient = factory.CreateClient();

            var responses = await Task.WhenAll(
                firstClient.SendAsync(StatusRequest(tenantId, actorUserId, firstMembershipId)),
                secondClient.SendAsync(StatusRequest(tenantId, actorUserId, secondMembershipId)));

            Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
            var conflict = Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);
            Assert.Contains(
                "membership_status_change_conflict",
                await conflict.Content.ReadAsStringAsync(),
                StringComparison.Ordinal);

            await using var verification = new GccsDbContext(options);
            var memberships = await verification.TenantMemberships
                .AsNoTracking()
                .Where(item => item.TenantId == tenantId && item.RoleName == RoleCatalog.Admin)
                .ToArrayAsync();
            Assert.Equal(2, memberships.Length);
            Assert.Single(memberships, item => item.Status == MembershipStatus.Active);
            Assert.Single(memberships, item => item.Status == MembershipStatus.Deactivated);
            Assert.Equal(
                1,
                await verification.AuditLogEntries.CountAsync(item =>
                    item.TenantId == tenantId &&
                    item.EntityType == "TenantMembership" &&
                    item.Action == AuditAction.Updated));
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }
        finally
        {
            await CleanupAsync(options, tenantId);
        }
    }

    private static DbContextOptions<GccsDbContext> Options()
    {
        return new DbContextOptionsBuilder<GccsDbContext>().UseGccsPostgres(ConnectionString()).Options;
    }

    private static string ConnectionString() =>
        Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION") ??
        throw new InvalidOperationException("Set GCCS_TEST_POSTGRES_CONNECTION to run the PostgreSQL recovery tests.");

    private static WebApplicationFactory<Program> CreatePostgresFactory(TwoTransactionBarrier barrier) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<GccsDbContext>();
                services.RemoveAll<DbContextOptions<GccsDbContext>>();
                services.RemoveAll<ITenantMembershipRepository>();
                services.AddDbContext<GccsDbContext>(options => options.UseGccsPostgres(ConnectionString()));
                services.AddScoped<ITenantMembershipRepository>(provider =>
                    new BarrierTenantMembershipRepository(
                        new EfTenantMembershipRepository(
                            provider.GetRequiredService<GccsDbContext>(),
                            provider.GetRequiredService<ICurrentTenantContext>()),
                        barrier));
            });
        });

    private static HttpRequestMessage StatusRequest(
        Guid tenantId,
        Guid actorUserId,
        Guid membershipId)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/tenant-members/{membershipId}/status")
        {
            Content = JsonContent.Create(new UpdateTenantMembershipStatusRequest(
                MembershipStatus.Deactivated,
                "Synthetic concurrent admin removal."))
        };
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", actorUserId.ToString());
        request.Headers.Add("X-Gccs-Dev-Permissions", Permission.ManageUsers.ToString());
        return request;
    }

    private static async Task SeedAsync(
        DbContextOptions<GccsDbContext> options,
        TenantEntity[] tenants,
        UserEntity[] users,
        TenantMembershipEntity[] memberships)
    {
        await using var setup = new GccsDbContext(options);
        await PostgresTestDatabase.MigrateAsync(setup);
        setup.Tenants.AddRange(tenants);
        setup.Users.AddRange(users);
        setup.TenantMemberships.AddRange(memberships);
        await setup.SaveChangesAsync();
    }

    private static Harness CreateHarness(
        DbContextOptions<GccsDbContext> options,
        Guid tenantId,
        Guid actorUserId,
        IAuditEventWriter? auditWriter = null)
    {
        var db = new GccsDbContext(options);
        var provider = new ServiceCollection().AddSingleton(db).BuildServiceProvider();
        var tenantContext = new FixedTenantContext(tenantId, actorUserId);
        ITenantMembershipRepository repository = new EfTenantMembershipRepository(db, tenantContext);

        auditWriter ??= new EfAuditEventWriter(
            db,
            new StaticAuditRequestMetadata("127.0.0.1", "membership-recovery-postgres-test", Guid.NewGuid().ToString()));
        var service = new TenantMembershipService(
            repository,
            tenantContext,
            auditWriter,
            new EfApplicationTransaction(provider));
        return new Harness(service, db, provider);
    }

    private static async Task CleanupAsync(DbContextOptions<GccsDbContext> options, params Guid[] tenantIds)
    {
        await using var cleanup = new GccsDbContext(options);
        await cleanup.AuditLogEntries.Where(item => tenantIds.Contains(item.TenantId)).ExecuteDeleteAsync();
        await cleanup.TenantMemberships.Where(item => tenantIds.Contains(item.TenantId)).ExecuteDeleteAsync();
        await cleanup.Users.Where(item => tenantIds.Contains(item.TenantId)).ExecuteDeleteAsync();
        await cleanup.Tenants.Where(item => tenantIds.Contains(item.Id)).ExecuteDeleteAsync();
    }

    private static TenantEntity Tenant(Guid tenantId) => new()
    {
        Id = tenantId,
        Name = $"Membership recovery tenant {tenantId:N}",
        Status = TenantStatus.Active,
        DataPosture = TenantDataPosture.NoCui,
        CreatedAt = DateTimeOffset.UtcNow
    };

    private static UserEntity User(Guid userId, Guid tenantId, string name) => new()
    {
        Id = userId,
        TenantId = tenantId,
        Email = $"{name}-{userId:N}@example.invalid",
        DisplayName = name,
        Status = UserStatus.Active,
        MfaEnabled = true,
        CreatedAt = DateTimeOffset.UtcNow
    };

    private static TenantMembershipEntity Membership(
        Guid membershipId,
        Guid tenantId,
        Guid userId,
        string roleName) => new()
    {
        Id = membershipId,
        TenantId = tenantId,
        UserId = userId,
        Status = MembershipStatus.Active,
        RoleName = roleName,
        CreatedAt = DateTimeOffset.UtcNow
    };

    private sealed record FixedTenantContext(Guid TenantId, Guid UserId) : ICurrentTenantContext
    {
        public string UserEmail => "membership-recovery@example.invalid";
    }

    private sealed record StaticAuditRequestMetadata(
        string IpAddress,
        string UserAgent,
        string CorrelationId) : IAuditRequestMetadata;

    private sealed class FailingAuditWriter : IAuditEventWriter
    {
        public Task WriteAsync(
            Guid tenantId,
            Guid actorUserId,
            AuditAction action,
            string entityType,
            string entityId,
            string summary,
            IReadOnlyDictionary<string, string>? metadata = null,
            CancellationToken cancellationToken = default) =>
            throw new AuditWriteException("Synthetic membership audit failure.");
    }

    private sealed class TwoTransactionBarrier
    {
        private readonly TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int arrivals;

        public async Task SignalAndWaitAsync(CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref arrivals) == 2)
            {
                completion.TrySetResult();
            }

            await completion.Task.WaitAsync(cancellationToken);
        }
    }

    private sealed class BarrierTenantMembershipRepository(
        ITenantMembershipRepository inner,
        TwoTransactionBarrier barrier) : ITenantMembershipRepository
    {
        public Task<IReadOnlyList<TenantMemberDto>> ListCurrentTenantMembersAsync(
            CancellationToken cancellationToken = default) =>
            inner.ListCurrentTenantMembersAsync(cancellationToken);

        public Task<bool> CurrentTenantMembershipExistsAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            inner.CurrentTenantMembershipExistsAsync(userId, cancellationToken);

        public Task<TenantAuthorizationContextDto?> FindActiveCurrentUserAuthorizationAsync(
            CancellationToken cancellationToken = default) =>
            inner.FindActiveCurrentUserAuthorizationAsync(cancellationToken);

        public Task<TenantMemberDto> AddToCurrentTenantAsync(
            User user,
            TenantMembership membership,
            CancellationToken cancellationToken = default) =>
            inner.AddToCurrentTenantAsync(user, membership, cancellationToken);

        public async Task<TenantMemberDto?> UpdateStatusInCurrentTenantScopeAsync(
            Guid membershipId,
            MembershipStatus status,
            Guid actorUserId,
            CancellationToken cancellationToken = default)
        {
            await barrier.SignalAndWaitAsync(cancellationToken);
            return await inner.UpdateStatusInCurrentTenantScopeAsync(
                membershipId,
                status,
                actorUserId,
                cancellationToken);
        }
    }

    private sealed record Harness(
        TenantMembershipService Service,
        GccsDbContext Db,
        ServiceProvider Provider) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await Provider.DisposeAsync();
        }
    }
}

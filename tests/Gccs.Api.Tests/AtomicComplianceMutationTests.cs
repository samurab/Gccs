using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Gccs.Application.Audit;
using Gccs.Application.Companies;
using Gccs.Application.Common;
using Gccs.Application.Identity;
using Gccs.Domain.Audit;
using Gccs.Domain.Companies;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class AtomicComplianceMutationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public AtomicComplianceMutationTests(WebApplicationFactory<Program> factory) => this.factory = factory;

    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task Audit_failure_rolls_back_business_mutation_and_prior_audit_append()
    {
        var tenantId = Guid.NewGuid();
        await using var testFactory = CreatePostgresFactory<FailOnSecondAuditEventWriter>(dbContext =>
            dbContext.Tenants.Add(Tenant(tenantId, "Atomic company profile tenant")));

        try
        {
            using var client = testFactory.CreateClient();
            using var request = AuthenticatedRequest(
                HttpMethod.Put,
                "/api/company-profile",
                tenantId,
                Guid.NewGuid(),
                "profile.owner@example.com",
                Permission.ManageCompanyProfile,
                new UpsertCompanyProfileRequest(
                    "Atomic Profile", null, null, null, null, [], [], [], ContractorRole.Unknown,
                    string.Empty, CompanyRange.Unknown, CompanyRange.Unknown, [],
                    new ItEnvironmentSummaryDto(string.Empty, false, null, []),
                    DataHandlingPosture.Unknown, false));

            using var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Contains("audit_write_failed", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
            using var scope = testFactory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
            Assert.False(await dbContext.CompanyProfiles.AnyAsync(profile => profile.TenantId == tenantId));
            Assert.False(await dbContext.AuditLogEntries.AnyAsync(audit => audit.TenantId == tenantId));
        }
        finally
        {
            await DeleteTenantAsync(testFactory, tenantId);
        }
    }

    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task Invitation_acceptance_audit_failure_rolls_back_claim_user_and_membership()
    {
        var tenantId = Guid.NewGuid();
        var invitationId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var invitationValue = $"atomic-invitation-{Guid.NewGuid():N}";
        const string email = "new.owner@example.com";
        await using var testFactory = CreatePostgresFactory<AlwaysFailAuditEventWriter>(dbContext =>
        {
            dbContext.Tenants.Add(Tenant(tenantId, "Atomic invitation tenant"));
            dbContext.TenantInvitations.Add(new TenantInvitationEntity
            {
                Id = invitationId,
                TenantId = tenantId,
                Email = email,
                RoleName = "Owner",
                InvitationTokenHash = HashToken(invitationValue),
                Status = TenantInvitationStatus.Pending,
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
                DeliveryStatus = InvitationDeliveryStatus.Sent,
                NotificationSentAt = DateTimeOffset.UtcNow,
                NotificationPlaceholder = "Invitation sent.",
                CreatedAt = DateTimeOffset.UtcNow
            });
        });

        try
        {
            using var client = testFactory.CreateClient();
            using var request = AuthenticatedRequest(
                HttpMethod.Post,
                $"/api/invitations/{invitationValue}/accept",
                tenantId,
                actorUserId,
                email,
                Permission.ManageUsers,
                new AcceptTenantInvitationRequest("New Owner"));

            using var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            using var scope = testFactory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
            var invitation = await dbContext.TenantInvitations.SingleAsync(candidate => candidate.Id == invitationId);
            Assert.Equal(TenantInvitationStatus.Pending, invitation.Status);
            Assert.Equal(HashToken(invitationValue), invitation.InvitationTokenHash);
            Assert.False(await dbContext.Users.AnyAsync(user => user.TenantId == tenantId));
            Assert.False(await dbContext.TenantMemberships.AnyAsync(membership => membership.TenantId == tenantId));
            Assert.False(await dbContext.AuditLogEntries.AnyAsync(audit => audit.TenantId == tenantId));
        }
        finally
        {
            await DeleteTenantAsync(testFactory, tenantId);
        }
    }

    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task Rejected_attempt_audit_survives_business_transaction_rollback()
    {
        var tenantId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        await using var testFactory = CreatePostgresFactory<EfAuditEventWriter>(dbContext =>
            dbContext.Tenants.Add(Tenant(tenantId, "Rejected attempt audit tenant")));

        try
        {
            using (var scope = testFactory.Services.CreateScope())
            {
                var transaction = scope.ServiceProvider.GetRequiredService<IApplicationTransaction>();
                var audit = scope.ServiceProvider.GetRequiredService<IAuditEventWriter>();
                await Assert.ThrowsAsync<ContentClassificationValidationException>(() =>
                    transaction.ExecuteAsync<bool>(async cancellationToken =>
                    {
                        await audit.WriteAsync(
                            tenantId,
                            actorUserId,
                            AuditAction.Rejected,
                            "EvidenceUploadIntent",
                            Guid.NewGuid().ToString(),
                            "A prohibited evidence upload attempt was rejected.",
                            cancellationToken: cancellationToken);
                        throw new ContentClassificationValidationException("Synthetic prohibited classification.");
                    }));
            }

            using var verificationScope = testFactory.Services.CreateScope();
            var dbContext = verificationScope.ServiceProvider.GetRequiredService<GccsDbContext>();
            var rejection = await dbContext.AuditLogEntries.SingleAsync(audit => audit.TenantId == tenantId);
            Assert.Equal(AuditAction.Rejected, rejection.Action);
            Assert.Equal("EvidenceUploadIntent", rejection.EntityType);
            Assert.Equal(actorUserId, rejection.ActorUserId);
        }
        finally
        {
            await DeleteTenantAsync(testFactory, tenantId);
        }
    }

    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task Concurrent_user_conflict_returns_409_without_committing_invitation_claim()
    {
        var connectionString = Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION")
            ?? throw new InvalidOperationException("GCCS_TEST_POSTGRES_CONNECTION is required.");
        var tenantId = Guid.NewGuid();
        var invitationId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var invitationValue = $"concurrent-invitation-{Guid.NewGuid():N}";
        const string email = "concurrent.owner@example.com";
        var conflict = new InvitationUserConflictInterceptor(connectionString, tenantId, email);
        await using var testFactory = CreatePostgresFactory<EfAuditEventWriter>(dbContext =>
        {
            dbContext.Tenants.Add(Tenant(tenantId, "Concurrent invitation tenant"));
            dbContext.TenantInvitations.Add(new TenantInvitationEntity
            {
                Id = invitationId,
                TenantId = tenantId,
                Email = email,
                RoleName = "Owner",
                InvitationTokenHash = HashToken(invitationValue),
                Status = TenantInvitationStatus.Pending,
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
                DeliveryStatus = InvitationDeliveryStatus.Sent,
                NotificationSentAt = DateTimeOffset.UtcNow,
                NotificationPlaceholder = "Invitation sent.",
                CreatedAt = DateTimeOffset.UtcNow
            });
        }, conflict);

        try
        {
            using var client = testFactory.CreateClient();
            conflict.Enabled = true;
            using var request = AuthenticatedRequest(
                HttpMethod.Post,
                $"/api/invitations/{invitationValue}/accept",
                tenantId,
                actorUserId,
                email,
                Permission.ManageUsers,
                new AcceptTenantInvitationRequest("Losing Concurrent Owner"));

            using var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            using var scope = testFactory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
            var invitation = await dbContext.TenantInvitations.SingleAsync(candidate => candidate.Id == invitationId);
            Assert.Equal(TenantInvitationStatus.Pending, invitation.Status);
            Assert.Equal(HashToken(invitationValue), invitation.InvitationTokenHash);
            Assert.Single(await dbContext.Users.Where(user => user.TenantId == tenantId && user.Email == email).ToArrayAsync());
            Assert.False(await dbContext.TenantMemberships.AnyAsync(membership => membership.TenantId == tenantId));
            Assert.False(await dbContext.AuditLogEntries.AnyAsync(audit => audit.TenantId == tenantId));
        }
        finally
        {
            await DeleteTenantAsync(testFactory, tenantId);
        }
    }

    private WebApplicationFactory<Program> CreatePostgresFactory<TAuditWriter>(
        Action<GccsDbContext> seed,
        SaveChangesInterceptor? interceptor = null)
        where TAuditWriter : class, IAuditEventWriter =>
        factory.WithWebHostBuilder(builder =>
        {
            var connectionString = Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION")
                ?? throw new InvalidOperationException("GCCS_TEST_POSTGRES_CONNECTION is required.");
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<GccsDbContext>();
                services.RemoveAll<DbContextOptions<GccsDbContext>>();
                services.RemoveAll<IAuditEventWriter>();
                services.AddDbContext<GccsDbContext>(options =>
                {
                    options.UseGccsPostgres(connectionString);
                    if (interceptor is not null) options.AddInterceptors(interceptor);
                });
                services.AddScoped<IAuditEventWriter, TAuditWriter>();

                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
                PostgresTestDatabase.Migrate(dbContext);
                seed(dbContext);
                dbContext.SaveChanges();
            });
        });

    private static HttpRequestMessage AuthenticatedRequest<T>(
        HttpMethod method,
        string path,
        Guid tenantId,
        Guid userId,
        string email,
        Permission permission,
        T body)
    {
        var request = new HttpRequestMessage(method, path) { Content = JsonContent.Create(body) };
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", userId.ToString());
        request.Headers.Add("X-Gccs-Dev-Email", email);
        request.Headers.Add("X-Gccs-Dev-Permissions", permission.ToString());
        return request;
    }

    private static TenantEntity Tenant(Guid id, string name) => new()
    {
        Id = id,
        Name = name,
        Status = TenantStatus.Active,
        DataPosture = TenantDataPosture.NoCui,
        CreatedAt = DateTimeOffset.UtcNow
    };

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static async Task DeleteTenantAsync(WebApplicationFactory<Program> testFactory, Guid tenantId)
    {
        using var scope = testFactory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        await dbContext.TenantInvitations
            .Where(invitation => invitation.TenantId == tenantId)
            .ExecuteDeleteAsync();
        await dbContext.Users
            .Where(user => user.TenantId == tenantId)
            .ExecuteDeleteAsync();
        var tenant = await dbContext.Tenants.SingleOrDefaultAsync(candidate => candidate.Id == tenantId);
        if (tenant is null) return;
        dbContext.Tenants.Remove(tenant);
        await dbContext.SaveChangesAsync();
    }

    private sealed class FailOnSecondAuditEventWriter(
        GccsDbContext dbContext,
        IAuditRequestMetadata requestMetadata) : IAuditEventWriter
    {
        private readonly EfAuditEventWriter inner = new(dbContext, requestMetadata);
        private int writeCount;

        public Task WriteAsync(
            Guid tenantId, Guid actorUserId, AuditAction action, string entityType, string entityId,
            string summary, IReadOnlyDictionary<string, string>? metadata = null,
            CancellationToken cancellationToken = default) =>
            Interlocked.Increment(ref writeCount) == 2
                ? throw new AuditWriteException("Synthetic failure after the first audit append.")
                : inner.WriteAsync(tenantId, actorUserId, action, entityType, entityId, summary, metadata, cancellationToken);
    }

    private sealed class AlwaysFailAuditEventWriter : IAuditEventWriter
    {
        public Task WriteAsync(
            Guid tenantId, Guid actorUserId, AuditAction action, string entityType, string entityId,
            string summary, IReadOnlyDictionary<string, string>? metadata = null,
            CancellationToken cancellationToken = default) =>
            throw new AuditWriteException("Synthetic audit persistence failure.");
    }

    private sealed class InvitationUserConflictInterceptor(
        string connectionString,
        Guid tenantId,
        string email) : SaveChangesInterceptor
    {
        private int inserted;
        public bool Enabled { get; set; }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (!Enabled ||
                Interlocked.CompareExchange(ref inserted, 1, 0) != 0 ||
                eventData.Context?.ChangeTracker.Entries<UserEntity>().Any(entry =>
                    entry.State == EntityState.Added &&
                    entry.Entity.TenantId == tenantId &&
                    entry.Entity.Email == email) != true)
            {
                return result;
            }

            var options = new DbContextOptionsBuilder<GccsDbContext>()
                .UseGccsPostgres(connectionString)
                .Options;
            await using var competingContext = new GccsDbContext(options);
            competingContext.Users.Add(new UserEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Email = email,
                DisplayName = "Winning Concurrent User",
                Status = UserStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await competingContext.SaveChangesAsync(cancellationToken);
            return result;
        }
    }
}

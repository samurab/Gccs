using Gccs.Application.Audit;
using Gccs.Application.Compliance;
using Gccs.Domain.Audit;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Compliance;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class FedRampPersistenceTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public FedRampPersistenceTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Durable_repositories_survive_context_restart_and_enforce_tenant_scope()
    {
        var databaseName = $"fedramp-persistence-{Guid.NewGuid():N}";
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        await using var provider = CreateInMemoryProvider(databaseName);
        await SeedTenantsAsync(provider, tenantA, tenantB);

        Guid mappingId;
        await using (var scope = provider.CreateAsyncScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IFedRampControlMappingRepository>();
            mappingId = (await repository.CreateAsync(tenantA, ValidMapping(), Guid.NewGuid())).Id;
        }

        await using var restartedScope = provider.CreateAsyncScope();
        var restartedRepository = restartedScope.ServiceProvider.GetRequiredService<IFedRampControlMappingRepository>();
        Assert.NotNull(await restartedRepository.GetAsync(tenantA, mappingId));
        Assert.Null(await restartedRepository.GetAsync(tenantB, mappingId));
    }

    [Fact]
    public async Task Readiness_package_ignores_client_authorization_input_and_survives_context_restart()
    {
        var databaseName = $"fedramp-package-{Guid.NewGuid():N}";
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        await using var provider = CreateInMemoryProvider(databaseName);
        await SeedTenantsAsync(provider, tenantId, otherTenantId);

        Guid packageId;
        await using (var scope = provider.CreateAsyncScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IFedRampReadinessExportPackageRepository>();
            var package = await repository.CreateAsync(tenantId, ValidPackage(tenantId, true), Guid.NewGuid());
            packageId = package.Id;
            Assert.Equal(EfFedRampReadinessExportPackageRepository.ReadinessOnlyLanguage, package.AuthorizationLanguage);
        }

        await using var restartedScope = provider.CreateAsyncScope();
        var restartedRepository = restartedScope.ServiceProvider.GetRequiredService<IFedRampReadinessExportPackageRepository>();
        var persisted = await restartedRepository.GetAsync(tenantId, packageId);
        Assert.NotNull(persisted);
        Assert.Equal(EfFedRampReadinessExportPackageRepository.ReadinessOnlyLanguage, persisted.AuthorizationLanguage);
        Assert.Null(await restartedRepository.GetAsync(otherTenantId, packageId));
    }

    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task PostgreSQL_rolls_back_package_creation_when_audit_fails()
    {
        var connectionString = Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION") ??
            throw new InvalidOperationException("Set GCCS_TEST_POSTGRES_CONNECTION to run this test.");
        var tenantId = Guid.NewGuid();
        await using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<GccsDbContext>();
                services.RemoveAll<DbContextOptions<GccsDbContext>>();
                services.RemoveAll<IAuditEventWriter>();
                services.AddDbContext<GccsDbContext>(options => options.UseNpgsql(connectionString));
                services.AddScoped<IAuditEventWriter, FailingAuditWriter>();

                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
                context.Database.Migrate();
                context.Tenants.Add(Tenant(tenantId));
                context.SaveChanges();
            });
        });

        try
        {
            using var client = factory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/enterprise/fedramp/readiness-packages")
            {
                Content = System.Net.Http.Json.JsonContent.Create(ValidPackage(tenantId, true))
            };
            request.Headers.Add("X-Gccs-Dev-Auth", "true");
            request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
            request.Headers.Add("X-Gccs-Dev-User", Guid.NewGuid().ToString());
            request.Headers.Add("X-Gccs-Dev-Permissions", "ManageTenant");

            using var response = await client.SendAsync(request);
            Assert.Equal(System.Net.HttpStatusCode.InternalServerError, response.StatusCode);
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
            Assert.False(await context.FedRampReadinessPackages.AnyAsync(package => package.TenantId == tenantId));
            Assert.False(await context.AuditLogEntries.AnyAsync(entry => entry.TenantId == tenantId));
        }
        finally
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
            var tenant = await context.Tenants.SingleOrDefaultAsync(candidate => candidate.Id == tenantId);
            if (tenant is not null)
            {
                context.Tenants.Remove(tenant);
                await context.SaveChangesAsync();
            }
        }
    }

    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task PostgreSQL_rejects_stale_concurrent_mapping_update()
    {
        var connectionString = Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION") ??
            throw new InvalidOperationException("Set GCCS_TEST_POSTGRES_CONNECTION to run this test.");
        var options = new DbContextOptionsBuilder<GccsDbContext>().UseNpgsql(connectionString).Options;
        var tenantId = Guid.NewGuid();
        Guid mappingId;

        await using (var setup = new GccsDbContext(options))
        {
            await setup.Database.MigrateAsync();
            setup.Tenants.Add(Tenant(tenantId));
            await setup.SaveChangesAsync();
            mappingId = (await new EfFedRampControlMappingRepository(setup).CreateAsync(tenantId, ValidMapping(), Guid.NewGuid())).Id;
        }

        try
        {
            await using var firstContext = new GccsDbContext(options);
            await using var secondContext = new GccsDbContext(options);
            var first = new EfFedRampControlMappingRepository(firstContext);
            var second = new EfFedRampControlMappingRepository(secondContext);
            Assert.NotNull(await first.GetAsync(tenantId, mappingId));
            Assert.NotNull(await second.GetAsync(tenantId, mappingId));

            var inReview = new FedRampControlReviewRequest(FedRampReviewState.InReview, "reviewer-a", DateOnly.FromDateTime(DateTime.UtcNow), "first update");
            var gapIdentified = new FedRampControlReviewRequest(FedRampReviewState.GapIdentified, "reviewer-b", DateOnly.FromDateTime(DateTime.UtcNow), "stale update");
            await first.ChangeStateAsync(tenantId, mappingId, inReview, Guid.NewGuid());
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
                second.ChangeStateAsync(tenantId, mappingId, gapIdentified, Guid.NewGuid()));
        }
        finally
        {
            await using var cleanup = new GccsDbContext(options);
            var mapping = await cleanup.FedRampControlMappings.SingleOrDefaultAsync(candidate => candidate.Id == mappingId);
            if (mapping is not null)
            {
                cleanup.FedRampControlMappings.Remove(mapping);
                await cleanup.SaveChangesAsync();
            }
            var tenant = await cleanup.Tenants.SingleOrDefaultAsync(candidate => candidate.Id == tenantId);
            if (tenant is not null)
            {
                cleanup.Tenants.Remove(tenant);
                await cleanup.SaveChangesAsync();
            }
        }
    }

    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task PostgreSQL_rejects_stale_concurrent_package_update()
    {
        var connectionString = Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION") ??
            throw new InvalidOperationException("Set GCCS_TEST_POSTGRES_CONNECTION to run this test.");
        var options = new DbContextOptionsBuilder<GccsDbContext>().UseNpgsql(connectionString).Options;
        var tenantId = Guid.NewGuid();
        Guid packageId;

        await using (var setup = new GccsDbContext(options))
        {
            await setup.Database.MigrateAsync();
            setup.Tenants.Add(Tenant(tenantId));
            await setup.SaveChangesAsync();
            packageId = (await new EfFedRampReadinessExportPackageRepository(setup).CreateAsync(tenantId, ValidPackage(tenantId, false), Guid.NewGuid())).Id;
        }

        try
        {
            await using var firstContext = new GccsDbContext(options);
            await using var secondContext = new GccsDbContext(options);
            var first = new EfFedRampReadinessExportPackageRepository(firstContext);
            var second = new EfFedRampReadinessExportPackageRepository(secondContext);
            Assert.NotNull(await first.GetAsync(tenantId, packageId));
            Assert.NotNull(await second.GetAsync(tenantId, packageId));

            await first.ChangeStatusAsync(tenantId, packageId, new FedRampReadinessPackageStatusRequest(FedRampReadinessPackageStatus.InReview, "reviewer-a"), Guid.NewGuid());
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
                second.ChangeStatusAsync(tenantId, packageId, new FedRampReadinessPackageStatusRequest(FedRampReadinessPackageStatus.Archived, "reviewer-b"), Guid.NewGuid()));
        }
        finally
        {
            await using var cleanup = new GccsDbContext(options);
            var package = await cleanup.FedRampReadinessPackages.SingleOrDefaultAsync(candidate => candidate.Id == packageId);
            if (package is not null)
            {
                cleanup.FedRampReadinessPackages.Remove(package);
                await cleanup.SaveChangesAsync();
            }
            var tenant = await cleanup.Tenants.SingleOrDefaultAsync(candidate => candidate.Id == tenantId);
            if (tenant is not null)
            {
                cleanup.Tenants.Remove(tenant);
                await cleanup.SaveChangesAsync();
            }
        }
    }

    private static ServiceProvider CreateInMemoryProvider(string databaseName)
    {
        var services = new ServiceCollection();
        services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddScoped<IFedRampControlMappingRepository, EfFedRampControlMappingRepository>();
        services.AddScoped<IFedRampReadinessExportPackageRepository, EfFedRampReadinessExportPackageRepository>();
        return services.BuildServiceProvider();
    }

    private static async Task SeedTenantsAsync(IServiceProvider provider, params Guid[] tenantIds)
    {
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        context.Tenants.AddRange(tenantIds.Select(Tenant));
        await context.SaveChangesAsync();
    }

    private static TenantEntity Tenant(Guid tenantId) => new()
    {
        Id = tenantId,
        Name = $"Tenant {tenantId:N}",
        Status = TenantStatus.Active,
        DataPosture = TenantDataPosture.NoCui,
        CreatedAt = DateTimeOffset.UtcNow
    };

    private static CreateFedRampControlMappingRequest ValidMapping() =>
        new("AC-2", "Access Control", "Moderate", "security-owner", FedRampImplementationStatus.Implemented, "Identity controls are mapped for readiness review.", "Azure", [new FedRampEvidenceLinkDto("identity evidence", "evidence://identity", FedRampEvidenceType.Identity)], null, "NIST SP 800-53 Rev. 5");

    private static CreateFedRampReadinessPackageRequest ValidPackage(Guid tenantId, bool governanceAuthorized)
    {
#pragma warning disable CS0618
        return new CreateFedRampReadinessPackageRequest(
            $"package-{Guid.NewGuid():N}", "readiness", "commercial-production", "reviewer", governanceAuthorized,
            [new FedRampPackageRecordDto("control", "AC-2", "Access control", FedRampPackageRecordStatus.Approved, false, false, tenantId)],
            [], [], "Readiness review only.");
#pragma warning restore CS0618
    }

    private sealed class FailingAuditWriter : IAuditEventWriter
    {
        public Task WriteAsync(Guid tenantId, Guid actorUserId, AuditAction action, string entityType, string entityId, string summary, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default) =>
            throw new AuditWriteException("Synthetic audit failure.");
    }
}

using System.Net;
using System.Net.Http.Json;
using Gccs.Application.Notifications;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class NotificationPreferencePostgresConcurrencyTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public NotificationPreferencePostgresConcurrencyTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task Concurrent_gets_create_one_tenant_scoped_notification_preference()
    {
        var connectionString = Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION") ??
            throw new InvalidOperationException("Set GCCS_TEST_POSTGRES_CONNECTION to run the PostgreSQL concurrency test.");

        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await using var factory = CreateFactory(connectionString, tenantId);

        try
        {
            using var client = factory.CreateClient();
            var startGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var requests = Enumerable.Range(0, 32)
                .Select(_ => Task.Run(async () =>
                {
                    await startGate.Task;
                    using var request = CreateGetRequest(tenantId, userId);
                    using var response = await client.SendAsync(request);
                    var body = await response.Content.ReadAsStringAsync();
                    Assert.True(
                        response.StatusCode == HttpStatusCode.OK,
                        $"Expected 200 OK but received {(int)response.StatusCode}: {body}");
                    return await response.Content.ReadFromJsonAsync<NotificationPreferenceDto>() ??
                        throw new InvalidOperationException("Expected a notification preference response.");
                }))
                .ToArray();

            startGate.SetResult();
            var results = await Task.WhenAll(requests);

            Assert.Single(results.Select(result => result.Id).Distinct());
            Assert.All(results, result =>
            {
                Assert.Equal(tenantId, result.TenantId);
                Assert.Equal(userId, result.UserId);
            });

            using var verificationScope = factory.Services.CreateScope();
            var dbContext = verificationScope.ServiceProvider.GetRequiredService<GccsDbContext>();
            Assert.Equal(1, await dbContext.NotificationPreferences.CountAsync(preference =>
                preference.TenantId == tenantId && preference.UserId == userId));
        }
        finally
        {
            await DeleteTenantAsync(factory, tenantId);
        }
    }

    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task Concurrent_gets_for_same_user_do_not_cross_tenant_boundaries()
    {
        var connectionString = Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION") ??
            throw new InvalidOperationException("Set GCCS_TEST_POSTGRES_CONNECTION to run the PostgreSQL concurrency test.");

        var tenantAId = Guid.NewGuid();
        var tenantBId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await using var factory = CreateFactory(connectionString, tenantAId, tenantBId);

        try
        {
            using var client = factory.CreateClient();
            var tenantARequest = SendGetAsync(client, tenantAId, userId);
            var tenantBRequest = SendGetAsync(client, tenantBId, userId);

            var results = await Task.WhenAll(tenantARequest, tenantBRequest);

            Assert.Equal(tenantAId, results[0].TenantId);
            Assert.Equal(tenantBId, results[1].TenantId);
            Assert.NotEqual(results[0].Id, results[1].Id);

            using var verificationScope = factory.Services.CreateScope();
            var dbContext = verificationScope.ServiceProvider.GetRequiredService<GccsDbContext>();
            Assert.Equal(2, await dbContext.NotificationPreferences.CountAsync(preference =>
                preference.UserId == userId &&
                (preference.TenantId == tenantAId || preference.TenantId == tenantBId)));
        }
        finally
        {
            await DeleteTenantAsync(factory, tenantAId, tenantBId);
        }
    }

    private WebApplicationFactory<Program> CreateFactory(string connectionString, params Guid[] tenantIds) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<GccsDbContext>();
                services.RemoveAll<DbContextOptions<GccsDbContext>>();
                services.AddDbContext<GccsDbContext>(options => options.UseNpgsql(connectionString));

                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
                dbContext.Database.Migrate();
                dbContext.Tenants.AddRange(tenantIds.Select(tenantId => new TenantEntity
                {
                    Id = tenantId,
                    Name = "Notification concurrency tenant",
                    Status = TenantStatus.Active,
                    DataPosture = TenantDataPosture.NoCui,
                    CreatedAt = DateTimeOffset.UtcNow
                }));
                dbContext.SaveChanges();
            });
        });

    private static async Task<NotificationPreferenceDto> SendGetAsync(
        HttpClient client,
        Guid tenantId,
        Guid userId)
    {
        using var request = CreateGetRequest(tenantId, userId);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<NotificationPreferenceDto>() ??
            throw new InvalidOperationException("Expected a notification preference response.");
    }

    private static HttpRequestMessage CreateGetRequest(Guid tenantId, Guid userId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/notification-preferences");
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", userId.ToString());
        request.Headers.Add("X-Gccs-Dev-Role", RoleCatalog.ComplianceManager);
        return request;
    }

    private static async Task DeleteTenantAsync(
        WebApplicationFactory<Program> factory,
        params Guid[] tenantIds)
    {
        using var cleanupScope = factory.Services.CreateScope();
        var dbContext = cleanupScope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var preferences = await dbContext.NotificationPreferences
            .Where(preference => tenantIds.Contains(preference.TenantId))
            .ToArrayAsync();
        dbContext.NotificationPreferences.RemoveRange(preferences);
        var tenants = await dbContext.Tenants
            .Where(tenant => tenantIds.Contains(tenant.Id))
            .ToArrayAsync();
        dbContext.Tenants.RemoveRange(tenants);
        await dbContext.SaveChangesAsync();
    }
}

internal sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION")))
        {
            Skip = "Set GCCS_TEST_POSTGRES_CONNECTION to run this PostgreSQL integration test.";
        }
    }
}

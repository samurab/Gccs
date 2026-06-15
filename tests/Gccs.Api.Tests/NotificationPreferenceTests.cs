using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Notifications;
using Gccs.Domain.Audit;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Notifications;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class NotificationPreferenceTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public NotificationPreferenceTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_16_1_1_Create_users_by_role_assigns_default_notification_preferences()
    {
        var ids = StoryIds.ForCase("tc-16-1-1");
        await using var factory = CreateFactory("tc-16-1-1", dbContext => SeedTenants(dbContext, ids));
        using var client = factory.CreateClient();

        var manager = await GetPreferencesAsync(client, ids.TenantAId, ids.ManagerUserId, RoleCatalog.ComplianceManager);
        var auditor = await GetPreferencesAsync(client, ids.TenantAId, ids.AuditorUserId, RoleCatalog.Auditor);

        Assert.True(manager.AssignmentNotificationsEnabled);
        Assert.True(manager.CertificationRenewalNotificationsEnabled);
        Assert.True(manager.CmmcAffirmationNotificationsEnabled);
        Assert.False(auditor.AssignmentNotificationsEnabled);
        Assert.True(auditor.DueSoonNotificationsEnabled);
        Assert.False(auditor.EvidenceRequestNotificationsEnabled);
    }

    [Fact]
    public async Task TC_16_1_2_Update_preferences_for_all_notification_categories()
    {
        var ids = StoryIds.ForCase("tc-16-1-2");
        await using var factory = CreateFactory("tc-16-1-2", dbContext => SeedTenants(dbContext, ids));
        using var client = factory.CreateClient();
        var update = new NotificationPreferenceUpdateRequest(false, true, false, true, false, true);

        var preferences = await UpdatePreferencesAsync(client, ids.TenantAId, ids.ManagerUserId, RoleCatalog.ComplianceManager, update);

        Assert.False(preferences.AssignmentNotificationsEnabled);
        Assert.True(preferences.DueSoonNotificationsEnabled);
        Assert.False(preferences.OverdueNotificationsEnabled);
        Assert.True(preferences.EvidenceRequestNotificationsEnabled);
        Assert.False(preferences.CertificationRenewalNotificationsEnabled);
        Assert.True(preferences.CmmcAffirmationNotificationsEnabled);
        Assert.NotNull(preferences.UpdatedAt);
    }

    [Fact]
    public async Task TC_16_1_3_Notification_preferences_are_tenant_scoped_for_multi_tenant_user()
    {
        var ids = StoryIds.ForCase("tc-16-1-3");
        await using var factory = CreateFactory("tc-16-1-3", dbContext => SeedTenants(dbContext, ids));
        using var client = factory.CreateClient();

        await UpdatePreferencesAsync(
            client,
            ids.TenantAId,
            ids.ManagerUserId,
            RoleCatalog.ComplianceManager,
            new NotificationPreferenceUpdateRequest(false, false, false, false, false, false));
        var tenantB = await GetPreferencesAsync(client, ids.TenantBId, ids.ManagerUserId, RoleCatalog.ComplianceManager);

        Assert.Equal(ids.TenantBId, tenantB.TenantId);
        Assert.Equal(ids.ManagerUserId, tenantB.UserId);
        Assert.True(tenantB.AssignmentNotificationsEnabled);
        Assert.True(tenantB.CmmcAffirmationNotificationsEnabled);
    }

    [Fact]
    public async Task TC_16_1_4_Notification_preference_changes_are_audit_logged()
    {
        var ids = StoryIds.ForCase("tc-16-1-4");
        await using var factory = CreateFactory("tc-16-1-4", dbContext => SeedTenants(dbContext, ids));
        using var client = factory.CreateClient();

        var preferences = await UpdatePreferencesAsync(
            client,
            ids.TenantAId,
            ids.ManagerUserId,
            RoleCatalog.ComplianceManager,
            new NotificationPreferenceUpdateRequest(true, true, true, false, true, false));

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Contains(await dbContext.AuditLogEntries.Where(audit => audit.TenantId == ids.TenantAId).ToArrayAsync(), audit =>
            audit.EntityType == "NotificationPreference" &&
            audit.EntityId == preferences.Id.ToString() &&
            audit.Action == AuditAction.Updated &&
            audit.MetadataJson.Contains("evidenceRequestNotificationsEnabled", StringComparison.Ordinal));
    }

    private static async Task<NotificationPreferenceDto> GetPreferencesAsync(
        HttpClient client,
        Guid tenantId,
        Guid userId,
        string roleName)
    {
        using var request = CreateRequest<object?>(HttpMethod.Get, "/api/notification-preferences", null, tenantId, userId, roleName);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<NotificationPreferenceDto>(JsonOptions) ??
            throw new InvalidOperationException("Expected notification preference response.");
    }

    private static async Task<NotificationPreferenceDto> UpdatePreferencesAsync(
        HttpClient client,
        Guid tenantId,
        Guid userId,
        string roleName,
        NotificationPreferenceUpdateRequest body)
    {
        using var request = CreateRequest(HttpMethod.Put, "/api/notification-preferences", body, tenantId, userId, roleName);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<NotificationPreferenceDto>(JsonOptions) ??
            throw new InvalidOperationException("Expected notification preference response.");
    }

    private WebApplicationFactory<Program> CreateFactory(string databaseName, Action<GccsDbContext>? seed = null) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<NotificationPreferenceService>();
                services.AddScoped<INotificationPreferenceRepository, EfNotificationPreferenceRepository>();
                services.AddScoped<IAuditEventWriter, EfAuditEventWriter>();

                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
                dbContext.Database.EnsureDeleted();
                dbContext.Database.EnsureCreated();
                seed?.Invoke(dbContext);
                dbContext.SaveChanges();
            });
        });

    private static HttpRequestMessage CreateRequest<TContent>(
        HttpMethod method,
        string requestUri,
        TContent content,
        Guid tenantId,
        Guid userId,
        string roleName)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", userId.ToString());
        request.Headers.Add("X-Gccs-Dev-Role", roleName);
        if (content is not null)
        {
            request.Content = JsonContent.Create(content, options: JsonOptions);
        }

        return request;
    }

    private static void SeedTenants(GccsDbContext dbContext, StoryIds ids)
    {
        dbContext.Tenants.AddRange(
            CreateTenant(ids.TenantAId),
            CreateTenant(ids.TenantBId));
    }

    private static TenantEntity CreateTenant(Guid tenantId) =>
        new()
        {
            Id = tenantId,
            Name = "Notification Tenant",
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.UtcNow
        };

    private sealed record StoryIds(Guid TenantAId, Guid TenantBId, Guid ManagerUserId, Guid AuditorUserId)
    {
        public static StoryIds ForCase(string caseName)
        {
            var suffix = Math.Abs(caseName.GetHashCode(StringComparison.Ordinal)) % 10000;
            return new StoryIds(
                Guid.Parse($"16116116-1161-1611-6116-11611611{suffix:D4}"),
                Guid.Parse($"16116116-1161-1611-6116-11611612{suffix:D4}"),
                Guid.Parse($"16116116-1161-1611-6116-11611613{suffix:D4}"),
                Guid.Parse($"16116116-1161-1611-6116-11611614{suffix:D4}"));
        }
    }
}

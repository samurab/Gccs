using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Identity;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Identity;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class TenantWorkspaceSelectionTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
    private readonly WebApplicationFactory<Program> _factory;

    public TenantWorkspaceSelectionTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task List_returns_only_authenticated_users_memberships_and_marks_unavailable_tenants()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var activeTenantId = Guid.NewGuid();
        var suspendedTenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        using var factory = CreateFactory(
            nameof(List_returns_only_authenticated_users_memberships_and_marks_unavailable_tenants),
            dbContext =>
            {
                dbContext.Tenants.AddRange(
                    CreateTenant(activeTenantId, "Active workspace", TenantStatus.Active),
                    CreateTenant(suspendedTenantId, "Suspended workspace", TenantStatus.Suspended),
                    CreateTenant(otherTenantId, "Other workspace", TenantStatus.Active));
                dbContext.Users.AddRange(
                    CreateUser(userId, activeTenantId, "user@example.com"),
                    CreateUser(otherUserId, otherTenantId, "other@example.com"));
                dbContext.TenantMemberships.AddRange(
                    CreateMembership(activeTenantId, userId),
                    CreateMembership(suspendedTenantId, userId),
                    CreateMembership(otherTenantId, otherUserId));
            });
        using var client = factory.CreateClient();
        using var request = CreateAuthenticatedRequest(HttpMethod.Get, "/api/me/tenants", userId);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<TenantWorkspaceListDto>(JsonOptions);
        Assert.NotNull(result);
        Assert.Equal(2, result.Tenants.Count);
        Assert.DoesNotContain(result.Tenants, tenant => tenant.TenantId == otherTenantId);
        Assert.True(result.Tenants.Single(tenant => tenant.TenantId == activeTenantId).IsSelectable);
        var suspended = result.Tenants.Single(tenant => tenant.TenantId == suspendedTenantId);
        Assert.False(suspended.IsSelectable);
        Assert.Equal("The tenant is suspended.", suspended.UnavailableReason);
    }

    [Fact]
    public async Task Select_persists_validated_preference_access_time_and_audit_event()
    {
        var userId = Guid.NewGuid();
        var homeTenantId = Guid.NewGuid();
        var selectedTenantId = Guid.NewGuid();
        using var factory = CreateFactory(
            nameof(Select_persists_validated_preference_access_time_and_audit_event),
            dbContext =>
            {
                dbContext.Tenants.AddRange(
                    CreateTenant(homeTenantId, "Home workspace", TenantStatus.Active),
                    CreateTenant(selectedTenantId, "Selected workspace", TenantStatus.Trialing));
                dbContext.Users.Add(CreateUser(userId, homeTenantId, "user@example.com"));
                dbContext.TenantMemberships.AddRange(
                    CreateMembership(homeTenantId, userId),
                    CreateMembership(selectedTenantId, userId, "Contributor"));
            });
        using var client = factory.CreateClient();
        using var request = CreateAuthenticatedRequest(HttpMethod.Post, "/api/me/tenant-selection", userId);
        request.Content = JsonContent.Create(new SelectTenantWorkspaceRequest(selectedTenantId), options: JsonOptions);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<TenantWorkspaceSelectionDto>(JsonOptions);
        Assert.Equal(selectedTenantId, result?.TenantId);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Equal(selectedTenantId, (await dbContext.Users.SingleAsync(user => user.Id == userId)).PreferredTenantId);
        Assert.NotNull((await dbContext.TenantMemberships.SingleAsync(
            membership => membership.UserId == userId && membership.TenantId == selectedTenantId)).LastAccessedAt);
        Assert.Contains(
            await dbContext.AuditLogEntries.ToListAsync(),
            audit => audit.TenantId == selectedTenantId &&
                     audit.ActorUserId == userId &&
                     audit.EntityType == "TenantWorkspaceSelection");
    }

    [Theory]
    [InlineData(TenantStatus.Suspended)]
    [InlineData(TenantStatus.Archived)]
    [InlineData(TenantStatus.PendingActivation)]
    public async Task Select_denies_non_operational_tenant_without_changing_preference(TenantStatus tenantStatus)
    {
        var userId = Guid.NewGuid();
        var homeTenantId = Guid.NewGuid();
        var requestedTenantId = Guid.NewGuid();
        using var factory = CreateFactory(
            $"{nameof(Select_denies_non_operational_tenant_without_changing_preference)}-{tenantStatus}",
            dbContext =>
            {
                dbContext.Tenants.AddRange(
                    CreateTenant(homeTenantId, "Home workspace", TenantStatus.Active),
                    CreateTenant(requestedTenantId, "Unavailable workspace", tenantStatus));
                var user = CreateUser(userId, homeTenantId, "user@example.com");
                user.PreferredTenantId = homeTenantId;
                dbContext.Users.Add(user);
                dbContext.TenantMemberships.AddRange(
                    CreateMembership(homeTenantId, userId),
                    CreateMembership(requestedTenantId, userId));
            });
        using var client = factory.CreateClient();
        using var request = CreateAuthenticatedRequest(HttpMethod.Post, "/api/me/tenant-selection", userId);
        request.Content = JsonContent.Create(new SelectTenantWorkspaceRequest(requestedTenantId), options: JsonOptions);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Equal(homeTenantId, (await dbContext.Users.SingleAsync(user => user.Id == userId)).PreferredTenantId);
        Assert.DoesNotContain(
            await dbContext.AuditLogEntries.ToListAsync(),
            audit => audit.EntityType == "TenantWorkspaceSelection");
    }

    [Fact]
    public async Task Tenant_scoped_endpoint_denies_membership_when_selected_tenant_is_suspended()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        using var factory = CreateFactory(
            nameof(Tenant_scoped_endpoint_denies_membership_when_selected_tenant_is_suspended),
            dbContext =>
            {
                dbContext.Tenants.Add(CreateTenant(tenantId, "Suspended workspace", TenantStatus.Suspended));
                dbContext.Users.Add(CreateUser(userId, tenantId, "user@example.com"));
                dbContext.TenantMemberships.Add(CreateMembership(tenantId, userId));
            },
            enforceMembership: true);
        using var client = factory.CreateClient();
        using var request = CreateAuthenticatedRequest(HttpMethod.Get, "/api/me/access", userId, tenantId);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private WebApplicationFactory<Program> CreateFactory(
        string databaseName,
        Action<GccsDbContext> seed,
        bool enforceMembership = false) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.UseSetting("Security:MembershipAuthorization:Enforce", enforceMembership.ToString());
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<TenantWorkspaceSelectionService>();
                services.AddScoped<ITenantWorkspaceSelectionRepository, EfTenantWorkspaceSelectionRepository>();
                services.AddScoped<ITenantMembershipRepository, EfTenantMembershipRepository>();
                services.AddScoped<IAuditEventWriter, EfAuditEventWriter>();

                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
                dbContext.Database.EnsureDeleted();
                dbContext.Database.EnsureCreated();
                seed(dbContext);
                dbContext.SaveChanges();
            });
        });

    private static HttpRequestMessage CreateAuthenticatedRequest(
        HttpMethod method,
        string path,
        Guid userId,
        Guid? selectedTenantId = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-User", userId.ToString());
        if (selectedTenantId.HasValue)
        {
            request.Headers.Add("X-Gccs-Tenant", selectedTenantId.Value.ToString());
        }

        return request;
    }

    private static TenantEntity CreateTenant(Guid tenantId, string name, TenantStatus status) =>
        new()
        {
            Id = tenantId,
            Name = name,
            Status = status,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.Parse("2026-07-24T12:00:00Z")
        };

    private static UserEntity CreateUser(Guid userId, Guid homeTenantId, string email) =>
        new()
        {
            Id = userId,
            TenantId = homeTenantId,
            Email = email,
            DisplayName = email,
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.Parse("2026-07-24T12:00:00Z")
        };

    private static TenantMembershipEntity CreateMembership(
        Guid tenantId,
        Guid userId,
        string roleName = "Admin") =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            Status = MembershipStatus.Active,
            RoleName = roleName,
            CreatedAt = DateTimeOffset.Parse("2026-07-24T12:00:00Z")
        };
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Evidence;
using Gccs.Application.Security;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Evidence;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class EvidenceRequestDashboardTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public EvidenceRequestDashboardTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_26_3_1_and_TC_26_3_2_Dashboard_is_tenant_scoped_and_filters_by_status_due_assignee_type_and_priority()
    {
        var ids = StoryIds.ForCase("tc-26-3-1");
        await using var factory = CreateFactory("tc-26-3-1", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();

        var filtered = await ListAsync(client, ids.TenantId, $"?status=Open&dueFrom=2026-08-01&dueTo=2026-08-31&assigneeUserId={ids.AssigneeUserId}&relatedRecordType=Obligation&priority=High");

        var item = Assert.Single(filtered);
        Assert.Equal(ids.OpenHighId, item.Id);
        Assert.DoesNotContain(filtered, request => request.TenantId == ids.OtherTenantId);
    }

    [Fact]
    public async Task TC_26_3_3_Overdue_is_calculated_from_due_date_and_current_status()
    {
        var ids = StoryIds.ForCase("tc-26-3-3");
        await using var factory = CreateFactory("tc-26-3-3", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();

        var items = await ListAsync(client, ids.TenantId, string.Empty);

        Assert.Contains(items, item => item.Id == ids.OverdueOpenId && item.IsOverdue);
        Assert.Contains(items, item => item.Id == ids.AcceptedPastDueId && !item.IsOverdue);
    }

    [Fact]
    public async Task TC_26_3_4_Bulk_reminders_create_notifications_without_status_changes()
    {
        var ids = StoryIds.ForCase("tc-26-3-4");
        await using var factory = CreateFactory("tc-26-3-4", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();

        using var request = CreateRequest(
            HttpMethod.Post,
            "/api/evidence-requests/reminders",
            new EvidenceRequestReminderRequest([ids.OpenHighId, ids.SubmittedNormalId]),
            ids.TenantId,
            Permission.ManageEvidence);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Equal("Open", await dbContext.EvidenceRequests.Where(item => item.Id == ids.OpenHighId).Select(item => item.Status).SingleAsync());
        Assert.Equal(2, await dbContext.NotificationDeliveries.CountAsync(notification =>
            notification.TenantId == ids.TenantId && notification.Category == "evidence_request_reminder"));
    }

    [Fact]
    public async Task TC_26_3_5_Auditors_can_view_but_not_modify()
    {
        var ids = StoryIds.ForCase("tc-26-3-5");
        await using var factory = CreateFactory("tc-26-3-5", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();

        using var view = CreateRequest<object?>(HttpMethod.Get, "/api/evidence-requests?status=Accepted", null, ids.TenantId, Permission.ViewEvidence, Permission.AuditorReadOnly);
        var viewResponse = await client.SendAsync(view);
        var items = await viewResponse.Content.ReadFromJsonAsync<EvidenceRequestDashboardItemDto[]>(JsonOptions) ?? [];
        using var modify = CreateRequest(HttpMethod.Post, "/api/evidence-requests/reminders", new EvidenceRequestReminderRequest([ids.AcceptedPastDueId]), ids.TenantId, Permission.ViewEvidence, Permission.AuditorReadOnly);
        var modifyResponse = await client.SendAsync(modify);

        Assert.Equal(HttpStatusCode.OK, viewResponse.StatusCode);
        Assert.Single(items);
        Assert.Equal(HttpStatusCode.Forbidden, modifyResponse.StatusCode);
    }

    private static async Task<EvidenceRequestDashboardItemDto[]> ListAsync(HttpClient client, Guid tenantId, string query)
    {
        using var request = CreateRequest<object?>(HttpMethod.Get, $"/api/evidence-requests{query}", null, tenantId, Permission.ViewEvidence);
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<EvidenceRequestDashboardItemDto[]>(JsonOptions) ?? [];
    }

    private WebApplicationFactory<Program> CreateFactory(string databaseName, Action<GccsDbContext>? seed = null) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<EvidenceRequestService>();
                services.AddScoped<IEvidenceRequestRepository, EfEvidenceRequestRepository>();
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

    private static HttpRequestMessage CreateRequest<TContent>(HttpMethod method, string requestUri, TContent content, Guid tenantId, params Permission[] permissions)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", Guid.NewGuid().ToString());
        request.Headers.Add("X-Gccs-Dev-Permissions", string.Join(",", permissions.Select(permission => permission.ToString())));
        if (content is not null)
        {
            request.Content = JsonContent.Create(content, options: JsonOptions);
        }

        return request;
    }

    private static void SeedScenario(GccsDbContext dbContext, StoryIds ids)
    {
        dbContext.Tenants.AddRange(CreateTenant(ids.TenantId), CreateTenant(ids.OtherTenantId));
        dbContext.Users.Add(new UserEntity
        {
            Id = ids.AssigneeUserId,
            TenantId = ids.TenantId,
            Email = "assignee@example.test",
            DisplayName = "Assignee",
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.EvidenceRequests.AddRange(
            Request(ids.TenantId, ids.OpenHighId, ids.AssigneeUserId, "Open", "High", new DateOnly(2026, 8, 15), "Obligation"),
            Request(ids.TenantId, ids.SubmittedNormalId, ids.AssigneeUserId, "Submitted", "Normal", new DateOnly(2026, 8, 20), "Control"),
            Request(ids.TenantId, ids.OverdueOpenId, ids.AssigneeUserId, "Open", "Low", DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-2), "Contract"),
            Request(ids.TenantId, ids.AcceptedPastDueId, ids.AssigneeUserId, "Accepted", "Critical", DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-3), "Obligation"),
            Request(ids.OtherTenantId, Guid.NewGuid(), Guid.NewGuid(), "Open", "High", new DateOnly(2026, 8, 15), "Obligation"));
    }

    private static EvidenceRequestEntity Request(Guid tenantId, Guid id, Guid assigneeUserId, string status, string priority, DateOnly dueDate, string relatedType) =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            RequesterUserId = Guid.NewGuid(),
            AssigneeUserId = assigneeUserId,
            DueDate = dueDate,
            Status = status,
            Priority = priority,
            Instructions = "Dashboard request",
            RelatedRecordType = relatedType,
            RelatedRecordId = "related",
            CreatedAt = DateTimeOffset.UtcNow
        };

    private static TenantEntity CreateTenant(Guid tenantId) =>
        new()
        {
            Id = tenantId,
            Name = $"Evidence Dashboard Tenant {tenantId:N}",
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.UtcNow
        };

    private sealed record StoryIds(
        Guid TenantId,
        Guid OtherTenantId,
        Guid AssigneeUserId,
        Guid OpenHighId,
        Guid SubmittedNormalId,
        Guid OverdueOpenId,
        Guid AcceptedPastDueId)
    {
        public static StoryIds ForCase(string caseName)
        {
            var suffix = Math.Abs(caseName.GetHashCode(StringComparison.Ordinal)) % 10000;
            return new StoryIds(
                Guid.Parse($"26326326-3263-2632-6326-32632631{suffix:D4}"),
                Guid.Parse($"26326326-3263-2632-6326-32632632{suffix:D4}"),
                Guid.Parse($"26326326-3263-2632-6326-32632633{suffix:D4}"),
                Guid.Parse($"26326326-3263-2632-6326-32632634{suffix:D4}"),
                Guid.Parse($"26326326-3263-2632-6326-32632635{suffix:D4}"),
                Guid.Parse($"26326326-3263-2632-6326-32632636{suffix:D4}"),
                Guid.Parse($"26326326-3263-2632-6326-32632637{suffix:D4}"));
        }
    }
}

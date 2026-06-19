using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Tenancy;
using Gccs.Domain.Audit;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Gccs.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class GovernmentCloudEnvironmentTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public GovernmentCloudEnvironmentTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_36_1_1_Create_government_cloud_environment_persists_required_settings()
    {
        var ids = CreateTestIds("a1");
        await using var factory = CreateFactory("tc-36-1-1", dbContext => SeedTenant(dbContext, ids.TenantId));
        using var client = factory.CreateClient();

        var environment = await CreateEnvironmentAsync(client, ids);

        Assert.Equal(EnvironmentDeploymentType.GovCloud, environment.EnvironmentType);
        Assert.Equal("us-gov-west-1", environment.Region);
        Assert.Equal("fedramp-moderate-boundary", environment.Boundary);
        Assert.Equal("private-vnet-a", environment.NetworkSegment);
        Assert.Equal("govstorage", environment.StorageAccount);
        Assert.Equal("gov-postgres", environment.DatabaseService);
        Assert.Equal("gov-kms", environment.KeyManagementService);
        Assert.Equal("gov-logs", environment.LoggingWorkspace);
        Assert.Equal("daily-pitr-35-days", environment.BackupPolicy);
        Assert.Equal(EnvironmentReadinessStatus.Draft, environment.Status);
    }

    [Fact]
    public async Task TC_36_1_2_Approval_validation_blocks_missing_controls_disallowed_region_and_missing_review_metadata()
    {
        var ids = CreateTestIds("a2");
        await using var factory = CreateFactory("tc-36-1-2", dbContext => SeedTenant(dbContext, ids.TenantId));
        using var client = factory.CreateClient();
        var invalidRegion = await CreateEnvironmentAsync(client, ids, region: "us-east-1");
        var missingControls = await CreateEnvironmentAsync(client, ids, name: "Missing Controls", encrypted: false);

        var invalidRegionResponse = await ApproveAsync(client, ids, invalidRegion.Id);
        var missingControlsResponse = await ApproveAsync(client, ids, missingControls.Id);
        var missingMetadataResponse = await client.SendAsync(CreateRequest(
            HttpMethod.Post,
            $"/api/enterprise/government-cloud-environments/{invalidRegion.Id}/approve",
            new ReviewGovernmentCloudEnvironmentRequest("", ""),
            ids.TenantId,
            ids.ActorUserId,
            Permission.ManageTenant));

        Assert.Equal(HttpStatusCode.BadRequest, invalidRegionResponse.StatusCode);
        Assert.Contains("allowlisted", await invalidRegionResponse.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.BadRequest, missingControlsResponse.StatusCode);
        Assert.Contains("encryption", await missingControlsResponse.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.BadRequest, missingMetadataResponse.StatusCode);
    }

    [Fact]
    public async Task TC_36_1_3_Only_approved_environments_can_be_selected_for_regulated_tenant_deployment()
    {
        var ids = CreateTestIds("a3");
        await using var factory = CreateFactory("tc-36-1-3", dbContext => SeedTenant(dbContext, ids.TenantId));
        using var client = factory.CreateClient();
        var draft = await CreateEnvironmentAsync(client, ids, name: "Draft");
        var blocked = await CreateEnvironmentAsync(client, ids, name: "Blocked");
        var retired = await CreateEnvironmentAsync(client, ids, name: "Retired");
        var approved = await CreateEnvironmentAsync(client, ids, name: "Approved");

        await PostStatusAsync(client, ids, blocked.Id, "block");
        await PostStatusAsync(client, ids, retired.Id, "retire");
        await PostStatusAsync(client, ids, approved.Id, "approve");

        var draftSelection = await SelectAsync(client, ids, draft.Id);
        var blockedSelection = await SelectAsync(client, ids, blocked.Id);
        var retiredSelection = await SelectAsync(client, ids, retired.Id);
        var approvedSelection = await SelectAsync(client, ids, approved.Id);

        Assert.False(draftSelection.Allowed);
        Assert.False(blockedSelection.Allowed);
        Assert.False(retiredSelection.Allowed);
        Assert.True(approvedSelection.Allowed);
    }

    [Fact]
    public async Task TC_36_1_4_Status_transitions_preserve_reviewer_metadata_and_history()
    {
        var ids = CreateTestIds("a4");
        await using var factory = CreateFactory("tc-36-1-4", dbContext => SeedTenant(dbContext, ids.TenantId));
        using var client = factory.CreateClient();
        var environment = await CreateEnvironmentAsync(client, ids);

        await PostStatusAsync(client, ids, environment.Id, "submit-review");
        await PostStatusAsync(client, ids, environment.Id, "approve");
        await PostStatusAsync(client, ids, environment.Id, "block");
        await PostStatusAsync(client, ids, environment.Id, "deploy");
        var retired = await PostStatusAsync(client, ids, environment.Id, "retire");

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var history = await dbContext.GovernmentCloudEnvironmentStatusHistory
            .Where(candidate => candidate.EnvironmentId == environment.Id)
            .OrderBy(candidate => candidate.ChangedAt)
            .ToListAsync();

        Assert.Equal(EnvironmentReadinessStatus.Retired, retired.Status);
        Assert.Equal("Security Reviewer", retired.ReviewerName);
        Assert.Equal(6, history.Count);
        Assert.Contains(history, entry => entry.NewStatus == EnvironmentReadinessStatus.UnderReview);
        Assert.Contains(history, entry => entry.NewStatus == EnvironmentReadinessStatus.Approved);
        Assert.Contains(history, entry => entry.NewStatus == EnvironmentReadinessStatus.Blocked);
        Assert.Contains(history, entry => entry.NewStatus == EnvironmentReadinessStatus.Deployed);
        Assert.Contains(history, entry => entry.NewStatus == EnvironmentReadinessStatus.Retired);
    }

    [Fact]
    public async Task TC_36_1_5_Environment_lifecycle_actions_are_audit_logged()
    {
        var ids = CreateTestIds("a5");
        await using var factory = CreateFactory("tc-36-1-5", dbContext => SeedTenant(dbContext, ids.TenantId));
        using var client = factory.CreateClient();
        var environment = await CreateEnvironmentAsync(client, ids);
        await client.SendAsync(CreateRequest(HttpMethod.Put, $"/api/enterprise/government-cloud-environments/{environment.Id}", ValidRequest("Updated"), ids.TenantId, ids.ActorUserId, Permission.ManageTenant));
        await PostStatusAsync(client, ids, environment.Id, "approve");
        await PostStatusAsync(client, ids, environment.Id, "block");
        await PostStatusAsync(client, ids, environment.Id, "deploy");
        await PostStatusAsync(client, ids, environment.Id, "retire");

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var audits = await dbContext.AuditLogEntries
            .Where(audit => audit.TenantId == ids.TenantId && audit.EntityType == "GovernmentCloudEnvironment")
            .ToListAsync();

        Assert.Contains(audits, audit => audit.Action == AuditAction.Created);
        Assert.Contains(audits, audit => audit.Action == AuditAction.Updated && audit.Summary.Contains("updated", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(audits, audit => audit.Action == AuditAction.Approved);
        Assert.Contains(audits, audit => audit.Action == AuditAction.Rejected);
        Assert.Contains(audits, audit => audit.Summary.Contains("deployed", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(audits, audit => audit.Action == AuditAction.Archived);
    }

    private static async Task<GovernmentCloudEnvironmentDto> CreateEnvironmentAsync(
        HttpClient client,
        TestIds ids,
        string name = "GovCloud Primary",
        string region = "us-gov-west-1",
        bool encrypted = true)
    {
        var response = await client.SendAsync(CreateRequest(
            HttpMethod.Post,
            "/api/enterprise/government-cloud-environments",
            ValidRequest(name, region, encrypted),
            ids.TenantId,
            ids.ActorUserId,
            Permission.ManageTenant));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return Assert.IsType<GovernmentCloudEnvironmentDto>(await response.Content.ReadFromJsonAsync<GovernmentCloudEnvironmentDto>(JsonOptions));
    }

    private static async Task<HttpResponseMessage> ApproveAsync(HttpClient client, TestIds ids, Guid environmentId) =>
        await client.SendAsync(CreateRequest(
            HttpMethod.Post,
            $"/api/enterprise/government-cloud-environments/{environmentId}/approve",
            new ReviewGovernmentCloudEnvironmentRequest("Security Reviewer", "Reviewed required controls."),
            ids.TenantId,
            ids.ActorUserId,
            Permission.ManageTenant));

    private static async Task<GovernmentCloudEnvironmentDto> PostStatusAsync(HttpClient client, TestIds ids, Guid environmentId, string action)
    {
        var response = await client.SendAsync(CreateRequest(
            HttpMethod.Post,
            $"/api/enterprise/government-cloud-environments/{environmentId}/{action}",
            new ReviewGovernmentCloudEnvironmentRequest("Security Reviewer", $"Lifecycle action {action}."),
            ids.TenantId,
            ids.ActorUserId,
            Permission.ManageTenant));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return Assert.IsType<GovernmentCloudEnvironmentDto>(await response.Content.ReadFromJsonAsync<GovernmentCloudEnvironmentDto>(JsonOptions));
    }

    private static async Task<RegulatedEnvironmentSelectionResult> SelectAsync(HttpClient client, TestIds ids, Guid environmentId)
    {
        var response = await client.SendAsync(CreateRequest(HttpMethod.Post, $"/api/enterprise/government-cloud-environments/{environmentId}/select-regulated-deployment", ids.TenantId, ids.ActorUserId, Permission.ManageTenant));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return Assert.IsType<RegulatedEnvironmentSelectionResult>(await response.Content.ReadFromJsonAsync<RegulatedEnvironmentSelectionResult>(JsonOptions));
    }

    private static UpsertGovernmentCloudEnvironmentRequest ValidRequest(string name, string region = "us-gov-west-1", bool encrypted = true) =>
        new(
            name,
            EnvironmentDeploymentType.GovCloud,
            region,
            "fedramp-moderate-boundary",
            "private-vnet-a",
            "govstorage",
            "gov-postgres",
            "gov-kms",
            "gov-logs",
            "daily-pitr-35-days",
            PrivateNetworkingEnabled: true,
            StorageEncryptionEnabled: encrypted,
            DatabaseEncryptionEnabled: encrypted,
            CustomerManagedKeysEnabled: encrypted,
            AuditLoggingEnabled: true,
            ImmutableLoggingEnabled: true,
            BackupEnabled: true,
            RestoreTested: true);

    private WebApplicationFactory<Program> CreateFactory(string databaseName, Action<GccsDbContext>? seed = null) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<GovernmentCloudEnvironmentService>();
                services.AddScoped<IGovernmentCloudEnvironmentRepository, EfGovernmentCloudEnvironmentRepository>();
                services.AddScoped<IAuditEventWriter, EfAuditEventWriter>();

                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
                dbContext.Database.EnsureDeleted();
                dbContext.Database.EnsureCreated();
                seed?.Invoke(dbContext);
            });
        });

    private static HttpRequestMessage CreateRequest<TContent>(HttpMethod method, string requestUri, TContent? content, Guid tenantId, Guid userId, Permission permission)
    {
        var request = CreateRequest(method, requestUri, tenantId, userId, permission);
        if (content is not null)
        {
            request.Content = JsonContent.Create(content, options: JsonOptions);
        }

        return request;
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string requestUri, Guid tenantId, Guid userId, Permission permission)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", userId.ToString());
        request.Headers.Add("X-Gccs-Dev-Email", "admin@example.com");
        request.Headers.Add("X-Gccs-Dev-Permissions", permission.ToString());
        return request;
    }

    private static void SeedTenant(GccsDbContext dbContext, Guid tenantId)
    {
        dbContext.Tenants.Add(new TenantEntity
        {
            Id = tenantId,
            Name = $"Tenant {tenantId}",
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.Parse("2026-06-19T12:00:00Z")
        });
        dbContext.SaveChanges();
    }

    private static TestIds CreateTestIds(string suffix) =>
        new(Guid.NewGuid(), Guid.NewGuid());

    private sealed record TestIds(Guid TenantId, Guid ActorUserId);
}

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

public sealed class RegulatedTenantProvisioningTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public RegulatedTenantProvisioningTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_36_2_1_Create_request_persists_environment_data_mode_key_policy_support_and_migration()
    {
        var ids = new TestIds(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        await using var factory = CreateFactory("tc-36-2-1", dbContext => Seed(dbContext, ids, EnvironmentReadinessStatus.Approved));
        using var client = factory.CreateClient();

        var created = await CreateRequestAsync(client, ids);

        Assert.Equal("GovCloud Customer A", created.TenantName);
        Assert.Equal("DoD Supplier", created.CustomerType);
        Assert.Equal(ids.EnvironmentId, created.EnvironmentId);
        Assert.Equal(TenantDataPosture.CuiReady, created.DataHandlingMode);
        Assert.True(created.CuiApprovalComplete);
        Assert.Equal("customer-managed-key-required", created.KeyPolicy);
        Assert.Equal("us-person-support", created.SupportModel);
        Assert.Equal("commercial-tenant-migration", created.MigrationSource);
        Assert.Equal(RegulatedProvisioningStatus.Requested, created.Status);
    }

    [Fact]
    public async Task TC_36_2_2_Start_is_blocked_until_all_approvals_and_checklist_items_are_complete()
    {
        var ids = new TestIds(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        await using var factory = CreateFactory("tc-36-2-2", dbContext => Seed(dbContext, ids, EnvironmentReadinessStatus.Approved));
        using var client = factory.CreateClient();
        var created = await CreateRequestAsync(client, ids);

        var blocked = await client.SendAsync(CreateHttpRequest(HttpMethod.Post, $"/api/enterprise/regulated-tenant-provisioning/{created.Id}/start", ids.TenantId, ids.ActorUserId));
        await CompleteAllGatesAsync(client, ids, created.Id);
        var started = await client.SendAsync(CreateHttpRequest(HttpMethod.Post, $"/api/enterprise/regulated-tenant-provisioning/{created.Id}/start", ids.TenantId, ids.ActorUserId));
        var startedBody = await started.Content.ReadFromJsonAsync<RegulatedTenantProvisioningRequestDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, blocked.StatusCode);
        Assert.Equal(HttpStatusCode.OK, started.StatusCode);
        Assert.Equal(RegulatedProvisioningStatus.Provisioning, startedBody?.Status);
    }

    [Fact]
    public async Task TC_36_2_3_Provisioning_creates_tenant_only_in_approved_target_environment()
    {
        var ids = new TestIds(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var blockedEnvironmentId = Guid.NewGuid();
        await using var factory = CreateFactory("tc-36-2-3", dbContext =>
        {
            Seed(dbContext, ids, EnvironmentReadinessStatus.Approved);
            dbContext.GovernmentCloudEnvironments.Add(CreateEnvironment(ids.TenantId, blockedEnvironmentId, EnvironmentReadinessStatus.Blocked, "Blocked"));
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();
        var rejected = await client.SendAsync(CreateHttpRequest(
            HttpMethod.Post,
            "/api/enterprise/regulated-tenant-provisioning",
            ValidRequest(blockedEnvironmentId),
            ids.TenantId,
            ids.ActorUserId));
        var created = await CreateRequestAsync(client, ids);
        await CompleteAllGatesAsync(client, ids, created.Id);
        await client.SendAsync(CreateHttpRequest(HttpMethod.Post, $"/api/enterprise/regulated-tenant-provisioning/{created.Id}/start", ids.TenantId, ids.ActorUserId));

        var completed = await client.SendAsync(CreateHttpRequest(HttpMethod.Post, $"/api/enterprise/regulated-tenant-provisioning/{created.Id}/complete", ids.TenantId, ids.ActorUserId));
        var completedBody = await completed.Content.ReadFromJsonAsync<RegulatedTenantProvisioningRequestDto>(JsonOptions);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();

        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        Assert.Equal(HttpStatusCode.OK, completed.StatusCode);
        Assert.Equal(RegulatedProvisioningStatus.Ready, completedBody?.Status);
        Assert.NotNull(completedBody?.ProvisionedTenantId);
        Assert.True(await dbContext.Tenants.AnyAsync(tenant => tenant.Id == completedBody!.ProvisionedTenantId));
        Assert.Equal(ids.EnvironmentId, completedBody!.EnvironmentId);
    }

    [Fact]
    public async Task TC_36_2_4_Failed_provisioning_records_reason_rollback_decision_and_owner()
    {
        var ids = new TestIds(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        await using var factory = CreateFactory("tc-36-2-4", dbContext => Seed(dbContext, ids, EnvironmentReadinessStatus.Approved));
        using var client = factory.CreateClient();
        var created = await CreateRequestAsync(client, ids);

        var failure = await client.SendAsync(CreateHttpRequest(
            HttpMethod.Post,
            $"/api/enterprise/regulated-tenant-provisioning/{created.Id}/fail",
            new RegulatedProvisioningFailureRequest("Terraform apply failed.", "Rollback storage and tenant records.", "ops-owner"),
            ids.TenantId,
            ids.ActorUserId));
        var failed = await failure.Content.ReadFromJsonAsync<RegulatedTenantProvisioningRequestDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, failure.StatusCode);
        Assert.Equal(RegulatedProvisioningStatus.Failed, failed?.Status);
        Assert.Equal("Terraform apply failed.", failed?.FailureReason);
        Assert.Equal("Rollback storage and tenant records.", failed?.RollbackDecision);
        Assert.Equal("ops-owner", failed?.FailureOwner);
    }

    [Fact]
    public async Task TC_36_2_5_Lifecycle_changes_are_audit_logged()
    {
        var ids = new TestIds(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        await using var factory = CreateFactory("tc-36-2-5", dbContext => Seed(dbContext, ids, EnvironmentReadinessStatus.Approved));
        using var client = factory.CreateClient();
        var created = await CreateRequestAsync(client, ids);
        await CompleteAllGatesAsync(client, ids, created.Id);
        await client.SendAsync(CreateHttpRequest(HttpMethod.Post, $"/api/enterprise/regulated-tenant-provisioning/{created.Id}/start", ids.TenantId, ids.ActorUserId));
        await client.SendAsync(CreateHttpRequest(HttpMethod.Post, $"/api/enterprise/regulated-tenant-provisioning/{created.Id}/validation", ids.TenantId, ids.ActorUserId));
        await client.SendAsync(CreateHttpRequest(HttpMethod.Post, $"/api/enterprise/regulated-tenant-provisioning/{created.Id}/complete", ids.TenantId, ids.ActorUserId));
        await client.SendAsync(CreateHttpRequest(HttpMethod.Post, $"/api/enterprise/regulated-tenant-provisioning/{created.Id}/fail", new RegulatedProvisioningFailureRequest("post-ready issue", "no rollback", "ops"), ids.TenantId, ids.ActorUserId));
        await client.SendAsync(CreateHttpRequest(HttpMethod.Post, $"/api/enterprise/regulated-tenant-provisioning/{created.Id}/suspend", ids.TenantId, ids.ActorUserId));
        await client.SendAsync(CreateHttpRequest(HttpMethod.Post, $"/api/enterprise/regulated-tenant-provisioning/{created.Id}/retire", ids.TenantId, ids.ActorUserId));

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var audits = await dbContext.AuditLogEntries
            .Where(audit => audit.TenantId == ids.TenantId && audit.EntityType == "RegulatedTenantProvisioningRequest")
            .ToListAsync();

        Assert.Contains(audits, audit => audit.Action == AuditAction.Created && audit.Summary.Contains("created", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(audits, audit => audit.Action == AuditAction.Approved);
        Assert.Contains(audits, audit => audit.Summary.Contains("started", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(audits, audit => audit.Summary.Contains("validation", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(audits, audit => audit.Summary.Contains("provisioned", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(audits, audit => audit.Action == AuditAction.Rejected);
        Assert.Contains(audits, audit => audit.Summary.Contains("suspended", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(audits, audit => audit.Action == AuditAction.Archived);
    }

    private static async Task<RegulatedTenantProvisioningRequestDto> CreateRequestAsync(HttpClient client, TestIds ids)
    {
        var response = await client.SendAsync(CreateHttpRequest(HttpMethod.Post, "/api/enterprise/regulated-tenant-provisioning", ValidRequest(ids.EnvironmentId), ids.TenantId, ids.ActorUserId));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return Assert.IsType<RegulatedTenantProvisioningRequestDto>(await response.Content.ReadFromJsonAsync<RegulatedTenantProvisioningRequestDto>(JsonOptions));
    }

    private static async Task CompleteAllGatesAsync(HttpClient client, TestIds ids, Guid requestId)
    {
        foreach (var area in Enum.GetValues<RegulatedProvisioningApprovalArea>())
        {
            var approval = await client.SendAsync(CreateHttpRequest(HttpMethod.Post, $"/api/enterprise/regulated-tenant-provisioning/{requestId}/approvals", new RegulatedProvisioningApprovalRequest(area, $"{area} reviewer", "Approved."), ids.TenantId, ids.ActorUserId));
            Assert.Equal(HttpStatusCode.OK, approval.StatusCode);
        }

        foreach (var item in Enum.GetValues<RegulatedProvisioningChecklistItem>())
        {
            var checklist = await client.SendAsync(CreateHttpRequest(HttpMethod.Post, $"/api/enterprise/regulated-tenant-provisioning/{requestId}/checklist", new RegulatedProvisioningChecklistRequest(item, "Ops", $"evidence-{item}"), ids.TenantId, ids.ActorUserId));
            Assert.Equal(HttpStatusCode.OK, checklist.StatusCode);
        }
    }

    private static CreateRegulatedTenantProvisioningRequest ValidRequest(Guid environmentId) =>
        new("GovCloud Customer A", "DoD Supplier", environmentId, TenantDataPosture.CuiReady, true, "customer-managed-key-required", "us-person-support", "commercial-tenant-migration");

    private WebApplicationFactory<Program> CreateFactory(string databaseName, Action<GccsDbContext>? seed = null) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<RegulatedTenantProvisioningService>();
                services.AddScoped<IRegulatedTenantProvisioningRepository, EfRegulatedTenantProvisioningRepository>();
                services.AddScoped<IAuditEventWriter, EfAuditEventWriter>();

                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
                dbContext.Database.EnsureDeleted();
                dbContext.Database.EnsureCreated();
                seed?.Invoke(dbContext);
            });
        });

    private static HttpRequestMessage CreateHttpRequest<TContent>(HttpMethod method, string requestUri, TContent? content, Guid tenantId, Guid userId)
    {
        var request = CreateHttpRequest(method, requestUri, tenantId, userId);
        if (content is not null)
        {
            request.Content = JsonContent.Create(content, options: JsonOptions);
        }

        return request;
    }

    private static HttpRequestMessage CreateHttpRequest(HttpMethod method, string requestUri, Guid tenantId, Guid userId)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", userId.ToString());
        request.Headers.Add("X-Gccs-Dev-Email", "ops@example.com");
        request.Headers.Add("X-Gccs-Dev-Permissions", Permission.ManageTenant.ToString());
        return request;
    }

    private static void Seed(GccsDbContext dbContext, TestIds ids, EnvironmentReadinessStatus environmentStatus)
    {
        dbContext.Tenants.Add(new TenantEntity
        {
            Id = ids.TenantId,
            Name = "Operations Tenant",
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.Parse("2026-06-19T12:00:00Z")
        });
        dbContext.GovernmentCloudEnvironments.Add(CreateEnvironment(ids.TenantId, ids.EnvironmentId, environmentStatus, "Approved"));
        dbContext.SaveChanges();
    }

    private static GovernmentCloudEnvironmentEntity CreateEnvironment(Guid tenantId, Guid environmentId, EnvironmentReadinessStatus status, string name) =>
        new()
        {
            Id = environmentId,
            TenantId = tenantId,
            Name = name,
            EnvironmentType = EnvironmentDeploymentType.GovCloud,
            Region = "us-gov-west-1",
            Boundary = "fedramp-moderate",
            NetworkSegment = "private-vnet",
            StorageAccount = "storage",
            DatabaseService = "postgres",
            KeyManagementService = "kms",
            LoggingWorkspace = "logs",
            BackupPolicy = "daily",
            PrivateNetworkingEnabled = true,
            StorageEncryptionEnabled = true,
            DatabaseEncryptionEnabled = true,
            CustomerManagedKeysEnabled = true,
            AuditLoggingEnabled = true,
            ImmutableLoggingEnabled = true,
            BackupEnabled = true,
            RestoreTested = true,
            Status = status,
            CreatedAt = DateTimeOffset.Parse("2026-06-19T12:00:00Z")
        };

    private sealed record TestIds(Guid TenantId, Guid ActorUserId, Guid EnvironmentId);
}

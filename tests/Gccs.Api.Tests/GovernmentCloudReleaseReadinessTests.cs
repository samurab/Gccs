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

public sealed class GovernmentCloudReleaseReadinessTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public GovernmentCloudReleaseReadinessTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task TC_36_3_1_Incomplete_readiness_checklist_blocks_promotion()
    {
        var ids = Ids();
        await using var factory = CreateFactory("tc-36-3-1", db => Seed(db, ids));
        using var client = factory.CreateClient();
        var readiness = await CreateAsync(client, ids);

        var response = await ApproveAsync(client, ids, readiness.Id);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("checklist", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TC_36_3_2_Open_critical_gaps_block_release_approval()
    {
        var ids = Ids();
        await using var factory = CreateFactory("tc-36-3-2", db => Seed(db, ids));
        using var client = factory.CreateClient();
        var readiness = await CreateAsync(client, ids);
        await CompleteReadinessAsync(client, ids, readiness.Id);
        foreach (var area in Enum.GetValues<GovernmentCloudReleaseGapArea>())
        {
            await client.SendAsync(Request(HttpMethod.Post, $"/api/enterprise/government-cloud-release-readiness/{readiness.Id}/gaps", new GovernmentCloudReleaseGapRequest(area, GovernmentCloudReleaseGapSeverity.Critical, $"critical {area} gap"), ids));
        }

        var response = await ApproveAsync(client, ids, readiness.Id);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("critical", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TC_36_3_3_Readiness_record_links_required_operations_evidence()
    {
        var ids = Ids();
        await using var factory = CreateFactory("tc-36-3-3", db => Seed(db, ids));
        using var client = factory.CreateClient();
        var readiness = await CreateAsync(client, ids);
        var completed = await CompleteReadinessAsync(client, ids, readiness.Id);

        Assert.Equal(Enum.GetValues<GovernmentCloudReleaseChecklistItem>().Length, completed.CompletedChecklist.Length);
        foreach (var evidenceType in Enum.GetValues<GovernmentCloudReleaseEvidenceType>())
        {
            Assert.Contains(completed.EvidenceLinks, link => link.EvidenceType == evidenceType);
        }
    }

    [Fact]
    public async Task TC_36_3_4_Approved_release_deployment_stores_history_fields()
    {
        var ids = Ids();
        await using var factory = CreateFactory("tc-36-3-4", db => Seed(db, ids));
        using var client = factory.CreateClient();
        var readiness = await CreateAsync(client, ids);
        await CompleteReadinessAsync(client, ids, readiness.Id);
        var approval = await ApproveAsync(client, ids, readiness.Id);
        var approved = await approval.Content.ReadFromJsonAsync<GovernmentCloudReleaseReadinessDto>(JsonOptions);
        var deployment = await client.SendAsync(Request(HttpMethod.Post, $"/api/enterprise/government-cloud-release-readiness/{readiness.Id}/deploy", new GovernmentCloudReleaseDeploymentRequest("success", "rollback-not-needed"), ids));
        var deployed = await deployment.Content.ReadFromJsonAsync<GovernmentCloudReleaseReadinessDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, approval.StatusCode);
        Assert.Equal(GovernmentCloudReleaseStatus.Approved, approved?.Status);
        Assert.Equal(HttpStatusCode.OK, deployment.StatusCode);
        Assert.Equal(ids.EnvironmentId, deployed?.EnvironmentId);
        Assert.Equal("2026.06.19", deployed?.Version);
        Assert.Equal("Sunday 0200Z", deployed?.ReleaseWindow);
        Assert.Equal("ops-owner", deployed?.Owner);
        Assert.Equal("release-approver", deployed?.ApproverName);
        Assert.Equal("success", deployed?.Result);
        Assert.Equal("rollback-not-needed", deployed?.RollbackStatus);
    }

    [Fact]
    public async Task TC_36_3_5_Release_approval_and_deployment_actions_are_audit_logged()
    {
        var ids = Ids();
        await using var factory = CreateFactory("tc-36-3-5", db => Seed(db, ids));
        using var client = factory.CreateClient();
        var readiness = await CreateAsync(client, ids);
        await CompleteReadinessAsync(client, ids, readiness.Id);
        await ApproveAsync(client, ids, readiness.Id);
        await client.SendAsync(Request(HttpMethod.Post, $"/api/enterprise/government-cloud-release-readiness/{readiness.Id}/deploy", new GovernmentCloudReleaseDeploymentRequest("success", "rollback-not-needed"), ids));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var audits = await db.AuditLogEntries.Where(a => a.TenantId == ids.TenantId && a.EntityType == "GovernmentCloudReleaseReadiness").ToListAsync();

        Assert.Contains(audits, audit => audit.Action == AuditAction.Created);
        Assert.Contains(audits, audit => audit.Action == AuditAction.Approved);
        Assert.Contains(audits, audit => audit.Summary.Contains("deployed", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<GovernmentCloudReleaseReadinessDto> CreateAsync(HttpClient client, TestIds ids)
    {
        var response = await client.SendAsync(Request(HttpMethod.Post, "/api/enterprise/government-cloud-release-readiness", new CreateGovernmentCloudReleaseReadinessRequest(ids.EnvironmentId, "2026.06.19", "Sunday 0200Z", "ops-owner"), ids));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return Assert.IsType<GovernmentCloudReleaseReadinessDto>(await response.Content.ReadFromJsonAsync<GovernmentCloudReleaseReadinessDto>(JsonOptions));
    }

    private static async Task<GovernmentCloudReleaseReadinessDto> CompleteReadinessAsync(HttpClient client, TestIds ids, Guid readinessId)
    {
        GovernmentCloudReleaseReadinessDto? result = null;
        foreach (var item in Enum.GetValues<GovernmentCloudReleaseChecklistItem>())
        {
            var response = await client.SendAsync(Request(HttpMethod.Post, $"/api/enterprise/government-cloud-release-readiness/{readinessId}/checklist", new CompleteGovernmentCloudReleaseChecklistRequest(item, $"evidence-{item}"), ids));
            result = await response.Content.ReadFromJsonAsync<GovernmentCloudReleaseReadinessDto>(JsonOptions);
        }

        foreach (var evidenceType in Enum.GetValues<GovernmentCloudReleaseEvidenceType>())
        {
            var response = await client.SendAsync(Request(HttpMethod.Post, $"/api/enterprise/government-cloud-release-readiness/{readinessId}/evidence", new GovernmentCloudReleaseEvidenceRequest(evidenceType, $"https://evidence.example.com/{evidenceType}"), ids));
            result = await response.Content.ReadFromJsonAsync<GovernmentCloudReleaseReadinessDto>(JsonOptions);
        }

        return Assert.IsType<GovernmentCloudReleaseReadinessDto>(result);
    }

    private static Task<HttpResponseMessage> ApproveAsync(HttpClient client, TestIds ids, Guid readinessId) =>
        client.SendAsync(Request(HttpMethod.Post, $"/api/enterprise/government-cloud-release-readiness/{readinessId}/approve", new GovernmentCloudReleaseApprovalRequest("release-approver", "All readiness evidence reviewed."), ids));

    private WebApplicationFactory<Program> CreateFactory(string databaseName, Action<GccsDbContext> seed) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<GovernmentCloudReleaseReadinessService>();
                services.AddScoped<IGovernmentCloudReleaseReadinessRepository, EfGovernmentCloudReleaseReadinessRepository>();
                services.AddScoped<IAuditEventWriter, EfAuditEventWriter>();
                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
                seed(db);
            });
        });

    private static HttpRequestMessage Request<T>(HttpMethod method, string uri, T body, TestIds ids)
    {
        var request = Request(method, uri, ids);
        request.Content = JsonContent.Create(body, options: JsonOptions);
        return request;
    }

    private static HttpRequestMessage Request(HttpMethod method, string uri, TestIds ids)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", ids.TenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", ids.ActorUserId.ToString());
        request.Headers.Add("X-Gccs-Dev-Email", "ops@example.com");
        request.Headers.Add("X-Gccs-Dev-Permissions", Permission.ManageTenant.ToString());
        return request;
    }

    private static void Seed(GccsDbContext db, TestIds ids)
    {
        db.Tenants.Add(new TenantEntity { Id = ids.TenantId, Name = "Ops", Status = TenantStatus.Active, DataPosture = TenantDataPosture.NoCui, CreatedAt = DateTimeOffset.UtcNow });
        db.GovernmentCloudEnvironments.Add(new GovernmentCloudEnvironmentEntity
        {
            Id = ids.EnvironmentId,
            TenantId = ids.TenantId,
            Name = "GovCloud",
            EnvironmentType = EnvironmentDeploymentType.GovCloud,
            Region = "us-gov-west-1",
            Boundary = "fedramp",
            NetworkSegment = "private",
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
            Status = EnvironmentReadinessStatus.Approved,
            CreatedAt = DateTimeOffset.UtcNow
        });
        db.SaveChanges();
    }

    private static TestIds Ids() => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
    private sealed record TestIds(Guid TenantId, Guid ActorUserId, Guid EnvironmentId);
}

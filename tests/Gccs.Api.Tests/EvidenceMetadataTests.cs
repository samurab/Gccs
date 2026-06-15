using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Evidence;
using Gccs.Application.Security;
using Gccs.Domain.Audit;
using Gccs.Domain.Evidence;
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

public sealed class EvidenceMetadataTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public EvidenceMetadataTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_12_1_1_Creates_evidence_metadata_with_required_fields_tags_dates_and_source_links()
    {
        var tenantId = Guid.Parse("12112111-2112-1112-1211-2111211121a1");
        await using var factory = CreateFactory("tc-12-1-1", dbContext => SeedTenant(dbContext, tenantId));
        using var client = factory.CreateClient();
        var request = CreateRequestBody() with
        {
            Tags = ["policy", "access-control"],
            ObligationIds = ["obligation-fci-safeguards"],
            ControlIds = ["AC.L1-3.1.1"]
        };

        var evidence = await CreateEvidenceAsync(client, tenantId, request);

        Assert.Equal("Access control policy", evidence.Title);
        Assert.Equal(EvidenceType.Policy, evidence.Type);
        Assert.Equal("Security", evidence.OwnerFunction);
        Assert.Equal(EvidenceStatus.Requested, evidence.Status);
        Assert.Equal(new DateOnly(2026, 8, 15), evidence.ExpiresAt);
        Assert.Contains("policy", evidence.Tags);
        Assert.Contains("obligation-fci-safeguards", evidence.ObligationIds);
        Assert.Contains("AC.L1-3.1.1", evidence.ControlIds);
    }

    [Fact]
    public async Task TC_12_1_2_Links_evidence_to_multiple_obligations_and_controls_for_detail_reuse()
    {
        var tenantId = Guid.Parse("12112111-2112-1112-1211-2111211121a2");
        await using var factory = CreateFactory("tc-12-1-2", dbContext => SeedTenant(dbContext, tenantId));
        using var client = factory.CreateClient();
        var created = await CreateEvidenceAsync(client, tenantId, CreateRequestBody() with
        {
            ObligationIds = ["obligation-fci-safeguards", "obligation-bytedance"],
            ControlIds = ["AC.L1-3.1.1", "IA.L1-3.5.1"]
        });

        var detail = await GetEvidenceAsync(client, tenantId, created.Id);

        Assert.Equal(["obligation-bytedance", "obligation-fci-safeguards"], detail.ObligationIds.Order().ToArray());
        Assert.Equal(["AC.L1-3.1.1", "IA.L1-3.5.1"], detail.ControlIds.Order().ToArray());
    }

    [Fact]
    public async Task TC_12_1_3_Filters_evidence_by_folderless_tags()
    {
        var tenantId = Guid.Parse("12112111-2112-1112-1211-2111211121a3");
        await using var factory = CreateFactory("tc-12-1-3", dbContext => SeedTenant(dbContext, tenantId));
        using var client = factory.CreateClient();
        await CreateEvidenceAsync(client, tenantId, CreateRequestBody() with { Title = "Access review", Tags = ["access-review", "quarterly"] });
        await CreateEvidenceAsync(client, tenantId, CreateRequestBody() with { Title = "Incident record", Type = EvidenceType.IncidentRecord, Tags = ["incident"] });

        using var request = CreateRequest<object?>(HttpMethod.Get, "/api/evidence-items?tag=quarterly", null, tenantId, Permission.ViewEvidence);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var results = await response.Content.ReadFromJsonAsync<EvidenceMetadataDto[]>(JsonOptions) ?? [];
        var item = Assert.Single(results);
        Assert.Equal("Access review", item.Title);
        Assert.Contains("quarterly", item.Tags);
    }

    [Fact]
    public async Task TC_12_1_4_Evidence_expiration_generates_task_and_metadata_changes_are_audit_logged()
    {
        var tenantId = Guid.Parse("12112111-2112-1112-1211-2111211121a4");
        await using var factory = CreateFactory("tc-12-1-4", dbContext => SeedTenant(dbContext, tenantId));
        using var client = factory.CreateClient();
        var created = await CreateEvidenceAsync(client, tenantId, CreateRequestBody());

        var updated = await UpdateEvidenceAsync(client, tenantId, created.Id, CreateRequestBody() with
        {
            Title = "Access control policy updated",
            Status = EvidenceStatus.InReview,
            Tags = ["policy", "reviewed"]
        });

        Assert.Equal("Access control policy updated", updated.Title);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var task = await dbContext.ComplianceTasks.SingleAsync(task => task.TenantId == tenantId && task.EvidenceItemId == created.Id);
        Assert.Equal(new DateOnly(2026, 7, 16), task.DueAt);
        Assert.Equal("Security", task.OwnerFunction);

        var audits = await dbContext.AuditLogEntries
            .Where(audit => audit.TenantId == tenantId && audit.EntityType == "EvidenceItem" && audit.EntityId == created.Id.ToString())
            .OrderBy(audit => audit.OccurredAt)
            .ToArrayAsync();
        Assert.Contains(audits, audit => audit.Action == AuditAction.Created);
        Assert.Contains(audits, audit => audit.Action == AuditAction.Updated && audit.MetadataJson.Contains("reviewed", StringComparison.OrdinalIgnoreCase));
    }

    private async Task<EvidenceMetadataDto> CreateEvidenceAsync(
        HttpClient client,
        Guid tenantId,
        UpsertEvidenceMetadataRequest body)
    {
        using var request = CreateRequest(HttpMethod.Post, "/api/evidence-items", body, tenantId, Permission.ManageEvidence);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<EvidenceMetadataDto>(JsonOptions) ??
            throw new InvalidOperationException("Expected evidence metadata response.");
    }

    private async Task<EvidenceMetadataDto> UpdateEvidenceAsync(
        HttpClient client,
        Guid tenantId,
        Guid evidenceItemId,
        UpsertEvidenceMetadataRequest body)
    {
        using var request = CreateRequest(HttpMethod.Put, $"/api/evidence-items/{evidenceItemId}", body, tenantId, Permission.ManageEvidence);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<EvidenceMetadataDto>(JsonOptions) ??
            throw new InvalidOperationException("Expected evidence metadata response.");
    }

    private async Task<EvidenceMetadataDto> GetEvidenceAsync(HttpClient client, Guid tenantId, Guid evidenceItemId)
    {
        using var request = CreateRequest<object?>(HttpMethod.Get, $"/api/evidence-items/{evidenceItemId}", null, tenantId, Permission.ViewEvidence);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<EvidenceMetadataDto>(JsonOptions) ??
            throw new InvalidOperationException("Expected evidence metadata response.");
    }

    private WebApplicationFactory<Program> CreateFactory(string databaseName, Action<GccsDbContext>? seed = null) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<EvidenceMetadataService>();
                services.AddScoped<IEvidenceMetadataRepository, EfEvidenceMetadataRepository>();
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
        Permission permission)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", Guid.NewGuid().ToString());
        request.Headers.Add("X-Gccs-Dev-Permissions", permission.ToString());
        if (content is not null)
        {
            request.Content = JsonContent.Create(content, options: JsonOptions);
        }

        return request;
    }

    private static UpsertEvidenceMetadataRequest CreateRequestBody() =>
        new(
            "Access control policy",
            EvidenceType.Policy,
            "Security",
            EvidenceStatus.Requested,
            new DateOnly(2026, 1, 15),
            new DateOnly(2026, 8, 15),
            ["policy"],
            "Policy evidence for access control obligations.",
            [],
            [],
            [],
            [],
            [],
            [],
            []);

    private static void SeedTenant(GccsDbContext dbContext, Guid tenantId)
    {
        dbContext.Tenants.Add(new TenantEntity
        {
            Id = tenantId,
            Name = "Evidence Tenant",
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }
}

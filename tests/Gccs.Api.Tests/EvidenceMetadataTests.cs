using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Evidence;
using Gccs.Application.Security;
using Gccs.Domain.Audit;
using Gccs.Domain.Cmmc;
using Gccs.Domain.Compliance;
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
        Assert.Contains(audits, audit =>
            audit.Action == AuditAction.Created
            && audit.Summary == "Evidence metadata 'Access control policy' was created."
            && audit.MetadataJson.Contains("\"title\":\"Access control policy\"", StringComparison.Ordinal));
        Assert.Contains(audits, audit =>
            audit.Action == AuditAction.Updated
            && audit.Summary == "Evidence metadata 'Access control policy updated' was updated."
            && audit.MetadataJson.Contains("\"title\":\"Access control policy updated\"", StringComparison.Ordinal)
            && audit.MetadataJson.Contains("reviewed", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task TC_12_1_5_Rejects_unknown_control_id_before_database_save()
    {
        var tenantId = Guid.Parse("12112111-2112-1112-1211-2111211121a5");
        await using var factory = CreateFactory("tc-12-1-5", dbContext => SeedTenant(dbContext, tenantId));
        using var client = factory.CreateClient();
        using var request = CreateRequest(
            HttpMethod.Post,
            "/api/evidence-items",
            CreateRequestBody() with { ControlIds = ["CA.L2-3.12.4"] },
            tenantId,
            Permission.ManageEvidence);

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Control 'CA.L2-3.12.4' was not found", body);
    }

    [Fact]
    public async Task Rejects_create_when_expires_date_is_before_effective_date_without_side_effects()
    {
        var tenantId = Guid.Parse("12112111-2112-1112-1211-2111211121a6");
        await using var factory = CreateFactory("evidence-invalid-create-dates", dbContext => SeedTenant(dbContext, tenantId));
        using var client = factory.CreateClient();
        using var request = CreateRequest(
            HttpMethod.Post,
            "/api/evidence-items",
            CreateRequestBody() with
            {
                EffectiveAt = new DateOnly(2026, 8, 1),
                ExpiresAt = new DateOnly(2026, 7, 31)
            },
            tenantId,
            Permission.ManageEvidence);

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Expires date must be on or after Effective date.", body);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Empty(await dbContext.EvidenceItems.Where(item => item.TenantId == tenantId).ToArrayAsync());
        Assert.Empty(await dbContext.ComplianceTasks.Where(task => task.TenantId == tenantId).ToArrayAsync());
        Assert.Empty(await dbContext.AuditLogEntries.Where(audit => audit.TenantId == tenantId).ToArrayAsync());
    }

    [Fact]
    public async Task Rejects_update_when_expires_date_is_before_effective_date_and_preserves_existing_record()
    {
        var tenantId = Guid.Parse("12112111-2112-1112-1211-2111211121a7");
        await using var factory = CreateFactory("evidence-invalid-update-dates", dbContext => SeedTenant(dbContext, tenantId));
        using var client = factory.CreateClient();
        var created = await CreateEvidenceAsync(client, tenantId, CreateRequestBody());
        using var request = CreateRequest(
            HttpMethod.Put,
            $"/api/evidence-items/{created.Id}",
            CreateRequestBody() with
            {
                Title = "Invalid update must not persist",
                EffectiveAt = new DateOnly(2026, 8, 1),
                ExpiresAt = new DateOnly(2026, 7, 31)
            },
            tenantId,
            Permission.ManageEvidence);

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Expires date must be on or after Effective date.", body);
        var persisted = await GetEvidenceAsync(client, tenantId, created.Id);
        Assert.Equal(created.Title, persisted.Title);
        Assert.Equal(created.EffectiveAt, persisted.EffectiveAt);
        Assert.Equal(created.ExpiresAt, persisted.ExpiresAt);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Single(await dbContext.AuditLogEntries
            .Where(audit => audit.TenantId == tenantId && audit.EntityType == "EvidenceItem")
            .ToArrayAsync());
    }

    [Fact]
    public async Task Accepts_equal_or_individually_omitted_evidence_dates()
    {
        var tenantId = Guid.Parse("12112111-2112-1112-1211-2111211121a8");
        await using var factory = CreateFactory("evidence-date-boundaries", dbContext => SeedTenant(dbContext, tenantId));
        using var client = factory.CreateClient();
        var boundaryDate = new DateOnly(2026, 8, 1);

        var equalDates = await CreateEvidenceAsync(
            client,
            tenantId,
            CreateRequestBody() with { Title = "One-day evidence", EffectiveAt = boundaryDate, ExpiresAt = boundaryDate });
        var effectiveOnly = await CreateEvidenceAsync(
            client,
            tenantId,
            CreateRequestBody() with { Title = "Effective-only evidence", EffectiveAt = boundaryDate, ExpiresAt = null });
        var expirationOnly = await CreateEvidenceAsync(
            client,
            tenantId,
            CreateRequestBody() with { Title = "Expiration-only evidence", EffectiveAt = null, ExpiresAt = boundaryDate });

        Assert.Equal(boundaryDate, equalDates.ExpiresAt);
        Assert.Null(effectiveOnly.ExpiresAt);
        Assert.Null(expirationOnly.EffectiveAt);
    }

    [Fact]
    public async Task Updating_expiration_reschedules_one_active_renewal_task_and_cancels_stale_duplicates()
    {
        var tenantId = Guid.Parse("12112111-2112-1112-1211-2111211121a9");
        await using var factory = CreateFactory("evidence-renewal-reschedule", dbContext => SeedTenant(dbContext, tenantId));
        using var client = factory.CreateClient();
        var created = await CreateEvidenceAsync(client, tenantId, CreateRequestBody());

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
            dbContext.ComplianceTasks.Add(new ComplianceTaskEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Title = $"Renew evidence: {created.Title}",
                Description = "Stale duplicate renewal task.",
                Type = ComplianceTaskType.Renewal,
                Status = ComplianceTaskStatus.Open,
                RiskLevel = RiskLevel.Medium,
                OwnerFunction = created.OwnerFunction,
                DueAt = new DateOnly(2026, 1, 1),
                EvidenceItemId = created.Id,
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(1)
            });
            await dbContext.SaveChangesAsync();
        }

        var replacementExpiration = new DateOnly(2027, 1, 31);
        await UpdateEvidenceAsync(
            client,
            tenantId,
            created.Id,
            CreateRequestBody() with { ExpiresAt = replacementExpiration });

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
            var tasks = await dbContext.ComplianceTasks
                .Where(task => task.TenantId == tenantId && task.EvidenceItemId == created.Id)
                .OrderBy(task => task.CreatedAt)
                .ToArrayAsync();
            Assert.Equal(2, tasks.Length);
            var active = Assert.Single(tasks, task => task.Status != ComplianceTaskStatus.Canceled);
            Assert.Equal(replacementExpiration.AddDays(-30), active.DueAt);
            Assert.Contains($"{replacementExpiration:yyyy-MM-dd}", active.Description);
            Assert.Single(tasks, task => task.Status == ComplianceTaskStatus.Canceled);
        }

        await UpdateEvidenceAsync(
            client,
            tenantId,
            created.Id,
            CreateRequestBody() with { ExpiresAt = null });

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
            Assert.Empty(await dbContext.ComplianceTasks
                .Where(task =>
                    task.TenantId == tenantId &&
                    task.EvidenceItemId == created.Id &&
                    task.Status != ComplianceTaskStatus.Canceled)
                .ToArrayAsync());
        }
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

        dbContext.Controls.AddRange(
            CreateControl("AC.L1-3.1.1", CmmcLevel.Level1),
            CreateControl("IA.L1-3.5.1", CmmcLevel.Level1),
            CreateControl("AC.L2-3.1.3", CmmcLevel.Level2));
    }

    private static ControlEntity CreateControl(string id, CmmcLevel level) =>
        new()
        {
            Id = id,
            Framework = ControlFramework.Cmmc,
            CmmcLevel = level,
            Family = id[..2],
            Title = $"{id} synthetic control",
            Requirement = $"{id} synthetic requirement.",
            AssessmentObjective = $"{id} synthetic objective.",
            EvidenceExamplesJson = "[]",
            SourceName = "GCCS test fixture",
            SourceUrl = "https://example.invalid/gccs/test-control",
            SourceLastReviewedAt = new DateOnly(2026, 6, 18),
            SourceConfidence = "synthetic-test"
        };
}

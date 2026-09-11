using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Labor;
using Gccs.Application.NoCui;
using Gccs.Application.Security;
using Gccs.Application.Storage;
using Gccs.Application.Tenancy;
using Gccs.Domain.Audit;
using Gccs.Domain.Common;
using Gccs.Domain.Companies;
using Gccs.Domain.Compliance;
using Gccs.Domain.Contracts;
using Gccs.Domain.Evidence;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.NoCui;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class LaborApplicabilityWageDeterminationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
    private readonly WebApplicationFactory<Program> factory;
    public LaborApplicabilityWageDeterminationTests(WebApplicationFactory<Program> factory) => this.factory = factory;

    [Fact]
    public async Task TC_32_1_1_Record_and_update_persist_explicit_applicability_source_review_and_links()
    {
        var ids = StoryIds.Create();
        await using var app = CreateFactory(nameof(TC_32_1_1_Record_and_update_persist_explicit_applicability_source_review_and_links), ids);
        using var client = app.CreateClient();
        var created = await CreateAsync(client, ids, RequestBody(ids));
        Assert.True(created.ScaApplicable); Assert.False(created.DbaApplicable);
        Assert.Equal(ids.ClauseId, created.SourceContractClauseId);
        Assert.Equal(ids.EvidenceId, created.WageDeterminationEvidenceItemId);
        Assert.Equal(LaborApplicabilityReviewStatus.PendingReview, created.ReviewStatus);

        using var update = Request(HttpMethod.Put, $"/api/contracts/{ids.ContractId}/labor-applicabilities/{created.Id}",
            RequestBody(ids) with { DbaApplicable = true, ReviewStatus = LaborApplicabilityReviewStatus.Reviewed, ReviewNotes = "HR review completed." },
            ids.TenantId, ids.UserId, Permission.ManageContracts);
        using var response = await client.SendAsync(update);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = app.Services.CreateScope();
        var stored = await scope.ServiceProvider.GetRequiredService<GccsDbContext>().LaborApplicabilities.SingleAsync();
        Assert.True(stored.DbaApplicable); Assert.Equal("Norfolk, VA", stored.PlaceOfPerformance);
        Assert.Equal("WD-2015-4341 Rev 24", stored.WageDeterminationReference);
        Assert.Equal(ids.UserId, stored.ReviewedByUserId); Assert.NotNull(stored.ReviewedAt);
    }

    [Fact]
    public async Task TC_32_1_2_Wage_upload_enforces_acknowledgement_classification_scan_and_contract_link()
    {
        var ids = StoryIds.Create();
        await using var app = CreateFactory(nameof(TC_32_1_2_Wage_upload_enforces_acknowledgement_classification_scan_and_contract_link), ids);
        using var client = app.CreateClient();
        var created = await CreateAsync(client, ids, RequestBody(ids));

        using (var blocked = await UploadAsync(client, ids, created.Id))
            Assert.True(blocked.StatusCode == (HttpStatusCode)428, await blocked.Content.ReadAsStringAsync());
        await AcknowledgeAsync(client, ids);
        using (var prohibited = await UploadAsync(client, ids, created.Id, "Prohibited"))
            Assert.Equal(HttpStatusCode.BadRequest, prohibited.StatusCode);
        using (var rejectedScope = app.Services.CreateScope())
            Assert.Empty(await rejectedScope.ServiceProvider.GetRequiredService<GccsDbContext>().EvidenceFileVersions.ToArrayAsync());
        using var accepted = await UploadAsync(client, ids, created.Id);
        Assert.Equal(HttpStatusCode.Created, accepted.StatusCode);
        var file = await accepted.Content.ReadFromJsonAsync<EvidenceFileAccessDto>(JsonOptions);
        Assert.NotNull(file); Assert.Equal(ids.EvidenceId, file.EvidenceItemId);
        Assert.Equal("accepted", file.ValidationStatus); Assert.Equal("clean", file.MalwareScanStatus); Assert.True(file.IsUsable);

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.True(await db.Set<EvidenceContractEntity>().AnyAsync(x => x.EvidenceItemId == ids.EvidenceId && x.ContractId == ids.ContractId));
        Assert.Single(await db.EvidenceFileVersions.Where(x => x.EvidenceItemId == ids.EvidenceId).ToArrayAsync());
    }

    [Fact]
    public async Task TC_32_1_3_Missing_source_or_rationale_blocks_activation_without_task_or_activation_audit()
    {
        var ids = StoryIds.Create();
        await using var app = CreateFactory(nameof(TC_32_1_3_Missing_source_or_rationale_blocks_activation_without_task_or_activation_audit), ids);
        using var client = app.CreateClient();
        var body = RequestBody(ids) with { SourceContractClauseId = null, SourceClause = null, Rationale = " " };
        var created = await CreateAsync(client, ids, body);
        using var activate = Request(HttpMethod.Patch, $"/api/contracts/{ids.ContractId}/labor-applicabilities/{created.Id}/status",
            new UpdateLaborApplicabilityStatusRequest(LaborApplicabilityStatus.Active), ids.TenantId, ids.UserId, Permission.ManageContracts);
        using var response = await client.SendAsync(activate);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Empty(await db.ComplianceTasks.ToArrayAsync());
        Assert.Equal(LaborApplicabilityStatus.Draft, (await db.LaborApplicabilities.SingleAsync()).Status);
        Assert.Single(await db.AuditLogEntries.Where(x => x.EntityType == "LaborApplicability").ToArrayAsync());
    }

    [Fact]
    public async Task TC_32_1_4_Activation_creates_then_updates_one_linked_review_task()
    {
        var ids = StoryIds.Create();
        await using var app = CreateFactory(nameof(TC_32_1_4_Activation_creates_then_updates_one_linked_review_task), ids);
        using var client = app.CreateClient();
        var created = await CreateAsync(client, ids, RequestBody(ids));
        var first = await ChangeStatusAsync(client, ids, created.Id, LaborApplicabilityStatus.Active);
        Assert.NotNull(first.TaskId); Assert.Equal("WaitingForReview", first.ReviewTask?.Status);

        using (var invalidUpdate = Request(HttpMethod.Put, $"/api/contracts/{ids.ContractId}/labor-applicabilities/{created.Id}",
            RequestBody(ids) with { SourceContractClauseId = null, SourceClause = null, Rationale = null }, ids.TenantId, ids.UserId, Permission.ManageContracts))
            Assert.Equal(HttpStatusCode.BadRequest, (await client.SendAsync(invalidUpdate)).StatusCode);

        using (var update = Request(HttpMethod.Put, $"/api/contracts/{ids.ContractId}/labor-applicabilities/{created.Id}",
            RequestBody(ids) with { ContractPeriodEnd = new DateOnly(2027, 3, 31) }, ids.TenantId, ids.UserId, Permission.ManageContracts))
            Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(update)).StatusCode);
        var second = await ChangeStatusAsync(client, ids, created.Id, LaborApplicabilityStatus.Active);
        Assert.Equal(first.TaskId, second.TaskId); Assert.Equal(new DateOnly(2027, 3, 31), second.ReviewTask?.DueAt);

        using var scope = app.Services.CreateScope();
        Assert.Single(await scope.ServiceProvider.GetRequiredService<GccsDbContext>().ComplianceTasks.ToArrayAsync());
    }

    [Fact]
    public async Task TC_32_1_5_Create_update_activate_and_deactivate_are_audited()
    {
        var ids = StoryIds.Create();
        await using var app = CreateFactory(nameof(TC_32_1_5_Create_update_activate_and_deactivate_are_audited), ids);
        using var client = app.CreateClient();
        var created = await CreateAsync(client, ids, RequestBody(ids));
        using (var update = Request(HttpMethod.Put, $"/api/contracts/{ids.ContractId}/labor-applicabilities/{created.Id}",
            RequestBody(ids) with { PlaceOfPerformance = "Richmond, VA" }, ids.TenantId, ids.UserId, Permission.ManageContracts))
            Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(update)).StatusCode);
        await ChangeStatusAsync(client, ids, created.Id, LaborApplicabilityStatus.Active);
        await ChangeStatusAsync(client, ids, created.Id, LaborApplicabilityStatus.Inactive);

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var events = await db.AuditLogEntries.Where(x => x.EntityType == "LaborApplicability").OrderBy(x => x.OccurredAt).ToArrayAsync();
        Assert.Equal(4, events.Length); Assert.All(events, x => Assert.Equal(ids.TenantId, x.TenantId));
        Assert.Equal(ComplianceTaskStatus.Canceled, (await db.ComplianceTasks.SingleAsync()).Status);
    }

    [Fact]
    public async Task Tenant_scope_and_server_RBAC_fail_closed_without_cross_tenant_mutation()
    {
        var ids = StoryIds.Create();
        await using var app = CreateFactory(nameof(Tenant_scope_and_server_RBAC_fail_closed_without_cross_tenant_mutation), ids);
        using var client = app.CreateClient();
        using var forbidden = Request(HttpMethod.Post, $"/api/contracts/{ids.ContractId}/labor-applicabilities", RequestBody(ids), ids.TenantId, ids.UserId, Permission.ViewContracts);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(forbidden)).StatusCode);
        var created = await CreateAsync(client, ids, RequestBody(ids));
        using var crossTenant = Request<object?>(HttpMethod.Get, $"/api/contracts/{ids.ContractId}/labor-applicabilities", null, ids.OtherTenantId, ids.UserId, Permission.ViewContracts);
        Assert.Equal(HttpStatusCode.NotFound, (await client.SendAsync(crossTenant)).StatusCode);
        using var crossMutation = Request(HttpMethod.Patch, $"/api/contracts/{ids.ContractId}/labor-applicabilities/{created.Id}/status",
            new UpdateLaborApplicabilityStatusRequest(LaborApplicabilityStatus.Active), ids.OtherTenantId, ids.UserId, Permission.ManageContracts);
        Assert.Equal(HttpStatusCode.NotFound, (await client.SendAsync(crossMutation)).StatusCode);
        using var scope = app.Services.CreateScope();
        Assert.Equal(LaborApplicabilityStatus.Draft, (await scope.ServiceProvider.GetRequiredService<GccsDbContext>().LaborApplicabilities.SingleAsync()).Status);
    }

    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task PostgreSQL_audit_failure_rolls_back_labor_applicability_and_task()
    {
        var connectionString = Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION")
            ?? throw new InvalidOperationException("GCCS_TEST_POSTGRES_CONNECTION is required.");
        var ids = StoryIds.Create();
        await using var app = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<GccsDbContext>(); services.RemoveAll<DbContextOptions<GccsDbContext>>(); services.RemoveAll<IAuditEventWriter>();
                services.AddDbContext<GccsDbContext>(options => options.UseGccsPostgres(connectionString));
                services.AddScoped<IAuditEventWriter, FailingAuditWriter>();
                using var provider = services.BuildServiceProvider(); using var scope = provider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>(); PostgresTestDatabase.Migrate(db);
                db.Tenants.Add(Tenant(ids.TenantId));
                db.Contracts.Add(new ContractEntity
                {
                    Id = ids.ContractId, TenantId = ids.TenantId, ContractNumber = $"FA-{ids.ContractId:N}", Title = "Atomic labor contract",
                    AgencyOrPrimeName = "Department of Defense", Relationship = ContractorRelationship.Prime, Kind = ContractKind.FixedPrice,
                    Status = ContractStatus.Active, PeriodOfPerformanceStart = new(2026, 1, 1), PeriodOfPerformanceEnd = new(2027, 12, 31),
                    PlaceOfPerformance = "Norfolk, VA", Description = "Synthetic No-CUI fixture.", DataHandlingPosture = DataHandlingPosture.FciOnly,
                    CreatedAt = DateTimeOffset.UtcNow
                });
                db.SaveChanges();
            });
        });
        try
        {
            using var client = app.CreateClient();
            var body = RequestBody(ids) with { SourceContractClauseId = null, WageDeterminationEvidenceItemId = null };
            using var request = Request(HttpMethod.Post, $"/api/contracts/{ids.ContractId}/labor-applicabilities", body, ids.TenantId, ids.UserId, Permission.ManageContracts);
            using var response = await client.SendAsync(request); Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            await using var scope = app.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
            Assert.False(await db.LaborApplicabilities.AnyAsync(x => x.TenantId == ids.TenantId));
            Assert.False(await db.ComplianceTasks.AnyAsync(x => x.TenantId == ids.TenantId));
        }
        finally
        {
            await using var scope = app.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
            await db.AuditLogEntries.Where(x => x.TenantId == ids.TenantId).ExecuteDeleteAsync();
            await db.LaborApplicabilities.Where(x => x.TenantId == ids.TenantId).ExecuteDeleteAsync();
            await db.ComplianceTasks.Where(x => x.TenantId == ids.TenantId).ExecuteDeleteAsync();
            await db.Contracts.Where(x => x.TenantId == ids.TenantId).ExecuteDeleteAsync();
            await db.Tenants.Where(x => x.Id == ids.TenantId).ExecuteDeleteAsync();
        }
    }

    private WebApplicationFactory<Program> CreateFactory(string name, StoryIds ids) => factory.WithWebHostBuilder(builder =>
    {
        builder.UseSetting("LocalDependencies:Enabled", "false");
        builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
        builder.ConfigureServices(services =>
        {
            services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase($"labor-{name}"));
            services.AddScoped<IAuditEventWriter, EfAuditEventWriter>();
            services.AddScoped<INoCuiAcknowledgementRepository, EfNoCuiAcknowledgementRepository>();
            services.AddScoped<ITenantRepository, Gccs.Infrastructure.Tenancy.EfTenantRepository>();
            services.AddScoped<IDataHandlingNoticeAcknowledgementRepository, Gccs.Infrastructure.Tenancy.EfDataHandlingNoticeAcknowledgementRepository>();
            services.AddSingleton<IObjectStorageService, TestObjectStorage>();
            services.AddSingleton<IMalwareScanner>(new CleanScanner());
            using var provider = services.BuildServiceProvider(); using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>(); db.Database.EnsureDeleted(); db.Database.EnsureCreated();
            Seed(db, ids); db.SaveChanges();
        });
    });

    private static void Seed(GccsDbContext db, StoryIds ids)
    {
        db.Tenants.AddRange(Tenant(ids.TenantId), Tenant(ids.OtherTenantId));
        db.Contracts.Add(new ContractEntity
        {
            Id = ids.ContractId, TenantId = ids.TenantId, ContractNumber = "FA-32-1", Title = "Labor test contract",
            AgencyOrPrimeName = "Department of Defense", Relationship = ContractorRelationship.Prime,
            Kind = ContractKind.FixedPrice, Status = ContractStatus.Active, PeriodOfPerformanceStart = new(2026, 1, 1),
            PeriodOfPerformanceEnd = new(2027, 12, 31), PlaceOfPerformance = "Norfolk, VA",
            Description = "Synthetic No-CUI labor fixture.", DataHandlingPosture = DataHandlingPosture.FciOnly, CreatedAt = DateTimeOffset.UtcNow
        });
        db.Set<ContractClauseEntity>().Add(new ContractClauseEntity
        {
            Id = ids.ClauseId, ContractId = ids.ContractId, ClauseLibraryId = "far-52.222-41", ClauseNumber = "FAR 52.222-41",
            Title = "Service Contract Labor Standards", Source = ClauseSource.Far, SourceUrl = "https://www.acquisition.gov/far/52.222-41",
            AttachmentReason = "Synthetic test fixture", LastReviewedAt = new(2026, 1, 1), CreatedAt = DateTimeOffset.UtcNow
        });
        db.EvidenceItems.Add(new EvidenceItemEntity
        {
            Id = ids.EvidenceId, TenantId = ids.TenantId, Name = "Wage determination WD-2015-4341",
            Description = "Synthetic public wage determination fixture.", Type = EvidenceType.Other,
            OwnerFunction = "Contracts/HR", Status = EvidenceStatus.InReview, Classification = ContentClassification.Unclassified,
            CreatedAt = DateTimeOffset.UtcNow
        });
        db.Set<EvidenceContractEntity>().Add(new EvidenceContractEntity { EvidenceItemId = ids.EvidenceId, ContractId = ids.ContractId });
    }

    private static TenantEntity Tenant(Guid id) => new() { Id = id, Name = $"Tenant {id:N}", Status = TenantStatus.Active, DataPosture = TenantDataPosture.NoCui, CreatedAt = DateTimeOffset.UtcNow };
    private static LaborApplicabilityRequest RequestBody(StoryIds ids) => new(ids.ContractId, true, false, null, "Norfolk, VA",
        new(2026, 1, 1), new(2026, 12, 31), "WD-2015-4341 Rev 24", ids.EvidenceId, ids.ClauseId,
        "FAR 52.222-41", "Contract-file review supports the applicability decision.", "Contracts/HR",
        LaborApplicabilityReviewStatus.PendingReview, "Pending HR review.");

    private static async Task<LaborApplicabilityDto> CreateAsync(HttpClient client, StoryIds ids, LaborApplicabilityRequest body)
    {
        using var request = Request(HttpMethod.Post, $"/api/contracts/{ids.ContractId}/labor-applicabilities", body, ids.TenantId, ids.UserId, Permission.ManageContracts);
        using var response = await client.SendAsync(request); Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<LaborApplicabilityDto>(JsonOptions))!;
    }

    private static async Task<LaborApplicabilityDto> ChangeStatusAsync(HttpClient client, StoryIds ids, Guid id, LaborApplicabilityStatus status)
    {
        using var request = Request(HttpMethod.Patch, $"/api/contracts/{ids.ContractId}/labor-applicabilities/{id}/status",
            new UpdateLaborApplicabilityStatusRequest(status), ids.TenantId, ids.UserId, Permission.ManageContracts);
        using var response = await client.SendAsync(request); Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<LaborApplicabilityDto>(JsonOptions))!;
    }

    private static async Task AcknowledgeAsync(HttpClient client, StoryIds ids)
    {
        using (var request = Request(HttpMethod.Post, "/api/no-cui-acknowledgement", new AcknowledgeNoCuiRequest(true, NoCuiNotice.CurrentVersion), ids.TenantId, ids.UserId, Permission.ManageEvidence))
            Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(request)).StatusCode);
        using var notice = Request(HttpMethod.Post, $"/api/tenants/{ids.TenantId}/data-handling-notice-acknowledgements",
            new AcknowledgeDataHandlingNoticeRequest(TenantDataPosture.NoCui, "EvidenceUpload", "no-cui-general", "2026.06.phase1a", true), ids.TenantId, ids.UserId, Permission.ManageEvidence);
        Assert.Equal(HttpStatusCode.Created, (await client.SendAsync(notice)).StatusCode);
    }

    private static Task<HttpResponseMessage> UploadAsync(HttpClient client, StoryIds ids, Guid applicabilityId, string classification = "Unclassified")
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/contracts/{ids.ContractId}/labor-applicabilities/{applicabilityId}/wage-determination/file");
        AddAuth(request, ids.TenantId, ids.UserId, Permission.ManageEvidence);
        var bytes = new ByteArrayContent(Encoding.UTF8.GetBytes("Synthetic public wage determination content.")); bytes.Headers.ContentType = new("text/plain");
        request.Content = new MultipartFormDataContent
        {
            { new StringContent(classification), "classification" }, { new StringContent("true"), "noCuiAttestation" },
            { new StringContent($"User selected {classification} for the synthetic wage determination."), "classificationReason" }, { bytes, "file", "wd-2015-4341.txt" }
        };
        return client.SendAsync(request);
    }

    private static HttpRequestMessage Request<T>(HttpMethod method, string uri, T? body, Guid tenantId, Guid userId, Permission permission)
    {
        var request = new HttpRequestMessage(method, uri); AddAuth(request, tenantId, userId, permission);
        if (body is not null) request.Content = JsonContent.Create(body, options: JsonOptions);
        return ClassifiedWorkflowTestData.Confirm(request);
    }

    private static void AddAuth(HttpRequestMessage request, Guid tenantId, Guid userId, Permission permission)
    {
        request.Headers.Add("X-Gccs-Dev-Auth", "true"); request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", userId.ToString()); request.Headers.Add("X-Gccs-Dev-Permissions", permission.ToString());
    }

    private sealed record StoryIds(Guid TenantId, Guid OtherTenantId, Guid ContractId, Guid ClauseId, Guid EvidenceId, Guid UserId)
    {
        public static StoryIds Create() => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
    }

    private sealed class CleanScanner : IMalwareScanner
    {
        public Task<MalwareScanResult> ScanAsync(MalwareScanRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(MalwareScanResult.Clean("labor-test-scanner", "clean"));
    }

    private sealed class TestObjectStorage : IObjectStorageService
    {
        private readonly Dictionary<string, byte[]> objects = [];
        public async Task<ObjectStorageWriteResult> UploadAsync(ObjectStorageWriteRequest request, CancellationToken cancellationToken = default)
        {
            using var buffer = new MemoryStream(); await request.Content.CopyToAsync(buffer, cancellationToken);
            var name = ObjectStorageNames.BuildTenantBlobName(request.TenantId, request.ObjectName); objects[name] = buffer.ToArray();
            return new(request.Container, name, new Uri($"https://storage.test/{name}"), "test", DateTimeOffset.UtcNow);
        }
        public Task<ObjectStorageReadResult?> OpenReadAsync(ObjectStorageReadRequest request, CancellationToken cancellationToken = default) => Task.FromResult<ObjectStorageReadResult?>(null);
        public Task<bool> ExistsAsync(ObjectStorageReadRequest request, CancellationToken cancellationToken = default) => Task.FromResult(objects.ContainsKey(ObjectStorageNames.BuildTenantBlobName(request.TenantId, request.ObjectName)));
        public Task<bool> DeleteAsync(ObjectStorageReadRequest request, CancellationToken cancellationToken = default) => Task.FromResult(objects.Remove(ObjectStorageNames.BuildTenantBlobName(request.TenantId, request.ObjectName)));
    }

    private sealed class FailingAuditWriter : IAuditEventWriter
    {
        public Task WriteAsync(Guid tenantId, Guid actorUserId, AuditAction action, string entityType, string entityId,
            string summary, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default) =>
            throw new AuditWriteException("Synthetic labor audit persistence failure.");
    }
}

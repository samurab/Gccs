using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Application.Contracts;
using Gccs.Application.Evidence;
using Gccs.Application.NoCui;
using Gccs.Application.Security;
using Gccs.Application.Tenancy;
using Gccs.Domain.Audit;
using Gccs.Domain.Companies;
using Gccs.Domain.Common;
using Gccs.Domain.Contracts;
using Gccs.Domain.Evidence;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Contracts;
using Gccs.Infrastructure.NoCui;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Gccs.Infrastructure.Tenancy;
using Gccs.Infrastructure.Evidence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class TenantModeWorkflowEnforcementTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public TenantModeWorkflowEnforcementTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_1A_1_2_1_DemoSandbox_rejects_real_cui_upload_and_allows_synthetic_policy()
    {
        var ids = StoryIds.ForCase("tc-1a-1-2-1");
        await using var factory = CreateFactory("tc-1a-1-2-1", dbContext =>
        {
            SeedTenant(dbContext, ids.TenantId, TenantDataPosture.DemoSandbox);
            SeedContract(dbContext, ids);
            SeedAcknowledgement(dbContext, ids);
        });
        using var client = factory.CreateClient();
        using var upload = CreateRequest(
            HttpMethod.Post,
            $"/api/contracts/{ids.ContractId}/documents",
            new ContractDocumentUploadRequest(ContractDocumentType.Contract, "customer-cui.pdf", "application/pdf", 2048, true, CuiClassification()),
            ids.TenantId,
            ids.ActorUserId,
            Permission.ManageContracts);

        var response = await client.SendAsync(upload);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("tenant_data_handling_mode_restricted", body, StringComparison.Ordinal);

        var policy = CreatePolicy(TenantDataPosture.DemoSandbox, ids);
        await policy.EnsureAllowedAsync(
            new TenantDataHandlingModePolicyRequest(
                TenantDataHandlingWorkflow.ContractDocumentUpload,
                ContainsRealCui: true,
                ContainsSyntheticCui: true,
                EntityType: "ContractDocument",
                EntityId: "synthetic-demo-seed"),
            ids.ActorUserId);
    }

    [Fact]
    public async Task TC_1A_1_2_2_NoCui_blocks_real_cui_create_upload_process_and_evidence_submission()
    {
        var ids = StoryIds.ForCase("tc-1a-1-2-2");
        await using var factory = CreateFactory("tc-1a-1-2-2", dbContext =>
        {
            SeedTenant(dbContext, ids.TenantId, TenantDataPosture.NoCui);
            SeedContract(dbContext, ids);
            SeedPotentialCuiDocument(dbContext, ids);
            SeedEvidenceRequest(dbContext, ids);
            SeedAcknowledgement(dbContext, ids);
        });
        using var client = factory.CreateClient();

        using var createContract = CreateRequest(
            HttpMethod.Post,
            "/api/contracts",
            CreateContractRequest(DataHandlingPosture.Cui),
            ids.TenantId,
            ids.ActorUserId,
            Permission.ManageContracts);
        using var uploadEvidence = CreateRequest(
            HttpMethod.Post,
            $"/api/evidence-items/{ids.EvidenceItemId}/upload-intents",
            new EvidenceUploadIntentRequest("evidence-cui.pdf", "application/pdf", 2048, ContainsPotentialCui: true),
            ids.TenantId,
            ids.ActorUserId,
            Permission.ManageEvidence);
        using var submitEvidence = CreateRequest(
            HttpMethod.Put,
            $"/api/evidence-requests/{ids.EvidenceRequestId}/submit",
            new SubmitEvidenceRequestRequest(ids.EvidenceItemId, true, "Potential CUI evidence."),
            ids.TenantId,
            ids.ActorUserId,
            Permission.ManageEvidence);
        using var startExtraction = CreateRequest<object?>(
            HttpMethod.Post,
            $"/api/contracts/{ids.ContractId}/documents/{ids.DocumentId}/extraction-jobs",
            null,
            ids.TenantId,
            ids.ActorUserId,
            Permission.ManageContracts);

        var responses = new[]
        {
            await client.SendAsync(createContract),
            await client.SendAsync(uploadEvidence),
            await client.SendAsync(submitEvidence),
            await client.SendAsync(startExtraction)
        };

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode));
    }

    [Fact]
    public async Task TC_1A_1_2_3_CuiReady_requires_classification_and_approval_checks()
    {
        var ids = StoryIds.ForCase("tc-1a-1-2-3");
        await using var factory = CreateFactory("tc-1a-1-2-3", dbContext => SeedTenant(dbContext, ids.TenantId, TenantDataPosture.CuiReady));

        var policy = CreatePolicy(TenantDataPosture.CuiReady, ids);

        await Assert.ThrowsAsync<TenantDataHandlingModeRestrictedException>(() =>
            policy.EnsureAllowedAsync(
                new TenantDataHandlingModePolicyRequest(
                    TenantDataHandlingWorkflow.EvidenceUpload,
                    ContainsRealCui: true,
                    ClassificationConfirmed: false,
                    ApprovalChecksPassed: true),
                ids.ActorUserId));
        await Assert.ThrowsAsync<TenantDataHandlingModeRestrictedException>(() =>
            policy.EnsureAllowedAsync(
                new TenantDataHandlingModePolicyRequest(
                    TenantDataHandlingWorkflow.EvidenceUpload,
                    ContainsRealCui: true,
                    ClassificationConfirmed: true,
                    ApprovalChecksPassed: false),
                ids.ActorUserId));
        await policy.EnsureAllowedAsync(
            new TenantDataHandlingModePolicyRequest(
                TenantDataHandlingWorkflow.EvidenceUpload,
                ContainsRealCui: true,
                ClassificationConfirmed: true,
                ApprovalChecksPassed: true),
            ids.ActorUserId);
    }

    [Fact]
    public async Task TC_1A_1_2_4_Direct_api_restricted_calls_match_server_side_mode_checks()
    {
        var ids = StoryIds.ForCase("tc-1a-1-2-4");
        await using var factory = CreateFactory("tc-1a-1-2-4", dbContext =>
        {
            SeedTenant(dbContext, ids.TenantId, TenantDataPosture.NoCui);
            SeedContract(dbContext, ids);
            SeedPotentialCuiDocument(dbContext, ids);
            SeedEvidenceRequest(dbContext, ids);
            SeedAcknowledgement(dbContext, ids);
        });
        using var client = factory.CreateClient();

        var directCalls = new[]
        {
            CreateRequest(HttpMethod.Post, "/api/contracts", CreateContractRequest(DataHandlingPosture.Cui), ids.TenantId, ids.ActorUserId, Permission.ManageContracts),
            CreateRequest(HttpMethod.Post, $"/api/contracts/{ids.ContractId}/documents", new ContractDocumentUploadRequest(ContractDocumentType.Contract, "direct-cui.pdf", "application/pdf", 1024, true, CuiClassification()), ids.TenantId, ids.ActorUserId, Permission.ManageContracts),
            CreateRequest(HttpMethod.Put, $"/api/evidence-requests/{ids.EvidenceRequestId}/submit", new SubmitEvidenceRequestRequest(ids.EvidenceItemId, true, "Direct CUI"), ids.TenantId, ids.ActorUserId, Permission.ManageEvidence),
            CreateRequest<object?>(HttpMethod.Post, $"/api/contracts/{ids.ContractId}/documents/{ids.DocumentId}/extraction-jobs", null, ids.TenantId, ids.ActorUserId, Permission.ManageContracts)
        };

        foreach (var request in directCalls)
        {
            using (request)
            {
                var response = await client.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();

                Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
                Assert.Contains("tenant_data_handling_mode_restricted", body, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public async Task TC_1A_1_2_5_Mode_enforcement_failure_returns_clear_error_and_audit_event()
    {
        var ids = StoryIds.ForCase("tc-1a-1-2-5");
        await using var factory = CreateFactory("tc-1a-1-2-5", dbContext =>
        {
            SeedTenant(dbContext, ids.TenantId, TenantDataPosture.NoCui);
            SeedContract(dbContext, ids);
        });
        using var client = factory.CreateClient();
        using var request = CreateRequest(
            HttpMethod.Post,
            "/api/contracts",
            CreateContractRequest(DataHandlingPosture.Cui),
            ids.TenantId,
            ids.ActorUserId,
            Permission.ManageContracts);

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("Tenant data handling mode restricted", body, StringComparison.Ordinal);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var audit = await dbContext.AuditLogEntries.SingleAsync(entry =>
            entry.TenantId == ids.TenantId &&
            entry.ActorUserId == ids.ActorUserId &&
            entry.EntityType == "TenantDataHandlingModePolicy");
        var metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(audit.MetadataJson) ?? [];

        Assert.Equal(AuditAction.Rejected, audit.Action);
        Assert.Equal("ContractIntake", metadata["workflow"]);
        Assert.Equal("NoCui", metadata["mode"]);
        Assert.Equal("Rejected", metadata["result"]);
    }

    private WebApplicationFactory<Program> CreateFactory(string databaseName, Action<GccsDbContext>? seed = null) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<TenantDataHandlingModePolicyService>();
                services.AddScoped<ITenantRepository, EfTenantRepository>();
                services.AddScoped<ContractService>();
                services.AddScoped<IContractRepository, EfContractRepository>();
                services.AddScoped<IExtractionJobQueue, NoOpExtractionJobQueue>();
                services.AddScoped<IContractDocumentTextExtractor, DefaultContractDocumentTextExtractor>();
                services.AddScoped<NoCuiAcknowledgementService>();
                services.AddScoped<INoCuiAcknowledgementRepository, EfNoCuiAcknowledgementRepository>();
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

    private static HttpRequestMessage CreateRequest<TContent>(
        HttpMethod method,
        string requestUri,
        TContent? content,
        Guid tenantId,
        Guid userId,
        params Permission[] permissions)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", userId.ToString());
        request.Headers.Add("X-Gccs-Dev-Permissions", string.Join(",", permissions.Select(permission => permission.ToString())));
        if (content is not null)
        {
            request.Content = JsonContent.Create(content, options: JsonOptions);
        }

        return request;
    }

    private static UpsertContractRequest CreateContractRequest(DataHandlingPosture posture) =>
        new(
            "CUI-TEST-001",
            "CUI workflow test contract",
            "Department of Defense",
            ContractorRelationship.Prime,
            ContractKind.FixedPrice,
            ContractStatus.Active,
            new DateOnly(2026, 6, 18),
            new DateOnly(2026, 7, 1),
            new DateOnly(2027, 6, 30),
            "Arlington, VA",
            "Mode enforcement test contract.",
            posture);

    private static ContentClassificationRequest CuiClassification() =>
        new(ContentClassification.Cui, Reason: "Direct API test marked real CUI.");

    private static void SeedTenant(GccsDbContext dbContext, Guid tenantId, TenantDataPosture mode)
    {
        dbContext.Tenants.Add(new TenantEntity
        {
            Id = tenantId,
            Name = $"Story 1A.1.2 Tenant {tenantId:N}",
            Status = TenantStatus.Active,
            DataPosture = mode,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    private static void SeedContract(GccsDbContext dbContext, StoryIds ids)
    {
        dbContext.Contracts.Add(new ContractEntity
        {
            Id = ids.ContractId,
            TenantId = ids.TenantId,
            ContractNumber = $"STORY-{ids.ContractId:N}"[..18],
            Title = "Mode enforcement seed contract",
            AgencyOrPrimeName = "Sample Prime",
            Relationship = ContractorRelationship.Prime,
            Kind = ContractKind.FixedPrice,
            Status = ContractStatus.Active,
            AwardedAt = new DateOnly(2026, 6, 18),
            PeriodOfPerformanceStart = new DateOnly(2026, 7, 1),
            PeriodOfPerformanceEnd = new DateOnly(2027, 6, 30),
            PlaceOfPerformance = "Remote",
            Description = "Seeded direct API enforcement contract.",
            DataHandlingPosture = DataHandlingPosture.FciOnly,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    private static void SeedPotentialCuiDocument(GccsDbContext dbContext, StoryIds ids)
    {
        dbContext.Set<ContractDocumentEntity>().Add(new ContractDocumentEntity
        {
            Id = ids.DocumentId,
            ContractId = ids.ContractId,
            Type = ContractDocumentType.Contract,
            FileName = "seeded-cui-contract.txt",
            ContentType = "text/plain",
            SizeBytes = 128,
            StorageUri = $"pending://contracts/{ids.ContractId}/documents/{ids.DocumentId}/seeded-cui-contract.txt",
            ValidationStatus = "accepted",
            MalwareScanStatus = "scan-pending",
            NoticeVersion = NoCuiNotice.CurrentVersion,
            UploadedAt = DateTimeOffset.UtcNow,
            UploadedByUserId = ids.ActorUserId,
            ContainsPotentialCui = true
        });
    }

    private static void SeedEvidenceRequest(GccsDbContext dbContext, StoryIds ids)
    {
        dbContext.EvidenceItems.Add(new EvidenceItemEntity
        {
            Id = ids.EvidenceItemId,
            TenantId = ids.TenantId,
            Name = "Mode enforcement evidence",
            Description = "Evidence for mode enforcement tests.",
            Type = EvidenceType.Policy,
            OwnerFunction = "Compliance",
            Status = EvidenceStatus.Uploaded,
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.EvidenceRequests.Add(new EvidenceRequestEntity
        {
            Id = ids.EvidenceRequestId,
            TenantId = ids.TenantId,
            RequesterUserId = ids.ActorUserId,
            AssigneeUserId = ids.ActorUserId,
            DueDate = new DateOnly(2026, 9, 1),
            Status = EvidenceRequestStatus.Open.ToString(),
            Priority = EvidenceRequestPriority.Normal.ToString(),
            Instructions = "Submit mode enforcement evidence.",
            RelatedRecordType = EvidenceRequestRelatedRecordType.Obligation.ToString(),
            RelatedRecordId = "story-1a-1-2",
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    private static void SeedAcknowledgement(GccsDbContext dbContext, StoryIds ids)
    {
        dbContext.NoCuiAcknowledgements.Add(new NoCuiAcknowledgementEntity
        {
            Id = Guid.NewGuid(),
            TenantId = ids.TenantId,
            UserId = ids.ActorUserId,
            NoticeVersion = NoCuiNotice.CurrentVersion,
            NoticeCopy = NoCuiNotice.Copy,
            AcknowledgedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = ids.ActorUserId
        });
    }

    private static TenantDataHandlingModePolicyService CreatePolicy(TenantDataPosture mode, StoryIds ids)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ITenantRepository>(new FixedTenantRepository(mode));
        return new TenantDataHandlingModePolicyService(
            services.BuildServiceProvider(),
            new FixedTenantContext(ids.TenantId, ids.ActorUserId),
            new NoOpAuditEventWriter());
    }

    private sealed record StoryIds(
        Guid TenantId,
        Guid ActorUserId,
        Guid ContractId,
        Guid DocumentId,
        Guid EvidenceItemId,
        Guid EvidenceRequestId)
    {
        public static StoryIds ForCase(string caseName)
        {
            var suffix = Math.Abs(caseName.GetHashCode(StringComparison.Ordinal)) % 10000;
            return new StoryIds(
                Guid.Parse($"1a121212-1212-1212-1212-12121211{suffix:D4}"),
                Guid.Parse($"1a121212-1212-1212-1212-12121212{suffix:D4}"),
                Guid.Parse($"1a121212-1212-1212-1212-12121213{suffix:D4}"),
                Guid.Parse($"1a121212-1212-1212-1212-12121214{suffix:D4}"),
                Guid.Parse($"1a121212-1212-1212-1212-12121215{suffix:D4}"),
                Guid.Parse($"1a121212-1212-1212-1212-12121216{suffix:D4}"));
        }
    }

    private sealed class FixedTenantRepository(TenantDataPosture mode) : ITenantRepository
    {
        public Task<Tenant?> FindInCurrentTenantScopeAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Tenant?>(null);

        public Task<TenantDataPosture?> FindCurrentTenantDataHandlingModeAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<TenantDataPosture?>(mode);

        public Task<IReadOnlyList<TenantDataHandlingModeHistoryDto>> ListDataHandlingModeHistoryInCurrentTenantScopeAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TenantDataHandlingModeHistoryDto>>([]);

        public Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task AddDataHandlingModeHistoryAsync(Guid tenantId, TenantDataPosture? previousMode, TenantDataPosture newMode, Guid actorUserId, string reason, string? approvalRecordReference, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<Tenant?> UpdateStatusInCurrentTenantScopeAsync(Guid tenantId, TenantStatus status, CancellationToken cancellationToken = default) =>
            Task.FromResult<Tenant?>(null);

        public Task<Tenant?> UpdateDataHandlingModeInCurrentTenantScopeAsync(Guid tenantId, TenantDataPosture dataHandlingMode, Guid actorUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Tenant?>(null);
    }

    private sealed record FixedTenantContext(Guid TenantId, Guid UserId) : ICurrentTenantContext
    {
        public string UserEmail => "story-1a-1-2@example.test";
    }

    private sealed class NoOpAuditEventWriter : IAuditEventWriter
    {
        public Task WriteAsync(
            Guid tenantId,
            Guid actorUserId,
            AuditAction action,
            string entityType,
            string entityId,
            string summary,
            IReadOnlyDictionary<string, string>? metadata = null,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}

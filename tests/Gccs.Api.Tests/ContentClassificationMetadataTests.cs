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
using Gccs.Infrastructure.Evidence;
using Gccs.Infrastructure.NoCui;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Gccs.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class ContentClassificationMetadataTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public ContentClassificationMetadataTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_1A_2_1_1_Document_metadata_requires_classification()
    {
        var ids = StoryIds.ForCase("tc-1a-2-1-1");
        await using var factory = CreateFactory("tc-1a-2-1-1", dbContext => SeedTenantContractAndAcknowledgement(dbContext, ids, TenantDataPosture.NoCui));
        using var client = factory.CreateClient();
        using var request = CreateRequest(
            HttpMethod.Post,
            $"/api/contracts/{ids.ContractId}/documents",
            new ContractDocumentUploadRequest(ContractDocumentType.Contract, "missing-classification.pdf", "application/pdf", 1024, false),
            ids.TenantId,
            ids.ActorUserId,
            Permission.ManageContracts);

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("content_classification_invalid", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TC_1A_2_1_2_Cui_classification_is_rejected_outside_cui_ready()
    {
        var noCuiIds = StoryIds.ForCase("tc-1a-2-1-2-no-cui");
        var demoIds = StoryIds.ForCase("tc-1a-2-1-2-demo");
        await using var factory = CreateFactory("tc-1a-2-1-2", dbContext =>
        {
            SeedTenantContractAndAcknowledgement(dbContext, noCuiIds, TenantDataPosture.NoCui);
            SeedTenantContractAndAcknowledgement(dbContext, demoIds, TenantDataPosture.DemoSandbox);
        });
        using var client = factory.CreateClient();

        foreach (var ids in new[] { noCuiIds, demoIds })
        {
            using var request = CreateRequest(
                HttpMethod.Post,
                $"/api/contracts/{ids.ContractId}/documents",
                DocumentRequest("customer-cui.pdf", ContentClassification.Cui),
                ids.TenantId,
                ids.ActorUserId,
                Permission.ManageContracts);
            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    [Fact]
    public async Task TC_1A_2_1_3_SyntheticCui_requires_approved_demo_seed_content()
    {
        var ids = StoryIds.ForCase("tc-1a-2-1-3");
        await using var factory = CreateFactory("tc-1a-2-1-3", dbContext => SeedTenantContractAndAcknowledgement(dbContext, ids, TenantDataPosture.DemoSandbox));
        using var client = factory.CreateClient();
        using var customerSynthetic = CreateRequest(
            HttpMethod.Post,
            $"/api/contracts/{ids.ContractId}/documents",
            DocumentRequest("customer-synthetic.pdf", ContentClassification.SyntheticCui),
            ids.TenantId,
            ids.ActorUserId,
            Permission.ManageContracts);
        using var approvedDemo = CreateRequest(
            HttpMethod.Post,
            $"/api/contracts/{ids.ContractId}/documents",
            new ContractDocumentUploadRequest(
                ContractDocumentType.Contract,
                "approved-demo-cui.pdf",
                "application/pdf",
                1024,
                true,
                new ContentClassificationRequest(
                    ContentClassification.SyntheticCui,
                    ContentClassificationSource.ImportedDemoSeed,
                    Confidence: 1m,
                    Reason: "Approved Phase 1A demo seed content.",
                    IsApprovedDemoContent: true)),
            ids.TenantId,
            ids.ActorUserId,
            Permission.ManageContracts);

        var rejected = await client.SendAsync(customerSynthetic);
        var accepted = await client.SendAsync(approvedDemo);

        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        Assert.Equal(HttpStatusCode.Created, accepted.StatusCode);
    }

    [Fact]
    public async Task TC_1A_2_1_4_Unknown_classification_blocks_downstream_extraction()
    {
        var ids = StoryIds.ForCase("tc-1a-2-1-4");
        await using var factory = CreateFactory("tc-1a-2-1-4", dbContext =>
        {
            SeedTenantContractAndAcknowledgement(dbContext, ids, TenantDataPosture.NoCui);
            SeedDocument(dbContext, ids, ContentClassification.Unknown);
        });
        using var client = factory.CreateClient();
        using var request = CreateRequest<object?>(
            HttpMethod.Post,
            $"/api/contracts/{ids.ContractId}/documents/{ids.DocumentId}/extraction-jobs",
            null,
            ids.TenantId,
            ids.ActorUserId,
            Permission.ManageContracts);

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Unknown classification", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TC_1A_2_1_5_Reclassification_preserves_history_metadata()
    {
        var ids = StoryIds.ForCase("tc-1a-2-1-5");
        await using var factory = CreateFactory("tc-1a-2-1-5", dbContext => SeedTenant(dbContext, ids.TenantId, TenantDataPosture.NoCui));
        using var client = factory.CreateClient();
        var createBody = EvidenceRequest("Evidence policy", Unclassified("Initial classification."));
        using var create = CreateRequest(HttpMethod.Post, "/api/evidence-items", createBody, ids.TenantId, ids.ActorUserId, Permission.ManageEvidence);
        var createResponse = await client.SendAsync(create);
        var evidence = await createResponse.Content.ReadFromJsonAsync<EvidenceMetadataDto>(JsonOptions);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(evidence);

        var reviewedAt = DateTimeOffset.Parse("2026-06-18T18:00:00Z");
        using var update = CreateRequest(
            HttpMethod.Put,
            $"/api/evidence-items/{evidence.Id}",
            EvidenceRequest(
                "Evidence policy",
                new ContentClassificationRequest(
                    ContentClassification.Fci,
                    ContentClassificationSource.AdminReviewed,
                    Confidence: 0.96m,
                    ReviewedByUserId: ids.ActorUserId,
                    ReviewedAt: reviewedAt,
                    Reason: "Reviewed as FCI, not CUI.")),
            ids.TenantId,
            ids.ActorUserId,
            Permission.ManageEvidence);

        var updateResponse = await client.SendAsync(update);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var history = await dbContext.ContentClassificationHistory
            .Where(item => item.TenantId == ids.TenantId && item.EntityId == evidence.Id.ToString())
            .OrderBy(item => item.ChangedAt)
            .ToArrayAsync();

        Assert.Equal(2, history.Length);
        Assert.Null(history[0].PreviousClassification);
        Assert.Equal(ContentClassification.Unclassified, history[0].NewClassification);
        Assert.Equal(ContentClassification.Unclassified, history[1].PreviousClassification);
        Assert.Equal(ContentClassification.Fci, history[1].NewClassification);
        Assert.Equal(ContentClassificationSource.AdminReviewed, history[1].Source);
        Assert.Equal(0.96m, history[1].Confidence);
        Assert.Equal(ids.ActorUserId, history[1].ReviewedByUserId);
        Assert.Equal(reviewedAt, history[1].ReviewedAt);
        Assert.Equal("Reviewed as FCI, not CUI.", history[1].Reason);
    }

    [Fact]
    public async Task Evidence_metadata_rejects_cross_tenant_contract_references()
    {
        var tenantA = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var tenantB = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var actorUserId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var contractB = Guid.Parse("44444444-4444-4444-4444-444444444444");
        await using var factory = CreateFactory("evidence-cross-tenant-contract-reference", dbContext =>
        {
            SeedTenant(dbContext, tenantA, TenantDataPosture.NoCui);
            SeedTenant(dbContext, tenantB, TenantDataPosture.NoCui);
            dbContext.Contracts.Add(new ContractEntity
            {
                Id = contractB,
                TenantId = tenantB,
                ContractNumber = "CROSS-TENANT",
                Title = "Cross tenant contract",
                AgencyOrPrimeName = "Sample Prime",
                Relationship = ContractorRelationship.Prime,
                Kind = ContractKind.FixedPrice,
                Status = ContractStatus.Active,
                AwardedAt = new DateOnly(2026, 6, 23),
                PeriodOfPerformanceStart = new DateOnly(2026, 7, 1),
                PeriodOfPerformanceEnd = new DateOnly(2027, 6, 30),
                PlaceOfPerformance = "Remote",
                Description = "Contract owned by another tenant.",
                DataHandlingPosture = DataHandlingPosture.FciOnly,
                CreatedAt = DateTimeOffset.Parse("2026-06-23T12:00:00Z")
            });
        });
        using var client = factory.CreateClient();
        var body = new UpsertEvidenceMetadataRequest(
            "Cross tenant linked evidence",
            EvidenceType.Policy,
            "Compliance",
            EvidenceStatus.Draft,
            null,
            null,
            ["tenant-scope"],
            "Evidence with an invalid cross-tenant contract reference.",
            [],
            [],
            [contractB],
            [],
            [],
            [],
            [],
            Unclassified("No CUI in this metadata-only test."));
        using var request = CreateRequest(
            HttpMethod.Post,
            "/api/evidence-items",
            body,
            tenantA,
            actorUserId,
            Permission.ManageEvidence);

        var response = await client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Evidence references must belong to the current tenant", responseBody, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(contractB.ToString(), responseBody, StringComparison.OrdinalIgnoreCase);
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
                services.AddScoped<ContentClassificationPolicy>();
                services.AddScoped<ITenantRepository, EfTenantRepository>();
                services.AddScoped<ContractService>();
                services.AddScoped<IContractRepository, EfContractRepository>();
                services.AddScoped<IExtractionJobQueue, NoOpExtractionJobQueue>();
                services.AddScoped<IContractDocumentTextExtractor, DefaultContractDocumentTextExtractor>();
                services.AddScoped<NoCuiAcknowledgementService>();
                services.AddScoped<INoCuiAcknowledgementRepository, EfNoCuiAcknowledgementRepository>();
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

    private static ContractDocumentUploadRequest DocumentRequest(string fileName, ContentClassification classification) =>
        new(
            ContractDocumentType.Contract,
            fileName,
            "application/pdf",
            1024,
            classification is ContentClassification.Cui or ContentClassification.SyntheticCui,
            new ContentClassificationRequest(classification, Reason: $"Test {classification} classification."));

    private static UpsertEvidenceMetadataRequest EvidenceRequest(string title, ContentClassificationRequest classification) =>
        new(
            title,
            EvidenceType.Policy,
            "Compliance",
            EvidenceStatus.Draft,
            null,
            null,
            ["classification"],
            "Classification metadata test evidence.",
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            classification);

    private static ContentClassificationRequest Unclassified(string reason) =>
        new(ContentClassification.Unclassified, Reason: reason);

    private static void SeedTenantContractAndAcknowledgement(GccsDbContext dbContext, StoryIds ids, TenantDataPosture mode)
    {
        SeedTenant(dbContext, ids.TenantId, mode);
        dbContext.Contracts.Add(new ContractEntity
        {
            Id = ids.ContractId,
            TenantId = ids.TenantId,
            ContractNumber = $"CC-{ids.ContractId:N}"[..16],
            Title = "Classification metadata contract",
            AgencyOrPrimeName = "Sample Prime",
            Relationship = ContractorRelationship.Prime,
            Kind = ContractKind.FixedPrice,
            Status = ContractStatus.Active,
            AwardedAt = new DateOnly(2026, 6, 18),
            PeriodOfPerformanceStart = new DateOnly(2026, 7, 1),
            PeriodOfPerformanceEnd = new DateOnly(2027, 6, 30),
            PlaceOfPerformance = "Remote",
            Description = "Seeded classification metadata contract.",
            DataHandlingPosture = DataHandlingPosture.FciOnly,
            CreatedAt = DateTimeOffset.UtcNow
        });
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

    private static void SeedTenant(GccsDbContext dbContext, Guid tenantId, TenantDataPosture mode)
    {
        dbContext.Tenants.Add(new TenantEntity
        {
            Id = tenantId,
            Name = $"Classification Tenant {tenantId:N}",
            Status = TenantStatus.Active,
            DataPosture = mode,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    private static void SeedDocument(GccsDbContext dbContext, StoryIds ids, ContentClassification classification)
    {
        dbContext.Set<ContractDocumentEntity>().Add(new ContractDocumentEntity
        {
            Id = ids.DocumentId,
            ContractId = ids.ContractId,
            Type = ContractDocumentType.Contract,
            FileName = "unknown-classification.txt",
            ContentType = "text/plain",
            SizeBytes = 128,
            StorageUri = $"pending://contracts/{ids.ContractId}/documents/{ids.DocumentId}/unknown-classification.txt",
            ValidationStatus = "accepted",
            MalwareScanStatus = "scan-pending",
            NoticeVersion = NoCuiNotice.CurrentVersion,
            UploadedAt = DateTimeOffset.UtcNow,
            UploadedByUserId = ids.ActorUserId,
            ContainsPotentialCui = false,
            Classification = classification,
            ClassificationSource = ContentClassificationSource.UserSelected,
            ClassificationReason = "Seeded downstream blocking test."
        });
    }

    private sealed record StoryIds(Guid TenantId, Guid ActorUserId, Guid ContractId, Guid DocumentId)
    {
        public static StoryIds ForCase(string caseName)
        {
            var suffix = Math.Abs(caseName.GetHashCode(StringComparison.Ordinal)) % 10000;
            return new StoryIds(
                Guid.Parse($"1a212121-2121-2121-2121-21212111{suffix:D4}"),
                Guid.Parse($"1a212121-2121-2121-2121-21212112{suffix:D4}"),
                Guid.Parse($"1a212121-2121-2121-2121-21212113{suffix:D4}"),
                Guid.Parse($"1a212121-2121-2121-2121-21212114{suffix:D4}"));
        }
    }
}

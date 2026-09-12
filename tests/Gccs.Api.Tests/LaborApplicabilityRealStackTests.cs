using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Storage.Blobs;
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
using Gccs.Infrastructure.NoCui;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Gccs.Infrastructure.Storage;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class LaborApplicabilityRealStackTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> factory;

    public LaborApplicabilityRealStackTests(WebApplicationFactory<Program> factory) => this.factory = factory;

    [Story32RealStackFact]
    [Trait("Category", "RealStackIntegration")]
    public async Task Wage_upload_uses_PostgreSQL_ClamAV_and_Azurite_and_cleans_up_after_audit_rollback()
    {
        var ids = StoryIds.Create();
        var failure = new AuditFailureInterceptor();
        var storageConnection = RequiredEnvironment("ConnectionStrings__AzureStorage");
        await using var app = CreateFactory(ids, failure);
        var blobContainer = new BlobServiceClient(storageConnection).GetBlobContainerClient("evidence");

        try
        {
            using var client = app.CreateClient();
            var labor = await CreateLaborAsync(client, ids);

            using (var scope = app.Services.CreateScope())
            {
                Assert.IsType<ClamAvMalwareScanner>(scope.ServiceProvider.GetRequiredService<IMalwareScanner>());
                Assert.IsType<AzureBlobObjectStorageService>(scope.ServiceProvider.GetRequiredService<IObjectStorageService>());
            }

            using (var malicious = await UploadAsync(client, ids, labor.Id, EicarTestPayload(), "eicar.txt"))
                Assert.Equal(HttpStatusCode.BadRequest, malicious.StatusCode);
            Assert.Equal(0, await CountTenantBlobsAsync(blobContainer, ids.TenantId));

            using (var clean = await UploadAsync(client, ids, labor.Id, "Synthetic public wage determination.", "wage-determination.txt"))
                Assert.Equal(HttpStatusCode.Created, clean.StatusCode);
            Assert.Equal(1, await CountTenantBlobsAsync(blobContainer, ids.TenantId));

            failure.Enabled = true;
            using (var rolledBack = await UploadAsync(client, ids, labor.Id, "Synthetic replacement wage determination.", "replacement.txt"))
                Assert.Equal(HttpStatusCode.InternalServerError, rolledBack.StatusCode);
            failure.Enabled = false;

            Assert.Equal(1, await CountTenantBlobsAsync(blobContainer, ids.TenantId));
            using var verification = app.Services.CreateScope();
            var db = verification.ServiceProvider.GetRequiredService<GccsDbContext>();
            Assert.Single(await db.EvidenceFileVersions.Where(x => x.EvidenceItemId == ids.EvidenceId).ToArrayAsync());
            Assert.Single(await db.AuditLogEntries.Where(x => x.TenantId == ids.TenantId &&
                x.EntityType == "EvidenceUploadIntent" && x.Action == AuditAction.Rejected).ToArrayAsync());
        }
        finally
        {
            failure.Enabled = false;
            await DeleteTenantBlobsAsync(blobContainer, ids.TenantId);
            await DeleteFixturesAsync(app, ids);
        }
    }

    private WebApplicationFactory<Program> CreateFactory(StoryIds ids, AuditFailureInterceptor failure) =>
        factory.WithWebHostBuilder(builder =>
        {
            var connectionString = RequiredEnvironment("GCCS_TEST_POSTGRES_CONNECTION");
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ObjectCleanupProcessing:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", connectionString);
            builder.UseSetting("ConnectionStrings:AzureStorage", RequiredEnvironment("ConnectionStrings__AzureStorage"));
            builder.UseSetting("MalwareScanning:Enabled", "true");
            builder.UseSetting("MalwareScanning:Host", RequiredEnvironment("MalwareScanning__Host"));
            builder.UseSetting("MalwareScanning:Port", RequiredEnvironment("MalwareScanning__Port"));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<GccsDbContext>();
                services.RemoveAll<DbContextOptions<GccsDbContext>>();
                services.AddDbContext<GccsDbContext>(options => options.UseGccsPostgres(connectionString).AddInterceptors(failure));

                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
                PostgresTestDatabase.Migrate(db);
                Seed(db, ids);
                db.SaveChanges();
            });
        });

    private static void Seed(GccsDbContext db, StoryIds ids)
    {
        db.Tenants.Add(new TenantEntity
        {
            Id = ids.TenantId, Name = $"Story 32 real stack {ids.TenantId:N}", Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui, CreatedAt = DateTimeOffset.UtcNow
        });
        db.Contracts.Add(new ContractEntity
        {
            Id = ids.ContractId, TenantId = ids.TenantId, ContractNumber = $"FA-{ids.ContractId:N}", Title = "Real-stack labor contract",
            AgencyOrPrimeName = "Department of Labor", Relationship = ContractorRelationship.Prime, Kind = ContractKind.FixedPrice,
            Status = ContractStatus.Active, PeriodOfPerformanceStart = new(2026, 1, 1), PeriodOfPerformanceEnd = new(2027, 12, 31),
            PlaceOfPerformance = "Norfolk, VA", Description = "Synthetic No-CUI provider test fixture.",
            DataHandlingPosture = DataHandlingPosture.FciOnly, CreatedAt = DateTimeOffset.UtcNow
        });
        db.Set<ContractClauseEntity>().Add(new ContractClauseEntity
        {
            Id = ids.ClauseId, ContractId = ids.ContractId, ClauseLibraryId = "far-52.222-41", ClauseNumber = "FAR 52.222-41",
            Title = "Service Contract Labor Standards", Source = ClauseSource.Far, SourceUrl = "https://www.acquisition.gov/far/52.222-41",
            AttachmentReason = "Synthetic provider test fixture", LastReviewedAt = new(2026, 1, 1), CreatedAt = DateTimeOffset.UtcNow
        });
        db.EvidenceItems.Add(new EvidenceItemEntity
        {
            Id = ids.EvidenceId, TenantId = ids.TenantId, Name = "Real-stack wage determination",
            Description = "Synthetic No-CUI provider test fixture.", Type = EvidenceType.Other, OwnerFunction = "Contracts/HR",
            Status = EvidenceStatus.InReview, Classification = ContentClassification.Unclassified, CreatedAt = DateTimeOffset.UtcNow
        });
        db.Set<EvidenceContractEntity>().Add(new EvidenceContractEntity { EvidenceItemId = ids.EvidenceId, ContractId = ids.ContractId });
        db.NoCuiAcknowledgements.Add(new NoCuiAcknowledgementEntity
        {
            Id = Guid.NewGuid(), TenantId = ids.TenantId, UserId = ids.UserId, NoticeVersion = NoCuiNotice.CurrentVersion,
            NoticeCopy = NoCuiNotice.Copy, AcknowledgedAt = DateTimeOffset.UtcNow, CreatedAt = DateTimeOffset.UtcNow
        });
        db.DataHandlingNoticeAcknowledgements.Add(new DataHandlingNoticeAcknowledgementEntity
        {
            Id = Guid.NewGuid(), TenantId = ids.TenantId, UserId = ids.UserId, Mode = TenantDataPosture.NoCui,
            WorkflowContext = "EvidenceUpload", NoticeId = "no-cui-general", NoticeVersion = "2026.06.phase1a",
            AcknowledgedAt = DateTimeOffset.UtcNow, CreatedAt = DateTimeOffset.UtcNow
        });
    }

    private static async Task<LaborApplicabilityDto> CreateLaborAsync(HttpClient client, StoryIds ids)
    {
        var body = new LaborApplicabilityRequest(ids.ContractId, true, false, null, "Norfolk, VA",
            new(2026, 1, 1), new(2026, 12, 31), "WD-2015-4341 Rev 24", ids.EvidenceId, ids.ClauseId,
            "FAR 52.222-41", "Synthetic real-stack verification rationale.", "Contracts/HR",
            LaborApplicabilityReviewStatus.PendingReview, "Provider verification pending.");
        using var request = Request(HttpMethod.Post, $"/api/contracts/{ids.ContractId}/labor-applicabilities", body,
            ids.TenantId, ids.UserId, Permission.ManageContracts);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<LaborApplicabilityDto>(JsonOptions))!;
    }

    private static Task<HttpResponseMessage> UploadAsync(HttpClient client, StoryIds ids, Guid applicabilityId, string content, string fileName)
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            $"/api/contracts/{ids.ContractId}/labor-applicabilities/{applicabilityId}/wage-determination/file");
        AddAuth(request, ids.TenantId, ids.UserId, Permission.ManageEvidence);
        var bytes = new ByteArrayContent(Encoding.ASCII.GetBytes(content));
        bytes.Headers.ContentType = new("text/plain");
        request.Content = new MultipartFormDataContent
        {
            { new StringContent("Unclassified"), "classification" },
            { new StringContent("true"), "noCuiAttestation" },
            { new StringContent("Synthetic real-stack provider verification."), "classificationReason" },
            { bytes, "file", fileName }
        };
        return client.SendAsync(request);
    }

    private static HttpRequestMessage Request<T>(HttpMethod method, string uri, T body, Guid tenantId, Guid userId, Permission permission)
    {
        var request = new HttpRequestMessage(method, uri) { Content = JsonContent.Create(body, options: JsonOptions) };
        AddAuth(request, tenantId, userId, permission);
        return ClassifiedWorkflowTestData.Confirm(request);
    }

    private static void AddAuth(HttpRequestMessage request, Guid tenantId, Guid userId, Permission permission)
    {
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", userId.ToString());
        request.Headers.Add("X-Gccs-Dev-Permissions", permission.ToString());
    }

    private static string EicarTestPayload() =>
        "X5O!P%@AP[4\\PZX54(P^)7CC)7}$EICAR-" + "STANDARD-ANTIVIRUS-TEST-FILE!$H+H*";

    private static async Task<int> CountTenantBlobsAsync(BlobContainerClient container, Guid tenantId)
    {
        if (!await container.ExistsAsync()) return 0;
        var count = 0;
        await foreach (var _ in container.GetBlobsAsync(prefix: $"tenants/{tenantId:D}/")) count++;
        return count;
    }

    private static async Task DeleteTenantBlobsAsync(BlobContainerClient container, Guid tenantId)
    {
        if (!await container.ExistsAsync()) return;
        await foreach (var blob in container.GetBlobsAsync(prefix: $"tenants/{tenantId:D}/"))
            await container.DeleteBlobIfExistsAsync(blob.Name);
    }

    private static async Task DeleteFixturesAsync(WebApplicationFactory<Program> app, StoryIds ids)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        await db.AuditLogEntries.Where(x => x.TenantId == ids.TenantId).ExecuteDeleteAsync();
        await db.ContentClassificationHistory.Where(x => x.TenantId == ids.TenantId).ExecuteDeleteAsync();
        await db.LaborApplicabilities.Where(x => x.TenantId == ids.TenantId).ExecuteDeleteAsync();
        await db.ComplianceTasks.Where(x => x.TenantId == ids.TenantId).ExecuteDeleteAsync();
        await db.EvidenceFileVersions.Where(x => x.EvidenceItemId == ids.EvidenceId).ExecuteDeleteAsync();
        await db.Set<EvidenceContractEntity>().Where(x => x.EvidenceItemId == ids.EvidenceId).ExecuteDeleteAsync();
        await db.EvidenceItems.Where(x => x.Id == ids.EvidenceId).ExecuteDeleteAsync();
        await db.NoCuiAcknowledgements.Where(x => x.TenantId == ids.TenantId).ExecuteDeleteAsync();
        await db.DataHandlingNoticeAcknowledgements.Where(x => x.TenantId == ids.TenantId).ExecuteDeleteAsync();
        await db.Set<ContractClauseEntity>().Where(x => x.ContractId == ids.ContractId).ExecuteDeleteAsync();
        await db.Contracts.Where(x => x.Id == ids.ContractId).ExecuteDeleteAsync();
        await db.Tenants.Where(x => x.Id == ids.TenantId).ExecuteDeleteAsync();
    }

    private static string RequiredEnvironment(string name) =>
        Environment.GetEnvironmentVariable(name) ?? throw new InvalidOperationException($"{name} is required for the Story 32 real-stack test.");

    private sealed record StoryIds(Guid TenantId, Guid ContractId, Guid ClauseId, Guid EvidenceId, Guid UserId)
    {
        public static StoryIds Create() => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
    }

    private sealed class AuditFailureInterceptor : SaveChangesInterceptor
    {
        public bool Enabled { get; set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (Enabled && eventData.Context!.ChangeTracker.Entries<AuditLogEntryEntity>().Any(x => x.State == EntityState.Added))
                throw new InvalidOperationException("Injected Story 32 audit persistence failure.");
            return ValueTask.FromResult(result);
        }
    }
}

internal sealed class Story32RealStackFactAttribute : FactAttribute
{
    public Story32RealStackFactAttribute()
    {
        var required = new[]
        {
            "GCCS_TEST_POSTGRES_CONNECTION", "ConnectionStrings__AzureStorage", "MalwareScanning__Host", "MalwareScanning__Port"
        };
        var missing = required.Where(name => string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(name))).ToArray();
        if (missing.Length > 0) Skip = $"Set the Story 32 real-stack environment values: {string.Join(", ", missing)}.";
    }
}

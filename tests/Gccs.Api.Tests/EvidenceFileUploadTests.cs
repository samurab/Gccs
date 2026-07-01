using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.NoCui;
using Gccs.Application.Security;
using Gccs.Application.Storage;
using Gccs.Domain.Audit;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.NoCui;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class EvidenceFileUploadTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public EvidenceFileUploadTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_12_2_1_Upload_before_no_cui_acknowledgement_is_blocked()
    {
        var tenantId = Guid.Parse("12212212-2122-1221-2212-2122122122a1");
        var userId = Guid.Parse("12212212-2122-1221-2212-2122122122b1");
        await using var factory = CreateFactory("tc-12-2-1", dbContext => SeedTenant(dbContext, tenantId));
        using var client = factory.CreateClient();

        using var request = CreateRequest(
            HttpMethod.Post,
            $"/api/evidence-items/{Guid.NewGuid()}/upload-intents",
            CreateUploadRequest("policy.pdf"),
            tenantId,
            userId,
            Permission.ManageEvidence);
        var response = await client.SendAsync(request);

        Assert.Equal((HttpStatusCode)428, response.StatusCode);
    }

    [Fact]
    public async Task TC_12_2_2_Uploaded_file_is_not_usable_until_validation_and_scan_allow_it()
    {
        var tenantId = Guid.Parse("12212212-2122-1221-2212-2122122122a2");
        var userId = Guid.Parse("12212212-2122-1221-2212-2122122122b2");
        var evidenceItemId = Guid.Parse("12212212-2122-1221-2212-2122122122e2");
        await using var factory = CreateFactory("tc-12-2-2", dbContext => SeedTenant(dbContext, tenantId));
        using var client = factory.CreateClient();
        await AcknowledgeAsync(client, tenantId, userId);

        await UploadAsync(client, tenantId, userId, evidenceItemId, "policy.pdf");
        var file = await DownloadAsync(client, tenantId, userId, evidenceItemId);

        Assert.Equal("accepted", file.ValidationStatus);
        Assert.Equal("scan-pending", file.MalwareScanStatus);
        Assert.False(file.IsUsable);
    }

    [Fact]
    public async Task TC_12_2_3_Replacement_upload_creates_new_version_without_overwriting_history()
    {
        var tenantId = Guid.Parse("12212212-2122-1221-2212-2122122122a3");
        var userId = Guid.Parse("12212212-2122-1221-2212-2122122122b3");
        var evidenceItemId = Guid.Parse("12212212-2122-1221-2212-2122122122e3");
        await using var factory = CreateFactory("tc-12-2-3", dbContext => SeedTenant(dbContext, tenantId));
        using var client = factory.CreateClient();
        await AcknowledgeAsync(client, tenantId, userId);

        await UploadAsync(client, tenantId, userId, evidenceItemId, "policy-v1.pdf");
        await UploadAsync(client, tenantId, userId, evidenceItemId, "policy-v2.pdf");

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var versions = await dbContext.EvidenceFileVersions
            .Where(version => version.EvidenceItemId == evidenceItemId)
            .OrderBy(version => version.VersionNumber)
            .ToArrayAsync();
        Assert.Equal([1, 2], versions.Select(version => version.VersionNumber).ToArray());
        Assert.Equal("policy-v1.pdf", versions[0].FileName);
        Assert.Equal("policy-v2.pdf", versions[1].FileName);
    }

    [Fact]
    public async Task TC_12_2_4_Download_and_delete_are_permissioned_and_audit_logged()
    {
        var tenantId = Guid.Parse("12212212-2122-1221-2212-2122122122a4");
        var userId = Guid.Parse("12212212-2122-1221-2212-2122122122b4");
        var evidenceItemId = Guid.Parse("12212212-2122-1221-2212-2122122122e4");
        await using var factory = CreateFactory("tc-12-2-4", dbContext => SeedTenant(dbContext, tenantId));
        using var client = factory.CreateClient();
        await AcknowledgeAsync(client, tenantId, userId);
        await UploadAsync(client, tenantId, userId, evidenceItemId, "policy.pdf");

        await DownloadAsync(client, tenantId, userId, evidenceItemId);
        using var forbiddenDeleteRequest = CreateRequest<object?>(
            HttpMethod.Delete,
            $"/api/evidence-items/{evidenceItemId}/file",
            null,
            tenantId,
            userId,
            Permission.ViewEvidence);
        var forbiddenDeleteResponse = await client.SendAsync(forbiddenDeleteRequest);
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenDeleteResponse.StatusCode);

        using var deleteRequest = CreateRequest<object?>(HttpMethod.Delete, $"/api/evidence-items/{evidenceItemId}/file", null, tenantId, userId, Permission.ManageEvidence);
        var deleteResponse = await client.SendAsync(deleteRequest);

        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Contains(await dbContext.AuditLogEntries.Where(audit => audit.TenantId == tenantId).ToArrayAsync(), audit =>
            audit.Action == AuditAction.Downloaded && audit.EntityType == "EvidenceFileVersion");
        Assert.Contains(await dbContext.AuditLogEntries.Where(audit => audit.TenantId == tenantId).ToArrayAsync(), audit =>
            audit.Action == AuditAction.Deleted && audit.EntityType == "EvidenceFileVersion");
    }

    [Fact]
    public async Task Uploaded_file_bytes_are_stored_and_streamed_through_the_api()
    {
        var tenantId = Guid.Parse("12212212-2122-1221-2212-2122122122a5");
        var userId = Guid.Parse("12212212-2122-1221-2212-2122122122b5");
        var evidenceItemId = Guid.Parse("12212212-2122-1221-2212-2122122122e5");
        await using var factory = CreateFactory("tc-12-2-5", dbContext => SeedTenant(dbContext, tenantId));
        using var client = factory.CreateClient();
        await AcknowledgeAsync(client, tenantId, userId);

        using var uploadRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/evidence-items/{evidenceItemId}/file");
        uploadRequest.Headers.Add("X-Gccs-Dev-Auth", "true");
        uploadRequest.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        uploadRequest.Headers.Add("X-Gccs-Dev-User", userId.ToString());
        uploadRequest.Headers.Add("X-Gccs-Dev-Permissions", Permission.ManageEvidence.ToString());
        using var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("policy evidence"));
        fileContent.Headers.ContentType = new("text/plain");
        using var content = new MultipartFormDataContent
        {
            { new StringContent("true"), "noCuiAttestation" },
            { fileContent, "file", "policy.txt" }
        };
        uploadRequest.Content = content;
        var uploadResponse = await client.SendAsync(uploadRequest);
        Assert.Equal(HttpStatusCode.Created, uploadResponse.StatusCode);

        using var downloadRequest = CreateRequest<object?>(
            HttpMethod.Get,
            $"/api/evidence-items/{evidenceItemId}/file/content",
            null,
            tenantId,
            userId,
            Permission.ViewEvidence);
        var blockedDownloadResponse = await client.SendAsync(downloadRequest);
        Assert.Equal(HttpStatusCode.Conflict, blockedDownloadResponse.StatusCode);

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
            var version = await dbContext.EvidenceFileVersions
                .Include(fileVersion => fileVersion.EvidenceItem)
                .SingleAsync(fileVersion =>
                    fileVersion.EvidenceItemId == evidenceItemId &&
                    fileVersion.EvidenceItem != null &&
                    fileVersion.EvidenceItem.TenantId == tenantId);
            version.MalwareScanStatus = "clean";
            version.EvidenceItem!.MalwareScanStatus = "clean";
            await dbContext.SaveChangesAsync();
        }

        using var cleanDownloadRequest = CreateRequest<object?>(
            HttpMethod.Get,
            $"/api/evidence-items/{evidenceItemId}/file/content",
            null,
            tenantId,
            userId,
            Permission.ViewEvidence);
        var downloadResponse = await client.SendAsync(cleanDownloadRequest);

        Assert.Equal(HttpStatusCode.OK, downloadResponse.StatusCode);
        Assert.Equal("policy evidence", await downloadResponse.Content.ReadAsStringAsync());
        Assert.Equal("text/plain", downloadResponse.Content.Headers.ContentType?.MediaType);
    }

    private async Task AcknowledgeAsync(HttpClient client, Guid tenantId, Guid userId)
    {
        using var request = CreateRequest(
            HttpMethod.Post,
            "/api/no-cui-acknowledgement",
            new AcknowledgeNoCuiRequest(true, NoCuiNotice.CurrentVersion),
            tenantId,
            userId,
            Permission.ManageEvidence);
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task UploadAsync(HttpClient client, Guid tenantId, Guid userId, Guid evidenceItemId, string fileName)
    {
        using var request = CreateRequest(
            HttpMethod.Post,
            $"/api/evidence-items/{evidenceItemId}/upload-intents",
            CreateUploadRequest(fileName),
            tenantId,
            userId,
            Permission.ManageEvidence);
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private async Task<EvidenceFileAccessDto> DownloadAsync(HttpClient client, Guid tenantId, Guid userId, Guid evidenceItemId)
    {
        using var request = CreateRequest<object?>(
            HttpMethod.Get,
            $"/api/evidence-items/{evidenceItemId}/download",
            null,
            tenantId,
            userId,
            Permission.ViewEvidence);
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<EvidenceFileAccessDto>(JsonOptions) ??
            throw new InvalidOperationException("Expected evidence file access response.");
    }

    private WebApplicationFactory<Program> CreateFactory(string databaseName, Action<GccsDbContext>? seed = null) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<NoCuiAcknowledgementService>();
                services.AddScoped<INoCuiAcknowledgementRepository, EfNoCuiAcknowledgementRepository>();
                services.AddScoped<IAuditEventWriter, EfAuditEventWriter>();
                services.AddSingleton<IObjectStorageService, InMemoryObjectStorageService>();

                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
                dbContext.Database.EnsureDeleted();
                dbContext.Database.EnsureCreated();
                seed?.Invoke(dbContext);
                dbContext.SaveChanges();
            });
        });

    private static EvidenceUploadIntentRequest CreateUploadRequest(string fileName) =>
        new(fileName, "application/pdf", 1024, NoCuiAttestation: true);

    private static HttpRequestMessage CreateRequest<TContent>(
        HttpMethod method,
        string requestUri,
        TContent content,
        Guid tenantId,
        Guid userId,
        Permission permission)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", userId.ToString());
        request.Headers.Add("X-Gccs-Dev-Permissions", permission.ToString());
        if (content is not null)
        {
            request.Content = JsonContent.Create(content, options: JsonOptions);
        }

        return request;
    }

    private static void SeedTenant(GccsDbContext dbContext, Guid tenantId)
    {
        dbContext.Tenants.Add(new TenantEntity
        {
            Id = tenantId,
            Name = "Evidence Upload Tenant",
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    private sealed class InMemoryObjectStorageService : IObjectStorageService
    {
        private readonly Dictionary<string, StoredObject> _objects = new(StringComparer.Ordinal);

        public Task<ObjectStorageWriteResult> UploadAsync(
            ObjectStorageWriteRequest request,
            CancellationToken cancellationToken = default)
        {
            var blobName = ObjectStorageNames.BuildTenantBlobName(request.TenantId, request.ObjectName);
            using var buffer = new MemoryStream();
            request.Content.CopyTo(buffer);
            _objects[blobName] = new StoredObject(buffer.ToArray(), request.ContentType, DateTimeOffset.UtcNow);

            return Task.FromResult(new ObjectStorageWriteResult(
                request.Container,
                blobName,
                new Uri($"https://storage.test/{blobName}"),
                "\"test\"",
                _objects[blobName].LastModified));
        }

        public Task<ObjectStorageReadResult?> OpenReadAsync(
            ObjectStorageReadRequest request,
            CancellationToken cancellationToken = default)
        {
            var blobName = ObjectStorageNames.BuildTenantBlobName(request.TenantId, request.ObjectName);
            if (!_objects.TryGetValue(blobName, out var storedObject))
            {
                return Task.FromResult<ObjectStorageReadResult?>(null);
            }

            return Task.FromResult<ObjectStorageReadResult?>(new ObjectStorageReadResult(
                request.Container,
                blobName,
                new MemoryStream(storedObject.Content, writable: false),
                storedObject.ContentType,
                storedObject.Content.Length,
                "\"test\"",
                storedObject.LastModified));
        }

        public Task<bool> ExistsAsync(
            ObjectStorageReadRequest request,
            CancellationToken cancellationToken = default)
        {
            var blobName = ObjectStorageNames.BuildTenantBlobName(request.TenantId, request.ObjectName);
            return Task.FromResult(_objects.ContainsKey(blobName));
        }

        public Task<bool> DeleteAsync(
            ObjectStorageReadRequest request,
            CancellationToken cancellationToken = default)
        {
            var blobName = ObjectStorageNames.BuildTenantBlobName(request.TenantId, request.ObjectName);
            return Task.FromResult(_objects.Remove(blobName));
        }

        private sealed record StoredObject(byte[] Content, string ContentType, DateTimeOffset LastModified);
    }
}

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
using Gccs.Domain.Evidence;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.NoCui;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
        var evidenceItemId = Guid.Parse("12212212-2122-1221-2212-2122122122e1");
        await using var factory = CreateFactory("tc-12-2-1", dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            SeedEvidenceItem(dbContext, tenantId, evidenceItemId);
        });
        using var client = factory.CreateClient();

        using var request = CreateRequest(
            HttpMethod.Post,
            $"/api/evidence-items/{evidenceItemId}/upload-intents",
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
        await using var factory = CreateFactory("tc-12-2-2", dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            SeedEvidenceItem(dbContext, tenantId, evidenceItemId);
        });
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
        var otherEvidenceItemId = Guid.Parse("12212212-2122-1221-2212-2122122122f3");
        await using var factory = CreateFactory("tc-12-2-3", dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            SeedEvidenceItem(dbContext, tenantId, evidenceItemId);
            SeedEvidenceItem(dbContext, tenantId, otherEvidenceItemId);
        });
        using var client = factory.CreateClient();
        await AcknowledgeAsync(client, tenantId, userId);

        await UploadAsync(client, tenantId, userId, evidenceItemId, "policy-v1.pdf");
        await UploadAsync(client, tenantId, userId, otherEvidenceItemId, "separate-record-v1.pdf");
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
        var separateVersion = await dbContext.EvidenceFileVersions.SingleAsync(
            version => version.EvidenceItemId == otherEvidenceItemId);
        Assert.Equal(1, separateVersion.VersionNumber);
    }

    [Fact]
    public async Task TC_12_2_4_Download_and_delete_are_permissioned_and_audit_logged()
    {
        var tenantId = Guid.Parse("12212212-2122-1221-2212-2122122122a4");
        var userId = Guid.Parse("12212212-2122-1221-2212-2122122122b4");
        var evidenceItemId = Guid.Parse("12212212-2122-1221-2212-2122122122e4");
        await using var factory = CreateFactory("tc-12-2-4", dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            SeedEvidenceItem(dbContext, tenantId, evidenceItemId);
        });
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
        await using var factory = CreateFactory("tc-12-2-5", dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            SeedEvidenceItem(dbContext, tenantId, evidenceItemId);
        });
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
            { new StringContent("Unclassified"), "classification" },
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
        Assert.Equal(HttpStatusCode.OK, blockedDownloadResponse.StatusCode);
        Assert.Equal("policy evidence", await blockedDownloadResponse.Content.ReadAsStringAsync());
        Assert.Equal("text/plain", blockedDownloadResponse.Content.Headers.ContentType?.MediaType);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var version = await dbContext.EvidenceFileVersions
            .Include(fileVersion => fileVersion.EvidenceItem)
            .SingleAsync(fileVersion =>
                fileVersion.EvidenceItemId == evidenceItemId &&
                fileVersion.EvidenceItem != null &&
                fileVersion.EvidenceItem.TenantId == tenantId);
        Assert.Equal("clean", version.MalwareScanStatus);
        Assert.Equal("clean", version.EvidenceItem!.MalwareScanStatus);
    }

    [Fact]
    public async Task Malware_detected_upload_is_rejected_before_file_version_is_persisted()
    {
        var tenantId = Guid.Parse("12212212-2122-1221-2212-2122122122a6");
        var userId = Guid.Parse("12212212-2122-1221-2212-2122122122b6");
        var evidenceItemId = Guid.Parse("12212212-2122-1221-2212-2122122122e6");
        await using var factory = CreateFactory(
            "tc-12-2-6",
            dbContext =>
            {
                SeedTenant(dbContext, tenantId);
                SeedEvidenceItem(dbContext, tenantId, evidenceItemId);
            },
            new TestMalwareScanner(MalwareScanResult.Malicious("test-scanner", "EICAR-Test-Signature FOUND")));
        using var client = factory.CreateClient();
        await AcknowledgeAsync(client, tenantId, userId);

        var response = await UploadFileBytesAsync(client, tenantId, userId, evidenceItemId, "unsafe evidence");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Empty(await dbContext.EvidenceFileVersions.Where(version => version.EvidenceItemId == evidenceItemId).ToArrayAsync());
        Assert.Contains(await dbContext.AuditLogEntries.Where(audit => audit.TenantId == tenantId).ToArrayAsync(), audit =>
            audit.Action == AuditAction.Rejected &&
            audit.EntityType == "EvidenceUploadIntent" &&
            audit.Summary.Contains("malware", StringComparison.OrdinalIgnoreCase) &&
            audit.MetadataJson.Contains("malware-detected", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Scanner_unavailable_blocks_upload_without_persisting_file_version()
    {
        var tenantId = Guid.Parse("12212212-2122-1221-2212-2122122122a7");
        var userId = Guid.Parse("12212212-2122-1221-2212-2122122122b7");
        var evidenceItemId = Guid.Parse("12212212-2122-1221-2212-2122122122e7");
        await using var factory = CreateFactory(
            "tc-12-2-7",
            dbContext =>
            {
                SeedTenant(dbContext, tenantId);
                SeedEvidenceItem(dbContext, tenantId, evidenceItemId);
            },
            new TestMalwareScanner(MalwareScanResult.Unavailable("test-scanner", "scanner timeout")));
        using var client = factory.CreateClient();
        await AcknowledgeAsync(client, tenantId, userId);

        var response = await UploadFileBytesAsync(client, tenantId, userId, evidenceItemId, "policy evidence");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Empty(await dbContext.EvidenceFileVersions.Where(version => version.EvidenceItemId == evidenceItemId).ToArrayAsync());
        Assert.Contains(await dbContext.AuditLogEntries.Where(audit => audit.TenantId == tenantId).ToArrayAsync(), audit =>
            audit.Action == AuditAction.Rejected &&
            audit.EntityType == "EvidenceUploadIntent" &&
            audit.MetadataJson.Contains("scan-unavailable", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Upload_cannot_create_or_attach_to_a_missing_or_cross_tenant_evidence_identity()
    {
        var tenantId = Guid.Parse("12212212-2122-1221-2212-2122122122a8");
        var otherTenantId = Guid.Parse("12212212-2122-1221-2212-2122122122a9");
        var userId = Guid.Parse("12212212-2122-1221-2212-2122122122b8");
        var crossTenantEvidenceItemId = Guid.Parse("12212212-2122-1221-2212-2122122122e8");
        var missingEvidenceItemId = Guid.Parse("12212212-2122-1221-2212-2122122122e9");
        await using var factory = CreateFactory("tc-12-2-identity", dbContext =>
        {
            SeedTenant(dbContext, tenantId);
            SeedTenant(dbContext, otherTenantId);
            SeedEvidenceItem(dbContext, otherTenantId, crossTenantEvidenceItemId);
        });
        using var client = factory.CreateClient();
        await AcknowledgeAsync(client, tenantId, userId);

        using var missingRequest = CreateRequest(
            HttpMethod.Post,
            $"/api/evidence-items/{missingEvidenceItemId}/upload-intents",
            CreateUploadRequest("missing.pdf"),
            tenantId,
            userId,
            Permission.ManageEvidence);
        var missingResponse = await client.SendAsync(missingRequest);
        using var crossTenantResponse = await UploadFileBytesAsync(
            client,
            tenantId,
            userId,
            crossTenantEvidenceItemId,
            "cross-tenant evidence");

        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, crossTenantResponse.StatusCode);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.False(await dbContext.EvidenceItems.AnyAsync(item => item.Id == missingEvidenceItemId));
        Assert.Empty(await dbContext.EvidenceFileVersions.ToArrayAsync());
    }

    [Theory]
    [InlineData("Unknown", false, HttpStatusCode.BadRequest)]
    [InlineData("Prohibited", false, HttpStatusCode.BadRequest)]
    [InlineData("Cui", false, HttpStatusCode.Forbidden)]
    [InlineData("Unclassified", true, HttpStatusCode.Forbidden)]
    public async Task Download_rechecks_parent_classification_and_containment(string classification, bool contained, HttpStatusCode expected)
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var evidenceId = Guid.NewGuid();
        await using var factory = CreateFactory($"download-policy-{Guid.NewGuid()}", db =>
        {
            SeedTenant(db, tenantId);
            SeedEvidenceItem(db, tenantId, evidenceId);
        });
        using var client = factory.CreateClient();
        await AcknowledgeAsync(client, tenantId, userId);
        using var upload = await UploadFileBytesAsync(client, tenantId, userId, evidenceId, "synthetic test content");
        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
            var evidence = await db.EvidenceItems.SingleAsync(e => e.Id == evidenceId);
            evidence.Classification = Enum.Parse<Gccs.Domain.Common.ContentClassification>(classification);
            if (contained) db.CuiSupportEscalations.Add(new CuiSupportEscalationEntity
            {
                Id = Guid.NewGuid(), TenantId = tenantId, AffectedEntityType = "EvidenceItem",
                AffectedEntityId = evidenceId.ToString(), Category = Gccs.Application.Tenancy.CuiSupportEscalationCategory.SuspectedCui,
                Status = Gccs.Application.Tenancy.CuiSupportEscalationStatus.Submitted, CreatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        }
        using var request = CreateRequest<object?>(HttpMethod.Get, $"/api/evidence-items/{evidenceId}/file/content", null,
            tenantId, userId, Permission.ViewEvidence);
        using var response = await client.SendAsync(request);
        Assert.Equal(expected, response.StatusCode);
        using var verification = factory.Services.CreateScope();
        var verifiedDb = verification.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.False(await verifiedDb.AuditLogEntries.AnyAsync(e => e.TenantId == tenantId && e.Action == AuditAction.Downloaded));
    }

    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task Audit_failure_rolls_back_upload_and_delete()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var evidenceId = Guid.NewGuid();
        var failure = new AuditFailureInterceptor();
        await using var factory = CreatePostgresFactory(
            Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION")!, tenantId, userId, evidenceId, failure);
        using var client = factory.CreateClient();
        try
        {
            await UploadAsync(client, tenantId, userId, evidenceId, "original.pdf");
            failure.Enabled = true;
            using var upload = CreateRequest(HttpMethod.Post, $"/api/evidence-items/{evidenceId}/upload-intents",
                CreateUploadRequest("replacement.pdf"), tenantId, userId, Permission.ManageEvidence);
            using var response = await client.SendAsync(upload);
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            using var bytesResponse = await UploadFileBytesAsync(client, tenantId, userId, evidenceId, "replacement bytes");
            Assert.Equal(HttpStatusCode.InternalServerError, bytesResponse.StatusCode);
            Assert.Equal(0, ((InMemoryObjectStorageService)factory.Services.GetRequiredService<IObjectStorageService>()).Count);
            using var delete = CreateRequest<object?>(HttpMethod.Delete, $"/api/evidence-items/{evidenceId}/file",
                null, tenantId, userId, Permission.ManageEvidence);
            using var deleteResponse = await client.SendAsync(delete);
            Assert.Equal(HttpStatusCode.InternalServerError, deleteResponse.StatusCode);
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
            var version = await db.EvidenceFileVersions.SingleAsync(v => v.EvidenceItemId == evidenceId);
            Assert.Null(version.DeletedAt);
            Assert.Equal("original.pdf", version.FileName);
            Assert.Equal("original.pdf", (await db.EvidenceItems.SingleAsync(e => e.Id == evidenceId)).OriginalFileName);
            Assert.Equal(1, await db.AuditLogEntries.CountAsync(e => e.TenantId == tenantId));
        }
        finally
        {
            failure.Enabled = false;
            await DeleteTenantAsync(factory, tenantId);
        }
    }

    [PostgresFact]
    public async Task Policy_rejection_rolls_back_business_changes_but_retains_rejection_audit()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var evidenceId = Guid.NewGuid();
        await using var factory = CreatePostgresFactory(Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION")!,
            tenantId, userId, evidenceId);
        try
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
            var before = (await db.EvidenceItems.SingleAsync(e => e.Id == evidenceId)).Name;
            var policy = new Gccs.Application.Tenancy.TenantDataHandlingModePolicyService(scope.ServiceProvider,
                new FixedContentContext(tenantId, userId), scope.ServiceProvider.GetRequiredService<IAuditEventWriter>(),
                scope.ServiceProvider.GetRequiredService<Gccs.Application.Tenancy.ICurrentDataHandlingNoticeGuard>());
            var transaction = scope.ServiceProvider.GetRequiredService<Gccs.Application.Common.IApplicationTransaction>();
            await Assert.ThrowsAsync<Gccs.Application.Tenancy.TenantDataHandlingModeRestrictedException>(() =>
                transaction.ExecuteAsync<bool>(async ct =>
                {
                    (await db.EvidenceItems.SingleAsync(e => e.Id == evidenceId, ct)).Name = "must roll back";
                    await db.SaveChangesAsync(ct);
                    await policy.EnsureAllowedAsync(new Gccs.Application.Tenancy.TenantDataHandlingModePolicyRequest(
                        Gccs.Application.Tenancy.TenantDataHandlingWorkflow.Report, ContainsRealCui: true,
                        EntityType: "EvidenceItem", EntityId: evidenceId.ToString()), userId, ct);
                    return true;
                }));
            Assert.Equal(before, (await db.EvidenceItems.AsNoTracking().SingleAsync(e => e.Id == evidenceId)).Name);
            Assert.Equal(1, await db.AuditLogEntries.CountAsync(e => e.TenantId == tenantId && e.Action == AuditAction.Rejected));
        }
        finally { await DeleteTenantAsync(factory, tenantId); }
    }

    [PostgresFact]
    public async Task Cleanup_survives_provider_failure_and_completion_audit_rollback()
    {
        var tenantId = Guid.NewGuid(); var userId = Guid.NewGuid(); var evidenceId = Guid.NewGuid();
        var failure = new AuditFailureInterceptor();
        await using var factory = CreatePostgresFactory(Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION")!,
            tenantId, userId, evidenceId, failure);
        using var client = factory.CreateClient();
        var storage = (InMemoryObjectStorageService)factory.Services.GetRequiredService<IObjectStorageService>();
        try
        {
            using var uploaded = await UploadFileBytesAsync(client, tenantId, userId, evidenceId, "synthetic cleanup fixture");
            Assert.Equal(HttpStatusCode.Created, uploaded.StatusCode);
            using var request = CreateRequest<object?>(HttpMethod.Delete, $"/api/evidence-items/{evidenceId}/file",
                null, tenantId, userId, Permission.ManageEvidence);
            using var deleted = await client.SendAsync(request);
            Assert.True(deleted.IsSuccessStatusCode);
            Assert.Equal(1, storage.Count);
            storage.FailDeletes = true;
            using (var scope = factory.Services.CreateScope())
            {
                Assert.True(await scope.ServiceProvider.GetRequiredService<IObjectCleanupQueue>().ProcessNextAsync());
                var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
                var item = await db.Set<ObjectCleanupEntity>().SingleAsync(e => e.TenantId == tenantId);
                Assert.Null(item.CompletedAt);
                Assert.Equal("cleanup_provider_failure", item.LastErrorCode);
                item.NextAttemptAt = DateTimeOffset.UtcNow.AddMinutes(-1);
                await db.SaveChangesAsync();
            }
            storage.FailDeletes = false;
            failure.Enabled = true;
            using (var scope = factory.Services.CreateScope())
                await Assert.ThrowsAsync<AuditWriteException>(() => scope.ServiceProvider.GetRequiredService<IObjectCleanupQueue>().ProcessNextAsync());
            Assert.Equal(0, storage.Count);
            failure.Enabled = false;
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
                Assert.Null((await db.Set<ObjectCleanupEntity>().AsNoTracking().SingleAsync(e => e.TenantId == tenantId)).CompletedAt);
                Assert.True(await scope.ServiceProvider.GetRequiredService<IObjectCleanupQueue>().ProcessNextAsync());
                Assert.NotNull((await db.Set<ObjectCleanupEntity>().SingleAsync(e => e.TenantId == tenantId)).CompletedAt);
                Assert.Equal(1, await db.AuditLogEntries.CountAsync(e => e.TenantId == tenantId && e.EntityType == "ObjectCleanup"));
            }
        }
        finally { failure.Enabled = false; await DeleteTenantAsync(factory, tenantId); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Missing_or_outdated_current_notice_blocks_before_storage(bool outdated)
    {
        var tenantId = Guid.NewGuid(); var userId = Guid.NewGuid(); var evidenceId = Guid.NewGuid();
        await using var factory = CreateFactory($"notice-gate-{Guid.NewGuid()}", db =>
        {
            SeedTenant(db, tenantId); SeedEvidenceItem(db, tenantId, evidenceId);
            db.NoCuiAcknowledgements.Add(new NoCuiAcknowledgementEntity
            {
                Id = Guid.NewGuid(), TenantId = tenantId, UserId = userId, NoticeVersion = NoCuiNotice.CurrentVersion,
                NoticeCopy = NoCuiNotice.Copy, AcknowledgedAt = DateTimeOffset.UtcNow, CreatedAt = DateTimeOffset.UtcNow
            });
            if (outdated) db.DataHandlingNoticeAcknowledgements.Add(new DataHandlingNoticeAcknowledgementEntity
            {
                Id = Guid.NewGuid(), TenantId = tenantId, UserId = userId, Mode = TenantDataPosture.NoCui,
                WorkflowContext = "EvidenceUpload", NoticeId = "no-cui-general", NoticeVersion = "obsolete",
                AcknowledgedAt = DateTimeOffset.UtcNow
            });
        });
        using var client = factory.CreateClient();
        using var response = await UploadFileBytesAsync(client, tenantId, userId, evidenceId, "synthetic notice fixture");
        Assert.Equal((HttpStatusCode)428, response.StatusCode);
        Assert.Contains("data_handling_notice_acknowledgement_required", await response.Content.ReadAsStringAsync());
        Assert.Equal(0, ((InMemoryObjectStorageService)factory.Services.GetRequiredService<IObjectStorageService>()).Count);
        using var scope = factory.Services.CreateScope();
        var verified = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.False(await verified.EvidenceFileVersions.AnyAsync());
        Assert.False(await verified.AuditLogEntries.AnyAsync());
    }

    private sealed record FixedContentContext(Guid TenantId, Guid UserId) : ICurrentTenantContext
    {
        public string UserEmail => "synthetic-test@example.invalid";
    }

    [PostgresFact]
    public async Task Notice_acknowledgement_is_atomic_and_concurrent_retries_are_idempotent()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var failure = new AuditFailureInterceptor();
        await using var factory = CreatePostgresFactory(Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION")!,
            tenantId, userId, Guid.NewGuid(), failure);
        var notice = new Gccs.Application.Tenancy.DataHandlingNoticeDto("no-cui-general", "test-version", TenantDataPosture.NoCui,
            ["EvidenceUpload"], "Synthetic test notice", "No CUI", "Published", "test", "test", new(2026, 1, 1), new(2026, 1, 1), "synthetic test");
        var request = new Gccs.Application.Tenancy.AcknowledgeDataHandlingNoticeRequest(TenantDataPosture.NoCui,
            "EvidenceUpload", notice.NoticeId, notice.Version, true);
        async Task Acknowledge()
        {
            using var scope = factory.Services.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<Gccs.Application.Tenancy.DataHandlingNoticeAcknowledgementService>();
            await service.AcknowledgeAsync(tenantId, userId, notice, request);
        }
        try
        {
            failure.Enabled = true;
            await Assert.ThrowsAsync<AuditWriteException>(Acknowledge);
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
                Assert.False(await db.DataHandlingNoticeAcknowledgements.AnyAsync(a => a.TenantId == tenantId && a.NoticeVersion == "test-version"));
            }
            failure.Enabled = false;
            await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Acknowledge()));
            using var verification = factory.Services.CreateScope();
            var verifiedDb = verification.ServiceProvider.GetRequiredService<GccsDbContext>();
            Assert.Equal(1, await verifiedDb.DataHandlingNoticeAcknowledgements.CountAsync(a => a.TenantId == tenantId && a.NoticeVersion == "test-version"));
            Assert.Equal(1, await verifiedDb.AuditLogEntries.CountAsync(a => a.TenantId == tenantId && a.EntityType == "DataHandlingNoticeAcknowledgement"));
        }
        finally
        {
            failure.Enabled = false;
            using var scope = factory.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<GccsDbContext>().DataHandlingNoticeAcknowledgements
                .Where(a => a.TenantId == tenantId).ExecuteDeleteAsync();
            await DeleteTenantAsync(factory, tenantId);
        }
    }

    private sealed class AuditFailureInterceptor : SaveChangesInterceptor
    {
        public bool Enabled { get; set; }
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (Enabled && eventData.Context!.ChangeTracker.Entries<AuditLogEntryEntity>().Any(e => e.State == EntityState.Added))
                throw new InvalidOperationException("Injected audit persistence failure.");
            return ValueTask.FromResult(result);
        }
    }

    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task Concurrent_replacements_receive_distinct_sequential_versions()
    {
        var connectionString = Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION") ??
            throw new InvalidOperationException("Set GCCS_TEST_POSTGRES_CONNECTION to run the PostgreSQL concurrency test.");
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var evidenceItemId = Guid.NewGuid();
        await using var factory = CreatePostgresFactory(connectionString, tenantId, userId, evidenceItemId);

        try
        {
            using var client = factory.CreateClient();
            var startGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var uploads = Enumerable.Range(1, 12)
                .Select(version => Task.Run(async () =>
                {
                    await startGate.Task;
                    using var response = await UploadFileBytesAsync(
                        client, tenantId, userId, evidenceItemId, $"Synthetic replacement {version}");
                    var body = await response.Content.ReadAsStringAsync();
                    Assert.True(
                        response.StatusCode == HttpStatusCode.Created,
                        $"Expected 201 Created but received {(int)response.StatusCode}: {body}");
                }))
                .ToArray();

            startGate.SetResult();
            await Task.WhenAll(uploads);

            using var verificationScope = factory.Services.CreateScope();
            var dbContext = verificationScope.ServiceProvider.GetRequiredService<GccsDbContext>();
            var versionNumbers = await dbContext.EvidenceFileVersions
                .Where(version => version.EvidenceItemId == evidenceItemId)
                .OrderBy(version => version.VersionNumber)
                .Select(version => version.VersionNumber)
                .ToArrayAsync();
            Assert.Equal(Enumerable.Range(1, 12), versionNumbers);
            Assert.Equal(12, ((InMemoryObjectStorageService)factory.Services.GetRequiredService<IObjectStorageService>()).Count);
            Assert.Equal(12, await dbContext.AuditLogEntries.CountAsync(
                entry => entry.TenantId == tenantId && entry.EntityType == "EvidenceFileVersion"));
        }
        finally
        {
            await DeleteTenantAsync(factory, tenantId);
        }
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
        using var noticeRequest = CreateRequest(HttpMethod.Post,
            $"/api/tenants/{tenantId}/data-handling-notice-acknowledgements",
            new Gccs.Application.Tenancy.AcknowledgeDataHandlingNoticeRequest(TenantDataPosture.NoCui,
                "EvidenceUpload", "no-cui-general", "2026.06.phase1a", true), tenantId, userId, Permission.ManageEvidence);
        using var noticeResponse = await client.SendAsync(noticeRequest);
        Assert.Equal(HttpStatusCode.Created, noticeResponse.StatusCode);
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

    private async Task<HttpResponseMessage> UploadFileBytesAsync(
        HttpClient client,
        Guid tenantId,
        Guid userId,
        Guid evidenceItemId,
        string contentText)
    {
        using var uploadRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/evidence-items/{evidenceItemId}/file");
        uploadRequest.Headers.Add("X-Gccs-Dev-Auth", "true");
        uploadRequest.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        uploadRequest.Headers.Add("X-Gccs-Dev-User", userId.ToString());
        uploadRequest.Headers.Add("X-Gccs-Dev-Permissions", Permission.ManageEvidence.ToString());
        using var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(contentText));
        fileContent.Headers.ContentType = new("text/plain");
        using var content = new MultipartFormDataContent
        {
            { new StringContent("Unclassified"), "classification" },
            { new StringContent("true"), "noCuiAttestation" },
            { fileContent, "file", "policy.txt" }
        };
        uploadRequest.Content = content;
        return await client.SendAsync(uploadRequest);
    }

    private WebApplicationFactory<Program> CreateFactory(
        string databaseName,
        Action<GccsDbContext>? seed = null,
        IMalwareScanner? malwareScanner = null) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<NoCuiAcknowledgementService>();
                services.AddScoped<Gccs.Application.Tenancy.ITenantRepository, Gccs.Infrastructure.Tenancy.EfTenantRepository>();
                services.AddScoped<Gccs.Application.Tenancy.IDataHandlingNoticeAcknowledgementRepository, Gccs.Infrastructure.Tenancy.EfDataHandlingNoticeAcknowledgementRepository>();
                services.AddScoped<INoCuiAcknowledgementRepository, EfNoCuiAcknowledgementRepository>();
                services.AddScoped<Gccs.Application.Evidence.IEvidenceMetadataRepository, Gccs.Infrastructure.Evidence.EfEvidenceMetadataRepository>();
                services.AddScoped<IAuditEventWriter, EfAuditEventWriter>();
                services.AddSingleton<IObjectStorageService, InMemoryObjectStorageService>();
                services.AddSingleton(malwareScanner ?? new TestMalwareScanner(MalwareScanResult.Clean("test-scanner", "clean")));

                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
                dbContext.Database.EnsureDeleted();
                dbContext.Database.EnsureCreated();
                seed?.Invoke(dbContext);
                dbContext.SaveChanges();
            });
        });

    private WebApplicationFactory<Program> CreatePostgresFactory(
        string connectionString,
        Guid tenantId,
        Guid userId,
        Guid evidenceItemId,
        AuditFailureInterceptor? auditFailure = null) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ObjectCleanupProcessing:Enabled", "false");
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IObjectStorageService, InMemoryObjectStorageService>();
                services.AddSingleton<IMalwareScanner>(new TestMalwareScanner(MalwareScanResult.Clean("test-scanner", "clean")));
                services.RemoveAll<GccsDbContext>();
                services.RemoveAll<DbContextOptions<GccsDbContext>>();
                services.AddDbContext<GccsDbContext>(options =>
                {
                    options.UseGccsPostgres(connectionString);
                    if (auditFailure is not null) options.AddInterceptors(auditFailure);
                });

                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
                PostgresTestDatabase.Migrate(dbContext);
                SeedTenant(dbContext, tenantId);
                SeedEvidenceItem(dbContext, tenantId, evidenceItemId);
                dbContext.NoCuiAcknowledgements.Add(new NoCuiAcknowledgementEntity
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    UserId = userId,
                    NoticeVersion = NoCuiNotice.CurrentVersion,
                    NoticeCopy = NoCuiNotice.Copy,
                    AcknowledgedAt = DateTimeOffset.UtcNow,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedByUserId = userId
                });
                dbContext.DataHandlingNoticeAcknowledgements.Add(new DataHandlingNoticeAcknowledgementEntity
                {
                    Id = Guid.NewGuid(), TenantId = tenantId, UserId = userId, Mode = TenantDataPosture.NoCui,
                    WorkflowContext = "EvidenceUpload", NoticeId = "no-cui-general", NoticeVersion = "2026.06.phase1a",
                    AcknowledgedAt = DateTimeOffset.UtcNow
                });
                dbContext.SaveChanges();
            });
        });

    private static async Task DeleteTenantAsync(WebApplicationFactory<Program> factory, Guid tenantId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var evidenceItems = await dbContext.EvidenceItems
            .Where(item => item.TenantId == tenantId)
            .ToArrayAsync();
        dbContext.EvidenceItems.RemoveRange(evidenceItems);
        var acknowledgements = await dbContext.NoCuiAcknowledgements
            .Where(acknowledgement => acknowledgement.TenantId == tenantId)
            .ToArrayAsync();
        dbContext.NoCuiAcknowledgements.RemoveRange(acknowledgements);
        await dbContext.DataHandlingNoticeAcknowledgements.Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
        await dbContext.Set<ObjectCleanupEntity>().Where(item => item.TenantId == tenantId).ExecuteDeleteAsync();
        // Remove only this test's synthetic audit fixtures during relational cleanup.
        await dbContext.AuditLogEntries.Where(entry => entry.TenantId == tenantId).ExecuteDeleteAsync();
        var tenant = await dbContext.Tenants.SingleOrDefaultAsync(candidate => candidate.Id == tenantId);
        if (tenant is not null)
        {
            dbContext.Tenants.Remove(tenant);
        }

        await dbContext.SaveChangesAsync();
    }

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

    private static void SeedEvidenceItem(GccsDbContext dbContext, Guid tenantId, Guid evidenceItemId)
    {
        dbContext.EvidenceItems.Add(new EvidenceItemEntity
        {
            Id = evidenceItemId,
            TenantId = tenantId,
            Name = $"Evidence {evidenceItemId:N}",
            Description = "Synthetic No-CUI test evidence.",
            Type = EvidenceType.Policy,
            OwnerFunction = "Compliance",
            Status = EvidenceStatus.InReview,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    private sealed class InMemoryObjectStorageService : IObjectStorageService
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, StoredObject> _objects = new(StringComparer.Ordinal);
        public int Count => _objects.Count;
        public bool FailDeletes { get; set; }

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
            if (FailDeletes) throw new IOException("synthetic provider failure");
            var blobName = ObjectStorageNames.BuildTenantBlobName(request.TenantId, request.ObjectName);
            return Task.FromResult(_objects.TryRemove(blobName, out _));
        }

        private sealed record StoredObject(byte[] Content, string ContentType, DateTimeOffset LastModified);
    }

    private sealed class TestMalwareScanner(MalwareScanResult result) : IMalwareScanner
    {
        public Task<MalwareScanResult> ScanAsync(
            MalwareScanRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(result);
    }
}

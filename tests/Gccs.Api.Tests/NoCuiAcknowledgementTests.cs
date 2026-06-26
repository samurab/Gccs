using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.NoCui;
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

public sealed class NoCuiAcknowledgementTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public NoCuiAcknowledgementTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_4_1_1_No_cui_notice_is_returned_before_first_upload()
    {
        var tenantId = Guid.Parse("41414141-4141-4141-4141-4141414141a1");
        var userId = Guid.Parse("41414141-4141-4141-4141-4141414141b1");
        await using var factory = CreateFactory("tc-4-1-1", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenant(tenantId, "TC-4.1.1 Tenant"));
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();
        using var request = CreateRequest(HttpMethod.Get, "/api/no-cui-acknowledgement", tenantId, userId, Permission.ViewEvidence);

        var response = await client.SendAsync(request);
        var status = await response.Content.ReadFromJsonAsync<NoCuiAcknowledgementStatusDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(status);
        Assert.False(status.IsAcknowledged);
        Assert.Equal(tenantId, status.TenantId);
        Assert.Null(status.AcknowledgedByUserId);
        Assert.Null(status.AcknowledgedAt);
        Assert.Equal(NoCuiNotice.CurrentVersion, status.NoticeVersion);
        Assert.Contains("compliance management only", status.NoticeCopy, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not ready to store CUI", status.NoticeCopy, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TC_4_1_2_Upload_intent_is_blocked_until_acknowledgement_and_permission_are_present()
    {
        var tenantId = Guid.Parse("41414141-4141-4141-4141-4141414141a2");
        var userId = Guid.Parse("41414141-4141-4141-4141-4141414141b2");
        await using var factory = CreateFactory("tc-4-1-2", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenant(tenantId, "TC-4.1.2 Tenant"));
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();
        using var blockedByPolicyRequest = CreateRequest(
            HttpMethod.Post,
            $"/api/evidence-items/{Guid.NewGuid()}/upload-intents",
            new EvidenceUploadIntentRequest("policy.pdf", "application/pdf", 1024),
            tenantId,
            userId,
            Permission.ManageEvidence);
        using var blockedByPermissionRequest = CreateRequest(
            HttpMethod.Post,
            $"/api/evidence-items/{Guid.NewGuid()}/upload-intents",
            new EvidenceUploadIntentRequest("policy.pdf", "application/pdf", 1024),
            tenantId,
            userId,
            Permission.ViewEvidence);

        var blockedByPolicyResponse = await client.SendAsync(blockedByPolicyRequest);
        var blockedByPolicyBody = await blockedByPolicyResponse.Content.ReadAsStringAsync();
        var blockedByPermissionResponse = await client.SendAsync(blockedByPermissionRequest);

        Assert.Equal(HttpStatusCode.PreconditionRequired, blockedByPolicyResponse.StatusCode);
        Assert.Equal("application/problem+json", blockedByPolicyResponse.Content.Headers.ContentType?.MediaType);
        Assert.Contains("no_cui_acknowledgement_required", blockedByPolicyBody, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No-CUI acknowledgement is required", blockedByPolicyBody, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.Forbidden, blockedByPermissionResponse.StatusCode);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Empty(await dbContext.NoCuiAcknowledgements.Where(candidate => candidate.TenantId == tenantId).ToListAsync());
    }

    [Fact]
    public async Task TC_4_1_3_Acknowledgement_persists_user_tenant_timestamp_and_notice_version()
    {
        var tenantAId = Guid.Parse("41414141-4141-4141-4141-4141414141a3");
        var tenantBId = Guid.Parse("41414141-4141-4141-4141-4141414141b3");
        var userId = Guid.Parse("41414141-4141-4141-4141-4141414141c3");
        await using var factory = CreateFactory("tc-4-1-3", dbContext =>
        {
            dbContext.Tenants.AddRange(
                CreateTenant(tenantAId, "TC-4.1.3 Tenant A"),
                CreateTenant(tenantBId, "TC-4.1.3 Tenant B"));
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();
        using var acknowledgeRequest = CreateRequest(
            HttpMethod.Post,
            "/api/no-cui-acknowledgement",
            new AcknowledgeNoCuiRequest(true, NoCuiNotice.CurrentVersion),
            tenantAId,
            userId,
            Permission.ManageEvidence);
        using var tenantBStatusRequest = CreateRequest(
            HttpMethod.Get,
            "/api/no-cui-acknowledgement",
            tenantBId,
            userId,
            Permission.ViewEvidence);

        var acknowledgeResponse = await client.SendAsync(acknowledgeRequest);
        var acknowledgedStatus = await acknowledgeResponse.Content.ReadFromJsonAsync<NoCuiAcknowledgementStatusDto>(JsonOptions);
        var tenantBStatusResponse = await client.SendAsync(tenantBStatusRequest);
        var tenantBStatus = await tenantBStatusResponse.Content.ReadFromJsonAsync<NoCuiAcknowledgementStatusDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, acknowledgeResponse.StatusCode);
        Assert.NotNull(acknowledgedStatus);
        Assert.True(acknowledgedStatus.IsAcknowledged);
        Assert.Equal(tenantAId, acknowledgedStatus.TenantId);
        Assert.Equal(userId, acknowledgedStatus.AcknowledgedByUserId);
        Assert.Equal(NoCuiNotice.CurrentVersion, acknowledgedStatus.NoticeVersion);
        Assert.True(acknowledgedStatus.AcknowledgedAt <= DateTimeOffset.UtcNow);

        Assert.Equal(HttpStatusCode.OK, tenantBStatusResponse.StatusCode);
        Assert.NotNull(tenantBStatus);
        Assert.False(tenantBStatus.IsAcknowledged);
        Assert.Equal(tenantBId, tenantBStatus.TenantId);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var acknowledgement = await dbContext.NoCuiAcknowledgements.SingleAsync();

        Assert.Equal(tenantAId, acknowledgement.TenantId);
        Assert.Equal(userId, acknowledgement.UserId);
        Assert.Equal(NoCuiNotice.CurrentVersion, acknowledgement.NoticeVersion);
        Assert.Equal(userId, acknowledgement.CreatedByUserId);
        Assert.True(acknowledgement.AcknowledgedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task TC_4_1_4_Acknowledgement_is_audit_logged_and_copy_states_no_cui_posture()
    {
        var tenantId = Guid.Parse("41414141-4141-4141-4141-4141414141a4");
        var userId = Guid.Parse("41414141-4141-4141-4141-4141414141b4");
        await using var factory = CreateFactory("tc-4-1-4", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenant(tenantId, "TC-4.1.4 Tenant"));
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();
        using var acknowledgeRequest = CreateRequest(
            HttpMethod.Post,
            "/api/no-cui-acknowledgement",
            new AcknowledgeNoCuiRequest(true, NoCuiNotice.CurrentVersion),
            tenantId,
            userId,
            Permission.ManageEvidence);

        var response = await client.SendAsync(acknowledgeRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var auditEvent = await dbContext.AuditLogEntries.SingleAsync(candidate =>
            candidate.TenantId == tenantId &&
            candidate.EntityType == "NoCuiAcknowledgement");

        Assert.Equal(userId, auditEvent.ActorUserId);
        Assert.Equal(AuditAction.Created, auditEvent.Action);
        Assert.Contains("acknowledged", auditEvent.Summary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(NoCuiNotice.CurrentVersion, auditEvent.MetadataJson, StringComparison.Ordinal);
        Assert.Contains("compliance management only", auditEvent.MetadataJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not ready to store CUI", auditEvent.MetadataJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TC_4_2_1_Disallowed_file_type_is_rejected_without_usable_evidence()
    {
        var tenantId = Guid.Parse("42424242-4242-4242-4242-4242424242a1");
        var userId = Guid.Parse("42424242-4242-4242-4242-4242424242b1");
        var evidenceItemId = Guid.Parse("42424242-4242-4242-4242-4242424242c1");
        await using var factory = CreateFactory("tc-4-2-1", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenant(tenantId, "TC-4.2.1 Tenant"));
            dbContext.NoCuiAcknowledgements.Add(CreateAcknowledgement(tenantId, userId));
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();
        using var uploadRequest = CreateRequest(
            HttpMethod.Post,
            $"/api/evidence-items/{evidenceItemId}/upload-intents",
            new EvidenceUploadIntentRequest("installer.exe", "application/x-msdownload", 1024, NoCuiAttestation: true),
            tenantId,
            userId,
            Permission.ManageEvidence);

        var response = await client.SendAsync(uploadRequest);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Evidence upload rejected", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("File type '.exe' is not allowed", body, StringComparison.OrdinalIgnoreCase);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Empty(await dbContext.EvidenceItems.Where(candidate => candidate.TenantId == tenantId).ToListAsync());
        Assert.Single(await dbContext.AuditLogEntries.Where(candidate =>
            candidate.TenantId == tenantId &&
            candidate.ActorUserId == userId &&
            candidate.Action == AuditAction.Rejected &&
            candidate.EntityType == "EvidenceUploadIntent" &&
            candidate.EntityId == evidenceItemId.ToString()).ToListAsync());
    }

    [Fact]
    public async Task TC_4_2_2_Oversized_file_is_rejected_server_side()
    {
        var tenantId = Guid.Parse("42424242-4242-4242-4242-4242424242a2");
        var userId = Guid.Parse("42424242-4242-4242-4242-4242424242b2");
        await using var factory = CreateFactory("tc-4-2-2", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenant(tenantId, "TC-4.2.2 Tenant"));
            dbContext.NoCuiAcknowledgements.Add(CreateAcknowledgement(tenantId, userId));
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();
        using var uploadRequest = CreateRequest(
            HttpMethod.Post,
            $"/api/evidence-items/{Guid.NewGuid()}/upload-intents",
            new EvidenceUploadIntentRequest("large-policy.pdf", "application/pdf", EvidenceUploadGuardrails.MaxSizeBytes + 1, NoCuiAttestation: true),
            tenantId,
            userId,
            Permission.ManageEvidence);

        var response = await client.SendAsync(uploadRequest);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("File size exceeds", body, StringComparison.OrdinalIgnoreCase);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Empty(await dbContext.EvidenceItems.Where(candidate => candidate.TenantId == tenantId).ToListAsync());
    }

    [Fact]
    public async Task TC_4_2_2A_Upload_without_per_file_no_cui_attestation_is_rejected_and_audit_logged()
    {
        var tenantId = Guid.Parse("42424242-4242-4242-4242-4242424242d2");
        var userId = Guid.Parse("42424242-4242-4242-4242-4242424242e2");
        var evidenceItemId = Guid.Parse("42424242-4242-4242-4242-4242424242f2");
        await using var factory = CreateFactory("tc-4-2-2a", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenant(tenantId, "TC-4.2.2A Tenant"));
            dbContext.NoCuiAcknowledgements.Add(CreateAcknowledgement(tenantId, userId));
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();
        using var uploadRequest = CreateRequest(
            HttpMethod.Post,
            $"/api/evidence-items/{evidenceItemId}/upload-intents",
            new EvidenceUploadIntentRequest("policy.pdf", "application/pdf", 1024),
            tenantId,
            userId,
            Permission.ManageEvidence);

        var response = await client.SendAsync(uploadRequest);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("noCuiAttestation", body, StringComparison.Ordinal);
        Assert.Contains("does not contain CUI", body, StringComparison.OrdinalIgnoreCase);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Empty(await dbContext.EvidenceItems.Where(candidate => candidate.TenantId == tenantId).ToListAsync());
        var auditEvent = await dbContext.AuditLogEntries.SingleAsync(candidate =>
            candidate.TenantId == tenantId &&
            candidate.ActorUserId == userId &&
            candidate.Action == AuditAction.Rejected &&
            candidate.EntityType == "EvidenceUploadIntent" &&
            candidate.EntityId == evidenceItemId.ToString());
        Assert.Contains("noCuiAttestation", auditEvent.MetadataJson, StringComparison.Ordinal);
        Assert.DoesNotContain("fileContent", auditEvent.MetadataJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("contentBytes", auditEvent.MetadataJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TC_4_2_3_Valid_upload_metadata_records_validation_and_scan_status()
    {
        var tenantId = Guid.Parse("42424242-4242-4242-4242-4242424242a3");
        var userId = Guid.Parse("42424242-4242-4242-4242-4242424242b3");
        var evidenceItemId = Guid.Parse("42424242-4242-4242-4242-4242424242c3");
        await using var factory = CreateFactory("tc-4-2-3", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenant(tenantId, "TC-4.2.3 Tenant"));
            dbContext.NoCuiAcknowledgements.Add(CreateAcknowledgement(tenantId, userId));
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();
        using var uploadRequest = CreateRequest(
            HttpMethod.Post,
            $"/api/evidence-items/{evidenceItemId}/upload-intents",
            new EvidenceUploadIntentRequest("policy.pdf", "application/pdf", 2048, NoCuiAttestation: true),
            tenantId,
            userId,
            Permission.ManageEvidence);

        var response = await client.SendAsync(uploadRequest);
        var uploadIntent = await response.Content.ReadFromJsonAsync<EvidenceUploadIntentDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(uploadIntent);
        Assert.Equal(evidenceItemId, uploadIntent.EvidenceItemId);
        Assert.Equal("policy.pdf", uploadIntent.FileName);
        Assert.Equal("application/pdf", uploadIntent.ContentType);
        Assert.Equal(2048, uploadIntent.SizeBytes);
        Assert.Equal(EvidenceUploadGuardrails.AcceptedValidationStatus, uploadIntent.ValidationStatus);
        Assert.Equal(EvidenceUploadGuardrails.PendingMalwareScanStatus, uploadIntent.MalwareScanStatus);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var evidenceItem = await dbContext.EvidenceItems.SingleAsync(candidate =>
            candidate.TenantId == tenantId &&
            candidate.Id == evidenceItemId);

        Assert.Equal("policy.pdf", evidenceItem.OriginalFileName);
        Assert.Equal("application/pdf", evidenceItem.ContentType);
        Assert.Equal(2048, evidenceItem.SizeBytes);
        Assert.Equal(EvidenceUploadGuardrails.AcceptedValidationStatus, evidenceItem.UploadValidationStatus);
        Assert.Equal(EvidenceUploadGuardrails.PendingMalwareScanStatus, evidenceItem.MalwareScanStatus);
        Assert.Equal(Gccs.Domain.Evidence.EvidenceStatus.InReview, evidenceItem.Status);
        Assert.Null(evidenceItem.StorageUri);
        Assert.Null(evidenceItem.FileHash);
        var auditEvent = await dbContext.AuditLogEntries.SingleAsync(candidate =>
            candidate.TenantId == tenantId &&
            candidate.ActorUserId == userId &&
            candidate.Action == AuditAction.Uploaded &&
            candidate.EntityType == "EvidenceFileVersion" &&
            candidate.EntityId == uploadIntent.Id.ToString());
        Assert.Contains("noCuiAttestation", auditEvent.MetadataJson, StringComparison.Ordinal);
        Assert.DoesNotContain("fileContent", auditEvent.MetadataJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("contentBytes", auditEvent.MetadataJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TC_4_2_4_Failed_upload_validation_is_audit_logged_and_not_usable()
    {
        var tenantId = Guid.Parse("42424242-4242-4242-4242-4242424242a4");
        var userId = Guid.Parse("42424242-4242-4242-4242-4242424242b4");
        var evidenceItemId = Guid.Parse("42424242-4242-4242-4242-4242424242c4");
        await using var factory = CreateFactory("tc-4-2-4", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenant(tenantId, "TC-4.2.4 Tenant"));
            dbContext.NoCuiAcknowledgements.Add(CreateAcknowledgement(tenantId, userId));
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();
        using var uploadRequest = CreateRequest(
            HttpMethod.Post,
            $"/api/evidence-items/{evidenceItemId}/upload-intents",
            new EvidenceUploadIntentRequest("policy.pdf", "image/png", 1024, NoCuiAttestation: true),
            tenantId,
            userId,
            Permission.ManageEvidence);

        var response = await client.SendAsync(uploadRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Empty(await dbContext.EvidenceItems.Where(candidate => candidate.TenantId == tenantId).ToListAsync());

        var auditEvent = await dbContext.AuditLogEntries.SingleAsync(candidate =>
            candidate.TenantId == tenantId &&
            candidate.ActorUserId == userId &&
            candidate.Action == AuditAction.Rejected &&
            candidate.EntityType == "EvidenceUploadIntent" &&
            candidate.EntityId == evidenceItemId.ToString());

        Assert.Contains("rejected", auditEvent.Summary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("policy.pdf", auditEvent.MetadataJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("image/png", auditEvent.MetadataJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Content type", auditEvent.MetadataJson, StringComparison.OrdinalIgnoreCase);
    }

    private WebApplicationFactory<Program> CreateFactory(
        string databaseName,
        Action<GccsDbContext>? seed = null) =>
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

                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
                dbContext.Database.EnsureDeleted();
                dbContext.Database.EnsureCreated();
                seed?.Invoke(dbContext);
            });
        });

    private static HttpRequestMessage CreateRequest<TContent>(
        HttpMethod method,
        string requestUri,
        TContent? content,
        Guid tenantId,
        Guid userId,
        Permission permission)
    {
        var request = CreateRequest(method, requestUri, tenantId, userId, permission);
        if (content is not null)
        {
            request.Content = JsonContent.Create(content, options: JsonOptions);
        }

        return request;
    }

    private static HttpRequestMessage CreateRequest(
        HttpMethod method,
        string requestUri,
        Guid tenantId,
        Guid userId,
        Permission permission)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", userId.ToString());
        request.Headers.Add("X-Gccs-Dev-Email", "no.cui.user@example.com");
        request.Headers.Add("X-Gccs-Dev-Permissions", permission.ToString());

        return request;
    }

    private static TenantEntity CreateTenant(Guid tenantId, string name) =>
        new()
        {
            Id = tenantId,
            Name = name,
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.Parse("2026-06-13T12:00:00Z")
        };

    private static NoCuiAcknowledgementEntity CreateAcknowledgement(Guid tenantId, Guid userId) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            NoticeVersion = NoCuiNotice.CurrentVersion,
            NoticeCopy = NoCuiNotice.Copy,
            AcknowledgedAt = DateTimeOffset.Parse("2026-06-14T12:00:00Z"),
            CreatedAt = DateTimeOffset.Parse("2026-06-14T12:00:00Z"),
            CreatedByUserId = userId
        };
}

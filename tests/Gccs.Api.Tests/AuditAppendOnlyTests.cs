using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Gccs.Application.Audit;
using Gccs.Application.NoCui;
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

public sealed class AuditAppendOnlyTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuditAppendOnlyTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_5_1_1_Sensitive_action_creates_audit_event_with_required_fields()
    {
        var tenantId = Guid.Parse("51515151-5151-5151-5151-5151515151a1");
        var actorUserId = Guid.Parse("51515151-5151-5151-5151-5151515151b1");
        await using var factory = CreateFactory("tc-5-1-1", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenant(tenantId, "TC-5.1.1 Tenant"));
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();
        using var request = CreateRequest(
            HttpMethod.Post,
            "/api/no-cui-acknowledgement",
            new AcknowledgeNoCuiRequest(true, NoCuiNotice.CurrentVersion),
            tenantId,
            actorUserId,
            Permission.ManageEvidence);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var auditEvent = await dbContext.AuditLogEntries.SingleAsync(candidate =>
            candidate.TenantId == tenantId &&
            candidate.ActorUserId == actorUserId);

        Assert.NotEqual(Guid.Empty, auditEvent.Id);
        Assert.Equal(AuditAction.Created, auditEvent.Action);
        Assert.Equal("NoCuiAcknowledgement", auditEvent.EntityType);
        Assert.Contains(actorUserId.ToString(), auditEvent.EntityId, StringComparison.Ordinal);
        Assert.True(auditEvent.OccurredAt <= DateTimeOffset.UtcNow);
        Assert.False(string.IsNullOrWhiteSpace(auditEvent.Summary));
        Assert.Contains("acknowledged", auditEvent.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TC_5_1_2_Audit_events_are_append_only_through_normal_apis()
    {
        var tenantId = Guid.Parse("51515151-5151-5151-5151-5151515151a2");
        var auditEntryId = Guid.Parse("51515151-5151-5151-5151-5151515151b2");
        await using var factory = CreateFactory("tc-5-1-2", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenant(tenantId, "TC-5.1.2 Tenant"));
            dbContext.AuditLogEntries.Add(CreateAuditEntry(auditEntryId, tenantId));
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();
        using var putRequest = CreateRequest(HttpMethod.Put, $"/api/audit-logs/{auditEntryId}", tenantId, Guid.NewGuid(), Permission.ViewAuditLog);
        using var patchRequest = CreateRequest(HttpMethod.Patch, $"/api/audit-logs/{auditEntryId}", tenantId, Guid.NewGuid(), Permission.ViewAuditLog);
        using var deleteRequest = CreateRequest(HttpMethod.Delete, $"/api/audit-logs/{auditEntryId}", tenantId, Guid.NewGuid(), Permission.ViewAuditLog);

        var putResponse = await client.SendAsync(putRequest);
        var patchResponse = await client.SendAsync(patchRequest);
        var deleteResponse = await client.SendAsync(deleteRequest);
        var deleteBody = await deleteResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.MethodNotAllowed, putResponse.StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, patchResponse.StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, deleteResponse.StatusCode);
        Assert.Contains("audit_log_append_only", deleteBody, StringComparison.OrdinalIgnoreCase);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var auditEntry = await dbContext.AuditLogEntries.SingleAsync(candidate => candidate.Id == auditEntryId);

        Assert.Equal("Original append-only summary.", auditEntry.Summary);
        Assert.Equal(AuditAction.Created, auditEntry.Action);
    }

    [Fact]
    public async Task TC_5_1_3_Critical_audit_writer_failure_surfaces_clear_error()
    {
        var tenantId = Guid.Parse("51515151-5151-5151-5151-5151515151a3");
        var actorUserId = Guid.Parse("51515151-5151-5151-5151-5151515151b3");
        await using var factory = CreateFactory(
            "tc-5-1-3",
            dbContext =>
            {
                dbContext.Tenants.Add(CreateTenant(tenantId, "TC-5.1.3 Tenant"));
                dbContext.SaveChanges();
            },
            failAuditWrites: true);
        using var client = factory.CreateClient();
        using var request = CreateRequest(
            HttpMethod.Post,
            "/api/no-cui-acknowledgement",
            new AcknowledgeNoCuiRequest(true, NoCuiNotice.CurrentVersion),
            tenantId,
            actorUserId,
            Permission.ManageEvidence);

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("Critical audit failure", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("audit_write_failed", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TC_5_1_4_Request_metadata_is_captured_when_available()
    {
        var tenantId = Guid.Parse("51515151-5151-5151-5151-5151515151a4");
        var actorUserId = Guid.Parse("51515151-5151-5151-5151-5151515151b4");
        const string correlationId = "tc-5-1-4-correlation";
        const string sourceIp = "203.0.113.42";
        const string userAgent = "GCCS Story 5.1 Test Agent";
        await using var factory = CreateFactory("tc-5-1-4", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenant(tenantId, "TC-5.1.4 Tenant"));
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();
        using var request = CreateRequest(
            HttpMethod.Post,
            "/api/no-cui-acknowledgement",
            new AcknowledgeNoCuiRequest(true, NoCuiNotice.CurrentVersion),
            tenantId,
            actorUserId,
            Permission.ManageEvidence);
        request.Headers.Add("X-Correlation-ID", correlationId);
        request.Headers.Add("X-Forwarded-For", sourceIp);
        request.Headers.UserAgent.ParseAdd(userAgent);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var auditEvent = await dbContext.AuditLogEntries.SingleAsync(candidate => candidate.TenantId == tenantId);

        Assert.Equal(sourceIp, auditEvent.IpAddress);
        Assert.Equal(userAgent, auditEvent.UserAgent);
        Assert.Equal(correlationId, auditEvent.CorrelationId);
        Assert.Contains(correlationId, auditEvent.MetadataJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Audit_writer_persists_change_snapshot_and_redacts_sensitive_values()
    {
        var tenantId = Guid.Parse("51515151-5151-5151-5151-5151515151a5");
        var actorUserId = Guid.Parse("51515151-5151-5151-5151-5151515151b5");
        await using var factory = CreateFactory("audit-change-snapshot", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenant(tenantId, "Audit change snapshot tenant"));
            dbContext.SaveChanges();
        });

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var writer = new EfAuditEventWriter(
            dbContext,
            new StaticAuditRequestMetadata("198.51.100.5", "audit-test-agent", "audit-change-correlation"));

        await writer.WriteChangeAsync(
            tenantId,
            actorUserId,
            AuditAction.Updated,
            "ControlAssessment",
            "assessment:AC.L1-3.1.1",
            "Control status changed.",
            SensitiveSnapshot("NotStarted", "before-value", "before-marker"),
            SensitiveSnapshot("Implemented", "after-value", "after-marker"),
            new Dictionary<string, string>
            {
                ["reason"] = "reviewed",
                [string.Concat("refresh", "Token")] = "refresh-marker"
            });

        var auditEvent = await dbContext.AuditLogEntries.SingleAsync(candidate => candidate.TenantId == tenantId);

        Assert.Equal(actorUserId, auditEvent.ActorUserId);
        Assert.Equal("audit-change-correlation", auditEvent.CorrelationId);
        Assert.Contains("\"status\":\"NotStarted\"", auditEvent.OldValue, StringComparison.Ordinal);
        Assert.Contains("\"status\":\"Implemented\"", auditEvent.NewValue, StringComparison.Ordinal);
        Assert.DoesNotContain("before-value", auditEvent.OldValue, StringComparison.Ordinal);
        Assert.DoesNotContain("after-value", auditEvent.NewValue, StringComparison.Ordinal);
        Assert.DoesNotContain("before-marker", auditEvent.OldValue, StringComparison.Ordinal);
        Assert.DoesNotContain("after-marker", auditEvent.NewValue, StringComparison.Ordinal);
        Assert.DoesNotContain("refresh-marker", auditEvent.MetadataJson, StringComparison.Ordinal);
        Assert.Contains("[redacted]", auditEvent.MetadataJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Audit_log_entries_cannot_be_updated_or_deleted_through_db_context()
    {
        var tenantId = Guid.Parse("51515151-5151-5151-5151-5151515151a6");
        var auditEntryId = Guid.Parse("51515151-5151-5151-5151-5151515151b6");
        await using var factory = CreateFactory("audit-dbcontext-append-only", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenant(tenantId, "Audit DbContext append-only tenant"));
            dbContext.AuditLogEntries.Add(CreateAuditEntry(auditEntryId, tenantId));
            dbContext.SaveChanges();
        });

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
            var auditEntry = await dbContext.AuditLogEntries.SingleAsync(candidate => candidate.Id == auditEntryId);
            auditEntry.Summary = "Tampered summary.";

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => dbContext.SaveChangesAsync());
            Assert.Contains("append-only", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
            var auditEntry = await dbContext.AuditLogEntries.SingleAsync(candidate => candidate.Id == auditEntryId);
            dbContext.AuditLogEntries.Remove(auditEntry);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => dbContext.SaveChangesAsync());
            Assert.Contains("append-only", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Failed_authorization_attempt_is_audit_logged_when_tenant_context_is_available()
    {
        var tenantId = Guid.Parse("51515151-5151-5151-5151-5151515151a7");
        var actorUserId = Guid.Parse("51515151-5151-5151-5151-5151515151b7");
        const string correlationId = "failed-authorization-correlation";
        await using var factory = CreateFactory("audit-failed-authorization", dbContext =>
        {
            dbContext.Tenants.Add(CreateTenant(tenantId, "Audit failed authorization tenant"));
            dbContext.SaveChanges();
        });
        using var client = factory.CreateClient();
        using var request = CreateRequest(
            HttpMethod.Get,
            "/api/audit-logs",
            tenantId,
            actorUserId,
            Permission.ManageEvidence);
        request.Headers.Add("X-Correlation-ID", correlationId);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var auditEvent = await dbContext.AuditLogEntries.SingleAsync(candidate => candidate.TenantId == tenantId);

        Assert.Equal(actorUserId, auditEvent.ActorUserId);
        Assert.Equal(AuditAction.Rejected, auditEvent.Action);
        Assert.Equal("Authorization", auditEvent.EntityType);
        Assert.Equal("/api/audit-logs", auditEvent.EntityId);
        Assert.Equal(correlationId, auditEvent.CorrelationId);
        Assert.Contains("Authorization attempt was denied", auditEvent.Summary, StringComparison.Ordinal);
    }

    private WebApplicationFactory<Program> CreateFactory(
        string databaseName,
        Action<GccsDbContext>? seed = null,
        bool failAuditWrites = false) =>
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
                services.AddSingleton<IObjectStorageService, TestObjectStorageService>();

                if (failAuditWrites)
                {
                    services.AddScoped<IAuditEventWriter, FailingAuditEventWriter>();
                }

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
            request.Content = JsonContent.Create(content);
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
        request.Headers.Add("X-Gccs-Dev-Email", "audit.user@example.com");
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
            CreatedAt = DateTimeOffset.Parse("2026-06-15T12:00:00Z")
        };

    private static AuditLogEntryEntity CreateAuditEntry(Guid auditEntryId, Guid tenantId) =>
        new()
        {
            Id = auditEntryId,
            TenantId = tenantId,
            ActorUserId = Guid.NewGuid(),
            Action = AuditAction.Created,
            EntityType = "Tenant",
            EntityId = tenantId.ToString(),
            OccurredAt = DateTimeOffset.Parse("2026-06-15T12:05:00Z"),
            IpAddress = "203.0.113.10",
            UserAgent = "seed",
            CorrelationId = "seed-correlation",
            Summary = "Original append-only summary.",
            MetadataJson = "{}"
        };

    private static string SensitiveSnapshot(string status, string sensitiveValue, string sensitiveMarker) =>
        JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["status"] = status,
            [string.Concat("pass", "word")] = sensitiveValue,
            ["nested"] = new Dictionary<string, string>
            {
                [string.Concat("api", "Token")] = sensitiveMarker
            }
        });

    private sealed class FailingAuditEventWriter : IAuditEventWriter
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
            throw new AuditWriteException("A critical audit event could not be written.");
    }

    private sealed record StaticAuditRequestMetadata(
        string IpAddress,
        string UserAgent,
        string CorrelationId) : IAuditRequestMetadata;
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Gccs.Application.Audit;
using Gccs.Domain.Audit;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class CuiAuditExportTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly Guid TenantId = Guid.Parse("1a080200-0000-4000-8000-000000000001");
    private static readonly Guid ActorUserId = Guid.Parse("1a080200-0000-4000-8000-000000000002");
    private static readonly Guid OtherTenantId = Guid.Parse("1a080200-0000-4000-8000-000000000003");

    [Fact]
    public async Task TC_1A_8_2_1_Actual_list_endpoint_filters_normalized_persisted_columns_and_tenant()
    {
        await using var factory = CreateFactory("cui-audit-filter", db =>
        {
            db.AuditLogEntries.AddRange(
                Entry(TenantId, 1, "blocked-upload", "Cui", "NoCui", "blocked", ActorUserId, "EvidenceUploadIntent"),
                Entry(TenantId, 2, "blocked-upload", "Fci", "NoCui", "blocked", ActorUserId, "EvidenceUploadIntent"),
                Entry(TenantId, 3, "escalation-create", "Cui", "NoCui", "succeeded", ActorUserId, "CuiSupportEscalation"),
                Entry(OtherTenantId, 1, "blocked-upload", "Cui", "NoCui", "blocked", ActorUserId, "EvidenceUploadIntent"));
        });
        using var client = factory.CreateClient();
        var from = Uri.EscapeDataString("2026-09-08T12:00:00Z");
        var to = Uri.EscapeDataString("2026-09-08T12:02:00Z");
        using var request = Request(HttpMethod.Get,
            $"/api/audit-logs?eventType=BLOCKED_UPLOAD&classification=cui&mode=nocui&result=BLOCKED&actorUserId={ActorUserId}&entityType=EvidenceUploadIntent&from={from}&to={to}");

        using var response = await client.SendAsync(request);
        var page = await response.Content.ReadFromJsonAsync<PagedResultDto<AuditLogEntryDto>>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var item = Assert.Single(Assert.IsType<PagedResultDto<AuditLogEntryDto>>(page).Items);
        Assert.Equal("blocked-upload", item.EventType);
        Assert.Equal("Cui", item.Classification);
        Assert.Equal("NoCui", item.Mode);
        Assert.Equal("blocked", item.Result);
        Assert.Equal(TenantId, item.TenantId);
    }

    [Theory]
    [InlineData("/api/audit-logs")]
    [InlineData("/api/audit-logs/cui-export")]
    public async Task TC_1A_8_2_2_Actual_endpoints_deny_callers_without_audit_permission(string path)
    {
        await using var factory = CreateFactory($"cui-audit-denied-{path.GetHashCode()}", _ => { });
        using var client = factory.CreateClient();
        var isExport = path.EndsWith("cui-export", StringComparison.Ordinal);
        using var request = Request(isExport ? HttpMethod.Post : HttpMethod.Get, path, Permission.ViewEvidence,
            isExport ? new CuiAuditExportRequest(null, null, null, null, null, null, null, null) : null);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var deniedAudit = Assert.Single(await scope.ServiceProvider.GetRequiredService<GccsDbContext>().AuditLogEntries.ToListAsync());
        Assert.Equal(AuditAction.Rejected, deniedAudit.Action);
        Assert.Equal("rejected", deniedAudit.Result);
        Assert.Equal(path, deniedAudit.EntityId);
        Assert.DoesNotContain("CuiAuditExport", deniedAudit.EntityType, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TC_1A_8_2_3_Actual_export_returns_all_matching_rows_and_persists_export_audit()
    {
        const int matchingCount = 350;
        await using var factory = CreateFactory("cui-audit-all-matches", db =>
        {
            db.AuditLogEntries.AddRange(Enumerable.Range(1, matchingCount)
                .Select(index => Entry(TenantId, index, "blocked-upload", "Cui", "NoCui", "blocked", ActorUserId, "EvidenceUploadIntent")));
            db.AuditLogEntries.Add(Entry(TenantId, matchingCount + 1, "download", "Fci", "NoCui", "succeeded", ActorUserId, "EvidenceFileVersion"));
            db.AuditLogEntries.Add(Entry(OtherTenantId, matchingCount + 2, "blocked-upload", "Cui", "NoCui", "blocked", ActorUserId, "EvidenceUploadIntent"));
        });
        using var client = factory.CreateClient();
        var filters = new CuiAuditExportRequest("blocked-upload", "Cui", "NoCui", ActorUserId,
            "EvidenceUploadIntent", null, null, "blocked", "Rejected");
        using var request = Request(HttpMethod.Post, "/api/audit-logs/cui-export", Permission.ViewAuditLog, filters);

        using var response = await client.SendAsync(request);
        var export = await response.Content.ReadFromJsonAsync<CuiAuditExportDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(export);
        Assert.Equal(TenantId, export.TenantId);
        Assert.Equal(ActorUserId, export.GeneratedByUserId);
        Assert.NotEqual(default, export.GeneratedAt);
        Assert.Equal(filters, export.Filters);
        Assert.Equal(matchingCount, export.Events.Count);
        Assert.Equal(matchingCount, export.Events.Select(item => item.Id).Distinct().Count());
        Assert.All(export.Events, item =>
        {
            Assert.Equal(TenantId, item.TenantId);
            Assert.Equal("blocked-upload", item.EventType);
            Assert.Equal("Cui", item.Classification);
            Assert.Equal("NoCui", item.Mode);
            Assert.Equal("blocked", item.Result);
        });

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var persisted = await db.AuditLogEntries.SingleAsync(item => item.EntityType == "CuiAuditExport");
        Assert.Equal(Phase1ACuiAuditEvents.Export, persisted.EventType);
        Assert.Equal("succeeded", persisted.Result);
        Assert.Equal(ActorUserId, persisted.ActorUserId);
    }

    [Fact]
    public async Task Oversized_actual_export_returns_413_without_a_success_audit_or_partial_response()
    {
        await using var factory = CreateFactory("cui-audit-limit", db =>
            db.AuditLogEntries.AddRange(Enumerable.Range(1, 10001)
                .Select(index => Entry(TenantId, index, "blocked-upload", "Cui", "NoCui", "blocked", ActorUserId, "EvidenceUploadIntent"))));
        using var client = factory.CreateClient();
        using var request = Request(HttpMethod.Post, "/api/audit-logs/cui-export", Permission.ViewAuditLog,
            new CuiAuditExportRequest("blocked-upload", null, null, null, null, null, null, null));

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Contains("audit_export_too_large", await response.Content.ReadAsStringAsync());
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.False(await db.AuditLogEntries.AnyAsync(item => item.EntityType == "CuiAuditExport"));
    }

    private static WebApplicationFactory<Program> CreateFactory(string databaseName, Action<GccsDbContext> seed) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<AuditLogService>();
                services.AddScoped<CuiAuditExportService>();
                services.AddScoped<IAuditLogRepository, EfAuditLogRepository>();
                services.AddScoped<IAuditEventWriter, EfAuditEventWriter>();

                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
                db.Tenants.AddRange(
                    new TenantEntity { Id = TenantId, Name = "Audit tenant", Status = TenantStatus.Active, DataPosture = TenantDataPosture.NoCui },
                    new TenantEntity { Id = OtherTenantId, Name = "Other audit tenant", Status = TenantStatus.Active, DataPosture = TenantDataPosture.NoCui });
                seed(db);
                db.SaveChanges();
            });
        });

    private static HttpRequestMessage Request(HttpMethod method, string path, Permission permission = Permission.ViewAuditLog, object? body = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", TenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", ActorUserId.ToString());
        request.Headers.Add("X-Gccs-Dev-Permissions", permission.ToString());
        if (body is not null) request.Content = JsonContent.Create(body);
        return request;
    }

    private static AuditLogEntryEntity Entry(Guid tenantId, int sequence, string eventType, string classification,
        string mode, string result, Guid actorUserId, string entityType) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        ActorUserId = actorUserId,
        Action = result == "blocked" ? AuditAction.Rejected : AuditAction.Created,
        EventType = eventType,
        Classification = classification,
        Mode = mode,
        Result = result,
        EntityType = entityType,
        EntityId = Guid.NewGuid().ToString(),
        OccurredAt = DateTimeOffset.Parse("2026-09-08T12:00:00Z").AddSeconds(sequence),
        IpAddress = "203.0.113.10",
        UserAgent = "test",
        CorrelationId = $"cui-audit-{sequence}",
        Summary = $"{eventType} recorded.",
        MetadataJson = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["eventType"] = eventType,
            ["classification"] = classification,
            ["mode"] = mode,
            ["result"] = result
        })
    };
}

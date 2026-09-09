using System.Net;
using System.Net.Http.Json;
using Gccs.Application.Audit;
using Gccs.Application.Tenancy;
using Gccs.Domain.Common;
using Gccs.Domain.Identity;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Gccs.Infrastructure.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.NoCui;
using Gccs.Application.NoCui;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class Phase1ACuiAuditEventTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly Guid TenantId = Guid.Parse("1a080100-0000-4000-8000-000000000001");
    private static readonly Guid ActorUserId = Guid.Parse("1a080100-0000-4000-8000-000000000002");
    private static readonly Guid EvidenceId = Guid.Parse("1a080100-0000-4000-8000-000000000003");
    private readonly WebApplicationFactory<Program> factory;

    public Phase1ACuiAuditEventTests(WebApplicationFactory<Program> factory) => this.factory = factory;

    [Fact]
    public async Task Mode_change_and_failed_CuiReady_approval_emit_normalized_persisted_events()
    {
        await using var app = CreateFactory("phase1a-audit-mode");
        using var client = app.CreateClient();
        using var success = await client.SendAsync(Request(HttpMethod.Patch,
            $"/api/tenants/{TenantId}/data-handling-mode", Permission.ManageTenant,
            new { dataHandlingMode = "DemoSandbox", reason = "Synthetic mode transition." }));
        using var rejected = await client.SendAsync(Request(HttpMethod.Patch,
            $"/api/tenants/{TenantId}/data-handling-mode", Permission.ManageTenant,
            new { dataHandlingMode = "CuiReady", reason = "Synthetic rejected transition.", approvalRecordReference = Guid.NewGuid().ToString() }));

        Assert.Equal(HttpStatusCode.OK, success.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        await using var scope = app.Services.CreateAsyncScope();
        var events = await scope.ServiceProvider.GetRequiredService<GccsDbContext>().AuditLogEntries
            .Where(e => e.TenantId == TenantId).ToArrayAsync();
        Assert.Contains(events, e => e.EventType == Phase1ACuiAuditEvents.ModeChange && e.Mode == "DemoSandbox" && e.Result == "succeeded");
        Assert.Contains(events, e => e.EventType == Phase1ACuiAuditEvents.FailedModeChange && e.Mode == "CuiReady" && e.Result == "failed");
        Assert.All(events, AssertRequiredDimensions);
    }

    [Fact]
    public async Task Blocked_upload_endpoint_persists_a_safe_normalized_failure_event()
    {
        await using var app = CreateFactory("phase1a-audit-upload");
        using var client = app.CreateClient();
        const string sensitiveMarker = "classified paragraph alpha-bravo";
        using var response = await client.SendAsync(Request(HttpMethod.Post,
            $"/api/evidence-items/{EvidenceId}/upload-intents", Permission.ManageEvidence,
            new { fileName = "synthetic.pdf", contentType = "application/pdf", sizeBytes = 256,
                noCuiAttestation = true, containsPotentialCui = true,
                classification = new { classification = "Cui", source = "UserSelected", reason = sensitiveMarker } }));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await using var scope = app.Services.CreateAsyncScope();
        var audit = await scope.ServiceProvider.GetRequiredService<GccsDbContext>().AuditLogEntries
            .Where(e => e.TenantId == TenantId && e.EventType == Phase1ACuiAuditEvents.BlockedUpload).ToArrayAsync();
        Assert.NotEmpty(audit);
        Assert.All(audit, item =>
        {
            Assert.Equal("rejected", item.Result);
            Assert.DoesNotContain(sensitiveMarker, item.Summary, StringComparison.OrdinalIgnoreCase);
            AssertRequiredDimensions(item);
        });
    }

    [Fact]
    public async Task Escalation_create_and_update_endpoints_commit_history_and_audit_together()
    {
        await using var app = CreateFactory("phase1a-audit-escalation", includeSupportNotice: true);
        using var client = app.CreateClient();
        using var createdResponse = await client.SendAsync(Request(HttpMethod.Post,
            $"/api/tenants/{TenantId}/cui-support-escalations", Permission.ViewEvidence,
            new { sourceWorkflow = "EvidenceDetail", affectedEntityType = "EvidenceItem", affectedEntityId = EvidenceId.ToString(),
                category = "SuspectedCui", severity = "High", description = "Synthetic metadata-only concern." }));
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var createdDocument = await createdResponse.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var escalationId = createdDocument.GetProperty("id").GetGuid();

        using var updatedResponse = await client.SendAsync(Request(HttpMethod.Patch,
            $"/api/tenants/{TenantId}/cui-support-escalations/{escalationId}", Permission.ManageTenant,
            new { owner = "Security", severity = "Critical", status = "Triage", note = "Synthetic triage started." }));
        Assert.Equal(HttpStatusCode.OK, updatedResponse.StatusCode);

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var escalation = await db.CuiSupportEscalations.Include(e => e.Events).SingleAsync(e => e.Id == escalationId);
        var audits = await db.AuditLogEntries.Where(e => e.EntityId == escalationId.ToString()).ToArrayAsync();
        Assert.Equal(CuiSupportEscalationStatus.Triage, escalation.Status);
        Assert.Equal(2, escalation.Events.Count);
        Assert.Contains(audits, e => e.EventType == Phase1ACuiAuditEvents.EscalationCreate && e.Result == "succeeded");
        Assert.Contains(audits, e => e.EventType == Phase1ACuiAuditEvents.EscalationUpdate && e.Result == "succeeded");
        Assert.All(audits, AssertRequiredDimensions);
    }

    [Fact]
    public void Required_event_catalog_is_complete_and_normalized()
    {
        Assert.Equal(18, Phase1ACuiAuditEvents.RequiredEvents.Select(e => e.EventType).Distinct().Count());
        Assert.All(Phase1ACuiAuditEvents.RequiredEvents, required =>
            Assert.Equal(required.EventType, Phase1ACuiAuditEvents.NormalizeEventType(required.EventType)));
    }

    private WebApplicationFactory<Program> CreateFactory(string databaseName, bool includeSupportNotice = false) =>
        factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<IAuditEventWriter, EfAuditEventWriter>();
                services.AddScoped<ITenantRepository, EfTenantRepository>();
                services.AddScoped<ICuiReadyApprovalChecklistRepository, EfCuiReadyApprovalChecklistRepository>();
                services.AddScoped<ICuiReadyApprovalChecklistGate>(provider => provider.GetRequiredService<CuiReadyApprovalChecklistService>());
                services.AddScoped<ICuiSupportEscalationRepository, EfCuiSupportEscalationRepository>();
                services.AddScoped<IDataHandlingNoticeAcknowledgementRepository, EfDataHandlingNoticeAcknowledgementRepository>();
                services.AddScoped<INoCuiAcknowledgementRepository, EfNoCuiAcknowledgementRepository>();
                services.AddScoped<Gccs.Application.Evidence.IEvidenceMetadataRepository, Gccs.Infrastructure.Evidence.EfEvidenceMetadataRepository>();
                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
                db.Tenants.Add(new TenantEntity { Id = TenantId, Name = "Synthetic audit tenant", Status = TenantStatus.Active,
                    DataPosture = TenantDataPosture.NoCui, CreatedAt = DateTimeOffset.UtcNow });
                db.EvidenceItems.Add(new EvidenceItemEntity { Id = EvidenceId, TenantId = TenantId, Name = "Synthetic evidence",
                    OwnerFunction = "Security", Classification = ContentClassification.Unclassified,
                    ClassificationSource = ContentClassificationSource.UserSelected, CreatedAt = DateTimeOffset.UtcNow });
                db.NoCuiAcknowledgements.Add(new NoCuiAcknowledgementEntity { Id = Guid.NewGuid(), TenantId = TenantId,
                    UserId = ActorUserId, NoticeVersion = Gccs.Application.NoCui.NoCuiNotice.CurrentVersion,
                    NoticeCopy = "Synthetic fixture", AcknowledgedAt = DateTimeOffset.UtcNow, CreatedAt = DateTimeOffset.UtcNow });
                if (includeSupportNotice)
                {
                    var notice = new DataHandlingNoticeService(new FileDataHandlingNoticeRepository())
                        .GetPublishedAsync(CuiReadinessTestData.PackageRoot, TenantDataPosture.NoCui, "Support").GetAwaiter().GetResult()!;
                    db.DataHandlingNoticeAcknowledgements.Add(new DataHandlingNoticeAcknowledgementEntity { Id = Guid.NewGuid(), TenantId = TenantId,
                        UserId = ActorUserId, Mode = TenantDataPosture.NoCui, WorkflowContext = "Support", NoticeId = notice.NoticeId,
                        NoticeVersion = notice.Version, AcknowledgedAt = DateTimeOffset.UtcNow, CreatedAt = DateTimeOffset.UtcNow });
                }
                db.SaveChanges();
            });
        });

    private static HttpRequestMessage Request(HttpMethod method, string uri, Permission permission, object body)
    {
        var request = new HttpRequestMessage(method, uri) { Content = JsonContent.Create(body) };
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", TenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", ActorUserId.ToString());
        request.Headers.Add("X-Gccs-Dev-Email", "security.owner@example.invalid");
        request.Headers.Add("X-Gccs-Dev-Permissions", permission.ToString());
        request.Headers.Add("X-Gccs-Dev-Platform-Permissions", "ApproveCuiReadiness");
        request.Headers.Add("X-Correlation-ID", $"phase1a-{Guid.NewGuid():N}");
        return request;
    }

    private static void AssertRequiredDimensions(AuditLogEntryEntity audit)
    {
        Assert.Equal(TenantId, audit.TenantId);
        Assert.Equal(ActorUserId, audit.ActorUserId);
        Assert.False(string.IsNullOrWhiteSpace(audit.EventType));
        Assert.False(string.IsNullOrWhiteSpace(audit.EntityType));
        Assert.False(string.IsNullOrWhiteSpace(audit.EntityId));
        Assert.False(string.IsNullOrWhiteSpace(audit.Result));
        Assert.False(string.IsNullOrWhiteSpace(audit.CorrelationId));
        Assert.NotEqual(default, audit.OccurredAt);
    }
}

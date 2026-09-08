using System.Net;
using System.Net.Http.Json;
using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Application.Security;
using Gccs.Application.Tenancy;
using Gccs.Domain.Common;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Gccs.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class ClassifiedWorkflowTests
{
    private readonly Guid tenant = Guid.NewGuid();
    private readonly Guid user = Guid.NewGuid();

    private WebApplicationFactory<Program> Factory(bool notice = true, TenantDataPosture mode = TenantDataPosture.NoCui, bool postgres = false, AuditFailure? failure = null) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:GccsDatabase", "");
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(o =>
                {
                    if (postgres) o.UseGccsPostgres(Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION")!);
                    else o.UseInMemoryDatabase(tenant.ToString());
                    if (failure is not null) o.AddInterceptors(failure);
                });
                services.AddScoped<ITenantRepository, EfTenantRepository>();
                services.AddScoped<ISyntheticContentApprovalRepository, EfSyntheticContentApprovalRepository>();
                services.AddScoped<IClassifiedNoteRepository, Gccs.Infrastructure.Common.EfClassifiedNoteRepository>();
                services.AddScoped<IDataHandlingNoticeAcknowledgementRepository, EfDataHandlingNoticeAcknowledgementRepository>();
                services.AddScoped<IAuditEventWriter, EfAuditEventWriter>();
                services.AddScoped<Gccs.Application.Reports.IReportRepository, Gccs.Infrastructure.Reports.EfReportRepository>();
                services.AddScoped<ICuiReadyApprovalChecklistRepository, EfCuiReadyApprovalChecklistRepository>();
                services.AddScoped<ICuiReadyApprovalChecklistGate, CuiReadyApprovalChecklistService>();
                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
                if (postgres) PostgresTestDatabase.Migrate(db);
                db.Tenants.Add(new() { Id = tenant, Name = "Synthetic classified workflow", DataPosture = mode });
                if (notice) foreach (var workflow in new[] { "Onboarding", "ClassifiedNote", "ReportGeneration", "ContractUpload", "ExtractionJob" })
                    db.DataHandlingNoticeAcknowledgements.Add(new() { Id = Guid.NewGuid(), TenantId = tenant, UserId = user,
                        Mode = mode, WorkflowContext = workflow, NoticeId = mode == TenantDataPosture.NoCui ? "no-cui-general" : mode == TenantDataPosture.DemoSandbox ? "demo-sandbox-general" : "cui-ready-general",
                        NoticeVersion = "2026.06.phase1a", AcknowledgedAt = DateTimeOffset.UtcNow });
                db.SaveChanges();
            });
        });
    private HttpRequestMessage Request(HttpMethod method, string path, object? body = null, Guid? tenantId = null, string permission = "ManageEvidence")
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add("X-Gccs-Dev-Auth", "true"); request.Headers.Add("X-Gccs-Dev-Tenant", (tenantId ?? tenant).ToString());
        request.Headers.Add("X-Gccs-Dev-User", user.ToString()); request.Headers.Add("X-Gccs-Dev-Permissions", permission);
        if (body is not null) request.Content = JsonContent.Create(body);
        return request;
    }
    private static object Note(string classification = "Unclassified", long revision = 0) =>
        new { title = "Synthetic note", body = "Synthetic No-CUI note text.", classification = new { classification }, revision };

    [Theory]
    [InlineData("Unclassified", 201)] [InlineData("Fci", 201)] [InlineData("Unknown", 201)]
    [InlineData("Cui", 403)] [InlineData("Prohibited", 400)] [InlineData("SyntheticCui", 400)]
    public async Task Note_classification_controls_storage(string classification, int status)
    {
        await using var factory = Factory(); using var client = factory.CreateClient();
        using var response = await client.SendAsync(Request(HttpMethod.Post, "/api/classified-notes", Note(classification)));
        Assert.Equal(status, (int)response.StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Equal(status == 201 ? 1 : 0, await db.Set<ClassifiedNoteEntity>().CountAsync());
        Assert.Equal(status == 201 ? 1 : 0, await db.ContentClassificationHistory.CountAsync());
    }
    [Fact]
    public async Task Note_requires_current_notice_before_any_content_or_history_write()
    {
        await using var factory = Factory(false); using var client = factory.CreateClient();
        using var response = await client.SendAsync(Request(HttpMethod.Post, "/api/classified-notes", Note()));
        Assert.Equal(428, (int)response.StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Empty(db.Set<ClassifiedNoteEntity>()); Assert.Empty(db.ContentClassificationHistory); Assert.Empty(db.AuditLogEntries);
    }
    [Fact]
    public async Task Classified_note_notice_can_be_retrieved_acknowledged_and_then_enforced_by_the_api()
    {
        await using var factory = Factory(false); using var client = factory.CreateClient();
        using var current = await client.SendAsync(Request(HttpMethod.Get,
            "/api/data-handling-notices/published?workflowContext=ClassifiedNote", permission: "ManageEvidence"));
        current.EnsureSuccessStatusCode();
        var notice = (await current.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>());
        var noticeId = notice.GetProperty("noticeId").GetString()!;
        var noticeVersion = notice.GetProperty("version").GetString()!;
        using var acknowledged = await client.SendAsync(Request(HttpMethod.Post,
            $"/api/tenants/{tenant}/data-handling-notice-acknowledgements",
            new AcknowledgeDataHandlingNoticeRequest(TenantDataPosture.NoCui, "ClassifiedNote", noticeId, noticeVersion, true)));
        Assert.Equal(HttpStatusCode.Created, acknowledged.StatusCode);

        using var saved = await client.SendAsync(Request(HttpMethod.Post, "/api/classified-notes", Note()));

        Assert.Equal(HttpStatusCode.Created, saved.StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var record = Assert.Single(db.DataHandlingNoticeAcknowledgements);
        Assert.Equal("ClassifiedNote", record.WorkflowContext);
        Assert.Equal(tenant, record.TenantId); Assert.Equal(user, record.UserId);
        Assert.Contains(db.AuditLogEntries, audit => audit.EntityType == "DataHandlingNoticeAcknowledgement");
    }
    [Theory]
    [InlineData("/api/classified-notes", "ManageEvidence")]
    [InlineData("/api/reports/compliance-status", "ManageReports")]
    [InlineData("/api/reports/subcontractor-compliance", "ManageReports")]
    [InlineData("/api/reports/cmmc-readiness?assessmentId=aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", "ManageReports")]
    [InlineData("/api/reports/evidence-packages", "ManageReports")]
    [InlineData("/api/contracts/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/documents/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb/extraction-jobs", "ManageContracts")]
    public async Task Missing_body_or_classification_enum_is_a_client_error(string path, string permission)
    {
        await using var factory = Factory(); using var client = factory.CreateClient();
        using var missing = await client.SendAsync(Request(HttpMethod.Post, path, permission: permission));
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        using var incomplete = await client.SendAsync(Request(HttpMethod.Post, path, new { classification = new { } }, permission: permission));
        Assert.Equal(HttpStatusCode.BadRequest, incomplete.StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Empty(db.Reports); Assert.Empty(db.Set<ClassifiedNoteEntity>()); Assert.Empty(db.ContentClassificationHistory);
    }
    [Theory]
    [InlineData("/api/classified-notes", "ManageEvidence")]
    [InlineData("/api/reports/compliance-status", "ManageReports")]
    [InlineData("/api/reports/subcontractor-compliance", "ManageReports")]
    [InlineData("/api/reports/cmmc-readiness?assessmentId=aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", "ManageReports")]
    [InlineData("/api/reports/evidence-packages", "ManageReports")]
    public async Task Missing_classification_is_not_defaulted(string path, string permission)
    {
        await using var factory = Factory(); using var client = factory.CreateClient();
        using var response = await client.SendAsync(Request(HttpMethod.Post, path, new { title = "Synthetic", body = "Synthetic" }, permission: permission));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    [Theory]
    [InlineData("/api/reports/compliance-status")]
    [InlineData("/api/reports/subcontractor-compliance")]
    [InlineData("/api/reports/cmmc-readiness?assessmentId=aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")]
    [InlineData("/api/reports/evidence-packages")]
    public async Task Report_requires_current_notice_without_creating_artifacts(string path)
    {
        await using var factory = Factory(notice: false); using var client = factory.CreateClient();
        using var response = await client.SendAsync(Request(HttpMethod.Post, path, new {
            classification = new { classification = "Unclassified" }, obligationIds = new[] { "synthetic" },
            contractIds = Array.Empty<Guid>(), controlIds = Array.Empty<string>(), subcontractorIds = Array.Empty<Guid>()
        }, permission: "ManageReports"));
        Assert.Equal(428, (int)response.StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Empty(db.Reports); Assert.Empty(db.ContentClassificationHistory); Assert.Empty(db.AuditLogEntries);
    }
    [Theory]
    [InlineData("Unclassified", 400)]
    [InlineData("Fci", 201)]
    public async Task Evidence_package_cannot_downgrade_included_evidence(string classification, int status)
    {
        await using var factory = Factory(); using var client = factory.CreateClient();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>(); var id = Guid.NewGuid();
            db.EvidenceItems.Add(new() { Id = id, TenantId = tenant, Name = "Synthetic FCI evidence",
                Status = Gccs.Domain.Evidence.EvidenceStatus.Approved, Classification = ContentClassification.Fci,
                ClassificationSource = ContentClassificationSource.UserSelected,
                Obligations = [new() { EvidenceItemId = id, ObligationId = "synthetic" }] });
            await db.SaveChangesAsync();
        }
        using var response = await client.SendAsync(Request(HttpMethod.Post, "/api/reports/evidence-packages", new {
            classification = new { classification }, obligationIds = new[] { "synthetic" },
            contractIds = Array.Empty<Guid>(), controlIds = Array.Empty<string>(), subcontractorIds = Array.Empty<Guid>()
        }, permission: "ManageReports"));
        Assert.Equal(status, (int)response.StatusCode);
        using var verification = factory.Services.CreateScope(); var database = verification.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Equal(status == 201 ? 1 : 0, await database.Reports.CountAsync());
        Assert.Equal(status == 201 ? 1 : 0, await database.AuditLogEntries.CountAsync(e => e.EntityType == "Report"));
    }

    [Fact]
    public async Task Note_edits_are_permissioned_tenant_scoped_and_revision_checked()
    {
        await using var factory = Factory(); using var client = factory.CreateClient();
        using var created = await client.SendAsync(Request(HttpMethod.Post, "/api/classified-notes", Note()));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var json = await created.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(); var id = json.GetProperty("id").GetGuid();
        using var other = await client.SendAsync(Request(HttpMethod.Get, $"/api/classified-notes/{id}", tenantId: Guid.NewGuid(), permission: "ViewEvidence"));
        Assert.Equal(HttpStatusCode.NotFound, other.StatusCode);
        Assert.Contains("resource_not_found", await other.Content.ReadAsStringAsync());
        using var denied = await client.SendAsync(Request(HttpMethod.Put, $"/api/classified-notes/{id}", Note(revision: 1), permission: "ViewEvidence"));
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        using var saved = await client.SendAsync(Request(HttpMethod.Put, $"/api/classified-notes/{id}", Note(revision: 1)));
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        using var stale = await client.SendAsync(Request(HttpMethod.Put, $"/api/classified-notes/{id}", Note(revision: 1)));
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Equal(2, (await db.Set<ClassifiedNoteEntity>().SingleAsync()).Revision);
        Assert.Equal(2, await db.AuditLogEntries.CountAsync(entry => entry.EntityType == "ClassifiedNote"));
    }
    [Theory]
    [InlineData("Owner", true)]
    [InlineData("Admin", true)]
    [InlineData("Compliance Manager", true)]
    [InlineData("Contributor", true)]
    [InlineData("Advisor", true)]
    [InlineData("Auditor", false)]
    public async Task Note_endpoint_role_matrix(string role, bool canWrite)
    {
        await using var factory = Factory(); using var client = factory.CreateClient();
        using var seed = await client.SendAsync(Request(HttpMethod.Post, "/api/classified-notes", Note()));
        var id = (await seed.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("id").GetGuid();
        HttpRequestMessage AsRole(HttpMethod method, string path, object? body = null)
        {
            var request = Request(method, path, body);
            request.Headers.Remove("X-Gccs-Dev-Permissions"); request.Headers.Add("X-Gccs-Dev-Role", role);
            return request;
        }
        using var list = await client.SendAsync(AsRole(HttpMethod.Get, "/api/classified-notes"));
        using var detail = await client.SendAsync(AsRole(HttpMethod.Get, $"/api/classified-notes/{id}"));
        Assert.Equal(HttpStatusCode.OK, list.StatusCode); Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        Assert.DoesNotContain("Synthetic No-CUI note text", await list.Content.ReadAsStringAsync());
        using var create = await client.SendAsync(AsRole(HttpMethod.Post, "/api/classified-notes", Note()));
        using var edit = await client.SendAsync(AsRole(HttpMethod.Put, $"/api/classified-notes/{id}", Note(revision: 1)));
        Assert.Equal(canWrite ? HttpStatusCode.Created : HttpStatusCode.Forbidden, create.StatusCode);
        Assert.Equal(canWrite ? HttpStatusCode.OK : HttpStatusCode.Forbidden, edit.StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Equal(canWrite ? 2 : 1, await db.Set<ClassifiedNoteEntity>().CountAsync());
        Assert.Equal(canWrite ? 2 : 1, (await db.Set<ClassifiedNoteEntity>().SingleAsync(n => n.Id == id)).Revision);
        Assert.Equal(canWrite ? 3 : 1, await db.AuditLogEntries.CountAsync(a => a.EntityType == "ClassifiedNote"));
    }

    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task Notes_serialize_concurrent_edits_and_roll_back_when_audit_fails()
    {
        var failure = new AuditFailure();
        await using var factory = Factory(postgres: true, failure: failure); using var client = factory.CreateClient();
        using var created = await client.SendAsync(Request(HttpMethod.Post, "/api/classified-notes", Note()));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var json = await created.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(); var id = json.GetProperty("id").GetGuid();
        var responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => client.SendAsync(Request(HttpMethod.Put, $"/api/classified-notes/{id}", Note(revision: 1)))));
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Equal(7, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));
        foreach (var response in responses) response.Dispose();
        failure.Enabled = true;
        using var failed = await client.SendAsync(Request(HttpMethod.Put, $"/api/classified-notes/{id}", Note(revision: 2)));
        Assert.Equal(HttpStatusCode.InternalServerError, failed.StatusCode);
        failure.Enabled = false;
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Equal(2, (await db.Set<ClassifiedNoteEntity>().SingleAsync(n => n.Id == id)).Revision);
        Assert.Equal(2, await db.AuditLogEntries.CountAsync(e => e.TenantId == tenant && e.EntityType == "ClassifiedNote"));
        Assert.Single(await db.ContentClassificationHistory.Where(e => e.TenantId == tenant).ToListAsync());
        await db.Set<ClassifiedNoteEntity>().Where(n => n.TenantId == tenant).ExecuteDeleteAsync();
        await db.ContentClassificationHistory.Where(n => n.TenantId == tenant).ExecuteDeleteAsync();
        await db.DataHandlingNoticeAcknowledgements.Where(n => n.TenantId == tenant).ExecuteDeleteAsync();
        await db.AuditLogEntries.Where(n => n.TenantId == tenant).ExecuteDeleteAsync();
        await db.Tenants.Where(n => n.Id == tenant).ExecuteDeleteAsync();
    }
    private sealed class AuditFailure : Microsoft.EntityFrameworkCore.Diagnostics.SaveChangesInterceptor
    {
        public bool Enabled { get; set; }
        public override ValueTask<Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<int>> SavingChangesAsync(
            Microsoft.EntityFrameworkCore.Diagnostics.DbContextEventData eventData, Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (Enabled && eventData.Context!.ChangeTracker.Entries<AuditLogEntryEntity>().Any(e => e.State == EntityState.Added))
                throw new InvalidOperationException("Synthetic audit failure");
            return ValueTask.FromResult(result);
        }
    }

    [Theory]
    [InlineData("Approved", false, false, 403)]
    [InlineData("Superseded", false, false, 403)]
    [InlineData("Rejected", false, false, 403)]
    [InlineData("Approved", true, false, 403)]
    [InlineData("Approved", false, true, 403)]
    public async Task Incomplete_or_stale_persisted_approval_cannot_authorize_notes(string state, bool expired, bool foreignTenant, int status)
    {
        await using var factory = Factory(mode: TenantDataPosture.CuiReady); using var client = factory.CreateClient();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>(); var checklistId = Guid.NewGuid();
            db.CuiReadyApprovalChecklists.Add(new() { Id = checklistId, TenantId = foreignTenant ? Guid.NewGuid() : tenant,
                State = Enum.Parse<CuiReadyChecklistState>(state), ReviewedByUserId = user,
                ReviewedAt = expired ? DateTimeOffset.UtcNow.AddYears(-2) : DateTimeOffset.UtcNow,
                Items = [new() { Id = Guid.NewGuid(), ItemKey = "security-review", IsRequired = true,
                    Status = CuiReadyChecklistItemStatus.Complete, ReviewerUserId = user, ReviewedAt = DateOnly.FromDateTime(DateTime.UtcNow) }] });
            db.TenantDataHandlingModeHistory.Add(new() { Id = Guid.NewGuid(), TenantId = tenant,
                NewMode = TenantDataPosture.CuiReady, ActorUserId = user, ChangedAt = DateTimeOffset.UtcNow, ApprovalRecordReference = checklistId.ToString() });
            await db.SaveChangesAsync();
        }
        using var response = await client.SendAsync(Request(HttpMethod.Post, "/api/classified-notes", new {
            title = "Synthetic approval test", body = "Synthetic fixture, not customer CUI", classification = new { classification = "Cui" }, approvalChecksPassed = false }));
        Assert.Equal(status, (int)response.StatusCode);
    }

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(false, false, false)]
    [InlineData(true, true, false)]
    public async Task Synthetic_approval_is_read_from_tenant_owned_seed_records(bool approved, bool foreignTenant, bool allowed)
    {
        await using var factory = Factory(mode: TenantDataPosture.DemoSandbox); using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var id = Guid.NewGuid();
        db.EvidenceItems.Add(new() { Id = id, TenantId = foreignTenant ? Guid.NewGuid() : tenant,
            Name = "Synthetic provenance fixture", Classification = ContentClassification.SyntheticCui,
            ClassificationSource = ContentClassificationSource.ImportedDemoSeed, ClassificationIsApprovedDemoContent = approved });
        await db.SaveChangesAsync();
        // Exercise the repository using an explicit trusted server context, not request flags.
        var repository = new EfSyntheticContentApprovalRepository(db, new SyntheticContext(tenant, user));
        Assert.Equal(allowed, await repository.IsApprovedAsync("EvidenceItem", id.ToString(), CancellationToken.None));
        Assert.False(await repository.IsApprovedAsync("EvidenceItem", Guid.NewGuid().ToString(), CancellationToken.None));
    }

    private sealed record SyntheticContext(Guid TenantId, Guid UserId) : ICurrentTenantContext
    {
        public string UserEmail => "synthetic@example.invalid";
    }

    [Theory]
    [InlineData(true)] [InlineData(false)]
    public async Task Caller_approval_boolean_cannot_authorize_Cui(bool assertedApproval)
    {
        await using var factory = Factory(mode: TenantDataPosture.CuiReady); using var client = factory.CreateClient();
        using var response = await client.SendAsync(Request(HttpMethod.Post, "/api/classified-notes", new {
            title = "Synthetic", body = "Synthetic classification fixture", classification = new { classification = "Cui" }, approvalChecksPassed = assertedApproval }));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        using var scope = factory.Services.CreateScope(); Assert.Empty(scope.ServiceProvider.GetRequiredService<GccsDbContext>().Set<ClassifiedNoteEntity>());
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Tenancy;
using Gccs.Domain.Audit;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Gccs.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class CuiReadinessGateTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    private readonly Guid tenant = Guid.NewGuid(), actor = Guid.NewGuid(), other = Guid.NewGuid();
    private WebApplicationFactory<Program> Factory(bool postgres = false, AuditFailure? failure = null) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:GccsDatabase", "");
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ExtractionProcessing:Enabled", "false");
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(o =>
                {
                    if (postgres) o.UseGccsPostgres(Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION")!);
                    else o.UseInMemoryDatabase(tenant.ToString());
                    if (failure is not null) o.AddInterceptors(failure);
                });
                services.AddScoped<IAuditEventWriter, EfAuditEventWriter>();
                services.AddScoped<ITenantRepository, EfTenantRepository>();
                services.AddScoped<ICuiReadyApprovalChecklistRepository, EfCuiReadyApprovalChecklistRepository>();
                services.AddScoped<ICuiReadinessEvidenceRepository, EfCuiReadinessEvidenceRepository>();
                services.AddScoped<ICuiReadyApprovalChecklistGate>(s => s.GetRequiredService<CuiReadyApprovalChecklistService>());
                services.AddScoped<IDataHandlingNoticeAcknowledgementRepository, EfDataHandlingNoticeAcknowledgementRepository>();
                using var provider = services.BuildServiceProvider(); using var scope = provider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
                if (postgres) PostgresTestDatabase.Migrate(db);
                db.Tenants.AddRange(new TenantEntity { Id = tenant, Name = "Synthetic gate tenant", Status = TenantStatus.Active, DataPosture = TenantDataPosture.NoCui },
                    new TenantEntity { Id = other, Name = "Synthetic other tenant", Status = TenantStatus.Active, DataPosture = TenantDataPosture.NoCui });
                CuiReadinessTestData.Seed(db, tenant, actor); CuiReadinessTestData.Seed(db, other, actor); db.SaveChanges();
            });
        });
    private HttpClient Client(WebApplicationFactory<Program> factory, bool platform = true, string permission = "ManageTenant")
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Gccs-Dev-Auth", "true");
        client.DefaultRequestHeaders.Add("X-Gccs-Dev-Tenant", tenant.ToString());
        client.DefaultRequestHeaders.Add("X-Gccs-Dev-User", actor.ToString());
        client.DefaultRequestHeaders.Add("X-Gccs-Dev-Permissions", permission);
        if (platform) client.DefaultRequestHeaders.Add("X-Gccs-Dev-Platform-Permissions", "ApproveCuiReadiness");
        return client;
    }
    private string Path(Guid id, string action) => $"/api/tenants/{tenant}/cui-ready-checklists/{id}/{action}";
    private async Task<CuiReadyApprovalChecklistDto> Checklist(HttpClient client, bool approve = true)
    {
        var created = await client.PostAsJsonAsync($"/api/tenants/{tenant}/cui-ready-checklists", new { });
        created.EnsureSuccessStatusCode();
        var checklist = (await created.Content.ReadFromJsonAsync<CuiReadyApprovalChecklistDto>(Json))!;
        var sources = (await client.GetFromJsonAsync<CuiReadinessSupportingRecord[]>("/api/cui-readiness-evidence/sources"))!;
        foreach (var item in checklist.Items)
        {
            var source = sources.SingleOrDefault(s => s.Kind == item.ItemKey);
            var result = await client.PutAsJsonAsync(Path(checklist.Id, $"items/{item.ItemKey}"), new UpdateCuiReadyChecklistItemRequest(
                CuiReadyChecklistItemStatus.Complete, "Synthetic owner", "synthetic:test", actor, DateOnly.FromDateTime(DateTime.UtcNow),
                "Synthetic review", source?.Id, source?.Version));
            result.EnsureSuccessStatusCode();
        }
        (await client.PostAsJsonAsync(Path(checklist.Id, "submit"), new { })).EnsureSuccessStatusCode();
        if (approve)
        {
            var result = await client.PostAsJsonAsync(Path(checklist.Id, "approve"), new ReviewCuiReadyChecklistRequest("Synthetic final review"));
            result.EnsureSuccessStatusCode();
            checklist = (await result.Content.ReadFromJsonAsync<CuiReadyApprovalChecklistDto>(Json))!;
        }
        return checklist;
    }
    private Task<HttpResponseMessage> Enable(HttpClient client, string reference) => client.PatchAsJsonAsync(
        $"/api/tenants/{tenant}/data-handling-mode", new UpdateTenantDataHandlingModeRequest(TenantDataPosture.CuiReady, "Synthetic gate evaluation", reference));

    [Fact]
    public async Task TC_1A_4_2_1_Tenant_admin_cannot_approve_without_platform_permission()
    {
        await using var factory = Factory(); using var admin = Client(factory, false); using var platform = Client(factory);
        var checklist = await Checklist(platform, false);
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.PostAsJsonAsync(Path(checklist.Id, "approve"), new ReviewCuiReadyChecklistRequest("Attempt"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Enable(admin, checklist.Id.ToString())).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await platform.PostAsJsonAsync(Path(checklist.Id, "approve"), new ReviewCuiReadyChecklistRequest("Qualified review"))).StatusCode);
        using var denied = Client(factory, true, "ViewEvidence");
        Assert.Equal(HttpStatusCode.Forbidden, (await denied.GetAsync("/api/cui-readiness-evidence")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await denied.PostAsJsonAsync("/api/cui-readiness-evidence", Request("support-escalation"))).StatusCode);
    }

    [Fact]
    public async Task Invalid_checklist_state_returns_validation_problem_instead_of_server_error()
    {
        await using var factory = Factory(); using var client = Client(factory);
        var checklist = await Checklist(client, false);

        var response = await client.PostAsJsonAsync(Path(checklist.Id, "submit"), new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task TC_1A_4_2_3_4_Current_links_allow_approval_and_mode_history()
    {
        await using var factory = Factory(); using var client = Client(factory);
        var checklist = await Checklist(client);
        Assert.Equal(actor, checklist.ReviewedByUserId); Assert.NotNull(checklist.ReviewedAt);
        Assert.Equal("Synthetic final review", checklist.ReviewNotes); Assert.True(checklist.Version > 1);
        (await Enable(client, checklist.Id.ToString())).EnsureSuccessStatusCode();
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Contains(await db.TenantDataHandlingModeHistory.ToArrayAsync(), h => h.TenantId == tenant && h.ApprovalRecordReference == checklist.Id.ToString());
        Assert.Equal(TenantDataPosture.NoCui, (await db.Tenants.SingleAsync(t => t.Id == other)).DataPosture);
    }

    [Theory]
    [InlineData("security-review", "Expired")]
    [InlineData("incident-response", "Expired")]
    [InlineData("backup-restore", "Expired")]
    [InlineData("support-escalation", "Expired")]
    [InlineData("security-review", "Superseded")]
    [InlineData("backup-restore", "Rejected")]
    public async Task TC_1A_4_2_2_5_Stale_support_blocks_reuse_and_is_audited(string kind, string state)
    {
        await using var factory = Factory(); using var client = Client(factory); var checklist = await Checklist(client);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var record = await db.Set<CuiReadinessEvidenceEntity>().SingleAsync(e => e.TenantId == tenant && e.Kind == kind);
        if (state == "Expired") record.ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(-1); else record.State = state;
        await db.SaveChangesAsync();
        var response = await Enable(client, checklist.Id.ToString());
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(kind, await response.Content.ReadAsStringAsync());
        db.ChangeTracker.Clear();
        Assert.Equal(TenantDataPosture.NoCui, (await db.Tenants.SingleAsync(t => t.Id == tenant)).DataPosture);
        Assert.True(await db.AuditLogEntries.AnyAsync(e => e.TenantId == tenant && e.EntityType == "CuiReadyApprovalChecklist" && e.Action == AuditAction.Rejected));
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("missing")]
    [InlineData("cross-tenant")]
    [InlineData("rejected")]
    [InlineData("superseded")]
    [InlineData("expired")]
    public async Task TC_1A_4_2_5_Each_failed_reference_has_a_tenant_safe_audit(string scenario)
    {
        await using var factory = Factory(); using var client = Client(factory); var checklist = await Checklist(client);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var entity = await db.CuiReadyApprovalChecklists.SingleAsync(e => e.Id == checklist.Id);
        var reference = checklist.Id.ToString();
        if (scenario == "invalid") reference = "not-an-id";
        if (scenario == "missing") reference = Guid.NewGuid().ToString();
        if (scenario == "cross-tenant") entity.TenantId = other;
        if (scenario == "expired") entity.ReviewedAt = DateTimeOffset.UtcNow.AddYears(-2);
        if (scenario == "rejected") entity.State = CuiReadyChecklistState.Rejected;
        if (scenario == "superseded") entity.State = CuiReadyChecklistState.Superseded;
        await db.SaveChangesAsync();
        var before = await db.AuditLogEntries.CountAsync(e => e.Action == AuditAction.Rejected && e.EntityType == "CuiReadyApprovalChecklist");
        Assert.Equal(HttpStatusCode.BadRequest, (await Enable(client, reference)).StatusCode);
        Assert.Equal(before + 1, await db.AuditLogEntries.CountAsync(e => e.Action == AuditAction.Rejected && e.EntityType == "CuiReadyApprovalChecklist"));
        Assert.False(await db.AuditLogEntries.AnyAsync(e => e.TenantId == other));
    }

    [Fact]
    public async Task Editing_approval_invalidates_it_and_missing_links_cannot_be_overridden_by_complete()
    {
        await using var factory = Factory(); using var client = Client(factory); var checklist = await Checklist(client);
        (await client.PutAsJsonAsync(Path(checklist.Id, "items/security-review"), new UpdateCuiReadyChecklistItemRequest(
            CuiReadyChecklistItemStatus.Complete, "Owner", "synthetic:manual", actor, DateOnly.FromDateTime(DateTime.UtcNow), "Manual complete"))).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.BadRequest, (await Enable(client, checklist.Id.ToString())).StatusCode);
        (await client.PostAsJsonAsync(Path(checklist.Id, "submit"), new { })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(Path(checklist.Id, "approve"), new ReviewCuiReadyChecklistRequest("Attempt"))).StatusCode);
    }

    private RecordCuiReadinessEvidenceRequest Request(string kind) => new(kind, 1, DateTimeOffset.UtcNow.AddMonths(2),
        "synthetic:source", "Synthetic reviewed evidence", CuiReadinessTestData.Details(kind, actor));

    [Theory]
    [InlineData("security-review")]
    [InlineData("incident-response")]
    [InlineData("backup-restore")]
    [InlineData("support-escalation")]
    [InlineData("data-handling-notice")]
    [InlineData("shared-responsibility-matrix")]
    public async Task Cross_tenant_supporting_links_are_rejected_without_disclosing_source_details(string kind)
    {
        await using var factory = Factory(); using var client = Client(factory); var checklist = await Checklist(client, false);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var foreign = (await CuiReadinessTestData.Repository(db, other, actor).SourcesAsync(other, default)).First(s => s.Kind == kind);
        var item = await db.CuiReadyApprovalChecklistItems.SingleAsync(i => i.ChecklistId == checklist.Id && i.ItemKey == kind);
        item.SupportingRecordId = foreign.Id; item.SupportingVersion = foreign.Version; await db.SaveChangesAsync();
        var response = await client.PostAsJsonAsync(Path(checklist.Id, "approve"), new ReviewCuiReadyChecklistRequest("Review"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.DoesNotContain(foreign.Id.ToString(), await response.Content.ReadAsStringAsync());
        Assert.False(await db.AuditLogEntries.AnyAsync(a => a.TenantId == other));
    }

    [Theory]
    [InlineData("data-handling-notice")]
    [InlineData("shared-responsibility-matrix")]
    public async Task Superseded_published_document_versions_do_not_satisfy_gate(string kind)
    {
        await using var factory = Factory(); using var client = Client(factory); var checklist = await Checklist(client, false);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var item = await db.CuiReadyApprovalChecklistItems.SingleAsync(i => i.ChecklistId == checklist.Id && i.ItemKey == kind);
        item.SupportingVersion = "superseded-version";
        if (kind == "data-handling-notice") (await db.DataHandlingNoticeAcknowledgements.SingleAsync(a => a.Id == item.SupportingRecordId)).NoticeVersion = item.SupportingVersion;
        else (await db.SharedResponsibilityMatrixAcknowledgements.SingleAsync(a => a.Id == item.SupportingRecordId)).MatrixVersion = item.SupportingVersion;
        await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(Path(checklist.Id, "approve"), new ReviewCuiReadyChecklistRequest("Review"))).StatusCode);
    }

    [Fact]
    public async Task Missing_cross_tenant_approval_attempts_are_404_with_rejection_audits()
    {
        await using var factory = Factory(); using var client = Client(factory);
        var checklist = await Checklist(client, false);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        (await db.CuiReadyApprovalChecklists.SingleAsync(c => c.Id == checklist.Id)).TenantId = other; await db.SaveChangesAsync();
        foreach (var id in new[] { Guid.NewGuid(), checklist.Id })
            Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync(Path(id, "approve"), new ReviewCuiReadyChecklistRequest("Review"))).StatusCode);
        Assert.Equal(2, await db.AuditLogEntries.CountAsync(a => a.TenantId == tenant && a.EntityType == "CuiReadyApprovalChecklist" && a.Action == AuditAction.Rejected));
    }

    [Fact]
    public async Task Readiness_notice_requires_current_explicit_consent_and_does_not_change_mode()
    {
        await using var factory = Factory(); using var client = Client(factory, false);
        var notice = (await client.GetFromJsonAsync<DataHandlingNoticeDto>("/api/cui-readiness-evidence/notice", Json))!;
        var request = new AcknowledgeDataHandlingNoticeRequest(TenantDataPosture.CuiReady, "Onboarding", notice.NoticeId, notice.Version, false);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/cui-readiness-evidence/notice-acknowledgement", request)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/cui-readiness-evidence/notice-acknowledgement", request with { Acknowledged = true, NoticeVersion = "old" })).StatusCode);
        (await client.PostAsJsonAsync("/api/cui-readiness-evidence/notice-acknowledgement", request with { Acknowledged = true })).EnsureSuccessStatusCode();
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Equal(TenantDataPosture.NoCui, (await db.Tenants.SingleAsync(t => t.Id == tenant)).DataPosture);
    }

    [Fact]
    public async Task Expired_risk_acceptance_and_open_critical_gaps_are_rejected_without_source_changes()
    {
        await using var factory = Factory(); using var client = Client(factory);
        var security = Request("security-review");
        security = security with { Details = security.Details with { AcceptedRisks = [new(actor, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)), "Synthetic scope", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)), null, "Synthetic mitigation")] } };
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/cui-readiness-evidence", security)).StatusCode);
        var incident = Request("incident-response");
        incident = incident with { Details = incident.Details with { IncidentGaps = [new("Synthetic", SecurityReviewFindingSeverity.Critical, IncidentResponseGapStatus.Open, "Synthetic gap")] } };
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/cui-readiness-evidence", incident)).StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.All(await db.Set<CuiReadinessEvidenceEntity>().Where(e => e.TenantId == tenant).ToArrayAsync(), e => { Assert.Equal(1, e.Version); Assert.Equal("Approved", e.State); });
    }

    [PostgresFact, Trait("Category", "PostgresIntegration")]
    public async Task PostgreSQL_concurrent_replacements_have_one_winner_and_invalidate_old_approval()
    {
        await using var factory = Factory(true); using var client = Client(factory); var checklist = await Checklist(client);
        var results = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => client.PostAsJsonAsync("/api/cui-readiness-evidence", Request("support-escalation"))));
        Assert.Single(results, r => r.StatusCode == HttpStatusCode.OK); Assert.Single(results, r => r.StatusCode == HttpStatusCode.BadRequest);
        Assert.Equal(HttpStatusCode.BadRequest, (await Enable(client, checklist.Id.ToString())).StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var records = await db.Set<CuiReadinessEvidenceEntity>().Where(e => e.TenantId == tenant && e.Kind == "support-escalation").OrderBy(e => e.Version).ToArrayAsync();
        Assert.Equal(new[] { 1, 2 }, records.Select(e => e.Version)); Assert.Equal("Superseded", records[0].State);
        Assert.True(await db.AuditLogEntries.AnyAsync(e => e.TenantId == tenant && e.EntityType == "CuiReadyApprovalChecklist" && e.Action == AuditAction.Rejected));
    }

    [PostgresFact, Trait("Category", "PostgresIntegration")]
    public async Task PostgreSQL_audit_failure_rolls_back_source_supersession()
    {
        var failure = new AuditFailure(); await using var factory = Factory(true, failure); using var client = Client(factory);
        // Initialize before fault injection so fixture setup remains independent.
        (await client.GetAsync("/api/cui-readiness-evidence")).EnsureSuccessStatusCode(); failure.Enabled = true;
        Assert.Equal(HttpStatusCode.InternalServerError, (await client.PostAsJsonAsync("/api/cui-readiness-evidence", Request("support-escalation"))).StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var record = Assert.Single(await db.Set<CuiReadinessEvidenceEntity>().Where(e => e.TenantId == tenant && e.Kind == "support-escalation").ToArrayAsync());
        Assert.Equal(1, record.Version); Assert.Equal("Approved", record.State);
    }
    private sealed class AuditFailure : SaveChangesInterceptor
    {
        public bool Enabled;
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
        {
            if (Enabled && eventData.Context!.ChangeTracker.Entries<AuditLogEntryEntity>().Any(e => e.State == EntityState.Added))
                throw new InvalidOperationException("Synthetic audit failure");
            return base.SavingChangesAsync(eventData, result, ct);
        }
    }
}

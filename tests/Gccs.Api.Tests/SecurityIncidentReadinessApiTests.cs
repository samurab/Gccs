using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Tenancy;
using Gccs.Application.Notifications;
using Gccs.Domain.Audit;
using Gccs.Domain.Tenancy;
using Gccs.Domain.Compliance;
using Gccs.Domain.Evidence;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Gccs.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class SecurityIncidentReadinessApiTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    private readonly Guid tenantA = Guid.NewGuid(), tenantB = Guid.NewGuid(), userA = Guid.NewGuid(), userB = Guid.NewGuid();
    private WebApplicationFactory<Program> Factory() => new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
    {
        b.UseSetting("ConnectionStrings:GccsDatabase", ""); b.UseSetting("LocalDependencies:Enabled", "false"); b.UseSetting("ExtractionProcessing:Enabled", "false");
        b.ConfigureServices(s =>
        {
            s.AddDbContext<GccsDbContext>(o => o.UseInMemoryDatabase($"readiness-{tenantA}")); s.AddScoped<IAuditEventWriter, EfAuditEventWriter>(); s.AddScoped<ISecurityIncidentReadinessRepository, EfSecurityIncidentReadinessRepository>(); s.AddScoped<SecurityIncidentReadinessService>(); s.AddScoped<ICuiReadinessEvidenceRepository, EfCuiReadinessEvidenceRepository>(); s.AddScoped<CuiReadinessEvidenceService>();
            using var p = s.BuildServiceProvider(); using var scope = p.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>(); db.Tenants.AddRange(new TenantEntity { Id = tenantA, Name = "Tenant A", Status = TenantStatus.Active, DataPosture = TenantDataPosture.NoCui }, new TenantEntity { Id = tenantB, Name = "Tenant B", Status = TenantStatus.Active, DataPosture = TenantDataPosture.NoCui }); db.SaveChanges();
        });
    });
    private HttpClient Client(WebApplicationFactory<Program> f, Guid tenant, Guid user, bool manage = true, bool approve = true) { var c = f.CreateClient(); c.DefaultRequestHeaders.Add("X-Gccs-Dev-Auth", "true"); c.DefaultRequestHeaders.Add("X-Gccs-Dev-Tenant", tenant.ToString()); c.DefaultRequestHeaders.Add("X-Gccs-Dev-User", user.ToString()); c.DefaultRequestHeaders.Add("X-Gccs-Dev-Permissions", manage ? "ManageTenant" : "ViewEvidence"); if (approve) c.DefaultRequestHeaders.Add("X-Gccs-Dev-Platform-Permissions", "ApproveCuiReadiness"); return c; }
    private SaveSecurityReviewRequest Security(int version = 0, SecurityReviewFindingSeverity? severity = null) => new(version,
        SecurityReviewChecklist.RequiredAreas.Select(a => new SecurityReviewChecklistItemDto(a, SecurityReviewItemStatus.Passed, userA, DateOnly.FromDateTime(DateTime.UtcNow), $"artifact:{a}", "Executed review")).ToArray(),
        severity is null ? [] : [new SaveSecurityReviewFindingRequest(null, "logging", severity.Value, SecurityReviewFindingStatus.Open, "Synthetic finding", "Security", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)), null)], []);

    [Fact]
    public async Task Real_endpoints_persist_tenant_scoped_security_records_history_and_audit()
    {
        await using var f = Factory(); using var a = Client(f, tenantA, userA); using var b = Client(f, tenantB, userB);
        var saved = await a.PutAsJsonAsync("/api/security-incident-readiness/security-review", Security(), Json); saved.EnsureSuccessStatusCode();
        var record = (await saved.Content.ReadFromJsonAsync<SecurityReviewRecordDto>(Json))!;
        (await a.PostAsJsonAsync("/api/security-incident-readiness/security-review/approve", new ApproveReadinessRecordRequest(record.Version, "Qualified synthetic review"), Json)).EnsureSuccessStatusCode();
        var tenantBResponse = await b.GetAsync("/api/security-incident-readiness/security-review");
        Assert.Equal(HttpStatusCode.OK, tenantBResponse.StatusCode);
        Assert.Equal("null", await tenantBResponse.Content.ReadAsStringAsync());
        using var scope = f.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Equal(13, await db.SecurityReviewChecklistItems.CountAsync(x => x.TenantId == tenantA)); Assert.False(await db.SecurityReviewRecords.AnyAsync(x => x.TenantId == tenantB));
        Assert.True(await db.ReadinessHistory.AnyAsync(x => x.TenantId == tenantA && x.Action == "approved")); Assert.True(await db.ReadinessApprovals.AnyAsync(x => x.TenantId == tenantA)); Assert.True(await db.AuditLogEntries.AnyAsync(x => x.TenantId == tenantA && x.Action == AuditAction.Approved));
    }

    [Fact]
    public async Task Open_high_or_critical_finding_blocks_approval_and_source_generation()
    {
        await using var f = Factory(); using var c = Client(f, tenantA, userA); var saved = await c.PutAsJsonAsync("/api/security-incident-readiness/security-review", Security(0, SecurityReviewFindingSeverity.Critical), Json); saved.EnsureSuccessStatusCode(); var r = (await saved.Content.ReadFromJsonAsync<SecurityReviewRecordDto>(Json))!;
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/security-incident-readiness/security-review/approve", new ApproveReadinessRecordRequest(r.Version, "Attempt"), Json)).StatusCode);
        using var scope = f.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>(); Assert.Equal("Draft", (await db.SecurityReviewRecords.SingleAsync()).State); Assert.Empty(await db.ReadinessApprovals.ToArrayAsync());
    }

    [Fact]
    public async Task Typed_internal_evidence_is_tenant_validated_and_rechecked_on_approval()
    {
        await using var f = Factory(); using var client = Client(f, tenantA, userA); var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var ownVersion = Guid.NewGuid(); var foreignVersion = Guid.NewGuid();
        using (var scope = f.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
            AddEvidence(db, tenantA, ownVersion, "Own executed evidence"); AddEvidence(db, tenantB, foreignVersion, "Foreign executed evidence"); await db.SaveChangesAsync();
        }
        SaveTechnicalReadinessRequest Request(Guid version) => new(0, [new(null, "backup-restore", today, "staging", userA, "Passed", "Approved evidence file", today.AddMonths(3), "Verified", "EvidenceFileVersion", version)]);
        var foreign = await client.PutAsJsonAsync("/api/security-incident-readiness/technical", Request(foreignVersion), Json);
        Assert.Equal(HttpStatusCode.BadRequest, foreign.StatusCode);
        var saved = await client.PutAsJsonAsync("/api/security-incident-readiness/technical", Request(ownVersion), Json); saved.EnsureSuccessStatusCode();
        var record = (await saved.Content.ReadFromJsonAsync<TechnicalReadinessRecordDto>(Json))!;
        var options = (await client.GetFromJsonAsync<ReadinessEvidenceOptionDto[]>("/api/security-incident-readiness/evidence-options", Json))!;
        Assert.Contains(options, x => x.EvidenceFileVersionId == ownVersion); Assert.DoesNotContain(options, x => x.EvidenceFileVersionId == foreignVersion);
        using (var scope = f.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>(); (await db.EvidenceFileVersions.SingleAsync(x => x.Id == ownVersion)).DeletedAt = DateTimeOffset.UtcNow; await db.SaveChangesAsync(); }
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/security-incident-readiness/technical/approve", new ApproveReadinessRecordRequest(record.Version, "Attempt"), Json)).StatusCode);
        using var verify = f.Services.CreateScope(); var verifyDb = verify.ServiceProvider.GetRequiredService<GccsDbContext>(); Assert.Empty(await verifyDb.ReadinessApprovals.ToArrayAsync());
    }

    [Fact]
    public async Task Automated_due_date_sweep_is_idempotent()
    {
        await using var f = Factory();
        using (var scope = f.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>(); db.ComplianceTasks.Add(new ComplianceTaskEntity { Id=Guid.NewGuid(), TenantId=tenantA, Title="Incident readiness review", Description="Review", Type=ComplianceTaskType.PolicyReview, Status=ComplianceTaskStatus.Open, RiskLevel=RiskLevel.High, AssignedToUserId=userA, OwnerFunction="Security", DueAt=DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)), CreatedAt=DateTimeOffset.UtcNow, CreatedByUserId=userA }); await db.SaveChangesAsync();
            var repository = new Gccs.Infrastructure.Notifications.EfDueDateReminderRepository(db);
            Assert.Equal(1, await repository.RunAutomatedAsync(14, 200)); Assert.Equal(0, await repository.RunAutomatedAsync(14, 200));
            (await db.ComplianceTasks.SingleAsync(x => x.TenantId == tenantA)).AssignedToUserId = userB; await db.SaveChangesAsync();
            Assert.Equal(1, await repository.RunAutomatedAsync(14, 200));
            Assert.Equal(2, await db.NotificationDeliveries.CountAsync(x => x.TenantId == tenantA));
        }
    }

    [Fact]
    public async Task Restricted_roles_and_missing_platform_approval_are_rejected_without_mutation()
    {
        await using var f = Factory(); using var denied = Client(f, tenantA, userA, false); Assert.Equal(HttpStatusCode.Forbidden, (await denied.PutAsJsonAsync("/api/security-incident-readiness/security-review", Security(), Json)).StatusCode); Assert.Equal(HttpStatusCode.Forbidden, (await denied.GetAsync("/api/security-incident-readiness/evidence-options")).StatusCode);
        using var admin = Client(f, tenantA, userA, true, false); var saved = await admin.PutAsJsonAsync("/api/security-incident-readiness/security-review", Security(), Json); saved.EnsureSuccessStatusCode(); var r = (await saved.Content.ReadFromJsonAsync<SecurityReviewRecordDto>(Json))!;
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.PostAsJsonAsync("/api/security-incident-readiness/security-review/approve", new ApproveReadinessRecordRequest(r.Version, "Attempt"), Json)).StatusCode);
        using var scope = f.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>(); Assert.Empty(await db.ReadinessApprovals.ToArrayAsync());
    }

    [Fact]
    public async Task Accepted_risk_and_finding_closure_keep_metadata_and_specific_audit_events()
    {
        await using var f = Factory(); using var c = Client(f, tenantA, userA); var today = DateOnly.FromDateTime(DateTime.UtcNow); var findingId = Guid.NewGuid(); var riskId = Guid.NewGuid();
        var items = Security().Items;
        var first = new SaveSecurityReviewRequest(0, items, [new(findingId, "monitoring", SecurityReviewFindingSeverity.Medium, SecurityReviewFindingStatus.AcceptedRisk, "Synthetic monitored risk", "Security", today.AddDays(30), null)], [new(riskId, findingId, userA, today, "Synthetic monitoring exception", null, today.AddMonths(3), "Daily review until remediation")]);
        var created = await c.PutAsJsonAsync("/api/security-incident-readiness/security-review", first, Json); created.EnsureSuccessStatusCode();
        var second = first with { ExpectedVersion = 1, Findings = [first.Findings[0] with { Status = SecurityReviewFindingStatus.Closed, ClosureNotes = "Verified remediation" }] };
        (await c.PutAsJsonAsync("/api/security-incident-readiness/security-review", second, Json)).EnsureSuccessStatusCode();
        using var scope = f.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Equal("Daily review until remediation", (await db.AcceptedSecurityRisks.OrderByDescending(x => x.ReviewId).FirstAsync()).MitigationNote);
        Assert.True(await db.AuditLogEntries.AnyAsync(x => x.TenantId == tenantA && x.EntityType == "AcceptedSecurityRisk"));
        Assert.True(await db.AuditLogEntries.AnyAsync(x => x.TenantId == tenantA && x.EntityType == "SecurityReviewFinding" && x.Summary.Contains("closed")));
    }

    [Fact]
    public async Task Technical_and_incident_evidence_are_relational_records_not_dto_snapshots()
    {
        await using var f = Factory(); using var c = Client(f, tenantA, userA); var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var controls = new[] { "tenant-isolation", "evidence-storage", "malware-scanner", "backup-restore", "administrator-access", "support-access" }.Select(x => new SaveExecutedControlEvidenceRequest(null, x, today, "staging", userA, "Passed", $"https://evidence.example.test/{x}", today.AddMonths(6), "Executed synthetic verification", "ExternalArtifact", null, $"https://evidence.example.test/{x}", new string('a', 64))).ToArray();
        var technical = await c.PutAsJsonAsync("/api/security-incident-readiness/technical", new SaveTechnicalReadinessRequest(0, controls), Json); technical.EnsureSuccessStatusCode(); var tr = (await technical.Content.ReadFromJsonAsync<TechnicalReadinessRecordDto>(Json))!; (await c.PostAsJsonAsync("/api/security-incident-readiness/technical/approve", new ApproveReadinessRecordRequest(tr.Version, "Reviewed"), Json)).EnsureSuccessStatusCode();
        var tablet = Guid.NewGuid(); var playbooks = IncidentResponseReadiness.RequiredPlaybooks.Select(k => new SaveIncidentPlaybookRequest(null, k, "Synthetic trigger", ["Contain"], "Security then support", ["Audit record"], "Security", "Evidence retained and follow-ups assigned")).ToArray();
        var contacts = new[] { "security", "support", "legal-compliance", "engineering", "customer-success" }.Select(function => new SaveIncidentContactRequest(null, function, $"{function}@example.test", "Incident owner")).ToArray();
        var incident = await c.PutAsJsonAsync("/api/security-incident-readiness/incident", new SaveIncidentReadinessRequest(0, today.AddMonths(11), "Annual", contacts, playbooks, [new SaveIncidentTabletopRequest(tablet, today, "staging", ["Security", "Support"], ["Synthetic gap review"], "https://evidence.example.test/tabletop", userA, "ExternalArtifact", null, "https://evidence.example.test/tabletop", new string('b', 64))], [new SaveIncidentFollowUpRequest(null, tablet, SecurityReviewFindingSeverity.Medium, IncidentResponseGapStatus.Closed, "Synthetic follow-up", "Security", today, "Verified closed")]), Json); incident.EnsureSuccessStatusCode(); var ir = (await incident.Content.ReadFromJsonAsync<IncidentReadinessRecordDto>(Json))!; (await c.PostAsJsonAsync("/api/security-incident-readiness/incident/approve", new ApproveReadinessRecordRequest(ir.Version, "Reviewed"), Json)).EnsureSuccessStatusCode();
        using var scope = f.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>(); Assert.Equal(6, await db.ExecutedControlEvidence.CountAsync()); Assert.Equal(5, await db.IncidentContacts.CountAsync()); Assert.Equal(6, await db.IncidentPlaybooks.CountAsync()); Assert.Single(await db.IncidentTabletops.ToArrayAsync()); Assert.Single(await db.IncidentFollowUps.ToArrayAsync()); Assert.Single(await db.ComplianceTasks.Where(x => x.TenantId == tenantA && x.ControlId == "incident-readiness-review" && x.DueAt == today.AddMonths(11)).ToArrayAsync());
    }

    [Fact]
    public async Task Open_critical_incident_gap_blocks_approval_without_creating_approval()
    {
        await using var f = Factory(); using var c = Client(f, tenantA, userA); var today = DateOnly.FromDateTime(DateTime.UtcNow); var tabletopId = Guid.NewGuid();
        var contacts = new[] { "security", "support", "legal-compliance", "engineering", "customer-success" }.Select(function => new SaveIncidentContactRequest(null, function, $"{function}@example.test", "Incident owner")).ToArray();
        var playbooks = IncidentResponseReadiness.RequiredPlaybooks.Select(key => new SaveIncidentPlaybookRequest(null, key, "Synthetic trigger", ["Contain"], "Security then support", ["Audit records"], "Security", "Contained and reviewed")).ToArray();
        var request = new SaveIncidentReadinessRequest(0, today.AddMonths(6), "Release", contacts, playbooks,
            [new(tabletopId, today, "staging", ["Security"], ["Critical response gap"], "https://evidence.example.test/tabletop", userA, "ExternalArtifact", null, "https://evidence.example.test/tabletop", new string('c', 64))],
            [new(null, tabletopId, SecurityReviewFindingSeverity.Critical, IncidentResponseGapStatus.Open, "Critical response gap", "Security", today.AddDays(7), null)]);
        var saved = await c.PutAsJsonAsync("/api/security-incident-readiness/incident", request, Json); saved.EnsureSuccessStatusCode();
        var record = (await saved.Content.ReadFromJsonAsync<IncidentReadinessRecordDto>(Json))!;
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/security-incident-readiness/incident/approve", new ApproveReadinessRecordRequest(record.Version, "Attempt"), Json)).StatusCode);
        using var scope = f.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Equal("Draft", (await db.IncidentReadinessRecords.SingleAsync()).State); Assert.Empty(await db.ReadinessApprovals.ToArrayAsync());
    }

    [Fact]
    public async Task Expired_accepted_risk_removes_approved_security_review_from_gate_sources()
    {
        await using var f = Factory(); using var c = Client(f, tenantA, userA); var today = DateOnly.FromDateTime(DateTime.UtcNow); var findingId = Guid.NewGuid();
        var request = new SaveSecurityReviewRequest(0, Security().Items,
            [new(findingId, "monitoring", SecurityReviewFindingSeverity.Medium, SecurityReviewFindingStatus.AcceptedRisk, "Monitored exception", "Security", today.AddDays(30), null)],
            [new(null, findingId, userA, today, "Temporary monitoring exception", null, today.AddMonths(3), "Daily monitoring")]);
        var saved = await c.PutAsJsonAsync("/api/security-incident-readiness/security-review", request, Json); saved.EnsureSuccessStatusCode(); var record = (await saved.Content.ReadFromJsonAsync<SecurityReviewRecordDto>(Json))!;
        (await c.PostAsJsonAsync("/api/security-incident-readiness/security-review/approve", new ApproveReadinessRecordRequest(record.Version, "Approved temporary exception"), Json)).EnsureSuccessStatusCode();
        using (var scope = f.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>(); (await db.AcceptedSecurityRisks.SingleAsync()).ReviewAt = today.AddDays(-1); await db.SaveChangesAsync(); }
        var sources = (await c.GetFromJsonAsync<CuiReadinessSupportingRecord[]>("/api/cui-readiness-evidence/sources", Json))!;
        Assert.DoesNotContain(sources, x => x.Kind == "security-review");
    }

    private static void AddEvidence(GccsDbContext db, Guid tenantId, Guid versionId, string name)
    {
        var item = new EvidenceItemEntity { Id=Guid.NewGuid(), TenantId=tenantId, Name=name, Description="Synthetic", Type=EvidenceType.SystemConfiguration, OwnerFunction="Security", Status=EvidenceStatus.Approved, CreatedAt=DateTimeOffset.UtcNow };
        item.FileVersions.Add(new EvidenceFileVersionEntity { Id=versionId, EvidenceItemId=item.Id, VersionNumber=1, FileName="evidence.pdf", ContentType="application/pdf", SizeBytes=10, ValidationStatus="accepted", MalwareScanStatus="clean", StorageUri="evidence/object", FileHash=new string('e',64), UploadedAt=DateTimeOffset.UtcNow, UploadedByUserId=Guid.NewGuid() }); db.EvidenceItems.Add(item);
    }
}

public sealed class SecurityIncidentReadinessPostgresTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;
    public SecurityIncidentReadinessPostgresTests(WebApplicationFactory<Program> factory) => this.factory = factory;
    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task Concurrent_automated_reminder_sweeps_create_one_delivery()
    {
        var connection = Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION") ?? throw new InvalidOperationException("GCCS_TEST_POSTGRES_CONNECTION is required.");
        var tenant = Guid.NewGuid(); var user = Guid.NewGuid(); var task = Guid.NewGuid();
        await using var f = factory.WithWebHostBuilder(b => { b.UseSetting("ConnectionStrings:GccsDatabase", connection); b.UseSetting("LocalDependencies:Enabled", "false"); b.UseSetting("DueDateReminderProcessing:Enabled", "false"); });
        using var startup = f.CreateClient();
        using (var setupScope = f.Services.CreateScope()) { var setup = setupScope.ServiceProvider.GetRequiredService<GccsDbContext>(); setup.Tenants.Add(new TenantEntity { Id=tenant, Name="Reminder concurrency tenant", Status=TenantStatus.Active, DataPosture=TenantDataPosture.NoCui }); setup.ComplianceTasks.Add(new ComplianceTaskEntity { Id=task, TenantId=tenant, Title="Incident readiness review", Description="Review", Type=ComplianceTaskType.PolicyReview, Status=ComplianceTaskStatus.Open, RiskLevel=RiskLevel.High, AssignedToUserId=user, OwnerFunction="Security", DueAt=DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)), CreatedAt=DateTimeOffset.UtcNow, CreatedByUserId=user }); await setup.SaveChangesAsync(); }
        try
        {
            using var scopeA = f.Services.CreateScope(); using var scopeB = f.Services.CreateScope(); var dbA = scopeA.ServiceProvider.GetRequiredService<GccsDbContext>(); var dbB = scopeB.ServiceProvider.GetRequiredService<GccsDbContext>();
            var results = await Task.WhenAll(new Gccs.Infrastructure.Notifications.EfDueDateReminderRepository(dbA).RunAutomatedAsync(14, 200), new Gccs.Infrastructure.Notifications.EfDueDateReminderRepository(dbB).RunAutomatedAsync(14, 200));
            Assert.Equal(1, results.Sum());
            using var verifyScope = f.Services.CreateScope(); var verify = verifyScope.ServiceProvider.GetRequiredService<GccsDbContext>(); Assert.Single(await verify.NotificationDeliveries.Where(x => x.TenantId == tenant && x.SourceTaskId == task).ToArrayAsync());
        }
        finally
        {
            using var cleanupScope = f.Services.CreateScope(); var cleanup = cleanupScope.ServiceProvider.GetRequiredService<GccsDbContext>(); await cleanup.NotificationDeliveries.Where(x => x.TenantId == tenant).ExecuteDeleteAsync(); await cleanup.ComplianceTasks.Where(x => x.TenantId == tenant).ExecuteDeleteAsync(); await cleanup.Tenants.Where(x => x.Id == tenant).ExecuteDeleteAsync();
        }
    }
    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task Audit_failure_rolls_back_readiness_record_children_and_history()
    {
        var connection = Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION") ?? throw new InvalidOperationException("GCCS_TEST_POSTGRES_CONNECTION is required."); var tenant = Guid.NewGuid(); var user = Guid.NewGuid();
        await using var f = factory.WithWebHostBuilder(b => { b.UseSetting("ConnectionStrings:GccsDatabase", connection); b.UseSetting("LocalDependencies:Enabled", "false"); b.ConfigureServices(s => { s.RemoveAll<IAuditEventWriter>(); s.AddScoped<IAuditEventWriter, FailingAudit>(); using var p = s.BuildServiceProvider(); using var scope = p.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>(); PostgresTestDatabase.Migrate(db); db.Tenants.Add(new TenantEntity { Id = tenant, Name = "Atomic readiness tenant", Status = TenantStatus.Active, DataPosture = TenantDataPosture.NoCui }); db.SaveChanges(); }); });
        try
        {
            using var client = f.CreateClient(); client.DefaultRequestHeaders.Add("X-Gccs-Dev-Auth", "true"); client.DefaultRequestHeaders.Add("X-Gccs-Dev-Tenant", tenant.ToString()); client.DefaultRequestHeaders.Add("X-Gccs-Dev-User", user.ToString()); client.DefaultRequestHeaders.Add("X-Gccs-Dev-Permissions", "ManageTenant");
            var request = new SaveSecurityReviewRequest(0, SecurityReviewChecklist.RequiredAreas.Select(a => new SecurityReviewChecklistItemDto(a, SecurityReviewItemStatus.NotStarted, null, null, null, null)).ToArray(), [], []);
            Assert.Equal(HttpStatusCode.InternalServerError, (await client.PutAsJsonAsync("/api/security-incident-readiness/security-review", request)).StatusCode);
            using var scope = f.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>(); Assert.False(await db.SecurityReviewRecords.AnyAsync(x => x.TenantId == tenant)); Assert.False(await db.SecurityReviewChecklistItems.AnyAsync(x => x.TenantId == tenant)); Assert.False(await db.ReadinessHistory.AnyAsync(x => x.TenantId == tenant));
        }
        finally { await DeleteTenantAsync(f, tenant); }
    }
    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task Incident_audit_failure_rolls_back_playbooks_followups_and_reminder_task()
    {
        var connection = Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION") ?? throw new InvalidOperationException("GCCS_TEST_POSTGRES_CONNECTION is required."); var tenant = Guid.NewGuid(); var user = Guid.NewGuid();
        await using var f = factory.WithWebHostBuilder(b => { b.UseSetting("ConnectionStrings:GccsDatabase", connection); b.UseSetting("LocalDependencies:Enabled", "false"); b.ConfigureServices(s => { s.RemoveAll<IAuditEventWriter>(); s.AddScoped<IAuditEventWriter, FailingAudit>(); using var p = s.BuildServiceProvider(); using var scope = p.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>(); PostgresTestDatabase.Migrate(db); db.Tenants.Add(new TenantEntity { Id = tenant, Name = "Atomic incident tenant", Status = TenantStatus.Active, DataPosture = TenantDataPosture.NoCui }); db.SaveChanges(); }); });
        try
        {
            using var client = f.CreateClient(); client.DefaultRequestHeaders.Add("X-Gccs-Dev-Auth", "true"); client.DefaultRequestHeaders.Add("X-Gccs-Dev-Tenant", tenant.ToString()); client.DefaultRequestHeaders.Add("X-Gccs-Dev-User", user.ToString()); client.DefaultRequestHeaders.Add("X-Gccs-Dev-Permissions", "ManageTenant");
            var today = DateOnly.FromDateTime(DateTime.UtcNow); var tabletopId = Guid.NewGuid();
            var contacts = new[] { "security", "support", "legal-compliance", "engineering", "customer-success" }.Select(x => new SaveIncidentContactRequest(null, x, $"{x}@example.test", "Incident owner")).ToArray();
            var playbooks = IncidentResponseReadiness.RequiredPlaybooks.Select(x => new SaveIncidentPlaybookRequest(null, x, "Synthetic trigger", ["Contain"], "Security then support", ["Audit record"], "Security", "Closed after verification")).ToArray();
            var request = new SaveIncidentReadinessRequest(0, today.AddMonths(6), "Release", contacts, playbooks, [new(tabletopId, today, "staging", ["Security"], ["Synthetic exercise"], "https://evidence.example.test/tabletop", user, "ExternalArtifact", null, "https://evidence.example.test/tabletop", new string('d', 64))], [new(null, tabletopId, SecurityReviewFindingSeverity.Medium, IncidentResponseGapStatus.Open, "Follow-up", "Security", today.AddDays(14), null)]);
            Assert.Equal(HttpStatusCode.InternalServerError, (await client.PutAsJsonAsync("/api/security-incident-readiness/incident", request)).StatusCode);
            using var scope = f.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>(); Assert.False(await db.IncidentReadinessRecords.AnyAsync(x => x.TenantId == tenant)); Assert.False(await db.IncidentPlaybooks.AnyAsync(x => x.TenantId == tenant)); Assert.False(await db.IncidentFollowUps.AnyAsync(x => x.TenantId == tenant)); Assert.False(await db.ComplianceTasks.AnyAsync(x => x.TenantId == tenant && x.ControlId == "incident-readiness-review")); Assert.False(await db.ReadinessHistory.AnyAsync(x => x.TenantId == tenant));
        }
        finally { await DeleteTenantAsync(f, tenant); }
    }
    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task Concurrent_security_review_replacements_have_one_version_winner()
    {
        var connection = Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION") ?? throw new InvalidOperationException("GCCS_TEST_POSTGRES_CONNECTION is required."); var tenant = Guid.NewGuid(); var user = Guid.NewGuid();
        await using var f = factory.WithWebHostBuilder(b => { b.UseSetting("ConnectionStrings:GccsDatabase", connection); b.UseSetting("LocalDependencies:Enabled", "false"); b.ConfigureServices(s => { using var p = s.BuildServiceProvider(); using var scope = p.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>(); PostgresTestDatabase.Migrate(db); db.Tenants.Add(new TenantEntity { Id = tenant, Name = "Concurrent readiness tenant", Status = TenantStatus.Active, DataPosture = TenantDataPosture.NoCui }); db.SaveChanges(); }); });
        try
        {
            HttpClient Client() { var c = f.CreateClient(); c.DefaultRequestHeaders.Add("X-Gccs-Dev-Auth", "true"); c.DefaultRequestHeaders.Add("X-Gccs-Dev-Tenant", tenant.ToString()); c.DefaultRequestHeaders.Add("X-Gccs-Dev-User", user.ToString()); c.DefaultRequestHeaders.Add("X-Gccs-Dev-Permissions", "ManageTenant"); return c; }
            var payload = new SaveSecurityReviewRequest(0, SecurityReviewChecklist.RequiredAreas.Select(a => new SecurityReviewChecklistItemDto(a, SecurityReviewItemStatus.NotStarted, null, null, null, null)).ToArray(), [], []); using var a = Client(); using var b = Client();
            var responses = await Task.WhenAll(a.PutAsJsonAsync("/api/security-incident-readiness/security-review", payload), b.PutAsJsonAsync("/api/security-incident-readiness/security-review", payload)); Assert.Single(responses, x => x.StatusCode == HttpStatusCode.OK); Assert.Single(responses, x => x.StatusCode == HttpStatusCode.BadRequest);
            using var scope = f.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>(); Assert.Single(await db.SecurityReviewRecords.Where(x => x.TenantId == tenant).ToArrayAsync()); Assert.Single(await db.ReadinessHistory.Where(x => x.TenantId == tenant).ToArrayAsync());
        }
        finally { await DeleteTenantAsync(f, tenant); }
    }
    private static async Task DeleteTenantAsync(WebApplicationFactory<Program> f, Guid tenant) { using var scope = f.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>(); await db.IncidentFollowUps.Where(x => x.TenantId == tenant).ExecuteDeleteAsync(); await db.IncidentTabletops.Where(x => x.TenantId == tenant).ExecuteDeleteAsync(); await db.IncidentPlaybooks.Where(x => x.TenantId == tenant).ExecuteDeleteAsync(); await db.IncidentContacts.Where(x => x.TenantId == tenant).ExecuteDeleteAsync(); await db.IncidentReadinessRecords.Where(x => x.TenantId == tenant).ExecuteDeleteAsync(); await db.ExecutedControlEvidence.Where(x => x.TenantId == tenant).ExecuteDeleteAsync(); await db.TechnicalReadinessRecords.Where(x => x.TenantId == tenant).ExecuteDeleteAsync(); await db.AcceptedSecurityRisks.Where(x => x.TenantId == tenant).ExecuteDeleteAsync(); await db.SecurityReviewChecklistItems.Where(x => x.TenantId == tenant).ExecuteDeleteAsync(); await db.SecurityReviewFindings.Where(x => x.TenantId == tenant).ExecuteDeleteAsync(); await db.SecurityReviewRecords.Where(x => x.TenantId == tenant).ExecuteDeleteAsync(); await db.ComplianceTasks.Where(x => x.TenantId == tenant).ExecuteDeleteAsync(); await db.ReadinessHistory.Where(x => x.TenantId == tenant).ExecuteDeleteAsync(); await db.ReadinessApprovals.Where(x => x.TenantId == tenant).ExecuteDeleteAsync(); await db.AuditLogEntries.Where(x => x.TenantId == tenant).ExecuteDeleteAsync(); var t = await db.Tenants.SingleOrDefaultAsync(x => x.Id == tenant); if (t is not null) { db.Tenants.Remove(t); await db.SaveChangesAsync(); } }
    private sealed class FailingAudit : IAuditEventWriter { public Task WriteAsync(Guid tenantId, Guid actorUserId, AuditAction action, string entityType, string entityId, string summary, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default) => throw new AuditWriteException("Synthetic audit persistence failure."); }
}

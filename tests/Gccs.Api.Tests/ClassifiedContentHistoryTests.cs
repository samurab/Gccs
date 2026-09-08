using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Application.Tenancy;
using Gccs.Domain.Common;
using Gccs.Domain.Contracts;
using Gccs.Domain.Reports;
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

public sealed class ClassifiedContentHistoryTests
{
    private readonly Guid tenant = Guid.NewGuid(), user = Guid.NewGuid(), contract = Guid.NewGuid();
    private readonly Dictionary<string, Guid> ids = Types.ToDictionary(t => t[0], _ => Guid.NewGuid());
    public static IEnumerable<object[]> Cases => Types.Select(t => new object[] { t[0], t[1], t[2], t[3] });
    private static readonly string[][] Types = [
        ["EvidenceItem", "evidence-items", "ViewEvidence", "ApproveEvidence"],
        ["EvidenceFileVersion", "evidence-file-versions", "ViewEvidence", "ApproveEvidence"],
        ["ClassifiedNote", "notes", "ViewEvidence", "ApproveEvidence"],
        ["ContractDocument", "contract-documents", "ViewContracts", "ReviewClauses"],
        ["ExtractionJob", "extraction-jobs", "ViewContracts", "ReviewClauses"],
        ["Report", "reports", "ViewReports", "ManageReports"] ];
    private WebApplicationFactory<Program> Factory(bool postgres = false, AuditFailure? failure = null,
        bool notice = true,
        Gccs.Application.Contracts.IContractDocumentTextExtractor? extractor = null) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:GccsDatabase", ""); builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ExtractionProcessing:Enabled", "false");
            builder.ConfigureServices(services =>
            {
                if (extractor is not null) services.AddSingleton(extractor);
                services.AddDbContext<GccsDbContext>(o => {
                    if (postgres) o.UseGccsPostgres(Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION")!);
                    else o.UseInMemoryDatabase(tenant.ToString());
                    if (failure is not null) o.AddInterceptors(failure);
                });
                services.AddScoped<IAuditEventWriter, EfAuditEventWriter>();
                services.AddScoped<ITenantRepository, EfTenantRepository>();
                services.AddScoped<ICuiSupportEscalationRepository, EfCuiSupportEscalationRepository>();
                services.AddScoped<IDataHandlingNoticeAcknowledgementRepository, EfDataHandlingNoticeAcknowledgementRepository>();
                services.AddScoped<IClassifiedNoteRepository, Gccs.Infrastructure.Common.EfClassifiedNoteRepository>();
                services.AddScoped<Gccs.Application.Evidence.IEvidenceMetadataRepository, Gccs.Infrastructure.Evidence.EfEvidenceMetadataRepository>();
                services.AddScoped<Gccs.Application.NoCui.INoCuiAcknowledgementRepository, Gccs.Infrastructure.NoCui.EfNoCuiAcknowledgementRepository>();
                services.AddScoped<Gccs.Application.Contracts.IContractRepository, Gccs.Infrastructure.Contracts.EfContractRepository>();
                services.AddScoped<Gccs.Application.Reports.IReportRepository, Gccs.Infrastructure.Reports.EfReportRepository>();
                using var provider = services.BuildServiceProvider(); using var scope = provider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
                if (postgres) PostgresTestDatabase.Migrate(db);
                var now = DateTimeOffset.UtcNow;
                db.Tenants.Add(new() { Id = tenant, Name = $"Synthetic history {tenant}", DataPosture = TenantDataPosture.NoCui });
                db.NoCuiAcknowledgements.Add(new() { Id = Guid.NewGuid(), TenantId = tenant, UserId = user,
                    NoticeVersion = Gccs.Application.NoCui.NoCuiNotice.CurrentVersion, NoticeCopy = Gccs.Application.NoCui.NoCuiNotice.Copy,
                    AcknowledgedAt = now, CreatedAt = now, CreatedByUserId = user });
                db.Contracts.Add(new() { Id = contract, TenantId = tenant, ContractNumber = $"H-{contract}", Title = "Synthetic contract" });
                db.EvidenceItems.Add(new() { Id = ids["EvidenceItem"], TenantId = tenant, Name = "Synthetic evidence",
                    Classification = ContentClassification.Unknown, CreatedAt = now });
                db.EvidenceFileVersions.Add(new() { Id = ids["EvidenceFileVersion"], EvidenceItemId = ids["EvidenceItem"],
                    VersionNumber = 1, FileName = "synthetic.txt", ContentType = "text/plain", SizeBytes = 4,
                    StorageUri = "synthetic-classification-test.txt",
                    Classification = ContentClassification.Unknown, UploadedByUserId = user, UploadedAt = now });
                db.Set<ClassifiedNoteEntity>().Add(new() { Id = ids["ClassifiedNote"], TenantId = tenant,
                    Title = "Synthetic note", Body = "Synthetic classified note body", Classification = ContentClassification.Unknown,
                    Revision = 1, CreatedAt = now, UpdatedAt = now });
                db.Set<ContractDocumentEntity>().Add(new() { Id = ids["ContractDocument"], ContractId = contract,
                    Type = ContractDocumentType.Contract, FileName = "synthetic.txt", ContentType = "text/plain", SizeBytes = 4,
                    Classification = ContentClassification.Unknown, UploadedByUserId = user, UploadedAt = now });
                db.Set<ExtractionJobEntity>().Add(new() { Id = ids["ExtractionJob"], TenantId = tenant, SourceDocumentId = ids["ContractDocument"],
                    RequestedByUserId = user, RequestedAt = now, Status = ExtractionJobStatus.Completed,
                    CompletedAt = now, Classification = ContentClassification.Unknown });
                db.Reports.Add(new() { Id = ids["Report"], TenantId = tenant, Title = "Synthetic report", Type = ReportType.ComplianceStatus,
                    Status = ReportStatus.Complete, GeneratedAt = now, GeneratedByUserId = user, Classification = ContentClassification.Unknown,
                    SnapshotJson = "{\"baseline\":\"immutable\"}", ExportHtml = "<p>Immutable synthetic snapshot</p>" });
                if (notice) foreach (var workflow in new[] { "Onboarding", "EvidenceUpload", "ContractUpload", "ClassifiedNote", "ReportGeneration", "ExtractionJob", "Support" })
                    db.DataHandlingNoticeAcknowledgements.Add(new() { Id = Guid.NewGuid(), TenantId = tenant, UserId = user,
                        Mode = TenantDataPosture.NoCui, WorkflowContext = workflow, NoticeId = "no-cui-general",
                        NoticeVersion = "2026.06.phase1a", AcknowledgedAt = now });
                db.SaveChanges();
            });
        });

    [Fact]
    public async Task Support_escalation_requires_current_support_notice_without_mutating_content()
    {
        await using var factory = Factory(notice: false); using var client = factory.CreateClient();
        var path = $"/api/tenants/{tenant}/cui-support-escalations";
        var body = new { sourceWorkflow = "ClassificationReview", affectedEntityType = "EvidenceItem",
            affectedEntityId = ids["EvidenceItem"].ToString(), category = "SuspectedCui", severity = "High",
            description = "Synthetic metadata-only concern." };

        using var response = await client.SendAsync(Request(HttpMethod.Post, path, "ManageTenant", body));

        Assert.Equal((HttpStatusCode)428, response.StatusCode);
        Assert.Contains("data_handling_notice_acknowledgement_required", await response.Content.ReadAsStringAsync());
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Empty(db.CuiSupportEscalations);
        Assert.Empty(db.AuditLogEntries);
    }
    private HttpRequestMessage Request(HttpMethod method, string path, string permission, object? body = null, Guid? other = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add("X-Gccs-Dev-Auth", "true"); request.Headers.Add("X-Gccs-Dev-Tenant", (other ?? tenant).ToString());
        request.Headers.Add("X-Gccs-Dev-User", user.ToString()); request.Headers.Add("X-Gccs-Dev-Permissions", permission);
        if (body is not null) request.Content = JsonContent.Create(body);
        return request;
    }
    private static object Review(long revision, string classification = "Fci", string reason = "Synthetic reviewed classification") =>
        new { expectedRevision = revision, classification = new { classification, reason,
            reviewedByUserId = Guid.Empty, reviewedAt = DateTimeOffset.Parse("2000-01-01T00:00:00Z"), isApprovedDemoContent = true } };

    [Theory, MemberData(nameof(Cases))]
    public async Task Every_type_has_tenant_scoped_review_and_complete_history(string type, string route, string read, string write)
    {
        await using var factory = Factory(); using var client = factory.CreateClient(); var path = $"/api/classified-content/{route}/{ids[type]}";
        using var queue = await client.SendAsync(Request(HttpMethod.Get, $"/api/classified-content/{route}?reviewOnly=true", read));
        Assert.Equal(HttpStatusCode.OK, queue.StatusCode); Assert.Contains(ids[type].ToString(), await queue.Content.ReadAsStringAsync());
        Assert.DoesNotContain("Synthetic classified note body", await queue.Content.ReadAsStringAsync());
        foreach (var suffix in new[] { "", "/history" })
        {
            using var foreign = await client.SendAsync(Request(HttpMethod.Get, path + suffix, read, other: Guid.NewGuid()));
            Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        }
        using var denied = await client.SendAsync(Request(HttpMethod.Patch, path + "/classification", read, Review(0)));
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        using var foreignWrite = await client.SendAsync(Request(HttpMethod.Patch, path + "/classification", write, Review(0), Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.NotFound, foreignWrite.StatusCode);
        using var updated = await client.SendAsync(Request(HttpMethod.Patch, path + "/classification", write, Review(0)));
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var result = await updated.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, result.GetProperty("revision").GetInt64());
        Assert.Equal(user, result.GetProperty("classification").GetProperty("reviewedByUserId").GetGuid());
        Assert.False(result.GetProperty("classification").GetProperty("isApprovedDemoContent").GetBoolean());
        using var sameClass = await client.SendAsync(Request(HttpMethod.Patch, path + "/classification", write, Review(1, reason: "Second review, same label")));
        Assert.Equal(HttpStatusCode.OK, sameClass.StatusCode);
        using var stale = await client.SendAsync(Request(HttpMethod.Patch, path + "/classification", write, Review(0, "Unclassified")));
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        using var history = await client.SendAsync(Request(HttpMethod.Get, path + "/history", read));
        var entries = (await history.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().ToArray();
        Assert.Equal(2, entries.Length);
        Assert.All(entries, e => Assert.Equal(user, e.GetProperty("changedByUserId").GetGuid()));
        var latest = entries.Single(e => e.GetProperty("revision").GetInt64() == 2);
        Assert.Equal("Fci", latest.GetProperty("previousMetadata").GetProperty("classification").GetString());
        Assert.Equal("Synthetic reviewed classification", latest.GetProperty("previousMetadata").GetProperty("reason").GetString());
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Equal(2, await db.ContentClassificationHistory.CountAsync(e => e.EntityType == type));
        Assert.Equal(2, await db.AuditLogEntries.CountAsync(e => e.EntityType == type));
        if (type == "Report")
        {
            var original = await db.Reports.SingleAsync();
            Assert.Equal(ContentClassification.Unknown, original.Classification);
            Assert.Equal("{\"baseline\":\"immutable\"}", original.SnapshotJson);
            Assert.Equal("<p>Immutable synthetic snapshot</p>", original.ExportHtml);
            Assert.Equal(ContentClassification.Fci, (await db.Set<ReportClassificationEntity>().SingleAsync()).Classification);
            using var access = await client.SendAsync(Request(HttpMethod.Get, $"/api/reports/{ids[type]}", read));
            Assert.Equal(HttpStatusCode.OK, access.StatusCode);
        }
        var tracked = await db.ContentClassificationHistory.FirstAsync(); tracked.Reason = "Invalid rewrite";
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }

    [Theory]
    [InlineData("compliance-status")]
    [InlineData("subcontractor-compliance")]
    [InlineData("cmmc-readiness")]
    [InlineData("evidence-packages")]
    public async Task Report_generation_and_lists_expose_current_classification_without_rewriting_snapshot(string route)
    {
        await using var factory = Factory(); using var client = factory.CreateClient();
        var assessmentId = Guid.NewGuid();
        using (var setup = factory.Services.CreateScope())
        {
            var db = setup.ServiceProvider.GetRequiredService<GccsDbContext>();
            db.Assessments.Add(new() { Id = assessmentId, TenantId = tenant, Name = "Synthetic classification assessment",
                Level = Gccs.Domain.Cmmc.CmmcLevel.Level1 });
            await db.SaveChangesAsync();
        }
        using var generated = await client.SendAsync(Request(HttpMethod.Post,
            $"/api/reports/{route}" + (route == "cmmc-readiness" ? $"?assessmentId={assessmentId}" : ""), "ManageReports",
            new { classification = new { classification = "Fci" }, title = "Synthetic classified report",
                contractIds = new[] { contract }, obligationIds = Array.Empty<string>(), controlIds = Array.Empty<string>(),
                subcontractorIds = Array.Empty<Guid>() }));
        Assert.Equal(HttpStatusCode.Created, generated.StatusCode);
        var body = await generated.Content.ReadFromJsonAsync<JsonElement>(); var id = body.GetProperty("id").GetGuid();
        Assert.Equal("Fci", body.GetProperty("classification").GetProperty("classification").GetString());
        using var review = await client.SendAsync(Request(HttpMethod.Patch,
            $"/api/classified-content/reports/{id}/classification", "ManageReports", Review(0, "Unclassified")));
        Assert.Equal(HttpStatusCode.OK, review.StatusCode);
        using var list = await client.SendAsync(Request(HttpMethod.Get, route == "evidence-packages" ?
            "/api/reports/approved-evidence-packages" : "/api/reports/recent", "ViewReports"));
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var row = (await list.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray()
            .Single(r => r.GetProperty(route == "evidence-packages" ? "reportId" : "id").GetGuid() == id);
        Assert.Equal("Unclassified", row.GetProperty("classification").GetProperty("classification").GetString());
        using var detail = await client.SendAsync(Request(HttpMethod.Get,
            route == "evidence-packages" ? $"/api/reports/evidence-packages/{id}" : $"/api/reports/{id}", "ViewReports"));
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        Assert.Equal("Unclassified", (await detail.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("classification").GetProperty("classification").GetString());
        using var scope = factory.Services.CreateScope();
        Assert.Equal(ContentClassification.Fci, (await scope.ServiceProvider.GetRequiredService<GccsDbContext>().Reports.SingleAsync(r => r.Id == id)).Classification);
    }

    [Theory, MemberData(nameof(Cases))]
    public async Task Cui_quarantine_is_visible_in_the_review_queue_for_every_type(string type, string route, string read, string write)
    {
        await using var factory = Factory(); using var client = factory.CreateClient();
        using var review = await client.SendAsync(Request(HttpMethod.Patch,
            $"/api/classified-content/{route}/{ids[type]}/classification", write, Review(0, "Cui")));
        Assert.Equal(HttpStatusCode.OK, review.StatusCode);
        using var queue = await client.SendAsync(Request(HttpMethod.Get, $"/api/classified-content/{route}?reviewOnly=true", read));
        Assert.Equal(HttpStatusCode.OK, queue.StatusCode);
        Assert.Contains(ids[type].ToString(), await queue.Content.ReadAsStringAsync());
    }

    [Theory, MemberData(nameof(Cases))]
    public async Task Invalid_reviews_leave_classification_history_and_audit_unchanged(string type, string route, string read, string write)
    {
        await using var factory = Factory(); using var client = factory.CreateClient(); var path = $"/api/classified-content/{route}/{ids[type]}/classification";
        foreach (var body in new[] { Review(0, "SyntheticCui"), Review(0, reason: ""), Review(-1) })
        {
            using var response = await client.SendAsync(Request(HttpMethod.Patch, path, write, body));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        using var missingRevision = await client.SendAsync(Request(HttpMethod.Patch, path, write, new { classification = new { classification = "Fci", reason = "Synthetic" } }));
        Assert.Equal(HttpStatusCode.BadRequest, missingRevision.StatusCode);
        using var current = await client.SendAsync(Request(HttpMethod.Get, $"/api/classified-content/{route}/{ids[type]}", read));
        Assert.Equal(0, (await current.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("revision").GetInt64());
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Empty(db.ContentClassificationHistory); Assert.Empty(db.AuditLogEntries);
    }

    [Fact]
    public async Task Metadata_and_upload_intents_require_explicit_classification()
    {
        await using var factory = Factory(); using var client = factory.CreateClient();
        using var metadata = await client.SendAsync(Request(HttpMethod.Post, "/api/evidence-items", "ManageEvidence", new {
            title = "Synthetic", type = "Policy", ownerFunction = "Security", status = "Draft", description = "Synthetic",
            tags = Array.Empty<string>(), obligationIds = Array.Empty<string>(), controlIds = Array.Empty<string>(),
            contractIds = Array.Empty<Guid>(), vendorIds = Array.Empty<Guid>(), subcontractorIds = Array.Empty<Guid>(),
            employeeIds = Array.Empty<Guid>(), reportIds = Array.Empty<Guid>() }));
        Assert.Equal(HttpStatusCode.BadRequest, metadata.StatusCode);
        using var upload = await client.SendAsync(Request(HttpMethod.Post, $"/api/evidence-items/{ids["EvidenceItem"]}/upload-intents", "ManageEvidence",
            new { fileName = "synthetic.txt", contentType = "text/plain", sizeBytes = 4, noCuiAttestation = true }));
        Assert.Equal(HttpStatusCode.BadRequest, upload.StatusCode);
        using var document = await client.SendAsync(Request(HttpMethod.Post, $"/api/contracts/{contract}/documents", "ManageContracts",
            new { type = "Contract", fileName = "synthetic.txt", contentType = "text/plain", sizeBytes = 4, containsPotentialCui = false }));
        Assert.Equal(HttpStatusCode.BadRequest, document.StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Single(db.EvidenceItems); Assert.Single(db.EvidenceFileVersions); Assert.Single(db.Set<ContractDocumentEntity>());
        Assert.Empty(db.ContentClassificationHistory);
        Assert.DoesNotContain(db.AuditLogEntries, a => a.Action is Gccs.Domain.Audit.AuditAction.Created or Gccs.Domain.Audit.AuditAction.Uploaded);
    }

    [Theory]
    [InlineData("Report", "reports", "ViewReports", "ManageReports")]
    [InlineData("ClassifiedNote", "notes", "ViewEvidence", "ApproveEvidence")]
    public async Task Current_classification_controls_content_access_without_changing_content(string type, string route, string read, string write)
    {
        await using var factory = Factory(); using var client = factory.CreateClient();
        var contentPath = type == "Report" ? $"/api/reports/{ids[type]}" : $"/api/classified-notes/{ids[type]}";
        using var unknown = await client.SendAsync(Request(HttpMethod.Get, contentPath, read));
        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
        var reviewPath = $"/api/classified-content/{route}/{ids[type]}/classification";
        using var approved = await client.SendAsync(Request(HttpMethod.Patch, reviewPath, write, Review(0)));
        Assert.Equal(HttpStatusCode.OK, approved.StatusCode);
        using var allowed = await client.SendAsync(Request(HttpMethod.Get, contentPath, read));
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        using var prohibited = await client.SendAsync(Request(HttpMethod.Patch, reviewPath, write, Review(1, "Prohibited")));
        Assert.Equal(HttpStatusCode.OK, prohibited.StatusCode);
        using var blocked = await client.SendAsync(Request(HttpMethod.Get, contentPath, read));
        Assert.Equal(HttpStatusCode.BadRequest, blocked.StatusCode);
        using var cui = await client.SendAsync(Request(HttpMethod.Patch, reviewPath, write, Review(2, "Cui")));
        Assert.Equal(HttpStatusCode.OK, cui.StatusCode);
        using var modeBlocked = await client.SendAsync(Request(HttpMethod.Get, contentPath, read));
        Assert.Equal(HttpStatusCode.Forbidden, modeBlocked.StatusCode);
    }

    [Theory]
    [InlineData("ContractDocument", "contract-documents")]
    [InlineData("ExtractionJob", "extraction-jobs")]
    public async Task Extraction_results_and_every_candidate_action_fail_closed(string type, string route)
    {
        await using var factory = Factory(); using var client = factory.CreateClient();
        var otherType = type == "ContractDocument" ? "ExtractionJob" : "ContractDocument";
        var otherRoute = type == "ContractDocument" ? "extraction-jobs" : "contract-documents";
        using var prepare = await client.SendAsync(Request(HttpMethod.Patch,
            $"/api/classified-content/{otherRoute}/{ids[otherType]}/classification", "ReviewClauses", Review(0)));
        Assert.Equal(HttpStatusCode.OK, prepare.StatusCode);
        var root = $"/api/contracts/{contract}/documents/{ids["ContractDocument"]}";
        var candidate = root + $"/clause-candidates/{Guid.NewGuid()}";
        var actions = new[] { (HttpMethod.Get, root + "/extraction-results", "ViewContracts"),
            (HttpMethod.Patch, candidate, "ReviewClauses"),
            (HttpMethod.Post, candidate + "/accept", "ReviewClauses"),
            (HttpMethod.Post, candidate + "/reject", "ReviewClauses"),
            (HttpMethod.Post, candidate + "/needs-clarification", "ReviewClauses"),
            (HttpMethod.Post, candidate + "/supersede", "ReviewClauses") };
        foreach (var label in new[] { "Unknown", "Prohibited", "Cui" })
        {
            if (label != "Unknown")
            {
                using var review = await client.SendAsync(Request(HttpMethod.Patch,
                    $"/api/classified-content/{route}/{ids[type]}/classification", "ReviewClauses",
                    Review(label == "Prohibited" ? 0 : 1, label)));
                Assert.Equal(HttpStatusCode.OK, review.StatusCode);
            }
            foreach (var (method, path, permission) in actions)
            {
                using var response = await client.SendAsync(Request(method, path, permission,
                    method == HttpMethod.Get ? null : new { reason = "Synthetic reason", normalizedCitation = "FAR 52.204-21",
                        rawExtractedText = "Synthetic", locationMetadata = "page 1", clauseLibraryId = "FAR-52.204-21" }));
                Assert.Equal(label == "Cui" ? HttpStatusCode.Forbidden : HttpStatusCode.BadRequest, response.StatusCode);
                using var foreign = await client.SendAsync(Request(method, path, permission,
                    method == HttpMethod.Get ? null : new { reason = "Synthetic reason" }, Guid.NewGuid()));
                Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
                using var denied = await client.SendAsync(Request(method, path, "ViewDashboard",
                    method == HttpMethod.Get ? null : new { reason = "Synthetic reason" }));
                Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
            }
        }
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Empty(db.Set<ClauseCandidateEntity>());
        Assert.Equal(ExtractionJobStatus.Completed, (await db.Set<ExtractionJobEntity>().SingleAsync()).Status);
        Assert.DoesNotContain(db.AuditLogEntries, a => a.EntityType == "ClauseCandidate");
    }

    [Theory]
    [InlineData("archive")]
    [InlineData("restore")]
    public async Task Restricted_report_lifecycle_cannot_return_snapshot_or_mutate(string action)
    {
        await using var factory = Factory(); using var client = factory.CreateClient();
        using var response = await client.SendAsync(Request(HttpMethod.Post,
            $"/api/reports/{ids["Report"]}/{action}", "ArchiveReports", new { reason = "Synthetic reason" }));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.DoesNotContain("immutable", await response.Content.ReadAsStringAsync());
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Equal(ReportStatus.Complete, (await db.Reports.SingleAsync()).Status);
        Assert.Empty(db.AuditLogEntries);
    }

    [Fact]
    public async Task Version_escalation_blocks_both_download_endpoints_without_a_parent_escalation()
    {
        await using var factory = Factory(); using var client = factory.CreateClient();
        foreach (var (type, route) in new[] { ("EvidenceItem", "evidence-items"), ("EvidenceFileVersion", "evidence-file-versions") })
        {
            using var reviewed = await client.SendAsync(Request(HttpMethod.Patch,
                $"/api/classified-content/{route}/{ids[type]}/classification", "ApproveEvidence", Review(0)));
            Assert.Equal(HttpStatusCode.OK, reviewed.StatusCode);
        }
        var root = $"/api/evidence-items/{ids["EvidenceItem"]}";
        using var allowed = await client.SendAsync(Request(HttpMethod.Get, root + "/download", "ViewEvidence"));
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        using var escalation = await client.SendAsync(Request(HttpMethod.Post, $"/api/tenants/{tenant}/cui-support-escalations",
            "ManageTenant", new { sourceWorkflow = "ClassificationReview", affectedEntityType = "EvidenceFileVersion",
                affectedEntityId = ids["EvidenceFileVersion"].ToString(), category = "ProhibitedData", severity = "High",
                description = "Synthetic version-only concern." }));
        Assert.Equal(HttpStatusCode.Created, escalation.StatusCode);
        foreach (var suffix in new[] { "/download", "/file/content" })
        {
            using var denied = await client.SendAsync(Request(HttpMethod.Get, root + suffix, "ViewEvidence"));
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        }
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Equal(1, await db.AuditLogEntries.CountAsync(a => a.Action == Gccs.Domain.Audit.AuditAction.Downloaded));
        Assert.Equal(ContentClassification.Fci, (await db.EvidenceItems.SingleAsync()).Classification);
        Assert.Equal(1, await db.EvidenceFileVersions.CountAsync());
    }

    [Theory, MemberData(nameof(Cases))]
    public async Task Every_content_type_can_be_escalated_and_only_released_after_review(string type, string route, string read, string write)
    {
        await using var factory = Factory(); using var client = factory.CreateClient();
        using var safe = await client.SendAsync(Request(HttpMethod.Patch,
            $"/api/classified-content/{route}/{ids[type]}/classification", write, Review(0)));
        Assert.Equal(HttpStatusCode.OK, safe.StatusCode);
        var path = $"/api/tenants/{tenant}/cui-support-escalations";
        object Body(Guid id) => new { sourceWorkflow = "ClassificationReview", affectedEntityType = type,
            affectedEntityId = id.ToString(), category = "ProhibitedData", severity = "High", description = "Synthetic metadata-only concern." };
        using var unavailable = await client.SendAsync(Request(HttpMethod.Post, path, "ManageTenant", Body(Guid.NewGuid())));
        Assert.Equal(HttpStatusCode.BadRequest, unavailable.StatusCode);
        using var denied = await client.SendAsync(Request(HttpMethod.Post, path,
            read == "ViewReports" ? "ViewEvidence" : "ViewReports", Body(ids[type])));
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        using var created = await client.SendAsync(Request(HttpMethod.Post, path, read, Body(ids[type])));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var escalation = await created.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(escalation.GetProperty("isAffectedContentBlocked").GetBoolean());
        var resolve = path + "/" + escalation.GetProperty("id").GetString() + "/resolve";
        var resolution = new { resolutionType = "FalsePositive", summary = "Synthetic reviewed false positive" };
        using var premature = await client.SendAsync(Request(HttpMethod.Post, resolve, "ManageTenant", resolution));
        Assert.Equal(HttpStatusCode.BadRequest, premature.StatusCode);
        // A prior safe label cannot release newly escalated content; review must follow the escalation.
        using var reviewed = await client.SendAsync(Request(HttpMethod.Patch,
            $"/api/classified-content/{route}/{ids[type]}/classification", write, Review(1)));
        Assert.Equal(HttpStatusCode.OK, reviewed.StatusCode);
        if (type == "EvidenceItem")
        {
            using var version = await client.SendAsync(Request(HttpMethod.Patch,
                $"/api/classified-content/evidence-file-versions/{ids["EvidenceFileVersion"]}/classification", write, Review(0)));
            Assert.Equal(HttpStatusCode.OK, version.StatusCode);
        }
        using var released = await client.SendAsync(Request(HttpMethod.Post, resolve, "ManageTenant", resolution));
        Assert.Equal(HttpStatusCode.OK, released.StatusCode);
        Assert.False((await released.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("isAffectedContentBlocked").GetBoolean());
        using var repeated = await client.SendAsync(Request(HttpMethod.Post, resolve, "ManageTenant", resolution));
        Assert.Equal(HttpStatusCode.BadRequest, repeated.StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Single(db.CuiSupportEscalations); Assert.Single(db.CuiSupportEscalationResolutions);
    }

    [Theory, MemberData(nameof(Cases))]
    public async Task Six_role_matrix_covers_list_detail_history_and_review(string type, string route, string read, string write)
    {
        await using var factory = Factory(); using var client = factory.CreateClient(); long revision = 0;
        foreach (var role in new[] { "Owner", "Admin", "Compliance Manager", "Contributor", "Advisor", "Auditor" })
        {
            var permissions = Gccs.Domain.Identity.RoleCatalog.GetPermissions(role).Select(p => p.ToString()).ToArray();
            HttpRequestMessage AsRole(HttpMethod method, string path, object? body = null)
            {
                var request = Request(method, path, "", body); request.Headers.Remove("X-Gccs-Dev-Permissions");
                request.Headers.Add("X-Gccs-Dev-Role", role); return request;
            }
            foreach (var suffix in new[] { "", "/" + ids[type], "/" + ids[type] + "/history" })
            {
                using var response = await client.SendAsync(AsRole(HttpMethod.Get, $"/api/classified-content/{route}" + suffix));
                Assert.Equal(permissions.Contains(read) ? HttpStatusCode.OK : HttpStatusCode.Forbidden, response.StatusCode);
            }
            using var updated = await client.SendAsync(AsRole(HttpMethod.Patch,
                $"/api/classified-content/{route}/{ids[type]}/classification", Review(revision)));
            Assert.Equal(permissions.Contains(write) ? HttpStatusCode.OK : HttpStatusCode.Forbidden, updated.StatusCode);
            if (permissions.Contains(write)) revision++;
        }
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Equal(revision, await db.ContentClassificationHistory.LongCountAsync());
        Assert.Equal(revision, await db.AuditLogEntries.LongCountAsync(a => a.EntityType == type));
    }

    [PostgresFact, Trait("Category", "PostgresIntegration")]
    public async Task Reclassification_during_extraction_prevents_result_publication()
    {
        var extractor = new PausedExtractor();
        await using var factory = Factory(true, extractor: extractor); using var client = factory.CreateClient();
        foreach (var (type, route) in new[] { ("ContractDocument", "contract-documents"), ("ExtractionJob", "extraction-jobs") })
        {
            using var prepare = await client.SendAsync(Request(HttpMethod.Patch,
                $"/api/classified-content/{route}/{ids[type]}/classification", "ReviewClauses", Review(0)));
            Assert.Equal(HttpStatusCode.OK, prepare.StatusCode);
        }
        var processing = client.SendAsync(Request(HttpMethod.Post, $"/api/extraction-jobs/{ids["ExtractionJob"]}/process", "ManageContracts"));
        await extractor.Started.Task.WaitAsync(TimeSpan.FromSeconds(15));
        try
        {
            using var restrict = await client.SendAsync(Request(HttpMethod.Patch,
                $"/api/classified-content/extraction-jobs/{ids["ExtractionJob"]}/classification", "ReviewClauses", Review(1, "Prohibited")));
            Assert.Equal(HttpStatusCode.OK, restrict.StatusCode);
        }
        finally { extractor.Release.TrySetResult(); }
        using var result = await processing;
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Empty(await db.Set<ClauseCandidateEntity>().Where(c => c.TenantId == tenant).ToArrayAsync());
        Assert.Equal(ContentClassification.Prohibited, (await db.Set<ExtractionJobEntity>().SingleAsync(j => j.Id == ids["ExtractionJob"])).Classification);
        Assert.DoesNotContain(await db.AuditLogEntries.Where(a => a.TenantId == tenant).ToArrayAsync(),
            a => a.Summary.Contains("completed", StringComparison.OrdinalIgnoreCase));
    }

    [PostgresFact, Trait("Category", "PostgresIntegration")]
    public async Task Concurrent_escalation_resolutions_release_content_only_after_every_case_is_resolved()
    {
        await using var factory = Factory(true); using var client = factory.CreateClient();
        using var initialReview = await client.SendAsync(Request(HttpMethod.Patch,
            $"/api/classified-content/evidence-items/{ids["EvidenceItem"]}/classification", "ApproveEvidence", Review(0)));
        Assert.Equal(HttpStatusCode.OK, initialReview.StatusCode);
        var path = $"/api/tenants/{tenant}/cui-support-escalations";
        var body = new { sourceWorkflow = "EvidenceDetail", affectedEntityType = "EvidenceItem",
            affectedEntityId = ids["EvidenceItem"].ToString(), category = "SuspectedCui", severity = "High", description = "Synthetic concurrency concern." };
        var creates = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => client.SendAsync(Request(HttpMethod.Post, path, "ViewEvidence", body))));
        Assert.All(creates, response => Assert.Equal(HttpStatusCode.Created, response.StatusCode));
        var escalationIds = new List<string>();
        foreach (var response in creates) { escalationIds.Add((await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!); response.Dispose(); }
        using var postEscalationReview = await client.SendAsync(Request(HttpMethod.Patch,
            $"/api/classified-content/evidence-items/{ids["EvidenceItem"]}/classification", "ApproveEvidence", Review(1)));
        Assert.Equal(HttpStatusCode.OK, postEscalationReview.StatusCode);
        using var versionReview = await client.SendAsync(Request(HttpMethod.Patch,
            $"/api/classified-content/evidence-file-versions/{ids["EvidenceFileVersion"]}/classification", "ApproveEvidence", Review(0)));
        Assert.Equal(HttpStatusCode.OK, versionReview.StatusCode);
        var resolutionMarker = $"Concurrent safe review {Guid.NewGuid():N}.";
        var resolution = new { resolutionType = "FalseAlarm", summary = resolutionMarker };
        var results = await Task.WhenAll(escalationIds.Select(id => client.SendAsync(Request(HttpMethod.Post, $"{path}/{id}/resolve", "ManageTenant", resolution))));
        Assert.All(results, response => { Assert.Equal(HttpStatusCode.OK, response.StatusCode); response.Dispose(); });
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.False((await db.EvidenceItems.AsNoTracking().SingleAsync(e => e.Id == ids["EvidenceItem"])).IsUseBlocked);
        var createdEscalationIds = escalationIds.Select(Guid.Parse).ToArray();
        Assert.Equal(2, await db.CuiSupportEscalationResolutions.CountAsync(r =>
            createdEscalationIds.Contains(r.EscalationId) && r.Summary == resolutionMarker));
    }
    private sealed class PausedExtractor : Gccs.Application.Contracts.IContractDocumentTextExtractor
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task<Gccs.Application.Contracts.DocumentTextExtractionResult> ExtractTextAsync(
            Gccs.Application.Contracts.ContractDocumentDto document, CancellationToken ct = default)
        {
            Started.TrySetResult(); await Release.Task.WaitAsync(ct);
            return Gccs.Application.Contracts.DocumentTextExtractionResult.Success("FAR 52.204-21 Synthetic test text.");
        }
    }

    [PostgresFact, Trait("Category", "PostgresIntegration")]
    public async Task PostgreSQL_serializes_every_type_and_rolls_back_failed_audit()
    {
        var failure = new AuditFailure(); await using var factory = Factory(true, failure); using var client = factory.CreateClient();
        string originalSnapshot;
        using (var before = factory.Services.CreateScope())
            originalSnapshot = await before.ServiceProvider.GetRequiredService<GccsDbContext>().Reports
                .Where(e => e.Id == ids["Report"]).Select(e => e.SnapshotJson).SingleAsync();
        foreach (var t in Types)
        {
            var path = $"/api/classified-content/{t[1]}/{ids[t[0]]}/classification";
            var responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => client.SendAsync(Request(HttpMethod.Patch, path, t[3], Review(0)))));
            Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
            Assert.Equal(7, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));
            foreach (var response in responses) response.Dispose();
            failure.Enabled = true;
            using var rejected = await client.SendAsync(Request(HttpMethod.Patch, path, t[3], Review(1, "Unclassified")));
            Assert.Equal(HttpStatusCode.InternalServerError, rejected.StatusCode); failure.Enabled = false;
            using var current = await client.SendAsync(Request(HttpMethod.Get, path.Replace("/classification", ""), t[2]));
            Assert.Equal(1, (await current.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("revision").GetInt64());
            using var queue = await client.SendAsync(Request(HttpMethod.Get, $"/api/classified-content/{t[1]}?reviewOnly=true", t[2]));
            Assert.Equal(HttpStatusCode.OK, queue.StatusCode);
        }
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        Assert.Equal(6, await db.ContentClassificationHistory.CountAsync(e => e.TenantId == tenant));
        Assert.Equal(6, await db.AuditLogEntries.CountAsync(e => e.TenantId == tenant));
        var report = await db.Reports.SingleAsync(e => e.Id == ids["Report"]);
        Assert.Equal(originalSnapshot, report.SnapshotJson);
        Assert.Equal("<p>Immutable synthetic snapshot</p>", report.ExportHtml);
        // Keep this tenant's synthetic history for migration/append-only inspection; no customer records are modified.
    }
    private sealed class AuditFailure : Microsoft.EntityFrameworkCore.Diagnostics.SaveChangesInterceptor
    {
        public bool Enabled { get; set; }
        public override ValueTask<Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<int>> SavingChangesAsync(
            Microsoft.EntityFrameworkCore.Diagnostics.DbContextEventData data, Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<int> result,
            CancellationToken ct = default)
        {
            if (Enabled && data.Context!.ChangeTracker.Entries<AuditLogEntryEntity>().Any(e => e.State == EntityState.Added))
                throw new InvalidOperationException("Synthetic audit failure");
            return ValueTask.FromResult(result);
        }
    }
}

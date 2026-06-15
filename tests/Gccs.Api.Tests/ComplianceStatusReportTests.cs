using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Reports;
using Gccs.Domain.Audit;
using Gccs.Domain.Cmmc;
using Gccs.Domain.Common;
using Gccs.Domain.Companies;
using Gccs.Domain.Compliance;
using Gccs.Domain.Contracts;
using Gccs.Domain.Evidence;
using Gccs.Domain.Identity;
using Gccs.Domain.Reports;
using Gccs.Domain.Tenancy;
using Gccs.Domain.Vendors;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Gccs.Infrastructure.Reports;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class ComplianceStatusReportTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public ComplianceStatusReportTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_15_1_1_Generate_current_status_report_includes_summary_sections()
    {
        var ids = StoryIds.ForCase("tc-15-1-1");
        await using var factory = CreateFactory("tc-15-1-1", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();

        var report = await GenerateReportAsync(client, ids.TenantId);

        Assert.Equal(ReportType.ComplianceStatus, report.Type);
        Assert.Equal(ReportStatus.Complete, report.Status);
        Assert.Equal(1, report.Snapshot.TotalObligations);
        Assert.Equal(1, report.Snapshot.HighRiskObligations);
        Assert.Equal(1, report.Snapshot.OverdueTasks);
        Assert.Equal(1, report.Snapshot.EvidenceStatusCounts["Uploaded"]);
        Assert.Equal(1, report.Snapshot.CmmcAssessments);
        Assert.Equal(1, report.Snapshot.CmmcControlsImplemented);
        Assert.Equal(2, report.Snapshot.CmmcControlsTotal);
        Assert.Equal(2, report.Snapshot.SubcontractorGaps);
        Assert.Contains(report.Snapshot.HighRiskItems, item => item.Contains("High risk obligation", StringComparison.Ordinal));
        Assert.Contains("Compliance status report", report.ExportHtml);
    }

    [Fact]
    public async Task TC_15_1_2_Compliance_status_report_excludes_other_tenant_data()
    {
        var ids = StoryIds.ForCase("tc-15-1-2");
        await using var factory = CreateFactory("tc-15-1-2", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();

        var report = await GenerateReportAsync(client, ids.TenantId);

        Assert.Equal(1, report.Snapshot.TotalObligations);
        Assert.Equal(1, report.Snapshot.EvidenceStatusCounts.Values.Sum());
        Assert.DoesNotContain(report.Snapshot.HighRiskItems, item => item.Contains("Other tenant", StringComparison.Ordinal));
    }

    [Fact]
    public async Task TC_15_1_3_Generation_timestamp_and_snapshot_metadata_are_stored()
    {
        var ids = StoryIds.ForCase("tc-15-1-3");
        await using var factory = CreateFactory("tc-15-1-3", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();

        var report = await GenerateReportAsync(client, ids.TenantId);

        Assert.True(report.GeneratedAt <= DateTimeOffset.UtcNow.AddMinutes(1));
        Assert.Equal(report.GeneratedAt, report.Snapshot.GeneratedAt);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var stored = await dbContext.Reports.SingleAsync(candidate => candidate.Id == report.Id);
        Assert.Contains("totalObligations", stored.SnapshotJson, StringComparison.Ordinal);
        Assert.Contains("<html>", stored.ExportHtml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TC_15_1_4_Compliance_status_report_generation_is_audit_logged()
    {
        var ids = StoryIds.ForCase("tc-15-1-4");
        await using var factory = CreateFactory("tc-15-1-4", dbContext => SeedScenario(dbContext, ids));
        using var client = factory.CreateClient();

        var report = await GenerateReportAsync(client, ids.TenantId);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var audit = await dbContext.AuditLogEntries.SingleAsync(candidate =>
            candidate.TenantId == ids.TenantId &&
            candidate.EntityType == "Report" &&
            candidate.EntityId == report.Id.ToString());
        Assert.Equal(AuditAction.Created, audit.Action);
        Assert.Contains("ComplianceStatus", audit.MetadataJson, StringComparison.Ordinal);
    }

    private static async Task<ComplianceStatusReportDto> GenerateReportAsync(HttpClient client, Guid tenantId)
    {
        using var request = CreateRequest<object?>(HttpMethod.Post, "/api/reports/compliance-status", null, tenantId, Permission.ViewReports);
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<ComplianceStatusReportDto>(JsonOptions) ??
            throw new InvalidOperationException("Expected compliance status report response.");
    }

    private WebApplicationFactory<Program> CreateFactory(string databaseName, Action<GccsDbContext>? seed = null) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<ComplianceStatusReportService>();
                services.AddScoped<IReportRepository, EfReportRepository>();
                services.AddScoped<IAuditEventWriter, EfAuditEventWriter>();

                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
                dbContext.Database.EnsureDeleted();
                dbContext.Database.EnsureCreated();
                seed?.Invoke(dbContext);
                dbContext.SaveChanges();
            });
        });

    private static HttpRequestMessage CreateRequest<TContent>(
        HttpMethod method,
        string requestUri,
        TContent content,
        Guid tenantId,
        Permission permission)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", Guid.NewGuid().ToString());
        request.Headers.Add("X-Gccs-Dev-Permissions", permission.ToString());
        if (content is not null)
        {
            request.Content = JsonContent.Create(content, options: JsonOptions);
        }

        return request;
    }

    private static void SeedScenario(GccsDbContext dbContext, StoryIds ids)
    {
        dbContext.Tenants.AddRange(CreateTenant(ids.TenantId), CreateTenant(ids.OtherTenantId));
        SeedTenantData(dbContext, ids.TenantId, ids.ContractId, ids.ContractClauseId, ids.ObligationId, ids.SubcontractorId, ids.EvidenceItemId, "High risk obligation");
        SeedTenantData(dbContext, ids.OtherTenantId, ids.OtherContractId, ids.OtherContractClauseId, ids.OtherObligationId, ids.OtherSubcontractorId, ids.OtherEvidenceItemId, "Other tenant obligation");
    }

    private static void SeedTenantData(
        GccsDbContext dbContext,
        Guid tenantId,
        Guid contractId,
        Guid contractClauseId,
        string obligationId,
        Guid subcontractorId,
        Guid evidenceItemId,
        string obligationTitle)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var assessmentId = Guid.NewGuid();
        dbContext.Contracts.Add(new ContractEntity
        {
            Id = contractId,
            TenantId = tenantId,
            ContractNumber = $"REPORT-{contractId.ToString()[..8]}",
            Title = "Compliance report contract",
            AgencyOrPrimeName = "Prime Integrator",
            Relationship = ContractorRelationship.Prime,
            Kind = ContractKind.FixedPrice,
            Status = ContractStatus.Active,
            AwardedAt = today.AddDays(-10),
            PeriodOfPerformanceStart = today.AddDays(-5),
            PeriodOfPerformanceEnd = today.AddYears(1),
            PlaceOfPerformance = "Arlington, VA",
            Description = "No-CUI compliance report seed.",
            DataHandlingPosture = DataHandlingPosture.FciOnly,
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.Obligations.Add(new ObligationEntity
        {
            Id = obligationId,
            Source = "FAR",
            Title = obligationTitle,
            PlainEnglishSummary = "Report seed obligation.",
            TriggerCondition = "Contract contains report seed clause.",
            RequiredAction = "Track status for report.",
            OwnerFunction = "Contracts",
            RiskLevel = RiskLevel.High,
            RequiresFlowDown = true,
            FlowDownRequirement = "Flow down when applicable.",
            SourceName = "Acquisition source",
            SourceUrl = "https://example.test/source",
            SourceLastReviewedAt = new DateOnly(2026, 6, 15),
            LastReviewedAt = new DateOnly(2026, 6, 15),
            Confidence = "high",
            SourceConfidence = "high",
            ReviewState = ReviewState.Approved
        });
        dbContext.Set<ContractClauseEntity>().Add(new ContractClauseEntity
        {
            Id = contractClauseId,
            ContractId = contractId,
            ClauseLibraryId = $"report-clause-{contractClauseId}",
            ClauseNumber = "52.000-1",
            Title = "Report seed clause",
            Source = ClauseSource.Far,
            SourceUrl = "https://example.test/source",
            AttachmentReason = "Seed report obligation.",
            RequiresFlowDown = true,
            LastReviewedAt = new DateOnly(2026, 6, 15),
            Confidence = "high",
            ReviewState = ReviewState.Approved,
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.Set<ContractClauseObligationEntity>().Add(new ContractClauseObligationEntity
        {
            ContractClauseId = contractClauseId,
            ObligationId = obligationId
        });
        dbContext.ComplianceTasks.Add(new ComplianceTaskEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Title = obligationTitle.Contains("Other", StringComparison.Ordinal) ? "Other tenant overdue task" : "Overdue report task",
            Description = "Overdue report seed task.",
            Type = ComplianceTaskType.ObligationAction,
            Status = ComplianceTaskStatus.Open,
            RiskLevel = RiskLevel.High,
            OwnerFunction = "Contracts",
            DueAt = today.AddDays(-1),
            ObligationId = obligationId,
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.EvidenceItems.Add(new EvidenceItemEntity
        {
            Id = evidenceItemId,
            TenantId = tenantId,
            Name = "Report evidence",
            Description = "Report seed evidence.",
            Type = EvidenceType.Policy,
            OwnerFunction = "Security",
            Status = EvidenceStatus.Uploaded,
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.Assessments.Add(new AssessmentEntity
        {
            Id = assessmentId,
            TenantId = tenantId,
            Name = "Report CMMC readiness",
            Type = AssessmentType.SelfAssessment,
            Level = CmmcLevel.Level1,
            Framework = "CMMC",
            Status = AssessmentStatus.InProgress,
            StartedAt = today.AddDays(-10),
            OwnerFunction = "Security",
            CreatedAt = DateTimeOffset.UtcNow,
            Controls =
            [
                new ControlAssessmentEntity
                {
                    AssessmentId = assessmentId,
                    ControlId = "AC.L1-3.1.1",
                    ImplementationStatus = ControlImplementationStatus.Implemented,
                    Result = AssessmentResult.Met
                },
                new ControlAssessmentEntity
                {
                    AssessmentId = assessmentId,
                    ControlId = "AC.L1-3.1.2",
                    ImplementationStatus = ControlImplementationStatus.NotStarted,
                    Result = AssessmentResult.NotMet
                }
            ]
        });
        dbContext.Subcontractors.Add(new SubcontractorEntity
        {
            Id = subcontractorId,
            TenantId = tenantId,
            Name = "Report Supplier LLC",
            Status = SubcontractorStatus.Active,
            RoleDescription = "Report support",
            SmallBusinessStatus = "Small",
            CmmcStatus = "Level 1",
            NdaStatus = "Executed",
            WorkshareDescription = "Report seed",
            HasFciAccess = true,
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.FlowDownClauses.Add(new FlowDownClauseEntity
        {
            Id = Guid.NewGuid(),
            SubcontractorId = subcontractorId,
            ClauseNumber = "52.244-6",
            Title = "Open flow-down gap",
            Status = FlowDownStatus.Sent,
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.SubcontractorEvidenceRequests.Add(new SubcontractorEvidenceRequestEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SubcontractorId = subcontractorId,
            RequestedItem = "Overdue supplier evidence",
            RequestedEvidenceTypesJson = "[\"SignedFlowDown\"]",
            DueDate = today.AddDays(-2),
            Status = SubcontractorEvidenceRequestStatus.Sent,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    private static TenantEntity CreateTenant(Guid tenantId) =>
        new()
        {
            Id = tenantId,
            Name = "Report Tenant",
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.UtcNow
        };

    private sealed record StoryIds(
        Guid TenantId,
        Guid OtherTenantId,
        Guid ContractId,
        Guid OtherContractId,
        Guid ContractClauseId,
        Guid OtherContractClauseId,
        string ObligationId,
        string OtherObligationId,
        Guid SubcontractorId,
        Guid OtherSubcontractorId,
        Guid EvidenceItemId,
        Guid OtherEvidenceItemId)
    {
        public static StoryIds ForCase(string caseName)
        {
            var suffix = Math.Abs(caseName.GetHashCode(StringComparison.Ordinal)) % 10000;
            return new StoryIds(
                Guid.Parse($"15115115-1151-1511-5115-11511511{suffix:D4}"),
                Guid.Parse($"15115115-1151-1511-5115-11511512{suffix:D4}"),
                Guid.Parse($"15115115-1151-1511-5115-11511513{suffix:D4}"),
                Guid.Parse($"15115115-1151-1511-5115-11511514{suffix:D4}"),
                Guid.Parse($"15115115-1151-1511-5115-11511515{suffix:D4}"),
                Guid.Parse($"15115115-1151-1511-5115-11511516{suffix:D4}"),
                $"obligation-15-1-{suffix:D4}",
                $"other-obligation-15-1-{suffix:D4}",
                Guid.Parse($"15115115-1151-1511-5115-11511517{suffix:D4}"),
                Guid.Parse($"15115115-1151-1511-5115-11511518{suffix:D4}"),
                Guid.Parse($"15115115-1151-1511-5115-11511519{suffix:D4}"),
                Guid.Parse($"15115115-1151-1511-5115-11511520{suffix:D4}"));
        }
    }
}

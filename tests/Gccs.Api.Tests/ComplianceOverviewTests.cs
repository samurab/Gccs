using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Gccs.Application.Compliance;
using Gccs.Domain.Audit;
using Gccs.Domain.Cmmc;
using Gccs.Domain.Companies;
using Gccs.Domain.Compliance;
using Gccs.Domain.Common;
using Gccs.Domain.Contracts;
using Gccs.Domain.Evidence;
using Gccs.Domain.Identity;
using Gccs.Infrastructure.Compliance;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class ComplianceOverviewTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly WebApplicationFactory<Program> _factory;

    public ComplianceOverviewTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
        });
    }

    [Fact]
    public async Task Authorized_tenant_user_gets_overview_successfully()
    {
        var tenantId = Guid.Parse("51515151-5151-5151-5151-5151515151a1");
        await using var factory = CreatePersistenceFactory("overview-authorized", dbContext =>
        {
            SeedTenantOverviewData(dbContext, tenantId);
        });
        using var client = factory.CreateClient();
        using var request = CreateOverviewRequest(tenantId, Permission.ViewObligations);

        var response = await client.SendAsync(request);
        var overview = await response.Content.ReadFromJsonAsync<ComplianceOverviewDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(overview);
        Assert.Equal(tenantId, overview.TenantId);
        Assert.Equal(3, overview.ControlsTotal);
        Assert.Equal(1, overview.ControlsImplemented);
        Assert.Equal(1, overview.ControlsInProgress);
        Assert.Equal(1, overview.ControlsNotStarted);
        Assert.Equal(2, overview.OpenPoams);
        Assert.Equal(1, overview.OverduePoams);
        Assert.Equal(2, overview.EvidenceItems);
        Assert.Equal(new ReadinessScoreDto(33, 3, 3, 1, 0, "Low coverage"), overview.ReadinessScore);
        Assert.Equal("High", overview.ContractRiskIndicator.Level);
        Assert.Equal(1, overview.ContractRiskIndicator.ActiveContracts);
        Assert.Equal(1, overview.ContractRiskIndicator.HighRiskObligations);
        Assert.Equal(1, overview.ContractRiskIndicator.OverdueHighRiskTasks);
        Assert.Equal(["Evidence uploaded"], overview.RecentAuditEvents.Select(item => item.Summary).ToArray());
        Assert.Contains(overview.Alerts, alert => alert.AlertType == "overdue_poam");
        Assert.Contains(overview.Alerts, alert => alert.AlertType == "control_without_evidence");
        Assert.Contains(overview.Alerts, alert => alert.AlertType == "evidence_pending_review");
    }

    [Fact]
    public async Task Empty_tenant_returns_zero_counts_and_empty_recent_audit_events()
    {
        var tenantId = Guid.Parse("51515151-5151-5151-5151-5151515151a2");
        await using var factory = CreatePersistenceFactory("overview-empty");
        using var client = factory.CreateClient();
        using var request = CreateOverviewRequest(tenantId, Permission.ViewObligations);

        var response = await client.SendAsync(request);
        var overview = await response.Content.ReadFromJsonAsync<ComplianceOverviewDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(overview);
        Assert.Equal(tenantId, overview.TenantId);
        Assert.Equal(0, overview.ControlsTotal);
        Assert.Equal(0, overview.ControlsImplemented);
        Assert.Equal(0, overview.ControlsInProgress);
        Assert.Equal(0, overview.ControlsNotStarted);
        Assert.Equal(0, overview.OpenPoams);
        Assert.Equal(0, overview.OverduePoams);
        Assert.Equal(0, overview.EvidenceItems);
        Assert.Equal(new ReadinessScoreDto(null, 0, 0, 0, 0, "Not started"), overview.ReadinessScore);
        Assert.Equal(new ContractRiskIndicatorDto("Low", 0, 0, 0, 0, 0, 0, 0), overview.ContractRiskIndicator);
        Assert.Empty(overview.RecentAuditEvents);
        Assert.Empty(overview.Alerts);
    }

    [Fact]
    public async Task Coverage_excludes_not_applicable_controls_and_historical_assessments()
    {
        var tenantId = Guid.Parse("51515151-5151-5151-5151-5151515151a7");
        await using var factory = CreatePersistenceFactory("overview-coverage-scope", dbContext =>
        {
            var currentAssessmentId = Guid.NewGuid();
            var supersededAssessmentId = Guid.NewGuid();
            dbContext.Assessments.AddRange(
                AssessmentFor(currentAssessmentId, tenantId, AssessmentStatus.InProgress),
                AssessmentFor(supersededAssessmentId, tenantId, AssessmentStatus.Superseded));
            dbContext.ControlAssessments.AddRange(
                new ControlAssessmentEntity
                {
                    AssessmentId = currentAssessmentId,
                    ControlId = "AC.CURRENT.1",
                    ImplementationStatus = ControlImplementationStatus.Implemented,
                    Result = AssessmentResult.Met
                },
                new ControlAssessmentEntity
                {
                    AssessmentId = currentAssessmentId,
                    ControlId = "AC.CURRENT.2",
                    ImplementationStatus = ControlImplementationStatus.NotApplicable,
                    Result = AssessmentResult.NotApplicable
                },
                new ControlAssessmentEntity
                {
                    AssessmentId = supersededAssessmentId,
                    ControlId = "AC.HISTORICAL.1",
                    ImplementationStatus = ControlImplementationStatus.NotStarted,
                    Result = AssessmentResult.NotAssessed
                });
        });
        using var client = factory.CreateClient();
        using var request = CreateOverviewRequest(tenantId, Permission.ViewObligations);

        var response = await client.SendAsync(request);
        var overview = await response.Content.ReadFromJsonAsync<ComplianceOverviewDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(overview);
        Assert.Equal(2, overview.ControlsTotal);
        Assert.Equal(new ReadinessScoreDto(100, 2, 1, 1, 1, "High coverage"), overview.ReadinessScore);
    }

    [Fact]
    public void Coverage_is_unavailable_when_every_scoped_control_is_not_applicable()
    {
        var score = ComplianceOverviewScoring.BuildReadinessScore(3, 0, 3);

        Assert.Equal(new ReadinessScoreDto(null, 3, 0, 0, 3, "No applicable controls"), score);
    }

    [Fact]
    public async Task Unauthorized_user_without_view_obligations_permission_is_blocked()
    {
        using var client = _factory.CreateClient();
        using var request = CreateOverviewRequest(
            Guid.Parse("51515151-5151-5151-5151-5151515151a3"),
            Permission.ManageTasks);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Cross_tenant_data_is_not_included()
    {
        var tenantAId = Guid.Parse("51515151-5151-5151-5151-5151515151a4");
        var tenantBId = Guid.Parse("51515151-5151-5151-5151-5151515151b4");
        await using var factory = CreatePersistenceFactory("overview-cross-tenant", dbContext =>
        {
            SeedTenantOverviewData(dbContext, tenantAId);
            SeedTenantOverviewData(dbContext, tenantBId, "B");
        });
        using var client = factory.CreateClient();
        using var request = CreateOverviewRequest(tenantAId, Permission.ViewObligations);

        var response = await client.SendAsync(request);
        var overview = await response.Content.ReadFromJsonAsync<ComplianceOverviewDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(overview);
        Assert.Equal(tenantAId, overview.TenantId);
        Assert.Equal(3, overview.ControlsTotal);
        Assert.Equal(2, overview.EvidenceItems);
        Assert.Equal(1, overview.ContractRiskIndicator.ActiveContracts);
        Assert.Equal(1, overview.ContractRiskIndicator.HighRiskObligations);
        Assert.DoesNotContain(overview.RecentAuditEvents, item => item.Summary.Contains("B", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(overview.Alerts, item => item.Message.Contains("B", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Overdue_poam_generates_dashboard_alert()
    {
        var tenantId = Guid.Parse("51515151-5151-5151-5151-5151515151a7");
        await using var factory = CreatePersistenceFactory("overview-overdue-poam-alert", dbContext =>
        {
            SeedTenantOverviewData(dbContext, tenantId);
        });
        using var client = factory.CreateClient();
        using var request = CreateOverviewRequest(tenantId, Permission.ViewObligations);

        var response = await client.SendAsync(request);
        var overview = await response.Content.ReadFromJsonAsync<ComplianceOverviewDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(overview);
        var alert = Assert.Single(overview.Alerts, item => item.AlertType == "overdue_poam");
        Assert.Equal("High", alert.Severity);
        Assert.Equal("PoamItem", alert.EntityType);
        Assert.Contains("AC.A.2", alert.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Control_without_evidence_generates_dashboard_alert()
    {
        var tenantId = Guid.Parse("51515151-5151-5151-5151-5151515151a8");
        await using var factory = CreatePersistenceFactory("overview-control-without-evidence-alert", dbContext =>
        {
            SeedTenantOverviewData(dbContext, tenantId);
        });
        using var client = factory.CreateClient();
        using var request = CreateOverviewRequest(tenantId, Permission.ViewObligations);

        var response = await client.SendAsync(request);
        var overview = await response.Content.ReadFromJsonAsync<ComplianceOverviewDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(overview);
        Assert.Contains(overview.Alerts, alert =>
            alert.AlertType == "control_without_evidence" &&
            alert.EntityType == "ControlAssessment" &&
            alert.Message.Contains("AC.A.2", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Evidence_pending_review_generates_dashboard_alert()
    {
        var tenantId = Guid.Parse("51515151-5151-5151-5151-5151515151a9");
        await using var factory = CreatePersistenceFactory("overview-evidence-pending-alert", dbContext =>
        {
            SeedTenantOverviewData(dbContext, tenantId);
        });
        using var client = factory.CreateClient();
        using var request = CreateOverviewRequest(tenantId, Permission.ViewObligations);

        var response = await client.SendAsync(request);
        var overview = await response.Content.ReadFromJsonAsync<ComplianceOverviewDto>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(overview);
        Assert.Contains(overview.Alerts, alert =>
            alert.AlertType == "evidence_pending_review" &&
            alert.EntityType == "EvidenceItem" &&
            alert.Message.Contains("Screenshot A", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Unexpected_service_failure_returns_standard_api_error_contract()
    {
        const string correlationId = "overview-failure-correlation";
        await using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IComplianceOverviewRepository>();
                services.AddScoped<IComplianceOverviewRepository, ThrowingComplianceOverviewRepository>();
            });
        });
        using var client = factory.CreateClient();
        using var request = CreateOverviewRequest(
            Guid.Parse("51515151-5151-5151-5151-5151515151a5"),
            Permission.ViewObligations);
        request.Headers.Add("X-Correlation-ID", correlationId);

        var response = await client.SendAsync(request);
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("compliance_overview_unavailable", payload.RootElement.GetProperty("errorCode").GetString());
        Assert.Equal(correlationId, payload.RootElement.GetProperty("correlationId").GetString());
        Assert.True(payload.RootElement.TryGetProperty("traceId", out _));
        Assert.DoesNotContain("Simulated overview failure", payload.RootElement.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Endpoint_does_not_return_500_for_normal_empty_state_scenarios()
    {
        using var client = _factory.CreateClient();
        using var request = CreateOverviewRequest(
            Guid.Parse("51515151-5151-5151-5151-5151515151a6"),
            Permission.ViewObligations);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private WebApplicationFactory<Program> CreatePersistenceFactory(
        string databaseName,
        Action<GccsDbContext>? seed = null) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.RemoveAll<IComplianceOverviewRepository>();
                services.AddScoped<IComplianceOverviewRepository, EfComplianceOverviewRepository>();

                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
                dbContext.Database.EnsureDeleted();
                dbContext.Database.EnsureCreated();
                seed?.Invoke(dbContext);
                dbContext.SaveChanges();
            });
        });

    private static HttpRequestMessage CreateOverviewRequest(Guid tenantId, Permission permission)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/compliance/overview");
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", Guid.Parse("61616161-6161-6161-6161-616161616161").ToString());
        request.Headers.Add("X-Gccs-Dev-Permissions", permission.ToString());
        return request;
    }

    private static AssessmentEntity AssessmentFor(Guid assessmentId, Guid tenantId, AssessmentStatus status) =>
        new()
        {
            Id = assessmentId,
            TenantId = tenantId,
            Name = $"{status} assessment",
            Type = AssessmentType.Readiness,
            Level = CmmcLevel.Level2,
            Framework = "CMMC",
            Status = status,
            StartedAt = DateOnly.Parse("2026-06-01"),
            OwnerFunction = "Security",
            CreatedAt = DateTimeOffset.Parse("2026-06-20T12:00:00Z")
        };

    private static void SeedTenantOverviewData(GccsDbContext dbContext, Guid tenantId, string suffix = "A")
    {
        var assessmentId = Guid.NewGuid();
        var contractId = Guid.NewGuid();
        var contractClauseId = Guid.NewGuid();
        var obligationId = $"overview-risk-{suffix}";
        var overdueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30);
        dbContext.Assessments.Add(new AssessmentEntity
        {
            Id = assessmentId,
            TenantId = tenantId,
            Name = $"Assessment {suffix}",
            Type = AssessmentType.Readiness,
            Level = CmmcLevel.Level2,
            Framework = "CMMC",
            Status = AssessmentStatus.InProgress,
            StartedAt = DateOnly.Parse("2026-06-01"),
            OwnerFunction = "Security",
            CreatedAt = DateTimeOffset.Parse("2026-06-20T12:00:00Z")
        });
        dbContext.Set<ControlAssessmentEntity>().AddRange(
            new ControlAssessmentEntity
            {
                AssessmentId = assessmentId,
                ControlId = $"AC.{suffix}.1",
                ImplementationStatus = ControlImplementationStatus.Implemented,
                Result = AssessmentResult.Met
            },
            new ControlAssessmentEntity
            {
                AssessmentId = assessmentId,
                ControlId = $"AC.{suffix}.2",
                ImplementationStatus = ControlImplementationStatus.PartiallyImplemented,
                Result = AssessmentResult.NotMet
            },
            new ControlAssessmentEntity
            {
                AssessmentId = assessmentId,
                ControlId = $"AC.{suffix}.3",
                ImplementationStatus = ControlImplementationStatus.NotStarted,
                Result = AssessmentResult.NotAssessed
            });
        dbContext.PoamItems.AddRange(
            new PoamItemEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                AssessmentId = assessmentId,
                ControlId = $"AC.{suffix}.2",
                Weakness = "Missing control evidence",
                PlannedRemediation = "Collect evidence",
                RiskLevel = RiskLevel.High,
                Status = PoamStatus.Open,
                OwnerFunction = "Security",
                TargetCompletionAt = overdueDate,
                CreatedAt = DateTimeOffset.Parse("2026-06-20T12:00:00Z")
            },
            new PoamItemEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                AssessmentId = assessmentId,
                ControlId = $"AC.{suffix}.3",
                Weakness = "Implementation incomplete",
                PlannedRemediation = "Finish implementation",
                RiskLevel = RiskLevel.Medium,
                Status = PoamStatus.InProgress,
                OwnerFunction = "Security",
                TargetCompletionAt = futureDate,
                CreatedAt = DateTimeOffset.Parse("2026-06-20T12:00:00Z")
            },
            new PoamItemEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                AssessmentId = assessmentId,
                ControlId = $"AC.{suffix}.4",
                Weakness = "Closed item",
                PlannedRemediation = "Complete",
                RiskLevel = RiskLevel.Low,
                Status = PoamStatus.Closed,
                OwnerFunction = "Security",
                TargetCompletionAt = overdueDate,
                CreatedAt = DateTimeOffset.Parse("2026-06-20T12:00:00Z")
            });
        dbContext.Contracts.Add(new ContractEntity
        {
            Id = contractId,
            TenantId = tenantId,
            ContractNumber = $"W15QKN-26-C-000{suffix}",
            Title = $"Risk contract {suffix}",
            AgencyOrPrimeName = "Department of Defense",
            Relationship = ContractorRelationship.Prime,
            Kind = ContractKind.FixedPrice,
            Status = ContractStatus.Active,
            PeriodOfPerformanceStart = DateOnly.Parse("2026-06-01"),
            PeriodOfPerformanceEnd = DateOnly.Parse("2027-06-01"),
            PlaceOfPerformance = "Arlington, VA",
            Description = "Tenant-scoped risk seed.",
            DataHandlingPosture = DataHandlingPosture.FciOnly,
            CreatedAt = DateTimeOffset.Parse("2026-06-20T12:00:00Z")
        });
        dbContext.Set<ContractClauseEntity>().Add(new ContractClauseEntity
        {
            Id = contractClauseId,
            ContractId = contractId,
            ClauseLibraryId = $"far-risk-{suffix}",
            ClauseNumber = $"52.204-{suffix}",
            Title = $"Risk clause {suffix}",
            Source = ClauseSource.Far,
            SourceUrl = "https://www.acquisition.gov/far",
            AttachmentReason = "Test risk mapping.",
            RequiresFlowDown = false,
            LastReviewedAt = DateOnly.Parse("2026-06-01"),
            ReviewState = ReviewState.Published,
            CreatedAt = DateTimeOffset.Parse("2026-06-20T12:00:00Z")
        });
        dbContext.Set<ObligationEntity>().Add(new ObligationEntity
        {
            Id = obligationId,
            Source = $"FAR risk {suffix}",
            Title = $"High risk obligation {suffix}",
            PlainEnglishSummary = "Test obligation.",
            TriggerCondition = "Contract includes clause.",
            RequiredAction = "Complete high-risk action.",
            OwnerFunction = "Security",
            RiskLevel = RiskLevel.High,
            RequiresFlowDown = false,
            SourceName = "FAR",
            SourceUrl = "https://www.acquisition.gov/far",
            SourceLastReviewedAt = DateOnly.Parse("2026-06-01"),
            LastReviewedAt = DateOnly.Parse("2026-06-01"),
            ReviewState = ReviewState.Published
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
            Title = $"Overdue high risk task {suffix}",
            Description = "High-risk contract task.",
            Type = ComplianceTaskType.ObligationAction,
            Status = ComplianceTaskStatus.Open,
            RiskLevel = RiskLevel.High,
            OwnerFunction = "Security",
            DueAt = overdueDate,
            ContractId = contractId,
            ObligationId = obligationId,
            CreatedAt = DateTimeOffset.Parse("2026-06-20T12:00:00Z")
        });
        dbContext.EvidenceItems.AddRange(
            new EvidenceItemEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = $"Policy {suffix}",
                Type = EvidenceType.Policy,
                OwnerFunction = "Security",
                Status = EvidenceStatus.Approved,
                CreatedAt = DateTimeOffset.Parse("2026-06-20T12:00:00Z")
            },
            new EvidenceItemEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = $"Screenshot {suffix}",
                Type = EvidenceType.Screenshot,
                OwnerFunction = "Security",
                Status = EvidenceStatus.Uploaded,
                CreatedAt = DateTimeOffset.Parse("2026-06-20T12:00:00Z")
            });
        dbContext.AuditLogEntries.Add(new AuditLogEntryEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ActorUserId = Guid.Parse("61616161-6161-6161-6161-616161616161"),
            Action = AuditAction.Uploaded,
            EntityType = "EvidenceItem",
            EntityId = Guid.NewGuid().ToString(),
            OccurredAt = DateTimeOffset.Parse("2026-06-20T12:00:00Z"),
            IpAddress = "127.0.0.1",
            UserAgent = "test",
            CorrelationId = $"overview-{suffix}",
            Summary = suffix == "A" ? "Evidence uploaded" : $"Evidence uploaded {suffix}",
            MetadataJson = "{}"
        });
    }

    private sealed class ThrowingComplianceOverviewRepository : IComplianceOverviewRepository
    {
        public Task<ComplianceOverviewDto> GetCurrentTenantOverviewAsync(CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Simulated overview failure");
    }
}

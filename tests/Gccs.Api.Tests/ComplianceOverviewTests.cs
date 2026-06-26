using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Gccs.Application.Compliance;
using Gccs.Domain.Audit;
using Gccs.Domain.Cmmc;
using Gccs.Domain.Compliance;
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
        Assert.Equal(["Evidence uploaded"], overview.RecentAuditEvents.Select(item => item.Summary).ToArray());
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
        Assert.Empty(overview.RecentAuditEvents);
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
        Assert.DoesNotContain(overview.RecentAuditEvents, item => item.Summary.Contains("B", StringComparison.OrdinalIgnoreCase));
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

    private static void SeedTenantOverviewData(GccsDbContext dbContext, Guid tenantId, string suffix = "A")
    {
        var assessmentId = Guid.NewGuid();
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

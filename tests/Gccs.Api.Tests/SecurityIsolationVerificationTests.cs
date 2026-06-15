using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Calendar;
using Gccs.Application.Cmmc;
using Gccs.Application.Companies;
using Gccs.Application.Compliance;
using Gccs.Application.Contracts;
using Gccs.Application.Evidence;
using Gccs.Application.Identity;
using Gccs.Application.NoCui;
using Gccs.Application.Notifications;
using Gccs.Application.Reports;
using Gccs.Application.Repositories;
using Gccs.Application.Subcontractors;
using Gccs.Application.Tasks;
using Gccs.Application.Tenancy;
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
using Gccs.Infrastructure.Calendar;
using Gccs.Infrastructure.Cmmc;
using Gccs.Infrastructure.Companies;
using Gccs.Infrastructure.Compliance;
using Gccs.Infrastructure.Contracts;
using Gccs.Infrastructure.Evidence;
using Gccs.Infrastructure.NoCui;
using Gccs.Infrastructure.Notifications;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Gccs.Infrastructure.Reports;
using Gccs.Infrastructure.Subcontractors;
using Gccs.Infrastructure.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class SecurityIsolationVerificationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public SecurityIsolationVerificationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_17_2_1_Cross_tenant_access_is_denied_across_tenant_owned_modules()
    {
        var ids = SecurityIds.Create("tc-17-2-1");
        await using var factory = CreateFactory("tc-17-2-1", dbContext => SeedTwoTenantScenario(dbContext, ids));
        using var client = factory.CreateClient();

        await AssertOtherTenantRecordDeniedAsync(client, ids, $"/api/contracts/{ids.TenantAContractId}");
        await AssertOtherTenantRecordDeniedAsync(client, ids, $"/api/tasks/{ids.TenantATaskId}");
        await AssertOtherTenantRecordDeniedAsync(client, ids, $"/api/evidence-items/{ids.TenantAEvidenceId}");
        await AssertOtherTenantRecordDeniedAsync(client, ids, $"/api/cmmc/assessments/{ids.TenantAAssessmentId}");
        await AssertOtherTenantRecordDeniedAsync(client, ids, $"/api/subcontractors/{ids.TenantASubcontractorId}");

        var tenantBProfile = await GetAsync<CompanyProfileDto>(client, "/api/company-profile", ids.TenantBId, ids.TenantBUserId, RoleCatalog.Owner, HttpStatusCode.OK);
        var tenantBContracts = await GetAsync<ContractDto[]>(client, "/api/contracts", ids.TenantBId, ids.TenantBUserId, RoleCatalog.Owner, HttpStatusCode.OK);
        var crossTenantMatrixResponse = await SendAsync<object?>(client, HttpMethod.Get, $"/api/contracts/{ids.TenantAContractId}/obligations", null, ids.TenantBId, ids.TenantBUserId, RoleCatalog.Owner);
        var tenantBCalendar = await GetAsync<CalendarEventDto[]>(client, "/api/calendar/events?from=2026-06-01&to=2026-08-31", ids.TenantBId, ids.TenantBUserId, RoleCatalog.Owner, HttpStatusCode.OK);
        var tenantBReports = await GetAsync<ApprovedEvidencePackageDto[]>(client, "/api/reports/approved-evidence-packages", ids.TenantBId, ids.TenantBUserId, RoleCatalog.Owner, HttpStatusCode.OK);
        var tenantBNotifications = await GetAsync<NotificationCenterItemDto[]>(client, "/api/notifications", ids.TenantBId, ids.TenantBUserId, RoleCatalog.Owner, HttpStatusCode.OK);

        Assert.Equal("Tenant B Federal Services", tenantBProfile.LegalEntityName);
        Assert.DoesNotContain(tenantBContracts, contract => contract.Id == ids.TenantAContractId || contract.ContractNumber == "TENANT-A-001");
        Assert.Equal(HttpStatusCode.NotFound, crossTenantMatrixResponse.StatusCode);
        Assert.DoesNotContain(tenantBCalendar, item => item.RelatedEntityId == ids.TenantATaskId.ToString() || item.ContractId == ids.TenantAContractId);
        Assert.DoesNotContain(tenantBReports, report => report.ReportId == ids.TenantAReportId || report.TenantId == ids.TenantAId);
        Assert.DoesNotContain(tenantBNotifications, notification => notification.TenantId == ids.TenantAId || notification.SourceTaskId == ids.TenantATaskId);
    }

    [Fact]
    public async Task TC_17_2_2_Restricted_role_actions_are_denied_for_direct_api_calls()
    {
        var ids = SecurityIds.Create("tc-17-2-2");
        await using var factory = CreateFactory("tc-17-2-2", dbContext => SeedTwoTenantScenario(dbContext, ids));
        using var client = factory.CreateClient();

        var checks = new[]
        {
            RestrictedCall.Json(
                RoleCatalog.Admin,
                HttpMethod.Post,
                "/api/tenants",
                new CreateTenantRequest("Blocked Admin Tenant")),
            RestrictedCall.Json(
                RoleCatalog.ComplianceManager,
                HttpMethod.Post,
                "/api/tenant-members",
                new AssignTenantMemberRequest(Guid.NewGuid(), "blocked@example.com", "Blocked User", RoleCatalog.Contributor)),
            RestrictedCall.Json(
                RoleCatalog.Contributor,
                HttpMethod.Post,
                "/api/contracts",
                CreateContractRequest("BLOCKED-CONTRIBUTOR")),
            RestrictedCall.Json(
                RoleCatalog.Auditor,
                HttpMethod.Post,
                "/api/tasks",
                new CreateComplianceTaskRequest("Blocked auditor task", "Auditor cannot create work.", "open", RiskLevel.Low, null, "Audit", null, "general", null)),
            RestrictedCall.Json(
                RoleCatalog.Advisor,
                HttpMethod.Put,
                "/api/company-profile",
                CreateCompanyProfileRequest("Blocked Advisor Company"))
        };

        foreach (var check in checks)
        {
            using var request = CreateRequest(check.Method, check.Path, check.Body, ids.TenantAId, ids.TenantAUserId, check.RoleName);
            var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            Assert.Contains("permission_denied", body, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void TC_17_2_3_Tenant_owned_repository_and_service_filter_tests_are_present()
    {
        var root = FindRepositoryRoot();
        var expectedCoverage = new Dictionary<string, string[]>
        {
            ["CompanyProfileTests.cs"] = ["TC_7_1_4_Profile_changes_are_audited_and_tenant_scoped"],
            ["ContractRecordTests.cs"] = ["TC_8_1_2_Contract_list_is_tenant_scoped"],
            ["ObligationDashboardTests.cs"] = ["TC_10_1_1_Dashboard_returns_current_tenant_obligations_only"],
            ["ComplianceTaskManagementTests.cs"] =
            [
                "TC_11_1_3_Task_updates_are_tenant_scoped",
                "TC_11_1_4_Task_status_changes_are_audit_logged"
            ],
            ["CalendarViewTests.cs"] = ["TC_11_2_4_Calendar_excludes_other_tenant_items"],
            ["EvidenceMetadataTests.cs"] = ["TC_12_1_3_Filters_evidence_by_folderless_tags"],
            ["CmmcAssessmentTests.cs"] = ["TC_13_1_2_Links_assessment_to_company_profile_and_contracts_for_detail_display"],
            ["SubcontractorProfileTests.cs"] = ["TC_14_1_4_Cross_tenant_access_is_denied_and_changes_are_audit_logged"],
            ["ComplianceStatusReportTests.cs"] = ["TC_15_1_2_Compliance_status_report_excludes_other_tenant_data"],
            ["AssignmentNotificationTests.cs"] = ["TC_16_3_4_Unauthorized_user_cannot_open_assignment_notification_link"]
        };

        foreach (var (fileName, signals) in expectedCoverage)
        {
            var source = File.ReadAllText(Path.Combine(root, "tests", "Gccs.Api.Tests", fileName));
            Assert.All(signals, signal => Assert.Contains(signal, source, StringComparison.Ordinal));
        }
    }

    [Fact]
    public void TC_17_2_4_Security_verification_results_are_documented()
    {
        var artifact = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "artifacts",
            "test-results",
            "tc-17.2-security-and-tenant-isolation-verification.md"));

        Assert.Contains("Cross-tenant API access", artifact, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("server-side RBAC", artifact, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("tenant-owned query filtering", artifact, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("audit logging", artifact, StringComparison.OrdinalIgnoreCase);
    }

    private WebApplicationFactory<Program> CreateFactory(string databaseName, Action<GccsDbContext>? seed = null) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                services.AddScoped<CompanyProfileService>();
                services.AddScoped<ICompanyProfileRepository, EfCompanyProfileRepository>();
                services.AddScoped<ContractService>();
                services.AddScoped<IContractRepository, EfContractRepository>();
                services.AddScoped<IContractObligationMatrixRepository, EfContractObligationMatrixRepository>();
                services.AddScoped<IObligationDashboardRepository, EfObligationDashboardRepository>();
                services.AddScoped<ComplianceTaskService>();
                services.AddScoped<IComplianceTaskRepository, EfComplianceTaskRepository>();
                services.AddScoped<ICalendarRepository, EfCalendarRepository>();
                services.AddScoped<EvidenceMetadataService>();
                services.AddScoped<IEvidenceMetadataRepository, EfEvidenceMetadataRepository>();
                services.AddScoped<NoCuiAcknowledgementService>();
                services.AddScoped<INoCuiAcknowledgementRepository, EfNoCuiAcknowledgementRepository>();
                services.AddScoped<CmmcAssessmentService>();
                services.AddScoped<ICmmcAssessmentRepository, EfCmmcAssessmentRepository>();
                services.AddScoped<SubcontractorService>();
                services.AddScoped<ISubcontractorRepository, EfSubcontractorRepository>();
                services.AddScoped<AssignmentNotificationService>();
                services.AddScoped<IAssignmentNotificationRepository, EfAssignmentNotificationRepository>();
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

    private static async Task AssertOtherTenantRecordDeniedAsync(HttpClient client, SecurityIds ids, string requestUri)
    {
        using var request = CreateRequest<object?>(HttpMethod.Get, requestUri, null, ids.TenantBId, ids.TenantBUserId, RoleCatalog.Owner);
        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain("Tenant A", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("TENANT-A-001", body, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<TResponse> GetAsync<TResponse>(
        HttpClient client,
        string requestUri,
        Guid tenantId,
        Guid userId,
        string roleName,
        HttpStatusCode expectedStatus)
    {
        using var request = CreateRequest<object?>(HttpMethod.Get, requestUri, null, tenantId, userId, roleName);
        var response = await client.SendAsync(request);
        Assert.Equal(expectedStatus, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions) ??
            throw new InvalidOperationException($"Expected response body from {requestUri}.");
    }

    private static async Task<HttpResponseMessage> SendAsync<TRequest>(
        HttpClient client,
        HttpMethod method,
        string requestUri,
        TRequest body,
        Guid tenantId,
        Guid userId,
        string roleName)
    {
        using var request = CreateRequest(method, requestUri, body, tenantId, userId, roleName);
        return await client.SendAsync(request);
    }

    private static HttpRequestMessage CreateRequest<TContent>(
        HttpMethod method,
        string requestUri,
        TContent content,
        Guid tenantId,
        Guid userId,
        string roleName)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", userId.ToString());
        request.Headers.Add("X-Gccs-Dev-Role", roleName);
        if (content is not null)
        {
            request.Content = JsonContent.Create(content, options: JsonOptions);
        }

        return request;
    }

    private static UpsertCompanyProfileRequest CreateCompanyProfileRequest(string legalName) =>
        new(
            legalName,
            null,
            "SECURITY1234",
            "9S3C1",
            new DateOnly(2027, 6, 15),
            [new CompanyNaicsCodeDto("541519", "Other Computer Related Services", true, "$34M", true, new DateOnly(2026, 6, 15))],
            [],
            ["Department of Defense"],
            ContractorRole.Subcontractor,
            "Security verification services.",
            CompanyRange.Small,
            CompanyRange.Small,
            [new CompanyLocationDto("HQ", "100 Security Way", null, "Arlington", "VA", "22201", "US", true)],
            new ItEnvironmentSummaryDto("No-CUI test environment.", false, null, ["M365"]),
            DataHandlingPosture.FciOnly,
            true);

    private static UpsertContractRequest CreateContractRequest(string contractNumber) =>
        new(
            contractNumber,
            "Security boundary contract",
            "Prime Integrator",
            ContractorRelationship.Subcontractor,
            ContractKind.FixedPrice,
            ContractStatus.Active,
            new DateOnly(2026, 6, 15),
            new DateOnly(2026, 7, 1),
            new DateOnly(2027, 6, 30),
            "Arlington, VA",
            "No-CUI security verification contract.",
            DataHandlingPosture.FciOnly);

    private static void SeedTwoTenantScenario(GccsDbContext dbContext, SecurityIds ids)
    {
        dbContext.Tenants.AddRange(CreateTenant(ids.TenantAId, "Tenant A"), CreateTenant(ids.TenantBId, "Tenant B"));
        dbContext.Controls.Add(CreateControl());
        dbContext.Clauses.Add(CreateClause());
        dbContext.Obligations.Add(CreateObligation());
        SeedTenantOwnedData(dbContext, ids.TenantAId, ids.TenantAUserId, ids.TenantAContractId, ids.TenantAContractClauseId, ids.TenantATaskId, ids.TenantAEvidenceId, ids.TenantAAssessmentId, ids.TenantASubcontractorId, ids.TenantAReportId, "Tenant A", "TENANT-A-001");
        SeedTenantOwnedData(dbContext, ids.TenantBId, ids.TenantBUserId, ids.TenantBContractId, ids.TenantBContractClauseId, ids.TenantBTaskId, ids.TenantBEvidenceId, ids.TenantBAssessmentId, ids.TenantBSubcontractorId, ids.TenantBReportId, "Tenant B", "TENANT-B-001");
    }

    private static void SeedTenantOwnedData(
        GccsDbContext dbContext,
        Guid tenantId,
        Guid userId,
        Guid contractId,
        Guid contractClauseId,
        Guid taskId,
        Guid evidenceId,
        Guid assessmentId,
        Guid subcontractorId,
        Guid reportId,
        string tenantName,
        string contractNumber)
    {
        dbContext.CompanyProfiles.Add(CreateCompanyProfile(tenantId, tenantName));
        dbContext.Contracts.Add(CreateContract(tenantId, contractId, contractNumber, $"{tenantName} contract"));
        dbContext.Set<ContractClauseEntity>().Add(CreateContractClause(contractId, contractClauseId));
        dbContext.Set<ContractClauseObligationEntity>().Add(new ContractClauseObligationEntity
        {
            ContractClauseId = contractClauseId,
            ObligationId = "obligation-fci-safeguards"
        });
        dbContext.ComplianceTasks.Add(new ComplianceTaskEntity
        {
            Id = taskId,
            TenantId = tenantId,
            ContractId = contractId,
            ObligationId = "obligation-fci-safeguards",
            EvidenceItemId = evidenceId,
            Title = $"{tenantName} security task",
            Description = "Tenant-owned security task.",
            Type = ComplianceTaskType.EvidenceRequest,
            Status = ComplianceTaskStatus.Open,
            RiskLevel = RiskLevel.Medium,
            AssignedToUserId = userId,
            OwnerFunction = "Security",
            DueAt = new DateOnly(2026, 7, 15),
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.EvidenceItems.Add(new EvidenceItemEntity
        {
            Id = evidenceId,
            TenantId = tenantId,
            Name = $"{tenantName} access policy",
            Description = "Tenant-owned evidence.",
            Type = EvidenceType.Policy,
            OwnerFunction = "Security",
            Status = EvidenceStatus.Approved,
            TagsJson = """["security"]""",
            ApprovedAt = DateTimeOffset.UtcNow,
            ApprovedByUserId = userId,
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.Set<EvidenceObligationEntity>().Add(new EvidenceObligationEntity { EvidenceItemId = evidenceId, ObligationId = "obligation-fci-safeguards" });
        dbContext.Set<EvidenceContractEntity>().Add(new EvidenceContractEntity { EvidenceItemId = evidenceId, ContractId = contractId });
        dbContext.Set<EvidenceControlEntity>().Add(new EvidenceControlEntity { EvidenceItemId = evidenceId, ControlId = "AC.L1-3.1.1" });
        dbContext.Assessments.Add(new AssessmentEntity
        {
            Id = assessmentId,
            TenantId = tenantId,
            Name = $"{tenantName} CMMC assessment",
            Type = AssessmentType.Readiness,
            Level = CmmcLevel.Level1,
            Framework = "CMMC 2.0 / FAR 52.204-21",
            Status = AssessmentStatus.InProgress,
            StartedAt = new DateOnly(2026, 6, 15),
            OwnerFunction = "Security",
            ContractIdsJson = JsonSerializer.Serialize(new[] { contractId }),
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.Subcontractors.Add(new SubcontractorEntity
        {
            Id = subcontractorId,
            TenantId = tenantId,
            Name = $"{tenantName} Subcontractor",
            Status = SubcontractorStatus.Active,
            RoleDescription = "Security support.",
            SmallBusinessStatus = "Small business",
            CmmcStatus = "Level 1 in progress",
            NdaStatus = "Signed",
            WorkshareDescription = "Support only.",
            HasFciAccess = true,
            HasCuiAccess = false,
            HasExportControlledAccess = false,
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.Set<ContractSubcontractorEntity>().Add(new ContractSubcontractorEntity
        {
            ContractId = contractId,
            SubcontractorId = subcontractorId
        });
        dbContext.Reports.Add(new ReportEntity
        {
            Id = reportId,
            TenantId = tenantId,
            Type = ReportType.PrimeEvidencePackage,
            Title = $"{tenantName} approved package",
            Status = ReportStatus.Complete,
            GeneratedAt = DateTimeOffset.UtcNow,
            GeneratedByUserId = userId,
            SnapshotJson = "{}",
            ExportHtml = "<html>approved package</html>",
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.Set<ReportEvidenceEntity>().Add(new ReportEvidenceEntity { ReportId = reportId, EvidenceItemId = evidenceId });
        dbContext.NotificationDeliveries.Add(new NotificationDeliveryEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            SourceTaskId = taskId,
            SourceType = "ComplianceTask",
            LinkUrl = $"/tasks/{taskId}",
            Category = "assignment",
            Status = "Delivered",
            Placeholder = $"{tenantName} assignment notification",
            AttemptedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    private static TenantEntity CreateTenant(Guid tenantId, string name) =>
        new()
        {
            Id = tenantId,
            Name = name,
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.UtcNow
        };

    private static CompanyProfileEntity CreateCompanyProfile(Guid tenantId, string tenantName) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            LegalEntityName = $"{tenantName} Federal Services",
            Uei = $"SECURITY{tenantName[^1]}123",
            CageCode = $"9S3C{tenantName[^1]}",
            SamRegistrationExpiresAt = new DateOnly(2027, 6, 15),
            ContractorRole = ContractorRole.Subcontractor,
            ProductsAndServices = "Security verification services.",
            EmployeeRange = CompanyRange.Small,
            RevenueRange = CompanyRange.Small,
            ItEnvironmentDescription = "No-CUI test environment.",
            KeySystemsJson = """["M365"]""",
            AgencyCustomersJson = """["Department of Defense"]""",
            DataHandlingPosture = DataHandlingPosture.FciOnly,
            CreatedAt = DateTimeOffset.UtcNow
        };

    private static ContractEntity CreateContract(Guid tenantId, Guid contractId, string contractNumber, string title) =>
        new()
        {
            Id = contractId,
            TenantId = tenantId,
            ContractNumber = contractNumber,
            Title = title,
            AgencyOrPrimeName = "Prime Integrator",
            Relationship = ContractorRelationship.Subcontractor,
            Kind = ContractKind.FixedPrice,
            Status = ContractStatus.Active,
            AwardedAt = new DateOnly(2026, 6, 15),
            PeriodOfPerformanceStart = new DateOnly(2026, 7, 1),
            PeriodOfPerformanceEnd = new DateOnly(2027, 6, 30),
            PlaceOfPerformance = "Arlington, VA",
            Description = "No-CUI security verification contract.",
            DataHandlingPosture = DataHandlingPosture.FciOnly,
            CreatedAt = DateTimeOffset.UtcNow
        };

    private static ContractClauseEntity CreateContractClause(Guid contractId, Guid contractClauseId) =>
        new()
        {
            Id = contractClauseId,
            ContractId = contractId,
            ClauseLibraryId = "far-52-204-21",
            ClauseNumber = "52.204-21",
            Title = "Basic Safeguarding",
            Source = ClauseSource.Far,
            SourceUrl = "https://www.acquisition.gov/far/52.204-21",
            AttachmentReason = "Security verification clause.",
            RequiresFlowDown = true,
            LastReviewedAt = new DateOnly(2026, 6, 3),
            Confidence = "high",
            ReviewState = ReviewState.Approved,
            CreatedAt = DateTimeOffset.UtcNow
        };

    private static ClauseEntity CreateClause() =>
        new()
        {
            Id = "far-52-204-21",
            Source = "FAR 52.204-21",
            Number = "52.204-21",
            Title = "Basic Safeguarding",
            PlainEnglishSummary = "Protect FCI.",
            ApplicabilityLogic = "Contract involves FCI.",
            ClauseTextVersion = "current",
            RequiredActionIdsJson = """["obligation-fci-safeguards"]""",
            UsuallyRequiresFlowDown = true,
            SourceName = "FAR 52.204-21",
            SourceUrl = "https://www.acquisition.gov/far/52.204-21",
            SourceLastReviewedAt = new DateOnly(2026, 6, 3),
            SourceConfidence = "high",
            LastReviewedAt = new DateOnly(2026, 6, 3),
            Confidence = "high",
            ReviewState = ReviewState.Published
        };

    private static ObligationEntity CreateObligation() =>
        new()
        {
            Id = "obligation-fci-safeguards",
            Source = "FAR 52.204-21",
            Title = "Apply FCI safeguards",
            PlainEnglishSummary = "Apply basic safeguarding controls to systems that handle FCI.",
            TriggerCondition = "Contract involves FCI.",
            RequiredAction = "Apply basic safeguarding controls.",
            OwnerFunction = "IT/security",
            RiskLevel = RiskLevel.High,
            RequiresFlowDown = true,
            FlowDownRequirement = "Flow down to subcontractors handling FCI.",
            EvidenceExamplesJson = """["Access control policy"]""",
            SourceName = "FAR 52.204-21",
            SourceUrl = "https://www.acquisition.gov/far/52.204-21",
            SourceLastReviewedAt = new DateOnly(2026, 6, 3),
            SourceConfidence = "high",
            LastReviewedAt = new DateOnly(2026, 6, 3),
            Confidence = "high",
            ReviewState = ReviewState.Published
        };

    private static ControlEntity CreateControl() =>
        new()
        {
            Id = "AC.L1-3.1.1",
            Framework = ControlFramework.Cmmc,
            CmmcLevel = CmmcLevel.Level1,
            Family = "Access Control",
            Title = "Limit system access",
            Requirement = "Limit information system access to authorized users.",
            AssessmentObjective = "Verify authorized user access.",
            SourceName = "FAR 52.204-21 / CMMC Level 1",
            SourceUrl = "https://dodcio.defense.gov/CMMC/Resources-Documentation/",
            SourceLastReviewedAt = new DateOnly(2026, 6, 15),
            SourceConfidence = "high"
        };

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Gccs.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }

    private sealed record RestrictedCall(string RoleName, HttpMethod Method, string Path, object Body)
    {
        public static RestrictedCall Json(string roleName, HttpMethod method, string path, object body) =>
            new(roleName, method, path, body);
    }

    private sealed record SecurityIds(
        Guid TenantAId,
        Guid TenantBId,
        Guid TenantAUserId,
        Guid TenantBUserId,
        Guid TenantAContractId,
        Guid TenantBContractId,
        Guid TenantAContractClauseId,
        Guid TenantBContractClauseId,
        Guid TenantATaskId,
        Guid TenantBTaskId,
        Guid TenantAEvidenceId,
        Guid TenantBEvidenceId,
        Guid TenantAAssessmentId,
        Guid TenantBAssessmentId,
        Guid TenantASubcontractorId,
        Guid TenantBSubcontractorId,
        Guid TenantAReportId,
        Guid TenantBReportId)
    {
        public static SecurityIds Create(string caseName)
        {
            var suffix = Math.Abs(caseName.GetHashCode(StringComparison.Ordinal)) % 10000;
            return new SecurityIds(
                Guid.Parse($"17217217-2172-1721-7217-21721710{suffix:D4}"),
                Guid.Parse($"17217217-2172-1721-7217-21721711{suffix:D4}"),
                Guid.Parse($"17217217-2172-1721-7217-21721712{suffix:D4}"),
                Guid.Parse($"17217217-2172-1721-7217-21721713{suffix:D4}"),
                Guid.Parse($"17217217-2172-1721-7217-21721714{suffix:D4}"),
                Guid.Parse($"17217217-2172-1721-7217-21721715{suffix:D4}"),
                Guid.Parse($"17217217-2172-1721-7217-21721716{suffix:D4}"),
                Guid.Parse($"17217217-2172-1721-7217-21721717{suffix:D4}"),
                Guid.Parse($"17217217-2172-1721-7217-21721718{suffix:D4}"),
                Guid.Parse($"17217217-2172-1721-7217-21721719{suffix:D4}"),
                Guid.Parse($"17217217-2172-1721-7217-21721720{suffix:D4}"),
                Guid.Parse($"17217217-2172-1721-7217-21721721{suffix:D4}"),
                Guid.Parse($"17217217-2172-1721-7217-21721722{suffix:D4}"),
                Guid.Parse($"17217217-2172-1721-7217-21721723{suffix:D4}"),
                Guid.Parse($"17217217-2172-1721-7217-21721724{suffix:D4}"),
                Guid.Parse($"17217217-2172-1721-7217-21721725{suffix:D4}"),
                Guid.Parse($"17217217-2172-1721-7217-21721726{suffix:D4}"),
                Guid.Parse($"17217217-2172-1721-7217-21721727{suffix:D4}"));
        }
    }
}

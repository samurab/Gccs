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
using Gccs.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class PilotWorkflowTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly WebApplicationFactory<Program> _factory;

    public PilotWorkflowTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TC_17_1_1_Non_cui_pilot_tenant_completes_core_mvp_workflow()
    {
        var ids = PilotIds.Create("tc-17-1-1");
        await using var factory = CreateFactory("tc-17-1-1", dbContext => SeedPilotFoundation(dbContext, ids));
        using var client = factory.CreateClient();

        var profile = await PutAsync<UpsertCompanyProfileRequest, CompanyProfileDto>(
            client,
            "/api/company-profile",
            CreateCompanyProfileRequest(),
            ids,
            ids.AdminUserId,
            RoleCatalog.Admin,
            HttpStatusCode.OK);
        var contract = await PostAsync<UpsertContractRequest, ContractDto>(
            client,
            "/api/contracts",
            CreateContractRequest(),
            ids,
            ids.AdminUserId,
            RoleCatalog.Admin,
            HttpStatusCode.Created);
        var clause = await PostAsync<AttachContractClauseRequest, ContractClauseDto>(
            client,
            $"/api/contracts/{contract.Id}/clauses",
            new AttachContractClauseRequest("far-52-204-21", "Pilot contract handles FCI.", "pilot-contract.pdf"),
            ids,
            ids.AdminUserId,
            RoleCatalog.Admin,
            HttpStatusCode.Created);
        var obligations = await GetAsync<ObligationDashboardItemDto[]>(
            client,
            $"/api/contract-obligations?contractId={contract.Id}",
            ids,
            ids.ComplianceManagerUserId,
            RoleCatalog.ComplianceManager,
            HttpStatusCode.OK);
        var evidence = await PostAsync<UpsertEvidenceMetadataRequest, EvidenceMetadataDto>(
            client,
            "/api/evidence-items",
            CreateEvidenceRequest(contract.Id),
            ids,
            ids.ContributorUserId,
            RoleCatalog.Contributor,
            HttpStatusCode.Created);
        var uploadIntent = await PostAsync<EvidenceUploadIntentRequest, EvidenceUploadIntentDto>(
            client,
            $"/api/evidence-items/{evidence.Id}/upload-intents",
            new EvidenceUploadIntentRequest("access-control-policy.pdf", "application/pdf", 42_000),
            ids,
            ids.ContributorUserId,
            RoleCatalog.Contributor,
            HttpStatusCode.Created);
        var assessment = await PostAsync<UpsertCmmcAssessmentRequest, CmmcAssessmentDto>(
            client,
            "/api/cmmc/assessments",
            CreateAssessmentRequest(profile.Id, contract.Id),
            ids,
            ids.ComplianceManagerUserId,
            RoleCatalog.ComplianceManager,
            HttpStatusCode.Created);
        var control = await PatchAsync<UpsertCmmcControlStatusRequest, CmmcControlStatusDto>(
            client,
            $"/api/cmmc/assessments/{assessment.Id}/controls/AC.L1-3.1.1",
            new UpsertCmmcControlStatusRequest(
                ControlImplementationStatus.Implemented,
                AssessmentResult.Met,
                [evidence.Id],
                [],
                [],
                [],
                ids.ComplianceManagerUserId,
                new DateOnly(2026, 6, 15),
                "Pilot evidence reviewed."),
            ids,
            ids.ComplianceManagerUserId,
            RoleCatalog.ComplianceManager,
            HttpStatusCode.OK);
        var poam = await PostAsync<UpsertCmmcPoamItemRequest, CmmcPoamItemDto>(
            client,
            $"/api/cmmc/assessments/{assessment.Id}/poam-items",
            new UpsertCmmcPoamItemRequest(
                "AC.L1-3.1.1",
                "Formalize quarterly access review evidence.",
                "Add recurring access review task and evidence reminder.",
                RiskLevel.Medium,
                PoamStatus.Open,
                ids.ComplianceManagerUserId,
                "Security",
                new DateOnly(2026, 8, 15),
                null,
                null,
                [evidence.Id]),
            ids,
            ids.ComplianceManagerUserId,
            RoleCatalog.ComplianceManager,
            HttpStatusCode.Created);
        var subcontractor = await PostAsync<UpsertSubcontractorRequest, SubcontractorDto>(
            client,
            "/api/subcontractors",
            CreateSubcontractorRequest(contract.Id),
            ids,
            ids.ComplianceManagerUserId,
            RoleCatalog.ComplianceManager,
            HttpStatusCode.Created);
        var flowDown = await PostAsync<UpsertSubcontractorFlowDownRequest, SubcontractorFlowDownDto>(
            client,
            $"/api/subcontractors/{subcontractor.Id}/flow-downs",
            new UpsertSubcontractorFlowDownRequest(
                contract.Id,
                clause.Id,
                "obligation-fci-safeguards",
                "52.204-21",
                "Basic Safeguarding",
                FlowDownStatus.Sent,
                new DateOnly(2026, 6, 15),
                null,
                null,
                null,
                null),
            ids,
            ids.ComplianceManagerUserId,
            RoleCatalog.ComplianceManager,
            HttpStatusCode.Created);
        var assignedTask = await PostAsync<CreateComplianceTaskRequest, ComplianceTaskDto>(
            client,
            "/api/tasks",
            new CreateComplianceTaskRequest(
                "Pilot evidence follow-up",
                "Verify the access-control evidence package is ready for prime review.",
                "open",
                RiskLevel.Medium,
                ids.ContributorUserId,
                "Security",
                new DateOnly(2026, 7, 15),
                "evidence",
                evidence.Id.ToString()),
            ids,
            ids.ComplianceManagerUserId,
            RoleCatalog.ComplianceManager,
            HttpStatusCode.Created);
        var calendar = await GetAsync<CalendarEventDto[]>(
            client,
            "/api/calendar/events?from=2026-06-01&to=2026-08-31",
            ids,
            ids.ComplianceManagerUserId,
            RoleCatalog.ComplianceManager,
            HttpStatusCode.OK);
        var notifications = await GetAsync<NotificationCenterItemDto[]>(
            client,
            "/api/notifications",
            ids,
            ids.ContributorUserId,
            RoleCatalog.Contributor,
            HttpStatusCode.OK);

        Assert.True(profile.IsComplete);
        Assert.Equal("PILOT-2026-001", contract.ContractNumber);
        Assert.Equal("52.204-21", clause.ClauseNumber);
        Assert.Contains(obligations, obligation => obligation.ObligationId == "obligation-fci-safeguards");
        Assert.Equal(evidence.Id, uploadIntent.EvidenceItemId);
        Assert.Equal(ControlImplementationStatus.Implemented, control.Status);
        Assert.Equal("AC.L1-3.1.1", poam.ControlId);
        Assert.Equal([contract.Id], subcontractor.ContractIds);
        Assert.Equal(FlowDownStatus.Sent, flowDown.Status);
        Assert.Contains(calendar, item => item.Title == assignedTask.Title || item.ContractId == contract.Id);
        Assert.Contains(notifications, notification => notification.SourceTaskId == assignedTask.Id && notification.UserId == ids.ContributorUserId);
    }

    [Fact]
    public async Task TC_17_1_2_Pilot_roles_can_only_perform_permitted_actions()
    {
        var ids = PilotIds.Create("tc-17-1-2");
        await using var factory = CreateFactory("tc-17-1-2", dbContext => SeedPilotFoundation(dbContext, ids));
        using var client = factory.CreateClient();

        var ownerReport = await SendAsync<object?>(client, HttpMethod.Post, "/api/reports/compliance-status", null, ids, ids.OwnerUserId, RoleCatalog.Owner);
        var adminProfile = await SendAsync(client, HttpMethod.Put, "/api/company-profile", CreateCompanyProfileRequest(), ids, ids.AdminUserId, RoleCatalog.Admin);
        var managerTask = await SendAsync(
            client,
            HttpMethod.Post,
            "/api/tasks",
            new CreateComplianceTaskRequest("Role matrix task", "Allowed for compliance manager.", "open", RiskLevel.Low, null, "Contracts", null, "general", null),
            ids,
            ids.ComplianceManagerUserId,
            RoleCatalog.ComplianceManager);
        var contributorEvidence = await SendAsync(client, HttpMethod.Post, "/api/evidence-items", CreateEvidenceRequest(null), ids, ids.ContributorUserId, RoleCatalog.Contributor);
        var auditorPackages = await SendAsync<object?>(client, HttpMethod.Get, "/api/reports/approved-evidence-packages", null, ids, ids.AuditorUserId, RoleCatalog.Auditor);
        var auditorTaskWrite = await SendAsync(
            client,
            HttpMethod.Post,
            "/api/tasks",
            new CreateComplianceTaskRequest("Blocked auditor task", "Auditor cannot create tasks.", "open", RiskLevel.Low, null, "Audit", null, "general", null),
            ids,
            ids.AuditorUserId,
            RoleCatalog.Auditor);
        var advisorReport = await SendAsync<object?>(client, HttpMethod.Post, "/api/reports/compliance-status", null, ids, ids.AdvisorUserId, RoleCatalog.Advisor);
        var advisorTenantCreate = await SendAsync(client, HttpMethod.Post, "/api/tenants", new CreateTenantRequest("Blocked Advisor Tenant"), ids, ids.AdvisorUserId, RoleCatalog.Advisor);

        Assert.Equal(HttpStatusCode.Created, ownerReport.StatusCode);
        Assert.Equal(HttpStatusCode.OK, adminProfile.StatusCode);
        Assert.Equal(HttpStatusCode.Created, managerTask.StatusCode);
        Assert.Equal(HttpStatusCode.Created, contributorEvidence.StatusCode);
        Assert.Equal(HttpStatusCode.OK, auditorPackages.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, auditorTaskWrite.StatusCode);
        Assert.Equal(HttpStatusCode.Created, advisorReport.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, advisorTenantCreate.StatusCode);
    }

    [Fact]
    public async Task TC_17_1_3_Pilot_reports_reflect_workflow_data()
    {
        var ids = PilotIds.Create("tc-17-1-3");
        await using var factory = CreateFactory("tc-17-1-3", dbContext => SeedPilotDataForReports(dbContext, ids));
        using var client = factory.CreateClient();

        var statusReport = await PostAsync<object?, ComplianceStatusReportDto>(
            client,
            "/api/reports/compliance-status",
            null,
            ids,
            ids.AdvisorUserId,
            RoleCatalog.Advisor,
            HttpStatusCode.Created);
        var matrix = await GetAsync<ContractObligationMatrixRowDto[]>(
            client,
            $"/api/contracts/{ids.ContractId}/obligations",
            ids,
            ids.AdvisorUserId,
            RoleCatalog.Advisor,
            HttpStatusCode.OK);
        var evidencePackage = await PostAsync<EvidencePackageGenerateRequest, EvidencePackageReportDto>(
            client,
            "/api/reports/evidence-packages",
            new EvidencePackageGenerateRequest
            {
                Title = "Pilot prime review package",
                ObligationIds = ["obligation-fci-safeguards"],
                ContractIds = [ids.ContractId],
                ControlIds = ["AC.L1-3.1.1"],
                SubcontractorIds = [ids.SubcontractorId]
            },
            ids,
            ids.AdvisorUserId,
            RoleCatalog.Advisor,
            HttpStatusCode.Created);

        Assert.Equal(ReportType.ComplianceStatus, statusReport.Type);
        Assert.True(statusReport.Snapshot.TotalObligations >= 1);
        Assert.Contains(statusReport.Snapshot.HighRiskItems, item => item.Contains("Apply FCI safeguards", StringComparison.Ordinal));
        Assert.Contains(matrix, row => row.ContractId == ids.ContractId && row.ObligationId == "obligation-fci-safeguards");
        Assert.Contains(evidencePackage.Manifest.Items, item => item.EvidenceItemId == ids.EvidenceItemId);
        Assert.Contains("Pilot prime review package", evidencePackage.ExportHtml, StringComparison.Ordinal);
    }

    [Fact]
    public void TC_17_1_4_Automated_regression_coverage_exists_for_pilot_workflow_critical_path()
    {
        var testSource = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "tests", "Gccs.Api.Tests", "PilotWorkflowTests.cs"));

        Assert.Contains("TC_17_1_1_Non_cui_pilot_tenant_completes_core_mvp_workflow", testSource);
        Assert.Contains("TC_17_1_2_Pilot_roles_can_only_perform_permitted_actions", testSource);
        Assert.Contains("TC_17_1_3_Pilot_reports_reflect_workflow_data", testSource);
        Assert.Contains("TC_17_1_4_Automated_regression_coverage_exists_for_pilot_workflow_critical_path", testSource);
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
                services.AddScoped<IObligationDashboardRepository, EfObligationDashboardRepository>();
                services.AddScoped<IObligationDetailRepository, EfObligationDetailRepository>();
                services.AddScoped<IObligationRepository, EfObligationRepository>();
                services.AddScoped<IContractObligationMatrixRepository, EfContractObligationMatrixRepository>();
                services.AddScoped<ComplianceTaskService>();
                services.AddScoped<IComplianceTaskRepository, EfComplianceTaskRepository>();
                services.AddScoped<ICalendarRepository, EfCalendarRepository>();
                services.AddScoped<EvidenceMetadataService>();
                services.AddScoped<IEvidenceMetadataRepository, EfEvidenceMetadataRepository>();
                services.AddScoped<NoCuiAcknowledgementService>();
                services.AddScoped<INoCuiAcknowledgementRepository, EfNoCuiAcknowledgementRepository>();
                services.AddScoped<CmmcAssessmentService>();
                services.AddScoped<CmmcPoamService>();
                services.AddScoped<ICmmcAssessmentRepository, EfCmmcAssessmentRepository>();
                services.AddScoped<ICmmcPoamRepository, EfCmmcPoamRepository>();
                services.AddScoped<SubcontractorService>();
                services.AddScoped<ISubcontractorRepository, EfSubcontractorRepository>();
                services.AddScoped<AssignmentNotificationService>();
                services.AddScoped<IAssignmentNotificationRepository, EfAssignmentNotificationRepository>();
                services.AddScoped<ComplianceStatusReportService>();
                services.AddScoped<EvidencePackageReportService>();
                services.AddScoped<IReportRepository, EfReportRepository>();
                services.AddScoped<ITenantRepository, EfTenantRepository>();
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

    private static async Task<TResponse> GetAsync<TResponse>(
        HttpClient client,
        string requestUri,
        PilotIds ids,
        Guid userId,
        string roleName,
        HttpStatusCode expectedStatus)
    {
        using var request = CreateRequest<object?>(HttpMethod.Get, requestUri, null, ids, userId, roleName);
        var response = await client.SendAsync(request);
        Assert.Equal(expectedStatus, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions) ??
            throw new InvalidOperationException($"Expected response body from {requestUri}.");
    }

    private static async Task<TResponse> PostAsync<TRequest, TResponse>(
        HttpClient client,
        string requestUri,
        TRequest body,
        PilotIds ids,
        Guid userId,
        string roleName,
        HttpStatusCode expectedStatus)
    {
        using var request = CreateRequest(HttpMethod.Post, requestUri, body, ids, userId, roleName);
        var response = await client.SendAsync(request);
        Assert.Equal(expectedStatus, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions) ??
            throw new InvalidOperationException($"Expected response body from {requestUri}.");
    }

    private static async Task<TResponse> PutAsync<TRequest, TResponse>(
        HttpClient client,
        string requestUri,
        TRequest body,
        PilotIds ids,
        Guid userId,
        string roleName,
        HttpStatusCode expectedStatus)
    {
        using var request = CreateRequest(HttpMethod.Put, requestUri, body, ids, userId, roleName);
        var response = await client.SendAsync(request);
        Assert.Equal(expectedStatus, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions) ??
            throw new InvalidOperationException($"Expected response body from {requestUri}.");
    }

    private static async Task<TResponse> PatchAsync<TRequest, TResponse>(
        HttpClient client,
        string requestUri,
        TRequest body,
        PilotIds ids,
        Guid userId,
        string roleName,
        HttpStatusCode expectedStatus)
    {
        using var request = CreateRequest(HttpMethod.Patch, requestUri, body, ids, userId, roleName);
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
        PilotIds ids,
        Guid userId,
        string roleName)
    {
        using var request = CreateRequest(method, requestUri, body, ids, userId, roleName);
        return await client.SendAsync(request);
    }

    private static HttpRequestMessage CreateRequest<TContent>(
        HttpMethod method,
        string requestUri,
        TContent content,
        PilotIds ids,
        Guid userId,
        string roleName)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", ids.TenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", userId.ToString());
        request.Headers.Add("X-Gccs-Dev-Email", $"{roleName.ToLowerInvariant().Replace(" ", ".", StringComparison.Ordinal)}@example.com");
        request.Headers.Add("X-Gccs-Dev-Role", roleName);
        if (content is not null)
        {
            request.Content = JsonContent.Create(content, options: JsonOptions);
        }

        return request;
    }

    private static UpsertCompanyProfileRequest CreateCompanyProfileRequest() =>
        new(
            "Pilot Federal Services LLC",
            "Pilot Gov",
            "PILOT1234567",
            "7P1L0",
            new DateOnly(2027, 6, 15),
            [new CompanyNaicsCodeDto("541519", "Other Computer Related Services", true, "$34M", true, new DateOnly(2026, 6, 15))],
            [new CompanyCertificationDto(null, CertificationType.Wosb, CertificationStatus.Active, "SBA", new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1), "WOSB-PILOT")],
            ["Department of Defense"],
            ContractorRole.Subcontractor,
            "Cybersecurity support and compliance operations.",
            CompanyRange.Small,
            CompanyRange.Small,
            [new CompanyLocationDto("HQ", "100 Pilot Way", null, "Arlington", "VA", "22201", "US", true)],
            new ItEnvironmentSummaryDto("Microsoft 365, endpoint protection, and managed firewall.", true, "Pilot MSP", ["M365", "EDR", "Firewall"]),
            DataHandlingPosture.FciOnly,
            true);

    private static UpsertContractRequest CreateContractRequest() =>
        new(
            "PILOT-2026-001",
            "Pilot cybersecurity support subcontract",
            "Prime Integrator LLC",
            ContractorRelationship.Subcontractor,
            ContractKind.FixedPrice,
            ContractStatus.Active,
            new DateOnly(2026, 6, 15),
            new DateOnly(2026, 7, 1),
            new DateOnly(2027, 6, 30),
            "Arlington, VA",
            "Non-CUI pilot contract with FCI handling.",
            DataHandlingPosture.FciOnly);

    private static UpsertEvidenceMetadataRequest CreateEvidenceRequest(Guid? contractId) =>
        new(
            "Access control policy",
            EvidenceType.Policy,
            "Security",
            EvidenceStatus.Approved,
            new DateOnly(2026, 6, 15),
            new DateOnly(2027, 6, 15),
            ["policy", "access-control", "pilot"],
            "Non-CUI access control policy for FCI safeguarding.",
            ["obligation-fci-safeguards"],
            ["AC.L1-3.1.1"],
            contractId.HasValue ? [contractId.Value] : [],
            [],
            [],
            [],
            []);

    private static UpsertCmmcAssessmentRequest CreateAssessmentRequest(Guid companyProfileId, Guid contractId) =>
        new(
            "Pilot CMMC Level 1 readiness",
            AssessmentType.Readiness,
            CmmcLevel.Level1,
            "CMMC 2.0 / FAR 52.204-21",
            AssessmentStatus.InProgress,
            new DateOnly(2026, 6, 15),
            null,
            new DateOnly(2027, 6, 15),
            "Security",
            companyProfileId,
            [contractId]);

    private static UpsertSubcontractorRequest CreateSubcontractorRequest(Guid contractId) =>
        new(
            "Pilot Controls Sub LLC",
            "SUBPILOT1234",
            "8S2B1",
            SubcontractorStatus.Active,
            "Provides help desk support for the pilot contract.",
            "Small business",
            "CMMC Level 1 self-assessment in progress",
            new DateOnly(2027, 3, 31),
            "Signed",
            "Performs Tier 1 support only.",
            18.5m,
            true,
            false,
            false,
            "Level 1",
            "Jordan Contracts",
            "jordan.contracts@example.com",
            "555-0100",
            "Contracts Manager",
            [contractId]);

    private static void SeedPilotFoundation(GccsDbContext dbContext, PilotIds ids)
    {
        dbContext.Tenants.Add(new TenantEntity
        {
            Id = ids.TenantId,
            Name = "Pilot Tenant",
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.UtcNow
        });
        SeedUsers(dbContext, ids);
        SeedNoCuiAcknowledgements(dbContext, ids);
        dbContext.Controls.Add(CreateControl());
        dbContext.Clauses.Add(CreateClause());
        dbContext.Obligations.Add(CreateObligation());
    }

    private static void SeedPilotDataForReports(GccsDbContext dbContext, PilotIds ids)
    {
        SeedPilotFoundation(dbContext, ids);
        dbContext.CompanyProfiles.Add(new CompanyProfileEntity
        {
            Id = ids.CompanyProfileId,
            TenantId = ids.TenantId,
            LegalEntityName = "Pilot Federal Services LLC",
            DoingBusinessAs = "Pilot Gov",
            Uei = "PILOT1234567",
            CageCode = "7P1L0",
            SamRegistrationExpiresAt = new DateOnly(2027, 6, 15),
            AgencyCustomersJson = """["Department of Defense"]""",
            ContractorRole = ContractorRole.Subcontractor,
            ProductsAndServices = "Cybersecurity support.",
            EmployeeRange = CompanyRange.Small,
            RevenueRange = CompanyRange.Small,
            ItEnvironmentDescription = "Microsoft 365, endpoint protection, and managed firewall.",
            UsesExternalServiceProvider = true,
            ExternalServiceProviderName = "Pilot MSP",
            KeySystemsJson = """["M365","EDR","Firewall"]""",
            DataHandlingPosture = DataHandlingPosture.FciOnly,
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.Contracts.Add(CreateContract(ids.TenantId, ids.ContractId));
        dbContext.Set<ContractClauseEntity>().Add(CreateContractClause(ids.ContractId, ids.ContractClauseId));
        dbContext.Set<ContractClauseObligationEntity>().Add(new ContractClauseObligationEntity
        {
            ContractClauseId = ids.ContractClauseId,
            ObligationId = "obligation-fci-safeguards"
        });
        dbContext.EvidenceItems.Add(new EvidenceItemEntity
        {
            Id = ids.EvidenceItemId,
            TenantId = ids.TenantId,
            Name = "Access control policy",
            Description = "Approved non-CUI pilot evidence.",
            Type = EvidenceType.Policy,
            OwnerFunction = "Security",
            Status = EvidenceStatus.Approved,
            EffectiveAt = new DateOnly(2026, 6, 15),
            ExpiresAt = new DateOnly(2027, 6, 15),
            TagsJson = """["pilot","access-control"]""",
            ApprovedAt = DateTimeOffset.UtcNow,
            ApprovedByUserId = ids.ComplianceManagerUserId,
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.Set<EvidenceObligationEntity>().Add(new EvidenceObligationEntity { EvidenceItemId = ids.EvidenceItemId, ObligationId = "obligation-fci-safeguards" });
        dbContext.Set<EvidenceContractEntity>().Add(new EvidenceContractEntity { EvidenceItemId = ids.EvidenceItemId, ContractId = ids.ContractId });
        dbContext.Set<EvidenceControlEntity>().Add(new EvidenceControlEntity { EvidenceItemId = ids.EvidenceItemId, ControlId = "AC.L1-3.1.1" });
        dbContext.Assessments.Add(new AssessmentEntity
        {
            Id = ids.AssessmentId,
            TenantId = ids.TenantId,
            Name = "Pilot CMMC Level 1 readiness",
            Type = AssessmentType.Readiness,
            Level = CmmcLevel.Level1,
            Framework = "CMMC 2.0 / FAR 52.204-21",
            Status = AssessmentStatus.InProgress,
            StartedAt = new DateOnly(2026, 6, 15),
            OwnerFunction = "Security",
            CompanyProfileId = ids.CompanyProfileId,
            ContractIdsJson = JsonSerializer.Serialize(new[] { ids.ContractId }),
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.Set<ControlAssessmentEntity>().Add(new ControlAssessmentEntity
        {
            AssessmentId = ids.AssessmentId,
            ControlId = "AC.L1-3.1.1",
            ImplementationStatus = ControlImplementationStatus.Implemented,
            Result = AssessmentResult.Met,
            EvidenceItemIdsJson = JsonSerializer.Serialize(new[] { ids.EvidenceItemId }),
            TaskIdsJson = "[]",
            AssetIdsJson = "[]",
            PoamItemIdsJson = "[]",
            Notes = "Pilot evidence reviewed.",
            AssessedByUserId = ids.ComplianceManagerUserId,
            AssessedAt = new DateOnly(2026, 6, 15)
        });
        dbContext.Subcontractors.Add(CreateSubcontractor(ids.TenantId, ids.SubcontractorId));
        dbContext.Set<ContractSubcontractorEntity>().Add(new ContractSubcontractorEntity
        {
            ContractId = ids.ContractId,
            SubcontractorId = ids.SubcontractorId
        });
        dbContext.ComplianceTasks.Add(new ComplianceTaskEntity
        {
            Id = ids.TaskId,
            TenantId = ids.TenantId,
            ContractId = ids.ContractId,
            ObligationId = "obligation-fci-safeguards",
            EvidenceItemId = ids.EvidenceItemId,
            Title = "Pilot evidence follow-up",
            Description = "Verify evidence package.",
            Type = ComplianceTaskType.EvidenceRequest,
            Status = ComplianceTaskStatus.Open,
            RiskLevel = RiskLevel.Medium,
            AssignedToUserId = ids.ContributorUserId,
            OwnerFunction = "Security",
            DueAt = new DateOnly(2026, 7, 15),
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    private static void SeedUsers(GccsDbContext dbContext, PilotIds ids)
    {
        foreach (var (userId, roleName) in ids.Users)
        {
            dbContext.Users.Add(new UserEntity
            {
                Id = userId,
                TenantId = ids.TenantId,
                Email = $"{roleName.ToLowerInvariant().Replace(" ", ".", StringComparison.Ordinal)}@example.com",
                DisplayName = $"{roleName} User",
                Status = UserStatus.Active,
                MfaEnabled = true,
                CreatedAt = DateTimeOffset.UtcNow
            });
            dbContext.TenantMemberships.Add(new TenantMembershipEntity
            {
                Id = Guid.NewGuid(),
                TenantId = ids.TenantId,
                UserId = userId,
                Status = MembershipStatus.Active,
                RoleName = roleName,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }
    }

    private static void SeedNoCuiAcknowledgements(GccsDbContext dbContext, PilotIds ids)
    {
        foreach (var (userId, _) in ids.Users)
        {
            dbContext.NoCuiAcknowledgements.Add(new NoCuiAcknowledgementEntity
            {
                Id = Guid.NewGuid(),
                TenantId = ids.TenantId,
                UserId = userId,
                NoticeVersion = NoCuiNotice.CurrentVersion,
                NoticeCopy = NoCuiNotice.Copy,
                AcknowledgedAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }
    }

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
            ApplicabilityJson = "{}",
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
            EvidenceExamplesJson = """["Access control policy"]""",
            SourceName = "FAR 52.204-21 / CMMC Level 1",
            SourceUrl = "https://dodcio.defense.gov/CMMC/Resources-Documentation/",
            SourceLastReviewedAt = new DateOnly(2026, 6, 15),
            SourceConfidence = "high"
        };

    private static ContractEntity CreateContract(Guid tenantId, Guid contractId) =>
        new()
        {
            Id = contractId,
            TenantId = tenantId,
            ContractNumber = "PILOT-2026-001",
            Title = "Pilot cybersecurity support subcontract",
            AgencyOrPrimeName = "Prime Integrator LLC",
            Relationship = ContractorRelationship.Subcontractor,
            Kind = ContractKind.FixedPrice,
            Status = ContractStatus.Active,
            AwardedAt = new DateOnly(2026, 6, 15),
            PeriodOfPerformanceStart = new DateOnly(2026, 7, 1),
            PeriodOfPerformanceEnd = new DateOnly(2027, 6, 30),
            PlaceOfPerformance = "Arlington, VA",
            Description = "Non-CUI pilot contract with FCI handling.",
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
            AttachmentReason = "Pilot contract handles FCI.",
            SourceDocumentReference = "pilot-contract.pdf",
            RequiresFlowDown = true,
            LastReviewedAt = new DateOnly(2026, 6, 3),
            Confidence = "high",
            ReviewState = ReviewState.Approved,
            CreatedAt = DateTimeOffset.UtcNow
        };

    private static SubcontractorEntity CreateSubcontractor(Guid tenantId, Guid subcontractorId) =>
        new()
        {
            Id = subcontractorId,
            TenantId = tenantId,
            Name = "Pilot Controls Sub LLC",
            Uei = "SUBPILOT1234",
            CageCode = "8S2B1",
            Status = SubcontractorStatus.Active,
            RoleDescription = "Provides help desk support for the pilot contract.",
            SmallBusinessStatus = "Small business",
            CmmcStatus = "CMMC Level 1 self-assessment in progress",
            InsuranceExpiresAt = new DateOnly(2027, 3, 31),
            NdaStatus = "Signed",
            WorkshareDescription = "Performs Tier 1 support only.",
            WorksharePercentage = 18.5m,
            HasFciAccess = true,
            HasCuiAccess = false,
            HasExportControlledAccess = false,
            RequiredCmmcLevel = "Level 1",
            ContactName = "Jordan Contracts",
            ContactEmail = "jordan.contracts@example.com",
            ContactPhone = "555-0100",
            ContactTitle = "Contracts Manager",
            CreatedAt = DateTimeOffset.UtcNow
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

    private sealed record PilotIds(
        Guid TenantId,
        Guid OwnerUserId,
        Guid AdminUserId,
        Guid ComplianceManagerUserId,
        Guid ContributorUserId,
        Guid AuditorUserId,
        Guid AdvisorUserId,
        Guid CompanyProfileId,
        Guid ContractId,
        Guid ContractClauseId,
        Guid EvidenceItemId,
        Guid AssessmentId,
        Guid SubcontractorId,
        Guid TaskId)
    {
        public IReadOnlyList<(Guid UserId, string RoleName)> Users =>
        [
            (OwnerUserId, RoleCatalog.Owner),
            (AdminUserId, RoleCatalog.Admin),
            (ComplianceManagerUserId, RoleCatalog.ComplianceManager),
            (ContributorUserId, RoleCatalog.Contributor),
            (AuditorUserId, RoleCatalog.Auditor),
            (AdvisorUserId, RoleCatalog.Advisor)
        ];

        public static PilotIds Create(string caseName)
        {
            var suffix = Math.Abs(caseName.GetHashCode(StringComparison.Ordinal)) % 10000;
            return new PilotIds(
                Guid.Parse($"17117117-1171-1711-7117-11711711{suffix:D4}"),
                Guid.Parse($"17117117-1171-1711-7117-11711712{suffix:D4}"),
                Guid.Parse($"17117117-1171-1711-7117-11711713{suffix:D4}"),
                Guid.Parse($"17117117-1171-1711-7117-11711714{suffix:D4}"),
                Guid.Parse($"17117117-1171-1711-7117-11711715{suffix:D4}"),
                Guid.Parse($"17117117-1171-1711-7117-11711716{suffix:D4}"),
                Guid.Parse($"17117117-1171-1711-7117-11711717{suffix:D4}"),
                Guid.Parse($"17117117-1171-1711-7117-11711718{suffix:D4}"),
                Guid.Parse($"17117117-1171-1711-7117-11711719{suffix:D4}"),
                Guid.Parse($"17117117-1171-1711-7117-11711720{suffix:D4}"),
                Guid.Parse($"17117117-1171-1711-7117-11711721{suffix:D4}"),
                Guid.Parse($"17117117-1171-1711-7117-11711722{suffix:D4}"),
                Guid.Parse($"17117117-1171-1711-7117-11711723{suffix:D4}"),
                Guid.Parse($"17117117-1171-1711-7117-11711724{suffix:D4}"));
        }
    }
}

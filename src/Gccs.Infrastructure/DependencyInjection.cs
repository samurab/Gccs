using Gccs.Application.Audit;
using Gccs.Application.Ai;
using Gccs.Application.Common;
using Gccs.Application.Calendar;
using Gccs.Application.Companies;
using Gccs.Application.Cmmc;
using Gccs.Application.Compliance;
using Gccs.Application.Contracts;
using Gccs.Application.Demo;
using Gccs.Application.Evidence;
using Gccs.Application.Identity;
using Gccs.Application.Labor;
using Gccs.Application.NoCui;
using Gccs.Application.Notifications;
using Gccs.Application.Repositories;
using Gccs.Application.Reports;
using Gccs.Application.SamGov;
using Gccs.Application.Subcontractors;
using Gccs.Application.Tasks;
using Gccs.Application.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Ai;
using Gccs.Infrastructure.Calendar;
using Gccs.Infrastructure.Companies;
using Gccs.Infrastructure.Cmmc;
using Gccs.Infrastructure.Compliance;
using Gccs.Infrastructure.Common;
using Gccs.Infrastructure.Contracts;
using Gccs.Infrastructure.Demo;
using Gccs.Infrastructure.Evidence;
using Gccs.Infrastructure.Identity;
using Gccs.Infrastructure.Labor;
using Gccs.Infrastructure.NoCui;
using Gccs.Infrastructure.Notifications;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Reports;
using Gccs.Infrastructure.SamGov;
using Gccs.Infrastructure.Subcontractors;
using Gccs.Infrastructure.Tenancy;
using Gccs.Infrastructure.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Gccs.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddGccsInfrastructure(this IServiceCollection services, IConfiguration? configuration = null)
    {
        services.AddScoped<ComplianceOverviewService>();
        services.AddScoped<ComplianceContentReviewService>();
        services.AddScoped<PolicyTemplateService>();
        services.AddScoped<SbaSizeStandardService>();
        services.AddScoped<SuggestedObligationService>();
        services.AddScoped<ExpertReviewQueueService>();
        services.AddScoped<ApplicabilityFactService>();
        services.AddScoped<ObligationApplicabilityService>();
        services.AddScoped<ClauseLibraryService>();
        services.AddScoped<ObligationDetailService>();
        services.AddScoped<CompanyProfileService>();
        services.AddScoped<CompanySizeEvaluationService>();
        services.AddScoped<CompanyEntityLookupService>();
        services.AddScoped<ContractService>();
        services.AddScoped<ContractSizeCheckService>();
        services.AddScoped<TenantService>();
        services.AddScoped<CuiReadyApprovalChecklistService>();
        services.AddScoped<SharedResponsibilityMatrixService>();
        services.AddScoped<SharedResponsibilityMatrixAcknowledgementService>();
        services.AddScoped<DataHandlingNoticeService>();
        services.AddScoped<DataHandlingNoticeAcknowledgementService>();
        services.AddScoped<CuiSupportEscalationService>();
        services.AddScoped<TenantDataHandlingModePolicyService>();
        services.AddScoped<ContentClassificationPolicy>();
        services.AddScoped<ContentClassificationReviewService>();
        services.AddScoped<SyntheticDemoDatasetService>();
        services.AddScoped<DemoTenantSeedService>();
        services.AddSingleton<ISyntheticDemoDatasetRepository, FileSyntheticDemoDatasetRepository>();
        services.AddSingleton<ISharedResponsibilityMatrixRepository, FileSharedResponsibilityMatrixRepository>();
        services.AddSingleton<IDataHandlingNoticeRepository, FileDataHandlingNoticeRepository>();
        services.AddSingleton<ISprsScoringRuleRepository, FileSprsScoringRuleRepository>();
        services.AddSingleton<ISprsScoreCalculationHistoryRepository, InMemorySprsScoreCalculationHistoryRepository>();
        services.AddSingleton<IEsrsApplicabilityRepository, InMemoryEsrsApplicabilityRepository>();
        services.AddSingleton<ISubcontractingReportDataRepository, InMemorySubcontractingReportDataRepository>();
        services.AddSingleton<IEsrsReportPackageRepository, InMemoryEsrsReportPackageRepository>();
        services.AddSingleton<ILaborApplicabilityRepository, InMemoryLaborApplicabilityRepository>();
        services.AddSingleton<ILaborClassificationRepository, InMemoryLaborClassificationRepository>();
        services.AddSingleton<IAiRetrievalSourceRepository, InMemoryAiRetrievalSourceRepository>();
        services.AddScoped<TenantMembershipService>();
        services.AddScoped<TenantInvitationService>();
        services.AddScoped<NoCuiAcknowledgementService>();
        services.AddScoped<NotificationPreferenceService>();
        services.AddScoped<DueDateReminderService>();
        services.AddScoped<AuditLogService>();
        services.AddScoped<CuiAuditExportService>();
        services.AddScoped<ComplianceTaskService>();
        services.AddScoped<RenewalGenerationService>();
        services.AddScoped<EvidenceMetadataService>();
        services.AddScoped<EvidenceRequestService>();
        services.AddScoped<EvidenceApprovalService>();
        services.AddScoped<CmmcAssessmentService>();
        services.AddScoped<CmmcPoamService>();
        services.AddScoped<CmmcAffirmationService>();
        services.AddScoped<SprsScoringRuleService>();
        services.AddScoped<SprsScoreCalculationService>();
        services.AddScoped<SubcontractorService>();
        services.AddScoped<SubcontractorEntityLookupService>();
        services.AddScoped<ComplianceStatusReportService>();
        services.AddScoped<CmmcReadinessReportService>();
        services.AddScoped<SprsReadinessReportService>();
        services.AddScoped<EsrsApplicabilityService>();
        services.AddScoped<SubcontractingReportDataService>();
        services.AddScoped<EsrsReportPackageService>();
        services.AddScoped<LaborApplicabilityService>();
        services.AddScoped<LaborClassificationService>();
        services.AddScoped<LaborComplianceReportService>();
        services.AddScoped<ILaborWageDeterminationUploadGuard, TenantLaborWageDeterminationUploadGuard>();
        services.AddScoped<AiRetrievalAssistantService>();
        services.AddScoped<EvidencePackageReportService>();
        services.AddScoped<SubcontractorComplianceReportService>();
        if (configuration is not null)
        {
            services.Configure<SamGovOptions>(options =>
            {
                options.BaseUrl = configuration[$"{SamGovOptions.SectionName}:BaseUrl"] ?? options.BaseUrl;
                options.ApiKey = configuration[$"{SamGovOptions.SectionName}:ApiKey"] ?? options.ApiKey;
                options.TimeoutSeconds = ReadInt(configuration, $"{SamGovOptions.SectionName}:TimeoutSeconds", options.TimeoutSeconds);
                options.MaxRetries = ReadInt(configuration, $"{SamGovOptions.SectionName}:MaxRetries", options.MaxRetries);
                options.RateLimitPerMinute = ReadInt(configuration, $"{SamGovOptions.SectionName}:RateLimitPerMinute", options.RateLimitPerMinute);
            });
        }
        else
        {
            services.Configure<SamGovOptions>(_ => { });
        }

        services.AddScoped<ISamGovEntityLookupClient>(provider =>
            new SamGovEntityLookupClient(
                new HttpClient(),
                provider.GetRequiredService<IOptions<SamGovOptions>>(),
                provider.GetRequiredService<ILogger<SamGovEntityLookupClient>>()));

        var connectionString = configuration?.GetConnectionString("GccsDatabase");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<GccsDbContext>(options =>
                options.UseNpgsql(connectionString, npgsql =>
                    npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "gccs")));

            services.AddScoped<ITenantRepository, EfTenantRepository>();
            services.AddScoped<ICuiReadyApprovalChecklistRepository, EfCuiReadyApprovalChecklistRepository>();
            services.AddScoped<ISharedResponsibilityMatrixAcknowledgementRepository, EfSharedResponsibilityMatrixAcknowledgementRepository>();
            services.AddScoped<IDataHandlingNoticeAcknowledgementRepository, EfDataHandlingNoticeAcknowledgementRepository>();
            services.AddScoped<ICuiSupportEscalationRepository, EfCuiSupportEscalationRepository>();
            services.AddScoped<ICuiReadyApprovalChecklistGate>(provider => provider.GetRequiredService<CuiReadyApprovalChecklistService>());
            services.AddScoped<ITenantMembershipRepository, EfTenantMembershipRepository>();
            services.AddScoped<ITenantInvitationRepository, EfTenantInvitationRepository>();
            services.AddScoped<INoCuiAcknowledgementRepository, EfNoCuiAcknowledgementRepository>();
            services.AddScoped<INotificationPreferenceRepository, EfNotificationPreferenceRepository>();
            services.AddScoped<IDueDateReminderRepository, EfDueDateReminderRepository>();
            services.AddScoped<IAssignmentNotificationRepository, EfAssignmentNotificationRepository>();
            services.AddScoped<AssignmentNotificationService>();
            services.AddScoped<IReportRepository, EfReportRepository>();
            services.AddScoped<IContractObligationMatrixRepository, EfContractObligationMatrixRepository>();
            services.AddScoped<IAuditLogRepository, EfAuditLogRepository>();
            services.AddScoped<IAuditEventWriter, EfAuditEventWriter>();
            services.AddScoped<ICompanyProfileRepository, EfCompanyProfileRepository>();
            services.AddScoped<ICompanySizeEvaluationRepository, EfCompanySizeEvaluationRepository>();
            services.AddScoped<IContractRepository, EfContractRepository>();
            services.AddScoped<IContractSizeCheckRepository, EfContractSizeCheckRepository>();
            services.AddScoped<IExtractionJobQueue, NoOpExtractionJobQueue>();
            services.AddScoped<IContractDocumentTextExtractor, DefaultContractDocumentTextExtractor>();
            services.AddScoped<IComplianceContentImporter, ComplianceContentImporter>();
            services.AddScoped<IComplianceContentReviewRepository, EfComplianceContentReviewRepository>();
            services.AddScoped<IPolicyTemplateRepository, EfPolicyTemplateRepository>();
            services.AddScoped<ISbaSizeStandardRepository, EfSbaSizeStandardRepository>();
            services.AddScoped<ISuggestedObligationRepository, EfSuggestedObligationRepository>();
            services.AddScoped<IExpertReviewQueueRepository, EfExpertReviewQueueRepository>();
            services.AddScoped<IClauseLibraryRepository, EfClauseLibraryRepository>();
            services.AddScoped<IApplicabilityFactRepository, EfApplicabilityFactRepository>();
            services.AddScoped<IObligationApplicabilityRepository, EfObligationApplicabilityRepository>();
            services.AddScoped<IObligationDashboardRepository, EfObligationDashboardRepository>();
            services.AddScoped<IObligationDetailRepository, EfObligationDetailRepository>();
            services.AddScoped<IObligationRepository, EfObligationRepository>();
            services.AddScoped<IComplianceTaskRepository, EfComplianceTaskRepository>();
            services.AddScoped<IRenewalTaskRepository, EfRenewalTaskRepository>();
            services.AddScoped<ICalendarRepository, EfCalendarRepository>();
            services.AddScoped<IEvidenceMetadataRepository, EfEvidenceMetadataRepository>();
            services.AddScoped<IEvidenceRequestRepository, EfEvidenceRequestRepository>();
            services.AddScoped<ICmmcAssessmentRepository, EfCmmcAssessmentRepository>();
            services.AddScoped<ICmmcPoamRepository, EfCmmcPoamRepository>();
            services.AddScoped<ICmmcAffirmationRepository, EfCmmcAffirmationRepository>();
            services.AddScoped<ISubcontractorRepository, EfSubcontractorRepository>();
            services.AddScoped<IContentClassificationReviewRepository, EfContentClassificationReviewRepository>();
            services.AddScoped<IDemoTenantSeedRepository, EfDemoTenantSeedRepository>();
        }
        else
        {
            services.AddSingleton<IClauseLibraryRepository, InMemoryClauseLibraryRepository>();
            services.AddSingleton<IObligationRepository, InMemoryObligationRepository>();
            services.AddScoped<ITenantRepository>(_ =>
                throw new InvalidOperationException("Tenant persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<ICuiReadyApprovalChecklistRepository>(_ =>
                throw new InvalidOperationException("CUI-ready approval checklist persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<ISharedResponsibilityMatrixAcknowledgementRepository>(_ =>
                throw new InvalidOperationException("Shared responsibility matrix acknowledgement persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<IDataHandlingNoticeAcknowledgementRepository>(_ =>
                throw new InvalidOperationException("Data handling notice acknowledgement persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<ICuiSupportEscalationRepository>(_ =>
                throw new InvalidOperationException("CUI support escalation persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<ITenantMembershipRepository>(_ =>
                throw new InvalidOperationException("Tenant membership persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<ITenantInvitationRepository>(_ =>
                throw new InvalidOperationException("Tenant invitation persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<INoCuiAcknowledgementRepository>(_ =>
                throw new InvalidOperationException("No-CUI acknowledgement persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<INotificationPreferenceRepository>(_ =>
                throw new InvalidOperationException("Notification preference persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<IDueDateReminderRepository>(_ =>
                throw new InvalidOperationException("Due-date reminder persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<IReportRepository>(_ =>
                throw new InvalidOperationException("Report persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<IContractObligationMatrixRepository>(_ =>
                throw new InvalidOperationException("Contract obligation matrix persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<IAuditLogRepository>(_ =>
                throw new InvalidOperationException("Audit log persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<IAuditEventWriter>(_ =>
                throw new InvalidOperationException("Audit persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<ICompanyProfileRepository>(_ =>
                throw new InvalidOperationException("Company profile persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<ICompanySizeEvaluationRepository>(_ =>
                throw new InvalidOperationException("Company size evaluation requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<IContractRepository>(_ =>
                throw new InvalidOperationException("Contract persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<IContractSizeCheckRepository>(_ =>
                throw new InvalidOperationException("Contract size checks require ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<IExtractionJobQueue, NoOpExtractionJobQueue>();
            services.AddScoped<IContractDocumentTextExtractor, DefaultContractDocumentTextExtractor>();
            services.AddScoped<IComplianceContentImporter>(_ =>
                throw new InvalidOperationException("Compliance content import requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<IComplianceContentReviewRepository>(_ =>
                throw new InvalidOperationException("Compliance content review persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<IPolicyTemplateRepository>(_ =>
                throw new InvalidOperationException("Policy template persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<ISbaSizeStandardRepository>(_ =>
                throw new InvalidOperationException("SBA size standard persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<ISuggestedObligationRepository>(_ =>
                throw new InvalidOperationException("Suggested obligation persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<IExpertReviewQueueRepository>(_ =>
                throw new InvalidOperationException("Expert review queue persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<IObligationDashboardRepository>(_ =>
                throw new InvalidOperationException("Obligation dashboard persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<IApplicabilityFactRepository>(_ =>
                throw new InvalidOperationException("Applicability facts require ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<IObligationApplicabilityRepository>(_ =>
                throw new InvalidOperationException("Obligation applicability persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<IObligationDetailRepository>(_ =>
                throw new InvalidOperationException("Obligation detail persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<IComplianceTaskRepository>(_ =>
                throw new InvalidOperationException("Task persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<IRenewalTaskRepository>(_ =>
                throw new InvalidOperationException("Renewal task generation requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<ICalendarRepository>(_ =>
                throw new InvalidOperationException("Calendar persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<IEvidenceMetadataRepository>(_ =>
                throw new InvalidOperationException("Evidence metadata persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<IEvidenceRequestRepository>(_ =>
                throw new InvalidOperationException("Evidence request persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<ICmmcAssessmentRepository>(_ =>
                throw new InvalidOperationException("CMMC assessment persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<ICmmcPoamRepository>(_ =>
                throw new InvalidOperationException("CMMC POA&M persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<ICmmcAffirmationRepository>(_ =>
                throw new InvalidOperationException("CMMC affirmation persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<ISubcontractorRepository>(_ =>
                throw new InvalidOperationException("Subcontractor persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<IContentClassificationReviewRepository>(_ =>
                throw new InvalidOperationException("Content classification review persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<IDemoTenantSeedRepository>(_ =>
                throw new InvalidOperationException("Demo tenant seed persistence requires ConnectionStrings:GccsDatabase to be configured."));
        }

        return services;
    }

    private static int ReadInt(IConfiguration configuration, string key, int fallback) =>
        int.TryParse(configuration[key], out var value) ? value : fallback;
}

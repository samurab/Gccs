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
using Gccs.Application.Marketing;
using Gccs.Application.NoCui;
using Gccs.Application.Notifications;
using Gccs.Application.Portals;
using Gccs.Application.Repositories;
using Gccs.Application.Reports;
using Gccs.Application.SamGov;
using Gccs.Application.Security;
using Gccs.Application.Subcontractors;
using Gccs.Application.Storage;
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
using Gccs.Infrastructure.Marketing;
using Gccs.Infrastructure.NoCui;
using Gccs.Infrastructure.Notifications;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Portals;
using Gccs.Infrastructure.Reports;
using Gccs.Infrastructure.SamGov;
using Gccs.Infrastructure.Storage;
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
        services.AddScoped<IApplicationTransaction, EfApplicationTransaction>();
        services.AddScoped<ComplianceOverviewService>();
        services.AddScoped<ComplianceChecklistService>();
        services.AddScoped<ComplianceContentReviewService>();
        services.AddScoped<FedRampControlMappingService>();
        services.AddScoped<TrustArtifactLibraryService>();
        services.AddScoped<FedRampReadinessExportPackageService>();
        services.AddScoped<SspSectionService>();
        services.AddScoped<CuiEnclaveBoundaryService>();
        services.AddScoped<CustomerManagedKeyPolicyService>();
        services.AddScoped<CuiEnclaveAccessControlService>();
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
        services.AddScoped<PlatformTenantProvisioningService>();
        services.AddScoped<PlatformCustomerService>();
        services.AddScoped<TenantSubscriptionService>();
        services.AddScoped<GovernmentCloudEnvironmentService>();
        services.AddScoped<RegulatedTenantProvisioningService>();
        services.AddScoped<GovernmentCloudReleaseReadinessService>();
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
        services.AddSingleton<IAiOutputReviewRepository, InMemoryAiOutputReviewRepository>();
        services.AddSingleton<IGuardedAssistantRepository, InMemoryGuardedAssistantRepository>();
        services.AddSingleton<IExternalPortalAccessRepository, InMemoryExternalPortalAccessRepository>();
        services.AddSingleton<IPortalPackageRepository, InMemoryPortalPackageRepository>();
        services.AddSingleton<IPortalPackageLifecycleRepository, InMemoryPortalPackageLifecycleRepository>();
        services.AddSingleton<IFedRampControlMappingRepository, InMemoryFedRampControlMappingRepository>();
        services.AddSingleton<ITrustArtifactLibraryRepository, InMemoryTrustArtifactLibraryRepository>();
        services.AddSingleton<IFedRampReadinessExportPackageRepository, InMemoryFedRampReadinessExportPackageRepository>();
        services.AddSingleton<ISspSectionRepository, InMemorySspSectionRepository>();
        services.AddSingleton<ISspNarrativeRepository>(provider => (InMemorySspSectionRepository)provider.GetRequiredService<ISspSectionRepository>());
        services.AddSingleton<ISspExportPackageRepository>(provider => (InMemorySspSectionRepository)provider.GetRequiredService<ISspSectionRepository>());
        services.AddSingleton<ICuiEnclaveBoundaryRepository, InMemoryCuiEnclaveBoundaryRepository>();
        services.AddSingleton<ICustomerManagedKeyPolicyRepository, InMemoryCustomerManagedKeyPolicyRepository>();
        services.AddSingleton<ICuiEnclaveAccessControlRepository, InMemoryCuiEnclaveAccessControlRepository>();
        services.AddScoped<TenantMembershipService>();
        services.AddScoped<TenantWorkspaceSelectionService>();
        services.AddScoped<TenantInvitationService>();
        services.AddScoped<InvitationDeliveryService>();
        services.AddScoped<SamlIdentityProviderConfigurationService>();
        services.AddScoped<SsoSignInEnforcementService>();
        services.AddScoped<ScimProvisioningService>();
        services.AddScoped<NoCuiAcknowledgementService>();
        services.AddScoped<EvidenceFileService>();
        services.AddScoped<NoCuiAcknowledgementStatusService>();
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
        services.AddScoped<ReportHistoryService>();
        services.AddScoped<ReportLifecycleService>();
        services.AddScoped<SprsReadinessReportService>();
        services.AddScoped<EsrsApplicabilityService>();
        services.AddScoped<SubcontractingReportDataService>();
        services.AddScoped<EsrsReportPackageService>();
        services.AddScoped<LaborApplicabilityService>();
        services.AddScoped<LaborClassificationService>();
        services.AddScoped<LaborComplianceReportService>();
        services.AddScoped<ILaborWageDeterminationUploadGuard, TenantLaborWageDeterminationUploadGuard>();
        services.AddScoped<AiRetrievalAssistantService>();
        services.AddScoped<AiOutputReviewService>();
        services.AddScoped<GuardedAssistantExperienceService>();
        services.AddScoped<ExternalPortalAccessService>();
        services.AddScoped<ApprovedPackagePortalReviewService>();
        services.AddScoped<PortalPackageLifecycleService>();
        services.AddScoped<EvidencePackageReportService>();
        services.AddScoped<SubcontractorComplianceReportService>();
        services.AddScoped<SimpleReportExportService>();
        services.AddScoped<DemoRequestService>();
        services.AddScoped<DemoFollowUpService>();
        services.AddScoped<DemoRequestResponseService>();
        services.AddScoped<DemoRequestCalendarService>();
        services.AddScoped<DemoAppointmentService>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(new TenantSubscriptionSettings(
            Math.Clamp(configuration is null ? 90 : ReadInt(configuration, "Subscriptions:MaximumPilotDays", 90), 1, 365),
            Math.Clamp(configuration is null ? 7 : ReadInt(configuration, "Subscriptions:GracePeriodDays", 7), 0, 30)));
        services.Configure<DemoRequestOptions>(options =>
        {
            if (configuration is null) return;
            var prefix = DemoRequestOptions.SectionName;
            options.Enabled = ReadBool(configuration, $"{prefix}:Enabled", options.Enabled);
            options.Provider = configuration[$"{prefix}:Provider"] ?? options.Provider;
            options.Endpoint = configuration[$"{prefix}:Endpoint"] ?? options.Endpoint;
            options.ConnectionString = configuration[$"{prefix}:ConnectionString"] ?? options.ConnectionString;
            options.UseManagedIdentity = ReadBool(configuration, $"{prefix}:UseManagedIdentity", options.UseManagedIdentity);
            options.SenderAddress = configuration[$"{prefix}:SenderAddress"] ?? options.SenderAddress;
            options.RecipientAddress = configuration[$"{prefix}:RecipientAddress"] ?? options.RecipientAddress;
            options.PublicWebBaseUrl = configuration[$"{prefix}:PublicWebBaseUrl"] ?? options.PublicWebBaseUrl;
            options.FollowUpTokenSigningKey = configuration[$"{prefix}:FollowUpTokenSigningKey"] ?? options.FollowUpTokenSigningKey;
            options.FollowUpTokenLifetimeHours = ReadInt(configuration, $"{prefix}:FollowUpTokenLifetimeHours", options.FollowUpTokenLifetimeHours);
            options.PollIntervalSeconds = ReadInt(configuration, $"{prefix}:PollIntervalSeconds", options.PollIntervalSeconds);
            options.LeaseMinutes = ReadInt(configuration, $"{prefix}:LeaseMinutes", options.LeaseMinutes);
            options.MaximumAttempts = ReadInt(configuration, $"{prefix}:MaximumAttempts", options.MaximumAttempts);
            options.RetentionDays = ReadInt(configuration, $"{prefix}:RetentionDays", options.RetentionDays);
        });
        services.AddScoped<IDemoRequestDeliveryTransport, AzureCommunicationDemoRequestEmailSender>();
        services.AddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<DemoRequestOptions>>().Value;
            var isDevelopmentCapture = string.Equals(
                options.Provider,
                DemoRequestOptions.DevelopmentCaptureProvider,
                StringComparison.OrdinalIgnoreCase);
            var publicWebBaseUrl = string.IsNullOrWhiteSpace(options.PublicWebBaseUrl) && isDevelopmentCapture
                ? "http://localhost:5173"
                : options.PublicWebBaseUrl.TrimEnd('/');
            var signingKey = string.IsNullOrWhiteSpace(options.FollowUpTokenSigningKey) && isDevelopmentCapture
                ? System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("FeDril local development follow-up token key"))
                : System.Text.Encoding.UTF8.GetBytes(options.FollowUpTokenSigningKey);
            return new DemoFollowUpSecuritySettings(
                publicWebBaseUrl,
                signingKey,
                TimeSpan.FromHours(Math.Clamp(options.FollowUpTokenLifetimeHours, 1, 168)));
        });
        services.AddSingleton<DemoFollowUpTokenCodec>();
        services.AddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<DemoRequestOptions>>().Value;
            return new DemoRequestDeliverySettings(
                TimeSpan.FromMinutes(Math.Clamp(options.LeaseMinutes, 1, 30)),
                Math.Clamp(options.MaximumAttempts, 1, 10));
        });
        services.Configure<InvitationEmailOptions>(options =>
        {
            if (configuration is null)
            {
                return;
            }

            var prefix = InvitationEmailOptions.SectionName;
            options.Enabled = ReadBool(configuration, $"{prefix}:Enabled", options.Enabled);
            options.Provider = configuration[$"{prefix}:Provider"] ?? options.Provider;
            options.PublicWebBaseUrl = configuration[$"{prefix}:PublicWebBaseUrl"] ?? options.PublicWebBaseUrl;
            options.Endpoint = configuration[$"{prefix}:Endpoint"] ?? options.Endpoint;
            options.ConnectionString = configuration[$"{prefix}:ConnectionString"] ?? options.ConnectionString;
            options.UseManagedIdentity = ReadBool(configuration, $"{prefix}:UseManagedIdentity", options.UseManagedIdentity);
            options.SenderAddress = configuration[$"{prefix}:SenderAddress"] ?? options.SenderAddress;
            options.PollIntervalSeconds = ReadInt(configuration, $"{prefix}:PollIntervalSeconds", options.PollIntervalSeconds);
            options.LeaseMinutes = ReadInt(configuration, $"{prefix}:LeaseMinutes", options.LeaseMinutes);
            options.MaximumAttempts = ReadInt(configuration, $"{prefix}:MaximumAttempts", options.MaximumAttempts);
        });
        services.AddScoped<IInvitationEmailSender, AzureCommunicationInvitationEmailSender>();
        services.AddScoped<IAssignmentEmailSender, AzureCommunicationAssignmentEmailSender>();
        services.AddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<InvitationEmailOptions>>().Value;
            return new InvitationDeliverySettings(
                options.PublicWebBaseUrl,
                TimeSpan.FromMinutes(Math.Clamp(options.LeaseMinutes, 1, 30)),
                Math.Clamp(options.MaximumAttempts, 1, 10));
        });
        services.AddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<InvitationEmailOptions>>().Value;
            return new AssignmentEmailDeliverySettings(
                options.PublicWebBaseUrl,
                TimeSpan.FromMinutes(Math.Clamp(options.LeaseMinutes, 1, 30)),
                Math.Clamp(options.MaximumAttempts, 1, 10));
        });
        services.Configure<AzureBlobStorageOptions>(options =>
        {
            if (configuration is null)
            {
                return;
            }

            options.AccountName = configuration[$"{AzureBlobStorageOptions.SectionName}:AccountName"] ?? options.AccountName;
            options.BlobServiceUri = configuration[$"{AzureBlobStorageOptions.SectionName}:BlobServiceUri"] ?? options.BlobServiceUri;
            options.UseManagedIdentity = ReadBool(
                configuration,
                $"{AzureBlobStorageOptions.SectionName}:UseManagedIdentity",
                options.UseManagedIdentity);
            options.Containers.ContractDocuments = configuration[$"{AzureBlobStorageOptions.SectionName}:Containers:ContractDocuments"] ??
                options.Containers.ContractDocuments;
            options.Containers.Evidence = configuration[$"{AzureBlobStorageOptions.SectionName}:Containers:Evidence"] ?? options.Containers.Evidence;
            options.Containers.Exports = configuration[$"{AzureBlobStorageOptions.SectionName}:Containers:Exports"] ?? options.Containers.Exports;
            options.Containers.Reports = configuration[$"{AzureBlobStorageOptions.SectionName}:Containers:Reports"] ?? options.Containers.Reports;
        });
        services.AddScoped<IObjectStorageService, AzureBlobObjectStorageService>();
        services.AddScoped<ContractDocumentFileService>();
        services.Configure<MalwareScanningOptions>(options =>
        {
            if (configuration is null)
            {
                return;
            }

            options.Enabled = ReadBool(configuration, $"{MalwareScanningOptions.SectionName}:Enabled", options.Enabled);
            options.Provider = configuration[$"{MalwareScanningOptions.SectionName}:Provider"] ?? options.Provider;
            options.Host = configuration[$"{MalwareScanningOptions.SectionName}:Host"] ?? options.Host;
            options.Port = ReadInt(configuration, $"{MalwareScanningOptions.SectionName}:Port", options.Port);
            options.TimeoutSeconds = ReadInt(configuration, $"{MalwareScanningOptions.SectionName}:TimeoutSeconds", options.TimeoutSeconds);
            options.MaxChunkSizeBytes = ReadInt(configuration, $"{MalwareScanningOptions.SectionName}:MaxChunkSizeBytes", options.MaxChunkSizeBytes);
        });
        services.AddScoped<IMalwareScanner, ClamAvMalwareScanner>();
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
            services.AddScoped<IPlatformTenantProvisioningRepository, EfPlatformTenantProvisioningRepository>();
            services.AddScoped<IPlatformCustomerRepository, EfPlatformCustomerRepository>();
            services.AddScoped<ITenantSubscriptionRepository, EfTenantSubscriptionRepository>();
            services.AddScoped<IGovernmentCloudEnvironmentRepository, EfGovernmentCloudEnvironmentRepository>();
            services.AddScoped<IRegulatedTenantProvisioningRepository, EfRegulatedTenantProvisioningRepository>();
            services.AddScoped<IGovernmentCloudReleaseReadinessRepository, EfGovernmentCloudReleaseReadinessRepository>();
            services.AddScoped<ICuiReadyApprovalChecklistRepository, EfCuiReadyApprovalChecklistRepository>();
            services.AddScoped<ISharedResponsibilityMatrixAcknowledgementRepository, EfSharedResponsibilityMatrixAcknowledgementRepository>();
            services.AddScoped<IDataHandlingNoticeAcknowledgementRepository, EfDataHandlingNoticeAcknowledgementRepository>();
            services.AddScoped<ICuiSupportEscalationRepository, EfCuiSupportEscalationRepository>();
            services.AddScoped<ICuiReadyApprovalChecklistGate>(provider => provider.GetRequiredService<CuiReadyApprovalChecklistService>());
            services.AddScoped<ITenantMembershipRepository, EfTenantMembershipRepository>();
            services.AddScoped<ITenantWorkspaceSelectionRepository, EfTenantWorkspaceSelectionRepository>();
            services.AddScoped<ITenantInvitationRepository, EfTenantInvitationRepository>();
            services.AddScoped<IInvitationDeliveryRepository, EfInvitationDeliveryRepository>();
            services.AddScoped<ISamlIdentityProviderConfigurationRepository, EfSamlIdentityProviderConfigurationRepository>();
            services.AddScoped<ISsoSignInEnforcementRepository, EfSsoSignInEnforcementRepository>();
            services.AddScoped<IScimProvisioningRepository, EfScimProvisioningRepository>();
            services.AddScoped<INoCuiAcknowledgementRepository, EfNoCuiAcknowledgementRepository>();
            services.AddScoped<INotificationPreferenceRepository, EfNotificationPreferenceRepository>();
            services.AddScoped<IDueDateReminderRepository, EfDueDateReminderRepository>();
            services.AddScoped<IAssignmentNotificationRepository, EfAssignmentNotificationRepository>();
            services.AddScoped<IAssignmentEmailDeliveryRepository, EfAssignmentEmailDeliveryRepository>();
            services.AddScoped<AssignmentEmailDeliveryService>();
            services.AddScoped<AssignmentNotificationService>();
            services.AddScoped<IDemoRequestRepository, EfDemoRequestRepository>();
            services.AddScoped<IDemoAppointmentRepository, EfDemoAppointmentRepository>();
            services.AddScoped<IDemoFollowUpRepository, EfDemoFollowUpRepository>();
            services.AddScoped<DemoRequestDeliveryService>();
            services.AddScoped<IReportRepository, EfReportRepository>();
            services.AddScoped<ISimpleReportExportRepository, EfSimpleReportExportRepository>();
            services.AddScoped<IContractObligationMatrixRepository, EfContractObligationMatrixRepository>();
            services.AddScoped<IAuditLogRepository, EfAuditLogRepository>();
            services.AddScoped<IAuditEventWriter, EfAuditEventWriter>();
            services.AddScoped<ICompanyProfileRepository, EfCompanyProfileRepository>();
            services.AddScoped<ICompanySizeEvaluationRepository, EfCompanySizeEvaluationRepository>();
            services.AddScoped<IContractRepository, EfContractRepository>();
            services.AddScoped<IContractSizeCheckRepository, EfContractSizeCheckRepository>();
            services.AddScoped<IExtractionJobWorkRepository, EfExtractionJobWorkRepository>();
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
            services.AddScoped<IComplianceOverviewRepository, EfComplianceOverviewRepository>();
            services.AddScoped<IComplianceChecklistRepository, EfComplianceChecklistRepository>();
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
            services.AddScoped<IPlatformTenantProvisioningRepository>(_ =>
                throw new InvalidOperationException("Platform tenant provisioning requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<IPlatformCustomerRepository>(_ =>
                throw new InvalidOperationException("Platform customer operations require ConnectionStrings:GccsDatabase to be configured."));
            services.AddSingleton<ITenantSubscriptionRepository, UnconfiguredTenantSubscriptionRepository>();
            services.AddScoped<IGovernmentCloudEnvironmentRepository>(_ =>
                throw new InvalidOperationException("Government cloud environment persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<IRegulatedTenantProvisioningRepository>(_ =>
                throw new InvalidOperationException("Regulated tenant provisioning persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<IGovernmentCloudReleaseReadinessRepository>(_ =>
                throw new InvalidOperationException("Government cloud release readiness persistence requires ConnectionStrings:GccsDatabase to be configured."));
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
            services.AddScoped<ITenantWorkspaceSelectionRepository>(_ =>
                throw new InvalidOperationException("Tenant workspace selection requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<ITenantInvitationRepository>(_ =>
                throw new InvalidOperationException("Tenant invitation persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<IInvitationDeliveryRepository>(_ =>
                throw new InvalidOperationException("Invitation delivery requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<ISamlIdentityProviderConfigurationRepository>(_ =>
                throw new InvalidOperationException("SAML identity provider persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<ISsoSignInEnforcementRepository>(_ =>
                throw new InvalidOperationException("SSO sign-in enforcement persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<IScimProvisioningRepository>(_ =>
                throw new InvalidOperationException("SCIM provisioning persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<INoCuiAcknowledgementRepository>(_ =>
                throw new InvalidOperationException("No-CUI acknowledgement persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<INotificationPreferenceRepository>(_ =>
                throw new InvalidOperationException("Notification preference persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<IDueDateReminderRepository>(_ =>
                throw new InvalidOperationException("Due-date reminder persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<IReportRepository>(_ =>
                throw new InvalidOperationException("Report persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<ISimpleReportExportRepository>(_ =>
                throw new InvalidOperationException("Simple report export persistence requires ConnectionStrings:GccsDatabase to be configured."));
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
            services.AddScoped<IComplianceOverviewRepository, EmptyComplianceOverviewRepository>();
            services.AddScoped<IComplianceChecklistRepository>(_ =>
                throw new InvalidOperationException("Compliance checklist persistence requires ConnectionStrings:GccsDatabase to be configured."));
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
            services.AddScoped<IDemoRequestRepository>(_ =>
                throw new InvalidOperationException("Demo request persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<IDemoAppointmentRepository>(_ =>
                throw new InvalidOperationException("Demo appointment persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<IDemoFollowUpRepository>(_ =>
                throw new InvalidOperationException("Demo follow-up persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<DemoRequestDeliveryService>();
        }

        return services;
    }

    public static IServiceCollection AddGccsDevelopmentTestingInfrastructure(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        services.AddScoped<DevelopmentTestingContextService>();

        var connectionString = configuration?.GetConnectionString("GccsDatabase");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddScoped<IDevelopmentTenantCatalogRepository, EfDevelopmentTenantCatalogRepository>();
        }
        else
        {
            services.AddScoped<IDevelopmentTenantCatalogRepository>(_ =>
                throw new InvalidOperationException(
                    "Development tenant catalog requires ConnectionStrings:GccsDatabase to be configured."));
        }

        return services;
    }

    private static int ReadInt(IConfiguration configuration, string key, int fallback) =>
        int.TryParse(configuration[key], out var value) ? value : fallback;

    private static bool ReadBool(IConfiguration configuration, string key, bool fallback) =>
        bool.TryParse(configuration[key], out var value) ? value : fallback;
}

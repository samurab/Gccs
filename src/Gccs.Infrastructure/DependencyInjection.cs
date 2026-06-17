using Gccs.Application.Audit;
using Gccs.Application.Calendar;
using Gccs.Application.Companies;
using Gccs.Application.Cmmc;
using Gccs.Application.Compliance;
using Gccs.Application.Contracts;
using Gccs.Application.Evidence;
using Gccs.Application.Identity;
using Gccs.Application.NoCui;
using Gccs.Application.Notifications;
using Gccs.Application.Repositories;
using Gccs.Application.Reports;
using Gccs.Application.Subcontractors;
using Gccs.Application.Tasks;
using Gccs.Application.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Calendar;
using Gccs.Infrastructure.Companies;
using Gccs.Infrastructure.Cmmc;
using Gccs.Infrastructure.Compliance;
using Gccs.Infrastructure.Contracts;
using Gccs.Infrastructure.Evidence;
using Gccs.Infrastructure.Identity;
using Gccs.Infrastructure.NoCui;
using Gccs.Infrastructure.Notifications;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Reports;
using Gccs.Infrastructure.Subcontractors;
using Gccs.Infrastructure.Tenancy;
using Gccs.Infrastructure.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gccs.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddGccsInfrastructure(this IServiceCollection services, IConfiguration? configuration = null)
    {
        services.AddScoped<ComplianceOverviewService>();
        services.AddScoped<ComplianceContentReviewService>();
        services.AddScoped<ClauseLibraryService>();
        services.AddScoped<ObligationDetailService>();
        services.AddScoped<CompanyProfileService>();
        services.AddScoped<ContractService>();
        services.AddScoped<TenantService>();
        services.AddScoped<TenantMembershipService>();
        services.AddScoped<TenantInvitationService>();
        services.AddScoped<NoCuiAcknowledgementService>();
        services.AddScoped<NotificationPreferenceService>();
        services.AddScoped<DueDateReminderService>();
        services.AddScoped<AuditLogService>();
        services.AddScoped<ComplianceTaskService>();
        services.AddScoped<RenewalGenerationService>();
        services.AddScoped<EvidenceMetadataService>();
        services.AddScoped<EvidenceApprovalService>();
        services.AddScoped<CmmcAssessmentService>();
        services.AddScoped<CmmcPoamService>();
        services.AddScoped<CmmcAffirmationService>();
        services.AddScoped<SubcontractorService>();
        services.AddScoped<ComplianceStatusReportService>();
        services.AddScoped<CmmcReadinessReportService>();
        services.AddScoped<EvidencePackageReportService>();
        services.AddScoped<SubcontractorComplianceReportService>();

        var connectionString = configuration?.GetConnectionString("GccsDatabase");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<GccsDbContext>(options =>
                options.UseNpgsql(connectionString, npgsql =>
                    npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "gccs")));

            services.AddScoped<ITenantRepository, EfTenantRepository>();
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
            services.AddScoped<IContractRepository, EfContractRepository>();
            services.AddScoped<IExtractionJobQueue, NoOpExtractionJobQueue>();
            services.AddScoped<IContractDocumentTextExtractor, DefaultContractDocumentTextExtractor>();
            services.AddScoped<IComplianceContentImporter, ComplianceContentImporter>();
            services.AddScoped<IComplianceContentReviewRepository, EfComplianceContentReviewRepository>();
            services.AddScoped<IClauseLibraryRepository, EfClauseLibraryRepository>();
            services.AddScoped<IObligationDashboardRepository, EfObligationDashboardRepository>();
            services.AddScoped<IObligationDetailRepository, EfObligationDetailRepository>();
            services.AddScoped<IObligationRepository, EfObligationRepository>();
            services.AddScoped<IComplianceTaskRepository, EfComplianceTaskRepository>();
            services.AddScoped<IRenewalTaskRepository, EfRenewalTaskRepository>();
            services.AddScoped<ICalendarRepository, EfCalendarRepository>();
            services.AddScoped<IEvidenceMetadataRepository, EfEvidenceMetadataRepository>();
            services.AddScoped<ICmmcAssessmentRepository, EfCmmcAssessmentRepository>();
            services.AddScoped<ICmmcPoamRepository, EfCmmcPoamRepository>();
            services.AddScoped<ICmmcAffirmationRepository, EfCmmcAffirmationRepository>();
            services.AddScoped<ISubcontractorRepository, EfSubcontractorRepository>();
        }
        else
        {
            services.AddSingleton<IClauseLibraryRepository, InMemoryClauseLibraryRepository>();
            services.AddSingleton<IObligationRepository, InMemoryObligationRepository>();
            services.AddScoped<ITenantRepository>(_ =>
                throw new InvalidOperationException("Tenant persistence requires ConnectionStrings:GccsDatabase to be configured."));
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
            services.AddScoped<IContractRepository>(_ =>
                throw new InvalidOperationException("Contract persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<IExtractionJobQueue, NoOpExtractionJobQueue>();
            services.AddScoped<IContractDocumentTextExtractor, DefaultContractDocumentTextExtractor>();
            services.AddScoped<IComplianceContentImporter>(_ =>
                throw new InvalidOperationException("Compliance content import requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<IComplianceContentReviewRepository>(_ =>
                throw new InvalidOperationException("Compliance content review persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<IObligationDashboardRepository>(_ =>
                throw new InvalidOperationException("Obligation dashboard persistence requires ConnectionStrings:GccsDatabase to be configured."));
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
            services.AddScoped<ICmmcAssessmentRepository>(_ =>
                throw new InvalidOperationException("CMMC assessment persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<ICmmcPoamRepository>(_ =>
                throw new InvalidOperationException("CMMC POA&M persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<ICmmcAffirmationRepository>(_ =>
                throw new InvalidOperationException("CMMC affirmation persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<ISubcontractorRepository>(_ =>
                throw new InvalidOperationException("Subcontractor persistence requires ConnectionStrings:GccsDatabase to be configured."));
        }

        return services;
    }
}

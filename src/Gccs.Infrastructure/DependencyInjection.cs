using Gccs.Application.Audit;
using Gccs.Application.Companies;
using Gccs.Application.Compliance;
using Gccs.Application.Contracts;
using Gccs.Application.Identity;
using Gccs.Application.NoCui;
using Gccs.Application.Repositories;
using Gccs.Application.Reports;
using Gccs.Application.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Companies;
using Gccs.Infrastructure.Compliance;
using Gccs.Infrastructure.Contracts;
using Gccs.Infrastructure.Identity;
using Gccs.Infrastructure.NoCui;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Reports;
using Gccs.Infrastructure.Tenancy;
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
        services.AddScoped<CompanyProfileService>();
        services.AddScoped<ContractService>();
        services.AddScoped<TenantService>();
        services.AddScoped<TenantMembershipService>();
        services.AddScoped<TenantInvitationService>();
        services.AddScoped<NoCuiAcknowledgementService>();
        services.AddScoped<AuditLogService>();

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
            services.AddScoped<IReportRepository, EfReportRepository>();
            services.AddScoped<IAuditLogRepository, EfAuditLogRepository>();
            services.AddScoped<IAuditEventWriter, EfAuditEventWriter>();
            services.AddScoped<ICompanyProfileRepository, EfCompanyProfileRepository>();
            services.AddScoped<IContractRepository, EfContractRepository>();
            services.AddScoped<IComplianceContentImporter, ComplianceContentImporter>();
            services.AddScoped<IComplianceContentReviewRepository, EfComplianceContentReviewRepository>();
            services.AddScoped<IObligationRepository, EfObligationRepository>();
        }
        else
        {
            services.AddSingleton<IObligationRepository, InMemoryObligationRepository>();
            services.AddScoped<ITenantRepository>(_ =>
                throw new InvalidOperationException("Tenant persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<ITenantMembershipRepository>(_ =>
                throw new InvalidOperationException("Tenant membership persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<ITenantInvitationRepository>(_ =>
                throw new InvalidOperationException("Tenant invitation persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<INoCuiAcknowledgementRepository>(_ =>
                throw new InvalidOperationException("No-CUI acknowledgement persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<IReportRepository>(_ =>
                throw new InvalidOperationException("Report persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<IAuditLogRepository>(_ =>
                throw new InvalidOperationException("Audit log persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<IAuditEventWriter>(_ =>
                throw new InvalidOperationException("Audit persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<ICompanyProfileRepository>(_ =>
                throw new InvalidOperationException("Company profile persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<IContractRepository>(_ =>
                throw new InvalidOperationException("Contract persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<IComplianceContentImporter>(_ =>
                throw new InvalidOperationException("Compliance content import requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<IComplianceContentReviewRepository>(_ =>
                throw new InvalidOperationException("Compliance content review persistence requires ConnectionStrings:GccsDatabase to be configured."));
        }

        return services;
    }
}

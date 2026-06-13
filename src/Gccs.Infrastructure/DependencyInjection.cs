using Gccs.Application.Audit;
using Gccs.Application.Compliance;
using Gccs.Application.Repositories;
using Gccs.Application.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Compliance;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gccs.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddGccsInfrastructure(this IServiceCollection services, IConfiguration? configuration = null)
    {
        services.AddSingleton<IObligationRepository, InMemoryObligationRepository>();
        services.AddScoped<ComplianceOverviewService>();
        services.AddScoped<TenantService>();

        var connectionString = configuration?.GetConnectionString("GccsDatabase");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<GccsDbContext>(options =>
                options.UseNpgsql(connectionString, npgsql =>
                    npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "gccs")));

            services.AddScoped<ITenantRepository, EfTenantRepository>();
            services.AddScoped<IAuditEventWriter, EfAuditEventWriter>();
        }
        else
        {
            services.AddScoped<ITenantRepository>(_ =>
                throw new InvalidOperationException("Tenant persistence requires ConnectionStrings:GccsDatabase to be configured."));
            services.AddScoped<IAuditEventWriter>(_ =>
                throw new InvalidOperationException("Audit persistence requires ConnectionStrings:GccsDatabase to be configured."));
        }

        return services;
    }
}

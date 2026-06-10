using Gccs.Application.Compliance;
using Gccs.Application.Repositories;
using Gccs.Infrastructure.Compliance;
using Gccs.Infrastructure.Persistence;
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

        var connectionString = configuration?.GetConnectionString("GccsDatabase");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<GccsDbContext>(options =>
                options.UseNpgsql(connectionString, npgsql =>
                    npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "gccs")));
        }

        return services;
    }
}

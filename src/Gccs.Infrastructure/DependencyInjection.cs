using Gccs.Application.Compliance;
using Gccs.Application.Repositories;
using Gccs.Infrastructure.Compliance;
using Microsoft.Extensions.DependencyInjection;

namespace Gccs.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddGccsInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IObligationRepository, InMemoryObligationRepository>();
        services.AddScoped<ComplianceOverviewService>();

        return services;
    }
}

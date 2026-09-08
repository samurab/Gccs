using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Persistence;

public static class GccsPostgresOptions
{
    public static DbContextOptionsBuilder UseGccsPostgres(this DbContextOptionsBuilder options, string connectionString) =>
        options.UseNpgsql(connectionString, provider => provider.MigrationsHistoryTable("__EFMigrationsHistory", "gccs"));

    public static DbContextOptionsBuilder<TContext> UseGccsPostgres<TContext>(
        this DbContextOptionsBuilder<TContext> options, string connectionString) where TContext : DbContext
    {
        ((DbContextOptionsBuilder)options).UseGccsPostgres(connectionString);
        return options;
    }
}

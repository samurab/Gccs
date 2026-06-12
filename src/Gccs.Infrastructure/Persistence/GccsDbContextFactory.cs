using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Gccs.Infrastructure.Persistence;

public sealed class GccsDbContextFactory : IDesignTimeDbContextFactory<GccsDbContext>
{
    private const string DevelopmentConnectionString =
        "Host=localhost;Port=15432;Database=gccs;Username=gccs;Password=gccs_dev_password";

    public GccsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("GCCS_DATABASE")
            ?? DevelopmentConnectionString;

        var options = new DbContextOptionsBuilder<GccsDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "gccs"))
            .Options;

        return new GccsDbContext(options);
    }
}

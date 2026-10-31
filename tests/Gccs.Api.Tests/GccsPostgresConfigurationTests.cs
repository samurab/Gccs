using Gccs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class GccsPostgresConfigurationTests
{
    [Fact]
    public void Runtime_options_use_the_canonical_migration_history_schema()
    {
        var options = new DbContextOptionsBuilder<GccsDbContext>();
        ((DbContextOptionsBuilder)options).UseGccsPostgres("Host=localhost;Database=configuration_only");
        using var db = new GccsDbContext(options.Options);
        AssertCanonicalHistory(db);
    }

    [Fact]
    public void Generic_test_options_use_the_canonical_migration_history_schema()
    {
        using var db = new GccsDbContext(new DbContextOptionsBuilder<GccsDbContext>()
            .UseGccsPostgres("Host=localhost;Database=configuration_only").Options);
        AssertCanonicalHistory(db);
    }

    [Fact]
    public void Design_time_options_use_the_canonical_migration_history_schema()
    {
        using var db = new GccsDbContextFactory().CreateDbContext([]);
        AssertCanonicalHistory(db);
    }

    private static void AssertCanonicalHistory(GccsDbContext db) =>
        Assert.Matches("(?:\"gccs\"|gccs)\\.\"__EFMigrationsHistory\"", db.GetService<IHistoryRepository>().GetCreateScript());
}

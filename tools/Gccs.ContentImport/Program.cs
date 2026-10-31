using Gccs.Application.Compliance;
using Gccs.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var configuration = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .AddCommandLine(args)
    .Build();

if (configuration.GetValue<bool>("help") || configuration.GetValue<bool>("h"))
{
    PrintUsage();
    return 0;
}

var connectionString = FirstNonEmpty(
    configuration["connection-string"],
    configuration.GetConnectionString("GccsDatabase"),
    configuration["ConnectionStrings:GccsDatabase"]);
var packageRoot = configuration["package-root"] ?? FindDefaultPackageRoot();
var requireStagingConfirmation = configuration.GetValue("require-staging-confirmation", true);
var confirmedStaging = configuration.GetValue<bool>("confirm-staging");

if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("Missing required PostgreSQL connection string. Pass --connection-string or set ConnectionStrings__GccsDatabase.");
    PrintUsage();
    return 2;
}

if (!Directory.Exists(packageRoot))
{
    Console.Error.WriteLine($"Compliance content package root was not found: {packageRoot}");
    return 2;
}

if (requireStagingConfirmation && !confirmedStaging)
{
    Console.Error.WriteLine("Refusing to import without --confirm-staging true. This tool is intended for explicit staging operations.");
    return 2;
}

using var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        var importConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:GccsDatabase"] = connectionString
            })
            .Build();

        services.AddGccsInfrastructure(importConfiguration);
    })
    .Build();

using var scope = host.Services.CreateScope();
var importer = scope.ServiceProvider.GetRequiredService<IComplianceContentImporter>();
var report = await importer.ImportDirectoryAsync(packageRoot);

foreach (var log in report.Logs)
{
    Console.WriteLine(log);
}

Console.WriteLine($"Files processed: {report.FilesProcessed}");
Console.WriteLine($"Clauses created/updated: {report.ClausesCreated}/{report.ClausesUpdated}");
Console.WriteLine($"Mappings created/updated: {report.ClauseObligationMappingsCreated}/{report.ClauseObligationMappingsUpdated}");
Console.WriteLine($"Obligations created/updated: {report.ObligationsCreated}/{report.ObligationsUpdated}");

if (report.Succeeded)
{
    Console.WriteLine("Compliance content import completed successfully.");
    return 0;
}

Console.Error.WriteLine("Compliance content import failed.");
foreach (var error in report.Errors)
{
    Console.Error.WriteLine($"{error.File} {error.Path} {error.Field}: {error.Message}");
}

return 1;

static string? FirstNonEmpty(params string?[] values) =>
    values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

static string FindDefaultPackageRoot()
{
    var current = new DirectoryInfo(AppContext.BaseDirectory);
    while (current is not null && !File.Exists(Path.Combine(current.FullName, "Gccs.slnx")))
    {
        current = current.Parent;
    }

    if (current is null)
    {
        return Path.Combine(Environment.CurrentDirectory, "packages", "compliance-content");
    }

    return Path.Combine(current.FullName, "packages", "compliance-content");
}

static void PrintUsage()
{
    Console.WriteLine("""
    GCCS compliance content import

    Required:
      --connection-string "<postgres connection string>"
      --confirm-staging true

    Optional:
      --package-root "/path/to/packages/compliance-content"
      --require-staging-confirmation false

    Environment alternative:
      ConnectionStrings__GccsDatabase="<postgres connection string>"

    This tool imports source-backed compliance content into the configured database. Use synthetic/staging environments unless production approval exists.
    """);
}

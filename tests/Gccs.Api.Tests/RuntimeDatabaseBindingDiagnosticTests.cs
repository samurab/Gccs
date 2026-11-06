using System.Data;
using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Gccs.Application.Operations;
using Gccs.Infrastructure;
using Gccs.Infrastructure.Operations;
using Gccs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class RuntimeDatabaseBindingDiagnosticTests
{
    private const string Server = "db.diagnostic.example";
    private const string Database = "DiagnosticDatabase";
    private const string Sentinel = "sentinel-credential-do-not-disclose";

    [Theory]
    [InlineData("db.diagnostic.example", 5432, "DiagnosticDatabase", true, true, true)]
    [InlineData("DB.DIAGNOSTIC.EXAMPLE.", 5432, "DiagnosticDatabase", true, true, true)]
    [InlineData("other.diagnostic.example", 5432, "DiagnosticDatabase", false, true, true)]
    [InlineData("db.diagnostic.example", 5433, "DiagnosticDatabase", true, false, true)]
    [InlineData("db.diagnostic.example", 5432, "diagnosticdatabase", true, true, false)]
    public void Closed_Npgsql_metadata_is_compared_without_opening_or_disposing_the_connection(
        string server, int port, string database, bool serverMatches, bool portMatches, bool databaseMatches)
    {
        using var connection = Connection(server, port, database);
        var fixtureConnectionString = connection.ConnectionString;
        var fixtureDataSource = connection.DataSource;
        var interceptor = new RejectConnectionOpen();
        var options = new DbContextOptionsBuilder<GccsDbContext>()
            .UseNpgsql(connection, contextOwnsConnection: false)
            .AddInterceptors(interceptor).Options;

        using (var context = new GccsDbContext(options))
        using (var provider = new ServiceCollection().AddSingleton(context).BuildServiceProvider())
        {
            var result = new EfRuntimeDatabaseBindingDiagnostic(provider, databaseWasExplicitlyConfigured: true).Observe(Expected());

            Assert.Equal(serverMatches && portMatches && databaseMatches
                ? RuntimeDatabaseBindingStatus.Matched : RuntimeDatabaseBindingStatus.Mismatch, result.Status);
            Assert.True(result.ProviderMatches);
            Assert.Equal(serverMatches, result.ServerMatches);
            Assert.Equal(portMatches, result.PortMatches);
            Assert.Equal(databaseMatches, result.DatabaseMatches);
            Assert.Same(connection, context.Database.GetDbConnection());
            Assert.Equal(ConnectionState.Closed, connection.State);
            Assert.Equal(0, interceptor.Attempts);
            AssertAllowlisted(result);
        }

        // Reconfigure metadata only: the externally owned connection remains reusable after context disposal.
        connection.ConnectionString = fixtureConnectionString;
        Assert.Equal(fixtureDataSource, connection.DataSource);
        Assert.Equal(database, connection.Database);
        Assert.Equal(ConnectionState.Closed, connection.State);
        Assert.Equal(0, interceptor.Attempts);
    }

    [Fact]
    public void Npgsql_rejects_empty_host_before_a_diagnostic_context_can_be_created()
    {
        Assert.Throws<ArgumentNullException>(() => Connection(string.Empty, 5432, Database));
    }

    [Theory]
    [InlineData("localhost")]
    [InlineData("db.diagnostic.example,other.diagnostic.example")]
    [InlineData("/tmp/diagnostic.socket")]
    [InlineData("db..diagnostic.example")]
    [InlineData("db.diagnostic.example..")]
    [InlineData("db_diagnostic.example")]
    [InlineData("db.diagnostic.example ")]
    [InlineData("127.0.0.1")]
    public void Unsupported_host_metadata_is_unavailable_without_opening(string server)
    {
        using var connection = Connection(server, 5432, Database);
        var interceptor = new RejectConnectionOpen();
        using var context = new GccsDbContext(new DbContextOptionsBuilder<GccsDbContext>()
            .UseNpgsql(connection, contextOwnsConnection: false).AddInterceptors(interceptor).Options);
        using var provider = new ServiceCollection().AddSingleton(context).BuildServiceProvider();

        var result = new EfRuntimeDatabaseBindingDiagnostic(provider, databaseWasExplicitlyConfigured: true).Observe(Expected());

        Assert.Equal(RuntimeDatabaseBindingStatus.Unavailable, result.Status);
        Assert.True(result.ProviderMatches);
        Assert.Null(result.ServerMatches);
        Assert.Null(result.DatabaseMatches);
        Assert.Equal(ConnectionState.Closed, connection.State);
        Assert.Equal(0, interceptor.Attempts);
        AssertAllowlisted(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Missing_database_is_not_inferred_from_the_username(string? database)
    {
        using var connection = Connection(Server, 5432, database);
        var interceptor = new RejectConnectionOpen();
        using var context = new GccsDbContext(new DbContextOptionsBuilder<GccsDbContext>()
            .UseNpgsql(connection, contextOwnsConnection: false).AddInterceptors(interceptor).Options);
        using var provider = new ServiceCollection().AddSingleton(context).BuildServiceProvider();

        var result = new EfRuntimeDatabaseBindingDiagnostic(provider, databaseWasExplicitlyConfigured: false).Observe(Expected());

        Assert.Equal(RuntimeDatabaseBindingStatus.Unavailable, result.Status);
        Assert.Null(result.DatabaseMatches);
        Assert.Equal(ConnectionState.Closed, connection.State);
        Assert.Equal(0, interceptor.Attempts);
        AssertAllowlisted(result);
    }

    [Fact]
    public void Missing_context_is_unavailable()
    {
        using var provider = new ServiceCollection().BuildServiceProvider();
        var result = new EfRuntimeDatabaseBindingDiagnostic(provider, databaseWasExplicitlyConfigured: true).Observe(Expected());

        Assert.Equal(new RuntimeDatabaseBindingResult(RuntimeDatabaseBindingStatus.Unavailable), result);
        AssertAllowlisted(result);
    }

    [Fact]
    public void Non_Npgsql_provider_is_a_mismatch_without_a_relational_connection_lookup()
    {
        using var context = new GccsDbContext(new DbContextOptionsBuilder<GccsDbContext>()
            .UseInMemoryDatabase("d01-diagnostic-provider-only").Options);
        using var provider = new ServiceCollection().AddSingleton(context).BuildServiceProvider();

        var result = new EfRuntimeDatabaseBindingDiagnostic(provider, databaseWasExplicitlyConfigured: true).Observe(Expected());

        Assert.Equal(new RuntimeDatabaseBindingResult(RuntimeDatabaseBindingStatus.Mismatch, ProviderMatches: false), result);
        AssertAllowlisted(result);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("ABC", "abc")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA", "0000000000000000000000000000000000000000000000000000000000000000")]
    [InlineData("gggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggg", "0000000000000000000000000000000000000000000000000000000000000000")]
    public void Invalid_expectations_do_not_resolve_a_context(string serverDigest, string databaseDigest)
    {
        var resolutions = 0;
        using var provider = new ServiceCollection().AddScoped<GccsDbContext>(_ =>
        {
            resolutions++;
            throw new InvalidOperationException(Sentinel);
        }).BuildServiceProvider();

        var result = new EfRuntimeDatabaseBindingDiagnostic(provider, databaseWasExplicitlyConfigured: true)
            .Observe(new(serverDigest, databaseDigest));

        Assert.Equal(new RuntimeDatabaseBindingResult(RuntimeDatabaseBindingStatus.InvalidConfiguration), result);
        Assert.Equal(0, resolutions);
        AssertAllowlisted(result);
    }

    [Fact]
    public void Context_resolution_failure_returns_only_fixed_failure_status()
    {
        using var provider = new ServiceCollection().AddScoped<GccsDbContext>(_ =>
            throw new InvalidOperationException(Sentinel)).BuildServiceProvider();

        var result = new EfRuntimeDatabaseBindingDiagnostic(provider, databaseWasExplicitlyConfigured: true).Observe(Expected());

        Assert.Equal(new RuntimeDatabaseBindingResult(RuntimeDatabaseBindingStatus.Failed), result);
        AssertAllowlisted(result);
    }

    [Fact]
    public void Existing_infrastructure_registration_resolves_the_diagnostic_without_configured_persistence()
    {
        var services = new ServiceCollection();
        services.AddGccsInfrastructure(new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["ConnectionStrings:GccsDatabase"] = string.Empty }).Build());
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var result = scope.ServiceProvider.GetRequiredService<IRuntimeDatabaseBindingDiagnostic>().Observe(Expected());

        Assert.Equal(new RuntimeDatabaseBindingResult(RuntimeDatabaseBindingStatus.Unavailable), result);
        AssertAllowlisted(result);
    }

    [Theory]
    [InlineData(null, RuntimeDatabaseBindingStatus.Unavailable)]
    [InlineData("", RuntimeDatabaseBindingStatus.Unavailable)]
    [InlineData(Database, RuntimeDatabaseBindingStatus.Matched)]
    public void Infrastructure_registration_binds_explicit_database_from_the_same_configuration_input(
        string? database, RuntimeDatabaseBindingStatus expectedStatus)
    {
        var builder = new NpgsqlConnectionStringBuilder { Host = Server, Username = Database, Password = Sentinel };
        if (database is not null)
            builder.Database = database;
        var services = new ServiceCollection();
        services.AddGccsInfrastructure(new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["ConnectionStrings:GccsDatabase"] = builder.ConnectionString }).Build());
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var result = scope.ServiceProvider.GetRequiredService<IRuntimeDatabaseBindingDiagnostic>().Observe(Expected());

        Assert.Equal(expectedStatus, result.Status);
        Assert.True(result.ProviderMatches);
        if (expectedStatus == RuntimeDatabaseBindingStatus.Unavailable)
            Assert.Null(result.DatabaseMatches);
        Assert.Equal(ConnectionState.Closed, scope.ServiceProvider.GetRequiredService<GccsDbContext>().Database.GetDbConnection().State);
        AssertAllowlisted(result);
    }

    private static RuntimeDatabaseBindingExpectation Expected() => new(Digest(Server), Digest(Database));

    private static string Digest(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static NpgsqlConnection Connection(string server, int port, string? database)
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = server, Port = port, Username = Sentinel, Password = Sentinel
        };
        if (database is not null)
            builder.Database = database;
        return new(builder.ConnectionString);
    }

    private static void AssertAllowlisted(RuntimeDatabaseBindingResult result)
    {
        var json = JsonSerializer.Serialize(result);
        var properties = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;
        Assert.Equal(new[] { "DatabaseMatches", "PortMatches", "ProviderMatches", "ServerMatches", "Status" },
            properties.Keys.OrderBy(key => key, StringComparer.Ordinal).ToArray());
        Assert.DoesNotContain(Sentinel, json);
        Assert.DoesNotContain(Server, json);
        Assert.DoesNotContain(Database, json);
        Assert.DoesNotContain(Sentinel, result.ToString());
    }

    private sealed class RejectConnectionOpen : DbConnectionInterceptor
    {
        public int Attempts { get; private set; }

        public override InterceptionResult ConnectionOpening(
            DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
        {
            Attempts++;
            throw new InvalidOperationException("The offline diagnostic must not open a database connection.");
        }

        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(
            DbConnection connection, ConnectionEventData eventData, InterceptionResult result,
            CancellationToken cancellationToken = default)
        {
            Attempts++;
            throw new InvalidOperationException("The offline diagnostic must not open a database connection.");
        }
    }
}

using System.Security.Cryptography;
using System.Text;
using Gccs.Application.Operations;
using Gccs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Gccs.Infrastructure.Operations;

public sealed class EfRuntimeDatabaseBindingDiagnostic(
    IServiceProvider services, bool databaseWasExplicitlyConfigured) : IRuntimeDatabaseBindingDiagnostic
{
    public RuntimeDatabaseBindingResult Observe(RuntimeDatabaseBindingExpectation expectation)
    {
        if (!expectation.IsValid)
            return new(RuntimeDatabaseBindingStatus.InvalidConfiguration);

        try
        {
            var context = services.GetService<GccsDbContext>();
            if (context is null)
                return new(RuntimeDatabaseBindingStatus.Unavailable);

            if (context.Database.ProviderName != "Npgsql.EntityFrameworkCore.PostgreSQL")
                return new(RuntimeDatabaseBindingStatus.Mismatch, ProviderMatches: false);

            if (context.Database.GetDbConnection() is not NpgsqlConnection connection)
                return new(RuntimeDatabaseBindingStatus.Unavailable, ProviderMatches: true);

            // Npgsql's Database getter falls back to Username when the database key was omitted.
            if (!databaseWasExplicitlyConfigured)
                return new(RuntimeDatabaseBindingStatus.Unavailable, ProviderMatches: true);

            // Npgsql Host/Port describe an open connector; DataSource also describes a closed connection.
            var dataSource = connection.DataSource;
            var database = connection.Database;
            if (string.IsNullOrEmpty(database) || dataSource.Any(character => character > 127 || char.IsWhiteSpace(character)) ||
                !Uri.TryCreate(dataSource, UriKind.Absolute, out var source) || source.Scheme != "tcp" ||
                source.UserInfo.Length != 0 || source.Query.Length != 0 || source.Fragment.Length != 0 ||
                source.AbsolutePath is not ("" or "/") || source.Port <= 0)
                return new(RuntimeDatabaseBindingStatus.Unavailable, ProviderMatches: true);

            var host = source.Host;
            var normalizedHost = host.EndsWith('.') ? host[..^1] : host;
            if (normalizedHost.Length is 0 or > 253 || !normalizedHost.Contains('.') ||
                normalizedHost.Any(character => character > 127 || char.IsWhiteSpace(character)) ||
                normalizedHost.EndsWith('.') || Uri.CheckHostName(normalizedHost) != UriHostNameType.Dns ||
                normalizedHost.Split('.').Any(label => label.Length is 0 or > 63 ||
                    label.StartsWith('-') || label.EndsWith('-') ||
                    label.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '-')))
                return new(RuntimeDatabaseBindingStatus.Unavailable, ProviderMatches: true);

            var serverMatches = Digest(normalizedHost.ToLowerInvariant()) == expectation.ServerSha256;
            var portMatches = source.Port == 5432;
            var databaseMatches = Digest(database) == expectation.DatabaseSha256;
            return new(
                serverMatches && portMatches && databaseMatches
                    ? RuntimeDatabaseBindingStatus.Matched
                    : RuntimeDatabaseBindingStatus.Mismatch,
                true, serverMatches, portMatches, databaseMatches);
        }
        catch (Exception)
        {
            // Metadata failures can contain connection values; return only a fixed status.
            return new(RuntimeDatabaseBindingStatus.Failed);
        }
    }

    private static string Digest(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}

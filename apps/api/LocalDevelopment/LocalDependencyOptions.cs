using Microsoft.Extensions.Configuration;

namespace Gccs.Api.LocalDevelopment;

public sealed class LocalDependencyOptions
{
    public const string SectionName = "LocalDependencies";

    public bool Enabled { get; init; }

    public RedisDependencyOptions Redis { get; init; } = new();

    public ObjectStorageDependencyOptions ObjectStorage { get; init; } = new();

    public MalwareScannerDependencyOptions MalwareScanner { get; init; } = new();

    public static void ValidateRequiredConfiguration(IConfiguration configuration)
    {
        if (!configuration.GetValue<bool>($"{SectionName}:Enabled"))
        {
            return;
        }

        var missingKeys = new List<string>();

        AddMissingKey(missingKeys, "ConnectionStrings:GccsDatabase", configuration.GetConnectionString("GccsDatabase"));
        AddMissingKey(missingKeys, $"{SectionName}:Redis:ConnectionString", configuration[$"{SectionName}:Redis:ConnectionString"]);
        AddMissingKey(missingKeys, $"{SectionName}:ObjectStorage:Endpoint", configuration[$"{SectionName}:ObjectStorage:Endpoint"]);
        AddMissingKey(missingKeys, $"{SectionName}:ObjectStorage:Bucket", configuration[$"{SectionName}:ObjectStorage:Bucket"]);
        AddMissingKey(missingKeys, $"{SectionName}:ObjectStorage:AccessKey", configuration[$"{SectionName}:ObjectStorage:AccessKey"]);
        AddMissingKey(missingKeys, $"{SectionName}:ObjectStorage:SecretKey", configuration[$"{SectionName}:ObjectStorage:SecretKey"]);
        AddMissingKey(missingKeys, $"{SectionName}:MalwareScanner:Host", configuration[$"{SectionName}:MalwareScanner:Host"]);

        if (configuration.GetValue<int?>($"{SectionName}:MalwareScanner:Port") is null or <= 0)
        {
            missingKeys.Add($"{SectionName}:MalwareScanner:Port");
        }

        if (missingKeys.Count > 0)
        {
            var missingEnvironmentVariables = missingKeys
                .Select(key => key.Replace(":", "__", StringComparison.Ordinal))
                .ToArray();

            throw new InvalidOperationException(
                "Local dependency configuration is incomplete. Set these development configuration keys: " +
                string.Join(", ", missingKeys) +
                ". Equivalent environment variables: " +
                string.Join(", ", missingEnvironmentVariables) +
                ". Use .env.example or apps/api/appsettings.Development.json as the local template; do not use production secrets.");
        }
    }

    private static void AddMissingKey(List<string> missingKeys, string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            missingKeys.Add(key);
        }
    }
}

public sealed class RedisDependencyOptions
{
    public string ConnectionString { get; init; } = string.Empty;
}

public sealed class ObjectStorageDependencyOptions
{
    public string Endpoint { get; init; } = string.Empty;

    public string Bucket { get; init; } = string.Empty;

    public string AccessKey { get; init; } = string.Empty;

    public string SecretKey { get; init; } = string.Empty;
}

public sealed class MalwareScannerDependencyOptions
{
    public string Host { get; init; } = string.Empty;

    public int Port { get; init; }
}

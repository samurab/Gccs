using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using Xunit.Sdk;

namespace Gccs.Api.Tests;

public sealed class LocalDependencyConfigurationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly string[] RequiredDependencyNames =
    [
        "postgresql",
        "redis",
        "object-storage",
        "malware-scanner",
        "background-jobs"
    ];

    private static readonly string[] ComposeServiceNames =
    [
        "postgres",
        "redis",
        "minio",
        "minio-init",
        "clamav"
    ];

    private readonly WebApplicationFactory<Program> _factory;

    public LocalDependencyConfigurationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    public static TheoryData<string, string, string> RequiredLocalDependencyConfiguration =>
        new()
        {
            { "ConnectionStrings:GccsDatabase", string.Empty, "ConnectionStrings__GccsDatabase" },
            { "LocalDependencies:Redis:ConnectionString", string.Empty, "LocalDependencies__Redis__ConnectionString" },
            { "LocalDependencies:ObjectStorage:Endpoint", string.Empty, "LocalDependencies__ObjectStorage__Endpoint" },
            { "LocalDependencies:ObjectStorage:Bucket", string.Empty, "LocalDependencies__ObjectStorage__Bucket" },
            { "LocalDependencies:ObjectStorage:AccessKey", string.Empty, "LocalDependencies__ObjectStorage__AccessKey" },
            { "LocalDependencies:ObjectStorage:SecretKey", string.Empty, "LocalDependencies__ObjectStorage__SecretKey" },
            { "LocalDependencies:MalwareScanner:Host", string.Empty, "LocalDependencies__MalwareScanner__Host" },
            { "LocalDependencies:MalwareScanner:Port", "0", "LocalDependencies__MalwareScanner__Port" }
        };

    [Fact(Timeout = 180_000)]
    public async Task Documented_one_command_local_services_startup_reports_all_services_healthy()
    {
        EnsureDockerAvailable();
        var repoRoot = GetRepositoryRoot();
        var readme = await File.ReadAllTextAsync(Path.Combine(repoRoot, "README.md"));

        Assert.Contains("docker compose -f infra/docker/docker-compose.yml up -d --wait", readme);

        var startup = await RunProcessAsync(
            "docker",
            "compose -f infra/docker/docker-compose.yml up -d --wait",
            repoRoot,
            TimeSpan.FromMinutes(3));

        Assert.True(startup.ExitCode == 0, FormatProcessFailure(startup));

        var ps = await RunProcessAsync(
            "docker",
            "compose -f infra/docker/docker-compose.yml ps --format json",
            repoRoot,
            TimeSpan.FromSeconds(30));

        Assert.True(ps.ExitCode == 0, FormatProcessFailure(ps));

        var serviceHealth = ParseComposeServices(ps.StandardOutput)
            .ToDictionary(service => service.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var serviceName in ComposeServiceNames)
        {
            Assert.True(serviceHealth.TryGetValue(serviceName, out var service), $"Compose service '{serviceName}' was not started.");
            Assert.Equal("running", service.State);
            Assert.Equal("healthy", service.Health);
        }
    }

    [Fact(Timeout = 60_000)]
    public async Task Api_health_reports_connectivity_for_local_database_cache_storage_and_scanner()
    {
        EnsureDockerAvailable();
        await EnsureLocalServicesAreRunningAsync();

        using var client = CreateFactoryWithLocalDependencyConfiguration().CreateClient();

        var response = await client.GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("ok", root.GetProperty("status").GetString());
        Assert.Equal("gccs-api", root.GetProperty("service").GetString());
        Assert.Equal("No-CUI / compliance management only", root.GetProperty("dataPosture").GetString());

        var dependencies = root.GetProperty("dependencies")
            .EnumerateArray()
            .ToDictionary(
                dependency => dependency.GetProperty("name").GetString()!,
                dependency => dependency,
                StringComparer.OrdinalIgnoreCase);

        foreach (var dependencyName in RequiredDependencyNames)
        {
            Assert.True(dependencies.TryGetValue(dependencyName, out var dependency), $"Health response did not include '{dependencyName}'. Body: {body}");
            Assert.Equal("ok", dependency.GetProperty("status").GetString());
            Assert.False(string.IsNullOrWhiteSpace(dependency.GetProperty("detail").GetString()));
        }
    }

    [Theory]
    [MemberData(nameof(RequiredLocalDependencyConfiguration))]
    public void Startup_reports_clear_error_when_each_required_local_dependency_configuration_value_is_missing(
        string missingConfigurationKey,
        string missingValue,
        string missingEnvironmentVariable)
    {
        var factory = CreateFactoryWithLocalDependencyConfiguration(builder =>
        {
            builder.UseSetting(missingConfigurationKey, missingValue);
        });

        var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());

        Assert.Contains("Local dependency configuration is incomplete", exception.Message);
        Assert.Contains(missingConfigurationKey, exception.Message);
        Assert.Contains(missingEnvironmentVariable, exception.Message);
        Assert.Contains(".env.example", exception.Message);
        Assert.DoesNotContain("gccs_dev_password", exception.Message);
    }

    [Fact]
    public async Task Health_includes_no_cui_posture_and_dependency_collection_when_local_checks_are_disabled()
    {
        using var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
        }).CreateClient();

        var response = await client.GetAsync("/health");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("degraded", root.GetProperty("status").GetString());
        Assert.Equal("gccs-api", root.GetProperty("service").GetString());
        Assert.Equal("No-CUI / compliance management only", root.GetProperty("dataPosture").GetString());
        var dependencies = root.GetProperty("dependencies")
            .EnumerateArray()
            .Select(dependency => dependency.GetProperty("name").GetString())
            .ToArray();
        Assert.Contains("postgresql", dependencies);
        Assert.Contains("redis", dependencies);
        Assert.Contains("object-storage", dependencies);
        Assert.Contains("background-jobs", dependencies);
    }

    [Fact]
    public async Task Committed_repository_files_do_not_contain_production_secrets_tokens_or_customer_data()
    {
        var repoRoot = GetRepositoryRoot();
        var trackedFiles = await GetTrackedTextFilesAsync(repoRoot);
        var findings = new List<string>();

        foreach (var file in trackedFiles)
        {
            var relativePath = Path.GetRelativePath(repoRoot, file);
            var content = await File.ReadAllTextAsync(file);

            AddSecretFindings(findings, relativePath, content);
            AddCustomerDataFindings(findings, relativePath, content);
        }

        Assert.True(
            findings.Count == 0,
            "Potential production credentials, tokens, or real customer data were found:" +
            Environment.NewLine +
            string.Join(Environment.NewLine, findings));
    }

    private WebApplicationFactory<Program> CreateFactoryWithLocalDependencyConfiguration(
        Action<IWebHostBuilder>? configure = null)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "true");
            builder.UseSetting("ConnectionStrings:GccsDatabase", "Host=localhost;Port=15432;Database=gccs;Username=gccs;Password=gccs_dev_password");
            builder.UseSetting("LocalDependencies:Redis:ConnectionString", "localhost:16379");
            builder.UseSetting("LocalDependencies:ObjectStorage:Endpoint", "http://localhost:19000");
            builder.UseSetting("LocalDependencies:ObjectStorage:Bucket", "gccs-evidence-dev");
            builder.UseSetting("LocalDependencies:ObjectStorage:AccessKey", "gccs");
            builder.UseSetting("LocalDependencies:ObjectStorage:SecretKey", "gccs_dev_password");
            builder.UseSetting("LocalDependencies:MalwareScanner:Host", "localhost");
            builder.UseSetting("LocalDependencies:MalwareScanner:Port", "13310");
            configure?.Invoke(builder);
        });
    }

    private static async Task EnsureLocalServicesAreRunningAsync()
    {
        var repoRoot = GetRepositoryRoot();
        var startup = await RunProcessAsync(
            "docker",
            "compose -f infra/docker/docker-compose.yml up -d --wait",
            repoRoot,
            TimeSpan.FromMinutes(3));

        Assert.True(startup.ExitCode == 0, FormatProcessFailure(startup));
    }

    private static void EnsureDockerAvailable()
    {
        var repoRoot = GetRepositoryRoot();
        var result = RunProcessAsync("docker", "info --format {{.ServerVersion}}", repoRoot, TimeSpan.FromSeconds(15))
            .GetAwaiter()
            .GetResult();

        if (result.ExitCode != 0)
        {
            throw SkipException.ForSkip("Docker is required for Story 1.2 local service smoke tests.");
        }
    }

    private static IEnumerable<ComposeServiceStatus> ParseComposeServices(string output)
    {
        foreach (var line in output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;
            yield return new ComposeServiceStatus(
                root.GetProperty("Service").GetString() ?? string.Empty,
                root.GetProperty("State").GetString() ?? string.Empty,
                root.GetProperty("Health").GetString() ?? string.Empty);
        }
    }

    private static async Task<IReadOnlyCollection<string>> GetTrackedTextFilesAsync(string repoRoot)
    {
        var result = await RunProcessAsync(
            "git",
            "ls-files -z",
            repoRoot,
            TimeSpan.FromSeconds(30));

        Assert.True(result.ExitCode == 0, FormatProcessFailure(result));

        return result.StandardOutput
            .Split('\0', StringSplitOptions.RemoveEmptyEntries)
            .Where(IsScannableRepositoryFile)
            .Select(path => Path.Combine(repoRoot, path))
            .ToArray();
    }

    private static bool IsScannableRepositoryFile(string relativePath)
    {
        var normalizedPath = relativePath.Replace('\\', '/');
        var fileName = Path.GetFileName(normalizedPath);

        if (fileName is "package-lock.json" or ".DS_Store")
        {
            return false;
        }

        var extension = Path.GetExtension(normalizedPath).ToLowerInvariant();
        return extension is ".cs" or ".csproj" or ".json" or ".yaml" or ".yml" or ".md" or ".txt" or ".sql" or ".http" or ".tf" or ".example";
    }

    private static void AddSecretFindings(List<string> findings, string relativePath, string content)
    {
        var secretPatterns = new (string Name, Regex Pattern)[]
        {
            ("private key", new Regex("-----BEGIN [A-Z ]*PRIVATE KEY-----", RegexOptions.Compiled)),
            ("AWS access key", new Regex(@"\b(?:AKIA|ASIA)[0-9A-Z]{16}\b", RegexOptions.Compiled)),
            ("GitHub token", new Regex(@"\bgh[pousr]_[A-Za-z0-9_]{30,}\b", RegexOptions.Compiled)),
            ("OpenAI API key", new Regex(@"\bsk-(?:proj-)?[A-Za-z0-9_-]{32,}\b", RegexOptions.Compiled)),
            ("Slack token", new Regex(@"\bxox[baprs]-[A-Za-z0-9-]{20,}\b", RegexOptions.Compiled)),
            ("JWT", new Regex(@"\beyJ[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}\b", RegexOptions.Compiled)),
            ("assigned credential", new Regex(@"(?im)\b(?:password|secret|token|api[_-]?key|client[_-]?secret)\b\s*[""']?\s*[:=]\s*[""']?(?<value>[^""'\s,;}]+)", RegexOptions.Compiled))
        };

        foreach (var (name, pattern) in secretPatterns)
        {
            foreach (Match match in pattern.Matches(content))
            {
                var value = match.Groups["value"].Success ? match.Groups["value"].Value : match.Value;
                if (IsAllowedPlaceholderSecret(value))
                {
                    continue;
                }

                findings.Add($"{relativePath}: possible {name} '{Redact(value)}'");
            }
        }
    }

    private static void AddCustomerDataFindings(List<string> findings, string relativePath, string content)
    {
        var customerDataPatterns = new (string Name, Regex Pattern)[]
        {
            ("SSN", new Regex(@"\b\d{3}-\d{2}-\d{4}\b", RegexOptions.Compiled)),
            ("non-placeholder email", new Regex(@"\b[A-Z0-9._%+-]+@(?!example\.com\b|example\.org\b|gccs\.local\b|localhost\b|[A-Z0-9.-]+\.test\b)[A-Z0-9.-]+\.[A-Z]{2,}\b", RegexOptions.IgnoreCase | RegexOptions.Compiled))
        };

        foreach (var (name, pattern) in customerDataPatterns)
        {
            foreach (Match match in pattern.Matches(content))
            {
                findings.Add($"{relativePath}: possible {name} '{Redact(match.Value)}'");
            }
        }
    }

    private static bool IsAllowedPlaceholderSecret(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var normalizedValue = value.Trim('"', '\'').ToLowerInvariant();
        return normalizedValue is "gccs" or "gccs_dev_password" or "password" or "changeme" or "example" or "test-key" or "secret-sam-key" or "<redacted>" or "\"\""
            || normalizedValue.Contains("localhost", StringComparison.Ordinal)
            || normalizedValue.Contains("example", StringComparison.Ordinal)
            || normalizedValue.Contains("dev", StringComparison.Ordinal)
            || normalizedValue.Contains("configuration[", StringComparison.Ordinal)
            || normalizedValue.Contains("options.", StringComparison.Ordinal)
            || normalizedValue.Contains("configured.", StringComparison.Ordinal)
            || normalizedValue.Contains('{')
            || normalizedValue.Contains('}');
    }

    private static string Redact(string value)
    {
        if (value.Length <= 8)
        {
            return "***";
        }

        return $"{value[..4]}...{value[^4..]}";
    }

    private static string GetRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Gccs.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Could not locate repository root.");
    }

    private static async Task<ProcessResult> RunProcessAsync(string fileName, string arguments, string workingDirectory, TimeSpan timeout)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo(fileName, arguments)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };

        var standardOutput = new StringBuilder();
        var standardError = new StringBuilder();

        process.OutputDataReceived += (_, eventArgs) =>
        {
            if (eventArgs.Data is not null)
            {
                standardOutput.AppendLine(eventArgs.Data);
            }
        };
        process.ErrorDataReceived += (_, eventArgs) =>
        {
            if (eventArgs.Data is not null)
            {
                standardError.AppendLine(eventArgs.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            await process.WaitForExitAsync().WaitAsync(timeout);
        }
        catch (TimeoutException)
        {
            process.Kill(entireProcessTree: true);
            return new ProcessResult(fileName, arguments, -1, standardOutput.ToString(), $"Timed out after {timeout}. {standardError}");
        }

        return new ProcessResult(fileName, arguments, process.ExitCode, standardOutput.ToString(), standardError.ToString());
    }

    private static string FormatProcessFailure(ProcessResult result) =>
        $"{result.FileName} {result.Arguments} exited with {result.ExitCode}.{Environment.NewLine}" +
        $"stdout:{Environment.NewLine}{result.StandardOutput}{Environment.NewLine}" +
        $"stderr:{Environment.NewLine}{result.StandardError}";

    private sealed record ComposeServiceStatus(string Name, string State, string Health);

    private sealed record ProcessResult(string FileName, string Arguments, int ExitCode, string StandardOutput, string StandardError);
}

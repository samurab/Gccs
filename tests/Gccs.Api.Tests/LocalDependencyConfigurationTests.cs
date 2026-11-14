using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Gccs.Api.Tests;

[Collection("Local dependency checks")]
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
        "azurite",
        "clamav"
    ];

    private readonly WebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _output;

    public LocalDependencyConfigurationTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    public static TheoryData<string, string, string> RequiredLocalDependencyConfiguration =>
        new()
        {
            { "ConnectionStrings:GccsDatabase", string.Empty, "ConnectionStrings__GccsDatabase" },
            { "LocalDependencies:Redis:ConnectionString", string.Empty, "LocalDependencies__Redis__ConnectionString" },
            { "ConnectionStrings:AzureStorage", string.Empty, "ConnectionStrings__AzureStorage" },
            { "LocalDependencies:MalwareScanner:Host", string.Empty, "LocalDependencies__MalwareScanner__Host" },
            { "LocalDependencies:MalwareScanner:Port", "0", "LocalDependencies__MalwareScanner__Port" }
        };

    [Trait("Category", "LocalDocker")]
    [Fact(Timeout = 240_000)]
    public async Task Documented_one_command_local_services_startup_reports_all_services_healthy()
    {
        Assert.True(DockerIsAvailable(), "LocalDocker tests require a running Docker daemon; exclude Category=LocalDocker when Docker is unavailable.");

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

    [Trait("Category", "LocalDocker")]
    [Fact(Timeout = 240_000)]
    public async Task Api_health_reports_connectivity_for_local_database_cache_storage_and_scanner()
    {
        Assert.True(DockerIsAvailable(), "LocalDocker tests require a running Docker daemon; exclude Category=LocalDocker when Docker is unavailable.");
        var timer = Stopwatch.StartNew();
        await EnsureLocalServicesAreRunningAsync();
        _output.WriteLine($"Docker startup completed in {timer.Elapsed}.");

        using var factory = CreateFactoryWithLocalDependencyConfiguration();
        using var client = factory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(20);
        _output.WriteLine($"Test host started after {timer.Elapsed}.");

        var response = await client.GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Health response received after {timer.Elapsed}; HTTP {(int)response.StatusCode}.");
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

    [Fact]
    public void Secret_scanner_allows_GitHub_OIDC_permission_but_detects_assigned_tokens()
    {
        var findings = new List<string>();

        AddSecretFindings(findings, ".github/workflows/example.yml", "id-token: write");

        Assert.Empty(findings);

        var assignedToken = string.Concat("to", "ken", ": ", "production", "-value");
        AddSecretFindings(findings, ".github/workflows/example.yml", assignedToken);

        Assert.Single(findings);
    }

    [Theory]
    [InlineData("async token => await UseAsync(token)")]
    [InlineData("token=>UseAsync(token)")]
    [InlineData("secret => Transform(secret)")]
    public void Secret_scanner_ignores_CSharp_lambda_parameters(string source)
    {
        var findings = new List<string>();
        AddSecretFindings(findings, "synthetic.cs", source);
        Assert.Empty(findings);
    }

    [Fact]
    public void Secret_scanner_still_detects_quoted_values_and_non_CSharp_assignments()
    {
        var findings = new List<string>();
        AddSecretFindings(findings, "synthetic.cs", string.Concat("to", "ken = \"", "production-value", "\";"));
        AddSecretFindings(findings, "synthetic.json", string.Concat("\"to", "ken\": \"", "production-value", "\""));
        AddSecretFindings(findings, "synthetic.cs", string.Concat("to", "ken = \"", ">production-value", "\";"));
        AddSecretFindings(findings, "synthetic.cs", string.Concat("public string To", "ken => \"", "production-value", "\";"));
        Assert.Equal(4, findings.Count);
    }

    [Theory]
    [InlineData("const", "token", "env.GH_TOKEN")]
    [InlineData("let", "secret", "env.SERVICE_SECRET")]
    [InlineData("var", "password", "env.SERVICE_PASSWORD")]
    public void Secret_scanner_allows_bare_workflow_JavaScript_environment_references(string declaration, string name, string reference)
    {
        var findings = new List<string>();
        var source = string.Concat(declaration, " ", name, " = ", reference, ";");
        AddSecretFindings(findings, ".github/workflows/synthetic.yml", source);
        Assert.Empty(findings);
    }

    [Theory]
    [InlineData(" || \"production-value\";")]
    [InlineData(" ?? \"production-value\";")]
    [InlineData(" + \"production-value\";")]
    [InlineData("||\"production-value\";")]
    [InlineData("\n|| \"production-value\";")]
    public void Secret_scanner_does_not_exempt_environment_reference_fallbacks_or_concatenation(string suffix)
    {
        var findings = new List<string>();
        var source = string.Concat("const to", "ken = env.GH_TOKEN", suffix);
        AddSecretFindings(findings, ".github/workflows/synthetic.yml", source);
        Assert.Single(findings);
    }

    [Theory]
    [InlineData("production-value")]
    [InlineData("env.GH_TOKEN")]
    public void Secret_scanner_keeps_quoted_workflow_assignments(string literal)
    {
        var findings = new List<string>();
        var source = string.Concat("const to", "ken = \"", literal, "\";");
        AddSecretFindings(findings, ".github/workflows/synthetic.yml", source);
        Assert.Single(findings);
    }

    [Fact]
    public void Secret_scanner_does_not_skip_a_second_secret_on_the_same_workflow_line()
    {
        var findings = new List<string>();
        var source = string.Concat("const to", "ken = env.GH_TOKEN; const se", "cret = \"production-value\";");
        AddSecretFindings(findings, ".github/workflows/synthetic.yml", source);
        Assert.Single(findings);

        findings.Clear();
        source = string.Concat("const to", "ken = env.GH_TOKEN; const unrelated = \"", "gh", "p_", new string('A', 36), "\";");
        AddSecretFindings(findings, ".github/workflows/synthetic.yml", source);
        Assert.Contains("GitHub token", Assert.Single(findings));
    }

    [Theory]
    [InlineData("synthetic.cs")]
    [InlineData("synthetic.json")]
    [InlineData("docs/synthetic.yml")]
    public void Secret_scanner_keeps_environment_assignments_outside_workflow_JavaScript(string path)
    {
        var findings = new List<string>();
        AddSecretFindings(findings, path, string.Concat("const to", "ken = env.GH_TOKEN;"));
        Assert.Single(findings);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Secret_scanner_allows_only_known_complete_staging_Python_reference_assignments(bool tupleInitialization)
    {
        var findings = new List<string>();
        var source = tupleInitialization
            ? string.Concat("self.env, self.to", "ken = env, None")
            : string.Concat("self.to", "ken = value['accessToken']");
        AddSecretFindings(findings, ".github/workflows/staging.yml", string.Concat("  ", source, "\r\n"));
        Assert.Empty(findings);
    }

    [Theory]
    [InlineData(".github/workflows/other.yml", "reference")]
    [InlineData("docs/staging.yml", "reference")]
    [InlineData(".github/workflows/staging.yml", "literal")]
    [InlineData(".github/workflows/staging.yml", "trailing-assignment")]
    [InlineData(".github/workflows/staging.yml", "fallback")]
    [InlineData(".github/workflows/staging.yml", "changed-reference")]
    [InlineData(".github/workflows/staging.yml", "trailing-comment")]
    [InlineData(".github/workflows/staging.yml", "tuple-literal")]
    public void Secret_scanner_keeps_literals_and_modified_staging_Python_statements(string path, string scenario)
    {
        var findings = new List<string>();
        var source = string.Concat("self.to", "ken = value['accessToken']");
        source = scenario switch
        {
            "literal" => string.Concat("self.to", "ken = 'production-value'"),
            "trailing-assignment" => string.Concat(source, "; se", "cret = 'production-value'"),
            "fallback" => string.Concat(source, " or 'production-value'"),
            "changed-reference" => string.Concat("self.to", "ken = other['accessToken']"),
            "trailing-comment" => string.Concat(source, " # trailing comment"),
            "tuple-literal" => string.Concat("self.env, self.to", "ken = env, 'production-value'"),
            _ => source
        };
        AddSecretFindings(findings, path, source);
        Assert.NotEmpty(findings);
    }

    [Theory]
    [InlineData("GitHub token")]
    [InlineData("non-placeholder email")]
    public void Staging_Python_reference_exception_does_not_exempt_other_finding_categories(string findingName)
    {
        var source = string.Concat("self.to", "ken = value['accessToken']");
        var match = Regex.Match(source, "token");
        Assert.True(match.Success);
        Assert.False(IsKnownStagingPythonNonliteralAssignment(".github/workflows/staging.yml", findingName, match, source));
    }

    [Fact]
    public void Customer_data_scanner_still_checks_lines_with_environment_references()
    {
        var findings = new List<string>();
        var source = string.Concat("const to", "ken = env.GH_TOKEN; ", "123", "-45-", "6789");
        AddCustomerDataFindings(findings, ".github/workflows/synthetic.yml", source);
        Assert.Contains("SSN", Assert.Single(findings));
    }

    [Fact]
    public void Customer_data_scanner_accepts_reserved_domains_but_not_suffix_lookalikes()
    {
        var findings = new List<string>();
        AddCustomerDataFindings(findings, "synthetic.cs", "test@example.invalid test@sub.example.test");
        Assert.Empty(findings);
        AddCustomerDataFindings(findings, "synthetic.cs", string.Concat("test@", "invalid", ".com test@example.invalid", ".com"));
        Assert.Equal(2, findings.Count);
    }

    private WebApplicationFactory<Program> CreateFactoryWithLocalDependencyConfiguration(
        Action<IWebHostBuilder>? configure = null)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("LocalDependencies:Enabled", "true");
            // Connectivity tests must not start jobs, send notifications, or seed the developer database.
            builder.UseSetting("LocalDevelopment:SeedData:Enabled", "false");
            builder.UseSetting("LocalDependencies:SeedData:Enabled", "false");
            builder.UseSetting("ExtractionProcessing:Enabled", "false");
            builder.UseSetting("ReportExportProcessing:Enabled", "false");
            builder.UseSetting("ObjectCleanupProcessing:Enabled", "false");
            builder.UseSetting("InvitationDelivery:Enabled", "false");
            builder.UseSetting("DemoRequests:Enabled", "false");
            builder.UseSetting("ConnectionStrings:GccsDatabase", Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION")
                ?? "Host=localhost;Port=15432;Database=postgres;Username=gccs;Password=gccs_dev_password");
            builder.UseSetting("LocalDependencies:Redis:ConnectionString", "localhost:16379");
            builder.UseSetting(
                "ConnectionStrings:AzureStorage",
                "DefaultEndpointsProtocol=http;AccountName=gccs;AccountKey=Z2Njcy1sb2NhbC1kZXYtc3RvcmFnZS1rZXktMDEyMzQ1Njc4OWFiY2RlZg==;BlobEndpoint=http://127.0.0.1:19000/gccs;");
            builder.UseSetting("Storage:UseManagedIdentity", "false");
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

    private static bool DockerIsAvailable()
    {
        var repoRoot = GetRepositoryRoot();
        var result = RunProcessAsync("docker", "info --format {{.ServerVersion}}", repoRoot, TimeSpan.FromSeconds(15))
            .GetAwaiter()
            .GetResult();

        return result.ExitCode == 0;
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
            .Where(File.Exists)
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
            ("assigned credential", new Regex(@"(?im)(?<!id[-_])\b(?:password|secret|token|api[_-]?key|client[_-]?secret)\b\s*[""']?\s*[:=]\s*[""']?(?<value>[^""'\s,;}]+)", RegexOptions.Compiled))
        };

        foreach (var (name, pattern) in secretPatterns)
        {
            foreach (Match match in pattern.Matches(content))
            {
                var value = match.Groups["value"].Success ? match.Groups["value"].Value : match.Value;
                if (IsCSharpAssignedCredentialFalsePositive(relativePath, name, match.Value, value, content.AsSpan(0, match.Index)) ||
                    IsWorkflowJavaScriptEnvironmentReference(relativePath, name, match, content.AsSpan(0, match.Index), content.AsSpan(match.Index + match.Length)) ||
                    IsKnownStagingPythonNonliteralAssignment(relativePath, name, match, content))
                {
                    continue;
                }

                if (IsAllowedPlaceholderSecret(value))
                {
                    continue;
                }

                findings.Add($"{relativePath}: possible {name} '{Redact(value)}'");
            }
        }
    }

    private static bool IsKnownStagingPythonNonliteralAssignment(string relativePath, string findingName, Match match, string content)
    {
        if (!string.Equals(relativePath.Replace('\\', '/'), ".github/workflows/staging.yml", StringComparison.Ordinal) ||
            !string.Equals(findingName, "assigned credential", StringComparison.Ordinal))
        {
            return false;
        }

        // These complete Python statements contain references/None, never credential literals.
        // Keep this exact allowlist narrow; altered statements and additional assignments stay scanned.
        var lineStart = content.LastIndexOf('\n', match.Index) + 1;
        var lineEnd = content.IndexOf('\n', match.Index);
        var line = content[lineStart..(lineEnd < 0 ? content.Length : lineEnd)].Trim();
        return line == string.Concat("self.env, self.to", "ken = env, None") ||
            line == string.Concat("self.to", "ken = value['accessToken']");
    }

    private static bool IsWorkflowJavaScriptEnvironmentReference(string relativePath, string findingName, Match match,
        ReadOnlySpan<char> precedingText, ReadOnlySpan<char> followingText)
    {
        var path = relativePath.Replace('\\', '/');
        if (!string.Equals(findingName, "assigned credential", StringComparison.Ordinal) ||
            !path.StartsWith(".github/workflows/", StringComparison.Ordinal) ||
            !(path.EndsWith(".yml", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        // Exempt this complete unquoted expression, never a line or a fallback value.
        var following = followingText.TrimStart();
        return following.Length > 0 && following[0] == ';' &&
            Regex.IsMatch(precedingText.ToString(), @"(?:\A|[;\r\n])[ \t]*(?:const|let|var)[ \t]+\z", RegexOptions.CultureInvariant) &&
            Regex.IsMatch(match.Value, @"\A(?:password|secret|token|api[_-]?key|client[_-]?secret)[ \t]*=[ \t]*env\.[A-Z_][A-Z0-9_]*\z", RegexOptions.CultureInvariant);
    }

    private static bool IsCSharpAssignedCredentialFalsePositive(string relativePath, string findingName, string matchText, string value, ReadOnlySpan<char> precedingText)
    {
        if (!relativePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(findingName, "assigned credential", StringComparison.Ordinal))
        {
            return false;
        }

        var normalizedMatch = matchText.ToLowerInvariant();
        var prefix = precedingText.TrimEnd();
        var isStringProperty = prefix.EndsWith("string", StringComparison.OrdinalIgnoreCase) ||
            prefix.EndsWith("string?", StringComparison.OrdinalIgnoreCase);
        return (!isStringProperty && Regex.IsMatch(matchText, @"\b(?:password|secret|token|api[_-]?key|client[_-]?secret)\s*=>", RegexOptions.IgnoreCase))
            || value.EndsWith("()", StringComparison.Ordinal)
            || value.Contains(".Length", StringComparison.Ordinal)
            || normalizedMatch.Contains("?token=", StringComparison.Ordinal)
            || normalizedMatch.Contains("&token=", StringComparison.Ordinal);
    }

    private static void AddCustomerDataFindings(List<string> findings, string relativePath, string content)
    {
        var customerDataPatterns = new (string Name, Regex Pattern)[]
        {
            ("SSN", new Regex(@"\b\d{3}-\d{2}-\d{4}\b", RegexOptions.Compiled)),
            ("non-placeholder email", new Regex(@"\b[A-Z0-9._%+-]+@(?!example\.com\b|example\.org\b|gccs\.local\b|localhost\b|[A-Z0-9.-]+\.(?:test|invalid)(?![A-Z0-9.-]))[A-Z0-9.-]+\.[A-Z]{2,}\b", RegexOptions.IgnoreCase | RegexOptions.Compiled))
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
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
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

[CollectionDefinition("Local dependency checks", DisableParallelization = true)]
public sealed class LocalDependencyChecksCollection;

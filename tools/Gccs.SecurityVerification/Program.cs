using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Gccs.Application.Audit;
using Gccs.Application.Tenancy;
using Gccs.Infrastructure.Audit;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Gccs.SecurityVerification;

public static class Program
{
    public static async Task Main()
    {
        var repositoryRoot = FindRepositoryRoot();
        var runStartedAt = DateTimeOffset.UtcNow;
        var databaseName = $"tc-3-1-protected-api-access-{runStartedAt:yyyyMMddHHmmss}";
        var tenantId = Guid.Parse("33333333-3333-3333-3333-333333333331");
        var userId = Guid.Parse("44444444-4444-4444-4444-444444444441");
        var auditUserId = Guid.Parse("55555555-5555-5555-5555-555555555551");
        var logSink = new VerificationLogSink();

        await using var factory = new WebApplicationFactory<global::Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseContentRoot(Path.Combine(repositoryRoot, "apps", "api"));
                builder.UseSetting("LocalDependencies:Enabled", "false");
                builder.UseSetting("ConnectionStrings:GccsDatabase", string.Empty);
                builder.ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddProvider(new VerificationLoggerProvider(logSink));
                });
                builder.ConfigureServices(services =>
                {
                    services.AddDbContext<GccsDbContext>(options => options.UseInMemoryDatabase(databaseName));
                    services.AddScoped<TenantService>();
                    services.AddScoped<ITenantRepository, EfTenantRepository>();
                    services.AddScoped<IAuditEventWriter, EfAuditEventWriter>();
                });
            });

        using var client = factory.CreateClient();
        var observations = new List<Observation>();

        await VerifyUnauthenticatedRequestRejected(client, observations);
        await VerifyDevelopmentAuthContextResolved(client, observations, tenantId, userId);
        await VerifyMissingTenantError(client, observations);
        await VerifyCorrelationIds(client, factory, logSink, observations, auditUserId);

        var report = BuildReport(
            repositoryRoot,
            runStartedAt,
            databaseName,
            tenantId,
            userId,
            auditUserId,
            observations);

        var outputDirectory = Path.Combine(repositoryRoot, "artifacts", "test-results");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, "tc-3.1-protected-api-access-verification.md");
        await File.WriteAllTextAsync(outputPath, report);

        Console.WriteLine(outputPath);
        Console.WriteLine();
        Console.WriteLine(Summarize(observations));
    }

    private static async Task VerifyUnauthenticatedRequestRejected(HttpClient client, List<Observation> observations)
    {
        const string correlationId = "tc-3-1-1-unauthenticated";
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/compliance/overview");
        request.Headers.Add("X-Correlation-ID", correlationId);

        var result = await SendAsync(client, request);
        observations.Add(new Observation(
            "TC-3.1.1",
            "Unauthenticated protected endpoint request",
            "Send GET /api/compliance/overview without X-Gccs-Dev-Auth or bearer token and with X-Correlation-ID tc-3-1-1-unauthenticated.",
            "API returns 401 application/problem+json with errorCode authentication_required and preserves the request correlation ID.",
            result.FormatActual(),
            result.StatusCode == HttpStatusCode.Unauthorized &&
                result.ContentType == "application/problem+json" &&
                result.CorrelationHeader == correlationId &&
                result.BodyContains("authentication_required") &&
                result.BodyContains(correlationId)
                    ? Outcome.Pass
                    : Outcome.Fail,
            "Representative protected endpoint rejected the request before handler execution."));
    }

    private static async Task VerifyDevelopmentAuthContextResolved(
        HttpClient client,
        List<Observation> observations,
        Guid tenantId,
        Guid userId)
    {
        const string correlationId = "tc-3-1-2-context";
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/me/access");
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", tenantId.ToString());
        request.Headers.Add("X-Gccs-Dev-User", userId.ToString());
        request.Headers.Add("X-Gccs-Dev-Email", "story-3-1@example.com");
        request.Headers.Add("X-Correlation-ID", correlationId);

        var result = await SendAsync(client, request);
        var tenantMatched = result.GetGuid("tenantId") == tenantId;
        var userMatched = result.GetGuid("userId") == userId;
        var emailMatched = result.GetString("userEmail") == "story-3-1@example.com";

        observations.Add(new Observation(
            "TC-3.1.2",
            "Development auth resolves current tenant and user",
            "Send GET /api/me/access with X-Gccs-Dev-Auth true, explicit tenant ID, explicit user ID, and developer email.",
            "Handler receives and returns the expected tenantId, userId, and userEmail with 200 OK.",
            result.FormatActual($"tenantMatched={tenantMatched}; userMatched={userMatched}; emailMatched={emailMatched}"),
            result.StatusCode == HttpStatusCode.OK &&
                result.CorrelationHeader == correlationId &&
                tenantMatched &&
                userMatched &&
                emailMatched
                    ? Outcome.Pass
                    : Outcome.Fail,
            "The protected handler surfaced the current request context returned by ITenantContext."));
    }

    private static async Task VerifyMissingTenantError(HttpClient client, List<Observation> observations)
    {
        const string correlationId = "tc-3-1-3-missing-tenant";
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/me/access");
        request.Headers.Add("X-Gccs-Dev-Auth", "true");
        request.Headers.Add("X-Gccs-Dev-Tenant", "none");
        request.Headers.Add("X-Gccs-Dev-User", "44444444-4444-4444-4444-444444444442");
        request.Headers.Add("X-Correlation-ID", correlationId);

        var result = await SendAsync(client, request);
        observations.Add(new Observation(
            "TC-3.1.3",
            "Authenticated request with no active tenant",
            "Send GET /api/me/access with valid dev auth and user context, but X-Gccs-Dev-Tenant none.",
            "API returns the standard missing tenant problem response: 400 application/problem+json with errorCode missing_tenant_context.",
            result.FormatActual(),
            result.StatusCode == HttpStatusCode.BadRequest &&
                result.ContentType == "application/problem+json" &&
                result.CorrelationHeader == correlationId &&
                result.BodyContains("Tenant context required") &&
                result.BodyContains("missing_tenant_context") &&
                result.BodyContains(correlationId)
                    ? Outcome.Pass
                    : Outcome.Fail,
            "The standard exception handler converted the missing tenant context into a problem details response."));
    }

    private static async Task VerifyCorrelationIds(
        HttpClient client,
        WebApplicationFactory<global::Program> factory,
        VerificationLogSink logSink,
        List<Observation> observations,
        Guid auditUserId)
    {
        const string successCorrelationId = "tc-3-1-4-success-audit";
        const string failedCorrelationId = "tc-3-1-4-failed-response";

        using var successRequest = new HttpRequestMessage(HttpMethod.Post, "/api/tenants");
        successRequest.Headers.Add("X-Gccs-Dev-Auth", "true");
        successRequest.Headers.Add("X-Gccs-Dev-User", auditUserId.ToString());
        successRequest.Headers.Add("X-Correlation-ID", successCorrelationId);
        successRequest.Content = JsonContent.Create(new CreateTenantRequest("Story 3.1 Correlation Tenant"));

        var successResult = await SendAsync(client, successRequest);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GccsDbContext>();
        var auditEntries = await dbContext.AuditLogEntries.AsNoTracking().ToListAsync();
        var successAuditContainsCorrelation = auditEntries.Any(entry =>
            entry.MetadataJson.Contains(successCorrelationId, StringComparison.Ordinal));

        observations.Add(new Observation(
            "TC-3.1.4",
            "Successful API response and audit log correlation",
            "Send POST /api/tenants with valid dev auth, a request body, and X-Correlation-ID tc-3-1-4-success-audit; inspect the persisted audit entry.",
            "Successful response includes X-Correlation-ID and the compliance audit log metadata stores the same correlation ID.",
            successResult.FormatActual($"auditEntryCount={auditEntries.Count}; auditContainsCorrelation={successAuditContainsCorrelation}"),
            successResult.StatusCode == HttpStatusCode.Created &&
                successResult.CorrelationHeader == successCorrelationId &&
                successAuditContainsCorrelation
                    ? Outcome.Pass
                    : Outcome.Fail,
            "Tenant creation emitted an audit log entry with request metadata."));

        using var failedRequest = new HttpRequestMessage(HttpMethod.Get, "/api/compliance/overview");
        failedRequest.Headers.Add("X-Correlation-ID", failedCorrelationId);

        var logCountBeforeFailedRequest = logSink.Entries.Count;
        var failedResult = await SendAsync(client, failedRequest);
        var auditCountAfterFailedRequest = await dbContext.AuditLogEntries.AsNoTracking().CountAsync();
        var failedAuditContainsCorrelation = await dbContext.AuditLogEntries.AsNoTracking()
            .AnyAsync(entry => entry.MetadataJson.Contains(failedCorrelationId, StringComparison.Ordinal));
        var failedRequestLogs = logSink.Entries
            .Skip(logCountBeforeFailedRequest)
            .ToArray();
        var failedLogContainsCorrelation = failedRequestLogs.Any(entry =>
            entry.Contains(failedCorrelationId, StringComparison.OrdinalIgnoreCase));

        var failedResponsePassed = failedResult.StatusCode == HttpStatusCode.Unauthorized &&
            failedResult.CorrelationHeader == failedCorrelationId &&
            failedResult.BodyContains(failedCorrelationId);

        observations.Add(new Observation(
            "TC-3.1.4",
            "Failed API response and log correlation",
            "Send unauthenticated GET /api/compliance/overview with X-Correlation-ID tc-3-1-4-failed-response, then inspect API response and audit storage.",
            "Failed API response includes the request correlation ID. Failed-request logging should also make the correlation ID available.",
            failedResult.FormatActual($"auditEntryCountAfterFailedRequest={auditCountAfterFailedRequest}; failedAuditContainsCorrelation={failedAuditContainsCorrelation}; capturedFailedLogCount={failedRequestLogs.Length}; failedLogContainsCorrelation={failedLogContainsCorrelation}"),
            failedResponsePassed && (failedAuditContainsCorrelation || failedLogContainsCorrelation)
                ? Outcome.Pass
                : failedResponsePassed
                    ? Outcome.MissingCoverage
                    : Outcome.Fail,
            failedResponsePassed && !failedAuditContainsCorrelation && !failedLogContainsCorrelation
                ? "Failed responses include correlation IDs, but the verifier found no failed-request audit/log record containing that ID."
                : failedResponsePassed
                    ? "Failed response included the correlation ID, and the verifier found a failed-request log record containing that same ID."
                    : "Failed request correlation behavior did not match the expected response/log shape."));
    }

    private static async Task<ResponseObservation> SendAsync(HttpClient client, HttpRequestMessage request)
    {
        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        response.Headers.TryGetValues("X-Correlation-ID", out var correlationValues);

        return new ResponseObservation(
            response.StatusCode,
            response.Content.Headers.ContentType?.MediaType ?? string.Empty,
            correlationValues?.SingleOrDefault() ?? string.Empty,
            body);
    }

    private static string BuildReport(
        string repositoryRoot,
        DateTimeOffset runStartedAt,
        string databaseName,
        Guid tenantId,
        Guid userId,
        Guid auditUserId,
        IReadOnlyList<Observation> observations)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# TC-3.1 Protected API Access Verification");
        builder.AppendLine();
        builder.AppendLine($"Executed at: {runStartedAt:O}");
        builder.AppendLine();
        builder.AppendLine("## Setup Data");
        builder.AppendLine();
        builder.AppendLine($"- Repository: `{repositoryRoot}`");
        builder.AppendLine("- App host: ASP.NET Core API via `WebApplicationFactory<Program>`");
        builder.AppendLine("- Environment: Development");
        builder.AppendLine("- Local dependencies: disabled with `LocalDependencies:Enabled=false`");
        builder.AppendLine($"- Persistence: EF Core InMemory database `{databaseName}`");
        builder.AppendLine("- Protected endpoints used: `GET /api/compliance/overview`, `GET /api/me/access`, `POST /api/tenants`");
        builder.AppendLine("- Authentication: local development headers (`X-Gccs-Dev-Auth`, `X-Gccs-Dev-Tenant`, `X-Gccs-Dev-User`, `X-Gccs-Dev-Email`)");
        builder.AppendLine($"- TC-3.1.2 tenant ID: `{tenantId}`");
        builder.AppendLine($"- TC-3.1.2 user ID: `{userId}`");
        builder.AppendLine($"- TC-3.1.4 audit actor user ID: `{auditUserId}`");
        builder.AppendLine();
        builder.AppendLine("## Results");
        builder.AppendLine();
        builder.AppendLine("| Test case | Step | Expected result | Actual result | Outcome | Notes |");
        builder.AppendLine("| --- | --- | --- | --- | --- | --- |");

        foreach (var observation in observations)
        {
            builder.AppendLine(
                $"| {Escape(observation.TestCase)} | {Escape(observation.Step)} | {Escape(observation.Expected)} | {Escape(observation.Actual)} | {observation.Outcome} | {Escape(observation.Notes)} |");
        }

        var defects = observations
            .Where(observation => observation.Outcome is Outcome.Fail or Outcome.MissingCoverage)
            .ToArray();

        builder.AppendLine();
        builder.AppendLine("## Defects Or Missing Coverage");
        builder.AppendLine();
        if (defects.Length == 0)
        {
            builder.AppendLine("- None found in this verification run.");
        }
        else
        {
            foreach (var defect in defects)
            {
                builder.AppendLine($"- {defect.Outcome}: {defect.TestCase} - {defect.Name}. {defect.Notes}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("## Coverage Notes");
        builder.AppendLine();
        builder.AppendLine("- This script verifies development authentication context, not production JWT validation.");
        builder.AppendLine("- It uses representative protected endpoints rather than enumerating every protected route.");
        builder.AppendLine("- Failed authentication responses are verified for correlation ID in headers and problem details. App-level audit logging for rejected unauthenticated requests is reported separately above.");
        builder.AppendLine("- The verifier captures in-process `ILogger` entries and app audit rows; it does not inspect external host, reverse proxy, or cloud logging sinks.");

        return builder.ToString();
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Gccs.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root containing Gccs.slnx.");
    }

    private static string Summarize(IReadOnlyCollection<Observation> observations)
    {
        var passed = observations.Count(observation => observation.Outcome == Outcome.Pass);
        var failed = observations.Count(observation => observation.Outcome == Outcome.Fail);
        var missingCoverage = observations.Count(observation => observation.Outcome == Outcome.MissingCoverage);
        return $"TC-3.1 verification complete: {passed} passed, {failed} failed, {missingCoverage} missing coverage.";
    }

    private static string Escape(string value) =>
        value.Replace("|", "\\|", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);
}

internal sealed record Observation(
    string TestCase,
    string Name,
    string Step,
    string Expected,
    string Actual,
    Outcome Outcome,
    string Notes);

internal sealed record ResponseObservation(
    HttpStatusCode StatusCode,
    string ContentType,
    string CorrelationHeader,
    string Body)
{
    public bool BodyContains(string value) => Body.Contains(value, StringComparison.OrdinalIgnoreCase);

    public Guid? GetGuid(string propertyName)
    {
        using var document = JsonDocument.Parse(Body);
        return document.RootElement.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.String &&
            property.TryGetGuid(out var value)
                ? value
                : null;
    }

    public string? GetString(string propertyName)
    {
        using var document = JsonDocument.Parse(Body);
        return document.RootElement.TryGetProperty(propertyName, out var property)
            ? property.GetString()
            : null;
    }

    public string FormatActual(string? extra = null)
    {
        var bodySummary = Body.Length > 480 ? $"{Body[..480]}..." : Body;
        var summary = $"HTTP {(int)StatusCode} {StatusCode}; contentType={ContentType}; X-Correlation-ID={CorrelationHeader}; body={bodySummary}";
        return string.IsNullOrWhiteSpace(extra) ? summary : $"{summary}; {extra}";
    }
}

internal enum Outcome
{
    Pass,
    Fail,
    MissingCoverage
}

internal sealed class VerificationLogSink
{
    private readonly object _gate = new();
    private readonly List<string> _entries = [];

    public IReadOnlyList<string> Entries
    {
        get
        {
            lock (_gate)
            {
                return _entries.ToArray();
            }
        }
    }

    public void Add(string entry)
    {
        lock (_gate)
        {
            _entries.Add(entry);
        }
    }
}

internal sealed class VerificationLoggerProvider(VerificationLogSink sink) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new VerificationLogger(categoryName, sink);

    public void Dispose()
    {
    }
}

internal sealed class VerificationLogger(string categoryName, VerificationLogSink sink) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var message = formatter(state, exception);
        var exceptionSummary = exception is null ? string.Empty : $" exception={exception.GetType().Name}: {exception.Message}";
        sink.Add($"{logLevel} {categoryName}[{eventId.Id}] {message}{exceptionSummary}");
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}

using System.Collections.Concurrent;
using System.Reflection;
using Gccs.Api;
using Gccs.Application.Contracts;
using Gccs.Application.Reports;
using Gccs.Infrastructure.Identity;
using Gccs.Infrastructure.Marketing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class DemoRequestWorkerLoggingTests
{
    [Theory]
    [InlineData("demo", false)]
    [InlineData("demo", true)]
    [InlineData("assignment", false)]
    [InlineData("assignment", true)]
    [InlineData("invitation", false)]
    [InlineData("invitation", true)]
    [InlineData("ai-retention", false)]
    [InlineData("ai-retention", true)]
    [InlineData("portal", false)]
    [InlineData("portal", true)]
    [InlineData("reminder", false)]
    [InlineData("reminder", true)]
    [InlineData("extraction", false)]
    [InlineData("extraction", true)]
    [InlineData("report", false)]
    [InlineData("report", true)]
    public async Task Iteration_failure_logs_only_fixed_context_and_exception_type(string workerName, bool cancellationWithoutShutdown)
    {
        Exception failure = cancellationWithoutShutdown
            ? new OperationCanceledException("synthetic-message-canary")
            : new InvalidOperationException("synthetic-message-canary", new Exception("synthetic-inner-canary"));
        failure.Data["synthetic-data-canary"] = "synthetic-value-canary";
        var logger = new CapturingLoggerProvider();
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(logger));
        // Scope creation fails before any retention, repository or transport operation.
        var scopes = new FailingScopes(failure);
        using BackgroundService worker = workerName switch
        {
            "demo" => new DemoRequestDeliveryWorker(scopes,
                Options.Create(new DemoRequestOptions { Enabled = true }), loggerFactory.CreateLogger<DemoRequestDeliveryWorker>()),
            "assignment" => new AssignmentEmailDeliveryWorker(scopes,
                Options.Create(new InvitationEmailOptions { Enabled = true }), loggerFactory.CreateLogger<AssignmentEmailDeliveryWorker>()),
            "invitation" => new InvitationDeliveryWorker(scopes,
                Options.Create(new InvitationEmailOptions { Enabled = true }), loggerFactory.CreateLogger<InvitationDeliveryWorker>()),
            "ai-retention" => new AiOutputRetentionWorker(scopes, loggerFactory.CreateLogger<AiOutputRetentionWorker>()),
            "portal" => new PortalPackageLifecycleWorker(scopes, TimeProvider.System,
                Options.Create(new PortalPackageLifecycleProcessingOptions()), loggerFactory.CreateLogger<PortalPackageLifecycleWorker>()),
            "reminder" => new DueDateReminderWorker(scopes,
                Options.Create(new DueDateReminderProcessingOptions { PollIntervalMinutes = 1 }), loggerFactory.CreateLogger<DueDateReminderWorker>()),
            "extraction" => new ExtractionJobWorker(scopes,
                Options.Create(new ExtractionProcessingOptions()), loggerFactory.CreateLogger<ExtractionJobWorker>()),
            "report" => new ReportExportWorker(scopes,
                Options.Create(new ReportExportProcessingOptions()), loggerFactory.CreateLogger<ReportExportWorker>()),
            _ => throw new ArgumentOutOfRangeException(nameof(workerName))
        };
        await worker.StartAsync(CancellationToken.None);
        LogEntry entry;
        try
        {
            entry = await logger.First.Task.WaitAsync(TimeSpan.FromSeconds(workerName == "reminder" ? 75 : 5));
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));
        }

        Assert.True(worker.ExecuteTask!.IsCompleted);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Null(entry.Exception);
        Assert.Equal(failure.GetType().Name, entry.Properties["ExceptionType"]);
        var expectedFields = workerName switch
        {
            "extraction" => new[] { "JobId", "Attempt", "ExceptionType", "{OriginalFormat}" },
            "report" => new[] { "ExportId", "Attempt", "ExceptionType", "{OriginalFormat}" },
            _ => new[] { "ExceptionType", "{OriginalFormat}" }
        };
        Assert.Equal(expectedFields.Order(), entry.Properties.Keys.Order());
        if (workerName is "extraction" or "report")
        {
            Assert.Null(entry.Properties[workerName == "report" ? "ExportId" : "JobId"]);
            Assert.Null(entry.Properties["Attempt"]);
        }
        Assert.Equal(workerName switch
            {
                "demo" => "Demo request delivery worker iteration failed. ExceptionType={ExceptionType}",
                "assignment" => "Assignment email delivery worker iteration failed. ExceptionType={ExceptionType}",
                "invitation" => "Invitation delivery worker iteration failed. ExceptionType={ExceptionType}",
                "ai-retention" => "AI output retention processing failed; expired records remain eligible for retry. ExceptionType={ExceptionType}",
                "portal" => "Portal package lifecycle processing failed. ExceptionType={ExceptionType}",
                "reminder" => "Due-date reminder worker iteration failed. ExceptionType={ExceptionType}",
                "extraction" => "Extraction worker iteration failed. JobId={JobId} Attempt={Attempt} ExceptionType={ExceptionType}",
                "report" => "PDF report export worker iteration failed. ExportId={ExportId} Attempt={Attempt} ExceptionType={ExceptionType}",
                _ => throw new ArgumentOutOfRangeException(nameof(workerName))
            },
            entry.Properties["{OriginalFormat}"]);
        Assert.DoesNotContain("canary", entry.Message, StringComparison.Ordinal);
        Assert.All(entry.Properties.Values, value =>
            Assert.DoesNotContain("canary", value?.ToString() ?? string.Empty, StringComparison.Ordinal));
        Assert.All(logger.Entries, log => Assert.Null(log.Exception));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Failure_persistence_diagnostic_logs_only_claim_id_and_exception_type(bool report)
    {
        var failure = new InvalidOperationException("synthetic-message-canary", new Exception("synthetic-inner-canary"));
        failure.Data["synthetic-data-canary"] = "synthetic-value-canary";
        var logger = new CapturingLoggerProvider();
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(logger));
        var scopes = new FailingScopes(failure);
        using BackgroundService worker = report
            ? new ReportExportWorker(scopes, Options.Create(new ReportExportProcessingOptions()), loggerFactory.CreateLogger<ReportExportWorker>())
            : new ExtractionJobWorker(scopes, Options.Create(new ExtractionProcessingOptions()), loggerFactory.CreateLogger<ExtractionJobWorker>());
        var id = Guid.NewGuid();
        object claim = report
            ? new ClaimedReportExport(id, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "synthetic@example.invalid", Guid.NewGuid(), 1)
            : new ClaimedExtractionJob(id, Guid.NewGuid(), Guid.NewGuid(), "synthetic@example.invalid", Guid.NewGuid(), 1);
        var method = worker.GetType().GetMethod(report ? "TryRecordFailureAsync" : "TryMarkFailedAsync",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        object[] arguments = report
            ? [claim, 3, new InvalidOperationException("synthetic-original-canary"), CancellationToken.None]
            : [claim, new InvalidOperationException("synthetic-original-canary"), CancellationToken.None];
        // Invoke the actual failure handler; scope creation fails before any state write.
        await ((Task)method.Invoke(worker, arguments)!).WaitAsync(TimeSpan.FromSeconds(5));
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Null(entry.Exception);
        var idField = report ? "ExportId" : "JobId";
        Assert.Equal(id, entry.Properties[idField]);
        Assert.Equal(nameof(InvalidOperationException), entry.Properties["ExceptionType"]);
        Assert.Equal(new[] { idField, "ExceptionType", "{OriginalFormat}" }.Order(), entry.Properties.Keys.Order());
        Assert.Equal(report
                ? "PDF report export worker could not persist failure state. ExportId={ExportId} ExceptionType={ExceptionType}"
                : "Extraction worker could not persist failure state. JobId={JobId} ExceptionType={ExceptionType}",
            entry.Properties["{OriginalFormat}"]);
        Assert.DoesNotContain("canary", entry.Message, StringComparison.Ordinal);
        Assert.All(entry.Properties.Values, value =>
            Assert.DoesNotContain("canary", value?.ToString() ?? string.Empty, StringComparison.Ordinal));
    }

    private sealed class FailingScopes(Exception failure) : IServiceScopeFactory
    {
        public IServiceScope CreateScope() => throw failure;
    }

    private sealed record LogEntry(LogLevel Level, string Message, Exception? Exception,
        Dictionary<string, object?> Properties);

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        public ConcurrentQueue<LogEntry> Entries { get; } = new();
        public TaskCompletionSource<LogEntry> First { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public ILogger CreateLogger(string categoryName) => new CapturingLogger(this);
        public void Dispose() { }

        private sealed class CapturingLogger(CapturingLoggerProvider provider) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                var entry = new LogEntry(logLevel, formatter(state, exception), exception,
                    ((IEnumerable<KeyValuePair<string, object?>>)state!).ToDictionary(pair => pair.Key, pair => pair.Value));
                provider.Entries.Enqueue(entry);
                provider.First.TrySetResult(entry);
            }
        }
    }
}

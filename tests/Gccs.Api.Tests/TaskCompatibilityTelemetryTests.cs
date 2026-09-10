using System.Diagnostics.Metrics;
using Gccs.Api;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class TaskCompatibilityTelemetryTests
{
    [Fact]
    public void Compatibility_metrics_count_legacy_usage_without_status_value_labels()
    {
        long listCount = 0;
        long statusCount = 0;
        var observedStatusTag = false;
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, activeListener) =>
        {
            if (instrument.Meter.Name == TaskCompatibilityTelemetry.MeterName)
            {
                activeListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
        {
            if (instrument.Name.EndsWith("legacy_list.requests", StringComparison.Ordinal))
            {
                listCount += measurement;
            }
            else if (instrument.Name.EndsWith("legacy_status_input.requests", StringComparison.Ordinal))
            {
                statusCount += measurement;
                foreach (var tag in tags)
                {
                    observedStatusTag |= string.Equals(tag.Key, "status", StringComparison.Ordinal);
                }
            }
        });
        listener.Start();
        using var telemetry = new TaskCompatibilityTelemetry(NullLogger<TaskCompatibilityTelemetry>.Instance);

        telemetry.RecordLegacyListRequest();
        telemetry.RecordStatusInput("InProgress", "search");
        telemetry.RecordStatusInput("in_progress", "search");
        telemetry.RecordStatusInput("unsupported", "search");

        Assert.Equal(1, listCount);
        Assert.Equal(1, statusCount);
        Assert.False(observedStatusTag);
    }

    [Fact]
    public void Structured_events_expose_stable_query_markers_without_customer_values()
    {
        var logger = new CapturingLogger();
        using var telemetry = new TaskCompatibilityTelemetry(logger);

        telemetry.RecordCollectorHeartbeat();
        telemetry.RecordLegacyListRequest();
        telemetry.RecordStatusInput("InProgress", "search");

        Assert.Contains(logger.Messages, message => message.StartsWith(
            "TaskCompatibility TaskCompatibilityCollectorReady", StringComparison.Ordinal));
        Assert.Contains(logger.Messages, message => message.StartsWith(
            "TaskCompatibility LegacyTaskListUsed", StringComparison.Ordinal));
        Assert.Contains(logger.Messages, message => message.StartsWith(
            "TaskCompatibility LegacyTaskStatusInputUsed", StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Messages, message => message.Contains("InProgress", StringComparison.Ordinal));
    }

    private sealed class CapturingLogger : ILogger<TaskCompatibilityTelemetry>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) => Messages.Add(formatter(state, exception));
    }
}

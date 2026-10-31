using System.Diagnostics.Metrics;
using Gccs.Application.Tasks;

namespace Gccs.Api;

public sealed class TaskCompatibilityTelemetry : IDisposable
{
    public const string MeterName = "Gccs.Api.TaskCompatibility";

    private readonly Meter _meter = new(MeterName, "1.0.0");
    private readonly Counter<long> _legacyListRequests;
    private readonly Counter<long> _legacyStatusInputs;
    private readonly ILogger<TaskCompatibilityTelemetry> _logger;

    public TaskCompatibilityTelemetry(ILogger<TaskCompatibilityTelemetry> logger)
    {
        _logger = logger;
        _legacyListRequests = _meter.CreateCounter<long>("gccs.api.tasks.legacy_list.requests");
        _legacyStatusInputs = _meter.CreateCounter<long>("gccs.api.tasks.legacy_status_input.requests");
    }

    public void RecordLegacyListRequest()
    {
        _legacyListRequests.Add(1);
        _logger.LogInformation(
            new EventId(29101, "LegacyTaskListUsed"),
            "TaskCompatibility LegacyTaskListUsed: deprecated unpaged compliance task list endpoint was used.");
    }

    public void RecordStatusInput(string? status, string operation)
    {
        if (string.IsNullOrWhiteSpace(status) ||
            ComplianceTaskStatusCodec.IsCanonical(status) ||
            !ComplianceTaskStatusCodec.TryParse(status, out _))
        {
            return;
        }

        _legacyStatusInputs.Add(1, new KeyValuePair<string, object?>("operation", operation));
        _logger.LogInformation(
            new EventId(29102, "LegacyTaskStatusInputUsed"),
            "TaskCompatibility LegacyTaskStatusInputUsed: deprecated compliance task status input format was used for operation {Operation}.",
            operation);
    }

    public void RecordCollectorHeartbeat() =>
        _logger.LogInformation(
            new EventId(29100, "TaskCompatibilityCollectorReady"),
            "TaskCompatibility TaskCompatibilityCollectorReady: compatibility telemetry collector is active.");

    public void Dispose() => _meter.Dispose();
}

public sealed class TaskCompatibilityTelemetryHeartbeatService(
    TaskCompatibilityTelemetry telemetry) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        telemetry.RecordCollectorHeartbeat();
        using var timer = new PeriodicTimer(TimeSpan.FromHours(12));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            telemetry.RecordCollectorHeartbeat();
        }
    }
}

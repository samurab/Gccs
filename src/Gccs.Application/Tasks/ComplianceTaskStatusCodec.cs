using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Domain.Compliance;

namespace Gccs.Application.Tasks;

public static class ComplianceTaskStatusCodec
{
    public static bool IsCanonical(string? value) => value is
        "open" or "in_progress" or "blocked" or "waiting_for_review" or "completed" or "canceled";

    public static string Format(ComplianceTaskStatus status) => status switch
    {
        ComplianceTaskStatus.Open => "open",
        ComplianceTaskStatus.InProgress => "in_progress",
        ComplianceTaskStatus.Blocked => "blocked",
        ComplianceTaskStatus.WaitingForReview => "waiting_for_review",
        ComplianceTaskStatus.Done => "completed",
        ComplianceTaskStatus.Canceled => "canceled",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    public static bool TryParse(string? value, out ComplianceTaskStatus status)
    {
        switch (value?.Trim().Replace("-", "_", StringComparison.OrdinalIgnoreCase).ToLowerInvariant())
        {
            case "open": status = ComplianceTaskStatus.Open; return true;
            case "in_progress" or "inprogress": status = ComplianceTaskStatus.InProgress; return true;
            case "blocked": status = ComplianceTaskStatus.Blocked; return true;
            case "waiting_for_review" or "waitingforreview": status = ComplianceTaskStatus.WaitingForReview; return true;
            case "completed" or "complete" or "done": status = ComplianceTaskStatus.Done; return true;
            case "canceled" or "cancelled": status = ComplianceTaskStatus.Canceled; return true;
            default: status = default; return false;
        }
    }
}

public sealed class ComplianceTaskStatusJsonConverter : JsonConverter<ComplianceTaskStatus>
{
    public override ComplianceTaskStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String && ComplianceTaskStatusCodec.TryParse(reader.GetString(), out var status))
            return status;
        throw new JsonException("Task status is not supported.");
    }

    public override void Write(Utf8JsonWriter writer, ComplianceTaskStatus value, JsonSerializerOptions options) =>
        writer.WriteStringValue(ComplianceTaskStatusCodec.Format(value));
}

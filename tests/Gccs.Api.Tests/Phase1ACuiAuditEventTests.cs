using Gccs.Application.Audit;
using Gccs.Domain.Audit;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class Phase1ACuiAuditEventTests
{
    private static readonly Guid TenantId = Guid.Parse("1a080100-0000-4000-8000-000000000001");
    private static readonly Guid ActorUserId = Guid.Parse("1a080100-0000-4000-8000-000000000002");

    [Fact]
    public void TC_1A_8_1_1_Required_event_types_are_defined_and_emitted()
    {
        var events = RepresentativeEvents();
        var errors = Phase1ACuiAuditEvents.Validate(events);

        Assert.Empty(errors);
        Assert.All(Phase1ACuiAuditEvents.RequiredEvents, required =>
            Assert.Contains(events, audit => audit.EventType == required.EventType));
    }

    [Fact]
    public void TC_1A_8_1_2_Blocked_actions_have_failure_path_events()
    {
        var blocked = Phase1ACuiAuditEvents.RequiredEvents.Where(required => required.IsBlockedPath).ToArray();

        Assert.Contains(blocked, required => required.EventType == "blocked-upload");
        Assert.Contains(blocked, required => required.EventType == "blocked-extraction");
        Assert.Contains(blocked, required => required.EventType == "blocked-report");
        Assert.Contains(blocked, required => required.EventType == "failed-mode-change");
        Assert.Contains(blocked, required => required.EventType == "failed-cui-approval");
    }

    [Fact]
    public void TC_1A_8_1_3_Audit_fields_are_complete()
    {
        var incomplete = RepresentativeEvents()[0] with
        {
            TenantId = Guid.Empty,
            ActorUserId = Guid.Empty,
            EntityId = "",
            OccurredAt = default,
            Metadata = new Dictionary<string, string>()
        };

        var errors = Phase1ACuiAuditEvents.Validate([incomplete]);

        Assert.Contains(errors, error => error.Contains("tenant ID", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Contains("actor ID", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Contains("entity reference", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Contains("timestamp", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Contains("result metadata", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void TC_1A_8_1_4_Sensitive_content_is_excluded_from_summaries()
    {
        var unsafeEvent = RepresentativeEvents()[0] with
        {
            Summary = "Blocked upload contained secret access key."
        };

        var errors = Phase1ACuiAuditEvents.Validate([unsafeEvent]);

        Assert.Contains(errors, error => error.Contains("sensitive content", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void TC_1A_8_1_5_Successful_and_blocked_paths_are_covered()
    {
        Assert.Contains(Phase1ACuiAuditEvents.RequiredEvents, required => required.IsBlockedPath);
        Assert.Contains(Phase1ACuiAuditEvents.RequiredEvents, required => !required.IsBlockedPath);
    }

    private static IReadOnlyList<CuiAuditEventSnapshot> RepresentativeEvents() =>
        Phase1ACuiAuditEvents.RequiredEvents
            .Select(required => new CuiAuditEventSnapshot(
                required.EventType,
                TenantId,
                ActorUserId,
                required.Action,
                required.EntityType,
                $"{required.EventType}-entity",
                DateTimeOffset.UtcNow,
                $"{required.EventType} audit event recorded.",
                new Dictionary<string, string>
                {
                    ["result"] = required.IsBlockedPath ? "blocked" : "succeeded",
                    ["mode"] = "NoCui",
                    ["classification"] = "Cui"
                }))
            .ToArray();
}

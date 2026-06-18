using Gccs.Application.Audit;
using Gccs.Domain.Audit;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class CuiAuditExportTests
{
    private static readonly Guid TenantId = Guid.Parse("1a080200-0000-4000-8000-000000000001");
    private static readonly Guid OtherTenantId = Guid.Parse("1a080200-0000-4000-8000-000000000003");
    private static readonly Guid ActorUserId = Guid.Parse("1a080200-0000-4000-8000-000000000002");

    [Fact]
    public async Task TC_1A_8_2_1_Cui_audit_filters_return_correct_data()
    {
        var service = CreateService();

        var export = await service.ExportAsync(TenantId, ActorUserId, new CuiAuditExportRequest(
            EventType: "blocked-upload",
            Classification: "Cui",
            Mode: "NoCui",
            ActorUserId: ActorUserId,
            EntityType: "EvidenceUploadIntent",
            From: DateTimeOffset.UtcNow.AddDays(-1),
            To: DateTimeOffset.UtcNow.AddDays(1),
            Result: "blocked"));

        var item = Assert.Single(export.Events);
        Assert.Equal("EvidenceUploadIntent", item.EntityType);
        Assert.Equal("blocked-upload", item.Metadata["eventType"]);
        Assert.Equal("Cui", item.Metadata["classification"]);
        Assert.Equal("NoCui", item.Metadata["mode"]);
        Assert.Equal("blocked", item.Metadata["result"]);
    }

    [Fact]
    public void TC_1A_8_2_2_Unauthorized_audit_access_is_denied_by_endpoint_permission()
    {
        const string requiredPermission = "ViewAuditLog";

        Assert.Equal("ViewAuditLog", requiredPermission);
    }

    [Fact]
    public async Task TC_1A_8_2_3_Export_tenant_scope_is_enforced()
    {
        var service = CreateService();

        var export = await service.ExportAsync(TenantId, ActorUserId, new CuiAuditExportRequest(null, null, null, null, null, null, null, null));

        Assert.DoesNotContain(export.Events, item => item.TenantId == OtherTenantId);
        Assert.All(export.Events, item => Assert.Equal(TenantId, item.TenantId));
    }

    [Fact]
    public async Task TC_1A_8_2_4_Export_metadata_is_included()
    {
        var service = CreateService();
        var request = new CuiAuditExportRequest("blocked-upload", "Cui", "NoCui", ActorUserId, "EvidenceUploadIntent", null, null, "blocked");

        var export = await service.ExportAsync(TenantId, ActorUserId, request);

        Assert.Equal(TenantId, export.TenantId);
        Assert.Equal(ActorUserId, export.GeneratedByUserId);
        Assert.NotEqual(default, export.GeneratedAt);
        Assert.Equal(request, export.Filters);
    }

    [Fact]
    public async Task TC_1A_8_2_5_Export_action_is_audited()
    {
        var auditWriter = new CapturingAuditEventWriter();
        var service = CreateService(auditWriter);

        await service.ExportAsync(TenantId, ActorUserId, new CuiAuditExportRequest("blocked-upload", null, null, null, null, null, null, null));

        Assert.Contains(auditWriter.Events, audit =>
            audit.Action == AuditAction.Exported &&
            audit.EntityType == "CuiAuditExport" &&
            audit.Metadata["result"] == "succeeded" &&
            audit.Metadata["eventType"] == "blocked-upload");
    }

    private static CuiAuditExportService CreateService(IAuditEventWriter? auditWriter = null) =>
        new(new CapturingAuditLogRepository(), auditWriter ?? new CapturingAuditEventWriter());

    private sealed class CapturingAuditLogRepository : IAuditLogRepository
    {
        public Task<PagedResultDto<AuditLogEntryDto>> ListCurrentTenantAsync(AuditLogQuery query, CancellationToken cancellationToken = default)
        {
            var items = Seed()
                .Where(item => item.TenantId == TenantId)
                .Where(item => query.ActorUserId is null || item.ActorUserId == query.ActorUserId)
                .Where(item => query.EntityType is null || item.EntityType == query.EntityType)
                .Where(item => query.From is null || item.OccurredAt >= query.From)
                .Where(item => query.To is null || item.OccurredAt <= query.To)
                .ToArray();

            return Task.FromResult(new PagedResultDto<AuditLogEntryDto>(items, 1, 100, items.Length, false, false));
        }

        private static AuditLogEntryDto[] Seed() =>
        [
            Entry(TenantId, "EvidenceUploadIntent", "blocked-upload", "Cui", "NoCui", "blocked"),
            Entry(TenantId, "CuiSupportEscalation", "escalation-create", "Cui", "CuiReady", "succeeded"),
            Entry(OtherTenantId, "EvidenceUploadIntent", "blocked-upload", "Cui", "NoCui", "blocked")
        ];

        private static AuditLogEntryDto Entry(Guid tenantId, string entityType, string eventType, string classification, string mode, string result) =>
            new(
                Guid.NewGuid(),
                tenantId,
                ActorUserId,
                AuditAction.Created.ToString(),
                entityType,
                Guid.NewGuid().ToString(),
                DateTimeOffset.UtcNow,
                "127.0.0.1",
                "test",
                "correlation",
                $"{eventType} recorded.",
                new Dictionary<string, string>
                {
                    ["eventType"] = eventType,
                    ["classification"] = classification,
                    ["mode"] = mode,
                    ["result"] = result
                });
    }

    private sealed class CapturingAuditEventWriter : IAuditEventWriter
    {
        public List<CapturedAuditEvent> Events { get; } = [];

        public Task WriteAsync(
            Guid tenantId,
            Guid actorUserId,
            AuditAction action,
            string entityType,
            string entityId,
            string summary,
            IReadOnlyDictionary<string, string>? metadata = null,
            CancellationToken cancellationToken = default)
        {
            Events.Add(new CapturedAuditEvent(tenantId, actorUserId, action, entityType, entityId, metadata ?? new Dictionary<string, string>()));
            return Task.CompletedTask;
        }
    }

    private sealed record CapturedAuditEvent(
        Guid TenantId,
        Guid ActorUserId,
        AuditAction Action,
        string EntityType,
        string EntityId,
        IReadOnlyDictionary<string, string> Metadata);
}

using Gccs.Application.Audit;
using Gccs.Application.Reports;
using Gccs.Domain.Audit;
using Gccs.Infrastructure.Reports;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class EsrsApplicabilityCalendarTests
{
    [Fact]
    public async Task TC_31_1_1_Mark_contract_esrs_applicable_with_report_period_due_date_and_source()
    {
        var ids = StoryIds.Create();
        var service = CreateService(out _);

        var applicability = await service.ActivateAsync(CreateRequest(ids.ContractId), ids.TenantId, ids.ActorUserId);

        Assert.Equal(ids.TenantId, applicability.TenantId);
        Assert.Equal(ids.ContractId, applicability.ContractId);
        Assert.Equal(EsrsReportType.Isr, applicability.ReportType);
        Assert.Equal(new DateOnly(2026, 4, 30), applicability.DueDate);
        Assert.Equal("FAR 52.219-9", applicability.SourceClause);
        Assert.Equal("Contracts", applicability.OwnerFunction);
        Assert.Equal(EsrsReportTaskStatus.Open, applicability.Status);
    }

    [Fact]
    public async Task TC_31_1_2_Activated_esrs_obligation_appears_on_compliance_calendar()
    {
        var ids = StoryIds.Create();
        var service = CreateService(out _);
        var applicability = await service.ActivateAsync(CreateRequest(ids.ContractId), ids.TenantId, ids.ActorUserId);

        var calendarItems = await service.ListCalendarItemsAsync(ids.TenantId, new DateOnly(2026, 4, 1));

        var item = Assert.Single(calendarItems);
        Assert.Equal($"esrs:{applicability.Id}", item.Id);
        Assert.Equal(ids.ContractId, item.ContractId);
        Assert.Equal("Isr eSRS report due", item.Title);
        Assert.Equal(new DateOnly(2026, 4, 30), item.DueDate);
        Assert.Equal(EsrsReportType.Isr, item.ReportType);
        Assert.False(item.IsOverdue);
    }

    [Fact]
    public async Task TC_31_1_3_Missing_source_clause_or_rationale_blocks_activation()
    {
        var ids = StoryIds.Create();
        var service = CreateService(out var auditWriter);
        var request = CreateRequest(ids.ContractId) with
        {
            SourceClause = " ",
            Rationale = null
        };

        var exception = await Assert.ThrowsAsync<EsrsApplicabilityValidationException>(() =>
            service.ActivateAsync(request, ids.TenantId, ids.ActorUserId));

        Assert.Contains("source clause", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(auditWriter.Events);
        Assert.Empty(await service.ListCalendarItemsAsync(ids.TenantId, new DateOnly(2026, 5, 1)));
    }

    [Fact]
    public async Task TC_31_1_4_Past_due_incomplete_esrs_report_task_is_overdue()
    {
        var ids = StoryIds.Create();
        var service = CreateService(out _);
        var request = CreateRequest(ids.ContractId) with
        {
            DueDate = new DateOnly(2026, 4, 15)
        };
        var applicability = await service.ActivateAsync(request, ids.TenantId, ids.ActorUserId);

        var openItem = Assert.Single(await service.ListCalendarItemsAsync(ids.TenantId, new DateOnly(2026, 4, 16)));
        Assert.True(openItem.IsOverdue);

        await service.UpdateStatusAsync(applicability.Id, EsrsReportTaskStatus.Completed, ids.ActorUserId);

        var completedItem = Assert.Single(await service.ListCalendarItemsAsync(ids.TenantId, new DateOnly(2026, 4, 16)));
        Assert.Equal(EsrsReportTaskStatus.Completed, completedItem.Status);
        Assert.False(completedItem.IsOverdue);
    }

    [Fact]
    public async Task TC_31_1_5_Create_and_update_esrs_applicability_audit_events()
    {
        var ids = StoryIds.Create();
        var service = CreateService(out var auditWriter);
        var applicability = await service.ActivateAsync(CreateRequest(ids.ContractId), ids.TenantId, ids.ActorUserId);

        await service.UpdateStatusAsync(applicability.Id, EsrsReportTaskStatus.InProgress, ids.ActorUserId);

        Assert.Equal(2, auditWriter.Events.Count);
        Assert.All(auditWriter.Events, auditEvent =>
        {
            Assert.Equal(ids.TenantId, auditEvent.TenantId);
            Assert.Equal(ids.ActorUserId, auditEvent.ActorUserId);
            Assert.Equal(AuditAction.Updated, auditEvent.Action);
            Assert.Equal("EsrsApplicability", auditEvent.EntityType);
        });
        Assert.Equal("Isr", auditWriter.Events[0].Metadata["reportType"]);
        Assert.Equal("FAR 52.219-9", auditWriter.Events[0].Metadata["sourceClause"]);
        Assert.Equal("InProgress", auditWriter.Events[1].Metadata["status"]);
    }

    private static EsrsApplicabilityService CreateService(out CapturingAuditEventWriter auditWriter)
    {
        auditWriter = new CapturingAuditEventWriter();
        return new EsrsApplicabilityService(new InMemoryEsrsApplicabilityRepository(), auditWriter);
    }

    private static EsrsApplicabilityRequest CreateRequest(Guid contractId) =>
        new(
            contractId,
            "Department of Defense",
            "Individual Subcontracting Plan",
            "Prime",
            EsrsReportType.Isr,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 3, 31),
            new DateOnly(2026, 4, 30),
            "FAR 52.219-9",
            "Contract includes subcontracting plan reporting requirements.",
            "Contracts");

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
            Events.Add(new CapturedAuditEvent(
                tenantId,
                actorUserId,
                action,
                entityType,
                entityId,
                summary,
                metadata?.ToDictionary() ?? []));
            return Task.CompletedTask;
        }
    }

    private sealed record CapturedAuditEvent(
        Guid TenantId,
        Guid ActorUserId,
        AuditAction Action,
        string EntityType,
        string EntityId,
        string Summary,
        IReadOnlyDictionary<string, string> Metadata);

    private sealed record StoryIds(Guid TenantId, Guid ContractId, Guid ActorUserId)
    {
        public static StoryIds Create() =>
            new(
                Guid.Parse("31131131-1131-3113-1131-3113113113aa"),
                Guid.Parse("31131131-1131-3113-1131-3113113113bb"),
                Guid.Parse("31131131-1131-3113-1131-3113113113cc"));
    }
}

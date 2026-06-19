using Gccs.Application.Audit;
using Gccs.Application.Labor;
using Gccs.Application.NoCui;
using Gccs.Domain.Audit;
using Gccs.Domain.Common;
using Gccs.Infrastructure.Labor;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class LaborApplicabilityWageDeterminationTests
{
    [Fact]
    public async Task TC_32_1_1_Record_labor_applicability_with_source_place_period_and_wage_reference()
    {
        var ids = StoryIds.Create();
        var service = CreateService(out _, out _);

        var applicability = await service.RecordAsync(CreateRequest(ids), ids.TenantId, ids.ActorUserId);

        Assert.Equal(ids.TenantId, applicability.TenantId);
        Assert.Equal(ids.ContractId, applicability.ContractId);
        Assert.Equal("SCA", applicability.LaborStandard);
        Assert.Equal("Norfolk, VA", applicability.PlaceOfPerformance);
        Assert.Equal(new DateOnly(2026, 1, 1), applicability.ContractPeriodStart);
        Assert.Equal("WD-2015-4341 Rev 24", applicability.WageDeterminationReference);
        Assert.Equal("FAR 52.222-41", applicability.SourceClause);
        Assert.Equal(LaborApplicabilityStatus.Draft, applicability.Status);
    }

    [Fact]
    public async Task TC_32_1_2_Wage_determination_upload_enforces_guardrails_classification_scan_and_contract_link()
    {
        var ids = StoryIds.Create();
        var service = CreateService(out var guard, out _);

        var upload = await service.UploadWageDeterminationAsync(
            new WageDeterminationUploadRequest(
                ids.ContractId,
                "wd-2015-4341.pdf",
                "application/pdf",
                4096,
                ContainsPotentialCui: false,
                ContentClassification.Fci),
            ids.TenantId,
            ids.ActorUserId);

        Assert.True(guard.WasCalled);
        Assert.Equal(ids.ContractId, upload.ContractId);
        Assert.Equal(ContentClassification.Fci, upload.Classification);
        Assert.Equal(EvidenceUploadGuardrails.AcceptedValidationStatus, upload.ValidationStatus);
        Assert.Equal(EvidenceUploadGuardrails.PendingMalwareScanStatus, upload.MalwareScanStatus);
    }

    [Fact]
    public async Task TC_32_1_3_Activation_without_source_clause_or_rationale_fails()
    {
        var ids = StoryIds.Create();
        var service = CreateService(out _, out var auditWriter);
        var applicability = await service.RecordAsync(
            CreateRequest(ids) with
            {
                SourceClause = null,
                Rationale = " "
            },
            ids.TenantId,
            ids.ActorUserId);

        var exception = await Assert.ThrowsAsync<LaborApplicabilityValidationException>(() =>
            service.ActivateAsync(applicability.Id, ids.ActorUserId));

        Assert.Contains("source clause", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(auditWriter.Events, auditEvent => auditEvent.Summary.Contains("activated", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task TC_32_1_4_Activation_creates_or_updates_linked_review_task()
    {
        var ids = StoryIds.Create();
        var service = CreateService(out _, out _);
        var applicability = await service.RecordAsync(CreateRequest(ids), ids.TenantId, ids.ActorUserId);

        var activated = await service.ActivateAsync(applicability.Id, ids.ActorUserId);
        var reactivated = await service.ActivateAsync(applicability.Id, ids.ActorUserId);

        Assert.NotNull(activated?.ReviewTask);
        Assert.Equal(ids.ContractId, activated.ReviewTask.ContractId);
        Assert.Equal("WaitingForReview", activated.ReviewTask.Status);
        Assert.Equal(activated.ReviewTask.Id, reactivated?.ReviewTask?.Id);
    }

    [Fact]
    public async Task TC_32_1_5_Create_update_activate_and_deactivate_are_audited()
    {
        var ids = StoryIds.Create();
        var service = CreateService(out _, out var auditWriter);
        var applicability = await service.RecordAsync(CreateRequest(ids), ids.TenantId, ids.ActorUserId);
        var updated = await service.UpdateAsync(applicability.Id, CreateRequest(ids) with { PlaceOfPerformance = "Richmond, VA" }, ids.ActorUserId);
        await service.ActivateAsync(applicability.Id, ids.ActorUserId);
        await service.DeactivateAsync(applicability.Id, ids.ActorUserId);

        var laborEvents = auditWriter.Events.Where(auditEvent => auditEvent.EntityType == "LaborApplicability").ToArray();
        Assert.Equal(4, laborEvents.Length);
        Assert.Equal(AuditAction.Created, laborEvents[0].Action);
        Assert.Equal("Richmond, VA", updated?.PlaceOfPerformance);
        Assert.Equal("Active", laborEvents[2].Metadata["status"]);
        Assert.Equal("Inactive", laborEvents[3].Metadata["status"]);
        Assert.All(laborEvents, auditEvent =>
        {
            Assert.Equal(ids.TenantId, auditEvent.TenantId);
            Assert.Equal(ids.ActorUserId, auditEvent.ActorUserId);
            Assert.Equal(ids.ContractId.ToString(), auditEvent.Metadata["contractId"]);
        });
    }

    private static LaborApplicabilityService CreateService(
        out CapturingLaborUploadGuard guard,
        out CapturingAuditEventWriter auditWriter)
    {
        guard = new CapturingLaborUploadGuard();
        auditWriter = new CapturingAuditEventWriter();
        return new LaborApplicabilityService(new InMemoryLaborApplicabilityRepository(), guard, auditWriter);
    }

    private static LaborApplicabilityRequest CreateRequest(StoryIds ids) =>
        new(
            ids.ContractId,
            "SCA",
            "Norfolk, VA",
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31),
            "WD-2015-4341 Rev 24",
            ids.EvidenceItemId,
            "FAR 52.222-41",
            "Service Contract Labor Standards applies to covered services.",
            "Contracts/HR");

    private sealed class CapturingLaborUploadGuard : ILaborWageDeterminationUploadGuard
    {
        public bool WasCalled { get; private set; }

        public Task EnsureAllowedAsync(
            WageDeterminationUploadRequest request,
            Guid actorUserId,
            CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            return Task.CompletedTask;
        }
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

    private sealed record StoryIds(Guid TenantId, Guid ContractId, Guid EvidenceItemId, Guid ActorUserId)
    {
        public static StoryIds Create() =>
            new(
                Guid.Parse("32132132-2132-1321-3213-2132132132aa"),
                Guid.Parse("32132132-2132-1321-3213-2132132132bb"),
                Guid.Parse("32132132-2132-1321-3213-2132132132cc"),
                Guid.Parse("32132132-2132-1321-3213-2132132132dd"));
    }
}

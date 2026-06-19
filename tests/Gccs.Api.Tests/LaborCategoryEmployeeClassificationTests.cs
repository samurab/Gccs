using Gccs.Application.Audit;
using Gccs.Application.Labor;
using Gccs.Domain.Audit;
using Gccs.Infrastructure.Labor;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class LaborCategoryEmployeeClassificationTests
{
    [Fact]
    public async Task TC_32_2_1_Create_labor_category_and_employee_assignment()
    {
        var ids = StoryIds.Create();
        var service = CreateService(out _);

        var category = await service.CreateCategoryAsync(CreateCategory(ids), ids.TenantId, ids.ActorUserId);
        var assignment = await service.CreateAssignmentAsync(CreateAssignment(ids, category.Id), ids.TenantId, ids.ActorUserId);

        Assert.Equal(ids.ContractId, category.ContractId);
        Assert.Equal("Help Desk Technician II", category.Title);
        Assert.Equal(34.12m, category.HourlyWage);
        Assert.Equal(4.98m, category.FringeRate);
        Assert.Equal("WD-2015-4341 Rev 24", category.SourceReference);
        Assert.Equal(ids.EmployeeId, assignment.EmployeeId);
        Assert.Equal("Taylor Employee", assignment.EmployeeName);
        Assert.Equal(category.Id, assignment.CategoryId);
        Assert.Equal("Help Desk Technician II", assignment.LaborCategoryTitle);
        Assert.Equal(LaborAssignmentStatus.Active, assignment.Status);
    }

    [Fact]
    public async Task TC_32_2_2_Assignment_validation_rejects_inactive_missing_source_and_date_conflict()
    {
        var ids = StoryIds.Create();
        var service = CreateService(out _);
        var category = await service.CreateCategoryAsync(CreateCategory(ids), ids.TenantId, ids.ActorUserId);
        await service.CreateAssignmentAsync(CreateAssignment(ids, category.Id), ids.TenantId, ids.ActorUserId);
        await service.DeactivateCategoryAsync(category.Id, ids.ActorUserId);

        await Assert.ThrowsAsync<LaborClassificationValidationException>(() =>
            service.CreateAssignmentAsync(CreateAssignment(ids, category.Id) with { EmployeeId = ids.SecondEmployeeId }, ids.TenantId, ids.ActorUserId));

        var activeCategory = await service.CreateCategoryAsync(CreateCategory(ids) with { Title = "Systems Administrator" }, ids.TenantId, ids.ActorUserId);
        await Assert.ThrowsAsync<LaborClassificationValidationException>(() =>
            service.CreateAssignmentAsync(CreateAssignment(ids, activeCategory.Id) with { SourceReference = " " }, ids.TenantId, ids.ActorUserId));
        await Assert.ThrowsAsync<LaborClassificationValidationException>(() =>
            service.CreateAssignmentAsync(CreateAssignment(ids, activeCategory.Id), ids.TenantId, ids.ActorUserId));
    }

    [Fact]
    public async Task TC_32_2_3_Sensitive_employee_fields_are_permission_restricted()
    {
        var ids = StoryIds.Create();
        var service = CreateService(out _);
        var category = await service.CreateCategoryAsync(CreateCategory(ids), ids.TenantId, ids.ActorUserId);
        var assignment = await service.CreateAssignmentAsync(CreateAssignment(ids, category.Id), ids.TenantId, ids.ActorUserId);

        var hrView = await service.ViewAssignmentAsync(assignment.Id, canViewSensitiveEmployeeData: true);
        var restrictedView = await service.ViewAssignmentAsync(assignment.Id, canViewSensitiveEmployeeData: false);

        Assert.Equal("Taylor Employee", hrView?.EmployeeName);
        Assert.Equal("taylor@example.test", hrView?.EmployeeEmail);
        Assert.Null(restrictedView?.EmployeeName);
        Assert.Null(restrictedView?.EmployeeEmail);
        Assert.Equal(ids.EmployeeId, restrictedView?.EmployeeId);
        Assert.Equal("Help Desk Technician II", restrictedView?.LaborCategoryTitle);
    }

    [Fact]
    public async Task TC_32_2_4_Classification_history_preserves_prior_new_actor_timestamp_and_reason()
    {
        var ids = StoryIds.Create();
        var service = CreateService(out _);
        var original = await service.CreateCategoryAsync(CreateCategory(ids), ids.TenantId, ids.ActorUserId);
        var next = await service.CreateCategoryAsync(CreateCategory(ids) with { Title = "Network Technician III" }, ids.TenantId, ids.ActorUserId);
        var assignment = await service.CreateAssignmentAsync(CreateAssignment(ids, original.Id), ids.TenantId, ids.ActorUserId);

        var reclassified = await service.ReclassifyAsync(assignment.Id, next.Id, "Promotion and revised WD mapping.", ids.ActorUserId);

        var history = Assert.Single(reclassified?.History ?? []);
        Assert.Equal(original.Id, history.PriorCategoryId);
        Assert.Equal("Help Desk Technician II", history.PriorCategoryTitle);
        Assert.Equal(next.Id, history.NewCategoryId);
        Assert.Equal("Network Technician III", history.NewCategoryTitle);
        Assert.Equal(ids.ActorUserId, history.ActorUserId);
        Assert.NotEqual(default, history.ChangedAt);
        Assert.Equal("Promotion and revised WD mapping.", history.Reason);
    }

    [Fact]
    public async Task TC_32_2_5_Create_update_deactivate_and_reclassify_are_audited()
    {
        var ids = StoryIds.Create();
        var service = CreateService(out var auditWriter);
        var original = await service.CreateCategoryAsync(CreateCategory(ids), ids.TenantId, ids.ActorUserId);
        var next = await service.CreateCategoryAsync(CreateCategory(ids) with { Title = "Network Technician III" }, ids.TenantId, ids.ActorUserId);
        var assignment = await service.CreateAssignmentAsync(CreateAssignment(ids, original.Id), ids.TenantId, ids.ActorUserId);
        await service.UpdateAssignmentAsync(assignment.Id, CreateAssignment(ids, original.Id) with { WorkLocation = "Richmond, VA" }, ids.ActorUserId);
        await service.DeactivateAssignmentAsync(assignment.Id, ids.ActorUserId);
        await service.ReclassifyAsync(assignment.Id, next.Id, "Correction after HR review.", ids.ActorUserId);

        var assignmentEvents = auditWriter.Events.Where(auditEvent => auditEvent.EntityType == "LaborEmployeeAssignment").ToArray();
        Assert.Equal(4, assignmentEvents.Length);
        Assert.Equal(AuditAction.Created, assignmentEvents[0].Action);
        Assert.Equal("Inactive", assignmentEvents[2].Metadata["status"]);
        Assert.Equal("1", assignmentEvents[3].Metadata["historyCount"]);
        Assert.All(assignmentEvents, auditEvent =>
        {
            Assert.Equal(ids.TenantId, auditEvent.TenantId);
            Assert.Equal(ids.ActorUserId, auditEvent.ActorUserId);
            Assert.Equal(ids.ContractId.ToString(), auditEvent.Metadata["contractId"]);
        });
    }

    private static LaborClassificationService CreateService(out CapturingAuditEventWriter auditWriter)
    {
        auditWriter = new CapturingAuditEventWriter();
        return new LaborClassificationService(new InMemoryLaborClassificationRepository(), auditWriter);
    }

    private static LaborCategoryRequest CreateCategory(StoryIds ids) =>
        new(
            ids.ContractId,
            "Help Desk Technician II",
            "Computer Operator IV",
            34.12m,
            4.98m,
            "Health and welfare fringe",
            new DateOnly(2026, 1, 1),
            null,
            "WD-2015-4341 Rev 24");

    private static LaborEmployeeAssignmentRequest CreateAssignment(StoryIds ids, Guid categoryId) =>
        new(
            ids.EmployeeId,
            "Taylor Employee",
            "taylor@example.test",
            ids.ContractId,
            categoryId,
            "Norfolk, VA",
            new DateOnly(2026, 1, 1),
            null,
            "HR classification review 2026-01");

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

    private sealed record StoryIds(
        Guid TenantId,
        Guid ContractId,
        Guid EmployeeId,
        Guid SecondEmployeeId,
        Guid ActorUserId)
    {
        public static StoryIds Create() =>
            new(
                Guid.Parse("32232232-2232-2322-3223-2232232232aa"),
                Guid.Parse("32232232-2232-2322-3223-2232232232bb"),
                Guid.Parse("32232232-2232-2322-3223-2232232232cc"),
                Guid.Parse("32232232-2232-2322-3223-2232232232dd"),
                Guid.Parse("32232232-2232-2322-3223-2232232232ee"));
    }
}

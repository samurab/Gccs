using Gccs.Application.Security;
using Gccs.Application.Tasks;
using Gccs.Domain.Common;
using Gccs.Domain.Compliance;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Gccs.Infrastructure.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class ComplianceTaskSearchPersistenceTests
{
    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task PostgreSQL_search_applies_filters_pagination_and_tenant_scope()
    {
        var connectionString = Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION") ??
            throw new InvalidOperationException("Set GCCS_TEST_POSTGRES_CONNECTION to run this test.");
        var options = new DbContextOptionsBuilder<GccsDbContext>().UseGccsPostgres(connectionString).Options;
        var tenantA = Guid.NewGuid(); var tenantB = Guid.NewGuid(); var owner = Guid.NewGuid();
        await using (var setup = new GccsDbContext(options))
        {
            await PostgresTestDatabase.MigrateAsync(setup);
            setup.Tenants.AddRange(Tenant(tenantA), Tenant(tenantB));
            setup.ComplianceTasks.AddRange(
                Task(tenantA, owner, ComplianceTaskStatus.InProgress, new DateOnly(2026, 7, 1), "First"),
                Task(tenantA, owner, ComplianceTaskStatus.InProgress, new DateOnly(2026, 7, 2), "Second"),
                Task(tenantB, owner, ComplianceTaskStatus.InProgress, new DateOnly(2026, 7, 1), "Other tenant"));
            await setup.SaveChangesAsync();
        }

        try
        {
            await using var db = new GccsDbContext(options);
            var repository = new EfComplianceTaskRepository(db, new FixedTenantContext(tenantA, owner));
            var page = await repository.SearchCurrentTenantAsync(
                ComplianceTaskStatus.InProgress,
                new ComplianceTaskSearchQuery("in_progress", owner, new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 2), 1, 1),
                null);

            Assert.Equal(2, page.TotalCount);
            Assert.Equal("First", Assert.Single(page.Items).Title);
            Assert.Equal("in_progress", page.Items[0].Status);
            Assert.True(page.HasMore);

            var first = page.Items[0];
            var next = await repository.SearchCurrentTenantAsync(
                ComplianceTaskStatus.InProgress,
                new ComplianceTaskSearchQuery("in_progress", owner, new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 2), 1, 1),
                new ComplianceTaskCursor(first.DueAt, first.CreatedAt, first.Id));
            Assert.Equal("Second", Assert.Single(next.Items).Title);
            Assert.False(next.HasMore);
        }
        finally
        {
            await using var cleanup = new GccsDbContext(options);
            cleanup.ComplianceTasks.RemoveRange(cleanup.ComplianceTasks.Where(x => x.TenantId == tenantA || x.TenantId == tenantB));
            cleanup.Tenants.RemoveRange(cleanup.Tenants.Where(x => x.Id == tenantA || x.Id == tenantB));
            await cleanup.SaveChangesAsync();
        }
    }

    private static TenantEntity Tenant(Guid id) => new()
    {
        Id = id, Name = $"Task search {id:N}", Status = TenantStatus.Active, DataPosture = TenantDataPosture.NoCui,
        CreatedAt = DateTimeOffset.UtcNow
    };

    private static ComplianceTaskEntity Task(Guid tenantId, Guid owner, ComplianceTaskStatus status, DateOnly dueAt, string title) => new()
    {
        Id = Guid.NewGuid(), TenantId = tenantId, Title = title, Description = "Search fixture",
        Type = ComplianceTaskType.ObligationAction, Status = status, RiskLevel = RiskLevel.Medium,
        AssignedToUserId = owner, OwnerFunction = "Compliance", DueAt = dueAt, CreatedAt = DateTimeOffset.UtcNow
    };

    private sealed record FixedTenantContext(Guid TenantId, Guid UserId) : ICurrentTenantContext
    {
        public string UserEmail => "task.search@example.invalid";
    }
}

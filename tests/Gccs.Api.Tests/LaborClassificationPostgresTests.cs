using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Application.Labor;
using Gccs.Application.Security;
using Gccs.Domain.Audit;
using Gccs.Domain.Contracts;
using Gccs.Domain.People;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Common;
using Gccs.Infrastructure.Labor;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class LaborClassificationPostgresTests
{
    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task Concurrent_overlap_constraint_rejects_second_active_assignment()
    {
        var options = Options();
        var ids = Ids.Create();
        await SeedAsync(options, ids);
        try
        {
            await using (var first = new GccsDbContext(options))
            {
                first.LaborEmployeeAssignments.Add(Assignment(ids, Guid.NewGuid(), new(2026, 1, 1), new(2026, 6, 30)));
                await first.SaveChangesAsync();
            }

            await using var second = new GccsDbContext(options);
            second.LaborEmployeeAssignments.Add(Assignment(ids, Guid.NewGuid(), new(2026, 6, 30), new(2026, 12, 31)));
            var exception = await Assert.ThrowsAsync<DbUpdateException>(() => second.SaveChangesAsync());
            Assert.Equal(PostgresErrorCodes.ExclusionViolation, Assert.IsType<PostgresException>(exception.InnerException).SqlState);

            await using var verify = new GccsDbContext(options);
            Assert.Single(await verify.LaborEmployeeAssignments.AsNoTracking().Where(x => x.TenantId == ids.TenantId).ToArrayAsync());
        }
        finally
        {
            await CleanupAsync(options, ids.TenantId);
        }
    }

    [PostgresFact]
    [Trait("Category", "PostgresIntegration")]
    public async Task Audit_failure_rolls_back_labor_category_write()
    {
        var options = Options();
        var ids = Ids.Create();
        await SeedAsync(options, ids);
        try
        {
            await using var db = new GccsDbContext(options);
            using var services = new ServiceCollection().AddSingleton(db).BuildServiceProvider();
            var service = new LaborClassificationService(
                new EfLaborClassificationRepository(db, new FixedTenantContext(ids.TenantId, ids.ActorUserId)),
                new FailingAuditWriter());
            var transaction = new EfApplicationTransaction(services);

            await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.ExecuteAsync(
                async cancellationToken => await service.CreateCategoryAsync(Category(ids), ids.TenantId, ids.ActorUserId, cancellationToken),
                CancellationToken.None));

            await using var verify = new GccsDbContext(options);
            var categories = await verify.LaborCategories.AsNoTracking().Where(x => x.TenantId == ids.TenantId).ToArrayAsync();
            Assert.Single(categories);
            Assert.DoesNotContain(categories, x => x.CreatedByUserId == ids.ActorUserId);
            Assert.Empty(await verify.AuditLogEntries.AsNoTracking().Where(x => x.TenantId == ids.TenantId).ToArrayAsync());
        }
        finally
        {
            await CleanupAsync(options, ids.TenantId);
        }
    }

    private static DbContextOptions<GccsDbContext> Options() => new DbContextOptionsBuilder<GccsDbContext>()
        .UseGccsPostgres(Environment.GetEnvironmentVariable("GCCS_TEST_POSTGRES_CONNECTION")!).Options;

    private static async Task SeedAsync(DbContextOptions<GccsDbContext> options, Ids ids)
    {
        await using var db = new GccsDbContext(options);
        await PostgresTestDatabase.MigrateAsync(db);
        var now = DateTimeOffset.UtcNow;
        db.Tenants.Add(new TenantEntity { Id = ids.TenantId, Name = "Story 32.2 PostgreSQL tenant", Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui, CreatedAt = now });
        db.Contracts.Add(new ContractEntity { Id = ids.ContractId, TenantId = ids.TenantId, ContractNumber = $"LAB-{ids.ContractId:N}",
            Title = "Labor classification constraint", AgencyOrPrimeName = "Synthetic agency", Relationship = ContractorRelationship.Prime,
            Kind = ContractKind.FixedPrice, Status = ContractStatus.Active, PeriodOfPerformanceStart = new(2026, 1, 1),
            PeriodOfPerformanceEnd = new(2026, 12, 31), PlaceOfPerformance = "Norfolk, VA", Description = "Synthetic No-CUI fixture.", CreatedAt = now });
        db.Employees.Add(new EmployeeEntity { Id = ids.EmployeeId, TenantId = ids.TenantId, EmployeeNumber = "E-100",
            Name = "Synthetic Employee", Email = "synthetic@example.invalid", Status = EmploymentStatus.Active, CreatedAt = now });
        db.LaborCategories.Add(new LaborCategoryEntity { Id = ids.CategoryId, TenantId = ids.TenantId, ContractId = ids.ContractId,
            Title = "Help Desk Technician II", WageDeterminationClassification = "Computer Operator IV", HourlyWage = 34.12m,
            FringeRate = 4.98m, FringeDescription = "Health and welfare", EffectiveStart = new(2026, 1, 1),
            EffectiveEnd = new(2026, 12, 31), SourceReference = "WD-2015-4341 Rev 24", CreatedAt = now });
        await db.SaveChangesAsync();
    }

    private static LaborEmployeeAssignmentEntity Assignment(Ids ids, Guid id, DateOnly start, DateOnly end) => new()
    {
        Id = id, TenantId = ids.TenantId, EmployeeId = ids.EmployeeId, ContractId = ids.ContractId,
        LaborCategoryId = ids.CategoryId, WorkLocation = "Norfolk, VA", EffectiveStart = start, EffectiveEnd = end,
        Status = LaborAssignmentStatus.Active, SourceReference = "HR review", ReviewStatus = LaborClassificationReviewStatus.PendingReview,
        CreatedAt = DateTimeOffset.UtcNow
    };

    private static LaborCategoryRequest Category(Ids ids) => new(ids.ContractId, "Help Desk Technician II",
        "Computer Operator IV", 34.12m, 4.98m, "Health and welfare", new(2026, 1, 1), new(2026, 12, 31), "WD-2015-4341 Rev 24");

    private static async Task CleanupAsync(DbContextOptions<GccsDbContext> options, Guid tenantId)
    {
        await using var db = new GccsDbContext(options);
        await db.AuditLogEntries.Where(x => x.TenantId == tenantId).ExecuteDeleteAsync();
        await db.LaborClassificationEvidence.Where(x => x.TenantId == tenantId).ExecuteDeleteAsync();
        await db.LaborClassificationHistory.Where(x => x.TenantId == tenantId).ExecuteDeleteAsync();
        await db.LaborEmployeeAssignments.Where(x => x.TenantId == tenantId).ExecuteDeleteAsync();
        await db.LaborCategories.Where(x => x.TenantId == tenantId).ExecuteDeleteAsync();
        await db.Employees.Where(x => x.TenantId == tenantId).ExecuteDeleteAsync();
        await db.Contracts.Where(x => x.TenantId == tenantId).ExecuteDeleteAsync();
        await db.Tenants.Where(x => x.Id == tenantId).ExecuteDeleteAsync();
    }

    private sealed record FixedTenantContext(Guid TenantId, Guid UserId) : ICurrentTenantContext
    {
        public string UserEmail => "labor-reviewer@example.invalid";
    }

    private sealed class FailingAuditWriter : IAuditEventWriter
    {
        public Task WriteAsync(Guid tenantId, Guid actorUserId, AuditAction action, string entityType, string entityId,
            string summary, IReadOnlyDictionary<string, string>? metadata = null, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Synthetic audit failure.");
    }

    private sealed record Ids(Guid TenantId, Guid ContractId, Guid EmployeeId, Guid CategoryId, Guid ActorUserId)
    {
        public static Ids Create() => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
    }
}

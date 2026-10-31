using Gccs.Application.Reports;
using Gccs.Application.Security;
using Gccs.Domain.Reports;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Gccs.Infrastructure.Reports;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class ReportExportRepositoryTests
{
    [Fact]
    public async Task Processing_failure_requeues_until_the_bounded_attempt_limit_then_fails_terminally()
    {
        var tenantId = Guid.NewGuid();
        var reportId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        await using var dbContext = new GccsDbContext(new DbContextOptionsBuilder<GccsDbContext>()
            .UseInMemoryDatabase($"report-export-retry-{Guid.NewGuid():N}")
            .Options);
        dbContext.Tenants.Add(new TenantEntity
        {
            Id = tenantId,
            Name = "Retry tenant",
            Status = TenantStatus.Active,
            DataPosture = TenantDataPosture.NoCui,
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.Reports.Add(new ReportEntity
        {
            Id = reportId,
            TenantId = tenantId,
            Type = ReportType.ComplianceStatus,
            Title = "Retry report",
            Status = ReportStatus.Complete,
            GeneratedAt = DateTimeOffset.UtcNow,
            GeneratedByUserId = actorId,
            SnapshotJson = "{}",
            CreatedAt = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();
        var repository = new EfReportExportRepository(dbContext, new FixedTenantContext(tenantId, actorId));
        var requested = await repository.RequestPdfAsync(reportId, actorId);
        Assert.NotNull(requested);

        var firstClaim = await repository.TryClaimNextAsync(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(5), 2);
        Assert.NotNull(firstClaim);
        var retry = await repository.RecordProcessingFailureAsync(
            firstClaim!.ExportId,
            firstClaim.LeaseId,
            2,
            "synthetic_transient_failure");
        Assert.Equal(ReportExportStatus.Queued, retry?.Status);
        Assert.Null(retry?.CompletedAt);

        var secondClaim = await repository.TryClaimNextAsync(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(5), 2);
        Assert.NotNull(secondClaim);
        var failed = await repository.RecordProcessingFailureAsync(
            secondClaim!.ExportId,
            secondClaim.LeaseId,
            2,
            "synthetic_repeated_failure");
        Assert.Equal(ReportExportStatus.Failed, failed?.Status);
        Assert.NotNull(failed?.CompletedAt);
        Assert.Null(await repository.TryClaimNextAsync(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(5), 2));

        var requeued = await repository.RequestPdfAsync(reportId, actorId);
        Assert.NotNull(requeued);
        Assert.False(requeued!.Created);
        Assert.True(requeued.Queued);
        Assert.Equal(ReportExportStatus.Queued, requeued.Export.Status);
        Assert.Null(requeued.Export.FailureCode);

        var retryClaim = await repository.TryClaimNextAsync(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(5), 2);
        Assert.NotNull(retryClaim);
        Assert.Equal(1, retryClaim!.AttemptNumber);
    }

    private sealed class FixedTenantContext(Guid tenantId, Guid userId) : ICurrentTenantContext
    {
        public Guid TenantId => tenantId;
        public Guid UserId => userId;
        public string UserEmail => "owner@example.test";
    }
}

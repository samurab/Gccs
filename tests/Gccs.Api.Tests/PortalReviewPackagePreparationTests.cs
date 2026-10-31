using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Application.Portals;
using Gccs.Application.Reports;
using Gccs.Domain.Audit;
using Gccs.Domain.Common;
using Gccs.Domain.Compliance;
using Gccs.Domain.Evidence;
using Gccs.Domain.Reports;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Gccs.Infrastructure.Portals;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Gccs.Api.Tests;

public sealed class PortalReviewPackagePreparationTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 13, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Obligation_matrix_is_persisted_as_tenant_scoped_immutable_report_with_safe_evidence()
    {
        var tenantId = Guid.NewGuid(); var actorId = Guid.NewGuid(); var contractId = Guid.NewGuid(); var evidenceId = Guid.NewGuid();
        await using var db = CreateDb(nameof(Obligation_matrix_is_persisted_as_tenant_scoped_immutable_report_with_safe_evidence));
        db.EvidenceItems.Add(new EvidenceItemEntity
        {
            Id = evidenceId, TenantId = tenantId, Name = "Approved policy", Description = "Safe",
            Type = EvidenceType.Policy, Status = EvidenceStatus.Approved, Classification = ContentClassification.Fci,
            ApprovedAt = Now, ApprovedByUserId = actorId, CreatedAt = Now
        });
        await db.SaveChangesAsync();
        var repository = new EfPortalReviewPackagePreparationRepository(db,
            new MatrixRepository(contractId, evidenceId));

        var result = await repository.CreateAsync(new PreparePortalReviewPackageRequest(
            PortalReviewPreparationSource.ContractObligationMatrix, contractId,
            "Approved obligation matrix", ContentClassification.Fci), tenantId, actorId, Now);

        var report = await db.Reports.Include(item => item.Contracts).Include(item => item.EvidenceItems)
            .SingleAsync(item => item.Id == result.PackageId);
        Assert.Equal(tenantId, report.TenantId);
        Assert.Equal(ReportType.ContractObligationMatrix, report.Type);
        Assert.Equal(ReportStatus.Complete, report.Status);
        Assert.Equal(contractId, Assert.Single(report.Contracts).ContractId);
        Assert.Equal(evidenceId, Assert.Single(report.EvidenceItems).EvidenceItemId);
        Assert.Contains("Approved policy", report.ExportHtml);
    }

    [Fact]
    public async Task Audit_log_package_excludes_other_tenant_and_raw_metadata()
    {
        var tenantId = Guid.NewGuid(); var otherTenantId = Guid.NewGuid(); var actorId = Guid.NewGuid();
        await using var db = CreateDb(nameof(Audit_log_package_excludes_other_tenant_and_raw_metadata));
        db.AuditLogEntries.AddRange(
            Audit(tenantId, actorId, "Current tenant event", "{\"secret\":\"not exported\"}"),
            Audit(otherTenantId, actorId, "Other tenant event", "{}"));
        await db.SaveChangesAsync();
        var repository = new EfPortalReviewPackagePreparationRepository(db, new MatrixRepository(Guid.NewGuid(), Guid.NewGuid()));

        var result = await repository.CreateAsync(new PreparePortalReviewPackageRequest(
            PortalReviewPreparationSource.AuditLogExport, null, "Approved audit history",
            ContentClassification.Unclassified), tenantId, actorId, Now);

        var html = await db.Reports.Where(item => item.Id == result.PackageId).Select(item => item.ExportHtml).SingleAsync();
        Assert.Contains("Current tenant event", html);
        Assert.DoesNotContain("Other tenant event", html);
        Assert.DoesNotContain("secret", html);
    }

    [Fact]
    public async Task Preparation_rejects_CUI_and_writes_audit_atomically_for_allowed_package()
    {
        var repository = new CapturingPreparationRepository();
        var audit = new CapturingAuditWriter();
        var service = new PortalReviewPackagePreparationService(
            repository, audit, new PassThroughTransaction(), new FixedTimeProvider(Now));
        await Assert.ThrowsAsync<PortalPackageValidationException>(() => service.PrepareAsync(
            new PreparePortalReviewPackageRequest(PortalReviewPreparationSource.AuditLogExport,
                null, "CUI export", ContentClassification.Cui), Guid.NewGuid(), Guid.NewGuid()));
        Assert.False(repository.Called);
        Assert.Empty(audit.Events);

        var tenantId = Guid.NewGuid(); var actorId = Guid.NewGuid();
        await service.PrepareAsync(new PreparePortalReviewPackageRequest(
            PortalReviewPreparationSource.AuditLogExport, null, "Safe audit export",
            ContentClassification.Unclassified), tenantId, actorId);
        Assert.Single(audit.Events);
        Assert.Equal(tenantId, audit.Events[0].TenantId);
    }

    private static GccsDbContext CreateDb(string name) => new(
        new DbContextOptionsBuilder<GccsDbContext>().UseInMemoryDatabase(name).Options);

    private static AuditLogEntryEntity Audit(Guid tenantId, Guid actorId, string summary, string metadata) => new()
    {
        Id = Guid.NewGuid(), TenantId = tenantId, ActorUserId = actorId, Action = AuditAction.Viewed,
        EntityType = "Report", EntityId = Guid.NewGuid().ToString(), Summary = summary,
        MetadataJson = metadata, OccurredAt = Now
    };

    private sealed class MatrixRepository(Guid contractId, Guid evidenceId) : IContractObligationMatrixRepository
    {
        private readonly ContractObligationMatrixRowDto[] _rows =
        [
            new(contractId, "C-1", "Contract", Guid.NewGuid(), "52.204-21", "Basic Safeguarding",
                "FAR", "https://www.acquisition.gov/far/52.204-21", new DateOnly(2026, 1, 1),
                "OBL-1", "Protect FCI", "Apply safeguards", "Security", "InProgress", RiskLevel.High,
                null, [evidenceId], ["Approved policy"], false, [],
                "https://example.test/obligation", new DateOnly(2026, 1, 1))
        ];
        public Task<IReadOnlyList<ContractObligationMatrixRowDto>?> ListCurrentTenantAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ContractObligationMatrixRowDto>?>(id == contractId ? _rows : null);
        public Task<ContractObligationMatrixExportDto?> ExportCurrentTenantAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<ContractObligationMatrixExportDto?>(id == contractId
                ? new(id, "matrix.csv", "text/csv", _rows, "evidenceName\nApproved policy") : null);
    }

    private sealed class CapturingPreparationRepository : IPortalReviewPackagePreparationRepository
    {
        public bool Called { get; private set; }
        public Task<PreparedPortalReviewPackageDto> CreateAsync(PreparePortalReviewPackageRequest request,
            Guid tenantId, Guid actorUserId, DateTimeOffset generatedAt, CancellationToken cancellationToken = default)
        {
            Called = true;
            return Task.FromResult(new PreparedPortalReviewPackageDto(Guid.NewGuid(), request.SourceType,
                request.Title, request.Classification, request.ContractId, [], generatedAt));
        }
    }

    private sealed class CapturingAuditWriter : IAuditEventWriter
    {
        public List<(Guid TenantId, AuditAction Action)> Events { get; } = [];
        public Task WriteAsync(Guid tenantId, Guid actorUserId, AuditAction action, string entityType,
            string entityId, string summary, IReadOnlyDictionary<string, string>? metadata = null,
            CancellationToken cancellationToken = default)
        { Events.Add((tenantId, action)); return Task.CompletedTask; }
    }
    private sealed class PassThroughTransaction : IApplicationTransaction
    {
        public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) => operation(cancellationToken);
    }
    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}

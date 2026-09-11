using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Reports;
using Gccs.Application.Security;
using Gccs.Domain.Compliance;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Gccs.Infrastructure.Reports;

public sealed class EfEsrsReportPackageRepository(
    GccsDbContext dbContext,
    ICurrentTenantContext tenantContext) : IEsrsReportPackageRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<EsrsReportPackageDto> CreateAsync(
        EsrsReportPackageGenerateRequest request,
        EsrsReportPackageSnapshotDto snapshot,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (!await dbContext.Contracts.AsNoTracking().AnyAsync(
                contract => contract.TenantId == tenantContext.TenantId && contract.Id == request.ContractId,
                cancellationToken))
            throw new EsrsReportPackageException("The contract was not found.");
        if (!await dbContext.EsrsApplicabilities.AsNoTracking().AnyAsync(applicability =>
                applicability.TenantId == tenantContext.TenantId && applicability.ContractId == request.ContractId &&
                applicability.ReportType == request.ReportType && applicability.PeriodStart == request.PeriodStart &&
                applicability.PeriodEnd == request.PeriodEnd && dbContext.ComplianceTasks.Any(task =>
                    task.TenantId == tenantContext.TenantId && task.Id == applicability.TaskId && task.Status != ComplianceTaskStatus.Canceled),
                cancellationToken))
            throw new EsrsReportPackageException("The package period does not match an active SPR reporting obligation for this contract.");

        var nextVersion = await CurrentTenantPackages()
            .Where(package => package.ContractId == request.ContractId && package.ReportType == request.ReportType &&
                              package.PeriodStart == request.PeriodStart && package.PeriodEnd == request.PeriodEnd)
            .Select(package => (int?)package.Version)
            .MaxAsync(cancellationToken) + 1 ?? 1;
        var now = DateTimeOffset.UtcNow;
        var entity = new SprReportPackageEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId,
            ContractId = request.ContractId,
            ReportType = request.ReportType,
            PeriodStart = request.PeriodStart,
            PeriodEnd = request.PeriodEnd,
            Status = EsrsReportPackageStatus.Draft,
            Version = nextVersion,
            NotSubmittedDisclaimer = EsrsReportPackageService.NotSubmittedDisclaimer,
            SnapshotJson = JsonSerializer.Serialize(snapshot, JsonOptions),
            GeneratedAt = now,
            CreatedAt = now,
            CreatedByUserId = actorUserId
        };
        dbContext.SprReportPackages.Add(entity);
        await SaveAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<EsrsReportPackageDto?> FindAsync(Guid packageId, CancellationToken cancellationToken = default)
    {
        var entity = await CurrentTenantPackages().AsNoTracking()
            .SingleOrDefaultAsync(package => package.Id == packageId, cancellationToken);
        return entity is null ? null : ToDto(entity);
    }

    public async Task<IReadOnlyList<EsrsReportPackageDto>> ListAsync(CancellationToken cancellationToken = default) =>
        (await CurrentTenantPackages().AsNoTracking().OrderByDescending(package => package.GeneratedAt)
            .ToArrayAsync(cancellationToken)).Select(ToDto).ToArray();

    public async Task<EsrsReportPackageDto?> UpdateStatusAsync(
        Guid packageId,
        EsrsReportPackageStatus expectedStatus,
        EsrsReportPackageStatus status,
        string reviewerName,
        string? reviewNotes,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var normalizedReviewerName = reviewerName.Trim();
        var normalizedReviewNotes = string.IsNullOrWhiteSpace(reviewNotes) ? null : reviewNotes.Trim();
        var candidates = CurrentTenantPackages().Where(package => package.Id == packageId && package.Status == expectedStatus);
        if (dbContext.Database.IsRelational())
        {
            var affected = status == EsrsReportPackageStatus.Approved
                ? await candidates.ExecuteUpdateAsync(setters => setters
                    .SetProperty(package => package.Status, status)
                    .SetProperty(package => package.ReviewerName, normalizedReviewerName)
                    .SetProperty(package => package.ReviewerUserId, actorUserId)
                    .SetProperty(package => package.ReviewNotes, normalizedReviewNotes)
                    .SetProperty(package => package.ApprovedAt, now)
                    .SetProperty(package => package.UpdatedAt, now)
                    .SetProperty(package => package.UpdatedByUserId, actorUserId), cancellationToken)
                : await candidates.ExecuteUpdateAsync(setters => setters
                    .SetProperty(package => package.Status, status)
                    .SetProperty(package => package.ReviewerName, normalizedReviewerName)
                    .SetProperty(package => package.ReviewerUserId, actorUserId)
                    .SetProperty(package => package.ReviewNotes, normalizedReviewNotes)
                    .SetProperty(package => package.UpdatedAt, now)
                    .SetProperty(package => package.UpdatedByUserId, actorUserId), cancellationToken);
            if (affected == 0)
                throw new EsrsReportPackageConflictException("The SPR package status changed. Reload it and try again.");
        }
        else
        {
            var entity = await candidates.SingleOrDefaultAsync(cancellationToken);
            if (entity is null)
                throw new EsrsReportPackageConflictException("The SPR package status changed. Reload it and try again.");
            entity.Status = status;
            entity.ReviewerName = normalizedReviewerName;
            entity.ReviewerUserId = actorUserId;
            entity.ReviewNotes = normalizedReviewNotes;
            entity.ApprovedAt = status == EsrsReportPackageStatus.Approved ? now : entity.ApprovedAt;
            entity.UpdatedAt = now;
            entity.UpdatedByUserId = actorUserId;
            await SaveAsync(cancellationToken);
        }

        var updated = await CurrentTenantPackages().AsNoTracking()
            .SingleAsync(package => package.Id == packageId, cancellationToken);
        return ToDto(updated);
    }

    public async Task<SprManualSubmissionReceiptDto> CreateManualSubmissionReceiptAsync(
        Guid packageId,
        SprManualSubmissionReceiptRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var package = await CurrentTenantPackages().AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == packageId, cancellationToken)
            ?? throw new EsrsReportPackageException("The SPR package was not found.");
        if (request.EvidenceItemId is Guid evidenceId && !await dbContext.EvidenceItems.AsNoTracking().AnyAsync(
                evidence => evidence.TenantId == tenantContext.TenantId && evidence.Id == evidenceId && !evidence.IsUseBlocked,
                cancellationToken))
            throw new EsrsReportPackageException("The evidence record was not found or is blocked from use.");
        if (request.SupersedesReceiptId is Guid supersedesId && !await CurrentTenantReceipts().AsNoTracking().AnyAsync(
                receipt => receipt.Id == supersedesId && receipt.PackageId == packageId, cancellationToken))
            throw new EsrsReportPackageException("The receipt to supersede was not found for this package.");

        var now = DateTimeOffset.UtcNow;
        var entity = new SprManualSubmissionReceiptEntity
        {
            Id = Guid.NewGuid(),
            TenantId = package.TenantId,
            PackageId = packageId,
            SubmittedAt = request.SubmittedAt,
            ConfirmationReference = request.ConfirmationReference.Trim(),
            Outcome = request.Outcome,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            EvidenceItemId = request.EvidenceItemId,
            SupersedesReceiptId = request.SupersedesReceiptId,
            RecordedByUserId = actorUserId,
            RecordedAt = now
        };
        dbContext.SprManualSubmissionReceipts.Add(entity);
        await SaveAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<IReadOnlyList<SprManualSubmissionReceiptDto>> ListManualSubmissionReceiptsAsync(
        Guid packageId,
        CancellationToken cancellationToken = default) =>
        (await CurrentTenantReceipts().AsNoTracking().Where(receipt => receipt.PackageId == packageId)
            .OrderByDescending(receipt => receipt.RecordedAt).ToArrayAsync(cancellationToken)).Select(ToDto).ToArray();

    private IQueryable<SprReportPackageEntity> CurrentTenantPackages() =>
        dbContext.SprReportPackages.Where(package => package.TenantId == tenantContext.TenantId);

    private IQueryable<SprManualSubmissionReceiptEntity> CurrentTenantReceipts() =>
        dbContext.SprManualSubmissionReceipts.Where(receipt => receipt.TenantId == tenantContext.TenantId);

    private static EsrsReportPackageDto ToDto(SprReportPackageEntity entity) => new(
        entity.Id, entity.TenantId, entity.ContractId, entity.ReportType, entity.PeriodStart, entity.PeriodEnd,
        entity.Status, entity.Version, entity.NotSubmittedDisclaimer,
        JsonSerializer.Deserialize<EsrsReportPackageSnapshotDto>(entity.SnapshotJson, JsonOptions)
            ?? throw new InvalidOperationException("Stored SPR package snapshot is invalid."),
        entity.ReviewerName, entity.ApprovedAt, entity.ReviewNotes, entity.GeneratedAt, entity.UpdatedAt,
        entity.ReviewerUserId);

    private static SprManualSubmissionReceiptDto ToDto(SprManualSubmissionReceiptEntity entity) => new(
        entity.Id, entity.TenantId, entity.PackageId, entity.SubmittedAt, entity.ConfirmationReference,
        entity.Outcome, entity.Notes, entity.EvidenceItemId, entity.SupersedesReceiptId,
        entity.RecordedByUserId, entity.RecordedAt);

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new EsrsReportPackageConflictException("The SPR package changed. Reload it and try again.");
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new EsrsReportPackageConflictException("A package version conflict occurred. Reload and try again.");
        }
    }
}

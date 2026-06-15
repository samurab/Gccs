using Gccs.Application.Contracts;
using Gccs.Application.Security;
using Gccs.Domain.Common;
using Gccs.Domain.Compliance;
using Gccs.Domain.Contracts;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Contracts;

public sealed class EfContractRepository(GccsDbContext dbContext, ICurrentTenantContext tenantContext) : IContractRepository
{
    public async Task<IReadOnlyList<ContractDto>> ListCurrentTenantAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Contracts
            .AsNoTracking()
            .Where(contract => contract.TenantId == tenantContext.TenantId)
            .OrderByDescending(contract => contract.UpdatedAt ?? contract.CreatedAt)
            .ThenBy(contract => contract.ContractNumber)
            .Select(contract => ToDto(contract))
            .ToArrayAsync(cancellationToken);

    public async Task<ContractDto?> FindCurrentTenantAsync(Guid contractId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Contracts
            .AsNoTracking()
            .SingleOrDefaultAsync(
                contract => contract.TenantId == tenantContext.TenantId && contract.Id == contractId,
                cancellationToken);

        return entity is null ? null : ToDto(entity);
    }

    public async Task<IReadOnlyList<ContractDocumentDto>?> ListDocumentsAsync(
        Guid contractId,
        CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.Contracts.AnyAsync(
            contract => contract.TenantId == tenantContext.TenantId && contract.Id == contractId,
            cancellationToken);

        if (!exists)
        {
            return null;
        }

        return await dbContext.Set<ContractDocumentEntity>()
            .AsNoTracking()
            .Where(document => document.ContractId == contractId)
            .OrderByDescending(document => document.UploadedAt)
            .Select(document => ToDocumentDto(document))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ContractDeliverableDto>?> ListDeliverablesAsync(
        Guid contractId,
        CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.Contracts.AnyAsync(
            contract => contract.TenantId == tenantContext.TenantId && contract.Id == contractId,
            cancellationToken);

        if (!exists)
        {
            return null;
        }

        return await dbContext.Set<ContractDeliverableEntity>()
            .AsNoTracking()
            .Where(deliverable => deliverable.ContractId == contractId)
            .OrderBy(deliverable => deliverable.DueAt ?? DateOnly.MaxValue)
            .ThenBy(deliverable => deliverable.Name)
            .Select(deliverable => ToDeliverableDto(deliverable))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<ContractDto> CreateCurrentTenantAsync(
        UpsertContractRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var entity = new ContractEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId,
            CreatedAt = now,
            CreatedByUserId = actorUserId
        };

        Apply(entity, request);
        dbContext.Contracts.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<ContractDto?> UpdateCurrentTenantAsync(
        Guid contractId,
        UpsertContractRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Contracts
            .SingleOrDefaultAsync(
                contract => contract.TenantId == tenantContext.TenantId && contract.Id == contractId,
                cancellationToken);

        if (entity is null)
        {
            return null;
        }

        Apply(entity, request);
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedByUserId = actorUserId;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<ContractDocumentDto?> CreateDocumentMetadataAsync(
        Guid contractId,
        ContractDocumentUploadRequest request,
        Guid actorUserId,
        string noticeVersion,
        CancellationToken cancellationToken = default)
    {
        var contractExists = await dbContext.Contracts.AnyAsync(
            contract => contract.TenantId == tenantContext.TenantId && contract.Id == contractId,
            cancellationToken);

        if (!contractExists)
        {
            return null;
        }

        var documentId = Guid.NewGuid();
        var fileName = request.FileName.Trim();
        var document = new ContractDocumentEntity
        {
            Id = documentId,
            ContractId = contractId,
            Type = request.Type,
            FileName = fileName,
            ContentType = request.ContentType.Trim().ToLowerInvariant(),
            SizeBytes = request.SizeBytes,
            StorageUri = $"pending://contracts/{contractId}/documents/{documentId}/{Uri.EscapeDataString(fileName)}",
            ExtractedTextHash = null,
            ValidationStatus = "accepted",
            MalwareScanStatus = "scan-pending",
            NoticeVersion = noticeVersion,
            UploadedAt = DateTimeOffset.UtcNow,
            UploadedByUserId = actorUserId,
            ContainsPotentialCui = request.ContainsPotentialCui
        };

        dbContext.Set<ContractDocumentEntity>().Add(document);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDocumentDto(document);
    }

    public async Task<ContractDocumentDto?> DeleteDocumentAsync(
        Guid contractId,
        Guid documentId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var document = await dbContext.Set<ContractDocumentEntity>()
            .Include(item => item.Contract)
            .SingleOrDefaultAsync(
                item =>
                    item.Id == documentId &&
                    item.ContractId == contractId &&
                    item.Contract != null &&
                    item.Contract.TenantId == tenantContext.TenantId,
                cancellationToken);

        if (document is null)
        {
            return null;
        }

        var dto = ToDocumentDto(document);
        dbContext.Set<ContractDocumentEntity>().Remove(document);
        await dbContext.SaveChangesAsync(cancellationToken);
        return dto;
    }

    public async Task<ContractDeliverableDto?> CreateDeliverableAsync(
        Guid contractId,
        UpsertContractDeliverableRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var contractExists = await dbContext.Contracts.AnyAsync(
            contract => contract.TenantId == tenantContext.TenantId && contract.Id == contractId,
            cancellationToken);

        if (!contractExists)
        {
            return null;
        }

        var deliverable = new ContractDeliverableEntity
        {
            Id = Guid.NewGuid(),
            ContractId = contractId
        };

        Apply(deliverable, request);
        dbContext.Set<ContractDeliverableEntity>().Add(deliverable);
        SyncDeliverableTask(contractId, deliverable, actorUserId);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDeliverableDto(deliverable);
    }

    public async Task<ContractDeliverableDto?> UpdateDeliverableAsync(
        Guid contractId,
        Guid deliverableId,
        UpsertContractDeliverableRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var deliverable = await dbContext.Set<ContractDeliverableEntity>()
            .Include(item => item.Contract)
            .SingleOrDefaultAsync(
                item =>
                    item.Id == deliverableId &&
                    item.ContractId == contractId &&
                    item.Contract != null &&
                    item.Contract.TenantId == tenantContext.TenantId,
                cancellationToken);

        if (deliverable is null)
        {
            return null;
        }

        Apply(deliverable, request);
        SyncDeliverableTask(contractId, deliverable, actorUserId);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDeliverableDto(deliverable);
    }

    private static void Apply(ContractEntity entity, UpsertContractRequest request)
    {
        entity.ContractNumber = request.ContractNumber;
        entity.Title = request.Title;
        entity.AgencyOrPrimeName = request.AgencyOrPrimeName;
        entity.Relationship = request.Relationship;
        entity.Kind = request.Kind;
        entity.Status = request.Status;
        entity.AwardedAt = request.AwardedAt;
        entity.PeriodOfPerformanceStart = request.PeriodOfPerformanceStart;
        entity.PeriodOfPerformanceEnd = request.PeriodOfPerformanceEnd;
        entity.PlaceOfPerformance = request.PlaceOfPerformance;
        entity.Description = request.Description;
        entity.DataHandlingPosture = request.DataHandlingPosture;
    }

    private static void Apply(ContractDeliverableEntity entity, UpsertContractDeliverableRequest request)
    {
        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.DueAt = request.DueAt;
        entity.OwnerFunction = request.OwnerFunction;
        entity.Status = request.Status;
    }

    private void SyncDeliverableTask(Guid contractId, ContractDeliverableEntity deliverable, Guid actorUserId)
    {
        if (deliverable.DueAt is null)
        {
            return;
        }

        var task = dbContext.ComplianceTasks.Local.FirstOrDefault(task => task.ContractId == contractId && task.Title == deliverable.Name) ??
            dbContext.ComplianceTasks.FirstOrDefault(task => task.ContractId == contractId && task.Title == deliverable.Name);
        var now = DateTimeOffset.UtcNow;

        if (task is null)
        {
            task = new ComplianceTaskEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantContext.TenantId,
                ContractId = contractId,
                Type = ComplianceTaskType.CalendarReminder,
                RiskLevel = RiskLevel.Medium,
                CreatedAt = now,
                CreatedByUserId = actorUserId
            };
            dbContext.ComplianceTasks.Add(task);
        }
        else
        {
            task.UpdatedAt = now;
            task.UpdatedByUserId = actorUserId;
        }

        task.Title = deliverable.Name;
        task.Description = deliverable.Description;
        task.OwnerFunction = deliverable.OwnerFunction;
        task.DueAt = deliverable.DueAt;
        task.Status = IsComplete(deliverable.Status) ? ComplianceTaskStatus.Done : ComplianceTaskStatus.Open;
    }

    private static ContractDto ToDto(ContractEntity entity) =>
        new(
            entity.Id,
            entity.TenantId,
            entity.ContractNumber,
            entity.Title,
            entity.AgencyOrPrimeName,
            entity.Relationship,
            entity.Kind,
            entity.Status,
            entity.AwardedAt,
            entity.PeriodOfPerformanceStart,
            entity.PeriodOfPerformanceEnd,
            entity.PlaceOfPerformance,
            entity.Description,
            entity.DataHandlingPosture,
            entity.CreatedAt,
            entity.UpdatedAt);

    private static ContractDocumentDto ToDocumentDto(ContractDocumentEntity entity) =>
        new(
            entity.Id,
            entity.ContractId,
            entity.Type,
            entity.FileName,
            entity.ContentType,
            entity.SizeBytes,
            entity.StorageUri,
            entity.ExtractedTextHash,
            entity.ValidationStatus,
            entity.MalwareScanStatus,
            entity.NoticeVersion,
            entity.UploadedAt,
            entity.UploadedByUserId,
            entity.ContainsPotentialCui);

    private static ContractDeliverableDto ToDeliverableDto(ContractDeliverableEntity entity) =>
        new(
            entity.Id,
            entity.ContractId,
            entity.Name,
            entity.Description,
            entity.DueAt,
            entity.OwnerFunction,
            entity.Status,
            entity.DueAt < DateOnly.FromDateTime(DateTime.UtcNow) && !IsComplete(entity.Status));

    private static bool IsComplete(DeliverableStatus status) =>
        status is DeliverableStatus.Submitted or DeliverableStatus.Accepted or DeliverableStatus.Waived;
}

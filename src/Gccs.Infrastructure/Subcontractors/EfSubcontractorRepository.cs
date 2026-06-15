using Gccs.Application.Security;
using Gccs.Application.Subcontractors;
using Gccs.Domain.Evidence;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Subcontractors;

public sealed class EfSubcontractorRepository(
    GccsDbContext dbContext,
    ICurrentTenantContext tenantContext) : ISubcontractorRepository
{
    public async Task<IReadOnlyList<SubcontractorDto>> ListCurrentTenantAsync(CancellationToken cancellationToken = default)
    {
        var subcontractors = await QueryCurrentTenant()
            .OrderBy(subcontractor => subcontractor.Name)
            .ToArrayAsync(cancellationToken);
        return subcontractors.Select(ToDto).ToArray();
    }

    public async Task<SubcontractorDto?> FindCurrentTenantAsync(Guid subcontractorId, CancellationToken cancellationToken = default)
    {
        var subcontractor = await QueryCurrentTenant()
            .SingleOrDefaultAsync(candidate => candidate.Id == subcontractorId, cancellationToken);
        return subcontractor is null ? null : ToDto(subcontractor);
    }

    public async Task<SubcontractorDto> CreateAsync(
        UpsertSubcontractorRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var entity = new SubcontractorEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = actorUserId
        };

        Apply(entity, request);
        SyncContractLinks(entity, request.ContractIds);
        dbContext.Subcontractors.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<SubcontractorDto?> UpdateAsync(
        Guid subcontractorId,
        UpsertSubcontractorRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var entity = await QueryCurrentTenant()
            .SingleOrDefaultAsync(candidate => candidate.Id == subcontractorId, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        Apply(entity, request);
        SyncContractLinks(entity, request.ContractIds);
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedByUserId = actorUserId;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<IReadOnlyList<SubcontractorFlowDownDto>?> ListFlowDownsAsync(
        Guid subcontractorId,
        Guid? contractId,
        CancellationToken cancellationToken = default)
    {
        var subcontractorExists = await dbContext.Subcontractors
            .AnyAsync(candidate => candidate.Id == subcontractorId && candidate.TenantId == tenantContext.TenantId, cancellationToken);
        if (!subcontractorExists)
        {
            return null;
        }

        var query = dbContext.FlowDownClauses
            .AsNoTracking()
            .Where(flowDown => flowDown.SubcontractorId == subcontractorId);
        if (contractId is not null)
        {
            query = query.Where(flowDown => flowDown.ContractId == contractId);
        }

        var flowDowns = await query
            .OrderBy(flowDown => flowDown.ClauseNumber)
            .ThenBy(flowDown => flowDown.Title)
            .ToArrayAsync(cancellationToken);
        return flowDowns.Select(ToDto).ToArray();
    }

    public async Task<SubcontractorFlowDownDto?> CreateFlowDownAsync(
        Guid subcontractorId,
        UpsertSubcontractorFlowDownRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var subcontractorExists = await dbContext.Subcontractors
            .AnyAsync(candidate => candidate.Id == subcontractorId && candidate.TenantId == tenantContext.TenantId, cancellationToken);
        if (!subcontractorExists)
        {
            return null;
        }

        var contractId = await ResolveAndValidateContractReferencesAsync(request, cancellationToken);
        await ValidateSignedEvidenceAsync(request.SignedEvidenceItemId, cancellationToken);

        var entity = new FlowDownClauseEntity
        {
            Id = Guid.NewGuid(),
            SubcontractorId = subcontractorId,
            ContractId = contractId,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = actorUserId
        };
        Apply(entity, request);
        dbContext.FlowDownClauses.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<SubcontractorFlowDownDto?> UpdateFlowDownAsync(
        Guid subcontractorId,
        Guid flowDownId,
        UpsertSubcontractorFlowDownRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.FlowDownClauses
            .Include(flowDown => flowDown.Subcontractor)
            .SingleOrDefaultAsync(
                candidate => candidate.Id == flowDownId &&
                    candidate.SubcontractorId == subcontractorId &&
                    candidate.Subcontractor != null &&
                    candidate.Subcontractor.TenantId == tenantContext.TenantId,
                cancellationToken);
        if (entity is null)
        {
            return null;
        }

        var contractId = await ResolveAndValidateContractReferencesAsync(request, cancellationToken);
        await ValidateSignedEvidenceAsync(request.SignedEvidenceItemId, cancellationToken);
        Apply(entity, request);
        entity.ContractId = contractId;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedByUserId = actorUserId;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    private IQueryable<SubcontractorEntity> QueryCurrentTenant() =>
        dbContext.Subcontractors
            .Include(subcontractor => subcontractor.Contracts)
            .Where(subcontractor => subcontractor.TenantId == tenantContext.TenantId);

    private static void Apply(SubcontractorEntity entity, UpsertSubcontractorRequest request)
    {
        entity.Name = request.Name;
        entity.Uei = request.Uei;
        entity.CageCode = request.CageCode;
        entity.Status = request.Status;
        entity.RoleDescription = request.RoleDescription;
        entity.SmallBusinessStatus = request.SmallBusinessStatus;
        entity.CmmcStatus = request.CmmcStatus;
        entity.InsuranceExpiresAt = request.InsuranceExpiresAt;
        entity.NdaStatus = request.NdaStatus;
        entity.WorkshareDescription = request.WorkshareDescription;
        entity.WorksharePercentage = request.WorksharePercentage;
        entity.HasFciAccess = request.HasFciAccess;
        entity.HasCuiAccess = request.HasCuiAccess;
        entity.HasExportControlledAccess = request.HasExportControlledAccess;
        entity.RequiredCmmcLevel = request.RequiredCmmcLevel;
        entity.ContactName = request.ContactName;
        entity.ContactEmail = request.ContactEmail;
        entity.ContactPhone = request.ContactPhone;
        entity.ContactTitle = request.ContactTitle;
    }

    private async Task<Guid?> ResolveAndValidateContractReferencesAsync(
        UpsertSubcontractorFlowDownRequest request,
        CancellationToken cancellationToken)
    {
        Guid? contractId = request.ContractId;
        if (request.ContractClauseId is not null)
        {
            var clause = await dbContext.Set<ContractClauseEntity>()
                .AsNoTracking()
                .Include(candidate => candidate.Contract)
                .SingleOrDefaultAsync(candidate => candidate.Id == request.ContractClauseId, cancellationToken);
            if (clause?.Contract is null || clause.Contract.TenantId != tenantContext.TenantId)
            {
                throw new SubcontractorValidationException("Contract clause was not found for the current tenant.");
            }

            if (contractId is not null && contractId != clause.ContractId)
            {
                throw new SubcontractorValidationException("Contract clause does not belong to the requested contract.");
            }

            contractId = clause.ContractId;

            if (request.ObligationId is not null)
            {
                var clauseHasObligation = await dbContext.Set<ContractClauseObligationEntity>()
                    .AnyAsync(
                        link => link.ContractClauseId == request.ContractClauseId && link.ObligationId == request.ObligationId,
                        cancellationToken);
                if (!clauseHasObligation)
                {
                    throw new SubcontractorValidationException("Obligation is not assigned to the requested contract clause.");
                }
            }
        }

        if (contractId is not null)
        {
            var contractExists = await dbContext.Contracts
                .AnyAsync(contract => contract.Id == contractId && contract.TenantId == tenantContext.TenantId, cancellationToken);
            if (!contractExists)
            {
                throw new SubcontractorValidationException("Contract was not found for the current tenant.");
            }
        }

        if (request.ObligationId is not null && request.ContractClauseId is null)
        {
            var obligationExists = await dbContext.Obligations
                .AnyAsync(obligation => obligation.Id == request.ObligationId, cancellationToken);
            if (!obligationExists)
            {
                throw new SubcontractorValidationException("Obligation was not found.");
            }
        }

        return contractId;
    }

    private async Task ValidateSignedEvidenceAsync(Guid? signedEvidenceItemId, CancellationToken cancellationToken)
    {
        if (signedEvidenceItemId is null)
        {
            return;
        }

        var evidence = await dbContext.EvidenceItems
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == signedEvidenceItemId, cancellationToken);
        if (evidence is null || evidence.TenantId != tenantContext.TenantId)
        {
            throw new SubcontractorValidationException("Signed evidence was not found for the current tenant.");
        }

        if (evidence.Status != EvidenceStatus.Approved || evidence.Type != EvidenceType.SignedFlowDown)
        {
            throw new SubcontractorValidationException("Signed evidence must be an approved signed flow-down evidence item.");
        }
    }

    private static void Apply(FlowDownClauseEntity entity, UpsertSubcontractorFlowDownRequest request)
    {
        entity.ContractClauseId = request.ContractClauseId;
        entity.ObligationId = request.ObligationId;
        entity.ClauseNumber = request.ClauseNumber;
        entity.Title = request.Title;
        entity.Status = request.Status;
        entity.SentAt = request.SentAt;
        entity.AcknowledgedAt = request.AcknowledgedAt;
        entity.SignedAt = request.SignedAt;
        entity.WaivedAt = request.WaivedAt;
        entity.SignedEvidenceItemId = request.SignedEvidenceItemId;
    }

    private static void SyncContractLinks(SubcontractorEntity entity, IReadOnlyList<Guid> contractIds)
    {
        entity.Contracts.Clear();
        foreach (var contractId in contractIds)
        {
            entity.Contracts.Add(new ContractSubcontractorEntity
            {
                ContractId = contractId,
                SubcontractorId = entity.Id
            });
        }
    }

    private static SubcontractorDto ToDto(SubcontractorEntity entity) =>
        new(
            entity.Id,
            entity.TenantId,
            entity.Name,
            entity.Uei,
            entity.CageCode,
            entity.Status,
            entity.RoleDescription,
            entity.SmallBusinessStatus,
            entity.CmmcStatus,
            entity.InsuranceExpiresAt,
            entity.NdaStatus,
            entity.WorkshareDescription,
            entity.WorksharePercentage,
            entity.HasFciAccess,
            entity.HasCuiAccess,
            entity.HasExportControlledAccess,
            entity.RequiredCmmcLevel,
            entity.ContactName,
            entity.ContactEmail,
            entity.ContactPhone,
            entity.ContactTitle,
            entity.Contracts.Select(link => link.ContractId).OrderBy(id => id).ToArray(),
            entity.CreatedAt,
            entity.UpdatedAt);

    private static SubcontractorFlowDownDto ToDto(FlowDownClauseEntity entity) =>
        new(
            entity.Id,
            entity.SubcontractorId,
            entity.ContractId,
            entity.ContractClauseId,
            entity.ObligationId,
            entity.ClauseNumber,
            entity.Title,
            entity.Status,
            entity.SentAt,
            entity.AcknowledgedAt,
            entity.SignedAt,
            entity.WaivedAt,
            entity.SignedEvidenceItemId,
            entity.CreatedAt,
            entity.UpdatedAt);
}

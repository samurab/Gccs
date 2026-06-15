using Gccs.Application.Security;
using Gccs.Application.Subcontractors;
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
}

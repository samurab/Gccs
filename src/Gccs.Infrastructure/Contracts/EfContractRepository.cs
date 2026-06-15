using Gccs.Application.Contracts;
using Gccs.Application.Security;
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
}

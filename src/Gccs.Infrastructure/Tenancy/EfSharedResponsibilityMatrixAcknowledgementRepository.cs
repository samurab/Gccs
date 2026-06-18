using Gccs.Application.Tenancy;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Tenancy;

public sealed class EfSharedResponsibilityMatrixAcknowledgementRepository(GccsDbContext dbContext)
    : ISharedResponsibilityMatrixAcknowledgementRepository
{
    public async Task<IReadOnlyList<SharedResponsibilityMatrixAcknowledgementDto>> ListAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var acknowledgements = await dbContext.SharedResponsibilityMatrixAcknowledgements
            .AsNoTracking()
            .Where(acknowledgement => acknowledgement.TenantId == tenantId)
            .OrderByDescending(acknowledgement => acknowledgement.AcknowledgedAt)
            .ToArrayAsync(cancellationToken);

        return acknowledgements.Select(ToDto).ToArray();
    }

    public async Task<SharedResponsibilityMatrixAcknowledgementDto?> FindAsync(
        Guid tenantId,
        string matrixId,
        string matrixVersion,
        CancellationToken cancellationToken = default)
    {
        var acknowledgement = await dbContext.SharedResponsibilityMatrixAcknowledgements
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.TenantId == tenantId &&
                    candidate.MatrixId == matrixId &&
                    candidate.MatrixVersion == matrixVersion,
                cancellationToken);

        return acknowledgement is null ? null : ToDto(acknowledgement);
    }

    public async Task<SharedResponsibilityMatrixAcknowledgementDto> AddAsync(
        Guid tenantId,
        string matrixId,
        string matrixVersion,
        string matrixTitle,
        Guid actorUserId,
        DateTimeOffset acknowledgedAt,
        CancellationToken cancellationToken = default)
    {
        var acknowledgement = new SharedResponsibilityMatrixAcknowledgementEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            MatrixId = matrixId,
            MatrixVersion = matrixVersion,
            MatrixTitle = matrixTitle,
            AcknowledgedByUserId = actorUserId,
            AcknowledgedAt = acknowledgedAt,
            CreatedAt = acknowledgedAt,
            CreatedByUserId = actorUserId
        };

        dbContext.SharedResponsibilityMatrixAcknowledgements.Add(acknowledgement);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(acknowledgement);
    }

    private static SharedResponsibilityMatrixAcknowledgementDto ToDto(SharedResponsibilityMatrixAcknowledgementEntity entity) =>
        new(
            entity.Id,
            entity.TenantId,
            entity.MatrixId,
            entity.MatrixVersion,
            entity.MatrixTitle,
            entity.AcknowledgedByUserId,
            entity.AcknowledgedAt,
            SharedResponsibilityMatrixAcknowledgementStatus.Outdated);
}

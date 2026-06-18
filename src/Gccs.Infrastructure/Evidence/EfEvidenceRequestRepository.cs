using Gccs.Application.Evidence;
using Gccs.Application.Security;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Evidence;

public sealed class EfEvidenceRequestRepository(
    GccsDbContext dbContext,
    ICurrentTenantContext tenantContext) : IEvidenceRequestRepository
{
    public async Task<EvidenceRequestDto> CreateAsync(
        CreateEvidenceRequestRequest request,
        Guid requesterUserId,
        CancellationToken cancellationToken = default)
    {
        await ValidateAssigneeAsync(request, cancellationToken);
        await ValidateRelatedRecordAsync(request, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var entity = new EvidenceRequestEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId,
            RequesterUserId = requesterUserId,
            AssigneeUserId = request.AssigneeUserId,
            AssigneeSubcontractorId = request.AssigneeSubcontractorId,
            DueDate = request.DueDate,
            Status = EvidenceRequestStatus.Open.ToString(),
            Instructions = request.Instructions.Trim(),
            RelatedRecordType = request.RelatedRecordType.ToString(),
            RelatedRecordId = request.RelatedRecordId.Trim(),
            CreatedAt = now,
            CreatedByUserId = requesterUserId
        };
        dbContext.EvidenceRequests.Add(entity);

        if (request.AssigneeUserId is not null)
        {
            dbContext.NotificationDeliveries.Add(new NotificationDeliveryEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantContext.TenantId,
                UserId = request.AssigneeUserId.Value,
                SourceTaskId = entity.Id,
                SourceType = "EvidenceRequest",
                LinkUrl = $"/evidence-requests/{entity.Id}",
                Category = "evidence_request",
                Status = "Delivered",
                Placeholder = $"Evidence request due {request.DueDate:O}: {request.Instructions.Trim()}",
                AttemptedAt = now,
                CreatedAt = now,
                CreatedByUserId = requesterUserId
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    private async Task ValidateAssigneeAsync(CreateEvidenceRequestRequest request, CancellationToken cancellationToken)
    {
        if (request.AssigneeUserId is not null)
        {
            var userExists = await dbContext.Users.AnyAsync(
                user => user.Id == request.AssigneeUserId && user.TenantId == tenantContext.TenantId,
                cancellationToken);
            if (!userExists)
            {
                throw new EvidenceRequestValidationException("Assignee user was not found for the current tenant.");
            }
        }

        if (request.AssigneeSubcontractorId is not null)
        {
            var subcontractorExists = await dbContext.Subcontractors.AnyAsync(
                subcontractor => subcontractor.Id == request.AssigneeSubcontractorId && subcontractor.TenantId == tenantContext.TenantId,
                cancellationToken);
            if (!subcontractorExists)
            {
                throw new EvidenceRequestValidationException("Assignee subcontractor was not found for the current tenant.");
            }
        }
    }

    private async Task ValidateRelatedRecordAsync(CreateEvidenceRequestRequest request, CancellationToken cancellationToken)
    {
        var exists = request.RelatedRecordType switch
        {
            EvidenceRequestRelatedRecordType.Obligation => await dbContext.Obligations.AnyAsync(obligation => obligation.Id == request.RelatedRecordId, cancellationToken),
            EvidenceRequestRelatedRecordType.Control => await dbContext.Controls.AnyAsync(control => control.Id == request.RelatedRecordId, cancellationToken),
            EvidenceRequestRelatedRecordType.Contract => Guid.TryParse(request.RelatedRecordId, out var contractId) &&
                await dbContext.Contracts.AnyAsync(contract => contract.Id == contractId && contract.TenantId == tenantContext.TenantId, cancellationToken),
            EvidenceRequestRelatedRecordType.Subcontractor => Guid.TryParse(request.RelatedRecordId, out var subcontractorId) &&
                await dbContext.Subcontractors.AnyAsync(subcontractor => subcontractor.Id == subcontractorId && subcontractor.TenantId == tenantContext.TenantId, cancellationToken),
            _ => false
        };
        if (!exists)
        {
            throw new EvidenceRequestValidationException("Related record was not found for the current tenant.");
        }
    }

    private static EvidenceRequestDto ToDto(EvidenceRequestEntity entity) =>
        new(
            entity.Id,
            entity.TenantId,
            entity.RequesterUserId,
            entity.AssigneeUserId,
            entity.AssigneeSubcontractorId,
            entity.DueDate,
            Enum.Parse<EvidenceRequestStatus>(entity.Status),
            entity.Instructions,
            Enum.Parse<EvidenceRequestRelatedRecordType>(entity.RelatedRecordType),
            entity.RelatedRecordId,
            entity.CreatedAt);
}

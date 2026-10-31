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
    public async Task<IReadOnlyList<EvidenceRequestDashboardItemDto>> ListAsync(
        EvidenceRequestDashboardQuery query,
        CancellationToken cancellationToken = default)
    {
        var items = dbContext.EvidenceRequests
            .AsNoTracking()
            .Where(request => request.TenantId == tenantContext.TenantId);
        if (query.Status is not null)
        {
            var status = query.Status.Value.ToString();
            items = items.Where(request => request.Status == status);
        }

        if (query.DueFrom is not null)
        {
            items = items.Where(request => request.DueDate >= query.DueFrom);
        }

        if (query.DueTo is not null)
        {
            items = items.Where(request => request.DueDate <= query.DueTo);
        }

        if (query.AssigneeUserId is not null)
        {
            items = items.Where(request => request.AssigneeUserId == query.AssigneeUserId);
        }

        if (query.RelatedRecordType is not null)
        {
            var relatedType = query.RelatedRecordType.Value.ToString();
            items = items.Where(request => request.RelatedRecordType == relatedType);
        }

        if (query.Priority is not null)
        {
            var priority = query.Priority.Value.ToString();
            items = items.Where(request => request.Priority == priority);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var requests = await items.OrderBy(request => request.DueDate).ThenBy(request => request.CreatedAt).ToArrayAsync(cancellationToken);
        return requests.Select(request => ToDashboardDto(request, today)).ToArray();
    }

    public async Task<int> SendBulkRemindersAsync(
        EvidenceRequestReminderRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var requests = await dbContext.EvidenceRequests
            .Where(candidate =>
                candidate.TenantId == tenantContext.TenantId &&
                request.EvidenceRequestIds.Contains(candidate.Id) &&
                candidate.AssigneeUserId != null)
            .ToArrayAsync(cancellationToken);
        foreach (var evidenceRequest in requests)
        {
            dbContext.NotificationDeliveries.Add(new NotificationDeliveryEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantContext.TenantId,
                UserId = evidenceRequest.AssigneeUserId!.Value,
                SourceTaskId = evidenceRequest.Id,
                SourceType = "EvidenceRequest",
                LinkUrl = $"/evidence-requests/{evidenceRequest.Id}",
                Category = "evidence_request_reminder",
                Status = "Delivered",
                Placeholder = $"Reminder: evidence request due {evidenceRequest.DueDate:O}.",
                AttemptedAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedByUserId = actorUserId
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return requests.Length;
    }

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
            Priority = request.Priority.ToString(),
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

    public async Task<EvidenceRequestDto?> SubmitAsync(
        Guid requestId,
        SubmitEvidenceRequestRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.EvidenceRequests.SingleOrDefaultAsync(
            candidate => candidate.Id == requestId && candidate.TenantId == tenantContext.TenantId,
            cancellationToken);
        if (entity is null)
        {
            return null;
        }

        if (entity.Status != EvidenceRequestStatus.Open.ToString() || entity.AssigneeUserId != actorUserId)
        {
            throw new EvidenceRequestValidationException("Only the assigned user can submit evidence to an open request.");
        }

        var evidenceExists = await dbContext.EvidenceItems.AnyAsync(
            evidence => evidence.Id == request.EvidenceItemId && evidence.TenantId == tenantContext.TenantId,
            cancellationToken);
        if (!evidenceExists)
        {
            throw new EvidenceRequestValidationException("Submitted evidence was not found for the current tenant.");
        }

        entity.SubmittedEvidenceItemId = request.EvidenceItemId;
        entity.SubmissionComment = request.Comment;
        entity.Status = EvidenceRequestStatus.Submitted.ToString();
        entity.SubmittedAt = DateTimeOffset.UtcNow;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedByUserId = actorUserId;
        dbContext.NotificationDeliveries.Add(new NotificationDeliveryEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantContext.TenantId,
            UserId = entity.RequesterUserId,
            SourceTaskId = entity.Id,
            SourceType = "EvidenceRequest",
            LinkUrl = $"/evidence-requests/{entity.Id}",
            Category = "evidence_request_submitted",
            Status = "Delivered",
            Placeholder = "Evidence request was submitted for review.",
            AttemptedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = actorUserId
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<EvidenceRequestDto?> ReviewAsync(
        Guid requestId,
        ReviewEvidenceRequestRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.EvidenceRequests.SingleOrDefaultAsync(
            candidate => candidate.Id == requestId && candidate.TenantId == tenantContext.TenantId,
            cancellationToken);
        if (entity is null)
        {
            return null;
        }

        if (entity.Status != EvidenceRequestStatus.Submitted.ToString() || entity.SubmittedEvidenceItemId is null)
        {
            throw new EvidenceRequestValidationException("Only submitted evidence requests can be reviewed.");
        }

        entity.ReviewComment = request.Comment;
        entity.ReviewedAt = DateTimeOffset.UtcNow;
        entity.Status = request.Decision == EvidenceRequestReviewDecision.Accept
            ? EvidenceRequestStatus.Accepted.ToString()
            : EvidenceRequestStatus.Returned.ToString();
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedByUserId = actorUserId;

        if (request.Decision == EvidenceRequestReviewDecision.Accept)
        {
            LinkAcceptedEvidence(entity);
        }
        else if (entity.AssigneeUserId is not null)
        {
            dbContext.NotificationDeliveries.Add(new NotificationDeliveryEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantContext.TenantId,
                UserId = entity.AssigneeUserId.Value,
                SourceTaskId = entity.Id,
                SourceType = "EvidenceRequest",
                LinkUrl = $"/evidence-requests/{entity.Id}",
                Category = "evidence_request_returned",
                Status = "Delivered",
                Placeholder = $"Evidence request was returned: {request.Comment}",
                AttemptedAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedByUserId = actorUserId
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
            Enum.Parse<EvidenceRequestPriority>(entity.Priority),
            Enum.Parse<EvidenceRequestRelatedRecordType>(entity.RelatedRecordType),
            entity.RelatedRecordId,
            entity.SubmittedEvidenceItemId,
            entity.SubmissionComment,
            entity.ReviewComment,
            entity.CreatedAt);

    private static EvidenceRequestDashboardItemDto ToDashboardDto(EvidenceRequestEntity entity, DateOnly today)
    {
        var status = Enum.Parse<EvidenceRequestStatus>(entity.Status);
        return new EvidenceRequestDashboardItemDto(
            entity.Id,
            entity.TenantId,
            entity.RequesterUserId,
            entity.AssigneeUserId,
            entity.AssigneeSubcontractorId,
            entity.DueDate,
            status,
            Enum.Parse<EvidenceRequestPriority>(entity.Priority),
            entity.Instructions,
            Enum.Parse<EvidenceRequestRelatedRecordType>(entity.RelatedRecordType),
            entity.RelatedRecordId,
            entity.DueDate < today && status is EvidenceRequestStatus.Open or EvidenceRequestStatus.Submitted or EvidenceRequestStatus.Returned,
            entity.CreatedAt);
    }

    private void LinkAcceptedEvidence(EvidenceRequestEntity entity)
    {
        if (entity.SubmittedEvidenceItemId is null)
        {
            return;
        }

        if (entity.RelatedRecordType == EvidenceRequestRelatedRecordType.Obligation.ToString())
        {
            dbContext.Set<EvidenceObligationEntity>().Add(new EvidenceObligationEntity
            {
                EvidenceItemId = entity.SubmittedEvidenceItemId.Value,
                ObligationId = entity.RelatedRecordId
            });
        }
        else if (entity.RelatedRecordType == EvidenceRequestRelatedRecordType.Control.ToString())
        {
            dbContext.Set<EvidenceControlEntity>().Add(new EvidenceControlEntity
            {
                EvidenceItemId = entity.SubmittedEvidenceItemId.Value,
                ControlId = entity.RelatedRecordId
            });
        }
        else if (entity.RelatedRecordType == EvidenceRequestRelatedRecordType.Contract.ToString() &&
            Guid.TryParse(entity.RelatedRecordId, out var contractId))
        {
            dbContext.Set<EvidenceContractEntity>().Add(new EvidenceContractEntity
            {
                EvidenceItemId = entity.SubmittedEvidenceItemId.Value,
                ContractId = contractId
            });
        }
        else if (entity.RelatedRecordType == EvidenceRequestRelatedRecordType.Subcontractor.ToString() &&
            Guid.TryParse(entity.RelatedRecordId, out var subcontractorId))
        {
            dbContext.Set<SubcontractorEvidenceEntity>().Add(new SubcontractorEvidenceEntity
            {
                EvidenceItemId = entity.SubmittedEvidenceItemId.Value,
                SubcontractorId = subcontractorId
            });
        }
    }
}

using Gccs.Application.Audit;
using Gccs.Domain.Audit;

namespace Gccs.Application.Evidence;

public sealed class EvidenceRequestService(
    IEvidenceRequestRepository repository,
    IAuditEventWriter auditEventWriter)
{
    public async Task<EvidenceRequestDto> CreateAsync(
        CreateEvidenceRequestRequest request,
        Guid requesterUserId,
        CancellationToken cancellationToken = default)
    {
        Validate(request);
        var created = await repository.CreateAsync(request, requesterUserId, cancellationToken);
        await auditEventWriter.WriteAsync(
            created.TenantId,
            requesterUserId,
            AuditAction.Created,
            "EvidenceRequest",
            created.Id.ToString(),
            "Evidence request was created.",
            new Dictionary<string, string>
            {
                ["relatedRecordType"] = created.RelatedRecordType.ToString(),
                ["relatedRecordId"] = created.RelatedRecordId,
                ["status"] = created.Status.ToString(),
                ["assigneeUserId"] = created.AssigneeUserId?.ToString() ?? string.Empty,
                ["assigneeSubcontractorId"] = created.AssigneeSubcontractorId?.ToString() ?? string.Empty
            },
            cancellationToken);
        return created;
    }

    private static void Validate(CreateEvidenceRequestRequest request)
    {
        if (request.AssigneeUserId is null && request.AssigneeSubcontractorId is null)
        {
            throw new EvidenceRequestValidationException("Evidence request requires a user or subcontractor assignee.");
        }

        if (request.AssigneeUserId is not null && request.AssigneeSubcontractorId is not null)
        {
            throw new EvidenceRequestValidationException("Evidence request can have only one assignee type.");
        }

        if (request.DueDate < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new EvidenceRequestValidationException("Evidence request due date cannot be in the past.");
        }

        if (string.IsNullOrWhiteSpace(request.Instructions))
        {
            throw new EvidenceRequestValidationException("Evidence request instructions are required.");
        }
    }
}

public interface IEvidenceRequestRepository
{
    Task<EvidenceRequestDto> CreateAsync(CreateEvidenceRequestRequest request, Guid requesterUserId, CancellationToken cancellationToken = default);
}

public enum EvidenceRequestRelatedRecordType
{
    Obligation,
    Control,
    Contract,
    Subcontractor
}

public enum EvidenceRequestStatus
{
    Open,
    Submitted,
    Satisfied,
    Cancelled
}

public sealed record CreateEvidenceRequestRequest(
    EvidenceRequestRelatedRecordType RelatedRecordType,
    string RelatedRecordId,
    Guid? AssigneeUserId,
    Guid? AssigneeSubcontractorId,
    DateOnly DueDate,
    string Instructions);

public sealed record EvidenceRequestDto(
    Guid Id,
    Guid TenantId,
    Guid RequesterUserId,
    Guid? AssigneeUserId,
    Guid? AssigneeSubcontractorId,
    DateOnly DueDate,
    EvidenceRequestStatus Status,
    string Instructions,
    EvidenceRequestRelatedRecordType RelatedRecordType,
    string RelatedRecordId,
    DateTimeOffset CreatedAt);

public sealed class EvidenceRequestValidationException(string message) : InvalidOperationException(message);

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

    public async Task<EvidenceRequestDto?> SubmitAsync(
        Guid requestId,
        SubmitEvidenceRequestRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (request.ContainsPotentialCui)
        {
            throw new EvidenceRequestValidationException("Potential CUI cannot be submitted in a No-CUI evidence request workflow.");
        }

        var updated = await repository.SubmitAsync(requestId, request, actorUserId, cancellationToken);
        if (updated is not null)
        {
            await WriteStatusAuditAsync(updated, actorUserId, AuditAction.Updated, "Evidence request evidence was submitted.", cancellationToken);
        }

        return updated;
    }

    public async Task<EvidenceRequestDto?> ReviewAsync(
        Guid requestId,
        ReviewEvidenceRequestRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var updated = await repository.ReviewAsync(requestId, request, actorUserId, cancellationToken);
        if (updated is not null)
        {
            await WriteStatusAuditAsync(
                updated,
                actorUserId,
                request.Decision == EvidenceRequestReviewDecision.Accept ? AuditAction.Approved : AuditAction.Rejected,
                "Evidence request review decision was recorded.",
                cancellationToken);
        }

        return updated;
    }

    private Task WriteStatusAuditAsync(
        EvidenceRequestDto request,
        Guid actorUserId,
        AuditAction action,
        string summary,
        CancellationToken cancellationToken) =>
        auditEventWriter.WriteAsync(
            request.TenantId,
            actorUserId,
            action,
            "EvidenceRequest",
            request.Id.ToString(),
            summary,
            new Dictionary<string, string>
            {
                ["status"] = request.Status.ToString(),
                ["submittedEvidenceItemId"] = request.SubmittedEvidenceItemId?.ToString() ?? string.Empty,
                ["reviewComment"] = request.ReviewComment ?? string.Empty
            },
            cancellationToken);

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
    Task<EvidenceRequestDto?> SubmitAsync(Guid requestId, SubmitEvidenceRequestRequest request, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<EvidenceRequestDto?> ReviewAsync(Guid requestId, ReviewEvidenceRequestRequest request, Guid actorUserId, CancellationToken cancellationToken = default);
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
    Accepted,
    Returned,
    Overdue,
    Cancelled
}

public enum EvidenceRequestReviewDecision
{
    Accept,
    Return
}

public sealed record CreateEvidenceRequestRequest(
    EvidenceRequestRelatedRecordType RelatedRecordType,
    string RelatedRecordId,
    Guid? AssigneeUserId,
    Guid? AssigneeSubcontractorId,
    DateOnly DueDate,
    string Instructions);

public sealed record SubmitEvidenceRequestRequest(
    Guid EvidenceItemId,
    bool ContainsPotentialCui,
    string? Comment);

public sealed record ReviewEvidenceRequestRequest(
    EvidenceRequestReviewDecision Decision,
    string Comment);

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
    Guid? SubmittedEvidenceItemId,
    string? SubmissionComment,
    string? ReviewComment,
    DateTimeOffset CreatedAt);

public sealed class EvidenceRequestValidationException(string message) : InvalidOperationException(message);

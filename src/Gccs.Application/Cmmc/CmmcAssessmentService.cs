using Gccs.Application.Audit;
using Gccs.Domain.Audit;
using Gccs.Domain.Cmmc;

namespace Gccs.Application.Cmmc;

public sealed class CmmcAssessmentService(
    ICmmcAssessmentRepository repository,
    IAuditEventWriter auditEventWriter)
{
    public Task<IReadOnlyList<CmmcAssessmentDto>> ListCurrentTenantAsync(CancellationToken cancellationToken = default) =>
        repository.ListCurrentTenantAsync(cancellationToken);

    public Task<CmmcAssessmentDto?> FindCurrentTenantAsync(Guid assessmentId, CancellationToken cancellationToken = default) =>
        repository.FindCurrentTenantAsync(assessmentId, cancellationToken);

    public async Task<CmmcAssessmentDto> CreateAsync(
        UpsertCmmcAssessmentRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(request);
        Validate(normalized);
        var created = await repository.CreateCurrentTenantAsync(normalized, actorUserId, cancellationToken);
        await WriteAssessmentAuditAsync(created, actorUserId, AuditAction.Created, "CMMC readiness assessment was created.", cancellationToken);
        return created;
    }

    public async Task<CmmcAssessmentDto?> UpdateAsync(
        Guid assessmentId,
        UpsertCmmcAssessmentRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(request);
        Validate(normalized);
        var updated = await repository.UpdateCurrentTenantAsync(assessmentId, normalized, actorUserId, cancellationToken);
        if (updated is null)
        {
            return null;
        }

        await WriteAssessmentAuditAsync(updated, actorUserId, AuditAction.Updated, "CMMC readiness assessment was updated.", cancellationToken);
        return updated;
    }

    public Task<IReadOnlyList<CmmcControlStatusDto>?> ListControlStatusesAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default) =>
        repository.ListControlStatusesAsync(assessmentId, cancellationToken);

    public async Task<CmmcControlStatusDto?> UpsertControlStatusAsync(
        Guid assessmentId,
        string controlId,
        UpsertCmmcControlStatusRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(request);
        Validate(normalized);
        var assessment = await repository.FindCurrentTenantAsync(assessmentId, cancellationToken);
        if (assessment is null)
        {
            return null;
        }

        var controlStatus = await repository.UpsertControlStatusAsync(
            assessmentId,
            controlId.Trim(),
            normalized,
            actorUserId,
            cancellationToken);
        if (controlStatus is null)
        {
            return null;
        }

        await auditEventWriter.WriteAsync(
            assessment.TenantId,
            actorUserId,
            AuditAction.Updated,
            "ControlAssessment",
            $"{assessmentId}:{controlStatus.ControlId}",
            "CMMC control assessment status was updated.",
            new Dictionary<string, string>
            {
                ["assessmentId"] = assessmentId.ToString(),
                ["controlId"] = controlStatus.ControlId,
                ["status"] = controlStatus.Status.ToString(),
                ["result"] = controlStatus.Result.ToString()
            },
            cancellationToken);

        return controlStatus;
    }

    private async Task WriteAssessmentAuditAsync(
        CmmcAssessmentDto assessment,
        Guid actorUserId,
        AuditAction action,
        string summary,
        CancellationToken cancellationToken)
    {
        await auditEventWriter.WriteAsync(
            assessment.TenantId,
            actorUserId,
            action,
            "CmmcAssessment",
            assessment.Id.ToString(),
            summary,
            new Dictionary<string, string>
            {
                ["level"] = assessment.Level.ToString(),
                ["status"] = assessment.Status.ToString(),
                ["ownerFunction"] = assessment.OwnerFunction,
                ["completionPercentage"] = assessment.ControlSummary.CompletionPercentage.ToString()
            },
            cancellationToken);
    }

    private static UpsertCmmcAssessmentRequest Normalize(UpsertCmmcAssessmentRequest request) =>
        request with
        {
            Name = request.Name.Trim(),
            Framework = request.Framework.Trim(),
            OwnerFunction = request.OwnerFunction.Trim(),
            ContractIds = request.ContractIds.Distinct().OrderBy(id => id).ToArray()
        };

    private static UpsertCmmcControlStatusRequest Normalize(UpsertCmmcControlStatusRequest request) =>
        request with
        {
            EvidenceItemIds = request.EvidenceItemIds.Distinct().OrderBy(id => id).ToArray(),
            Notes = request.Notes?.Trim() ?? string.Empty
        };

    private static void Validate(UpsertCmmcAssessmentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new CmmcAssessmentValidationException("Assessment name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Framework))
        {
            throw new CmmcAssessmentValidationException("CMMC framework is required.");
        }

        if (string.IsNullOrWhiteSpace(request.OwnerFunction))
        {
            throw new CmmcAssessmentValidationException("Responsible owner is required.");
        }

        if (request.Level is not (CmmcLevel.Level1 or CmmcLevel.Level2))
        {
            throw new CmmcAssessmentValidationException("The MVP supports CMMC Level 1 and Level 2 readiness assessments.");
        }
    }

    private static void Validate(UpsertCmmcControlStatusRequest request)
    {
        if (request.Notes?.Length > 1000)
        {
            throw new CmmcAssessmentValidationException("Control assessment notes must be 1000 characters or fewer.");
        }
    }
}

public sealed class CmmcAssessmentValidationException(string message) : InvalidOperationException(message);

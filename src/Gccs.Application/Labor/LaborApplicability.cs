using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Domain.Audit;

namespace Gccs.Application.Labor;

public sealed class LaborApplicabilityService(
    ILaborApplicabilityRepository repository,
    IAuditEventWriter auditEventWriter,
    IApplicationTransaction transaction)
{
    public Task<IReadOnlyList<LaborApplicabilityDto>?> ListForContractAsync(Guid contractId, CancellationToken cancellationToken = default) =>
        repository.ListForContractAsync(contractId, cancellationToken);

    public Task<LaborApplicabilityDto?> FindAsync(Guid contractId, Guid applicabilityId, CancellationToken cancellationToken = default) =>
        repository.FindAsync(contractId, applicabilityId, cancellationToken);

    public async Task<LaborApplicabilityDto?> RecordAsync(LaborApplicabilityRequest request, Guid tenantId, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(request);
        LaborApplicabilityRules.Validate(normalized, requireActivationSource: false);
        return await transaction.ExecuteAsync(async token =>
        {
            var saved = await repository.CreateAsync(normalized, tenantId, actorUserId, token);
            if (saved is null) return null;
            await WriteAuditAsync(saved, actorUserId, AuditAction.Created, "Labor applicability was recorded.", token);
            return saved;
        }, cancellationToken);
    }

    public async Task<LaborApplicabilityDto?> UpdateAsync(Guid contractId, Guid applicabilityId, LaborApplicabilityRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(request with { ContractId = contractId });
        LaborApplicabilityRules.Validate(normalized, requireActivationSource: false);
        return await transaction.ExecuteAsync(async token =>
        {
            var updated = await repository.UpdateAsync(contractId, applicabilityId, normalized, actorUserId, token);
            if (updated is null) return null;
            await WriteAuditAsync(updated, actorUserId, AuditAction.Updated, "Labor applicability was updated.", token);
            return updated;
        }, cancellationToken);
    }

    public async Task<LaborApplicabilityDto?> ActivateAsync(Guid contractId, Guid applicabilityId, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var existing = await repository.FindAsync(contractId, applicabilityId, cancellationToken);
        if (existing is null) return null;
        LaborApplicabilityRules.Validate(existing.ToRequest(), requireActivationSource: true);
        return await transaction.ExecuteAsync(async token =>
        {
            var activated = await repository.UpdateStatusAsync(contractId, applicabilityId, LaborApplicabilityStatus.Active, actorUserId, token);
            if (activated is null) return null;
            await WriteAuditAsync(activated, actorUserId, AuditAction.Updated, "Labor applicability was activated.", token);
            return activated;
        }, cancellationToken);
    }

    public async Task<LaborApplicabilityDto?> DeactivateAsync(Guid contractId, Guid applicabilityId, Guid actorUserId, CancellationToken cancellationToken = default) =>
        await transaction.ExecuteAsync(async token =>
        {
            var deactivated = await repository.UpdateStatusAsync(contractId, applicabilityId, LaborApplicabilityStatus.Inactive, actorUserId, token);
            if (deactivated is null) return null;
            await WriteAuditAsync(deactivated, actorUserId, AuditAction.Updated, "Labor applicability was deactivated.", token);
            return deactivated;
        }, cancellationToken);

    private static LaborApplicabilityRequest Normalize(LaborApplicabilityRequest request) => request with
    {
        OtherFarPart22Obligations = NullIfBlank(request.OtherFarPart22Obligations),
        PlaceOfPerformance = (request.PlaceOfPerformance ?? string.Empty).Trim(),
        WageDeterminationReference = NullIfBlank(request.WageDeterminationReference),
        SourceClause = NullIfBlank(request.SourceClause),
        Rationale = NullIfBlank(request.Rationale),
        OwnerFunction = string.IsNullOrWhiteSpace(request.OwnerFunction) ? "Contracts/HR" : request.OwnerFunction.Trim(),
        ReviewNotes = NullIfBlank(request.ReviewNotes)
    };

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private Task WriteAuditAsync(LaborApplicabilityDto item, Guid actorUserId, AuditAction action, string summary, CancellationToken cancellationToken) =>
        auditEventWriter.WriteAsync(item.TenantId, actorUserId, action, "LaborApplicability", item.Id.ToString(), summary,
            new Dictionary<string, string>
            {
                ["contractId"] = item.ContractId.ToString(),
                ["sourceContractClauseId"] = item.SourceContractClauseId?.ToString() ?? string.Empty,
                ["taskId"] = item.TaskId?.ToString() ?? string.Empty,
                ["wageDeterminationEvidenceItemId"] = item.WageDeterminationEvidenceItemId?.ToString() ?? string.Empty,
                ["status"] = item.Status.ToString(),
                ["reviewStatus"] = item.ReviewStatus.ToString(),
                ["sourceBackedBy"] = item.SourceContractClauseId.HasValue ? "contract-clause" : item.SourceClause is not null ? "citation" : item.Rationale is not null ? "rationale" : "none"
            }, cancellationToken);
}

public interface ILaborApplicabilityRepository
{
    Task<IReadOnlyList<LaborApplicabilityDto>?> ListForContractAsync(Guid contractId, CancellationToken cancellationToken = default);
    Task<LaborApplicabilityDto?> CreateAsync(LaborApplicabilityRequest request, Guid tenantId, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<LaborApplicabilityDto?> UpdateAsync(Guid contractId, Guid applicabilityId, LaborApplicabilityRequest request, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<LaborApplicabilityDto?> FindAsync(Guid contractId, Guid applicabilityId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LaborApplicabilityDto>> ListAsync(Guid tenantId, Guid? contractId = null, CancellationToken cancellationToken = default);
    Task<LaborApplicabilityDto?> UpdateStatusAsync(Guid contractId, Guid applicabilityId, LaborApplicabilityStatus status, Guid actorUserId, CancellationToken cancellationToken = default);
}

public static class LaborApplicabilityRules
{
    public static void Validate(LaborApplicabilityRequest request, bool requireActivationSource)
    {
        var errors = new Dictionary<string, string[]>();
        if (request.ContractId == Guid.Empty) errors["contractId"] = ["Contract is required."];
        Required(errors, "placeOfPerformance", request.PlaceOfPerformance, 240);
        Required(errors, "ownerFunction", request.OwnerFunction ?? string.Empty, 120);
        if (request.ContractPeriodStart == default) errors["contractPeriodStart"] = ["Contract period start is required."];
        if (request.ContractPeriodEnd == default) errors["contractPeriodEnd"] = ["Contract period end is required."];
        if (request.ContractPeriodEnd < request.ContractPeriodStart) errors["contractPeriodEnd"] = ["Contract period end cannot be before start."];
        if (request.OtherFarPart22Obligations?.Length > 2_000) errors["otherFarPart22Obligations"] = ["Other FAR Part 22 obligations cannot exceed 2000 characters."];
        if (request.WageDeterminationReference?.Length > 240) errors["wageDeterminationReference"] = ["Wage determination reference cannot exceed 240 characters."];
        if (request.SourceClause?.Length > 240) errors["sourceClause"] = ["Source clause cannot exceed 240 characters."];
        if (request.Rationale?.Length > 2_000) errors["rationale"] = ["Documented rationale cannot exceed 2000 characters."];
        if (request.ReviewNotes?.Length > 2_000) errors["reviewNotes"] = ["Review notes cannot exceed 2000 characters."];
        if (!Enum.IsDefined(request.ReviewStatus)) errors["reviewStatus"] = ["Review status is not supported."];
        if (requireActivationSource && request.SourceContractClauseId is null && request.SourceClause is null && request.Rationale is null)
            errors["source"] = ["Labor obligation activation requires an attached source clause, source citation, or documented rationale."];
        if (requireActivationSource && !request.ScaApplicable && !request.DbaApplicable && request.OtherFarPart22Obligations is null)
            errors["applicability"] = ["Select SCA, DBA, or document another FAR Part 22 obligation before activation."];
        if (errors.Count > 0) throw new LaborApplicabilityValidationException(errors);
    }

    private static void Required(Dictionary<string, string[]> errors, string name, string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) errors[name] = [$"{name} is required."];
        else if (value.Length > maxLength) errors[name] = [$"{name} cannot exceed {maxLength} characters."];
    }
}

public sealed record LaborApplicabilityRequest(
    Guid ContractId,
    bool ScaApplicable,
    bool DbaApplicable,
    string? OtherFarPart22Obligations,
    string PlaceOfPerformance,
    DateOnly ContractPeriodStart,
    DateOnly ContractPeriodEnd,
    string? WageDeterminationReference,
    Guid? WageDeterminationEvidenceItemId,
    Guid? SourceContractClauseId,
    string? SourceClause,
    string? Rationale,
    string? OwnerFunction,
    LaborApplicabilityReviewStatus ReviewStatus = LaborApplicabilityReviewStatus.Draft,
    string? ReviewNotes = null);

public sealed record LaborApplicabilityDto(
    Guid Id,
    Guid TenantId,
    Guid ContractId,
    Guid? TaskId,
    bool ScaApplicable,
    bool DbaApplicable,
    string? OtherFarPart22Obligations,
    string PlaceOfPerformance,
    DateOnly ContractPeriodStart,
    DateOnly ContractPeriodEnd,
    string? WageDeterminationReference,
    Guid? WageDeterminationEvidenceItemId,
    Guid? SourceContractClauseId,
    string? SourceClause,
    string? Rationale,
    string OwnerFunction,
    LaborApplicabilityStatus Status,
    LaborApplicabilityReviewStatus ReviewStatus,
    string? ReviewNotes,
    Guid? ReviewedByUserId,
    DateTimeOffset? ReviewedAt,
    LaborReviewTaskDto? ReviewTask,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt)
{
    public string LaborStandard => string.Join(", ", new[]
    {
        ScaApplicable ? "SCA" : null,
        DbaApplicable ? "DBA" : null,
        OtherFarPart22Obligations is not null ? "Other FAR Part 22" : null
    }.Where(value => value is not null));

    public LaborApplicabilityRequest ToRequest() => new(ContractId, ScaApplicable, DbaApplicable, OtherFarPart22Obligations,
        PlaceOfPerformance, ContractPeriodStart, ContractPeriodEnd, WageDeterminationReference, WageDeterminationEvidenceItemId,
        SourceContractClauseId, SourceClause, Rationale, OwnerFunction, ReviewStatus, ReviewNotes);
}

public sealed record LaborReviewTaskDto(Guid Id, Guid TenantId, Guid ContractId, string Title, string Description, string Status, DateOnly? DueAt);
public sealed record UpdateLaborApplicabilityStatusRequest(LaborApplicabilityStatus Status);

public enum LaborApplicabilityStatus { Draft, Active, Inactive }
public enum LaborApplicabilityReviewStatus { Draft, PendingReview, Reviewed, Rejected }

public sealed class LaborApplicabilityValidationException : InvalidOperationException
{
    public LaborApplicabilityValidationException(string message) : this(new Dictionary<string, string[]> { ["laborApplicability"] = [message] }) { }
    public LaborApplicabilityValidationException(IReadOnlyDictionary<string, string[]> errors) : base(errors.Values.SelectMany(value => value).First()) => Errors = errors;
    public IReadOnlyDictionary<string, string[]> Errors { get; }
}

public sealed class LaborApplicabilityConflictException : InvalidOperationException
{
    public LaborApplicabilityConflictException() : base("Labor applicability changed while the status transition was being saved. Reload and retry.") { }
}

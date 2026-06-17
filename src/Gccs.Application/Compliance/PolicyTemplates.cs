using Gccs.Application.Audit;
using Gccs.Domain.Audit;

namespace Gccs.Application.Compliance;

public sealed class PolicyTemplateService(
    IPolicyTemplateRepository repository,
    IAuditEventWriter auditEventWriter)
{
    public Task<IReadOnlyList<PolicyTemplateDto>> ListAsync(bool includeReviewStates, CancellationToken cancellationToken = default) =>
        repository.ListAsync(includeReviewStates, cancellationToken);

    public Task<IReadOnlyList<PolicyTemplateVersionDto>> ListVersionsAsync(Guid templateId, CancellationToken cancellationToken = default) =>
        repository.ListVersionsAsync(templateId, cancellationToken);

    public Task<GeneratedPolicyDto?> FindGeneratedPolicyAsync(Guid policyId, CancellationToken cancellationToken = default) =>
        repository.FindGeneratedPolicyAsync(policyId, cancellationToken);

    public async Task<PolicyTemplateDto> CreateAsync(
        UpsertPolicyTemplateRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(request);
        ValidateForSave(normalized);
        if (normalized.Status == PolicyTemplateStatus.Approved)
        {
            ValidateForApproval(normalized);
        }

        var created = await repository.CreateAsync(normalized, actorUserId, cancellationToken);
        await WriteAuditAsync(created, actorUserId, AuditAction.Created, "Policy template was created.", null, cancellationToken);
        return created;
    }

    public async Task<PolicyTemplateDto?> ChangeLifecycleAsync(
        Guid templateId,
        ChangePolicyTemplateLifecycleRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var current = await repository.FindAsync(templateId, includeReviewStates: true, cancellationToken);
        if (current is null)
        {
            return null;
        }

        if (request.Status == PolicyTemplateStatus.Approved)
        {
            ValidateForApproval(current);
        }

        var updated = await repository.ChangeLifecycleAsync(
            templateId,
            request.Status,
            request.ReviewerUserId,
            request.ReviewedAt,
            actorUserId,
            cancellationToken);
        if (updated is not null)
        {
            await WriteAuditAsync(updated, actorUserId, ToAuditAction(request.Status), "Policy template lifecycle changed.", current.Status.ToString(), cancellationToken);
        }

        return updated;
    }

    public async Task<GeneratedPolicyDto?> GenerateDraftPolicyAsync(
        Guid templateId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var generated = await repository.GenerateDraftPolicyAsync(templateId, actorUserId, cancellationToken);
        if (generated is not null)
        {
            await auditEventWriter.WriteAsync(
                generated.TenantId,
                actorUserId,
                AuditAction.Created,
                "GeneratedPolicy",
                generated.Id.ToString(),
                "Draft policy was generated from an approved template.",
                new Dictionary<string, string>
                {
                    ["templateId"] = generated.SourceTemplateId.ToString(),
                    ["sourceTemplateVersion"] = generated.SourceTemplateVersion,
                    ["status"] = generated.Status.ToString()
                },
                cancellationToken);
        }

        return generated;
    }

    public Task<GeneratedPolicyDto?> UpdateGeneratedPolicyAsync(
        Guid policyId,
        UpdateGeneratedPolicyRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var normalized = request with
        {
            Title = request.Title.Trim(),
            Body = request.Body.Trim()
        };
        return repository.UpdateGeneratedPolicyAsync(policyId, normalized, actorUserId, cancellationToken);
    }

    private async Task WriteAuditAsync(
        PolicyTemplateDto template,
        Guid actorUserId,
        AuditAction action,
        string summary,
        string? previousStatus,
        CancellationToken cancellationToken)
    {
        var metadata = new Dictionary<string, string>
        {
            ["title"] = template.Title,
            ["category"] = template.Category,
            ["version"] = template.Version,
            ["status"] = template.Status.ToString(),
            ["ownerFunction"] = template.OwnerFunction,
            ["lastReviewedAt"] = template.LastReviewedAt?.ToString("O") ?? string.Empty
        };

        if (previousStatus is not null)
        {
            metadata["previousStatus"] = previousStatus;
        }

        await auditEventWriter.WriteAsync(
            template.TenantId,
            actorUserId,
            action,
            "PolicyTemplate",
            template.Id.ToString(),
            summary,
            metadata,
            cancellationToken);
    }

    private static UpsertPolicyTemplateRequest Normalize(UpsertPolicyTemplateRequest request) =>
        request with
        {
            Title = request.Title.Trim(),
            Category = request.Category.Trim(),
            Body = request.Body.Trim(),
            Version = request.Version.Trim(),
            OwnerFunction = request.OwnerFunction.Trim(),
            Placeholders = request.Placeholders.Select(value => value.Trim()).Where(value => value.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value => value).ToArray(),
            SourceReferences = request.SourceReferences
                .Select(source => source with
                {
                    SourceName = source.SourceName.Trim(),
                    SourceUrl = source.SourceUrl.Trim()
                })
                .ToArray()
        };

    private static void ValidateForSave(UpsertPolicyTemplateRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        AddIf(errors, string.IsNullOrWhiteSpace(request.Title), "title", "Template title is required.");
        AddIf(errors, string.IsNullOrWhiteSpace(request.Category), "category", "Template category is required.");
        AddIf(errors, string.IsNullOrWhiteSpace(request.Body), "body", "Template body is required.");
        AddIf(errors, string.IsNullOrWhiteSpace(request.Version), "version", "Template version is required.");
        AddIf(errors, string.IsNullOrWhiteSpace(request.OwnerFunction), "ownerFunction", "Template owner is required.");

        if (errors.Count > 0)
        {
            throw new PolicyTemplateValidationException(errors);
        }
    }

    private static void ValidateForApproval(UpsertPolicyTemplateRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        AddIf(errors, request.SourceReferences.Count == 0, "sourceReferences", "Approved templates require at least one source reference.");
        AddIf(errors, request.LastReviewedAt is null, "lastReviewedAt", "Approved templates require a last reviewed date.");
        AddIf(errors, request.ReviewerUserId is null, "reviewerUserId", "Approved templates require a reviewer.");
        AddIf(errors, string.IsNullOrWhiteSpace(request.OwnerFunction), "ownerFunction", "Approved templates require an owner.");
        AddIf(errors, request.SourceReferences.Any(source => string.IsNullOrWhiteSpace(source.SourceName) || string.IsNullOrWhiteSpace(source.SourceUrl)),
            "sourceReferences",
            "Approved template source references require source names and URLs.");

        if (errors.Count > 0)
        {
            throw new PolicyTemplateValidationException(errors);
        }
    }

    private static void ValidateForApproval(PolicyTemplateDto template)
    {
        var errors = new Dictionary<string, string[]>();
        AddIf(errors, template.SourceReferences.Count == 0, "sourceReferences", "Approved templates require at least one source reference.");
        AddIf(errors, template.LastReviewedAt is null, "lastReviewedAt", "Approved templates require a last reviewed date.");
        AddIf(errors, template.ReviewerUserId is null, "reviewerUserId", "Approved templates require a reviewer.");
        AddIf(errors, string.IsNullOrWhiteSpace(template.OwnerFunction), "ownerFunction", "Approved templates require an owner.");

        if (errors.Count > 0)
        {
            throw new PolicyTemplateValidationException(errors);
        }
    }

    private static void AddIf(Dictionary<string, string[]> errors, bool condition, string key, string message)
    {
        if (condition)
        {
            errors[key] = [message];
        }
    }

    private static AuditAction ToAuditAction(PolicyTemplateStatus status) =>
        status switch
        {
            PolicyTemplateStatus.Deprecated => AuditAction.Archived,
            PolicyTemplateStatus.Superseded => AuditAction.Updated,
            PolicyTemplateStatus.Approved => AuditAction.Approved,
            _ => AuditAction.Updated
        };
}

public interface IPolicyTemplateRepository
{
    Task<IReadOnlyList<PolicyTemplateDto>> ListAsync(bool includeReviewStates, CancellationToken cancellationToken = default);
    Task<PolicyTemplateDto?> FindAsync(Guid templateId, bool includeReviewStates, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PolicyTemplateVersionDto>> ListVersionsAsync(Guid templateId, CancellationToken cancellationToken = default);
    Task<PolicyTemplateDto> CreateAsync(UpsertPolicyTemplateRequest request, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<PolicyTemplateDto?> ChangeLifecycleAsync(Guid templateId, PolicyTemplateStatus status, Guid? reviewerUserId, DateOnly? reviewedAt, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<GeneratedPolicyDto?> GenerateDraftPolicyAsync(Guid templateId, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<GeneratedPolicyDto?> FindGeneratedPolicyAsync(Guid policyId, CancellationToken cancellationToken = default);
    Task<GeneratedPolicyDto?> UpdateGeneratedPolicyAsync(Guid policyId, UpdateGeneratedPolicyRequest request, Guid actorUserId, CancellationToken cancellationToken = default);
}

public enum PolicyTemplateStatus
{
    Draft,
    UnderReview,
    Approved,
    Deprecated,
    Superseded
}

public enum GeneratedPolicyStatus
{
    Draft,
    Approved,
    Archived
}

public sealed record PolicyTemplateSourceReferenceDto(
    string SourceName,
    string SourceUrl,
    DateOnly LastReviewedAt);

public sealed record PolicyTemplateDto(
    Guid Id,
    Guid TenantId,
    string Title,
    string Category,
    string Body,
    IReadOnlyList<string> Placeholders,
    IReadOnlyList<PolicyTemplateSourceReferenceDto> SourceReferences,
    string Version,
    PolicyTemplateStatus Status,
    string OwnerFunction,
    DateOnly? LastReviewedAt,
    Guid? ReviewerUserId,
    bool RequiresExpertReview,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record PolicyTemplateVersionDto(
    Guid Id,
    Guid TemplateId,
    string Version,
    string BodyPreview,
    PolicyTemplateStatus Status,
    DateTimeOffset CreatedAt,
    Guid CreatedByUserId);

public sealed record UpsertPolicyTemplateRequest(
    string Title,
    string Category,
    string Body,
    IReadOnlyList<string> Placeholders,
    IReadOnlyList<PolicyTemplateSourceReferenceDto> SourceReferences,
    string Version,
    PolicyTemplateStatus Status,
    string OwnerFunction,
    DateOnly? LastReviewedAt,
    Guid? ReviewerUserId,
    bool RequiresExpertReview);

public sealed record ChangePolicyTemplateLifecycleRequest(
    PolicyTemplateStatus Status,
    Guid? ReviewerUserId,
    DateOnly? ReviewedAt);

public sealed record GeneratedPolicyDto(
    Guid Id,
    Guid TenantId,
    Guid SourceTemplateId,
    string SourceTemplateVersion,
    DateTimeOffset GeneratedAt,
    string Title,
    string Body,
    GeneratedPolicyStatus Status,
    IReadOnlyDictionary<string, string> PlaceholderValues,
    IReadOnlyList<string> MissingPlaceholders,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record UpdateGeneratedPolicyRequest(string Title, string Body);

public sealed class PolicyTemplateValidationException(IReadOnlyDictionary<string, string[]> errors) : InvalidOperationException("Policy template validation failed.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}

using Gccs.Application.Audit;
using Gccs.Application.Ai;
using Gccs.Application.Common;
using Gccs.Application.Tenancy;
using Gccs.Domain.Audit;
using Gccs.Domain.Common;

namespace Gccs.Application.Compliance;

public sealed class PolicyTemplateService(
    IPolicyTemplateRepository repository,
    IAuditEventWriter auditEventWriter,
    ContentClassificationPolicy classificationPolicy,
    IApplicationTransaction transaction,
    AiOutputReviewService? aiOutputReview = null)
{
    public Task<IReadOnlyList<PolicyTemplateDto>> ListAsync(bool includeReviewStates, CancellationToken cancellationToken = default) =>
        repository.ListAsync(includeReviewStates, cancellationToken);

    public Task<IReadOnlyList<PolicyTemplateVersionDto>> ListVersionsAsync(Guid templateId, CancellationToken cancellationToken = default) =>
        repository.ListVersionsAsync(templateId, cancellationToken);

    public Task<GeneratedPolicyDto?> FindGeneratedPolicyAsync(Guid policyId, CancellationToken cancellationToken = default) =>
        repository.FindGeneratedPolicyAsync(policyId, cancellationToken);

    public Task<IReadOnlyList<PolicyRevisionDto>> ListPolicyRevisionsAsync(Guid policyId, CancellationToken cancellationToken = default) =>
        repository.ListPolicyRevisionsAsync(policyId, cancellationToken);

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
        GenerateDraftPolicyRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateClassification(request.Classification);
        await classificationPolicy.EnsureAllowedAsync(request.Classification, TenantDataHandlingWorkflow.GeneratedPolicy,
            actorUserId, cancellationToken: cancellationToken);
        var classification = ToDto(request.Classification);
        return await transaction.ExecuteAsync(async token =>
        {
            var generated = await repository.GenerateDraftPolicyAsync(templateId, classification, actorUserId, token);
            if (generated is null) return null;
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
                    ["status"] = generated.Status.ToString(),
                    ["classification"] = generated.Classification.Classification.ToString()
                },
                token);
            if (request.AiOutputId is Guid aiOutputId)
                await RequiredAiReview().RequireDeliverableLinkAsync(aiOutputId, generated.TenantId,
                    AiDeliverableType.Policy, generated.Id, actorUserId, token);
            return generated;
        }, cancellationToken);
    }

    private AiOutputReviewService RequiredAiReview() => aiOutputReview ??
        throw new AiOutputReviewValidationException("aiOutputId", "AI output provenance processing is unavailable.");

    public async Task<GeneratedPolicyDto?> UpdateGeneratedPolicyAsync(
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
        ValidateClassification(normalized.Classification);
        await classificationPolicy.EnsureAllowedAsync(normalized.Classification, TenantDataHandlingWorkflow.GeneratedPolicy,
            actorUserId, "GeneratedPolicy", policyId.ToString(), cancellationToken);
        return await transaction.ExecuteAsync(async token =>
        {
            var updated = await repository.UpdateGeneratedPolicyAsync(policyId, normalized, ToDto(normalized.Classification), actorUserId, token);
            if (updated is not null)
                await WriteGeneratedPolicyAuditAsync(updated, actorUserId, AuditAction.Updated, "Generated policy draft was edited.", token);
            return updated;
        }, cancellationToken);
    }

    public async Task<GeneratedPolicyDto?> ReviewGeneratedPolicyAsync(
        Guid policyId,
        PolicyApprovalRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        return await transaction.ExecuteAsync(async token =>
        {
            var current = await repository.FindGeneratedPolicyAsync(policyId, token);
            if (current is null) return null;
            if (request.Decision == PolicyApprovalDecision.Approve)
            {
                if (current.MissingPlaceholders.Count > 0)
                    throw new PolicyTemplateValidationException(new Dictionary<string, string[]> { ["missingPlaceholders"] = ["Generated policy approval is blocked while placeholders remain unresolved."] });
                await classificationPolicy.EnsureUsableAsync(current.Classification, TenantDataHandlingWorkflow.GeneratedPolicy,
                    actorUserId, "GeneratedPolicy", policyId.ToString(), token);
            }
            var updated = await repository.ReviewGeneratedPolicyAsync(policyId, request, actorUserId, token);
            if (updated is null) return null;
            var action = request.Decision switch
            {
                PolicyApprovalDecision.Approve => AuditAction.Approved,
                PolicyApprovalDecision.Reject => AuditAction.Rejected,
                _ => AuditAction.Updated
            };
            await WriteGeneratedPolicyAuditAsync(updated, actorUserId, action,
                $"Generated policy was {request.Decision.ToString().ToLowerInvariant()}.", token);
            return updated;
        }, cancellationToken);
    }

    private Task WriteGeneratedPolicyAuditAsync(GeneratedPolicyDto policy, Guid actorUserId, AuditAction action, string summary, CancellationToken cancellationToken) =>
        auditEventWriter.WriteAsync(policy.TenantId, actorUserId, action, "GeneratedPolicy", policy.Id.ToString(), summary,
            new Dictionary<string, string>
            {
                ["status"] = policy.Status.ToString(), ["sourceTemplateId"] = policy.SourceTemplateId.ToString(),
                ["sourceTemplateVersion"] = policy.SourceTemplateVersion,
                ["evidenceItemId"] = policy.EvidenceItemId?.ToString() ?? string.Empty,
                ["classification"] = policy.Classification.Classification.ToString(),
                ["classificationRevision"] = policy.ClassificationRevision.ToString()
            }, cancellationToken);

    private static void ValidateClassification(ContentClassificationRequest? classification)
    {
        if (classification is null)
            throw new ContentClassificationValidationException("Explicit generated policy classification is required.");
        ContentClassificationPolicy.ValidateUserSelection(classification);
        ContentClassificationPolicy.EnsureProcessable(classification.Classification, TenantDataHandlingWorkflow.GeneratedPolicy.ToString());
    }

    private static ContentClassificationDto ToDto(ContentClassificationRequest classification) => new(
        classification.Classification, classification.Source, classification.Confidence, classification.ReviewedByUserId,
        classification.ReviewedAt, classification.Reason, classification.IsApprovedDemoContent);

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
    Task<GeneratedPolicyDto?> GenerateDraftPolicyAsync(Guid templateId, ContentClassificationDto classification, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<GeneratedPolicyDto?> FindGeneratedPolicyAsync(Guid policyId, CancellationToken cancellationToken = default);
    Task<GeneratedPolicyDto?> UpdateGeneratedPolicyAsync(Guid policyId, UpdateGeneratedPolicyRequest request, ContentClassificationDto classification, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<GeneratedPolicyDto?> ReviewGeneratedPolicyAsync(Guid policyId, PolicyApprovalRequest request, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PolicyRevisionDto>> ListPolicyRevisionsAsync(Guid policyId, CancellationToken cancellationToken = default);
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
    Rejected,
    RevisionRequested,
    Archived
}

public enum PolicyApprovalDecision
{
    Approve,
    Reject,
    Revise
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
    Guid? ApprovedByUserId,
    DateTimeOffset? ApprovedAt,
    DateOnly? ReviewDueAt,
    Guid? EvidenceItemId,
    IReadOnlyDictionary<string, string> PlaceholderValues,
    IReadOnlyList<string> MissingPlaceholders,
    ContentClassificationDto Classification,
    long ClassificationRevision,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record GenerateDraftPolicyRequest(ContentClassificationRequest Classification, Guid? AiOutputId = null);

public sealed record UpdateGeneratedPolicyRequest(string Title, string Body, ContentClassificationRequest Classification);

public sealed record PolicyApprovalRequest(
    PolicyApprovalDecision Decision,
    DateOnly? ReviewDueAt,
    IReadOnlyList<string> ObligationIds,
    IReadOnlyList<string> ControlIds,
    string? Reason = null);

public sealed record PolicyRevisionDto(
    Guid Id,
    Guid GeneratedPolicyId,
    string Title,
    string Body,
    GeneratedPolicyStatus Status,
    ContentClassificationDto Classification,
    long ClassificationRevision,
    DateTimeOffset PreservedAt,
    Guid PreservedByUserId);

public sealed class PolicyTemplateValidationException(IReadOnlyDictionary<string, string[]> errors) : InvalidOperationException("Policy template validation failed.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}

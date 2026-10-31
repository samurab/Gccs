using Gccs.Application.Tenancy;
namespace Gccs.Application.Common;

public sealed record ClassifiedWorkflowRequest(ContentClassificationRequest? Classification, Guid? AiOutputId = null);

public static class ClassifiedWorkflowValidation
{
    public static async Task<ContentClassificationRequest> ConfirmAsync(ContentClassificationPolicy policy,
        ContentClassificationRequest? classification, TenantDataHandlingWorkflow workflow, Guid actor, CancellationToken ct)
    {
        if (classification is null) throw new ContentClassificationValidationException("Explicit classification confirmation is required.");
        ContentClassificationPolicy.ValidateUserSelection(classification);
        ContentClassificationPolicy.EnsureProcessable(classification.Classification, workflow.ToString());
        await policy.EnsureAllowedAsync(classification, workflow, actor, cancellationToken: ct);
        return classification;
    }
}

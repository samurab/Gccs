using Gccs.Application.Security;

namespace Gccs.Application.Tenancy;

public sealed record DataHandlingNoticePackage(string Root);

public interface ICurrentDataHandlingNoticeGuard
{
    Task EnsureAsync(string workflow, Guid actorUserId, CancellationToken cancellationToken = default);
}

public sealed class CurrentDataHandlingNoticeService(
    ITenantRepository tenants,
    ICurrentTenantContext tenantContext,
    DataHandlingNoticeService notices,
    DataHandlingNoticeAcknowledgementService acknowledgements,
    DataHandlingNoticePackage package) : ICurrentDataHandlingNoticeGuard
{
    public async Task<DataHandlingNoticeDto> GetAsync(string workflow, CancellationToken cancellationToken = default)
    {
        var mode = await tenants.FindCurrentTenantDataHandlingModeAsync(cancellationToken)
            ?? throw new DataHandlingNoticeValidationException("The current tenant mode is unavailable.");
        var context = NormalizeWorkflow(workflow);
        return await notices.GetPublishedAsync(package.Root, mode, context, cancellationToken)
            ?? throw new DataHandlingNoticeValidationException("No current published notice covers this workflow.");
    }

    public async Task<DataHandlingNoticeAcknowledgementDto> AcknowledgeAsync(
        AcknowledgeDataHandlingNoticeRequest request, CancellationToken cancellationToken = default)
    {
        var workflow = NormalizeWorkflow(request.WorkflowContext);
        var notice = await GetAsync(workflow, cancellationToken);
        return await acknowledgements.AcknowledgeAsync(tenantContext.TenantId, tenantContext.UserId, notice,
            request with { WorkflowContext = workflow }, cancellationToken);
    }

    public async Task EnsureAsync(string workflow, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var context = NormalizeWorkflow(workflow);
        var notice = await GetAsync(context, cancellationToken);
        await acknowledgements.EnsureAcknowledgedAsync(tenantContext.TenantId, actorUserId, notice, context, cancellationToken);
    }

    public static string NormalizeWorkflow(string workflow) => workflow?.Trim() switch
    {
        "ContractDocumentUpload" or "ExtractionJob" or "ContractIntake" => "ContractIntake",
        "EvidenceSubmission" or "EvidenceUpload" => "EvidenceUpload",
        "Report" or "ReportGeneration" => "ReportGeneration",
        "Onboarding" => "Onboarding",
        "Support" => "Support",
        _ => throw new DataHandlingNoticeValidationException("The notice workflow is not supported.")
    };
}

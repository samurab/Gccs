using Gccs.Application.Audit;
using Gccs.Domain.Audit;

namespace Gccs.Application.Tasks;

public sealed class RenewalGenerationService(
    IRenewalTaskRepository repository,
    IAuditEventWriter auditEventWriter)
{
    private const int DefaultLeadTimeDays = 30;

    public async Task<RenewalTaskGenerationResult> GenerateAsync(
        GenerateRenewalTasksRequest request,
        Guid actorUserId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var leadTimeDays = request.LeadTimeDays ?? DefaultLeadTimeDays;
        if (leadTimeDays is < 0 or > 365)
        {
            throw new ComplianceTaskValidationException("Renewal lead time must be between 0 and 365 days.");
        }

        var result = await repository.GenerateForCurrentTenantAsync(leadTimeDays, actorUserId, cancellationToken);
        await auditEventWriter.WriteAsync(
            tenantId,
            actorUserId,
            AuditAction.Created,
            "ComplianceTaskRenewalGeneration",
            tenantId.ToString(),
            $"Renewal generation created {result.CreatedCount} tasks and skipped {result.SkippedDuplicateCount} duplicates.",
            new Dictionary<string, string>
            {
                ["leadTimeDays"] = result.LeadTimeDays.ToString(),
                ["createdCount"] = result.CreatedCount.ToString(),
                ["skippedDuplicateCount"] = result.SkippedDuplicateCount.ToString()
            },
            cancellationToken);

        return result;
    }
}

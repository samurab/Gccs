namespace Gccs.Application.Tenancy;
public interface ISyntheticContentApprovalRepository
{
    Task<bool> IsApprovedAsync(string? entityType, string? entityId, CancellationToken cancellationToken);
}

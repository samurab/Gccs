namespace Gccs.Application.Compliance;

public sealed class ApplicabilityFactService(IApplicabilityFactRepository repository)
{
    public Task<IReadOnlyList<ApplicabilityFactDto>> ListAsync(
        ApplicabilityFactQuery query,
        CancellationToken cancellationToken = default) =>
        repository.ListAsync(query, cancellationToken);
}

public interface IApplicabilityFactRepository
{
    Task<IReadOnlyList<ApplicabilityFactDto>> ListAsync(
        ApplicabilityFactQuery query,
        CancellationToken cancellationToken = default);
}

public sealed record ApplicabilityFactQuery(
    Guid TenantId,
    Guid? ContractId = null,
    Guid? ClauseId = null,
    Guid? SubcontractorId = null);

public sealed record ApplicabilityFactDto(
    Guid TenantId,
    string Key,
    string Value,
    bool IsUnknown,
    string SourceType,
    string SourceId,
    DateTimeOffset? LastUpdatedAt);

using Gccs.Application.Audit;
using Gccs.Domain.Audit;
using Gccs.Domain.Companies;
using Gccs.Domain.Contracts;

namespace Gccs.Application.Contracts;

public sealed class ContractService(IContractRepository repository, IAuditEventWriter auditEventWriter)
{
    public Task<IReadOnlyList<ContractDto>> ListCurrentTenantAsync(CancellationToken cancellationToken = default) =>
        repository.ListCurrentTenantAsync(cancellationToken);

    public Task<ContractDto?> FindCurrentTenantAsync(Guid contractId, CancellationToken cancellationToken = default) =>
        repository.FindCurrentTenantAsync(contractId, cancellationToken);

    public async Task<ContractDto> CreateCurrentTenantAsync(
        UpsertContractRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(request);
        Validate(normalized);
        var created = await repository.CreateCurrentTenantAsync(normalized, actorUserId, cancellationToken);
        await WriteAuditAsync(created, actorUserId, AuditAction.Created, cancellationToken);
        return created;
    }

    public async Task<ContractDto?> UpdateCurrentTenantAsync(
        Guid contractId,
        UpsertContractRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(request);
        Validate(normalized);
        var updated = await repository.UpdateCurrentTenantAsync(contractId, normalized, actorUserId, cancellationToken);

        if (updated is not null)
        {
            await WriteAuditAsync(updated, actorUserId, AuditAction.Updated, cancellationToken);
        }

        return updated;
    }

    private async Task WriteAuditAsync(
        ContractDto contract,
        Guid actorUserId,
        AuditAction action,
        CancellationToken cancellationToken)
    {
        await auditEventWriter.WriteAsync(
            contract.TenantId,
            actorUserId,
            action,
            "Contract",
            contract.Id.ToString(),
            action == AuditAction.Created
                ? $"Contract '{contract.ContractNumber}' was created."
                : $"Contract '{contract.ContractNumber}' was updated.",
            new Dictionary<string, string>
            {
                ["contractNumber"] = contract.ContractNumber,
                ["status"] = contract.Status.ToString(),
                ["relationship"] = contract.Relationship.ToString(),
                ["kind"] = contract.Kind.ToString(),
                ["dataHandlingPosture"] = contract.DataHandlingPosture.ToString()
            },
            cancellationToken);
    }

    private static UpsertContractRequest Normalize(UpsertContractRequest request) =>
        request with
        {
            ContractNumber = request.ContractNumber.Trim(),
            Title = request.Title.Trim(),
            AgencyOrPrimeName = request.AgencyOrPrimeName.Trim(),
            PlaceOfPerformance = request.PlaceOfPerformance.Trim(),
            Description = request.Description.Trim()
        };

    private static void Validate(UpsertContractRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        AddIf(errors, string.IsNullOrWhiteSpace(request.ContractNumber), "contractNumber", "Contract number is required.");
        AddIf(errors, string.IsNullOrWhiteSpace(request.Title), "title", "Contract title is required.");
        AddIf(errors, string.IsNullOrWhiteSpace(request.AgencyOrPrimeName), "agencyOrPrimeName", "Agency or prime is required.");
        AddIf(errors, request.Kind is ContractKind.Unknown, "kind", "Contract type is required.");
        AddIf(errors, request.Status is not (ContractStatus.Draft or ContractStatus.Active), "status", "Story 8.1 supports Draft and Active contract records.");
        AddIf(errors, string.IsNullOrWhiteSpace(request.PlaceOfPerformance), "placeOfPerformance", "Place of performance is required.");
        AddIf(errors, request.PeriodOfPerformanceEnd < request.PeriodOfPerformanceStart, "periodOfPerformanceEnd", "Period of performance end must be on or after the start date.");
        AddIf(errors, request.DataHandlingPosture is DataHandlingPosture.Unknown, "dataHandlingPosture", "Data handling posture is required.");

        if (errors.Count > 0)
        {
            throw new ContractValidationException(errors);
        }
    }

    private static void AddIf(IDictionary<string, string[]> errors, bool condition, string field, string message)
    {
        if (condition)
        {
            errors[field] = [message];
        }
    }
}

public interface IContractRepository
{
    Task<IReadOnlyList<ContractDto>> ListCurrentTenantAsync(CancellationToken cancellationToken = default);

    Task<ContractDto?> FindCurrentTenantAsync(Guid contractId, CancellationToken cancellationToken = default);

    Task<ContractDto> CreateCurrentTenantAsync(
        UpsertContractRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<ContractDto?> UpdateCurrentTenantAsync(
        Guid contractId,
        UpsertContractRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}

public sealed class ContractValidationException(IReadOnlyDictionary<string, string[]> errors)
    : InvalidOperationException("Contract record is missing required fields.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}

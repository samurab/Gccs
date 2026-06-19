using System.Collections.Concurrent;
using Gccs.Application.Compliance;

namespace Gccs.Infrastructure.Compliance;

public sealed class InMemoryCustomerManagedKeyPolicyRepository : ICustomerManagedKeyPolicyRepository
{
    private readonly ConcurrentDictionary<Guid, List<CustomerManagedKeyPolicyDto>> _records = new();

    public Task<CustomerManagedKeyPolicyDto?> GetAsync(Guid tenantId, Guid policyId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_records.GetOrAdd(tenantId, _ => []).SingleOrDefault(record => record.Id == policyId));

    public Task<CustomerManagedKeyPolicyDto> CreateAsync(Guid tenantId, RegisterCustomerManagedKeyPolicyRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var policy = new CustomerManagedKeyPolicyDto(
            Guid.NewGuid(),
            tenantId,
            request.Provider,
            request.KeyId,
            request.Environment,
            CustomerManagedKeyPolicyStatus.Draft,
            request.RotationCadenceDays,
            request.LastRotationDate,
            request.NextRotationDate,
            request.Owner,
            request.Approver,
            request.EmergencyContact,
            null,
            [Event(CustomerManagedKeyPolicyStatus.Draft, request.Owner, "Policy registered.")]);

        _records.GetOrAdd(tenantId, _ => []).Add(policy);
        return Task.FromResult(policy);
    }

    public Task<CustomerManagedKeyPolicyDto?> RecordValidationAsync(Guid tenantId, Guid policyId, CustomerManagedKeyValidationRequest request, bool valid, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var status = valid ? CustomerManagedKeyPolicyStatus.Validated : CustomerManagedKeyPolicyStatus.ValidationFailed;
        return UpdateAsync(tenantId, policyId, policy => policy with
        {
            Status = status,
            LastValidation = request,
            History = [.. policy.History, Event(status, request.Reviewer, valid ? "Validation succeeded." : "Validation failed.")]
        });
    }

    public Task<CustomerManagedKeyPolicyDto?> ChangeStatusAsync(Guid tenantId, Guid policyId, CustomerManagedKeyStatusRequest request, Guid actorUserId, CancellationToken cancellationToken = default) =>
        UpdateAsync(tenantId, policyId, policy => policy with
        {
            Status = request.Status,
            LastRotationDate = request.Status == CustomerManagedKeyPolicyStatus.Rotated ? DateOnly.FromDateTime(DateTime.UtcNow) : policy.LastRotationDate,
            NextRotationDate = request.Status == CustomerManagedKeyPolicyStatus.Rotated ? DateOnly.FromDateTime(DateTime.UtcNow).AddDays(policy.RotationCadenceDays) : policy.NextRotationDate,
            History = [.. policy.History, Event(request.Status, request.Reviewer, request.Notes ?? $"Moved to {request.Status}.")]
        });

    private Task<CustomerManagedKeyPolicyDto?> UpdateAsync(Guid tenantId, Guid policyId, Func<CustomerManagedKeyPolicyDto, CustomerManagedKeyPolicyDto> update)
    {
        var records = _records.GetOrAdd(tenantId, _ => []);
        var index = records.FindIndex(record => record.Id == policyId);
        if (index < 0)
        {
            return Task.FromResult<CustomerManagedKeyPolicyDto?>(null);
        }

        records[index] = update(records[index]);
        return Task.FromResult<CustomerManagedKeyPolicyDto?>(records[index]);
    }

    private static CustomerManagedKeyPolicyEventDto Event(CustomerManagedKeyPolicyStatus status, string reviewer, string summary) =>
        new(DateTimeOffset.UtcNow, status, reviewer, summary);
}

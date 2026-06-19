using Gccs.Application.Audit;
using Gccs.Application.Security;
using Gccs.Domain.Audit;

namespace Gccs.Application.Compliance;

public sealed class FedRampReadinessExportPackageService(
    IFedRampReadinessExportPackageRepository repository,
    ICurrentTenantContext tenantContext,
    IAuditEventWriter auditEventWriter)
{
    public async Task<FedRampReadinessPackageDto> GenerateAsync(CreateFedRampReadinessPackageRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        if (request.Records.Length == 0)
        {
            throw new FedRampReadinessPackageValidationException("At least one package record is required.");
        }

        var package = await repository.CreateAsync(tenantContext.TenantId, request, actorUserId, cancellationToken);
        await WriteAuditAsync(package, actorUserId, AuditAction.Created, "FedRAMP readiness package was generated.", cancellationToken);
        return package;
    }

    public async Task<FedRampReadinessPackageDto?> ChangeStatusAsync(Guid packageId, FedRampReadinessPackageStatusRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ActorName))
        {
            throw new FedRampReadinessPackageValidationException("Actor metadata is required.");
        }

        var updated = await repository.ChangeStatusAsync(tenantContext.TenantId, packageId, request, actorUserId, cancellationToken);
        if (updated is not null)
        {
            var action = request.Status is FedRampReadinessPackageStatus.Archived or FedRampReadinessPackageStatus.Superseded ? AuditAction.Archived : AuditAction.Updated;
            await WriteAuditAsync(updated, actorUserId, action, $"FedRAMP readiness package moved to {updated.Status}.", cancellationToken);
        }

        return updated;
    }

    public async Task<FedRampReadinessPackageDto?> ShareAsync(Guid packageId, FedRampReadinessPackageShareRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var package = await repository.GetAsync(tenantContext.TenantId, packageId, cancellationToken);
        if (package is null)
        {
            return null;
        }

        if (package.Status is not FedRampReadinessPackageStatus.Approved and not FedRampReadinessPackageStatus.Shared)
        {
            throw new FedRampReadinessPackageValidationException("Only approved packages can be shared.");
        }

        var shared = await repository.ShareAsync(tenantContext.TenantId, packageId, request, actorUserId, cancellationToken);
        if (shared is not null)
        {
            await WriteAuditAsync(shared, actorUserId, AuditAction.Exported, "FedRAMP readiness package was shared.", cancellationToken);
        }

        return shared;
    }

    private Task WriteAuditAsync(FedRampReadinessPackageDto package, Guid actorUserId, AuditAction action, string summary, CancellationToken cancellationToken) =>
        auditEventWriter.WriteAsync(tenantContext.TenantId, actorUserId, action, "FedRampReadinessPackage", package.Id.ToString(), summary, new Dictionary<string, string> { ["status"] = package.Status.ToString(), ["version"] = package.PackageVersion }, cancellationToken);
}

public interface IFedRampReadinessExportPackageRepository
{
    Task<FedRampReadinessPackageDto?> GetAsync(Guid tenantId, Guid packageId, CancellationToken cancellationToken = default);
    Task<FedRampReadinessPackageDto> CreateAsync(Guid tenantId, CreateFedRampReadinessPackageRequest request, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<FedRampReadinessPackageDto?> ChangeStatusAsync(Guid tenantId, Guid packageId, FedRampReadinessPackageStatusRequest request, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<FedRampReadinessPackageDto?> ShareAsync(Guid tenantId, Guid packageId, FedRampReadinessPackageShareRequest request, Guid actorUserId, CancellationToken cancellationToken = default);
}

public sealed record CreateFedRampReadinessPackageRequest(string PackageVersion, string Scope, string Environment, string Reviewer, bool GovernanceAuthorizedFedRampClaim, FedRampPackageRecordDto[] Records, string[] Gaps, string[] AcceptedRisks, string ReadinessSummary);
public sealed record FedRampReadinessPackageStatusRequest(FedRampReadinessPackageStatus Status, string ActorName, string? Notes = null);
public sealed record FedRampReadinessPackageShareRequest(string Recipient, string Purpose);
public sealed record FedRampPackageRecordDto(string RecordType, string RecordId, string Title, FedRampPackageRecordStatus Status, bool Restricted, bool Prohibited, Guid TenantId);
public sealed record FedRampReadinessPackageDto(Guid Id, Guid TenantId, DateTimeOffset GeneratedAt, string PackageVersion, string Scope, string Environment, string Reviewer, string AuthorizationLanguage, string[] Gaps, string[] AcceptedRisks, string ReadinessSummary, FedRampPackageRecordDto[] IncludedRecords, FedRampReadinessPackageStatus Status, string? LastActor, DateTimeOffset? SharedAt);

public enum FedRampPackageRecordStatus { Draft, Approved, Published, Expired, Superseded, Archived }
public enum FedRampReadinessPackageStatus { Draft, InReview, Approved, Shared, Revoked, Superseded, Archived }

public sealed class FedRampReadinessPackageValidationException(string message) : InvalidOperationException(message);

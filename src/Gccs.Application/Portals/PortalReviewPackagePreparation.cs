using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Domain.Audit;
using Gccs.Domain.Common;

namespace Gccs.Application.Portals;

public sealed class PortalReviewPackagePreparationService(
    IPortalReviewPackagePreparationRepository repository,
    IAuditEventWriter auditEventWriter,
    IApplicationTransaction transaction,
    TimeProvider timeProvider)
{
    public Task<PreparedPortalReviewPackageDto> PrepareAsync(
        PreparePortalReviewPackageRequest request,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        Validate(request);
        return transaction.ExecuteAsync(async token =>
        {
            var package = await repository.CreateAsync(
                request with { Title = request.Title.Trim() }, tenantId, actorUserId,
                timeProvider.GetUtcNow(), token);
            await auditEventWriter.WriteAsync(
                tenantId, actorUserId, AuditAction.Created, "Report", package.PackageId.ToString(),
                "An immutable package was prepared for an external-review approval decision.",
                new Dictionary<string, string>
                {
                    ["sourceType"] = package.SourceType.ToString(),
                    ["classification"] = package.Classification.ToString(),
                    ["evidenceCount"] = package.EvidenceItemIds.Count.ToString()
                }, token);
            return package;
        }, cancellationToken);
    }

    private static void Validate(PreparePortalReviewPackageRequest request)
    {
        if (!Enum.IsDefined(request.SourceType))
            throw new PortalPackageValidationException("A supported package source type is required.");
        if (request.Title?.Trim().Length is < 1 or > 240)
            throw new PortalPackageValidationException("Package title is required and cannot exceed 240 characters.");
        if (SensitiveContentMarkerDetector.ContainsExplicitRestrictedMarking(request.Title))
            throw new PortalPackageValidationException("Package title contains a prohibited data marking.");
        if (request.Classification is not (ContentClassification.Unclassified or ContentClassification.Fci))
            throw new PortalPackageValidationException("External-review packages must be Unclassified or FCI.");
        if (request.SourceType == PortalReviewPreparationSource.ContractObligationMatrix && request.ContractId is null)
            throw new PortalPackageValidationException("A contract is required for an obligation-matrix package.");
        if (request.SourceType == PortalReviewPreparationSource.AuditLogExport && request.ContractId is not null)
            throw new PortalPackageValidationException("An audit-log package cannot specify a contract.");
    }
}

public interface IPortalReviewPackagePreparationRepository
{
    Task<PreparedPortalReviewPackageDto> CreateAsync(
        PreparePortalReviewPackageRequest request, Guid tenantId, Guid actorUserId,
        DateTimeOffset generatedAt, CancellationToken cancellationToken = default);
}

public sealed record PreparePortalReviewPackageRequest(
    PortalReviewPreparationSource SourceType,
    Guid? ContractId,
    string Title,
    ContentClassification Classification);

public sealed record PreparedPortalReviewPackageDto(
    Guid PackageId,
    PortalReviewPreparationSource SourceType,
    string Title,
    ContentClassification Classification,
    Guid? ContractId,
    IReadOnlyList<Guid> EvidenceItemIds,
    DateTimeOffset GeneratedAt);

public enum PortalReviewPreparationSource
{
    ContractObligationMatrix,
    AuditLogExport
}

public sealed class PortalReviewPackageSourceNotFoundException(string message) : InvalidOperationException(message);

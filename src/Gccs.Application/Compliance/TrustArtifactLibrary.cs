using Gccs.Application.Audit;
using Gccs.Application.Security;
using Gccs.Domain.Audit;

namespace Gccs.Application.Compliance;

public sealed class TrustArtifactLibraryService(
    ITrustArtifactLibraryRepository repository,
    ICurrentTenantContext tenantContext,
    IAuditEventWriter auditEventWriter)
{
    public async Task<TrustArtifactDto> CreateAsync(CreateTrustArtifactRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        ValidateCreate(request);
        var created = await repository.CreateAsync(tenantContext.TenantId, request, actorUserId, cancellationToken);
        await WriteAuditAsync(created, actorUserId, AuditAction.Created, "Trust artifact was created.", cancellationToken);
        return created;
    }

    public async Task<TrustArtifactDto?> ChangeStatusAsync(Guid artifactId, TrustArtifactStatusRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        ValidateText(request.ActorName, nameof(request.ActorName), 200);
        if (request.Status is TrustArtifactStatus.Published &&
            (string.IsNullOrWhiteSpace(request.ReviewedBy) || string.IsNullOrWhiteSpace(request.ApprovedBy) || !request.ReviewDate.HasValue))
        {
            throw new TrustArtifactValidationException("Publication requires review date, reviewer, and approver metadata.");
        }

        var updated = await repository.ChangeStatusAsync(tenantContext.TenantId, artifactId, request, actorUserId, cancellationToken);
        if (updated is not null)
        {
            await WriteAuditAsync(updated, actorUserId, AuditAction.Updated, $"Trust artifact moved to {updated.Status}.", cancellationToken);
        }

        return updated;
    }

    public async Task<TrustArtifactShareResult?> ShareAsync(Guid artifactId, TrustArtifactShareRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        ValidateText(request.Recipient, nameof(request.Recipient), 320);
        var artifact = await repository.GetAsync(tenantContext.TenantId, artifactId, cancellationToken);
        if (artifact is null)
        {
            return null;
        }

        if (artifact.Status is not TrustArtifactStatus.Published || artifact.ExpirationDate < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            await WriteAuditAsync(artifact, actorUserId, AuditAction.Rejected, "Trust artifact sharing was blocked by artifact status or expiration.", cancellationToken);
            return new TrustArtifactShareResult(false, "artifact_not_shareable", artifactId, request.Recipient);
        }

        if (artifact.NdaRequired && !request.NdaAccepted)
        {
            return new TrustArtifactShareResult(false, "nda_required", artifactId, request.Recipient);
        }

        if (artifact.Audience != request.Audience || artifact.AllowedEnvironment != request.Environment || artifact.AllowedTenantTier != request.TenantTier)
        {
            return new TrustArtifactShareResult(false, "recipient_not_permitted", artifactId, request.Recipient);
        }

        await repository.RecordShareAsync(tenantContext.TenantId, artifactId, request, actorUserId, cancellationToken);
        await WriteAuditAsync(artifact, actorUserId, AuditAction.Exported, "Trust artifact was shared externally.", cancellationToken);
        return new TrustArtifactShareResult(true, "artifact_shared", artifactId, request.Recipient);
    }

    private static void ValidateCreate(CreateTrustArtifactRequest request)
    {
        ValidateText(request.Owner, nameof(request.Owner), 200);
        ValidateText(request.Version, nameof(request.Version), 80);
        ValidateText(request.SourceFile, nameof(request.SourceFile), 600);
        if (request.EffectiveDate == default || request.ExpirationDate == default || request.ExpirationDate <= request.EffectiveDate)
        {
            throw new TrustArtifactValidationException("Effective and future expiration dates are required.");
        }
    }

    private static void ValidateText(string? value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new TrustArtifactValidationException($"{fieldName} is required.");
        }

        if (value.Trim().Length > maxLength)
        {
            throw new TrustArtifactValidationException($"{fieldName} must be {maxLength} characters or fewer.");
        }
    }

    private Task WriteAuditAsync(TrustArtifactDto artifact, Guid actorUserId, AuditAction action, string summary, CancellationToken cancellationToken) =>
        auditEventWriter.WriteAsync(tenantContext.TenantId, actorUserId, action, "TrustArtifact", artifact.Id.ToString(), summary, new Dictionary<string, string> { ["artifactType"] = artifact.ArtifactType.ToString(), ["status"] = artifact.Status.ToString() }, cancellationToken);
}

public interface ITrustArtifactLibraryRepository
{
    Task<TrustArtifactDto?> GetAsync(Guid tenantId, Guid artifactId, CancellationToken cancellationToken = default);
    Task<TrustArtifactDto> CreateAsync(Guid tenantId, CreateTrustArtifactRequest request, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<TrustArtifactDto?> ChangeStatusAsync(Guid tenantId, Guid artifactId, TrustArtifactStatusRequest request, Guid actorUserId, CancellationToken cancellationToken = default);
    Task RecordShareAsync(Guid tenantId, Guid artifactId, TrustArtifactShareRequest request, Guid actorUserId, CancellationToken cancellationToken = default);
}

public sealed record CreateTrustArtifactRequest(TrustArtifactType ArtifactType, string Owner, string Version, TrustArtifactAudience Audience, DateOnly EffectiveDate, DateOnly ExpirationDate, string SourceFile, bool NdaRequired, string AllowedTenantTier, string AllowedEnvironment);
public sealed record TrustArtifactStatusRequest(TrustArtifactStatus Status, string ActorName, DateOnly? ReviewDate = null, string? ReviewedBy = null, string? ApprovedBy = null);
public sealed record TrustArtifactShareRequest(string Recipient, TrustArtifactAudience Audience, string TenantTier, string Environment, bool NdaAccepted);
public sealed record TrustArtifactShareResult(bool Allowed, string ReasonCode, Guid ArtifactId, string Recipient);

public sealed record TrustArtifactDto(Guid Id, Guid TenantId, TrustArtifactType ArtifactType, string Owner, string Version, TrustArtifactStatus Status, TrustArtifactAudience Audience, DateOnly EffectiveDate, DateOnly? ReviewDate, DateOnly ExpirationDate, string? ReviewedBy, string? ApprovedBy, string SourceFile, bool NdaRequired, string AllowedTenantTier, string AllowedEnvironment, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt);

public enum TrustArtifactType { SecurityOverview, ArchitectureDiagram, SharedResponsibilityMatrix, SubprocessorsList, DataRetentionPolicy, IncidentResponseSummary, AiUsagePolicy, AccessControlSummary, SupportSla }
public enum TrustArtifactStatus { Draft, InReview, Approved, Published, Expired, Superseded, Archived }
public enum TrustArtifactAudience { Public, Prospect, Customer, RegulatedCustomer, Internal }

public sealed class TrustArtifactValidationException(string message) : InvalidOperationException(message);

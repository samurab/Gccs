using System.Text;
using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Domain.Audit;
using Gccs.Domain.Common;

namespace Gccs.Application.Portals;

public sealed class ApprovedPackagePortalReviewService(
    ExternalPortalAccessService accessService,
    IPortalPackageRepository packageRepository,
    PortalPackageLifecycleService lifecycleService,
    IAuditEventWriter auditEventWriter,
    IApplicationTransaction transaction,
    TimeProvider timeProvider,
    PortalReviewDownloadPolicy downloadPolicy)
{
    public Task<IReadOnlyList<PortalPackageDashboardItemDto>> ListPackagesAsync(
        Guid invitationId, PortalReviewerIdentity identity, CancellationToken cancellationToken = default) =>
        transaction.ExecuteAsync(
            token => ListPackagesCoreAsync(invitationId, identity, token), cancellationToken);

    private async Task<IReadOnlyList<PortalPackageDashboardItemDto>> ListPackagesCoreAsync(
        Guid invitationId, PortalReviewerIdentity identity, CancellationToken cancellationToken = default)
    {
        var invitation = await RequireInvitationAsync(invitationId, cancellationToken);
        var asOf = timeProvider.GetUtcNow();
        if (!accessService.IsInvitationIdentityEligible(
                invitation, identity.ActorUserId, identity.Email,
                identity.StrongAuthenticationSatisfied, asOf))
        {
            await auditEventWriter.WriteAsync(
                invitation.TenantId, identity.ActorUserId, AuditAction.Rejected,
                "ExternalPortalInvitation", invitation.Id.ToString(),
                "External portal package dashboard access was denied.",
                new Dictionary<string, string> { ["result"] = "portal_identity_or_invitation_denied" },
                cancellationToken);
            throw new PortalPackageAccessDeniedException("The portal invitation is unavailable.");
        }
        var shares = await lifecycleService.ListAccessibleAsync(invitation.TenantId, invitationId, asOf, cancellationToken);
        if (shares.Count == 0) return [];

        var packages = await packageRepository.ListPackagesAsync(
            invitation.TenantId, shares.Select(share => share.PackageId).ToArray(), cancellationToken);
        var byId = packages.ToDictionary(package => package.Id);
        var visible = new List<PortalPackageDashboardItemDto>();

        foreach (var share in shares.OrderBy(item => item.ExpiresAt))
        {
            if (!byId.TryGetValue(share.PackageId, out var package) || !IsVisible(package) ||
                !MatchesApprovedSource(share, package)) continue;
            var access = await ValidateAccessAsync(invitation, package, identity, false, asOf, cancellationToken);
            if (!access.Allowed)
                throw new PortalPackageAccessDeniedException("The portal invitation is unavailable.");

            await lifecycleService.RecordActivityAsync(
                share.Id, invitation.TenantId, invitationId, package.Id,
                PortalPackageActivityType.Access, identity.ActorUserId, cancellationToken);
            await WriteAuditAsync(invitation.TenantId, identity.ActorUserId, AuditAction.Viewed,
                "PortalPackage", package.Id.ToString(), "Portal package was reviewed.", share.Id, package.Id, cancellationToken);
            var messages = await packageRepository.ListMessagesAsync(invitation.TenantId, share.Id, cancellationToken);
            visible.Add(ToDashboardItem(share, package, invitation.CanDownload, messages));
        }

        return visible;
    }

    public Task<PortalPackageDashboardItemDto> GetPackageAsync(
        Guid invitationId, Guid sharedPackageId, PortalReviewerIdentity identity,
        CancellationToken cancellationToken = default) =>
        transaction.ExecuteAsync(
            token => GetPackageCoreAsync(invitationId, sharedPackageId, identity, token), cancellationToken);

    private async Task<PortalPackageDashboardItemDto> GetPackageCoreAsync(
        Guid invitationId, Guid sharedPackageId, PortalReviewerIdentity identity,
        CancellationToken cancellationToken = default)
    {
        var (invitation, share, package) = await EnsurePortalAccessAsync(
            invitationId, sharedPackageId, identity, false, cancellationToken);
        await lifecycleService.RecordActivityAsync(
            share.Id, invitation.TenantId, invitation.Id, package.Id,
            PortalPackageActivityType.Access, identity.ActorUserId, cancellationToken);
        await WriteAuditAsync(invitation.TenantId, identity.ActorUserId, AuditAction.Viewed,
            "PortalPackage", package.Id.ToString(), "Portal package was reviewed.", share.Id, package.Id, cancellationToken);
        var messages = await packageRepository.ListMessagesAsync(invitation.TenantId, share.Id, cancellationToken);
        return ToDashboardItem(share, package, invitation.CanDownload, messages);
    }

    public Task<PortalPackageReviewMessageDto> AddMessageAsync(
        Guid invitationId, Guid sharedPackageId, PortalPackageReviewMessageRequest request,
        PortalReviewerIdentity identity, CancellationToken cancellationToken = default) =>
        transaction.ExecuteAsync(
            token => AddMessageCoreAsync(invitationId, sharedPackageId, request, identity, token), cancellationToken);

    private async Task<PortalPackageReviewMessageDto> AddMessageCoreAsync(
        Guid invitationId, Guid sharedPackageId, PortalPackageReviewMessageRequest request,
        PortalReviewerIdentity identity, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(request.Kind))
            throw new PortalPackageValidationException("A supported reviewer message type is required.");
        var body = NormalizeMessage(request.Body);
        var (invitation, share, package) = await EnsurePortalAccessAsync(
            invitationId, sharedPackageId, identity, false, cancellationToken);
        var message = await packageRepository.AddMessageAsync(
            invitation.TenantId, invitation.Id, share.Id, package.Id,
            identity.ActorUserId, request.Kind, body, timeProvider.GetUtcNow(), cancellationToken);
        await lifecycleService.RecordActivityAsync(
            share.Id, invitation.TenantId, invitation.Id, package.Id,
            PortalPackageActivityType.Comment, identity.ActorUserId, cancellationToken);
        await WriteAuditAsync(invitation.TenantId, identity.ActorUserId, AuditAction.Created,
            "PortalPackageReviewMessage", message.Id.ToString(),
            request.Kind == PortalCommentKind.Question
                ? "Portal package question was added."
                : "Portal package comment was added.",
            share.Id, package.Id, cancellationToken);
        return message;
    }

    public Task<PortalPackageDownloadDto> DownloadAsync(
        Guid invitationId, Guid sharedPackageId, PortalReviewerIdentity identity,
        CancellationToken cancellationToken = default) =>
        transaction.ExecuteAsync(
            token => DownloadCoreAsync(invitationId, sharedPackageId, identity, token), cancellationToken);

    private async Task<PortalPackageDownloadDto> DownloadCoreAsync(
        Guid invitationId, Guid sharedPackageId, PortalReviewerIdentity identity,
        CancellationToken cancellationToken = default)
    {
        var (invitation, share, package) = await EnsurePortalAccessAsync(
            invitationId, sharedPackageId, identity, true, cancellationToken);
        var artifact = await packageRepository.GetArtifactAsync(invitation.TenantId, package.Id, cancellationToken)
            ?? throw new PortalPackageAccessDeniedException("The package artifact is unavailable for external review.");
        if (!IsVisible(artifact.Package))
            throw new PortalPackageAccessDeniedException("The package is not eligible for external review.");

        var downloadedAt = timeProvider.GetUtcNow();
        var watermark = downloadPolicy.WatermarkDownloads
            ? $"External review copy | package {package.Id} | downloaded {downloadedAt:O}"
            : null;
        var metadata = new PortalPackageDownloadMetadataDto(
            package.Id, share.Id, package.SourceKind, package.Title, package.Version,
            package.GeneratedAt, downloadedAt, watermark);
        var content = AddDownloadEnvelope(artifact.Content, artifact.ContentType, metadata);

        await lifecycleService.RecordActivityAsync(
            share.Id, invitation.TenantId, invitation.Id, package.Id,
            PortalPackageActivityType.Download, identity.ActorUserId, cancellationToken);
        await WriteAuditAsync(invitation.TenantId, identity.ActorUserId, AuditAction.Downloaded,
            "PortalPackage", package.Id.ToString(), "Portal package was downloaded.", share.Id, package.Id, cancellationToken);
        return new PortalPackageDownloadDto(
            artifact.FileName, artifact.ContentType, Encoding.UTF8.GetBytes(content), metadata);
    }

    private async Task<(ExternalPortalInvitationDto Invitation, SharedPortalPackageDto Share, PortalPackageDto Package)>
        EnsurePortalAccessAsync(Guid invitationId, Guid sharedPackageId, PortalReviewerIdentity identity,
            bool requiresDownloadPermission, CancellationToken cancellationToken)
    {
        var invitation = await RequireInvitationAsync(invitationId, cancellationToken);
        var share = await lifecycleService.FindAsync(sharedPackageId, invitation.TenantId, cancellationToken)
            ?? throw new PortalPackageAccessDeniedException("The shared package is unavailable.");
        if (share.InvitationId != invitation.Id)
            throw new PortalPackageAccessDeniedException("The shared package is unavailable.");
        var package = await packageRepository.FindPackageAsync(invitation.TenantId, share.PackageId, cancellationToken)
            ?? throw new PortalPackageAccessDeniedException("The shared package is unavailable.");
        if (!IsVisible(package))
            throw new PortalPackageAccessDeniedException("The package is not eligible for external review.");
        if (!MatchesApprovedSource(share, package))
            throw new PortalPackageAccessDeniedException("The approved package source changed and must be shared again.");

        var asOf = timeProvider.GetUtcNow();
        var access = await ValidateAccessAsync(
            invitation, package, identity, requiresDownloadPermission, asOf, cancellationToken);
        if (!access.Allowed || !await lifecycleService.CanAccessAsync(
                share.Id, invitation.TenantId, invitation.Id, package.Id, asOf, cancellationToken))
            throw new PortalPackageAccessDeniedException("The shared package is unavailable.");
        return (invitation, share, package);
    }

    private Task<ExternalPortalAccessResultDto> ValidateAccessAsync(
        ExternalPortalInvitationDto invitation, PortalPackageDto package, PortalReviewerIdentity identity,
        bool requiresDownloadPermission, DateTimeOffset asOf, CancellationToken cancellationToken) =>
        accessService.ValidateAccessAsync(new ExternalPortalAccessRequest(
            invitation.Id, package.Id, package.ContractId, identity.ActorUserId, identity.Email,
            identity.StrongAuthenticationSatisfied, asOf, requiresDownloadPermission), cancellationToken);

    private async Task<ExternalPortalInvitationDto> RequireInvitationAsync(
        Guid invitationId, CancellationToken cancellationToken) =>
        await packageRepository.FindInvitationAsync(invitationId, cancellationToken)
        ?? throw new PortalPackageAccessDeniedException("The portal invitation is unavailable.");

    private static bool IsVisible(PortalPackageDto package) =>
        package.Status == PortalPackageStatus.Approved &&
        !package.ContainsInternalNotes &&
        package.IsExternallySafe &&
        package.Classification is ContentClassification.Unclassified or ContentClassification.Fci;

    private static bool MatchesApprovedSource(SharedPortalPackageDto share, PortalPackageDto package) =>
        !string.IsNullOrWhiteSpace(share.ApprovedSourceFingerprint) &&
        share.ApprovedSourceVersion == package.Version &&
         string.Equals(share.ApprovedSourceFingerprint, PortalPackageFingerprint.Create(package),
             StringComparison.OrdinalIgnoreCase);

    private static string NormalizeMessage(string? body)
    {
        var normalized = body?.Trim() ?? string.Empty;
        if (normalized.Length is < 1 or > 2_000)
            throw new PortalPackageValidationException("A reviewer message is required and cannot exceed 2,000 characters.");
        if (SensitiveContentMarkerDetector.ContainsExplicitRestrictedMarking(normalized))
            throw new PortalPackageValidationException("Reviewer messages cannot contain CUI, classified, or other prohibited data markings.");
        return normalized;
    }

    private static PortalPackageDashboardItemDto ToDashboardItem(
        SharedPortalPackageDto share, PortalPackageDto package, bool invitationAllowsDownload,
        IReadOnlyList<PortalPackageReviewMessageDto> messages) =>
        new(share.Id, package.Id, package.SourceKind, package.Title, package.Version,
            package.Status, package.Classification, package.ContractId, package.EvidenceItemIds,
            package.EvidenceReferences, package.GeneratedAt, share.ReviewDueAt,
            share.ExternalReviewApprovedAt, share.ApprovedSourceVersion,
            share.ApprovedSourceFingerprint, share.State,
            invitationAllowsDownload && package.DownloadAvailable, messages);

    private static string AddDownloadEnvelope(
        string content, string contentType, PortalPackageDownloadMetadataDto metadata)
    {
        if (contentType.Contains("html", StringComparison.OrdinalIgnoreCase))
        {
            var banner = metadata.Watermark is null
                ? string.Empty
                : $"<aside data-portal-watermark=\"true\">{System.Net.WebUtility.HtmlEncode(metadata.Watermark)}</aside>";
            return $"<!-- FeDril portal package={metadata.PackageId}; sharedPackage={metadata.SharedPackageId}; version={metadata.Version}; downloadedAt={metadata.DownloadedAt:O} -->{banner}{content}";
        }

        var header = $"FeDril portal download\nPackage: {metadata.PackageId}\nShared package: {metadata.SharedPackageId}\nType: {metadata.SourceKind}\nTitle: {metadata.Title}\nVersion: {metadata.Version}\nGenerated: {metadata.GeneratedAt:O}\nDownloaded: {metadata.DownloadedAt:O}\n";
        if (metadata.Watermark is not null) header += $"Watermark: {metadata.Watermark}\n";
        return $"{header}\n{content}";
    }

    private Task WriteAuditAsync(
        Guid tenantId, Guid actorUserId, AuditAction action, string entityType, string entityId,
        string summary, Guid sharedPackageId, Guid packageId, CancellationToken cancellationToken) =>
        auditEventWriter.WriteAsync(
            tenantId, actorUserId, action, entityType, entityId, summary,
            new Dictionary<string, string>
            {
                ["sharedPackageId"] = sharedPackageId.ToString(),
                ["packageId"] = packageId.ToString()
            }, cancellationToken);
}

public interface IPortalPackageRepository
{
    Task<ExternalPortalInvitationDto?> FindInvitationAsync(Guid invitationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PortalPackageDto>> ListPackagesAsync(Guid tenantId, IReadOnlyCollection<Guid> packageIds, CancellationToken cancellationToken = default);
    Task<PortalPackageDto?> FindPackageAsync(Guid tenantId, Guid packageId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PortalPackageReviewMessageDto>> ListMessagesAsync(Guid tenantId, Guid sharedPackageId, CancellationToken cancellationToken = default);
    Task<PortalPackageReviewMessageDto> AddMessageAsync(Guid tenantId, Guid invitationId, Guid sharedPackageId, Guid packageId, Guid actorUserId, PortalCommentKind kind, string body, DateTimeOffset createdAt, CancellationToken cancellationToken = default);
    Task<PortalPackageArtifactDto?> GetArtifactAsync(Guid tenantId, Guid packageId, CancellationToken cancellationToken = default);
}

public sealed record PortalReviewerIdentity(Guid ActorUserId, string Email, bool StrongAuthenticationSatisfied);

public sealed record PortalPackageDto(
    Guid Id, Guid TenantId, Guid? ContractId, string Title, int Version,
    PortalPackageStatus Status, ContentClassification Classification, bool ContainsInternalNotes,
    IReadOnlyList<Guid> EvidenceItemIds, DateTimeOffset GeneratedAt)
{
    public string SourceKind { get; init; } = "Report";
    public bool IsExternallySafe { get; init; } = true;
    public bool DownloadAvailable { get; init; } = true;
    public IReadOnlyList<PortalEvidenceReferenceDto> EvidenceReferences { get; init; } = [];
    public string? SourceIntegrityFingerprint { get; init; }
}

public sealed record PortalEvidenceReferenceDto(
    Guid Id, string Name, string Type, ContentClassification Classification,
    DateTimeOffset? ApprovedAt, DateOnly? ExpiresAt);

public sealed record PortalPackageDashboardItemDto(
    Guid SharedPackageId, Guid PackageId, string SourceKind, string Title, int Version,
    PortalPackageStatus Status, ContentClassification Classification, Guid? ContractId,
    IReadOnlyList<Guid> EvidenceItemIds, IReadOnlyList<PortalEvidenceReferenceDto> EvidenceReferences,
    DateTimeOffset GeneratedAt, DateTimeOffset ReviewDueAt,
    DateTimeOffset ExternalReviewApprovedAt, int ApprovedSourceVersion, string ApprovedSourceFingerprint,
    SharedPortalPackageState ShareState, bool DownloadAvailable,
    IReadOnlyList<PortalPackageReviewMessageDto> ReviewerMessages);

public sealed record PortalPackageReviewMessageRequest(PortalCommentKind Kind, string Body);

public sealed record PortalPackageReviewMessageDto(
    Guid Id, PortalCommentKind Kind, string Body, DateTimeOffset CreatedAt);

public sealed record PortalPackageArtifactDto(
    PortalPackageDto Package, string FileName, string ContentType, string Content);

public sealed record PortalPackageDownloadMetadataDto(
    Guid PackageId, Guid SharedPackageId, string SourceKind, string Title, int Version,
    DateTimeOffset GeneratedAt, DateTimeOffset DownloadedAt, string? Watermark);

public sealed record PortalPackageDownloadDto(
    string FileName, string ContentType, byte[] Content, PortalPackageDownloadMetadataDto Metadata);

public sealed record PortalReviewDownloadPolicy(bool WatermarkDownloads);

public enum PortalPackageStatus { Draft, Approved, Archived }
public enum PortalCommentKind { Comment, Question }

public sealed class PortalPackageValidationException(string message) : InvalidOperationException(message);

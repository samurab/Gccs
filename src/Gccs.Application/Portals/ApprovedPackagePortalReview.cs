using Gccs.Application.Audit;
using Gccs.Domain.Audit;
using Gccs.Domain.Common;

namespace Gccs.Application.Portals;

public sealed class ApprovedPackagePortalReviewService(
    ExternalPortalAccessService accessService,
    IPortalPackageRepository packageRepository,
    PortalPackageLifecycleService lifecycleService,
    IAuditEventWriter auditEventWriter)
{
    public async Task<IReadOnlyList<PortalPackageDto>> ListPackagesAsync(
        Guid invitationId,
        DateTimeOffset asOf,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var invitation = await packageRepository.FindInvitationAsync(invitationId, cancellationToken);
        if (invitation is null)
        {
            return [];
        }

        var accessibleShares = (await lifecycleService.ListAccessibleAsync(invitation.TenantId, invitationId, asOf, cancellationToken))
            .ToDictionary(package => package.PackageId);
        var packages = await packageRepository.ListPackagesAsync(invitation.TenantId, cancellationToken);
        var visible = new List<PortalPackageDto>();
        foreach (var package in packages)
        {
            if (!accessibleShares.TryGetValue(package.Id, out var share)) continue;
            var access = await accessService.ValidateAccessAsync(invitationId, package.Id, package.ContractId, asOf, actorUserId, cancellationToken);
            if (access.Allowed && IsVisible(package))
            {
                await lifecycleService.RecordActivityAsync(
                    share.Id, invitation.TenantId, invitationId, package.Id, PortalPackageActivityType.Access, actorUserId, cancellationToken);
                visible.Add(package);
            }
        }

        return visible;
    }

    public async Task<PortalPackageCommentDto> AddCommentAsync(
        Guid sharedPackageId,
        Guid invitationId,
        PortalPackageCommentRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var package = await EnsurePortalAccessAsync(
            sharedPackageId, invitationId, request.PackageId, actorUserId, cancellationToken);
        var comment = await packageRepository.AddCommentAsync(request, package.TenantId, actorUserId, cancellationToken);
        await lifecycleService.RecordActivityAsync(
            sharedPackageId, package.TenantId, invitationId, request.PackageId, PortalPackageActivityType.Comment, actorUserId, cancellationToken);
        await WriteAuditAsync(package.TenantId, actorUserId, AuditAction.Created, "PortalPackageComment", comment.Id.ToString(), "Portal package comment was added.", request.PackageId, cancellationToken);
        return comment;
    }

    public Task<PortalPackageCommentDto> AddQuestionAsync(
        Guid sharedPackageId,
        Guid invitationId,
        PortalPackageCommentRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default) =>
        AddCommentAsync(sharedPackageId, invitationId, request with { Kind = PortalCommentKind.Question }, actorUserId, cancellationToken);

    public async Task<PortalPackageDownloadDto> DownloadAsync(
        Guid sharedPackageId,
        Guid invitationId,
        Guid packageId,
        bool watermark,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var package = await EnsurePortalAccessAsync(
            sharedPackageId, invitationId, packageId, actorUserId, cancellationToken);
        var download = new PortalPackageDownloadDto(
            package.Id,
            package.TenantId,
            package.Title,
            package.Version,
            package.GeneratedAt,
            watermark ? $"External Review - {package.TenantId}" : null,
            DateTimeOffset.UtcNow);
        await lifecycleService.RecordActivityAsync(
            sharedPackageId, package.TenantId, invitationId, packageId, PortalPackageActivityType.Download, actorUserId, cancellationToken);
        await WriteAuditAsync(package.TenantId, actorUserId, AuditAction.Downloaded, "PortalPackage", package.Id.ToString(), "Portal package was downloaded.", package.Id, cancellationToken);
        return download;
    }

    private async Task<PortalPackageDto> EnsurePortalAccessAsync(
        Guid sharedPackageId,
        Guid invitationId,
        Guid packageId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var package = await packageRepository.FindPackageAsync(packageId, cancellationToken)
            ?? throw new ExternalPortalAccessException("Package was not found.");
        if (!IsVisible(package))
            throw new PortalPackageAccessDeniedException("The package is not approved for external review.");
        var access = await accessService.ValidateAccessAsync(
            invitationId, packageId, package.ContractId, DateTimeOffset.UtcNow, actorUserId, cancellationToken);
        if (!access.Allowed)
            throw new PortalPackageAccessDeniedException("The package is outside the active portal invitation scope.");
        if (!await lifecycleService.CanAccessAsync(
                sharedPackageId, package.TenantId, invitationId, packageId, DateTimeOffset.UtcNow, cancellationToken))
            throw new PortalPackageAccessDeniedException("The shared package is unavailable or expired.");
        return package;
    }

    private static bool IsVisible(PortalPackageDto package) =>
        package.Status == PortalPackageStatus.Approved &&
        !package.ContainsInternalNotes &&
        package.Classification is ContentClassification.Unclassified or ContentClassification.Fci;

    private async Task WriteAuditAsync(
        Guid tenantId,
        Guid actorUserId,
        AuditAction action,
        string entityType,
        string entityId,
        string summary,
        Guid packageId,
        CancellationToken cancellationToken)
    {
        await auditEventWriter.WriteAsync(
            tenantId,
            actorUserId,
            action,
            entityType,
            entityId,
            summary,
            new Dictionary<string, string> { ["packageId"] = packageId.ToString() },
            cancellationToken);
    }
}

public interface IPortalPackageRepository
{
    Task<ExternalPortalInvitationDto?> FindInvitationAsync(Guid invitationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PortalPackageDto>> ListPackagesAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<PortalPackageDto?> FindPackageAsync(Guid packageId, CancellationToken cancellationToken = default);
    Task<PortalPackageCommentDto> AddCommentAsync(PortalPackageCommentRequest request, Guid tenantId, Guid actorUserId, CancellationToken cancellationToken = default);
}

public sealed record PortalPackageDto(
    Guid Id,
    Guid TenantId,
    Guid? ContractId,
    string Title,
    int Version,
    PortalPackageStatus Status,
    ContentClassification Classification,
    bool ContainsInternalNotes,
    IReadOnlyList<Guid> EvidenceItemIds,
    DateTimeOffset GeneratedAt);

public sealed record PortalPackageCommentRequest(
    Guid PackageId,
    PortalCommentKind Kind,
    string Body);

public sealed record PortalPackageCommentDto(
    Guid Id,
    Guid TenantId,
    Guid PackageId,
    Guid ActorUserId,
    PortalCommentKind Kind,
    string Body,
    DateTimeOffset CreatedAt);

public sealed record PortalPackageDownloadDto(
    Guid PackageId,
    Guid TenantId,
    string Title,
    int Version,
    DateTimeOffset GeneratedAt,
    string? Watermark,
    DateTimeOffset DownloadedAt);

public enum PortalPackageStatus
{
    Draft,
    Approved,
    Archived
}

public enum PortalCommentKind
{
    Comment,
    Question
}

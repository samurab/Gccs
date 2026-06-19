using Gccs.Application.Audit;
using Gccs.Domain.Audit;
using Gccs.Domain.Common;

namespace Gccs.Application.Portals;

public sealed class ApprovedPackagePortalReviewService(
    ExternalPortalAccessService accessService,
    IPortalPackageRepository packageRepository,
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

        var packages = await packageRepository.ListPackagesAsync(invitation.TenantId, cancellationToken);
        var visible = new List<PortalPackageDto>();
        foreach (var package in packages)
        {
            var access = await accessService.ValidateAccessAsync(invitationId, package.Id, package.ContractId, asOf, actorUserId, cancellationToken);
            if (access.Allowed && IsVisible(package))
            {
                visible.Add(package);
            }
        }

        return visible;
    }

    public async Task<PortalPackageCommentDto> AddCommentAsync(
        PortalPackageCommentRequest request,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var comment = await packageRepository.AddCommentAsync(request, tenantId, actorUserId, cancellationToken);
        await WriteAuditAsync(tenantId, actorUserId, AuditAction.Created, "PortalPackageComment", comment.Id.ToString(), "Portal package comment was added.", request.PackageId, cancellationToken);
        return comment;
    }

    public Task<PortalPackageCommentDto> AddQuestionAsync(
        PortalPackageCommentRequest request,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default) =>
        AddCommentAsync(request with { Kind = PortalCommentKind.Question }, tenantId, actorUserId, cancellationToken);

    public async Task<PortalPackageDownloadDto> DownloadAsync(
        Guid packageId,
        bool watermark,
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var package = await packageRepository.FindPackageAsync(packageId, cancellationToken) ??
            throw new ExternalPortalAccessException("Package was not found.");
        var download = new PortalPackageDownloadDto(
            package.Id,
            tenantId,
            package.Title,
            package.Version,
            package.GeneratedAt,
            watermark ? $"External Review - {tenantId}" : null,
            DateTimeOffset.UtcNow);
        await WriteAuditAsync(tenantId, actorUserId, AuditAction.Downloaded, "PortalPackage", package.Id.ToString(), "Portal package was downloaded.", package.Id, cancellationToken);
        return download;
    }

    private static bool IsVisible(PortalPackageDto package) =>
        package.Status == PortalPackageStatus.Approved &&
        !package.ContainsInternalNotes &&
        package.Classification is not (ContentClassification.Prohibited or ContentClassification.Unknown);

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

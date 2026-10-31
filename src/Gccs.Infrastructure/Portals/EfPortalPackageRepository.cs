using System.Text.Json;
using Gccs.Application.Compliance;
using Gccs.Application.Portals;
using Gccs.Application.Reports;
using Gccs.Domain.Common;
using Gccs.Domain.Evidence;
using Gccs.Domain.Reports;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Portals;

public sealed class EfPortalPackageRepository(GccsDbContext dbContext, TimeProvider timeProvider) : IPortalPackageRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ExternalPortalInvitationDto?> FindInvitationAsync(
        Guid invitationId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.ExternalPortalInvitations.AsNoTracking()
            .Include(item => item.PackageScopes).Include(item => item.ContractScopes)
            .SingleOrDefaultAsync(item => item.Id == invitationId, cancellationToken);
        return entity is null ? null : ToInvitation(entity);
    }

    public async Task<IReadOnlyList<PortalPackageDto>> ListPackagesAsync(
        Guid tenantId, IReadOnlyCollection<Guid> packageIds, CancellationToken cancellationToken = default)
    {
        if (packageIds.Count == 0) return [];
        var ids = packageIds.Distinct().ToArray();
        var packages = new List<PortalPackageDto>();
        packages.AddRange(await LoadReportsAsync(tenantId, ids, cancellationToken));
        packages.AddRange(await LoadSspPackagesAsync(tenantId, ids, cancellationToken));
        packages.AddRange(await LoadSprPackagesAsync(tenantId, ids, cancellationToken));
        return packages;
    }

    public async Task<PortalPackageDto?> FindPackageAsync(
        Guid tenantId, Guid packageId, CancellationToken cancellationToken = default) =>
        (await ListPackagesAsync(tenantId, [packageId], cancellationToken)).SingleOrDefault();

    public async Task<IReadOnlyList<PortalPackageReviewMessageDto>> ListMessagesAsync(
        Guid tenantId, Guid sharedPackageId, CancellationToken cancellationToken = default) =>
        await dbContext.PortalPackageReviewMessages.AsNoTracking()
            .Where(item => item.TenantId == tenantId && item.SharedPackageId == sharedPackageId)
            .OrderBy(item => item.CreatedAt).ThenBy(item => item.Id)
            .Select(item => new PortalPackageReviewMessageDto(
                item.Id, item.Kind, item.Body, item.CreatedAt))
            .ToArrayAsync(cancellationToken);

    public async Task<PortalPackageReviewMessageDto> AddMessageAsync(
        Guid tenantId, Guid invitationId, Guid sharedPackageId, Guid packageId, Guid actorUserId,
        PortalCommentKind kind, string body, DateTimeOffset createdAt,
        CancellationToken cancellationToken = default)
    {
        var entity = new PortalPackageReviewMessageEntity
        {
            Id = Guid.NewGuid(), TenantId = tenantId, InvitationId = invitationId,
            SharedPackageId = sharedPackageId, PackageId = packageId, ActorUserId = actorUserId,
            Kind = kind, Body = body, CreatedAt = createdAt
        };
        dbContext.PortalPackageReviewMessages.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new(entity.Id, entity.Kind, entity.Body, entity.CreatedAt);
    }

    public async Task<PortalPackageArtifactDto?> GetArtifactAsync(
        Guid tenantId, Guid packageId, CancellationToken cancellationToken = default)
    {
        var package = await FindPackageAsync(tenantId, packageId, cancellationToken);
        if (package is null || !package.DownloadAvailable || !package.IsExternallySafe) return null;

        if (package.SourceKind.StartsWith("Report:", StringComparison.Ordinal))
        {
            var content = await dbContext.Reports.AsNoTracking()
                .Where(item => item.TenantId == tenantId && item.Id == packageId)
                .Select(item => item.ExportHtml).SingleOrDefaultAsync(cancellationToken);
            return string.IsNullOrWhiteSpace(content) ? null :
                new(package, $"fedril-{package.Id:N}.html", "text/html; charset=utf-8", content);
        }

        if (package.SourceKind == "SspPackage")
        {
            var content = await dbContext.SspExportPackages.AsNoTracking()
                .Where(item => item.TenantId == tenantId && item.Id == packageId)
                .Select(item => item.HumanReadableReport).SingleOrDefaultAsync(cancellationToken);
            return string.IsNullOrWhiteSpace(content) ? null :
                new(package, $"fedril-ssp-{package.Id:N}.html", "text/html; charset=utf-8", content);
        }

        if (package.SourceKind == "SprPackage")
        {
            var entity = await dbContext.SprReportPackages.AsNoTracking()
                .SingleOrDefaultAsync(item => item.TenantId == tenantId && item.Id == packageId, cancellationToken);
            if (entity is null) return null;
            var content = $"<main><h1>SAM.gov subcontracting report preparation package</h1>" +
                $"<dl><dt>Contract</dt><dd>{entity.ContractId}</dd><dt>Period</dt><dd>{entity.PeriodStart:yyyy-MM-dd} through {entity.PeriodEnd:yyyy-MM-dd}</dd>" +
                $"<dt>Version</dt><dd>{entity.Version}</dd></dl><pre>{System.Net.WebUtility.HtmlEncode(entity.SnapshotJson)}</pre>" +
                $"<p>{System.Net.WebUtility.HtmlEncode(entity.NotSubmittedDisclaimer)}</p></main>";
            return new(package, $"fedril-spr-{package.Id:N}.html", "text/html; charset=utf-8", content);
        }

        return null;
    }

    private async Task<IReadOnlyList<PortalPackageDto>> LoadReportsAsync(
        Guid tenantId, Guid[] packageIds, CancellationToken cancellationToken)
    {
        var entities = await dbContext.Reports.AsNoTracking()
            .Include(item => item.Contracts).Include(item => item.EvidenceItems)
            .Where(item => item.TenantId == tenantId && packageIds.Contains(item.Id))
            .ToArrayAsync(cancellationToken);
        var results = new List<PortalPackageDto>(entities.Length);
        foreach (var entity in entities)
        {
            var evidenceIds = entity.EvidenceItems.Select(item => item.EvidenceItemId).Distinct().ToArray();
            var evidenceReferences = await LoadSafeEvidenceReferencesAsync(tenantId, evidenceIds, cancellationToken);
            var classification = entity.CurrentClassification?.Classification ?? entity.Classification;
            var contractIds = entity.Contracts.Select(item => item.ContractId).Distinct().ToArray();
            results.Add(new PortalPackageDto(
                entity.Id, tenantId, contractIds.Length == 1 ? contractIds[0] : null,
                entity.Title, 1, entity.Status == ReportStatus.Complete ? PortalPackageStatus.Approved : PortalPackageStatus.Draft,
                classification, ContainsInternalNotes(entity.SnapshotJson), evidenceIds, entity.GeneratedAt)
            {
                SourceKind = $"Report:{entity.Type}",
                IsExternallySafe = !entity.IsUseBlocked && evidenceReferences is not null,
                EvidenceReferences = evidenceReferences ?? [],
                SourceIntegrityFingerprint = Hash(entity.Title, entity.Status.ToString(), classification.ToString(),
                    entity.SnapshotJson, entity.ExportHtml),
                DownloadAvailable = !string.IsNullOrWhiteSpace(entity.ExportHtml)
            });
        }
        return results;
    }

    private async Task<IReadOnlyList<PortalPackageDto>> LoadSspPackagesAsync(
        Guid tenantId, Guid[] packageIds, CancellationToken cancellationToken)
    {
        var entities = await dbContext.SspExportPackages.AsNoTracking()
            .Where(item => item.TenantId == tenantId && packageIds.Contains(item.Id)).ToArrayAsync(cancellationToken);
        var results = new List<PortalPackageDto>(entities.Length);
        foreach (var entity in entities)
        {
            var evidenceIds = DeserializeSspEvidenceIds(entity.EvidenceReferencesJson);
            var evidenceReferences = await LoadSafeEvidenceReferencesAsync(tenantId, evidenceIds, cancellationToken);
            results.Add(new PortalPackageDto(
                entity.Id, tenantId, null, $"SSP package {entity.PackageVersion}", ParseVersion(entity.PackageVersion),
                entity.Status is nameof(SspExportPackageStatus.ExternalShareApproved) or nameof(SspExportPackageStatus.Shared)
                    ? PortalPackageStatus.Approved : PortalPackageStatus.Draft,
                ContentClassification.Fci, false, evidenceIds, entity.GeneratedAt)
            {
                SourceKind = "SspPackage",
                IsExternallySafe = evidenceReferences is not null,
                EvidenceReferences = evidenceReferences ?? [],
                SourceIntegrityFingerprint = Hash(entity.Status, entity.PackageVersion,
                    entity.EvidenceReferencesJson, entity.HumanReadableReport),
                DownloadAvailable = !string.IsNullOrWhiteSpace(entity.HumanReadableReport)
            });
        }
        return results;
    }

    private async Task<IReadOnlyList<PortalPackageDto>> LoadSprPackagesAsync(
        Guid tenantId, Guid[] packageIds, CancellationToken cancellationToken)
    {
        var entities = await dbContext.SprReportPackages.AsNoTracking()
            .Where(item => item.TenantId == tenantId && packageIds.Contains(item.Id)).ToArrayAsync(cancellationToken);
        var results = new List<PortalPackageDto>(entities.Length);
        foreach (var entity in entities)
        {
            var evidenceIds = DeserializeSprEvidenceIds(entity.SnapshotJson);
            var evidenceReferences = await LoadSafeEvidenceReferencesAsync(tenantId, evidenceIds, cancellationToken);
            results.Add(new PortalPackageDto(
                entity.Id, tenantId, entity.ContractId,
                $"SAM.gov {entity.ReportType} preparation package", entity.Version,
                entity.Status == EsrsReportPackageStatus.Approved ? PortalPackageStatus.Approved : PortalPackageStatus.Draft,
                ContentClassification.Fci, false, evidenceIds, entity.GeneratedAt)
            {
                SourceKind = "SprPackage",
                IsExternallySafe = evidenceReferences is not null,
                EvidenceReferences = evidenceReferences ?? [],
                SourceIntegrityFingerprint = Hash(entity.Status.ToString(), entity.Version.ToString(),
                    entity.SnapshotJson, entity.NotSubmittedDisclaimer),
                DownloadAvailable = true
            });
        }
        return results;
    }

    private async Task<IReadOnlyList<PortalEvidenceReferenceDto>?> LoadSafeEvidenceReferencesAsync(
        Guid tenantId, IReadOnlyCollection<Guid> evidenceIds, CancellationToken cancellationToken)
    {
        if (evidenceIds.Count == 0) return [];
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var references = await dbContext.EvidenceItems.AsNoTracking().Where(item =>
            item.TenantId == tenantId && evidenceIds.Contains(item.Id) &&
            item.Status == EvidenceStatus.Approved && !item.IsUseBlocked &&
            (item.ExpiresAt == null || item.ExpiresAt >= today) &&
            (item.Classification == ContentClassification.Unclassified || item.Classification == ContentClassification.Fci))
            .OrderBy(item => item.Name).ThenBy(item => item.Id)
            .Select(item => new PortalEvidenceReferenceDto(
                item.Id, item.Name, item.Type.ToString(), item.Classification, item.ApprovedAt, item.ExpiresAt))
            .ToArrayAsync(cancellationToken);
        return references.Length == evidenceIds.Distinct().Count() ? references : null;
    }

    private static bool ContainsInternalNotes(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return ContainsInternalNotes(document.RootElement);
        }
        catch (JsonException)
        {
            return true;
        }
    }

    private static bool ContainsInternalNotes(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.Name.Equals("reviewerNotes", StringComparison.OrdinalIgnoreCase) &&
                    property.Value.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined) &&
                    !string.IsNullOrWhiteSpace(property.Value.ToString())) return true;
                if (ContainsInternalNotes(property.Value)) return true;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in element.EnumerateArray())
                if (ContainsInternalNotes(child)) return true;
        }
        return false;
    }

    private static Guid[] DeserializeSspEvidenceIds(string json)
    {
        try
        {
            return (JsonSerializer.Deserialize<SspExportEvidenceReferenceDto[]>(json, JsonOptions) ?? [])
                .Select(item => item.Id).Distinct().ToArray();
        }
        catch (JsonException) { return [Guid.Empty]; }
    }

    private static Guid[] DeserializeSprEvidenceIds(string json)
    {
        try
        {
            var snapshot = JsonSerializer.Deserialize<EsrsReportPackageSnapshotDto>(json, JsonOptions);
            return snapshot?.EvidenceReferences.Select(item => item.EvidenceItemId).Distinct().ToArray() ?? [];
        }
        catch (JsonException) { return [Guid.Empty]; }
    }

    private static int ParseVersion(string version) =>
        int.TryParse(version.TrimStart('v', 'V'), out var parsed) ? parsed : 1;

    private static string Hash(params string?[] values) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(string.Join('\u001f', values))))
            .ToLowerInvariant();

    private static ExternalPortalInvitationDto ToInvitation(ExternalPortalInvitationEntity entity) => new(
        entity.Id, entity.TenantId, entity.Email, entity.Role,
        entity.PackageScopes.Select(item => item.PackageId).Order().ToArray(),
        entity.ContractScopes.Select(item => item.ContractId).Order().ToArray(),
        entity.ExpiresAt, entity.CanDownload, entity.StrongAuthenticationRequired, entity.Status,
        entity.ExternalUserId, entity.LastAccessedAt, entity.RevokedAt, entity.RevocationReason,
        entity.ResendCount, entity.LastResentAt, entity.Version, entity.CreatedAt, entity.UpdatedAt);
}

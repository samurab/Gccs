using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gccs.Application.Audit;
using Gccs.Application.Ai;
using Gccs.Application.Common;
using Gccs.Application.Security;
using Gccs.Domain.Audit;
using Gccs.Domain.Cmmc;
using Gccs.Domain.Common;
using Gccs.Domain.Compliance;
using Gccs.Domain.Evidence;

namespace Gccs.Application.Compliance;

public sealed class SspExportPackageService(
    ISspSectionRepository sectionRepository,
    ISspNarrativeRepository narrativeRepository,
    ISspExportSourceRepository sourceRepository,
    ISspExportPackageRepository packageRepository,
    ICurrentTenantContext tenantContext,
    IAuditEventWriter auditEventWriter,
    IApplicationTransaction transaction,
    TimeProvider timeProvider,
    AiOutputReviewService? aiOutputReview = null)
{
    public const string ReviewOnlyDisclaimer =
        "Draft SSP review package for human review only. FeDril does not provide certification, an assessment determination, system authorization, legal advice, or government approval or endorsement. Source records and conclusions must be independently reviewed by qualified personnel.";

    public Task<IReadOnlyList<SspExportPackageDto>> ListAsync(CancellationToken cancellationToken = default) =>
        packageRepository.ListAsync(tenantContext.TenantId, cancellationToken);

    public Task<SspExportPackageDto?> GetAsync(Guid packageId, CancellationToken cancellationToken = default) =>
        packageRepository.GetAsync(tenantContext.TenantId, packageId, cancellationToken);

    public async Task<SspExportPackageDto> GenerateAsync(
        CreateSspExportPackageRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateCreate(request);
        if (request.ExternalShareRequested)
            throw new SspExportPackageValidationException("External sharing is not authorized during package generation. Generate an internal review package, then obtain explicit external-share approval.");

        return await transaction.ExecuteAsync(async transactionToken =>
        {
            if (await packageRepository.PackageVersionExistsAsync(tenantContext.TenantId, request.PackageVersion.Trim(), transactionToken))
                throw new SspExportPackageValidationException("Package version already exists for the current tenant.");

            var sourceSnapshot = await sourceRepository.ResolveAsync(
                tenantContext.TenantId,
                request.EvidenceItemIds.Distinct().ToArray(),
                request.PoamItemIds.Distinct().ToArray(),
                transactionToken);

            var sections = await sectionRepository.ListAsync(tenantContext.TenantId, transactionToken);
            if (sections.Count == 0)
                throw new SspExportPackageValidationException("At least one SSP section is required before export.");

            if (sourceSnapshot.Evidence.Length != request.EvidenceItemIds.Distinct().Count() ||
                sourceSnapshot.PoamItems.Length != request.PoamItemIds.Distinct().Count())
                throw new SspExportPackageValidationException("One or more requested records are unavailable or ineligible for SSP export.");

            var exportedSections = new List<SspExportSectionDto>(sections.Count);
            var approvedNarratives = await narrativeRepository.ListCurrentApprovedNarrativesAsync(
                tenantContext.TenantId,
                sections.Select(section => section.Id).ToArray(),
                transactionToken);
            foreach (var section in sections.OrderBy(item => item.SectionType).ThenBy(item => item.Title))
            {
                approvedNarratives.TryGetValue(section.Id, out var narrative);
                exportedSections.Add(new SspExportSectionDto(
                    section.Id,
                    section.SectionType,
                    section.Title,
                    section.Status,
                    section.Owner,
                    section.Reviewer,
                    section.ReviewDate,
                    section.SourceReferences,
                    narrative?.ApprovedText,
                    narrative?.Id,
                    narrative?.Reviewer,
                    narrative?.ReviewDate,
                    narrative?.SourceRecords ?? []));
            }

            EnsureNoPositiveAssuranceClaims(
            [
                request.SystemBoundary,
                request.Reviewer,
                .. exportedSections.SelectMany(section => new[] { section.Title, section.Owner, section.Reviewer, section.ApprovedNarrativeText }),
                .. exportedSections.SelectMany(section => section.SourceReferences.Select(source => source.Source)),
                .. exportedSections.SelectMany(section => section.NarrativeSources.SelectMany(source => new[] { source.Label, source.Summary })),
                .. sourceSnapshot.Evidence.SelectMany(evidence => new[] { evidence.Title, evidence.OwnerFunction }),
                .. sourceSnapshot.PoamItems.SelectMany(poam => new[] { poam.Weakness, poam.PlannedRemediation, poam.OwnerFunction })
            ]);

            var now = timeProvider.GetUtcNow();
            var packageId = Guid.NewGuid();
            var history = new[]
            {
                new SspExportHistoryDto(Guid.NewGuid(), "Generated", actorUserId, tenantContext.UserEmail, now, "Internal review package generated.")
            };
            var package = new SspExportPackageDto(
                packageId,
                tenantContext.TenantId,
                sourceSnapshot.TenantName,
                now,
                request.PackageVersion.Trim(),
                request.SystemBoundary.Trim(),
                request.Reviewer.Trim(),
                request.Format,
                ReviewOnlyDisclaimer,
                string.Empty,
                default,
                exportedSections.ToArray(),
                sourceSnapshot.Evidence,
                sourceSnapshot.PoamItems,
                SspExportPackageStatus.InternalReview,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                history);
            package = package with
            {
                HumanReadableReport = BuildHumanReadableReport(package),
                MachineReadableMetadata = BuildMachineReadableMetadata(package)
            };

            var saved = await packageRepository.CreateAsync(package, actorUserId, transactionToken);
            await auditEventWriter.WriteAsync(
                tenantContext.TenantId,
                actorUserId,
                AuditAction.Exported,
                "SspExportPackage",
                saved.Id.ToString(),
                "SSP review package was exported for internal review.",
                new Dictionary<string, string>
                {
                    ["packageVersion"] = saved.PackageVersion,
                    ["status"] = saved.Status.ToString(),
                    ["includedSections"] = saved.Sections.Length.ToString(),
                    ["includedEvidence"] = saved.IncludedEvidence.Length.ToString(),
                    ["includedPoamItems"] = saved.PoamReferences.Length.ToString(),
                    ["draftOnly"] = "true"
                },
                transactionToken);
            if (request.AiOutputId is Guid aiOutputId)
                await RequiredAiReview().RequireDeliverableLinkAsync(aiOutputId, saved.TenantId,
                    AiDeliverableType.CustomerDeliverable, saved.Id, actorUserId, transactionToken);
            return saved;
        }, cancellationToken);
    }

    private AiOutputReviewService RequiredAiReview() => aiOutputReview ??
        throw new AiOutputReviewValidationException("aiOutputId", "AI output provenance processing is unavailable.");

    public Task<SspExportPackageDto?> ApproveExternalShareAsync(
        Guid packageId,
        SspExternalShareApprovalRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var reason = NormalizeRequired(request.Reason, "Approval reason", 1_000);
        return transaction.ExecuteAsync(async transactionToken =>
        {
            var approved = await packageRepository.ApproveExternalShareAsync(
                tenantContext.TenantId,
                packageId,
                reason,
                actorUserId,
                tenantContext.UserEmail,
                timeProvider.GetUtcNow(),
                transactionToken);
            if (approved is not null)
                await WriteLifecycleAuditAsync(approved, actorUserId, AuditAction.Approved, "SSP package external sharing was explicitly approved.", transactionToken);
            return approved;
        }, cancellationToken);
    }

    public Task<SspExportPackageDto?> ShareAsync(
        Guid packageId,
        SspExternalShareRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var recipient = NormalizeRequired(request.Recipient, "Recipient", 320);
        var purpose = NormalizeRequired(request.Purpose, "Purpose", 1_000);
        return transaction.ExecuteAsync(async transactionToken =>
        {
            var current = await packageRepository.GetAsync(tenantContext.TenantId, packageId, transactionToken);
            if (current is null) return null;
            if (current.ExternalShareApprovedAt is null || current.Status != SspExportPackageStatus.ExternalShareApproved)
                throw new SspExportPackageValidationException("An external share may be recorded once and requires explicit approval for this package.");

            var shared = await packageRepository.ShareAsync(
                tenantContext.TenantId,
                packageId,
                recipient,
                purpose,
                actorUserId,
                tenantContext.UserEmail,
                timeProvider.GetUtcNow(),
                transactionToken);
            if (shared is not null)
                await WriteLifecycleAuditAsync(shared, actorUserId, AuditAction.Exported, "SSP review package external-share record was created.", transactionToken);
            return shared;
        }, cancellationToken);
    }

    private Task WriteLifecycleAuditAsync(SspExportPackageDto package, Guid actorUserId, AuditAction action, string summary, CancellationToken cancellationToken) =>
        auditEventWriter.WriteAsync(
            tenantContext.TenantId,
            actorUserId,
            action,
            "SspExportPackage",
            package.Id.ToString(),
            summary,
            new Dictionary<string, string> { ["packageVersion"] = package.PackageVersion, ["status"] = package.Status.ToString() },
            cancellationToken);

    private static void ValidateCreate(CreateSspExportPackageRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        NormalizeRequired(request.PackageVersion, "Package version", 80);
        NormalizeRequired(request.SystemBoundary, "System boundary", 4_000);
        NormalizeRequired(request.Reviewer, "Reviewer", 200);
        if (!Enum.IsDefined(request.Format)) throw new SspExportPackageValidationException("A valid export format is required.");
        ValidateIds(request.EvidenceItemIds, "Evidence item IDs");
        ValidateIds(request.PoamItemIds, "POA&M item IDs");
    }

    private static void ValidateIds(Guid[]? ids, string field)
    {
        if (ids is null) throw new SspExportPackageValidationException($"{field} must be an array.");
        if (ids.Length > 500) throw new SspExportPackageValidationException($"{field} cannot contain more than 500 items.");
        if (ids.Any(id => id == Guid.Empty)) throw new SspExportPackageValidationException($"{field} cannot contain empty identifiers.");
        if (ids.Distinct().Count() != ids.Length) throw new SspExportPackageValidationException($"{field} cannot contain duplicates.");
    }

    private static string NormalizeRequired(string? value, string field, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new SspExportPackageValidationException($"{field} is required.");
        var normalized = value.Trim();
        if (normalized.Length > maxLength) throw new SspExportPackageValidationException($"{field} must be {maxLength} characters or fewer.");
        return normalized;
    }

    private static void EnsureNoPositiveAssuranceClaims(IEnumerable<string?> values)
    {
        string[] prohibitedClaims =
        [
            "certified",
            "certification",
            "certified compliant",
            "cmmc certified",
            "is compliant",
            "are compliant",
            "fully compliant",
            "compliance guaranteed",
            "assessment determination",
            "assessor determination",
            "government approved",
            "government-approved",
            "government endorsed",
            "government-endorsed",
            "authorization granted",
            "authorized to handle cui",
            "authorized to process cui",
            "authorized to store cui"
        ];
        if (values.Where(value => !string.IsNullOrWhiteSpace(value)).Any(value =>
                prohibitedClaims.Any(claim => value!.Contains(claim, StringComparison.OrdinalIgnoreCase))))
            throw new SspExportPackageValidationException("SSP package content contains prohibited certification, assessment, authorization, or government-endorsement language.");
    }

    private static string BuildHumanReadableReport(SspExportPackageDto package)
    {
        var report = new StringBuilder();
        report.AppendLine($"FeDril SSP Review Package {package.PackageVersion}");
        report.AppendLine($"Generated: {package.GeneratedAt:O}");
        report.AppendLine($"Tenant: {package.TenantName} ({package.TenantId})");
        report.AppendLine($"System boundary: {package.SystemBoundary}");
        report.AppendLine($"Package reviewer: {package.Reviewer}");
        report.AppendLine($"Status: {package.Status}");
        report.AppendLine();
        report.AppendLine(package.Disclaimer);
        report.AppendLine();
        report.AppendLine("SSP sections");
        foreach (var section in package.Sections)
        {
            report.AppendLine($"- {section.SectionType}: {section.Title} [{section.Status}] — owner {section.Owner}");
            if (!string.IsNullOrWhiteSpace(section.Reviewer)) report.AppendLine($"  Section review: {section.Reviewer} on {section.ReviewDate:yyyy-MM-dd}");
            if (!string.IsNullOrWhiteSpace(section.ApprovedNarrativeText))
            {
                report.AppendLine("  Approved narrative summary:");
                report.AppendLine($"  {section.ApprovedNarrativeText}");
                report.AppendLine($"  Narrative review: {section.NarrativeReviewer} on {section.NarrativeReviewDate:yyyy-MM-dd}");
            }
            foreach (var source in section.SourceReferences)
                report.AppendLine($"  Source: {source.Source} — {source.SourceUrl} (reviewed {source.LastReviewedAt:yyyy-MM-dd})");
            foreach (var source in section.NarrativeSources)
                report.AppendLine($"  Narrative source: {source.SourceType} {source.RecordId} — {source.SourceUrl}");
        }
        report.AppendLine();
        report.AppendLine("Approved evidence references");
        foreach (var evidence in package.IncludedEvidence)
            report.AppendLine($"- {evidence.Id}: {evidence.Title} [{evidence.Classification}] — approved {evidence.ApprovedAt:O}");
        report.AppendLine();
        report.AppendLine("POA&M references");
        foreach (var poam in package.PoamReferences)
            report.AppendLine($"- {poam.Id}: {poam.ControlId} [{poam.Status}] — {poam.Weakness}");
        report.AppendLine();
        report.AppendLine("Package history");
        foreach (var history in package.History)
            report.AppendLine($"- {history.OccurredAt:O}: {history.Action} by {history.ActorName} ({history.ActorUserId}) — {history.Notes}");
        return report.ToString();
    }

    private static JsonElement BuildMachineReadableMetadata(SspExportPackageDto package) =>
        JsonSerializer.SerializeToElement(new
        {
            package.Id,
            package.TenantId,
            package.TenantName,
            package.GeneratedAt,
            package.PackageVersion,
            package.SystemBoundary,
            package.Reviewer,
            package.Format,
            package.Disclaimer,
            DraftOnly = true,
            package.Status,
            package.Sections,
            package.IncludedEvidence,
            package.PoamReferences,
            package.History
        }, JsonOptions);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}

public interface ISspExportSourceRepository
{
    Task<SspExportSourceSnapshot> ResolveAsync(Guid tenantId, Guid[] evidenceItemIds, Guid[] poamItemIds, CancellationToken cancellationToken = default);
}

public interface ISspExportPackageRepository
{
    Task<IReadOnlyList<SspExportPackageDto>> ListAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<SspExportPackageDto?> GetAsync(Guid tenantId, Guid packageId, CancellationToken cancellationToken = default);
    Task<bool> PackageVersionExistsAsync(Guid tenantId, string packageVersion, CancellationToken cancellationToken = default);
    Task<SspExportPackageDto> CreateAsync(SspExportPackageDto package, Guid actorUserId, CancellationToken cancellationToken = default);
    Task<SspExportPackageDto?> ApproveExternalShareAsync(Guid tenantId, Guid packageId, string reason, Guid actorUserId, string actorName, DateTimeOffset approvedAt, CancellationToken cancellationToken = default);
    Task<SspExportPackageDto?> ShareAsync(Guid tenantId, Guid packageId, string recipient, string purpose, Guid actorUserId, string actorName, DateTimeOffset sharedAt, CancellationToken cancellationToken = default);
}

public sealed record CreateSspExportPackageRequest(
    string PackageVersion,
    string SystemBoundary,
    string Reviewer,
    SspExportFormat Format,
    bool ExternalShareRequested,
    Guid[] EvidenceItemIds,
    Guid[] PoamItemIds,
    Guid? AiOutputId = null);
public sealed record SspExternalShareApprovalRequest(string Reason);
public sealed record SspExternalShareRequest(string Recipient, string Purpose);
public sealed record SspExportSourceSnapshot(string TenantName, SspExportEvidenceReferenceDto[] Evidence, SspExportPoamReferenceDto[] PoamItems);
public sealed record SspExportEvidenceReferenceDto(Guid Id, string Title, EvidenceStatus Status, ContentClassification Classification, string OwnerFunction, DateTimeOffset ApprovedAt, Guid ApprovedByUserId, DateOnly? EffectiveAt, DateOnly? ExpiresAt);
public sealed record SspExportPoamReferenceDto(Guid Id, Guid AssessmentId, string ControlId, string Weakness, string PlannedRemediation, PoamStatus Status, string OwnerFunction, DateOnly TargetCompletionAt);
public sealed record SspExportSectionDto(Guid SectionId, SspSectionType SectionType, string Title, SspSectionStatus Status, string Owner, string? Reviewer, DateOnly? ReviewDate, SspSourceReferenceDto[] SourceReferences, string? ApprovedNarrativeText, Guid? ApprovedNarrativeId, string? NarrativeReviewer, DateOnly? NarrativeReviewDate, SspNarrativeSourceRecordDto[] NarrativeSources);
public sealed record SspExportHistoryDto(Guid Id, string Action, Guid ActorUserId, string ActorName, DateTimeOffset OccurredAt, string? Notes);
public sealed record SspExportPackageDto(
    Guid Id,
    Guid TenantId,
    string TenantName,
    DateTimeOffset GeneratedAt,
    string PackageVersion,
    string SystemBoundary,
    string Reviewer,
    SspExportFormat Format,
    string Disclaimer,
    string HumanReadableReport,
    JsonElement MachineReadableMetadata,
    SspExportSectionDto[] Sections,
    SspExportEvidenceReferenceDto[] IncludedEvidence,
    SspExportPoamReferenceDto[] PoamReferences,
    SspExportPackageStatus Status,
    Guid? ExternalShareApprovedByUserId,
    DateTimeOffset? ExternalShareApprovedAt,
    string? ExternalShareApprovalReason,
    Guid? SharedByUserId,
    DateTimeOffset? SharedAt,
    string? SharedRecipient,
    string? SharedPurpose,
    SspExportHistoryDto[] History);

public enum SspExportFormat { HumanReadable, MachineReadable, Both }
public enum SspExportPackageStatus { InternalReview, ExternalShareApproved, Shared }

public sealed class SspExportPackageValidationException(string message) : InvalidOperationException(message);

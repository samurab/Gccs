using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Domain.Audit;
using System.Net;
using System.Text.Json;

namespace Gccs.Application.Reports;

public sealed class EsrsReportPackageService(
    SubcontractingReportDataService reportDataService,
    IEsrsReportPackageRepository repository,
    ISprSubmissionProvider submissionProvider,
    IAuditEventWriter auditEventWriter,
    IApplicationTransaction transaction)
{
    public const string NotSubmittedDisclaimer =
        "FeDril has not submitted this report to SAM.gov. This package is preparation-only for customer review and manual entry in SAM.gov Subcontracting Plan Reporting (SPR).";

    public async Task<EsrsReportPackageDto> GenerateAsync(
        EsrsReportPackageGenerateRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (request.ContractId == Guid.Empty) throw new EsrsReportPackageException("Contract is required.");
        if (!Enum.IsDefined(request.ReportType)) throw new EsrsReportPackageException("Report type must be ISR or SSR.");
        if (request.PeriodStart == default || request.PeriodEnd == default || request.PeriodEnd < request.PeriodStart)
            throw new EsrsReportPackageException("A valid package period is required.");
        var packageRows = await reportDataService.PreparePackageRowsAsync(
            new SubcontractingReportPackageRowsRequest(
                request.ContractId,
                request.ReportType,
                request.PeriodStart,
                request.PeriodEnd,
                FinalPackage: true),
            cancellationToken);
        return await transaction.ExecuteAsync(async token =>
        {
            var package = await repository.CreateAsync(request, BuildSnapshot(request, packageRows), actorUserId, token);
            await WriteAuditAsync(package, actorUserId, AuditAction.Created, "SAM.gov SPR preparation package was generated.", token);
            return package;
        }, cancellationToken);
    }

    public async Task<EsrsReportPackageDto?> FindAsync(
        Guid packageId,
        CancellationToken cancellationToken = default)
        => await repository.FindAsync(packageId, cancellationToken);

    public async Task<IReadOnlyList<EsrsReportPackageDto>> ListAsync(CancellationToken cancellationToken = default) =>
        await repository.ListAsync(cancellationToken);

    public async Task<EsrsReportPackageDto?> ApproveAsync(
        Guid packageId,
        EsrsReportPackageReviewRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        return await ChangeStatusAsync(packageId, EsrsReportPackageStatus.Approved, request, actorUserId,
            AuditAction.Approved, "SAM.gov SPR preparation package was approved.", cancellationToken);
    }

    public async Task<EsrsReportPackageDto?> BeginReviewAsync(
        Guid packageId,
        EsrsReportPackageReviewRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        return await ChangeStatusAsync(packageId, EsrsReportPackageStatus.InReview, request, actorUserId,
            AuditAction.Updated, "SAM.gov SPR preparation package review was started.", cancellationToken);
    }

    public async Task<EsrsReportPackageDto?> SupersedeAsync(
        Guid packageId,
        EsrsReportPackageReviewRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        return await ChangeStatusAsync(packageId, EsrsReportPackageStatus.Superseded, request, actorUserId,
            AuditAction.Updated, "SAM.gov SPR preparation package was superseded.", cancellationToken);
    }

    public async Task<EsrsReportPackageDto?> ArchiveAsync(
        Guid packageId,
        EsrsReportPackageReviewRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        return await ChangeStatusAsync(packageId, EsrsReportPackageStatus.Archived, request, actorUserId,
            AuditAction.Archived, "SAM.gov SPR preparation package was archived.", cancellationToken);
    }

    public async Task<SprPackageExportDto?> ExportAsync(Guid packageId, SprPackageExportFormat format,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        return await transaction.ExecuteAsync(async token =>
        {
            var package = await repository.FindAsync(packageId, token);
            if (package is null) return null;
            var content = format == SprPackageExportFormat.Json
                ? JsonSerializer.Serialize(package, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true })
                : BuildHtml(package);
            var export = new SprPackageExportDto(package.Id, format, format == SprPackageExportFormat.Json ? "application/json" : "text/html",
                $"sam-gov-spr-{package.ContractId:N}-v{package.Version}.{(format == SprPackageExportFormat.Json ? "json" : "html")}", content,
                NotSubmittedDisclaimer);
            await auditEventWriter.WriteAsync(package.TenantId, actorUserId, AuditAction.Exported, "EsrsReportPackage", package.Id.ToString(),
                "SAM.gov SPR preparation package was exported.", new Dictionary<string, string>
                {
                    ["format"] = format.ToString(), ["version"] = package.Version.ToString(), ["contractId"] = package.ContractId.ToString()
                }, token);
            return export;
        }, cancellationToken);
    }

    public async Task<SprManualSubmissionReceiptDto?> RecordManualSubmissionReceiptAsync(Guid packageId,
        SprManualSubmissionReceiptRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        if (request.SubmittedAt == default || request.SubmittedAt > DateTimeOffset.UtcNow.AddMinutes(5))
            throw new EsrsReportPackageException("A valid external submission date that is not in the future is required.");
        if (!Enum.IsDefined(request.Outcome))
            throw new EsrsReportPackageException("A supported external submission outcome is required.");
        if (string.IsNullOrWhiteSpace(request.ConfirmationReference) || request.ConfirmationReference.Length > 200)
            throw new EsrsReportPackageException("A SAM.gov confirmation or reference of 200 characters or fewer is required.");
        if (request.Notes?.Length > 2_000) throw new EsrsReportPackageException("Submission notes cannot exceed 2,000 characters.");
        if (request.Outcome is (SprManualSubmissionOutcome.Rejected or SprManualSubmissionOutcome.Corrected) && string.IsNullOrWhiteSpace(request.Notes))
            throw new EsrsReportPackageException("Notes are required for rejected or corrected external outcomes.");
        return await transaction.ExecuteAsync(async token =>
        {
            var package = await repository.FindAsync(packageId, token);
            if (package is null) return null;
            if (package.Status != EsrsReportPackageStatus.Approved)
                throw new EsrsReportPackageException("Only an approved preparation package can have a manual SAM.gov submission receipt.");
            var receipt = await repository.CreateManualSubmissionReceiptAsync(packageId, request, actorUserId, token);
            await auditEventWriter.WriteAsync(package.TenantId, actorUserId, AuditAction.Created, "SprManualSubmissionReceipt", receipt.Id.ToString(),
                "A user-recorded external SAM.gov submission receipt was added; FeDril did not perform or verify the submission.",
                new Dictionary<string, string> { ["packageId"] = package.Id.ToString(), ["outcome"] = receipt.Outcome.ToString(),
                    ["hasEvidence"] = (receipt.EvidenceItemId is not null).ToString(), ["supersedesReceiptId"] = receipt.SupersedesReceiptId?.ToString() ?? string.Empty }, token);
            return receipt;
        }, cancellationToken);
    }

    public Task<IReadOnlyList<SprManualSubmissionReceiptDto>> ListManualSubmissionReceiptsAsync(Guid packageId,
        CancellationToken cancellationToken = default) =>
        repository.ListManualSubmissionReceiptsAsync(packageId, cancellationToken);

    public SprSubmissionCapabilityDto GetSubmissionCapability() => new(false,
        "No authorized contractor-facing SAM.gov SPR submission provider is configured. FeDril supports preparation, export, and user-recorded receipts only.");

    public async Task SubmitAsync(Guid packageId, SprSubmissionRequest request, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Length > 128)
            throw new EsrsReportPackageException("An idempotency key of 128 characters or fewer is required.");
        var package = await repository.FindAsync(packageId, cancellationToken) ?? throw new EsrsReportPackageException("The SPR package was not found.");
        if (package.Status != EsrsReportPackageStatus.Approved) throw new EsrsReportPackageException("Only approved packages can be submitted.");
        await submissionProvider.SubmitAsync(new SprSubmissionPayload(package, request.IdempotencyKey, actorUserId), cancellationToken);
    }

    private async Task<EsrsReportPackageDto?> ChangeStatusAsync(Guid packageId, EsrsReportPackageStatus status,
        EsrsReportPackageReviewRequest request, Guid actorUserId, AuditAction action, string summary, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ReviewerName) || request.ReviewerName.Length > 200)
            throw new EsrsReportPackageException("Reviewer display name is required and cannot exceed 200 characters.");
        if (status is EsrsReportPackageStatus.Superseded or EsrsReportPackageStatus.Archived && string.IsNullOrWhiteSpace(request.ReviewNotes))
            throw new EsrsReportPackageException("Review notes are required when superseding or archiving a package.");
        return await transaction.ExecuteAsync(async token =>
        {
            var existing = await repository.FindAsync(packageId, token);
            if (existing is null) return null;
            if (status == EsrsReportPackageStatus.Approved && existing.Snapshot.RowCount == 0)
                throw new EsrsReportPackageException("A package with no eligible report data rows cannot be approved.");
            EnsureTransition(existing.Status, status);
            var updated = await repository.UpdateStatusAsync(packageId, existing.Status, status, request.ReviewerName, request.ReviewNotes, actorUserId, token);
            if (updated is not null) await WriteAuditAsync(updated, actorUserId, action, summary, token);
            return updated;
        }, cancellationToken);
    }

    private static void EnsureTransition(EsrsReportPackageStatus current, EsrsReportPackageStatus requested)
    {
        var allowed = current switch
        {
            EsrsReportPackageStatus.Draft => requested is EsrsReportPackageStatus.InReview or EsrsReportPackageStatus.Archived,
            EsrsReportPackageStatus.InReview => requested is EsrsReportPackageStatus.Approved or EsrsReportPackageStatus.Archived,
            EsrsReportPackageStatus.Approved => requested is EsrsReportPackageStatus.Superseded or EsrsReportPackageStatus.Archived,
            EsrsReportPackageStatus.Superseded => requested is EsrsReportPackageStatus.Archived,
            _ => false
        };
        if (!allowed) throw new EsrsReportPackageException($"SPR package transition from {current} to {requested} is not allowed.");
    }

    private static EsrsReportPackageSnapshotDto BuildSnapshot(
        EsrsReportPackageGenerateRequest request,
        IReadOnlyList<SubcontractingReportPackageRowDto> packageRows)
    {
        var spendSummaries = packageRows
            .GroupBy(row => row.SocioeconomicCategory)
            .Select(group => new EsrsSpendSummaryDto(
                group.Key,
                group.Sum(row => row.Amount),
                group.Select(row => row.SubcontractorId).Distinct().Count()))
            .OrderBy(summary => summary.SocioeconomicCategory)
            .ToArray();
        var evidenceReferences = packageRows
            .SelectMany(row => row.SupportingEvidenceItemIds.Select(evidenceId => new EsrsPackageEvidenceReferenceDto(row.RowId, evidenceId)))
            .ToArray();
        var exceptions = packageRows.Count == 0
            ? new[] { "No reviewed or accepted subcontracting report data rows were available for this period." }
            : packageRows.Where(row => row.SupportingEvidenceItemIds.Count == 0)
                .Select(row => $"Row {row.RowId} has no supporting evidence.")
                .ToArray();

        var schemaProfiles = packageRows.Select(row => row.SchemaProfile).Distinct().OrderBy(profile => profile.Version).ToArray();
        return new EsrsReportPackageSnapshotDto(
            request.ContractId,
            request.ReportType,
            request.PeriodStart,
            request.PeriodEnd,
            packageRows.Count,
            packageRows.Sum(row => row.Amount),
            spendSummaries,
            evidenceReferences,
            exceptions,
            schemaProfiles);
    }

    private static string BuildHtml(EsrsReportPackageDto package)
    {
        static string E(string value) => WebUtility.HtmlEncode(value);
        var summaries = string.Join("", package.Snapshot.SpendSummaries.Select(item =>
            $"<tr><td>{E(item.SocioeconomicCategory)}</td><td>{item.SubcontractorCount}</td><td>{item.TotalSpend:0}</td></tr>"));
        var exceptions = string.Join("", package.Snapshot.Exceptions.Select(item => $"<li>{E(item)}</li>"));
        var evidence = string.Join("", package.Snapshot.EvidenceReferences.Select(item =>
            $"<tr><td>{item.RowId}</td><td>{item.EvidenceItemId}</td></tr>"));
        var schemas = string.Join("", package.Snapshot.SchemaProfiles.Select(profile =>
            $"<tr><td>{E(profile.Id)}</td><td>{E(profile.Version)}</td><td><a href=\"{E(profile.SourceUrl)}\">{E(profile.SourceUrl)}</a></td><td>{E(profile.DefinitionSha256)}</td></tr>"));
        var review = package.Status == EsrsReportPackageStatus.Approved
            ? $"<h2>Approval</h2><p>Reviewer: {E(package.ReviewerName ?? "Unknown")}</p><p>Approved: {E(package.ApprovedAt?.ToString("O") ?? "Unknown")}</p><p>Review notes: {E(package.ReviewNotes ?? "None")}</p>"
            : $"<h2>Review</h2><p>Status: {E(package.Status.ToString())}</p><p>Reviewer: {E(package.ReviewerName ?? "Not assigned")}</p><p>Review notes: {E(package.ReviewNotes ?? "None")}</p>";
        return $"<!doctype html><html><head><meta charset=\"utf-8\"><title>SAM.gov SPR preparation package</title></head><body>" +
            $"<h1>SAM.gov SPR preparation package</h1><p>{E(NotSubmittedDisclaimer)}</p><p>Package {package.Id}; version {package.Version}; generated {package.GeneratedAt:O}</p>" +
            $"<h2>Report scope</h2><p>Contract: {package.ContractId}</p><p>Report type: {E(package.ReportType.ToString().ToUpperInvariant())}</p>" +
            $"<p>Period: {package.PeriodStart:yyyy-MM-dd} through {package.PeriodEnd:yyyy-MM-dd}</p><p>Rows: {package.Snapshot.RowCount}; total whole-dollar spend: {package.Snapshot.TotalSpend:0}</p>" +
            $"<h2>Spend summaries</h2><table><thead><tr><th>Category</th><th>Subcontractors</th><th>Whole-dollar spend</th></tr></thead><tbody>{summaries}</tbody></table>" +
            $"<h2>Exceptions</h2><ul>{exceptions}</ul><h2>Evidence references</h2><table><thead><tr><th>Report data row</th><th>Evidence item</th></tr></thead><tbody>{evidence}</tbody></table>" +
            $"<h2>Schema provenance</h2><table><thead><tr><th>Profile</th><th>Version</th><th>Source</th><th>Definition SHA-256</th></tr></thead><tbody>{schemas}</tbody></table>{review}</body></html>";
    }

    private async Task WriteAuditAsync(
        EsrsReportPackageDto package,
        Guid actorUserId,
        AuditAction action,
        string summary,
        CancellationToken cancellationToken)
    {
        await auditEventWriter.WriteAsync(
            package.TenantId,
            actorUserId,
            action,
            "EsrsReportPackage",
            package.Id.ToString(),
            summary,
            new Dictionary<string, string>
            {
                ["contractId"] = package.ContractId.ToString(),
                ["reportType"] = package.ReportType.ToString(),
                ["status"] = package.Status.ToString(),
                ["version"] = package.Version.ToString(),
                ["totalSpend"] = package.Snapshot.TotalSpend.ToString("0.00"),
                ["schemaVersions"] = string.Join(',', package.Snapshot.SchemaProfiles.Select(profile => profile.Version))
            },
            cancellationToken);
    }
}

public interface IEsrsReportPackageRepository
{
    Task<EsrsReportPackageDto> CreateAsync(
        EsrsReportPackageGenerateRequest request,
        EsrsReportPackageSnapshotDto snapshot,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<EsrsReportPackageDto?> FindAsync(Guid packageId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EsrsReportPackageDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<EsrsReportPackageDto?> UpdateStatusAsync(
        Guid packageId,
        EsrsReportPackageStatus expectedStatus,
        EsrsReportPackageStatus status,
        string reviewerName,
        string? reviewNotes,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
    Task<SprManualSubmissionReceiptDto> CreateManualSubmissionReceiptAsync(Guid packageId, SprManualSubmissionReceiptRequest request,
        Guid actorUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SprManualSubmissionReceiptDto>> ListManualSubmissionReceiptsAsync(Guid packageId, CancellationToken cancellationToken = default);
}

public sealed record EsrsReportPackageGenerateRequest(
    Guid ContractId,
    EsrsReportType ReportType,
    DateOnly PeriodStart,
    DateOnly PeriodEnd);

public sealed record EsrsReportPackageReviewRequest(
    string ReviewerName,
    string? ReviewNotes);

public sealed record EsrsReportPackageDto(
    Guid Id,
    Guid TenantId,
    Guid ContractId,
    EsrsReportType ReportType,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    EsrsReportPackageStatus Status,
    int Version,
    string NotSubmittedDisclaimer,
    EsrsReportPackageSnapshotDto Snapshot,
    string? ReviewerName,
    DateTimeOffset? ApprovedAt,
    string? ReviewNotes,
    DateTimeOffset GeneratedAt,
    DateTimeOffset? UpdatedAt,
    Guid? ReviewerUserId = null);

public sealed record EsrsReportPackageSnapshotDto(
    Guid ContractId,
    EsrsReportType ReportType,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    int RowCount,
    decimal TotalSpend,
    IReadOnlyList<EsrsSpendSummaryDto> SpendSummaries,
    IReadOnlyList<EsrsPackageEvidenceReferenceDto> EvidenceReferences,
    IReadOnlyList<string> Exceptions,
    IReadOnlyList<SprSchemaReferenceDto> SchemaProfiles);

public sealed record EsrsSpendSummaryDto(
    string SocioeconomicCategory,
    decimal TotalSpend,
    int SubcontractorCount);

public sealed record EsrsPackageEvidenceReferenceDto(
    Guid RowId,
    Guid EvidenceItemId);

public enum EsrsReportPackageStatus
{
    Draft,
    InReview,
    Approved,
    Superseded,
    Archived
}

public sealed record SprManualSubmissionReceiptRequest(DateTimeOffset SubmittedAt, string ConfirmationReference,
    SprManualSubmissionOutcome Outcome, string? Notes, Guid? EvidenceItemId, Guid? SupersedesReceiptId = null);
public sealed record SprManualSubmissionReceiptDto(Guid Id, Guid TenantId, Guid PackageId, DateTimeOffset SubmittedAt,
    string ConfirmationReference, SprManualSubmissionOutcome Outcome, string? Notes, Guid? EvidenceItemId,
    Guid? SupersedesReceiptId, Guid RecordedByUserId, DateTimeOffset RecordedAt);
public enum SprManualSubmissionOutcome { Submitted, Accepted, Rejected, Corrected }
public enum SprPackageExportFormat { Html, Json }
public sealed record SprPackageExportDto(Guid PackageId, SprPackageExportFormat Format, string ContentType, string FileName, string Content, string Disclaimer);
public sealed record SprSubmissionCapabilityDto(bool Enabled, string Reason);
public sealed record SprSubmissionRequest(string IdempotencyKey);
public sealed record SprSubmissionPayload(EsrsReportPackageDto Package, string IdempotencyKey, Guid ActorUserId);

public interface ISprSubmissionProvider
{
    Task SubmitAsync(SprSubmissionPayload payload, CancellationToken cancellationToken = default);
}

public sealed class DisabledSprSubmissionProvider : ISprSubmissionProvider
{
    public Task SubmitAsync(SprSubmissionPayload payload, CancellationToken cancellationToken = default) =>
        throw new SprSubmissionUnavailableException(
            "SAM.gov SPR submission is unavailable because no authorized contractor-facing provider is configured.");
}

public sealed class SprSubmissionUnavailableException(string message) : InvalidOperationException(message);
public sealed class EsrsReportPackageConflictException(string message) : InvalidOperationException(message);

public sealed class EsrsReportPackageException(string message) : InvalidOperationException(message);

using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Application.Security;
using Gccs.Domain.Audit;

namespace Gccs.Application.Tenancy;

public sealed record CuiReadinessEvidenceDetails(
    IReadOnlyList<SecurityReviewChecklistItemDto>? SecurityItems = null,
    IReadOnlyList<SecurityReviewFindingDto>? SecurityFindings = null,
    IReadOnlyList<AcceptedSecurityRiskDto>? AcceptedRisks = null,
    IReadOnlyList<IncidentResponsePlaybookDto>? Playbooks = null,
    IReadOnlyList<IncidentResponseGapDto>? IncidentGaps = null,
    IncidentReadinessTabletopDto? Tabletop = null,
    BackupRestoreVerificationDto? BackupRestore = null,
    string? SupportOwner = null, string? EscalationContact = null,
    string? RunbookReference = null, string? Coverage = null);

public sealed record RecordCuiReadinessEvidenceRequest(string Kind, int ExpectedVersion,
    DateTimeOffset ExpiresAt, string SourceReference, string ReviewNotes,
    CuiReadinessEvidenceDetails Details, bool Rejected = false);

public sealed record CuiReadinessEvidenceDto(Guid Id, Guid TenantId, string Kind, int Version,
    string State, DateTimeOffset ReviewedAt, Guid ReviewedByUserId, DateTimeOffset ExpiresAt,
    string SourceReference, string ReviewNotes, CuiReadinessEvidenceDetails Details);

public sealed record CuiReadinessSupportingRecord(Guid Id, string Kind, string Version, string Title);

public interface ICuiReadinessEvidenceRepository
{
    Task LockTenantAsync(Guid tenantId, CancellationToken ct);
    Task<IReadOnlyList<CuiReadinessEvidenceDto>> ListAsync(Guid tenantId, CancellationToken ct);
    Task<CuiReadinessEvidenceDto> RecordAsync(Guid tenantId, Guid actor, RecordCuiReadinessEvidenceRequest request, CancellationToken ct);
    Task<IReadOnlyList<CuiReadinessSupportingRecord>> SourcesAsync(Guid tenantId, CancellationToken ct);
    Task<string?> ValidateLinksAsync(Guid tenantId, CuiReadyApprovalChecklistDto checklist, CancellationToken ct);
}

public sealed class CuiReadinessEvidenceService(ICuiReadinessEvidenceRepository repository,
    ICurrentTenantContext context, IApplicationTransaction transaction, IAuditEventWriter audit)
{
    public Task<IReadOnlyList<CuiReadinessEvidenceDto>> ListAsync(CancellationToken ct) => repository.ListAsync(context.TenantId, ct);
    public Task<IReadOnlyList<CuiReadinessSupportingRecord>> SourcesAsync(CancellationToken ct) => repository.SourcesAsync(context.TenantId, ct);

    public Task<CuiReadinessEvidenceDto> RecordAsync(RecordCuiReadinessEvidenceRequest request, CancellationToken ct) =>
        transaction.ExecuteAsync(async token =>
        {
            await repository.LockTenantAsync(context.TenantId, token);
            var now = DateTimeOffset.UtcNow;
            if (request.ExpectedVersion < 0 || request.Details is null || System.Text.Json.JsonSerializer.Serialize(request.Details).Length > 64000 ||
                string.IsNullOrWhiteSpace(request.SourceReference) || request.SourceReference.Length > 600 ||
                string.IsNullOrWhiteSpace(request.ReviewNotes) || request.ReviewNotes.Length > 1200 ||
                request.ExpiresAt <= now || request.ExpiresAt > now.AddYears(1))
                throw new CuiReadyApprovalChecklistValidationException("Evidence requires a source, review notes, current version, and expiry within one year.");
            if (!Kinds.Contains(request.Kind))
                throw new CuiReadyApprovalChecklistValidationException("Unsupported readiness evidence kind.");
            if (!request.Rejected && Validate(request.Kind, request.Details, now) is { } error)
                throw new CuiReadyApprovalChecklistValidationException(error);
            var result = await repository.RecordAsync(context.TenantId, context.UserId, request, token);
            await audit.WriteAsync(context.TenantId, context.UserId, request.Rejected ? AuditAction.Rejected : AuditAction.Approved,
                "CuiReadinessEvidence", result.Id.ToString(), "Readiness evidence version recorded.",
                new Dictionary<string, string> { ["kind"] = result.Kind, ["version"] = result.Version.ToString(), ["state"] = result.State }, token);
            return result;
        }, ct);

    public static readonly string[] Kinds = ["security-review", "incident-response", "backup-restore", "support-escalation"];

    public static string? Validate(string kind, CuiReadinessEvidenceDetails details, DateTimeOffset now)
    {
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        bool Current(DateOnly date) => date <= today && date > today.AddYears(-1);
        switch (kind)
        {
            case "security-review":
                if (details.SecurityItems is not { Count: > 0 } items || details.SecurityFindings is null || details.AcceptedRisks is null ||
                    items.Any(i => i is null) || details.SecurityFindings.Any(f => f is null) || details.AcceptedRisks.Any(r => r is null) ||
                    SecurityReviewChecklist.ValidateItems(items).Count != 0 || items.Select(i => i.Area).Distinct().Count() != items.Count ||
                    items.Any(i => i.Status is not (SecurityReviewItemStatus.Passed or SecurityReviewItemStatus.AcceptedRisk) ||
                        i.ReviewerUserId is null || i.ReviewerUserId == Guid.Empty || i.ReviewedAt is null || !Current(i.ReviewedAt.Value)) ||
                    details.SecurityFindings.Any(f => !Enum.IsDefined(f.Status) || !Enum.IsDefined(f.Severity)) ||
                    SecurityReviewChecklist.BlocksCuiReadyApproval(details.SecurityFindings) ||
                    details.AcceptedRisks.Any(r => SecurityReviewChecklist.ValidateAcceptedRisk(r).Count != 0 || !Current(r.AcceptedAt) ||
                        r.ExpiresAt <= today || r.ReviewAt <= today) ||
                    ((items.Any(i => i.Status == SecurityReviewItemStatus.AcceptedRisk) || details.SecurityFindings.Any(f => f.Status == SecurityReviewFindingStatus.AcceptedRisk)) && details.AcceptedRisks.Count == 0))
                    return "Security evidence is incomplete, stale, or has blocking findings or expired accepted risks.";
                break;
            case "incident-response":
                if (details.Playbooks is not { Count: > 0 } playbooks || details.IncidentGaps is null || details.Tabletop is null ||
                    playbooks.Any(p => p is null || p.ContainmentSteps is null || p.EvidenceToCollect is null) || details.IncidentGaps.Any(g => g is null) ||
                    details.Tabletop.Participants is null || details.Tabletop.Findings is null || details.Tabletop.FollowUpActions is null ||
                    IncidentResponseReadiness.ValidatePlaybooks(playbooks).Count != 0 ||
                    IncidentResponseReadiness.ValidateTabletop(details.Tabletop).Count != 0 || !Current(details.Tabletop.TabletopDate) ||
                    details.IncidentGaps.Any(g => !Enum.IsDefined(g.Status) || !Enum.IsDefined(g.Severity) || g.Status == IncidentResponseGapStatus.AcceptedRisk) ||
                    IncidentResponseReadiness.BlocksCuiReadyApproval(details.IncidentGaps))
                    return "Incident readiness requires current playbooks and tabletop evidence without critical open gaps or unreviewed risk exceptions.";
                break;
            case "backup-restore":
                if (details.BackupRestore is null || TechnicalControlVerification.ValidateBackupRestore(details.BackupRestore).Count != 0 ||
                    !Current(details.BackupRestore.VerifiedAt) || details.BackupRestore.Result != "Passed")
                    return "Backup/restore evidence requires a current, passed restore verification.";
                break;
            case "support-escalation":
                if (string.IsNullOrWhiteSpace(details.SupportOwner) || string.IsNullOrWhiteSpace(details.EscalationContact) ||
                    string.IsNullOrWhiteSpace(details.RunbookReference) || string.IsNullOrWhiteSpace(details.Coverage))
                    return "Support evidence requires an owner, escalation contact, runbook, and coverage.";
                break;
            default: return "Unsupported readiness evidence kind.";
        }
        return null;
    }
}

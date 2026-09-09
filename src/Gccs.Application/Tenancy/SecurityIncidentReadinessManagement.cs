using Gccs.Application.Audit;
using Gccs.Application.Common;
using Gccs.Application.Security;
using Gccs.Domain.Audit;

namespace Gccs.Application.Tenancy;

public sealed record SecurityReviewRecordDto(Guid Id, Guid TenantId, int Version, string State,
    DateTimeOffset CreatedAt, Guid CreatedByUserId, DateTimeOffset? ApprovedAt, Guid? ApprovedByUserId,
    string? ApprovalNotes, IReadOnlyList<SecurityReviewChecklistItemDto> Items,
    IReadOnlyList<SecurityReviewFindingRecordDto> Findings, IReadOnlyList<AcceptedSecurityRiskRecordDto> AcceptedRisks);
public sealed record SecurityReviewFindingRecordDto(Guid Id, string Area, SecurityReviewFindingSeverity Severity,
    SecurityReviewFindingStatus Status, string Summary, string RemediationOwner, DateOnly? DueAt, string? ClosureNotes);
public sealed record AcceptedSecurityRiskRecordDto(Guid Id, Guid? FindingId, Guid ApproverUserId, DateOnly AcceptedAt,
    string Scope, DateOnly? ExpiresAt, DateOnly? ReviewAt, string MitigationNote);
public sealed record SaveSecurityReviewRequest(int ExpectedVersion, IReadOnlyList<SecurityReviewChecklistItemDto> Items,
    IReadOnlyList<SaveSecurityReviewFindingRequest> Findings, IReadOnlyList<SaveAcceptedSecurityRiskRequest> AcceptedRisks);
public sealed record SaveSecurityReviewFindingRequest(Guid? Id, string Area, SecurityReviewFindingSeverity Severity,
    SecurityReviewFindingStatus Status, string Summary, string RemediationOwner, DateOnly? DueAt, string? ClosureNotes);
public sealed record SaveAcceptedSecurityRiskRequest(Guid? Id, Guid? FindingId, Guid ApproverUserId, DateOnly AcceptedAt,
    string Scope, DateOnly? ExpiresAt, DateOnly? ReviewAt, string MitigationNote);

public sealed record TechnicalReadinessRecordDto(Guid Id, Guid TenantId, int Version, string State,
    DateTimeOffset CreatedAt, Guid CreatedByUserId, DateTimeOffset? ApprovedAt, Guid? ApprovedByUserId,
    string? ApprovalNotes, IReadOnlyList<ExecutedControlEvidenceDto> Evidence);
public sealed record ExecutedControlEvidenceDto(Guid Id, string ControlType, DateOnly ExecutedAt, string Environment,
    Guid ReviewerUserId, string Result, string EvidenceReference, DateOnly? ExpiresAt, string Notes,
    string EvidenceSourceType = "Legacy", Guid? EvidenceFileVersionId = null, string? ExternalUri = null, string? Sha256Digest = null);
public sealed record SaveTechnicalReadinessRequest(int ExpectedVersion, IReadOnlyList<SaveExecutedControlEvidenceRequest> Evidence);
public sealed record SaveExecutedControlEvidenceRequest(Guid? Id, string ControlType, DateOnly ExecutedAt, string Environment,
    Guid ReviewerUserId, string Result, string EvidenceReference, DateOnly? ExpiresAt, string Notes,
    string EvidenceSourceType = "Legacy", Guid? EvidenceFileVersionId = null, string? ExternalUri = null, string? Sha256Digest = null);

public sealed record IncidentReadinessRecordDto(Guid Id, Guid TenantId, int Version, string State,
    DateTimeOffset CreatedAt, Guid CreatedByUserId, DateTimeOffset? ApprovedAt, Guid? ApprovedByUserId,
    string? ApprovalNotes, DateOnly ReviewDueAt, string ReviewBasis, IReadOnlyList<IncidentContactRecordDto> Contacts,
    IReadOnlyList<IncidentPlaybookRecordDto> Playbooks,
    IReadOnlyList<IncidentTabletopRecordDto> Tabletops, IReadOnlyList<IncidentFollowUpRecordDto> FollowUps);
public sealed record IncidentPlaybookRecordDto(Guid Id, string Key, string Trigger, IReadOnlyList<string> ContainmentSteps,
    string NotificationPath, IReadOnlyList<string> EvidenceToCollect, string Owner, string ClosureCriteria);
public sealed record IncidentTabletopRecordDto(Guid Id, DateOnly ExecutedAt, string Environment,
    IReadOnlyList<string> Participants, IReadOnlyList<string> Findings, string EvidenceReference, Guid ReviewerUserId,
    string EvidenceSourceType = "Legacy", Guid? EvidenceFileVersionId = null, string? ExternalUri = null, string? Sha256Digest = null);
public sealed record IncidentFollowUpRecordDto(Guid Id, Guid TabletopId, SecurityReviewFindingSeverity Severity,
    IncidentResponseGapStatus Status, string Summary, string Owner, DateOnly DueAt, string? ClosureNotes);
public sealed record IncidentContactRecordDto(Guid Id, string Function, string Contact, string EscalationRole);
public sealed record SaveIncidentReadinessRequest(int ExpectedVersion, DateOnly ReviewDueAt, string ReviewBasis,
    IReadOnlyList<SaveIncidentContactRequest> Contacts, IReadOnlyList<SaveIncidentPlaybookRequest> Playbooks,
    IReadOnlyList<SaveIncidentTabletopRequest> Tabletops, IReadOnlyList<SaveIncidentFollowUpRequest> FollowUps);
public sealed record SaveIncidentPlaybookRequest(Guid? Id, string Key, string Trigger, IReadOnlyList<string> ContainmentSteps,
    string NotificationPath, IReadOnlyList<string> EvidenceToCollect, string Owner, string ClosureCriteria);
public sealed record SaveIncidentTabletopRequest(Guid? Id, DateOnly ExecutedAt, string Environment,
    IReadOnlyList<string> Participants, IReadOnlyList<string> Findings, string EvidenceReference, Guid ReviewerUserId,
    string EvidenceSourceType = "Legacy", Guid? EvidenceFileVersionId = null, string? ExternalUri = null, string? Sha256Digest = null);
public sealed record SaveIncidentFollowUpRequest(Guid? Id, Guid TabletopId, SecurityReviewFindingSeverity Severity,
    IncidentResponseGapStatus Status, string Summary, string Owner, DateOnly DueAt, string? ClosureNotes);
public sealed record SaveIncidentContactRequest(Guid? Id, string Function, string Contact, string EscalationRole);
public sealed record ApproveReadinessRecordRequest(int ExpectedVersion, string Notes);
public sealed record ReadinessHistoryDto(Guid Id, string RecordType, Guid RecordId, int Version, string Action,
    Guid ActorUserId, DateTimeOffset OccurredAt, string Summary);
public sealed record ReadinessReleaseSummaryDto(IReadOnlyList<string> PassedChecks,
    IReadOnlyList<SecurityReviewFindingRecordDto> OpenFindings, IReadOnlyList<AcceptedSecurityRiskRecordDto> AcceptedRisks,
    IReadOnlyList<IncidentFollowUpRecordDto> OpenIncidentGaps, string ReleaseRecommendation);
public sealed record ReadinessEvidenceOptionDto(Guid EvidenceFileVersionId, Guid EvidenceItemId, int VersionNumber,
    string Title, string FileName, string Sha256Digest, DateTimeOffset UploadedAt);

public interface ISecurityIncidentReadinessRepository
{
    Task LockTenantAsync(Guid tenantId, CancellationToken ct);
    Task<SecurityReviewRecordDto?> CurrentSecurityAsync(Guid tenantId, CancellationToken ct);
    Task<SecurityReviewRecordDto> SaveSecurityAsync(Guid tenantId, Guid actor, SaveSecurityReviewRequest request, CancellationToken ct);
    Task<SecurityReviewRecordDto?> ApproveSecurityAsync(Guid tenantId, Guid actor, ApproveReadinessRecordRequest request, CancellationToken ct);
    Task<TechnicalReadinessRecordDto?> CurrentTechnicalAsync(Guid tenantId, CancellationToken ct);
    Task<TechnicalReadinessRecordDto> SaveTechnicalAsync(Guid tenantId, Guid actor, SaveTechnicalReadinessRequest request, CancellationToken ct);
    Task<TechnicalReadinessRecordDto?> ApproveTechnicalAsync(Guid tenantId, Guid actor, ApproveReadinessRecordRequest request, CancellationToken ct);
    Task<IncidentReadinessRecordDto?> CurrentIncidentAsync(Guid tenantId, CancellationToken ct);
    Task<IncidentReadinessRecordDto> SaveIncidentAsync(Guid tenantId, Guid actor, SaveIncidentReadinessRequest request, CancellationToken ct);
    Task<IncidentReadinessRecordDto?> ApproveIncidentAsync(Guid tenantId, Guid actor, ApproveReadinessRecordRequest request, CancellationToken ct);
    Task<IReadOnlyList<ReadinessHistoryDto>> HistoryAsync(Guid tenantId, CancellationToken ct);
    Task ValidateEvidenceSourcesAsync(Guid tenantId, IReadOnlyList<ReadinessEvidenceSource> sources, CancellationToken ct);
    Task<IReadOnlyList<ReadinessEvidenceOptionDto>> ListEvidenceOptionsAsync(Guid tenantId, CancellationToken ct);
}

public sealed record ReadinessEvidenceSource(string SourceType, Guid? EvidenceFileVersionId, string? ExternalUri, string? Sha256Digest);

public sealed class SecurityIncidentReadinessService(ISecurityIncidentReadinessRepository repository,
    ICurrentTenantContext context, IApplicationTransaction transaction, IAuditEventWriter audit)
{
    public Task<SecurityReviewRecordDto?> GetSecurityAsync(CancellationToken ct) => repository.CurrentSecurityAsync(context.TenantId, ct);
    public Task<TechnicalReadinessRecordDto?> GetTechnicalAsync(CancellationToken ct) => repository.CurrentTechnicalAsync(context.TenantId, ct);
    public Task<IncidentReadinessRecordDto?> GetIncidentAsync(CancellationToken ct) => repository.CurrentIncidentAsync(context.TenantId, ct);
    public Task<IReadOnlyList<ReadinessHistoryDto>> HistoryAsync(CancellationToken ct) => repository.HistoryAsync(context.TenantId, ct);
    public Task<IReadOnlyList<ReadinessEvidenceOptionDto>> ListEvidenceOptionsAsync(CancellationToken ct) => repository.ListEvidenceOptionsAsync(context.TenantId, ct);
    public async Task<ReadinessReleaseSummaryDto> SummaryAsync(CancellationToken ct)
    {
        var security = await repository.CurrentSecurityAsync(context.TenantId, ct);
        var technical = await repository.CurrentTechnicalAsync(context.TenantId, ct);
        var incident = await repository.CurrentIncidentAsync(context.TenantId, ct);
        var passed = new List<string>();
        if (security is not null) passed.AddRange(security.Items.Where(i => i.Status is SecurityReviewItemStatus.Passed or SecurityReviewItemStatus.AcceptedRisk).Select(i => i.Area));
        if (technical is not null) passed.AddRange(technical.Evidence.Where(e => e.Result == "Passed").Select(e => e.ControlType));
        var findings = security?.Findings.Where(f => f.Status == SecurityReviewFindingStatus.Open).ToArray() ?? [];
        var gaps = incident?.FollowUps.Where(f => f.Status == IncidentResponseGapStatus.Open).ToArray() ?? [];
        var ready = security?.State == "Approved" && technical?.State == "Approved" && incident?.State == "Approved" &&
            !findings.Any(f => f.Severity is SecurityReviewFindingSeverity.High or SecurityReviewFindingSeverity.Critical) &&
            !gaps.Any(g => g.Severity == SecurityReviewFindingSeverity.Critical);
        return new(passed.Distinct().Order().ToArray(), findings, security?.AcceptedRisks ?? [], gaps, ready ? "ReadyForChecklistReview" : "DoNotRelease");
    }

    public Task<SecurityReviewRecordDto> SaveSecurityAsync(SaveSecurityReviewRequest request, CancellationToken ct) => transaction.ExecuteAsync(async token =>
    {
        await repository.LockTenantAsync(context.TenantId, token); ValidateSecurity(request);
        var previous = await repository.CurrentSecurityAsync(context.TenantId, token);
        var result = await repository.SaveSecurityAsync(context.TenantId, context.UserId, request, token);
        await WriteAuditAsync(previous is null ? AuditAction.Created : AuditAction.Updated, "SecurityReview", result.Id, previous is null ? "created" : "updated", token);
        var oldFindings = previous?.Findings.ToDictionary(f => f.Id) ?? new Dictionary<Guid, SecurityReviewFindingRecordDto>();
        foreach (var finding in result.Findings)
        {
            var action = !oldFindings.TryGetValue(finding.Id, out var old) ? "created" : old.Status != finding.Status ? finding.Status == SecurityReviewFindingStatus.Closed ? "closed" : "status-changed" : null;
            if (action is not null) await WriteAuditAsync(AuditAction.Updated, "SecurityReviewFinding", finding.Id, action, token);
        }
        var oldRisks = previous?.AcceptedRisks.Select(r => r.Id).ToHashSet() ?? [];
        foreach (var risk in result.AcceptedRisks.Where(r => !oldRisks.Contains(r.Id)))
            await WriteAuditAsync(AuditAction.Approved, "AcceptedSecurityRisk", risk.Id, "accepted", token);
        return result;
    }, ct);
    public Task<SecurityReviewRecordDto?> ApproveSecurityAsync(ApproveReadinessRecordRequest request, CancellationToken ct) =>
        MutateAsync<SecurityReviewRecordDto?>("SecurityReview", "approved", async token =>
        {
            if (string.IsNullOrWhiteSpace(request.Notes) || request.Notes.Length > 1200) throw Invalid("Approval notes are required and must not exceed 1200 characters.");
            var record = await repository.CurrentSecurityAsync(context.TenantId, token) ?? throw Invalid("A security review has not been created.");
            ValidateSecurity(new(record.Version, record.Items, record.Findings.Select(f => new SaveSecurityReviewFindingRequest(f.Id, f.Area, f.Severity, f.Status, f.Summary, f.RemediationOwner, f.DueAt, f.ClosureNotes)).ToArray(), record.AcceptedRisks.Select(r => new SaveAcceptedSecurityRiskRequest(r.Id, r.FindingId, r.ApproverUserId, r.AcceptedAt, r.Scope, r.ExpiresAt, r.ReviewAt, r.MitigationNote)).ToArray()), true);
            return await repository.ApproveSecurityAsync(context.TenantId, context.UserId, request, token);
        }, ct);
    public Task<TechnicalReadinessRecordDto> SaveTechnicalAsync(SaveTechnicalReadinessRequest request, CancellationToken ct) =>
        MutateAsync("TechnicalReadiness", "saved", async token => { ValidateTechnical(request, false); await ValidateSourcesAsync(request.Evidence.Select(Source), token); return await repository.SaveTechnicalAsync(context.TenantId, context.UserId, request, token); }, ct);
    public Task<TechnicalReadinessRecordDto?> ApproveTechnicalAsync(ApproveReadinessRecordRequest request, CancellationToken ct) =>
        MutateAsync<TechnicalReadinessRecordDto?>("TechnicalReadiness", "approved", async token =>
        {
            var record = await repository.CurrentTechnicalAsync(context.TenantId, token) ?? throw Invalid("Technical readiness evidence has not been created.");
            ValidateTechnical(new(record.Version, record.Evidence.Select(e => new SaveExecutedControlEvidenceRequest(e.Id, e.ControlType, e.ExecutedAt, e.Environment, e.ReviewerUserId, e.Result, e.EvidenceReference, e.ExpiresAt, e.Notes, e.EvidenceSourceType, e.EvidenceFileVersionId, e.ExternalUri, e.Sha256Digest)).ToArray()), true);
            await ValidateSourcesAsync(record.Evidence.Select(Source), token);
            return await repository.ApproveTechnicalAsync(context.TenantId, context.UserId, request, token);
        }, ct);
    public Task<IncidentReadinessRecordDto> SaveIncidentAsync(SaveIncidentReadinessRequest request, CancellationToken ct) =>
        MutateAsync("IncidentReadiness", "saved", async token => { ValidateIncident(request, false); await ValidateSourcesAsync(request.Tabletops.Select(Source), token); return await repository.SaveIncidentAsync(context.TenantId, context.UserId, request, token); }, ct);
    public Task<IncidentReadinessRecordDto?> ApproveIncidentAsync(ApproveReadinessRecordRequest request, CancellationToken ct) =>
        MutateAsync<IncidentReadinessRecordDto?>("IncidentReadiness", "approved", async token =>
        {
            var record = await repository.CurrentIncidentAsync(context.TenantId, token) ?? throw Invalid("Incident readiness has not been created.");
            ValidateIncident(new(record.Version, record.ReviewDueAt, record.ReviewBasis, record.Contacts.Select(c => new SaveIncidentContactRequest(c.Id, c.Function, c.Contact, c.EscalationRole)).ToArray(), record.Playbooks.Select(p => new SaveIncidentPlaybookRequest(p.Id, p.Key, p.Trigger, p.ContainmentSteps, p.NotificationPath, p.EvidenceToCollect, p.Owner, p.ClosureCriteria)).ToArray(), record.Tabletops.Select(t => new SaveIncidentTabletopRequest(t.Id, t.ExecutedAt, t.Environment, t.Participants, t.Findings, t.EvidenceReference, t.ReviewerUserId, t.EvidenceSourceType, t.EvidenceFileVersionId, t.ExternalUri, t.Sha256Digest)).ToArray(), record.FollowUps.Select(f => new SaveIncidentFollowUpRequest(f.Id, f.TabletopId, f.Severity, f.Status, f.Summary, f.Owner, f.DueAt, f.ClosureNotes)).ToArray()), true);
            await ValidateSourcesAsync(record.Tabletops.Select(Source), token);
            return await repository.ApproveIncidentAsync(context.TenantId, context.UserId, request, token);
        }, ct);

    private Task<T> MutateAsync<T>(string type, string action, Func<CancellationToken, Task<T>> operation, CancellationToken ct) => transaction.ExecuteAsync(async token =>
    {
        await repository.LockTenantAsync(context.TenantId, token);
        var result = await operation(token);
        await audit.WriteAsync(context.TenantId, context.UserId, action == "approved" ? AuditAction.Approved : AuditAction.Updated,
            type, context.TenantId.ToString(), $"{type} {action}.", new Dictionary<string, string> { { "eventType", $"readiness.{type.ToLowerInvariant()}.{action}" }, { "result", "succeeded" } }, token);
        return result;
    }, ct);
    private Task WriteAuditAsync(AuditAction action, string type, Guid id, string lifecycle, CancellationToken token) =>
        audit.WriteAsync(context.TenantId, context.UserId, action, type, id.ToString(), $"{type} {lifecycle}.",
            new Dictionary<string, string> { { "eventType", $"readiness.{type.ToLowerInvariant()}.{lifecycle}" }, { "result", "succeeded" } }, token);

    private static void ValidateSecurity(SaveSecurityReviewRequest request, bool approval = false)
    {
        if (request.ExpectedVersion < 0 || request.Items is null || request.Findings is null || request.AcceptedRisks is null) throw Invalid("Security review payload is invalid.");
        var errors = SecurityReviewChecklist.ValidateItems(request.Items);
        if (errors.Count != 0 || request.Items.Select(i => i.Area).Distinct(StringComparer.Ordinal).Count() != request.Items.Count) throw Invalid(errors.FirstOrDefault() ?? "Security review areas must be unique.");
        foreach (var finding in request.Findings)
            if (!SecurityReviewChecklist.RequiredAreas.Contains(finding.Area) || string.IsNullOrWhiteSpace(finding.Summary) || finding.Summary.Length > 1200 || string.IsNullOrWhiteSpace(finding.RemediationOwner)) throw Invalid("Every finding requires a valid area, summary, and remediation owner.");
        if (request.Findings.Where(f => f.Id.HasValue).GroupBy(f => f.Id).Any(g => g.Count() > 1) ||
            request.AcceptedRisks.Where(r => r.Id.HasValue).GroupBy(r => r.Id).Any(g => g.Count() > 1))
            throw Invalid("Finding and accepted-risk identifiers must be unique within a review version.");
        foreach (var risk in request.AcceptedRisks)
            if (SecurityReviewChecklist.ValidateAcceptedRisk(new(risk.ApproverUserId, risk.AcceptedAt, risk.Scope, risk.ExpiresAt, risk.ReviewAt, risk.MitigationNote)).Count != 0) throw Invalid("Accepted risk metadata is incomplete.");
        var findingIds = request.Findings.Where(f => f.Id.HasValue).Select(f => f.Id!.Value).ToHashSet();
        if (request.AcceptedRisks.Any(r => r.FindingId.HasValue && !findingIds.Contains(r.FindingId.Value))) throw Invalid("Accepted risk must reference a finding in the same review version.");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (approval && (request.Items.Any(i => i.Status is not (SecurityReviewItemStatus.Passed or SecurityReviewItemStatus.AcceptedRisk)) ||
            SecurityReviewChecklist.BlocksCuiReadyApproval(request.Findings.Select(f => new SecurityReviewFindingDto(f.Area, f.Severity, f.Status, f.Summary)).ToArray()) ||
            request.AcceptedRisks.Any(r => (r.ExpiresAt ?? r.ReviewAt) <= today))) throw Invalid("Security review cannot be approved with incomplete checks, open high/critical findings, or expired accepted risk.");
    }
    private static readonly string[] ControlTypes = ["tenant-isolation", "evidence-storage", "malware-scanner", "backup-restore", "administrator-access", "support-access"];
    private static void ValidateTechnical(SaveTechnicalReadinessRequest request, bool approval)
    {
        if (request.ExpectedVersion < 0 || request.Evidence is null) throw Invalid("Technical readiness payload is invalid.");
        if (request.Evidence.Where(e => e.Id.HasValue).GroupBy(e => e.Id).Any(g => g.Count() > 1)) throw Invalid("Executed-control identifiers must be unique within a readiness version.");
        if (request.Evidence.Any(e => !ControlTypes.Contains(e.ControlType) || e.ExecutedAt == default || string.IsNullOrWhiteSpace(e.Environment) || e.ReviewerUserId == Guid.Empty || string.IsNullOrWhiteSpace(e.Result) || string.IsNullOrWhiteSpace(e.EvidenceReference) || !ValidSource(e.EvidenceSourceType, e.EvidenceFileVersionId, e.ExternalUri, e.Sha256Digest))) throw Invalid("Executed control evidence requires a typed, immutable evidence source.");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (approval && ControlTypes.Any(type => !request.Evidence.Any(e => e.ControlType == type && e.Result == "Passed" && e.ExecutedAt > today.AddYears(-1) && (e.ExpiresAt is null || e.ExpiresAt > today)))) throw Invalid("Every required technical control needs current executed and passed evidence.");
    }
    private static void ValidateIncident(SaveIncidentReadinessRequest request, bool approval)
    {
        if (request.ExpectedVersion < 0 || request.Playbooks is null || request.Tabletops is null || request.FollowUps is null) throw Invalid("Incident readiness payload is invalid.");
        if (new IEnumerable<Guid?>[] { request.Contacts?.Select(x => x.Id) ?? [], request.Playbooks.Select(x => x.Id), request.Tabletops.Select(x => x.Id), request.FollowUps.Select(x => x.Id) }
            .Any(ids => ids.Where(id => id.HasValue).GroupBy(id => id).Any(g => g.Count() > 1))) throw Invalid("Incident child identifiers must be unique within a readiness version.");
        var contactFunctions = new[] { "security", "support", "legal-compliance", "engineering", "customer-success" };
        if (request.Contacts is null || contactFunctions.Any(function => !request.Contacts.Any(c => c.Function == function && !string.IsNullOrWhiteSpace(c.Contact) && !string.IsNullOrWhiteSpace(c.EscalationRole)))) throw Invalid("Incident readiness requires current escalation contacts for every required function.");
        var playbooks = request.Playbooks.Select(p => new IncidentResponsePlaybookDto(p.Key, p.Trigger, p.ContainmentSteps, p.NotificationPath, p.EvidenceToCollect, p.Owner, p.ClosureCriteria)).ToArray();
        var errors = IncidentResponseReadiness.ValidatePlaybooks(playbooks);
        if (errors.Count != 0) throw Invalid(errors[0]);
        if (request.Tabletops.Any(t => t.ExecutedAt == default || string.IsNullOrWhiteSpace(t.Environment) || t.Participants.Count == 0 || t.Findings.Count == 0 || string.IsNullOrWhiteSpace(t.EvidenceReference) || t.ReviewerUserId == Guid.Empty || !ValidSource(t.EvidenceSourceType, t.EvidenceFileVersionId, t.ExternalUri, t.Sha256Digest))) throw Invalid("Tabletop evidence requires execution context, participants, findings, reviewer, and a typed immutable source.");
        var tabletopIds = request.Tabletops.Where(t => t.Id.HasValue).Select(t => t.Id!.Value).ToHashSet();
        if (request.FollowUps.Any(f => !tabletopIds.Contains(f.TabletopId))) throw Invalid("Every incident follow-up must reference a tabletop in the same readiness version.");
        if (request.FollowUps.Any(f => string.IsNullOrWhiteSpace(f.Summary) || string.IsNullOrWhiteSpace(f.Owner) || f.DueAt == default || (f.Status == IncidentResponseGapStatus.Closed && string.IsNullOrWhiteSpace(f.ClosureNotes)))) throw Invalid("Incident follow-up metadata is incomplete.");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (request.ReviewDueAt <= today || request.ReviewDueAt > today.AddYears(1) || request.ReviewBasis is not ("Annual" or "Release")) throw Invalid("Incident review reminder requires an annual or release basis and a future due date within one year.");
        if (approval && (request.Tabletops.All(t => t.ExecutedAt <= today.AddYears(-1)) || request.FollowUps.Any(f => f.Status == IncidentResponseGapStatus.Open && f.Severity == SecurityReviewFindingSeverity.Critical))) throw Invalid("Incident readiness requires a current tabletop and no open critical gaps.");
    }
    private static CuiReadyApprovalChecklistValidationException Invalid(string message) => new(message);
    private Task ValidateSourcesAsync(IEnumerable<ReadinessEvidenceSource> sources, CancellationToken ct) =>
        repository.ValidateEvidenceSourcesAsync(context.TenantId, sources.ToArray(), ct);
    private static ReadinessEvidenceSource Source(SaveExecutedControlEvidenceRequest value) => new(value.EvidenceSourceType, value.EvidenceFileVersionId, value.ExternalUri, value.Sha256Digest);
    private static ReadinessEvidenceSource Source(ExecutedControlEvidenceDto value) => new(value.EvidenceSourceType, value.EvidenceFileVersionId, value.ExternalUri, value.Sha256Digest);
    private static ReadinessEvidenceSource Source(SaveIncidentTabletopRequest value) => new(value.EvidenceSourceType, value.EvidenceFileVersionId, value.ExternalUri, value.Sha256Digest);
    private static ReadinessEvidenceSource Source(IncidentTabletopRecordDto value) => new(value.EvidenceSourceType, value.EvidenceFileVersionId, value.ExternalUri, value.Sha256Digest);
    private static bool ValidSource(string type, Guid? versionId, string? externalUri, string? digest)
    {
        if (type == "EvidenceFileVersion") return versionId.HasValue && versionId != Guid.Empty && string.IsNullOrWhiteSpace(externalUri) && string.IsNullOrWhiteSpace(digest);
        return type == "ExternalArtifact" && versionId is null && Uri.TryCreate(externalUri, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps &&
            digest is { Length: 64 } && digest.All(Uri.IsHexDigit);
    }
}

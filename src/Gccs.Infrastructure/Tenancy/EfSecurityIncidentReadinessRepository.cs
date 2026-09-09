using System.Text.Json;
using Gccs.Application.Security;
using Gccs.Application.Tenancy;
using Gccs.Domain.Common;
using Gccs.Domain.Compliance;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Tenancy;

public sealed class EfSecurityIncidentReadinessRepository(GccsDbContext db, ICurrentTenantContext context) : ISecurityIncidentReadinessRepository
{
    private void Scope(Guid tenantId) { if (tenantId != context.TenantId) throw new CuiReadyApprovalChecklistValidationException("Readiness record was not found in the current tenant."); }
    public async Task LockTenantAsync(Guid tenantId, CancellationToken ct) { Scope(tenantId); if (db.Database.IsNpgsql()) { if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("Readiness mutations require a transaction."); await db.Tenants.FromSqlInterpolated($"SELECT * FROM gccs.tenants WHERE id = {tenantId} FOR UPDATE").AsNoTracking().SingleAsync(ct); } }

    public async Task<SecurityReviewRecordDto?> CurrentSecurityAsync(Guid tenantId, CancellationToken ct) { Scope(tenantId); var e = await db.SecurityReviewRecords.AsNoTracking().Include(x => x.Items).Include(x => x.Findings).Include(x => x.AcceptedRisks).Where(x => x.TenantId == tenantId).OrderByDescending(x => x.Version).FirstOrDefaultAsync(ct); return e is null ? null : Dto(e); }
    public async Task<SecurityReviewRecordDto> SaveSecurityAsync(Guid tenantId, Guid actor, SaveSecurityReviewRequest r, CancellationToken ct)
    {
        Scope(tenantId); await CheckVersion(db.SecurityReviewRecords.Where(x => x.TenantId == tenantId).Select(x => x.Version), r.ExpectedVersion, ct); await Supersede(db.SecurityReviewRecords.Where(x => x.TenantId == tenantId && x.State == "Approved"), ct);
        var e = new SecurityReviewRecordEntity { Id = Guid.NewGuid(), TenantId = tenantId, Version = r.ExpectedVersion + 1, CreatedAt = DateTimeOffset.UtcNow, CreatedByUserId = actor };
        e.Items = r.Items.Select(i => new SecurityReviewChecklistItemRecordEntity { Id = Guid.NewGuid(), TenantId = tenantId, Area = i.Area, Status = (int)i.Status, ReviewerUserId = i.ReviewerUserId, ReviewedAt = i.ReviewedAt, EvidenceLink = i.EvidenceLink, Rationale = i.Rationale }).ToList();
        var findingRows = r.Findings.Select(x => new { Request = x, LogicalId = x.Id ?? Guid.NewGuid(), RowId = Guid.NewGuid() }).ToArray();
        var findingRowIds = findingRows.ToDictionary(x => x.LogicalId, x => x.RowId);
        e.Findings = findingRows.Select(x => new SecurityReviewFindingRecordEntity { Id = x.RowId, LogicalId = x.LogicalId, TenantId = tenantId, Area = x.Request.Area, Severity = (int)x.Request.Severity, Status = (int)x.Request.Status, Summary = x.Request.Summary.Trim(), RemediationOwner = x.Request.RemediationOwner.Trim(), DueAt = x.Request.DueAt, ClosureNotes = x.Request.ClosureNotes }).ToList();
        e.AcceptedRisks = r.AcceptedRisks.Select(x => new AcceptedSecurityRiskRecordEntity { Id = Guid.NewGuid(), LogicalId = x.Id ?? Guid.NewGuid(), TenantId = tenantId, FindingId = x.FindingId is null ? null : findingRowIds[x.FindingId.Value], ApproverUserId = x.ApproverUserId, AcceptedAt = x.AcceptedAt, Scope = x.Scope.Trim(), ExpiresAt = x.ExpiresAt, ReviewAt = x.ReviewAt, MitigationNote = x.MitigationNote.Trim() }).ToList(); db.Add(e); AddHistory(tenantId, "security-review", e.Id, e.Version, r.ExpectedVersion == 0 ? "created" : "updated", actor, "Security review version saved."); await db.SaveChangesAsync(ct); return Dto(e);
    }
    public async Task<SecurityReviewRecordDto?> ApproveSecurityAsync(Guid tenantId, Guid actor, ApproveReadinessRecordRequest r, CancellationToken ct) { Scope(tenantId); var e = await db.SecurityReviewRecords.Include(x => x.Items).Include(x => x.Findings).Include(x => x.AcceptedRisks).Where(x => x.TenantId == tenantId).OrderByDescending(x => x.Version).FirstOrDefaultAsync(ct); if (e is null) return null; Approve(e, "security-review", actor, r); await db.SaveChangesAsync(ct); return Dto(e); }

    public async Task<TechnicalReadinessRecordDto?> CurrentTechnicalAsync(Guid tenantId, CancellationToken ct) { Scope(tenantId); var e = await db.TechnicalReadinessRecords.AsNoTracking().Include(x => x.Evidence).Where(x => x.TenantId == tenantId).OrderByDescending(x => x.Version).FirstOrDefaultAsync(ct); return e is null ? null : Dto(e); }
    public async Task<TechnicalReadinessRecordDto> SaveTechnicalAsync(Guid tenantId, Guid actor, SaveTechnicalReadinessRequest r, CancellationToken ct) { Scope(tenantId); await CheckVersion(db.TechnicalReadinessRecords.Where(x => x.TenantId == tenantId).Select(x => x.Version), r.ExpectedVersion, ct); await Supersede(db.TechnicalReadinessRecords.Where(x => x.TenantId == tenantId && x.State == "Approved"), ct); var e = new TechnicalReadinessRecordEntity { Id = Guid.NewGuid(), TenantId = tenantId, Version = r.ExpectedVersion + 1, CreatedAt = DateTimeOffset.UtcNow, CreatedByUserId = actor, Evidence = r.Evidence.Select(x => new ExecutedControlEvidenceEntity { Id = Guid.NewGuid(), LogicalId = x.Id ?? Guid.NewGuid(), TenantId = tenantId, ControlType = x.ControlType, ExecutedAt = x.ExecutedAt, Environment = x.Environment.Trim(), ReviewerUserId = x.ReviewerUserId, Result = x.Result.Trim(), EvidenceReference = x.EvidenceReference.Trim(), ExpiresAt = x.ExpiresAt, Notes = x.Notes.Trim() }).ToList() }; db.Add(e); AddHistory(tenantId, "technical-readiness", e.Id, e.Version, r.ExpectedVersion == 0 ? "created" : "updated", actor, "Technical readiness version saved."); await db.SaveChangesAsync(ct); return Dto(e); }
    public async Task<TechnicalReadinessRecordDto?> ApproveTechnicalAsync(Guid tenantId, Guid actor, ApproveReadinessRecordRequest r, CancellationToken ct) { Scope(tenantId); var e = await db.TechnicalReadinessRecords.Include(x => x.Evidence).Where(x => x.TenantId == tenantId).OrderByDescending(x => x.Version).FirstOrDefaultAsync(ct); if (e is null) return null; Approve(e, "technical-readiness", actor, r); await db.SaveChangesAsync(ct); return Dto(e); }

    public async Task<IncidentReadinessRecordDto?> CurrentIncidentAsync(Guid tenantId, CancellationToken ct) { Scope(tenantId); var e = await db.IncidentReadinessRecords.AsNoTracking().Include(x => x.Contacts).Include(x => x.Playbooks).Include(x => x.Tabletops).Include(x => x.FollowUps).Where(x => x.TenantId == tenantId).OrderByDescending(x => x.Version).FirstOrDefaultAsync(ct); return e is null ? null : Dto(e); }
    public async Task<IncidentReadinessRecordDto> SaveIncidentAsync(Guid tenantId, Guid actor, SaveIncidentReadinessRequest r, CancellationToken ct)
    {
        Scope(tenantId); await CheckVersion(db.IncidentReadinessRecords.Where(x => x.TenantId == tenantId).Select(x => x.Version), r.ExpectedVersion, ct); await Supersede(db.IncidentReadinessRecords.Where(x => x.TenantId == tenantId && x.State == "Approved"), ct);
        var tabletopRows = r.Tabletops.Select(x => new { Request = x, LogicalId = x.Id ?? Guid.NewGuid(), RowId = Guid.NewGuid() }).ToArray();
        var tabletopRowIds = tabletopRows.ToDictionary(x => x.LogicalId, x => x.RowId);
        var e = new IncidentReadinessRecordEntity { Id = Guid.NewGuid(), TenantId = tenantId, Version = r.ExpectedVersion + 1, CreatedAt = DateTimeOffset.UtcNow, CreatedByUserId = actor, ReviewDueAt = r.ReviewDueAt, ReviewBasis = r.ReviewBasis };
        e.Contacts = r.Contacts.Select(x => new IncidentContactRecordEntity { Id = Guid.NewGuid(), LogicalId = x.Id ?? Guid.NewGuid(), TenantId = tenantId, Function = x.Function, Contact = x.Contact.Trim(), EscalationRole = x.EscalationRole.Trim() }).ToList();
        e.Playbooks = r.Playbooks.Select(x => new IncidentPlaybookRecordEntity { Id = Guid.NewGuid(), LogicalId = x.Id ?? Guid.NewGuid(), TenantId = tenantId, Key = x.Key, Trigger = x.Trigger.Trim(), ContainmentStepsJson = JsonSerializer.Serialize(x.ContainmentSteps), NotificationPath = x.NotificationPath.Trim(), EvidenceToCollectJson = JsonSerializer.Serialize(x.EvidenceToCollect), Owner = x.Owner.Trim(), ClosureCriteria = x.ClosureCriteria.Trim() }).ToList();
        e.Tabletops = tabletopRows.Select(x => new IncidentTabletopRecordEntity { Id = x.RowId, LogicalId = x.LogicalId, TenantId = tenantId, ExecutedAt = x.Request.ExecutedAt, Environment = x.Request.Environment.Trim(), ParticipantsJson = JsonSerializer.Serialize(x.Request.Participants), FindingsJson = JsonSerializer.Serialize(x.Request.Findings), EvidenceReference = x.Request.EvidenceReference.Trim(), ReviewerUserId = x.Request.ReviewerUserId }).ToList();
        e.FollowUps = r.FollowUps.Select(x => new IncidentFollowUpRecordEntity { Id = Guid.NewGuid(), LogicalId = x.Id ?? Guid.NewGuid(), TenantId = tenantId, TabletopId = tabletopRowIds[x.TabletopId], Severity = (int)x.Severity, Status = (int)x.Status, Summary = x.Summary.Trim(), Owner = x.Owner.Trim(), DueAt = x.DueAt, ClosureNotes = x.ClosureNotes }).ToList();
        foreach (var previousReminder in await db.ComplianceTasks.Where(x => x.TenantId == tenantId && x.ControlId == "incident-readiness-review" && x.Status != ComplianceTaskStatus.Done && x.Status != ComplianceTaskStatus.Canceled).ToArrayAsync(ct))
        {
            previousReminder.Status = ComplianceTaskStatus.Canceled;
            previousReminder.UpdatedAt = DateTimeOffset.UtcNow;
            previousReminder.UpdatedByUserId = actor;
        }
        db.ComplianceTasks.Add(new ComplianceTaskEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Title = "Review incident response readiness",
            Description = $"{r.ReviewBasis} review of incident playbooks, contacts, tabletop evidence, and follow-up gaps.",
            Type = ComplianceTaskType.PolicyReview,
            Status = ComplianceTaskStatus.Open,
            RiskLevel = RiskLevel.High,
            AssignedToUserId = actor,
            OwnerFunction = "Security",
            DueAt = r.ReviewDueAt,
            ControlId = "incident-readiness-review",
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = actor
        });
        db.Add(e); AddHistory(tenantId, "incident-readiness", e.Id, e.Version, r.ExpectedVersion == 0 ? "created" : "updated", actor, "Incident readiness version saved."); await db.SaveChangesAsync(ct); return Dto(e);
    }
    public async Task<IncidentReadinessRecordDto?> ApproveIncidentAsync(Guid tenantId, Guid actor, ApproveReadinessRecordRequest r, CancellationToken ct) { Scope(tenantId); var e = await db.IncidentReadinessRecords.Include(x => x.Contacts).Include(x => x.Playbooks).Include(x => x.Tabletops).Include(x => x.FollowUps).Where(x => x.TenantId == tenantId).OrderByDescending(x => x.Version).FirstOrDefaultAsync(ct); if (e is null) return null; Approve(e, "incident-readiness", actor, r); await db.SaveChangesAsync(ct); return Dto(e); }
    public async Task<IReadOnlyList<ReadinessHistoryDto>> HistoryAsync(Guid tenantId, CancellationToken ct) { Scope(tenantId); return await db.ReadinessHistory.AsNoTracking().Where(x => x.TenantId == tenantId).OrderByDescending(x => x.OccurredAt).Take(200).Select(x => new ReadinessHistoryDto(x.Id, x.RecordType, x.RecordId, x.Version, x.Action, x.ActorUserId, x.OccurredAt, x.Summary)).ToArrayAsync(ct); }

    private async Task CheckVersion(IQueryable<int> q, int expected, CancellationToken ct) { var actual = await q.OrderByDescending(x => x).FirstOrDefaultAsync(ct); if (actual != expected) throw new CuiReadyApprovalChecklistValidationException("Readiness record changed. Reload and retry."); }
    private static async Task Supersede<TEntity>(IQueryable<TEntity> q, CancellationToken ct) where TEntity : ReadinessRecordEntity { foreach (var e in await q.ToArrayAsync(ct)) e.State = "Superseded"; }
    private void Approve(ReadinessRecordEntity e, string type, Guid actor, ApproveReadinessRecordRequest r) { if (e.Version != r.ExpectedVersion || e.State != "Draft") throw new CuiReadyApprovalChecklistValidationException("Only the current draft version can be approved."); e.State = "Approved"; e.ApprovedAt = DateTimeOffset.UtcNow; e.ApprovedByUserId = actor; e.ApprovalNotes = r.Notes.Trim(); db.ReadinessApprovals.Add(new() { Id = Guid.NewGuid(), TenantId = e.TenantId, RecordType = type, RecordId = e.Id, Version = e.Version, ApprovedAt = e.ApprovedAt.Value, ApprovedByUserId = actor, Notes = e.ApprovalNotes }); AddHistory(e.TenantId, type, e.Id, e.Version, "approved", actor, "Readiness record approved."); }
    private void AddHistory(Guid tenant, string type, Guid id, int version, string action, Guid actor, string summary) => db.ReadinessHistory.Add(new() { Id = Guid.NewGuid(), TenantId = tenant, RecordType = type, RecordId = id, Version = version, Action = action, ActorUserId = actor, OccurredAt = DateTimeOffset.UtcNow, Summary = summary });
    private static SecurityReviewRecordDto Dto(SecurityReviewRecordEntity e)
    {
        var findingLogicalIds = e.Findings.ToDictionary(x => x.Id, x => x.LogicalId);
        return new(e.Id, e.TenantId, e.Version, e.State, e.CreatedAt, e.CreatedByUserId, e.ApprovedAt, e.ApprovedByUserId, e.ApprovalNotes, e.Items.Select(i => new SecurityReviewChecklistItemDto(i.Area, (SecurityReviewItemStatus)i.Status, i.ReviewerUserId, i.ReviewedAt, i.EvidenceLink, i.Rationale)).ToArray(), e.Findings.Select(f => new SecurityReviewFindingRecordDto(f.LogicalId, f.Area, (SecurityReviewFindingSeverity)f.Severity, (SecurityReviewFindingStatus)f.Status, f.Summary, f.RemediationOwner, f.DueAt, f.ClosureNotes)).ToArray(), e.AcceptedRisks.Select(r => new AcceptedSecurityRiskRecordDto(r.LogicalId, r.FindingId is null ? null : findingLogicalIds[r.FindingId.Value], r.ApproverUserId, r.AcceptedAt, r.Scope, r.ExpiresAt, r.ReviewAt, r.MitigationNote)).ToArray());
    }
    private static TechnicalReadinessRecordDto Dto(TechnicalReadinessRecordEntity e) => new(e.Id, e.TenantId, e.Version, e.State, e.CreatedAt, e.CreatedByUserId, e.ApprovedAt, e.ApprovedByUserId, e.ApprovalNotes, e.Evidence.Select(x => new ExecutedControlEvidenceDto(x.LogicalId, x.ControlType, x.ExecutedAt, x.Environment, x.ReviewerUserId, x.Result, x.EvidenceReference, x.ExpiresAt, x.Notes)).ToArray());
    private static IncidentReadinessRecordDto Dto(IncidentReadinessRecordEntity e)
    {
        var tabletopLogicalIds = e.Tabletops.ToDictionary(x => x.Id, x => x.LogicalId);
        return new(e.Id, e.TenantId, e.Version, e.State, e.CreatedAt, e.CreatedByUserId, e.ApprovedAt, e.ApprovedByUserId, e.ApprovalNotes, e.ReviewDueAt, e.ReviewBasis, e.Contacts.Select(x => new IncidentContactRecordDto(x.LogicalId, x.Function, x.Contact, x.EscalationRole)).ToArray(), e.Playbooks.Select(x => new IncidentPlaybookRecordDto(x.LogicalId, x.Key, x.Trigger, JsonSerializer.Deserialize<string[]>(x.ContainmentStepsJson) ?? [], x.NotificationPath, JsonSerializer.Deserialize<string[]>(x.EvidenceToCollectJson) ?? [], x.Owner, x.ClosureCriteria)).ToArray(), e.Tabletops.Select(x => new IncidentTabletopRecordDto(x.LogicalId, x.ExecutedAt, x.Environment, JsonSerializer.Deserialize<string[]>(x.ParticipantsJson) ?? [], JsonSerializer.Deserialize<string[]>(x.FindingsJson) ?? [], x.EvidenceReference, x.ReviewerUserId)).ToArray(), e.FollowUps.Select(x => new IncidentFollowUpRecordDto(x.LogicalId, tabletopLogicalIds[x.TabletopId], (SecurityReviewFindingSeverity)x.Severity, (IncidentResponseGapStatus)x.Status, x.Summary, x.Owner, x.DueAt, x.ClosureNotes)).ToArray());
    }
}

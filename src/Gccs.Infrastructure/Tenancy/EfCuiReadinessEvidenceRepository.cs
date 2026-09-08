using System.Text.Json;
using Gccs.Application.Security;
using Gccs.Application.Tenancy;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Gccs.Infrastructure.Tenancy;

public sealed class EfCuiReadinessEvidenceRepository(GccsDbContext db, ICurrentTenantContext context,
    DataHandlingNoticeService notices, SharedResponsibilityMatrixService matrices, DataHandlingNoticePackage package)
    : ICuiReadinessEvidenceRepository
{
    private void Scope(Guid tenantId)
    {
        if (tenantId != context.TenantId)
            throw new CuiReadyApprovalChecklistValidationException("Readiness records were not found in the current tenant.");
    }
    public async Task LockTenantAsync(Guid tenantId, CancellationToken ct)
    {
        Scope(tenantId);
        if (db.Database.IsNpgsql())
        {
            if (db.Database.CurrentTransaction is null)
                throw new CuiReadyApprovalChecklistValidationException("Readiness changes require an application transaction.");
            await db.Tenants.FromSqlInterpolated($"SELECT * FROM gccs.tenants WHERE id = {tenantId} FOR UPDATE").AsNoTracking().SingleAsync(ct);
        }
    }
    public async Task<IReadOnlyList<CuiReadinessEvidenceDto>> ListAsync(Guid tenantId, CancellationToken ct)
    {
        Scope(tenantId);
        return (await db.Set<CuiReadinessEvidenceEntity>().AsNoTracking().Where(e => e.TenantId == tenantId)
            .OrderByDescending(e => e.ReviewedAt).Take(100).ToArrayAsync(ct)).Select(Dto).ToArray();
    }
    public async Task<CuiReadinessEvidenceDto> RecordAsync(Guid tenantId, Guid actor, RecordCuiReadinessEvidenceRequest request, CancellationToken ct)
    {
        Scope(tenantId);
        var previous = await db.Set<CuiReadinessEvidenceEntity>().Where(e => e.TenantId == tenantId && e.Kind == request.Kind)
            .OrderByDescending(e => e.Version).FirstOrDefaultAsync(ct);
        if ((previous?.Version ?? 0) != request.ExpectedVersion)
            throw new CuiReadyApprovalChecklistValidationException("Readiness evidence changed. Reload before recording another version.");
        if (previous is not null) previous.State = "Superseded";
        var entity = new CuiReadinessEvidenceEntity
        {
            Id = Guid.NewGuid(), TenantId = tenantId, Kind = request.Kind, Version = request.ExpectedVersion + 1,
            State = request.Rejected ? "Rejected" : "Approved", ReviewedAt = DateTimeOffset.UtcNow, ReviewedByUserId = actor,
            ExpiresAt = request.ExpiresAt.ToUniversalTime(), SourceReference = request.SourceReference.Trim(), ReviewNotes = request.ReviewNotes.Trim(),
            DetailsJson = JsonSerializer.Serialize(request.Details)
        };
        db.Add(entity);
        await db.SaveChangesAsync(ct);
        return Dto(entity);
    }
    public async Task<IReadOnlyList<CuiReadinessSupportingRecord>> SourcesAsync(Guid tenantId, CancellationToken ct)
    {
        Scope(tenantId);
        var now = DateTimeOffset.UtcNow;
        var current = await db.Set<CuiReadinessEvidenceEntity>().AsNoTracking()
            .Where(e => e.TenantId == tenantId && e.State == "Approved" && e.ExpiresAt > now &&
                e.ReviewedAt > now.AddYears(-1) && e.ReviewedByUserId != Guid.Empty &&
                !db.Set<CuiReadinessEvidenceEntity>().Any(newer => newer.TenantId == tenantId && newer.Kind == e.Kind && newer.Version > e.Version)).ToArrayAsync(ct);
        var result = current.Where(e => e.ReviewedAt <= now && CuiReadinessEvidenceService.Validate(e.Kind, Dto(e).Details, now) is null)
            .Select(e => new CuiReadinessSupportingRecord(e.Id, e.Kind, e.Version.ToString(), $"{e.Kind} v{e.Version}")).ToList();
        var matrix = await matrices.GetPublishedAsync(package.Root, ct);
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        if (matrix.EffectiveAt <= today && matrix.ReviewedAt <= today && matrix.ReviewedAt > today.AddYears(-1))
        {
            var ack = await db.SharedResponsibilityMatrixAcknowledgements.AsNoTracking().Where(e => e.TenantId == tenantId &&
                e.MatrixId == matrix.MatrixId && e.MatrixVersion == matrix.Version && e.AcknowledgedAt <= now)
                .OrderByDescending(e => e.AcknowledgedAt).FirstOrDefaultAsync(ct);
            if (ack is not null) result.Add(new(ack.Id, "shared-responsibility-matrix", matrix.Version, $"{matrix.Title} · {matrix.Version}"));
        }
        var notice = await notices.GetPublishedAsync(package.Root, TenantDataPosture.CuiReady, "Onboarding", ct);
        if (notice is not null && notice.ReviewedAt <= today && notice.ReviewedAt > today.AddYears(-1))
        {
            var acknowledgements = await db.DataHandlingNoticeAcknowledgements.AsNoTracking().Where(e => e.TenantId == tenantId &&
                e.Mode == TenantDataPosture.CuiReady && e.NoticeId == notice.NoticeId && e.NoticeVersion == notice.Version &&
                e.WorkflowContext == "Onboarding" && e.AcknowledgedAt <= now).OrderByDescending(e => e.AcknowledgedAt).ToArrayAsync(ct);
            result.AddRange(acknowledgements.Select(ack => new CuiReadinessSupportingRecord(ack.Id, "data-handling-notice", notice.Version, $"{notice.Title} · {notice.Version}")));
        }
        return result;
    }
    public async Task<string?> ValidateLinksAsync(Guid tenantId, CuiReadyApprovalChecklistDto checklist, CancellationToken ct)
    {
        Scope(tenantId);
        var sources = await SourcesAsync(tenantId, ct);
        foreach (var kind in CuiReadinessEvidenceService.Kinds.Concat(["data-handling-notice", "shared-responsibility-matrix"]))
        {
            var item = checklist.Items.SingleOrDefault(i => i.ItemKey == kind);
            if (item is null || !sources.Any(s => s.Kind == kind && s.Id == item.SupportingRecordId && s.Version == item.SupportingVersion))
                return $"{kind}: link current approved supporting evidence. Missing, expired, rejected, superseded, or unavailable records cannot satisfy the gate.";
        }
        return null;
    }
    private static CuiReadinessEvidenceDto Dto(CuiReadinessEvidenceEntity e) => new(e.Id, e.TenantId, e.Kind, e.Version,
        e.State, e.ReviewedAt, e.ReviewedByUserId, e.ExpiresAt, e.SourceReference, e.ReviewNotes,
        JsonSerializer.Deserialize<CuiReadinessEvidenceDetails>(e.DetailsJson)!);
}

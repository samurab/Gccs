using System.Text.Json;
using Gccs.Application.Security;
using Gccs.Application.Tenancy;
using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;
using Gccs.Infrastructure.Tenancy;

namespace Gccs.Api.Tests;

internal static class CuiReadinessTestData
{
    internal static string PackageRoot
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "packages", "compliance-content"))) directory = directory.Parent;
            return Path.Combine(directory!.FullName, "packages", "compliance-content");
        }
    }
    internal sealed class Context(Guid tenant, Guid actor) : ICurrentTenantContext
    { public Guid TenantId => tenant; public Guid UserId => actor; public string UserEmail => "synthetic@example.invalid"; }
    internal static EfCuiReadinessEvidenceRepository Repository(GccsDbContext db, Guid tenant, Guid actor) =>
        new(db, new Context(tenant, actor), new(new FileDataHandlingNoticeRepository()), new(new FileSharedResponsibilityMatrixRepository()), new(PackageRoot));
    internal static CuiReadinessEvidenceDetails Details(string kind, Guid actor)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return kind switch
        {
            "security-review" => new(SecurityItems: SecurityReviewChecklist.RequiredAreas.Select(a => new SecurityReviewChecklistItemDto(a,
                SecurityReviewItemStatus.Passed, actor, today, "synthetic:test", "Synthetic test review")).ToArray(), SecurityFindings: [], AcceptedRisks: []),
            "incident-response" => new(Playbooks: IncidentResponseReadiness.RequiredPlaybooks.Select(k => new IncidentResponsePlaybookDto(k,
                "Synthetic trigger", ["Contain"], "Synthetic notification", ["Metadata"], "Synthetic owner", "Reviewed")).ToArray(),
                IncidentGaps: [], Tabletop: new(today, ["Synthetic reviewer"], ["No gaps"], ["Next review scheduled"])),
            "backup-restore" => new(BackupRestore: new(today, "Synthetic test", actor, "Passed")),
            "support-escalation" => new(SupportOwner: "Synthetic support", EscalationContact: "support@example.invalid", RunbookReference: "synthetic:runbook", Coverage: "Synthetic coverage"),
            _ => throw new ArgumentException(kind)
        };
    }
    internal static void Seed(GccsDbContext db, Guid tenant, Guid actor)
    {
        foreach (var kind in CuiReadinessEvidenceService.Kinds)
            db.Add(new CuiReadinessEvidenceEntity { Id = Guid.NewGuid(), TenantId = tenant, Kind = kind, Version = 1, State = "Approved",
                ReviewedAt = DateTimeOffset.UtcNow, ReviewedByUserId = actor, ExpiresAt = DateTimeOffset.UtcNow.AddMonths(3),
                SourceReference = "synthetic:test", ReviewNotes = "Synthetic reviewed fixture", DetailsJson = JsonSerializer.Serialize(Details(kind, actor)) });
        var matrix = new FileSharedResponsibilityMatrixRepository().LoadAsync(PackageRoot).GetAwaiter().GetResult();
        var notice = new DataHandlingNoticeService(new FileDataHandlingNoticeRepository()).GetPublishedAsync(PackageRoot, TenantDataPosture.CuiReady, "Onboarding").GetAwaiter().GetResult()!;
        db.SharedResponsibilityMatrixAcknowledgements.Add(new() { Id = Guid.NewGuid(), TenantId = tenant, MatrixId = matrix.MatrixId,
            MatrixVersion = matrix.Version, MatrixTitle = matrix.Title, AcknowledgedByUserId = actor, AcknowledgedAt = DateTimeOffset.UtcNow });
        db.DataHandlingNoticeAcknowledgements.Add(new() { Id = Guid.NewGuid(), TenantId = tenant, UserId = actor, Mode = TenantDataPosture.CuiReady,
            WorkflowContext = "Onboarding", NoticeId = notice.NoticeId, NoticeVersion = notice.Version, AcknowledgedAt = DateTimeOffset.UtcNow });
    }
    internal static async Task LinkAsync(GccsDbContext db, Guid tenant, Guid actor, Guid checklist)
    {
        var sources = await Repository(db, tenant, actor).SourcesAsync(tenant, default);
        foreach (var item in db.CuiReadyApprovalChecklistItems.Where(i => i.ChecklistId == checklist))
        {
            var source = sources.SingleOrDefault(s => s.Kind == item.ItemKey);
            item.SupportingRecordId = source?.Id; item.SupportingVersion = source?.Version;
        }
        await db.SaveChangesAsync();
    }
}

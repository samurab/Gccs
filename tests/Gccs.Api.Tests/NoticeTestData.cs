using Gccs.Domain.Tenancy;
using Gccs.Infrastructure.Persistence;
using Gccs.Infrastructure.Persistence.Models;

namespace Gccs.Api.Tests;

internal static class NoticeTestData
{
    // Explicit synthetic consent fixtures for tests whose subject is not notice renewal.
    internal static void Seed(GccsDbContext db, Guid? actor = null)
    {
        foreach (var tenant in db.Tenants.Local.ToArray())
        {
            var users = db.NoCuiAcknowledgements.Local.Where(a => a.TenantId == tenant.Id).Select(a => a.UserId)
                .Concat(actor is { } id ? new[] { id } : Array.Empty<Guid>()).Distinct().ToArray();
            foreach (var user in users)
            foreach (var workflow in new[] { "EvidenceUpload", "ContractIntake", "ReportGeneration" })
                db.DataHandlingNoticeAcknowledgements.Add(new DataHandlingNoticeAcknowledgementEntity
                {
                    Id = Guid.NewGuid(), TenantId = tenant.Id, UserId = user, Mode = tenant.DataPosture,
                    WorkflowContext = workflow, NoticeVersion = "2026.06.phase1a",
                    NoticeId = tenant.DataPosture == TenantDataPosture.DemoSandbox ? "demo-sandbox-general" :
                        tenant.DataPosture == TenantDataPosture.CuiReady ? "cui-ready-general" : "no-cui-general",
                    AcknowledgedAt = DateTimeOffset.UtcNow
                });
        }
    }
}

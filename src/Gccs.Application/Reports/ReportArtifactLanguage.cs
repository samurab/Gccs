namespace Gccs.Application.Reports;

public static class ReportArtifactLanguage
{
    public const string WorkflowGuidanceDisclaimer =
        "Workflow guidance only. This report is not legal advice, a certification decision, an assessor determination, a contracting-officer determination, or a government endorsement.";

    public const string SprsReadinessDisclaimer =
        "Draft readiness tracking only. FeDril has not submitted this score to SPRS. This report is workflow guidance, not legal advice, a certification decision, an assessor determination, a contracting-officer determination, or a government endorsement.";

    public static string For(Gccs.Domain.Reports.ReportType type) =>
        type == Gccs.Domain.Reports.ReportType.SprsReadiness
            ? SprsReadinessDisclaimer
            : WorkflowGuidanceDisclaimer;
}

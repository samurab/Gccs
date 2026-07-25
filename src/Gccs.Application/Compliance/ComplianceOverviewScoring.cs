namespace Gccs.Application.Compliance;

public static class ComplianceOverviewScoring
{
    public static ReadinessScoreDto BuildReadinessScore(
        int controlsTotal,
        int controlsImplemented,
        int controlsNotApplicable)
    {
        if (controlsTotal <= 0)
        {
            return new ReadinessScoreDto(null, 0, 0, 0, 0, "Not started");
        }

        var boundedNotApplicable = Math.Clamp(controlsNotApplicable, 0, controlsTotal);
        var applicableControls = controlsTotal - boundedNotApplicable;
        var boundedImplemented = Math.Clamp(controlsImplemented, 0, applicableControls);
        if (applicableControls == 0)
        {
            return new ReadinessScoreDto(null, controlsTotal, 0, 0, boundedNotApplicable, "No applicable controls");
        }

        var score = Math.Clamp((int)Math.Round(boundedImplemented * 100m / applicableControls, MidpointRounding.AwayFromZero), 0, 100);
        var status = score switch
        {
            >= 90 => "High coverage",
            >= 70 => "Moderate coverage",
            _ => "Low coverage"
        };

        return new ReadinessScoreDto(
            score,
            controlsTotal,
            applicableControls,
            boundedImplemented,
            boundedNotApplicable,
            status);
    }

    public static string DetermineContractRiskLevel(
        int overduePoams,
        int overdueHighRiskTasks,
        int openHighRiskTasks,
        int highRiskObligations,
        int openPoams,
        int missingEvidenceControls)
    {
        if (overduePoams > 0 || overdueHighRiskTasks > 0 || openHighRiskTasks >= 3)
        {
            return "High";
        }

        if (openHighRiskTasks > 0 || highRiskObligations > 0 || openPoams > 0 || missingEvidenceControls > 0)
        {
            return "Medium";
        }

        return "Low";
    }
}

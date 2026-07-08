namespace Gccs.Application.Compliance;

public static class ComplianceOverviewScoring
{
    public static ReadinessScoreDto BuildReadinessScore(int controlsTotal, int controlsImplemented)
    {
        if (controlsTotal <= 0)
        {
            return new ReadinessScoreDto(null, 0, 0, "Not started");
        }

        var boundedImplemented = Math.Clamp(controlsImplemented, 0, controlsTotal);
        var score = Math.Clamp((int)Math.Round(boundedImplemented * 100m / controlsTotal, MidpointRounding.AwayFromZero), 0, 100);
        var status = score switch
        {
            >= 90 => "Ready",
            >= 70 => "Needs attention",
            _ => "At risk"
        };

        return new ReadinessScoreDto(score, controlsTotal, boundedImplemented, status);
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

import { type CSSProperties } from "react";
import { type ComplianceOverview } from "@/lib/api";
import "./ControlCoverageMeter.css";

export function ControlCoverageMeter({
  readinessScore
}: {
  readinessScore: ComplianceOverview["readinessScore"];
}) {
  const { controlsApplicable, controlsImplemented, controlsNotApplicable, score } = readinessScore;
  const hasScore = score !== null;
  const boundedScore = hasScore ? Math.min(100, Math.max(0, score)) : 0;
  const meterStyle = { "--readiness-score-value": `${boundedScore}%` } as CSSProperties;
  const classes = ["readiness-score-meter", hasScore ? undefined : "readiness-score-meter--empty"].filter(Boolean).join(" ");
  const coverageText = hasScore
    ? `${controlsImplemented} of ${controlsApplicable} applicable controls implemented`
    : controlsNotApplicable > 0
      ? `${controlsNotApplicable} controls marked not applicable`
      : "No scoped controls";

  return (
    <span className={classes} style={meterStyle}>
      <span
        className="readiness-score-meter__bar"
        role={hasScore ? "progressbar" : "status"}
        aria-label={hasScore ? "Applicable control implementation coverage" : "Control implementation coverage unavailable"}
        aria-valuemax={hasScore ? 100 : undefined}
        aria-valuemin={hasScore ? 0 : undefined}
        aria-valuenow={hasScore ? boundedScore : undefined}
        aria-valuetext={hasScore ? coverageText : undefined}
      >
        <span className="readiness-score-meter__fill" aria-hidden="true" />
      </span>
      <span className="readiness-score-meter__value">{hasScore ? `${boundedScore}%` : "N/A"}</span>
      <span className="readiness-score-meter__caption">
        {coverageText}
        {hasScore && controlsNotApplicable > 0 ? ` · ${controlsNotApplicable} N/A excluded` : ""}
      </span>
    </span>
  );
}

export type ControlCoverageTone = "neutral" | "success" | "warning" | "danger";

export function controlCoverageTone(status: string): ControlCoverageTone {
  const normalized = status.toLowerCase();
  if (normalized.includes("high")) return "success";
  if (normalized.includes("moderate")) return "warning";
  if (normalized.includes("low")) return "danger";
  return "neutral";
}

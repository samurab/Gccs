import { cleanup, render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import { ClassificationReviewPanel } from "./ClassificationReviewPanel";
import {
  createCuiSupportEscalation, getClassifiedContent, getClassifiedContentDetail, getClassificationHistory,
  getCuiSupportEscalations, resolveCuiSupportEscalation, reviewContentClassification,
  type ClassifiedContent, type ClassificationHistory, type CuiSupportEscalation
} from "@/lib/api";
vi.mock("@/lib/api", () => ({
  getClassifiedContent: vi.fn(), getClassifiedContentDetail: vi.fn(), getClassificationHistory: vi.fn(),
  getCuiSupportEscalations: vi.fn(), reviewContentClassification: vi.fn(),
  createCuiSupportEscalation: vi.fn(), resolveCuiSupportEscalation: vi.fn()
}));
const item: ClassifiedContent = { id: "item-1", title: "Synthetic review item", entityType: "EvidenceItem", revision: 7,
  createdAt: "2026-09-08T12:00:00Z", classification: { classification: "Unknown", source: "UserSelected",
    confidence: null, reviewedByUserId: null, reviewedAt: null, reason: "Original reason", isApprovedDemoContent: false } };
const reviewed: ClassifiedContent = { ...item, revision: 8, classification: { ...item.classification,
  classification: "Fci", source: "AdminReviewed", reviewedByUserId: "server-reviewer", reviewedAt: "2026-09-08T13:00:00Z", reason: "Verified metadata" } };
const entry: ClassificationHistory = { id: "history-1", revision: 8, previousClassification: "Unknown", newClassification: "Fci",
  source: "AdminReviewed", confidence: null, reviewedByUserId: "server-reviewer", reviewedAt: "2026-09-08T13:00:00Z",
  reason: "Verified metadata", changedByUserId: "server-reviewer", changedAt: "2026-09-08T13:00:00Z", previousMetadata: item.classification };
const escalation: CuiSupportEscalation = { id: "escalation-1", tenantId: "tenant-1", sourceWorkflow: "ClassificationReview",
  affectedEntityType: "EvidenceItem", affectedEntityId: item.id, category: "ProhibitedData", severity: "High", status: "Submitted",
  owner: null, description: "Synthetic metadata concern", isAffectedContentBlocked: true, statusNote: null,
  statusChangedAt: null, statusChangedByUserId: null, createdAt: "2026-09-08T12:00:00Z", createdByUserId: "admin",
  updatedAt: null, updatedByUserId: null, resolutions: [] };
beforeEach(() => {
  vi.resetAllMocks();
  vi.mocked(getClassifiedContent).mockResolvedValue([item]);
  vi.mocked(getClassifiedContentDetail).mockResolvedValue(item);
  vi.mocked(getClassificationHistory).mockResolvedValue([]);
  vi.mocked(getCuiSupportEscalations).mockResolvedValue([]);
  vi.mocked(reviewContentClassification).mockResolvedValue({ data: reviewed, error: null });
});
afterEach(cleanup);
async function expand() {
  const user = userEvent.setup();
  await user.click(screen.getByText("Classification review and history", { selector: "summary" }));
  await user.click(await screen.findByRole("button", { name: "Inspect Synthetic review item" }));
  await screen.findByRole("article", { name: "Current classification detail" });
  return user;
}
it.each([
  ["evidence", "evidence-items", "ViewEvidence", "ApproveEvidence"],
  ["evidence", "evidence-file-versions", "ViewEvidence", "ApproveEvidence"],
  ["evidence", "notes", "ViewEvidence", "ApproveEvidence"],
  ["contracts", "contract-documents", "ViewContracts", "ReviewClauses"],
  ["contracts", "extraction-jobs", "ViewContracts", "ReviewClauses"],
  ["reports", "reports", "ViewReports", "ManageReports"]
] as const)("reviews %s / %s using the server revision and shows history", async (group, route, read, write) => {
  const onChanged = vi.fn();
  render(<ClassificationReviewPanel group={group} tenantId="tenant-1" permissions={[read, write]} onChanged={onChanged} />);
  const user = userEvent.setup();
  await user.click(screen.getByText("Classification review and history", { selector: "summary" }));
  await user.selectOptions(screen.getByLabelText("Content type"), route);
  await user.click(await screen.findByRole("button", { name: "Inspect Synthetic review item" }));
  await screen.findByLabelText("Reviewed classification");
  expect(screen.getByRole("button", { name: "Save classification review" })).toBeDisabled();
  await user.selectOptions(screen.getByLabelText("Reviewed classification"), "Fci");
  await user.type(screen.getByLabelText("Review reason"), "Verified metadata");
  vi.mocked(getClassificationHistory).mockResolvedValue([entry]);
  await user.click(screen.getByRole("button", { name: "Save classification review" }));
  expect(reviewContentClassification).toHaveBeenCalledWith(route, item.id, 7, "Fci", "Verified metadata");
  expect(await screen.findByText("Revision 8: Unknown → Fci")).toBeVisible();
  expect(onChanged).toHaveBeenCalledWith(reviewed);
  await user.click(screen.getByText("Previous classification metadata", { selector: "summary" }));
  expect(screen.getByText("Original reason")).toBeVisible();
});
it("allows inspection without exposing reviewer or admin actions to a read-only role", async () => {
  render(<ClassificationReviewPanel group="evidence" tenantId="tenant-1" permissions={["ViewEvidence"]} onChanged={vi.fn()} />);
  await expand();
  expect(screen.queryByRole("button", { name: "Save classification review" })).not.toBeInTheDocument();
  expect(getCuiSupportEscalations).not.toHaveBeenCalled();
  expect(within(screen.getByRole("article")).getByText("Unknown")).toBeVisible();
});
it("keeps escalation management separate from classification review permission", async () => {
  vi.mocked(getClassifiedContentDetail).mockResolvedValue({ ...item, classification: { ...item.classification, classification: "Prohibited" } });
  render(<ClassificationReviewPanel group="evidence" tenantId="tenant-1" permissions={["ViewEvidence", "ApproveEvidence"]} onChanged={vi.fn()} />);
  await expand();
  expect(screen.getByRole("button", { name: "Save classification review" })).toBeVisible();
  expect(screen.queryByRole("button", { name: "Escalate restricted content" })).not.toBeInTheDocument();
  expect(screen.getByText(/Ask a tenant Owner to open/)).toBeVisible();
  expect(getCuiSupportEscalations).not.toHaveBeenCalled();
});
it("displays imported synthetic provenance without allowing reviewers to assign it", async () => {
  vi.mocked(getClassifiedContentDetail).mockResolvedValue({ ...item, classification: { ...item.classification, classification: "SyntheticCui", isApprovedDemoContent: true } });
  render(<ClassificationReviewPanel group="evidence" tenantId="tenant-1" permissions={["ViewEvidence", "ApproveEvidence"]} onChanged={vi.fn()} />);
  const user = await expand();
  expect(screen.getByLabelText("Reviewed classification")).toHaveValue("SyntheticCui");
  await user.type(screen.getByLabelText("Review reason"), "Metadata review");
  expect(screen.getByRole("button", { name: "Save classification review" })).toBeDisabled();
  await user.selectOptions(screen.getByLabelText("Reviewed classification"), "Fci");
  expect(screen.getByRole("button", { name: "Save classification review" })).toBeEnabled();
});
it("fails closed when read permissions are absent", () => {
  render(<ClassificationReviewPanel group="reports" tenantId="tenant-1" permissions={[]} onChanged={vi.fn()} />);
  expect(screen.getByText(/do not have permission/)).toBeVisible();
  expect(getClassifiedContent).not.toHaveBeenCalled();
});
it("distinguishes load failure from empty results and allows retry", async () => {
  vi.mocked(getClassifiedContent).mockRejectedValueOnce(new Error("API unavailable"));
  render(<ClassificationReviewPanel group="evidence" tenantId="tenant-1" permissions={["ViewEvidence"]} onChanged={vi.fn()} />);
  const user = userEvent.setup(); await user.click(screen.getByText("Classification review and history", { selector: "summary" }));
  expect(await screen.findByRole("alert")).toHaveTextContent("API unavailable");
  expect(screen.queryByText("No classification records match this page.")).not.toBeInTheDocument();
  vi.mocked(getClassifiedContent).mockResolvedValue([]);
  await user.click(screen.getByRole("button", { name: "Refresh classification list" }));
  expect(await screen.findByText("No classification records match this page.")).toBeVisible();
});
it("requires reloading after a stale or uncertain write instead of overwriting", async () => {
  vi.mocked(reviewContentClassification).mockResolvedValue({ data: null, error: "A newer revision exists." });
  render(<ClassificationReviewPanel group="evidence" tenantId="tenant-1" permissions={["ViewEvidence", "ApproveEvidence"]} onChanged={vi.fn()} />);
  const user = await expand();
  await user.type(screen.getByLabelText("Review reason"), "Review reason");
  await user.click(screen.getByRole("button", { name: "Save classification review" }));
  expect(await screen.findByText(/A newer revision exists/)).toBeVisible();
  expect(screen.getByRole("button", { name: "Save classification review" })).toBeDisabled();
  expect(reviewContentClassification).toHaveBeenCalledTimes(1);
  vi.mocked(getClassifiedContentDetail).mockResolvedValue(reviewed);
  await user.click(screen.getByRole("button", { name: "Reload current item" }));
  expect(await screen.findByText(/revision 8/)).toBeVisible();
});
it("invalidates cached content immediately even when history refresh fails after save", async () => {
  const onChanged = vi.fn();
  render(<ClassificationReviewPanel group="evidence" tenantId="tenant-1" permissions={["ViewEvidence", "ApproveEvidence"]} onChanged={onChanged} />);
  const user = await expand();
  vi.mocked(getClassificationHistory).mockRejectedValue(new Error("History unavailable"));
  await user.type(screen.getByLabelText("Review reason"), "Verified metadata");
  await user.click(screen.getByRole("button", { name: "Save classification review" }));
  expect(await screen.findByText(/verify whether the review was saved/)).toBeVisible();
  expect(onChanged).toHaveBeenCalledWith(reviewed);
});
it("routes prohibited content to an admin escalation without submitting content bytes", async () => {
  const prohibited = { ...item, classification: { ...item.classification, classification: "Prohibited" } };
  vi.mocked(getClassifiedContentDetail).mockResolvedValue(prohibited);
  vi.mocked(createCuiSupportEscalation).mockResolvedValue({ data: escalation, error: null });
  render(<ClassificationReviewPanel group="evidence" tenantId="tenant-1" permissions={["ViewEvidence", "ManageTenant"]} onChanged={vi.fn()} />);
  const user = await expand();
  await user.type(screen.getByLabelText("Escalation or resolution reason"), "Synthetic metadata concern");
  await user.click(screen.getByRole("button", { name: "Escalate restricted content" }));
  expect(createCuiSupportEscalation).toHaveBeenCalledWith("tenant-1", {
    sourceWorkflow: "ClassificationReview", affectedEntityType: "EvidenceItem", affectedEntityId: "item-1",
    category: "ProhibitedData", severity: "High", description: "Synthetic metadata concern"
  });
  expect(await screen.findByText(/Escalation escalation-1/)).toBeVisible();
  expect(screen.getByRole("button", { name: "Resolve reviewed false positive" })).toBeDisabled();
  expect(screen.queryByRole("button", { name: "Escalate restricted content" })).not.toBeInTheDocument();
});
it("resolves a reviewed false positive separately from reclassification", async () => {
  vi.mocked(getClassifiedContentDetail).mockResolvedValue(reviewed);
  vi.mocked(getCuiSupportEscalations).mockResolvedValue([escalation]);
  vi.mocked(resolveCuiSupportEscalation).mockResolvedValue({ data: { ...escalation, status: "Resolved", isAffectedContentBlocked: false }, error: null });
  render(<ClassificationReviewPanel group="evidence" tenantId="tenant-1" permissions={["ViewEvidence", "ManageTenant"]} onChanged={vi.fn()} />);
  const user = await expand();
  await user.type(screen.getByLabelText("Escalation or resolution reason"), "Confirmed safe review after escalation");
  await user.click(screen.getByRole("button", { name: "Resolve reviewed false positive" }));
  expect(resolveCuiSupportEscalation).toHaveBeenCalledWith("tenant-1", "escalation-1", {
    resolutionType: "FalsePositive", summary: "Confirmed safe review after escalation"
  });
  expect(await screen.findByText("Reviewed false-positive escalation resolved.")).toBeVisible();
});

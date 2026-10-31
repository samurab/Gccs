import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { AiOutputGovernancePanel } from "./AiOutputGovernancePanel";

const mocks = vi.hoisted(() => ({ list: vi.fn(), history: vi.fn(), review: vi.fn(), link: vi.fn(), exportLogs: vi.fn() }));
vi.mock("@/lib/api", () => ({
  getAiOutputs: mocks.list,
  getAiOutputReviewHistory: mocks.history,
  reviewAiOutput: mocks.review,
  linkAiOutputToDeliverable: mocks.link,
  exportAiOutputs: mocks.exportLogs
}));

const output = {
  id: "answer-1", tenantId: "tenant-1", workflowContext: "obligation", status: "Draft",
  answer: "Source-backed draft.", citations: [{ sourceId: "source-1", title: "FAR source", sourceType: "ComplianceLibrary",
    sourceUrl: "https://example.test", tenantRecordReference: null, excerptPointer: "section 1", version: "1", lastReviewedAt: null }],
  supportStatus: "SourceSupported", draftLabel: "Draft", requiresReview: true, escalationRecommended: false,
  blockedReason: null, createdAt: "2026-09-12T10:00:00Z", humanReviewStatus: "pending",
  reviewedByUserId: null, reviewedAt: null, reviewDecision: null, reviewNotes: null,
  prompt: "Explain the requirement.", promptWasRedacted: false, promptMetadata: "{}", modelConfiguration: "{}",
  retrievalPolicy: "[]", classification: "Unclassified", result: "Draft", reviewState: "Draft",
  rejectionReason: null, retainUntil: "2027-09-12T10:00:00Z", version: 0
};

describe("AiOutputGovernancePanel", () => {
  afterEach(() => { cleanup(); vi.unstubAllGlobals(); });
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.list.mockResolvedValue([output]);
    mocks.history.mockResolvedValue([{ id: "review-1", tenantId: "tenant-1", answerId: "answer-1",
      previousState: "Draft", newState: "Approved", reviewerUserId: "reviewer-1", note: "Sources verified.",
      rejectionReason: null, createdAt: "2026-09-12T11:00:00Z" }]);
  });

  it("loads tenant-scoped output and append-only review history", async () => {
    const user = userEvent.setup();
    render(<AiOutputGovernancePanel permissions={["ViewObligations"]} />);
    expect(await screen.findByText("Source-backed draft.")).toBeVisible();
    await user.click(screen.getByRole("button", { name: "Load review history" }));
    expect(await screen.findByText(/Draft → Approved/)).toBeVisible();
    expect(screen.queryByRole("button", { name: "Save AI output decision" })).not.toBeInTheDocument();
  });

  it("records a review using the current optimistic version", async () => {
    const approved = { ...output, reviewState: "Approved", version: 1, humanReviewStatus: "Approved" };
    mocks.review.mockResolvedValue({ data: { answer: approved, review: { id: "review-1", newState: "Approved", createdAt: "2026-09-12T11:00:00Z" } }, error: null });
    const user = userEvent.setup();
    render(<AiOutputGovernancePanel permissions={["ViewObligations", "ManageObligations"]} />);
    await screen.findByText("Source-backed draft.");
    await user.selectOptions(screen.getByLabelText("Decision"), "Approved");
    await user.type(screen.getByLabelText("Review note"), "Sources verified.");
    await user.click(screen.getByRole("button", { name: "Save AI output decision" }));
    await waitFor(() => expect(mocks.review).toHaveBeenCalledWith("answer-1", "Approved", "Sources verified.", null, 0));
    expect(await screen.findByText(/is now Approved/)).toBeVisible();
  });

  it("links approved output and exports logs only with the corresponding controls", async () => {
    mocks.list.mockResolvedValue([{ ...output, reviewState: "Approved", version: 1 }]);
    mocks.link.mockResolvedValue({ data: { id: "usage-1", tenantId: "tenant-1", answerId: "answer-1",
      deliverableType: "Report", deliverableId: "34234234-4234-4234-8234-4234234234aa", linkedByUserId: "user-1",
      linkedAt: "2026-09-12T12:00:00Z" }, error: null });
    mocks.exportLogs.mockResolvedValue({ tenantId: "tenant-1", logCount: 1, exportedAt: "2026-09-12T12:00:00Z", logs: [] });
    const createObjectURL = vi.fn(() => "blob:ai-logs"); const revokeObjectURL = vi.fn();
    vi.stubGlobal("URL", { ...URL, createObjectURL, revokeObjectURL });
    vi.spyOn(HTMLAnchorElement.prototype, "click").mockImplementation(() => undefined);
    const user = userEvent.setup();
    render(<AiOutputGovernancePanel permissions={["ViewObligations", "ManageReports", "ExportReports"]} />);
    await screen.findByText("Source-backed draft.");
    await user.type(screen.getByLabelText("Existing deliverable ID"), "34234234-4234-4234-8234-4234234234aa");
    await user.click(screen.getByRole("button", { name: "Link approved output" }));
    await waitFor(() => expect(mocks.link).toHaveBeenCalledWith("answer-1", "Report", "34234234-4234-4234-8234-4234234234aa"));
    await user.click(screen.getByRole("button", { name: "Export AI logs" }));
    await waitFor(() => expect(mocks.exportLogs).toHaveBeenCalledWith(false));
    expect(revokeObjectURL).toHaveBeenCalledWith("blob:ai-logs");
  });
});

import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ExpertReviewQueuePanel } from "./ExpertReviewQueuePanel";

const mocks = vi.hoisted(() => ({ list: vi.fn(), candidates: vi.fn(), assign: vi.fn(), resolve: vi.fn(), review: vi.fn() }));
vi.mock("@/lib/api", () => ({
  assignExpertReviewItem: mocks.assign,
  getAssistantExpertReviewItems: mocks.list,
  getObligationAssignmentCandidates: mocks.candidates,
  resolveExpertReviewItem: mocks.resolve,
  reviewAiOutput: mocks.review
}));

const openItem = {
  id: "review-1", tenantId: "tenant-1", sourceType: "assistant_answer", sourceId: "answer-1",
  reason: "Confirm the interpretation.", priority: "medium", topic: "Obligation assistant answer review",
  assignedExpertUserId: null, dueAt: null, status: "open", createdByUserId: "user-1",
  createdAt: "2026-09-12T10:00:00Z", resolvedByUserId: null, resolvedAt: null,
  resolutionDecision: null, resolutionNotes: null
};

describe("ExpertReviewQueuePanel", () => {
  afterEach(cleanup);
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.list.mockResolvedValue([{ reviewItem: openItem, answer: {
      id: "answer-1", draftLabel: "Draft", supportStatus: "SourceSupported", answer: "Bounded answer.",
      humanReviewStatus: "queued",
      prompt: "Explain the safeguard.", promptWasRedacted: false, classification: "Unclassified",
      retainUntil: "2027-09-12T10:00:00Z", reviewState: "Draft", version: 0,
      citations: [{ sourceId: "source-1", title: "FAR 52.204-21", excerptPointer: "section", version: "2026.1" }]
    } }]);
    mocks.candidates.mockResolvedValue([{ userId: "expert-1", displayName: "Expert One" }]);
  });

  it("loads the tenant-scoped assistant queue and resolves an item", async () => {
    mocks.resolve.mockResolvedValue({ data: { ...openItem, status: "resolved", resolutionDecision: "revision_required", resolutionNotes: "Update the draft." }, error: null });
    const user = userEvent.setup();
    render(<ExpertReviewQueuePanel canResolve />);

    expect(await screen.findByText("Confirm the interpretation.")).toBeInTheDocument();
    expect(mocks.list).toHaveBeenCalledWith();
    expect(await screen.findByText("Bounded answer.")).toBeInTheDocument();
    await user.selectOptions(screen.getByLabelText("Decision"), "revision_required");
    await user.type(screen.getByLabelText("Resolution notes"), "Update the draft.");
    await user.click(screen.getByRole("button", { name: "Resolve review item" }));

    await waitFor(() => expect(mocks.resolve).toHaveBeenCalledWith("review-1", "revision_required", "Update the draft."));
    expect(screen.getByText((_, element) => element?.tagName === "P" && element.textContent?.includes("revision_required") === true)).toBeInTheDocument();
  });

  it("assigns an active tenant reviewer and due date", async () => {
    mocks.assign.mockResolvedValue({ data: { ...openItem, assignedExpertUserId: "expert-1", dueAt: "2026-12-01" }, error: null });
    const user = userEvent.setup();
    render(<ExpertReviewQueuePanel canResolve />);
    await screen.findByText("Confirm the interpretation.");
    await user.selectOptions(screen.getByLabelText("Assigned expert"), "expert-1");
    await user.type(screen.getByLabelText("Due date"), "2026-12-01");
    await user.click(screen.getByRole("button", { name: "Assign expert" }));
    await waitFor(() => expect(mocks.assign).toHaveBeenCalledWith("review-1", "expert-1", "2026-12-01"));
    expect(screen.getByText(/reviewer was notified/i)).toBeInTheDocument();
  });

  it("is read-only without manage permission", async () => {
    render(<ExpertReviewQueuePanel canResolve={false} />);
    expect(await screen.findByText("Confirm the interpretation.")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Resolve review item" })).not.toBeInTheDocument();
  });

  it("refreshes when an assistant escalation is created in the same workspace", async () => {
    render(<ExpertReviewQueuePanel canResolve />);
    expect(await screen.findByText("Confirm the interpretation.")).toBeInTheDocument();

    window.dispatchEvent(new Event("assistant-expert-review-routed"));

    await waitFor(() => expect(mocks.list).toHaveBeenCalledTimes(2));
  });

  it("records a governed AI output approval with the current version", async () => {
    mocks.review.mockResolvedValue({ data: { answer: {
      id: "answer-1", draftLabel: "Draft", supportStatus: "SourceSupported", answer: "Bounded answer.",
      humanReviewStatus: "Approved", prompt: "Explain the safeguard.", promptWasRedacted: false,
      classification: "Unclassified", retainUntil: "2027-09-12T10:00:00Z", reviewState: "Approved", version: 1,
      citations: []
    }, review: { id: "decision-1", newState: "Approved", createdAt: "2026-09-12T12:00:00Z" } }, error: null });
    const user = userEvent.setup();
    render(<ExpertReviewQueuePanel canResolve />);
    await screen.findByText("Explain the safeguard.");
    await user.selectOptions(screen.getByLabelText("AI output decision"), "Approved");
    await user.type(screen.getByLabelText("AI review note"), "Sources verified.");
    await user.click(screen.getByRole("button", { name: "Save AI output decision" }));
    await waitFor(() => expect(mocks.review).toHaveBeenCalledWith("answer-1", "Approved", "Sources verified.", null, 0));
    expect(screen.getByText(/now Approved/)).toBeInTheDocument();
  });
});

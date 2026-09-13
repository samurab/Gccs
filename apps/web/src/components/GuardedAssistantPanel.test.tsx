import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { GuardedAssistantPanel } from "./GuardedAssistantPanel";

const mocks = vi.hoisted(() => ({ ask: vi.fn(), action: vi.fn(), feedback: vi.fn(), escalation: vi.fn() }));
vi.mock("@/lib/api", () => ({
  askAssistant: mocks.ask,
  createAssistantDraftAction: mocks.action,
  escalateAssistantAnswer: mocks.escalation,
  submitAssistantFeedback: mocks.feedback
}));

const supportedAnswer = {
  id: "answer-1", tenantId: "tenant-1", workflowContext: "obligation", status: "Draft",
  answer: "FCI safeguarding requires basic controls.", supportStatus: "SourceSupported", draftLabel: "Draft",
  requiresReview: true, escalationRecommended: false, blockedReason: null, createdAt: "2026-09-12T10:00:00Z",
  citations: [{ sourceId: "far", title: "FAR 52.204-21", sourceType: "ComplianceLibrary", sourceUrl: "https://acquisition.gov/far/52.204-21",
    tenantRecordReference: null, excerptPointer: "section", version: "2026.1", lastReviewedAt: "2026-09-01" }]
};

describe("GuardedAssistantPanel", () => {
  afterEach(cleanup);
  beforeEach(() => { vi.clearAllMocks(); mocks.ask.mockResolvedValue({ data: supportedAnswer, error: null }); });

  it("shows draft, support, review requirement, and citations", async () => {
    const user = userEvent.setup();
    render(<GuardedAssistantPanel contexts={["obligation"]} permissions={["ManageTasks"]} />);
    await user.type(screen.getByLabelText("Question"), "Explain FCI safeguarding.");
    await user.click(screen.getByRole("button", { name: "Ask assistant" }));
    expect(await screen.findByText("SourceSupported")).toBeInTheDocument();
    expect(screen.getByText("Human review required")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Open source" })).toHaveAttribute("href", "https://acquisition.gov/far/52.204-21");
  });

  it("renders blocked answers without draft creation controls and routes review", async () => {
    mocks.ask.mockResolvedValue({ data: { ...supportedAnswer, status: "Blocked", draftLabel: "Blocked", supportStatus: "Unsupported",
      blockedReason: "classified-content", citations: [], escalationRecommended: true }, error: null });
    mocks.escalation.mockResolvedValue({ data: { created: true, reviewItem: { id: "review-1" }, feedback: { id: "feedback-1" } }, error: null });
    const user = userEvent.setup();
    render(<GuardedAssistantPanel contexts={["contract", "labor"]} permissions={["ManageTasks", "ManageObligations"]} />);
    await user.type(screen.getByLabelText("Question"), "Analyze this classified document.");
    await user.click(screen.getByRole("button", { name: "Ask assistant" }));
    expect(await screen.findByRole("alert")).toHaveTextContent("classified-content");
    expect(screen.queryByRole("button", { name: "Save draft" })).not.toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Route for expert review" }));
    await waitFor(() => expect(mocks.escalation).toHaveBeenCalledWith("answer-1", expect.any(String)));
    expect(screen.getByText(/added to the operational queue/i)).toBeInTheDocument();
  });

  it("renders an unsupported refusal without citations or draft actions", async () => {
    mocks.ask.mockResolvedValue({ data: { ...supportedAnswer, status: "NeedsReview", draftLabel: "NeedsReview",
      supportStatus: "NeedsReview", answer: "I do not have an approved source that supports an answer. Please route this question for human review.",
      citations: [], escalationRecommended: true }, error: null });
    const user = userEvent.setup();
    render(<GuardedAssistantPanel contexts={["evidence"]} permissions={["ManageEvidence"]} />);
    await user.type(screen.getByLabelText("Question"), "Explain an unsupported requirement.");
    await user.click(screen.getByRole("button", { name: "Ask assistant" }));
    expect(await screen.findByText(/do not have an approved source/i)).toBeInTheDocument();
    expect(screen.getByText(/No approved citation supports this answer/i)).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Save draft" })).not.toBeInTheDocument();
  });

  it("hides action controls when server-provided permissions do not allow creation", async () => {
    const user = userEvent.setup();
    render(<GuardedAssistantPanel contexts={["evidence"]} permissions={["ViewEvidence"]} />);
    await user.type(screen.getByLabelText("Question"), "What evidence is supported?");
    await user.click(screen.getByRole("button", { name: "Ask assistant" }));
    expect(await screen.findByText(/role cannot create assistant drafts/i)).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Save draft" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Route for expert review" })).not.toBeInTheDocument();
  });
});

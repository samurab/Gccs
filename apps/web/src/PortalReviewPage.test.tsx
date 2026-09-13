import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { PortalReviewPage } from "./PortalReviewPage";

const mocks = vi.hoisted(() => ({
  list: vi.fn(),
  message: vi.fn(),
  download: vi.fn()
}));

vi.mock("./lib/api", () => ({
  getPortalReviewPackages: mocks.list,
  createPortalReviewMessage: mocks.message,
  downloadPortalReviewPackage: mocks.download
}));

const invitationId = "34234234-4234-4234-8234-4234234234aa";
const sharedPackageId = "34234234-4234-4234-8234-4234234234bb";
const packageItem = {
  sharedPackageId,
  packageId: "34234234-4234-4234-8234-4234234234cc",
  sourceKind: "Report:CmmcReadiness",
  title: "CMMC readiness package",
  version: 2,
  status: "Approved",
  classification: "Fci",
  contractId: null,
  evidenceItemIds: ["34234234-4234-4234-8234-4234234234dd"],
  evidenceReferences: [{
    id: "34234234-4234-4234-8234-4234234234dd",
    name: "Approved access-control policy",
    type: "Policy",
    classification: "Fci",
    approvedAt: "2026-09-10T12:00:00Z",
    expiresAt: null
  }],
  generatedAt: "2026-09-12T12:00:00Z",
  reviewDueAt: "2026-10-12T12:00:00Z",
  externalReviewApprovedAt: "2026-09-12T12:30:00Z",
  approvedSourceVersion: 2,
  approvedSourceFingerprint: "a".repeat(64),
  shareState: "Active",
  downloadAvailable: true,
  reviewerMessages: []
};

describe("PortalReviewPage", () => {
  afterEach(() => cleanup());
  beforeEach(() => {
    window.history.pushState({}, "", `/portal/review/${invitationId}`);
    mocks.list.mockReset(); mocks.message.mockReset(); mocks.download.mockReset();
    mocks.list.mockResolvedValue([packageItem]);
  });

  it("shows only the server-approved package projection and its evidence references", async () => {
    render(<PortalReviewPage />);
    expect(screen.getByRole("status")).toHaveTextContent("Loading approved packages");
    expect(await screen.findByRole("heading", { name: packageItem.title })).toBeVisible();
    expect(screen.getByText("Approved access-control policy")).toBeInTheDocument();
    expect(screen.getByText(packageItem.evidenceItemIds[0])).toBeInTheDocument();
    expect(screen.getByText("No-CUI review boundary")).toBeVisible();
  });

  it("adds a reviewer question without exposing workspace mutation controls", async () => {
    const user = userEvent.setup();
    mocks.message.mockResolvedValue({ data: {
      id: "message-1", tenantId: "tenant-1", invitationId, sharedPackageId,
      packageId: packageItem.packageId, actorUserId: "reviewer-1", kind: "Question",
      body: "Which control supports this evidence?", createdAt: "2026-09-12T13:00:00Z"
    }, error: null });
    render(<PortalReviewPage />);
    await screen.findByRole("heading", { name: packageItem.title });
    await user.type(screen.getByLabelText("Message"), "Which control supports this evidence?");
    await user.click(screen.getByRole("button", { name: "Add reviewer message" }));
    expect(await screen.findByText("Reviewer message saved.")).toBeVisible();
    expect(screen.getByText("Which control supports this evidence?")).toBeVisible();
    expect(screen.queryByRole("button", { name: /edit|delete|approve/i })).not.toBeInTheDocument();
  });

  it("shows controlled empty and authorization-error states", async () => {
    mocks.list.mockResolvedValueOnce([]);
    const view = render(<PortalReviewPage />);
    expect(await screen.findByText("No approved packages are currently assigned to this invitation.")).toBeVisible();

    mocks.list.mockRejectedValueOnce(new Error("The requested package is unavailable."));
    view.unmount();
    render(<PortalReviewPage />);
    expect(await screen.findByRole("alert")).toHaveTextContent("unavailable");
  });

  it("fails closed for an invalid invitation route without issuing an API request", () => {
    window.history.pushState({}, "", "/portal/review/");
    render(<PortalReviewPage />);
    expect(screen.getByRole("alert")).toHaveTextContent("invitation link is invalid");
    expect(mocks.list).not.toHaveBeenCalled();
  });

  it("requests a controlled package download", async () => {
    const createObjectUrl = vi.fn(() => "blob:portal-package");
    const revokeObjectUrl = vi.fn();
    vi.stubGlobal("URL", { ...URL, createObjectURL: createObjectUrl, revokeObjectURL: revokeObjectUrl });
    const click = vi.spyOn(HTMLAnchorElement.prototype, "click").mockImplementation(() => undefined);
    mocks.download.mockResolvedValue({ data: { blob: new Blob(["package"]), fileName: "package.html" }, error: null });
    render(<PortalReviewPage />);
    await screen.findByRole("heading", { name: packageItem.title });
    fireEvent.click(screen.getByRole("button", { name: "Download controlled package" }));
    await waitFor(() => expect(mocks.download).toHaveBeenCalledWith(invitationId, sharedPackageId));
    expect(click).toHaveBeenCalled();
    expect(revokeObjectUrl).toHaveBeenCalledWith("blob:portal-package");
    click.mockRestore();
    vi.unstubAllGlobals();
  });
});

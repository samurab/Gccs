import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ExternalPortalInvitationPanel } from "./ExternalPortalInvitationPanel";

const mocks = vi.hoisted(() => ({
  list: vi.fn(), create: vi.fn(), resend: vi.fn(), extend: vi.fn(), revoke: vi.fn(), history: vi.fn()
}));

vi.mock("@/lib/api", () => ({
  getExternalPortalInvitations: mocks.list,
  createExternalPortalInvitation: mocks.create,
  resendExternalPortalInvitation: mocks.resend,
  extendExternalPortalInvitation: mocks.extend,
  revokeExternalPortalInvitation: mocks.revoke,
  getExternalPortalAccessHistory: mocks.history
}));

const invitation = {
  id: "34100000-0000-0000-0000-000000000001",
  tenantId: "34100000-0000-0000-0000-000000000002",
  email: "reviewer@example.test",
  role: "PrimeReviewer" as const,
  packageIds: ["34100000-0000-0000-0000-000000000003"],
  contractIds: ["34100000-0000-0000-0000-000000000004"],
  expiresAt: "2026-10-31T23:59:59Z",
  canDownload: true,
  strongAuthenticationRequired: true,
  status: "Pending" as const,
  externalUserId: null,
  lastAccessedAt: null,
  revokedAt: null,
  revocationReason: null,
  resendCount: 0,
  lastResentAt: null,
  version: 1,
  createdAt: "2026-09-12T12:00:00Z",
  updatedAt: null
};

describe("ExternalPortalInvitationPanel", () => {
  afterEach(cleanup);
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.list.mockResolvedValue([invitation]);
    mocks.history.mockResolvedValue([]);
  });

  it("shows scoped read-only invitation posture", async () => {
    render(<ExternalPortalInvitationPanel />);
    expect(await screen.findByText(/reviewer@example\.test · Prime reviewer/i)).toBeInTheDocument();
    expect(screen.getByText(/1 package.*1 contract scope/i)).toBeInTheDocument();
    expect(screen.getByText(/does not authorize CUI sharing/i)).toBeInTheDocument();
    expect(screen.getByText(/Strong authentication required/i)).toBeInTheDocument();
  });

  it("creates an invitation with normalized package and contract lists", async () => {
    mocks.list.mockResolvedValue([]);
    mocks.create.mockResolvedValue({ data: invitation, error: null });
    const user = userEvent.setup();
    render(<ExternalPortalInvitationPanel />);

    await user.type(await screen.findByLabelText("Portal reviewer email"), "reviewer@example.test");
    await user.type(screen.getByLabelText("Approved package IDs"), `${invitation.packageIds[0]}, ${invitation.packageIds[0]}`);
    await user.type(screen.getByLabelText("Contract scope IDs"), invitation.contractIds[0]);
    await user.type(screen.getByLabelText("Expiration date"), "2026-10-31");
    await user.click(screen.getByLabelText("Allow approved package downloads"));
    await user.click(screen.getByRole("button", { name: "Create portal invitation" }));

    await waitFor(() => expect(mocks.create).toHaveBeenCalledWith(expect.objectContaining({
      email: "reviewer@example.test",
      role: "PrimeReviewer",
      packageIds: invitation.packageIds,
      contractIds: invitation.contractIds,
      canDownload: true,
      strongAuthenticationRequired: true
    })));
    expect(await screen.findByRole("status")).toHaveTextContent("invitation created");
  });

  it("requires a revocation reason and removes invitation actions after revocation", async () => {
    const revoked = { ...invitation, status: "Revoked" as const, revocationReason: "Review completed." };
    mocks.revoke.mockResolvedValue({ data: revoked, error: null });
    const user = userEvent.setup();
    render(<ExternalPortalInvitationPanel />);

    await user.click(await screen.findByRole("button", { name: "Revoke invitation" }));
    await user.type(screen.getByLabelText("Revocation reason"), "Review completed.");
    await user.click(screen.getByRole("button", { name: "Confirm revoke" }));

    await waitFor(() => expect(mocks.revoke).toHaveBeenCalledWith(invitation.id, "Review completed."));
    expect(screen.getByText(/Revocation reason: Review completed/i)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Record resend" })).toBeDisabled();
  });

  it("fails closed when invitations cannot be loaded", async () => {
    mocks.list.mockRejectedValue(new Error("unavailable"));
    render(<ExternalPortalInvitationPanel />);
    expect(await screen.findByRole("alert")).toHaveTextContent("Portal invitations could not be loaded");
    expect(screen.queryByRole("button", { name: "Create portal invitation" })).not.toBeInTheDocument();
  });
});

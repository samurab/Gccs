import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { InvitationAcceptancePage } from "./InvitationAcceptancePage";

const { getContextMock, acceptMock, selectTenantMock } = vi.hoisted(() => ({
  getContextMock: vi.fn(),
  acceptMock: vi.fn(),
  selectTenantMock: vi.fn()
}));

vi.mock("./lib/api", async () => {
  const actual = await vi.importActual<typeof import("./lib/api")>("./lib/api");
  return {
    ...actual,
    getInvitationAcceptanceContext: getContextMock,
    acceptTenantInvitation: acceptMock,
    selectTenant: selectTenantMock
  };
});

describe("InvitationAcceptancePage", () => {
  beforeEach(() => {
    window.history.replaceState({}, "", "/invitations/accept?token=owner-token");
    getContextMock.mockResolvedValue({
      invitationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      tenantDisplayName: "Aegis Pilot Workspace",
      email: "owner@example.com",
      roleName: "Owner",
      status: "Pending",
      expiresAt: "2099-08-05T12:00:00Z"
    });
    acceptMock.mockResolvedValue({
      data: {
        tenantId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
        status: "Accepted"
      },
      error: null
    });
    selectTenantMock.mockReset();
  });

  afterEach(() => cleanup());

  it("verifies the signed-in owner, accepts once, and selects the activated tenant", async () => {
    const user = userEvent.setup();
    render(<InvitationAcceptancePage />);

    expect(await screen.findByRole("heading", { name: "Aegis Pilot Workspace" })).toBeInTheDocument();
    expect(screen.getByText("owner@example.com")).toBeInTheDocument();
    await user.type(screen.getByLabelText("Display name"), "Pilot Owner");
    await user.click(screen.getByRole("button", { name: "Accept invitation" }));

    expect(acceptMock).toHaveBeenCalledWith("owner-token", "Pilot Owner");
    expect(selectTenantMock).toHaveBeenCalledWith("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    expect(await screen.findByRole("heading", { name: "Workspace activated" })).toBeInTheDocument();
  });

  it("shows a controlled error when the authenticated email does not match", async () => {
    getContextMock.mockRejectedValue(new Error("The authenticated email does not match this invitation."));
    render(<InvitationAcceptancePage />);

    expect(await screen.findByRole("heading", { name: "Invitation unavailable" })).toBeInTheDocument();
    expect(screen.getByText(/does not match this invitation/i)).toBeInTheDocument();
  });
});

import { cleanup, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { InvitationAcceptancePage } from "./InvitationAcceptancePage";

const {
  getContextMock,
  acceptMock,
  selectTenantMock,
  selectDevelopmentInvitationIdentityMock,
  selectDevelopmentTestingContextMock,
  selectMicrosoftEntraAccountMock,
  authSessionState
} = vi.hoisted(() => ({
  getContextMock: vi.fn(),
  acceptMock: vi.fn(),
  selectTenantMock: vi.fn(),
  selectDevelopmentInvitationIdentityMock: vi.fn(),
  selectDevelopmentTestingContextMock: vi.fn(),
  selectMicrosoftEntraAccountMock: vi.fn(),
  authSessionState: { isMsalConfigured: true }
}));

vi.mock("./authSession", async () => {
  const actual = await vi.importActual<typeof import("./authSession")>("./authSession");
  return {
    ...actual,
    get isMsalConfigured() { return authSessionState.isMsalConfigured; },
    selectMicrosoftEntraAccount: selectMicrosoftEntraAccountMock
  };
});

vi.mock("./lib/api", async () => {
  const actual = await vi.importActual<typeof import("./lib/api")>("./lib/api");
  return {
    ...actual,
    getInvitationAcceptanceContext: getContextMock,
    acceptTenantInvitation: acceptMock,
    selectTenant: selectTenantMock,
    selectDevelopmentInvitationIdentity: selectDevelopmentInvitationIdentityMock,
    selectDevelopmentTestingContext: selectDevelopmentTestingContextMock
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
    selectDevelopmentInvitationIdentityMock.mockReset();
    selectDevelopmentTestingContextMock.mockReset();
    selectMicrosoftEntraAccountMock.mockReset().mockResolvedValue(undefined);
    authSessionState.isMsalConfigured = true;
  });

  afterEach(() => {
    cleanup();
    vi.unstubAllEnvs();
  });

  it("presents invitation errors with the danger message treatment", async () => {
    window.history.replaceState({}, "", "/invitations/accept");

    render(<InvitationAcceptancePage />);

    expect(await screen.findByRole("alert")).toHaveClass("invitation-activation-state--error");
    expect(screen.getByRole("alert")).toHaveTextContent("The activation link is missing its invitation token.");
  });

  it("uses the external FeDril brand when an invitation has expired", async () => {
    getContextMock.mockResolvedValueOnce({
      invitationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      tenantDisplayName: "Aegis Pilot Workspace",
      email: "owner@example.com",
      roleName: "Owner",
      status: "Pending",
      expiresAt: "2000-01-01T00:00:00Z"
    });

    render(<InvitationAcceptancePage />);

    const alert = await screen.findByRole("alert");
    expect(alert).toHaveTextContent("Ask a FeDril platform operator to resend it.");
    expect(alert).not.toHaveTextContent("GCCS");
  });

  it("verifies the signed-in owner, accepts once, and selects the activated tenant", async () => {
    authSessionState.isMsalConfigured = false;
    const user = userEvent.setup();
    render(<InvitationAcceptancePage />);

    expect(await screen.findByRole("heading", { name: "Aegis Pilot Workspace" })).toBeInTheDocument();
    expect(screen.getByText("owner@example.com")).toBeInTheDocument();
    await user.type(screen.getByLabelText("Display name"), "Pilot Owner");
    await user.click(screen.getByRole("button", { name: "Accept invitation" }));

    expect(acceptMock).toHaveBeenCalledWith("owner-token", "Pilot Owner");
    expect(selectDevelopmentTestingContextMock).toHaveBeenCalledWith(
      "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
      "Owner",
      null,
      "owner@example.com"
    );
    expect(await screen.findByRole("heading", { name: "Workspace activated" })).toBeInTheDocument();
  });

  it("lets a local tester switch to the invited email after an identity mismatch", async () => {
    authSessionState.isMsalConfigured = false;
    const user = userEvent.setup();
    getContextMock
      .mockRejectedValueOnce(new Error("The authenticated email does not match this invitation."))
      .mockResolvedValueOnce({
        invitationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
        tenantDisplayName: "Aegis Pilot Workspace",
        email: "invitee@example.com",
        roleName: "Contributor",
        status: "Pending",
        expiresAt: "2099-08-05T12:00:00Z"
      });
    render(<InvitationAcceptancePage />);

    expect(await screen.findByRole("heading", { name: "Use invited test identity" })).toBeInTheDocument();
    await user.type(screen.getByLabelText("Invited email"), "invitee@example.com");
    await user.click(screen.getByRole("button", { name: "Continue as invitee" }));

    expect(selectDevelopmentInvitationIdentityMock).toHaveBeenCalledWith("invitee@example.com");
    expect(await screen.findByRole("heading", { name: "Aegis Pilot Workspace" })).toBeInTheDocument();
  });

  it("lets a staging user switch Microsoft accounts without weakening invitation validation", async () => {
    vi.stubEnv("DEV", false);
    const user = userEvent.setup();
    getContextMock.mockRejectedValueOnce(new Error("The authenticated email does not match this invitation."));

    render(<InvitationAcceptancePage />);

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "The signed-in Microsoft account does not match this invitation."
    );
    await user.click(screen.getByRole("button", { name: "Switch account" }));

    expect(selectMicrosoftEntraAccountMock).toHaveBeenCalledOnce();
  });

  it("shows an existing-member conflict instead of a generic invitation failure", async () => {
    const user = userEvent.setup();
    acceptMock.mockResolvedValueOnce({
      data: null,
      error:
        "This email already belongs to a user in the tenant. Ask an administrator to revoke this invitation and manage the existing user's membership or role."
    });
    render(<InvitationAcceptancePage />);

    await user.type(await screen.findByLabelText("Display name"), "Existing Member");
    await user.click(screen.getByRole("button", { name: "Accept invitation" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("already belongs to a user in the tenant");
    expect(screen.getByRole("alert")).toHaveClass("invitation-activation-state--error");
  });
});

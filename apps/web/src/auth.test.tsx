import { cleanup, render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

const authMocks = vi.hoisted(() => ({
  activeAuthenticationPlane: "customer" as "customer" | "workforce",
  acquireTokenSilent: vi.fn(),
  clearStoredAccessToken: vi.fn(),
  getActiveAccount: vi.fn(),
  getAllAccounts: vi.fn(),
  handleRedirectPromise: vi.fn(),
  initialize: vi.fn(),
  loginRedirect: vi.fn(),
  selectMicrosoftEntraAccount: vi.fn(),
  shouldRestartSignInAfterLogout: vi.fn(),
  signOutOfFeDril: vi.fn(),
  switchMicrosoftEntraAccount: vi.fn(),
  setActiveAccount: vi.fn(),
  storeAccessToken: vi.fn()
}));

vi.mock("./authSession", () => ({
  get activeAuthenticationPlane() {
    return authMocks.activeAuthenticationPlane;
  },
  apiTokenRequest: { scopes: ["api://fedril/access"] },
  clearStoredAccessToken: authMocks.clearStoredAccessToken,
  isMsalConfigured: true,
  msalInstance: {
    acquireTokenSilent: authMocks.acquireTokenSilent,
    getActiveAccount: authMocks.getActiveAccount,
    getAllAccounts: authMocks.getAllAccounts,
    handleRedirectPromise: authMocks.handleRedirectPromise,
    initialize: authMocks.initialize,
    loginRedirect: authMocks.loginRedirect,
    setActiveAccount: authMocks.setActiveAccount
  },
  selectCachedAccount: (
    redirectAccount: { username: string } | null | undefined,
    activeAccount: { username: string } | null,
    cachedAccounts: Array<{ username: string }>
  ) => redirectAccount ?? activeAccount ?? (cachedAccounts.length === 1 ? cachedAccounts[0] : null),
  selectMicrosoftEntraAccount: authMocks.selectMicrosoftEntraAccount,
  shouldRestartSignInAfterLogout: authMocks.shouldRestartSignInAfterLogout,
  signOutOfFeDril: authMocks.signOutOfFeDril,
  switchMicrosoftEntraAccount: authMocks.switchMicrosoftEntraAccount,
  storeAccessToken: authMocks.storeAccessToken
}));

import { AuthGate } from "./auth";

describe("AuthGate", () => {
  beforeEach(() => {
    authMocks.activeAuthenticationPlane = "customer";
    authMocks.acquireTokenSilent.mockReset().mockResolvedValue({ accessToken: "access-token" });
    authMocks.clearStoredAccessToken.mockReset();
    authMocks.getActiveAccount.mockReset().mockReturnValue(null);
    authMocks.getAllAccounts.mockReset().mockReturnValue([]);
    authMocks.handleRedirectPromise.mockReset().mockResolvedValue(null);
    authMocks.initialize.mockReset().mockResolvedValue(undefined);
    authMocks.loginRedirect.mockReset().mockResolvedValue(undefined);
    authMocks.selectMicrosoftEntraAccount.mockReset().mockResolvedValue(undefined);
    authMocks.shouldRestartSignInAfterLogout.mockReset().mockReturnValue(false);
    authMocks.signOutOfFeDril.mockReset().mockResolvedValue(undefined);
    authMocks.switchMicrosoftEntraAccount.mockReset().mockResolvedValue(undefined);
    authMocks.setActiveAccount.mockReset();
    authMocks.storeAccessToken.mockReset();
    window.history.replaceState({}, "", "/invitations/accept?token=invitation-token");
  });

  afterEach(() => {
    cleanup();
  });

  it("offers customer account creation for an invitation", async () => {
    const user = userEvent.setup();
    render(
      <AuthGate>
        <div>Protected workspace</div>
      </AuthGate>
    );

    expect(await screen.findByRole("heading", { name: "Activate your FeDril account" })).toBeVisible();
    expect(screen.getByText("FeDril workspace")).toBeVisible();
    expect(screen.getByText("Create the customer account for the email address that received this invitation. Microsoft will verify the address with a one-time passcode.")).toBeVisible();
    expect(screen.queryByText(/staging/i)).not.toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Create invited account" }));

    expect(authMocks.selectMicrosoftEntraAccount).toHaveBeenCalledOnce();
    expect(screen.getByRole("heading", { name: "Opening customer account setup" })).toBeVisible();
  });

  it("offers email sign-in for an existing customer opening the workspace", async () => {
    const user = userEvent.setup();
    window.history.replaceState({}, "", "/app#/dashboard");

    render(
      <AuthGate>
        <div>Protected workspace</div>
      </AuthGate>
    );

    expect(await screen.findByRole("heading", { name: "Sign in to FeDril" })).toBeVisible();
    expect(screen.getByText("Sign in with your FeDril customer email address. Microsoft will send a one-time passcode.")).toBeVisible();
    await user.click(screen.getByRole("button", { name: "Sign in with email code" }));

    expect(authMocks.selectMicrosoftEntraAccount).toHaveBeenCalledOnce();
    expect(screen.getByRole("heading", { name: "Opening customer sign-in" })).toBeVisible();
  });

  it("surfaces a workforce sign-in failure and lets the operator retry", async () => {
    const user = userEvent.setup();
    authMocks.activeAuthenticationPlane = "workforce";
    authMocks.selectMicrosoftEntraAccount
      .mockRejectedValueOnce(new Error("Microsoft authority discovery failed."))
      .mockResolvedValueOnce(undefined);

    render(
      <AuthGate>
        <div>Protected workspace</div>
      </AuthGate>
    );

    await user.click(await screen.findByRole("button", { name: "Choose workforce account" }));

    expect(await screen.findByRole("heading", { name: "Sign-in failed" })).toBeVisible();
    expect(screen.getByText("Microsoft authority discovery failed.")).toBeVisible();
    await user.click(screen.getByRole("button", { name: "Choose another account" }));
    expect(authMocks.selectMicrosoftEntraAccount).toHaveBeenCalledTimes(2);
    expect(screen.getByRole("heading", { name: "Opening Microsoft sign-in" })).toBeVisible();
  });

  it("lets an authenticated user switch away from a cached account", async () => {
    const user = userEvent.setup();
    authMocks.getAllAccounts.mockReturnValue([{ username: "wrong-account@example.com" }]);

    render(
      <AuthGate>
        <div>Protected workspace</div>
      </AuthGate>
    );

    expect(await screen.findByText("Signed in as wrong-account@example.com")).toBeVisible();
    await user.click(screen.getByRole("button", { name: "Use another email" }));

    expect(authMocks.switchMicrosoftEntraAccount).toHaveBeenCalledOnce();
    expect(screen.getByRole("heading", { name: "Changing account" })).toBeVisible();
  });

  it("shows a retryable error when account switching fails", async () => {
    const user = userEvent.setup();
    authMocks.getAllAccounts.mockReturnValue([{ username: "wrong-account@example.com" }]);
    authMocks.switchMicrosoftEntraAccount.mockRejectedValueOnce(new Error("External ID sign-out failed."));

    render(
      <AuthGate>
        <div>Protected workspace</div>
      </AuthGate>
    );

    await user.click(await screen.findByRole("button", { name: "Use another email" }));

    expect(await screen.findByRole("heading", { name: "Sign-in failed" })).toBeVisible();
    expect(screen.getByText("External ID sign-out failed.")).toBeVisible();
    expect(screen.getByRole("button", { name: "Use another email" })).toBeVisible();
  });

  it("requires an explicit choice when more than one cached account exists", async () => {
    authMocks.getAllAccounts.mockReturnValue([
      { username: "first@example.com" },
      { username: "second@example.com" }
    ]);

    render(
      <AuthGate>
        <div>Protected workspace</div>
      </AuthGate>
    );

    expect(await screen.findByRole("heading", { name: "Activate your FeDril account" })).toBeVisible();
    expect(authMocks.acquireTokenSilent).not.toHaveBeenCalled();
  });

  it("restarts customer sign-in after a completed account-switch logout", async () => {
    authMocks.shouldRestartSignInAfterLogout.mockReturnValue(true);

    render(
      <AuthGate>
        <div>Protected workspace</div>
      </AuthGate>
    );

    await vi.waitFor(() => {
      expect(authMocks.selectMicrosoftEntraAccount).toHaveBeenCalledOnce();
    });
    expect(screen.queryByText("Protected workspace")).not.toBeInTheDocument();
  });

  it("clears the cached account before returning to the sign-in form", async () => {
    const user = userEvent.setup();
    authMocks.getAllAccounts.mockReturnValue([{ username: "customer@example.com" }]);
    let finishSignOut: (() => void) | undefined;
    authMocks.signOutOfFeDril.mockImplementationOnce(() => new Promise<void>(resolve => {
      finishSignOut = resolve;
    }));

    const { container } = render(
      <AuthGate>
        <div>Protected workspace</div>
      </AuthGate>
    );

    await user.click(await within(container).findByRole("button", { name: "Sign out" }));

    expect(authMocks.signOutOfFeDril).toHaveBeenCalledOnce();
    expect(within(container).getByRole("heading", { name: "Signing out" })).toBeVisible();
    expect(within(container).queryByRole("button", { name: "Create invited account" })).not.toBeInTheDocument();

    finishSignOut!();
    expect(await within(container).findByRole("heading", { name: "Activate your FeDril account" })).toBeVisible();
  });
});

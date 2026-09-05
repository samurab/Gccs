import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";

const authMocks = vi.hoisted(() => ({
  acquireTokenSilent: vi.fn(),
  clearStoredAccessToken: vi.fn(),
  getActiveAccount: vi.fn(),
  getAllAccounts: vi.fn(),
  handleRedirectPromise: vi.fn(),
  initialize: vi.fn(),
  loginRedirect: vi.fn(),
  selectMicrosoftEntraAccount: vi.fn(),
  signOutOfFeDril: vi.fn(),
  setActiveAccount: vi.fn(),
  storeAccessToken: vi.fn()
}));

vi.mock("./authSession", () => ({
  activeAuthenticationPlane: "customer",
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
  signOutOfFeDril: authMocks.signOutOfFeDril,
  storeAccessToken: authMocks.storeAccessToken
}));

import { AuthGate } from "./auth";

describe("AuthGate", () => {
  beforeEach(() => {
    authMocks.acquireTokenSilent.mockReset().mockResolvedValue({ accessToken: "access-token" });
    authMocks.clearStoredAccessToken.mockReset();
    authMocks.getActiveAccount.mockReset().mockReturnValue(null);
    authMocks.getAllAccounts.mockReset().mockReturnValue([]);
    authMocks.handleRedirectPromise.mockReset().mockResolvedValue(null);
    authMocks.initialize.mockReset().mockResolvedValue(undefined);
    authMocks.loginRedirect.mockReset().mockResolvedValue(undefined);
    authMocks.selectMicrosoftEntraAccount.mockReset().mockResolvedValue(undefined);
    authMocks.signOutOfFeDril.mockReset().mockResolvedValue(undefined);
    authMocks.setActiveAccount.mockReset();
    authMocks.storeAccessToken.mockReset();
  });

  it("offers the customer email one-time-passcode form", async () => {
    const user = userEvent.setup();
    render(
      <AuthGate>
        <div>Protected workspace</div>
      </AuthGate>
    );

    expect(await screen.findByRole("heading", { name: "Sign in to FeDril" })).toBeVisible();
    expect(screen.getByText("FeDril workspace")).toBeVisible();
    expect(screen.getByText("Enter the email address that received your FeDril invitation. We’ll send a one-time passcode.")).toBeVisible();
    expect(screen.queryByText(/staging/i)).not.toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Sign in with email code" }));

    expect(authMocks.selectMicrosoftEntraAccount).toHaveBeenCalledOnce();
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

    expect(authMocks.selectMicrosoftEntraAccount).toHaveBeenCalledOnce();
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

    expect(await screen.findByRole("heading", { name: "Sign in to FeDril" })).toBeVisible();
    expect(authMocks.acquireTokenSilent).not.toHaveBeenCalled();
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
    expect(within(container).queryByRole("button", { name: "Sign in with email code" })).not.toBeInTheDocument();

    finishSignOut!();
    expect(await within(container).findByRole("heading", { name: "Sign in to FeDril" })).toBeVisible();
  });
});

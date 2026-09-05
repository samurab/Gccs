import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";

const authMocks = vi.hoisted(() => ({
  acquireTokenSilent: vi.fn(),
  clearStoredAccessToken: vi.fn(),
  getAllAccounts: vi.fn(),
  handleRedirectPromise: vi.fn(),
  initialize: vi.fn(),
  loginRedirect: vi.fn(),
  selectMicrosoftEntraAccount: vi.fn(),
  setActiveAccount: vi.fn(),
  storeAccessToken: vi.fn()
}));

vi.mock("./authSession", () => ({
  apiTokenRequest: { scopes: ["api://fedril/access"] },
  clearStoredAccessToken: authMocks.clearStoredAccessToken,
  isMsalConfigured: true,
  msalInstance: {
    acquireTokenSilent: authMocks.acquireTokenSilent,
    getAllAccounts: authMocks.getAllAccounts,
    handleRedirectPromise: authMocks.handleRedirectPromise,
    initialize: authMocks.initialize,
    loginRedirect: authMocks.loginRedirect,
    setActiveAccount: authMocks.setActiveAccount
  },
  selectMicrosoftEntraAccount: authMocks.selectMicrosoftEntraAccount,
  storeAccessToken: authMocks.storeAccessToken
}));

import { AuthGate } from "./auth";

describe("AuthGate", () => {
  beforeEach(() => {
    authMocks.acquireTokenSilent.mockReset().mockResolvedValue({ accessToken: "access-token" });
    authMocks.clearStoredAccessToken.mockReset();
    authMocks.getAllAccounts.mockReset().mockReturnValue([]);
    authMocks.handleRedirectPromise.mockReset().mockResolvedValue(null);
    authMocks.initialize.mockReset().mockResolvedValue(undefined);
    authMocks.loginRedirect.mockReset().mockResolvedValue(undefined);
    authMocks.selectMicrosoftEntraAccount.mockReset().mockResolvedValue(undefined);
    authMocks.setActiveAccount.mockReset();
    authMocks.storeAccessToken.mockReset();
  });

  it("offers explicit Microsoft Entra account selection", async () => {
    const user = userEvent.setup();
    render(
      <AuthGate>
        <div>Protected workspace</div>
      </AuthGate>
    );

    expect(await screen.findByRole("heading", { name: "Sign in to FeDril" })).toBeVisible();
    expect(screen.getByText("FeDril workspace")).toBeVisible();
    expect(screen.getByText("Choose the Microsoft Entra account assigned to your FeDril access.")).toBeVisible();
    expect(screen.queryByText(/staging/i)).not.toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Choose account" }));

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
    await user.click(screen.getByRole("button", { name: "Switch account" }));

    expect(authMocks.selectMicrosoftEntraAccount).toHaveBeenCalledOnce();
  });
});

import { render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

const authMocks = vi.hoisted(() => ({
  clearStoredAccessToken: vi.fn(),
  getAllAccounts: vi.fn(),
  handleRedirectPromise: vi.fn(),
  initialize: vi.fn(),
  loginRedirect: vi.fn(),
  setActiveAccount: vi.fn(),
  storeAccessToken: vi.fn()
}));

vi.mock("./authSession", () => ({
  apiTokenRequest: { scopes: ["api://fedril/access"] },
  clearStoredAccessToken: authMocks.clearStoredAccessToken,
  isMsalConfigured: true,
  msalInstance: {
    getAllAccounts: authMocks.getAllAccounts,
    handleRedirectPromise: authMocks.handleRedirectPromise,
    initialize: authMocks.initialize,
    loginRedirect: authMocks.loginRedirect,
    setActiveAccount: authMocks.setActiveAccount
  },
  storeAccessToken: authMocks.storeAccessToken
}));

import { AuthGate } from "./auth";

describe("AuthGate", () => {
  beforeEach(() => {
    authMocks.clearStoredAccessToken.mockReset();
    authMocks.getAllAccounts.mockReset().mockReturnValue([]);
    authMocks.handleRedirectPromise.mockReset().mockResolvedValue(null);
    authMocks.initialize.mockReset().mockResolvedValue(undefined);
    authMocks.loginRedirect.mockReset().mockResolvedValue(undefined);
    authMocks.setActiveAccount.mockReset();
    authMocks.storeAccessToken.mockReset();
  });

  it("renders environment-neutral Microsoft Entra sign-in guidance", async () => {
    render(
      <AuthGate>
        <div>Protected workspace</div>
      </AuthGate>
    );

    expect(await screen.findByRole("heading", { name: "Sign in to FeDril" })).toBeVisible();
    expect(screen.getByText("FeDril workspace")).toBeVisible();
    expect(screen.getByText("Use your Microsoft Entra account to access your FeDril workspace.")).toBeVisible();
    expect(screen.queryByText(/staging/i)).not.toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Sign in" })).toBeEnabled();
  });
});

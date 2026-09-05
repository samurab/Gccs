import { beforeEach, describe, expect, it, vi } from "vitest";

const msalMocks = vi.hoisted(() => ({
  loginRedirect: vi.fn(),
  setActiveAccount: vi.fn()
}));

vi.mock("@azure/msal-browser", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@azure/msal-browser")>();
  return {
    ...actual,
    PublicClientApplication: function PublicClientApplication() {
      return {
        loginRedirect: msalMocks.loginRedirect,
        setActiveAccount: msalMocks.setActiveAccount
      };
    }
  };
});

describe("selectMicrosoftEntraAccount", () => {
  beforeEach(() => {
    vi.resetModules();
    vi.stubEnv("VITE_MSAL_CLIENT_ID", "web-client-id");
    vi.stubEnv("VITE_MSAL_TENANT_ID", "tenant-id");
    vi.stubEnv("VITE_MSAL_API_SCOPE", "api://fedril/access");
    msalMocks.loginRedirect.mockReset().mockResolvedValue(undefined);
    msalMocks.setActiveAccount.mockReset();
    const createStorage = (): Storage => {
      const values = new Map<string, string>();
      return {
        get length() { return values.size; },
        clear: () => values.clear(),
        getItem: (key) => values.get(key) ?? null,
        key: (index) => [...values.keys()][index] ?? null,
        removeItem: (key) => { values.delete(key); },
        setItem: (key, value) => { values.set(key, value); }
      };
    };
    Object.defineProperty(window, "localStorage", { configurable: true, value: createStorage() });
    Object.defineProperty(window, "sessionStorage", { configurable: true, value: createStorage() });
    window.history.replaceState({}, "", "/invitations/accept?token=invitation-token");
  });

  it("opens the account chooser and returns to the invitation URL", async () => {
    const { selectMicrosoftEntraAccount } = await import("./authSession");

    await selectMicrosoftEntraAccount();

    expect(msalMocks.setActiveAccount).toHaveBeenCalledWith(null);
    expect(msalMocks.loginRedirect).toHaveBeenCalledWith({
      scopes: ["api://fedril/access"],
      prompt: "select_account",
      redirectStartPage: window.location.href
    });
  });
});

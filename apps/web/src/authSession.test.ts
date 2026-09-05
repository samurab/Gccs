import { beforeEach, describe, expect, it, vi } from "vitest";

const msalMocks = vi.hoisted(() => ({
  clearCache: vi.fn(),
  configuration: null as unknown,
  getActiveAccount: vi.fn(),
  getAllAccounts: vi.fn(),
  loginRedirect: vi.fn(),
  setActiveAccount: vi.fn()
}));

vi.mock("@azure/msal-browser", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@azure/msal-browser")>();
  return {
    ...actual,
    PublicClientApplication: function PublicClientApplication(configuration: unknown) {
      msalMocks.configuration = configuration;
      return {
        clearCache: msalMocks.clearCache,
        getActiveAccount: msalMocks.getActiveAccount,
        getAllAccounts: msalMocks.getAllAccounts,
        loginRedirect: msalMocks.loginRedirect,
        setActiveAccount: msalMocks.setActiveAccount
      };
    }
  };
});

describe("route-specific authentication", () => {
  beforeEach(() => {
    vi.resetModules();
    vi.stubEnv("VITE_MSAL_CLIENT_ID", "workforce-client-id");
    vi.stubEnv("VITE_MSAL_TENANT_ID", "workforce-tenant-id");
    vi.stubEnv("VITE_MSAL_API_SCOPE", "api://fedril/workforce");
    vi.stubEnv("VITE_CUSTOMER_MSAL_CLIENT_ID", "customer-client-id");
    vi.stubEnv("VITE_CUSTOMER_MSAL_TENANT_ID", "customer-tenant-id");
    vi.stubEnv("VITE_CUSTOMER_MSAL_TENANT_SUBDOMAIN", "fedrilcustomersstaging");
    vi.stubEnv("VITE_CUSTOMER_MSAL_API_SCOPE", "api://fedril/customer");
    msalMocks.configuration = null;
    msalMocks.clearCache.mockReset().mockResolvedValue(undefined);
    msalMocks.getActiveAccount.mockReset().mockReturnValue(null);
    msalMocks.getAllAccounts.mockReset().mockReturnValue([]);
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

  it("uses the customer External ID authority and forces the email form on invitation routes", async () => {
    const { selectMicrosoftEntraAccount } = await import("./authSession");

    await selectMicrosoftEntraAccount();

    expect(msalMocks.configuration).toMatchObject({
      auth: {
        clientId: "customer-client-id",
        authority: "https://fedrilcustomersstaging.ciamlogin.com/customer-tenant-id",
        knownAuthorities: ["fedrilcustomersstaging.ciamlogin.com"],
        redirectUri: `${window.location.origin}/invitations/accept`
      }
    });
    expect(msalMocks.loginRedirect).toHaveBeenCalledWith({
      scopes: ["api://fedril/customer"],
      prompt: "login",
      redirectStartPage: window.location.href
    });
  });

  it("preserves the workforce account chooser on Platform Operator routes", async () => {
    window.history.replaceState({}, "", "/platform/tenants/new");
    const { selectMicrosoftEntraAccount } = await import("./authSession");

    await selectMicrosoftEntraAccount();

    expect(msalMocks.configuration).toMatchObject({
      auth: {
        clientId: "workforce-client-id",
        authority: "https://login.microsoftonline.com/workforce-tenant-id"
      }
    });
    expect(msalMocks.loginRedirect).toHaveBeenCalledWith({
      scopes: ["api://fedril/workforce"],
      prompt: "select_account",
      redirectStartPage: window.location.href
    });
  });

  it("falls back to the existing workforce configuration when customer identity is not configured", async () => {
    vi.stubEnv("VITE_CUSTOMER_MSAL_CLIENT_ID", "");
    vi.stubEnv("VITE_CUSTOMER_MSAL_TENANT_ID", "");
    vi.stubEnv("VITE_CUSTOMER_MSAL_TENANT_SUBDOMAIN", "");
    vi.stubEnv("VITE_CUSTOMER_MSAL_API_SCOPE", "");
    window.history.replaceState({}, "", "/app");
    const { activeAuthenticationPlane, selectMicrosoftEntraAccount } = await import("./authSession");

    await selectMicrosoftEntraAccount();

    expect(activeAuthenticationPlane).toBe("workforce");
    expect(msalMocks.configuration).toMatchObject({
      auth: {
        clientId: "workforce-client-id",
        authority: "https://login.microsoftonline.com/workforce-tenant-id"
      }
    });
  });

  it("does not choose an arbitrary account when multiple cached accounts are ambiguous", async () => {
    const { selectCachedAccount } = await import("./authSession");
    const first = { username: "first@example.com", tenantId: "customer-tenant-id" };
    const second = { username: "second@example.com", tenantId: "customer-tenant-id" };

    expect(selectCachedAccount(null, null, [first, second] as never)).toBeNull();
    expect(selectCachedAccount(null, second as never, [first, second] as never)).toBe(second);
    expect(selectCachedAccount(first as never, second as never, [first, second] as never)).toBe(first);
  });

  it("ignores a cached account from the other authentication plane", async () => {
    const { selectCachedAccount } = await import("./authSession");
    const workforce = { username: "operator@example.com", tenantId: "workforce-tenant-id" };
    const customer = { username: "customer@example.com", tenantId: "customer-tenant-id" };

    expect(selectCachedAccount(null, workforce as never, [workforce, customer] as never)).toBe(customer);
    expect(selectCachedAccount(workforce as never, null, [workforce] as never)).toBeNull();
  });

  it("removes every cached customer account without clearing the workforce plane", async () => {
    const firstCustomer = { username: "first@example.com", tenantId: "customer-tenant-id" };
    const secondCustomer = { username: "second@example.com", tenantId: "customer-tenant-id" };
    const workforce = { username: "operator@example.com", tenantId: "workforce-tenant-id" };
    msalMocks.getAllAccounts.mockReturnValue([firstCustomer, workforce, secondCustomer]);
    window.sessionStorage.setItem("gccs.accessToken", "stale-token");
    const { signOutOfFeDril } = await import("./authSession");

    await signOutOfFeDril();

    expect(msalMocks.clearCache).toHaveBeenCalledTimes(2);
    expect(msalMocks.clearCache).toHaveBeenCalledWith({ account: firstCustomer });
    expect(msalMocks.clearCache).toHaveBeenCalledWith({ account: secondCustomer });
    expect(msalMocks.clearCache).not.toHaveBeenCalledWith({ account: workforce });
    expect(msalMocks.setActiveAccount).toHaveBeenLastCalledWith(null);
    expect(window.sessionStorage.getItem("gccs.accessToken")).toBeNull();
  });
});

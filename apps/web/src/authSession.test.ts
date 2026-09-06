import { beforeEach, describe, expect, it, vi } from "vitest";

const msalMocks = vi.hoisted(() => ({
  acquireTokenSilent: vi.fn(),
  clearCache: vi.fn(),
  configuration: null as unknown,
  getActiveAccount: vi.fn(),
  getAllAccounts: vi.fn(),
  initialize: vi.fn(),
  loginRedirect: vi.fn(),
  logoutRedirect: vi.fn(),
  setActiveAccount: vi.fn()
}));

vi.mock("@azure/msal-browser", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@azure/msal-browser")>();
  return {
    ...actual,
    PublicClientApplication: function PublicClientApplication(configuration: unknown) {
      msalMocks.configuration = configuration;
      return {
        acquireTokenSilent: msalMocks.acquireTokenSilent,
        clearCache: msalMocks.clearCache,
        getActiveAccount: msalMocks.getActiveAccount,
        getAllAccounts: msalMocks.getAllAccounts,
        initialize: msalMocks.initialize,
        loginRedirect: msalMocks.loginRedirect,
        logoutRedirect: msalMocks.logoutRedirect,
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
    msalMocks.acquireTokenSilent.mockReset().mockResolvedValue({ accessToken: "fresh-token" });
    msalMocks.clearCache.mockReset().mockResolvedValue(undefined);
    msalMocks.getActiveAccount.mockReset().mockReturnValue(null);
    msalMocks.getAllAccounts.mockReset().mockReturnValue([]);
    msalMocks.initialize.mockReset().mockResolvedValue(undefined);
    msalMocks.loginRedirect.mockReset().mockResolvedValue(undefined);
    msalMocks.logoutRedirect.mockReset().mockResolvedValue(undefined);
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
    expect((msalMocks.configuration as { auth: object }).auth).not.toHaveProperty("knownAuthorities");
    expect(msalMocks.loginRedirect).toHaveBeenCalledWith({
      scopes: ["api://fedril/workforce"],
      prompt: "select_account",
      redirectStartPage: window.location.href
    });
    expect(msalMocks.logoutRedirect).not.toHaveBeenCalled();
  });

  it("clears a stale cross-client interaction and retries customer sign-in once", async () => {
    const interactionError = { errorCode: "interaction_in_progress" };
    window.sessionStorage.setItem("msal.interaction.status", JSON.stringify({
      clientId: "workforce-client-id",
      type: "signin"
    }));
    window.sessionStorage.setItem("msal.workforce-client-id.account", "preserved-workforce-cache");
    msalMocks.loginRedirect
      .mockRejectedValueOnce(interactionError)
      .mockResolvedValueOnce(undefined);
    const { selectMicrosoftEntraAccount } = await import("./authSession");

    await selectMicrosoftEntraAccount();

    expect(window.sessionStorage.getItem("msal.interaction.status")).toBeNull();
    expect(window.sessionStorage.getItem("msal.workforce-client-id.account")).toBe("preserved-workforce-cache");
    expect(msalMocks.clearCache).not.toHaveBeenCalled();
    expect(msalMocks.setActiveAccount).toHaveBeenCalledWith(null);
    expect(msalMocks.loginRedirect).toHaveBeenCalledTimes(2);
    expect(msalMocks.loginRedirect).toHaveBeenLastCalledWith({
      scopes: ["api://fedril/customer"],
      prompt: "login",
      redirectStartPage: window.location.href
    });
  });

  it("recovers a stale customer interaction without changing the workforce account chooser", async () => {
    window.history.replaceState({}, "", "/platform/tenants/new");
    window.sessionStorage.setItem("msal.interaction.status", JSON.stringify({
      clientId: "customer-client-id",
      type: "signin"
    }));
    msalMocks.loginRedirect
      .mockRejectedValueOnce({ errorCode: "interaction_in_progress" })
      .mockResolvedValueOnce(undefined);
    const { selectMicrosoftEntraAccount } = await import("./authSession");

    await selectMicrosoftEntraAccount();

    expect(window.sessionStorage.getItem("msal.interaction.status")).toBeNull();
    expect(msalMocks.loginRedirect).toHaveBeenCalledTimes(2);
    expect(msalMocks.loginRedirect).toHaveBeenLastCalledWith({
      scopes: ["api://fedril/workforce"],
      prompt: "select_account",
      redirectStartPage: window.location.href
    });
  });

  it("does not clear authentication state or retry unrelated sign-in failures", async () => {
    const providerError = new Error("identity provider unavailable");
    window.sessionStorage.setItem("msal.interaction.status", "unrelated-interaction");
    msalMocks.loginRedirect.mockRejectedValueOnce(providerError);
    const { selectMicrosoftEntraAccount } = await import("./authSession");

    await expect(selectMicrosoftEntraAccount()).rejects.toBe(providerError);

    expect(msalMocks.clearCache).not.toHaveBeenCalled();
    expect(window.sessionStorage.getItem("msal.interaction.status")).toBe("unrelated-interaction");
    expect(msalMocks.loginRedirect).toHaveBeenCalledOnce();
  });

  it("retries an interaction collision only once", async () => {
    const interactionError = { errorCode: "interaction_in_progress" };
    window.sessionStorage.setItem("msal.interaction.status", "stale-interaction");
    msalMocks.loginRedirect.mockRejectedValue(interactionError);
    const { selectMicrosoftEntraAccount } = await import("./authSession");

    await expect(selectMicrosoftEntraAccount()).rejects.toBe(interactionError);

    expect(msalMocks.clearCache).not.toHaveBeenCalled();
    expect(window.sessionStorage.getItem("msal.interaction.status")).toBeNull();
    expect(msalMocks.loginRedirect).toHaveBeenCalledTimes(2);
  });

  it("suppresses duplicate sign-in requests while a redirect is starting", async () => {
    let completeRedirect: (() => void) | undefined;
    msalMocks.loginRedirect.mockImplementationOnce(() => new Promise<void>(resolve => {
      completeRedirect = resolve;
    }));
    const { selectMicrosoftEntraAccount } = await import("./authSession");

    const firstRequest = selectMicrosoftEntraAccount();
    const duplicateRequest = selectMicrosoftEntraAccount();
    await duplicateRequest;

    expect(msalMocks.loginRedirect).toHaveBeenCalledOnce();
    completeRedirect!();
    await firstRequest;
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

  it("ends the active customer server session without clearing the workforce plane", async () => {
    const firstCustomer = { username: "first@example.com", tenantId: "customer-tenant-id" };
    const secondCustomer = { username: "second@example.com", tenantId: "customer-tenant-id" };
    const workforce = { username: "operator@example.com", tenantId: "workforce-tenant-id" };
    msalMocks.getAllAccounts.mockReturnValue([firstCustomer, workforce, secondCustomer]);
    msalMocks.getActiveAccount.mockReturnValue(firstCustomer);
    window.sessionStorage.setItem("gccs.accessToken", "stale-token");
    const { signOutOfFeDril } = await import("./authSession");

    await signOutOfFeDril();

    expect(msalMocks.clearCache).toHaveBeenCalledTimes(1);
    expect(msalMocks.clearCache).toHaveBeenCalledWith({ account: secondCustomer });
    expect(msalMocks.clearCache).not.toHaveBeenCalledWith({ account: workforce });
    expect(msalMocks.logoutRedirect).toHaveBeenCalledWith({
      account: firstCustomer,
      authority: "https://fedrilcustomersstaging.ciamlogin.com/customer-tenant-id",
      postLogoutRedirectUri: `${window.location.origin}/app`,
      state: expect.any(String)
    });
    expect(window.sessionStorage.getItem("gccs.accessToken")).toBeNull();
  });

  it("ends the active workforce session without clearing customer accounts", async () => {
    window.history.replaceState({}, "", "/platform/tenants/new");
    const workforce = { username: "operator@example.com", tenantId: "workforce-tenant-id" };
    const customer = { username: "customer@example.com", tenantId: "customer-tenant-id" };
    msalMocks.getAllAccounts.mockReturnValue([customer, workforce]);
    msalMocks.getActiveAccount.mockReturnValue(workforce);
    const { signOutOfFeDril } = await import("./authSession");

    await signOutOfFeDril();

    expect(msalMocks.clearCache).not.toHaveBeenCalledWith({ account: customer });
    expect(msalMocks.logoutRedirect).toHaveBeenCalledWith({
      account: workforce,
      authority: "https://login.microsoftonline.com/workforce-tenant-id",
      postLogoutRedirectUri: `${window.location.origin}/app`,
      state: expect.any(String)
    });
  });

  it("ends customer SSO before restarting sign-in with another email", async () => {
    const customer = { username: "wrong@example.com", tenantId: "customer-tenant-id" };
    msalMocks.getAllAccounts.mockReturnValue([customer]);
    msalMocks.getActiveAccount.mockReturnValue(customer);
    const { switchMicrosoftEntraAccount } = await import("./authSession");

    await switchMicrosoftEntraAccount();

    expect(msalMocks.logoutRedirect).toHaveBeenCalledWith({
      account: customer,
      authority: "https://fedrilcustomersstaging.ciamlogin.com/customer-tenant-id",
      postLogoutRedirectUri: `${window.location.origin}/app`,
      state: expect.any(String)
    });
    expect(msalMocks.loginRedirect).not.toHaveBeenCalled();
  });

  it("discards an in-flight customer token when server logout begins", async () => {
    const customer = { username: "wrong@example.com", tenantId: "customer-tenant-id" };
    msalMocks.getAllAccounts.mockReturnValue([customer]);
    msalMocks.getActiveAccount.mockReturnValue(customer);
    let finishTokenAcquisition: ((result: { accessToken: string }) => void) | undefined;
    msalMocks.acquireTokenSilent.mockImplementationOnce(() => new Promise(resolve => {
      finishTokenAcquisition = resolve;
    }));
    const { getFreshAccessToken, switchMicrosoftEntraAccount } = await import("./authSession");

    const tokenRequest = getFreshAccessToken();
    await vi.waitFor(() => expect(msalMocks.acquireTokenSilent).toHaveBeenCalledOnce());
    await switchMicrosoftEntraAccount();
    finishTokenAcquisition!({ accessToken: "stale-token" });

    expect(await tokenRequest).toBeNull();
    expect(window.sessionStorage.getItem("gccs.accessToken")).toBeNull();
  });

  it("restores only a nonce-bound customer invitation path after logout", async () => {
    window.sessionStorage.setItem("gccs.auth.postLogoutState", JSON.stringify({
      nonce: "logout-nonce",
      plane: "customer",
      restartSignIn: true,
      returnPath: "/invitations/accept?token=invitation-token",
      createdAt: Date.now()
    }));
    window.history.replaceState({}, "", "/app?state=eyJpZCI6ImxpYnJhcnktc3RhdGUifQ%3D%3D%7Clogout-nonce");

    const { activeAuthenticationPlane, shouldRestartSignInAfterLogout } = await import("./authSession");

    expect(activeAuthenticationPlane).toBe("customer");
    expect(shouldRestartSignInAfterLogout()).toBe(true);
    expect(shouldRestartSignInAfterLogout()).toBe(false);
    expect(window.location.pathname).toBe("/invitations/accept");
    expect(window.location.search).toBe("?token=invitation-token");
    expect(window.sessionStorage.getItem("gccs.auth.postLogoutState")).toBeNull();
  });

  it("rejects a cross-plane post-logout return path", async () => {
    window.sessionStorage.setItem("gccs.auth.postLogoutState", JSON.stringify({
      nonce: "logout-nonce",
      plane: "customer",
      restartSignIn: true,
      returnPath: "/platform/tenants/new",
      createdAt: Date.now()
    }));
    window.history.replaceState({}, "", "/app?state=eyJpZCI6ImxpYnJhcnktc3RhdGUifQ%3D%3D%7Clogout-nonce");

    const { activeAuthenticationPlane, shouldRestartSignInAfterLogout } = await import("./authSession");

    expect(activeAuthenticationPlane).toBe("customer");
    expect(shouldRestartSignInAfterLogout()).toBe(false);
    expect(window.location.pathname).toBe("/app");
    expect(window.sessionStorage.getItem("gccs.auth.postLogoutState")).toBeNull();
  });

  it("restores a workforce route without restarting customer sign-in", async () => {
    window.sessionStorage.setItem("gccs.auth.postLogoutState", JSON.stringify({
      nonce: "logout-nonce",
      plane: "workforce",
      restartSignIn: false,
      returnPath: "/platform/tenants/new",
      createdAt: Date.now()
    }));
    window.history.replaceState({}, "", "/app?state=library-state%7Clogout-nonce");

    const { activeAuthenticationPlane, shouldRestartSignInAfterLogout } = await import("./authSession");

    expect(activeAuthenticationPlane).toBe("workforce");
    expect(shouldRestartSignInAfterLogout()).toBe(false);
    expect(window.location.pathname).toBe("/platform/tenants/new");
  });
});

import {
  BrowserAuthErrorCodes,
  BrowserCacheLocation,
  InteractionRequiredAuthError,
  PublicClientApplication,
  type AccountInfo
} from "@azure/msal-browser";
import { getWorkspaceUrl } from "./routing";

export type AuthenticationPlane = "workforce" | "customer";

const accessTokenStorageKey = import.meta.env.VITE_GCCS_ACCESS_TOKEN_STORAGE_KEY ?? "gccs.accessToken";
const legacyAccessTokenStorageKey = "access_token";
const postLogoutStateStorageKey = "gccs.auth.postLogoutState";
const postLogoutStateLifetimeMs = 10 * 60 * 1000;
const interactionInProgressStorageKey = "msal.interaction.status";
let isEndingAuthenticationSession = false;
let isStartingAuthenticationSession = false;
let authenticationSessionGeneration = 0;

type PostLogoutState = {
  nonce: string;
  plane: AuthenticationPlane;
  restartSignIn: boolean;
  returnPath: string;
  createdAt: number;
};

let completedPostLogoutState = consumePostLogoutState();

const workforceClientId = import.meta.env.VITE_MSAL_CLIENT_ID;
const workforceTenantId = import.meta.env.VITE_MSAL_TENANT_ID;
const workforceApiScope = import.meta.env.VITE_MSAL_API_SCOPE;
const customerClientId = import.meta.env.VITE_CUSTOMER_MSAL_CLIENT_ID;
const customerTenantId = import.meta.env.VITE_CUSTOMER_MSAL_TENANT_ID;
const customerTenantSubdomain = import.meta.env.VITE_CUSTOMER_MSAL_TENANT_SUBDOMAIN;
const customerApiScope = import.meta.env.VITE_CUSTOMER_MSAL_API_SCOPE;
const customerAuthenticationConfigured = Boolean(
  customerClientId &&
  customerTenantId &&
  customerTenantSubdomain &&
  customerApiScope
);
const requestedAuthenticationPlane = getAuthenticationPlane();
const authenticationPlane = requestedAuthenticationPlane === "customer" && customerAuthenticationConfigured
  ? "customer"
  : "workforce";

const selectedConfiguration = authenticationPlane === "workforce"
  ? {
      clientId: workforceClientId,
      tenantId: workforceTenantId,
      authority: workforceTenantId ? `https://login.microsoftonline.com/${workforceTenantId}` : "",
      apiScope: workforceApiScope,
      redirectUri: getWorkspaceUrl(),
      knownAuthorities: undefined
    }
  : {
      clientId: customerClientId,
      tenantId: customerTenantId,
      authority:
        customerTenantSubdomain && customerTenantId
          ? `https://${customerTenantSubdomain}.ciamlogin.com/${customerTenantId}`
          : "",
      apiScope: customerApiScope,
      redirectUri: getCustomerRedirectUri(),
      knownAuthorities: customerTenantSubdomain ? [`${customerTenantSubdomain}.ciamlogin.com`] : undefined
    };

export const activeAuthenticationPlane = authenticationPlane;
export function shouldRestartSignInAfterLogout() {
  const shouldRestart = Boolean(
    completedPostLogoutState?.restartSignIn &&
    completedPostLogoutState.plane === authenticationPlane
  );
  completedPostLogoutState = null;
  return shouldRestart;
}

export function isAuthenticationSessionChanging() {
  return isEndingAuthenticationSession;
}
export const apiTokenRequest = { scopes: selectedConfiguration.apiScope ? [selectedConfiguration.apiScope] : [] };
export const isMsalConfigured = Boolean(
  selectedConfiguration.clientId &&
  selectedConfiguration.tenantId &&
  selectedConfiguration.authority &&
  selectedConfiguration.apiScope
);

export const msalInstance = isMsalConfigured
  ? new PublicClientApplication({
      auth: {
        clientId: selectedConfiguration.clientId!,
        authority: selectedConfiguration.authority,
        redirectUri: selectedConfiguration.redirectUri,
        postLogoutRedirectUri: getWorkspaceUrl(),
        ...(selectedConfiguration.knownAuthorities
          ? { knownAuthorities: selectedConfiguration.knownAuthorities }
          : {})
      },
      cache: {
        cacheLocation: BrowserCacheLocation.SessionStorage
      }
    })
  : null;

export function getAuthenticationPlane(location: Pick<Location, "pathname"> = window.location): AuthenticationPlane {
  return location.pathname === "/platform" || location.pathname.startsWith("/platform/")
    ? "workforce"
    : "customer";
}

export function selectCachedAccount(
  redirectAccount: AccountInfo | null | undefined,
  activeAccount: AccountInfo | null,
  cachedAccounts: AccountInfo[]
): AccountInfo | null {
  if (redirectAccount && accountBelongsToActivePlane(redirectAccount)) return redirectAccount;
  if (activeAccount && accountBelongsToActivePlane(activeAccount)) return activeAccount;

  const planeAccounts = cachedAccounts.filter(accountBelongsToActivePlane);
  return planeAccounts.length === 1 ? planeAccounts[0] : null;
}

export function accountBelongsToActivePlane(account: AccountInfo): boolean {
  const selectedTenantId = selectedConfiguration.tenantId?.trim();
  return Boolean(
    selectedTenantId &&
    account.tenantId &&
    account.tenantId.toLowerCase() === selectedTenantId.toLowerCase()
  );
}

export async function getFreshAccessToken(): Promise<string | null> {
  const requestedGeneration = authenticationSessionGeneration;
  if (isEndingAuthenticationSession) {
    clearStoredAccessToken();
    return null;
  }

  if (!msalInstance) {
    return getStoredAccessToken();
  }

  await msalInstance.initialize();
  if (requestedGeneration !== authenticationSessionGeneration) {
    clearStoredAccessToken();
    return null;
  }

  const account = selectCachedAccount(
    null,
    msalInstance.getActiveAccount(),
    msalInstance.getAllAccounts()
  );
  if (!account) {
    clearStoredAccessToken();
    return null;
  }

  msalInstance.setActiveAccount(account);

  try {
    const tokenResult = await msalInstance.acquireTokenSilent({ ...apiTokenRequest, account });
    if (requestedGeneration !== authenticationSessionGeneration || isEndingAuthenticationSession) {
      clearStoredAccessToken();
      return null;
    }
    storeAccessToken(tokenResult.accessToken);
    return tokenResult.accessToken;
  } catch (error) {
    if (requestedGeneration !== authenticationSessionGeneration || isEndingAuthenticationSession) {
      clearStoredAccessToken();
      return null;
    }

    if (error instanceof InteractionRequiredAuthError) {
      clearStoredAccessToken();
      await msalInstance.acquireTokenRedirect({ ...apiTokenRequest, account });
      return null;
    }

    throw error;
  }
}

export async function selectMicrosoftEntraAccount(): Promise<void> {
  if (!msalInstance || isEndingAuthenticationSession || isStartingAuthenticationSession) {
    return;
  }

  isStartingAuthenticationSession = true;
  try {
    clearStoredAccessToken();
    msalInstance.setActiveAccount(null);

    try {
      await startMicrosoftEntraAccountSelection();
    } catch (error) {
      if (!isInteractionInProgressError(error)) {
        throw error;
      }

      // MSAL 5 uses one temporary interaction marker per browser tab, even when
      // separate customer and workforce client IDs share this application origin.
      // Its redirect API has no public interaction override. Remove only that
      // stale marker so recovering one plane does not erase the other plane's
      // cached accounts and tokens, then let MSAL create a fresh transaction.
      window.sessionStorage.removeItem(interactionInProgressStorageKey);
      msalInstance.setActiveAccount(null);
      await startMicrosoftEntraAccountSelection();
    }
  } finally {
    isStartingAuthenticationSession = false;
  }
}

export async function switchMicrosoftEntraAccount(): Promise<void> {
  if (!msalInstance) {
    return;
  }

  if (authenticationPlane === "customer" && getCurrentPlaneAccount()) {
    await endMicrosoftEntraSession(true);
    return;
  }

  await selectMicrosoftEntraAccount();
}

export async function signOutOfFeDril(): Promise<void> {
  clearStoredAccessToken();
  if (!msalInstance) {
    return;
  }

  await endMicrosoftEntraSession(false);
}

export function storeAccessToken(accessToken: string) {
  window.localStorage.setItem(accessTokenStorageKey, accessToken);
  window.sessionStorage.setItem(accessTokenStorageKey, accessToken);
  window.localStorage.setItem(legacyAccessTokenStorageKey, accessToken);
  window.sessionStorage.setItem(legacyAccessTokenStorageKey, accessToken);
}

export function clearStoredAccessToken() {
  window.localStorage.removeItem(accessTokenStorageKey);
  window.sessionStorage.removeItem(accessTokenStorageKey);
  window.localStorage.removeItem(legacyAccessTokenStorageKey);
  window.sessionStorage.removeItem(legacyAccessTokenStorageKey);
}

function getCustomerRedirectUri(): string {
  if (window.location.pathname === "/invitations/accept") {
    return `${window.location.origin}/invitations/accept`;
  }

  return getWorkspaceUrl();
}

function getStoredAccessToken(): string | null {
  if (typeof window === "undefined") {
    return null;
  }

  const configuredKey = import.meta.env.VITE_GCCS_ACCESS_TOKEN_STORAGE_KEY;
  const storageKeys = [
    configuredKey,
    accessTokenStorageKey,
    legacyAccessTokenStorageKey
  ].filter((key): key is string => Boolean(key));

  for (const key of storageKeys) {
    const value = window.sessionStorage.getItem(key) ?? window.localStorage.getItem(key);
    if (value && value.trim()) {
      return value.trim();
    }
  }

  return null;
}

async function endMicrosoftEntraSession(restartSignIn: boolean): Promise<void> {
  if (!msalInstance || isEndingAuthenticationSession) {
    return;
  }

  isEndingAuthenticationSession = true;
  authenticationSessionGeneration += 1;
  try {
    clearStoredAccessToken();
    const accounts = msalInstance.getAllAccounts().filter(accountBelongsToActivePlane);
    const account = getCurrentPlaneAccount(accounts);
    if (!account) {
      await Promise.all(accounts.map(cachedAccount => msalInstance.clearCache({ account: cachedAccount })));
      msalInstance.setActiveAccount(null);
      isEndingAuthenticationSession = false;
      if (restartSignIn) {
        await selectMicrosoftEntraAccount();
      }
      return;
    }

    const otherAccounts = accounts.filter(cachedAccount => !isSameAccount(cachedAccount, account));
    await Promise.all(otherAccounts.map(cachedAccount => msalInstance.clearCache({ account: cachedAccount })));
    msalInstance.setActiveAccount(account);

    const postLogoutState: PostLogoutState = {
      nonce: crypto.randomUUID(),
      plane: authenticationPlane,
      restartSignIn,
      returnPath: `${window.location.pathname}${window.location.search}${window.location.hash}`,
      createdAt: Date.now()
    };
    window.sessionStorage.setItem(postLogoutStateStorageKey, JSON.stringify(postLogoutState));

    await msalInstance.logoutRedirect({
      account,
      authority: selectedConfiguration.authority,
      postLogoutRedirectUri: getWorkspaceUrl(),
      state: postLogoutState.nonce
    });
  } catch (error) {
    window.sessionStorage.removeItem(postLogoutStateStorageKey);
    throw error;
  } finally {
    isEndingAuthenticationSession = false;
  }
}

function getCurrentPlaneAccount(accounts = msalInstance?.getAllAccounts().filter(accountBelongsToActivePlane) ?? []) {
  if (!msalInstance) {
    return null;
  }

  return selectCachedAccount(null, msalInstance.getActiveAccount(), accounts);
}

function startMicrosoftEntraAccountSelection() {
  return msalInstance!.loginRedirect({
    ...apiTokenRequest,
    prompt: authenticationPlane === "workforce" ? "select_account" : "login",
    redirectStartPage: window.location.href
  });
}

function isInteractionInProgressError(error: unknown): error is { errorCode: string } {
  return Boolean(
    error &&
    typeof error === "object" &&
    "errorCode" in error &&
    error.errorCode === BrowserAuthErrorCodes.interactionInProgress
  );
}

function isSameAccount(left: AccountInfo, right: AccountInfo) {
  return left.homeAccountId && right.homeAccountId
    ? left.homeAccountId === right.homeAccountId
    : left === right;
}

function consumePostLogoutState(): PostLogoutState | null {
  const rawState = window.sessionStorage.getItem(postLogoutStateStorageKey);
  if (!rawState) {
    return null;
  }

  let pendingState: PostLogoutState;
  try {
    pendingState = JSON.parse(rawState) as PostLogoutState;
  } catch {
    window.sessionStorage.removeItem(postLogoutStateStorageKey);
    return null;
  }

  const returnedState = new URLSearchParams(window.location.search).get("state");
  const returnedNonce = returnedState?.slice(returnedState.lastIndexOf("|") + 1) ?? null;
  if (!returnedNonce || returnedNonce !== pendingState.nonce) {
    if (Date.now() - pendingState.createdAt > postLogoutStateLifetimeMs) {
      window.sessionStorage.removeItem(postLogoutStateStorageKey);
    }
    return null;
  }

  window.sessionStorage.removeItem(postLogoutStateStorageKey);
  if (
    !Number.isFinite(pendingState.createdAt) ||
    Date.now() - pendingState.createdAt < 0 ||
    Date.now() - pendingState.createdAt > postLogoutStateLifetimeMs ||
    !isAllowedPostLogoutReturnPath(pendingState.plane, pendingState.returnPath)
  ) {
    return null;
  }

  window.history.replaceState(null, "", pendingState.returnPath);
  return pendingState;
}

function isAllowedPostLogoutReturnPath(plane: AuthenticationPlane, returnPath: string) {
  if (!returnPath.startsWith("/")) {
    return false;
  }

  const returnUrl = new URL(returnPath, window.location.origin);
  if (returnUrl.origin !== window.location.origin) {
    return false;
  }

  return plane === "customer"
    ? returnUrl.pathname === "/app" || returnUrl.pathname === "/invitations/accept"
    : returnUrl.pathname === "/platform" || returnUrl.pathname.startsWith("/platform/");
}

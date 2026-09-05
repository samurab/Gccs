import {
  BrowserCacheLocation,
  InteractionRequiredAuthError,
  PublicClientApplication,
  type AccountInfo
} from "@azure/msal-browser";
import { getWorkspaceUrl } from "./routing";

export type AuthenticationPlane = "workforce" | "customer";

const accessTokenStorageKey = import.meta.env.VITE_GCCS_ACCESS_TOKEN_STORAGE_KEY ?? "gccs.accessToken";
const legacyAccessTokenStorageKey = "access_token";

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
      authority: customerTenantSubdomain ? `https://${customerTenantSubdomain}.ciamlogin.com/` : "",
      apiScope: customerApiScope,
      redirectUri: getCustomerRedirectUri(),
      knownAuthorities: customerTenantSubdomain ? [`${customerTenantSubdomain}.ciamlogin.com`] : undefined
    };

export const activeAuthenticationPlane = authenticationPlane;
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
        postLogoutRedirectUri: window.location.origin,
        knownAuthorities: selectedConfiguration.knownAuthorities
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
  if (!msalInstance) {
    return getStoredAccessToken();
  }

  await msalInstance.initialize();

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
    storeAccessToken(tokenResult.accessToken);
    return tokenResult.accessToken;
  } catch (error) {
    if (error instanceof InteractionRequiredAuthError) {
      clearStoredAccessToken();
      await msalInstance.acquireTokenRedirect({ ...apiTokenRequest, account });
      return null;
    }

    throw error;
  }
}

export async function selectMicrosoftEntraAccount(): Promise<void> {
  if (!msalInstance) {
    return;
  }

  clearStoredAccessToken();
  msalInstance.setActiveAccount(null);
  await msalInstance.loginRedirect({
    ...apiTokenRequest,
    prompt: authenticationPlane === "workforce" ? "select_account" : "login",
    redirectStartPage: window.location.href
  });
}

export async function signOutOfFeDril(): Promise<void> {
  clearStoredAccessToken();
  if (!msalInstance) {
    return;
  }

  const accounts = msalInstance.getAllAccounts().filter(accountBelongsToActivePlane);
  await Promise.all(accounts.map(account => msalInstance.clearCache({ account })));
  msalInstance.setActiveAccount(null);
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

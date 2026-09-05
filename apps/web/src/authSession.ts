import {
  BrowserCacheLocation,
  InteractionRequiredAuthError,
  PublicClientApplication
} from "@azure/msal-browser";
import { getWorkspaceUrl } from "./routing";

const accessTokenStorageKey = import.meta.env.VITE_GCCS_ACCESS_TOKEN_STORAGE_KEY ?? "gccs.accessToken";
const legacyAccessTokenStorageKey = "access_token";
const clientId = import.meta.env.VITE_MSAL_CLIENT_ID;
const tenantId = import.meta.env.VITE_MSAL_TENANT_ID;
const apiScope = import.meta.env.VITE_MSAL_API_SCOPE;
const authority = tenantId ? `https://login.microsoftonline.com/${tenantId}` : "";

export const apiTokenRequest = { scopes: apiScope ? [apiScope] : [] };
export const isMsalConfigured = Boolean(clientId && tenantId && apiScope);

export const msalInstance = isMsalConfigured
  ? new PublicClientApplication({
      auth: {
        clientId,
        authority,
        redirectUri: getWorkspaceUrl(),
        postLogoutRedirectUri: window.location.origin
      },
      cache: {
        cacheLocation: BrowserCacheLocation.SessionStorage
      }
    })
  : null;

export async function getFreshAccessToken(): Promise<string | null> {
  if (!msalInstance) {
    return getStoredAccessToken();
  }

  await msalInstance.initialize();

  const account = msalInstance.getActiveAccount() ?? msalInstance.getAllAccounts()[0] ?? null;
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
    prompt: "select_account",
    redirectStartPage: window.location.href
  });
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

  for (const storage of [window.localStorage, window.sessionStorage]) {
    for (const key of Object.keys(storage)) {
      if (key.startsWith("msal.")) {
        storage.removeItem(key);
      }
    }
  }
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

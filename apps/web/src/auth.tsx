import {
  BrowserCacheLocation,
  InteractionRequiredAuthError,
  PublicClientApplication,
  type AccountInfo
} from "@azure/msal-browser";
import { type ReactNode, useEffect, useMemo, useState } from "react";

const accessTokenStorageKey = import.meta.env.VITE_GCCS_ACCESS_TOKEN_STORAGE_KEY ?? "gccs.accessToken";
const legacyAccessTokenStorageKey = "access_token";
const clientId = import.meta.env.VITE_MSAL_CLIENT_ID;
const tenantId = import.meta.env.VITE_MSAL_TENANT_ID;
const apiScope = import.meta.env.VITE_MSAL_API_SCOPE;
const authority = tenantId ? `https://login.microsoftonline.com/${tenantId}` : "";

const isMsalConfigured = Boolean(clientId && tenantId && apiScope);

const msalInstance = isMsalConfigured
  ? new PublicClientApplication({
      auth: {
        clientId,
        authority,
        redirectUri: window.location.origin,
        postLogoutRedirectUri: window.location.origin
      },
      cache: {
        cacheLocation: BrowserCacheLocation.SessionStorage
      }
    })
  : null;

type AuthState =
  | { status: "disabled" }
  | { status: "initializing" }
  | { status: "signedOut" }
  | { status: "ready"; account: AccountInfo }
  | { status: "failed"; message: string };

export function AuthGate({ children }: { children: ReactNode }) {
  const [state, setState] = useState<AuthState>({ status: isMsalConfigured ? "initializing" : "disabled" });
  const tokenRequest = useMemo(() => ({ scopes: apiScope ? [apiScope] : [] }), []);

  useEffect(() => {
    if (!msalInstance) {
      return;
    }

    let isMounted = true;

    async function initializeAuth() {
      try {
        await msalInstance!.initialize();
        const redirectResult = await msalInstance!.handleRedirectPromise();
        const account = redirectResult?.account ?? msalInstance!.getAllAccounts()[0] ?? null;

        if (!account) {
          clearStoredAccessToken();
          if (isMounted) {
            setState({ status: "signedOut" });
          }
          return;
        }

        msalInstance!.setActiveAccount(account);
        const tokenResult = await msalInstance!.acquireTokenSilent({ ...tokenRequest, account });
        storeAccessToken(tokenResult.accessToken);

        if (isMounted) {
          setState({ status: "ready", account });
        }
      } catch (error) {
        if (error instanceof InteractionRequiredAuthError) {
          clearStoredAccessToken();
          if (isMounted) {
            setState({ status: "signedOut" });
          }
          return;
        }

        clearStoredAccessToken();
        if (isMounted) {
          setState({
            status: "failed",
            message: error instanceof Error ? error.message : "Sign-in could not be completed."
          });
        }
      }
    }

    void initializeAuth();

    return () => {
      isMounted = false;
    };
  }, [tokenRequest]);

  if (state.status === "disabled") {
    return <>{children}</>;
  }

  if (state.status === "initializing") {
    return <AuthShell title="Connecting to GCCS" body="Preparing the secure staging workspace." />;
  }

  if (state.status === "signedOut") {
    return (
      <AuthShell
        title="Sign in to GCCS"
        body="Use your Microsoft Entra account to access the staging workspace."
        actionLabel="Sign in"
        onAction={() => {
          void msalInstance!.loginRedirect(tokenRequest);
        }}
      />
    );
  }

  if (state.status === "failed") {
    return (
      <AuthShell
        title="Sign-in failed"
        body={state.message}
        actionLabel="Try again"
        onAction={() => {
          void msalInstance!.loginRedirect(tokenRequest);
        }}
      />
    );
  }

  return (
    <>
      <div className="auth-session" role="status">
        <span>Signed in as {state.account.username}</span>
        <button
          type="button"
          onClick={() => {
            clearStoredAccessToken();
            msalInstance!.setActiveAccount(null);
            setState({ status: "signedOut" });
          }}
        >
          Sign out
        </button>
      </div>
      {children}
    </>
  );
}

function storeAccessToken(accessToken: string) {
  window.localStorage.setItem(accessTokenStorageKey, accessToken);
  window.sessionStorage.setItem(accessTokenStorageKey, accessToken);
  window.localStorage.setItem(legacyAccessTokenStorageKey, accessToken);
  window.sessionStorage.setItem(legacyAccessTokenStorageKey, accessToken);
}

function clearStoredAccessToken() {
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

function AuthShell({
  title,
  body,
  actionLabel,
  onAction
}: {
  title: string;
  body: string;
  actionLabel?: string;
  onAction?: () => void;
}) {
  return (
    <main className="auth-shell">
      <section className="auth-panel" aria-label={title}>
        <p className="auth-kicker">GCCS staging</p>
        <h1>{title}</h1>
        <p>{body}</p>
        {actionLabel && onAction ? (
          <button className="auth-action" type="button" onClick={onAction}>
            {actionLabel}
          </button>
        ) : null}
      </section>
    </main>
  );
}

import {
  InteractionRequiredAuthError,
  type AccountInfo
} from "@azure/msal-browser";
import { type ReactNode, useEffect, useMemo, useState } from "react";
import {
  apiTokenRequest,
  clearStoredAccessToken,
  isMsalConfigured,
  msalInstance,
  storeAccessToken
} from "./authSession";

type AuthState =
  | { status: "disabled" }
  | { status: "initializing" }
  | { status: "signedOut" }
  | { status: "ready"; account: AccountInfo }
  | { status: "failed"; message: string };

export function AuthGate({ children }: { children: ReactNode }) {
  const [state, setState] = useState<AuthState>({ status: isMsalConfigured ? "initializing" : "disabled" });
  const tokenRequest = useMemo(() => apiTokenRequest, []);

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
    return <AuthShell title="Connecting to FeDril" body="Preparing your FeDril workspace." />;
  }

  if (state.status === "signedOut") {
    return (
      <AuthShell
        title="Sign in to FeDril"
        body="Use your Microsoft Entra account to access your FeDril workspace."
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
        <p className="auth-kicker">FeDril workspace</p>
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

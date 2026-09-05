import {
  InteractionRequiredAuthError,
  type AccountInfo
} from "@azure/msal-browser";
import { type ReactNode, useEffect, useMemo, useState } from "react";
import {
  activeAuthenticationPlane,
  apiTokenRequest,
  clearStoredAccessToken,
  isMsalConfigured,
  msalInstance,
  selectCachedAccount,
  selectMicrosoftEntraAccount,
  shouldRestartSignInAfterLogout,
  signOutOfFeDril,
  switchMicrosoftEntraAccount,
  storeAccessToken
} from "./authSession";

type AuthState =
  | { status: "disabled" }
  | { status: "initializing" }
  | { status: "signingIn" }
  | { status: "switchingAccount" }
  | { status: "signingOut" }
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
        const account = selectCachedAccount(
          redirectResult?.account,
          msalInstance!.getActiveAccount(),
          msalInstance!.getAllAccounts()
        );

        if (!account) {
          clearStoredAccessToken();
          if (shouldRestartSignInAfterLogout()) {
            await selectMicrosoftEntraAccount();
            return;
          }
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

  function handleAccountSwitch() {
    setState({ status: "switchingAccount" });
    void switchMicrosoftEntraAccount().catch(error => setState({
      status: "failed",
      message: error instanceof Error ? error.message : "Account switching could not be completed."
    }));
  }

  function handleSignIn() {
    setState({ status: "signingIn" });
    void selectMicrosoftEntraAccount().catch(error => setState({
      status: "failed",
      message: error instanceof Error ? error.message : "Sign-in could not be completed."
    }));
  }

  if (state.status === "disabled") {
    return <>{children}</>;
  }

  if (state.status === "initializing") {
    return <AuthShell title="Connecting to FeDril" body="Preparing your FeDril workspace." />;
  }

  if (state.status === "signingIn") {
    return <AuthShell title="Opening Microsoft sign-in" body="Connecting to the Microsoft account chooser." />;
  }

  if (state.status === "signingOut") {
    return <AuthShell title="Signing out" body="Clearing the current FeDril sign-in session." />;
  }

  if (state.status === "switchingAccount") {
    return <AuthShell title="Changing account" body="Ending the current sign-in session before choosing another account." />;
  }

  if (state.status === "signedOut") {
    const isCustomer = activeAuthenticationPlane === "customer";
    return (
      <AuthShell
        title="Sign in to FeDril"
        body={isCustomer
          ? "Enter the email address that received your FeDril invitation. We’ll send a one-time passcode."
          : "Choose the Microsoft Entra workforce account assigned to Platform Operations."}
        actionLabel={isCustomer ? "Sign in with email code" : "Choose workforce account"}
        onAction={handleSignIn}
      />
    );
  }

  if (state.status === "failed") {
    return (
      <AuthShell
        title="Sign-in failed"
        body={state.message}
        actionLabel={activeAuthenticationPlane === "customer" ? "Use another email" : "Choose another account"}
        onAction={activeAuthenticationPlane === "customer" ? handleAccountSwitch : handleSignIn}
      />
    );
  }

  return (
    <>
      <div className="auth-session" role="status">
        <span>Signed in as {state.account.username}</span>
        <button type="button" onClick={handleAccountSwitch}>
          {activeAuthenticationPlane === "customer" ? "Use another email" : "Switch account"}
        </button>
        <button
          type="button"
          onClick={() => {
            setState({ status: "signingOut" });
            void signOutOfFeDril()
              .then(() => setState({ status: "signedOut" }))
              .catch(error => setState({
                status: "failed",
                message: error instanceof Error ? error.message : "Sign-out could not be completed."
              }));
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

import { useCallback, useEffect, useState, type FormEvent } from "react";
import { CheckCircle2, LoaderCircle, LockKeyhole, ShieldAlert, UserCheck } from "lucide-react";
import {
  acceptTenantInvitation,
  getInvitationAcceptanceContext,
  selectDevelopmentInvitationIdentity,
  selectDevelopmentTestingContext,
  selectTenant,
  type InvitationAcceptanceContext
} from "./lib/api";
import { formatUsDateTime } from "./lib/dateFormat";

type PageState = "loading" | "identity" | "ready" | "submitting" | "success" | "error";

export function InvitationAcceptancePage() {
  const token = new URLSearchParams(window.location.search).get("token")?.trim() ?? "";
  const [state, setState] = useState<PageState>(token ? "loading" : "error");
  const [context, setContext] = useState<InvitationAcceptanceContext | null>(null);
  const [displayName, setDisplayName] = useState("");
  const [invitedEmail, setInvitedEmail] = useState("");
  const [message, setMessage] = useState(token ? "" : "The activation link is missing its invitation token.");

  const verifyInvitation = useCallback(async (isActive: () => boolean) => {
    if (!token) {
      return;
    }

    try {
      const result = await getInvitationAcceptanceContext(token);
      if (!isActive()) return;
      setContext(result);
      if (result.status !== "Pending") {
        setMessage(`This invitation is ${result.status.toLowerCase()} and cannot be accepted.`);
        setState("error");
        return;
      }
      if (new Date(result.expiresAt) <= new Date()) {
        setMessage("This invitation has expired. Ask a FeDril platform operator to resend it.");
        setState("error");
        return;
      }
      setState("ready");
    } catch (error) {
      if (!isActive()) return;
      const errorMessage = error instanceof Error ? error.message : "The invitation could not be verified.";
      if (import.meta.env.DEV && /authenticated email does not match this invitation/i.test(errorMessage)) {
        setMessage("Enter the exact email address that received this invitation. This control is available only in local development.");
        setState("identity");
        return;
      }
      setMessage(errorMessage);
      setState("error");
    }
  }, [token]);

  useEffect(() => {
    let active = true;
    const verificationTimer = window.setTimeout(() => {
      void verifyInvitation(() => active);
    }, 0);

    return () => {
      active = false;
      window.clearTimeout(verificationTimer);
    };
  }, [verifyInvitation]);

  async function handleDevelopmentIdentity(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    selectDevelopmentInvitationIdentity(invitedEmail);
    setState("loading");
    setMessage("");
    await verifyInvitation(() => true);
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setState("submitting");
    setMessage("");
    const result = await acceptTenantInvitation(token, displayName);
    if (!result.data) {
      setMessage(result.error ?? "The invitation could not be accepted.");
      setState("error");
      return;
    }

    if (import.meta.env.DEV && context) {
      selectDevelopmentTestingContext(result.data.tenantId, context.roleName, null, context.email);
    } else {
      selectTenant(result.data.tenantId);
    }
    setState("success");
  }

  return (
    <main className="invitation-activation-page">
      <header className="invitation-activation-header">
        <div>
          <p>FeDril account activation</p>
          <h1>{context?.tenantDisplayName ?? "Tenant invitation"}</h1>
        </div>
        <LockKeyhole aria-hidden="true" size={28} />
      </header>

      <section className="invitation-activation-boundary" aria-label="Data handling boundary">
        <ShieldAlert aria-hidden="true" size={19} />
        <span>No-CUI workspace. Do not upload CUI or prohibited sensitive data.</span>
      </section>

      <section className="invitation-activation-content">
        {state === "loading" ? (
          <ActivationState icon={LoaderCircle} title="Verifying invitation" body="Checking the signed-in account and invitation status." spin />
        ) : null}

        {state === "error" ? (
          <ActivationState icon={ShieldAlert} title="Invitation unavailable" body={message} tone="error" />
        ) : null}

        {state === "identity" ? (
          <form className="invitation-activation-form" onSubmit={handleDevelopmentIdentity}>
            <h2>Use invited test identity</h2>
            <p>{message}</p>
            <label>
              <span>Invited email</span>
              <input
                autoComplete="email"
                onChange={(event) => setInvitedEmail(event.target.value)}
                required
                type="email"
                value={invitedEmail}
              />
            </label>
            <button type="submit">
              <UserCheck aria-hidden="true" size={18} />
              Continue as invitee
            </button>
          </form>
        ) : null}

        {(state === "ready" || state === "submitting") && context ? (
          <form className="invitation-activation-form" onSubmit={handleSubmit}>
            <div className="invitation-activation-summary">
              <div><span>Account</span><strong>{context.email}</strong></div>
              <div><span>Role</span><strong>{context.roleName}</strong></div>
              <div><span>Expires</span><strong>{formatUsDateTime(context.expiresAt)}</strong></div>
            </div>
            <label>
              <span>Display name</span>
              <input
                autoComplete="name"
                maxLength={200}
                onChange={(event) => setDisplayName(event.target.value)}
                required
                value={displayName}
              />
            </label>
            <button disabled={state === "submitting"} type="submit">
              {state === "submitting" ? <LoaderCircle aria-hidden="true" className="spin" size={18} /> : <UserCheck aria-hidden="true" size={18} />}
              {state === "submitting" ? "Activating workspace" : "Accept invitation"}
            </button>
          </form>
        ) : null}

        {state === "success" ? (
          <div className="invitation-activation-success" role="status">
            <CheckCircle2 aria-hidden="true" size={34} />
            <h2>Workspace activated</h2>
            <p>Your {context?.roleName ?? "assigned"} membership is active and this tenant is selected for the current browser.</p>
            <a href="/app">Open workspace</a>
          </div>
        ) : null}
      </section>
    </main>
  );
}

function ActivationState({
  icon: Icon,
  title,
  body,
  spin = false,
  tone = "neutral"
}: {
  icon: typeof ShieldAlert;
  title: string;
  body: string;
  spin?: boolean;
  tone?: "neutral" | "error";
}) {
  return (
    <div
      className={`invitation-activation-state invitation-activation-state--${tone}`}
      role={tone === "error" ? "alert" : "status"}
    >
      <Icon aria-hidden="true" className={spin ? "spin" : undefined} size={28} />
      <div><h2>{title}</h2><p>{body}</p></div>
    </div>
  );
}

import { useEffect, useState, type FormEvent } from "react";
import { CheckCircle2, LoaderCircle, LockKeyhole, ShieldAlert, UserCheck } from "lucide-react";
import {
  acceptTenantInvitation,
  getInvitationAcceptanceContext,
  selectTenant,
  type InvitationAcceptanceContext
} from "./lib/api";

type PageState = "loading" | "ready" | "submitting" | "success" | "error";

export function InvitationAcceptancePage() {
  const token = new URLSearchParams(window.location.search).get("token")?.trim() ?? "";
  const [state, setState] = useState<PageState>(token ? "loading" : "error");
  const [context, setContext] = useState<InvitationAcceptanceContext | null>(null);
  const [displayName, setDisplayName] = useState("");
  const [message, setMessage] = useState(token ? "" : "The activation link is missing its invitation token.");

  useEffect(() => {
    let active = true;
    if (!token) {
      return;
    }

    getInvitationAcceptanceContext(token)
      .then((result) => {
        if (!active) return;
        setContext(result);
        if (result.status !== "Pending") {
          setMessage(`This invitation is ${result.status.toLowerCase()} and cannot be accepted.`);
          setState("error");
          return;
        }
        if (new Date(result.expiresAt) <= new Date()) {
          setMessage("This invitation has expired. Ask a GCCS platform operator to resend it.");
          setState("error");
          return;
        }
        setState("ready");
      })
      .catch((error) => {
        if (!active) return;
        setMessage(error instanceof Error ? error.message : "The invitation could not be verified.");
        setState("error");
      });

    return () => {
      active = false;
    };
  }, [token]);

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

    selectTenant(result.data.tenantId);
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
          <ActivationState icon={ShieldAlert} title="Invitation unavailable" body={message} />
        ) : null}

        {(state === "ready" || state === "submitting") && context ? (
          <form className="invitation-activation-form" onSubmit={handleSubmit}>
            <div className="invitation-activation-summary">
              <div><span>Account</span><strong>{context.email}</strong></div>
              <div><span>Role</span><strong>{context.roleName}</strong></div>
              <div><span>Expires</span><strong>{new Date(context.expiresAt).toLocaleString()}</strong></div>
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
            <p>Your Owner membership is active and this tenant is selected for the current browser.</p>
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
  spin = false
}: {
  icon: typeof ShieldAlert;
  title: string;
  body: string;
  spin?: boolean;
}) {
  return (
    <div className="invitation-activation-state" role={spin ? "status" : "alert"}>
      <Icon aria-hidden="true" className={spin ? "spin" : undefined} size={28} />
      <div><h2>{title}</h2><p>{body}</p></div>
    </div>
  );
}

import { useEffect, useState, type FormEvent } from "react";
import { CheckCircle2, LoaderCircle, ShieldAlert } from "lucide-react";
import {
  getDemoFollowUpContext,
  submitDemoFollowUpResponse,
  type DemoFollowUpContext,
} from "./demoRequestApi";

const workflowOptions = [
  ["ContractClauseIntake", "Contract and clause intake"],
  ["ObligationsDeadlines", "Obligation and deadline tracking"],
  ["CmmcReadiness", "CMMC readiness workflows"],
  ["EvidenceManagement", "Evidence organization"],
  ["SubcontractorFlowDowns", "Subcontractor flow-down tracking"],
  ["ReportingPreparation", "Reporting or review preparation"],
  ["Other", "Another workflow"],
] as const;

type PageState = "loading" | "ready" | "submitting" | "success" | "error";

function tokenFromFragment() {
  return new URLSearchParams(window.location.hash.replace(/^#/, "")).get("token")?.trim() ?? "";
}

export function DemoRequestDetailsPage() {
  const [token] = useState(tokenFromFragment);
  const [state, setState] = useState<PageState>(token ? "loading" : "error");
  const [context, setContext] = useState<DemoFollowUpContext | null>(null);
  const [message, setMessage] = useState(token ? "" : "The follow-up link is missing its access token.");
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({});
  const [workflows, setWorkflows] = useState<string[]>([]);
  const [otherWorkflow, setOtherWorkflow] = useState("");
  const [goals, setGoals] = useState("");
  const [challenges, setChallenges] = useState("");
  const [currentProcess, setCurrentProcess] = useState("");
  const [additionalContext, setAdditionalContext] = useState("");
  const [noCuiConfirmed, setNoCuiConfirmed] = useState(false);
  const [website, setWebsite] = useState("");

  useEffect(() => {
    if (!token) return;
    window.history.replaceState(null, "", `${window.location.pathname}${window.location.search}`);
    let active = true;
    void getDemoFollowUpContext(token).then(result => {
      if (!active) return;
      if (!result.data) {
        setMessage(result.error);
        setState("error");
        return;
      }

      setContext(result.data);
      if (result.data.status === "Responded") {
        setMessage("This follow-up request has already been answered.");
        setState("error");
      } else if (result.data.status === "Expired" || new Date(result.data.expiresAt) <= new Date()) {
        setMessage("This follow-up link has expired. Reply to the FeDril operations email to request a new link.");
        setState("error");
      } else {
        setState("ready");
      }
    });
    return () => { active = false; };
  }, [token]);

  function toggleWorkflow(value: string) {
    setWorkflows(current => current.includes(value)
      ? current.filter(item => item !== value)
      : [...current, value]);
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setState("submitting");
    setMessage("");
    setFieldErrors({});
    const result = await submitDemoFollowUpResponse({
      token,
      workflows,
      otherWorkflow: otherWorkflow.trim() || null,
      goals,
      challenges,
      currentProcess: currentProcess.trim() || null,
      additionalContext: additionalContext.trim() || null,
      noCuiConfirmed,
      website: website || null,
    });
    if (!result.data) {
      setMessage(result.error);
      setFieldErrors(result.fieldErrors);
      setState("error");
      return;
    }

    setState("success");
  }

  const canSubmit = workflows.length > 0 && goals.trim().length > 0 && challenges.trim().length > 0 &&
    noCuiConfirmed && (!workflows.includes("Other") || otherWorkflow.trim().length > 0);

  return <main className="demo-details-page">
    <header className="demo-details-header">
      <div><p>FeDril live demonstration</p><h1>Help us tailor your demonstration</h1></div>
      <ShieldAlert aria-hidden="true" size={28} />
    </header>

    <section className="demo-details-boundary" aria-label="Data handling boundary">
      <ShieldAlert aria-hidden="true" size={19} />
      <span>Provide only non-sensitive business-process information. Do not enter CUI, FCI, classified information, export-controlled or ITAR data, credentials, contract documents, or security configurations.</span>
    </section>

    {state === "loading" ? <section className="demo-details-state" role="status"><LoaderCircle className="spin" /><h2>Checking your follow-up link</h2></section> : null}
    {state === "error" && !context ? <section className="demo-details-state demo-details-state--error" role="alert"><ShieldAlert /><h2>Follow-up unavailable</h2><p>{message}</p></section> : null}
    {state === "error" && context?.status !== "Pending" ? <section className="demo-details-state demo-details-state--error" role="alert"><ShieldAlert /><h2>Follow-up unavailable</h2><p>{message}</p></section> : null}
    {state === "success" ? <section className="demo-details-state demo-details-state--success" role="status"><CheckCircle2 /><h2>Details received</h2><p>The FeDril team can now use this information to prepare your demonstration. This form did not schedule or change an appointment.</p></section> : null}

    {(state === "ready" || state === "submitting" || (state === "error" && context?.status === "Pending")) ? <form className="demo-details-form" onSubmit={submit}>
      <p className="demo-details-expiry">This single-use form expires {context ? new Date(context.expiresAt).toLocaleString() : "soon"}.</p>
      <fieldset>
        <legend>Which workflows would you like to see? *</legend>
        <div className="demo-details-workflows">{workflowOptions.map(([value, label]) => <label key={value}>
          <input checked={workflows.includes(value)} onChange={() => toggleWorkflow(value)} type="checkbox" />
          <span>{label}</span>
        </label>)}</div>
        {fieldErrors.workflows?.map(error => <p className="field-error" key={error}>{error}</p>)}
      </fieldset>

      {workflows.includes("Other") ? <label><span>Other workflow *</span><input maxLength={200} onChange={event => setOtherWorkflow(event.target.value)} required value={otherWorkflow} />{fieldErrors.otherWorkflow?.map(error => <small className="field-error" key={error}>{error}</small>)}</label> : null}
      <label><span>What should the demonstration help you accomplish? *</span><textarea maxLength={2000} onChange={event => setGoals(event.target.value)} required rows={4} value={goals} />{fieldErrors.goals?.map(error => <small className="field-error" key={error}>{error}</small>)}</label>
      <label><span>What process or readiness challenges are you experiencing? *</span><textarea maxLength={2000} onChange={event => setChallenges(event.target.value)} required rows={4} value={challenges} />{fieldErrors.challenges?.map(error => <small className="field-error" key={error}>{error}</small>)}</label>
      <label><span>How do you manage this work today? <small>Optional</small></span><textarea maxLength={1000} onChange={event => setCurrentProcess(event.target.value)} rows={3} value={currentProcess} /></label>
      <label><span>Additional non-sensitive context <small>Optional</small></span><textarea maxLength={2000} onChange={event => setAdditionalContext(event.target.value)} rows={3} value={additionalContext} /></label>

      <label className="demo-details-honeypot" aria-hidden="true"><span>Website</span><input autoComplete="off" onChange={event => setWebsite(event.target.value)} tabIndex={-1} value={website} /></label>
      <label className="demo-details-confirmation"><input checked={noCuiConfirmed} onChange={event => setNoCuiConfirmed(event.target.checked)} required type="checkbox" /><span>I confirm this response contains no CUI or other prohibited sensitive information. *</span></label>
      {fieldErrors.noCuiConfirmed?.map(error => <p className="field-error" key={error}>{error}</p>)}
      {state === "error" && message ? <p className="form-status form-status--error" role="alert">{message}</p> : null}
      <button disabled={state === "submitting" || !canSubmit} type="submit">{state === "submitting" ? <><LoaderCircle className="spin" size={18} /> Sending details…</> : "Send demo details"}</button>
    </form> : null}
  </main>;
}

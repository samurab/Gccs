import { Inbox, LoaderCircle, LockKeyhole, RefreshCw } from "lucide-react";
import { useCallback, useEffect, useState } from "react";
import { PlatformAdminNav } from "./PlatformAdminNav";
import { getPlatformAccess, getPlatformDemoRequests, queuePlatformDemoRequestResponse, type PlatformAccess, type PlatformDemoRequestPage } from "./lib/api";

const responseTemplates = [
  ["ReviewingRequestedTime", "We’re reviewing your requested time"],
  ["RequestMoreDetails", "Please provide more details"],
  ["RequestedTimeUnavailable", "The requested time is unavailable"],
] as const;

function ResponseControls({ requestId, requesterName }: { requestId: string; requesterName: string }) {
  const [templateKey, setTemplateKey] = useState(responseTemplates[0][0]);
  const [state, setState] = useState<"idle" | "sending" | "sent" | "error">("idle");
  const [message, setMessage] = useState("");
  const send = async () => {
    const label = responseTemplates.find(item => item[0] === templateKey)?.[1] ?? templateKey;
    if (!window.confirm(`Queue “${label}” for ${requesterName}?`)) return;
    setState("sending"); setMessage("");
    const result = await queuePlatformDemoRequestResponse(requestId, templateKey);
    if (result.error) { setState("error"); setMessage(result.error); return; }
    setState("sent"); setMessage(result.data?.status === "AlreadyQueued" ? "This response was already queued." : "Response queued for delivery.");
  };
  return <section className="platform-demo-response" aria-label={`Respond to ${requesterName}`}>
    <label><span>Response template</span><select onChange={event => { setTemplateKey(event.target.value as typeof templateKey); setState("idle"); setMessage(""); }} value={templateKey}>{responseTemplates.map(([key, label]) => <option key={key} value={key}>{label}</option>)}</select></label>
    <button disabled={state === "sending" || state === "sent"} onClick={() => void send()} type="button">{state === "sending" ? "Queueing…" : "Queue response"}</button>
    {message ? <p className={state === "error" ? "form-status form-status--error" : "form-status form-status--ok"} role="status">{message}</p> : null}
    <small>Emails use server-owned copy and the No-CUI warning. Queueing does not guarantee delivery until the email provider accepts it.</small>
  </section>;
}

export function PlatformDemoRequestsPage() {
  const [access, setAccess] = useState<PlatformAccess | null>(null);
  const [data, setData] = useState<PlatformDemoRequestPage | null>(null);
  const [error, setError] = useState("");
  const [page, setPage] = useState(1);
  const load = useCallback(async () => {
    try {
      const access = await getPlatformAccess();
      setAccess(access);
      if (access.canManageDemoRequests) setData(await getPlatformDemoRequests(page));
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "Demo requests could not be loaded.");
    }
  }, [page]);
  useEffect(() => {
    let active = true;
    getPlatformAccess().then(async access => {
      if (!active) return;
      setAccess(access);
      if (access.canManageDemoRequests) {
        const requests = await getPlatformDemoRequests(page);
        if (active) setData(requests);
      }
    }).catch(reason => {
      if (active) setError(reason instanceof Error ? reason.message : "Demo requests could not be loaded.");
    });
    return () => { active = false; };
  }, [page]);

  if (access === null && !error) return <main className="platform-demo-inbox"><LoaderCircle className="spin" /> Loading demo requests…</main>;
  if (access && !access.canManageDemoRequests) return <main className="platform-demo-inbox"><LockKeyhole /><h1>Demo-request access denied</h1><p>Your account lacks the ManageDemoRequests platform permission.</p></main>;
  return <main className="platform-demo-inbox">
    {access ? <PlatformAdminNav access={access} active="demo-requests" /> : null}
    <header><div><p className="landing-eyebrow">FeDril operations</p><h1>Demo requests</h1><p>Durable intake records and notification-delivery status. This view is platform-scoped, not tenant-scoped.</p></div><button onClick={() => { setError(""); void load(); }} type="button"><RefreshCw size={17} /> Refresh</button></header>
    {error ? <p className="form-status form-status--error" role="alert">{error}</p> : null}
    {data?.items.length === 0 ? <section className="platform-demo-empty"><Inbox size={34} /><h2>No demo requests</h2><p>New public submissions will appear here.</p></section> : null}
    {data?.items.map(item => <article className="platform-demo-card" key={item.id}>
      <div><span className={`platform-demo-status platform-demo-status--${item.deliveryStatus.toLowerCase()}`}>{item.deliveryStatus}</span><time dateTime={item.receivedAt}>{new Date(item.receivedAt).toLocaleString()}</time></div>
      <h2>{item.company}</h2><p><strong>{item.firstName} {item.lastName}</strong> · <a href={`mailto:${item.email}`}>{item.email}</a>{item.phone ? ` · ${item.phone}` : ""}</p>
      <dl><div><dt>Preferred time</dt><dd>{item.preferredStartAt ? new Date(item.preferredStartAt).toLocaleString(undefined, { timeZone: item.preferredTimeZone ?? undefined }) : "Not provided"}{item.preferredTimeZone ? ` (${item.preferredTimeZone})` : ""}</dd></div><div><dt>Internal notification</dt><dd>{item.deliveryStatus} · {item.deliveryAttemptCount} attempts</dd></div><div><dt>Requester acknowledgement</dt><dd>{item.acknowledgementStatus}</dd></div></dl>
      {item.message ? <blockquote>{item.message}</blockquote> : null}
      {item.deliveryFailureCode ? <p className="form-status form-status--error">Delivery failure: {item.deliveryFailureCode}</p> : null}
      <ResponseControls requestId={item.id} requesterName={`${item.firstName} ${item.lastName}`} />
    </article>)}
    {data ? <nav className="platform-demo-pagination" aria-label="Demo request pages"><button disabled={!data.hasPreviousPage} onClick={() => setPage(value => value - 1)} type="button">Previous</button><span>Page {data.page} · {data.totalCount} requests</span><button disabled={!data.hasNextPage} onClick={() => setPage(value => value + 1)} type="button">Next</button></nav> : null}
  </main>;
}

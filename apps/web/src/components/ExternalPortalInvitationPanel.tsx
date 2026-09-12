import { type FormEvent, useEffect, useState } from "react";
import {
  createExternalPortalInvitation,
  extendExternalPortalInvitation,
  getExternalPortalAccessHistory,
  getExternalPortalInvitations,
  resendExternalPortalInvitation,
  revokeExternalPortalInvitation,
  type ExternalPortalAccessHistory,
  type ExternalPortalInvitation,
  type ExternalPortalRole
} from "@/lib/api";

const roles: Array<{ value: ExternalPortalRole; label: string }> = [
  { value: "PrimeReviewer", label: "Prime reviewer" },
  { value: "AuditorReviewer", label: "Auditor reviewer" },
  { value: "AdvisorReviewer", label: "Advisor reviewer" },
  { value: "PackageRecipient", label: "Package recipient" }
];

function ids(value: string) {
  return [...new Set(value.split(/[\s,]+/).map(item => item.trim()).filter(Boolean))];
}

export function ExternalPortalInvitationPanel() {
  const [invitations, setInvitations] = useState<ExternalPortalInvitation[]>([]);
  const [loadState, setLoadState] = useState<"loading" | "ready" | "error">("loading");
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState("");
  const [email, setEmail] = useState("");
  const [role, setRole] = useState<ExternalPortalRole>("PrimeReviewer");
  const [packageIds, setPackageIds] = useState("");
  const [contractIds, setContractIds] = useState("");
  const [expiresAt, setExpiresAt] = useState("");
  const [canDownload, setCanDownload] = useState(false);
  const [strongAuthenticationRequired, setStrongAuthenticationRequired] = useState(true);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [action, setAction] = useState<"extend" | "revoke" | null>(null);
  const [actionValue, setActionValue] = useState("");
  const [history, setHistory] = useState<Record<string, ExternalPortalAccessHistory[]>>({});

  useEffect(() => {
    let mounted = true;
    getExternalPortalInvitations().then(items => {
      if (!mounted) return;
      setInvitations(items);
      setLoadState("ready");
    }).catch(() => { if (mounted) setLoadState("error"); });
    return () => { mounted = false; };
  }, []);

  function replace(updated: ExternalPortalInvitation) {
    setInvitations(current => current.map(item => item.id === updated.id ? updated : item));
  }

  async function create(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSaving(true);
    setMessage("");
    try {
      const result = await createExternalPortalInvitation({
        email, role, packageIds: ids(packageIds), contractIds: ids(contractIds),
        expiresAt: new Date(`${expiresAt}T23:59:59Z`).toISOString(), canDownload, strongAuthenticationRequired
      });
      if (!result.data) { setMessage(result.error ?? "The invitation could not be created."); return; }
      setInvitations(current => [result.data!, ...current]);
      setEmail(""); setPackageIds(""); setContractIds(""); setExpiresAt("");
      setMessage("External portal invitation created.");
    } catch { setMessage("The invitation could not be created."); }
    finally { setSaving(false); }
  }

  async function resend(invitationId: string) {
    setSaving(true); setMessage("");
    try {
      const result = await resendExternalPortalInvitation(invitationId);
      if (!result.data) { setMessage(result.error ?? "The invitation could not be resent."); return; }
      replace(result.data); setMessage("Invitation resend recorded without changing its scope or expiration.");
    } catch { setMessage("The invitation could not be resent."); }
    finally { setSaving(false); }
  }

  async function submitAction(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!selectedId || !action) return;
    setSaving(true); setMessage("");
    try {
      const result = action === "extend"
        ? await extendExternalPortalInvitation(selectedId, new Date(`${actionValue}T23:59:59Z`).toISOString())
        : await revokeExternalPortalInvitation(selectedId, actionValue);
      if (!result.data) { setMessage(result.error ?? "The invitation could not be updated."); return; }
      replace(result.data); setMessage(action === "extend" ? "Invitation expiration extended." : "Invitation access revoked immediately.");
      setSelectedId(null); setAction(null); setActionValue("");
    } catch { setMessage("The invitation could not be updated."); }
    finally { setSaving(false); }
  }

  async function loadHistory(invitationId: string) {
    setMessage("");
    try {
      const entries = await getExternalPortalAccessHistory(invitationId);
      setHistory(current => ({ ...current, [invitationId]: entries }));
    } catch { setMessage("Portal access history could not be loaded."); }
  }

  if (loadState === "loading") return <section aria-label="External portal invitations"><p role="status">Loading portal invitations…</p></section>;
  if (loadState === "error") return <section aria-label="External portal invitations"><p role="alert">Portal invitations could not be loaded.</p></section>;

  return <section className="route-panel workflow-control-surface" aria-labelledby="external-portal-invitations-heading">
    <div className="section-heading">
      <p className="eyebrow">External review identities</p>
      <h2 id="external-portal-invitations-heading">External portal invitations</h2>
      <p>Portal roles are read-only and grant access only to the listed approved packages and contract scopes. This does not authorize CUI sharing.</p>
    </div>
    {message ? <p role={message.includes("could not") ? "alert" : "status"}>{message}</p> : null}
    <form className="portal-package-lifecycle__form" onSubmit={create}>
      <h3>Invite an external reviewer</h3>
      <label>Portal reviewer email<input required type="email" value={email} onChange={event => setEmail(event.target.value)} /></label>
      <label>Portal role<select value={role} onChange={event => setRole(event.target.value as ExternalPortalRole)}>
        {roles.map(item => <option key={item.value} value={item.value}>{item.label}</option>)}
      </select></label>
      <label>Approved package IDs<textarea required value={packageIds} onChange={event => setPackageIds(event.target.value)} /></label>
      <label>Contract scope IDs<textarea value={contractIds} onChange={event => setContractIds(event.target.value)} /></label>
      <label>Expiration date<input required type="date" value={expiresAt} onChange={event => setExpiresAt(event.target.value)} /></label>
      <label><input type="checkbox" checked={canDownload} onChange={event => setCanDownload(event.target.checked)} /> Allow approved package downloads</label>
      <label><input type="checkbox" checked={strongAuthenticationRequired} onChange={event => setStrongAuthenticationRequired(event.target.checked)} /> Require strong authentication</label>
      <button disabled={saving} type="submit">Create portal invitation</button>
    </form>
    <h3>Invitation access</h3>
    {invitations.length === 0 ? <p>No external portal invitations exist for this tenant.</p> : invitations.map(item =>
      <article key={item.id} aria-label={`Invitation for ${item.email}`}>
        <strong>{item.email} · {roles.find(roleItem => roleItem.value === item.role)?.label ?? item.role}</strong>
        <span>{item.status} · expires {new Date(item.expiresAt).toLocaleString()}</span>
        <small>{item.packageIds.length} package(s) · {item.contractIds.length} contract scope(s) · downloads {item.canDownload ? "allowed" : "blocked"}</small>
        <small>Strong authentication {item.strongAuthenticationRequired ? "required" : "not required"} · last access {item.lastAccessedAt ? new Date(item.lastAccessedAt).toLocaleString() : "never"}</small>
        {item.revocationReason ? <small>Revocation reason: {item.revocationReason}</small> : null}
        <div className="portal-package-lifecycle__actions">
          <button disabled={saving || item.status === "Revoked"} onClick={() => void resend(item.id)} type="button">Record resend</button>
          <button disabled={item.status === "Revoked"} onClick={() => { setSelectedId(item.id); setAction("extend"); setActionValue(""); }} type="button">Extend</button>
          <button disabled={item.status === "Revoked"} onClick={() => { setSelectedId(item.id); setAction("revoke"); setActionValue(""); }} type="button">Revoke invitation</button>
          <button onClick={() => void loadHistory(item.id)} type="button">View access history</button>
        </div>
        {history[item.id] ? history[item.id].length === 0 ? <p>No access attempts recorded.</p> : <ul>
          {history[item.id].map(entry => <li key={entry.id}>{entry.allowed ? "Allowed" : "Denied"} · {entry.resultCode} · {new Date(entry.occurredAt).toLocaleString()}</li>)}
        </ul> : null}
      </article>)}
    {selectedId && action ? <form className="portal-package-lifecycle__form" onSubmit={submitAction}>
      <h3>{action === "extend" ? "Extend invitation" : "Revoke invitation"}</h3>
      {action === "extend"
        ? <label>New expiration date<input required type="date" value={actionValue} onChange={event => setActionValue(event.target.value)} /></label>
        : <label>Revocation reason<textarea required maxLength={500} value={actionValue} onChange={event => setActionValue(event.target.value)} /></label>}
      <button disabled={saving} type="submit">Confirm {action}</button>
      <button onClick={() => { setSelectedId(null); setAction(null); setActionValue(""); }} type="button">Cancel</button>
    </form> : null}
  </section>;
}

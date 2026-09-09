import { useEffect, useState } from "react";
import { getCurrentUserAccess, getReadinessSources, getReadinessEvidence, recordReadinessEvidence,
  getReadinessNotice, acknowledgeReadinessNotice, type DataHandlingNotice,
  type ReadinessSource, type ReadinessEvidence, type CuiReadyApprovalChecklistItem, type UpdateCuiReadyChecklistItemRequest } from "@/lib/api";

export function ReadinessEvidencePanel({ children }: { children: (sources: ReadinessSource[], canApprove: boolean) => React.ReactNode }) {
  const [sources, setSources] = useState<ReadinessSource[]>([]);
  const [records, setRecords] = useState<ReadinessEvidence[]>([]);
  const [canApprove, setCanApprove] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");
  const [reload, setReload] = useState(0);
  const [busy, setBusy] = useState(false);
  const [kind, setKind] = useState("support-escalation");
  const [source, setSource] = useState("");
  const [notes, setNotes] = useState("");
  const [expiry, setExpiry] = useState("");
  const [details, setDetails] = useState("{}");
  const [rejected, setRejected] = useState(false);
  const [notice, setNotice] = useState<DataHandlingNotice | null>(null);
  const [noticeAccepted, setNoticeAccepted] = useState(false);
  useEffect(() => {
    let active = true;
    Promise.all([getCurrentUserAccess(), getReadinessSources(), getReadinessEvidence(), getReadinessNotice()]).then(([access, available, history, currentNotice]) => {
      if (active) { setCanApprove(access.canApproveCuiReadiness === true); setSources(available); setRecords(history); setNotice(currentNotice); setNoticeAccepted(false); setError(""); setLoading(false); }
    }).catch(e => { if (active) { setError(e instanceof Error ? e.message : "Readiness evidence could not be loaded."); setCanApprove(false); setSources([]); setLoading(false); } });
    return () => { active = false; };
  }, [reload]);
  async function save() {
    setBusy(true); setMessage("");
    try {
      const result = await recordReadinessEvidence({ kind, expectedVersion: Math.max(0, ...records.filter(r => r.kind === kind).map(r => r.version)),
        expiresAt: new Date(expiry).toISOString(), sourceReference: source, reviewNotes: notes, details: JSON.parse(details), rejected });
      if (!result.data) throw new Error(result.error ?? "Evidence was not recorded.");
      setMessage("Evidence version recorded. Relink affected checklist items and request a new review.");
      setReload(v => v + 1);
    } catch (e) { setMessage(e instanceof Error ? e.message : "Evidence could not be recorded."); }
    finally { setBusy(false); }
  }
  return <>
    <p>Final approval checks the linked versions again. Expiry, replacement, or rejection invalidates their use even when an item is marked complete.</p>
    {loading && <p role="status">Loading readiness evidence…</p>}
    {error && <p role="alert">{error}</p>}
    <button type="button" disabled={busy || loading} onClick={() => { setLoading(true); setReload(v => v + 1); }}>Refresh supporting records</button>
    {!loading && !error && sources.length === 0 && <p>No current supporting records are available. A platform reviewer must record readiness evidence; current CuiReady onboarding notice and matrix acknowledgements are also needed.</p>}
    {!canApprove && !loading && <p>Final approval and recording readiness evidence require platform readiness approval permission.</p>}
    {children(sources, canApprove && !loading && !error)}
    {notice && <details><summary>Proposed CuiReady onboarding notice · {notice.version}</summary>
      <p>{notice.body}</p><p>Acknowledgement does not change tenant mode or authorize CUI handling.</p>
      <label><input type="checkbox" checked={noticeAccepted} onChange={e => setNoticeAccepted(e.target.checked)} />I acknowledge this proposed-mode notice.</label>
      <button type="button" disabled={!noticeAccepted || busy || loading} onClick={async () => {
        setBusy(true);
        try {
          const result = await acknowledgeReadinessNotice({ mode: "CuiReady", workflowContext: "Onboarding", noticeId: notice.noticeId, noticeVersion: notice.version, acknowledged: true });
          setMessage(result.data ? "Readiness notice acknowledged. Tenant mode is unchanged." : result.error ?? "Acknowledgement failed.");
          if (result.data) setReload(v => v + 1);
        } catch { setMessage("Acknowledgement could not be confirmed. Refresh before retrying."); }
        finally { setBusy(false); }
      }}>Acknowledge proposed-mode notice</button>
    </details>}
    <details><summary>Supporting evidence versions</summary>
      <ul>{records.map(r => <li key={r.id}>{r.kind} · v{r.version} · {r.state} · expires {new Date(r.expiresAt).toLocaleDateString()}</li>)}</ul>
      {canApprove && <form onSubmit={e => { e.preventDefault(); void save(); }}>
        <p>Record a qualified review of actual supporting evidence. Do not paste customer content, CUI, secrets, or credentials. Recording a new version supersedes the previous version.</p>
        <label>Evidence kind<select value={kind} onChange={e => { setKind(e.target.value); setDetails("{}"); }}>
          {["support-escalation"].map(k => <option key={k}>{k}</option>)}
        </select></label>
        <label>Source reference<input required maxLength={600} value={source} onChange={e => setSource(e.target.value)} /></label>
        <label>Review notes<textarea required maxLength={1200} value={notes} onChange={e => setNotes(e.target.value)} /></label>
        <label>Expires at<input type="datetime-local" required value={expiry} onChange={e => setExpiry(e.target.value)} /></label>
        <p>Support evidence requires supportOwner, escalationContact, runbookReference, and coverage. Security, technical, and incident records are managed in the structured readiness section.</p>
        <label>Structured review metadata (JSON)<textarea required maxLength={64000} rows={10} value={details} onChange={e => setDetails(e.target.value)} /></label>
        <label><input type="checkbox" checked={rejected} onChange={e => setRejected(e.target.checked)} />Record rejected evidence</label>
        <button disabled={busy || loading}>Record reviewed version</button>
      </form>}
    </details>
    {message && <p role="status">{message}</p>}
  </>;
}

export function ReadinessItemEditor({ item, sources, userId, disabled, onSave }: { item: CuiReadyApprovalChecklistItem;
  sources: ReadinessSource[]; userId: string | null; disabled: boolean; onSave: (request: UpdateCuiReadyChecklistItemRequest) => void }) {
  const [owner, setOwner] = useState(item.owner ?? "");
  const [notes, setNotes] = useState(item.notes ?? "");
  const [recordId, setRecordId] = useState(item.supportingRecordId ?? "");
  const linkedKind = ["security-review", "incident-response", "backup-restore", "support-escalation", "data-handling-notice", "shared-responsibility-matrix"].includes(item.itemKey);
  const selected = sources.find(s => s.id === recordId && s.kind === item.itemKey);
  return <form onSubmit={e => { e.preventDefault(); onSave({ status: "Complete", owner, notes, evidenceLink: item.evidenceLink,
    reviewerUserId: userId, reviewedAt: new Date().toISOString().slice(0, 10), supportingRecordId: selected?.id ?? null, supportingVersion: selected?.version ?? null }); }}>
    <label>Owner<input required value={owner} onChange={e => setOwner(e.target.value)} maxLength={180} /></label>
    <label>Review notes<textarea required value={notes} onChange={e => setNotes(e.target.value)} maxLength={1200} /></label>
    {linkedKind && <label>Current supporting record<select required value={recordId} onChange={e => setRecordId(e.target.value)}>
      <option value="">Select current supporting evidence</option>
      {sources.filter(s => s.kind === item.itemKey).map(s => <option key={s.id} value={s.id}>{s.title}</option>)}
    </select></label>}
    {linkedKind && !selected && <p>A current supporting record must be linked before completing this item.</p>}
    <button disabled={disabled || !userId || (linkedKind && !selected)}>Save reviewed item</button>
  </form>;
}

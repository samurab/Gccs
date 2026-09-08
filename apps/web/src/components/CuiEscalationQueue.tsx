import { useEffect, useState } from "react";
import "./ClassifiedContent.css";
import {
  changeCuiSupportEscalationStatus,
  getCuiSupportEscalations,
  updateCuiSupportEscalation,
  type CuiSupportEscalation
} from "@/lib/api";

export function CuiEscalationQueue({ tenantId, permissions }: { tenantId: string; permissions: string[] }) {
  const canManage = permissions.includes("ManageTenant");
  const [items, setItems] = useState<CuiSupportEscalation[]>([]);
  const [loading, setLoading] = useState(canManage);
  const [error, setError] = useState("");
  const [note, setNote] = useState<Record<string, string>>({});
  const [busyId, setBusyId] = useState("");

  useEffect(() => {
    let active = true;
    if (!canManage) return;
    void getCuiSupportEscalations(tenantId)
      .then(data => { if (active) { setItems(data); setError(""); } })
      .catch(err => { if (active) setError(err instanceof Error ? err.message : "Escalations could not be loaded."); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [canManage, tenantId]);

  if (!canManage) return null;
  async function assign(item: CuiSupportEscalation, form: HTMLFormElement) {
    const data = new FormData(form); setBusyId(item.id);
    const result = await updateCuiSupportEscalation(tenantId, item.id, {
      owner: String(data.get("owner") ?? ""), severity: String(data.get("severity") ?? "Medium"), status: item.status
    });
    if (result.data) setItems(current => current.map(candidate => candidate.id === item.id ? result.data! : candidate));
    else setError(result.error ?? "Escalation assignment failed.");
    setBusyId("");
  }
  async function changeStatus(item: CuiSupportEscalation, status: string) {
    const statusNote = note[item.id]?.trim(); if (!statusNote) return;
    setBusyId(item.id);
    const result = await changeCuiSupportEscalationStatus(tenantId, item.id, { status, note: statusNote });
    if (result.data) { setItems(current => current.map(candidate => candidate.id === item.id ? result.data! : candidate)); setNote(current => ({ ...current, [item.id]: "" })); }
    else setError(result.error ?? "Escalation status update failed.");
    setBusyId("");
  }

  return <details className="evidence-metadata classification-review-panel">
    <summary>CUI escalation work queue</summary>
    <section className="classification-review-body" aria-label="Restricted CUI escalation work queue">
      <p className="classification-guidance">Restricted tenant support view. Descriptions must contain metadata only, never document or note contents.</p>
      {loading ? <p role="status">Loading escalation queue…</p> : error ? <p role="alert">{error}</p> : items.length === 0 ? <p>No escalations are recorded.</p> :
        <div className="classification-history"><ol>{items.map(item => <li key={item.id}>
          <h4>{item.category} · {item.severity} · {item.status}</h4>
          <p>{item.sourceWorkflow} · {item.affectedEntityType} {item.affectedEntityId}</p><p>{item.description}</p>
          <form onSubmit={event => { event.preventDefault(); void assign(item, event.currentTarget); }}>
            <div className="classification-filters"><label>Assigned owner<input name="owner" defaultValue={item.owner ?? ""} required maxLength={180} /></label>
              <label>Severity<select name="severity" defaultValue={item.severity}><option>Low</option><option>Medium</option><option>High</option><option>Critical</option></select></label></div>
            <button type="submit" disabled={busyId === item.id}>Save assignment</button>
          </form>
          <label>Status note<textarea aria-label={`Status note for ${item.id}`} value={note[item.id] ?? ""} maxLength={1200} onChange={event => setNote(current => ({ ...current, [item.id]: event.target.value }))} /></label>
          <div className="classification-pagination"><button disabled={busyId === item.id || !note[item.id]?.trim()} onClick={() => void changeStatus(item, "Triage")}>Move to triage</button>
            <button disabled={busyId === item.id || !note[item.id]?.trim()} onClick={() => void changeStatus(item, "Contained")}>Mark contained</button></div>
        </li>)}</ol></div>}
    </section>
  </details>;
}

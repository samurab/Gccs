import { useEffect, useRef, useState } from "react";
import "./ClassifiedContent.css";
import {
  createCuiSupportEscalation, getClassifiedContent, getClassifiedContentDetail, getClassificationHistory,
  getCuiSupportEscalations, resolveCuiSupportEscalation, reviewContentClassification,
  type ClassifiedContent, type ClassifiedContentRoute, type ClassificationHistory, type ContentClassification,
  type CuiSupportEscalation
} from "@/lib/api";

const contentTypes = [
  { route: "evidence-items", label: "Evidence items", group: "evidence", read: "ViewEvidence", review: "ApproveEvidence" },
  { route: "evidence-file-versions", label: "File versions", group: "evidence", read: "ViewEvidence", review: "ApproveEvidence" },
  { route: "notes", label: "Notes", group: "evidence", read: "ViewEvidence", review: "ApproveEvidence" },
  { route: "contract-documents", label: "Contract documents", group: "contracts", read: "ViewContracts", review: "ReviewClauses" },
  { route: "extraction-jobs", label: "Extraction jobs", group: "contracts", read: "ViewContracts", review: "ReviewClauses" },
  { route: "reports", label: "Reports", group: "reports", read: "ViewReports", review: "ManageReports" }
] as const;

export function ClassificationBadge({ classification }: { classification: string }) {
  return <span className={`status classification-badge status--${classification.toLowerCase()}`}>
    {classification === "SyntheticCui" ? "Synthetic demo data" : classification}
  </span>;
}

function Metadata({ value }: { value: ContentClassification }) {
  return <dl className="classification-metadata">
    <dt>Classification</dt><dd><ClassificationBadge classification={value.classification} /></dd>
    <dt>Source</dt><dd>{value.source}</dd>
    <dt>Confidence</dt><dd>{value.confidence ?? "Not recorded"}</dd>
    <dt>Reviewer</dt><dd>{value.reviewedByUserId ?? "Not reviewed"}</dd>
    <dt>Review time</dt><dd>{value.reviewedAt ? new Date(value.reviewedAt).toLocaleString() : "Not recorded"}</dd>
    <dt>Reason</dt><dd>{value.reason ?? "Not recorded"}</dd>
    <dt>Imported demo approval</dt><dd>{value.isApprovedDemoContent ? "Recorded" : "Not recorded"}</dd>
  </dl>;
}

export function ClassificationReviewPanel({ group, tenantId, permissions, onChanged }: {
  group: "evidence" | "contracts" | "reports"; tenantId: string; permissions: string[]; onChanged: (item: ClassifiedContent) => void;
}) {
  const available = contentTypes.filter(t => t.group === group && permissions.includes(t.read));
  const [route, setRoute] = useState<ClassifiedContentRoute>(available[0]?.route ?? "evidence-items");
  const definition = contentTypes.find(t => t.route === route)!;
  const canRead = available.some(t => t.route === route);
  const canReview = canRead && permissions.includes(definition.review);
  const canEscalate = canRead;
  const canManageEscalations = canRead && permissions.includes("ManageTenant");
  const [reviewOnly, setReviewOnly] = useState(true);
  const [offset, setOffset] = useState(0);
  const [items, setItems] = useState<ClassifiedContent[]>([]);
  const [selected, setSelected] = useState<ClassifiedContent | null>(null);
  const [history, setHistory] = useState<ClassificationHistory[]>([]);
  const [moreHistory, setMoreHistory] = useState(false);
  const [escalations, setEscalations] = useState<CuiSupportEscalation[]>([]);
  const [classification, setClassification] = useState("");
  const [reason, setReason] = useState("");
  const [supportReason, setSupportReason] = useState("");
  const [supportCategory, setSupportCategory] = useState("SuspectedCui");
  const [supportSeverity, setSupportSeverity] = useState("Medium");
  const [busy, setBusy] = useState(false);
  const [loading, setLoading] = useState(true);
  const [listError, setListError] = useState("");
  const [message, setMessage] = useState("");
  const [mustReload, setMustReload] = useState(false);
  const [reload, setReload] = useState(0);
  const request = useRef(0);
  useEffect(() => {
    let active = true;
    if (canRead) void Promise.resolve().then(() => getClassifiedContent(route, reviewOnly, offset))
      .then(data => { if (active) { setItems(data); setLoading(false); setListError(""); } })
      .catch(error => { if (active) { setItems([]); setLoading(false); setListError(error instanceof Error ? error.message : "Classification records could not be loaded."); } });
    // This is a generation counter, not a DOM ref: cancel the latest request on scope change/unmount.
    // eslint-disable-next-line react-hooks/exhaustive-deps
    return () => { active = false; request.current++; };
  }, [route, reviewOnly, offset, reload, canRead]);

  function refreshList() { setLoading(true); setReload(v => v + 1); }
  function changeList(nextRoute: ClassifiedContentRoute, onlyReview: boolean, nextOffset: number) {
    request.current++; setSelected(null); setMessage(""); setItems([]); setLoading(true);
    setRoute(nextRoute); setReviewOnly(onlyReview); setOffset(nextOffset);
    setReload(value => value + 1);
  }
  async function open(id: string) {
    const attempt = ++request.current; setBusy(true); setSelected(null); setMessage(""); setMustReload(false);
    try {
      const [item, entries, support] = await Promise.all([
        getClassifiedContentDetail(route, id), getClassificationHistory(route, id),
        canManageEscalations ? getCuiSupportEscalations(tenantId) : Promise.resolve([])
      ]);
      if (attempt !== request.current) return;
      setSelected(item); setHistory(entries); setMoreHistory(entries.length === 100);
      setEscalations(support.filter(e => e.affectedEntityType === item.entityType && e.affectedEntityId === item.id && e.status !== "Resolved"));
      setClassification(item.classification.classification); setReason(""); setSupportReason("");
      setSupportCategory(item.classification.classification === "Prohibited" ? "ProhibitedData" : "SuspectedCui");
    } catch (error) {
      if (attempt === request.current) setMessage(error instanceof Error ? error.message : "The current item could not be loaded.");
    } finally { if (attempt === request.current) setBusy(false); }
  }
  async function review() {
    if (!selected || !canReview || busy || mustReload || !reason.trim()) return;
    const attempt = request.current; setBusy(true);
    try {
      const result = await reviewContentClassification(route, selected.id, selected.revision, classification, reason.trim());
      if (attempt !== request.current) return;
      if (!result.data) { setMessage(`${result.error ?? "Review failed."} Reload the current item before retrying.`); setMustReload(true); return; }
      setSelected(result.data); setReason(""); setMessage("Classification review saved. Existing escalations still require a separate resolution.");
      onChanged(result.data);
      const entries = await getClassificationHistory(route, selected.id);
      if (attempt !== request.current) return;
      setHistory(entries); setMoreHistory(entries.length === 100); refreshList();
    } catch {
      if (attempt === request.current) { setMessage("Reload the current item to verify whether the review was saved."); setMustReload(true); }
    } finally { if (attempt === request.current) setBusy(false); }
  }
  async function supportAction(escalationId?: string) {
    if (!selected || !canEscalate || (escalationId && !canManageEscalations) || busy || mustReload || !supportReason.trim()) return;
    const attempt = request.current; setBusy(true);
    try {
      const result = escalationId
        ? await resolveCuiSupportEscalation(tenantId, escalationId, { resolutionType: "FalsePositive", summary: supportReason.trim() })
        : await createCuiSupportEscalation(tenantId, { sourceWorkflow: "ClassificationReview", affectedEntityType: selected.entityType,
            affectedEntityId: selected.id, category: supportCategory,
            severity: supportSeverity, description: supportReason.trim() });
      if (attempt !== request.current) return;
      if (result.data) {
        setEscalations(current => escalationId ? current.filter(e => e.id !== escalationId) : [...current, result.data!]);
        setSupportReason(""); setMessage(escalationId ? "Reviewed false-positive escalation resolved." : "Escalation recorded. Use remains blocked until authorized resolution.");
        onChanged(selected);
      } else { setMessage(result.error ?? "Escalation action failed."); setMustReload(true); }
    } catch {
      if (attempt === request.current) { setMessage("Reload the current item before retrying the escalation action."); setMustReload(true); }
    } finally { if (attempt === request.current) setBusy(false); }
  }
  if (!canRead) return <p role="status">You do not have permission to view classification records.</p>;
  return <details className="evidence-metadata classification-review-panel">
    <summary>Classification review and history</summary>
    <section aria-label="Classification review and history" className="classification-review-body">
      <p className="classification-guidance">Unknown and Prohibited content cannot be used. CUI remains subject to tenant handling restrictions. Review does not authorize CUI storage.</p>
      <div className="classification-workbench">
      <div className="classification-browser">
      <div className="classification-filters">
        <label>Content type<select aria-label="Content type" value={route} disabled={busy} onChange={e => changeList(e.target.value as ClassifiedContentRoute, reviewOnly, 0)}>
          {available.map(t => <option key={t.route} value={t.route}>{t.label}</option>)}
        </select></label>
        <label className="classification-checkbox"><input type="checkbox" checked={reviewOnly} disabled={busy} onChange={e => changeList(route, e.target.checked, 0)} /> Needs classification review only</label>
      </div>
      {loading ? <p role="status">Loading classification records…</p> : listError ? <p role="alert">{listError}</p> :
        items.length === 0 ? <p>No classification records match this page.</p> :
          <ul className="classification-item-list" tabIndex={0} aria-label="Classification records">{items.map(item => <li key={item.id}>
            <button type="button" aria-pressed={selected?.id === item.id} disabled={busy} onClick={() => void open(item.id)}>Inspect {item.title}</button>{" "}
            <ClassificationBadge classification={item.classification.classification} />
          </li>)}</ul>}
      <div className="classification-pagination">
        <button type="button" disabled={busy || loading || offset === 0} onClick={() => changeList(route, reviewOnly, Math.max(0, offset - 100))}>Previous classification page</button>
        <button type="button" disabled={busy || loading || items.length < 100 || offset >= 100000} onClick={() => changeList(route, reviewOnly, offset + 100)}>Next classification page</button>
        <button type="button" disabled={busy || loading} onClick={refreshList}>Refresh classification list</button>
      </div>
      </div>
      {selected ? <article aria-label="Current classification detail" className="classification-detail">
        <h3>{selected.title}</h3><p className="classification-reference">{definition.label} · {selected.id} · revision {selected.revision}</p>
        <Metadata value={selected.classification} />
        <button type="button" disabled={busy} onClick={() => void open(selected.id)}>Reload current item</button>
        {canReview ? <form onSubmit={e => { e.preventDefault(); void review(); }}>
          <label>Reviewed classification<select aria-label="Reviewed classification" value={classification} disabled={busy || mustReload} onChange={e => setClassification(e.target.value)}>
            {classification === "SyntheticCui" && <option value="SyntheticCui" disabled>Synthetic demo data (imported; select a review classification)</option>}
            {["Unclassified", "Fci", "Cui", "Unknown", "Prohibited"].map(value => <option key={value}>{value}</option>)}
          </select></label>
          <label>Review reason<textarea aria-label="Review reason" required maxLength={600} value={reason} disabled={busy || mustReload} onChange={e => setReason(e.target.value)} /></label>
          <p>Use metadata-only reasons. Do not paste file or note contents. Synthetic demo provenance cannot be assigned here.</p>
          <button className="classification-primary" type="submit" disabled={busy || mustReload || !reason.trim() || classification === "SyntheticCui"}>Save classification review</button>
        </form> : <p>Your role can inspect classification and history but cannot reclassify this content.</p>}
        <section aria-label="Data handling escalation">
          <h4>Data handling escalation</h4>
          {canEscalate ? <>
            {!escalations.length && <div className="classification-filters">
              <label>Concern category<select aria-label="Concern category" value={supportCategory} disabled={busy || mustReload} onChange={e => setSupportCategory(e.target.value)}>
                <option value="AccidentalCuiUpload">Accidental CUI upload</option><option value="SuspectedCui">Suspected CUI</option>
                <option value="ProhibitedData">Prohibited data</option><option value="Misclassification">Misclassification</option><option value="CustomerQuestion">Customer question</option>
              </select></label>
              <label>Severity<select aria-label="Escalation severity" value={supportSeverity} disabled={busy || mustReload} onChange={e => setSupportSeverity(e.target.value)}>
                <option value="Low">Low</option><option value="Medium">Medium</option><option value="High">High</option><option value="Critical">Critical</option>
              </select></label>
            </div>}
            <label>Escalation or resolution reason<textarea aria-label="Escalation or resolution reason" maxLength={1000} value={supportReason} disabled={busy || mustReload} onChange={e => setSupportReason(e.target.value)} /></label>
            {canManageEscalations && escalations.length ? escalations.map(e => <div key={e.id}>
              <p>Escalation {e.id} · {e.status}</p>
              <button type="button" disabled={busy || mustReload || !supportReason.trim() || !["Unclassified", "Fci"].includes(selected.classification.classification)}
                onClick={() => void supportAction(e.id)}>Resolve reviewed false positive</button>
            </div>) : <button type="button" disabled={busy || mustReload || !supportReason.trim()} onClick={() => void supportAction()}>Report data-handling concern</button>}
            <p>Release requires a safe review after escalation. A classification change alone does not release contained content.</p>
          </> : null}
        </section>
        <section aria-label="Classification history" className="classification-history"><h4>Classification history</h4>
          {history.length ? <ol>{history.map(entry => <li key={entry.id}>
            <p>Revision {entry.revision ?? "legacy"}: {entry.previousClassification ?? "Not recorded"} → {entry.newClassification}</p>
            <p>Changed by {entry.changedByUserId} · {new Date(entry.changedAt).toLocaleString()}</p>
            <p>Source {entry.source} · confidence {entry.confidence ?? "not recorded"} · reviewer {entry.reviewedByUserId ?? "not recorded"} · {entry.reviewedAt ? new Date(entry.reviewedAt).toLocaleString() : "review time not recorded"}</p>
            <p>{entry.reason ?? "No reason recorded in this historical entry."}</p>
            {entry.previousMetadata && <details><summary>Previous classification metadata</summary><Metadata value={entry.previousMetadata} /></details>}
          </li>)}</ol> : <p>No classification history is recorded. Legacy history is not inferred.</p>}
          {moreHistory && <button type="button" disabled={busy} onClick={async () => {
            const attempt = request.current; setBusy(true);
            try { const older = await getClassificationHistory(route, selected.id, history.length);
              if (attempt === request.current) { setHistory(current => [...current, ...older]); setMoreHistory(older.length === 100); }
            } catch { if (attempt === request.current) setMessage("Older history could not be loaded."); }
            finally { if (attempt === request.current) setBusy(false); }
          }}>Load older classification history</button>}
        </section>
      </article> : items.length > 0 && <p className="classification-empty">Select an item to see its current classification, review details, and change history.</p>}
      </div>
      {message && <p role="status">{message}</p>}
      {busy && <p role="status">Working on the selected classification…</p>}
    </section>
  </details>;
}

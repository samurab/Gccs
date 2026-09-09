import { useEffect, useState, type FormEvent } from "react";
import {
  changeSspSectionStatus, createSspSection, getSspSections, updateSspSection,
  type SspLinkedRecordType, type SspSection, type SspSectionStatus, type SspSectionType
} from "@/lib/api";

const sectionTypes: SspSectionType[] = ["SystemDescription", "AuthorizationBoundary", "Environment", "Interconnections", "Users", "Roles", "DataTypes", "CuiHandlingPosture", "ControlImplementationNarratives", "InheritedResponsibilities", "ExternalServiceProviders", "EvidenceReferences"];
const linkTypes: SspLinkedRecordType[] = ["CompanyProfile", "SystemBoundary", "Asset", "CmmcControl", "ResponsibilityMatrix", "Policy", "PoamItem", "Evidence"];
const emptyForm = { sectionType: "SystemDescription" as SspSectionType, title: "", owner: "", source: "", sourceUrl: "", lastReviewedAt: "", recordType: "CompanyProfile" as SspLinkedRecordType, recordId: "", relationship: "" };

export function SspSectionsPanel({ canManage }: { canManage: boolean }) {
  const [sections, setSections] = useState<SspSection[]>([]);
  const [selected, setSelected] = useState<SspSection | null>(null);
  const [form, setForm] = useState(emptyForm);
  const [loading, setLoading] = useState(canManage);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");
  const [reviewer, setReviewer] = useState("");
  const [reviewDate, setReviewDate] = useState("");

  async function load() {
    setLoading(true); setError("");
    try { setSections(await getSspSections()); }
    catch (reason) { setSections([]); setError(reason instanceof Error ? reason.message : "SSP sections could not be loaded."); }
    finally { setLoading(false); }
  }
  useEffect(() => {
    if (!canManage) return;
    let active = true;
    getSspSections().then(records => { if (active) { setSections(records); setError(""); setLoading(false); } })
      .catch(reason => { if (active) { setSections([]); setError(reason instanceof Error ? reason.message : "SSP sections could not be loaded."); setLoading(false); } });
    return () => { active = false; };
  }, [canManage]);

  function edit(section: SspSection) {
    const source = section.sourceReferences[0]; const link = section.linkedRecords[0];
    setSelected(section); setReviewer(section.reviewer ?? ""); setReviewDate(section.reviewDate ?? "");
    setForm({ sectionType: section.sectionType, title: section.title, owner: section.owner, source: source?.source ?? "", sourceUrl: source?.sourceUrl ?? "", lastReviewedAt: source?.lastReviewedAt ?? "", recordType: link?.recordType ?? "CompanyProfile", recordId: link?.recordId ?? "", relationship: link?.relationship ?? "" });
  }

  async function save(event: FormEvent) {
    event.preventDefault(); setBusy(true); setError(""); setMessage("");
    const payload = { sectionType: form.sectionType, title: form.title.trim(), owner: form.owner.trim(),
      sourceReferences: [{ source: form.source.trim(), sourceUrl: form.sourceUrl.trim(), lastReviewedAt: form.lastReviewedAt }],
      linkedRecords: form.recordId.trim() ? [{ recordType: form.recordType, recordId: form.recordId.trim(), relationship: form.relationship.trim() }] : [] };
    try {
      const result = selected
        ? await updateSspSection(selected.id, { ...payload, expectedVersion: selected.version })
        : await createSspSection(payload);
      if (!result.data) { setError(result.error ?? "SSP section could not be saved."); return; }
      setMessage(selected ? "SSP section updated." : "SSP section created."); setSelected(null); setForm(emptyForm); await load();
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "SSP section could not be saved.");
    } finally {
      setBusy(false);
    }
  }

  async function transition(section: SspSection, status: SspSectionStatus) {
    setBusy(true); setError(""); setMessage("");
    try {
      const result = await changeSspSectionStatus(section.id, { status, actorName: "Authenticated user", expectedVersion: section.version,
        reviewer: status === "Approved" ? reviewer.trim() : null, reviewDate: status === "Approved" ? reviewDate : null });
      if (!result.data) { setError(result.error ?? "SSP section status could not be changed."); return; }
      setMessage(`SSP section moved to ${status}.`); setSelected(result.data); await load();
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "SSP section status could not be changed.");
    } finally {
      setBusy(false);
    }
  }

  return <section aria-label="SSP sections" className="cmmc-create">
    <div className="section-heading section-heading--split"><div><h3>System Security Plan sections</h3>
      <p className="section-summary">Build reusable SSP structure from governed records and reviewed sources. Do not paste CUI or unsupported compliance claims.</p></div>
      <button type="button" disabled={loading || busy} onClick={() => void load()}>Refresh</button></div>
    {loading && <p role="status">Loading SSP sections…</p>}
    {error && <p role="alert" className="form-status form-status--error">{error}</p>}
    {!canManage && !loading && <p role="note">ManageTenant permission is required to create, edit, approve, supersede, or archive SSP sections.</p>}
    {!loading && !error && sections.length === 0 && <p>No SSP sections exist for this tenant.</p>}
    {sections.length > 0 && <div className="evidence-list">{sections.map(section => <article className="ui-task-card" key={section.id}>
      <button type="button" onClick={() => edit(section)}><strong>{section.title}</strong></button>
      <span>{section.sectionType} · {section.status} · owner {section.owner} · v{section.version}</span>
      <span>{section.linkedRecords.length} governed links · {section.sourceReferences.length} source references · {section.history.length} lifecycle events</span>
    </article>)}</div>}
    <form onSubmit={save} aria-label="SSP section editor">
      <fieldset disabled={!canManage || busy || Boolean(selected && !["Draft", "InReview"].includes(selected.status))}>
        <div className="form-grid">
          <label><span>Section type</span><select value={form.sectionType} onChange={event => setForm(current => ({ ...current, sectionType: event.target.value as SspSectionType }))}>{sectionTypes.map(type => <option key={type}>{type}</option>)}</select></label>
          <label><span>Title</span><input required maxLength={200} value={form.title} onChange={event => setForm(current => ({ ...current, title: event.target.value }))} /></label>
          <label><span>Owner</span><input required maxLength={200} value={form.owner} onChange={event => setForm(current => ({ ...current, owner: event.target.value }))} /></label>
          <label><span>Source name</span><input required maxLength={200} value={form.source} onChange={event => setForm(current => ({ ...current, source: event.target.value }))} /></label>
          <label><span>Source URL</span><input required type="url" maxLength={1000} value={form.sourceUrl} onChange={event => setForm(current => ({ ...current, sourceUrl: event.target.value }))} /></label>
          <label><span>Source reviewed</span><input required type="date" value={form.lastReviewedAt} onChange={event => setForm(current => ({ ...current, lastReviewedAt: event.target.value }))} /></label>
          <label><span>Linked record type</span><select value={form.recordType} onChange={event => setForm(current => ({ ...current, recordType: event.target.value as SspLinkedRecordType }))}>{linkTypes.map(type => <option key={type}>{type}</option>)}</select></label>
          <label><span>Linked record ID</span><input maxLength={120} value={form.recordId} onChange={event => setForm(current => ({ ...current, recordId: event.target.value }))} /></label>
          <label><span>Link rationale</span><input required={Boolean(form.recordId)} maxLength={200} value={form.relationship} onChange={event => setForm(current => ({ ...current, relationship: event.target.value }))} /></label>
        </div>
      </fieldset>
      <button disabled={!canManage || busy || Boolean(selected && !["Draft", "InReview"].includes(selected.status))}>{busy ? "Saving" : selected ? "Update section" : "Create section"}</button>
      {selected && <button type="button" onClick={() => { setSelected(null); setForm(emptyForm); }}>Cancel edit</button>}
    </form>
    {selected && canManage && <div aria-label="SSP lifecycle controls">
      {selected.status === "Draft" && <button disabled={busy} type="button" onClick={() => void transition(selected, "InReview")}>Submit for review</button>}
      {selected.status === "InReview" && <><label>Reviewer<input value={reviewer} onChange={event => setReviewer(event.target.value)} /></label><label>Review date<input type="date" value={reviewDate} onChange={event => setReviewDate(event.target.value)} /></label>
        <button disabled={busy || !reviewer.trim() || !reviewDate} type="button" onClick={() => void transition(selected, "Approved")}>Approve section</button></>}
      {selected.status === "Approved" && <><button disabled={busy} type="button" onClick={() => void transition(selected, "Superseded")}>Supersede</button><button disabled={busy} type="button" onClick={() => void transition(selected, "Archived")}>Archive</button></>}
      {selected.status === "Superseded" && <button disabled={busy} type="button" onClick={() => void transition(selected, "Archived")}>Archive</button>}
    </div>}
    {message && <p role="status">{message}</p>}
  </section>;
}

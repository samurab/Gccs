import { useEffect, useState, type FormEvent } from "react";
import {
  approveSspNarrative, changeSspSectionStatus, compareSspNarrative, createSspSection, editSspNarrative,
  createSspExportPackage, generateSspNarrative, getSspExportPackages, getSspNarratives, getSspSections, updateSspSection,
  type SspLinkedRecordType, type SspNarrative, type SspNarrativeComparison, type SspNarrativeSourceType,
  type SspExportPackage, type SspSection, type SspSectionStatus, type SspSectionType
} from "@/lib/api";

const sectionTypes: SspSectionType[] = ["SystemDescription", "AuthorizationBoundary", "Environment", "Interconnections", "Users", "Roles", "DataTypes", "CuiHandlingPosture", "ControlImplementationNarratives", "InheritedResponsibilities", "ExternalServiceProviders", "EvidenceReferences"];
const linkTypes: SspLinkedRecordType[] = ["CompanyProfile", "SystemBoundary", "Asset", "CmmcControl", "ResponsibilityMatrix", "Policy", "PoamItem", "Evidence"];
const emptyForm = { sectionType: "SystemDescription" as SspSectionType, title: "", owner: "", source: "", sourceUrl: "", lastReviewedAt: "", recordType: "CompanyProfile" as SspLinkedRecordType, recordId: "", relationship: "" };

export function SspSectionsPanel({ canManage, canExport = false }: { canManage: boolean; canExport?: boolean }) {
  const [sections, setSections] = useState<SspSection[]>([]);
  const [selected, setSelected] = useState<SspSection | null>(null);
  const [form, setForm] = useState(emptyForm);
  const [loading, setLoading] = useState(true);
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
    let active = true;
    getSspSections().then(records => { if (active) { setSections(records); setError(""); setLoading(false); } })
      .catch(reason => { if (active) { setSections([]); setError(reason instanceof Error ? reason.message : "SSP sections could not be loaded."); setLoading(false); } });
    return () => { active = false; };
  }, []);

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
    {!canManage && !loading && <p role="note">ManageCmmc permission is required to create, edit, or approve SSP content. Read-only SSP access remains available.</p>}
    {!loading && !error && sections.length === 0 && <p>No SSP sections exist for this tenant.</p>}
    {sections.length > 0 && <div className="evidence-list">{sections.map(section => <article className="ui-task-card" key={section.id}>
      <button type="button" onClick={() => edit(section)}><strong>{section.title}</strong></button>
      <span>{section.sectionType} · {section.status} · owner {section.owner} · v{section.version}</span>
      <span>{section.linkedRecords.length} governed links · {section.sourceReferences.length} source references · {section.history.length} lifecycle events</span>
    </article>)}</div>}
    <form className="cmmc-form" onSubmit={save} aria-label="SSP section editor">
      <fieldset disabled={!canManage || busy || Boolean(selected && !["Draft", "InReview"].includes(selected.status))}>
        <div className="form-grid cmmc-form-grid">
          <label><span>Section type</span><select value={form.sectionType} onChange={event => setForm(current => ({ ...current, sectionType: event.target.value as SspSectionType }))}>{sectionTypes.map(type => <option key={type}>{type}</option>)}</select></label>
          <label><span>Title</span><input required maxLength={200} value={form.title} onChange={event => setForm(current => ({ ...current, title: event.target.value }))} /></label>
          <label><span>Owner</span><input required maxLength={200} value={form.owner} onChange={event => setForm(current => ({ ...current, owner: event.target.value }))} /></label>
          <label><span>Source name</span><input required maxLength={200} value={form.source} onChange={event => setForm(current => ({ ...current, source: event.target.value }))} /></label>
          <label><span>Source URL</span><input required type="url" maxLength={1000} value={form.sourceUrl} onChange={event => setForm(current => ({ ...current, sourceUrl: event.target.value }))} /></label>
          <label><span>Source reviewed</span><input required type="date" value={form.lastReviewedAt} onChange={event => setForm(current => ({ ...current, lastReviewedAt: event.target.value }))} /></label>
          <label><span>Linked record type</span><select value={form.recordType} onChange={event => setForm(current => ({ ...current, recordType: event.target.value as SspLinkedRecordType }))}>{linkTypes.map(type => <option key={type}>{type}</option>)}</select></label>
          <label><span>Linked record ID</span><input maxLength={120} value={form.recordId} onChange={event => setForm(current => ({ ...current, recordId: event.target.value }))} /></label>
          <label className="span-2"><span>Link rationale</span><input required={Boolean(form.recordId)} maxLength={200} value={form.relationship} onChange={event => setForm(current => ({ ...current, relationship: event.target.value }))} /></label>
        </div>
      </fieldset>
      <div className="form-actions">
        <button disabled={!canManage || busy || Boolean(selected && !["Draft", "InReview"].includes(selected.status))}>{busy ? "Saving" : selected ? "Update section" : "Create section"}</button>
        {selected && <button type="button" onClick={() => { setSelected(null); setForm(emptyForm); }}>Cancel edit</button>}
      </div>
    </form>
    {selected && canManage && <div aria-label="SSP lifecycle controls">
      {selected.status === "Draft" && <button disabled={busy} type="button" onClick={() => void transition(selected, "InReview")}>Submit for review</button>}
      {selected.status === "InReview" && <><label>Reviewer<input value={reviewer} onChange={event => setReviewer(event.target.value)} /></label><label>Review date<input type="date" value={reviewDate} onChange={event => setReviewDate(event.target.value)} /></label>
        <button disabled={busy || !reviewer.trim() || !reviewDate} type="button" onClick={() => void transition(selected, "Approved")}>Approve section</button></>}
      {selected.status === "Approved" && <><button disabled={busy} type="button" onClick={() => void transition(selected, "Superseded")}>Supersede</button><button disabled={busy} type="button" onClick={() => void transition(selected, "Archived")}>Archive</button></>}
      {selected.status === "Superseded" && <button disabled={busy} type="button" onClick={() => void transition(selected, "Archived")}>Archive</button>}
    </div>}
    {selected && <SspNarrativeWorkspace key={selected.id} section={selected} canManage={canManage} />}
    <SspExportWorkspace canExport={canExport} />
    {message && <p role="status">{message}</p>}
  </section>;
}

function SspExportWorkspace({ canExport }: { canExport: boolean }) {
  const [packages, setPackages] = useState<SspExportPackage[]>([]);
  const [selected, setSelected] = useState<SspExportPackage | null>(null);
  const [packageVersion, setPackageVersion] = useState("");
  const [systemBoundary, setSystemBoundary] = useState("");
  const [reviewer, setReviewer] = useState("");
  const [evidenceIds, setEvidenceIds] = useState("");
  const [poamIds, setPoamIds] = useState("");
  const [loading, setLoading] = useState(canExport);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");

  async function load(preferredId?: string) {
    if (!canExport) return;
    setLoading(true); setError("");
    try {
      const records = await getSspExportPackages();
      setPackages(records);
      setSelected(records.find(item => item.id === preferredId) ?? records[0] ?? null);
    } catch (reason) {
      setPackages([]); setSelected(null);
      setError(reason instanceof Error ? reason.message : "SSP package history could not be loaded.");
    } finally { setLoading(false); }
  }

  useEffect(() => {
    if (!canExport) return;
    let active = true;
    getSspExportPackages().then(records => {
      if (!active) return;
      setPackages(records); setSelected(records[0] ?? null); setError(""); setLoading(false);
    }).catch(reason => {
      if (!active) return;
      setPackages([]); setSelected(null); setError(reason instanceof Error ? reason.message : "SSP package history could not be loaded."); setLoading(false);
    });
    return () => { active = false; };
  }, [canExport]);

  async function generate(event: FormEvent) {
    event.preventDefault(); setBusy(true); setError(""); setMessage("");
    try {
      const result = await createSspExportPackage({
        packageVersion: packageVersion.trim(), systemBoundary: systemBoundary.trim(), reviewer: reviewer.trim(),
        format: "Both", externalShareRequested: false,
        evidenceItemIds: parseIds(evidenceIds), poamItemIds: parseIds(poamIds)
      });
      if (!result.data) { setError(result.error ?? "SSP review package could not be generated."); return; }
      setMessage("Internal SSP review package generated and audit logged. External sharing requires separate approval.");
      setPackageVersion(""); setEvidenceIds(""); setPoamIds(""); await load(result.data.id);
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : "SSP review package could not be generated.");
    } finally { setBusy(false); }
  }

  return <section aria-label="SSP export packages" className="cmmc-create">
    <div className="section-heading"><h4>SSP review packages</h4>
      <p>Create immutable internal-review snapshots from approved, current-tenant records. Packages remain draft review material and do not represent certification, an assessment determination, authorization, or government endorsement.</p></div>
    {!canExport && <p role="note">ExportReports permission is required to generate or view SSP package history.</p>}
    {loading && <p role="status">Loading SSP package history…</p>}
    {error && <p role="alert" className="form-status form-status--error">{error}</p>}
    {canExport && !loading && !error && packages.length === 0 && <p>No SSP review packages exist for this tenant.</p>}
    {canExport && <form className="cmmc-form" onSubmit={generate} aria-label="Generate SSP review package">
      <fieldset disabled={busy}>
        <div className="form-grid cmmc-form-grid">
          <label><span>Package version</span><input required maxLength={80} value={packageVersion} onChange={event => setPackageVersion(event.target.value)} /></label>
          <label><span>Package reviewer</span><input required maxLength={200} value={reviewer} onChange={event => setReviewer(event.target.value)} /></label>
          <label className="span-2"><span>System boundary</span><textarea required maxLength={4000} rows={4} value={systemBoundary} onChange={event => setSystemBoundary(event.target.value)} /></label>
          <label className="span-2"><span>Approved evidence IDs</span><textarea aria-describedby="ssp-evidence-help" rows={3} value={evidenceIds} onChange={event => setEvidenceIds(event.target.value)} /></label>
          <small className="cmmc-form-help span-2" id="ssp-evidence-help">Enter UUIDs separated by commas or new lines. The server rejects unavailable, unapproved, expired, prohibited, unknown, CUI, or cross-tenant evidence.</small>
          <label className="span-2"><span>POA&amp;M item IDs</span><textarea rows={3} value={poamIds} onChange={event => setPoamIds(event.target.value)} /></label>
        </div>
      </fieldset>
      <div className="form-actions"><button disabled={busy}>{busy ? "Generating package" : "Generate internal review package"}</button></div>
    </form>}
    {packages.length > 0 && <div className="evidence-list" aria-label="SSP package history">{packages.map(item =>
      <button type="button" key={item.id} onClick={() => setSelected(item)} aria-pressed={selected?.id === item.id}>
        <strong>{item.packageVersion}</strong><span>{item.status} · {new Date(item.generatedAt).toLocaleString()} · {item.sections.length} sections</span>
      </button>)}</div>}
    {selected && <article aria-label="Selected SSP review package">
      <h5>{selected.tenantName} · {selected.packageVersion}</h5>
      <p role="note">{selected.disclaimer}</p>
      <p>{selected.includedEvidence.length} approved evidence reference(s) · {selected.poamReferences.length} POA&amp;M reference(s) · {selected.history.length} history event(s)</p>
      <details><summary>Human-readable report</summary><pre>{selected.humanReadableReport}</pre></details>
      <details><summary>Machine-readable metadata</summary><pre>{JSON.stringify(selected.machineReadableMetadata, null, 2)}</pre></details>
    </article>}
    {message && <p role="status">{message}</p>}
  </section>;
}

function parseIds(value: string): string[] {
  return value.split(/[\s,]+/).map(item => item.trim()).filter(Boolean);
}

const narrativeSourceTypes: SspNarrativeSourceType[] = ["Evidence", "GeneratedPolicy", "Clause", "Obligation"];

function SspNarrativeWorkspace({ section, canManage }: { section: SspSection; canManage: boolean }) {
  const [narratives, setNarratives] = useState<SspNarrative[]>([]);
  const [selected, setSelected] = useState<SspNarrative | null>(null);
  const [sourceType, setSourceType] = useState<SspNarrativeSourceType>("Evidence");
  const [sourceId, setSourceId] = useState("");
  const [draftSources, setDraftSources] = useState<Array<{ sourceType: SspNarrativeSourceType; recordId: string }>>([]);
  const [text, setText] = useState("");
  const [notes, setNotes] = useState("");
  const [classification, setClassification] = useState<"Unclassified" | "Fci" | "Cui">("Unclassified");
  const [reviewDate, setReviewDate] = useState("");
  const [comparison, setComparison] = useState<SspNarrativeComparison | null>(null);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");

  function selectNarrative(narrative: SspNarrative) {
    setSelected(narrative);
    setText(narrative.editedText ?? narrative.generatedText);
    setNotes(narrative.reviewerNotes ?? "");
    const current = narrative.classification.classification;
    setClassification(current === "Fci" || current === "Cui" ? current : "Unclassified");
    setComparison(null);
  }

  async function load(preferredId?: string) {
    setLoading(true); setError("");
    try {
      const records = await getSspNarratives(section.id);
      setNarratives(records);
      const next = records.find(record => record.id === preferredId) ?? records[0] ?? null;
      if (next) selectNarrative(next); else setSelected(null);
    } catch (reason) {
      setNarratives([]); setSelected(null);
      setError(reason instanceof Error ? reason.message : "SSP narratives could not be loaded.");
    } finally { setLoading(false); }
  }

  useEffect(() => {
    let active = true;
    getSspNarratives(section.id).then(records => {
      if (!active) return;
      setNarratives(records);
      const next = records[0] ?? null;
      setSelected(next);
      setText(next?.editedText ?? next?.generatedText ?? "");
      setNotes(next?.reviewerNotes ?? "");
      const current = next?.classification.classification;
      setClassification(current === "Fci" || current === "Cui" ? current : "Unclassified");
      setComparison(null);
      setError("");
      setLoading(false);
    }).catch(reason => {
      if (!active) return;
      setNarratives([]);
      setSelected(null);
      setError(reason instanceof Error ? reason.message : "SSP narratives could not be loaded.");
      setLoading(false);
    });
    return () => { active = false; };
  }, [section.id]);

  async function generate(event: FormEvent) {
    event.preventDefault(); setBusy(true); setError(""); setMessage("");
    try {
      const currentId = sourceId.trim();
      const sources = currentId ? [...draftSources, { sourceType, recordId: currentId }] : draftSources;
      const unique = sources.filter((source, index) => sources.findIndex(candidate =>
        candidate.sourceType === source.sourceType && candidate.recordId === source.recordId) === index);
      if (unique.length !== sources.length) { setError("Duplicate narrative sources are not allowed."); return; }
      const result = await generateSspNarrative(section.id, { sources: unique });
      if (!result.data) { setError(result.error ?? "Narrative draft could not be generated."); return; }
      setMessage("Source-backed narrative draft generated. Human review is required before approval.");
      setSourceId(""); setDraftSources([]); await load(result.data.id);
    } catch (reason) { setError(reason instanceof Error ? reason.message : "Narrative draft could not be generated."); }
    finally { setBusy(false); }
  }

  async function save(event: FormEvent) {
    event.preventDefault(); if (!selected) return;
    setBusy(true); setError(""); setMessage("");
    try {
      const result = await editSspNarrative(section.id, selected.id, {
        editedText: text, reviewerNotes: notes.trim() || null, expectedVersion: selected.version,
        classification: { classification, source: "UserSelected" }
      });
      if (!result.data) { setError(result.error ?? "Narrative draft could not be saved."); return; }
      setMessage("Narrative draft saved and retained as draft-only."); await load(result.data.id);
    } catch (reason) { setError(reason instanceof Error ? reason.message : "Narrative draft could not be saved."); }
    finally { setBusy(false); }
  }

  async function approve() {
    if (!selected) return; setBusy(true); setError(""); setMessage("");
    try {
      const result = await approveSspNarrative(section.id, selected.id, reviewDate, selected.version);
      if (!result.data) { setError(result.error ?? "Narrative approval was blocked."); return; }
      setMessage("Narrative approved. The previous approved narrative, if any, was superseded."); await load(result.data.id);
    } catch (reason) { setError(reason instanceof Error ? reason.message : "Narrative approval was blocked."); }
    finally { setBusy(false); }
  }

  async function compare() {
    if (!selected) return; setBusy(true); setError("");
    try { setComparison(await compareSspNarrative(section.id, selected.id)); }
    catch (reason) { setError(reason instanceof Error ? reason.message : "Narrative comparison could not be loaded."); }
    finally { setBusy(false); }
  }

  function addSource() {
    const recordId = sourceId.trim();
    if (!recordId) { setError("Enter an approved source record ID before adding it."); return; }
    if (draftSources.some(source => source.sourceType === sourceType && source.recordId === recordId)) {
      setError("Duplicate narrative sources are not allowed."); return;
    }
    setDraftSources(current => [...current, { sourceType, recordId }]);
    setSourceId(""); setError("");
  }

  return <section aria-label={`SSP narrative builder for ${section.title}`} className="cmmc-create">
    <div className="section-heading"><h4>SSP narrative builder</h4>
      <p>Generate only from approved records. Generated text is a draft for human review—not legal advice, certification, an assessor determination, or authorization to handle CUI.</p></div>
    <p role="note">Do not paste CUI, classified information, ITAR/export-controlled data, or sensitive government-furnished information. Classification is enforced by the server.</p>
    {loading && <p role="status">Loading SSP narratives…</p>}
    {error && <p role="alert" className="form-status form-status--error">{error}</p>}
    {!loading && narratives.length === 0 && <p>No narrative drafts exist for this section.</p>}
    {narratives.length > 0 && <div className="evidence-list">{narratives.map(narrative => <article className="ui-task-card" key={narrative.id}>
      <button type="button" onClick={() => selectNarrative(narrative)}><strong>{narrative.status} narrative · v{narrative.version}</strong></button>
      {narrative.draftOnly && <span role="status">Draft—human review required</span>}
      {narrative.aiAssisted && <span>AI-assisted draft</span>}
      <span>{narrative.sourceRecords.length} approved source link{narrative.sourceRecords.length === 1 ? "" : "s"} · {narrative.classification.classification}</span>
    </article>)}</div>}
    {canManage && <form className="cmmc-form" onSubmit={generate} aria-label="Generate SSP narrative draft">
      <fieldset disabled={busy}><legend>Generate from an approved source</legend>
        <div className="form-grid cmmc-form-grid">
          <label><span>Source type</span><select value={sourceType} onChange={event => setSourceType(event.target.value as SspNarrativeSourceType)}>{narrativeSourceTypes.map(type => <option key={type}>{type}</option>)}</select></label>
          <label><span>Approved source record ID</span><input maxLength={120} value={sourceId} onChange={event => setSourceId(event.target.value)} /></label>
        </div>
        <div className="form-actions"><button type="button" disabled={busy || !sourceId.trim()} onClick={addSource}>Add another source</button></div>
        {draftSources.length > 0 && <ul aria-label="Sources selected for narrative generation">{draftSources.map((source, index) =>
          <li key={`${source.sourceType}:${source.recordId}`}>{source.sourceType} · {source.recordId}
            <button type="button" onClick={() => setDraftSources(current => current.filter((_, itemIndex) => itemIndex !== index))}>Remove</button></li>)}</ul>}
      </fieldset><div className="form-actions"><button disabled={busy || (draftSources.length === 0 && !sourceId.trim())}>{busy ? "Generating" : "Generate draft"}</button></div>
    </form>}
    {selected && <div aria-label="Selected SSP narrative">
      <p><strong>{selected.draftOnly ? "Draft—human review required" : selected.status}</strong>{selected.aiAssisted ? " · AI-assisted" : " · Deterministic source-backed generation"}</p>
      <h5>Source links</h5><ul>{selected.sourceRecords.map(source => <li key={`${source.sourceType}:${source.recordId}`}>
        <a href={source.sourceUrl}>{source.label}</a> · {source.sourceType} · {source.classification}
      </li>)}</ul>
      {selected.status === "Draft" && canManage && <form className="cmmc-form" onSubmit={save} aria-label="Edit SSP narrative draft">
        <div className="form-grid cmmc-form-grid">
          <label className="span-2"><span>Narrative text</span><textarea required maxLength={20000} rows={10} value={text} onChange={event => setText(event.target.value)} /></label>
          <label className="span-2"><span>Reviewer notes</span><textarea maxLength={4000} rows={4} value={notes} onChange={event => setNotes(event.target.value)} /></label>
          <label className="span-2"><span>Content classification</span><select value={classification} onChange={event => setClassification(event.target.value as typeof classification)}>
            <option value="Unclassified">Unclassified</option><option value="Fci">FCI</option><option value="Cui">CUI (blocked unless tenant is explicitly approved)</option>
          </select></label>
        </div>
        <div className="form-actions"><button disabled={busy || !text.trim()}>{busy ? "Saving" : "Save draft"}</button></div>
      </form>}
      <div><button type="button" disabled={busy} onClick={() => void compare()}>Compare with current approved</button>
        {selected.status === "Draft" && canManage && <><label>Review date<input type="date" value={reviewDate} onChange={event => setReviewDate(event.target.value)} /></label>
          <button type="button" disabled={busy || !reviewDate} onClick={() => void approve()}>Approve narrative</button></>}</div>
    </div>}
    {comparison && <div aria-label="Narrative comparison" className="form-grid">
      <section><h5>Current approved narrative</h5><p>{comparison.currentApprovedText ?? "No approved narrative exists."}</p>
        {comparison.currentApprovedReviewer && <p>Reviewed by {comparison.currentApprovedReviewer} on {comparison.currentApprovedReviewDate}</p>}</section>
      <section><h5>Proposed narrative</h5><p>{comparison.proposedText}</p>{comparison.proposedReviewerNotes && <p>Reviewer notes: {comparison.proposedReviewerNotes}</p>}</section>
    </div>}
    {message && <p role="status">{message}</p>}
  </section>;
}

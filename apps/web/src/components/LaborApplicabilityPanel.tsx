import { useEffect, useMemo, useState, type FormEvent } from "react";
import {
  createLaborApplicability,
  getContractLaborApplicabilities,
  updateLaborApplicability,
  updateLaborApplicabilityStatus,
  uploadLaborWageDetermination,
  type ContractClause,
  type EvidenceMetadata,
  type LaborApplicability,
  type UpsertLaborApplicabilityRequest
} from "@/lib/api";

const emptyForm = (contractId: string): UpsertLaborApplicabilityRequest => ({
  contractId, scaApplicable: false, dbaApplicable: false, otherFarPart22Obligations: null,
  placeOfPerformance: "", contractPeriodStart: "", contractPeriodEnd: "",
  wageDeterminationReference: null, wageDeterminationEvidenceItemId: null,
  sourceContractClauseId: null, sourceClause: null, rationale: null, ownerFunction: "Contracts/HR",
  reviewStatus: "Draft", reviewNotes: null
});

export function LaborApplicabilityPanel({ contractId, clauses, evidence, canManage, canUpload }: {
  contractId: string; clauses: ContractClause[]; evidence: EvidenceMetadata[]; canManage: boolean; canUpload: boolean;
}) {
  const [items, setItems] = useState<LaborApplicability[]>([]);
  const [form, setForm] = useState(() => emptyForm(contractId));
  const [editingId, setEditingId] = useState<string | null>(null);
  const [state, setState] = useState<"loading" | "ready" | "saving" | "error">("loading");
  const [message, setMessage] = useState("");
  const [uploadItemId, setUploadItemId] = useState("");
  const [uploadFile, setUploadFile] = useState<File | null>(null);
  const [uploadClassification, setUploadClassification] = useState("");
  const [uploadReason, setUploadReason] = useState("");
  const [uploadAttested, setUploadAttested] = useState(false);

  useEffect(() => {
    let active = true;
    getContractLaborApplicabilities(contractId)
      .then(nextItems => {
        if (!active) return;
        setItems(nextItems);
        setForm(emptyForm(contractId)); setEditingId(null); setState("ready");
      })
      .catch(() => { if (active) { setMessage("Labor applicability records could not be loaded."); setState("error"); } });
    return () => { active = false; };
  }, [contractId]);

  const uploadableItems = useMemo(() => items.filter(item => item.wageDeterminationEvidenceItemId), [items]);
  const set = <K extends keyof UpsertLaborApplicabilityRequest>(key: K, value: UpsertLaborApplicabilityRequest[K]) =>
    setForm(current => ({ ...current, [key]: value }));

  async function save(event: FormEvent) {
    event.preventDefault(); setState("saving"); setMessage("");
    const result = editingId ? await updateLaborApplicability(contractId, editingId, form) : await createLaborApplicability(contractId, form);
    if (!result.data) { setState("error"); setMessage(result.error ?? "Labor applicability could not be saved."); return; }
    setItems(current => editingId ? current.map(item => item.id === result.data!.id ? result.data! : item) : [...current, result.data!]);
    setForm(emptyForm(contractId)); setEditingId(null); setState("ready"); setMessage(editingId ? "Labor applicability updated." : "Labor applicability recorded as a draft.");
  }

  function edit(item: LaborApplicability) {
    setEditingId(item.id);
    setForm({ contractId, scaApplicable: item.scaApplicable, dbaApplicable: item.dbaApplicable,
      otherFarPart22Obligations: item.otherFarPart22Obligations, placeOfPerformance: item.placeOfPerformance,
      contractPeriodStart: item.contractPeriodStart, contractPeriodEnd: item.contractPeriodEnd,
      wageDeterminationReference: item.wageDeterminationReference,
      wageDeterminationEvidenceItemId: item.wageDeterminationEvidenceItemId,
      sourceContractClauseId: item.sourceContractClauseId, sourceClause: item.sourceClause,
      rationale: item.rationale, ownerFunction: item.ownerFunction, reviewStatus: item.reviewStatus, reviewNotes: item.reviewNotes });
  }

  async function changeStatus(item: LaborApplicability, status: "Active" | "Inactive") {
    setState("saving"); setMessage("");
    const result = await updateLaborApplicabilityStatus(contractId, item.id, status);
    if (!result.data) { setState("error"); setMessage(result.error ?? "Labor applicability status could not be changed."); return; }
    setItems(current => current.map(existing => existing.id === item.id ? result.data! : existing));
    setState("ready"); setMessage(status === "Active" ? "Labor applicability activated and review task synchronized." : "Labor applicability deactivated and review task canceled.");
  }

  async function upload(event: FormEvent) {
    event.preventDefault();
    if (!uploadItemId || !uploadFile || !uploadClassification || !uploadAttested) { setState("error"); setMessage("Select a record, file, classification, and confirm the No-CUI attestation."); return; }
    setState("saving"); setMessage("");
    const result = await uploadLaborWageDetermination(contractId, uploadItemId, uploadFile, uploadClassification, uploadReason, uploadAttested);
    if (!result.data) { setState("error"); setMessage(result.error ?? "The wage determination upload was blocked."); return; }
    setState("ready"); setUploadFile(null); setUploadAttested(false);
    setMessage(`Wage determination uploaded. Classification ${result.data.classification.classification}; malware scan ${result.data.malwareScanStatus}.`);
  }

  return <section className="contract-esrs workflow-control-surface" aria-labelledby="labor-applicability-heading">
    <div className="contract-documents__header"><div><span>Labor applicability</span><strong id="labor-applicability-heading">{items.length}</strong></div></div>
    <p>Track source-backed SCA, DBA, and other FAR Part 22 decisions. FeDril organizes review work and does not provide a legal labor determination.</p>
    {state === "loading" ? <p role="status">Loading labor applicability…</p> : null}
    {state === "error" ? <p role="alert">{message}</p> : message ? <p role="status">{message}</p> : null}
    {items.length === 0 && state !== "loading" ? <p>No labor applicability decisions recorded for this contract.</p> : null}
    {items.map(item => <article key={item.id} aria-label={`${item.laborStandard || "Labor"} applicability`}>
      <strong>{item.laborStandard || "Applicability not selected"} · {item.status}</strong>
      <span>{item.placeOfPerformance} · {item.contractPeriodStart} to {item.contractPeriodEnd}</span>
      <small>Source: {item.sourceClause || item.rationale || "Not supplied"} · Review: {item.reviewStatus}</small>
      {item.wageDeterminationReference ? <small>Wage determination: {item.wageDeterminationReference}</small> : null}
      {item.reviewTask ? <small>Task: {item.reviewTask.status} · due {item.reviewTask.dueAt}</small> : null}
      {canManage ? <div className="contract-esrs__actions">
        <button type="button" onClick={() => edit(item)}>Edit</button>
        {item.status !== "Active" ? <button type="button" onClick={() => void changeStatus(item, "Active")}>Activate</button> :
          <button type="button" onClick={() => void changeStatus(item, "Inactive")}>Deactivate</button>}
      </div> : null}
    </article>)}
    {!canManage ? <p>You have read-only access to labor applicability.</p> : <form onSubmit={save}>
      <h3>{editingId ? "Edit labor applicability" : "Record labor applicability"}</h3>
      <label className="checkbox-row"><input type="checkbox" checked={form.scaApplicable} onChange={e => set("scaApplicable", e.target.checked)} /><span>SCA applies</span></label>
      <label className="checkbox-row"><input type="checkbox" checked={form.dbaApplicable} onChange={e => set("dbaApplicable", e.target.checked)} /><span>DBA applies</span></label>
      <label>Other FAR Part 22 obligations<textarea maxLength={2000} value={form.otherFarPart22Obligations ?? ""} onChange={e => set("otherFarPart22Obligations", e.target.value || null)} /></label>
      <label>Place of performance<input required maxLength={240} value={form.placeOfPerformance} onChange={e => set("placeOfPerformance", e.target.value)} /></label>
      <label>Contract period start<input required type="date" value={form.contractPeriodStart} onChange={e => set("contractPeriodStart", e.target.value)} /></label>
      <label>Contract period end<input required type="date" value={form.contractPeriodEnd} onChange={e => set("contractPeriodEnd", e.target.value)} /></label>
      <label>Wage determination reference<input maxLength={240} value={form.wageDeterminationReference ?? ""} onChange={e => set("wageDeterminationReference", e.target.value || null)} /></label>
      <label>Wage determination evidence<select value={form.wageDeterminationEvidenceItemId ?? ""} onChange={e => set("wageDeterminationEvidenceItemId", e.target.value || null)}>
        <option value="">No linked evidence</option>{evidence.filter(item => item.contractIds.includes(contractId)).map(item => <option key={item.id} value={item.id}>{item.title}</option>)}
      </select></label>
      <label>Attached source clause<select value={form.sourceContractClauseId ?? ""} onChange={e => {
        const id = e.target.value || null; set("sourceContractClauseId", id);
        const clause = clauses.find(item => item.id === id); if (clause) set("sourceClause", clause.clauseNumber);
      }}><option value="">No attached clause</option>{clauses.map(clause => <option key={clause.id} value={clause.id}>{clause.clauseNumber} — {clause.title}</option>)}</select></label>
      <label>Source citation<input maxLength={240} value={form.sourceClause ?? ""} onChange={e => set("sourceClause", e.target.value || null)} /></label>
      <label>Documented rationale<textarea maxLength={2000} value={form.rationale ?? ""} onChange={e => set("rationale", e.target.value || null)} /></label>
      <label>Review status<select value={form.reviewStatus} onChange={e => set("reviewStatus", e.target.value as UpsertLaborApplicabilityRequest["reviewStatus"])}>
        <option value="Draft">Draft</option><option value="PendingReview">Pending review</option><option value="Reviewed">Reviewed</option><option value="Rejected">Rejected</option>
      </select></label>
      <label>Review notes<textarea maxLength={2000} value={form.reviewNotes ?? ""} onChange={e => set("reviewNotes", e.target.value || null)} /></label>
      <p>Activation requires SCA, DBA, or another FAR Part 22 obligation plus an attached clause, source citation, or documented rationale.</p>
      <div className="form-actions"><button disabled={state === "saving"} type="submit">{editingId ? "Save labor applicability" : "Record draft"}</button>
        {editingId ? <button type="button" onClick={() => { setEditingId(null); setForm(emptyForm(contractId)); }}>Cancel edit</button> : null}</div>
    </form>}
    <form onSubmit={upload}>
      <h3>Upload wage determination</h3>
      <label>Labor record<select disabled={!canUpload} required value={uploadItemId} onChange={e => setUploadItemId(e.target.value)}>
        <option value="">Select linked record</option>{uploadableItems.map(item => <option key={item.id} value={item.id}>{item.wageDeterminationReference || item.laborStandard}</option>)}
      </select></label>
      <label>Wage determination file<input disabled={!canUpload} required type="file" accept=".csv,.docx,.jpg,.jpeg,.pdf,.png,.txt,.xlsx" onChange={e => setUploadFile(e.target.files?.[0] ?? null)} /></label>
      <label>Upload classification<select disabled={!canUpload} required value={uploadClassification} onChange={e => { setUploadClassification(e.target.value); setUploadReason(`User selected ${e.target.value} wage determination classification.`); }}>
        <option value="">Select classification</option><option value="Unclassified">Unclassified</option><option value="Fci">FCI</option><option value="Cui">CUI</option><option value="Unknown">Unknown</option><option value="Prohibited">Prohibited</option>
      </select></label>
      <label>Classification reason<input disabled={!canUpload} required value={uploadReason} onChange={e => setUploadReason(e.target.value)} /></label>
      <label className="checkbox-row"><input disabled={!canUpload} required type="checkbox" checked={uploadAttested} onChange={e => setUploadAttested(e.target.checked)} /><span>I confirm this file contains no CUI, classified, export-controlled/ITAR, or sensitive government-furnished information.</span></label>
      <button disabled={!canUpload || state === "saving" || !uploadItemId || !uploadFile || !uploadClassification || !uploadAttested} type="submit">Upload wage determination</button>
      {!canUpload ? <p>Upload requires evidence-management permission and the current No-CUI acknowledgement.</p> : null}
    </form>
  </section>;
}

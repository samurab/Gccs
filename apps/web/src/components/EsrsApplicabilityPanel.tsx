import { useEffect, useState, type FormEvent } from "react";
import {
  createEsrsApplicability,
  getContractEsrsApplicabilities,
  getEsrsScheduleTemplates,
  updateEsrsApplicability,
  updateEsrsApplicabilityStatus,
  type EsrsApplicability,
  type EsrsScheduleTemplate,
  type UpsertEsrsApplicabilityRequest
} from "@/lib/api";

const fiscalYear = new Date().getUTCFullYear() + (new Date().getUTCMonth() >= 9 ? 1 : 0);
const emptyForm = (contractId: string): UpsertEsrsApplicabilityRequest => ({
  contractId,
  contractType: "Prime contract",
  agency: "",
  subcontractingPlanType: "Individual",
  primeOrLowerTierRole: "Prime",
  reportType: "Isr",
  periodStart: "",
  periodEnd: "",
  dueDate: "",
  sourceClause: "",
  rationale: "",
  ownerFunction: "Contracts",
  assignedToUserId: null
});

export function EsrsApplicabilityPanel({ contractId, canManage }: { contractId: string; canManage: boolean }) {
  const [items, setItems] = useState<EsrsApplicability[]>([]);
  const [templates, setTemplates] = useState<EsrsScheduleTemplate[]>([]);
  const [form, setForm] = useState(() => emptyForm(contractId));
  const [editingId, setEditingId] = useState<string | null>(null);
  const [state, setState] = useState<"loading" | "ready" | "saving" | "error">("loading");
  const [message, setMessage] = useState("");

  useEffect(() => {
    let active = true;
    Promise.all([getContractEsrsApplicabilities(contractId), getEsrsScheduleTemplates(fiscalYear)])
      .then(([nextItems, nextTemplates]) => {
        if (!active) return;
        setItems(nextItems); setTemplates(nextTemplates); setForm(emptyForm(contractId)); setEditingId(null); setState("ready");
      })
      .catch(() => { if (active) { setMessage("SAM.gov SPR obligations could not be loaded."); setState("error"); } });
    return () => { active = false; };
  }, [contractId]);

  const set = <K extends keyof UpsertEsrsApplicabilityRequest>(key: K, value: UpsertEsrsApplicabilityRequest[K]) =>
    setForm(current => ({ ...current, [key]: value }));

  function applyTemplate(template: EsrsScheduleTemplate) {
    setForm(current => ({ ...current, reportType: template.reportType, periodStart: template.periodStart, periodEnd: template.periodEnd, dueDate: template.dueDate, sourceClause: template.sourceCitation }));
    setMessage(`${template.guidance} Source: ${template.sourceCitation}.`);
  }

  async function save(event: FormEvent) {
    event.preventDefault(); setState("saving"); setMessage("");
    const result = editingId
      ? await updateEsrsApplicability(contractId, editingId, form)
      : await createEsrsApplicability(contractId, form);
    if (!result.data) { setState("error"); setMessage(result.error ?? "SAM.gov SPR applicability could not be saved."); return; }
    setItems(current => editingId ? current.map(item => item.id === result.data!.id ? result.data! : item) : [...current, result.data!]);
    setForm(emptyForm(contractId)); setEditingId(null); setState("ready"); setMessage(editingId ? "SAM.gov SPR applicability updated." : "SAM.gov SPR obligation added to the compliance calendar.");
  }

  async function changeStatus(item: EsrsApplicability, status: EsrsApplicability["status"]) {
    setState("saving");
    const result = await updateEsrsApplicabilityStatus(contractId, item.id, status);
    if (!result.data) { setState("error"); setMessage(result.error ?? "SAM.gov SPR status could not be updated."); return; }
    setItems(current => current.map(existing => existing.id === item.id ? result.data! : existing));
    setState("ready"); setMessage("SAM.gov SPR task status updated.");
  }

  function edit(item: EsrsApplicability) {
    setEditingId(item.id);
    setForm({
      contractId, contractType: item.contractType, agency: item.agency,
      subcontractingPlanType: item.subcontractingPlanType, primeOrLowerTierRole: item.primeOrLowerTierRole,
      reportType: item.reportType, periodStart: item.periodStart, periodEnd: item.periodEnd, dueDate: item.dueDate,
      sourceClause: item.sourceClause, rationale: item.rationale, ownerFunction: item.ownerFunction,
      assignedToUserId: item.assignedToUserId
    });
  }

  return <section className="contract-esrs" aria-labelledby="contract-esrs-heading">
    <div className="contract-documents__header"><div><span>SAM.gov SPR</span><strong id="contract-esrs-heading">{items.length}</strong></div></div>
    <p>Track source-backed ISR and SSR deadlines. FeDril prepares and reminds; it does not submit reports to SAM.gov.</p>
    {state === "loading" ? <p role="status">Loading SAM.gov SPR obligations…</p> : null}
    {state === "error" ? <p role="alert">{message}</p> : message ? <p role="status">{message}</p> : null}
    {items.length === 0 && state !== "loading" ? <p>No SAM.gov SPR obligations recorded for this contract.</p> : null}
    {items.map(item => <article key={item.id} aria-label={`${item.reportType.toUpperCase()} SAM.gov SPR obligation`}>
      <strong>{item.reportType.toUpperCase()} · due {item.dueDate}</strong>
      <span>{item.periodStart} to {item.periodEnd} · {item.status}{item.isOverdue ? " · Overdue" : ""}</span>
      <small>Source: {item.sourceClause || item.rationale} · reviewed {item.reviewedAt.slice(0, 10)}</small>
      {canManage ? <div>
        <button type="button" onClick={() => edit(item)}>Edit</button>
        <select aria-label={`Status for ${item.reportType.toUpperCase()} due ${item.dueDate}`} value={item.status} onChange={event => void changeStatus(item, event.target.value as EsrsApplicability["status"])}>
          <option value="Open">Open</option><option value="InProgress">In progress</option>
          <option value="Completed">Completed</option><option value="Canceled">Canceled</option>
        </select>
      </div> : null}
    </article>)}
    {!canManage ? <p>You have read-only access to SAM.gov SPR applicability.</p> : <form onSubmit={save}>
      <h3>{editingId ? "Edit SAM.gov SPR obligation" : "Add SAM.gov SPR obligation"}</h3>
      <div>
        {templates.map(template => <button type="button" key={template.key} onClick={() => applyTemplate(template)}>Use {template.key.replaceAll("-", " ")}</button>)}
      </div>
      <label>Contract type<input required maxLength={120} value={form.contractType} onChange={e => set("contractType", e.target.value)} /></label>
      <label>Agency<input required maxLength={240} value={form.agency} onChange={e => set("agency", e.target.value)} /></label>
      <label>Plan type<input required maxLength={120} value={form.subcontractingPlanType} onChange={e => set("subcontractingPlanType", e.target.value)} /></label>
      <label>Prime or lower-tier role<select value={form.primeOrLowerTierRole} onChange={e => set("primeOrLowerTierRole", e.target.value)}><option>Prime</option><option>Lower-tier</option></select></label>
      <label>Report type<select value={form.reportType} onChange={e => set("reportType", e.target.value as "Isr" | "Ssr")}><option value="Isr">ISR</option><option value="Ssr">SSR</option></select></label>
      <label>Period start<input required type="date" value={form.periodStart} onChange={e => set("periodStart", e.target.value)} /></label>
      <label>Period end<input required type="date" value={form.periodEnd} onChange={e => set("periodEnd", e.target.value)} /></label>
      <label>SPR due date<input required type="date" value={form.dueDate} onChange={e => set("dueDate", e.target.value)} /></label>
      <label>Source clause<input maxLength={240} value={form.sourceClause ?? ""} onChange={e => set("sourceClause", e.target.value)} /></label>
      <label>Documented rationale<textarea maxLength={2000} value={form.rationale ?? ""} onChange={e => set("rationale", e.target.value)} /></label>
      <p>Provide a source clause or documented rationale. Suggested dates require confirmation against the contract and agency instructions. Do not enter CUI or other prohibited sensitive content.</p>
      <button type="submit" disabled={state === "saving"}>{editingId ? "Update SAM.gov SPR obligation" : "Activate SAM.gov SPR obligation"}</button>
      {editingId ? <button type="button" onClick={() => { setEditingId(null); setForm(emptyForm(contractId)); }}>Cancel edit</button> : null}
    </form>}
  </section>;
}

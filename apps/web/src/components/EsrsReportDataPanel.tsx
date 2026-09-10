import { useEffect, useState, type ChangeEvent, type FormEvent } from "react";
import {
  createContractEsrsReportData, downloadEsrsReportDataTemplate, getContractEsrsReportData,
  getEvidenceItems, getSubcontractors, importEsrsReportDataCsv, reviewContractEsrsReportData,
  updateContractEsrsReportData, type EvidenceMetadata, type SubcontractingReportDataRow,
  type Subcontractor, type UpsertSubcontractingReportDataRowRequest
} from "@/lib/api";

const emptyForm = (contractId: string): UpsertSubcontractingReportDataRowRequest => ({
  contractId, subcontractorId: "", reportType: "Isr", reportPeriodStart: "", reportPeriodEnd: "",
  rowPeriodStart: "", rowPeriodEnd: "", socioeconomicCategory: "", planCategory: "",
  amount: 0, supportingEvidenceItemIds: [], sourceReference: "", expectedVersion: null
});

export function EsrsReportDataPanel({ contractId, canManage }: { contractId: string; canManage: boolean }) {
  const [rows, setRows] = useState<SubcontractingReportDataRow[]>([]);
  const [subcontractors, setSubcontractors] = useState<Subcontractor[]>([]);
  const [evidence, setEvidence] = useState<EvidenceMetadata[]>([]);
  const [form, setForm] = useState(() => emptyForm(contractId));
  const [editingId, setEditingId] = useState<string | null>(null);
  const [state, setState] = useState<"loading" | "ready" | "saving" | "error">("loading");
  const [message, setMessage] = useState("");

  useEffect(() => {
    let active = true;
    Promise.all([getContractEsrsReportData(contractId), getSubcontractors(), getEvidenceItems()])
      .then(([nextRows, nextSubcontractors, nextEvidence]) => {
        if (!active) return;
        setRows(nextRows); setSubcontractors(nextSubcontractors.filter(item => item.contractIds.includes(contractId)));
        setEvidence(nextEvidence.filter(item => item.contractIds.includes(contractId)));
        setForm(emptyForm(contractId)); setEditingId(null); setState("ready");
      })
      .catch(() => { if (active) { setState("error"); setMessage("Subcontracting report data could not be loaded."); } });
    return () => { active = false; };
  }, [contractId]);

  const set = <K extends keyof UpsertSubcontractingReportDataRowRequest>(key: K, value: UpsertSubcontractingReportDataRowRequest[K]) =>
    setForm(current => ({ ...current, [key]: value }));

  async function save(event: FormEvent) {
    event.preventDefault(); setState("saving"); setMessage("");
    const result = editingId ? await updateContractEsrsReportData(contractId, editingId, form)
      : await createContractEsrsReportData(contractId, form);
    if (!result.data) { setState("error"); setMessage(result.error ?? "The report data row could not be saved."); return; }
    setRows(current => editingId ? current.map(row => row.id === result.data!.id ? result.data! : row) : [result.data!, ...current]);
    setForm(emptyForm(contractId)); setEditingId(null); setState("ready");
    setMessage(editingId ? "Report data updated and returned to review." : "Report data row created as a draft.");
  }

  function edit(row: SubcontractingReportDataRow) {
    setEditingId(row.id);
    setForm({ contractId, subcontractorId: row.subcontractorId, reportType: row.reportType,
      reportPeriodStart: row.reportPeriodStart, reportPeriodEnd: row.reportPeriodEnd,
      rowPeriodStart: row.rowPeriodStart, rowPeriodEnd: row.rowPeriodEnd,
      socioeconomicCategory: row.socioeconomicCategory, planCategory: row.planCategory,
      amount: row.amount, supportingEvidenceItemIds: row.supportingEvidenceItemIds,
      sourceReference: row.sourceReference, expectedVersion: row.version });
  }

  async function review(row: SubcontractingReportDataRow, status: "Reviewed" | "Accepted" | "Rejected") {
    const reviewerNotes = status === "Rejected" ? window.prompt("Reason for rejection") : null;
    if (status === "Rejected" && !reviewerNotes?.trim()) return;
    setState("saving");
    const result = await reviewContractEsrsReportData(contractId, row.id, status, reviewerNotes, row.version);
    if (!result.data) { setState("error"); setMessage(result.error ?? "The review decision could not be saved."); return; }
    setRows(current => current.map(item => item.id === row.id ? result.data! : item)); setState("ready");
    setMessage(`Report data row marked ${status.toLowerCase()}.`);
  }

  async function importCsv(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0]; if (!file) return;
    setState("saving"); const result = await importEsrsReportDataCsv(await file.text()); event.target.value = "";
    if (!result.data) { setState("error"); setMessage(result.error ?? "The CSV import was rejected."); return; }
    setRows(current => [...result.data!.filter(row => row.contractId === contractId), ...current]); setState("ready");
    setMessage(`${result.data.length} report data row${result.data.length === 1 ? "" : "s"} imported as draft.`);
  }

  async function downloadTemplate() {
    const result = await downloadEsrsReportDataTemplate();
    if (!result.data) { setState("error"); setMessage(result.error ?? "The template could not be downloaded."); return; }
    const url = URL.createObjectURL(result.data.blob); const anchor = document.createElement("a");
    anchor.href = url; anchor.download = result.data.fileName; anchor.click(); URL.revokeObjectURL(url);
  }

  return <section className="contract-esrs contract-esrs-data" aria-labelledby="contract-esrs-data-heading">
    <div className="contract-documents__header"><div><span>Subcontracting report data</span><strong id="contract-esrs-data-heading">{rows.length}</strong></div></div>
    <p>Collect documented ISR/SSR spend inputs. Only reviewed or explicitly accepted rows are eligible for a final preparation package.</p>
    <p>Do not enter or upload CUI, classified, export-controlled, ITAR, or sensitive government-furnished information.</p>
    {state === "loading" ? <p role="status">Loading subcontracting report data…</p> : null}
    {state === "error" ? <p role="alert">{message}</p> : message ? <p role="status">{message}</p> : null}
    {rows.length === 0 && state !== "loading" ? <p>No report data rows recorded for this contract.</p> : null}
    {rows.map(row => <article key={row.id} aria-label={`${row.socioeconomicCategory} report data`}>
      <strong>{row.socioeconomicCategory} · {row.amount.toLocaleString(undefined, { style: "currency", currency: "USD" })}</strong>
      <span>{row.reportType.toUpperCase()} · {row.rowPeriodStart} to {row.rowPeriodEnd} · {row.reviewStatus}</span>
      <small>Plan: {row.planCategory} · Source: {row.sourceReference} · Evidence: {row.supportingEvidenceItemIds.length}</small>
      {!row.isPackageEligible ? <small>Blocked from final package until reviewed or accepted.</small> : null}
      {canManage ? <div><button type="button" onClick={() => edit(row)}>Edit</button>
        <button type="button" onClick={() => void review(row, "Reviewed")}>Mark reviewed</button>
        <button type="button" onClick={() => void review(row, "Accepted")}>Accept</button>
        <button type="button" onClick={() => void review(row, "Rejected")}>Reject</button></div> : null}
    </article>)}
    {!canManage ? <p>You have read-only access to subcontracting report data.</p> : <>
      <div><button type="button" onClick={() => void downloadTemplate()}>Download CSV template</button>
        <label>Import completed CSV<input aria-label="Import completed CSV" type="file" accept=".csv,text/csv" onChange={event => void importCsv(event)} /></label></div>
      <form onSubmit={event => void save(event)}>
        <h3>{editingId ? "Edit report data" : "Add report data"}</h3>
        <label>Subcontractor<select required value={form.subcontractorId} onChange={event => set("subcontractorId", event.target.value)}>
          <option value="">Select subcontractor</option>{subcontractors.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>
        <label>Report type<select value={form.reportType} onChange={event => set("reportType", event.target.value as "Isr" | "Ssr")}><option value="Isr">ISR</option><option value="Ssr">SSR</option></select></label>
        <label>Report period start<input required type="date" value={form.reportPeriodStart} onChange={event => set("reportPeriodStart", event.target.value)} /></label>
        <label>Report period end<input required type="date" value={form.reportPeriodEnd} onChange={event => set("reportPeriodEnd", event.target.value)} /></label>
        <label>Row period start<input required type="date" value={form.rowPeriodStart} onChange={event => set("rowPeriodStart", event.target.value)} /></label>
        <label>Row period end<input required type="date" value={form.rowPeriodEnd} onChange={event => set("rowPeriodEnd", event.target.value)} /></label>
        <label>Socioeconomic category<input required maxLength={120} value={form.socioeconomicCategory} onChange={event => set("socioeconomicCategory", event.target.value)} /></label>
        <label>Plan category<input required maxLength={120} value={form.planCategory} onChange={event => set("planCategory", event.target.value)} /></label>
        <label>Amount<input required min="0" max="999999999999.99" step="0.01" type="number" value={form.amount} onChange={event => set("amount", Number(event.target.value))} /></label>
        <label>Supporting evidence<select multiple value={form.supportingEvidenceItemIds} onChange={event => set("supportingEvidenceItemIds", Array.from(event.target.selectedOptions, option => option.value))}>
          {evidence.map(item => <option key={item.id} value={item.id}>{item.title}</option>)}</select></label>
        <label>Source reference<input required maxLength={500} value={form.sourceReference ?? ""} onChange={event => set("sourceReference", event.target.value)} /></label>
        <button type="submit" disabled={state === "saving"}>{editingId ? "Update report data" : "Create report data row"}</button>
        {editingId ? <button type="button" onClick={() => { setEditingId(null); setForm(emptyForm(contractId)); }}>Cancel edit</button> : null}
      </form>
    </>}
  </section>;
}

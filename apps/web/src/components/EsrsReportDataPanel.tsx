import { useEffect, useState, type ChangeEvent, type FormEvent } from "react";
import {
  createContractSubcontractingPlanReportData, downloadSubcontractingPlanReportDataTemplate, getContractSubcontractingPlanReportData,
  getEvidenceItems, getSubcontractors, importSubcontractingPlanReportDataCsv, reviewContractSubcontractingPlanReportData,
  updateContractSubcontractingPlanReportData, type EvidenceMetadata, type SubcontractingReportDataRow,
  type Subcontractor, type UpsertSubcontractingReportDataRowRequest
} from "@/lib/api";

const sprCategories = ["Small Business Concerns (SB)", "Other Than Small Business Concerns (OTSB)",
  "Small Disadvantaged Business (SDB)", "Women-Owned Small Business (WOSB)", "HBCU/MSI",
  "HUBZone Small Business", "Veteran-Owned Small Business (VOSB)",
  "Service-Disabled Veteran-Owned Small Business (SDVOSB)", "ANC/Indian Tribe"];

const currentFiscalYear = () => new Date().getMonth() >= 9 ? new Date().getFullYear() + 1 : new Date().getFullYear();
const emptyForm = (contractId: string, contractNumber: string, companyUei: string | null): UpsertSubcontractingReportDataRowRequest => ({
  contractId, subcontractorId: "", reportType: "Isr", reportPeriodStart: "", reportPeriodEnd: "",
  rowPeriodStart: "", rowPeriodEnd: "", socioeconomicCategory: "", planCategory: "",
  amount: 0, supportingEvidenceItemIds: [], sourceReference: "", expectedVersion: null,
  reportingRole: "PrimeContractor", reportingFiscalYear: currentFiscalYear(), reportingPeriod: "March31",
  reportingEntityUei: companyUei ?? "", primeContractPiid: contractNumber, subcontractNumber: null,
  sprEligibilityConfirmed: false, sprEligibilityBasis: ""
});

export function EsrsReportDataPanel({ contractId, contractNumber, companyUei, canManage }:
  { contractId: string; contractNumber: string; companyUei: string | null; canManage: boolean }) {
  const [rows, setRows] = useState<SubcontractingReportDataRow[]>([]);
  const [subcontractors, setSubcontractors] = useState<Subcontractor[]>([]);
  const [evidence, setEvidence] = useState<EvidenceMetadata[]>([]);
  const [form, setForm] = useState(() => emptyForm(contractId, contractNumber, companyUei));
  const [editingId, setEditingId] = useState<string | null>(null);
  const [state, setState] = useState<"loading" | "ready" | "saving" | "error">("loading");
  const [message, setMessage] = useState("");

  useEffect(() => {
    let active = true;
    Promise.all([getContractSubcontractingPlanReportData(contractId), getSubcontractors(), getEvidenceItems()])
      .then(([nextRows, nextSubcontractors, nextEvidence]) => {
        if (!active) return;
        setRows(nextRows); setSubcontractors(nextSubcontractors.filter(item => item.contractIds.includes(contractId)));
        setEvidence(nextEvidence.filter(item => item.contractIds.includes(contractId)));
        setForm(emptyForm(contractId, contractNumber, companyUei)); setEditingId(null); setState("ready");
      })
      .catch(() => { if (active) { setState("error"); setMessage("Subcontracting report data could not be loaded."); } });
    return () => { active = false; };
  }, [contractId, contractNumber, companyUei]);

  const set = <K extends keyof UpsertSubcontractingReportDataRowRequest>(key: K, value: UpsertSubcontractingReportDataRowRequest[K]) =>
    setForm(current => ({ ...current, [key]: value }));

  async function save(event: FormEvent) {
    event.preventDefault(); setState("saving"); setMessage("");
    const result = editingId ? await updateContractSubcontractingPlanReportData(contractId, editingId, form)
      : await createContractSubcontractingPlanReportData(contractId, form);
    if (!result.data) { setState("error"); setMessage(result.error ?? "The report data row could not be saved."); return; }
    setRows(current => editingId ? current.map(row => row.id === result.data!.id ? result.data! : row) : [result.data!, ...current]);
    setForm(emptyForm(contractId, contractNumber, companyUei)); setEditingId(null); setState("ready");
    setMessage(editingId ? "Report data updated and returned to review." : "Report data row created as a draft.");
  }

  function edit(row: SubcontractingReportDataRow) {
    setEditingId(row.id);
    setForm({ contractId, subcontractorId: row.subcontractorId, reportType: row.reportType,
      reportPeriodStart: row.reportPeriodStart, reportPeriodEnd: row.reportPeriodEnd,
      rowPeriodStart: row.rowPeriodStart, rowPeriodEnd: row.rowPeriodEnd,
      socioeconomicCategory: row.socioeconomicCategory, planCategory: row.planCategory,
      amount: row.amount, supportingEvidenceItemIds: row.supportingEvidenceItemIds,
      sourceReference: row.sourceReference, expectedVersion: row.version,
      reportingRole: row.reportingRole, reportingFiscalYear: row.reportingFiscalYear,
      reportingPeriod: row.reportingPeriod, reportingEntityUei: row.reportingEntityUei,
      primeContractPiid: row.primeContractPiid, subcontractNumber: row.subcontractNumber,
      sprEligibilityConfirmed: row.sprEligibilityConfirmed, sprEligibilityBasis: row.sprEligibilityBasis });
  }

  async function review(row: SubcontractingReportDataRow, status: "Reviewed" | "Accepted" | "Rejected") {
    const reviewerNotes = status === "Rejected" ? window.prompt("Reason for rejection") : null;
    if (status === "Rejected" && !reviewerNotes?.trim()) return;
    setState("saving");
    const result = await reviewContractSubcontractingPlanReportData(contractId, row.id, status, reviewerNotes, row.version);
    if (!result.data) { setState("error"); setMessage(result.error ?? "The review decision could not be saved."); return; }
    setRows(current => current.map(item => item.id === row.id ? result.data! : item)); setState("ready");
    setMessage(`Report data row marked ${status.toLowerCase()}.`);
  }

  async function importCsv(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0]; if (!file) return;
    setState("saving"); const result = await importSubcontractingPlanReportDataCsv(await file.text()); event.target.value = "";
    if (!result.data) { setState("error"); setMessage(result.error ?? "The CSV import was rejected."); return; }
    setRows(current => [...result.data!.filter(row => row.contractId === contractId), ...current]); setState("ready");
    setMessage(`${result.data.length} report data row${result.data.length === 1 ? "" : "s"} imported as draft.`);
  }

  async function downloadTemplate() {
    const result = await downloadSubcontractingPlanReportDataTemplate();
    if (!result.data) { setState("error"); setMessage(result.error ?? "The template could not be downloaded."); return; }
    const url = URL.createObjectURL(result.data.blob); const anchor = document.createElement("a");
    anchor.href = url; anchor.download = result.data.fileName; anchor.click(); URL.revokeObjectURL(url);
  }

  return <section className="contract-esrs contract-esrs-data" aria-labelledby="contract-esrs-data-heading">
    <div className="contract-documents__header"><div><span>SAM.gov subcontracting plan reporting</span><strong id="contract-esrs-data-heading">{rows.length}</strong></div></div>
    <p>Prepare documented ISR/SSR inputs for manual entry in SAM.gov Subcontracting Plan Reporting (SPR). FeDril does not submit or synchronize reports with SAM.gov.</p>
    <p>Do not enter or upload CUI, classified, export-controlled, ITAR, or sensitive government-furnished information.</p>
    {state === "loading" ? <p role="status">Loading subcontracting report data…</p> : null}
    {state === "error" ? <p role="alert">{message}</p> : message ? <p role="status">{message}</p> : null}
    {rows.length === 0 && state !== "loading" ? <p>No report data rows recorded for this contract.</p> : null}
    {rows.map(row => <article key={row.id} aria-label={`${row.socioeconomicCategory} report data`}>
      <strong>{row.socioeconomicCategory} · {row.amount.toLocaleString(undefined, { style: "currency", currency: "USD" })}</strong>
      <span>{row.reportType.toUpperCase()} · {row.rowPeriodStart} to {row.rowPeriodEnd} · {row.reviewStatus}</span>
      <small>Plan: {row.planCategory} · Source: {row.sourceReference} · Evidence: {row.supportingEvidenceItemIds.length} · SPR: {row.sprReadinessStatus}</small>
      {!row.isPackageEligible ? <small>Blocked from final package until SPR metadata is ready and the row is reviewed or accepted.</small> : null}
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
        <label>Subcontractor<select required value={form.subcontractorId} onChange={event => {
          const subcontractorId = event.target.value;
          const selectedSubcontractor = subcontractors.find(item => item.id === subcontractorId);
          setForm(current => ({ ...current, subcontractorId,
            reportingEntityUei: current.reportingRole === "Subcontractor" ? selectedSubcontractor?.uei ?? "" : current.reportingEntityUei }));
        }}>
          <option value="">Select subcontractor</option>{subcontractors.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>
        <label>Report type<select value={form.reportType} onChange={event => set("reportType", event.target.value as "Isr" | "Ssr")}><option value="Isr">ISR</option><option value="Ssr">SSR</option></select></label>
        <label>Reporting role<select value={form.reportingRole ?? ""} onChange={event => {
          const role = event.target.value as "PrimeContractor" | "Subcontractor";
          const selectedSubcontractor = subcontractors.find(item => item.id === form.subcontractorId);
          setForm(current => ({ ...current, reportingRole: role,
            reportingEntityUei: role === "PrimeContractor" ? companyUei ?? "" : selectedSubcontractor?.uei ?? "",
            subcontractNumber: role === "PrimeContractor" ? null : current.subcontractNumber }));
        }}><option value="PrimeContractor">Prime contractor</option><option value="Subcontractor">Subcontractor</option></select></label>
        <label>Reporting fiscal year<input required type="number" min={currentFiscalYear() - 9} max={currentFiscalYear()} value={form.reportingFiscalYear ?? ""} onChange={event => set("reportingFiscalYear", Number(event.target.value))} /></label>
        <label>SPR reporting period<select value={form.reportingPeriod ?? ""} onChange={event => set("reportingPeriod", event.target.value as "March31" | "September30" | "Final")}><option value="March31">March 31</option><option value="September30">September 30</option><option value="Final">Final</option></select></label>
        <label>Reporting entity UEI<input required minLength={12} maxLength={12} value={form.reportingEntityUei ?? ""} onChange={event => set("reportingEntityUei", event.target.value.toUpperCase())} /></label>
        <label>Prime contract PIID<input required maxLength={64} value={form.primeContractPiid ?? ""} onChange={event => set("primeContractPiid", event.target.value)} /></label>
        {form.reportingRole === "Subcontractor" ? <label>Subcontract number<input required maxLength={64} value={form.subcontractNumber ?? ""} onChange={event => set("subcontractNumber", event.target.value)} /></label> : null}
        <label>Report period start<input required type="date" value={form.reportPeriodStart} onChange={event => set("reportPeriodStart", event.target.value)} /></label>
        <label>Report period end<input required type="date" value={form.reportPeriodEnd} onChange={event => set("reportPeriodEnd", event.target.value)} /></label>
        <label>Row period start<input required type="date" value={form.rowPeriodStart} onChange={event => set("rowPeriodStart", event.target.value)} /></label>
        <label>Row period end<input required type="date" value={form.rowPeriodEnd} onChange={event => set("rowPeriodEnd", event.target.value)} /></label>
        <label>Socioeconomic category<select required value={form.socioeconomicCategory} onChange={event => set("socioeconomicCategory", event.target.value)}><option value="">Select category</option>{sprCategories.map(category => <option key={category} value={category}>{category}</option>)}</select></label>
        <label>Plan category<input required maxLength={120} value={form.planCategory} onChange={event => set("planCategory", event.target.value)} /></label>
        <label>Amount (whole dollars)<input required min="0" max="999999999999" step="1" type="number" value={form.amount} onChange={event => set("amount", Number(event.target.value))} /></label>
        <label>Supporting evidence<select multiple value={form.supportingEvidenceItemIds} onChange={event => set("supportingEvidenceItemIds", Array.from(event.target.selectedOptions, option => option.value))}>
          {evidence.map(item => <option key={item.id} value={item.id}>{item.title}</option>)}</select></label>
        <label>Source reference<input required maxLength={500} value={form.sourceReference ?? ""} onChange={event => set("sourceReference", event.target.value)} /></label>
        <label>SPR eligibility basis<textarea required maxLength={500} value={form.sprEligibilityBasis ?? ""} onChange={event => set("sprEligibilityBasis", event.target.value)} /></label>
        <label><input required type="checkbox" checked={form.sprEligibilityConfirmed} onChange={event => set("sprEligibilityConfirmed", event.target.checked)} /> I confirmed the external SAM.gov SPR eligibility basis for this report.</label>
        <button type="submit" disabled={state === "saving"}>{editingId ? "Update report data" : "Create report data row"}</button>
        {editingId ? <button type="button" onClick={() => { setEditingId(null); setForm(emptyForm(contractId, contractNumber, companyUei)); }}>Cancel edit</button> : null}
      </form>
    </>}
  </section>;
}

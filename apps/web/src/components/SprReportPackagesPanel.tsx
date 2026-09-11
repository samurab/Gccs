import { useEffect, useState, type FormEvent } from "react";
import {
  createSprManualSubmissionReceipt, createSprReportPackage, downloadSprReportPackage, getEvidenceItems,
  getSprManualSubmissionReceipts, getSprReportPackages, getSprSubmissionCapability, reviewSprReportPackage,
  type EvidenceMetadata, type SprManualSubmissionReceipt, type SprReportPackage, type SprSubmissionCapability
} from "@/lib/api";

export function SprReportPackagesPanel({ contractId, canManage, canExport }:
  { contractId: string; canManage: boolean; canExport: boolean }) {
  const [packages, setPackages] = useState<SprReportPackage[]>([]);
  const [receipts, setReceipts] = useState<Record<string, SprManualSubmissionReceipt[]>>({});
  const [evidence, setEvidence] = useState<EvidenceMetadata[]>([]);
  const [capability, setCapability] = useState<SprSubmissionCapability | null>(null);
  const [reportType, setReportType] = useState<"Isr" | "Ssr">("Isr");
  const [periodStart, setPeriodStart] = useState("");
  const [periodEnd, setPeriodEnd] = useState("");
  const [reviewerName, setReviewerName] = useState("");
  const [receiptPackageId, setReceiptPackageId] = useState("");
  const [confirmationReference, setConfirmationReference] = useState("");
  const [outcome, setOutcome] = useState<SprManualSubmissionReceipt["outcome"]>("Submitted");
  const [receiptNotes, setReceiptNotes] = useState("");
  const [evidenceItemId, setEvidenceItemId] = useState("");
  const [supersedesReceiptId, setSupersedesReceiptId] = useState("");
  const [state, setState] = useState<"loading" | "ready" | "saving" | "error">("loading");
  const [message, setMessage] = useState("");

  useEffect(() => {
    let active = true;
    Promise.all([getSprReportPackages(), getSprSubmissionCapability(), getEvidenceItems()])
      .then(async ([allPackages, nextCapability, allEvidence]) => {
        const nextPackages = allPackages.filter(item => item.contractId === contractId);
        const receiptEntries = await Promise.all(nextPackages.map(async item =>
          [item.id, await getSprManualSubmissionReceipts(item.id)] as const));
        if (!active) return;
        setPackages(nextPackages); setCapability(nextCapability);
        setEvidence(allEvidence.filter(item => item.contractIds.includes(contractId)));
        setReceipts(Object.fromEntries(receiptEntries));
        setReceiptPackageId(nextPackages.find(item => item.status === "Approved")?.id ?? "");
        setState("ready");
      })
      .catch(() => { if (active) { setState("error"); setMessage("SPR preparation packages could not be loaded."); } });
    return () => { active = false; };
  }, [contractId]);

  async function generate(event: FormEvent) {
    event.preventDefault(); setState("saving"); setMessage("");
    const result = await createSprReportPackage(contractId, reportType, periodStart, periodEnd);
    if (!result.data) { setState("error"); setMessage(result.error ?? "The package could not be generated."); return; }
    setPackages(current => [result.data!, ...current]); setReceipts(current => ({ ...current, [result.data!.id]: [] }));
    setState("ready"); setMessage("Immutable SPR preparation snapshot generated as a draft.");
  }

  async function review(item: SprReportPackage, action: "begin-review" | "approve" | "supersede" | "archive") {
    if (!reviewerName.trim()) { setState("error"); setMessage("Enter the reviewer display name first."); return; }
    const notes = action === "approve" ? "Approved for customer review and manual SAM.gov entry."
      : action === "begin-review" ? "Internal package review started."
      : window.prompt(`Reason to ${action} this package`);
    if (action !== "approve" && action !== "begin-review" && !notes?.trim()) return;
    setState("saving");
    const result = await reviewSprReportPackage(item.id, action, reviewerName, notes ?? null);
    if (!result.data) { setState("error"); setMessage(result.error ?? "The package status could not be changed."); return; }
    setPackages(current => current.map(candidate => candidate.id === item.id ? result.data! : candidate));
    if (action === "approve") setReceiptPackageId(item.id);
    setState("ready"); setMessage(`Package ${action} action recorded.`);
  }

  async function recordReceipt(event: FormEvent) {
    event.preventDefault(); setState("saving"); setMessage("");
    const result = await createSprManualSubmissionReceipt(receiptPackageId, {
      submittedAt: new Date().toISOString(), confirmationReference, outcome,
      notes: receiptNotes.trim() || null, evidenceItemId: evidenceItemId || null,
      supersedesReceiptId: outcome === "Corrected" ? supersedesReceiptId : null
    });
    if (!result.data) { setState("error"); setMessage(result.error ?? "The external receipt could not be recorded."); return; }
    setReceipts(current => ({ ...current, [receiptPackageId]: [result.data!, ...(current[receiptPackageId] ?? [])] }));
    setConfirmationReference(""); setReceiptNotes(""); setEvidenceItemId(""); setSupersedesReceiptId(""); setState("ready");
    setMessage("User-reported external SAM.gov receipt recorded. FeDril did not verify or perform the submission.");
  }

  async function download(item: SprReportPackage, format: "Html" | "Json") {
    const result = await downloadSprReportPackage(item.id, format);
    if (!result.data) { setState("error"); setMessage(result.error ?? "The package export failed."); return; }
    const url = URL.createObjectURL(result.data.blob); const anchor = document.createElement("a");
    anchor.href = url; anchor.download = result.data.fileName; anchor.click();
    window.setTimeout(() => URL.revokeObjectURL(url), 0);
  }

  const approvedPackages = packages.filter(item => item.status === "Approved");
  return <section className="contract-esrs contract-esrs-packages workflow-control-surface" aria-labelledby="spr-package-heading">
    <div className="contract-documents__header"><div><span>SPR preparation packages</span><strong id="spr-package-heading">{packages.length}</strong></div></div>
    <p>Packages are immutable, versioned snapshots for customer review and manual entry in SAM.gov. FeDril does not submit or synchronize them.</p>
    {capability ? <p role="status">Direct submission: {capability.enabled ? "configured" : `unavailable — ${capability.reason}`}</p> : null}
    {state === "loading" ? <p role="status">Loading SPR preparation packages…</p> : null}
    {state === "error" ? <p role="alert">{message}</p> : message ? <p role="status">{message}</p> : null}
    {packages.length === 0 && state !== "loading" ? <p>No preparation packages have been generated for this contract.</p> : null}
    {packages.map(item => <article key={item.id} aria-label={`SPR package version ${item.version}`}>
      <strong>{item.reportType.toUpperCase()} · version {item.version} · {item.status}</strong>
      <span>{item.periodStart} to {item.periodEnd} · {item.snapshot.rowCount} rows · {item.snapshot.totalSpend.toLocaleString(undefined, { style: "currency", currency: "USD" })}</span>
      <small>Generated {new Date(item.generatedAt).toLocaleString()}</small>
      <small>Schema: {item.snapshot.schemaProfiles.map(profile => profile.version).join(", ") || "none"} · Evidence links: {item.snapshot.evidenceReferences.length}</small>
      <small>{item.notSubmittedDisclaimer}</small>
      {item.reviewerName ? <small>Reviewer: {item.reviewerName}</small> : null}
      {item.approvedAt ? <small>Approved: {new Date(item.approvedAt).toLocaleString()}</small> : null}
      {item.reviewNotes ? <small>Review notes: {item.reviewNotes}</small> : null}
      {item.snapshot.exceptions.map(exception => <small key={exception}>Exception: {exception}</small>)}
      {(receipts[item.id] ?? []).map(receipt => <small key={receipt.id}>External receipt: {receipt.confirmationReference} · {receipt.outcome} · recorded {new Date(receipt.recordedAt).toLocaleString()}</small>)}
      <div className="contract-esrs__actions">
        {canExport ? <><button type="button" onClick={() => void download(item, "Html")}>Export HTML</button>
          <button type="button" onClick={() => void download(item, "Json")}>Export JSON</button></> : null}
        {canManage && item.status === "Draft" ? <button type="button" onClick={() => void review(item, "begin-review")}>Begin review</button> : null}
        {canManage && item.status === "InReview" ? <button type="button" onClick={() => void review(item, "approve")}>Approve package</button> : null}
        {canManage && item.status === "Approved" ? <button type="button" onClick={() => void review(item, "supersede")}>Supersede package</button> : null}
        {canManage && item.status !== "Archived" ? <button type="button" onClick={() => void review(item, "archive")}>Archive package</button> : null}
      </div>
    </article>)}
    {!canManage ? <p>You have read-only access to SPR preparation packages.</p> : state !== "error" ? <>
      <label>Reviewer display name<input maxLength={200} value={reviewerName} onChange={event => setReviewerName(event.target.value)} /></label>
      <form onSubmit={event => void generate(event)}><h3>Generate preparation snapshot</h3>
        <label>Report type<select value={reportType} onChange={event => setReportType(event.target.value as "Isr" | "Ssr")}><option value="Isr">ISR</option><option value="Ssr">SSR</option></select></label>
        <label>Period start<input required type="date" value={periodStart} onChange={event => setPeriodStart(event.target.value)} /></label>
        <label>Period end<input required type="date" value={periodEnd} onChange={event => setPeriodEnd(event.target.value)} /></label>
        <div className="form-actions"><button type="submit" disabled={state === "saving"}>Generate package</button></div>
      </form>
      {approvedPackages.length > 0 ? <form onSubmit={event => void recordReceipt(event)}><h3>Record manual SAM.gov receipt</h3>
        <p>This records a customer-reported external event; it does not verify a SAM.gov submission.</p>
        <label>Approved package<select required value={receiptPackageId} onChange={event => { setReceiptPackageId(event.target.value); setSupersedesReceiptId(""); }}>{approvedPackages.map(item => <option key={item.id} value={item.id}>Version {item.version} · {item.reportType.toUpperCase()}</option>)}</select></label>
        <label>Confirmation reference<input required maxLength={200} value={confirmationReference} onChange={event => setConfirmationReference(event.target.value)} /></label>
        <label>Outcome<select value={outcome} onChange={event => { setOutcome(event.target.value as SprManualSubmissionReceipt["outcome"]); setSupersedesReceiptId(""); }}><option>Submitted</option><option>Accepted</option><option>Rejected</option><option>Corrected</option></select></label>
        {outcome === "Corrected" ? <label>Receipt being corrected<select required value={supersedesReceiptId} onChange={event => setSupersedesReceiptId(event.target.value)}>
          <option value="">Select a prior receipt</option>{(receipts[receiptPackageId] ?? []).map(receipt =>
            <option key={receipt.id} value={receipt.id}>{receipt.confirmationReference} · {receipt.outcome} · {new Date(receipt.recordedAt).toLocaleString()}</option>)}</select></label> : null}
        <label>Supporting evidence<select value={evidenceItemId} onChange={event => setEvidenceItemId(event.target.value)}><option value="">None</option>{evidence.map(item => <option key={item.id} value={item.id}>{item.title}</option>)}</select></label>
        <label>Notes<textarea required={outcome === "Rejected" || outcome === "Corrected"} maxLength={2000} value={receiptNotes} onChange={event => setReceiptNotes(event.target.value)} /></label>
        <div className="form-actions"><button type="submit" disabled={state === "saving"}>Record external receipt</button></div>
      </form> : null}
    </> : null}
  </section>;
}

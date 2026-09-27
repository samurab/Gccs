import { type FormEvent, useEffect, useState } from "react";
import {
  exportAiOutputs,
  getAiOutputReviewHistory,
  getAiOutputs,
  linkAiOutputToDeliverable,
  reviewAiOutput,
  type AiDeliverableType,
  type AiOutputReviewHistory,
  type GuardedAssistantAnswer
} from "@/lib/api";

export function AiOutputGovernancePanel({ permissions }: { permissions: string[] }) {
  const [outputs, setOutputs] = useState<GuardedAssistantAnswer[]>([]);
  const [history, setHistory] = useState<Record<string, AiOutputReviewHistory[]>>({});
  const [state, setState] = useState<"loading" | "ready" | "error">("loading");
  const [message, setMessage] = useState("");
  const [includeArchived, setIncludeArchived] = useState(false);
  const [decisions, setDecisions] = useState<Record<string, string>>({});
  const [notes, setNotes] = useState<Record<string, string>>({});
  const [reasons, setReasons] = useState<Record<string, string>>({});
  const [deliverableTypes, setDeliverableTypes] = useState<Record<string, AiDeliverableType>>({});
  const [deliverableIds, setDeliverableIds] = useState<Record<string, string>>({});
  const canReview = permissions.includes("ManageObligations");
  const canExport = permissions.includes("ExportReports");
  const allowedDeliverableTypes: AiDeliverableType[] = [
    ...(permissions.includes("ManageReports") ? ["Report", "CustomerDeliverable"] as AiDeliverableType[] : []),
    ...(permissions.includes("ManageObligations") ? ["Policy"] as AiDeliverableType[] : []),
    ...(permissions.includes("ManageCmmc") ? ["Ssp", "Poam"] as AiDeliverableType[] : [])
  ];

  useEffect(() => {
    let active = true;
    getAiOutputs(includeArchived).then(items => {
      if (!active) return;
      setOutputs(items); setState("ready"); setMessage("");
    }).catch(error => {
      if (!active) return;
      setState("error");
      setMessage(error instanceof Error ? error.message : "AI output logs could not be loaded.");
    });
    return () => { active = false; };
  }, [includeArchived]);

  async function loadHistory(answerId: string) {
    try {
      const items = await getAiOutputReviewHistory(answerId);
      setHistory(current => ({ ...current, [answerId]: items }));
    } catch (error) {
      setMessage(error instanceof Error ? error.message : "AI output review history could not be loaded.");
    }
  }

  async function review(event: FormEvent, output: GuardedAssistantAnswer) {
    event.preventDefault();
    const decision = decisions[output.id] ?? "";
    const result = await reviewAiOutput(output.id, decision, notes[output.id] ?? "",
      decision === "Rejected" ? reasons[output.id] ?? "" : null, output.version);
    if (!result.data) { setMessage(result.error ?? "The AI output decision could not be saved."); return; }
    setOutputs(current => current.map(item => item.id === output.id ? result.data!.answer : item));
    setMessage(`AI output ${output.id} is now ${result.data.answer.reviewState}.`);
    await loadHistory(output.id);
  }

  async function link(event: FormEvent, output: GuardedAssistantAnswer) {
    event.preventDefault();
    const type = deliverableTypes[output.id] ?? allowedDeliverableTypes[0];
    if (!type) { setMessage("Your role cannot link AI output to a governed deliverable."); return; }
    const result = await linkAiOutputToDeliverable(output.id, type, deliverableIds[output.id] ?? "");
    setMessage(result.data
      ? `Approved AI output linked to ${result.data.deliverableType} ${result.data.deliverableId}.`
      : result.error ?? "The deliverable provenance link could not be created.");
  }

  async function downloadExport() {
    try {
      const data = await exportAiOutputs(includeArchived);
      const url = URL.createObjectURL(new Blob([JSON.stringify(data, null, 2)], { type: "application/json" }));
      const anchor = document.createElement("a");
      anchor.href = url; anchor.download = `ai-output-logs-${data.tenantId}.json`; anchor.click();
      URL.revokeObjectURL(url);
      setMessage(`${data.logCount} AI output logs exported.`);
    } catch (error) {
      setMessage(error instanceof Error ? error.message : "AI output logs could not be exported.");
    }
  }

  return <section className="route-panel ai-output-governance" aria-labelledby="ai-output-governance-heading">
    <div className="section-heading">
      <p className="eyebrow">AI governance</p>
      <h2 id="ai-output-governance-heading">AI output logs and review</h2>
      <p>Review source-backed drafts, inspect append-only decisions, and declare approved use in governed deliverables.</p>
    </div>
    <div className="governance-toolbar">
      <label className="governance-checkbox"><input type="checkbox" checked={includeArchived} onChange={event => { setState("loading"); setIncludeArchived(event.target.checked); }} /> Include archived output</label>
      {canExport ? <button className="secondary-action" type="button" onClick={() => void downloadExport()}>Export AI logs</button> : null}
    </div>
    {state === "loading" ? <p role="status">Loading AI output logs…</p> : null}
    {state === "error" ? <p role="alert">{message}</p> : null}
    {state === "ready" && outputs.length === 0 ? <p>No AI output logs are available for this workflow.</p> : null}
    {message && state !== "error" ? <p role="status">{message}</p> : null}
    {outputs.map(output => <article key={output.id} className="expert-review-queue__item">
      <div className="governance-status-list"><strong>{output.reviewState}</strong><span>{output.workflowContext}</span><span>{output.classification}</span></div>
      <p><strong>Prompt:</strong> {output.promptWasRedacted ? "Excluded under prohibited-data policy." : output.prompt}</p>
      <p>{output.answer}</p>
      <small>Created {new Date(output.createdAt).toLocaleString()} · retain until {new Date(output.retainUntil).toLocaleDateString()}</small>
      <ul>{output.citations.map(citation => <li key={citation.sourceId}>{citation.title} · {citation.excerptPointer} · {citation.version}</li>)}</ul>
      <div className="governance-subsection ai-output-governance__history">
        <button className="secondary-action" type="button" onClick={() => void loadHistory(output.id)}>Load review history</button>
        {history[output.id]?.length === 0 ? <p>No review decisions recorded.</p> : null}
        {history[output.id]?.length ? <ol>{history[output.id].map(item => <li key={item.id}>{item.previousState} → {item.newState}: {item.note ?? "System transition"}</li>)}</ol> : null}
      </div>
      {canReview ? <form className="governance-form governance-form--section" onSubmit={event => void review(event, output)}>
        <h3>Review output</h3>
        <div className="governance-form__grid governance-form__grid--two">
          <label className="governance-field">Decision<select required value={decisions[output.id] ?? ""} onChange={event => setDecisions(current => ({ ...current, [output.id]: event.target.value }))}>
            <option value="">Select decision</option><option>Approved</option><option>Rejected</option><option>Superseded</option><option>Archived</option>
          </select></label>
          <label className="governance-field">Review note<textarea required maxLength={1000} value={notes[output.id] ?? ""} onChange={event => setNotes(current => ({ ...current, [output.id]: event.target.value }))} /></label>
          {decisions[output.id] === "Rejected" ? <label className="governance-field governance-field--wide">Rejection reason<textarea required maxLength={1000} value={reasons[output.id] ?? ""} onChange={event => setReasons(current => ({ ...current, [output.id]: event.target.value }))} /></label> : null}
        </div>
        <div className="governance-form__actions"><button className="primary-action" type="submit">Save AI output decision</button></div>
      </form> : null}
      {output.reviewState === "Approved" && allowedDeliverableTypes.length > 0 ? <form className="governance-form governance-form--section" onSubmit={event => void link(event, output)}>
        <h3>Link approved output</h3>
        <div className="governance-form__grid governance-form__grid--two">
          <label className="governance-field">Deliverable type<select value={deliverableTypes[output.id] ?? allowedDeliverableTypes[0]} onChange={event => setDeliverableTypes(current => ({ ...current, [output.id]: event.target.value as AiDeliverableType }))}>
            {allowedDeliverableTypes.map(type => <option key={type} value={type}>{type === "Ssp" ? "SSP" : type === "Poam" ? "POA&M" : type === "CustomerDeliverable" ? "Customer deliverable" : type}</option>)}
          </select></label>
          <label className="governance-field">Existing deliverable ID<input required value={deliverableIds[output.id] ?? ""} onChange={event => setDeliverableIds(current => ({ ...current, [output.id]: event.target.value }))} /></label>
        </div>
        <div className="governance-form__actions"><button className="primary-action" type="submit">Link approved output</button></div>
      </form> : null}
    </article>)}
  </section>;
}

import { type FormEvent, useEffect, useState } from "react";
import { assignExpertReviewItem, getAssistantExpertReviewItems, getObligationAssignmentCandidates, resolveExpertReviewItem,
  type ExpertReviewItem, type GuardedAssistantAnswer, type ObligationAssignmentCandidate } from "@/lib/api";

async function loadAssistantReviewQueue(canResolve: boolean) {
  const [queueItems, candidates] = await Promise.all([
    getAssistantExpertReviewItems(),
    canResolve ? getObligationAssignmentCandidates() : Promise.resolve([])
  ]);
  return {
    reviewItems: queueItems.map(item => item.reviewItem),
    answers: Object.fromEntries(queueItems.flatMap(item => item.answer ? [[item.reviewItem.sourceId, item.answer]] : [])) as Record<string, GuardedAssistantAnswer>,
    candidates
  };
}

export function ExpertReviewQueuePanel({ canResolve }: { canResolve: boolean }) {
  const [items, setItems] = useState<ExpertReviewItem[]>([]);
  const [answers, setAnswers] = useState<Record<string, GuardedAssistantAnswer>>({});
  const [candidates, setCandidates] = useState<ObligationAssignmentCandidate[]>([]);
  const [state, setState] = useState<"loading" | "ready" | "error">("loading");
  const [message, setMessage] = useState("");
  const [decision, setDecision] = useState<Record<string, string>>({});
  const [notes, setNotes] = useState<Record<string, string>>({});
  const [assignees, setAssignees] = useState<Record<string, string>>({});
  const [dueDates, setDueDates] = useState<Record<string, string>>({});

  useEffect(() => {
    let active = true;
    const refresh = () => {
      void loadAssistantReviewQueue(canResolve).then(({ reviewItems, answers: loadedAnswers, candidates: loadedCandidates }) => {
        if (!active) return;
        setItems(reviewItems);
        setAnswers(loadedAnswers);
        setCandidates(loadedCandidates);
        setState("ready");
      })
      .catch(error => {
        if (!active) return;
        setState("error");
        setMessage(error instanceof Error ? error.message : "The expert review queue could not be loaded.");
      });
    };
    refresh();
    window.addEventListener("assistant-expert-review-routed", refresh);
    return () => {
      active = false;
      window.removeEventListener("assistant-expert-review-routed", refresh);
    };
  }, [canResolve]);

  async function assign(event: FormEvent<HTMLFormElement>, item: ExpertReviewItem) {
    event.preventDefault();
    const result = await assignExpertReviewItem(item.id, assignees[item.id] ?? "", dueDates[item.id] || null);
    if (!result.data) {
      setMessage(result.error ?? "The expert review item could not be assigned.");
      return;
    }
    setItems(current => current.map(candidate => candidate.id === item.id ? result.data! : candidate));
    setMessage(`Expert review item ${item.id} was assigned and the reviewer was notified.`);
  }

  async function resolve(event: FormEvent<HTMLFormElement>, item: ExpertReviewItem) {
    event.preventDefault();
    const result = await resolveExpertReviewItem(item.id, decision[item.id] ?? "", notes[item.id] ?? "");
    if (!result.data) {
      setMessage(result.error ?? "The expert review item could not be resolved.");
      return;
    }
    setItems(current => current.map(candidate => candidate.id === item.id ? result.data! : candidate));
    setAnswers(current => current[item.sourceId]
      ? { ...current, [item.sourceId]: { ...current[item.sourceId], humanReviewStatus: decision[item.id] ?? "pending",
          reviewDecision: decision[item.id] ?? null, reviewNotes: notes[item.id] ?? null } }
      : current);
    setMessage(`Expert review item ${item.id} was resolved and retained in review history.`);
  }

  return <section className="route-panel expert-review-queue" aria-labelledby="assistant-review-queue-heading">
    <div className="section-heading">
      <p className="eyebrow">Human review workflow</p>
      <h2 id="assistant-review-queue-heading">Assistant expert review queue</h2>
      <p>Tenant-scoped assistant escalations remain draft guidance until a qualified reviewer records a resolution.</p>
    </div>
    {state === "loading" ? <p role="status">Loading expert review items…</p> : null}
    {state === "error" ? <p role="alert">{message}</p> : null}
    {state === "ready" && items.length === 0 ? <p>No assistant answers are currently routed for expert review.</p> : null}
    {items.map(item => <article className="expert-review-queue__item" key={item.id}>
      <div className="guarded-assistant__status">
        <strong>{item.status}</strong><span>{item.priority} priority</span><span>{item.topic}</span>
      </div>
      <p>{item.reason}</p>
      {answers[item.sourceId] ? <div className="expert-review-queue__answer">
        <strong>{answers[item.sourceId].draftLabel} · {answers[item.sourceId].supportStatus} · human review {answers[item.sourceId].humanReviewStatus}</strong>
        <p>{answers[item.sourceId].answer}</p>
        <ul>{answers[item.sourceId].citations.map(citation =>
          <li key={citation.sourceId}>{citation.title} · {citation.excerptPointer} · version {citation.version}</li>)}</ul>
      </div> : <p>Answer detail is unavailable. Do not resolve this item without reviewing its supporting record.</p>}
      <small>Answer {item.sourceId} · created {new Date(item.createdAt).toLocaleString()}</small>
      {item.status === "open" && canResolve ? <form onSubmit={event => void assign(event, item)}>
        <label>Assigned expert<select required value={assignees[item.id] ?? item.assignedExpertUserId ?? ""}
          onChange={event => setAssignees(current => ({ ...current, [item.id]: event.target.value }))}>
          <option value="">Select an active tenant member</option>
          {candidates.map(candidate => <option key={candidate.userId} value={candidate.userId}>{candidate.displayName}</option>)}
        </select></label>
        <label>Due date<input type="date" value={dueDates[item.id] ?? item.dueAt ?? ""}
          onChange={event => setDueDates(current => ({ ...current, [item.id]: event.target.value }))} /></label>
        <button type="submit">Assign expert</button>
      </form> : null}
      {item.status === "open" && canResolve ? <form onSubmit={event => void resolve(event, item)}>
        <label>Decision<select required value={decision[item.id] ?? ""}
          onChange={event => setDecision(current => ({ ...current, [item.id]: event.target.value }))}>
          <option value="">Select a governed disposition</option>
          <option value="accepted_as_reviewed_draft">Accept as reviewed draft</option>
          <option value="revision_required">Revision required</option>
          <option value="rejected">Reject</option>
        </select></label>
        <label>Resolution notes<textarea required maxLength={1000} value={notes[item.id] ?? ""}
          onChange={event => setNotes(current => ({ ...current, [item.id]: event.target.value }))} /></label>
        <button type="submit">Resolve review item</button>
      </form> : null}
      {item.status === "resolved" ? <p>Decision: {item.resolutionDecision}. {item.resolutionNotes}</p> : null}
    </article>)}
    {state !== "error" && message ? <p role="status">{message}</p> : null}
  </section>;
}

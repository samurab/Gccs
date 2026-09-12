import { type FormEvent, useMemo, useState } from "react";
import {
  askAssistant,
  createAssistantDraftAction,
  escalateAssistantAnswer,
  submitAssistantFeedback,
  type AssistantDraftActionType,
  type AssistantFeedbackType,
  type AssistantWorkflowContext,
  type GuardedAssistantAnswer
} from "@/lib/api";

const contextLabels: Record<AssistantWorkflowContext, string> = {
  obligation: "Obligation",
  contract: "Contract",
  evidence: "Evidence",
  cmmc: "CMMC readiness",
  ssp: "SSP",
  poam: "POA&M",
  labor: "Labor compliance",
  subcontractor: "Subcontractor"
};

function isSafeSourceUrl(value: string | null): value is string {
  if (!value) return false;
  try {
    return ["https:", "http:"].includes(new URL(value).protocol);
  } catch {
    return false;
  }
}

export function GuardedAssistantPanel({
  contexts,
  permissions
}: {
  contexts: AssistantWorkflowContext[];
  permissions: string[];
}) {
  const [context, setContext] = useState<AssistantWorkflowContext>(contexts[0]);
  const [question, setQuestion] = useState("");
  const [answer, setAnswer] = useState<GuardedAssistantAnswer | null>(null);
  const [state, setState] = useState<"idle" | "asking" | "ready" | "error">("idle");
  const [message, setMessage] = useState("");
  const [actionType, setActionType] = useState<AssistantDraftActionType>("Task");
  const [actionTitle, setActionTitle] = useState("");
  const [actionBody, setActionBody] = useState("");
  const [feedbackType, setFeedbackType] = useState<AssistantFeedbackType>("Helpful");
  const [feedbackReason, setFeedbackReason] = useState("");

  const allowedActions = useMemo(() => [
    permissions.includes("ManageTasks") ? "Task" as const : null,
    permissions.includes("ManageEvidence") ? "EvidenceRequest" as const : null,
    permissions.includes("ManageEvidence") ? "Note" as const : null,
    permissions.includes("ManageObligations") ? "ReviewItem" as const : null
  ].filter((item): item is AssistantDraftActionType => item !== null), [permissions]);

  async function ask(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setState("asking");
    setMessage("");
    setAnswer(null);
    const result = await askAssistant(question, context);
    if (!result.data) {
      setState("error");
      setMessage(result.error ?? "The assistant request could not be completed.");
      return;
    }
    setAnswer(result.data);
    setActionTitle(`Review ${contextLabels[context]} assistant answer`);
    setActionBody(result.data.answer);
    setState("ready");
  }

  async function createAction(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!answer) return;
    setMessage("");
    const result = await createAssistantDraftAction(answer.id, actionType, actionTitle, actionBody);
    setMessage(result.data
      ? `${result.data.actionType} saved as a draft for human review.`
      : result.error ?? "The draft action could not be created.");
  }

  async function sendFeedback(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!answer) return;
    setMessage("");
    const result = await submitAssistantFeedback(answer.id, feedbackType, feedbackReason);
    setMessage(result.data ? "Feedback recorded with this answer." : result.error ?? "Feedback could not be recorded.");
    if (result.data) setFeedbackReason("");
  }

  async function routeForReview() {
    if (!answer) return;
    const reason = feedbackReason.trim() || `Qualified review requested for ${contextLabels[context]} answer.`;
    const result = await escalateAssistantAnswer(answer.id, reason);
    setMessage(result.data
      ? result.data.created
        ? `Expert review item ${result.data.reviewItem.id} was added to the operational queue.`
        : `This answer is already routed as open review item ${result.data.reviewItem.id}.`
      : result.error ?? "Review routing could not be completed.");
    if (result.data?.created) window.dispatchEvent(new Event("assistant-expert-review-routed"));
    if (result.data) setFeedbackReason("");
  }

  return <section className="route-panel guarded-assistant" aria-labelledby="guarded-assistant-heading">
    <div className="section-heading">
      <p className="eyebrow">Source-bounded assistance</p>
      <h2 id="guarded-assistant-heading">Compliance assistant</h2>
      <p>Draft guidance only. Do not enter CUI, classified, export-controlled, another tenant&apos;s, or other prohibited data. A qualified person must review all output before use.</p>
    </div>

    <form className="guarded-assistant__question" onSubmit={ask}>
      <label>Workflow context
        <select value={context} onChange={event => { setContext(event.target.value as AssistantWorkflowContext); setAnswer(null); setMessage(""); }}>
          {contexts.map(item => <option key={item} value={item}>{contextLabels[item]}</option>)}
        </select>
      </label>
      <label>Question
        <textarea required maxLength={4000} value={question} onChange={event => setQuestion(event.target.value)}
          placeholder="Ask a question that can be answered from approved sources…" />
      </label>
      <button disabled={state === "asking"} type="submit">{state === "asking" ? "Checking sources…" : "Ask assistant"}</button>
    </form>

    {state === "error" ? <p role="alert">{message}</p> : null}
    {state === "asking" ? <p role="status">Checking prompt boundaries and approved sources…</p> : null}
    {answer ? <article className={`guarded-assistant__answer${answer.status === "Blocked" ? " guarded-assistant__answer--blocked" : ""}`} aria-label="Assistant answer">
      <div className="guarded-assistant__status">
        <strong>{answer.draftLabel}</strong>
        <span>{answer.supportStatus}</span>
        <span>{answer.requiresReview ? "Human review required" : "Review status unavailable"}</span>
      </div>
      <p>{answer.answer}</p>
      {answer.blockedReason ? <p role="alert">Blocked category: {answer.blockedReason}. The prompt text was not written to the audit log.</p> : null}
      <div>
        <h3>Citations</h3>
        {answer.citations.length === 0 ? <p>No approved citation supports this answer. Do not rely on it.</p> : <ol>
          {answer.citations.map(citation => <li key={citation.sourceId}>
            <strong>{citation.title}</strong> · {citation.sourceType} · {citation.excerptPointer} · version {citation.version}
            {citation.lastReviewedAt ? ` · reviewed ${citation.lastReviewedAt}` : ""}
            {isSafeSourceUrl(citation.sourceUrl) ? <> · <a href={citation.sourceUrl} target="_blank" rel="noreferrer">Open source</a></> : null}
            {citation.tenantRecordReference ? <small>Tenant record: {citation.tenantRecordReference}</small> : null}
          </li>)}
        </ol>}
      </div>

      {answer.supportStatus === "SourceSupported" && allowedActions.length > 0 ? <form className="guarded-assistant__action" onSubmit={createAction}>
        <h3>Create a reviewable draft</h3>
        <label>Draft type<select value={actionType} onChange={event => setActionType(event.target.value as AssistantDraftActionType)}>
          {allowedActions.map(item => <option key={item} value={item}>{item.replace(/([A-Z])/g, " $1").trim()}</option>)}
        </select></label>
        <label>Title<input required maxLength={240} value={actionTitle} onChange={event => setActionTitle(event.target.value)} /></label>
        <label>Draft content<textarea required maxLength={4000} value={actionBody} onChange={event => setActionBody(event.target.value)} /></label>
        <button type="submit">Save draft</button>
      </form> : answer.supportStatus === "SourceSupported" ? <p>You can review this answer, but your role cannot create assistant drafts.</p> : null}

      <form className="guarded-assistant__feedback" onSubmit={sendFeedback}>
        <h3>Feedback and escalation</h3>
        <label>Feedback<select value={feedbackType} onChange={event => setFeedbackType(event.target.value as AssistantFeedbackType)}>
          <option value="Helpful">Helpful</option>
          <option value="Incorrect">Incorrect</option>
          <option value="MissingSource">Missing source</option>
          <option value="NeedsExpertReview">Needs expert review</option>
        </select></label>
        <label>Reason<textarea required maxLength={1000} value={feedbackReason} onChange={event => setFeedbackReason(event.target.value)} /></label>
        <div><button type="submit">Submit feedback</button>{permissions.includes("ManageObligations")
          ? <button type="button" onClick={() => void routeForReview()}>Route for expert review</button>
          : null}</div>
      </form>
      {message ? <p role="status">{message}</p> : null}
    </article> : null}
  </section>;
}

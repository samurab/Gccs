import { useEffect, useState } from "react";
import {
  createPortalReviewMessage,
  downloadPortalReviewPackage,
  getPortalReviewPackages,
  type PortalReviewPackage
} from "./lib/api";
import { getPortalReviewInvitationId } from "./routing";

type LoadState = "loading" | "ready" | "error";

export function PortalReviewPage() {
  const invitationId = getPortalReviewInvitationId();
  const [packages, setPackages] = useState<PortalReviewPackage[]>([]);
  const [loadState, setLoadState] = useState<LoadState>("loading");
  const [loadError, setLoadError] = useState("");

  useEffect(() => {
    if (!invitationId) return;
    let active = true;
    getPortalReviewPackages(invitationId)
      .then(items => { if (active) { setPackages(items); setLoadState("ready"); } })
      .catch(reason => {
        if (active) {
          setLoadError(reason instanceof Error ? reason.message : "Portal packages could not be loaded.");
          setLoadState("error");
        }
      });
    return () => { active = false; };
  }, [invitationId]);

  const effectiveLoadState: LoadState = invitationId ? loadState : "error";
  const effectiveLoadError = invitationId ? loadError : "The portal invitation link is invalid.";

  function addMessage(sharedPackageId: string, message: Awaited<ReturnType<typeof createPortalReviewMessage>>["data"]) {
    if (!message) return;
    setPackages(current => current.map(item => item.sharedPackageId === sharedPackageId
      ? { ...item, reviewerMessages: [...item.reviewerMessages, message] }
      : item));
  }

  return (
    <main className="platform-console portal-review-page">
      <header className="platform-admin-header">
        <div><p className="platform-admin-kicker">External package review</p><h1>FeDril review portal</h1></div>
        <p className="platform-admin-operator">Read-only tenant access</p>
      </header>
      <section className="platform-posture-band" aria-label="Data handling boundary">
        <div><strong>No-CUI review boundary</strong><span>Only explicitly shared, eligible package snapshots are shown. Do not submit CUI, classified information, credentials, or other prohibited data in reviewer messages.</span></div>
      </section>
      {effectiveLoadState === "loading" ? <p role="status">Loading approved packages…</p> : null}
      {effectiveLoadState === "error" ? <p role="alert">{effectiveLoadError}</p> : null}
      {effectiveLoadState === "ready" && packages.length === 0 ? <p>No approved packages are currently assigned to this invitation.</p> : null}
      {effectiveLoadState === "ready" ? packages.map(item => (
        <PortalPackageCard invitationId={invitationId!} item={item} key={item.sharedPackageId} onMessage={addMessage} />
      )) : null}
    </main>
  );
}

function PortalPackageCard({ invitationId, item, onMessage }: {
  invitationId: string;
  item: PortalReviewPackage;
  onMessage: (sharedPackageId: string, message: Awaited<ReturnType<typeof createPortalReviewMessage>>["data"]) => void;
}) {
  const [kind, setKind] = useState<"Comment" | "Question">("Question");
  const [body, setBody] = useState("");
  const [messageState, setMessageState] = useState<"idle" | "saving" | "saved" | "error">("idle");
  const [messageError, setMessageError] = useState("");
  const [downloadError, setDownloadError] = useState("");

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    setMessageState("saving");
    setMessageError("");
    const result = await createPortalReviewMessage(invitationId, item.sharedPackageId, kind, body);
    if (!result.data) {
      setMessageError(result.error ?? "The reviewer message could not be saved.");
      setMessageState("error");
      return;
    }
    onMessage(item.sharedPackageId, result.data);
    setBody("");
    setMessageState("saved");
  }

  async function download() {
    setDownloadError("");
    const result = await downloadPortalReviewPackage(invitationId, item.sharedPackageId);
    if (!result.data) { setDownloadError(result.error ?? "The package could not be downloaded."); return; }
    const url = URL.createObjectURL(result.data.blob);
    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = result.data.fileName;
    anchor.click();
    URL.revokeObjectURL(url);
  }

  return (
    <article className="platform-form-section portal-review-package">
      <div className="platform-section-heading"><div><h2>{item.title}</h2><p>{item.sourceKind} · version {item.version}</p></div></div>
      <dl className="platform-result-grid">
        <Value label="Status" value={item.status} />
        <Value label="Classification" value={item.classification} />
        <Value label="Generated" value={formatDate(item.generatedAt)} />
        <Value label="Approved for external review" value={formatDate(item.externalReviewApprovedAt)} />
        <Value label="Review due" value={formatDate(item.reviewDueAt)} />
        <Value label="Evidence references" value={String(item.evidenceItemIds.length)} />
        <Value label="Contract" value={item.contractId ?? "Package scope only"} />
      </dl>
      {item.evidenceReferences.length ? <details><summary>Evidence references</summary><ul>{item.evidenceReferences.map(reference =>
        <li key={reference.id}><strong>{reference.name}</strong> — {reference.type}, {reference.classification}; approved {reference.approvedAt ? formatDate(reference.approvedAt) : "date unavailable"}{reference.expiresAt ? `; expires ${reference.expiresAt}` : ""}<br /><small>{reference.id}</small></li>)}</ul></details> : null}
      <section aria-label={`Reviewer messages for ${item.title}`}>
        <h3>Reviewer comments and questions</h3>
        {item.reviewerMessages.length === 0 ? <p>No reviewer messages have been added.</p> :
          <ol>{item.reviewerMessages.map(message => <li key={message.id}><strong>{message.kind}</strong><p>{message.body}</p><small>{formatDate(message.createdAt)}</small></li>)}</ol>}
        <form onSubmit={submit}>
          <label>Message type<select value={kind} onChange={event => setKind(event.target.value as "Comment" | "Question")}><option>Question</option><option>Comment</option></select></label>
          <label>Message<textarea maxLength={2000} required value={body} onChange={event => setBody(event.target.value)} /></label>
          <button disabled={messageState === "saving" || !body.trim()} type="submit">{messageState === "saving" ? "Saving…" : "Add reviewer message"}</button>
          {messageState === "saved" ? <p role="status">Reviewer message saved.</p> : null}
          {messageState === "error" ? <p role="alert">{messageError}</p> : null}
        </form>
      </section>
      {item.downloadAvailable ? <button onClick={() => void download()} type="button">Download controlled package</button> : <p>Downloads are not enabled for this package.</p>}
      {downloadError ? <p role="alert">{downloadError}</p> : null}
    </article>
  );
}

function Value({ label, value }: { label: string; value: string }) {
  return <div><dt>{label}</dt><dd>{value}</dd></div>;
}

function formatDate(value: string) {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? value : date.toLocaleString();
}

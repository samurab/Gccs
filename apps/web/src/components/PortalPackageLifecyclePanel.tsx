import { type FormEvent, useEffect, useState } from "react";
import {
  archiveSharedPortalPackage,
  expireSharedPortalPackage,
  getPortalPackageActivityReport,
  getSharedPortalPackages,
  reissueSharedPortalPackage,
  revokeSharedPortalPackage,
  supersedeSharedPortalPackage,
  type PortalPackageActivity,
  type SharedPortalPackage
} from "@/lib/api";

type ActionState = "idle" | "saving" | "error";

export function PortalPackageLifecyclePanel({
  canManage,
  canViewActivity
}: {
  canManage: boolean;
  canViewActivity: boolean;
}) {
  const [packages, setPackages] = useState<SharedPortalPackage[]>([]);
  const [activities, setActivities] = useState<PortalPackageActivity[]>([]);
  const [loadState, setLoadState] = useState<"loading" | "ready" | "error">("loading");
  const [actionState, setActionState] = useState<ActionState>("idle");
  const [message, setMessage] = useState("");
  const [selectedId, setSelectedId] = useState("");
  const [selectedAction, setSelectedAction] = useState<"revoke" | "supersede" | "reissue" | null>(null);
  const [reason, setReason] = useState("");
  const [replacementPackageId, setReplacementPackageId] = useState("");
  const [replacementSharedPackageId, setReplacementSharedPackageId] = useState("");
  const [expiresAt, setExpiresAt] = useState("");
  const [reviewDueAt, setReviewDueAt] = useState("");
  const [approvalReason, setApprovalReason] = useState("");

  useEffect(() => {
    let mounted = true;
    Promise.all([
      getSharedPortalPackages(),
      canViewActivity ? getPortalPackageActivityReport() : Promise.resolve({ tenantId: "", activities: [] })
    ]).then(([nextPackages, report]) => {
      if (!mounted) return;
      setPackages(nextPackages);
      setActivities(report.activities);
      setLoadState("ready");
    }).catch(() => {
      if (mounted) setLoadState("error");
    });
    return () => { mounted = false; };
  }, [canViewActivity]);

  function replacePackage(updated: SharedPortalPackage) {
    setPackages(current => current.map(item => item.id === updated.id ? updated : item));
  }

  async function refreshActivity() {
    if (!canViewActivity) return;
    const report = await getPortalPackageActivityReport();
    setActivities(report.activities);
  }

  async function runAction(action: () => Promise<{ data: SharedPortalPackage | null; error: string | null }>, success: string) {
    setActionState("saving");
    setMessage("");
    try {
      const result = await action();
      if (!result.data) {
        setActionState("error");
        setMessage(result.error ?? "The package lifecycle action failed.");
        return false;
      }
      replacePackage(result.data);
      await refreshActivity();
      setActionState("idle");
      setMessage(success);
      return true;
    } catch {
      setActionState("error");
      setMessage("The package lifecycle action failed.");
      return false;
    }
  }

  async function revoke(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (await runAction(() => revokeSharedPortalPackage(selectedId, reason), "Portal package access was revoked immediately.")) {
      setSelectedId("");
      setSelectedAction(null);
      setReason("");
    }
  }

  async function reissue(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const expiration = new Date(`${expiresAt}T23:59:59Z`).toISOString();
    const reviewDue = new Date(`${reviewDueAt}T23:59:59Z`).toISOString();
    setActionState("saving");
    setMessage("");
    try {
      const result = await reissueSharedPortalPackage(
        selectedId, replacementPackageId, expiration, 7, reviewDue, approvalReason);
      if (!result.data) {
        setActionState("error");
        setMessage(result.error ?? "The package could not be reissued.");
        return;
      }
      const [nextPackages, report] = await Promise.all([
        getSharedPortalPackages(),
        canViewActivity ? getPortalPackageActivityReport() : Promise.resolve({ tenantId: "", activities: [] })
      ]);
      setPackages(nextPackages);
      setActivities(report.activities);
      setActionState("idle");
      setMessage(`Package reissued as version ${result.data.version}; the prior share remains in lifecycle history.`);
      setSelectedId("");
      setSelectedAction(null);
      setReplacementPackageId("");
      setExpiresAt("");
      setReviewDueAt("");
      setApprovalReason("");
    } catch {
      setActionState("error");
      setMessage("The package could not be reissued.");
    }
  }

  async function supersede(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (await runAction(
      () => supersedeSharedPortalPackage(selectedId, replacementSharedPackageId),
      "The prior share now links to its active replacement."
    )) {
      setSelectedId("");
      setSelectedAction(null);
      setReplacementSharedPackageId("");
    }
  }

  if (loadState === "loading") return <section aria-label="Portal package lifecycle"><p role="status">Loading shared portal packages…</p></section>;
  if (loadState === "error") return <section aria-label="Portal package lifecycle"><p role="alert">Shared portal packages could not be loaded.</p></section>;

  return <section className="route-panel portal-package-lifecycle workflow-control-surface" aria-labelledby="portal-package-lifecycle-heading">
    <div className="section-heading">
      <p className="eyebrow">External review access</p>
      <h2 id="portal-package-lifecycle-heading">Portal package lifecycle</h2>
      <p>Revocation and expiration are enforced by the server on every package access check. This workspace does not authorize CUI sharing.</p>
    </div>
    {message ? <p role={actionState === "error" ? "alert" : "status"}>{message}</p> : null}
    {packages.length === 0 ? <p>No shared portal packages are available for this tenant.</p> : packages.map(item =>
      <article key={item.id} aria-label={`Shared package version ${item.version}`}>
        <strong>Version {item.version} · {item.state}</strong>
        <span>Source package {item.packageId}</span>
        <small>Expires {new Date(item.expiresAt).toLocaleString()} · reminder {item.reminderSentAt ? "sent" : `scheduled ${new Date(item.reminderAt).toLocaleString()}`}</small>
        <small>Review due {new Date(item.reviewDueAt).toLocaleString()} · approved {new Date(item.externalReviewApprovedAt).toLocaleString()} · source version {item.approvedSourceVersion}</small>
        {item.revocationReason ? <small>Revocation reason: {item.revocationReason}</small> : null}
        {item.replacementSharedPackageId ? <small>Replacement share: {item.replacementSharedPackageId}</small> : null}
        {canManage && item.state === "Active" ? <div className="portal-package-lifecycle__actions">
          <button disabled={actionState === "saving"} onClick={() => void runAction(
            () => expireSharedPortalPackage(item.id), "The shared package was expired."
          )} type="button">Expire</button>
          <button onClick={() => { setSelectedId(item.id); setSelectedAction("revoke"); setReason(""); }} type="button">Revoke</button>
          <button onClick={() => { setSelectedId(item.id); setSelectedAction("supersede"); setReplacementSharedPackageId(""); }} type="button">Supersede</button>
        </div> : null}
        {canManage && item.state !== "Archived" ? <div className="portal-package-lifecycle__actions">
          <button onClick={() => { setSelectedId(item.id); setSelectedAction("reissue"); setReplacementPackageId(""); setExpiresAt(""); setReviewDueAt(""); setApprovalReason(""); }} type="button">Reissue</button>
          <button disabled={actionState === "saving"} onClick={() => void runAction(
            () => archiveSharedPortalPackage(item.id), "The shared package was archived."
          )} type="button">Archive</button>
        </div> : null}
      </article>
    )}
    {!canManage ? <p>Portal package lifecycle management requires tenant administrator access.</p> : null}
    {selectedAction === "revoke" && selectedId && packages.some(item => item.id === selectedId && item.state === "Active") ? <form className="portal-package-lifecycle__form" onSubmit={revoke}>
      <h3>Revoke package access</h3>
      <label>Revocation reason<textarea required maxLength={500} value={reason} onChange={event => setReason(event.target.value)} /></label>
      <button disabled={actionState === "saving"} type="submit">Confirm revoke</button>
      <button onClick={() => { setSelectedId(""); setSelectedAction(null); }} type="button">Cancel</button>
    </form> : null}
    {selectedAction === "supersede" && selectedId && packages.some(item => item.id === selectedId && item.state === "Active") ? <form className="portal-package-lifecycle__form" onSubmit={supersede}>
      <h3>Link an existing replacement share</h3>
      <label>Replacement share<select required value={replacementSharedPackageId} onChange={event => setReplacementSharedPackageId(event.target.value)}>
        <option value="">Select an active replacement</option>
        {packages.filter(item => item.id !== selectedId && item.state === "Active").map(item =>
          <option key={item.id} value={item.id}>Version {item.version} · {item.packageId}</option>)}
      </select></label>
      <button disabled={actionState === "saving"} type="submit">Confirm supersede</button>
    </form> : null}
    {selectedAction === "reissue" && selectedId && packages.some(item => item.id === selectedId && item.state !== "Archived") ? <form className="portal-package-lifecycle__form" onSubmit={reissue}>
      <h3>Reissue as a new share version</h3>
      <label>Replacement source package ID<input required value={replacementPackageId} onChange={event => setReplacementPackageId(event.target.value)} /></label>
      <label>New expiration date<input required type="date" value={expiresAt} onChange={event => setExpiresAt(event.target.value)} /></label>
      <label>Review due date<input required type="date" value={reviewDueAt} onChange={event => setReviewDueAt(event.target.value)} /></label>
      <label>External-review approval reason<textarea required maxLength={1000} value={approvalReason} onChange={event => setApprovalReason(event.target.value)} /></label>
      <button disabled={actionState === "saving"} type="submit">Confirm reissue</button>
    </form> : null}
    {canViewActivity ? <div>
      <h3>Portal activity</h3>
      {activities.length === 0 ? <p>No portal activity has been recorded.</p> : <ul>
        {activities.map(activity => <li key={activity.id}>
          {activity.activityType} · {new Date(activity.occurredAt).toLocaleString()}
          {activity.detail ? ` · ${activity.detail}` : ""}
        </li>)}
      </ul>}
    </div> : null}
  </section>;
}

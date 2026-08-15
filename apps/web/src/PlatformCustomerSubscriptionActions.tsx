import { Ban } from "lucide-react";
import { useState } from "react";
import {
  cancelPlatformPilotSubscription,
  convertPlatformPilotSubscription,
  expirePlatformPilotSubscription,
  extendPlatformPilotSubscription,
  type TenantSubscription
} from "./lib/api";

export function PlatformCustomerSubscriptionActions({
  customerName,
  customerReference,
  onChanged,
  subscription,
  tenantId
}: {
  customerName: string;
  customerReference: string | null;
  onChanged: (subscription: TenantSubscription) => void;
  subscription: TenantSubscription;
  tenantId: string;
}) {
  const [reason, setReason] = useState("");
  const [newEndsOn, setNewEndsOn] = useState("");
  const [planCode, setPlanCode] = useState("");
  const [externalReference, setExternalReference] = useState("");
  const [state, setState] = useState<"idle" | "submitting" | "error" | "success">("idle");
  const [message, setMessage] = useState("");
  const [retryRequest, setRetryRequest] = useState<{ identity: string; key: string } | null>(null);
  const [pendingAction, setPendingAction] = useState<"expire" | "cancel" | "convert" | null>(null);

  async function execute(action: "extend" | "expire" | "cancel" | "convert") {
    setState("submitting");
    setMessage("");
    const identity = JSON.stringify({ action, newEndsOn, planCode, externalReference, reason, version: subscription.version });
    const idempotencyKey = retryRequest?.identity === identity ? retryRequest.key : crypto.randomUUID();
    setRetryRequest({ identity, key: idempotencyKey });
    try {
      const response = action === "extend"
        ? await extendPlatformPilotSubscription(tenantId, newEndsOn, reason, subscription.version, idempotencyKey)
        : action === "expire"
          ? await expirePlatformPilotSubscription(tenantId, reason, subscription.version, idempotencyKey)
          : action === "cancel"
            ? await cancelPlatformPilotSubscription(tenantId, reason, subscription.version, idempotencyKey)
            : await convertPlatformPilotSubscription(tenantId, planCode, externalReference, reason, subscription.version, idempotencyKey);
      if (!response.data) {
        setMessage(response.error ?? "Subscription transition failed. Refresh before retrying.");
        setState("error");
        return;
      }
      onChanged(response.data);
      setRetryRequest(null);
      setPendingAction(null);
      setReason("");
      setNewEndsOn("");
      setPlanCode("");
      setExternalReference("");
      setMessage({
        extend: "Pilot extension completed.",
        expire: "Pilot grace period started.",
        cancel: "Pilot subscription cancelled.",
        convert: "Pilot converted to commercial."
      }[action]);
      setState("success");
    } catch (error) {
      setMessage(error instanceof Error ? error.message : "Subscription transition failed. Refresh before retrying.");
      setState("error");
    }
  }

  const confirmation = pendingAction === "expire"
    ? ["Start the read-only grace period?", "This immediately blocks tenant mutations.", "Confirm start grace period"]
    : pendingAction === "cancel"
      ? ["Cancel this pilot subscription?", "This immediately denies tenant workspace access without deleting tenant data or audit history.", "Confirm pilot cancellation"]
      : pendingAction === "convert"
        ? ["Convert this pilot to commercial?", "Confirm payment separately; FeDril does not verify billing-provider payment status.", "Confirm commercial conversion"]
        : null;
  const reasonMissing = reason.trim().length === 0;

  return (
    <section className="platform-form-section" aria-labelledby="subscription-actions-heading">
      <div className="platform-section-heading">
        <span>04</span>
        <div>
          <h2 id="subscription-actions-heading">Subscription actions</h2>
          <p>{customerName} · {customerReference ?? tenantId}</p>
        </div>
      </div>
      <div className="platform-form-grid">
        <label><span>New pilot end date</span><input type="date" value={newEndsOn} onChange={(event) => setNewEndsOn(event.target.value)} /><small>The date is inclusive; access ends at 00:00 UTC the following day.</small></label>
        <label><span>Commercial plan code</span><input maxLength={80} value={planCode} onChange={(event) => setPlanCode(event.target.value)} /></label>
        <label><span>External subscription reference</span><input maxLength={160} value={externalReference} onChange={(event) => setExternalReference(event.target.value)} /></label>
        <label className="platform-span-2"><span>Required reason</span><textarea aria-required="true" maxLength={600} rows={2} value={reason} onChange={(event) => setReason(event.target.value)} /></label>
      </div>
      <div className="platform-form-actions">
        <button disabled={state === "submitting" || reasonMissing || !newEndsOn} onClick={() => void execute("extend")} type="button">Extend pilot</button>
        <button disabled={state === "submitting" || reasonMissing || subscription.effectiveStatus !== "Active"} onClick={() => setPendingAction("expire")} type="button">Start grace period</button>
        <button disabled={state === "submitting" || reasonMissing} onClick={() => setPendingAction("cancel")} type="button">Cancel pilot</button>
        <button disabled={state === "submitting" || reasonMissing || !planCode.trim() || !externalReference.trim()} onClick={() => setPendingAction("convert")} type="button">Convert to commercial</button>
      </div>
      {confirmation && pendingAction ? (
        <div className="platform-cancel-panel" role="group" aria-label={confirmation[0]}>
          <div><strong>{confirmation[0]}</strong><span>{confirmation[1]}</span></div>
          <div className="platform-cancel-actions">
            <button className="platform-danger-action" disabled={state === "submitting"} onClick={() => void execute(pendingAction)} type="button"><Ban aria-hidden="true" size={17} />{state === "submitting" ? "Updating subscription" : confirmation[2]}</button>
            <button className="platform-secondary-action" disabled={state === "submitting"} onClick={() => setPendingAction(null)} type="button">Keep current subscription</button>
          </div>
        </div>
      ) : null}
      {message ? <div className={state === "error" ? "platform-form-error" : "platform-pending-state"} role={state === "error" ? "alert" : "status"}>{message}</div> : null}
    </section>
  );
}

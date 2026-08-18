import { useEffect, useMemo, useRef, useState, type FormEvent } from "react";
import {
  BadgeDollarSign,
  Ban,
  Building2,
  Check,
  CheckCircle2,
  ChevronLeft,
  ChevronRight,
  Clipboard,
  FlaskConical,
  LoaderCircle,
  LockKeyhole,
  Plus,
  RefreshCw,
  ShieldAlert,
  UserRoundPlus
} from "lucide-react";
import {
  cancelPlatformTenantOnboarding,
  getPlatformAccess,
  getPlatformTenantOnboardings,
  provisionPlatformTenant,
  resendPlatformTenantInvitation,
  type PlatformAccess,
  type PlatformTenantOnboardingPage,
  type PlatformTenantProvisioningRequest,
  type PlatformTenantProvisioningResult
} from "./lib/api";
import { PlatformAdminNav } from "./PlatformAdminNav";
import { formatUsDateTime } from "./lib/dateFormat";

type FormState = {
  onboardingType: "Pilot" | "Paid";
  customerReference: string;
  displayName: string;
  ownerEmail: string;
  ownerDisplayName: string;
  trialEndsAt: string;
  planCode: string;
  subscriptionReference: string;
  setupReason: string;
  confirmsNoCui: boolean;
  commercialApprovalConfirmed: boolean;
};

const initialForm: FormState = {
  onboardingType: "Pilot",
  customerReference: "",
  displayName: "",
  ownerEmail: "",
  ownerDisplayName: "",
  trialEndsAt: "",
  planCode: "",
  subscriptionReference: "",
  setupReason: "",
  confirmsNoCui: false,
  commercialApprovalConfirmed: false
};

function newIdempotencyKey() {
  return crypto.randomUUID();
}

function invitationDeliveryLabel(status: string) {
  switch (status) {
    case "Processing": return "Submitting";
    case "RetryScheduled": return "Retry scheduled";
    case "Sent": return "Provider accepted";
    default: return status;
  }
}

function invitationDeliveryMessage(result: PlatformTenantProvisioningResult, deliveryMode: PlatformAccess["invitationDeliveryMode"]) {
  if (deliveryMode === "Disabled" && result.invitationDeliveryStatus === "Queued") {
    return "Invitation recorded, but email delivery is disabled. Configure invitation delivery before requeuing it.";
  }

  switch (result.invitationDeliveryStatus) {
    case "Sent":
      return `Email provider accepted the invitation${result.invitationNotificationSentAt ? ` ${formatUsDateTime(result.invitationNotificationSentAt)}` : ""}. Inbox delivery is not confirmed.`;
    case "Processing":
      return "Invitation submission to the email provider is in progress.";
    case "RetryScheduled":
      return "The provider submission failed and is scheduled for retry.";
    case "Failed":
      return "Invitation delivery failed after the configured retry limit.";
    case "Cancelled":
      return "Invitation delivery was cancelled.";
    default:
      return "Invitation is queued for asynchronous provider submission.";
  }
}

export function PlatformTenantAdminPage() {
  const [access, setAccess] = useState<PlatformAccess | null>(null);
  const [accessState, setAccessState] = useState<"loading" | "ready" | "error">("loading");
  const [accessError, setAccessError] = useState("");
  const [form, setForm] = useState<FormState>(initialForm);
  const [idempotencyKey, setIdempotencyKey] = useState(newIdempotencyKey);
  const [submitState, setSubmitState] = useState<"idle" | "submitting" | "error" | "success">("idle");
  const [submitError, setSubmitError] = useState("");
  const [result, setResult] = useState<PlatformTenantProvisioningResult | null>(null);
  const [copiedValue, setCopiedValue] = useState<string | null>(null);
  const [onboardings, setOnboardings] = useState<PlatformTenantOnboardingPage | null>(null);
  const [onboardingsState, setOnboardingsState] = useState<"idle" | "loading" | "ready" | "error">("idle");
  const [onboardingsError, setOnboardingsError] = useState("");
  const [onboardingsPage, setOnboardingsPage] = useState(1);
  const [onboardingsRefresh, setOnboardingsRefresh] = useState(0);
  const deliveryPolling = useRef<{ onboardingId: string | null; attempts: number }>({ onboardingId: null, attempts: 0 });
  const pilotTrialDateRules = access?.pilotTrialDateRules;
  const canManageTenantOnboarding = access?.canManageTenantOnboarding ?? access?.canProvisionTenants ?? false;

  useEffect(() => {
    let active = true;
    getPlatformAccess()
      .then((nextAccess) => {
        if (!active) return;
        setAccess(nextAccess);
        setAccessState("ready");
      })
      .catch((error) => {
        if (!active) return;
        setAccessError(error instanceof Error ? error.message : "Platform access could not be verified.");
        setAccessState("error");
      });

    return () => {
      active = false;
    };
  }, []);

  useEffect(() => {
    if (!canManageTenantOnboarding) return;

    let active = true;
    getPlatformTenantOnboardings(onboardingsPage, 25, "PendingOwnerAcceptance")
      .then((page) => {
        if (!active) return;
        if (page.items.length === 0 && page.page > 1 && page.totalCount > 0) {
          setOnboardingsPage(page.page - 1);
          return;
        }
        setOnboardings(page);
        setResult((current) => {
          if (!current) return current;
          const refreshed = page.items.find((item) => item.onboardingId === current.onboardingId);
          return refreshed
            ? {
                ...current,
                invitationStatus: refreshed.invitationStatus,
                invitationDeliveryStatus: refreshed.invitationDeliveryStatus,
                invitationNotificationSentAt: refreshed.invitationNotificationSentAt
              }
            : current;
        });
        setOnboardingsState("ready");
      })
      .catch((error) => {
        if (!active) return;
        setOnboardingsError(error instanceof Error ? error.message : "Pending tenant onboardings could not be loaded.");
        setOnboardingsState("error");
      });

    return () => {
      active = false;
    };
  }, [canManageTenantOnboarding, onboardingsPage, onboardingsRefresh]);

  useEffect(() => {
    const onboardingId = result?.onboardingId ?? null;
    if (deliveryPolling.current.onboardingId !== onboardingId) {
      deliveryPolling.current = { onboardingId, attempts: 0 };
    }

    if (
      !onboardingId ||
      access?.invitationDeliveryMode === "Disabled" ||
      result?.invitationDeliveryStatus === "Sent" ||
      result?.invitationDeliveryStatus === "Failed" ||
      result?.invitationDeliveryStatus === "Cancelled" ||
      deliveryPolling.current.attempts >= 10
    ) {
      return;
    }

    const timeoutId = window.setTimeout(() => {
      deliveryPolling.current.attempts += 1;
      setOnboardingsRefresh((current) => current + 1);
    }, 1000);

    return () => window.clearTimeout(timeoutId);
  }, [access?.invitationDeliveryMode, onboardingsRefresh, result?.invitationDeliveryStatus, result?.onboardingId]);

  const request = useMemo<PlatformTenantProvisioningRequest>(
    () => ({
      onboardingType: form.onboardingType,
      customerReference: form.customerReference,
      displayName: form.displayName,
      ownerEmail: form.ownerEmail,
      ownerDisplayName: form.ownerDisplayName,
      trialEndsAt: form.onboardingType === "Pilot" ? form.trialEndsAt || null : null,
      planCode: form.onboardingType === "Paid" ? form.planCode || null : null,
      subscriptionReference: form.onboardingType === "Paid" ? form.subscriptionReference || null : null,
      setupReason: form.setupReason,
      confirmsNoCui: form.confirmsNoCui,
      commercialApprovalConfirmed: form.onboardingType === "Paid" && form.commercialApprovalConfirmed
    }),
    [form]
  );

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSubmitState("submitting");
    setSubmitError("");

    if (
      request.onboardingType === "Pilot" &&
      pilotTrialDateRules &&
      (!request.trialEndsAt ||
        request.trialEndsAt < pilotTrialDateRules.minimumEndsOn ||
        request.trialEndsAt > pilotTrialDateRules.maximumEndsOn)
    ) {
      setSubmitError(
        `Pilot end date must be between ${pilotTrialDateRules.minimumEndsOn} and ${pilotTrialDateRules.maximumEndsOn} (UTC).`
      );
      setSubmitState("error");
      return;
    }

    const response = await provisionPlatformTenant(request, idempotencyKey);
    if (!response.data) {
      setSubmitError(response.error ?? "Tenant onboarding could not be created.");
      setSubmitState("error");
      return;
    }

    setResult(response.data);
    setSubmitState("success");
    setOnboardingsState("loading");
    setOnboardingsError("");
    setOnboardingsPage(1);
    setOnboardingsRefresh((current) => current + 1);
  }

  function resetForm() {
    setForm(initialForm);
    setIdempotencyKey(newIdempotencyKey());
    setResult(null);
    setSubmitError("");
    setSubmitState("idle");
  }

  async function copyValue(value: string) {
    await navigator.clipboard.writeText(value);
    setCopiedValue(value);
    window.setTimeout(() => setCopiedValue(null), 1600);
  }

  async function resendInvitation() {
    if (!result) return null;
    const response = await resendPlatformTenantInvitation(result.invitationId);
    const invitation = response.data;
    if (invitation) {
      setResult((current) => current ? {
        ...current,
        invitationDeliveryStatus: invitation.deliveryStatus,
        invitationNotificationSentAt: invitation.notificationSentAt
      } : current);
    }
    return response.error;
  }

  async function cancelOnboarding(onboardingId: string, reason: string) {
    const response = await cancelPlatformTenantOnboarding(onboardingId, reason);
    if (response.data) {
      if (result?.onboardingId === onboardingId) {
        setResult(response.data);
      }
      setOnboardingsState("loading");
      setOnboardingsError("");
      setOnboardingsRefresh((current) => current + 1);
    }
    return response.error;
  }

  if (accessState === "loading") {
    return <PlatformState icon={LoaderCircle} title="Verifying platform access" body="Checking operator authorization." spin />;
  }

  if (accessState === "error") {
    return <PlatformState icon={ShieldAlert} title="Platform access unavailable" body={accessError} />;
  }

  if (!access || !canManageTenantOnboarding) {
    return (
      <PlatformState
        icon={LockKeyhole}
        title="Provisioning access denied"
        body="Your authenticated account does not have a platform tenant onboarding management permission."
      />
    );
  }

  const verifiedAccess = access;

  return (
    <main className="platform-admin-page">
      <PlatformAdminNav access={verifiedAccess} active="tenant-onboarding" />
      <header className="platform-admin-header">
        <div>
          <p className="platform-admin-kicker">FeDril platform operations</p>
          <h1>Tenant onboarding</h1>
          <p className="platform-admin-operator">Signed in as {verifiedAccess.userEmail ?? verifiedAccess.userId}</p>
        </div>
      </header>

      <section className="platform-posture-band" aria-label="Data handling boundary">
        <ShieldAlert aria-hidden="true" size={20} />
        <div>
          <strong>No-CUI product boundary</strong>
          <span>Tenant creation does not authorize CUI, classified, ITAR, or sensitive government data.</span>
        </div>
      </section>

      {canManageTenantOnboarding ? (
        <PendingOnboardings
          data={onboardings}
          error={onboardingsError}
          onCancel={cancelOnboarding}
          onPageChange={(page) => {
            setOnboardingsState("loading");
            setOnboardingsError("");
            setOnboardingsPage(page);
          }}
          state={onboardingsState}
        />
      ) : null}

      {canManageTenantOnboarding && submitState === "success" && result ? (
        <ProvisioningSuccess
          result={result}
          copiedValue={copiedValue}
          deliveryMode={access.invitationDeliveryMode}
          onCopy={copyValue}
          onReset={resetForm}
          onResend={resendInvitation}
        />
      ) : canManageTenantOnboarding ? (
        <form className="platform-onboarding-form" onSubmit={handleSubmit}>
          <section className="platform-form-section" aria-labelledby="onboarding-type-heading">
            <div className="platform-section-heading">
              <span>01</span>
              <div>
                <h2 id="onboarding-type-heading">Onboarding type</h2>
                <p>Select the commercial path approved for this tenant.</p>
              </div>
            </div>
            <div className="platform-mode-control" role="radiogroup" aria-label="Onboarding type">
              <button
                aria-checked={form.onboardingType === "Pilot"}
                className={form.onboardingType === "Pilot" ? "is-active" : ""}
                onClick={() =>
                  setForm((current) => ({
                    ...current,
                    onboardingType: "Pilot",
                    planCode: "",
                    subscriptionReference: "",
                    commercialApprovalConfirmed: false
                  }))
                }
                role="radio"
                type="button"
              >
                <FlaskConical aria-hidden="true" size={18} />
                <span><strong>Pilot</strong><small>Time-bound evaluation</small></span>
              </button>
              <button
                aria-checked={form.onboardingType === "Paid"}
                className={form.onboardingType === "Paid" ? "is-active" : ""}
                onClick={() => setForm((current) => ({ ...current, onboardingType: "Paid", trialEndsAt: "" }))}
                role="radio"
                type="button"
              >
                <BadgeDollarSign aria-hidden="true" size={18} />
                <span><strong>Paid</strong><small>Approved subscription</small></span>
              </button>
            </div>
          </section>

          <section className="platform-form-section" aria-labelledby="tenant-details-heading">
            <div className="platform-section-heading">
              <span>02</span>
              <div>
                <h2 id="tenant-details-heading">Tenant record</h2>
                <p>Use the approved non-sensitive customer reference.</p>
              </div>
            </div>
            <div className="platform-form-grid">
              <label>
                <span>Customer reference</span>
                <input
                  autoComplete="off"
                  maxLength={120}
                  onChange={(event) => setForm((current) => ({ ...current, customerReference: event.target.value }))}
                  placeholder={form.onboardingType === "Pilot" ? "PILOT-003" : "CUSTOMER-014"}
                  required
                  value={form.customerReference}
                />
              </label>
              <label>
                <span>Tenant display name</span>
                <input
                  maxLength={240}
                  onChange={(event) => setForm((current) => ({ ...current, displayName: event.target.value }))}
                  placeholder="Aegis Workspace"
                  required
                  value={form.displayName}
                />
              </label>
              {form.onboardingType === "Pilot" ? (
                <label>
                  <span>Pilot end date</span>
                  <input
                    aria-label="Pilot end date"
                    aria-describedby={pilotTrialDateRules ? "pilot-end-date-help" : undefined}
                    max={pilotTrialDateRules?.maximumEndsOn}
                    min={pilotTrialDateRules?.minimumEndsOn}
                    onChange={(event) => setForm((current) => ({ ...current, trialEndsAt: event.target.value }))}
                    required
                    type="date"
                    value={form.trialEndsAt}
                  />
                  {pilotTrialDateRules ? (
                    <small id="pilot-end-date-help">
                      Select {pilotTrialDateRules.minimumEndsOn} through {pilotTrialDateRules.maximumEndsOn} UTC; pilots are limited to {pilotTrialDateRules.maximumPilotDays} days.
                    </small>
                  ) : null}
                </label>
              ) : (
                <>
                  <label>
                    <span>Plan code</span>
                    <input
                      maxLength={80}
                      onChange={(event) => setForm((current) => ({ ...current, planCode: event.target.value }))}
                      placeholder="FOUNDATION-ANNUAL"
                      required
                      value={form.planCode}
                    />
                  </label>
                  <label>
                    <span>Subscription reference</span>
                    <input
                      maxLength={160}
                      onChange={(event) =>
                        setForm((current) => ({ ...current, subscriptionReference: event.target.value }))
                      }
                      placeholder="SUB-000014"
                      required
                      value={form.subscriptionReference}
                    />
                  </label>
                </>
              )}
              <label className="platform-span-2">
                <span>Setup reason</span>
                <textarea
                  maxLength={600}
                  onChange={(event) => setForm((current) => ({ ...current, setupReason: event.target.value }))}
                  placeholder="Provision approved No-CUI onboarding record."
                  required
                  rows={3}
                  value={form.setupReason}
                />
              </label>
            </div>
          </section>

          <section className="platform-form-section" aria-labelledby="owner-details-heading">
            <div className="platform-section-heading">
              <span>03</span>
              <div>
                <h2 id="owner-details-heading">Initial Owner</h2>
                <p>The Owner remains pending until the invitation is accepted by the matching authenticated email.</p>
              </div>
            </div>
            <div className="platform-form-grid">
              <label>
                <span>Owner email</span>
                <input
                  autoComplete="email"
                  maxLength={320}
                  onChange={(event) => setForm((current) => ({ ...current, ownerEmail: event.target.value }))}
                  placeholder="owner@customer.com"
                  required
                  type="email"
                  value={form.ownerEmail}
                />
              </label>
              <label>
                <span>Owner display name</span>
                <input
                  autoComplete="name"
                  maxLength={200}
                  onChange={(event) => setForm((current) => ({ ...current, ownerDisplayName: event.target.value }))}
                  placeholder="Jordan Lee"
                  required
                  value={form.ownerDisplayName}
                />
              </label>
            </div>
          </section>

          <section className="platform-form-section" aria-labelledby="approval-heading">
            <div className="platform-section-heading">
              <span>04</span>
              <div>
                <h2 id="approval-heading">Operator confirmations</h2>
                <p>Both the UI and API enforce the applicable confirmations.</p>
              </div>
            </div>
            <div className="platform-confirmations">
              <label>
                <input
                  checked={form.confirmsNoCui}
                  onChange={(event) => setForm((current) => ({ ...current, confirmsNoCui: event.target.checked }))}
                  required
                  type="checkbox"
                />
                <span><strong>No-CUI boundary confirmed</strong><small>The customer has received the prohibited-data guidance.</small></span>
              </label>
              {form.onboardingType === "Paid" ? (
                <label>
                  <input
                    checked={form.commercialApprovalConfirmed}
                    onChange={(event) =>
                      setForm((current) => ({ ...current, commercialApprovalConfirmed: event.target.checked }))
                    }
                    required
                    type="checkbox"
                  />
                  <span><strong>Commercial approval confirmed</strong><small>The subscription reference is approved in the billing system of record.</small></span>
                </label>
              ) : null}
            </div>
          </section>

          {submitState === "error" ? (
            <div className="platform-form-error" role="alert">
              <ShieldAlert aria-hidden="true" size={18} />
              <span>{submitError}</span>
            </div>
          ) : null}

          <footer className="platform-form-actions">
            <div>
              <span>Request key</span>
              <code>{idempotencyKey}</code>
            </div>
            <button className="platform-primary-action" disabled={submitState === "submitting"} type="submit">
              {submitState === "submitting" ? (
                <><LoaderCircle aria-hidden="true" className="spin" size={18} /> Creating pending tenant</>
              ) : (
                <><UserRoundPlus aria-hidden="true" size={18} /> Create pending tenant</>
              )}
            </button>
          </footer>
        </form>
      ) : null}
    </main>
  );
}

function PendingOnboardings({
  data,
  error,
  onCancel,
  onPageChange,
  state
}: {
  data: PlatformTenantOnboardingPage | null;
  error: string;
  onCancel: (onboardingId: string, reason: string) => Promise<string | null>;
  onPageChange: (page: number) => void;
  state: "idle" | "loading" | "ready" | "error";
}) {
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [reason, setReason] = useState("");
  const [cancelState, setCancelState] = useState<"idle" | "submitting" | "error">("idle");
  const [cancelError, setCancelError] = useState("");

  async function handleCancel() {
    if (!selectedId) return;
    setCancelState("submitting");
    setCancelError("");
    const nextError = await onCancel(selectedId, reason);
    if (nextError) {
      setCancelError(nextError);
      setCancelState("error");
      return;
    }

    setSelectedId(null);
    setReason("");
    setCancelState("idle");
  }

  return (
    <section className="platform-pending" aria-labelledby="pending-onboardings-heading">
      <div className="platform-pending-heading">
        <div>
          <p>Platform operations</p>
          <h2 id="pending-onboardings-heading">Pending tenant onboardings</h2>
        </div>
        {data ? <span>{data.totalCount} pending</span> : null}
      </div>

      {state === "loading" || state === "idle" ? (
        <div className="platform-pending-state"><LoaderCircle aria-hidden="true" className="spin" size={18} /> Loading pending onboardings</div>
      ) : null}
      {state === "error" ? (
        <div className="platform-form-error" role="alert"><ShieldAlert aria-hidden="true" size={18} /><span>{error}</span></div>
      ) : null}
      {state === "ready" && data?.items.length === 0 ? (
        <div className="platform-pending-state">No tenant onboardings are awaiting Owner acceptance.</div>
      ) : null}
      {state === "ready" && data && data.items.length > 0 ? (
        <>
          <div className="platform-pending-table-wrap">
            <table className="platform-pending-table">
              <thead>
                <tr>
                  <th>Tenant</th>
                  <th>Customer reference</th>
                  <th>Owner</th>
                  <th>Delivery</th>
                  <th><span className="sr-only">Actions</span></th>
                </tr>
              </thead>
              <tbody>
                {data.items.map((item) => (
                  <tr key={item.onboardingId}>
                    <td data-label="Tenant"><strong>{item.displayName}</strong><span>{item.onboardingType}</span></td>
                    <td data-label="Customer reference">{item.customerReference}</td>
                    <td data-label="Owner">{item.ownerEmail}</td>
                    <td data-label="Delivery">{invitationDeliveryLabel(item.invitationDeliveryStatus)}</td>
                    <td data-label="Action">
                      <button
                        aria-label={`Cancel onboarding for ${item.displayName}`}
                        className="platform-cancel-icon"
                        onClick={() => {
                          setSelectedId(item.onboardingId);
                          setReason("");
                          setCancelError("");
                          setCancelState("idle");
                        }}
                        title="Cancel pending onboarding"
                        type="button"
                      >
                        <Ban aria-hidden="true" size={17} />
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div className="platform-pending-pagination">
            <button
              aria-label="Previous pending onboardings page"
              disabled={!data.hasPreviousPage}
              onClick={() => onPageChange(data.page - 1)}
              title="Previous page"
              type="button"
            >
              <ChevronLeft aria-hidden="true" size={17} />
            </button>
            <span>Page {data.page}</span>
            <button
              aria-label="Next pending onboardings page"
              disabled={!data.hasNextPage}
              onClick={() => onPageChange(data.page + 1)}
              title="Next page"
              type="button"
            >
              <ChevronRight aria-hidden="true" size={17} />
            </button>
          </div>
        </>
      ) : null}

      {selectedId ? (
        <div className="platform-cancel-panel">
          <div>
            <strong>Cancel pending onboarding</strong>
            <span>This revokes the Owner invitation and archives the inactive tenant record.</span>
          </div>
          <label htmlFor="platform-cancellation-reason">Cancellation reason</label>
          <textarea
            id="platform-cancellation-reason"
            maxLength={600}
            onChange={(event) => setReason(event.target.value)}
            required
            rows={3}
            value={reason}
          />
          {cancelState === "error" ? <div className="platform-form-error" role="alert"><ShieldAlert aria-hidden="true" size={18} /><span>{cancelError}</span></div> : null}
          <div className="platform-cancel-actions">
            <button
              className="platform-danger-action"
              disabled={cancelState === "submitting" || reason.trim().length === 0}
              onClick={() => void handleCancel()}
              type="button"
            >
              <Ban aria-hidden="true" size={17} />
              {cancelState === "submitting" ? "Cancelling" : "Confirm cancellation"}
            </button>
            <button
              className="platform-secondary-action"
              disabled={cancelState === "submitting"}
              onClick={() => setSelectedId(null)}
              type="button"
            >
              Keep onboarding
            </button>
          </div>
        </div>
      ) : null}
    </section>
  );
}

function ProvisioningSuccess({
  result,
  copiedValue,
  deliveryMode,
  onCopy,
  onReset,
  onResend
}: {
  result: PlatformTenantProvisioningResult;
  copiedValue: string | null;
  deliveryMode: PlatformAccess["invitationDeliveryMode"];
  onCopy: (value: string) => Promise<void>;
  onReset: () => void;
  onResend: () => Promise<string | null>;
}) {
  const [resendState, setResendState] = useState<"idle" | "sending" | "sent" | "error">("idle");
  const [resendError, setResendError] = useState("");
  const isCancelled = result.onboardingStatus === "Cancelled";

  async function handleResend() {
    setResendState("sending");
    setResendError("");
    const error = await onResend();
    if (error) {
      setResendError(error);
      setResendState("error");
      return;
    }
    setResendState("sent");
  }

  return (
    <section className="platform-success" aria-labelledby="provisioning-success-heading">
      <div className="platform-success-heading">
        {isCancelled ? <Ban aria-hidden="true" size={28} /> : <CheckCircle2 aria-hidden="true" size={28} />}
        <div>
          <p>{isCancelled ? `${result.onboardingType} onboarding cancelled` : `${result.onboardingType} onboarding created`}</p>
          <h2 id="provisioning-success-heading">{result.displayName}</h2>
          <span>
            {isCancelled
              ? "The Owner invitation is revoked and the inactive tenant record is preserved for audit history."
              : "The tenant is pending Owner acceptance and remains within the No-CUI boundary."}
          </span>
        </div>
      </div>
      <dl className="platform-result-grid">
        <ResultValue label="Tenant ID" value={result.tenantId} copiedValue={copiedValue} onCopy={onCopy} />
        <ResultValue label="Onboarding ID" value={result.onboardingId} copiedValue={copiedValue} onCopy={onCopy} />
        <div><dt>Customer reference</dt><dd>{result.customerReference}</dd></div>
        <div><dt>Onboarding status</dt><dd>{result.onboardingStatus}</dd></div>
        <div><dt>Tenant status</dt><dd>{result.tenantStatus}</dd></div>
        <div><dt>Owner</dt><dd>{result.ownerEmail}</dd></div>
        <div><dt>Invitation</dt><dd>{result.invitationStatus}</dd></div>
        <div><dt>Email submission</dt><dd>{invitationDeliveryLabel(result.invitationDeliveryStatus)}</dd></div>
        <div><dt>Data handling</dt><dd>{result.dataHandlingMode}</dd></div>
        <div><dt>Expires</dt><dd>{formatUsDateTime(result.invitationExpiresAt)}</dd></div>
      </dl>
      <div className="platform-success-note">
        <LockKeyhole aria-hidden="true" size={18} />
        {isCancelled
          ? `Cancelled${result.cancelledAt ? ` ${formatUsDateTime(result.cancelledAt)}` : ""}: ${result.cancellationReason ?? "No reason recorded."}`
          : invitationDeliveryMessage(result, deliveryMode)}
      </div>
      {resendState === "error" ? <div className="platform-form-error" role="alert"><ShieldAlert aria-hidden="true" size={18} /><span>{resendError}</span></div> : null}
      <div className="platform-success-actions">
        {!isCancelled ? (
          <button className="platform-secondary-action" disabled={resendState === "sending"} onClick={() => void handleResend()} type="button">
            <RefreshCw aria-hidden="true" className={resendState === "sending" ? "spin" : undefined} size={17} />
            {resendState === "sending" ? "Requeuing invitation" : resendState === "sent" ? "Invitation requeued" : "Resend invitation"}
          </button>
        ) : null}
        <button className="platform-secondary-action" onClick={onReset} type="button">
          <Plus aria-hidden="true" size={17} />
          Provision another tenant
        </button>
      </div>
    </section>
  );
}

function ResultValue({
  label,
  value,
  copiedValue,
  onCopy
}: {
  label: string;
  value: string;
  copiedValue: string | null;
  onCopy: (value: string) => Promise<void>;
}) {
  return (
    <div>
      <dt>{label}</dt>
      <dd className="platform-copy-value">
        <code>{value}</code>
        <button aria-label={`Copy ${label}`} onClick={() => void onCopy(value)} title={`Copy ${label}`} type="button">
          {copiedValue === value ? <Check aria-hidden="true" size={16} /> : <Clipboard aria-hidden="true" size={16} />}
        </button>
      </dd>
    </div>
  );
}

function PlatformState({
  icon: Icon,
  title,
  body,
  spin = false
}: {
  icon: typeof Building2;
  title: string;
  body: string;
  spin?: boolean;
}) {
  return (
    <main className="platform-state-page">
      <section className="platform-state-panel">
        <Icon aria-hidden="true" className={spin ? "spin" : undefined} size={28} />
        <h1>{title}</h1>
        <p>{body}</p>
        {!spin ? (
          <a href="/platform/tenants/new">
            <RefreshCw aria-hidden="true" size={16} /> Retry
          </a>
        ) : null}
      </section>
    </main>
  );
}

import { ArrowLeft, Building2, LoaderCircle, LockKeyhole, RefreshCw, ShieldAlert } from "lucide-react";
import { useEffect, useState } from "react";
import { PlatformAdminNav } from "./PlatformAdminNav";
import { PlatformCustomerSubscriptionActions } from "./PlatformCustomerSubscriptionActions";
import { formatUsDateTime } from "./lib/dateFormat";
import { getPlatformAccess, getPlatformCustomer, resendPlatformTenantInvitation, type PlatformAccess, type PlatformCustomerDetail } from "./lib/api";
import { getPlatformCustomerTenantId } from "./routing";

export function PlatformCustomerDetailPage() {
  const tenantId = getPlatformCustomerTenantId();
  const [access, setAccess] = useState<PlatformAccess | null>(null);
  const [accessError, setAccessError] = useState("");
  const [detail, setDetail] = useState<PlatformCustomerDetail | null>(null);
  const [state, setState] = useState<"loading" | "ready" | "error">("loading");
  const [error, setError] = useState("");
  const [resendState, setResendState] = useState<"idle" | "sending" | "sent" | "error">("idle");

  useEffect(() => {
    let active = true;
    getPlatformAccess()
      .then((result) => { if (active) setAccess(result); })
      .catch((reason) => { if (active) setAccessError(reason instanceof Error ? reason.message : "Platform access could not be verified."); });
    return () => { active = false; };
  }, []);

  useEffect(() => {
    if (!tenantId || access?.canViewPlatformCustomers !== true) return;
    let active = true;
    getPlatformCustomer(tenantId)
      .then((result) => { if (active) { setDetail(result); setState("ready"); } })
      .catch((reason) => { if (active) { setError(reason instanceof Error ? reason.message : "Customer details could not be loaded."); setState("error"); } });
    return () => { active = false; };
  }, [access?.canViewPlatformCustomers, tenantId]);

  if (!access && !accessError) return <DetailState icon={LoaderCircle} title="Loading customer" body="Verifying operator access." spin />;
  if (accessError) return <DetailState icon={ShieldAlert} title="Platform access unavailable" body={accessError} />;
  if (access?.canViewPlatformCustomers !== true) return <DetailState icon={LockKeyhole} title="Customer access denied" body="Your account does not have the ViewPlatformCustomers permission." />;
  if (!tenantId) return <DetailState icon={ShieldAlert} title="Customer unavailable" body="The customer identifier is invalid." />;
  if (state === "loading") return <DetailState icon={LoaderCircle} title="Loading customer" body="Loading operational metadata." spin />;
  if (state === "error" || !detail) return <DetailState icon={ShieldAlert} title="Customer unavailable" body={error || "The customer could not be loaded."} />;

  const customer = detail.customer;
  const subscription = customer.subscription;
  async function resendInvitation() {
    if (!detail?.invitationId) return;
    setResendState("sending");
    const result = await resendPlatformTenantInvitation(detail.invitationId);
    if (!result.data) { setResendState("error"); return; }
    setDetail((current) => current ? { ...current, customer: { ...current.customer, invitationDeliveryStatus: result.data!.deliveryStatus }, invitationNotificationSentAt: result.data!.notificationSentAt } : current);
    setResendState("sent");
  }

  return (
    <main className="platform-admin-page">
      <PlatformAdminNav access={access} active="customers" />
      <a className="platform-customer-back" href="/platform/customers"><ArrowLeft aria-hidden="true" size={17} /> Back to customers</a>
      <header className="platform-admin-header"><div><p className="platform-admin-kicker">Customer operations</p><h1>{customer.displayName}</h1><p>{customer.customerReference ?? customer.tenantId}</p></div><p className="platform-admin-operator">{customer.customerType ?? "Customer"} · {customer.tenantStatus}</p></header>
      <section className="platform-posture-band" aria-label="Data handling boundary"><ShieldAlert aria-hidden="true" size={20} /><div><strong>{customer.dataPosture}</strong><span>Platform access is limited to operational lifecycle metadata and does not grant access to tenant compliance content.</span></div></section>

      <section className="platform-form-section" aria-labelledby="account-overview-heading"><div className="platform-section-heading"><span>01</span><div><h2 id="account-overview-heading">Account overview</h2><p>Tenant, onboarding, and status metadata.</p></div></div><dl className="platform-result-grid">
        <Value label="Tenant ID" value={customer.tenantId} /><Value label="Customer reference" value={customer.customerReference} /><Value label="Customer type" value={customer.customerType} /><Value label="Tenant status" value={customer.tenantStatus} /><Value label="Onboarding status" value={customer.onboardingStatus} /><Value label="Data posture" value={customer.dataPosture} /><Value label="Setup reason" value={detail.setupReason} /><Value label="Cancelled" value={formatTimestamp(detail.cancelledAt)} /><Value label="Cancellation reason" value={detail.cancellationReason} /><Value label="Created" value={formatTimestamp(customer.createdAt)} /><Value label="Updated" value={formatTimestamp(customer.updatedAt)} />
      </dl></section>

      <section className="platform-form-section" aria-labelledby="owner-heading"><div className="platform-section-heading"><span>02</span><div><h2 id="owner-heading">Primary Owner</h2><p>Initial Owner invitation and activation state.</p></div></div><dl className="platform-result-grid">
        <Value label="Owner" value={detail.ownerDisplayName} /><Value label="Email" value={customer.ownerEmail} /><Value label="Invitation" value={customer.invitationStatus} /><Value label="Delivery" value={customer.invitationDeliveryStatus} /><Value label="Sent" value={formatTimestamp(detail.invitationNotificationSentAt)} /><Value label="Accepted" value={formatTimestamp(detail.invitationAcceptedAt)} /><Value label="Expires" value={formatTimestamp(detail.invitationExpiresAt)} />
      </dl>{access.canManageTenantOnboarding === true && detail.invitationId && customer.invitationStatus === "Pending" ? <button className="platform-secondary-action" disabled={resendState === "sending"} onClick={() => void resendInvitation()} type="button"><RefreshCw aria-hidden="true" size={17} />{resendState === "sending" ? "Requeuing invitation" : resendState === "sent" ? "Invitation requeued" : "Resend invitation"}</button> : null}{resendState === "error" ? <div className="platform-form-error" role="alert">Invitation could not be requeued.</div> : null}</section>

      <section className="platform-form-section" aria-labelledby="subscription-heading"><div className="platform-section-heading"><span>03</span><div><h2 id="subscription-heading">Subscription</h2><p>Provider-independent lifecycle metadata; billing payment is not verified by FeDril.</p></div></div>{subscription ? <dl className="platform-result-grid"><Value label="Plan" value={subscription.plan} /><Value label="Plan code" value={subscription.planCode || detail.planCode} /><Value label="Status" value={subscription.effectiveStatus} /><Value label="Access" value={subscription.accessLevel} /><Value label="Starts" value={formatTimestamp(subscription.startsAt)} /><Value label="Ends" value={formatTimestamp(subscription.endsAt)} /><Value label="Grace ends" value={formatTimestamp(subscription.graceEndsAt)} /><Value label="Customer reference" value={subscription.externalCustomerReference} /><Value label="Subscription reference" value={subscription.externalSubscriptionReference || detail.subscriptionReference} /><Value label="Status reason" value={subscription.statusReason} /><Value label="Version" value={String(subscription.version)} /></dl> : <div className="platform-pending-state">No subscription record is available.</div>}</section>

      {access.canManageTenantSubscriptions === true && subscription?.plan === "PilotEvaluation" && !["Cancelled", "Converted"].includes(subscription.status) ? <PlatformCustomerSubscriptionActions customerName={customer.displayName} customerReference={customer.customerReference} tenantId={customer.tenantId} subscription={subscription} onChanged={(next) => setDetail((current) => current ? { ...current, customer: { ...current.customer, subscription: next } } : current)} /> : null}

      <section className="platform-form-section" aria-labelledby="lifecycle-heading"><div className="platform-section-heading"><span>05</span><div><h2 id="lifecycle-heading">Lifecycle history</h2><p>Platform onboarding and subscription events only.</p></div></div>{detail.lifecycle.length ? <ol className="platform-customer-timeline">{detail.lifecycle.map((item, index) => <li key={`${item.eventType}-${item.occurredAt}-${index}`}><strong>{item.summary}</strong><span>{formatTimestamp(item.occurredAt)}</span></li>)}</ol> : <div className="platform-pending-state">No lifecycle events are available.</div>}</section>
    </main>
  );
}

function Value({ label, value }: { label: string; value: string | null | undefined }) { return <div><dt>{label}</dt><dd>{value || "—"}</dd></div>; }
function formatTimestamp(value: string | null | undefined) { return formatUsDateTime(value); }
function DetailState({ body, icon: Icon, spin, title }: { body: string; icon: typeof Building2; spin?: boolean; title: string }) { return <main className="platform-console-state"><Icon aria-hidden="true" className={spin ? "spin" : undefined} size={32} /><h1>{title}</h1><p>{body}</p><a href="/platform/customers">Return to customers</a></main>; }

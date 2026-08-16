import { Building2, ChevronLeft, ChevronRight, LoaderCircle, LockKeyhole, Search, ShieldAlert } from "lucide-react";
import { useEffect, useState, type FormEvent } from "react";
import { PlatformAdminNav } from "./PlatformAdminNav";
import { formatUsDateOnly } from "./lib/dateFormat";
import {
  getPlatformAccess,
  getPlatformCustomers,
  type PlatformAccess,
  type PlatformCustomerAttention,
  type PlatformCustomerPage,
  type PlatformCustomerQuery
} from "./lib/api";

const emptyFilters: PlatformCustomerQuery = { page: 1, pageSize: 25, sort: "UpdatedDescending" };

export function PlatformCustomersPage() {
  const [access, setAccess] = useState<PlatformAccess | null>(null);
  const [accessError, setAccessError] = useState("");
  const [data, setData] = useState<PlatformCustomerPage | null>(null);
  const [state, setState] = useState<"loading" | "ready" | "error">("loading");
  const [error, setError] = useState("");
  const [filters, setFilters] = useState<PlatformCustomerQuery>(emptyFilters);
  const [search, setSearch] = useState("");

  useEffect(() => {
    let active = true;
    getPlatformAccess()
      .then((result) => { if (active) setAccess(result); })
      .catch((reason) => { if (active) setAccessError(reason instanceof Error ? reason.message : "Platform access could not be verified."); });
    return () => { active = false; };
  }, []);

  useEffect(() => {
    if (access?.canViewPlatformCustomers !== true) return;
    let active = true;
    getPlatformCustomers(filters)
      .then((result) => {
        if (!active) return;
        if (result.items.length === 0 && result.page > 1 && result.totalCount > 0) {
          setFilters((current) => ({ ...current, page: result.page - 1 }));
          return;
        }
        setData(result);
        setState("ready");
      })
      .catch((reason) => {
        if (!active) return;
        setError(reason instanceof Error ? reason.message : "Customers could not be loaded.");
        setState("error");
      });
    return () => { active = false; };
  }, [access?.canViewPlatformCustomers, filters]);

  if (!access && !accessError) return <CustomerState icon={LoaderCircle} title="Loading customer operations" body="Verifying operator access." spin />;
  if (accessError) return <CustomerState icon={ShieldAlert} title="Platform access unavailable" body={accessError} />;
  if (access?.canViewPlatformCustomers !== true) return <CustomerState icon={LockKeyhole} title="Customer access denied" body="Your account does not have the ViewPlatformCustomers permission." />;

  function applySearch(event: FormEvent) {
    event.preventDefault();
    setState("loading");
    setError("");
    setFilters((current) => ({ ...current, page: 1, search: search.trim() || undefined }));
  }

  function updateFilter<K extends keyof PlatformCustomerQuery>(key: K, value: PlatformCustomerQuery[K]) {
    setState("loading");
    setError("");
    setFilters((current) => ({ ...current, page: 1, [key]: value || undefined }));
  }

  function updatePage(page: number) {
    setState("loading");
    setError("");
    setFilters((current) => ({ ...current, page }));
  }

  return (
    <main className="platform-admin-page">
      <PlatformAdminNav access={access} active="customers" />
      <header className="platform-admin-header">
        <div><p className="platform-admin-kicker">FeDril platform operations</p><h1>Customers</h1><p>Track Pilot and Paid customer lifecycle metadata without opening tenant compliance content.</p></div>
        <p className="platform-admin-operator">Signed in as {access.userEmail ?? access.userId}</p>
      </header>
      <section className="platform-posture-band" aria-label="Data handling boundary">
        <ShieldAlert aria-hidden="true" size={20} />
        <div><strong>Operational metadata only</strong><span>This directory does not expose customer evidence, contracts, reports, workspace audit contents, or invitation tokens.</span></div>
      </section>

      <section className="platform-form-section" aria-labelledby="customer-filters-heading">
        <div className="platform-section-heading"><span>01</span><div><h2 id="customer-filters-heading">Find customers</h2><p>Search and filter on the server. Results are bounded to 25 records per page.</p></div></div>
        <form className="platform-customer-filters" onSubmit={applySearch}>
          <label><span>Search</span><div className="platform-customer-search"><input maxLength={320} onChange={(event) => setSearch(event.target.value)} placeholder="Name, reference, owner, or subscription prefix" value={search} /><button type="submit" aria-label="Search customers"><Search aria-hidden="true" size={17} /></button></div></label>
          <label><span>Type</span><select value={filters.customerType ?? ""} onChange={(event) => updateFilter("customerType", event.target.value as PlatformCustomerQuery["customerType"])}><option value="">All</option><option value="Pilot">Pilot</option><option value="Paid">Paid</option></select></label>
          <label><span>Tenant status</span><select value={filters.tenantStatus ?? ""} onChange={(event) => updateFilter("tenantStatus", event.target.value)}><option value="">All</option><option>PendingActivation</option><option>Active</option><option>Trialing</option><option>Suspended</option><option>Archived</option></select></label>
          <label><span>Onboarding</span><select value={filters.onboardingStatus ?? ""} onChange={(event) => updateFilter("onboardingStatus", event.target.value)}><option value="">All</option><option>PendingOwnerAcceptance</option><option>Active</option><option>InvitationDeliveryFailed</option><option>Cancelled</option></select></label>
          <label><span>Subscription</span><select value={filters.subscriptionStatus ?? ""} onChange={(event) => updateFilter("subscriptionStatus", event.target.value)}><option value="">All</option><option>Pending</option><option>Active</option><option>GracePeriod</option><option>Expired</option><option>Cancelled</option><option>Converted</option></select></label>
          <label><span>Attention</span><select value={filters.attention ?? ""} onChange={(event) => updateFilter("attention", event.target.value as PlatformCustomerAttention)}><option value="">All</option><option value="PendingOwnerAcceptance">Pending Owner</option><option value="InvitationDeliveryFailed">Invitation failed</option><option value="PilotExpiring">Pilot expiring</option><option value="GracePeriod">Grace period</option><option value="Expired">Expired</option><option value="SubscriptionMissing">Subscription missing</option></select></label>
          <label><span>Sort</span><select value={filters.sort ?? "UpdatedDescending"} onChange={(event) => updateFilter("sort", event.target.value as PlatformCustomerQuery["sort"])}><option value="UpdatedDescending">Recently updated</option><option value="NameAscending">Name</option><option value="CreatedDescending">Recently created</option><option value="PilotEndAscending">Pilot end date</option></select></label>
        </form>
      </section>

      <section className="platform-pending" aria-labelledby="customer-results-heading">
        <div className="platform-pending-heading"><div><p>Customer operations</p><h2 id="customer-results-heading">Customer directory</h2></div>{data ? <span>{data.totalCount} customers</span> : null}</div>
        {state === "loading" ? <div className="platform-pending-state"><LoaderCircle aria-hidden="true" className="spin" size={18} /> Loading customers</div> : null}
        {state === "error" ? <div className="platform-form-error" role="alert"><ShieldAlert aria-hidden="true" size={18} /><span>{error}</span></div> : null}
        {state === "ready" && data?.items.length === 0 ? <div className="platform-pending-state">No customers match the selected filters.</div> : null}
        {state === "ready" && data && data.items.length > 0 ? (
          <>
            <div className="platform-pending-table-wrap"><table className="platform-pending-table platform-customer-table"><thead><tr><th>Customer</th><th>Type</th><th>Tenant</th><th>Subscription</th><th>Owner</th><th>Invitation</th><th>Pilot end</th><th>Attention</th><th>Updated</th></tr></thead><tbody>{data.items.map((customer) => (
              <tr key={customer.tenantId}>
                <td data-label="Customer"><a href={`/platform/customers/${customer.tenantId}`}><strong>{customer.displayName}</strong></a><span>{customer.customerReference ?? customer.tenantId}</span></td>
                <td data-label="Type">{customer.customerType ?? "—"}</td>
                <td data-label="Tenant">{customer.tenantStatus}</td>
                <td data-label="Subscription">{customer.subscription?.effectiveStatus ?? "Missing"}<span>{customer.subscription?.planCode ?? "—"}</span></td>
                <td data-label="Owner">{customer.ownerEmail ?? "—"}</td>
                <td data-label="Invitation">{customer.invitationStatus ?? "—"}<span>{customer.invitationDeliveryStatus ?? "—"}</span></td>
                <td data-label="Pilot end">{formatDate(customer.subscription?.endsAt)}</td>
                <td data-label="Attention">{customer.attention.length ? customer.attention.map(attentionLabel).join(", ") : "None"}</td>
                <td data-label="Updated">{formatDate(customer.updatedAt)}</td>
              </tr>
            ))}</tbody></table></div>
            <div className="platform-pending-pagination"><button aria-label="Previous customer page" disabled={!data.hasPreviousPage} onClick={() => updatePage(data.page - 1)} type="button"><ChevronLeft aria-hidden="true" size={17} /></button><span>Page {data.page}</span><button aria-label="Next customer page" disabled={!data.hasNextPage} onClick={() => updatePage(data.page + 1)} type="button"><ChevronRight aria-hidden="true" size={17} /></button></div>
          </>
        ) : null}
      </section>
    </main>
  );
}

function formatDate(value: string | null | undefined) {
  return formatUsDateOnly(value);
}

function attentionLabel(value: PlatformCustomerAttention) {
  return ({ PendingOwnerAcceptance: "Pending Owner", InvitationDeliveryFailed: "Invitation failed", PilotExpiring: "Pilot expiring", GracePeriod: "Grace period", Expired: "Expired", SubscriptionMissing: "Subscription missing" })[value];
}

function CustomerState({ body, icon: Icon, spin, title }: { body: string; icon: typeof Building2; spin?: boolean; title: string }) {
  return <main className="platform-console-state"><Icon aria-hidden="true" className={spin ? "spin" : undefined} size={32} /><h1>{title}</h1><p>{body}</p><a href="/platform">Return to platform overview</a></main>;
}

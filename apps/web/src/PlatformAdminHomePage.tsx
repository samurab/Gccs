import { ArrowRight, Building2, Inbox, LoaderCircle, LockKeyhole, ShieldAlert } from "lucide-react";
import { useEffect, useState } from "react";
import { PlatformAdminNav } from "./PlatformAdminNav";
import {
  getPlatformAccess,
  getPlatformDemoRequests,
  getPlatformTenantOnboardings,
  type PlatformAccess
} from "./lib/api";

type Metric = { count: number | null; error: string };

const emptyMetric: Metric = { count: null, error: "" };

export function PlatformAdminHomePage() {
  const [access, setAccess] = useState<PlatformAccess | null>(null);
  const [accessError, setAccessError] = useState("");
  const [demoRequests, setDemoRequests] = useState<Metric>(emptyMetric);
  const [pendingOnboardings, setPendingOnboardings] = useState<Metric>(emptyMetric);

  useEffect(() => {
    let active = true;

    getPlatformAccess()
      .then(async (nextAccess) => {
        if (!active) return;
        setAccess(nextAccess);

        const tasks: Promise<void>[] = [];
        if (nextAccess.canManageDemoRequests) {
          tasks.push(
            getPlatformDemoRequests(1, 5)
              .then((page) => { if (active) setDemoRequests({ count: page.totalCount, error: "" }); })
              .catch((error) => { if (active) setDemoRequests({ count: null, error: error instanceof Error ? error.message : "Demo requests could not be loaded." }); })
          );
        }
        if (nextAccess.canProvisionTenants) {
          tasks.push(
            getPlatformTenantOnboardings(1, 5, "PendingOwnerAcceptance")
              .then((page) => { if (active) setPendingOnboardings({ count: page.totalCount, error: "" }); })
              .catch((error) => { if (active) setPendingOnboardings({ count: null, error: error instanceof Error ? error.message : "Pending onboardings could not be loaded." }); })
          );
        }
        await Promise.all(tasks);
      })
      .catch((error) => {
        if (active) setAccessError(error instanceof Error ? error.message : "Platform access could not be verified.");
      });

    return () => { active = false; };
  }, []);

  if (!access && !accessError) {
    return <PlatformConsoleState icon={LoaderCircle} title="Loading platform operations" body="Verifying operator access." spin />;
  }

  if (accessError) {
    return <PlatformConsoleState icon={ShieldAlert} title="Platform access unavailable" body={accessError} />;
  }

  if (!access || (!access.canManageDemoRequests && !access.canProvisionTenants)) {
    return <PlatformConsoleState icon={LockKeyhole} title="Platform access denied" body="Your account has no platform-operations permissions." />;
  }

  return (
    <main className="platform-console-page">
      <PlatformAdminNav access={access} active="overview" />
      <header className="platform-console-hero">
        <div>
          <p className="platform-admin-kicker">FeDril platform operations</p>
          <h1>Admin overview</h1>
          <p>Review public demo intake and manage pending tenant onboarding from one permission-aware workspace.</p>
        </div>
        <p className="platform-console-operator">Signed in as <strong>{access.userEmail ?? access.userId}</strong></p>
      </header>

      <section className="platform-posture-band" aria-label="Data handling boundary">
        <ShieldAlert aria-hidden="true" size={20} />
        <div><strong>No-CUI product boundary</strong><span>Platform operations do not authorize CUI, classified, ITAR, or sensitive government data.</span></div>
      </section>

      <section aria-label="Available platform operations" className="platform-console-grid">
        {access.canManageDemoRequests ? (
          <OperationCard
            count={demoRequests.count}
            description="Review requester details, preferred demo times, delivery state, and queue a server-owned response template."
            error={demoRequests.error}
            href="/platform/demo-requests"
            icon={Inbox}
            label="requests captured"
            title="Demo requests"
          />
        ) : null}
        {access.canProvisionTenants ? (
          <OperationCard
            count={pendingOnboardings.count}
            description="Create pilot or paid pending tenants, resend owner invitations, and cancel incomplete onboarding."
            error={pendingOnboardings.error}
            href="/platform/tenants/new"
            icon={Building2}
            label="pending owner acceptance"
            title="Tenant onboarding"
          />
        ) : null}
      </section>

      <aside className="platform-console-scope">
        <strong>Implemented scope</strong>
        <p>This console exposes only operations currently enforced by platform APIs and your assigned permissions. Tenant administration after activation remains in the tenant workspace.</p>
      </aside>
    </main>
  );
}

function OperationCard({ count, description, error, href, icon: Icon, label, title }: {
  count: number | null;
  description: string;
  error: string;
  href: string;
  icon: typeof Inbox;
  label: string;
  title: string;
}) {
  return (
    <article className="platform-console-card">
      <div className="platform-console-card-icon"><Icon aria-hidden="true" size={24} /></div>
      <div>
        <h2>{title}</h2>
        {error ? <p className="form-status form-status--error" role="alert">{error}</p> : <p className="platform-console-metric"><strong>{count ?? "—"}</strong> {count === null ? "Loading…" : label}</p>}
        <p>{description}</p>
      </div>
      <a href={href}>Open {title.toLowerCase()} <ArrowRight aria-hidden="true" size={17} /></a>
    </article>
  );
}

function PlatformConsoleState({ body, icon: Icon, spin, title }: { body: string; icon: typeof Inbox; spin?: boolean; title: string }) {
  return (
    <main className="platform-console-state">
      <Icon aria-hidden="true" className={spin ? "spin" : undefined} size={32} />
      <h1>{title}</h1>
      <p>{body}</p>
      <a href="/app">Return to workspace</a>
    </main>
  );
}

import {
  Archive,
  Building2,
  CalendarClock,
  ClipboardCheck,
  FileSearch,
  FolderKanban,
  GitBranch,
  ShieldCheck,
  UsersRound
} from "lucide-react";
import { useEffect, useState } from "react";
import { ModuleCard } from "@/components/ModuleCard";
import { fallbackOverview, getComplianceOverview, getTenantMembers, type TenantMember } from "@/lib/api";

const moduleIcons = [Building2, FileSearch, ClipboardCheck, CalendarClock, Archive, ShieldCheck, GitBranch, FolderKanban];

export function App() {
  const [overview, setOverview] = useState(fallbackOverview);
  const [members, setMembers] = useState<TenantMember[]>([]);
  const hasModules = overview.modules.length > 0;
  const hasPriorityObligations = overview.priorityObligations.length > 0;
  const hasMembers = members.length > 0;

  useEffect(() => {
    let isMounted = true;

    Promise.all([getComplianceOverview(), getTenantMembers()]).then(([nextOverview, nextMembers]) => {
      if (isMounted) {
        setOverview(nextOverview);
        setMembers(nextMembers);
      }
    });

    return () => {
      isMounted = false;
    };
  }, []);

  return (
    <main>
      <section className="workspace-hero">
        <div className="workspace-hero__content">
          <div>
            <p className="eyebrow">GCCS Compliance Workspace</p>
            <h1>Govcon obligations, evidence, and readiness in one operating view.</h1>
            <p className="hero-copy">{overview.productPromise}</p>
          </div>
          <div className="hero-panel" aria-label="MVP platform posture">
            <span>Current data posture</span>
            <strong>{overview.mvpDataPosture}</strong>
          </div>
        </div>
      </section>

      <section className="overview-band">
        <div className="section-shell metrics-grid">
          <div className="metric">
            <span>Priority obligations</span>
            <strong>{overview.priorityObligations.length}</strong>
          </div>
          <div className="metric">
            <span>MVP modules</span>
            <strong>{overview.modules.length}</strong>
          </div>
          <div className="metric">
            <span>Evidence posture</span>
            <strong>No-CUI</strong>
          </div>
          <div className="metric">
            <span>Source status</span>
            <strong>Seeded</strong>
          </div>
        </div>
      </section>

      <section className="section-shell work-grid" aria-label="Compliance operations">
        <div>
          <div className="section-heading">
            <p className="eyebrow">MVP modules</p>
            <h2>Application structure</h2>
          </div>
          <div className="module-grid">
            {hasModules ? (
              overview.modules.map((module, index) => {
                const Icon = moduleIcons[index % moduleIcons.length];
                return <ModuleCard key={module.key} module={module} icon={Icon} />;
              })
            ) : (
              <div className="empty-state">
                <h3>API overview unavailable</h3>
                <p>Backend source data must load before module status is shown.</p>
              </div>
            )}
          </div>
        </div>

        <aside className="obligation-rail" aria-label="Priority source-backed obligations">
          <div className="section-heading">
            <p className="eyebrow">Source-backed</p>
            <h2>Priority obligations</h2>
          </div>
          <div className="obligation-list">
            {hasPriorityObligations ? (
              overview.priorityObligations.map((obligation) => (
                <article key={obligation.id} className="obligation-item">
                  <div>
                    <span className={`risk risk--${obligation.riskLevel.toLowerCase()}`}>{obligation.riskLevel}</span>
                    <h3>{obligation.source}</h3>
                  </div>
                  <p>{obligation.title}</p>
                  <dl>
                    <div>
                      <dt>Owner</dt>
                      <dd>{obligation.ownerFunction}</dd>
                    </div>
                    <div>
                      <dt>Reviewed</dt>
                      <dd>{obligation.lastReviewedAt}</dd>
                    </div>
                  </dl>
                </article>
              ))
            ) : (
              <div className="empty-state">
                <h3>Source data unavailable</h3>
                <p>Priority obligations are provided by the API, not by UI-only fallback content.</p>
              </div>
            )}
          </div>
        </aside>
      </section>

      <section className="section-shell members-section" aria-label="Tenant team members">
        <div className="section-heading">
          <p className="eyebrow">Tenant access</p>
          <h2>Team members</h2>
        </div>
        {hasMembers ? (
          <div className="member-table" role="table" aria-label="Current tenant members">
            <div className="member-row member-row--header" role="row">
              <span role="columnheader">Member</span>
              <span role="columnheader">Role</span>
              <span role="columnheader">Status</span>
              <span role="columnheader">MFA</span>
            </div>
            {members.map((member) => (
              <article className="member-row" role="row" key={member.membershipId}>
                <span className="member-person" role="cell">
                  <span className="icon-box icon-box--small" aria-hidden="true">
                    <UsersRound size={17} />
                  </span>
                  <span>
                    <strong>{member.displayName}</strong>
                    <small>{member.email}</small>
                  </span>
                </span>
                <span role="cell">{member.roleName}</span>
                <span role="cell">
                  <span className={`status status--${member.membershipStatus.toLowerCase()}`}>
                    {member.membershipStatus}
                  </span>
                </span>
                <span role="cell">{member.mfaEnabled ? "Enabled" : "Not enabled"}</span>
              </article>
            ))}
          </div>
        ) : (
          <div className="empty-state">
            <h3>No tenant members available</h3>
            <p>Team membership is loaded from the active tenant context.</p>
          </div>
        )}
      </section>
    </main>
  );
}

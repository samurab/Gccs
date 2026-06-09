import {
  Archive,
  Building2,
  CalendarClock,
  ClipboardCheck,
  FileSearch,
  FolderKanban,
  GitBranch,
  ShieldCheck
} from "lucide-react";
import { ModuleCard } from "@/components/ModuleCard";
import { getComplianceOverview } from "@/lib/api";

const moduleIcons = [Building2, FileSearch, ClipboardCheck, CalendarClock, Archive, ShieldCheck, GitBranch, FolderKanban];

export default async function HomePage() {
  const overview = await getComplianceOverview();

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
            {overview.modules.map((module, index) => {
              const Icon = moduleIcons[index % moduleIcons.length];
              return <ModuleCard key={module.key} module={module} icon={Icon} />;
            })}
          </div>
        </div>

        <aside className="obligation-rail" aria-label="Priority source-backed obligations">
          <div className="section-heading">
            <p className="eyebrow">Source-backed</p>
            <h2>Priority obligations</h2>
          </div>
          <div className="obligation-list">
            {overview.priorityObligations.map((obligation) => (
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
            ))}
          </div>
        </aside>
      </section>
    </main>
  );
}

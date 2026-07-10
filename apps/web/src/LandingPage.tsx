import {
  ArrowRight,
  CalendarClock,
  CheckCircle2,
  ClipboardCheck,
  DatabaseZap,
  FileCheck2,
  FileText,
  FolderSearch,
  Gauge,
  GitBranch,
  Layers3,
  LockKeyhole,
  SearchCheck,
  ShieldCheck,
  Sparkles,
  Target,
  UsersRound
} from "lucide-react";
import { useEffect } from "react";

const workflowSteps = [
  {
    icon: <FileText size={21} />,
    title: "Model the workflow",
    body: "Capture company profile details, contract metadata, manual clause tags, and source-backed obligation records."
  },
  {
    icon: <ClipboardCheck size={21} />,
    title: "Assign the work",
    body: "Turn obligations into owned tasks with due dates, risk labels, evidence expectations, and readiness status."
  },
  {
    icon: <FileCheck2 size={21} />,
    title: "Produce the report",
    body: "Export readiness-oriented reports that show obligation status, evidence metadata, owners, and audit history."
  }
];

const proofPoints = [
  "Source-backed obligations",
  "Evidence metadata tracking",
  "Task ownership and due dates",
  "CMMC readiness workflows",
  "Subcontractor flow-down tracking",
  "Audit history and reports"
];

const pageTabs = [
  { label: "Platform", href: "#platform" },
  { label: "Workflow", href: "#workflow" },
  { label: "Pilot", href: "#pilot" },
  { label: "Security", href: "#security" },
  { label: "SEO", href: "#seo" }
];

const audienceCards = [
  {
    icon: <UsersRound size={20} />,
    title: "Owners and operators",
    body: "See obligations, evidence gaps, owners, and readiness status without managing another spreadsheet."
  },
  {
    icon: <CalendarClock size={20} />,
    title: "Contracts and compliance leads",
    body: "Turn contract requirements into tracked obligations, reminders, evidence records, and status reports."
  },
  {
    icon: <ShieldCheck size={20} />,
    title: "Security and MSP teams",
    body: "Track CMMC-related readiness work while preserving a No-CUI compliance management posture."
  }
];

const featureTiles = [
  {
    icon: <SearchCheck size={20} />,
    title: "Obligation intake",
    body: "Capture contract context and reviewed obligation records without claiming automated legal interpretation."
  },
  {
    icon: <GitBranch size={20} />,
    title: "Evidence workflow",
    body: "Route tasks from owner assignment to metadata review, report readiness, and audit history."
  },
  {
    icon: <Gauge size={20} />,
    title: "Readiness view",
    body: "Show gaps, due dates, risk labels, and status trends in a pilot-friendly operating dashboard."
  },
  {
    icon: <DatabaseZap size={20} />,
    title: "Report package",
    body: "Export readiness artifacts that preserve source references, review state, and No-CUI boundaries."
  }
];

export function LandingPage() {
  useEffect(() => {
    document.title = "GCCS | GovCon Compliance Readiness Software";

    const description =
      "GCCS is a No-CUI compliance management workspace for small government contractors to track obligations, evidence metadata, readiness workflows, and reports.";
    let metaDescription = document.querySelector<HTMLMetaElement>('meta[name="description"]');

    if (!metaDescription) {
      metaDescription = document.createElement("meta");
      metaDescription.name = "description";
      document.head.append(metaDescription);
    }

    metaDescription.content = description;
  }, []);

  return (
    <main className="landing-page">
      <section className="landing-hero" aria-label="GCCS landing page">
        <div className="landing-nav" aria-label="Primary">
          <a className="landing-brand" href="/landing" aria-label="GCCS landing page">
            <span className="landing-brand__mark" aria-hidden="true">
              <ShieldCheck size={22} />
            </span>
            <span>
              <strong>GCCS</strong>
              <small>GovCon compliance workspace</small>
            </span>
          </a>
          <div className="landing-nav__links">
            <a href="#platform">Platform</a>
            <a href="#workflow">Workflow</a>
            <a href="#pilot">Pilot</a>
            <a href="#security">Data posture</a>
          </div>
        </div>

        <nav className="landing-tabs" aria-label="Landing page sections">
          {pageTabs.map((tab) => (
            <a href={tab.href} key={tab.href}>
              {tab.label}
            </a>
          ))}
        </nav>

        <div className="landing-hero__content">
          <div className="landing-hero__copy">
            <p className="landing-eyebrow">No-CUI compliance management for small GovCon teams</p>
            <h1>Compliance readiness tracking for small government contractors.</h1>
            <p className="landing-hero__lede">
              GCCS helps small GovCon teams organize source-backed obligations, assign compliance work, track evidence metadata,
              monitor readiness, and generate reports under a No-CUI compliance management posture.
            </p>
            <div className="landing-actions">
              <a className="landing-button landing-button--primary" href="mailto:hello@gccs.example?subject=GCCS%20pilot%20demo">
                <span>Request a pilot demo</span>
                <ArrowRight size={18} />
              </a>
              <a className="landing-button landing-button--secondary" href="#security">
                <LockKeyhole size={18} />
                <span>View No-CUI policy</span>
              </a>
            </div>
            <p className="landing-disclaimer">
              GCCS does not certify compliance, provide legal advice, or accept real CUI in the MVP.
            </p>
          </div>

          <div className="landing-product" aria-label="GCCS product preview">
            <div className="landing-product__halo" aria-hidden="true" />
            <div className="landing-product__bar">
              <span>GCCS workspace</span>
              <strong>No-CUI</strong>
            </div>
            <div className="landing-product__grid">
              <div className="landing-product__metric">
                <span>Readiness</span>
                <strong>72%</strong>
                <small>Internal workflow view</small>
              </div>
              <div className="landing-product__metric">
                <span>Open obligations</span>
                <strong>24</strong>
                <small>6 high priority</small>
              </div>
              <div className="landing-product__metric">
                <span>Evidence gaps</span>
                <strong>11</strong>
                <small>Metadata only</small>
              </div>
            </div>
            <div className="landing-product__workflow">
              {["Contract intake", "Obligation review", "Evidence ready", "Report export"].map((label, index) => (
                <div className="landing-product__step" key={label}>
                  <span>{index + 1}</span>
                  <p>{label}</p>
                </div>
              ))}
            </div>
            <div className="landing-product__signal" aria-label="Readiness signal preview">
              <div className="landing-signal-ring" aria-hidden="true">
                <span />
                <strong>72%</strong>
              </div>
              <div>
                <p>Sample readiness signal</p>
                <small>Combines obligation status, owner coverage, evidence metadata, and unresolved gaps.</small>
              </div>
            </div>
            <div className="landing-product__table" role="table" aria-label="Sample obligation status">
              <div role="row">
                <span role="columnheader">Obligation</span>
                <span role="columnheader">Owner</span>
                <span role="columnheader">Status</span>
              </div>
              <div role="row">
                <span role="cell">Access review</span>
                <span role="cell">Security</span>
                <span role="cell">On track</span>
              </div>
              <div role="row">
                <span role="cell">Supplier flow-down</span>
                <span role="cell">Contracts</span>
                <span role="cell">Needs evidence</span>
              </div>
              <div role="row">
                <span role="cell">Training attestation</span>
                <span role="cell">Ops</span>
                <span role="cell">Due soon</span>
              </div>
            </div>
          </div>
        </div>
      </section>

      <section className="landing-section landing-section--tight" aria-label="Product proof points">
        <div className="landing-proof-strip">
          {proofPoints.map((point) => (
            <span key={point}>
              <CheckCircle2 size={17} />
              {point}
            </span>
          ))}
        </div>
      </section>

      <section className="landing-section landing-platform" id="platform">
        <div className="landing-section__heading">
          <p className="landing-eyebrow">Visual compliance operations</p>
          <h2>A lightweight workspace for readiness work that has to be tracked, owned, and reported.</h2>
        </div>
        <div className="landing-platform__layout">
          <div className="landing-feature-grid">
            {featureTiles.map((feature) => (
              <article className="landing-feature" key={feature.title}>
                <span className="landing-card__icon" aria-hidden="true">
                  {feature.icon}
                </span>
                <h3>{feature.title}</h3>
                <p>{feature.body}</p>
              </article>
            ))}
          </div>
          <div className="landing-map" aria-label="GCCS readiness map graphic">
            <div className="landing-map__node landing-map__node--primary">
              <Layers3 size={22} />
              <strong>Control workspace</strong>
              <span>Obligations, owners, evidence metadata</span>
            </div>
            <div className="landing-map__rail" aria-hidden="true" />
            {["Contract", "Obligation", "Evidence", "Report"].map((item, index) => (
              <div className="landing-map__node" key={item}>
                <span>{`0${index + 1}`}</span>
                <strong>{item}</strong>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="landing-section landing-section--visual" id="workflow">
        <div className="landing-section__heading">
          <p className="landing-eyebrow">From scattered trackers to reportable work</p>
          <h2>Know what applies. Track what was done. Stay ready to prove it.</h2>
          <p>
            The first GCCS workflow is intentionally concrete: one company profile, one contract or synthetic workflow,
            reviewed obligations, evidence metadata, and a readiness report.
          </p>
        </div>
        <div className="landing-card-grid landing-card-grid--steps">
          {workflowSteps.map((step) => (
            <article className="landing-card" key={step.title}>
              <span className="landing-card__icon" aria-hidden="true">
                {step.icon}
              </span>
              <h3>{step.title}</h3>
              <p>{step.body}</p>
            </article>
          ))}
        </div>
        <div className="landing-flow-graphic" aria-label="Sample obligation and evidence workflow graphic">
          <div className="landing-flow-graphic__header">
            <span>Sample workflow</span>
            <strong>Evidence metadata only</strong>
          </div>
          <div className="landing-flow-graphic__lanes">
            {[
              ["Intake", "Contract profile", "Manual clause tags"],
              ["Review", "Source-backed obligation", "Qualified review state"],
              ["Execute", "Owner task", "Due date + risk label"],
              ["Report", "Readiness artifact", "Audit history"]
            ].map(([stage, first, second]) => (
              <div className="landing-flow-graphic__lane" key={stage}>
                <span>{stage}</span>
                <p>{first}</p>
                <p>{second}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="landing-section landing-section--split" aria-label="Buyer fit">
        <div className="landing-section__heading">
          <p className="landing-eyebrow">Built for early pilot workflows</p>
          <h2>Focused on small contractor readiness work, not enterprise GRC overhead.</h2>
        </div>
        <div className="landing-card-grid">
          {audienceCards.map((card) => (
            <article className="landing-card landing-card--compact" key={card.title}>
              <span className="landing-card__icon" aria-hidden="true">
                {card.icon}
              </span>
              <h3>{card.title}</h3>
              <p>{card.body}</p>
            </article>
          ))}
        </div>
      </section>

      <section className="landing-section landing-security" id="security">
        <div className="landing-section__heading">
          <p className="landing-eyebrow">Security and data-handling boundary</p>
          <h2>No-CUI / compliance management only.</h2>
          <p>
            The MVP is designed for synthetic, redacted, and non-sensitive records. Real CUI, classified information,
            ITAR/export-controlled data, sensitive government-furnished information, credentials, payroll records, SSNs,
            health data, and sensitive incident details are prohibited.
          </p>
        </div>
        <div className="landing-security__panel">
          <div>
            <LockKeyhole size={24} />
            <h3>What GCCS tracks</h3>
            <p>Obligation status, task ownership, evidence metadata, source references, review state, and audit history.</p>
          </div>
          <div>
            <FolderSearch size={24} />
            <h3>What requires review</h3>
            <p>Compliance content, customer-facing claims, report language, and any future CUI-ready operating posture.</p>
          </div>
        </div>
        <div className="landing-boundary">
          <div className="landing-boundary__item landing-boundary__item--allowed">
            <CheckCircle2 size={19} />
            <span>Allowed: redacted records, synthetic examples, metadata, status, source references</span>
          </div>
          <div className="landing-boundary__item landing-boundary__item--blocked">
            <LockKeyhole size={19} />
            <span>Blocked: real CUI, classified data, ITAR/export-controlled data, sensitive GFI</span>
          </div>
        </div>
      </section>

      <section className="landing-section landing-pilot" id="pilot">
        <div className="landing-pilot__copy">
          <p className="landing-eyebrow">Founder-friendly pilot</p>
          <h2>30-day guided readiness pilot.</h2>
          <p>
            Set up a company profile, model one workflow, tag obligations manually, assign owners, attach allowed evidence
            metadata, and generate a sample readiness report.
          </p>
        </div>
        <div className="landing-price" aria-label="Founder pilot pricing">
          <div className="landing-price__badge">
            <Sparkles size={18} />
            <span>Early pilot</span>
          </div>
          <span>Founder pilot</span>
          <strong>$500-$1,500</strong>
          <p>Flat fee hypothesis. Credit toward annual subscription if converted.</p>
          <a className="landing-button landing-button--primary" href="mailto:hello@gccs.example?subject=GCCS%20founder%20pilot">
            <span>Discuss pilot fit</span>
            <ArrowRight size={18} />
          </a>
        </div>
      </section>

      <section className="landing-section landing-seo" id="seo">
        <div className="landing-section__heading">
          <p className="landing-eyebrow">SEO-ready content structure</p>
          <h2>Built as a public page that can grow into a searchable marketing site.</h2>
          <p>
            The page now has clear section anchors, descriptive headings, source-safe product language, and a focused meta
            description for GovCon compliance readiness searches.
          </p>
        </div>
        <div className="landing-seo__grid" aria-label="SEO content pillars">
          {[
            ["Government contractor compliance software", "No-CUI readiness workspace for small businesses"],
            ["CMMC readiness workflow", "Track tasks, owners, evidence metadata, and reporting"],
            ["NIST 800-171 obligation tracking", "Source-backed records with review state and provenance"]
          ].map(([term, detail]) => (
            <div className="landing-seo__pillar" key={term}>
              <Target size={18} />
              <strong>{term}</strong>
              <span>{detail}</span>
            </div>
          ))}
        </div>
      </section>

      <footer className="landing-footer">
        <strong>GCCS</strong>
        <p>
          Reports are readiness artifacts and workflow guidance, not legal advice, certification decisions, government
          determinations, or substitutes for qualified expert review.
        </p>
      </footer>
    </main>
  );
}

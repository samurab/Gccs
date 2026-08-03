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
  UsersRound,
  Volume2
} from "lucide-react";
import { useEffect, useRef, useState } from "react";
import { DemoRequestButton } from "./DemoRequestButton";

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
  "Implemented: tenant-scoped workspace",
  "Implemented: evidence metadata",
  "Implemented: task ownership",
  "Implemented: audit history",
  "Implemented: readiness reports",
  "Current posture: No-CUI only"
];

const heroMetrics = [
  ["72%", "Sample readiness signal"],
  ["24", "Open obligations"],
  ["11", "Evidence metadata gaps"]
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
  const homepageVideoRef = useRef<HTMLVideoElement>(null);
  const [homepageVideoStatus, setHomepageVideoStatus] = useState<string | null>(null);

  useEffect(() => {
    document.title = "FeDril | GovCon Compliance Readiness Software";

    const description =
      "FeDril is a No-CUI compliance management workspace for small government contractors to track obligations, evidence metadata, readiness workflows, and reports.";
    let metaDescription = document.querySelector<HTMLMetaElement>('meta[name="description"]');

    if (!metaDescription) {
      metaDescription = document.createElement("meta");
      metaDescription.name = "description";
      document.head.append(metaDescription);
    }

    metaDescription.content = description;
  }, []);

  async function playHomepageVideoWithNarration() {
    const video = homepageVideoRef.current;
    if (!video) {
      setHomepageVideoStatus("The video player is unavailable.");
      return;
    }

    video.muted = false;
    video.volume = 1;

    if (
      video.ended ||
      (Number.isFinite(video.duration) && video.currentTime >= video.duration - 0.25)
    ) {
      video.currentTime = 0;
    }

    try {
      await video.play();
      setHomepageVideoStatus("Narration is playing.");
    } catch {
      setHomepageVideoStatus(
        "Playback was blocked by the browser. Use the player controls to start the video.",
      );
    }
  }

  return (
    <main className="landing-page">
      <section className="landing-hero" aria-label="FeDril landing page">
        <div className="landing-nav" aria-label="Primary">
          <a className="landing-brand" href="/landing" aria-label="FeDril landing page">
            <img className="landing-brand__logo" src="/F.svg" alt="" aria-hidden="true" />
            <span>
              <strong>FeDril</strong>
              <small>GovCon compliance workspace</small>
            </span>
          </a>
          <div className="landing-nav__links">
            <a href="#platform">Platform</a>
            <a href="#workflow">Workflow</a>
            <a href="#pilot">Pilot</a>
            <a href="#security">Data posture</a>
            <a className="landing-nav__cta" href="/app#/dashboard">Open workspace</a>
          </div>
        </div>

        <div className="landing-hero__content">
          <div className="landing-hero__copy">
            <p className="landing-eyebrow">No-CUI compliance management for small GovCon teams</p>
            <h1>Turn govcon compliance work into an operating system.</h1>
            <p className="landing-hero__lede">
              FeDril tracks obligations, evidence, deadlines, and readiness gaps in one No-CUI workspace, so your team
              can see what is missing before reviews, renewals, and contract deliverables.
            </p>
            <div className="landing-actions">
              <DemoRequestButton label="Request a pilot demo" />
              <a className="landing-button landing-button--secondary" href="#security">
                <LockKeyhole size={18} />
                <span>View No-CUI policy</span>
              </a>
              <a className="landing-button landing-button--secondary" href="/app#/dashboard">
                <ShieldCheck size={18} />
                <span>Open workspace</span>
              </a>
            </div>
            <div className="landing-hero__metrics" aria-label="FeDril sample operating metrics">
              {heroMetrics.map(([value, label]) => (
                <div key={label}>
                  <strong>{value}</strong>
                  <span>{label}</span>
                </div>
              ))}
            </div>
            <p className="landing-disclaimer">
              FeDril does not certify compliance, provide legal advice, or provide government approval. Do not upload real CUI in the MVP.
            </p>
          </div>

          <div className="landing-product landing-product--showcase" aria-label="FeDril product preview">
            <img src="/landing/compliance-operations-hero.png" alt="" aria-hidden="true" />
            <div className="landing-product__overlay" aria-hidden="true" />
            <div className="landing-product__bar">
              <span>FeDril workspace</span>
              <strong>No-CUI</strong>
            </div>
            <div className="landing-product__command">
              <span>Current workflow</span>
              <strong>Obligation review</strong>
              <small>Source-backed records, owners, status, evidence metadata</small>
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

      <section className="landing-section landing-demo-preview" aria-labelledby="demo-preview-title">
        <div className="landing-section__heading">
          <p className="landing-eyebrow">One-minute product walkthrough</p>
          <h2 id="demo-preview-title">See how FeDril organizes readiness work.</h2>
          <p>
            Follow a fictional team as it reviews gaps, ownership, evidence metadata, audit history, and reporting in a
            No-CUI compliance-management workspace.
          </p>
        </div>
        <div className="marketing-video marketing-video--homepage">
          <video
            aria-label="FeDril one-minute product walkthrough"
            controls
            playsInline
            poster="/landing/compliance-operations-hero.png"
            preload="metadata"
            ref={homepageVideoRef}
          >
            <source
              media="(max-width: 720px)"
              src="/videos/fedril-homepage-60-mobile.mp4"
              type="video/mp4"
            />
            <source src="/videos/fedril-homepage-60.mp4" type="video/mp4" />
            <track
              kind="captions"
              label="English"
              src="/captions/fedril-homepage-60.vtt"
              srcLang="en"
            />
            Your browser does not support embedded video. You can
            {" "}<a href="/videos/fedril-homepage-60.mp4">open the one-minute walkthrough directly</a>.
          </video>
          <div className="marketing-video__details">
            <p>Fictional demonstration · Northstar Precision Systems · No real CUI or customer data</p>
            <div className="marketing-video__actions">
              <button
                type="button"
                onClick={() => void playHomepageVideoWithNarration()}
              >
                <Volume2 size={18} />
                Play with narration
              </button>
              <a href="/demo">
                Watch the full product walkthrough
                <ArrowRight size={18} />
              </a>
            </div>
            {homepageVideoStatus ? (
              <p className="marketing-video__status" role="status">
                {homepageVideoStatus}
              </p>
            ) : null}
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
          <p className="landing-eyebrow">Operational clarity</p>
          <h2>Built for the messy middle between contract requirements and evidence packages.</h2>
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
          <div className="landing-map" aria-label="FeDril readiness map graphic">
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
          <h2>See the chain of work from obligation to owner to report.</h2>
          <p>
            The first FeDril workflow is intentionally concrete: one company profile, one contract or synthetic workflow,
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
            The MVP is for synthetic, redacted, and non-sensitive records. Do not upload real CUI, classified information,
            ITAR/export-controlled data, sensitive government-furnished information, credentials, payroll records, SSNs,
            health data, or sensitive incident details.
          </p>
        </div>
        <div className="landing-security__panel">
          <div>
            <LockKeyhole size={24} />
            <h3>What FeDril tracks</h3>
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
            <span>Do not upload: real CUI, classified data, ITAR/export-controlled data, sensitive GFI</span>
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
          <DemoRequestButton label="Discuss pilot fit" />
        </div>
      </section>

      <footer className="landing-footer">
        <strong>FeDril</strong>
        <p>
          Reports are readiness artifacts and workflow guidance, not legal advice, certification decisions, government
          determinations, or substitutes for qualified expert review.
        </p>
      </footer>
    </main>
  );
}

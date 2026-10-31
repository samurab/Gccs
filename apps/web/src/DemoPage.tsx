import { ArrowLeft, ShieldCheck } from "lucide-react";
import { useEffect } from "react";
import { DemoRequestButton } from "./DemoRequestButton";

export function DemoPage() {
  useEffect(() => {
    document.title = "FeDril Product Walkthrough | No-CUI Compliance Management";

    const description =
      "Watch a fictional FeDril product walkthrough showing readiness visibility, ownership, evidence metadata, remediation, audit history, and reporting.";
    let metaDescription = document.querySelector<HTMLMetaElement>('meta[name="description"]');

    if (!metaDescription) {
      metaDescription = document.createElement("meta");
      metaDescription.name = "description";
      document.head.append(metaDescription);
    }

    metaDescription.content = description;
  }, []);

  return (
    <main className="demo-page">
      <nav className="demo-nav" aria-label="Demo navigation">
        <a className="landing-brand" href="/landing" aria-label="FeDril landing page">
          <img className="landing-brand__logo" src="/F.svg" alt="" aria-hidden="true" />
          <span>
            <strong>FeDril</strong>
            <small>GovCon compliance workspace</small>
          </span>
        </a>
        <div className="demo-nav__links">
          <a href="/landing">
            <ArrowLeft size={17} />
            Back to overview
          </a>
          <a className="landing-nav__cta" href="/app#/dashboard">Open workspace</a>
        </div>
      </nav>

      <section className="demo-hero" aria-labelledby="demo-title">
        <div className="demo-hero__copy">
          <p className="landing-eyebrow">Flagship product walkthrough</p>
          <h1 id="demo-title">See readiness work move from gap to follow-through.</h1>
          <p>
            This fictional walkthrough shows how FeDril helps teams organize CMMC readiness work and provides visibility
            into ownership, evidence metadata, gaps, remediation, audit history, and reporting.
          </p>
          <div className="demo-facts" aria-label="Demonstration details">
            <span>3 minutes 23 seconds</span>
            <span>Northstar Precision Systems</span>
            <span>No-CUI demonstration</span>
          </div>
        </div>

        <div className="marketing-video marketing-video--flagship">
          <video
            aria-label="FeDril flagship product walkthrough"
            controls
            playsInline
            poster="/landing/compliance-operations-hero.png"
            preload="metadata"
          >
            <source src="/videos/fedril-flagship.mp4" type="video/mp4" />
            <track
              kind="captions"
              label="English"
              src="/captions/fedril-demo.vtt"
              srcLang="en"
            />
            Your browser does not support embedded video. You can
            {" "}<a href="/videos/fedril-flagship.mp4">open the flagship walkthrough directly</a>.
          </video>
          <div className="marketing-video__details">
            <p>Narration generated using AI voice technology.</p>
            <span>Captions available from the player controls</span>
          </div>
        </div>
      </section>

      <section className="demo-boundary" aria-labelledby="demo-boundary-title">
        <ShieldCheck size={26} aria-hidden="true" />
        <div>
          <h2 id="demo-boundary-title">A fictional, No-CUI demonstration</h2>
          <p>
            Northstar Precision Systems, its users, requirements, tasks, evidence filenames, dates, and activity history
            are fictional. The video contains no production data, real customer information, CUI, FCI, credentials, or secrets.
          </p>
        </div>
      </section>

      <section className="demo-cta" aria-labelledby="demo-cta-title">
        <div>
          <p className="landing-eyebrow">Explore your workflow</p>
          <h2 id="demo-cta-title">Schedule a live FeDril demonstration.</h2>
          <p>Discuss how FeDril could support a repeatable compliance-management process for your team.</p>
        </div>
        <DemoRequestButton label="Schedule a live demo" />
      </section>

      <footer className="landing-footer demo-footer">
        <strong>FeDril</strong>
        <p>
          FeDril supports readiness workflow management. It does not guarantee compliance, certification, approval, or
          assessment results.
        </p>
      </footer>
    </main>
  );
}

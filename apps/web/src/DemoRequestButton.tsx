import { ArrowRight, ShieldCheck, X } from "lucide-react";
import { CheckCircle2 } from "lucide-react";
import { useEffect, useId, useRef, useState } from "react";
import type { FormEvent, KeyboardEvent as ReactKeyboardEvent } from "react";
import { submitDemoRequest } from "./demoRequestApi";

type DemoRequestButtonProps = {
  label: string;
  className?: string;
};

export function DemoRequestButton({ label, className = "" }: DemoRequestButtonProps) {
  const [isOpen, setIsOpen] = useState(false);
  const [status, setStatus] = useState<"idle" | "submitting" | "success" | "error">("idle");
  const [error, setError] = useState("");
  const firstNameRef = useRef<HTMLInputElement>(null);
  const openButtonRef = useRef<HTMLButtonElement>(null);
  const dialogRef = useRef<HTMLElement>(null);
  const titleId = useId();
  const descriptionId = useId();
  const timeZone = Intl.DateTimeFormat().resolvedOptions().timeZone || "UTC";
  const [schedulerBounds] = useState(() => {
    const toLocalInput = (date: Date) => new Date(date.getTime() - date.getTimezoneOffset() * 60_000).toISOString().slice(0, 16);
    const now = Date.now();
    return { minimum: toLocalInput(new Date(now + 2 * 60 * 60 * 1000)), maximum: toLocalInput(new Date(now + 90 * 24 * 60 * 60 * 1000)) };
  });

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    firstNameRef.current?.focus();

    const closeOnEscape = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        setIsOpen(false);
        openButtonRef.current?.focus();
      }
    };

    document.addEventListener("keydown", closeOnEscape);
    return () => {
      document.body.style.overflow = previousOverflow;
      document.removeEventListener("keydown", closeOnEscape);
    };
  }, [isOpen]);

  const close = () => {
    setIsOpen(false);
    setStatus("idle");
    setError("");
    window.requestAnimationFrame(() => openButtonRef.current?.focus());
  };

  const submit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (status === "submitting") return;
    const form = event.currentTarget;
    const values = new FormData(form);
    const optional = (name: string) => String(values.get(name) ?? "").trim() || null;
    setStatus("submitting");
    setError("");
    const result = await submitDemoRequest({
      firstName: String(values.get("firstName") ?? "").trim(),
      lastName: String(values.get("lastName") ?? "").trim(),
      email: String(values.get("email") ?? "").trim(),
      phone: optional("phone"),
      company: String(values.get("company") ?? "").trim(),
      referralSource: optional("referralSource"),
      employeeCount: optional("employeeCount"),
      message: optional("message"),
      preferredStartAt: new Date(String(values.get("preferredLocalStart") ?? "")).toISOString(),
      preferredTimeZone: timeZone,
      privacyConsent: values.get("privacyConsent") === "on",
      website: optional("website"),
    });
    if (result.error) {
      setError(result.error);
      setStatus("error");
      return;
    }
    form.reset();
    setStatus("success");
  };

  const containFocus = (event: ReactKeyboardEvent<HTMLElement>) => {
    if (event.key !== "Tab") {
      return;
    }

    const focusable = Array.from(
      dialogRef.current?.querySelectorAll<HTMLElement>(
        'button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled])',
      ) ?? [],
    );
    const first = focusable[0];
    const last = focusable.at(-1);

    if (event.shiftKey && document.activeElement === first) {
      event.preventDefault();
      last?.focus();
    } else if (!event.shiftKey && document.activeElement === last) {
      event.preventDefault();
      first?.focus();
    }
  };

  return (
    <>
      <button
        className={`landing-button landing-button--primary demo-cta__button ${className}`.trim()}
        onClick={() => setIsOpen(true)}
        ref={openButtonRef}
        type="button"
      >
        <span>{label}</span>
        <ArrowRight aria-hidden="true" size={18} />
      </button>

      {isOpen ? (
        <div
          className="demo-request-backdrop"
          onMouseDown={(event) => {
            if (event.target === event.currentTarget) {
              close();
            }
          }}
        >
          <section
            aria-describedby={descriptionId}
            aria-labelledby={titleId}
            aria-modal="true"
            className="demo-request-dialog"
            onKeyDown={containFocus}
            ref={dialogRef}
            role="dialog"
          >
            <header className="demo-request-dialog__header">
              <div>
                <p className="landing-eyebrow">Talk with the FeDril team</p>
                <h2 id={titleId}>Schedule a live demo</h2>
                <p id={descriptionId}>Tell us about your team and the readiness workflow you want to discuss.</p>
              </div>
              <button aria-label="Close demo request form" className="demo-request-dialog__close" onClick={close} type="button">
                <X aria-hidden="true" size={22} />
              </button>
            </header>

            {status === "success" ? (
              <div className="demo-request-success" role="status">
                <CheckCircle2 aria-hidden="true" size={42} />
                <h3>Demo request received</h3>
                <p>Your preferred demo time was recorded. An acknowledgement will be sent to the work email you provided when email delivery is configured. FeDril will confirm availability separately.</p>
                <button className="landing-button landing-button--primary" onClick={close} type="button">Close</button>
              </div>
            ) : <form className="demo-request-form" onSubmit={submit}>
              <div className="demo-request-form__grid">
                <label className="demo-request-form__honeypot" aria-hidden="true">
                  <span>Website</span>
                  <input autoComplete="off" name="website" tabIndex={-1} />
                </label>
                <label>
                  <span>First name <b aria-hidden="true">*</b></span>
                  <input autoComplete="given-name" maxLength={100} name="firstName" ref={firstNameRef} required />
                </label>
                <label>
                  <span>Last name <b aria-hidden="true">*</b></span>
                  <input autoComplete="family-name" maxLength={100} name="lastName" required />
                </label>
                <label>
                  <span>Work email <b aria-hidden="true">*</b></span>
                  <input autoComplete="email" maxLength={254} name="email" required type="email" />
                </label>
                <label>
                  <span>Phone</span>
                  <input autoComplete="tel" maxLength={40} name="phone" type="tel" />
                </label>
                <label className="demo-request-form__wide">
                  <span>Company name <b aria-hidden="true">*</b></span>
                  <input autoComplete="organization" maxLength={160} name="company" required />
                </label>
                <label className="demo-request-form__wide">
                  <span>How did you hear about FeDril?</span>
                  <input maxLength={160} name="referralSource" />
                </label>
                <label className="demo-request-form__wide">
                  <span>Number of employees</span>
                  <select defaultValue="" name="employeeCount">
                    <option value="">Select company size</option>
                    <option value="1-10">1–10</option>
                    <option value="11-50">11–50</option>
                    <option value="51-200">51–200</option>
                    <option value="201-500">201–500</option>
                    <option value="501+">501+</option>
                  </select>
                </label>
                <label className="demo-request-form__wide">
                  <span>How can we help?</span>
                  <textarea maxLength={1200} name="message" rows={4} />
                </label>
                <fieldset className="demo-request-scheduler demo-request-form__wide">
                  <legend>Preferred demo time <b aria-hidden="true">*</b></legend>
                  <label><span>Date and time</span><input max={schedulerBounds.maximum} min={schedulerBounds.minimum} name="preferredLocalStart" required type="datetime-local" /></label>
                  <p>Time zone: <strong>{timeZone}</strong></p>
                  <small>This is a requested 30-minute time, not a confirmed reservation. FeDril will confirm availability separately.</small>
                </fieldset>
                <label className="demo-request-form__consent demo-request-form__wide">
                  <input name="privacyConsent" required type="checkbox" />
                  <span>I agree that FeDril may use these business-contact details to respond to this demo request. <b aria-hidden="true">*</b></span>
                </label>
              </div>

              <div className="demo-request-form__notice">
                <ShieldCheck aria-hidden="true" size={20} />
                <p>Do not include CUI, FCI, classified information, credentials, or other sensitive content.</p>
              </div>
              <div className="demo-request-form__actions">
                <button className="landing-button landing-button--secondary" onClick={close} type="button">Cancel</button>
                <button className="landing-button landing-button--primary" disabled={status === "submitting"} type="submit">
                  {status === "submitting" ? "Submitting…" : "Submit demo request"}
                  <ArrowRight aria-hidden="true" size={18} />
                </button>
              </div>
              {status === "error" ? <p className="demo-request-form__error" role="alert">{error}</p> : null}
              <p className="demo-request-form__handoff">Submitting stores these business-contact details and your preferred time so the FeDril team can respond. It does not reserve the time automatically.</p>
            </form>}
          </section>
        </div>
      ) : null}
    </>
  );
}

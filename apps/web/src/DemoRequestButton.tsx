import { ArrowRight, ShieldCheck, X } from "lucide-react";
import { CheckCircle2 } from "lucide-react";
import { useEffect, useId, useRef, useState } from "react";
import type { FormEvent, KeyboardEvent as ReactKeyboardEvent } from "react";
import { submitDemoRequest } from "./demoRequestApi";

type DemoRequestButtonProps = {
  label: string;
  className?: string;
};

function createSchedulerBounds() {
  const toLocalInput = (date: Date) => new Date(date.getTime() - date.getTimezoneOffset() * 60_000).toISOString().slice(0, 16);
  const now = Date.now();
  const minimum = new Date(Math.ceil((now + 2 * 60 * 60 * 1000) / 60_000) * 60_000);
  const maximum = new Date(Math.floor((now + 90 * 24 * 60 * 60 * 1000) / 60_000) * 60_000);
  return { minimum: toLocalInput(minimum), maximum: toLocalInput(maximum) };
}

function formatSchedulerBoundary(value: string) {
  const boundary = new Date(value);
  return Number.isNaN(boundary.getTime())
    ? value.replace("T", " ")
    : new Intl.DateTimeFormat(undefined, { dateStyle: "short", timeStyle: "short" }).format(boundary);
}

function getSchedulerValidationMessage(input: HTMLInputElement) {
  input.setCustomValidity("");
  if (input.validity.valueMissing) return "Select a date and time.";
  if (input.validity.rangeUnderflow) return `Value must be ${formatSchedulerBoundary(input.min)} or later.`;
  if (input.validity.rangeOverflow) return `Value must be ${formatSchedulerBoundary(input.max)} or earlier.`;
  return "";
}

export function DemoRequestButton({ label, className = "" }: DemoRequestButtonProps) {
  const [isOpen, setIsOpen] = useState(false);
  const [status, setStatus] = useState<"idle" | "submitting" | "success" | "error">("idle");
  const [error, setError] = useState("");
  const firstNameRef = useRef<HTMLInputElement>(null);
  const openButtonRef = useRef<HTMLButtonElement>(null);
  const dialogRef = useRef<HTMLElement>(null);
  const titleId = useId();
  const descriptionId = useId();
  const schedulerErrorId = useId();
  const timeZone = Intl.DateTimeFormat().resolvedOptions().timeZone || "UTC";
  const [schedulerBounds, setSchedulerBounds] = useState(createSchedulerBounds);
  const [schedulerError, setSchedulerError] = useState("");

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
    setSchedulerError("");
    window.requestAnimationFrame(() => openButtonRef.current?.focus());
  };

  const open = () => {
    setSchedulerBounds(createSchedulerBounds());
    setSchedulerError("");
    setIsOpen(true);
  };

  const submit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (status === "submitting") return;
    const form = event.currentTarget;
    const preferredStart = form.elements.namedItem("preferredLocalStart") as HTMLInputElement;
    const currentBounds = createSchedulerBounds();
    preferredStart.min = currentBounds.minimum;
    preferredStart.max = currentBounds.maximum;
    setSchedulerBounds(currentBounds);
    const currentSchedulerError = getSchedulerValidationMessage(preferredStart);
    if (currentSchedulerError) {
      preferredStart.setCustomValidity(currentSchedulerError);
      setSchedulerError(currentSchedulerError);
      setError("");
      setStatus("idle");
      preferredStart.focus();
      preferredStart.reportValidity();
      return;
    }
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
      const preferredStartError = result.fieldErrors.preferredStartAt?.[0];
      if (preferredStartError) {
        const refreshedBounds = createSchedulerBounds();
        preferredStart.min = refreshedBounds.minimum;
        preferredStart.max = refreshedBounds.maximum;
        setSchedulerBounds(refreshedBounds);
        const message = getSchedulerValidationMessage(preferredStart) || (
          preferredStart.value > refreshedBounds.maximum
            ? `Value must be ${formatSchedulerBoundary(refreshedBounds.maximum)} or earlier.`
            : `Value must be ${formatSchedulerBoundary(refreshedBounds.minimum)} or later.`
        );
        preferredStart.setCustomValidity(message);
        setSchedulerError(message);
        preferredStart.focus();
      }
      setError(preferredStartError ? "Please correct the preferred demo time." : result.error);
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
        onClick={open}
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
                {import.meta.env.DEV ? (
                  <>
                    <p>Your preferred demo time was recorded by the local development capture transport. No email was sent. FeDril will confirm availability separately.</p>
                    <a href="/platform/demo-requests">Open the local operator calendar</a>
                  </>
                ) : (
                  <p>Your preferred demo time was recorded. An acknowledgement will be sent to the work email you provided when email delivery is configured. FeDril will confirm availability separately.</p>
                )}
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
                  <label className="demo-request-form__explicit-validation">
                    <span>Date and time</span>
                    <input
                      aria-describedby={schedulerError ? schedulerErrorId : undefined}
                      max={schedulerBounds.maximum}
                      min={schedulerBounds.minimum}
                      name="preferredLocalStart"
                      onInput={(event) => {
                        event.currentTarget.setCustomValidity("");
                        setSchedulerError("");
                      }}
                      onInvalid={(event) => {
                        const message = getSchedulerValidationMessage(event.currentTarget);
                        event.currentTarget.setCustomValidity(message);
                        setSchedulerError(message);
                      }}
                      required
                      type="datetime-local"
                    />
                  </label>
                  {schedulerError ? <p className="demo-request-form__error" id={schedulerErrorId} role="alert">{schedulerError}</p> : null}
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

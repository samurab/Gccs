import { CalendarDays, ChevronLeft, ChevronRight, Inbox, LoaderCircle, LockKeyhole, RefreshCw } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import { PlatformAdminNav } from "./PlatformAdminNav";
import {
  getPlatformAccess,
  getPlatformDemoRequestCalendar,
  getPlatformDemoRequests,
  queuePlatformDemoRequestResponse,
  type PlatformAccess,
  type PlatformDemoRequestCalendarItem,
  type PlatformDemoRequestCalendarRange,
  type PlatformDemoRequestPage,
} from "./lib/api";

const responseTemplates = [
  ["ReviewingRequestedTime", "We’re reviewing your requested time"],
  ["RequestMoreDetails", "Please provide more details"],
  ["RequestedTimeUnavailable", "The requested time is unavailable"],
] as const;

function ResponseControls({ deliveryMode, requestId, requesterName }: { deliveryMode: PlatformAccess["demoRequestDeliveryMode"]; requestId: string; requesterName: string }) {
  const [templateKey, setTemplateKey] = useState(responseTemplates[0][0]);
  const [state, setState] = useState<"idle" | "sending" | "sent" | "error">("idle");
  const [message, setMessage] = useState("");
  const isDevelopmentCapture = deliveryMode === "DevelopmentCapture";
  const isDeliveryDisabled = deliveryMode === "Disabled";
  const send = async () => {
    const label = responseTemplates.find(item => item[0] === templateKey)?.[1] ?? templateKey;
    const action = isDevelopmentCapture ? "Capture" : "Queue";
    if (!window.confirm(`${action} “${label}” for ${requesterName}?`)) return;
    setState("sending"); setMessage("");
    const result = await queuePlatformDemoRequestResponse(requestId, templateKey);
    if (result.error) { setState("error"); setMessage(result.error); return; }
    setState("sent");
    setMessage(result.data?.status === "AlreadyQueued"
      ? isDevelopmentCapture ? "This response was already captured locally." : "This response was already queued."
      : isDevelopmentCapture ? "Response queued for local capture. No email will be sent." : "Response queued for email delivery.");
  };
  return <section className="platform-demo-response" aria-label={`Respond to ${requesterName}`}>
    <label><span>Response template</span><select onChange={event => { setTemplateKey(event.target.value as typeof templateKey); setState("idle"); setMessage(""); }} value={templateKey}>{responseTemplates.map(([key, label]) => <option key={key} value={key}>{label}</option>)}</select></label>
    <button disabled={isDeliveryDisabled || state === "sending" || state === "sent"} onClick={() => void send()} type="button">{state === "sending" ? isDevelopmentCapture ? "Capturing…" : "Queueing…" : isDevelopmentCapture ? "Capture response" : "Queue response"}</button>
    {message ? <p className={state === "error" ? "form-status form-status--error" : "form-status form-status--ok"} role="status">{message}</p> : null}
    <small>{isDeliveryDisabled
      ? "Demo-response delivery is disabled."
      : isDevelopmentCapture
        ? "Local development records the response and No-CUI warning without sending email."
        : "Emails use server-owned copy and the No-CUI warning. Queueing does not guarantee delivery until the email provider accepts it."}</small>
  </section>;
}

function monthRange(month: Date) {
  const from = new Date(month.getFullYear(), month.getMonth(), 1);
  const to = new Date(month.getFullYear(), month.getMonth() + 1, 1);
  return { from: from.toISOString(), to: to.toISOString() };
}

function localDateKey(value: Date) {
  return `${value.getFullYear()}-${String(value.getMonth() + 1).padStart(2, "0")}-${String(value.getDate()).padStart(2, "0")}`;
}

function DemoRequestCalendar({
  month,
  range,
  selectedDate,
  onMonthChange,
  onSelectDate,
}: {
  month: Date;
  range: PlatformDemoRequestCalendarRange;
  selectedDate: string | null;
  onMonthChange: (month: Date) => void;
  onSelectDate: (date: string) => void;
}) {
  const itemsByDate = useMemo(() => {
    const grouped = new Map<string, PlatformDemoRequestCalendarItem[]>();
    for (const item of range.items) {
      const key = localDateKey(new Date(item.preferredStartAt));
      grouped.set(key, [...(grouped.get(key) ?? []), item]);
    }
    return grouped;
  }, [range.items]);
  const firstDay = new Date(month.getFullYear(), month.getMonth(), 1);
  const dayCount = new Date(month.getFullYear(), month.getMonth() + 1, 0).getDate();
  const cells = [
    ...Array.from({ length: firstDay.getDay() }, () => null),
    ...Array.from({ length: dayCount }, (_, index) => new Date(month.getFullYear(), month.getMonth(), index + 1)),
  ];
  const selectedItems = selectedDate ? itemsByDate.get(selectedDate) ?? [] : [];

  return <section className="platform-demo-calendar" aria-labelledby="demo-calendar-heading">
    <header>
      <div><p className="landing-eyebrow">Requested-time calendar</p><h2 id="demo-calendar-heading">{month.toLocaleDateString(undefined, { month: "long", year: "numeric" })}</h2></div>
      <nav aria-label="Calendar month">
        <button aria-label="Previous month" onClick={() => onMonthChange(new Date(month.getFullYear(), month.getMonth() - 1, 1))} type="button"><ChevronLeft size={17} /></button>
        <button onClick={() => onMonthChange(new Date(new Date().getFullYear(), new Date().getMonth(), 1))} type="button">Today</button>
        <button aria-label="Next month" onClick={() => onMonthChange(new Date(month.getFullYear(), month.getMonth() + 1, 1))} type="button"><ChevronRight size={17} /></button>
      </nav>
    </header>
    <p className="platform-demo-calendar__notice"><CalendarDays size={17} /> These are customer-requested times, not confirmed appointments.</p>
    <div className="platform-demo-calendar__weekdays" aria-hidden="true">{["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"].map(day => <span key={day}>{day}</span>)}</div>
    <div className="platform-demo-calendar__grid">
      {cells.map((date, index) => {
        if (!date) return <span className="platform-demo-calendar__blank" key={`blank-${index}`} />;
        const key = localDateKey(date);
        const count = itemsByDate.get(key)?.length ?? 0;
        return <button
          aria-label={`${date.toLocaleDateString(undefined, { month: "long", day: "numeric" })}: ${count === 0 ? "no requested demos" : `${count} requested demo${count === 1 ? "" : "s"}`}`}
          aria-pressed={selectedDate === key}
          className={count > 0 ? "platform-demo-calendar__day platform-demo-calendar__day--has-requests" : "platform-demo-calendar__day"}
          key={key}
          onClick={() => onSelectDate(key)}
          type="button"
        ><strong>{date.getDate()}</strong><span>{count === 0 ? "No requests" : `${count} requested`}</span></button>;
      })}
    </div>
    <section className="platform-demo-agenda" aria-live="polite">
      <h3>{selectedDate ? new Date(`${selectedDate}T12:00:00`).toLocaleDateString(undefined, { weekday: "long", month: "long", day: "numeric" }) : "Daily agenda"}</h3>
      {!selectedDate ? <p>Select a date to review its requested demo times.</p> : null}
      {selectedDate && selectedItems.length === 0 ? <p>No demo times were requested for this date.</p> : null}
      {selectedItems.map(item => <article key={item.id}>
        <time dateTime={item.preferredStartAt}>{new Date(item.preferredStartAt).toLocaleTimeString(undefined, { hour: "numeric", minute: "2-digit" })}</time>
        <div><strong>{item.company}</strong><span>{item.firstName} {item.lastName} · {item.preferredTimeZone ?? "Time zone not recorded"}</span></div>
        <span className="platform-demo-status platform-demo-status--requested">{item.schedulingStatus}</span>
      </article>)}
    </section>
  </section>;
}

export function PlatformDemoRequestsPage() {
  const [access, setAccess] = useState<PlatformAccess | null>(null);
  const [data, setData] = useState<PlatformDemoRequestPage | null>(null);
  const [error, setError] = useState("");
  const [page, setPage] = useState(1);
  const [calendarMonth, setCalendarMonth] = useState(() => {
    const now = new Date();
    return new Date(now.getFullYear(), now.getMonth(), 1);
  });
  const [calendar, setCalendar] = useState<PlatformDemoRequestCalendarRange | null>(null);
  const [selectedDate, setSelectedDate] = useState<string | null>(null);
  const [refreshVersion, setRefreshVersion] = useState(0);
  const calendarQuery = useMemo(() => monthRange(calendarMonth), [calendarMonth]);
  useEffect(() => {
    let active = true;
    getPlatformAccess().then(result => {
      if (!active) return;
      setAccess(result);
      if (!result.canManageDemoRequests) {
        setData(null);
        setCalendar(null);
      }
    }).catch(reason => {
      if (active) setError(reason instanceof Error ? reason.message : "Demo-request access could not be loaded.");
    });
    return () => { active = false; };
  }, [refreshVersion]);
  useEffect(() => {
    if (!access?.canManageDemoRequests) return;
    let active = true;
    getPlatformDemoRequests(page).then(result => {
      if (active) setData(result);
    }).catch(reason => {
      if (active) setError(reason instanceof Error ? reason.message : "Demo requests could not be loaded.");
    });
    return () => { active = false; };
  }, [access?.canManageDemoRequests, page, refreshVersion]);
  useEffect(() => {
    if (!access?.canManageDemoRequests) return;
    let active = true;
    getPlatformDemoRequestCalendar(calendarQuery.from, calendarQuery.to).then(result => {
      if (active) setCalendar(result);
    }).catch(reason => {
      if (active) setError(reason instanceof Error ? reason.message : "Requested-time calendar could not be loaded.");
    });
    return () => { active = false; };
  }, [access?.canManageDemoRequests, calendarQuery.from, calendarQuery.to, refreshVersion]);

  if (access === null && !error) return <main className="platform-demo-inbox"><LoaderCircle className="spin" /> Loading demo requests…</main>;
  if (access && !access.canManageDemoRequests) return <main className="platform-demo-inbox"><LockKeyhole /><h1>Demo-request access denied</h1><p>Your account lacks the ManageDemoRequests platform permission.</p></main>;
  return <main className="platform-demo-inbox">
    {access ? <PlatformAdminNav access={access} active="demo-requests" /> : null}
    <header><div><p className="landing-eyebrow">FeDril operations</p><h1>Demo requests</h1><p>Durable intake records and notification-delivery status. This view is platform-scoped, not tenant-scoped.</p></div><button onClick={() => { setError(""); setCalendar(null); setRefreshVersion(value => value + 1); }} type="button"><RefreshCw size={17} /> Refresh</button></header>
    {error ? <p className="form-status form-status--error" role="alert">{error}</p> : null}
    {access?.canManageDemoRequests && !calendar && !error ? <section className="platform-demo-empty"><LoaderCircle className="spin" /><h2>Loading requested-time calendar</h2></section> : null}
    {calendar ? <DemoRequestCalendar month={calendarMonth} onMonthChange={month => { setCalendar(null); setSelectedDate(null); setCalendarMonth(month); }} onSelectDate={setSelectedDate} range={calendar} selectedDate={selectedDate} /> : null}
    {data?.items.length === 0 ? <section className="platform-demo-empty"><Inbox size={34} /><h2>No demo requests</h2><p>New public submissions will appear here.</p></section> : null}
    {data?.items.map(item => <article className="platform-demo-card" key={item.id}>
      <div><span className={`platform-demo-status platform-demo-status--${item.deliveryStatus.toLowerCase()}`}>{item.deliveryStatus}</span><time dateTime={item.receivedAt}>{new Date(item.receivedAt).toLocaleString()}</time></div>
      <h2>{item.company}</h2><p><strong>{item.firstName} {item.lastName}</strong> · <a href={`mailto:${item.email}`}>{item.email}</a>{item.phone ? ` · ${item.phone}` : ""}</p>
      <dl><div><dt>Preferred time</dt><dd>{item.preferredStartAt ? new Date(item.preferredStartAt).toLocaleString(undefined, { timeZone: item.preferredTimeZone ?? undefined }) : "Not provided"}{item.preferredTimeZone ? ` (${item.preferredTimeZone})` : ""}</dd></div><div><dt>Internal notification</dt><dd>{item.deliveryStatus} · {item.deliveryAttemptCount} attempts</dd></div><div><dt>Requester acknowledgement</dt><dd>{item.acknowledgementStatus}</dd></div></dl>
      {item.message ? <blockquote>{item.message}</blockquote> : null}
      {item.deliveryFailureCode ? <p className="form-status form-status--error">Delivery failure: {item.deliveryFailureCode}</p> : null}
      <ResponseControls deliveryMode={access?.demoRequestDeliveryMode} requestId={item.id} requesterName={`${item.firstName} ${item.lastName}`} />
    </article>)}
    {data ? <nav className="platform-demo-pagination" aria-label="Demo request pages"><button disabled={!data.hasPreviousPage} onClick={() => setPage(value => value - 1)} type="button">Previous</button><span>Page {data.page} · {data.totalCount} requests</span><button disabled={!data.hasNextPage} onClick={() => setPage(value => value + 1)} type="button">Next</button></nav> : null}
  </main>;
}

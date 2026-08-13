import { CalendarDays, ChevronLeft, ChevronRight, Inbox, LoaderCircle, LockKeyhole, RefreshCw } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import { PlatformAdminNav } from "./PlatformAdminNav";
import {
  confirmPlatformDemoAppointment,
  getPlatformAccess,
  getPlatformDemoRequestCalendar,
  getPlatformDemoRequests,
  queuePlatformDemoRequestResponse,
  type PlatformAccess,
  type PlatformDemoRequestCalendarItem,
  type PlatformDemoRequestCalendarRange,
  type PlatformDemoRequestPage,
  type ConfirmDemoAppointmentRequest,
  type DemoFollowUpOperationsItem,
} from "./lib/api";

const responseTemplates = [
  ["ReviewingRequestedTime", "We’re reviewing your requested time"],
  ["RequestMoreDetails", "Please provide more details"],
  ["RequestedTimeUnavailable", "The requested time is unavailable"],
  ["ConfirmAppointment", "Confirm appointment"],
] as const;
type ResponseTemplateKey = typeof responseTemplates[number][0];

const meetingMethods: ReadonlyArray<[ConfirmDemoAppointmentRequest["meetingMethod"], string]> = [
  ["ConnectionDetailsToFollow", "Connection details will follow"],
  ["MicrosoftTeams", "Microsoft Teams"],
  ["Zoom", "Zoom"],
  ["GoogleMeet", "Google Meet"],
  ["Phone", "Phone"],
];

function dateTimeLocalInZone(value: string | null, timeZone: string) {
  if (!value) return "";
  const parts = new Intl.DateTimeFormat("en-CA", {
    timeZone, year: "numeric", month: "2-digit", day: "2-digit",
    hour: "2-digit", minute: "2-digit", hourCycle: "h23",
  }).formatToParts(new Date(value));
  const part = (type: Intl.DateTimeFormatPartTypes) => parts.find(item => item.type === type)?.value ?? "";
  return `${part("year")}-${part("month")}-${part("day")}T${part("hour")}:${part("minute")}`;
}

function emailDeliveryLabel(status: string) {
  return status === "Sent" ? "Provider accepted" :
    status === "Captured" ? "Captured locally" :
    status === "Queued" ? "Queued" :
    status === "RetryScheduled" ? "Retry scheduled" :
    status === "Failed" ? "Failed" :
    status === "NotQueued" ? "Not queued" :
    status;
}

function deliveryStatusDetail(status: string) {
  return status === "Sent" ? "The email provider accepted this message. Inbox delivery can still be affected by recipient filtering or spam controls." :
    status === "Captured" ? "Development capture mode recorded this message locally; no external email was sent." :
    status === "Queued" ? "The outbox row exists, but the background worker has not yet recorded provider acceptance." :
    status === "RetryScheduled" ? "The provider call failed and the worker scheduled another attempt." :
    status === "Failed" ? "The provider call failed and no further automatic retry is scheduled." :
    status === "NotQueued" ? "No outbox row exists for this message type." :
    status;
}

function meetingMethodLabel(value: string | null) {
  return meetingMethods.find(([method]) => method === value)?.[1] ?? value ?? "Not provided";
}

const workflowLabels: Record<string, string> = {
  ContractClauseIntake: "Contract and clause intake",
  ObligationsDeadlines: "Obligation and deadline tracking",
  CmmcReadiness: "CMMC readiness workflows",
  EvidenceManagement: "Evidence organization",
  SubcontractorFlowDowns: "Subcontractor flow-down tracking",
  ReportingPreparation: "Reporting or review preparation",
  Other: "Other workflow",
  Unavailable: "Stored workflow data unavailable",
};

function ResponseControls({
  deliveryMode,
  requestId,
  requesterName,
  preferredStartAt,
  preferredTimeZone,
  schedulingStatus,
  onChanged,
}: {
  deliveryMode: PlatformAccess["demoRequestDeliveryMode"];
  requestId: string;
  requesterName: string;
  preferredStartAt: string | null;
  preferredTimeZone: string | null;
  schedulingStatus: string;
  onChanged: () => void;
}) {
  const [templateKey, setTemplateKey] = useState<ResponseTemplateKey>(responseTemplates[0][0]);
  const [state, setState] = useState<"idle" | "sending" | "sent" | "error">("idle");
  const [message, setMessage] = useState("");
  const defaultTimeZone = preferredTimeZone ?? Intl.DateTimeFormat().resolvedOptions().timeZone;
  const [confirmedLocalStart, setConfirmedLocalStart] = useState(() => dateTimeLocalInZone(preferredStartAt, defaultTimeZone));
  const [timeZone, setTimeZone] = useState(defaultTimeZone);
  const [meetingMethod, setMeetingMethod] = useState<ConfirmDemoAppointmentRequest["meetingMethod"]>("ConnectionDetailsToFollow");
  const [meetingJoinUrl, setMeetingJoinUrl] = useState("");
  const isDevelopmentCapture = deliveryMode === "DevelopmentCapture";
  const isDeliveryDisabled = deliveryMode === "Disabled";
  const isAppointment = templateKey === "ConfirmAppointment";
  const appointmentAlreadyConfirmed = schedulingStatus === "Confirmed";
  const isOnlineMeeting = (["MicrosoftTeams", "Zoom", "GoogleMeet"] as string[]).includes(meetingMethod);
  const send = async () => {
    const label = responseTemplates.find(item => item[0] === templateKey)?.[1] ?? templateKey;
    const action = isDevelopmentCapture ? "Capture" : "Queue";
    const confirmation = isAppointment
      ? `Confirm the 30-minute appointment for ${requesterName} at ${confirmedLocalStart.replace("T", " ")} (${timeZone}) and queue the confirmation?`
      : `${action} “${label}” for ${requesterName}?`;
    if (!window.confirm(confirmation)) return;
    setState("sending"); setMessage("");
    if (isAppointment) {
      const result = await confirmPlatformDemoAppointment(requestId, {
        confirmedLocalStart,
        timeZone,
        meetingMethod,
        meetingJoinUrl: meetingJoinUrl.trim() || null,
      });
      if (result.error) { setState("error"); setMessage(result.error); return; }
      setState("sent");
      setMessage(isDevelopmentCapture
        ? "Appointment confirmed. Confirmation queued for local capture; no email will be sent."
        : "Appointment confirmed. Confirmation email queued.");
      onChanged();
      return;
    }
    const result = await queuePlatformDemoRequestResponse(requestId, templateKey);
    if (result.error) { setState("error"); setMessage(result.error); return; }
    setState("sent");
    setMessage(result.data?.status === "AlreadyPending"
      ? "A detail request is already pending. Send another after the requester responds or the current link expires."
      : result.data?.status === "AlreadyQueued"
      ? isDevelopmentCapture ? "This response was already captured locally." : "This response was already queued."
      : isDevelopmentCapture ? "Response queued for local capture. No email will be sent." : "Response queued for email delivery.");
    if (templateKey === "RequestMoreDetails") onChanged();
  };
  return <section className="platform-demo-response" aria-label={`Respond to ${requesterName}`}>
    <label><span>Response template</span><select onChange={event => { setTemplateKey(event.target.value as ResponseTemplateKey); setState("idle"); setMessage(""); }} value={templateKey}>{responseTemplates.map(([key, label]) => <option disabled={key === "ConfirmAppointment" && appointmentAlreadyConfirmed} key={key} value={key}>{key === "ConfirmAppointment" && appointmentAlreadyConfirmed ? `${label} (already confirmed)` : label}</option>)}</select></label>
    {isAppointment ? <div className="platform-demo-confirmation-fields">
      <label><span>Confirmed date and time</span><input onChange={event => setConfirmedLocalStart(event.target.value)} required type="datetime-local" value={confirmedLocalStart} /></label>
      <label><span>Time zone</span><input maxLength={100} onChange={event => setTimeZone(event.target.value)} required value={timeZone} /></label>
      <label><span>Duration</span><input disabled value="30 minutes" /></label>
      <label><span>Meeting method</span><select onChange={event => { setMeetingMethod(event.target.value as ConfirmDemoAppointmentRequest["meetingMethod"]); setMeetingJoinUrl(""); }} value={meetingMethod}>{meetingMethods.map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label>
      {isOnlineMeeting ? <label className="platform-demo-confirmation-fields__wide"><span>HTTPS meeting link</span><input maxLength={2048} onChange={event => setMeetingJoinUrl(event.target.value)} placeholder="https://…" required type="url" value={meetingJoinUrl} /></label> : null}
      <p className="platform-demo-confirmation-fields__notice">The signed-in operator will be recorded as host. The appointment and confirmation-email outbox record are saved atomically.</p>
    </div> : null}
    <button disabled={isDeliveryDisabled || state === "sending" || (isAppointment && (!confirmedLocalStart || !timeZone || appointmentAlreadyConfirmed || (isOnlineMeeting && !meetingJoinUrl.trim())))} onClick={() => void send()} type="button">{state === "sending" ? isAppointment ? "Confirming…" : isDevelopmentCapture ? "Capturing…" : "Queueing…" : isAppointment ? "Confirm appointment and queue email" : isDevelopmentCapture ? "Capture response" : "Queue response"}</button>
    {message ? <p className={state === "error" ? "form-status form-status--error" : "form-status form-status--ok"} role="status">{message}</p> : null}
    <small>{isDeliveryDisabled
      ? "Demo-response delivery is disabled."
      : isDevelopmentCapture
        ? "Local development records the response and No-CUI warning without sending email."
        : "Emails use server-owned copy and the No-CUI warning. Queueing does not guarantee delivery until the email provider accepts it."}</small>
  </section>;
}

function FollowUpHistory({ items }: { items: DemoFollowUpOperationsItem[] }) {
  if (items.length === 0) return null;
  return <section className="platform-demo-follow-ups" aria-label="Demo detail follow-up history">
    <h3>Demo detail follow-up</h3>
    {items.map(item => <article key={item.id}>
      <header><strong>{item.status}</strong><span>Requested {new Date(item.requestedAt).toLocaleString()} · Email {emailDeliveryLabel(item.deliveryStatus)}</span></header>
      {item.status === "Pending" ? <p>Waiting for requester response. Link expires {new Date(item.expiresAt).toLocaleString()}.</p> : null}
      {item.status === "Expired" ? <p>The requester did not respond before {new Date(item.expiresAt).toLocaleString()}.</p> : null}
      {item.status === "Responded" ? <div className="platform-demo-follow-up-response">
        <p><strong>Received:</strong> {item.respondedAt ? new Date(item.respondedAt).toLocaleString() : "Recorded"}</p>
        <div><strong>Workflows</strong><ul>{item.workflows.map(workflow => <li key={workflow}>{workflowLabels[workflow] ?? workflow}</li>)}{item.otherWorkflow ? <li>{item.otherWorkflow}</li> : null}</ul></div>
        <div><strong>Desired outcome</strong><p>{item.goals}</p></div>
        <div><strong>Challenges</strong><p>{item.challenges}</p></div>
        {item.currentProcess ? <div><strong>Current process</strong><p>{item.currentProcess}</p></div> : null}
        {item.additionalContext ? <div><strong>Additional context</strong><p>{item.additionalContext}</p></div> : null}
        <small>No-CUI acknowledgement: {item.noCuiNoticeVersion}</small>
      </div> : null}
    </article>)}
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
      const key = localDateKey(new Date(item.confirmedStartAt ?? item.preferredStartAt));
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
    <p className="platform-demo-calendar__notice"><CalendarDays size={17} /> Requested times are tentative. Confirmed appointments use the operator-confirmed time.</p>
    <div className="platform-demo-calendar__weekdays" aria-hidden="true">{["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"].map(day => <span key={day}>{day}</span>)}</div>
    <div className="platform-demo-calendar__grid">
      {cells.map((date, index) => {
        if (!date) return <span className="platform-demo-calendar__blank" key={`blank-${index}`} />;
        const key = localDateKey(date);
        const dayItems = itemsByDate.get(key) ?? [];
        const count = dayItems.length;
        const confirmedCount = dayItems.filter(item => item.schedulingStatus === "Confirmed").length;
        return <button
          aria-label={`${date.toLocaleDateString(undefined, { month: "long", day: "numeric" })}: ${count === 0 ? "no demo appointments" : `${count} demo appointment${count === 1 ? "" : "s"}, ${confirmedCount} confirmed`}`}
          aria-pressed={selectedDate === key}
          className={count > 0 ? "platform-demo-calendar__day platform-demo-calendar__day--has-requests" : "platform-demo-calendar__day"}
          key={key}
          onClick={() => onSelectDate(key)}
          type="button"
        ><strong>{date.getDate()}</strong><span>{count === 0 ? "No appointments" : `${count} total · ${confirmedCount} confirmed`}</span></button>;
      })}
    </div>
    <section className="platform-demo-agenda" aria-live="polite">
      <h3>{selectedDate ? new Date(`${selectedDate}T12:00:00`).toLocaleDateString(undefined, { weekday: "long", month: "long", day: "numeric" }) : "Daily agenda"}</h3>
      {!selectedDate ? <p>Select a date to review its requested demo times.</p> : null}
      {selectedDate && selectedItems.length === 0 ? <p>No demo times were requested for this date.</p> : null}
      {selectedItems.map(item => { const scheduledAt = item.confirmedStartAt ?? item.preferredStartAt; const zone = item.confirmedTimeZone ?? item.preferredTimeZone; return <article key={item.id}>
        <time dateTime={scheduledAt}>{new Date(scheduledAt).toLocaleTimeString(undefined, { hour: "numeric", minute: "2-digit" })}</time>
        <div><strong>{item.company}</strong><span>{item.firstName} {item.lastName} · {zone ?? "Time zone not recorded"}</span></div>
        <span className={`platform-demo-status platform-demo-status--${item.schedulingStatus.toLowerCase()}`}>{item.schedulingStatus}</span>
      </article>; })}
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
  const [autoRefreshVersion, setAutoRefreshVersion] = useState(0);
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
  }, [access?.canManageDemoRequests, page, refreshVersion, autoRefreshVersion]);
  useEffect(() => {
    if (!access?.canManageDemoRequests) return;
    let active = true;
    getPlatformDemoRequestCalendar(calendarQuery.from, calendarQuery.to).then(result => {
      if (active) setCalendar(result);
    }).catch(reason => {
      if (active) setError(reason instanceof Error ? reason.message : "Requested-time calendar could not be loaded.");
    });
    return () => { active = false; };
  }, [access?.canManageDemoRequests, calendarQuery.from, calendarQuery.to, refreshVersion, autoRefreshVersion]);
  useEffect(() => {
    if (!access?.canManageDemoRequests) return;
    const timer = window.setInterval(() => {
      setAutoRefreshVersion(value => value + 1);
    }, 30_000);
    return () => window.clearInterval(timer);
  }, [access?.canManageDemoRequests]);

  if (access === null && !error) return <main className="platform-demo-inbox"><LoaderCircle className="spin" /> Loading demo requests…</main>;
  if (access && !access.canManageDemoRequests) return <main className="platform-demo-inbox"><LockKeyhole /><h1>Demo-request access denied</h1><p>Your account lacks the ManageDemoRequests platform permission.</p></main>;
  const isDevelopmentCapture = access?.demoRequestDeliveryMode === "DevelopmentCapture";
  return <main className="platform-demo-inbox">
    {access ? <PlatformAdminNav access={access} active="demo-requests" /> : null}
    <header><div><p className="landing-eyebrow">FeDril operations</p><h1>Demo requests</h1><p>Durable intake records and notification-delivery status. This view is platform-scoped, not tenant-scoped.</p></div><button onClick={() => { setError(""); setCalendar(null); setRefreshVersion(value => value + 1); }} type="button"><RefreshCw size={17} /> Refresh</button></header>
    <section className="platform-demo-delivery-note" aria-label="Demo request delivery mode">
      <strong>{isDevelopmentCapture ? "Development capture mode" : access?.demoRequestDeliveryMode === "ExternalEmail" ? "External email mode" : "Delivery disabled"}</strong>
      <span>{isDevelopmentCapture
        ? "Local development records acknowledgement and response messages in the outbox as captured. It does not send requester emails unless the DemoRequests provider is configured for external email."
        : access?.demoRequestDeliveryMode === "ExternalEmail"
          ? "Requester acknowledgements, detail requests, and appointment confirmations are sent by the server-side outbox worker. Provider accepted means Azure Communication Services accepted the message, not that the requester opened it."
          : "Demo-request delivery is disabled for this environment."}</span>
      <span>Calendar and inbox data auto-refresh every 30 seconds while this page is open.</span>
    </section>
    {error ? <p className="form-status form-status--error" role="alert">{error}</p> : null}
    {access?.canManageDemoRequests && !calendar && !error ? <section className="platform-demo-empty"><LoaderCircle className="spin" /><h2>Loading requested-time calendar</h2></section> : null}
    {calendar ? <DemoRequestCalendar month={calendarMonth} onMonthChange={month => { setCalendar(null); setSelectedDate(null); setCalendarMonth(month); }} onSelectDate={setSelectedDate} range={calendar} selectedDate={selectedDate} /> : null}
    {data?.items.length === 0 ? <section className="platform-demo-empty"><Inbox size={34} /><h2>No demo requests</h2><p>New public submissions will appear here.</p></section> : null}
    {data?.items.map(item => <article className="platform-demo-card" key={item.id}>
      <div><span className={`platform-demo-status platform-demo-status--${item.deliveryStatus.toLowerCase()}`}>{item.deliveryStatus}</span><time dateTime={item.receivedAt}>{new Date(item.receivedAt).toLocaleString()}</time></div>
      <h2>{item.company}</h2><p><strong>{item.firstName} {item.lastName}</strong> · <a href={`mailto:${item.email}`}>{item.email}</a>{item.phone ? ` · ${item.phone}` : ""}</p>
      <dl><div><dt>Requested time</dt><dd>{item.preferredStartAt ? new Date(item.preferredStartAt).toLocaleString(undefined, { timeZone: item.preferredTimeZone ?? undefined }) : "Not provided"}{item.preferredTimeZone ? ` (${item.preferredTimeZone})` : ""}</dd></div><div><dt>Scheduling status</dt><dd>{item.schedulingStatus}</dd></div><div><dt>Confirmed appointment</dt><dd>{item.confirmedStartAt ? `${new Date(item.confirmedStartAt).toLocaleString(undefined, { timeZone: item.confirmedTimeZone ?? undefined })} (${item.confirmedTimeZone}) · ${item.durationMinutes} minutes · ${meetingMethodLabel(item.meetingMethod)}` : "Not confirmed"}</dd></div><div><dt>Confirmation email</dt><dd title={deliveryStatusDetail(item.appointmentConfirmationStatus)}>{emailDeliveryLabel(item.appointmentConfirmationStatus)}</dd></div><div><dt>Internal notification</dt><dd title={deliveryStatusDetail(item.deliveryStatus)}>{emailDeliveryLabel(item.deliveryStatus)} · {item.deliveryAttemptCount} attempts</dd></div><div><dt>Requester acknowledgement</dt><dd title={deliveryStatusDetail(item.acknowledgementStatus)}>{emailDeliveryLabel(item.acknowledgementStatus)}</dd></div></dl>
      {item.message ? <blockquote>{item.message}</blockquote> : null}
      <FollowUpHistory items={item.followUpRequests ?? []} />
      {item.deliveryFailureCode ? <p className="form-status form-status--error">Delivery failure: {item.deliveryFailureCode}</p> : null}
      <ResponseControls deliveryMode={access?.demoRequestDeliveryMode} onChanged={() => { setCalendar(null); setRefreshVersion(value => value + 1); }} preferredStartAt={item.preferredStartAt} preferredTimeZone={item.preferredTimeZone} requestId={item.id} requesterName={`${item.firstName} ${item.lastName}`} schedulingStatus={item.schedulingStatus} />
    </article>)}
    {data ? <nav className="platform-demo-pagination" aria-label="Demo request pages"><button disabled={!data.hasPreviousPage} onClick={() => setPage(value => value - 1)} type="button">Previous</button><span>Page {data.page} · {data.totalCount} requests</span><button disabled={!data.hasNextPage} onClick={() => setPage(value => value + 1)} type="button">Next</button></nav> : null}
  </main>;
}

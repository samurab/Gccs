export const defaultUsTimeZone = "America/New_York";
const usTimeZoneLabels: Record<string, string> = {
  "America/New_York": "Eastern",
  "America/Chicago": "Central",
  "America/Denver": "Mountain",
  "America/Los_Angeles": "Pacific",
  "America/Anchorage": "Alaska",
  "Pacific/Honolulu": "Hawaii",
};

const usDateOnlyFormatter = new Intl.DateTimeFormat("en-US", {
  timeZone: "UTC",
  year: "numeric",
  month: "numeric",
  day: "numeric",
});

const usMonthYearFormatter = new Intl.DateTimeFormat("en-US", {
  timeZone: "UTC",
  month: "short",
  year: "numeric",
});

const usMonthDayFormatter = new Intl.DateTimeFormat("en-US", {
  timeZone: "UTC",
  month: "long",
  day: "numeric",
});

const usWeekdayMonthDayFormatter = new Intl.DateTimeFormat("en-US", {
  timeZone: "UTC",
  weekday: "long",
  month: "long",
  day: "numeric",
});

export function formatUsDateOnly(value: string | null | undefined) {
  if (!value) return "—";
  const date = parseIsoDateOnly(value);
  if (!date) return value;
  return usDateOnlyFormatter.format(date);
}

export function formatUsDateTime(value: string | null | undefined, timeZone?: string) {
  if (!value) return "—";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;
  return new Intl.DateTimeFormat("en-US", {
    year: "numeric",
    month: "numeric",
    day: "numeric",
    hour: "numeric",
    minute: "2-digit",
    timeZone: timeZone ?? defaultUsTimeZone,
  }).format(date);
}

export function formatUsMonthYear(value: string | null | undefined) {
  if (!value) return "—";
  const date = parseYearMonth(value);
  if (!date) return value;
  return usMonthYearFormatter.format(date);
}

export function formatUsMonthDay(value: string | null | undefined) {
  if (!value) return "—";
  const date = parseIsoDateOnly(value);
  if (!date) return value;
  return usMonthDayFormatter.format(date);
}

export function formatUsTimeZoneLabel(value: string | null | undefined) {
  if (!value) return "—";
  return usTimeZoneLabels[value] ?? value;
}

export function formatUsWeekdayMonthDay(value: string | null | undefined) {
  if (!value) return "—";
  const date = parseIsoDateOnly(value);
  if (!date) return value;
  return usWeekdayMonthDayFormatter.format(date);
}

function parseIsoDateOnly(value: string) {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value);
  if (!match) return null;
  const [, year, month, day] = match;
  return new Date(Date.UTC(Number(year), Number(month) - 1, Number(day)));
}

function parseYearMonth(value: string) {
  const match = /^(\d{4})-(\d{2})$/.exec(value);
  if (!match) return null;
  const [, year, month] = match;
  return new Date(Date.UTC(Number(year), Number(month) - 1, 1));
}

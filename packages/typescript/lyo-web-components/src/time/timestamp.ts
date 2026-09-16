/** How `LyoTimestamp` draws a UTC instant in the browser time zone. */
export type LyoTimestampKind = "Absolute" | "Relative" | "TimeUntil" | "TimeSince";

export const DEFAULT_RELATIVE_WINDOW_MS = 24 * 3_600_000;

const HAS_ZONE = /(Z|[+-]\d{2}:?\d{2})$/i;
const ISO_DATE_TIME = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}/;

/** Parses a projected value to epoch ms. A naive ISO date-time (no zone) is read as UTC, matching API payloads. */
export function toUtcMs(value: unknown): number | null {
  if (value == null) return null;
  if (value instanceof Date) return Number.isNaN(value.getTime()) ? null : value.getTime();
  if (typeof value === "number") return Number.isFinite(value) ? value : null;
  if (typeof value !== "string") return null;
  const text = value.trim();
  if (!text) return null;
  const normalized = ISO_DATE_TIME.test(text) && !HAS_ZONE.test(text) ? `${text}Z` : text;
  const ms = Date.parse(normalized);
  return Number.isNaN(ms) ? null : ms;
}

/** Newest instant in a scalar or collection of projected values. */
export function latestUtcMs(value: unknown): number | null {
  const items = Array.isArray(value) ? value : [value];
  let best: number | null = null;
  for (const item of items) {
    const ms = toUtcMs(item);
    if (ms != null && (best == null || ms > best)) best = ms;
  }
  return best;
}

/** Compact relative text (`3h 15m ago`, `in 2h`, `now`) when |delta| is inside the window, otherwise null. */
export function formatRelative(deltaMs: number, windowMs: number = DEFAULT_RELATIVE_WINDOW_MS): string | null {
  const limit = Math.max(0, windowMs);
  const abs = Math.abs(deltaMs);
  if (abs > limit) return null;
  if (abs < 60_000) return "now";
  const hours = Math.floor(abs / 3_600_000);
  const minutes = Math.floor((abs % 3_600_000) / 60_000);
  const body = hours > 0 && minutes > 0 ? `${hours}h ${minutes}m` : hours > 0 ? `${hours}h` : `${minutes}m`;
  return deltaMs < 0 ? `${body} ago` : `in ${body}`;
}

/** Display text for a kind. Relative kinds fall back to `absolute` outside the window or on the wrong side of now. */
export function formatKind(
  kind: LyoTimestampKind,
  utcMs: number,
  absolute: string,
  nowMs: number = Date.now(),
  windowMs: number = DEFAULT_RELATIVE_WINDOW_MS
): string {
  const delta = utcMs - nowMs;
  const relative = () => formatRelative(delta, windowMs) ?? absolute;
  switch (kind) {
    case "Relative":
      return relative();
    case "TimeUntil":
      return delta >= 0 ? relative() : absolute;
    case "TimeSince":
      return delta <= 0 ? relative() : absolute;
    default:
      return absolute;
  }
}

const UTC_ZONES = new Set(["utc", "etc/utc", "etc/universal", "etc/zulu", "universal", "zulu"]);

function zoneNamePart(timeZone: string, date: Date, style: "short" | "long" | "longOffset"): string {
  const part = new Intl.DateTimeFormat("en-US", { timeZone, timeZoneName: style })
    .formatToParts(date)
    .find((p) => p.type === "timeZoneName");
  return part?.value ?? "";
}

function acronym(name: string): string | null {
  if (!name || name.includes("/")) return null;
  const parts = name.split(/[\s_-]+/).filter(Boolean);
  if (parts.length < 3 || parts.length > 5 || parts.some((p) => !/^[A-Za-z]/.test(p))) return null;
  return parts.map((p) => p[0]!.toUpperCase()).join("");
}

function offsetLabel(timeZone: string, date: Date): string {
  const raw = zoneNamePart(timeZone, date, "longOffset");
  const m = /GMT([+-])(\d{1,2})(?::(\d{2}))?/.exec(raw);
  if (!m) return "UTC";
  const hours = Number(m[2]);
  const minutes = Number(m[3] ?? 0);
  return minutes === 0 ? `UTC${m[1]}${hours}` : `UTC${m[1]}${hours}:${String(minutes).padStart(2, "0")}`;
}

/**
 * Short zone label for an instant: `UTC` for UTC zones, a 2-5 letter abbreviation when the runtime has one (EDT), an acronym of a 3-5 word long name
 * (Central European Summer Time → CEST), otherwise a UTC offset (`UTC-4`, `UTC+5:45`). Never the IANA id.
 */
export function timeZoneLabel(timeZone: string, date: Date): string {
  if (UTC_ZONES.has(timeZone.toLowerCase())) return "UTC";
  const short = zoneNamePart(timeZone, date, "short");
  if (/^[A-Za-z]{2,5}$/.test(short)) return short;
  return acronym(zoneNamePart(timeZone, date, "long")) ?? offsetLabel(timeZone, date);
}

/** Browser time zone, or UTC when Intl cannot report one. */
export function browserTimeZone(): string {
  try {
    return Intl.DateTimeFormat().resolvedOptions().timeZone || "UTC";
  } catch {
    return "UTC";
  }
}

/** Short local date + time in `timeZone`, followed by the zone label (`7/1/26, 12:05 PM EDT`). */
export function formatAbsolute(utcMs: number, timeZone: string, locale?: string): string {
  const date = new Date(utcMs);
  const text = new Intl.DateTimeFormat(locale, { timeZone, dateStyle: "short", timeStyle: "short" }).format(date);
  return `${text.replace(/\u202f/g, " ")} ${timeZoneLabel(timeZone, date)}`;
}

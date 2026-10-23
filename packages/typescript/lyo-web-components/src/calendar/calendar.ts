/** Opaque pointer at an entity the host can resolve. The calendar stores it and does not load the record. */
export type CalendarEntityRef = {
  entityType: string;
  entityId: string;
};

/** One placed item. `start === end` is a point. Instants are ISO-8601 UTC. `muted` draws the item gray. */
export type CalendarEntry = {
  key: string;
  title: string;
  start: string;
  end: string;
  description?: string | null;
  hue?: number | null;
  category?: string | null;
  refs?: CalendarEntityRef[];
  muted?: boolean;
};

/** One civil day and the entries that intersect it. `date` is `yyyy-MM-dd`. */
export type CalendarDay = {
  date: string;
  entries: CalendarEntry[];
};

/** Placed civil range. Entries stay UTC. Days follow `timeZone`. */
export type CalendarModel = {
  start: string;
  end: string;
  timeZone: string;
  entries: CalendarEntry[];
  days: CalendarDay[];
};

/** One concrete span that can be placed on a calendar. */
export type TimeFrame = {
  key: string;
  title: string;
  start: string;
  end: string;
  description?: string | null;
  hue?: number | null;
  category?: string | null;
  refs?: CalendarEntityRef[];
  muted?: boolean;
};

type Zoned = { date: string; midnight: boolean };

/** Week grid containing `month` (1–12), padded so every week is full. Sunday starts the week. */
export function forMonth(year: number, month: number, timeZone = "UTC", frames: TimeFrame[] = []): CalendarModel {
  const first = isoDate(year, month, 1);
  const last = addDays(isoDate(year, month + 1, 1), -1);
  const startDow = dayOfWeek(first);
  const gridStart = addDays(first, -startDow);
  const endDow = dayOfWeek(last);
  const gridEnd = addDays(last, (6 - endDow + 7) % 7);
  return fromTimeFrames(gridStart, gridEnd, timeZone, frames);
}

/** Places spans onto an inclusive civil range. A span that misses the range is dropped. */
export function fromTimeFrames(start: string, end: string, timeZone: string, frames: TimeFrame[]): CalendarModel {
  const entries = frames.map(toEntry).filter((entry) => intersectsRange(entry, start, end, timeZone));
  return {
    start,
    end,
    timeZone,
    entries,
    days: buildDays(start, end, timeZone, entries),
  };
}

/** Keeps the same UTC entries and rebuckets civil days into `timeZone`. */
export function withTimeZone(calendar: CalendarModel, timeZone: string): CalendarModel {
  return fromTimeFrames(calendar.start, calendar.end, timeZone, calendar.entries.map(entryToFrame));
}

/** Formats an instant in `timeZone` as `22:00` or `10:00 PM`. */
export function formatCalendarClock(utc: Date, timeZone: string, use24Hour: boolean): string {
  const text = new Intl.DateTimeFormat("en-US", {
    timeZone,
    hour: "2-digit",
    minute: "2-digit",
    hour12: !use24Hour,
  }).format(utc);
  return text.replace(/\u202f/g, " ").replace(/\u00a0/g, " ");
}

/** Formats the zone offset at `utc` as `UTC-04:00` or `UTC+00:00`. */
export function formatCalendarOffset(timeZone: string, utc: Date): string {
  const minutes = offsetMinutes(timeZone, utc);
  const sign = minutes < 0 ? "-" : "+";
  const abs = Math.abs(minutes);
  const hours = String(Math.floor(abs / 60)).padStart(2, "0");
  const mins = String(abs % 60).padStart(2, "0");
  return `UTC${sign}${hours}:${mins}`;
}

/** One placed block on a civil day. `index` matches the input list. All-day blocks ignore lane and percent. */
export type DayBlock = {
  index: number;
  lane: number;
  laneCount: number;
  topPercent: number;
  heightPercent: number;
  allDay: boolean;
};

/** One month-cell row. Several fires in the same local hour share a row. */
export type MonthRow = {
  allDay: boolean;
  hour: number;
  indexes: number[];
};

const MINUTES_PER_DAY = 1440;
const POINT_MINUTES = 30;
const ALL_DAY_MINUTES = 18 * 60;

/** Places each UTC span on `day` in `timeZone`. Overlapping timed entries sit in side-by-side lanes. A span that covers the day is all-day. */
export function placeDayEvents(entries: { start: string; end: string }[], day: string, timeZone: string): DayBlock[] {
  const blocks: DayBlock[] = entries.map((_, index) => ({ index, lane: 0, laneCount: 1, topPercent: 0, heightPercent: 100, allDay: true }));
  const timed: { index: number; start: number; end: number }[] = [];
  entries.forEach((entry, index) => {
    const startMin = minutesOnDay(entry.start, day, timeZone);
    const endMin = minutesOnDay(entry.end, day, timeZone);
    let clipStart = Math.min(Math.max(startMin, 0), MINUTES_PER_DAY);
    let clipEnd = Math.min(Math.max(endMin, 0), MINUTES_PER_DAY);
    if (clipEnd < clipStart) clipEnd = clipStart;
    const coversDay = startMin <= 0 && endMin >= MINUTES_PER_DAY;
    if (coversDay || clipEnd - clipStart >= ALL_DAY_MINUTES) return;
    if (clipEnd <= clipStart) clipEnd = Math.min(MINUTES_PER_DAY, clipStart + POINT_MINUTES);
    timed.push({ index, start: clipStart, end: clipEnd });
  });
  assignLanes(timed, blocks);
  return blocks;
}

/** Collapses a day into at most `maxRows` rows. Extra hour groups are counted in `moreRows`. All-day spans lead. */
export function monthRows(
  entries: { start: string; end: string }[],
  day: string,
  timeZone: string,
  maxRows = 3
): { rows: MonthRow[]; moreRows: number } {
  const placed = placeDayEvents(entries, day, timeZone);
  const allDay: number[] = [];
  const byHour = new Map<number, number[]>();
  placed.forEach((block, index) => {
    if (block.allDay) {
      allDay.push(index);
      return;
    }
    const startMin = minutesOnDay(entries[index]!.start, day, timeZone);
    const hour = startMin < 0 ? 0 : Math.min(23, Math.floor(startMin / 60));
    const bucket = byHour.get(hour) ?? [];
    bucket.push(index);
    byHour.set(hour, bucket);
  });
  const rows: MonthRow[] = [];
  if (allDay.length > 0) rows.push({ allDay: true, hour: -1, indexes: allDay });
  for (const hour of [...byHour.keys()].sort((left, right) => left - right)) rows.push({ allDay: false, hour, indexes: byHour.get(hour) ?? [] });
  if (rows.length <= maxRows) return { rows, moreRows: 0 };
  return { rows: rows.slice(0, maxRows), moreRows: rows.length - maxRows };
}

function assignLanes(timed: { index: number; start: number; end: number }[], blocks: DayBlock[]) {
  const ordered = [...timed].sort((left, right) => left.start - right.start || left.end - right.end);
  let index = 0;
  while (index < ordered.length) {
    const cluster = [];
    let clusterEnd = ordered[index]!.end;
    let next = index;
    while (next < ordered.length && ordered[next]!.start < clusterEnd) {
      cluster.push(ordered[next]!);
      if (ordered[next]!.end > clusterEnd) clusterEnd = ordered[next]!.end;
      next += 1;
    }
    const laneEnds: number[] = [];
    const placed: { item: (typeof ordered)[number]; lane: number }[] = [];
    for (const item of cluster) {
      let lane = laneEnds.findIndex((end) => end <= item.start);
      if (lane < 0) {
        lane = laneEnds.length;
        laneEnds.push(item.end);
      } else laneEnds[lane] = item.end;
      placed.push({ item, lane });
    }
    for (const { item, lane } of placed) {
      blocks[item.index] = {
        index: item.index,
        lane,
        laneCount: laneEnds.length,
        topPercent: (item.start / MINUTES_PER_DAY) * 100,
        heightPercent: ((item.end - item.start) / MINUTES_PER_DAY) * 100,
        allDay: false,
      };
    }
    index = next;
  }
}

function minutesOnDay(utc: string, day: string, timeZone: string): number {
  const parts = zonedParts(new Date(utc), timeZone);
  const date = isoDate(parts.year, parts.month, parts.day);
  return daysBetween(day, date) * MINUTES_PER_DAY + parts.hour * 60 + parts.minute + parts.second / 60;
}

function daysBetween(start: string, end: string): number {
  const [startYear, startMonth, startDay] = start.split("-").map(Number);
  const [endYear, endMonth, endDay] = end.split("-").map(Number);
  return Math.round((Date.UTC(endYear, endMonth - 1, endDay) - Date.UTC(startYear, startMonth - 1, startDay)) / 86_400_000);
}

/** Converts a civil date and `HH:mm` clock in `timeZone` to a UTC ISO string. */
export function civilToUtc(date: string, time: string, timeZone: string): string {
  const [hour, minute] = time.split(":").map(Number);
  const [year, month, day] = date.split("-").map(Number);
  const guess = Date.UTC(year, month - 1, day, hour || 0, minute || 0, 0);
  const first = new Date(guess - offsetMinutes(timeZone, new Date(guess)) * 60_000);
  const second = new Date(guess - offsetMinutes(timeZone, first) * 60_000);
  return second.toISOString();
}

/** IANA zones the browser can name. Always includes UTC. */
export function calendarTimeZones(): string[] {
  const intl = Intl as typeof Intl & { supportedValuesOf?: (key: string) => string[] };
  const zones = intl.supportedValuesOf?.("timeZone") ?? [];
  return zones.includes("UTC") ? zones : ["UTC", ...zones];
}

function toEntry(frame: TimeFrame): CalendarEntry {
  return {
    key: frame.key,
    title: frame.title,
    start: frame.start,
    end: frame.end,
    description: frame.description,
    hue: frame.hue,
    category: frame.category,
    refs: frame.refs ?? [],
    muted: frame.muted,
  };
}

function entryToFrame(entry: CalendarEntry): TimeFrame {
  return { ...entry, refs: entry.refs ?? [] };
}

function buildDays(start: string, end: string, timeZone: string, entries: CalendarEntry[]): CalendarDay[] {
  const days: CalendarDay[] = [];
  for (let date = start; date <= end; date = addDays(date, 1)) {
    const onDay = entries
      .filter((entry) => intersectsRange(entry, date, date, timeZone))
      .sort((a, b) => a.start.localeCompare(b.start) || a.key.localeCompare(b.key));
    days.push({ date, entries: onDay });
  }
  return days;
}

function intersectsRange(entry: CalendarEntry, start: string, end: string, timeZone: string): boolean {
  const localStart = zoned(new Date(entry.start), timeZone);
  const localEnd = zoned(new Date(entry.end), timeZone);
  const point = entry.start === entry.end || (localStart.date === localEnd.date && sameInstant(entry));
  if (point) return localStart.date >= start && localStart.date <= end;

  const last = localEnd.midnight ? addDays(localEnd.date, -1) : localEnd.date;
  return localStart.date <= end && last >= start;
}

function sameInstant(entry: CalendarEntry): boolean {
  return new Date(entry.start).getTime() === new Date(entry.end).getTime();
}

function zoned(utc: Date, timeZone: string): Zoned {
  const dtf = new Intl.DateTimeFormat("en-US", {
    timeZone,
    hourCycle: "h23",
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
  });
  const bag: Record<string, string> = {};
  for (const part of dtf.formatToParts(utc)) {
    if (part.type !== "literal") bag[part.type] = part.value;
  }
  let hour = Number(bag.hour);
  const minute = Number(bag.minute);
  const second = Number(bag.second);
  if (hour === 24) hour = 0;
  return {
    date: `${bag.year}-${bag.month}-${bag.day}`,
    midnight: hour === 0 && minute === 0 && second === 0,
  };
}

function offsetMinutes(timeZone: string, utc: Date): number {
  const parts = zonedParts(utc, timeZone);
  const asUtc = Date.UTC(parts.year, parts.month - 1, parts.day, parts.hour, parts.minute, parts.second);
  return Math.round((asUtc - utc.getTime()) / 60_000);
}

function zonedParts(utc: Date, timeZone: string): { year: number; month: number; day: number; hour: number; minute: number; second: number } {
  const dtf = new Intl.DateTimeFormat("en-US", {
    timeZone,
    hourCycle: "h23",
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
  });
  const bag: Record<string, string> = {};
  for (const part of dtf.formatToParts(utc)) {
    if (part.type !== "literal") bag[part.type] = part.value;
  }
  let hour = Number(bag.hour);
  if (hour === 24) hour = 0;
  return {
    year: Number(bag.year),
    month: Number(bag.month),
    day: Number(bag.day),
    hour,
    minute: Number(bag.minute),
    second: Number(bag.second),
  };
}

function isoDate(year: number, month: number, day: number): string {
  const dt = new Date(Date.UTC(year, month - 1, day));
  const y = dt.getUTCFullYear();
  const m = String(dt.getUTCMonth() + 1).padStart(2, "0");
  const d = String(dt.getUTCDate()).padStart(2, "0");
  return `${y}-${m}-${d}`;
}

function addDays(iso: string, days: number): string {
  const [year, month, day] = iso.split("-").map(Number);
  return isoDate(year, month, day + days);
}

function dayOfWeek(iso: string): number {
  const [year, month, day] = iso.split("-").map(Number);
  return new Date(Date.UTC(year, month - 1, day)).getUTCDay();
}
